using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerLife))]
public sealed class PlayerDamageFeedback : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float duration = 1f;
    private PlayerLife life;
    private CanvasGroup overlay;
    private BloodDamageGraphic blood;
    private float remaining;
    private int hitPattern;

    public bool IsVisible => overlay != null && overlay.alpha > 0f;

    private void Awake()
    {
        life = GetComponent<PlayerLife>();
        var root = new GameObject("Player blood damage overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        // Independent screen-space canvas: the player root has a non-uniform world scale.
        DontDestroyOnLoad(root);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000; // Below death/loading/menu screens, above the normal HUD.
        overlay = root.GetComponent<CanvasGroup>();
        overlay.blocksRaycasts = overlay.interactable = false;
        overlay.alpha = 0;
        var image = new GameObject("Blood splatter", typeof(RectTransform), typeof(CanvasRenderer));
        image.transform.SetParent(root.transform, false);
        var rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        blood = image.AddComponent<BloodDamageGraphic>();
        blood.raycastTarget = false;
        root.SetActive(false);
    }

    private void OnEnable() => life.OnDamaged += ShowDamage;
    private void OnDisable()
    {
        if (life != null) life.OnDamaged -= ShowDamage;
        Clear();
    }

    private void ShowDamage(int amount)
    {
        if (amount <= 0 || overlay == null) return;
        remaining = duration;
        blood.SetPattern(++hitPattern);
        overlay.gameObject.SetActive(true);
        overlay.alpha = 1f;
    }

    private void Update()
    {
        if (remaining <= 0 || overlay == null) return;
        if (UniDoom.UI.DoorReloadScreen.IsReloading)
        {
            Clear();
            return;
        }
        // The effect also finishes when the player pauses or a fatal hit freezes game time.
        remaining = Mathf.Max(0, remaining - Time.unscaledDeltaTime);
        float progress = 1f - remaining / Mathf.Max(.1f, duration);
        overlay.alpha = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, 1f, progress));
        if (remaining <= 0) Clear();
    }

    private void Clear()
    {
        remaining = 0;
        if (overlay == null) return;
        overlay.alpha = 0;
        overlay.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay.gameObject);
    }
}
