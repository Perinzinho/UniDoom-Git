using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public enum TotalChaosSurfacePreset { Concrete, Metal, Wood, Stone, Deteriorated, Tiles }

public class TotalChaosMaterialConverter : EditorWindow
{
    public const string Art = "Assets/_Project/Scenes/ClassRoom/Surface";
    public const string BasementArt = "Assets/_Project/Scenes/Basement/Surface";
    TotalChaosSurfacePreset preset;
    GameObject root;
    [MenuItem("Tools/Total Chaos/Material Converter")]
    static void Open() => GetWindow<TotalChaosMaterialConverter>("Total Chaos Materials");
    void OnGUI()
    {
        root = (GameObject)EditorGUILayout.ObjectField("Hierarchy to apply / revert", root, typeof(GameObject), true);
        preset = (TotalChaosSurfacePreset)EditorGUILayout.EnumPopup("Preset", preset);
        EditorGUILayout.LabelField("Selected opaque URP Lit materials are copied. Originals are retained.", EditorStyles.wordWrappedLabel);
        if (root && GUILayout.Button("Select eligible hierarchy materials"))
            Selection.objects = root.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Where(Eligible).Distinct().Cast<Object>().ToArray();
        if (GUILayout.Button("Convert selected materials"))
        {
            foreach (var source in Selection.objects.OfType<Material>())
            {
                bool basement = root && root.GetComponentsInParent<Transform>(true).Any(t => t.name.StartsWith("Basement"));
                var converted = Convert(source, preset, basement ? BasementArt : Art);
                if (root && converted != source) Replace(root, source, converted);
            }
            AssetDatabase.SaveAssets();
        }
        if (GUILayout.Button("Revert converted references") && root) { Revert(root); AssetDatabase.SaveAssets(); }
    }
    public static bool Eligible(Material source) => source && source.shader &&
        source.shader.name == "Universal Render Pipeline/Lit" && source.GetFloat("_Surface") == 0 &&
        source.GetFloat("_AlphaClip") == 0 && source.GetFloat("_WorkflowMode") == 1 &&
        !source.IsKeywordEnabled("_EMISSION") && !Protected(source.name);
    static bool Protected(string name) => new[] { "blood", "glass", "chalk", "paper", "corpse", "book", "tube", "skilllamp", "caution", "coveredface", "vfx", "particle", "ui_" }
        .Any(part => name.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) || name == "Board" || name == "Wet" || name == "Map";
    public static Material Convert(Material source, TotalChaosSurfacePreset preset, string outputRoot = Art,
        string textureRoot = Art, string textureAsset = null)
    {
        if (!Eligible(source)) return source;
        string sourcePath = AssetDatabase.GetAssetPath(source), guid = AssetDatabase.AssetPathToGUID(sourcePath);
        if (string.IsNullOrEmpty(guid)) throw new InvalidOperationException("Save the source material asset before conversion.");
        Directory.CreateDirectory(outputRoot + "/Materials"); Directory.CreateDirectory(outputRoot + "/Originals"); AssetDatabase.Refresh();
        string path = outputRoot + "/Materials/" + guid + "_" + preset + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) return existing;
        string asset = preset == TotalChaosSurfacePreset.Metal ? "rusty_metal_02" : preset == TotalChaosSurfacePreset.Wood ? "wood_table_worn" :
            preset == TotalChaosSurfacePreset.Tiles ? "worn_tile_floor" : preset == TotalChaosSurfacePreset.Stone ? "rock_face" : "concrete_wall_006";
        string textures = textureRoot + "/Textures/" + (textureAsset ?? asset) + "/";
        var shader = Shader.Find("UniDoom/TotalChaosInspiredSurface");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Surface shader is missing or has compiler errors.");
        var material = new Material(source) { shader = shader, name = source.name + " TC " + preset };
        foreach (string key in new[] { "_SPECULAR_SETUP", "_PARALLAXMAP", "_DETAIL_MULX2", "_DETAIL_SCALED" }) material.DisableKeyword(key);
        Assign(material, "_BaseMap", textures + "Diffuse.png"); Assign(material, "_BumpMap", textures + "nor_gl.png");
        Assign(material, "_MetallicGlossMap", textures + "UnityMask.png"); Assign(material, "_OcclusionMap", textures + "UnityMask.png");
        Assign(material, "_DirtMap", textures + "UnityMask.png"); Assign(material, "_ParallaxMap", textures + "Displacement.png");
        material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.EnableKeyword("_OCCLUSIONMAP");
        material.SetFloat("_WorkflowMode", 1); material.SetFloat("_BumpScale", preset == TotalChaosSurfacePreset.Wood ? .38f : .55f);
        material.SetFloat("_Smoothness", preset == TotalChaosSurfacePreset.Metal ? .7f : preset == TotalChaosSurfacePreset.Wood ? .5f : .65f);
        material.SetFloat("_SmoothnessTextureChannel", 0); material.SetFloat("_OcclusionStrength", .5f);
        material.SetFloat("_Parallax", .005f); material.SetColor("_EmissionColor", Color.black);
        material.SetTexture("_DetailNormalMap", null); material.SetTexture("_DetailAlbedoMap", null);
        material.SetVector("_SpecColor", preset == TotalChaosSurfacePreset.Deteriorated ? new Vector4(.55f,.12f,.55f,1.05f) :
            new Vector4(.25f, preset == TotalChaosSurfacePreset.Metal ? .12f : .04f, .65f, 1));
        material.SetColor("_BaseColor", preset == TotalChaosSurfacePreset.Metal ? new Color(.65f,.67f,.63f) : preset == TotalChaosSurfacePreset.Wood ? new Color(1.35f,1.4f,1.32f) : new Color(.7f,.73f,.69f));
        material.enableInstancing = true;
        TotalChaosMaterialGUI.UpdateSharedMaskKeyword(material);
        AssetDatabase.CreateAsset(material, path);
        // Store a direct reference by GUID; filename is enough for reversal without a second material copy.
        File.WriteAllText(outputRoot + "/Originals/" + guid + "_" + preset + ".json", JsonUtility.ToJson(new Origin { materialGuid = guid, source = sourcePath }, true));
        return material;
    }
    [Serializable] class Origin { public string materialGuid, source; }
    static void Assign(Material material, string property, string path)
    {
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ProjectAssetPaths.Resolve(path));
        if (!texture) throw new InvalidOperationException("Missing imported PBR map: " + path);
        material.SetTexture(property, texture);
    }
    public static int Replace(GameObject root, Material original, Material replacement)
    {
        int changed = 0;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = renderer.sharedMaterials;
            if (!mats.Contains(original)) continue;
            Undo.RecordObject(renderer, "Convert Total Chaos material");
            renderer.sharedMaterials = mats.Select(m => m == original ? replacement : m).ToArray();
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); changed++;
        }
        return changed;
    }
    public static int Revert(GameObject root)
    {
        int changed = 0;
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mats = renderer.sharedMaterials; bool any = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (!mats[i]) continue;
                string path = AssetDatabase.GetAssetPath(mats[i]);
                if (!path.StartsWith(Art + "/Materials/") && !path.StartsWith(BasementArt + "/Materials/")) continue;
                string guid = Path.GetFileNameWithoutExtension(path).Split('_')[0];
                var original = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (!original) throw new InvalidOperationException("Original material missing: " + guid);
                mats[i] = original; any = true;
            }
            if (!any) continue;
            Undo.RecordObject(renderer, "Revert Total Chaos material"); renderer.sharedMaterials = mats;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer); changed++;
        }
        foreach(var probe in root.GetComponentsInChildren<ReflectionProbe>(true))
            if(AssetDatabase.GetAssetPath(probe.customBakedTexture) == Art + "/ClassRoomReflection.asset")
            {
                var original = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/_Project/Scenes/ClassRoom/Lighting/ClassRoomReflection.asset");
                if (!original) throw new InvalidOperationException("Original ClassRoom reflection missing");
                Undo.RecordObject(probe, "Revert ClassRoom reflection"); probe.customBakedTexture = original;
                PrefabUtility.RecordPrefabInstancePropertyModifications(probe);
            }
        return changed;
    }
}

public class TotalChaosMaterialGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        editor.PropertiesDefaultGUI(properties.Where(p => p.name != "_SpecColor" && p.name != "_WorkflowMode" && p.name != "_SpecGlossMap").ToArray());
        var controls = FindProperty("_SpecColor", properties); var value = controls.vectorValue;
        bool parallax = ((Material)editor.target).IsKeywordEnabled("_PARALLAXMAP");
        EditorGUI.BeginChangeCheck(); parallax = EditorGUILayout.Toggle("Enable optional parallax", parallax);
        if (EditorGUI.EndChangeCheck())
        {
            editor.RegisterPropertyChangeUndo("Optional parallax");
            foreach(var material in editor.targets.OfType<Material>()) Keyword(material,"_PARALLAXMAP",parallax && material.GetTexture("_ParallaxMap"));
        }
        EditorGUI.BeginChangeCheck();
        value.x = EditorGUILayout.Slider("Cavity dirt", value.x, 0, 1); value.y = EditorGUILayout.Slider("Cavity wetness", value.y, 0, 1);
        value.z = EditorGUILayout.Slider("Surface saturation", value.z, 0, 1.5f); value.w = EditorGUILayout.Slider("Surface contrast", value.w, .5f, 1.5f);
        if (EditorGUI.EndChangeCheck()) { editor.RegisterPropertyChangeUndo("Surface controls"); controls.vectorValue = value; }
        foreach (var material in editor.targets.OfType<Material>()) ValidateMaterial(material);
    }
    public override void ValidateMaterial(Material material)
    {
        material.SetFloat("_WorkflowMode", 1); material.DisableKeyword("_SPECULAR_SETUP");
        Keyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
        Keyword(material, "_METALLICSPECGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
        Keyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap"));
        Keyword(material, "_EMISSION", material.GetColor("_EmissionColor").maxColorComponent > 0);
        bool detail = material.GetTexture("_DetailNormalMap") || material.GetTexture("_DetailAlbedoMap");
        bool scaled = !Mathf.Approximately(material.GetFloat("_DetailAlbedoMapScale"), 1) ||
            !Mathf.Approximately(material.GetFloat("_DetailNormalMapScale"), 1);
        Keyword(material, "_DETAIL_MULX2", detail && !scaled);
        Keyword(material, "_DETAIL_SCALED", detail && scaled);
        UpdateSharedMaskKeyword(material);
    }
    // All three slots sample the same base UV in LitInput; their individual ST
    // values are unused. Identical texture objects also share sampler settings.
    public static void UpdateSharedMaskKeyword(Material material)
    {
        if (!material || !material.shader || material.shader.name != "UniDoom/TotalChaosInspiredSurface") return;
        var mask = material.GetTexture("_MetallicGlossMap");
        Keyword(material, "_TC_SHARED_MASK", mask &&
            material.IsKeywordEnabled("_METALLICSPECGLOSSMAP") &&
            !material.IsKeywordEnabled("_SPECULAR_SETUP") &&
            material.GetTexture("_OcclusionMap") == mask && material.GetTexture("_DirtMap") == mask);
    }
    static void Keyword(Material material, string key, bool active) { if (active) material.EnableKeyword(key); else material.DisableKeyword(key); }
}
