using System.Linq;
using UnityEditor;
using UnityEngine;

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
