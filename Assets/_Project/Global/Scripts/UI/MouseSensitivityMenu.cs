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
    /// Mouse controls styled to match the industrial horror loading screen.
    /// ESC and the HUD badge open the panel; preferences and camera bindings remain shared.
    /// </summary>
    public class MouseSensitivityMenu : MonoBehaviour
    {
        public static MouseSensitivityMenu Instance { get; private set; }

        /// <summary>Indica se o menu está atualmente aberto.</summary>
        public static bool IsOpen => GameMenus.BlocksGameplay || (Instance != null && (Instance.isMenuOpen || Instance.pauseHeld));
        public bool InvertY => invertY;

        /// <summary>Sensibilidade atual do mouse (multiplicador de 0.1x a 3.0x).</summary>
        public static float CurrentSensitivity => Instance != null ? Instance.sensitivity : 1f;

        private const string PrefKeySensitivity = "UniDoom_MouseSensitivity";
        private const string PrefKeyInvertY = "UniDoom_MouseInvertY";

        [Header("Configurações")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Escape;
        [Range(0.1f, 3.0f)]
        [SerializeField] private float sensitivity = 1.0f;
        [SerializeField] private bool invertY = false;

        [Header("Paleta industrial / tela de carregamento")]
        [SerializeField] private Color panelBgColor = new Color(.045f, .043f, .035f, .98f);
        [SerializeField] private Color cardInnerBg = new Color(.025f, .026f, .023f, 1f);
        [SerializeField] private Color doomRed = new Color(.65f, .13f, .075f, 1f);
        [SerializeField] private Color doomRedGlow = new Color(.76f, .22f, .12f, 1f);
        [SerializeField] private Color borderRed = new Color(.25f, .20f, .13f, 1f);
        [SerializeField] private Color amberHeader = new Color(.65f, .47f, .24f, 1f);
        [SerializeField] private Color buttonBgNormal = new Color(.10f, .09f, .075f, 1f);
        [SerializeField] private Color buttonBgHover = new Color(.18f, .15f, .10f, 1f);
        [SerializeField] private Color textMuted = new Color(.64f, .60f, .49f, 1f);
        private static readonly Color textPrimary = new Color(.83f, .80f, .69f, 1f);

        private Color HoverTint => new Color(1f + buttonBgHover.r, 1f + buttonBgHover.g, 1f + buttonBgHover.b, 1f);

        private Canvas canvas;
        private CanvasScaler canvasScaler;
        private GraphicRaycaster graphicRaycaster;

        private RectTransform collapsedBadge;
        private RectTransform expandedPanel;
        private RectTransform pauseLayer;
        private TMP_Text pausedLocation;
        private bool pauseHeld;
        private float previousTimeScale;
        private bool previousAudioPause, previousCursorVisible;
        private CursorLockMode previousCursorLock;
        private readonly Dictionary<CinemachineInputAxisController, bool> pausedCameraStates = new();
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

        private TMP_FontAsset customFont;
        private TMP_FontAsset bodyFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            registeredBaseGains.Clear();
        }

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

            LoadPreferences();
            ResolveFont();
            EnsureEventSystem();
            BuildInterface();
            ApplySensitivityToAllControllers();
            SetMenuOpenImmediate(false);
        }

        private void OnDisable()
        {
            if (Instance != this) return;
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            SetMenuOpenImmediate(false);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            ReleasePause();
            Instance = null;
        }

        private void Update()
        {
            if (GameMenus.Instance != null)
            {
                // The approved menus own presentation and pause; keep this shared camera/preferences service.
                if (canvas != null) canvas.enabled = false;
                EnsureControllersRegistered();
                return;
            }
            if (DeathScreen.IsVisible) return;
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
            if (pauseHeld)
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
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2f);
                badgeLedDot.color = Color.Lerp(doomRed * 0.75f, doomRed, pulse);
            }

            EnsureControllersRegistered();
            if (pauseHeld) FreezeCameraInputs(true);
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
            if (GameMenus.Instance != null) { GameMenus.Instance.OpenPause(); return; }
            SetMenuOpen(!isMenuOpen);
        }

        public void SetMenuOpen(bool open)
        {
            if (GameMenus.Instance != null)
            {
                if (open) GameMenus.Instance.OpenOptions(); else GameMenus.Instance.Back();
                return;
            }
            if (open && DeathScreen.IsVisible) return;
            if (isMenuOpen == open || DoorReloadScreen.IsReloading) return;
            isMenuOpen = open;
            if (open) AcquirePause();

            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);

            transitionRoutine = StartCoroutine(TransitionMenuRoutine(open));
        }

        public bool CloseForDeath()
        {
            if (!isMenuOpen && transitionRoutine == null) return false;
            if (transitionRoutine != null) StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            SetMenuOpenImmediate(false);
            return true;
        }

        private void SetMenuOpenImmediate(bool open)
        {
            isMenuOpen = open;
            if (open) AcquirePause();
            if (pauseLayer != null) pauseLayer.gameObject.SetActive(open);

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
            }
            else
            {
                ReleasePause();
                SavePreferences();
            }

            UpdateDisplay();
        }

        private IEnumerator TransitionMenuRoutine(bool open)
        {
            if (open)
            {
                if (collapsedBadge != null) collapsedBadge.gameObject.SetActive(false);
                if (pauseLayer != null) pauseLayer.gameObject.SetActive(true);
                RefreshPauseLocation();
                if (expandedPanel != null) expandedPanel.gameObject.SetActive(true);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                UpdateDisplay();

                float duration = 0.22f;
                float elapsed = 0f;
                float startingAlpha = panelCanvasGroup != null ? panelCanvasGroup.alpha : 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);

                    if (panelCanvasGroup != null)
                    {
                        panelCanvasGroup.alpha = Mathf.Lerp(startingAlpha, 1f, smoothT);
                    }
                    if (expandedPanel != null)
                    {
                        float scale = Mathf.Lerp(1.12f, 1.15f, smoothT);
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
                if (expandedPanel != null) expandedPanel.localScale = Vector3.one * 1.15f;
            }
            else
            {
                float duration = 0.16f;
                float elapsed = 0f;
                float startingAlpha = panelCanvasGroup != null ? panelCanvasGroup.alpha : 1f;
                if (panelCanvasGroup != null) panelCanvasGroup.interactable = false;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);

                    if (panelCanvasGroup != null)
                    {
                        panelCanvasGroup.alpha = Mathf.Lerp(startingAlpha, 0f, smoothT);
                    }
                    yield return null;
                }

                if (panelCanvasGroup != null)
                {
                    panelCanvasGroup.alpha = 0f;
                    panelCanvasGroup.interactable = false;
                    panelCanvasGroup.blocksRaycasts = false;
                }

                if (pauseLayer != null) pauseLayer.gameObject.SetActive(false);
                if (expandedPanel != null) expandedPanel.gameObject.SetActive(false);
                if (collapsedBadge != null) collapsedBadge.gameObject.SetActive(true);

                ReleasePause();
                SavePreferences();
            }

            transitionRoutine = null;
        }

        private void AcquirePause()
        {
            if (pauseHeld) return;
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            pauseHeld = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            FreezeCameraInputs(true);
        }

        private void ReleasePause()
        {
            if (!pauseHeld) return;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            FreezeCameraInputs(false);
            pauseHeld = false;
        }

        private void FreezeCameraInputs(bool freeze)
        {
            if (freeze)
            {
                foreach (var controller in FindObjectsByType<CinemachineInputAxisController>())
                {
                    if (!pausedCameraStates.ContainsKey(controller)) pausedCameraStates[controller] = controller.enabled;
                    controller.enabled = false;
                }
            }
            else
            {
                foreach (var state in pausedCameraStates) if (state.Key != null) state.Key.enabled = state.Value;
                pausedCameraStates.Clear();
            }
        }

        private void RefreshPauseLocation()
        {
            if (pausedLocation == null) return;
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            pausedLocation.text = scene switch
            {
                "Cylinder" => "BLOCO UNIVERSITÁRIO",
                "Library" => "BIBLIOTECA",
                "ClassRoom" => "SALA DE AULA",
                "DirectorRoom" => "DIRETORIA",
                "EntranceGarden" => "CORREDOR DE ENTRADA",
                "Basement" => "SUBSOLO",
                _ => scene.ToUpperInvariant()
            };
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
                catColor = textPrimary;
                catText = "PRECISÃO";
            }
            else if (sensitivity <= 1.25f)
            {
                catColor = textMuted;
                catText = "PADRÃO";
            }
            else if (sensitivity <= 1.95f)
            {
                catColor = amberHeader; // Amber
                catText = "ÁGIL";
            }
            else
            {
                catColor = doomRedGlow; // Flame red
                catText = "MUITO RÁPIDO";
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
                    ledSegments[i].color = isLit ? catColor : buttonBgNormal;
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
                    presetButtonTexts[i].color = isMatching ? textPrimary : textMuted;
                }
            }

            // Atualiza visual do switch Invert Y
            if (invertYSwitchBg != null)
            {
                invertYSwitchBg.color = invertY ? doomRed : buttonBgNormal;
            }
            if (invertYSwitchKnob != null)
            {
                invertYSwitchKnob.anchoredPosition = new Vector2(invertY ? 12f : -12f, 0f);
            }
            if (invertYStatusText != null)
            {
                invertYStatusText.text = invertY ? "[ ATIVO ]" : "[ DESATIVADO ]";
                invertYStatusText.color = invertY ? amberHeader : textMuted;
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

        #region UI Construction
        private void ResolveFont()
        {
            bodyFont = TMP_Settings.defaultFontAsset;
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
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
            graphicRaycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            BuildCollapsedBadge();
            BuildPauseLayer();
            BuildExpandedPanel();
        }

        private void BuildPauseLayer()
        {
            pauseLayer = CreateUIElement("PauseLayer", transform).GetComponent<RectTransform>();
            pauseLayer.anchorMin = Vector2.zero;
            pauseLayer.anchorMax = Vector2.one;
            pauseLayer.sizeDelta = Vector2.zero;
            panelCanvasGroup = pauseLayer.gameObject.AddComponent<CanvasGroup>();

            var darkness = CreateUIElement("Stillness", pauseLayer).GetComponent<RectTransform>();
            darkness.anchorMin = Vector2.zero;
            darkness.anchorMax = Vector2.one;
            darkness.sizeDelta = Vector2.zero;
            var shade = darkness.gameObject.AddComponent<Image>();
            shade.color = new Color(.012f, .014f, .012f, .88f);
            shade.raycastTarget = true;
            var veil = CreateUIElement("Peripheral shadows", pauseLayer).GetComponent<RectTransform>();
            veil.anchorMin = Vector2.zero;
            veil.anchorMax = Vector2.one;
            veil.sizeDelta = Vector2.zero;
            veil.gameObject.AddComponent<HorrorTransitionVeil>().raycastTarget = false;

            var caption = CreateUIElement("Pause caption", pauseLayer).GetComponent<RectTransform>();
            caption.anchorMin = caption.anchorMax = new Vector2(.12f, .55f);
            caption.pivot = new Vector2(0f, 1f);
            caption.sizeDelta = new Vector2(570, 280);

            var eyebrow = PauseLabel("Location", caption, 15, amberHeader, 0, 28);
            pausedLocation = eyebrow;
            var title = PauseLabel("Paused", caption, 78, textPrimary, -40, 110);
            title.text = "PAUSADO";
            title.fontStyle = FontStyles.Bold;
            title.characterSpacing = 6;
            if (customFont != null) title.font = customFont;
            var hint = PauseLabel("Hint", caption, 18, textMuted, -150, 35);
            hint.text = "Ajuste a sensibilidade do seu olhar.";

            CreateStyledButton("ResumeGame", caption, new Vector2(0, 1), new Vector2(0, 1),
                new Vector2(165, -225), new Vector2(330, 52), "CONTINUAR  [ESC]", 18,
                new Color(.16f, .08f, .055f, 1f), textPrimary, () => SetMenuOpen(false));
        }

        private TMP_Text PauseLabel(string name, Transform parent, float size, Color color, float y, float height)
        {
            var label = CreateText(name, parent, size, color, TextAlignmentOptions.Left);
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(570, height);
            return label;
        }

        // Compact control plate at the upper-left corner
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

            badgeBg.type = Image.Type.Simple;
            badgeBg.color = panelBgColor;
            badgeBg.raycastTarget = true;

            var badgeOutline = badgeObj.AddComponent<Outline>();
            badgeOutline.effectColor = borderRed;
            badgeOutline.effectDistance = new Vector2(1, -1);

            var btn = badgeObj.AddComponent<Button>();
            btn.targetGraphic = badgeBg;
            var c = btn.colors;
            c.normalColor = Color.white;
            c.highlightedColor = HoverTint;
            c.pressedColor = new Color(.7f, .65f, .55f);
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

            badgeLedDot.color = doomRed;
            badgeLedDot.raycastTarget = false;

            // Texto do valor
            badgeValueText = CreateText("Label", badgeObj.transform, 15, textPrimary, TextAlignmentOptions.Left);
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

            tagBg.type = Image.Type.Simple;
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
            var panelObj = CreateUIElement("ExpandedPanel", pauseLayer);
            expandedPanel = panelObj.GetComponent<RectTransform>();
            expandedPanel.anchorMin = new Vector2(.69f, .5f);
            expandedPanel.anchorMax = new Vector2(.69f, .5f);
            expandedPanel.pivot = new Vector2(.5f, .5f);
            expandedPanel.anchoredPosition = Vector2.zero;
            expandedPanel.localScale = Vector3.one * 1.15f;
            expandedPanel.sizeDelta = new Vector2(430, 460);


            var panelBg = panelObj.AddComponent<Image>();

            panelBg.type = Image.Type.Simple;
            panelBg.color = panelBgColor;
            panelBg.raycastTarget = true;

            var panelOutline = panelObj.AddComponent<Outline>();
            panelOutline.effectColor = borderRed;
            panelOutline.effectDistance = new Vector2(2, -2);

            var wear = CreateUIElement("SurfaceWear", panelObj.transform);
            var wearRect = wear.GetComponent<RectTransform>();
            wearRect.anchorMin = Vector2.zero;
            wearRect.anchorMax = Vector2.one;
            wearRect.sizeDelta = Vector2.zero;
            wear.AddComponent<HorrorLoadingBackdrop>().raycastTarget = false;

            var rail = CreateUIElement("OxideRail", panelObj.transform);
            var railRect = rail.GetComponent<RectTransform>();
            railRect.anchorMin = Vector2.zero;
            railRect.anchorMax = new Vector2(0f, 1f);
            railRect.anchoredPosition = new Vector2(2f, 0f);
            railRect.sizeDelta = new Vector2(4f, -2f);
            var railImage = rail.AddComponent<Image>();
            railImage.color = doomRed;
            railImage.raycastTarget = false;

            // Worn amber edge, matching the loading screen
            var accentBarObj = CreateUIElement("OxideTopEdge", panelObj.transform);
            var aRect = accentBarObj.GetComponent<RectTransform>();
            aRect.anchorMin = new Vector2(0f, 1f);
            aRect.anchorMax = new Vector2(1f, 1f);
            aRect.pivot = new Vector2(0.5f, 1f);
            aRect.anchoredPosition = new Vector2(0, -2);
            aRect.sizeDelta = new Vector2(-16, 3);
            var aImg = accentBarObj.AddComponent<Image>();

            aImg.type = Image.Type.Simple;
            aImg.color = amberHeader * .6f;
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
            tRect.anchorMax = new Vector2(0f, 0.5f);
            tRect.pivot = new Vector2(0f, .5f);
            tRect.anchoredPosition = new Vector2(22, 5);
            tRect.sizeDelta = new Vector2(350, 26);
            titleText.text = "SENSIBILIDADE";
            if (customFont != null) titleText.font = customFont;
            titleText.fontSize = 24f;
            titleText.color = textPrimary;
            titleText.fontStyle = FontStyles.Bold;

            var subText = CreateText("SubTitle", headerObj.transform, 11, textMuted, TextAlignmentOptions.Left);
            var stRect = subText.rectTransform;
            stRect.anchorMin = new Vector2(0f, 0.5f);
            stRect.anchorMax = new Vector2(0f, 0.5f);
            stRect.pivot = new Vector2(0f, .5f);
            stRect.anchoredPosition = new Vector2(22, -12);
            stRect.sizeDelta = new Vector2(350, 16);
            subText.text = "UNIDOOM  /  AJUSTES DE CONTROLE";

            // Botão Fechar [X] estilizado
            CreateStyledButton("CloseBtn", headerObj.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-24, 0), new Vector2(32, 32), "X", 15, buttonBgNormal, textPrimary,
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

            cardBg.type = Image.Type.Simple;
            cardBg.color = cardInnerBg;
            cardBg.raycastTarget = false;

            var outline = cardObj.AddComponent<Outline>();
            outline.effectColor = borderRed;
            outline.effectDistance = new Vector2(1, -1);

            // Número Digital Grande
            valueReadoutText = CreateText("ValueReadout", cardObj.transform, 44, textPrimary, TextAlignmentOptions.Center);
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

            categoryBadgeBg.type = Image.Type.Simple;
            categoryBadgeBg.color = new Color(amberHeader.r, amberHeader.g, amberHeader.b, .15f);
            categoryBadgeBg.raycastTarget = false;

            categoryBadgeText = CreateText("BadgeText", badgeObj.transform, 12, textPrimary, TextAlignmentOptions.Center);
            var btRect = categoryBadgeText.rectTransform;
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            categoryBadgeText.text = "PADRÃO";
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

                img.type = Image.Type.Simple;
                img.color = buttonBgNormal;
                img.raycastTarget = false;
                ledSegments[i] = img;
            }
        }

        private void BuildSliderControl(Transform parent, float posY)
        {
            // Botão [-]
            CreateStyledButton("StepDown", parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(38, posY), new Vector2(36, 36), "−", 22, buttonBgNormal, textPrimary,
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

            trackBg.type = Image.Type.Simple;
            trackBg.color = buttonBgNormal;
            trackBg.raycastTarget = true;

            var trackOutline = sliderObj.AddComponent<Outline>();
            trackOutline.effectColor = borderRed;
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

            sliderFillImg.type = Image.Type.Simple;
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
            hRect.sizeDelta = new Vector2(20, 4);

            sliderHandleImg = handleObj.AddComponent<Image>();

            sliderHandleImg.type = Image.Type.Simple;
            sliderHandleImg.color = textPrimary;
            sliderHandleImg.raycastTarget = true;

            // Núcleo de cor central no handle
            var handleCore = CreateUIElement("HandleCore", handleObj.transform);
            var hcRect = handleCore.GetComponent<RectTransform>();
            hcRect.anchorMin = new Vector2(0.5f, 0.5f);
            hcRect.anchorMax = new Vector2(0.5f, 0.5f);
            hcRect.sizeDelta = new Vector2(6, 18);
            var hcImg = handleCore.AddComponent<Image>();

            hcImg.type = Image.Type.Simple;
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
                new Vector2(-38, posY), new Vector2(36, 36), "+", 22, buttonBgNormal, textPrimary,
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
            string[] subLabels = { "LENTO", "PADRÃO", "ÁGIL", "RÁPIDO" };

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

                img.type = Image.Type.Simple;
                img.color = buttonBgNormal;
                img.raycastTarget = true;
                presetButtonBgs[i] = img;

                var outline = btnObj.AddComponent<Outline>();
                outline.effectColor = borderRed;
                outline.effectDistance = new Vector2(1, -1);

                var btn = btnObj.AddComponent<Button>();
                btn.targetGraphic = img;
                var cols = btn.colors;
                cols.normalColor = Color.white;
                cols.highlightedColor = HoverTint;
                cols.pressedColor = new Color(.7f, .65f, .55f);
                btn.colors = cols;
                btn.onClick.AddListener(() => SetSensitivity(pVal));
                presetButtons[i] = btn;

                var text = CreateText("ValText", btnObj.transform, 14, textPrimary, TextAlignmentOptions.Center);
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

            rowBg.type = Image.Type.Simple;
            rowBg.color = cardInnerBg;
            rowBg.raycastTarget = true;

            var outline = rowObj.AddComponent<Outline>();
            outline.effectColor = borderRed;
            outline.effectDistance = new Vector2(1, -1);

            invertYToggle = rowObj.AddComponent<Toggle>();
            invertYToggle.isOn = invertY;
            invertYToggle.targetGraphic = rowBg;

            // Ícone e Título da linha
            var title = CreateText("Title", rowObj.transform, 14, textPrimary, TextAlignmentOptions.Left);
            var tRect = title.rectTransform;
            tRect.anchorMin = new Vector2(0f, 0.5f);
            tRect.anchorMax = new Vector2(0f, 0.5f);
            tRect.pivot = new Vector2(0f, .5f);
            tRect.anchoredPosition = new Vector2(14, 7);
            tRect.sizeDelta = new Vector2(205, 22);
            title.text = "Inverter Eixo Vertical";
            title.fontStyle = FontStyles.Bold;

            var desc = CreateText("Desc", rowObj.transform, 11, textMuted, TextAlignmentOptions.Left);
            var dRect = desc.rectTransform;
            dRect.anchorMin = new Vector2(0f, 0.5f);
            dRect.anchorMax = new Vector2(0f, 0.5f);
            dRect.pivot = new Vector2(0f, .5f);
            dRect.anchoredPosition = new Vector2(14, -11);
            dRect.sizeDelta = new Vector2(205, 16);
            desc.text = "Olhar para cima / baixo";

            // Mechanical switch on the right
            var switchPill = CreateUIElement("SwitchPill", rowObj.transform);
            var spRect = switchPill.GetComponent<RectTransform>();
            spRect.anchorMin = new Vector2(1f, 0.5f);
            spRect.anchorMax = new Vector2(1f, 0.5f);
            spRect.pivot = new Vector2(1f, 0.5f);
            spRect.anchoredPosition = new Vector2(-14, 0);
            spRect.sizeDelta = new Vector2(50, 26);

            invertYSwitchBg = switchPill.AddComponent<Image>();

            invertYSwitchBg.type = Image.Type.Simple;
            invertYSwitchBg.color = invertY ? doomRed : buttonBgNormal;
            invertYSwitchBg.raycastTarget = false;

            var knobObj = CreateUIElement("Knob", switchPill.transform);
            invertYSwitchKnob = knobObj.GetComponent<RectTransform>();
            invertYSwitchKnob.anchorMin = new Vector2(0.5f, 0.5f);
            invertYSwitchKnob.anchorMax = new Vector2(0.5f, 0.5f);
            invertYSwitchKnob.sizeDelta = new Vector2(20, 20);
            invertYSwitchKnob.anchoredPosition = new Vector2(invertY ? 12f : -12f, 0f);

            var knobImg = knobObj.AddComponent<Image>();

            knobImg.color = textPrimary;
            knobImg.raycastTarget = false;

            // Status Text
            invertYStatusText = CreateText("StatusText", rowObj.transform, 11, textMuted, TextAlignmentOptions.Right);
            var stRect = invertYStatusText.rectTransform;
            stRect.anchorMin = new Vector2(1f, 0.5f);
            stRect.anchorMax = new Vector2(1f, 0.5f);
            stRect.pivot = new Vector2(1f, .5f);
            stRect.anchoredPosition = new Vector2(-78, 0);
            stRect.sizeDelta = new Vector2(102, 20);
            invertYStatusText.text = invertY ? "[ ATIVO ]" : "[ DESATIVADO ]";

            invertYToggle.onValueChanged.AddListener((val) => SetInvertY(val));
        }

        private void BuildFooterActions(Transform parent)
        {
            // Botão Redefinir
            CreateStyledButton("ResetBtn", parent, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(105, 40), new Vector2(174, 38), "REDEFINIR (1.0x)", 13, buttonBgNormal, textPrimary,
                () => { SetSensitivity(1.0f); SetInvertY(false); });

            // Botão Salvar e Fechar com destaque Doom Red
            CreateStyledButton("CloseConfirmBtn", parent, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-105, 40), new Vector2(174, 38), "VOLTAR AO JOGO", 13, doomRed, textPrimary,
                () => SetMenuOpen(false));

            // Dica de rodapé em fonte monospace sutil
            var hint = CreateText("FooterHint", parent, 10, textMuted, TextAlignmentOptions.Center);
            var hRect = hint.rectTransform;
            hRect.anchorMin = new Vector2(0.5f, 0f);
            hRect.anchorMax = new Vector2(0.5f, 0f);
            hRect.pivot = new Vector2(0.5f, 0f);
            hRect.anchoredPosition = new Vector2(0, 4);
            hRect.sizeDelta = new Vector2(380, 14);
            hint.text = "ESC  /  RETORNAR AO JOGO";
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
            if (bodyFont != null) tmp.font = bodyFont;
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

            img.type = Image.Type.Simple;
            img.color = bgColor;
            img.raycastTarget = true;

            var outline = btnObj.AddComponent<Outline>();
            outline.effectColor = borderRed;
            outline.effectDistance = new Vector2(1, -1);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = HoverTint;
            colors.pressedColor = new Color(.7f, .65f, .55f);
            colors.selectedColor = Color.white;
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
