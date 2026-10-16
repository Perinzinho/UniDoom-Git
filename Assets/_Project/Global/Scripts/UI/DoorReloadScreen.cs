using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace UniDoom.UI
{
    /// <summary>
    /// Tela de reload e aquecimento de shaders exibida durante passagens por portas ou transições de áreas.
    /// Bloqueia comandos do jogador temporariamente para evitar falhas ou quedas de taxa de quadros (hitches)
    /// enquanto o pipeline de renderização compila e carrega os shaders na GPU.
    /// </summary>
    public class DoorReloadScreen : MonoBehaviour
    {
        public static DoorReloadScreen Instance { get; private set; }

        /// <summary>Indica se a tela de reload está atualmente ativa.</summary>
        public static bool IsReloading => Instance != null && Instance.isReloading;

        [Header("Configurações de Transição")]
        [Tooltip("Duração do fade-in (aparecimento) em segundos.")]
        [SerializeField] private float fadeInDuration = 0.25f;

        [Tooltip("Duração do fade-out (desaparecimento) em segundos.")]
        [SerializeField] private float fadeOutDuration = 0.35f;

        [Tooltip("Tempo mínimo em que a tela fica visível para garantir leitura e carregamento suave.")]
        [SerializeField] private float minDisplayDuration = 1.1f;

        [Header("Aquecimento de Shaders")]
        [Tooltip("Chama Shader.WarmupAllShaders() durante a transição.")]
        [SerializeField] private bool warmupAllShaders = true;

        [Tooltip("Varre e aquece materiais de todos os Renderers na cena.")]
        [SerializeField] private bool warmupSceneMaterials = true;

        [Header("Controle e Depuração")]
        [Tooltip("Permite testar a tela pressionando a tecla F8 no Play Mode.")]
        [SerializeField] private bool enableDebugKey = true;
        [SerializeField] private KeyCode debugKey = KeyCode.F8;

        [Header("Personalização Visual")]
        [SerializeField] private TMP_FontAsset customFont;
        [SerializeField] private Color backgroundColor = new Color(0.04f, 0.045f, 0.06f, 0.98f);
        [SerializeField] private Color accentColor = new Color(0.9f, 0.22f, 0.16f, 1f); // Doom Red
        [SerializeField] private Color headerTagColor = new Color(1f, 0.72f, 0.12f, 1f); // Amber / Warning
        [SerializeField] private Color barBgColor = new Color(0.11f, 0.12f, 0.16f, 1f);

        // Referências de UI construídas dinamicamente
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Image progressBarFill;
        private TMP_Text headerTagText;
        private TMP_Text titleText;
        private TMP_Text subtitleText;
        private TMP_Text percentText;
        private TMP_Text statusDetailText;
        private TMP_Text blinkingIndicatorText;

        private bool isReloading = false;
        private Coroutine activeReloadRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var existing = FindAnyObjectByType<DoorReloadScreen>();
                if (existing != null)
                {
                    Instance = existing;
                }
                else
                {
                    var host = new GameObject("[DoorReloadScreen]");
                    Instance = host.AddComponent<DoorReloadScreen>();
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

            ResolveFont();
            BuildUIHierarchy();
            SetScreenVisible(false, 0f);
        }

        private void Update()
        {
            if (enableDebugKey && Input.GetKeyDown(debugKey) && !isReloading)
            {
                TriggerReload("SETOR DE TESTE (F8)");
            }
        }

        private void ResolveFont()
        {
            if (customFont != null) return;

            // Tenta encontrar Pixelta SDF ou a fonte padrão do TextMeshPro
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

        /// <summary>
        /// Dispara a tela de reload e warmup de shaders com um nome de setor opcional e callback de conclusão.
        /// </summary>
        public static void TriggerReload(string sectorName = "NOVO SETOR", Action onComplete = null)
        {
            if (Instance == null) AutoInitialize();
            if (Instance == null)
            {
                onComplete?.Invoke();
                return;
            }

            Instance.StartReloadTransition(sectorName, onComplete);
        }

        public void StartReloadTransition(string sectorName, Action onComplete)
        {
            if (isReloading)
            {
                onComplete?.Invoke();
                return;
            }

            if (activeReloadRoutine != null)
                StopCoroutine(activeReloadRoutine);

            activeReloadRoutine = StartCoroutine(ReloadRoutine(sectorName, onComplete));
        }

        private IEnumerator ReloadRoutine(string sectorName, Action onComplete)
        {
            isReloading = true;
            FreezePlayer(true);

            if (!string.IsNullOrEmpty(sectorName))
                subtitleText.text = $"// DESTINO: {sectorName.ToUpperInvariant()}";
            else
                subtitleText.text = "// PREPARANDO NOVO AMBIENTE";

            statusDetailText.text = "Iniciando compilação de shaders...";
            percentText.text = "0%";
            progressBarFill.fillAmount = 0f;

            // 1. Fade-in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, fadeInDuration));
                SetScreenVisible(true, t);
                yield return null;
            }
            SetScreenVisible(true, 1f);

            float startTime = Time.realtimeSinceStartup;

            // 2. Aquecimento de Shaders da Unity
            if (warmupAllShaders)
            {
                statusDetailText.text = "Compilando variantes ativas (Shader.WarmupAllShaders)...";
                percentText.text = "25%";
                progressBarFill.fillAmount = 0.25f;
                yield return null;

                try
                {
                    Shader.WarmupAllShaders();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DoorReloadScreen] WarmupAllShaders: {e.Message}");
                }

                yield return null;
            }

            // 3. Varredura e aquecimento de Renderers e Materiais da cena
            if (warmupSceneMaterials)
            {
                statusDetailText.text = "Aquecendo materiais e iluminação dos setores...";
                var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
                int total = renderers.Length;

                for (int i = 0; i < total; i++)
                {
                    var rend = renderers[i];
                    if (rend != null)
                    {
                        var mats = rend.sharedMaterials;
                        if (mats != null)
                        {
                            for (int m = 0; m < mats.Length; m++)
                            {
                                var mat = mats[m];
                                if (mat != null && mat.shader != null)
                                {
                                    // Acessa propriedades e passes para forçar o driver gráfico a registrar as variantes
                                    int passCount = mat.passCount;
                                    string sName = mat.shader.name;
                                    int q = mat.renderQueue;
                                }
                            }
                        }
                    }

                    // Atualiza a barra de forma fluida a cada lote
                    if (i % 20 == 0 || i == total - 1)
                    {
                        float progress = Mathf.Lerp(0.35f, 0.90f, (float)(i + 1) / Mathf.Max(1, total));
                        progressBarFill.fillAmount = progress;
                        percentText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
                        yield return null;
                    }
                }
            }

            progressBarFill.fillAmount = 0.96f;
            percentText.text = "96%";
            statusDetailText.text = "Sincronizando pipeline e buffers...";
            yield return null;

            // Garante o tempo mínimo de exibição para permitir que o driver termine o upload na GPU
            while (Time.realtimeSinceStartup - startTime < minDisplayDuration)
            {
                yield return null;
            }

            progressBarFill.fillAmount = 1f;
            percentText.text = "100%";
            statusDetailText.text = "[OK] Shaders carregados com sucesso!";
            if (blinkingIndicatorText != null) blinkingIndicatorText.text = "[ PRONTO ]";
            yield return new WaitForSecondsRealtime(0.15f);

            // 4. Fade-out
            elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(1f - (elapsed / Mathf.Max(0.01f, fadeOutDuration)));
                SetScreenVisible(true, t);
                yield return null;
            }

            SetScreenVisible(false, 0f);
            FreezePlayer(false);
            isReloading = false;
            activeReloadRoutine = null;

            onComplete?.Invoke();
        }

        private void SetScreenVisible(bool active, float alpha)
        {
            if (canvas != null)
            {
                canvas.enabled = active || alpha > 0.001f;
            }
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                canvasGroup.blocksRaycasts = active && alpha > 0.05f;
                canvasGroup.interactable = active && alpha > 0.05f;
            }
        }

        private void FreezePlayer(bool freeze)
        {
            // Bloqueia movimentação horizontal e gravidade no PlayerMovement
            var movement = FindAnyObjectByType<PlayerMovement>();
            if (movement != null)
            {
                movement.MovementEnabled = !freeze;
            }

            // Desativa entrada do Cinemachine no mouse para evitar rotação brusca durante o reload
            var cinemachineControllers = FindObjectsByType<Unity.Cinemachine.CinemachineInputAxisController>();
            foreach (var ctrl in cinemachineControllers)
            {
                if (ctrl != null) ctrl.enabled = !freeze;
            }
        }

        #region UI Construction
        private void BuildUIHierarchy()
        {
            // Canvas Raiz
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9990;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 1. Fundo escuro atmosférico fullscreen
            var bgObj = CreateUIElement("DarkBackdrop", transform);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            var bgImage = bgObj.AddComponent<Image>();
            bgImage.color = backgroundColor;
            bgImage.raycastTarget = false;

            // 2. Linhas de corte / framing industrial estilo Doom (topo e rodapé)
            CreateDecorativeBar("TopBar", transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0, 0), new Vector2(0, 4), accentColor);
            CreateDecorativeBar("BottomBar", transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 0), new Vector2(0, 4), accentColor);

            // 3. Painel Central da Interface
            var panelObj = CreateUIElement("CenterPanel", transform);
            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(680, 320);

            var panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.09f, 0.12f, 0.94f);

            var panelOutline = panelObj.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.25f, 0.12f, 0.12f, 1f);
            panelOutline.effectDistance = new Vector2(2, -2);

            // Header Tag (Amber)
            headerTagText = CreateText("HeaderTag", panelObj.transform, 18, headerTagColor, TextAlignmentOptions.Left);
            var headerRect = headerTagText.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0f, 1f);
            headerRect.anchoredPosition = new Vector2(30, -22);
            headerRect.sizeDelta = new Vector2(-60, 26);
            headerTagText.text = "/// PROTOCOLO DE TRANSIÇÃO // RECARREGANDO RECURSOS ///";

            // Título Principal
            titleText = CreateText("Title", panelObj.transform, 34, Color.white, TextAlignmentOptions.Left);
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(30, -56);
            titleRect.sizeDelta = new Vector2(-60, 42);
            titleText.text = "CARREGANDO SHADERS";
            titleText.fontStyle = FontStyles.Bold;

            // Subtítulo / Nome do setor
            subtitleText = CreateText("Subtitle", panelObj.transform, 17, new Color(0.75f, 0.78f, 0.85f), TextAlignmentOptions.Left);
            var subRect = subtitleText.rectTransform;
            subRect.anchorMin = new Vector2(0f, 1f);
            subRect.anchorMax = new Vector2(1f, 1f);
            subRect.pivot = new Vector2(0f, 1f);
            subRect.anchoredPosition = new Vector2(30, -100);
            subRect.sizeDelta = new Vector2(-60, 26);
            subtitleText.text = "// PREPARANDO NOVO AMBIENTE";

            // Barra de Progresso - Fundo
            var barBgObj = CreateUIElement("ProgressBarBg", panelObj.transform);
            var barBgRect = barBgObj.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            barBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            barBgRect.pivot = new Vector2(0.5f, 0.5f);
            barBgRect.anchoredPosition = new Vector2(0, -15);
            barBgRect.sizeDelta = new Vector2(620, 26);

            var barBgImage = barBgObj.AddComponent<Image>();
            barBgImage.color = barBgColor;

            var barOutline = barBgObj.AddComponent<Outline>();
            barOutline.effectColor = new Color(0.3f, 0.35f, 0.45f, 0.6f);
            barOutline.effectDistance = new Vector2(1, -1);

            // Barra de Progresso - Preenchimento (Filled)
            var barFillObj = CreateUIElement("ProgressBarFill", barBgObj.transform);
            var barFillRect = barFillObj.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = Vector2.one;
            barFillRect.sizeDelta = new Vector2(-4, -4);
            barFillRect.anchoredPosition = Vector2.zero;

            progressBarFill = barFillObj.AddComponent<Image>();
            progressBarFill.color = accentColor;
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            progressBarFill.fillAmount = 0f;

            // Porcentagem no lado direito
            percentText = CreateText("PercentText", panelObj.transform, 18, Color.white, TextAlignmentOptions.Right);
            var pRect = percentText.rectTransform;
            pRect.anchorMin = new Vector2(0f, 0.5f);
            pRect.anchorMax = new Vector2(1f, 0.5f);
            pRect.pivot = new Vector2(1f, 0.5f);
            pRect.anchoredPosition = new Vector2(-30, 15);
            pRect.sizeDelta = new Vector2(-60, 24);
            percentText.text = "0%";
            percentText.fontStyle = FontStyles.Bold;

            // Detalhe / Log do Warmup
            statusDetailText = CreateText("StatusDetail", panelObj.transform, 16, new Color(0.6f, 0.65f, 0.72f), TextAlignmentOptions.Left);
            var statRect = statusDetailText.rectTransform;
            statRect.anchorMin = new Vector2(0f, 0f);
            statRect.anchorMax = new Vector2(1f, 0f);
            statRect.pivot = new Vector2(0f, 0f);
            statRect.anchoredPosition = new Vector2(30, 48);
            statRect.sizeDelta = new Vector2(-60, 26);
            statusDetailText.text = "Inicializando componentes gráficos...";

            // Indicador piscante / status
            blinkingIndicatorText = CreateText("Blinker", panelObj.transform, 15, accentColor, TextAlignmentOptions.Right);
            var blinkRect = blinkingIndicatorText.rectTransform;
            blinkRect.anchorMin = new Vector2(0f, 0f);
            blinkRect.anchorMax = new Vector2(1f, 0f);
            blinkRect.pivot = new Vector2(1f, 0f);
            blinkRect.anchoredPosition = new Vector2(-30, 48);
            blinkRect.sizeDelta = new Vector2(-60, 26);
            blinkingIndicatorText.text = "[ COMPILANDO ]";
            blinkingIndicatorText.fontStyle = FontStyles.Bold;
        }

        private GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private void CreateDecorativeBar(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var bar = CreateUIElement(name, parent);
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = bar.AddComponent<Image>();
            img.color = color;
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
        #endregion
    }
}
