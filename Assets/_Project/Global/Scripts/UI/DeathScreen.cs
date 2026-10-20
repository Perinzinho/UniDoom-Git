using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace UniDoom.UI
{
    /// <summary>Animated horror/terminal overlay, built with the game's existing UI and fonts.</summary>
    public class DeathScreen : MonoBehaviour
    {
        public static DeathScreen Instance { get; private set; }
        public static bool IsVisible => Instance != null && Instance.deathActive;
        public bool CanRespawn => deathActive && !transitioning && Time.unscaledTime - openedAt >= 1.15f;

        private static readonly Color Red = new(0.94f, 0.16f, 0.12f, 1f);
        private static readonly Color Amber = new(1f, 0.73f, 0.32f, 1f);
        private static readonly Color Paper = new(0.92f, 0.9f, 0.85f, 1f);
        private static readonly Color Muted = new(0.52f, 0.52f, 0.55f, 1f);
        private PlayerRespawn player;
        private TMP_FontAsset font;
        private GameObject deathRoot;
        private CanvasGroup deathGroup;
        private RectTransform content;
        private Image blackout;
        private Image pulseBar;
        private TMP_Text checkpointText;
        private TMP_Text destinationText;
        private TMP_Text hint;
        private TMP_Text status;
        private Button retry;
        private readonly Image[] keyMarkers = new Image[3];
        private CanvasGroup toast;
        private TMP_Text toastLabel;
        private RectTransform toastRect;
        private float toastUntil;
        private float openedAt;
        private bool deathActive;
        private bool transitioning;

        private void Awake()
        {
            Instance = this;
            font = TMP_Settings.defaultFontAsset;
            BuildInterface();
            deathRoot.SetActive(false);
            toast.alpha = 0f;
        }

        public void Bind(PlayerRespawn owner) => player = owner;

        public void ShowCheckpoint(int index)
        {
            toastLabel.text = $"CHECKPOINT ATUALIZADO   /   CHAVE {index + 1:00}";
            toastUntil = Time.unscaledTime + 3.4f;
        }

        public void ShowDeath()
        {
            EnsureEventSystem();
            deathActive = true;
            transitioning = false;
            openedAt = Time.unscaledTime;
            toastUntil = 0f;
            toast.alpha = 0f;
            deathRoot.SetActive(true);
            deathGroup.alpha = 0f;
            blackout.color = Color.clear;
            retry.interactable = false;
            bool saved = player != null && player.HasCheckpoint;
            checkpointText.text = saved ? $"ÚLTIMO CHECKPOINT  /  CHAVE {player.CheckpointKeyIndex + 1:00}" : "PONTO DE RETORNO  /  INÍCIO";
            destinationText.text = saved ? "As chaves estão com você. Continue de onde parou." : "Nenhuma chave coletada. Uma nova tentativa começa no spawn inicial.";
            PlayerPickupKey keys = player != null ? player.GetComponent<PlayerPickupKey>() : null;
            for (int i = 0; i < keyMarkers.Length; i++)
            {
                bool collected = keys != null && (i == 0 ? keys.key01 : i == 1 ? keys.key02 : keys.key03);
                keyMarkers[i].color = collected ? Amber : new Color(0.16f, 0.16f, 0.18f);
            }
        }

        public void HideDeath()
        {
            deathActive = false;
            transitioning = false;
            if (deathRoot != null) deathRoot.SetActive(false);
            if (EventSystem.current != null && retry != null && EventSystem.current.currentSelectedGameObject == retry.gameObject)
                EventSystem.current.SetSelectedGameObject(null);
        }

        private void Update()
        {
            float remaining = toastUntil - Time.unscaledTime;
            float targetToast = remaining > 0f && !deathActive ? Mathf.Clamp01(remaining / 0.4f) : 0f;
            toast.alpha = Mathf.MoveTowards(toast.alpha, targetToast, Time.unscaledDeltaTime * 5f);
            toastRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-108f, -88f, toast.alpha));
            if (!deathActive || transitioning) return;
            float age = Time.unscaledTime - openedAt;
            float reveal = Mathf.SmoothStep(0f, 1f, age / 0.65f);
            deathGroup.alpha = reveal;
            content.anchoredPosition = new Vector2(0f, Mathf.Lerp(-22f, 0f, reveal));
            pulseBar.color = Color.Lerp(Red * 0.45f, Red, 0.5f + 0.5f * Mathf.Sin(age * 3f));
            status.text = age < 0.9f ? "ENCERRANDO SINAIS VITAIS..." : "SINAL VITAL  __________________  00 BPM";
            bool ready = CanRespawn;
            hint.text = ready ? "[ ENTER / ESPAÇO ]  VOLTAR À LUTA" : "AGUARDE  /  RECUPERANDO PONTO DE RETORNO";
            if (retry.interactable != ready)
            {
                retry.interactable = ready;
                if (ready && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(retry.gameObject);
            }
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            if (ready && ((keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
                          (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)))
                player.TryRespawn();
        }

        public IEnumerator FadeToBlack()
        {
            transitioning = true;
            retry.interactable = false;
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                blackout.color = new Color(0f, 0f, 0f, Mathf.Clamp01(t / 0.3f));
                yield return null;
            }
            blackout.color = Color.black;
        }

        public IEnumerator FadeFromBlack()
        {
            // Remove the death card under black, then reveal the actual checkpoint.
            deathGroup.alpha = 0f;
            for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
            {
                blackout.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(t / 0.45f));
                yield return null;
            }
            blackout.color = Color.clear;
        }

        private void BuildInterface()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 12000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();

            deathRoot = Rect("DeathScreen", transform, Vector2.zero, Vector2.zero).gameObject;
            Stretch(deathRoot.GetComponent<RectTransform>());
            var panel = Rect("Overlay", deathRoot.transform, Vector2.zero, Vector2.zero);
            Stretch(panel);
            deathGroup = panel.gameObject.AddComponent<CanvasGroup>();
            Image bg = Image("Charcoal", panel, Vector2.zero, Vector2.zero, new Color(0.04f, 0.024f, 0.032f, 0.985f));
            Stretch(bg.rectTransform);
            bg.raycastTarget = true;
            var pattern = Rect("Interference", panel, Vector2.zero, Vector2.zero);
            Stretch(pattern);
            pattern.gameObject.AddComponent<DeathSignalGraphic>().color = new Color(Red.r, Red.g, Red.b, 0.13f);
            Image("TopEdge", panel, new Vector2(0f, -2f), new Vector2(1920f, 4f), Red, new Vector2(0.5f, 1f));
            Image("BottomEdge", panel, new Vector2(0f, 2f), new Vector2(1920f, 4f), Red, new Vector2(0.5f, 0f));

            content = Rect("DeathCard", panel, Vector2.zero, new Vector2(940f, 680f));
            Text("Brand", content, "U N I D O O M   /   PROTOCOLO DE SOBREVIVÊNCIA", new Vector2(0, 306), new Vector2(920, 30), 17, Muted);
            Image("HeaderRule", content, new Vector2(0, 267), new Vector2(920, 1), new Color(0.3f, 0.12f, 0.12f));
            Image("WarningLED", content, new Vector2(-150, 220), new Vector2(7, 7), Red);
            Text("Warning", content, "CONEXÃO INTERROMPIDA", new Vector2(24, 220), new Vector2(360, 24), 19, Red);
            var title = Text("Title", content, "VOCÊ MORREU", new Vector2(0, 130), new Vector2(940, 150), 112, Paper);
            title.fontStyle = FontStyles.Bold;
            title.enableAutoSizing = true;
            title.fontSizeMin = 64;
            title.fontSizeMax = 112;
            title.characterSpacing = 4f;
            Text("Epitaph", content, "O CAMPUS AINDA NÃO TERMINOU COM VOCÊ.", new Vector2(0, 38), new Vector2(900, 30), 19, Muted);
            Image("SignalRail", content, new Vector2(0, -14), new Vector2(920, 2), new Color(0.25f, 0.07f, 0.08f));
            pulseBar = Image("Signal", content, new Vector2(0, -14), new Vector2(180, 2), Red);
            status = Text("VitalStatus", content, "SINAL VITAL  __________________  00 BPM", new Vector2(0, -44), new Vector2(900, 25), 13, Red);

            Image("CheckpointPanel", content, new Vector2(0, -144), new Vector2(920, 134), new Color(0.055f, 0.049f, 0.06f));
            Image("CheckpointAccent", content, new Vector2(-459, -144), new Vector2(2, 134), Amber);
            checkpointText = Text("CheckpointTitle", content, "", new Vector2(0, -111), new Vector2(860, 25), 19, Amber);
            destinationText = Text("CheckpointDetail", content, "", new Vector2(0, -147), new Vector2(880, 26), 16, Paper);
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 102f;
                keyMarkers[i] = Image($"Key{i + 1}", content, new Vector2(x - 30f, -184), new Vector2(6, 6), Muted);
                Text($"KeyLabel{i + 1}", content, $"{i + 1:00}", new Vector2(x, -184), new Vector2(40, 18), 12, Muted);
            }

            Image buttonImage = Image("ReturnToCheckpoint", content, new Vector2(0, -270), new Vector2(520, 62), Color.white);
            buttonImage.raycastTarget = true;
            retry = buttonImage.gameObject.AddComponent<Button>();
            retry.targetGraphic = buttonImage;
            retry.colors = new ColorBlock
            {
                normalColor = new Color(0.69f, 0.075f, 0.05f),
                highlightedColor = Red,
                selectedColor = new Color(0.85f, 0.13f, 0.08f),
                pressedColor = new Color(0.4f, 0.04f, 0.03f),
                disabledColor = new Color(0.19f, 0.09f, 0.09f),
                colorMultiplier = 1f,
                fadeDuration = 0.12f
            };
            retry.onClick.AddListener(() => player.TryRespawn());
            var label = Text("Label", buttonImage.transform, "RETORNAR À LUTA   >", Vector2.zero, new Vector2(500, 50), 21, Paper);
            label.fontStyle = FontStyles.Bold;
            hint = Text("RetryHint", content, "", new Vector2(0, -322), new Vector2(920, 24), 13, Muted);
            Text("Footer", panel, "A MORTE É UM DESVIO. NÃO O FIM.", new Vector2(0, 35), new Vector2(700, 22), 13, Muted, new Vector2(0.5f, 0f));
            blackout = Image("RespawnFade", deathRoot.transform, Vector2.zero, Vector2.zero, Color.clear);
            Stretch(blackout.rectTransform);
            blackout.raycastTarget = false;

            toastRect = Rect("CheckpointNotification", transform, new Vector2(0, -88), new Vector2(620, 76), new Vector2(0.5f, 1f));
            toast = toastRect.gameObject.AddComponent<CanvasGroup>();
            toast.blocksRaycasts = false;
            toast.interactable = false;
            Image("Background", toastRect, Vector2.zero, new Vector2(620, 76), new Color(0.025f, 0.027f, 0.034f, 0.97f));
            Image("Accent", toastRect, new Vector2(-308, 0), new Vector2(4, 76), Amber);
            toastLabel = Text("Message", toastRect, "", new Vector2(0, 13), new Vector2(600, 24), 18, Amber);
            Text("Detail", toastRect, "SEU PONTO DE RETORNO FOI SALVO", new Vector2(0, -15), new Vector2(600, 22), 12, Paper);
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(events);
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size, Vector2? anchor = null)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Image Image(string name, Transform parent, Vector2 pos, Vector2 size, Color color, Vector2? anchor = null)
        {
            var image = Rect(name, parent, pos, size, anchor).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private TMP_Text Text(string name, Transform parent, string value, Vector2 pos, Vector2 size, float fontSize, Color color, Vector2? anchor = null)
        {
            var text = Rect(name, parent, pos, size, anchor).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }

    /// <summary>Resolution-independent scanlines and broken signal; no generated texture assets.</summary>
    public class DeathSignalGraphic : MaskableGraphic
    {
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            var edge = new Color(0.68f, 0.035f, 0.025f, 0.16f);
            GradientQuad(vh, new Rect(r.xMin, r.yMin, r.width * 0.42f, r.height), edge, edge, Color.clear, Color.clear);
            GradientQuad(vh, new Rect(r.xMax - r.width * 0.42f, r.yMin, r.width * 0.42f, r.height), Color.clear, Color.clear, edge, edge);
            for (float y = r.yMin; y < r.yMax; y += 7f)
                Quad(vh, new Rect(r.xMin, y, r.width, 1f), new Color(color.r, color.g, color.b, color.a * 0.35f));
            // Peripheral signal bars leave a quiet center for the card.
            for (int i = 0; i < 22; i++)
            {
                float y = Mathf.Lerp(r.yMin + 70f, r.yMax - 70f, i / 21f);
                float width = i % 4 == 0 ? 92f : i % 3 == 0 ? 46f : 18f;
                Quad(vh, new Rect(r.xMin + 32f, y, width, 2f), color);
                Quad(vh, new Rect(r.xMax - 32f - width, -y, width, 2f), color);
            }
        }

        private static void Quad(VertexHelper vh, Rect rect, Color tint)
        {
            GradientQuad(vh, rect, tint, tint, tint, tint);
        }

        private static void GradientQuad(VertexHelper vh, Rect rect, Color bottomLeft, Color topLeft, Color topRight, Color bottomRight)
        {
            int first = vh.currentVertCount;
            vh.AddVert(new Vector3(rect.xMin, rect.yMin), bottomLeft, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), topLeft, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), topRight, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), bottomRight, Vector2.zero);
            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
