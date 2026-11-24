using UnityEngine;

namespace UniDoom
{
    /// <summary>
    /// Fallback for legacy TextMesh objects not covered by an attached
    /// DepthTestedWorldText: clone the font material once and swap only its shader
    /// for a depth-tested copy, so the font atlas and colour are preserved.
    /// </summary>
    public static class WorldTextDepthFix
    {
        const string MaterialPath = "Materials/UI/WorldTextDepthTested";
        static Material depthMaterial;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Apply()
        {
            var source = Resources.Load<Material>(MaterialPath);
            if (source == null || source.shader == null) return;

            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (renderer.GetComponent<TextMesh>() == null) continue;
                var original = renderer.sharedMaterial;
                if (original == null || original.shader == source.shader) continue;

                // One shared instance for every TextMesh using the same font atlas.
                if (depthMaterial == null)
                {
                    depthMaterial = new Material(original);
                    depthMaterial.name = original.name + " (depth tested)";
                    depthMaterial.shader = source.shader;
                }
                renderer.sharedMaterial = depthMaterial;
            }
        }
    }
}
