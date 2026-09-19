using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HeartAnimator : MonoBehaviour
{
    [SerializeField] private Image heartImage;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private float idleFrameRate = 3f;

    private const float PageMargin = 24f;
    private const float HeartSize = 130f;
    private const float Gap = 16f;

    private PlayerLife playerLife;
    private Image numberImage;
    private int currentFrame;
    private float timer;

    public void Init(PlayerLife life)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        heartImage = CreateImage("HealthHeart", true, new Vector2(PageMargin, PageMargin), new Vector2(HeartSize, HeartSize));
        SetHeartFrames(LoadSheetFrames());

        numberImage = CreateImage("HealthValue", false, Vector2.zero, Vector2.zero);

        SetPlayer(life);
    }

    public void SetHeartFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
            return;

        idleFrames = frames;
        currentFrame = 0;
        timer = 0f;
        heartImage.sprite = idleFrames[0];
    }

    public void SetPlayer(PlayerLife life)
    {
        if (playerLife != null)
        {
            playerLife.OnHealthChanged -= HandleHealthChanged;
        }

        playerLife = life;
        if (playerLife == null)
            return;

        playerLife.OnHealthChanged += HandleHealthChanged;
        HandleHealthChanged(playerLife.CurrentHealth, playerLife.MaxHealth);
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (numberImage == null)
            return;

        numberImage.sprite = PixelFontRenderer.Render(current.ToString(), 4);

        Rect r = numberImage.sprite.rect;
        RectTransform rt = (RectTransform)numberImage.transform;
        rt.sizeDelta = new Vector2(r.width, r.height);
        rt.anchoredPosition = new Vector2(
            PageMargin + HeartSize + Gap,
            PageMargin + HeartSize * 0.5f - r.height * 0.5f);
    }

    private void Update()
    {
        if (heartImage == null || idleFrames == null || idleFrames.Length == 0)
            return;

        float frameInterval = 1f / Mathf.Max(idleFrameRate, 1f);

        timer += Time.deltaTime;
        if (timer >= frameInterval)
        {
            timer = 0f;

            currentFrame = (currentFrame + 1) % idleFrames.Length;
            heartImage.sprite = idleFrames[currentFrame];
        }
    }

    private Image CreateImage(string name, bool preserveAspect, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image image = go.AddComponent<Image>();
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    private static Sprite[] LoadSheetFrames()
    {
#if UNITY_EDITOR
        const string path = "Assets/Sprites/life_sprt.png";

        if (!System.IO.File.Exists(path))
            return null;

        return UnityEditor.AssetDatabase
            .LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();
#else
        return null;
#endif
    }

    private void OnDestroy()
    {
        if (playerLife != null)
        {
            playerLife.OnHealthChanged -= HandleHealthChanged;
        }
    }
}