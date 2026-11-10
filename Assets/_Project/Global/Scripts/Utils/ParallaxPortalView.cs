using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UniDoom.Utils
{
    /// <summary>A wall window into a small, render-only diorama with off-axis perspective.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class ParallaxPortalView : MonoBehaviour
    {
        [SerializeField] Renderer surface;
        [SerializeField] GameObject previewPrefab;
        [SerializeField] Vector2 windowSize = new Vector2(2.5f, 3.15f);
        [SerializeField] Vector3 previewWindowCenter = new Vector3(0, 1.6f, 0);
        [SerializeField, Range(128, 1024)] int textureHeight = 512;
        [SerializeField, Range(1, 60)] float refreshRate = 30;
        [SerializeField, Min(1)] float maximumDistance = 26;
        [SerializeField, Min(.01f)] float perspectiveScale = .35f;

        const int PreviewLayer = 31;
        static readonly int ViewTexture = Shader.PropertyToID("_ViewTex");
        readonly Plane[] frustum = new Plane[6];
        readonly UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest();
        GameObject preview;
        Renderer[] previewRenderers;
        Camera viewCamera;
        Camera viewer;
        RenderTexture texture;
        MaterialPropertyBlock properties;
        Vector3 lastEye;
        double nextUpdate;
        bool hasFrame;
        bool wasVisible;

        public int RenderCount { get; private set; }
        public RenderTexture ViewTextureAsset => texture;

        void OnEnable()
        {
            hasFrame = wasVisible = false;
            viewer = null;
        }

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            if (!viewer || !viewer.isActiveAndEnabled) viewer = Camera.main;
            if (viewer) RenderForCamera(viewer);
        }

        // Also used by the editor integration check to exercise the actual runtime path.
        public void RenderForCamera(Camera source)
        {
            if (!isActiveAndEnabled || !surface || !previewPrefab || !source || source == viewCamera) return;
            Vector3 eye = surface.transform.InverseTransformPoint(source.transform.position);
            // Unity's Quad faces local -Z. The wall is immediately behind its +Z side.
            if (eye.z >= -.015f || Vector3.Distance(source.transform.position, surface.bounds.center) > maximumDistance ||
                (source.cullingMask & (1 << surface.gameObject.layer)) == 0 || !surface.enabled || !surface.gameObject.activeInHierarchy)
            { wasVisible = false; return; }
            GeometryUtility.CalculateFrustumPlanes(source, frustum);
            if (!GeometryUtility.TestPlanesAABB(frustum, surface.bounds)) { wasVisible = false; return; }
            bool newlyVisible = !wasVisible;
            wasVisible = true;
            // Eye movement changes perspective; camera rotation alone does not change a fixed window.
            if (hasFrame && !newlyVisible && ((eye - lastEye).sqrMagnitude < .000001f ||
                Time.realtimeSinceStartupAsDouble < nextUpdate)) return;
            if (!preview) CreatePreview();
            if (!preview || !viewCamera) return;

            Vector3 scale = surface.transform.lossyScale;
            Vector3 worldEye = new Vector3(eye.x * Mathf.Abs(scale.x), eye.y * Mathf.Abs(scale.y), eye.z * Mathf.Abs(scale.z));
            float distance = Mathf.Max(.05f, -worldEye.z) * perspectiveScale;
            Vector3 offset = new Vector3(worldEye.x, worldEye.y, 0) * perspectiveScale;
            viewCamera.transform.SetPositionAndRotation(preview.transform.position + previewWindowCenter + offset + Vector3.back * distance, Quaternion.identity);
            float near = Mathf.Max(.01f, distance);
            float halfWidth = windowSize.x * perspectiveScale * .5f;
            float halfHeight = windowSize.y * perspectiveScale * .5f;
            viewCamera.nearClipPlane = near;
            viewCamera.farClipPlane = distance + 35;
            viewCamera.projectionMatrix = Matrix4x4.Frustum(-halfWidth - offset.x, halfWidth - offset.x,
                -halfHeight - offset.y, halfHeight - offset.y, near, viewCamera.farClipPlane);
            request.destination = texture;
            try
            {
                foreach (var item in previewRenderers) if (item) item.forceRenderingOff = false;
                RenderPipeline.SubmitRenderRequest(viewCamera, request);
                hasFrame = true;
                lastEye = eye;
                nextUpdate = Time.realtimeSinceStartupAsDouble + 1.0 / Mathf.Max(1, refreshRate);
                RenderCount++;
            }
            finally
            {
                foreach (var item in previewRenderers) if (item) item.forceRenderingOff = true;
            }
        }

        void CreatePreview()
        {
            preview = Instantiate(previewPrefab, new Vector3(10000, 10000, 10000), Quaternion.identity);
            preview.name = "Portal render-only gate preview";
            preview.hideFlags = HideFlags.HideAndDontSave;
            foreach (var child in preview.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = PreviewLayer;
            previewRenderers = preview.GetComponentsInChildren<Renderer>(true);
            foreach (var item in previewRenderers) item.forceRenderingOff = true;
            var cameraObject = new GameObject("Portal preview camera") { hideFlags = HideFlags.HideAndDontSave };
            viewCamera = cameraObject.AddComponent<Camera>();
            viewCamera.enabled = false;
            viewCamera.cullingMask = 1 << PreviewLayer;
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = new Color(.17f, .22f, .27f, 1);
            viewCamera.allowHDR = false;
            viewCamera.allowMSAA = false;
            viewCamera.useOcclusionCulling = false;
            var data = viewCamera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresDepthOption = CameraOverrideOption.Off;
            data.antialiasing = AntialiasingMode.None;
            data.volumeLayerMask = 0;
            int height = Mathf.Clamp(textureHeight, 128, 1024);
            int width = Mathf.FloorToInt(height * windowSize.x / windowSize.y / 32) * 32;
            texture = new RenderTexture(Mathf.Max(128, width), height, 16, RenderTextureFormat.ARGB32)
            {
                name = "Portal gate view", hideFlags = HideFlags.HideAndDontSave,
                antiAliasing = 1, useMipMap = false, filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.Create();
            properties = new MaterialPropertyBlock();
            surface.GetPropertyBlock(properties);
            properties.SetTexture(ViewTexture, texture);
            surface.SetPropertyBlock(properties);
        }

        void OnDisable()
        {
            if (surface && properties != null)
            {
                surface.GetPropertyBlock(properties);
                properties.SetTexture(ViewTexture, Texture2D.blackTexture);
                surface.SetPropertyBlock(properties);
            }
            if (viewCamera) Release(viewCamera.gameObject);
            if (preview) Release(preview);
            if (texture) { texture.Release(); Release(texture); }
            viewCamera = null;
            preview = null;
            texture = null;
            hasFrame = wasVisible = false;
        }

        static void Release(Object item)
        {
            if (Application.isPlaying) Destroy(item); else DestroyImmediate(item);
        }
    }
}
