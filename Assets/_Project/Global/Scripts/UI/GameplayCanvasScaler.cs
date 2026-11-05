using UnityEngine;
using UnityEngine.UI;

namespace UniDoom.UI
{
    /// <summary>Consistent HUD/weapon proportions, including narrower and ultrawide displays.</summary>
    [DisallowMultipleComponent]
    public sealed class GameplayCanvasScaler : CanvasScaler
    {
        [SerializeField, Min(0)] float minimumScale = .5f;
        [SerializeField] bool respectSafeArea = true;
        Canvas targetCanvas;
        RectTransform safeRoot;
        Rect previousSafeArea, previousViewport;

        protected override void OnEnable()
        {
            targetCanvas = GetComponent<Canvas>();
            base.OnEnable();
        }

        protected override void Start()
        {
            base.Start();
            if (!Application.isPlaying || !respectSafeArea || safeRoot != null) return;

            // Keep the existing anchors and weapon offsets relative to a single inset frame.
            // Start runs after Awake, including KeysUI's dynamically created life icons.
            int children = transform.childCount;
            safeRoot = new GameObject("HUD safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(transform, false);
            safeRoot.anchorMin = Vector2.zero;
            safeRoot.anchorMax = Vector2.one;
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
            for (int i = 0; i < children; i++)
            {
                var child = transform.GetChild(0);
                var rect = child as RectTransform;
                Vector2 position = rect != null ? rect.anchoredPosition : Vector2.zero;
                child.SetParent(safeRoot, false);
                if (rect != null) rect.anchoredPosition = position;
            }
            UpdateSafeArea();
        }

        protected override void HandleScaleWithScreenSize()
        {
            if (targetCanvas == null) return;
            // Honor the serialized Canvas Scaler reference resolution and width/height match.
            base.HandleScaleWithScreenSize();
            SetScaleFactor(Mathf.Max(Mathf.Max(.01f, minimumScale), targetCanvas.scaleFactor));
        }

        public static float CalculateScale(Vector2 pixels, float minimum = .5f, float match = 1f)
        {
            float width = Mathf.Max(.01f, pixels.x / 1920f);
            float height = Mathf.Max(.01f, pixels.y / 1080f);
            float proportional = Mathf.Pow(2, Mathf.Lerp(Mathf.Log(width, 2), Mathf.Log(height, 2), Mathf.Clamp01(match)));
            return Mathf.Max(Mathf.Max(.01f, minimum), proportional);
        }

        protected override void Handle()
        {
            base.Handle();
            if (safeRoot != null) UpdateSafeArea();
        }

        void UpdateSafeArea()
        {
            Rect viewport = targetCanvas.pixelRect;
            if (viewport.width <= 0 || viewport.height <= 0) return;
            Rect safe = targetCanvas.targetDisplay == 0 ? Screen.safeArea : viewport;
            safe = Rect.MinMaxRect(Mathf.Max(safe.xMin, viewport.xMin), Mathf.Max(safe.yMin, viewport.yMin),
                Mathf.Min(safe.xMax, viewport.xMax), Mathf.Min(safe.yMax, viewport.yMax));
            if (safe.width <= 0 || safe.height <= 0) safe = viewport;
            if (safe == previousSafeArea && viewport == previousViewport) return;
            previousSafeArea = safe;
            previousViewport = viewport;
            safeRoot.anchorMin = new Vector2((safe.xMin - viewport.xMin) / viewport.width,
                (safe.yMin - viewport.yMin) / viewport.height);
            safeRoot.anchorMax = new Vector2((safe.xMax - viewport.xMin) / viewport.width,
                (safe.yMax - viewport.yMin) / viewport.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }
    }
}
