using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HeartAnimator : MonoBehaviour
{
    [Header("Animação idle")]
    [SerializeField] private float idleFrameRate = 3f;

    [Header("Número de vida")]
    [SerializeField] private float fontSize = 72f;
    [SerializeField] private Color textColor = Color.white;

    [Header("Feedback de dano")]
    [SerializeField] private float hitScale = 1.25f;
    [SerializeField] private float hitDuration = 0.25f;
    [SerializeField] private Color hitColor = Color.red;

    private const float PageMargin = 24f;
    private const float HeartSize = 130f;
    private const float Gap = 16f;

    private PlayerLife playerLife;
    private Sprite[] idleFrames;
    private Image heartImage;
    private TextMeshProUGUI healthText;

    private int currentFrame;
    private int lastHealth = -1;
    private float frameTimer;
    private float hitTimer;

    public int CurrentFrame => currentFrame;
    public int FrameCount => idleFrames != null ? idleFrames.Length : 0;

    // ---------- Unity ----------

    private void OnDestroy()
    {
        if (playerLife == null)
            return;

        playerLife.OnHealthChanged -= HandleHealthChanged;
    }

    private void OnValidate()
    {
        idleFrameRate = Mathf.Max(1f, idleFrameRate);
        fontSize = Mathf.Max(1f, fontSize);
        hitScale = Mathf.Max(1f, hitScale);
        hitDuration = Mathf.Max(0.01f, hitDuration);
    }

    private void Update()
    {
        UpdateIdle();
        UpdateHit();
    }


    public void Init(PlayerLife life, Sprite[] frames, TMP_FontAsset font)
    {
        BuildCanvas();
        heartImage = CreateHeart();
        healthText = CreateText(font);

        SetHeartFrames(frames);
        SetPlayer(life);
    }

    public void SetPlayer(PlayerLife life)
    {
        if (playerLife != null)
            playerLife.OnHealthChanged -= HandleHealthChanged;

        playerLife = life;
        lastHealth = -1; // não pisca de dano na primeira leitura

        if (playerLife == null)
            return;

        playerLife.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(playerLife.CurrentHealth, playerLife.MaxHealth);
    }

    public void SetHeartFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0 || heartImage == null)
            return;

        idleFrames = frames;
        currentFrame = 0;
        frameTimer = 0f;
        heartImage.sprite = idleFrames[0];
    }


    private void HandleHealthChanged(int current, int max)
    {
        if (healthText != null)
            healthText.SetText("{0}", current); // sem alocação

        bool tookDamage = lastHealth >= 0 && current < lastHealth;
        lastHealth = current;

        if (tookDamage)
            hitTimer = hitDuration;
    }

    private void UpdateIdle()
    {
        if (heartImage == null || FrameCount < 2)
            return;

        float interval = 1f / idleFrameRate;

        frameTimer += Time.deltaTime;
        if (frameTimer < interval)
            return;

        frameTimer -= interval;
        currentFrame = (currentFrame + 1) % FrameCount;
        heartImage.sprite = idleFrames[currentFrame];
    }

    private void UpdateHit()
    {
        if (heartImage == null || hitTimer <= 0f)
            return;

        hitTimer = Mathf.Max(0f, hitTimer - Time.deltaTime);

        float t = hitTimer / hitDuration; // 1 no impacto, 0 no fim
        heartImage.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, hitScale, t);
        heartImage.color = Color.Lerp(Color.white, hitColor, t);
    }

    private void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private Image CreateHeart()
    {
        RectTransform rt = CreateRect("HealthHeart", new Vector2(0.5f, 0.5f));
        rt.anchoredPosition = new Vector2(PageMargin + HeartSize * 0.5f, PageMargin + HeartSize * 0.5f);
        rt.sizeDelta = new Vector2(HeartSize, HeartSize);

        Image image = rt.gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private TextMeshProUGUI CreateText(TMP_FontAsset font)
    {
        RectTransform rt = CreateRect("HealthValue", new Vector2(0f, 0.5f));
        rt.anchoredPosition = new Vector2(PageMargin + HeartSize + Gap, PageMargin + HeartSize * 0.5f);
        rt.sizeDelta = new Vector2(400f, HeartSize);

        TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;

        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = false;
        return text;
    }

    private RectTransform CreateRect(string objName, Vector2 pivot)
    {
        RectTransform rt = new GameObject(objName, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = pivot;
        return rt;
    }
}