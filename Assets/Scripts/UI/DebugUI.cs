using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{
    public static DebugUI Instance { get; private set; }

    [SerializeField] private int maxLines = 15;

    private Text uiText;
    private string[] lines;
    private int lineCount;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        lines = new string[maxLines];
        lineCount = 0;

        CreateCanvas();
    }

    void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("DebugCanvas");
        canvasObj.transform.SetParent(transform);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject textObj = new GameObject("DebugText");
        textObj.transform.SetParent(canvasObj.transform);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -10);
        rt.sizeDelta = new Vector2(600, 400);

        uiText = textObj.AddComponent<Text>();
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.fontSize = 16;
        uiText.color = Color.green;
        uiText.alignment = TextAnchor.UpperLeft;
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;

        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);
    }

    void Update()
    {
        if (uiText != null && lineCount > 0)
        {
            float age = Time.unscaledDeltaTime;

            for (int i = lineCount - 1; i >= 0; i--)
            {
                if (lineTimers[i] > 0)
                {
                    lineTimers[i] -= age;
                    if (lineTimers[i] <= 0)
                    {
                        RemoveLine(i);
                    }
                }
            }
        }
    }

    private float[] lineTimers;

    void InitTimers()
    {
        if (lineTimers == null)
            lineTimers = new float[maxLines];
    }

    public static void Log(string message)
    {
        if (Instance != null)
            Instance.AddLine($"[LOG] {message}", 5f);
    }

    public static void LogWarning(string message)
    {
        if (Instance != null)
            Instance.AddLine($"[WARN] {message}", 7f);
    }

    public static void LogError(string message)
    {
        if (Instance != null)
            Instance.AddLine($"[ERROR] {message}", 10f);
    }

    void AddLine(string text, float duration)
    {
        InitTimers();

        if (lineCount < maxLines)
        {
            lines[lineCount] = text;
            lineTimers[lineCount] = duration;
            lineCount++;
        }
        else
        {
            for (int i = 0; i < maxLines - 1; i++)
            {
                lines[i] = lines[i + 1];
                lineTimers[i] = lineTimers[i + 1];
            }
            lines[maxLines - 1] = text;
            lineTimers[maxLines - 1] = duration;
        }

        RefreshDisplay();
    }

    void RemoveLine(int index)
    {
        for (int i = index; i < lineCount - 1; i++)
        {
            lines[i] = lines[i + 1];
            lineTimers[i] = lineTimers[i + 1];
        }
        lineCount--;
        RefreshDisplay();
    }

    void RefreshDisplay()
    {
        if (uiText == null) return;

        if (lineCount == 0)
        {
            uiText.text = "";
            return;
        }

        string display = "";
        for (int i = 0; i < lineCount; i++)
        {
            display += lines[i] + "\n";
        }
        uiText.text = display;
    }
}
