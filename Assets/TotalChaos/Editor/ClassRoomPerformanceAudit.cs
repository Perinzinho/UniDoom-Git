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
using TotalChaosMaps;

// Captures only read assets. Optimize explicitly saves the requested prefab and
// material keywords; all reports and backups are deliberately outside Assets.
public static class ClassRoomPerformanceAudit
{
    const string Prefab = "Assets/prefabs/Scenario/ClassRoom.prefab";
    const string Interior = "Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab";
    const string Output = ".utmp/ClassRoomOptimization/";

    public static void Before() => Capture("before");
    public static void After() => Capture("after");

    [Serializable] sealed class Binding { public string material, property, oldGuid, texture; }
    [Serializable] sealed class BindingList { public Binding[] bindings; }

    public static void RepairAndOptimize()
    {
        RepairTextures();
        Capture("repaired");
        Optimize();
    }

    [MenuItem("Tools/Total Chaos/Repair ClassRoom Textures")]
    public static void RepairTextures()
    {
        Directory.CreateDirectory(Output);
        var repairs = JsonUtility.FromJson<BindingList>(File.ReadAllText("Tools/ClassRoom/Optimization/texture-bindings.json"));
        var paths = AssetDatabase.GetDependencies(Prefab, true)
            .Where(p => p.EndsWith(".mat", StringComparison.Ordinal) &&
                (p.StartsWith("Assets/Art/ClassRoom/", StringComparison.Ordinal) ||
                 p.StartsWith("Assets/Art/SkillAtmosphere/Materials/ClassRoom/", StringComparison.Ordinal))).ToArray();
        var dirty = new HashSet<Material>();
        var lines = new List<string>();
        foreach (var binding in repairs.bindings)
        {
            if (!paths.Contains(binding.material)) throw new Exception("Repair outside ClassRoom dependencies: " + binding.material);
            var material = AssetDatabase.LoadAssetAtPath<Material>(binding.material);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(binding.texture);
            if (!material || !texture || !material.HasProperty(binding.property)) throw new Exception("Invalid repair binding: " + binding.material);
            if (material.GetTexture(binding.property)) continue;
            string serialized = File.ReadAllText(binding.material);
            if (!Regex.IsMatch(serialized, Regex.Escape("    - " + binding.property + ":") +
                @"\r?\n        m_Texture: \{fileID: \d+, guid: " + binding.oldGuid + @", type: \d+\}"))
                throw new Exception("Texture binding changed since the repair inventory: " + binding.material + " " + binding.property);
            string backup = Output + "source-before/" + binding.material;
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(binding.material, backup);
            material.SetTexture(binding.property, texture);
            dirty.Add(material);
            lines.Add(binding.material + " " + binding.property + " -> " + binding.texture);
        }
        // Normal vectors and scalar masks must be linear. Preserve resolution,
        // filtering, compression and the texture GUID; never regenerate imagery.
        var imports = new Dictionary<string, bool>();
        foreach (string path in paths)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            foreach (string property in new[] { "_BumpMap", "_DetailNormalMap", "_MetallicGlossMap", "_OcclusionMap", "_DirtMap", "_ParallaxMap" })
            {
                if (!material.HasProperty(property) || !material.GetTexture(property)) continue;
                string texturePath = AssetDatabase.GetAssetPath(material.GetTexture(property));
                if (!texturePath.StartsWith("Assets/Art/ClassRoom/", StringComparison.Ordinal)) continue;
                bool normal = property == "_BumpMap" || property == "_DetailNormalMap";
                if (imports.TryGetValue(texturePath, out bool previous) && previous != normal)
                    throw new Exception("Conflicting texture roles: " + texturePath);
                imports[texturePath] = normal;
            }
        }
        // Persist bindings before reimports can unload material objects.
        foreach (var material in dirty) { EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material); }
        foreach (var entry in imports)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(entry.Key);
            var type = entry.Value ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType == type && !importer.sRGBTexture) continue;
            string backup = Output + "source-before/" + entry.Key + ".meta";
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(entry.Key + ".meta", backup);
            importer.textureType = type;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
            lines.Add("Linear/normal import: " + entry.Key);
        }
        foreach (string path in paths)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material.shader.name != "UniDoom/TotalChaosInspiredSurface" && material.shader.name != "Universal Render Pipeline/Lit") continue;
            string backup = Output + "source-before/" + path;
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(path, backup);
            foreach (var pair in new[] { ("_BumpMap", "_NORMALMAP"), ("_MetallicGlossMap", "_METALLICSPECGLOSSMAP"), ("_OcclusionMap", "_OCCLUSIONMAP") })
                if (material.GetTexture(pair.Item1)) material.EnableKeyword(pair.Item2);
            // Separate texture restoration from the shared-sample optimization.
            material.DisableKeyword("_TC_SHARED_MASK");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }
        File.WriteAllLines(Output + "texture-repairs.txt", lines);
        Debug.Log("CLASSROOM_TEXTURE_REPAIR_OK " + lines.Count + " binding/import operations");
    }

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
            throw new Exception("ClassRoom validation build: " + result.summary.result);
        Debug.Log("CLASSROOM_VALIDATION_BUILD_OK");
    }

    [MenuItem("Tools/Total Chaos/Optimize ClassRoom Assets")]
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
        // Edit the owner asset, preserving nested prefab links and existing overrides.
        foreach (string target in new[] { Prefab, Interior })
        {
            string backup = Output + "source-before/" + target;
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(target, backup);
            var root = PrefabUtility.LoadPrefabContents(target);
            bool changed = false;
            try
            {
                foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
                    if (!PrefabUtility.IsPartOfPrefabInstance(filter) && filter.sharedMesh &&
                        replacements.TryGetValue(filter.sharedMesh, out var shared))
                    {
                        changes.Add(target + ": " + AssetDatabase.GetAssetPath(filter.sharedMesh) + " -> " + AssetDatabase.GetAssetPath(shared));
                        filter.sharedMesh = shared;
                        changed = true;
                    }
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, target, out bool saved);
                    if (!saved) throw new Exception("Could not save " + target);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (var path in dependencies.Where(p => p.EndsWith(".mat", StringComparison.Ordinal)))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!path.StartsWith("Assets/Art/ClassRoom/", StringComparison.Ordinal) || !material || material.shader.name != "UniDoom/TotalChaosInspiredSurface") continue;
            // Loading a prefab can validate the material in memory first. Compare
            // the on-disk state so the resulting keyword is always persisted.
            bool previous = File.ReadAllText(path).Contains("  - _TC_SHARED_MASK");
            TotalChaosMaterialGUI.UpdateSharedMaskKeyword(material);
            string materialBackup = Output + "material-backups/" + path;
            Directory.CreateDirectory(Path.GetDirectoryName(materialBackup));
            if (!File.Exists(materialBackup)) File.Copy(path, materialBackup);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            if (previous != material.IsKeywordEnabled("_TC_SHARED_MASK")) changes.Add("Shared mask keyword: " + path);
        }
        File.WriteAllLines(Output + "changes.txt", changes);
        Debug.Log("CLASSROOM_OPTIMIZED: " + changes.Count + " reference/keyword changes");
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
        if (components.Any(c => !c)) throw new Exception("Missing script in classroom");
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        var materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
        if (materials.Any(m => !m || !m.shader)) throw new Exception("Missing material/shader");
        var filters = root.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Any(f => !f.sharedMesh && f.GetComponent<Renderer>()?.enabled == true))
            throw new Exception("Missing rendered mesh: " + string.Join(", ", filters.Where(f => !f.sharedMesh).Select(f => f.name)));
        var meshes = filters.Select(f => f.sharedMesh).Where(m => m).Distinct().ToArray();
        var lines = components.GroupBy(c => c.GetType().FullName).OrderBy(g => g.Key)
            .Select(g => g.Key + ": " + g.Count()).ToList();
        lines.Add("Distinct renderer materials: " + materials.Length);
        lines.Add("Distinct MeshFilter meshes: " + meshes.Length);
        lines.Add("Mesh runtime memory bytes (Editor, not Player): " + meshes.Sum(m => Profiler.GetRuntimeMemorySizeLong(m)));
        lines.Add("Unique mesh vertices: " + meshes.Sum(m => (long)m.vertexCount));
        lines.AddRange(materials.GroupBy(m => m.shader.name).Select(g => "Shader " + g.Key + ": " + g.Count() + " materials"));
        lines.Add("Shared mask keyword materials: " + materials.Count(m => m.IsKeywordEnabled("_TC_SHARED_MASK")));
        lines.AddRange(materials.Where(m => m.shader.name == "UniDoom/TotalChaosInspiredSurface")
            .Select(m => AssetDatabase.GetAssetPath(m) + ": " + string.Join(" ", m.shaderKeywords)));
        lines.Add("Shadow-casting active lights: " + root.GetComponentsInChildren<Light>().Count(l => l.enabled && l.shadows != LightShadows.None));
        File.WriteAllLines(Output + label + "/inventory.txt", lines);
        var interior = root.transform.Find("AbandonedClassRoomInterior");
        if (!interior) throw new Exception("ClassRoom interior missing");
        CaptureViews(interior, label);
        if (label == "after")
        {
            var packed = materials.Where(m => m.IsKeywordEnabled("_TC_SHARED_MASK")).ToArray();
            try
            {
                foreach (var material in packed) material.DisableKeyword("_TC_SHARED_MASK");
                CaptureViews(interior, label + "/fallback");
            }
            finally { foreach (var material in packed) material.EnableKeyword("_TC_SHARED_MASK"); }
        }
        // Check errors from the variants exercised by these cameras.
        foreach (var shader in materials.Select(m => m.shader).Distinct())
        {
            var errors = ShaderUtil.GetShaderMessages(shader).Where(m => m.severity.ToString() == "Error").ToArray();
            if (errors.Length > 0) throw new Exception(shader.name + ": " + string.Join("\n", errors.Select(e => e.message)));
        }
        Object.DestroyImmediate(root);
        Debug.Log("CLASSROOM_PERFORMANCE_AUDIT_OK " + label);
    }

    static void CaptureViews(Transform interior, string label)
    {
        Directory.CreateDirectory(Output + label);
        var positions = new[] { new Vector3(2.1f,1.84f,-10.35f), new Vector3(5.6f,1.84f,1.1f), new Vector3(-4.25f,1.84f,3.6f), new Vector3(2.1f,1.84f,-10.35f) };
        var targets = new[] { new Vector3(.3f,1.5f,4.65f), new Vector3(7.5f,.55f,3.5f), new Vector3(2,1.2f,-7), new Vector3(.3f,1.5f,4.65f) };
        var names = new[] { "aisle", "front", "door", "flashlight" };
        for (int i = 0; i < names.Length; i++) Render(interior, positions[i], targets[i], label + "/" + names[i], i == 3);
    }

    static void Render(Transform area, Vector3 position, Vector3 look, string path, bool flashlight)
    {
        var go = new GameObject("Audit camera");
        var camera = go.AddComponent<Camera>();
        var data = go.AddComponent<UniversalAdditionalCameraData>();
        camera.enabled = false;
        camera.fieldOfView = 70;
        data.volumeLayerMask = 1;
        camera.nearClipPlane = .04f;
        camera.farClipPlane = 150;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.02f, .015f, .012f);
        go.transform.position = area.TransformPoint(position);
        go.transform.LookAt(area.TransformPoint(look));
        var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGBHalf);
        var ldr = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32);
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
                TCRegionAtmosphere.ApplyForCamera(camera, area.GetComponentsInChildren<TCRegionAtmosphere>(), flashlight);
                for (int i = 0; i < 8; i++) camera.Render();
                Graphics.Blit(target, ldr);
                RenderTexture.active = ldr;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                File.WriteAllBytes(Output + path + "-renderer" + renderer + ".png", image.EncodeToPNG());
            }
        }
        finally
        {
            TCRegionAtmosphere.RestoreEnvironment();
            camera.targetTexture = null;
            QualitySettings.renderPipeline = previousPipeline;
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(ldr);
            Object.DestroyImmediate(go);
        }
    }
}
