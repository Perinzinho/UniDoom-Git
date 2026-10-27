using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniDoom.Utils;

namespace UniDoom.UI
{
    /// <summary>Persistent loading cover shared by local door warmups and asynchronous scene travel.</summary>
    public class DoorReloadScreen : MonoBehaviour
    {
        public static DoorReloadScreen Instance { get; private set; }
        public static bool IsReloading => Instance != null && Instance.isReloading;

        [Header("Transição")]
        [SerializeField, Min(0f)] private float fadeInDuration = 0.45f;
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.65f;
        [SerializeField, Min(0f)] private float minDisplayDuration = 1.6f;
        [Header("Aquecimento de shaders")]
        [Tooltip("Coleção opcional de variantes usadas pelo jogo. O aquecimento global é evitado no URP.")]
        [SerializeField] private ShaderVariantCollection shaderVariants;
        [Header("Prévia")]
        [SerializeField] private bool enableDebugKey = true;
        [SerializeField] private KeyCode debugKey = KeyCode.F8;
        [Header("Aparência")]
        [SerializeField] private TMP_FontAsset customFont;
        [SerializeField] private Color headerTagColor = new Color(.65f, .47f, .24f, 1f);

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        [Header("Atmosfera da passagem")]
        [SerializeField] private Texture2D passageImage;
        [SerializeField] private AudioClip passageAmbience;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = .16f;
        [Serializable]
        private sealed class DestinationArtwork
        {
            public string sceneName;
            public string resourcePath;

            public DestinationArtwork(string scene)
            {
                sceneName = scene;
                resourcePath = "Textures/UI/Transitions/" + scene;
            }
        }

        [Tooltip("Imagem de cada destino. Os caminhos são relativos às pastas Resources dos cenários.")]
        [SerializeField] private DestinationArtwork[] destinationArtworks =
        {
            new DestinationArtwork("Cylinder"),
            new DestinationArtwork("Library"),
            new DestinationArtwork("ClassRoom"),
            new DestinationArtwork("DirectorRoom"),
            new DestinationArtwork("EntranceGarden")
        };
        private static readonly string[] DebugSectors =
            { "REDONDO", "BIBLIOTECA", "SALA DE AULA", "DIRETORIA", "CORREDOR DE ENTRADA" };
        private readonly Dictionary<string, Texture2D> destinationImages = new(StringComparer.OrdinalIgnoreCase);
        private Texture2D activePassageImage;
        private int debugDestinationIndex;
        private RawImage passageView;
        private AudioSource ambienceSource;
        private readonly Image[] passageMarks = new Image[3];
        private TMP_Text titleText, subtitleText, statusDetailText;
        private float transitionBegan, loadingProgress;
        private TMP_FontAsset bodyFont;
        private bool isReloading;
        private float savedTimeScale;
        private PlayerMovement frozenPlayer;
        private bool savedMovement;
        private readonly Dictionary<CinemachineInputAxisController, bool> inputStates = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance != null) return;
            var existing = FindAnyObjectByType<DoorReloadScreen>();
            if (existing != null) Instance = existing;
            else new GameObject("[DoorReloadScreen]").AddComponent<DoorReloadScreen>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ResolveFont();
            BuildUIHierarchy();
            SetScreenVisible(false, 0);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            if (isReloading) RestorePlayer();
            Instance = null;
        }

        private void Update()
        {
            if (enableDebugKey && Input.GetKeyDown(debugKey) && !isReloading)
            {
                TriggerReload(DebugSectors[debugDestinationIndex]);
                debugDestinationIndex = (debugDestinationIndex + 1) % DebugSectors.Length;
            }
            if (!isReloading) return;
            float elapsed = Time.unscaledTime - transitionBegan;
            float breath = .5f + .5f * Mathf.Sin(elapsed * .8f);
            passageView.color = new Color(.94f, .91f, .86f, .88f + .04f * breath);
            FitPassageImage(1.025f + elapsed * .0006f + .004f * breath);
            for (int i = 0; i < passageMarks.Length; i++)
            {
                float pulse = .5f + .5f * Mathf.Sin(elapsed * 1.6f - i * .8f);
                var mark = passageMarks[i];
                var tint = headerTagColor;
                tint.a = loadingProgress >= (i + 1) / 3f ? .7f : .18f + .18f * pulse;
                mark.color = tint;
            }
            if (ambienceSource != null)
                ambienceSource.volume = ambienceVolume * canvasGroup.alpha;
        }

        private void ResolveFont()
        {
            bodyFont = TMP_Settings.defaultFontAsset;
            if (customFont != null) return;
            foreach (var font in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                if (font != null && font.name.IndexOf("Pixelta", StringComparison.OrdinalIgnoreCase) >= 0)
                { customFont = font; return; }
            customFont = TMP_Settings.defaultFontAsset;
        }

        // Existing callers remain compatible. A busy screen never invokes callbacks prematurely.
        public static void TriggerReload(string sectorName = "NOVO SETOR", Action onComplete = null)
        {
            if (MouseSensitivityMenu.IsOpen || DeathScreen.IsVisible) return;
            if (Instance == null) AutoInitialize();
            Instance.StartReloadTransition(sectorName, onComplete);
        }

        public void StartReloadTransition(string sectorName, Action onComplete)
        {
            if (isReloading || MouseSensitivityMenu.IsOpen || DeathScreen.IsVisible) return;
            StartCoroutine(ReloadRoutine(sectorName, null, null, onComplete));
        }

        public static bool TryLoadScene(string sceneName, string spawnId, string label = null)
        {
            if (IsReloading || MouseSensitivityMenu.IsOpen || DeathScreen.IsVisible) return false;
            if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(spawnId) ||
                !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[DoorReloadScreen] Destino inválido ou ausente do Build Settings: '{sceneName}' / '{spawnId}'.");
                return false;
            }
            if (Instance == null) AutoInitialize();
            Instance.StartCoroutine(Instance.ReloadRoutine(
                string.IsNullOrWhiteSpace(label) ? sceneName : label, sceneName, spawnId, null));
            return true;
        }

        public static IEnumerator PlayCheckpointTransition(string scenePath, IEnumerator restoreCheckpoint)
        {
            if (Instance == null) AutoInitialize();
            string destination = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            string label = destination switch
            {
                "EntranceGarden" => "JARDIM DO CAMPUS",
                "Cylinder" => "BLOCO UNIVERSIT\u00c1RIO",
                "Library" => "BIBLIOTECA",
                "ClassRoom" => "SALA DE AULA",
                "DirectorRoom" => "DIRETORIA",
                _ => destination
            };
            yield return Instance.ReloadRoutine(label, null, null, null, scenePath, restoreCheckpoint);
        }

        private IEnumerator ReloadRoutine(string sectorName, string sceneName, string spawnId, Action onComplete,
            string checkpointScene = null, IEnumerator restoreCheckpoint = null)
        {
            isReloading = true;
            FreezePlayer();
            transitionBegan = Time.unscaledTime;
            if (ambienceSource.clip != null && ambienceVolume > 0f) ambienceSource.Play();
            bool success = false;
            try
            {
                SelectDestinationArtwork(checkpointScene ?? sceneName, sectorName);
                titleText.text = string.IsNullOrWhiteSpace(sectorName) ? "SETOR DESCONHECIDO" : sectorName.ToUpperInvariant();
                subtitleText.text = "ALÉM DA PORTA";
                SetProgress(0, "A porta se fecha...");
                for (float elapsed = 0; elapsed < fadeInDuration; elapsed += Time.unscaledDeltaTime)
                {
                    SetScreenVisible(true, Mathf.Clamp01(elapsed / Mathf.Max(.01f, fadeInDuration)));
                    yield return null;
                }
                SetScreenVisible(true, 1);
                // Render the opaque cover before loading or warming any shaders.
                yield return null;
                float started = Time.realtimeSinceStartup;
                string error = null;
                if (restoreCheckpoint != null)
                {
                    subtitleText.text = "RETORNO AO CHECKPOINT";
                    SetProgress(.1f, "Retornando ao ponto de retorno...");
                    yield return restoreCheckpoint;
                    SetProgress(.88f, "Do outro lado...");
                }
                if (sceneName != null)
                {
                    AsyncOperation load = null;
                    try { load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single); }
                    catch (Exception e) { error = e.Message; }
                    if (load == null) error = error ?? "A Unity não iniciou o carregamento.";
                    else
                    {
                        while (!load.isDone)
                        {
                            SetProgress(Mathf.Lerp(.05f, .85f, Mathf.Clamp01(load.progress / .9f)), "Atravessando...");
                            yield return null;
                        }
                        // GameManager.Start (when starting directly in a scene) and physics get a frame to settle.
                        yield return null;
                        if (!SceneArrivalPoint.TryPlacePlayer(SceneManager.GetActiveScene(), spawnId))
                            error = $"Ponto de chegada '{spawnId}' ou player não encontrado em '{sceneName}'.";
                        SetProgress(.88f, "Do outro lado...");
                        yield return null;
                    }
                }
                if (error == null && shaderVariants != null)
                {
                    SetProgress(.92f, "Do outro lado...");
                    yield return null;
                    try { shaderVariants.WarmUp(); }
                    catch (Exception e) { Debug.LogWarning($"[DoorReloadScreen] Shader warmup: {e.Message}"); }
                }
                while (Time.realtimeSinceStartup - started < minDisplayDuration) yield return null;
                if (error != null)
                {
                    Debug.LogError("[DoorReloadScreen] " + error);
                    subtitleText.text = "PASSAGEM INTERROMPIDA";
                    statusDetailText.text = "Não foi possível concluir a passagem.";
                    yield return new WaitForSecondsRealtime(2f);
                }
                else
                {
                    SetProgress(1, string.Empty);
                    success = true;
                    yield return new WaitForSecondsRealtime(.15f);
                }
                for (float elapsed = 0; elapsed < fadeOutDuration; elapsed += Time.unscaledDeltaTime)
                {
                    SetScreenVisible(true, 1f - Mathf.Clamp01(elapsed / Mathf.Max(.01f, fadeOutDuration)));
                    yield return null;
                }
            }
            finally
            {
                SetScreenVisible(false, 0);
                ambienceSource.Stop();
                ambienceSource.volume = 0f;
                RestorePlayer();
                isReloading = false;
            }
            if (success) onComplete?.Invoke();
        }

        private void SetProgress(float progress, string status)
        {
            loadingProgress = Mathf.Clamp01(progress);
            statusDetailText.text = status;
        }

        private void SelectDestinationArtwork(string sceneName, string sectorName)
        {
            // Actual travel always uses the scene ID; labels are only aliases for local/F8 previews.
            string destination = string.IsNullOrWhiteSpace(sceneName) ? sectorName : sceneName;
            destination = string.IsNullOrWhiteSpace(destination) ? string.Empty
                : System.IO.Path.GetFileNameWithoutExtension(destination).Trim().ToUpperInvariant();
            switch (destination)
            {
                case "HUB": case "CILINDRO": case "BLOCO UNIVERSITÁRIO": case "REDONDO": destination = "CYLINDER"; break;
                case "BIBLIOTECA": destination = "LIBRARY"; break;
                case "SALA DE AULA": destination = "CLASSROOM"; break;
                case "DIRETORIA": destination = "DIRECTORROOM"; break;
                case "ENTRANCEHALLWAY": case "CORREDOR DE ENTRADA": destination = "ENTRANCEGARDEN"; break;
            }

            // Reset to the shared image on every request, so an unknown destination cannot show the previous room.
            activePassageImage = passageImage;
            if (destinationArtworks != null)
                foreach (var artwork in destinationArtworks)
                {
                    if (artwork == null || !string.Equals(artwork.sceneName, destination, StringComparison.OrdinalIgnoreCase)
                        || string.IsNullOrWhiteSpace(artwork.resourcePath)) continue;
                    if (!destinationImages.TryGetValue(artwork.resourcePath, out var image))
                    {
                        image = Resources.Load<Texture2D>(artwork.resourcePath);
                        destinationImages[artwork.resourcePath] = image;
                        if (image == null) Debug.LogWarning($"[DoorReloadScreen] Imagem de destino ausente: {artwork.resourcePath}");
                    }
                    if (image != null) activePassageImage = image;
                    break;
                }
            passageView.texture = activePassageImage;
            passageView.enabled = activePassageImage != null;
            FitPassageImage(1.025f);
        }

        private void FreezePlayer()
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0;
            frozenPlayer = GameManager.SessionPlayer;
            if (frozenPlayer == null) frozenPlayer = FindAnyObjectByType<PlayerMovement>();
            if (frozenPlayer != null) { savedMovement = frozenPlayer.MovementEnabled; frozenPlayer.MovementEnabled = false; }
            inputStates.Clear();
            foreach (var input in FindObjectsByType<CinemachineInputAxisController>())
            { inputStates[input] = input.enabled; input.enabled = false; }
        }

        private void RestorePlayer()
        {
            Time.timeScale = savedTimeScale;
            if (frozenPlayer != null) frozenPlayer.MovementEnabled = savedMovement;
            foreach (var state in inputStates) if (state.Key != null) state.Key.enabled = state.Value;
            inputStates.Clear();
        }

        private void SetScreenVisible(bool active, float alpha)
        {
            canvas.enabled = active;
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = active;
            canvasGroup.interactable = false;
        }

        private void BuildUIHierarchy()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            // Resources keep the automatically-created screen available from every playable scene.
            if (passageImage == null) passageImage = Resources.Load<Texture2D>("Textures/UI/Transitions/AbandonedDoor");
            activePassageImage = passageImage;
            if (passageAmbience == null) passageAmbience = Resources.Load<AudioClip>("Audio/UI/Transitions/ThresholdDrone");
            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.volume = 0f;
            ambienceSource.clip = passageAmbience;

            var background = Rect("Blackout", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            background.gameObject.AddComponent<Image>().color = Color.black;
            var imageRect = Rect("The other side", background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            passageView = imageRect.gameObject.AddComponent<RawImage>();
            passageView.texture = passageImage;
            passageView.color = new Color(.94f, .91f, .86f, .9f);
            passageView.raycastTarget = false;
            if (passageImage == null) passageView.enabled = false;
            var veil = Rect("Peripheral darkness", background, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            veil.gameObject.AddComponent<HorrorTransitionVeil>().raycastTarget = false;

            var caption = Rect("Quiet destination", transform, new Vector2(.08f,.11f), new Vector2(.08f,.11f), Vector2.zero, new Vector2(900,100));
            caption.pivot = new Vector2(0,0);
            subtitleText = Label("Threshold", caption, "AL\u00c9M DA PORTA", 15, headerTagColor, new Vector2(0,0), new Vector2(800,24));
            titleText = Label("Destination", caption, "SETOR DESCONHECIDO", 42, new Color(.76f,.73f,.65f), new Vector2(0,-30), new Vector2(900,62));
            titleText.fontStyle = FontStyles.Bold;
            if (customFont != null) titleText.font = customFont;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 24;
            titleText.fontSizeMax = 42;
            titleText.characterSpacing = 5f;

            var activity = Rect("Waiting in the dark", transform, new Vector2(.92f,.11f), new Vector2(.92f,.11f), Vector2.zero, new Vector2(360,65));
            activity.pivot = new Vector2(1,0);
            statusDetailText = Label("Status", activity, "Atravessando...", 17, new Color(.46f,.43f,.36f), new Vector2(0,-18), new Vector2(360,32));
            statusDetailText.alignment = TextAlignmentOptions.Right;
            for (int i = 0; i < passageMarks.Length; i++)
            {
                var tick = Rect("Quiet pulse " + i, activity, Vector2.one, Vector2.one, new Vector2(-4 - (2-i)*16, 0), new Vector2(8,2));
                passageMarks[i] = tick.gameObject.AddComponent<Image>();
                passageMarks[i].color = headerTagColor;
                passageMarks[i].raycastTarget = false;
            }
        }

        private void FitPassageImage(float zoom)
        {
            if (activePassageImage == null || passageView == null) return;
            var imageRect = passageView.rectTransform;
            var size = ((RectTransform)imageRect.parent).rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            float imageAspect = (float)activePassageImage.width / activePassageImage.height;
            float screenAspect = size.x / size.y;
            // Preserve the entire doorway on ultrawide views; its dark edges blend into the blackout.
            imageRect.anchorMin = imageRect.anchorMax = new Vector2(.5f, .5f);
            imageRect.sizeDelta = screenAspect > imageAspect
                ? new Vector2(size.y * imageAspect, size.y)
                : new Vector2(size.x, size.x / imageAspect);
            float visible = 1f / Mathf.Clamp(zoom, 1f, 1.08f);
            passageView.uvRect = new Rect((1-visible)*.5f, (1-visible)*.5f, visible, visible);
        }

        private RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }

        private TMP_Text Label(string name, Transform parent, string value, float size, Color color, Vector2 position, Vector2 bounds)
        {
            var rect = Rect(name, parent, new Vector2(0,1), new Vector2(0,1), position, bounds);
            rect.pivot = new Vector2(0,1);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (bodyFont != null) text.font = bodyFont;
            text.text = value; text.fontSize = size; text.color = color;
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            text.richText = false;
            return text;
        }
    }
}
