// Run only inside the isolated revision project. Outputs are persistent Unity assets.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object = UnityEngine.Object;

public static class CylinderDetailBuilder
{
    public const string SceneRoot = "Assets/_Project/Scenes/Cylinder";
    public const string Prefab = SceneRoot + "/Prefabs/Cylinder.prefab";
    public const string Materials = SceneRoot + "/Materials/DetailRevision";
    public const string Meshes = SceneRoot + "/Meshes/DetailRevision";
    public const string Textures = SceneRoot + "/Textures/DetailRevision";
    const string ClassSurface = "Assets/_Project/Scenes/ClassRoom/Surface/Textures/";
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    static Transform atrium;
    static int serial;
    static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

    public static void Build()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/.utmp/CylinderDetailPreview/"))
            throw new Exception("Use the isolated CylinderDetailPreview project");
        Directory.CreateDirectory(Materials); Directory.CreateDirectory(Meshes);
        Directory.CreateDirectory(SceneRoot + "/Profiles/DetailRevision");
        AssetDatabase.Refresh();
        ImportModels();
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            atrium = root.transform.Find("Abandoned circular atrium");
            if (!atrium || atrium.Find("Detailed maintenance and abandonment")) throw new Exception("Wrong input revision");
            UpgradeSurfaces(root);
            var detail = Group(atrium, "Detailed maintenance and abandonment", Vector3.zero);
            ImproveTables();
            Maintenance(detail);
            ConfigureAtmosphere(root);
            AddSpawns(detail);
            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("CYLINDER_DETAIL_BUILD_OK");
    }

    static void ImportModels()
    {
        foreach (var id in new[] { "utility_box_02", "barrel_03", "plastic_monobloc_chair_01" })
        {
            string path = Meshes + "/PolyHaven/" + id + "/model.fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
            importer.isReadable = true; importer.SaveAndReimport();
            foreach (var role in new[] { "Diffuse", "nor_gl", "arm" })
                ImportTexture(Textures + "/" + id + "/" + role + ".png", role == "nor_gl", role == "Diffuse");
            var arm = ReadPNG(Textures + "/" + id + "/arm.png");
            var pixels = arm.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i]; pixels[i] = new Color32(c.b, c.r, c.g, (byte)(255 - c.g));
            }
            arm.SetPixels32(pixels); arm.Apply();
            string maskPath = Textures + "/" + id + "/UnityMask.png";
            File.WriteAllBytes(maskPath, arm.EncodeToPNG()); Object.DestroyImmediate(arm);
            AssetDatabase.ImportAsset(maskPath); ImportTexture(maskPath, false, false);
            var mat = Surface(id, Textures + "/" + id + "/", Color.white, 1);
            mat.SetFloat("_BumpScale", .7f); mat.SetFloat("_Smoothness", .65f);
            mats[id] = mat;
        }
    }

    static Texture2D ReadPNG(string path)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        if (!t.LoadImage(File.ReadAllBytes(path))) throw new Exception("Invalid PNG " + path);
        return t;
    }

    static void ImportTexture(string path, bool normal, bool srgb)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = srgb; importer.mipmapEnabled = true; importer.maxTextureSize = 1024;
        importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    static Material Surface(string name, string folder, Color tint, float tile)
    {
        var m = new Material(Shader.Find("UniDoom/TotalChaosInspiredSurface")) { name = name, enableInstancing = true };
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "Diffuse.png"));
        m.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "nor_gl.png"));
        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "UnityMask.png");
        if (!mask && folder == ClassSurface + "concrete_wall_006/")
            mask = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Global/Textures/ClassRoom/Surface/Textures/concrete_wall_006/UnityMask.png");
        if (!mask || !m.GetTexture("_BaseMap") || !m.GetTexture("_BumpMap")) throw new Exception("Incomplete PBR set " + folder);
        m.SetTexture("_MetallicGlossMap", mask); m.SetTexture("_OcclusionMap", mask); m.SetTexture("_DirtMap", mask);
        m.SetColor("_BaseColor", tint); m.SetVector("_SpecColor", new Vector4(.22f, .03f, .82f, 1));
        m.SetFloat("_BumpScale", .6f); m.SetFloat("_Smoothness", .55f); m.SetFloat("_OcclusionStrength", .65f);
        m.SetTextureScale("_BaseMap", Vector2.one * tile);
        m.EnableKeyword("_NORMALMAP"); m.EnableKeyword("_METALLICSPECGLOSSMAP");
        m.EnableKeyword("_OCCLUSIONMAP"); m.EnableKeyword("_TC_SHARED_MASK");
        AssetDatabase.CreateAsset(m, Materials + "/" + name + ".mat");
        return m;
    }

    static void UpgradeSurfaces(GameObject root)
    {
        var old = root.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().ToArray();
        var replacements = new Dictionary<Material, Material>();
        foreach (var original in old)
        {
            string name = original.name, set = null; float tile = 1;
            Color tint = Color.white;
            switch (name)
            {
                case "Plaster": set = "concrete_wall_006"; tint = new Color(.68f, .71f, .68f); break;
                case "Concrete": set = "concrete_wall_006"; tint = new Color(.62f, .64f, .62f); break;
                case "Roof": set = "rusty_metal_02"; tint = new Color(.43f, .48f, .48f); break;
                case "Floor": set = "concrete_wall_006"; tint = new Color(.48f, .51f, .50f); tile = .65f; break;
                case "Mosaic": set = "worn_tile_floor"; tint = new Color(.68f, .71f, .67f); break;
                case "Wood": set = "wood_table_worn"; tint = new Color(.55f, .49f, .39f); break;
                case "Iron": set = "rusty_metal_02"; tint = new Color(.30f, .34f, .34f); break;
                case "Rust": set = "rusty_metal_02"; tint = new Color(.55f, .43f, .32f); break;
                case "RedPaint": set = "rusty_metal_02"; tint = new Color(.37f, .18f, .13f); break;
                case "Rubble": set = "rock_face"; tint = new Color(.56f, .57f, .52f); break;
                case "SealedDoor": set = "rusty_metal_02"; tint = new Color(.30f, .40f, .34f); break;
            }
            if (set == null) continue;
            var replacement = Surface(name, ClassSurface + set + "/", tint, tile);
            mats[name] = replacement; replacements[original] = replacement;
        }
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m && replacements.TryGetValue(m, out var n) ? n : m).ToArray();
        foreach (var name in new[] { "Dark", "Amber", "SkillLamp", "DecayPaper", "DecayBlood", "Damp" })
        {
            var source = old.FirstOrDefault(m => m.name == name);
            if (!source) continue;
            var clone = new Material(source) { name = name + "Detail" };
            if (name == "Amber" || name == "SkillLamp")
            {
                clone.SetColor("_BaseColor", name == "Amber" ? new Color(.8f, .62f, .38f) : new Color(.65f, .78f, .76f));
                clone.SetColor("_EmissionColor", name == "Amber" ? new Color(1, .65f, .3f) * 1.2f : new Color(.58f, .76f, .72f) * 1.8f);
                clone.EnableKeyword("_EMISSION");
            }
            AssetDatabase.CreateAsset(clone, Materials + "/" + name + ".mat"); mats[name] = clone;
            if (name == "Amber" || name == "SkillLamp")
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m == source ? clone : m).ToArray();
        }
    }

    static Transform Group(Transform parent, string name, Vector3 position)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position;
        return t;
    }

    static GameObject Model(Transform parent, string id, string label, Vector3 feet, float yaw, float height, bool collision)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(Meshes + "/PolyHaven/" + id + "/model.fbx");
        var anchor = Group(parent, label, feet);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(source, anchor);
        // Unpack only new third-party instances; keep the existing room hierarchy intact.
        PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        model.transform.localPosition = Vector3.zero; model.transform.localRotation = source.transform.localRotation;
        var renderers = model.GetComponentsInChildren<MeshRenderer>();
        var bounds = renderers[0].bounds; foreach (var r in renderers) { bounds.Encapsulate(r.bounds); r.sharedMaterials = Enumerable.Repeat(mats[id], r.sharedMaterials.Length).ToArray(); }
        model.transform.localScale *= height / bounds.size.y;
        bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        model.transform.position += anchor.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        anchor.localRotation = Quaternion.Euler(0, yaw, 0);
        if (collision)
        {
            var collider = anchor.gameObject.AddComponent<BoxCollider>();
            // Compute the imported geometry in the anchor's own coordinates.
            var local = LocalBounds(anchor, renderers);
            collider.center = local.center; collider.size = local.size;
        }
        return anchor.gameObject;
    }

    static Bounds LocalBounds(Transform parent, Renderer[] renderers)
    {
        Bounds? result = null;
        foreach (var renderer in renderers)
        {
            var b = renderer.localBounds;
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                var p = parent.InverseTransformPoint(renderer.transform.TransformPoint(b.center + Vector3.Scale(b.extents, V(x, y, z))));
                if (!result.HasValue) result = new Bounds(p, Vector3.zero); else { var a = result.Value; a.Encapsulate(p); result = a; }
            }
        }
        return result.Value;
    }

    static void ImproveTables()
    {
        foreach (Transform table in atrium.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Cafe table cluster ")).ToArray())
        {
            // Original combined visual meshes include the chairs. Preserve their objects and colliders.
            foreach (var r in table.GetComponentsInChildren<MeshRenderer>(true)) r.enabled = false;
            var detail = Group(table, "Detailed cafe furniture", Vector3.zero);
            Cylinder(detail, "Rounded hardwood tabletop", V(0, .8f, 0), .65f, .075f, "Wood", 64);
            Cylinder(detail, "Iron pedestal", V(0, .4f, 0), .055f, .76f, "Iron", 24);
            Cylinder(detail, "Cast metal foot", V(0, .035f, 0), .34f, .07f, "Iron", 48);
            Cylinder(detail, "Pedestal collar", V(0, .71f, 0), .11f, .08f, "Rust", 24);
            var chairMarkers = table.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Chair" || t.name == "Fallen cafe chair").ToArray();
            foreach (var marker in chairMarkers)
            {
                var chair = Model(detail, "plastic_monobloc_chair_01", "Weathered cafeteria chair", marker.localPosition, marker.localEulerAngles.y + 180, .89f, false);
                if (marker.name.Contains("Fallen"))
                {
                    chair.transform.localRotation = marker.localRotation * Quaternion.Euler(0, 180, 0);
                    var b = LocalBounds(detail, chair.GetComponentsInChildren<Renderer>());
                    chair.transform.localPosition += Vector3.up * (.012f - b.min.y);
                }
            }
            Box(detail, "Abandoned receipt", V(.23f, .842f, -.12f), V(.17f, .003f, .24f), "DecayPaper", 0);
            Batch(detail, "Furniture", false);
        }
    }

    static void Maintenance(Transform detail)
    {
        foreach (int i in new[] { 4, 9, 16, 21, 28, 33, 40, 45 })
        {
            var bay = atrium.Find("Service bay " + i);
            var section = Group(detail, "Maintenance bay " + i, atrium.InverseTransformPoint(bay.position));
            section.rotation = bay.rotation;
            // The corridor is inward (-Z); all fittings hug the wall, away from the walking circuit.
            Box(section, "Vent housing", V(1.0f, 3.25f, -.30f), V(.78f, .50f, .18f), "Iron", .025f);
            Box(section, "Dark ventilation throat", V(1.0f, 3.25f, -.40f), V(.64f, .35f, .025f), "Dark", .003f);
            for (int s = 0; s < 7; s++) Box(section, "Angled vent slat", V(1.0f, 3.10f + s * .05f, -.43f), V(.66f, .022f, .045f), "Rust", .005f);
            Pipe(section, V(1.45f, .22f, -.34f), V(1.45f, 3.65f, -.34f), .035f, "Rust");
            foreach (float y in new[] { .45f, 1.8f, 3.4f })
            {
                Box(section, "Conduit saddle", V(1.45f, y, -.35f), V(.15f, .06f, .09f), "Iron", .008f);
                foreach (float x in new[] { 1.39f, 1.51f }) Cylinder(section, "Saddle bolt", V(x, y, -.405f), .016f, .014f, "Iron", 8, Quaternion.Euler(90, 0, 0));
            }
            Box(section, "Junction box", V(1.45f, 1.15f, -.37f), V(.26f, .34f, .14f), "Iron", .018f);
            Box(section, "Gasket seam", V(1.45f, 1.15f, -.448f), V(.23f, .30f, .012f), "Dark", .005f);
            Box(section, "Oxidized cover", V(1.45f, 1.15f, -.46f), V(.21f, .28f, .018f), "Rust", .009f);
            if (i == 9 || i == 28 || i == 40)
            {
                Model(section, "utility_box_02", "Corroded electrical service cabinet", V(-1.0f, .08f, -.54f), 180, 1.25f, true);
                Model(section, "barrel_03", "Abandoned maintenance drum", V(.30f, .025f, -.78f), i * 13, .90f, true);
            }
            Batch(section, "Wall fittings", true);
        }
        // Real fluorescent fixtures and conduit around the gallery; intermittent dead units create dark intervals.
        foreach (int i in new[] { 3, 8, 15, 20, 27, 32, 39, 44 })
        {
            var bay = atrium.Find("Service bay " + i);
            var fixture = Group(detail, "Gallery fluorescent fixture " + i, atrium.InverseTransformPoint(bay.TransformPoint(V(0, 3.7f, -.70f))));
            fixture.rotation = bay.rotation;
            Box(fixture, "Pressed steel reflector", Vector3.zero, V(1.30f, .10f, .28f), "Iron", .018f);
            bool live = i != 15 && i != 39;
            for (int n = -1; n <= 1; n += 2)
            {
                Cylinder(fixture, "Fluorescent tube", V(0, -.073f, n * .07f), .018f, 1.10f, live ? "SkillLamp" : "Dark", 16, Quaternion.Euler(0, 0, 90));
                foreach (float x in new[] { -.57f, .57f }) Box(fixture, "Tube socket", V(x, -.065f, n * .07f), V(.075f, .065f, .065f), "Concrete", .008f);
            }
            if (live) AddLight(fixture, V(0, -.15f, 0), new Color(.69f, .82f, .79f), 10, 10, i == 8 || i == 32);
            Batch(fixture, "Fixture", true);
        }
        var kiosk = atrium.GetComponentsInChildren<Transform>().First(t => t.name == "Kiosk bay").parent;
        var service = Group(detail, "Canteen service connections", Vector3.zero);
        // Pipes run above head height behind the canteen's service side.
        foreach (float x in new[] { -1.1f, 1.1f })
        {
            Pipe(service, V(x, 3.5f, -3.4f), V(x, 6.6f, -3.4f), .075f, "Rust");
            for (int j = 0; j < 4; j++) Cylinder(service, "Riser union", V(x, 3.8f + j * .75f, -3.4f), .11f, .10f, "Iron", 24);
        }
        Batch(service, "Service pipes", true);
    }

    static void ConfigureAtmosphere(GameObject root)
    {
        var reference = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Scenes/ClassRoom/Prefabs/AbandonedClassRoomInterior.prefab").GetComponentInChildren<TCRegionAtmosphere>(true);
        var region = root.GetComponentInChildren<TCRegionAtmosphere>(true);
        region.theme = "cylinder"; region.fogColor = reference.fogColor; region.ambient = reference.ambient * 1.7f; region.ambient.a = 1;
        region.fogDensity = .009f; region.blendDistance = 2;
        region.flashlightAngle = reference.flashlightAngle; region.flashlightInnerAngle = reference.flashlightInnerAngle;
        region.flashlightIntensity = reference.flashlightIntensity; region.flashlightRange = reference.flashlightRange;
        region.flashlightShadows = reference.flashlightShadows; region.stabilizeBackgroundShadows = true;
        var sourceVolume = reference.GetComponent<Volume>();
        var profile = Object.Instantiate(sourceVolume.sharedProfile); profile.name = "CylinderSuspense";
        profile.components = new List<VolumeComponent>();
        string profilePath = SceneRoot + "/Profiles/DetailRevision/CylinderSuspense.asset";
        AssetDatabase.CreateAsset(profile, profilePath);
        foreach (var source in sourceVolume.sharedProfile.components)
        {
            var component = Object.Instantiate(source); component.name = source.name;
            profile.components.Add(component); AssetDatabase.AddObjectToAsset(component, profile);
        }
        var volume = region.GetComponent<Volume>(); volume.sharedProfile = profile; volume.blendDistance = 2;
        var allLights = root.GetComponentsInChildren<Light>(true);
        foreach (var light in allLights)
        {
            if (light.transform.IsChildOf(atrium.Find("Detailed maintenance and abandonment"))) continue;
            if (light.intensity <= 0) continue;
            if (light.transform.parent == atrium) { light.enabled = false; continue; }
            bool red = light.color.r > light.color.g * 2;
            bool pendant = light.transform.parent && light.transform.parent.name == "Pendant";
            light.color = red ? new Color(.8f, .18f, .12f) : pendant ? new Color(.71f, .84f, .81f) : new Color(1, .73f, .45f);
            light.intensity = red ? .85f : pendant ? 18f : 3.4f;
            light.range = red ? 5 : pendant ? 15 : 7;
            if (pendant)
            {
                light.type = LightType.Spot; light.transform.rotation = Quaternion.Euler(90, 0, 0);
                light.spotAngle = 112; light.innerSpotAngle = 72;
            }
            light.shadows = LightShadows.None; light.shadowBias = .035f; light.shadowNormalBias = .12f;
        }
        foreach (var flicker in root.GetComponentsInChildren<TCFlickerLight>(true)) flicker.enabled = false;
        // Limit shadowed sources: two fixtures plus the player's existing region-controlled flashlight.
        foreach (var light in root.GetComponentsInChildren<Light>(true).Where(l => l.shadows != LightShadows.None))
        {
            var data = light.GetComponent<UniversalAdditionalLightData>() ?? light.gameObject.AddComponent<UniversalAdditionalLightData>();
            ShadowBudget(data);
        }
    }

    static Light AddLight(Transform parent, Vector3 position, Color color, float intensity, float range, bool shadows)
    {
        var go = Group(parent, "Cold fluorescent spill", position).gameObject;
        var l = go.AddComponent<Light>(); l.type = LightType.Spot; l.color = color; l.intensity = intensity; l.range = range;
        l.transform.localRotation = Quaternion.Euler(90, 0, 0); l.spotAngle = 112; l.innerSpotAngle = 72;
        l.shadows = shadows ? LightShadows.Soft : LightShadows.None; l.shadowBias = .035f; l.shadowNormalBias = .12f;
        ShadowBudget(go.AddComponent<UniversalAdditionalLightData>());
        return l;
    }

    static void ShadowBudget(UniversalAdditionalLightData data)
    {
        var so = new SerializedObject(data);
        so.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = 1;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void PolishLighting()
    {
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.name == "Cold fluorescent spill") { light.intensity = 10; light.range = 10; }
                else if (light.transform.parent.name == "Pendant" && light.intensity > 0) { light.intensity = 18; light.range = 15; }
                else if (light.type == LightType.Point && light.color.r > light.color.b && light.color.g > .4f && light.intensity > 0) light.intensity = 3.4f;
            }
            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        foreach (var name in new[] { "Amber", "SkillLamp" })
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Materials + "/" + name + ".mat");
            mat.SetColor("_BaseColor", name == "Amber" ? new Color(.8f, .62f, .38f) : new Color(.65f, .78f, .76f));
            mat.SetColor("_EmissionColor", name == "Amber" ? new Color(1, .65f, .3f) * 1.2f : new Color(.58f, .76f, .72f) * 1.8f);
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
        CylinderDetailVerifier.Capture("Generated");
        Debug.Log("CYLINDER_DETAIL_LIGHTING_OK");
    }

    public static void PolishIndirect()
    {
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var region = root.GetComponentInChildren<TCRegionAtmosphere>(true);
            region.ambient = new Color(.153f, .1785f, .204f);
            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        CylinderDetailVerifier.Capture("Generated");
    }

    static void AddSpawns(Transform detail)
    {
        var spawns = Group(detail, "Enemy Spawn Points", Vector3.zero);
        var points = new List<Transform>();
        foreach (var point in new[] { V(-23, 1.04f, 7), V(23, 1.04f, -7), V(-16, 1.04f, -14), V(16, 1.04f, -14), V(-12, 1.04f, 14), V(12, 1.04f, 14) })
        {
            var t = Group(spawns, "Enemy Spawn " + (points.Count + 1).ToString("00"), point);
            t.localRotation = Quaternion.LookRotation(new Vector3(-point.x, 0, -point.z)); points.Add(t);
        }
        var trigger = Group(detail, "Encounter trigger - south entrance", V(0, 1.6f, 16.5f));
        var box = trigger.gameObject.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = V(7, 3.2f, 3);
        var encounter = trigger.gameObject.AddComponent<CylinderEnemySpawns>();
        var so = new SerializedObject(encounter);
        so.FindProperty("enemyPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Enemy/Enemy.prefab");
        var array = so.FindProperty("spawnPoints"); array.arraySize = points.Count;
        for (int i = 0; i < points.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static Mesh Persist(Mesh mesh, string key)
    {
        mesh.name = key; mesh.RecalculateBounds();
        if (mesh.tangents.Length == 0) mesh.RecalculateTangents();
        string path = Meshes + "/" + key + ".asset";
        AssetDatabase.CreateAsset(mesh, path); return mesh;
    }

    static void MeshObject(Transform parent, string name, Mesh mesh, Vector3 position, Quaternion rotation, string material)
    {
        var t = Group(parent, name, position); t.localRotation = rotation;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mats[material];
    }

    static void Box(Transform parent, string name, Vector3 center, Vector3 size, string material, float bevel)
    {
        string key = "Box_" + size.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + size.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + size.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + bevel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        if (!meshes.TryGetValue(key, out var mesh))
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var h = size * .5f; float b = Mathf.Min(bevel, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * .8f);
            var inner = h - Vector3.one * b;
            for (int axis = 0; axis < 3; axis++) foreach (int sign in new[] { -1, 1 })
            {
                int u = (axis + 1) % 3, v = (axis + 2) % 3, first = vertices.Count;
                float[] us = { -h[u], -inner[u], inner[u], h[u] }, vs = { -h[v], -inner[v], inner[v], h[v] };
                for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++)
                {
                    var p = Vector3.zero; p[axis] = sign * h[axis]; p[u] = us[i]; p[v] = vs[j];
                    var q = V(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    var n = (p - q).normalized; if (b <= 0) { n = Vector3.zero; n[axis] = sign; }
                    vertices.Add(b > 0 ? q + n * b : p); normals.Add(n); uv.Add(new Vector2(p[u], p[v]));
                }
                for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++)
                {
                    int a = first + j * 4 + i, c = a + 4;
                    if (sign > 0) triangles.AddRange(new[] { a, a + 1, c + 1, a, c + 1, c });
                    else triangles.AddRange(new[] { a, c + 1, a + 1, a, c, c + 1 });
                }
            }
            mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            meshes[key] = Persist(mesh, key);
        }
        MeshObject(parent, name, mesh, center, Quaternion.identity, material);
    }

    static void Cylinder(Transform parent, string name, Vector3 center, float radius, float height, string material, int sides, Quaternion? rotation = null)
    {
        string key = "Cylinder_" + radius.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + height.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + sides;
        if (!meshes.TryGetValue(key, out var mesh))
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            // Four rings form bevels which catch grazing light; caps are separate for hard normals.
            float bevel = Mathf.Min(radius * .16f, height * .15f);
            for (int row = 0; row < 4; row++) for (int i = 0; i <= sides; i++)
            {
                float angle = i * 2 * Mathf.PI / sides;
                float r = row == 0 || row == 3 ? radius - bevel : radius;
                float y = row == 0 ? -height / 2 : row == 1 ? -height / 2 + bevel : row == 2 ? height / 2 - bevel : height / 2;
                vertices.Add(V(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r)); uv.Add(new Vector2(i / (float)sides, row / 3f));
            }
            for (int row = 0; row < 3; row++) for (int i = 0; i < sides; i++)
            {
                int a = row * (sides + 1) + i, c = a + sides + 1;
                triangles.AddRange(new[] { a, c, c + 1, a, c + 1, a + 1 });
            }
            foreach (int sign in new[] { -1, 1 })
            {
                int first = vertices.Count; vertices.Add(V(0, sign * height / 2, 0)); uv.Add(new Vector2(.5f, .5f));
                for (int i = 0; i <= sides; i++)
                {
                    float a = i * Mathf.PI * 2 / sides; vertices.Add(V(Mathf.Cos(a) * (radius - bevel), sign * height / 2, Mathf.Sin(a) * (radius - bevel)));
                    uv.Add(new Vector2(.5f + .5f * Mathf.Cos(a), .5f + .5f * Mathf.Sin(a)));
                    if (i < sides)
                        if (sign > 0) triangles.AddRange(new[] { first, first + i + 2, first + i + 1 }); else triangles.AddRange(new[] { first, first + i + 1, first + i + 2 });
                }
            }
            mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); meshes[key] = Persist(mesh, key);
        }
        MeshObject(parent, name, mesh, center, rotation ?? Quaternion.identity, material);
    }

    static void Pipe(Transform parent, Vector3 a, Vector3 b, float radius, string material)
        => Cylinder(parent, "Corroded pipe", (a + b) / 2, radius, Vector3.Distance(a, b), material, 20, Quaternion.FromToRotation(Vector3.up, b - a));

    static void Batch(Transform parent, string label, bool excludeImported)
    {
        var renderers = parent.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled && r.GetComponent<MeshFilter>() &&
            (!excludeImported || !AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh).EndsWith(".fbx"))).ToArray();
        foreach (var group in renderers.GroupBy(r => r.sharedMaterial))
        {
            var combines = new List<CombineInstance>();
            foreach (var r in group)
            {
                var mf = r.GetComponent<MeshFilter>();
                // Imported chair meshes share a single PBR finish; retain every submesh.
                for (int i = 0; i < mf.sharedMesh.subMeshCount; i++)
                    combines.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = i, transform = parent.worldToLocalMatrix * r.transform.localToWorldMatrix });
            }
            var combined = new Mesh { indexFormat = IndexFormat.UInt32 };
            combined.CombineMeshes(combines.ToArray(), true, true);
            Persist(combined, "Batch_" + (++serial).ToString("D3") + "_" + group.Key.name);
            var t = Group(parent, label + " - " + group.Key.name, Vector3.zero);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = combined; t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
            foreach (var r in group) { Object.DestroyImmediate(r.GetComponent<MeshFilter>()); Object.DestroyImmediate(r); }
        }
    }
}
