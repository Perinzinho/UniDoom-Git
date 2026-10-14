// Copy into Assets/Editor of an isolated project; never rebuild unrelated maps.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object = UnityEngine.Object;

public static class ClassRoomLightingBuilder
{
    public const string Interior = "Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab";
    public const string Art = "Assets/Art/ClassRoom/Lighting";
    const string Surfaces = "Assets/Art/SkillAtmosphere/Materials/ClassRoom";
    public static void Build()
    {
        RequireIsolated();
        ClassRoomLightingVerifier.Capture("Before");
        Directory.CreateDirectory(Art + "/Textures"); AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Surfaces, "*.mat"))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path.Replace('\\', '/'));
            foreach (string property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_MainTex" })
            {
                if (!mat.HasProperty(property) || !mat.GetTexture(property)) continue;
                string source = AssetDatabase.GetAssetPath(mat.GetTexture(property));
                if (!source.StartsWith("Assets/Art/TotalChaosInspired/Textures/")) continue;
                // Import settings belong to a texture asset, not a material. Keep other maps' imports intact.
                string target = Art + "/Textures/" + Path.GetFileName(source);
                if (!File.Exists(target) && !AssetDatabase.CopyAsset(source, target)) throw new Exception("Cannot reuse " + source);
                var importer = (TextureImporter)AssetImporter.GetAtPath(target);
                importer.maxTextureSize = 512; importer.filterMode = FilterMode.Trilinear;
                importer.anisoLevel = 4; importer.mipmapEnabled = true;
                var settings = importer.GetDefaultPlatformTextureSettings(); settings.maxTextureSize = 512;
                importer.SetPlatformTextureSettings(settings); importer.SaveAndReimport();
                mat.SetTexture(property, AssetDatabase.LoadAssetAtPath<Texture2D>(target));
            }
            if (mat.GetTexture("_MetallicGlossMap"))
            {
                // Alpha already stores smoothness. The former 0.1 multiplier erased its variation.
                mat.SetFloat("_Smoothness", mat.name == "Steel" || mat.name == "Iron" ? .85f : .7f);
                mat.SetFloat("_BumpScale", mat.name.Contains("Coat") || mat.name.Contains("Tarp") ? .34f : .48f);
                foreach (string property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap", "_MainTex" })
                    if (mat.HasProperty(property)) mat.SetTextureScale(property, Vector2.one);
            }
            if (mat.name == "Wet") { mat.SetFloat("_Metallic", 0); mat.SetFloat("_Smoothness", .72f); }
            if (mat.name == "Tube" || mat.name == "SkillLamp")
            {
                mat.SetColor("_BaseColor", new Color(.7f, .77f, .76f));
                mat.SetColor("_EmissionColor", new Color(.65f, .76f, .73f) * .8f);
            }
            EditorUtility.SetDirty(mat);
        }
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(Art + "/ClassRoomVisibility.asset");
        if (!profile) { profile = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile, Art + "/ClassRoomVisibility.asset"); }
        Configure<ColorAdjustments>(profile, c => { c.postExposure.Override(0); c.contrast.Override(6); c.saturation.Override(-12); });
        Configure<Tonemapping>(profile, c => c.mode.Override(TonemappingMode.Neutral));
        Configure<Vignette>(profile, c => { c.intensity.Override(.16f); c.smoothness.Override(.6f); });
        Configure<FilmGrain>(profile, c => c.intensity.Override(0));
        Configure<ChromaticAberration>(profile, c => c.intensity.Override(0));
        Configure<Bloom>(profile, c => { c.intensity.Override(.08f); c.threshold.Override(1.2f); });
        var root = PrefabUtility.LoadPrefabContents(Interior);
        try
        {
            var region = root.GetComponentInChildren<TCRegionAtmosphere>();
            region.theme = "classroom"; region.blendDistance = 2;
            region.stabilizeBackgroundShadows = true;
            region.fogDensity = .012f; region.fogColor = new Color(.095f, .11f, .12f);
            region.ambient = new Color(.09f, .105f, .12f);
            region.flashlightIntensity = 3.2f; region.flashlightRange = 12;
            region.flashlightAngle = 52; region.flashlightInnerAngle = 24; region.flashlightShadows = LightShadows.Soft;
            var volume = region.GetComponent<Volume>(); volume.blendDistance = region.blendDistance; volume.sharedProfile = profile;
            foreach (var flicker in root.GetComponentsInChildren<TCFlickerLight>(true))
                flicker.enabled = false; // Preserve component IDs; no random outage on navigation lights.
            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.name.StartsWith("Fluorescent light") && light.intensity > 0)
                {
                    light.type = LightType.Spot; light.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    light.spotAngle = 112; light.innerSpotAngle = 72; light.range = 9;
                    light.color = new Color(.71f, .84f, .81f);
                    // Ceiling sources now cast downward instead of shining through the suspended ceiling.
                    light.shadows = light.name == "Fluorescent light 0" || light.name == "Fluorescent light 7" ? LightShadows.Soft : LightShadows.None;
                    ShadowBudget(light, 1);
                }
                if (light.name.StartsWith("Cold window spill"))
                {
                    light.type = LightType.Spot; light.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    light.spotAngle = 120; light.innerSpotAngle = 85; light.range = 7;
                    light.intensity = .75f; light.color = new Color(.57f, .68f, .78f);
                }
                if (light.name == "Red exit spill") { light.color = new Color(.8f, .18f, .12f); light.intensity = .65f; }
                if (light.name == "Red quarantine light") { light.color = new Color(.7f, .15f, .09f); light.intensity = .8f; }
                if (light.name == "Warm desk lamp") { light.color = new Color(1, .73f, .45f); ShadowBudget(light, 0); }
                light.shadowBias = .035f; light.shadowNormalBias = .12f;
            }
            var probe = root.GetComponentInChildren<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Custom; probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.intensity = .35f; probe.boxProjection = true; probe.farClipPlane = 35;
            probe.backgroundColor = region.fogColor; probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
            PrefabUtility.SaveAsPrefabAsset(root, Interior);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        PolishStructureMaterials();
        BakeReflection();
        ClassRoomLightingVerifier.Verify();
        ClassRoomLightingVerifier.Capture("After");
        Debug.Log("CLASSROOM_LIGHTING_BUILD_OK");
    }
    public static void Finish()
    {
        RequireIsolated();
        var root = PrefabUtility.LoadPrefabContents(Interior);
        try { root.GetComponentInChildren<TCRegionAtmosphere>().stabilizeBackgroundShadows = true; PrefabUtility.SaveAsPrefabAsset(root, Interior); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        PolishStructureMaterials(); BakeReflection(); ClassRoomLightingVerifier.Verify(); ClassRoomLightingVerifier.Capture("After");
    }
    static void PolishStructureMaterials()
    {
        // These two existing materials are referenced only by the ClassRoom structure.
        var plaster = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ClassRoom/Materials/Plaster.mat");
        plaster.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/Masonry_Albedo.png"));
        plaster.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/Masonry_Normal.png"));
        plaster.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/Masonry_MetalRough.png"));
        plaster.EnableKeyword("_NORMALMAP"); plaster.EnableKeyword("_METALLICSPECGLOSSMAP");
        plaster.SetColor("_BaseColor", new Color(.55f,.51f,.44f)); plaster.SetFloat("_Smoothness", .65f); plaster.SetFloat("_BumpScale", .3f);
        foreach(string property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" }) plaster.SetTextureScale(property, new Vector2(6,2));
        EditorUtility.SetDirty(plaster);
        var floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/ClassRoom/Materials/Floor.mat");
        string original = "Assets/Art/DirectorOffice/Textures/Floor.png", target = Art + "/Textures/Floor.png";
        if (!File.Exists(target)) AssetDatabase.CopyAsset(original, target);
        var importer = (TextureImporter)AssetImporter.GetAtPath(target);
        importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4; importer.mipmapEnabled = true; importer.SaveAndReimport();
        floor.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(target));
        floor.SetTexture("_DetailNormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "/Textures/Masonry_Normal.png"));
        floor.SetTextureScale("_DetailNormalMap", new Vector2(30,25)); floor.SetFloat("_DetailNormalMapScale", .18f);
        floor.EnableKeyword("_DETAIL_MULX2"); floor.SetFloat("_Metallic", 0); floor.SetFloat("_Smoothness", .26f);
        EditorUtility.SetDirty(floor); AssetDatabase.SaveAssets();
    }
    static void Configure<T>(VolumeProfile profile, Action<T> configure) where T : VolumeComponent
    {
        if (!profile.TryGet<T>(out var component)) { component = profile.Add<T>(true); AssetDatabase.AddObjectToAsset(component, profile); }
        configure(component); EditorUtility.SetDirty(component); EditorUtility.SetDirty(profile);
    }
    static void ShadowBudget(Light light, int tier)
    {
        var data = light.GetUniversalAdditionalLightData(); data.usePipelineSettings = false;
        var serialized = new SerializedObject(data);
        serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = tier;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void BakeReflection(string outputPath = null)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var room = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name == "ClassRoom" && !t.parent);
        var region = room.GetComponentInChildren<TCRegionAtmosphere>(); var probe = room.GetComponentInChildren<ReflectionProbe>();
        var hidden = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && !r.transform.IsChildOf(room)).ToArray();
        foreach (var renderer in hidden) renderer.enabled = false;
        var go = new GameObject("Static ClassRoom reflection capture"); var camera = go.AddComponent<Camera>();
        camera.enabled = false; camera.transform.position = probe.transform.position; camera.farClipPlane = 35; camera.nearClipPlane = .1f;
        var cubemap = new Cubemap(128, TextureFormat.RGBAHalf, true);
        try
        {
            TCRegionAtmosphere.ApplyForCamera(camera, new[] { region }, false);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            ShaderUtil.allowAsyncCompilation = false;
            if (!camera.RenderToCubemap(cubemap)) throw new Exception("Cubemap capture failed");
            string path = outputPath ?? Art + "/ClassRoomReflection.asset"; var existing = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (existing) { EditorUtility.CopySerialized(cubemap, existing); Object.DestroyImmediate(cubemap); } else { AssetDatabase.CreateAsset(cubemap, path); existing = cubemap; }
            var root = PrefabUtility.LoadPrefabContents(Interior);
            try { root.GetComponentInChildren<ReflectionProbe>().customBakedTexture = existing; PrefabUtility.SaveAsPrefabAsset(root, Interior); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
        finally { TCRegionAtmosphere.RestoreEnvironment(); Object.DestroyImmediate(go); foreach (var renderer in hidden) if (renderer) renderer.enabled = true; }
    }
    public static void RequireIsolated()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/.utmp/")) throw new Exception("Run in an isolated .utmp project");
    }
}
