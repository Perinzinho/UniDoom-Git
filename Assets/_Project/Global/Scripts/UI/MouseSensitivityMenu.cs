using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Cinemachine;

namespace UniDoom.UI
{
    /// <summary>
    /// Menu interativo de sensibilidade do mouse com estética Sci-Fi/Doom premium.
    /// Posicionado no canto superior esquerdo, acionado exclusivamente pela tecla ESC ou pelo widget HUD.
    /// Possui cantos arredondados suavizados (9-slice), barra de LED dinâmica, slider arrastável,
    /// presets táteis e animações suaves de abertura.
    /// </summary>
    public class MouseSensitivityMenu : MonoBehaviour
    {
        public static MouseSensitivityMenu Instance { get; private set; }

        /// <summary>Indica se o menu está atualmente aberto.</summary>
        public static bool IsOpen => Instance != null && Instance.isMenuOpen;

        /// <summary>Sensibilidade atual do mouse (multiplicador de 0.1x a 3.0x).</summary>
        public static float CurrentSensitivity => Instance != null ? Instance.sensitivity : 1f;

        private const string PrefKeySensitivity = "UniDoom_MouseSensitivity";
        private const string PrefKeyInvertY = "UniDoom_MouseInvertY";

        [Header("Configurações")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Escape;
        [Range(0.1f, 3.0f)]
        [SerializeField] private float sensitivity = 1.0f;
        [SerializeField] private bool invertY = false;

        [Header("Paleta de Cores (Sci-Fi Dark / Doom Red)")]
        [SerializeField] private Color panelBgColor = new Color(0.045f, 0.052f, 0.070f, 0.97f);
        [SerializeField] private Color cardInnerBg = new Color(0.075f, 0.085f, 0.115f, 0.95f);
        [SerializeField] private Color doomRed = new Color(0.92f, 0.18f, 0.14f, 1f);
        [SerializeField] private Color doomRedGlow = new Color(1f, 0.28f, 0.22f, 0.9f);
        [SerializeField] private Color borderRed = new Color(0.55f, 0.12f, 0.10f, 0.9f);
        [SerializeField] private Color amberHeader = new Color(1f, 0.74f, 0.15f, 1f);
        [SerializeField] private Color buttonBgNormal = new Color(0.11f, 0.13f, 0.18f, 1f);
        [SerializeField] private Color buttonBgHover = new Color(0.18f, 0.22f, 0.30f, 1f);
        [SerializeField] private Color textMuted = new Color(0.55f, 0.60f, 0.70f, 1f);

        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private GraphicRaycaster graphicRaycaster;

        private RectTransform collapsedBadge;
        private RectTransform expandedPanel;
        private CanvasGroup panelCanvasGroup;

        private TMP_Text badgeValueText;
        private Image badgeLedDot;
        private TMP_Text valueReadoutText;
        private TMP_Text categoryBadgeText;
        private Image categoryBadgeBg;

        private Slider sensitivitySlider;
        private RectTransform sliderFillRect;
        private Image sliderFillImg;
        private Image sliderHandleImg;

        private Image[] ledSegments = new Image[10];
        private Button[] presetButtons = new Button[4];
        private Image[] presetButtonBgs = new Image[4];
        private TMP_Text[] presetButtonTexts = new TMP_Text[4];
        private readonly float[] presetValues = { 0.5f, 1.0f, 1.5f, 2.0f };

        private Toggle invertYToggle;
        private Image invertYSwitchBg;
        private RectTransform invertYSwitchKnob;
        private TMP_Text invertYStatusText;

        private bool isMenuOpen = false;
        private Coroutine transitionRoutine;
        private static readonly Dictionary<CinemachineInputAxisController, List<float>> registeredBaseGains = new Dictionary<CinemachineInputAxisController, List<float>>();

        private static Sprite roundedBoxSprite;
        private static Sprite roundedPillSprite;
        private static Sprite circleSprite;
        private TMP_FontAsset customFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var existing = FindAnyObjectByType<MouseSensitivityMenu>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    var host = new GameObject("[MouseSensitivityMenu]");
                    Instance = host.AddComponent<MouseSensitivityMenu>();
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitSprites();
            LoadPreferences();
            ResolveFont();
            EnsureEventSystem();
            BuildInterface();
            ApplySensitivityToAllControllers();
            SetMenuOpenImmediate(false);
        }

        private void Update()
        {
            // Atalho exclusivo ESC (compatível com Legacy e New Input System)
            bool escPressed = false;
            try
            {
                if (Input.GetKeyDown(toggleKey)) escPressed = true;
            }
            catch { }

#if ENABLE_INPUT_SYSTEM || true
            if (!escPressed && UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    escPressed = true;
                }
            }
#endif

            if (escPressed)
            {
                ToggleMenu();
            }

            // Garante cursor destravado e visível enquanto o menu estiver aberto
            if (isMenuOpen)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }

            // Pulso sutil no LED indicador do widget minimizado
            if (!isMenuOpen && badgeLedDot != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.5f);
                badgeLedDot.color = Color.Lerp(doomRed * 0.6f, doomRedGlow, pulse);
            }

            EnsureControllersRegistered();
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM || true
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
                DontDestroyOnLoad(es);
            }
        }

        private void LoadPreferences()
        {
            sensitivity = PlayerPrefs.GetFloat(PrefKeySensitivity, 1.0f);
            sensitivity = Mathf.Clamp(sensitivity, 0.1f, 3.0f);
            invertY = PlayerPrefs.GetInt(PrefKeyInvertY, 0) == 1;
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetFloat(PrefKeySensitivity, sensitivity);
            PlayerPrefs.SetInt(PrefKeyInvertY, invertY ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void ToggleMenu()
        {
            SetMenuOpen(!isMenuOpen);
        }

        public void SetMenuOpen(bool open)
        {
            if (isMenuOpen == open) return;
            isMenuOpen = open;

            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);

            transitionRoutine = StartCoroutine(TransitionMenuRoutine(open));
        }

        private void SetMenuOpenImmediate(bool open)
        {
            isMenuOpen = open;

            if (collapsedBadge != null)
                collapsedBadge.gameObject.SetActive(!open);

            if (expandedPanel != null)
            {
                expandedPanel.gameObject.SetActive(open);
                if (panelCanvasGroup != null)
                {
                    panelCanvasGroup.alpha = open ? 1f : 0f;
                    panelCanvasGroup.interactable = open;
                    panelCanvasGroup.blocksRaycasts = open;
                }
            }

            if (open)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                FreezeCameraInputs(true);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                FreezeCameraInputs(false);
                SavePreferences();
            }

            UpdateDisplay();
        }

        private IEnumerator TransitionMenuRoutine(bool open)
        {
            if (open)
            {
                if (collapsedBadge != null) collapsedBadge.gameObject.SetActive(false);
                if (expandedPanel != null) expandedPanel.gameObject.SetActive(true);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                FreezeCameraInputs(true);
                UpdateDisplay();

                float duration = 0.18f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);

                    if (panelCanvasGroup != null)
                    {
                        panelCanvasGroup.alpha = smoothT;
                    }
                    if (expandedPanel != null)
                    {
                        float scale = Mathf.Lerp(0.92f, 1f, smoothT);
                        expandedPanel.localScale = new Vector3(scale, scale, 1f);
                    }
                    yield return null;
                }

                if (panelCanvasGroup != null)
                {
                    panelCanvasGroup.alpha = 1f;
                    panelCanvasGroup.interactable = true;
                    panelCanvasGroup.blocksRaycasts = true;
                }
                if (expandedPanel != null) expandedPanel.localScale = Vector3.one;
            }
            else
            {
                float duration = 0.12f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);

                    if (panelCanvasGroup != null)
                    {
                        panelCanvasGroup.alpha = 1f - smoothT;
                    }
                    yield return null;
                }

                if (panelCanvasGroup != null)
                {
                    panelCanvasGroup.alpha = 0f;
                    panelCanvasGroup.interactable = false;
                    panelCanvasGroup.blocksRaycasts = false;
                }

                if (expandedPanel != null) expandedPanel.gameObject.SetActive(false);
                if (collapsedBadge != null) collapsedBadge.gameObject.SetActive(true);

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                FreezeCameraInputs(false);
                SavePreferences();
            }

            transitionRoutine = null;
        }

        private void FreezeCameraInputs(bool freeze)
        {
            var controllers = FindObjectsByType<CinemachineInputAxisController>();
            foreach (var ctrl in controllers)
            {
                if (ctrl != null) ctrl.enabled = !freeze;
            }
        }

        public void SetSensitivity(float newSens)
        {
            sensitivity = Mathf.Clamp((float)Math.Round(newSens, 2), 0.1f, 3.0f);

            if (sensitivitySlider != null && !Mathf.Approximately(sensitivitySlider.value, sensitivity))
            {
                sensitivitySlider.SetValueWithoutNotify(sensitivity);
            }

            ApplySensitivityToAllControllers();
            UpdateDisplay();
            SavePreferences();
        }

        public void SetInvertY(bool invert)
        {
            invertY = invert;

            if (invertYToggle != null && invertYToggle.isOn != invertY)
            {
                invertYToggle.SetIsOnWithoutNotify(invertY);
            }

            ApplySensitivityToAllControllers();
            UpdateDisplay();
            SavePreferences();
        }

        private void UpdateDisplay()
        {
            string sensStr = $"{sensitivity:0.00}x";

            // Widget minimizado
            if (badgeValueText != null)
                badgeValueText.text = $"SENS: {sensStr}";

            // Readout principal
            if (valueReadoutText != null)
                valueReadoutText.text = sensStr;

            // Categoria de velocidade com estilo dinâmico
            Color catColor;
            string catText;
            if (sensitivity <= 0.65f)
            {
                catColor = new Color(0.2f, 0.8f, 1f, 1f); // Cyan
                catText = "● PRECISÃO / SNIPER";
            }
            else if (sensitivity <= 1.25f)
            {
                catColor = new Color(0.3f, 0.95f, 0.4f, 1f); // Emerald
                catText = "● PADRÃO / TÁTICO";
            }
            else if (sensitivity <= 1.95f)
            {
                catColor = amberHeader; // Amber
                catText = "● ÁGIL / COMBATE";
            }
            else
            {
                catColor = doomRedGlow; // Flame red
                catText = "● RÁPIDO / OVERCLOCK";
            }

            if (categoryBadgeText != null)
            {
                categoryBadgeText.text = catText;
                categoryBadgeText.color = catColor;
            }
            if (categoryBadgeBg != null)
            {
                categoryBadgeBg.color = new Color(catColor.r, catColor.g, catColor.b, 0.15f);
            }

            // Atualiza barra de LED segmentada (10 segmentos)
            float norm = Mathf.Clamp01((sensitivity - 0.1f) / (3.0f - 0.1f));
            int litCount = Mathf.RoundToInt(norm * 10f);
            for (int i = 0; i < ledSegments.Length; i++)
            {
                if (ledSegments[i] != null)
                {
                    bool isLit = i < litCount;
                    ledSegments[i].color = isLit ? catColor : new Color(0.12f, 0.14f, 0.18f, 0.8f);
                }
            }

            // Destaque nos botões de preset
            for (int i = 0; i < presetValues.Length; i++)
            {
                bool isMatching = Mathf.Abs(sensitivity - presetValues[i]) <= 0.02f;
                if (presetButtonBgs[i] != null)
                {
                    presetButtonBgs[i].color = isMatching ? new Color(doomRed.r, doomRed.g, doomRed.b, 0.45f) : buttonBgNormal;
                }
                if (presetButtonTexts[i] != null)
                {
                    presetButtonTexts[i].color = isMatching ? Color.white : textMuted;
                }
            }

            // Atualiza visual do switch Invert Y
            if (invertYSwitchBg != null)
            {
                invertYSwitchBg.color = invertY ? new Color(0.2f, 0.75f, 0.35f, 0.9f) : new Color(0.18f, 0.20f, 0.26f, 0.9f);
            }
            if (invertYSwitchKnob != null)
            {
                invertYSwitchKnob.anchoredPosition = new Vector2(invertY ? 12f : -12f, 0f);
            }
            if (invertYStatusText != null)
            {
                invertYStatusText.text = invertY ? "[ ATIVO ]" : "[ DESATIVADO ]";
                invertYStatusText.color = invertY ? new Color(0.3f, 0.95f, 0.4f, 1f) : textMuted;
            }
        }

        private void EnsureControllersRegistered()
        {
            var controllers = FindObjectsByType<CinemachineInputAxisController>();
            foreach (var ctrl in controllers)
            {
                if (ctrl != null && !registeredBaseGains.ContainsKey(ctrl))
                {
                    RegisterAndApply(ctrl);
                }
            }
        }

        private void ApplySensitivityToAllControllers()
        {
            var controllers = FindObjectsByType<CinemachineInputAxisController>();
            foreach (var ctrl in controllers)
            {
                if (ctrl != null)
                {
                    RegisterAndApply(ctrl);
                }
            }
        }

        private void RegisterAndApply(CinemachineInputAxisController ctrl)
        {
            if (ctrl == null || ctrl.Controllers == null) return;

            if (!registeredBaseGains.TryGetValue(ctrl, out var baseGains))
            {
                baseGains = new List<float>();
                for (int i = 0; i < ctrl.Controllers.Count; i++)
                {
                    var c = ctrl.Controllers[i];
                    float g = (c != null && c.Input != null) ? c.Input.Gain : 1f;
                    if (Mathf.Approximately(g, 0f)) g = 1f;
                    baseGains.Add(g);
                }
                registeredBaseGains[ctrl] = baseGains;
            }

            for (int i = 0; i < ctrl.Controllers.Count; i++)
            {
                var c = ctrl.Controllers[i];
                if (c == null || c.Input == null) continue;

                float baseGain = (i < baseGains.Count) ? baseGains[i] : c.Input.Gain;
                bool isY = !string.IsNullOrEmpty(c.Name) &&
                           (c.Name.IndexOf("Y", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            c.Name.IndexOf("Tilt", StringComparison.OrdinalIgnoreCase) >= 0);

                float finalMultiplier = sensitivity;
                if (isY && invertY)
                {
                    finalMultiplier = -finalMultiplier;
                }

                c.Input.Gain = baseGain * finalMultiplier;
                c.Input.LegacyGain = finalMultiplier;
            }
        }

        #region Procedural Sprites & UI Construction
        private static void InitSprites()
        {
            if (roundedBoxSprite == null)
                roundedBoxSprite = GenerateSlicedBoxSprite(32, 8);

            if (roundedPillSprite == null)
                roundedPillSprite = GenerateSlicedBoxSprite(32, 16);

            if (circleSprite == null)
                circleSprite = GenerateCircleSprite(24);
        }

        private static Sprite GenerateSlicedBoxSprite(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = (x < radius) ? radius : (x >= size - radius) ? size - radius - 1 : x;
                    int cy = (y < radius) ? radius : (y >= size - radius) ? size - radius - 1 : y;
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private static Sprite GenerateCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            float radius = (size - 1) * 0.5f;
            Vector2 center = new Vector2(radius, radius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void ResolveFont()
        {
            if (customFont != null) return;

            var loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            foreach (var f in loadedFonts)
            {
                if (f != null && f.name.Contains("Pixelta", StringComparison.OrdinalIgnoreCase))
                {
                    customFont = f;
                    return;
                }
            }

            if (TMP_Settings.defaultFontAsset != null)
            {
                customFont = TMP_Settings.defaultFontAsset;
            }
        }

        private void BuildInterface()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;

            canvasScaler = gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;

            graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            BuildCollapsedBadge();
            BuildExpandedPanel();
        }

        // Widget HUD minimalista e moderno no topo esquerdo
        private void BuildCollapsedBadge()
        {
            var badgeObj = CreateUIElement("CollapsedBadge", transform);
            collapsedBadge = badgeObj.GetComponent<RectTransform>();
            collapsedBadge.anchorMin = new Vector2(0f, 1f);
            collapsedBadge.anchorMax = new Vector2(0f, 1f);
            collapsedBadge.pivot = new Vector2(0f, 1f);
            collapsedBadge.anchoredPosition = new Vector2(28, -26);
            collapsedBadge.sizeDelta = new Vector2(236, 42);

            var badgeBg = badgeObj.AddComponent<Image>();
            badgeBg.sprite = roundedPillSprite;
            badgeBg.type = Image.Type.Sliced;
            badgeBg.color = panelBgColor;
            badgeBg.raycastTarget = true;

            var badgeOutline = badgeObj.AddComponent<Outline>();
            badgeOutline.effectColor = borderRed;
            badgeOutline.effectDistance = new Vector2(1, -1);

            var btn = badgeObj.AddComponent<Button>();
            btn.targetGraphic = badgeBg;
            var c = btn.colors;
            c.normalColor = panelBgColor;
            c.highlightedColor = new Color(0.16f, 0.18f, 0.24f, 1f);
            c.pressedColor = new Color(0.24f, 0.10f, 0.10f, 1f);
            btn.colors = c;
            btn.onClick.AddListener(ToggleMenu);

            // LED Dot pulsante
            var ledObj = CreateUIElement("LedDot", badgeObj.transform);
            var lRect = ledObj.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0f, 0.5f);
            lRect.anchorMax = new Vector2(0f, 0.5f);
            lRect.pivot = new Vector2(0.5f, 0.5f);
            lRect.anchoredPosition = new Vector2(20, 0);
            lRect.sizeDelta = new Vector2(10, 10);
            badgeLedDot = ledObj.AddComponent<Image>();
            badgeLedDot.sprite = circleSprite;
            badgeLedDot.color = doomRed;
            badgeLedDot.raycastTarget = false;

            // Texto do valor
            badgeValueText = CreateText("Label", badgeObj.transform, 15, Color.white, TextAlignmentOptions.Left);
            var vRect = badgeValueText.rectTransform;
            vRect.anchorMin = new Vector2(0f, 0f);
            vRect.anchorMax = new Vector2(1f, 1f);
            vRect.anchoredPosition = new Vector2(36, 0);
            vRect.sizeDelta = new Vector2(-100, 0);
            badgeValueText.text = $"SENS: {sensitivity:0.00}x";
            badgeValueText.fontStyle = FontStyles.Bold;

            // Tag [ESC]
            var tagObj = CreateUIElement("EscTag", badgeObj.transform);
            var tRect = tagObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(1f, 0.5f);
            tRect.anchorMax = new Vector2(1f, 0.5f);
            tRect.pivot = new Vector2(1f, 0.5f);
            tRect.anchoredPosition = new Vector2(-8, 0);
            tRect.sizeDelta = new Vector2(56, 26);

            var tagBg = tagObj.AddComponent<Image>();
            tagBg.sprite = roundedPillSprite;
            tagBg.type = Image.Type.Sliced;
            tagBg.color = new Color(doomRed.r, doomRed.g, doomRed.b, 0.25f);
            tagBg.raycastTarget = false;

            var tagText = CreateText("TagText", tagObj.transform, 12, amberHeader, TextAlignmentOptions.Center);
            var ttRect = tagText.rectTransform;
            ttRect.anchorMin = Vector2.zero;
            ttRect.anchorMax = Vector2.one;
            ttRect.sizeDelta = Vector2.zero;
            tagText.text = "ESC";
            tagText.fontStyle = FontStyles.Bold;
        }

        // Painel Principal Expandido
        private void BuildExpandedPanel()
        {
            var panelObj = CreateUIElement("ExpandedPanel", transform);
            expandedPanel = panelObj.GetComponent<RectTransform>();
            expandedPanel.anchorMin = new Vector2(0f, 1f);
            expandedPanel.anchorMax = new Vector2(0f, 1f);
            expandedPanel.pivot = new Vector2(0f, 1f);
            expandedPanel.anchoredPosition = new Vector2(28, -26);
            expandedPanel.sizeDelta = new Vector2(430, 460);

            panelCanvasGroup = panelObj.AddComponent<CanvasGroup>();

            var panelBg = panelObj.AddComponent<Image>();
            panelBg.sprite = roundedBoxSprite;
            panelBg.type = Image.Type.Sliced;
            panelBg.color = panelBgColor;
            panelBg.raycastTarget = true;

            var panelOutline = panelObj.AddComponent<Outline>();
            panelOutline.effectColor = borderRed;
            panelOutline.effectDistance = new Vector2(2, -2);

            // Linha de acento neon no topo do painel
            var accentBarObj = CreateUIElement("AccentNeonBar", panelObj.transform);
            var aRect = accentBarObj.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0f, 1f);
            aRect.anchorMax = new Vector2(1f, 1f);
            aRect.pivot = new Vector2(0.5f, 1f);
            aRect.anchoredPosition = new Vector2(0, -2);
            aRect.sizeDelta = new Vector2(-16, 3);
            var aImg = accentBarObj.AddComponent<Image>();
            aImg.sprite = roundedPillSprite;
            aImg.type = Image.Type.Sliced;
            aImg.color = doomRed;
            aImg.raycastTarget = false;

            // 1. Barra de Título Superior
            BuildHeader(panelObj.transform);

            // 2. Card Central com Mostrador Digital e Barra de LED
            BuildReadoutGaugeCard(panelObj.transform, -56);

            // 3. Slider de Ajuste Contínuo com botões [-] e [+]
            BuildSliderControl(panelObj.transform, -188);

            // 4. Presets Rápidos
            BuildPresetsRow(panelObj.transform, -252);

            // 5. Inverter Eixo Y (Look Y)
            BuildInvertYToggle(panelObj.transform, -332);

            // 6. Rodapé com Botões de Ação
            BuildFooterActions(panelObj.transform);
        }

        private void BuildHeader(Transform parent)
        {
            var headerObj = CreateUIElement("HeaderBar", parent);
            var hRect = headerObj.GetComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0f, 1f);
            hRect.anchorMax = new Vector2(1f, 1f);
            hRect.pivot = new Vector2(0.5f, 1f);
            hRect.anchoredPosition = new Vector2(0, -6);
            hRect.sizeDelta = new Vector2(0, 44);

            var titleText = CreateText("Title", headerObj.transform, 16, amberHeader, TextAlignmentOptions.Left);
            var tRect = titleText.rectTransform;
            tRect.anchorMin = new Vector2(0f, 0.5f);
            tRect.anchorMax = new Vector2(1f, 0.5f);
            tRect.anchoredPosition = new Vector2(22, 5);
            tRect.sizeDelta = new Vector2(-70, 22);
            titleText.text = "/// CALIBRAÇÃO DE SENSIBILIDADE";
            titleText.fontStyle = FontStyles.Bold;

            var subText = CreateText("SubTitle", headerObj.transform, 11, textMuted, TextAlignmentOptions.Left);
            var stRect = subText.rectTransform;
            stRect.anchorMin = new Vector2(0f, 0.5f);
            stRect.anchorMax = new Vector2(1f, 0.5f);
            stRect.anchoredPosition = new Vector2(22, -12);
            stRect.sizeDelta = new Vector2(-70, 16);
            subText.text = "UNIDADE: MULTIPLICADOR DE EIXO // FPS COMBAT";

            // Botão Fechar [X] estilizado
            CreateStyledButton("CloseBtn", headerObj.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-16, 0), new Vector2(32, 32), "✕", 15, new Color(0.25f, 0.12f, 0.12f, 1f), Color.white,
                () => SetMenuOpen(false));
        }

        private void BuildReadoutGaugeCard(Transform parent, float posY)
        {
            var cardObj = CreateUIElement("GaugeCard", parent);
            var cRect = cardObj.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.anchoredPosition = new Vector2(0, posY);
            cRect.sizeDelta = new Vector2(-36, 118);

            var cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = roundedBoxSprite;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = cardInnerBg;
            cardBg.raycastTarget = false;

            var outline = cardObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.20f, 0.24f, 0.32f, 0.5f);
            outline.effectDistance = new Vector2(1, -1);

            // Número Digital Grande
            valueReadoutText = CreateText("ValueReadout", cardObj.transform, 44, Color.white, TextAlignmentOptions.Center);
            var vrRect = valueReadoutText.rectTransform;
            vrRect.anchorMin = new Vector2(0.5f, 1f);
            vrRect.anchorMax = new Vector2(0.5f, 1f);
            vrRect.pivot = new Vector2(0.5f, 1f);
            vrRect.anchoredPosition = new Vector2(0, -10);
            vrRect.sizeDelta = new Vector2(240, 48);
            valueReadoutText.text = $"{sensitivity:0.00}x";
            valueReadoutText.fontStyle = FontStyles.Bold;

            // Badge de Categoria de Velocidade
            var badgeObj = CreateUIElement("CategoryBadge", cardObj.transform);
            var bRect = badgeObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 1f);
            bRect.anchorMax = new Vector2(0.5f, 1f);
            bRect.pivot = new Vector2(0.5f, 1f);
            bRect.anchoredPosition = new Vector2(0, -62);
            bRect.sizeDelta = new Vector2(190, 22);

            categoryBadgeBg = badgeObj.AddComponent<Image>();
            categoryBadgeBg.sprite = roundedPillSprite;
            categoryBadgeBg.type = Image.Type.Sliced;
            categoryBadgeBg.color = new Color(0.2f, 0.8f, 1f, 0.15f);
            categoryBadgeBg.raycastTarget = false;

            categoryBadgeText = CreateText("BadgeText", badgeObj.transform, 12, Color.white, TextAlignmentOptions.Center);
            var btRect = categoryBadgeText.rectTransform;
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            categoryBadgeText.text = "● PADRÃO / TÁTICO";
            categoryBadgeText.fontStyle = FontStyles.Bold;

            // Barra de LED Segmentada (10 segmentos)
            var ledRow = CreateUIElement("LedRow", cardObj.transform);
            var lrRect = ledRow.GetComponent<RectTransform>();
            lrRect.anchorMin = new Vector2(0.5f, 0f);
            lrRect.anchorMax = new Vector2(0.5f, 0f);
            lrRect.pivot = new Vector2(0.5f, 0f);
            lrRect.anchoredPosition = new Vector2(0, 10);
            lrRect.sizeDelta = new Vector2(340, 10);

            float segW = 30f;
            float segH = 8f;
            float segGap = 4f;
            float totalW = 10 * segW + 9 * segGap;
            float startX = -totalW * 0.5f + segW * 0.5f;

            for (int i = 0; i < 10; i++)
            {
                var seg = CreateUIElement($"Seg_{i}", ledRow.transform);
                var srt = seg.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 0.5f);
                srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(startX + i * (segW + segGap), 0);
                srt.sizeDelta = new Vector2(segW, segH);

                var img = seg.AddComponent<Image>();
                img.sprite = roundedPillSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.12f, 0.14f, 0.18f, 0.8f);
                img.raycastTarget = false;
                ledSegments[i] = img;
            }
        }

        private void BuildSliderControl(Transform parent, float posY)
        {
            // Botão [-]
            CreateStyledButton("StepDown", parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(38, posY), new Vector2(36, 36), "−", 22, buttonBgNormal, Color.white,
                () => SetSensitivity(sensitivity - 0.05f));

            // Slider Container (área arrastável ampla)
            var sliderObj = CreateUIElement("SensitivitySlider", parent);
            var sRect = sliderObj.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0f, 1f);
            sRect.anchorMax = new Vector2(1f, 1f);
            sRect.pivot = new Vector2(0.5f, 0.5f);
            sRect.anchoredPosition = new Vector2(0, posY);
            sRect.sizeDelta = new Vector2(-156, 36);

            // Trilha de Fundo
            var trackBg = sliderObj.AddComponent<Image>();
            trackBg.sprite = roundedPillSprite;
            trackBg.type = Image.Type.Sliced;
            trackBg.color = new Color(0.10f, 0.12f, 0.16f, 1f);
            trackBg.raycastTarget = true;

            var trackOutline = sliderObj.AddComponent<Outline>();
            trackOutline.effectColor = new Color(0.24f, 0.28f, 0.36f, 0.7f);
            trackOutline.effectDistance = new Vector2(1, -1);

            sensitivitySlider = sliderObj.AddComponent<Slider>();
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = 3.0f;
            sensitivitySlider.wholeNumbers = false;
            sensitivitySlider.direction = Slider.Direction.LeftToRight;

            // Fill Area
            var fillArea = CreateUIElement("Fill Area", sliderObj.transform);
            var faRect = fillArea.GetComponent<RectTransform>();
            faRect.anchorMin = new Vector2(0f, 0.15f);
            faRect.anchorMax = new Vector2(1f, 0.85f);
            faRect.sizeDelta = new Vector2(-12, 0);

            var fillObj = CreateUIElement("Fill", fillArea.transform);
            sliderFillRect = fillObj.GetComponent<RectTransform>();
            sliderFillRect.anchorMin = Vector2.zero;
            sliderFillRect.anchorMax = Vector2.one;
            sliderFillRect.sizeDelta = Vector2.zero;

            sliderFillImg = fillObj.AddComponent<Image>();
            sliderFillImg.sprite = roundedPillSprite;
            sliderFillImg.type = Image.Type.Sliced;
            sliderFillImg.color = doomRed;
            sliderFillImg.raycastTarget = false;
            sensitivitySlider.fillRect = sliderFillRect;

            // Handle Slide Area
            var handleArea = CreateUIElement("Handle Area", sliderObj.transform);
            var haRect = handleArea.GetComponent<RectTransform>();
            haRect.anchorMin = Vector2.zero;
            haRect.anchorMax = Vector2.one;
            haRect.sizeDelta = new Vector2(-28, 0);

            var handleObj = CreateUIElement("Handle", handleArea.transform);
            var hRect = handleObj.GetComponent<RectTransform>();
            hRect.sizeDelta = new Vector2(26, 36);

            sliderHandleImg = handleObj.AddComponent<Image>();
            sliderHandleImg.sprite = roundedPillSprite;
            sliderHandleImg.type = Image.Type.Sliced;
            sliderHandleImg.color = Color.white;
            sliderHandleImg.raycastTarget = true;

            // Núcleo de cor central no handle
            var handleCore = CreateUIElement("HandleCore", handleObj.transform);
            var hcRect = handleCore.GetComponent<RectTransform>();
            hcRect.anchorMin = new Vector2(0.5f, 0.5f);
            hcRect.anchorMax = new Vector2(0.5f, 0.5f);
            hcRect.sizeDelta = new Vector2(6, 18);
            var hcImg = handleCore.AddComponent<Image>();
            hcImg.sprite = roundedPillSprite;
            hcImg.type = Image.Type.Sliced;
            hcImg.color = doomRed;
            hcImg.raycastTarget = false;

            sensitivitySlider.handleRect = hRect;
            sensitivitySlider.targetGraphic = sliderHandleImg;

            // Proxy de clique e arrasto suave em qualquer ponto da barra
            var proxy = sliderObj.AddComponent<SliderPointerProxy>();
            proxy.slider = sensitivitySlider;
            proxy.trackRect = sRect;
            proxy.onValueChanged = (val) => SetSensitivity(val);

            sensitivitySlider.onValueChanged.AddListener((val) => SetSensitivity(val));
            sensitivitySlider.SetValueWithoutNotify(sensitivity);

            // Botão [+]
            CreateStyledButton("StepUp", parent, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-38, posY), new Vector2(36, 36), "+", 22, buttonBgNormal, Color.white,
                () => SetSensitivity(sensitivity + 0.05f));

            // Marcadores de referência numérica embaixo do slider
            var ticksObj = CreateUIElement("TicksRow", parent);
            var trRect = ticksObj.GetComponent<RectTransform>();
            trRect.anchorMin = new Vector2(0f, 1f);
            trRect.anchorMax = new Vector2(1f, 1f);
            trRect.anchoredPosition = new Vector2(0, posY - 28);
            trRect.sizeDelta = new Vector2(-156, 16);

            CreateTickLabel(ticksObj.transform, 0.0f, "0.1x");
            CreateTickLabel(ticksObj.transform, 0.31f, "1.0x");
            CreateTickLabel(ticksObj.transform, 0.65f, "2.0x");
            CreateTickLabel(ticksObj.transform, 1.0f, "3.0x");
        }

        private void CreateTickLabel(Transform parent, float normX, string text)
        {
            var tmp = CreateText($"Tick_{text}", parent, 11, textMuted, TextAlignmentOptions.Center);
            var r = tmp.rectTransform;
            r.anchorMin = new Vector2(normX, 0.5f);
            r.anchorMax = new Vector2(normX, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = new Vector2(40, 16);
            tmp.text = text;
        }

        private void BuildPresetsRow(Transform parent, float posY)
        {
            var label = CreateText("PresetsTitle", parent, 12, textMuted, TextAlignmentOptions.Left);
            var lRect = label.rectTransform;
            lRect.anchorMin = new Vector2(0f, 1f);
            lRect.anchorMax = new Vector2(1f, 1f);
            lRect.anchoredPosition = new Vector2(20, posY);
            lRect.sizeDelta = new Vector2(-40, 16);
            label.text = "ATALHOS RÁPIDOS:";
            label.fontStyle = FontStyles.Bold;

            float startX = 20f;
            float btnW = 90f;
            float spacing = 10f;
            string[] presetLabels = { "0.5x", "1.0x", "1.5x", "2.0x" };
            string[] subLabels = { "LENTO", "PADRÃO", "ÁGIL", "INSANO" };

            for (int i = 0; i < presetValues.Length; i++)
            {
                float pVal = presetValues[i];
                float x = startX + i * (btnW + spacing);

                var btnObj = CreateUIElement($"PresetBtn_{pVal}", parent);
                var rt = btnObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(x, posY - 22);
                rt.sizeDelta = new Vector2(btnW, 36);

                var img = btnObj.AddComponent<Image>();
                img.sprite = roundedBoxSprite;
                img.type = Image.Type.Sliced;
                img.color = buttonBgNormal;
                img.raycastTarget = true;
                presetButtonBgs[i] = img;

                var outline = btnObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.24f, 0.28f, 0.36f, 0.6f);
                outline.effectDistance = new Vector2(1, -1);

                var btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = img;
                var cols = btn.colors;
                cols.normalColor = buttonBgNormal;
                cols.highlightedColor = buttonBgHover;
                cols.pressedColor = doomRed;
                btn.colors = cols;
                btn.onClick.AddListener(() => SetSensitivity(pVal));
                presetButtons[i] = btn;

                var text = CreateText("ValText", btnObj.transform, 14, Color.white, TextAlignmentOptions.Center);
                var tRect = text.rectTransform;
                tRect.anchorMin = new Vector2(0f, 0.5f);
                tRect.anchorMax = new Vector2(1f, 0.5f);
                tRect.anchoredPosition = new Vector2(0, 4);
                tRect.sizeDelta = new Vector2(0, 18);
                text.text = presetLabels[i];
                text.fontStyle = FontStyles.Bold;
                presetButtonTexts[i] = text;

                var sub = CreateText("SubText", btnObj.transform, 9, textMuted, TextAlignmentOptions.Center);
                var sRect = sub.rectTransform;
                sRect.anchorMin = new Vector2(0f, 0.5f);
                sRect.anchorMax = new Vector2(1f, 0.5f);
                sRect.anchoredPosition = new Vector2(0, -9);
                sRect.sizeDelta = new Vector2(0, 14);
                sub.text = subLabels[i];
            }
        }

        private void BuildInvertYToggle(Transform parent, float posY)
        {
            var rowObj = CreateUIElement("InvertYRow", parent);
            var rRect = rowObj.GetComponent<RectTransform>();
            rRect.anchorMin = new Vector2(0f, 1f);
            rRect.anchorMax = new Vector2(1f, 1f);
            rRect.pivot = new Vector2(0.5f, 1f);
            rRect.anchoredPosition = new Vector2(0, posY);
            rRect.sizeDelta = new Vector2(-36, 46);

            var rowBg = rowObj.AddComponent<Image>();
            rowBg.sprite = roundedBoxSprite;
            rowBg.type = Image.Type.Sliced;
            rowBg.color = cardInnerBg;
            rowBg.raycastTarget = true;

            var outline = rowObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.20f, 0.24f, 0.32f, 0.5f);
            outline.effectDistance = new Vector2(1, -1);

            invertYToggle = rowObj.AddComponent<Toggle>();
            invertYToggle.isOn = invertY;
            invertYToggle.targetGraphic = rowBg;

            // Ícone e Título da linha
            var title = CreateText("Title", rowObj.transform, 14, Color.white, TextAlignmentOptions.Left);
            var tRect = title.rectTransform;
            tRect.anchorMin = new Vector2(0f, 0.5f);
            tRect.anchorMax = new Vector2(1f, 0.5f);
            tRect.anchoredPosition = new Vector2(16, 5);
            tRect.sizeDelta = new Vector2(-160, 20);
            title.text = "Inverter Eixo Vertical";
            title.fontStyle = FontStyles.Bold;

            var desc = CreateText("Desc", rowObj.transform, 11, textMuted, TextAlignmentOptions.Left);
            var dRect = desc.rectTransform;
            dRect.anchorMin = new Vector2(0f, 0.5f);
            dRect.anchorMax = new Vector2(1f, 0.5f);
            dRect.anchoredPosition = new Vector2(16, -11);
            dRect.sizeDelta = new Vector2(-160, 16);
            desc.text = "Inverte a direção para cima/baixo (Look Y)";

            // Switch Pill moderno no lado direito
            var switchPill = CreateUIElement("SwitchPill", rowObj.transform);
            var spRect = switchPill.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(1f, 0.5f);
            spRect.anchorMax = new Vector2(1f, 0.5f);
            spRect.pivot = new Vector2(1f, 0.5f);
            spRect.anchoredPosition = new Vector2(-14, 0);
            spRect.sizeDelta = new Vector2(50, 26);

            invertYSwitchBg = switchPill.AddComponent<Image>();
            invertYSwitchBg.sprite = roundedPillSprite;
            invertYSwitchBg.type = Image.Type.Sliced;
            invertYSwitchBg.color = invertY ? new Color(0.2f, 0.75f, 0.35f, 0.9f) : new Color(0.18f, 0.20f, 0.26f, 0.9f);
            invertYSwitchBg.raycastTarget = false;

            var knobObj = CreateUIElement("Knob", switchPill.transform);
            invertYSwitchKnob = knobObj.GetComponent<RectTransform>();
            invertYSwitchKnob.anchorMin = new Vector2(0.5f, 0.5f);
            invertYSwitchKnob.anchorMax = new Vector2(0.5f, 0.5f);
            invertYSwitchKnob.sizeDelta = new Vector2(20, 20);
            invertYSwitchKnob.anchoredPosition = new Vector2(invertY ? 12f : -12f, 0f);

            var knobImg = knobObj.AddComponent<Image>();
            knobImg.sprite = circleSprite;
            knobImg.color = Color.white;
            knobImg.raycastTarget = false;

            // Status Text
            invertYStatusText = CreateText("StatusText", rowObj.transform, 11, textMuted, TextAlignmentOptions.Right);
            var stRect = invertYStatusText.rectTransform;
            stRect.anchorMin = new Vector2(1f, 0.5f);
            stRect.anchorMax = new Vector2(1f, 0.5f);
            stRect.anchoredPosition = new Vector2(-74, 0);
            stRect.sizeDelta = new Vector2(80, 20);
            invertYStatusText.text = invertY ? "[ ATIVO ]" : "[ DESATIVADO ]";

            invertYToggle.onValueChanged.AddListener((val) => SetInvertY(val));
        }

        private void BuildFooterActions(Transform parent)
        {
            // Botão Redefinir
            CreateStyledButton("ResetBtn", parent, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(105, 40), new Vector2(174, 38), "↺ REDEFINIR (1.0x)", 13, new Color(0.15f, 0.17f, 0.23f, 1f), Color.white,
                () => { SetSensitivity(1.0f); SetInvertY(false); });

            // Botão Salvar e Fechar com destaque Doom Red
            CreateStyledButton("CloseConfirmBtn", parent, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-105, 40), new Vector2(174, 38), "✓ CONCLUIR [ESC]", 13, doomRed, Color.white,
                () => SetMenuOpen(false));

            // Dica de rodapé em fonte monospace sutil
            var hint = CreateText("FooterHint", parent, 11, textMuted, TextAlignmentOptions.Center);
            var hRect = hint.rectTransform;
            hRect.anchorMin = new Vector2(0.5f, 0f);
            hRect.anchorMax = new Vector2(0.5f, 0f);
            hRect.pivot = new Vector2(0.5f, 0f);
            hRect.anchoredPosition = new Vector2(0, 10);
            hRect.sizeDelta = new Vector2(380, 18);
            hint.text = "// PRESSIONE ESC A QUALQUER MOMENTO PARA RETORNAR AO JOGO";
        }

        private GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private TMP_Text CreateText(string name, Transform parent, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var go = CreateUIElement(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (customFont != null) tmp.font = customFont;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return tmp;
        }

        private Button CreateStyledButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pos, Vector2 size, string text, float fontSize, Color bgColor, Color textColor, UnityAction onClick)
        {
            var btnObj = CreateUIElement(name, parent);
            var rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = btnObj.AddComponent<Image>();
            img.sprite = roundedBoxSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;
            img.raycastTarget = true;

            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.25f, 0.30f, 0.40f, 0.5f);
            outline.effectDistance = new Vector2(1, -1);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            colors.pressedColor = doomRed;
            colors.selectedColor = bgColor;
            btn.colors = colors;

            if (onClick != null) btn.onClick.AddListener(onClick);

            var label = CreateText("Text", btnObj.transform, fontSize, textColor, TextAlignmentOptions.Center);
            var lRect = label.rectTransform;
            lRect.anchorMin = Vector2.zero;
            lRect.anchorMax = Vector2.one;
            lRect.sizeDelta = Vector2.zero;
            label.text = text;
            label.fontStyle = FontStyles.Bold;

            return btn;
        }
        #endregion
    }

    /// <summary>
    /// Proxy auxiliar para permitir clique e arrasto contínuo em qualquer região da barra de sensibilidade.
    /// </summary>
    public class SliderPointerProxy : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public Slider slider;
        public RectTransform trackRect;
        public Action<float> onValueChanged;

        public void OnPointerDown(PointerEventData eventData) => ProcessPointer(eventData);
        public void OnDrag(PointerEventData eventData) => ProcessPointer(eventData);

        private void ProcessPointer(PointerEventData eventData)
        {
            if (slider == null || trackRect == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                float normalized = Mathf.Clamp01((localPoint.x - trackRect.rect.xMin) / Mathf.Max(1f, trackRect.rect.width));
                float calculatedVal = Mathf.Lerp(slider.minValue, slider.maxValue, normalized);
                slider.value = calculatedVal;
                onValueChanged?.Invoke(calculatedVal);
            }
        }
    }
}
