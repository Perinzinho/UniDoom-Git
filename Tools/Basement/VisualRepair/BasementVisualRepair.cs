using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object = UnityEngine.Object;

public static class BasementVisualRepair
{
    const string Room = "Assets/prefabs/Scenario/Basement.prefab";
    const string Arena = "Assets/Art/Basement/BasementFinalArena.prefab";
    const string Sanctum = "Assets/Art/Basement/BasementFinalSanctum.prefab";
    const string Art = "Assets/Art/Basement/VisualRepair";
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
    static readonly List<string> report = new List<string>();
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static readonly Dictionary<string, List<CombineInstance>> batches = new Dictionary<string, List<CombineInstance>>();
    static readonly List<Mesh> temporary = new List<Mesh>();

    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);
    static Transform Group(Transform parent, string name)
    { var t = new GameObject(name).transform; t.SetParent(parent, false); return t; }
    static Material Material(string name, string source, Color tint, float smoothness)
    {
        string path = Art + "/Materials/" + name + ".mat";
        var m = Load<Material>(path);
        if (m) return materials[name] = m;
        m = string.IsNullOrEmpty(source) ? new Material(Shader.Find("Universal Render Pipeline/Lit")) : new Material(Load<Material>(source));
        m.name = name; m.SetColor("_BaseColor", tint); m.SetFloat("_Smoothness", smoothness);
        m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black);
        AssetDatabase.CreateAsset(m, path); return materials[name] = m;
    }
    static void SetupMaterials()
    {
        foreach (string folder in new[] { "Meshes", "Materials", "Textures" }) Directory.CreateDirectory(Art + "/" + folder);
        AssetDatabase.Refresh(); materials.Clear();
        Material("RitualPigment", null, new Color(.38f, .105f, .065f), .03f);
        Material("RitualChalk", null, new Color(.46f, .40f, .30f), .02f);
        Material("OxidizedServiceSteel", "Assets/Art/Basement/ArenaPolish/Materials/Oxidized_structural_steel.mat", new Color(.34f, .31f, .26f), .22f);
        Material("ServiceEnamel", "Assets/Art/Basement/ArenaPolish/Materials/Aged_painted_equipment.mat", new Color(.20f, .25f, .23f), .17f);
        Material("CableRubber", "Assets/Art/Basement/ArenaPolish/Materials/Black_rubber_cable.mat", new Color(.075f, .069f, .057f), .12f);
        Material("MineralDamp", "Assets/Art/Basement/DetailPolish/Materials/Mineral_runoff.mat", new Color(.17f, .19f, .17f), .32f);
    }
    static Mesh StoreMesh(string name, Mesh generated)
    {
        generated.name = name;
        string path = Art + "/Meshes/" + name + ".asset";
        var stored = Load<Mesh>(path);
        if (!stored) { AssetDatabase.CreateAsset(generated, path); return generated; }
        EditorUtility.CopySerialized(generated, stored); Object.DestroyImmediate(generated); return stored;
    }
    static MeshRenderer MeshObject(Transform parent, string name, Mesh mesh, Material material)
    {
        var t = Group(parent, name); t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = t.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; return renderer;
    }
    static void Add(string key, Mesh mesh, Vector3 position, Quaternion rotation)
    {
        if (!batches.TryGetValue(key, out var instances)) batches[key] = instances = new List<CombineInstance>();
        instances.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation, Vector3.one) });
        temporary.Add(mesh);
    }
    static void Box(string key, Vector3 p, Vector3 size, Quaternion q)
        => Add(key, ChaosGeometry.BevelBox(size, .012f), p, q);
    static void Tube(string key, Vector3[] points, float radius)
        => Add(key, ChaosGeometry.Sweep(points, radius, 10), Vector3.zero, Quaternion.identity);
    static void Flush(Transform parent, string prefix, bool reuseObjects = false)
    {
        foreach (var entry in batches)
        {
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(entry.Value.ToArray(), true, true); mesh.RecalculateBounds();
            mesh = StoreMesh(prefix + "_" + entry.Key, mesh);
            var old = parent.Find(entry.Key);
            if (reuseObjects && old)
            {
                old.GetComponent<MeshFilter>().sharedMesh = mesh;
                PrefabUtility.RecordPrefabInstancePropertyModifications(old.GetComponent<MeshFilter>());
            }
            else MeshObject(parent, entry.Key, mesh, materials[entry.Key]);
        }
        batches.Clear(); foreach (var mesh in temporary) Object.DestroyImmediate(mesh); temporary.Clear();
    }

    static Mesh TrimOldFloorPaint(Mesh source, string name)
    {
        var mesh = Object.Instantiate(source); var vertices = source.vertices; var triangles = source.triangles;
        var keep = new List<int>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
            var center = (a + b + c) / 3;
            bool floorPaint = Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < .09f;
            bool withinSeal = new Vector2(center.x, center.z - 2.4f).magnitude < 4.15f;
            if (!floorPaint || !withinSeal) keep.AddRange(new[] { triangles[i], triangles[i + 1], triangles[i + 2] });
        }
        mesh.triangles = keep.ToArray(); mesh.RecalculateBounds(); return StoreMesh(name, mesh);
    }
    static Mesh Annulus(Vector3 center, float radius, float width, int segments = 256)
    {
        var builder = new ChaosGeometry.Builder();
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2 / segments, b = (i + 1) * Mathf.PI * 2 / segments;
            Func<float, float, Vector3> P = (angle, r) => center + V(Mathf.Sin(angle) * r, 0, Mathf.Cos(angle) * r);
            builder.Face(P(a, radius - width * .5f), P(a, radius + width * .5f), P(b, radius + width * .5f), P(b, radius - width * .5f));
        }
        return builder.Mesh("Continuous paint ring");
    }
    static Mesh Disk(Vector3 center, float radius)
    {
        var builder = new ChaosGeometry.Builder();
        for (int i = 0; i < 20; i++)
        {
            float a = i * Mathf.PI / 10, b = (i + 1) * Mathf.PI / 10;
            builder.Face(center, center + V(Mathf.Sin(a) * radius, 0, Mathf.Cos(a) * radius), center + V(Mathf.Sin(b) * radius, 0, Mathf.Cos(b) * radius));
        }
        return builder.Mesh("Closed paint joint");
    }
    static void RepairSymbol(GameObject root)
    {
        var prep = root.transform.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Ritual preparation and votive procession");
        foreach (string name in new[] { "Preparation_Oxidized_ritual_blood", "Preparation_Scraped_bone_chalk" })
        {
            var filter = prep.Find(name).GetComponent<MeshFilter>();
            var source = Load<Mesh>("Assets/Art/Basement/RitualExpansion/Meshes/" + name + ".asset");
            filter.sharedMesh = TrimOldFloorPaint(source, name + "_WithoutOldSeal");
        }
        if (prep.Find("Restored floor seal")) throw new Exception("Restore isolated prefab baseline before rebuilding");
        var group = Group(prep, "Restored floor seal");
        var center = V(0, .012f, 2.4f);
        Add("RitualPigment", Annulus(center, 3.25f, .095f), Vector3.zero, Quaternion.identity);
        Add("RitualPigment", Annulus(center + V(0, .0004f, 0), 3.02f, .045f), Vector3.zero, Quaternion.identity);
        for (int i = 0; i < 5; i++)
        {
            float a = i * Mathf.PI * 2 / 5, b = (i + 2) * Mathf.PI * 2 / 5;
            Vector3 p = center + V(Mathf.Sin(a) * 2.68f, .0008f + i * .00025f, Mathf.Cos(a) * 2.68f);
            Vector3 q = center + V(Mathf.Sin(b) * 2.68f, .0008f + i * .00025f, Mathf.Cos(b) * 2.68f);
            var across = Vector3.Cross(Vector3.up, (q - p).normalized) * .052f;
            var builder = new ChaosGeometry.Builder(); builder.Face(p - across, q - across, q + across, p + across);
            Add("RitualPigment", builder.Mesh("Unbroken pentagram stroke"), Vector3.zero, Quaternion.identity);
            var tip = center + V(Mathf.Sin(a) * 2.68f, .002f, Mathf.Cos(a) * 2.68f);
            Add("RitualPigment", Disk(tip, .052f), Vector3.zero, Quaternion.identity);
            var tick = center + V(Mathf.Sin(a) * 3.14f, .003f, Mathf.Cos(a) * 3.14f);
            Box("RitualChalk", tick, V(.035f, .001f, .13f), Quaternion.Euler(0, a * Mathf.Rad2Deg, 0));
        }
        Flush(group, "RestoredSeal");
    }

    static void Corridor(Transform floor, string suffix, bool sceneOverride)
    {
        var collider = floor.GetComponent<BoxCollider>();
        var scale = floor.lossyScale;
        var size = Vector3.Scale(collider.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        bool alongX = floor.name.EndsWith(" X");
        float half = (alongX ? size.x : size.z) * .5f - .3f;
        // The low end overlaps the other corridor at the elbow. There is no
        // backing wall there; stop service lines and stains before the opening.
        float start = -half + 3.0f, end = half - .5f;
        float middle = (start + end) * .5f, span = end - start;
        // Corridor wall colliders start at the floor's outer edge.
        float wall = (alongX ? size.z : size.x) * .5f - .08f;
        Func<float, float, float, Vector3> P = (s, y, t) => alongX ? V(s, y, t) : V(t, y, s);
        Quaternion rotation = alongX ? Quaternion.Euler(0, 90, 0) : Quaternion.identity;
        var group = floor.Find("Abandoned service installation");
        if (!group) group = Group(floor, "Abandoned service installation");
        group.localPosition = collider.center + V(0, collider.size.y * .5f, 0);
        group.localScale = V(1 / Mathf.Abs(scale.x), 1 / Mathf.Abs(scale.y), 1 / Mathf.Abs(scale.z));
        if (sceneOverride) PrefabUtility.RecordPrefabInstancePropertyModifications(group);
        var old = floor.Find("Ritual passage thresholds");
        foreach (var renderer in old.GetComponentsInChildren<MeshRenderer>(true))
            if (renderer.name.Contains("Oxidized_ritual_blood") || renderer.name.Contains("Scraped_bone_chalk")) renderer.enabled = false;
        // Two service lines with real joints and clamps; keep the central route clear.
        foreach (float height in new[] { 2.12f, 2.44f })
        {
            Tube("OxidizedServiceSteel", new[] { P(start, height, wall - .10f), P(end, height, wall - .10f) }, height < 2.2f ? .064f : .042f);
            for (float s = start + .3f; s < end; s += 2.1f)
            {
                Box("ServiceEnamel", P(s, 2.28f, wall + .025f), V(.15f, .63f, .11f), rotation);
                Box("OxidizedServiceSteel", P(s, height, wall - .10f), V(.17f, .17f, .075f), rotation);
            }
        }
        Tube("CableRubber", new[] { P(start, .34f, -wall - .045f), P(middle - .2f, .34f, -wall - .045f), P(middle + .2f, .26f, -wall - .045f), P(end, .26f, -wall - .045f) }, .021f);
        // A cable tray below the ceiling, with visible rails instead of a solid block.
        foreach (float t in new[] { wall - .55f, wall - .25f })
            Box("OxidizedServiceSteel", P(middle, 2.72f, t), V(.035f, .075f, span), rotation);
        for (float s = start; s <= end; s += .65f)
            Box("OxidizedServiceSteel", P(s, 2.70f, wall - .4f), V(.33f, .025f, .035f), rotation);
        Tube("CableRubber", new[] { P(start, 2.74f, wall - .41f), P(end, 2.74f, wall - .41f) }, .026f);
        var wheel = Enumerable.Range(0, 33).Select(i => P(middle + Mathf.Sin(i * Mathf.PI / 16) * .16f, 2.12f + Mathf.Cos(i * Mathf.PI / 16) * .16f, wall - .30f)).ToArray();
        Tube("OxidizedServiceSteel", wheel, .016f);
        Tube("OxidizedServiceSteel", new[] { P(middle, 2.12f, wall - .1f), P(middle, 2.12f, wall - .30f) }, .03f);
        for (int i = 0; i < 3; i++)
            Tube("OxidizedServiceSteel", new[] { P(middle, 2.12f, wall - .30f), wheel[i * 10] }, .013f);
        // One faded service hatch; fittings remain confined to the wall edge.
        float station = middle;
        Box("ServiceEnamel", P(station, 1.3f, -wall - .055f), V(.035f, .88f, .66f), rotation);
        foreach (float delta in new[] { -.28f, .28f })
            foreach (float y in new[] { .94f, 1.66f }) Box("OxidizedServiceSteel", P(station + delta, y, -wall - .025f), V(.035f, .035f, .025f), rotation);
        Box("OxidizedServiceSteel", P(station + .16f, 1.3f, -wall + .015f), V(.09f, .18f, .025f), rotation);
        // Mineral runoff follows joints and collects along the base of that wall.
        for (int i = 0; i < 5; i++)
        {
            float s = Mathf.Lerp(start + .15f, end - .15f, i / 4f), width = .07f + .035f * (i % 3);
            var builder = new ChaosGeometry.Builder();
            var points = new[] { P(s - width, .05f, wall + .065f), P(s + width * .6f, .05f, wall + .065f), P(s + width * .18f, 2.48f - .14f * (i % 2), wall + .065f), P(s - width * .25f, 2.6f, wall + .065f) };
            builder.Face(points); Array.Reverse(points); builder.Face(points);
            Add("MineralDamp", builder.Mesh("Gravity runoff"), Vector3.zero, Quaternion.identity);
        }
        for (int i = 0; i < 3; i++)
        {
            float s = Mathf.Lerp(start + .35f, end - .35f, i / 2f);
            var builder = new ChaosGeometry.Builder();
            for (int j = 0; j < 20; j++)
            {
                float a = j * Mathf.PI / 10, b = (j + 1) * Mathf.PI / 10;
                Func<float, Vector3> Q = angle => P(s + Mathf.Sin(angle) * .47f * (1 + .12f * Mathf.Sin(angle * 5 + i)), .004f, wall - .19f + Mathf.Cos(angle) * .16f);
                if (alongX) builder.Face(P(s, .004f, wall - .19f), Q(a), Q(b));
                else builder.Face(P(s, .004f, wall - .19f), Q(b), Q(a));
            }
            Add("MineralDamp", builder.Mesh("Irregular damp contact patch"), Vector3.zero, Quaternion.identity);
        }
        Flush(group, "Corridor" + (alongX ? "X" : "Z") + "_" + suffix, sceneOverride);
        report.Add(suffix + " " + floor.name + " actual size=" + size + "; group world scale=" + group.lossyScale);
    }

    static void ReplaceSigns(GameObject room)
    {
        // Final art direction: no lettering or textured sign faces in Basement.
        foreach (var face in room.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == "Physical sign face" && !PrefabUtility.IsPartOfPrefabInstance(t)).ToArray())
        {
            if (face.GetComponentsInChildren<Collider>(true).Length > 0 || face.GetComponentsInChildren<MonoBehaviour>(true).Length > 0)
                throw new Exception("Unexpected functional component on text sign");
            Object.DestroyImmediate(face.gameObject);
        }
        foreach (var text in room.GetComponentsInChildren<TextMesh>(true))
        {
            if (PrefabUtility.IsPartOfPrefabInstance(text)) continue;
            var renderer = text.GetComponent<MeshRenderer>(); renderer.enabled = false;
            text.text = string.Empty;
        }
    }

    public static void RemoveAllText()
    {
        BasementSurfaceBuilder.RequireIsolated();
        foreach (string path in new[] { Sanctum, Arena, Room })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ReplaceSigns(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        Verify();
        Debug.Log("BASEMENT_ALL_TEXT_REMOVED");
    }

    public static void Build()
    {
        BasementSurfaceBuilder.RequireIsolated(); report.Clear(); Capture("BeforeRepair"); SetupMaterials();
        var sanctum = PrefabUtility.LoadPrefabContents(Sanctum);
        try { ReplaceSigns(sanctum); PrefabUtility.SaveAsPrefabAsset(sanctum, Sanctum); }
        finally { PrefabUtility.UnloadPrefabContents(sanctum); }
        foreach (string path in new[] { Arena, Room })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                RepairSymbol(root);
                if (path == Room)
                {
                    foreach (var floor in root.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name.StartsWith("Passage Floor"))) Corridor(floor.transform, "Prefab", false);
                    ReplaceSigns(root);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var map = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name == "Basement" && !t.parent);
        foreach (var floor in map.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name.StartsWith("Passage Floor"))) Corridor(floor.transform, "Map", true);
        EditorSceneManager.SaveScene(map.gameObject.scene);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Preview/VisualRepair"); File.WriteAllLines("Preview/VisualRepair/build.txt", report);
        Capture("AfterRepair");
        Debug.Log("BASEMENT_VISUAL_REPAIR_BUILT");
    }

    public static void Rebuild()
    {
        BasementSurfaceBuilder.RequireIsolated();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        foreach (string path in new[] { Room, Arena, Sanctum, "Assets/Scenes/SampleScene.unity" })
        {
            File.WriteAllBytes(path, File.ReadAllBytes(Path.GetFullPath("../BasementVisualRepair/before/" + path)));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }
        Build();
        Verify();
    }

    public static void Verify()
    {
        BasementSurfaceBuilder.RequireIsolated(); report.Clear(); Capture("VerifiedRepair");
        var map = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name == "Basement" && !t.parent).gameObject;
        CheckVisuals(map, "MAP");
        var flags = BindingFlags.NonPublic | BindingFlags.Static;
        foreach (string check in new[] { "Check", "Traverse" })
            typeof(BasementFinalVerifier).GetMethod(check, flags).Invoke(null, new object[] { map, "MAP" });
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Room)); instance.transform.position = Vector3.zero;
        CheckVisuals(instance, "PREFAB");
        foreach (string check in new[] { "Check", "Traverse" })
            typeof(BasementFinalVerifier).GetMethod(check, flags).Invoke(null, new object[] { instance, "PREFAB" });
        typeof(BasementBuilder).GetMethod("TestEncounter", flags).Invoke(null, new object[] { instance });
        File.WriteAllLines("Preview/VisualRepair/validation.txt", report);
        Debug.Log("BASEMENT_VISUAL_REPAIR_VERIFIED");
    }
    static void CheckVisuals(GameObject room, string label)
    {
        foreach (var floor in room.GetComponentsInChildren<BoxCollider>(true).Where(c => c.name.StartsWith("Passage Floor")))
        {
            var installation = floor.transform.Find("Abandoned service installation");
            if (!installation || Vector3.Distance(installation.lossyScale, Vector3.one) > .001f) throw new Exception("Service installation stretched: " + label + floor.name);
            if (installation.GetComponentsInChildren<Collider>(true).Length > 0) throw new Exception("Added corridor blocker");
            var ritual = floor.transform.Find("Ritual passage thresholds");
            if (ritual.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.enabled && (r.name.Contains("Oxidized_ritual_blood") || r.name.Contains("Scraped_bone_chalk")))) throw new Exception("Old corridor symbol still visible");
            report.Add(label + " " + floor.name + ": world scale 1, old symbols hidden, no new collider, " + installation.GetComponentsInChildren<MeshRenderer>().Length + " material batches; " + installation.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3) + " triangles.");
        }
        var seal = room.transform.Find("PreBossArea/BasementFinalArena/Ritual preparation and votive procession/Restored floor seal");
        if (!seal || seal.GetComponentsInChildren<Collider>(true).Length > 0) throw new Exception("Seal absent/has collider");
        bool inscribed = seal.GetComponentsInChildren<MeshFilter>().All(f => AssetDatabase.GetAssetPath(f.sharedMesh).Contains("/SigilInscription/"));
        report.Add(label + (inscribed ? " seal: restored pentagram; one encoded band containing all three phrases; 6.5 m diameter; 12 mm floor clearance."
            : " seal: continuous 6.5 m outer circle; five connected strokes; 12 mm floor clearance."));
        foreach (var renderer in room.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
        {
            foreach (var material in renderer.sharedMaterials)
            {
                if (!material || !material.shader) throw new Exception("Missing renderer material");
                foreach (var message in ShaderUtil.GetShaderMessages(material.shader))
                    if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) throw new Exception(message.message);
            }
        }
        int signs = room.GetComponentsInChildren<Transform>(true).Count(t => t.name == "Physical sign face");
        if (signs != 0 || room.GetComponentsInChildren<TextMesh>(true).Any(t => !string.IsNullOrEmpty(t.text)))
            throw new Exception("Basement still contains lettering");
        if (room.GetComponentsInChildren<MeshRenderer>(true).Any(r => r.sharedMaterials.Any(m => m && m.name.StartsWith("Sign_"))))
            throw new Exception("Textured sign material still referenced");
        report.Add(label + ": no text strings, no textured lettering signs, including nested prefabs; actual CharacterController traversal and gate checked in Unity log.");
    }

    public static void Diagnose()
    {
        BasementSurfaceBuilder.RequireIsolated();
        Capture("BeforeRepair");
        var room = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name == "Basement" && !t.parent);
        foreach (var t in room.GetComponentsInChildren<Transform>(true).Where(t =>
            t.name == "BasementFinalArena" || t.name == "Ritual preparation and votive procession" ||
            t.name.StartsWith("Passage Floor") || t.name == "Ritual passage thresholds"))
            report.Add(PathOf(t) + " position=" + t.position.ToString("F4") + " scale=" + t.lossyScale.ToString("F4"));
        var prep = room.Find("PreBossArea/BasementFinalArena/Ritual preparation and votive procession");
        var f = room.Find("PreBossArea/Floor/Hall_Floor").GetComponent<BoxCollider>();
        var center = prep.TransformPoint(V(0, .052f, 2.4f));
        float top = f.transform.TransformPoint(f.center + Vector3.up * f.size.y * .5f).y;
        report.Add("Seal center " + center.ToString("F5") + "; actual floor top " + top.ToString("F5") + "; height difference " + (center.y - top).ToString("F5"));
        foreach (var renderer in prep.GetComponentsInChildren<MeshRenderer>())
            if (renderer.name.Contains("blood") || renderer.name.Contains("chalk"))
                report.Add(renderer.name + " mesh=" + renderer.GetComponent<MeshFilter>().sharedMesh.bounds + " enabled=" + renderer.enabled);
        Directory.CreateDirectory("Preview/VisualRepair");
        File.WriteAllLines("Preview/VisualRepair/diagnosis.txt", report);
        Debug.Log("BASEMENT_VISUAL_DIAGNOSED\n" + string.Join("\n", report));
    }

    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;

    public static void Capture(string prefix)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        ShaderUtil.allowAsyncCompilation = false;
        var room = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name == "Basement" && !t.parent);
        var arena = room.Find("PreBossArea");
        var boss = room.Find("BossRoom");
        float af = BasementFinalBuilder.Floor(arena, "Hall_Floor");
        var seal = arena.Find("BasementFinalArena/Ritual preparation and votive procession");
        var center = arena.InverseTransformPoint(seal.TransformPoint(V(0, .052f, 2.4f)));
        Render(room, arena, center + V(0, 6.2f, -6.5f), center, prefix + "-Symbol", true);
        Render(room, arena, center + V(-1.7f, 1.84f, -6), center + V(0, 0, 1), prefix + "-SymbolPlayer", true);
        Render(room, arena, V(2, af + 1.84f, -12), V(13, af + 2.2f, 14), prefix + "-PreArena", false);
        foreach (var axis in new[] { "X", "Z" })
        {
            var floor = boss.Find("Player sized corridor/Passage Floor " + axis).GetComponent<BoxCollider>();
            var box = floor.bounds;
            var direction = floor.transform.TransformDirection(axis == "X" ? Vector3.left : Vector3.forward).normalized;
            var dimensions = Vector3.Scale(floor.size, floor.transform.lossyScale);
            float length = axis == "X" ? dimensions.x : dimensions.z;
            var centerWorld = floor.transform.TransformPoint(floor.center + Vector3.up * floor.size.y * .5f) + Vector3.up * 1.72f;
            var eye = centerWorld - direction * Mathf.Max(0, length * .5f - .9f);
            Render(room, boss, boss.InverseTransformPoint(eye), boss.InverseTransformPoint(centerWorld + direction * length), prefix + "-Corridor" + axis, true);
            var detailEye = centerWorld - direction * Mathf.Min(1.7f, length * .25f);
            var side = floor.transform.TransformDirection(axis == "X" ? Vector3.forward : Vector3.right).normalized;
            var look = new Vector3(box.center.x, box.max.y + .6f, box.center.z) + side * .65f;
            Render(room, boss, boss.InverseTransformPoint(detailEye), boss.InverseTransformPoint(look), prefix + "-Detail" + axis, true);
        }
    }

    static void Render(Transform room, Transform coordinates, Vector3 eye, Vector3 look, string name, bool flashlight)
    {
        var method = typeof(BasementSurfaceBuilder).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Static);
        method.Invoke(null, new object[] { room, coordinates, eye, look, "VisualRepair-" + name, flashlight });
    }
}
