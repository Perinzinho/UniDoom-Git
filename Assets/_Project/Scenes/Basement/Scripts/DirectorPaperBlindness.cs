using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Camera-independent paper obstruction, owned and cleaned up by the hit player.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerLife))]
public sealed class DirectorPaperBlindness : MonoBehaviour
{
    PlayerLife life;
    GameObject canvasRoot;
    Image paper;
    float remaining, elapsed, tilt;
    public bool IsVisible => canvasRoot != null && canvasRoot.activeSelf;
    public float RemainingSeconds => remaining;

    void Awake() => life = GetComponent<PlayerLife>();

    void OnEnable()
    {
        life.OnDeath += Clear;
        SceneManager.activeSceneChanged += SceneChanged;
        SceneManager.sceneUnloaded += SceneUnloaded;
    }

    public void Show(Sprite sprite, float seconds)
    {
        if (!isActiveAndEnabled || sprite == null || life.IsDead ||
            UniDoom.UI.DoorReloadScreen.IsReloading) return;
        if (canvasRoot == null) CreateCanvas();
        paper.sprite = sprite;
        remaining = Mathf.Max(0, seconds);
        elapsed = 0;
        tilt = Random.Range(-5f, 5f);
        canvasRoot.SetActive(remaining > 0);
        LayoutPaper();
    }

    void CreateCanvas()
    {
        canvasRoot = new GameObject("Director paper blindness", typeof(RectTransform), typeof(Canvas));
        DontDestroyOnLoad(canvasRoot);
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 29900; // Above the normal HUD, below settings, blood, pause, death and loading UI.
        var group = canvasRoot.AddComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;
        var backdrop = new GameObject("Blood shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(canvasRoot.transform, false);
        Image shadow = backdrop.GetComponent<Image>();
        shadow.color = new Color(.045f, .004f, .003f, .96f);
        shadow.raycastTarget = false;
        shadow.rectTransform.anchorMin = Vector2.zero;
        shadow.rectTransform.anchorMax = Vector2.one;
        shadow.rectTransform.offsetMin = shadow.rectTransform.offsetMax = Vector2.zero;
        var sheet = new GameObject("Matricule-se paper", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        sheet.transform.SetParent(canvasRoot.transform, false);
        paper = sheet.GetComponent<Image>();
        paper.preserveAspect = true;
        paper.useSpriteMesh = true;
        paper.raycastTarget = false;
        paper.rectTransform.anchorMin = paper.rectTransform.anchorMax = new Vector2(.5f, .5f);
    }

    void Update()
    {
        if (!IsVisible) return;
        if (life.IsDead || UniDoom.UI.DoorReloadScreen.IsReloading) { Clear(); return; }
        // Use gameplay time: pausing also pauses the obstruction timer.
        remaining = Mathf.Max(0, remaining - Time.deltaTime);
        elapsed += Time.deltaTime;
        if (remaining <= 0) { Clear(); return; }
        LayoutPaper();
    }

    void LayoutPaper()
    {
        float size = Mathf.Max(Screen.height * 1.85f, Screen.width * 1.05f);
        float pop = Mathf.Lerp(1.08f, 1f, Mathf.Clamp01(elapsed / .12f));
        paper.rectTransform.sizeDelta = new Vector2(size, size) * pop;
        paper.rectTransform.anchoredPosition = new Vector2(0, size * .09f);
        paper.rectTransform.localRotation = Quaternion.Euler(0, 0, tilt + Mathf.Sin(elapsed * 2.8f) * .7f);
    }

    public void Clear()
    {
        remaining = 0;
        if (canvasRoot != null) canvasRoot.SetActive(false);
    }

    void SceneChanged(Scene previous, Scene current) => Clear();
    void SceneUnloaded(Scene scene) => Clear();

    void OnDisable()
    {
        if (life != null) life.OnDeath -= Clear;
        SceneManager.activeSceneChanged -= SceneChanged;
        SceneManager.sceneUnloaded -= SceneUnloaded;
        Clear();
    }

    void OnDestroy()
    {
        if (canvasRoot != null) Destroy(canvasRoot);
    }
}
