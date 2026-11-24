using UnityEngine;

namespace UniDoom
{
    /// <summary>
    /// Legacy TextMesh uses Unity's "GUI/Text Shader" (ZTest Always), so its glyphs
    /// draw on top of walls. Attached to the Cylinder door plaques: it clones the
    /// original font material (keeping its atlas, colour and vertex colours) and only
    /// swaps the shader to a depth-tested copy of the same one.
    /// </summary>
    public sealed class DepthTestedWorldText : MonoBehaviour
    {
        [SerializeField] Material shaderSource;
        Material runtimeMaterial;

        void OnEnable()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer == null || shaderSource == null || shaderSource.shader == null) return;

            var original = renderer.sharedMaterial;
            if (original == null) return;

            var depthMaterial = new Material(original);
            depthMaterial.name = original.name + " (depth tested)";
            depthMaterial.shader = shaderSource.shader;
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
            runtimeMaterial = depthMaterial;
            renderer.sharedMaterial = depthMaterial;
        }

        void OnDestroy()
        {
            if (runtimeMaterial != null) Destroy(runtimeMaterial);
        }
    }
}
