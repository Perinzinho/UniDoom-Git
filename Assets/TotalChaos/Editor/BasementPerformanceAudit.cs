using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEditor.Build.Reporting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

// Captures only read assets. Optimize explicitly saves the requested prefab and
// material keywords; all reports and backups are deliberately outside Assets.
public static class BasementPerformanceAudit
{
    const string Prefab = "Assets/prefabs/Scenario/Basement.prefab";
    const string Output = ".utmp/BasementOptimization/";

    public static void Before() => Capture("before");
    public static void After() => Capture("after");

    public static void ValidateBuild()
    {
        After();
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Output + "Build/UniDoom.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        });
        if (result.summary.result != BuildResult.Succeeded)
            throw new Exception("Basement validation build: " + result.summary.result);
        Debug.Log("BASEMENT_VALIDATION_BUILD_OK");
    }

    [MenuItem("Tools/Total Chaos/Optimize Basement Assets")]
    public static void Optimize()
    {
        Directory.CreateDirectory(Output);
        var dependencies = AssetDatabase.GetDependencies(Prefab, true);
        var canonical = new Dictionary<string, Mesh>();
        var replacements = new Dictionary<Mesh, Mesh>();
        var changes = new List<string>();
        // Compare every serialized mesh field, including bounds, UVs, indices,
        // readability and vertex layout. Only the asset name may differ.
        foreach (var path in dependencies.OrderBy(p => p, StringComparer.Ordinal))
        {
            if (!path.EndsWith(".asset", StringComparison.Ordinal)) continue;
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) continue;
            string serialized = Regex.Replace(File.ReadAllText(path), @"(?m)^  m_Name:[^\r\n]*", "");
            if (canonical.TryGetValue(serialized, out var existing)) replacements.Add(mesh, existing);
            else canonical.Add(serialized, mesh);
        }
        // Backup the exact bytes; do not overwrite the first baseline on reruns.
        string backup = Output + "Basement.prefab.before";
        if (!File.Exists(backup)) File.Copy(Prefab, backup);
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        bool changed = false;
        try
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh && replacements.TryGetValue(filter.sharedMesh, out var shared))
                {
                    changes.Add(AssetDatabase.GetAssetPath(filter.sharedMesh) + " -> " + AssetDatabase.GetAssetPath(shared));
                    filter.sharedMesh = shared;
                    changed = true;
                }
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true))
                if (collider.sharedMesh && replacements.TryGetValue(collider.sharedMesh, out var shared))
                { collider.sharedMesh = shared; changed = true; }
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, Prefab, out bool saved);
                if (!saved) throw new Exception("Could not save basement prefab");
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        foreach (var path in dependencies.Where(p => p.EndsWith(".mat", StringComparison.Ordinal)))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material || material.shader.name != "UniDoom/TotalChaosInspiredSurface") continue;
            bool previous = material.IsKeywordEnabled("_TC_SHARED_MASK");
            TotalChaosMaterialGUI.UpdateSharedMaskKeyword(material);
            if (previous == material.IsKeywordEnabled("_TC_SHARED_MASK")) continue;
            string materialBackup = Output + "material-backups/" + path;
            Directory.CreateDirectory(Path.GetDirectoryName(materialBackup));
            if (!File.Exists(materialBackup)) File.Copy(path, materialBackup);
            EditorUtility.SetDirty(material);
            changes.Add("Shared mask keyword: " + path);
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines(Output + "changes.txt", changes);
        Debug.Log("BASEMENT_OPTIMIZED: " + changes.Count + " reference/keyword changes");
        // The interactive menu must leave the user's current scene open.
        if (Application.isBatchMode) After();
    }

    static void Capture(string label)
    {
        Directory.CreateDirectory(Output + label);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ShaderUtil.allowAsyncCompilation = false;
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        root.transform.position = Vector3.zero;
        var components = root.GetComponentsInChildren<Component>(true);
        if (components.Any(c => !c)) throw new Exception("Missing script in basement");
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        var materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
        if (materials.Any(m => !m || !m.shader)) throw new Exception("Missing material/shader");
        var meshes = root.GetComponentsInChildren<MeshFilter>(true).Select(f => f.sharedMesh).Distinct().ToArray();
        if (meshes.Any(m => !m)) throw new Exception("Missing mesh");
        var lines = components.GroupBy(c => c.GetType().FullName).OrderBy(g => g.Key)
            .Select(g => g.Key + ": " + g.Count()).ToList();
        lines.Add("Distinct renderer materials: " + materials.Length);
        lines.Add("Distinct MeshFilter meshes: " + meshes.Length);
        lines.Add("Mesh runtime memory bytes (Editor, not Player): " + meshes.Sum(m => Profiler.GetRuntimeMemorySizeLong(m)));
        lines.Add("Unique mesh vertices: " + meshes.Sum(m => (long)m.vertexCount));
        lines.AddRange(materials.GroupBy(m => m.shader.name).Select(g => "Shader " + g.Key + ": " + g.Count() + " materials"));
        lines.Add("Shadow-casting active lights: " + root.GetComponentsInChildren<Light>().Count(l => l.enabled && l.shadows != LightShadows.None));
        File.WriteAllLines(Output + label + "/inventory.txt", lines);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.085f, .075f, .063f);
        RenderSettings.fog = false;
        var arena = root.transform.Find("PreBossArea");
        var boss = root.transform.Find("BossRoom");
        float af = Floor(arena.Find("Floor/Hall_Floor"));
        float bf = Floor(boss.Find("Floor/Main_Floor"));
        float front = boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
        Render(arena, new Vector3(2, af + 1.84f, -12), new Vector3(13, af + 3, 14), label + "/arena");
        Render(arena, new Vector3(3, af + 1.84f, 13), new Vector3(0, af + 2.3f, 19), label + "/gate");
        Render(boss, new Vector3(0, bf + 1.84f, front + 2), new Vector3(-10, bf + 5.2f, front + 33), label + "/boss");
        if (label == "after")
        {
            var packed = materials.Where(m => m.IsKeywordEnabled("_TC_SHARED_MASK")).ToArray();
            try
            {
                foreach (var material in packed) material.DisableKeyword("_TC_SHARED_MASK");
                Render(arena, new Vector3(2, af + 1.84f, -12), new Vector3(13, af + 3, 14), label + "/arena-fallback");
                Render(boss, new Vector3(0, bf + 1.84f, front + 2), new Vector3(-10, bf + 5.2f, front + 33), label + "/boss-fallback");
            }
            finally { foreach (var material in packed) material.EnableKeyword("_TC_SHARED_MASK"); }
            VerifyEncounter(root);
        }
        // Check errors from the variants exercised by these cameras.
        foreach (var shader in materials.Select(m => m.shader).Distinct())
        {
            var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity.ToString() == "Error").ToArray();
            if (errors.Length > 0) throw new Exception(shader.name + ": " + string.Join("\n", errors.Select(e => e.message)));
        }
        Object.DestroyImmediate(root);
        Debug.Log("BASEMENT_PERFORMANCE_AUDIT_OK " + label);
    }

    static float Floor(Transform t) => t.localPosition.y + t.localScale.y * .5f;

    // Same edit-mode encounter simulation used by the existing BasementBuilder.
    // This is a behavior check, not a Player/Play Mode performance measurement.
    static void VerifyEncounter(GameObject room)
    {
        var encounter = room.GetComponentInChildren<BasementArenaEncounter>();
        Call(encounter, "Awake");
        Call(encounter, "Update");
        if (encounter.Started || encounter.Cleared) throw new Exception("Arena unlocked before activation");
        var boss = Object.Instantiate(encounter.enemyPrefab, room.transform.Find("BossRoom"));
        boss.transform.localPosition = new Vector3(0, 1.25f, .5f);
        Call(boss.GetComponent<Enemy>(), "Awake");
        encounter.BeginEncounter();
        if (encounter.RemainingEnemies != 12 || encounter.Cleared) throw new Exception("Wrong enemy roster");
        var enemies = encounter.arena.GetComponentsInChildren<Enemy>();
        foreach (var enemy in enemies) Call(enemy, "Awake");
        for (int i = 0; i < enemies.Length - 1; i++) enemies[i].TakeDamage(10000);
        Call(encounter, "Update");
        if (encounter.Cleared || encounter.RemainingEnemies != 1) throw new Exception("Gate opened early");
        enemies.Last().TakeDamage(10000);
        Call(encounter, "Update");
        if (!encounter.Cleared || encounter.RemainingEnemies != 0 || boss.GetComponent<Enemy>().IsDead)
            throw new Exception("Arena clear/boss isolation failed");
        typeof(BasementArenaEncounter).GetField("openProgress", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(encounter, 1f);
        Call(encounter, "Update");
        if (encounter.gateLeaf.localPosition.y < 1.07f) throw new Exception("Gate failed to lift");
        foreach (var enemy in enemies) Object.DestroyImmediate(enemy.gameObject);
        Object.DestroyImmediate(boss);
        Debug.Log("BASEMENT_ENCOUNTER_CHECK_OK: 12 enemies, final death unlocks gate, boss excluded");
    }

    static void Call(object target, string method) => target.GetType()
        .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);

    static void Render(Transform area, Vector3 position, Vector3 look, string path)
    {
        var go = new GameObject("Audit camera");
        var camera = go.AddComponent<Camera>();
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        camera.fieldOfView = 72;
        camera.nearClipPlane = .04f;
        camera.farClipPlane = 150;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.02f, .015f, .012f);
        go.transform.position = area.TransformPoint(position);
        go.transform.LookAt(area.TransformPoint(look));
        var target = new RenderTexture(1280, 720, 24);
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var previousPipeline = QualitySettings.renderPipeline;
        try
        {
            camera.targetTexture = target;
            for (int renderer = 0; renderer < 2; renderer++)
            {
                QualitySettings.renderPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                    renderer == 0 ? "Assets/Settings/PC_RPAsset.asset" : "Assets/Settings/Mobile_RPAsset.asset");
                data.SetRenderer(0);
                for (int i = 0; i < 4; i++) camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Output + path + "-renderer" + renderer + ".png", image.EncodeToPNG());
            }
        }
        finally
        {
            camera.targetTexture = null;
            QualitySettings.renderPipeline = previousPipeline;
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(go);
        }
    }
}
