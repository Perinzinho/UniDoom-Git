using UnityEngine;

namespace UniDoom
{
    /// <summary>
    /// Legacy TextMesh uses Unity's built-in "GUI/Text Shader", whose ZTest Always
    /// makes the Cylinder door plaques render through walls. Swap those plaques to a
    /// depth-tested copy of the same shader, preserving the font atlas and vertex colour.
    /// </summary>
    public static class WorldTextDepthFix
    {
        const string MaterialPath = "Materials/UI/WorldTextDepthTested";
        const string PlaqueName = "placa de destino";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Apply()
        {
            var material = Resources.Load<Material>(MaterialPath);
            if (material == null || material.shader == null) return;

            foreach (var text in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if (text.font == null ||
                    text.name.IndexOf(PlaqueName, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var fontTexture = text.font.material != null ? text.font.material.mainTexture : null;
                if (fontTexture == null) continue;

                material.mainTexture = fontTexture;
                var renderer = text.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.sharedMaterial = material;
            }
        }
    }
}
