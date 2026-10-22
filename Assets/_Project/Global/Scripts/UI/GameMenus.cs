using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UniDoom.UI
{
    /// <summary>Shared main, pause, death, options and confirmation screens. Progress lasts for the current run.</summary>
    public sealed partial class GameMenus : MonoBehaviour
    {
        public enum Screen { Gameplay, Main, Pause, Death, Options, Credits, Confirm, MapSelect, GameOver, Document }
        public static GameMenus Instance { get; private set; }
        public static bool BlocksGameplay => Instance != null && (Instance.CurrentScreen != Screen.Gameplay || Instance.busy);
        public Screen CurrentScreen { get; private set; }
        public bool IsBusy => busy;

        RectTransform layout;
        Canvas canvas;
        RawImage backdrop;
        Image shade, fade;
        GameObject main, pause, death, options, credits, confirmation, mapSelection, gameOver;
        Button gameOverRestart, gameOverBack;
        Button chooseMap, mapFirst;
        Button mainFirst, pauseFirst, deathRetry, deathBack, optionsBack, creditsBack, cancel;
        Slider sensitivity, volume;
        Toggle invertY, fullscreen;
        TMP_Text sensitivityValue, volumeValue, confirmText, error, deathHint;
        Screen optionsOrigin, confirmOrigin;
        Action confirmAction;
        PlayerRespawn deadPlayer;
        DeathScreen deathOwner;
        bool busy, ownsPause;
        float previousTimeScale;
        bool previousAudioPause, previousMovement;
        PlayerMovement frozenPlayer;
        readonly Dictionary<CinemachineInputAxisController, bool> cameraStates = new();
        readonly Dictionary<Canvas, bool> hudStates = new();
        readonly List<Sprite> artwork = new();
        const string VolumeKey = "UniDoom_MasterVolume";
        const string FullscreenKey = "UniDoom_Fullscreen";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance == null) new GameObject("[UniDoom Menus]", typeof(RectTransform)).AddComponent<GameMenus>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1));
            if (PlayerPrefs.HasKey(FullscreenKey)) UnityEngine.Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey) == 1;
            Build();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SetScreen(Screen.Main);
            AcquirePause();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureEvents();
            if (CurrentScreen == Screen.Main) Select(mainFirst);
            if (BlocksGameplay) HideHUD();
            Fit();
        }

        void Update()
        {
            if (busy) return;
            bool back = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.buttonEast.wasPressedThisFrame));
            if (back && CurrentScreen != Screen.Document && !DoorReloadScreen.IsReloading)
            {
                if (CurrentScreen == Screen.Gameplay) OpenPause();
                else Back();
            }
            if (CurrentScreen == Screen.Death)
            {
                bool ready = deathOwner != null && deathOwner.CanRespawn;
                bool backReady = deathOwner != null && deathOwner.IsReady;
                if (deathRetry.interactable != ready || deathBack.interactable != backReady)
                {
                    deathRetry.interactable = ready;
                    deathBack.interactable = backReady;
                    if (backReady) Select(ready ? deathRetry : deathBack);
                }
            }
            if (CurrentScreen == Screen.GameOver)
            {
                bool ready = deathOwner != null && deathOwner.IsReady;
                if (gameOverRestart.interactable != ready)
                {
                    gameOverRestart.interactable = gameOverBack.interactable = ready;
                    if (ready) Select(gameOverRestart);
                }
            }
        }

        void LateUpdate()
        {
            if (CurrentScreen == Screen.Document && documentOwner == null) CloseDocument();
            if (ownsPause) { Time.timeScale = 0; AudioListener.pause = true; FreezePlayer(); }
            ApplyCursorState();
        }

        public void ApplyCursorState()
        {
            // Derive ownership from the current screen, never from a transient fade's cursor state.
            bool show = CurrentScreen != Screen.Gameplay && !DoorReloadScreen.IsReloading;
            Cursor.lockState = show ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = show;
        }

        public void OpenPause()
        {
            if (CurrentScreen != Screen.Gameplay || busy || DoorReloadScreen.IsReloading || DeathScreen.IsVisible) return;
            AcquirePause();
            SetScreen(Screen.Pause);
        }

        public void OpenOptions()
        {
            if (busy || CurrentScreen == Screen.Death || CurrentScreen == Screen.GameOver || CurrentScreen == Screen.Confirm || CurrentScreen == Screen.Document) return;
            if (CurrentScreen == Screen.Gameplay) OpenPause();
            optionsOrigin = CurrentScreen;
            if (MouseSensitivityMenu.Instance != null)
            {
                sensitivity.SetValueWithoutNotify(MouseSensitivityMenu.CurrentSensitivity);
                invertY.SetIsOnWithoutNotify(MouseSensitivityMenu.Instance.InvertY);
            }
            volume.SetValueWithoutNotify(AudioListener.volume);
            fullscreen.SetIsOnWithoutNotify(UnityEngine.Screen.fullScreen);
            UpdateOptionsLabels();
            SetScreen(Screen.Options);
        }

        public void Back()
        {
            if (busy) return;
            switch (CurrentScreen)
            {
                case Screen.Pause: Resume(); break;
                case Screen.Options: PlayerPrefs.Save(); SetScreen(optionsOrigin); break;
                case Screen.Credits: SetScreen(Screen.Main); break;
                case Screen.MapSelect: SetScreen(Screen.Main); Select(chooseMap); break;
                case Screen.Confirm: SetScreen(confirmOrigin); break;
            }
        }

        public void Resume()
        {
            if (CurrentScreen != Screen.Pause) return;
            SetScreen(Screen.Gameplay);
            ReleasePause();
        }

        public void CloseForDeath()
        {
            if (CurrentScreen == Screen.Document) CloseDocument();
            if (ownsPause) ReleasePause();
            SetScreen(Screen.Gameplay);
        }

        public void ShowDeath(PlayerRespawn player, DeathScreen owner)
        {
            if (CurrentScreen == Screen.Document) CloseDocument();
            deadPlayer = player;
            deathOwner = owner;
            int lives = player != null ? player.RemainingLives : 0;
            if (lives == 0)
            {
                gameOverRestart.interactable = gameOverBack.interactable = false;
                SetScreen(Screen.GameOver);
                return;
            }
            deathHint.text = $"Vidas restantes: {lives} / 3. Recomeçar no último ponto de retorno.";
            deathRetry.interactable = deathBack.interactable = false;
            SetScreen(Screen.Death);
        }

        public void HideDeath()
        {
            if (CurrentScreen == Screen.Death || CurrentScreen == Screen.GameOver ||
                (CurrentScreen == Screen.Confirm && (confirmOrigin == Screen.Death || confirmOrigin == Screen.GameOver)))
                SetScreen(Screen.Gameplay);
            deadPlayer = null;
            deathOwner = null;
        }

        public void SetFade(float alpha) { fade.color = new Color(0, 0, 0, Mathf.Clamp01(alpha)); fade.raycastTarget = alpha > 0; }

        public void RequestMainMenu()
        {
            if (busy || (CurrentScreen != Screen.Pause && CurrentScreen != Screen.Death && CurrentScreen != Screen.GameOver)) return;
            Confirm("VOCÊ TEM CERTEZA?\n<size=30>Os dados não serão salvos.</size>", ReturnToMain);
        }

        void Confirm(string message, Action action)
        {
            confirmOrigin = CurrentScreen;
            confirmAction = action;
            confirmText.text = message;
            SetScreen(Screen.Confirm);
        }

        void ReturnToMain()
        {
            var player = GameManager.SessionPlayer;
            if (player != null) player.GetComponent<PlayerRespawn>()?.PrepareForMainMenu();
            ReleasePause();
            GameManager.EndSession();
            SetScreen(Screen.Main);
            AcquirePause();
        }

        public void StartNewGame()
        {
            StartNewGameAtScene(SceneUtility.GetScenePathByBuildIndex(0));
        }

        void RestartFromBeginning()
        {
            if (CurrentScreen != Screen.GameOver || deathOwner == null || !deathOwner.IsReady || DoorReloadScreen.IsReloading) return;
            StartNewGame();
        }

        void OpenMapSelection()
        {
            if (busy || CurrentScreen != Screen.Main) return;
            SetScreen(Screen.MapSelect);
        }

        void StartNewGameAtScene(string scene, bool startAtBoss = false)
        {
            if (busy) return;
            if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene))
            { error.text = "O mapa não está disponível no Build Settings."; return; }
            StartCoroutine(NewGame(scene, startAtBoss));
        }

        IEnumerator NewGame(string scene, bool startAtBoss)
        {
            busy = true;
            for (float t = 0; t < .2f; t += Time.unscaledDeltaTime) { SetFade(t / .2f); yield return null; }
            SetFade(1);
            var oldPlayer = GameManager.SessionPlayer;
            if (oldPlayer != null) oldPlayer.GetComponent<PlayerRespawn>()?.PrepareForMainMenu();
            ReleasePause();
            GameManager.EndSession();
            yield return null;
            AsyncOperation load = null;
            bool startConfigured = !startAtBoss;
            void ConfigureStart(Scene loaded, LoadSceneMode mode)
            {
                if (!startAtBoss || loaded.path != scene) return;
                Transform start = null;
                DirectorBossEncounter encounter = null;
                // Awake has already moved the manager into DontDestroyOnLoad.
                CharacterController playerController = GameManager.SessionPlayerControllerPrefab;
                foreach (var root in loaded.GetRootGameObjects())
                {
                    if (root.name == "SpawnPoint") start = root.transform;
                    if (encounter == null) encounter = root.GetComponentInChildren<DirectorBossEncounter>();
                    var manager = root.GetComponent<GameManager>();
                    if (manager != null) playerController = manager.PlayerControllerPrefab;
                }
                // sceneLoaded runs before GameManager.Start creates the player and checkpoint.
                startConfigured = start != null && encounter != null && encounter.TryConfigurePlayerStart(start, playerController);
            }
            SceneManager.sceneLoaded += ConfigureStart;
            try
            {
                try { load = SceneManager.LoadSceneAsync(scene); }
                catch (Exception e) { Debug.LogException(e); }
                if (load != null) while (!load.isDone) yield return null;
            }
            finally { SceneManager.sceneLoaded -= ConfigureStart; }
            if (load == null)
            {
                busy = false;
                SetFade(0);
                SetScreen(Screen.Main);
                AcquirePause();
                error.text = "Não foi possível iniciar o jogo. Tente novamente.";
                yield break;
            }
            yield return null;
            if (!startConfigured)
            {
                busy = false;
                SetFade(0);
                SetScreen(Screen.Main);
                AcquirePause();
                error.text = "Não foi encontrado um ponto livre na arena do boss.";
                yield break;
            }
            SetScreen(Screen.Gameplay);
            Time.timeScale = 1;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            for (float t = 0; t < .3f; t += Time.unscaledDeltaTime) { SetFade(1 - t / .3f); yield return null; }
            SetFade(0);
            busy = false;
        }

        public void RestartCheckpoint()
        {
            if (busy || DoorReloadScreen.IsReloading || CurrentScreen == Screen.GameOver) return;
            if (CurrentScreen == Screen.Death)
            { if (deathOwner != null && deathOwner.CanRespawn) deadPlayer?.TryRespawn(); return; }
            var player = GameManager.SessionPlayer;
            var respawn = player != null ? player.GetComponent<PlayerRespawn>() : null;
            if (respawn == null) { error.text = "Ponto de retorno indisponível."; return; }
            Resume();
            respawn.RestartCheckpoint();
        }

        void AcquirePause()
        {
            if (ownsPause) return;
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            ownsPause = true;
            Time.timeScale = 0;
            AudioListener.pause = true;
            FreezePlayer();
        }

        void FreezePlayer()
        {
            var player = GameManager.SessionPlayer;
            if (player != null && player != frozenPlayer)
            {
                frozenPlayer = player;
                previousMovement = player.MovementEnabled;
                player.MovementEnabled = false;
                player.ResetMotion();
                foreach (var axis in player.GetComponentsInChildren<CinemachineInputAxisController>(true))
                { cameraStates[axis] = axis.enabled; axis.enabled = false; }
                HideHUD();
            }
            foreach (var pair in cameraStates) if (pair.Key != null) pair.Key.enabled = false;
        }

        void ReleasePause()
        {
            if (!ownsPause) return;
            ownsPause = false;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            if (frozenPlayer != null) frozenPlayer.MovementEnabled = previousMovement;
            foreach (var pair in cameraStates) if (pair.Key != null) pair.Key.enabled = pair.Value;
            cameraStates.Clear();
            frozenPlayer = null;
            ApplyCursorState();
        }

        void HideHUD()
        {
            // Hide gameplay HUD while preserving every canvas's previous enabled state.
            if (DebugUI.Instance != null)
                foreach (var debug in DebugUI.Instance.GetComponentsInChildren<Canvas>(true))
                {
                    if (!hudStates.ContainsKey(debug)) hudStates[debug] = debug.enabled;
                    debug.enabled = false;
                }
            if (GameManager.SessionPlayer == null) return;
            var root = GameManager.SessionPlayer.transform.root;
            foreach (var hud in root.GetComponentsInChildren<Canvas>(true))
            {
                if (!hudStates.ContainsKey(hud)) hudStates[hud] = hud.enabled;
                hud.enabled = false;
            }
        }

        void RestoreHUD()
        {
            foreach (var state in hudStates) if (state.Key != null) state.Key.enabled = state.Value;
            hudStates.Clear();
        }

        void SetScreen(Screen screen)
        {
            CurrentScreen = screen;
            main.SetActive(screen == Screen.Main);
            pause.SetActive(screen == Screen.Pause);
            death.SetActive(screen == Screen.Death);
            gameOver.SetActive(screen == Screen.GameOver);
            document.SetActive(screen == Screen.Document);
            options.SetActive(screen == Screen.Options);
            credits.SetActive(screen == Screen.Credits);
            confirmation.SetActive(screen == Screen.Confirm);
            mapSelection.SetActive(screen == Screen.MapSelect);
            bool mainBackground = screen == Screen.Main || screen == Screen.Credits || screen == Screen.MapSelect ||
                (screen == Screen.Options && optionsOrigin == Screen.Main);
            bool gameOverBackground = screen == Screen.GameOver || (screen == Screen.Confirm && confirmOrigin == Screen.GameOver);
            backdrop.gameObject.SetActive(mainBackground || gameOverBackground);
            shade.gameObject.SetActive(screen != Screen.Gameplay);
            shade.color = new Color(0, 0, 0, mainBackground || screen == Screen.GameOver ? 0 : screen == Screen.Document ? .72f : screen == Screen.Pause ? .1f : screen == Screen.Death ? .2f : .78f);
            error.text = "";
            if (screen == Screen.Gameplay) RestoreHUD(); else HideHUD();
            EnsureEvents();
            Select(screen switch { Screen.Main => mainFirst, Screen.Pause => pauseFirst,
                Screen.Death => deathRetry, Screen.Options => optionsBack, Screen.Credits => creditsBack,
                Screen.Confirm => cancel, Screen.MapSelect => mapFirst, Screen.GameOver => gameOverRestart,
                Screen.Document => documentClose, _ => null });
        }

        static void Select(Selectable control)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (control != null && control.IsInteractable()) control.Select();
        }

        void EnsureEvents()
        {
            var events = FindObjectsByType<EventSystem>();
            EventSystem chosen = null;
            foreach (var item in events)
                if (item.gameObject.scene == SceneManager.GetActiveScene()) { chosen = item; break; }
            if (chosen == null && events.Length > 0) chosen = events[0];
            if (chosen == null)
            {
                var host = new GameObject("[Menu EventSystem]", typeof(EventSystem));
                DontDestroyOnLoad(host);
                chosen = host.GetComponent<EventSystem>();
                host.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            foreach (var item in events) item.enabled = item == chosen;
            chosen.enabled = true;
            if (chosen.GetComponent<BaseInputModule>() == null) chosen.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            gameObject.AddComponent<GraphicRaycaster>();
            backdrop = MenuStyle.Rect("Real campus photograph", transform, 0, 0, 0, 0).gameObject.AddComponent<RawImage>();
            MenuStyle.Stretch(backdrop.rectTransform);
            backdrop.texture = Resources.Load<Texture2D>("Menus/CampusBackground");
            backdrop.raycastTarget = false;
            shade = MenuStyle.Image("Modal shade", transform, 0, 0, 0, 0, Color.clear);
            MenuStyle.Stretch(shade.rectTransform);
            shade.raycastTarget = true;
            layout = MenuStyle.Rect("1920 x 1080 composition", transform, 0, 0, 1920, 1080);
            layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
            layout.anchoredPosition = Vector2.zero;
            main = Page("Main"); pause = Page("Pause"); death = Page("Death");
            options = Page("Options"); credits = Page("Credits"); confirmation = Page("Confirm");
            mapSelection = Page("Temporary map selection");
            gameOver = Page("Game Over");
            BuildDocument();
            BuildGameOver();
            StandardHeader(main, 0);
            mainFirst = MenuStyle.Button(main.transform, "NewGame", "NOVO JOGO", 164, 505, 640, StartNewGame);
            chooseMap = MenuStyle.Button(main.transform, "TemporaryMapSelect", "ESCOLHER MAPA (TEMP.)", 164, 595, 640, OpenMapSelection);
            var mainOptions = MenuStyle.Button(main.transform, "Options", "OPÇÕES", 164, 685, 640, OpenOptions);
            var creditButton = MenuStyle.Button(main.transform, "Credits", "CRÉDITOS", 164, 775, 640, () => SetScreen(Screen.Credits));
            var quit = MenuStyle.Button(main.transform, "Quit", "SAIR", 164, 865, 640, Quit);
            MenuStyle.Link(mainFirst, chooseMap, mainOptions, creditButton, quit);
            Footer(main);
            BuildMapSelection();
            StandardHeader(pause, 1);
            pauseFirst = MenuStyle.Button(pause.transform, "Resume", "CONTINUAR   [ESC]", 164, 505, 640, Resume);
            var pauseOptions = MenuStyle.Button(pause.transform, "Options", "OPÇÕES", 164, 595, 640, OpenOptions);
            var restart = MenuStyle.Button(pause.transform, "Restart", "REINICIAR CHECKPOINT", 164, 685, 640, RestartCheckpoint);
            var returnMain = MenuStyle.Button(pause.transform, "MainMenu", "VOLTAR AO MENU", 164, 775, 640, RequestMainMenu);
            MenuStyle.Link(pauseFirst, pauseOptions, restart, returnMain);
            Footer(pause);
            StandardHeader(death, 2);
            deathHint = MenuStyle.Text("Return hint", death.transform, "", 165, 450, 1200, 38, 23, MenuStyle.Muted);
            deathRetry = MenuStyle.Button(death.transform, "Retry", "TENTAR NOVAMENTE", 164, 595, 640, RestartCheckpoint);
            deathBack = MenuStyle.Button(death.transform, "MainMenu", "VOLTAR AO MENU", 164, 685, 640, RequestMainMenu);
            MenuStyle.Link(deathRetry, deathBack);
            Footer(death);
            BuildOptions();
            MenuStyle.Text("Heading", credits.transform, "CRÉDITOS", 200, 230, 1400, 100, 76, MenuStyle.Paper);
            MenuStyle.Text("Game", credits.transform, "UNIDOOM", 205, 365, 1300, 70, 48, MenuStyle.Paper);
            MenuStyle.Text("Project", credits.transform, "Projeto de jogo desenvolvido na Unity.", 205, 460, 1400, 60, 30, MenuStyle.Muted);
            // Credits reflect dependencies actually used; no invented contributor names.
            MenuStyle.Text("Packages", credits.transform, "Unity · Universal Render Pipeline · Cinemachine · TextMesh Pro", 205, 535, 1500, 60, 25, MenuStyle.Muted);
            creditsBack = MenuStyle.Button(credits.transform, "Back", "VOLTAR", 165, 800, 500, Back);
            MenuStyle.Link(creditsBack);
            confirmText = MenuStyle.Text("Warning", confirmation.transform, "", 320, 300, 1350, 220, 66, MenuStyle.Paper);
            confirmText.textWrappingMode = TextWrappingModes.Normal;
            var confirm = MenuStyle.Button(confirmation.transform, "ConfirmReturn", "VOLTAR AO MENU", 320, 650, 600, () => confirmAction?.Invoke());
            cancel = MenuStyle.Button(confirmation.transform, "Cancel", "CANCELAR", 320, 750, 600, Back);
            MenuStyle.Link(cancel, confirm);
            error = MenuStyle.Text("Error", layout, "", 165, 1020, 1580, 50, 22, MenuStyle.Paper);
            fade = MenuStyle.Image("Transition", transform, 0, 0, 0, 0, Color.clear);
            MenuStyle.Stretch(fade.rectTransform);
            Fit();
        }

        GameObject Page(string name) => MenuStyle.Rect(name, layout, 0, 0, 1920, 1080).gameObject;

        void BuildGameOver()
        {
            // Reuse the approved transparent artwork for the title, rule and subtitle.
            // Buttons remain real UI controls so focus, pointer and gamepad feedback work.
            var region = new Rect(.16f, .17f, .68f, .47f);
            Header(gameOver, "GameOverArtwork", region, region.x * 1920, region.y * 1080,
                region.width * 1920, region.height * 1080);
            gameOverRestart = GameOverButton("RestartFromBeginning", "COMEÇAR DO INÍCIO", 724, RestartFromBeginning);
            gameOverBack = GameOverButton("MainMenu", "VOLTAR AO MENU", 838, RequestMainMenu);
            MenuStyle.Link(gameOverRestart, gameOverBack);
            var navigation = MenuStyle.Text("Navigation", gameOver.transform,
                "↑ ↓  NAVEGAR     ENTER  CONFIRMAR", 360, 985, 1200, 45, 23, MenuStyle.Muted);
            navigation.alignment = TextAlignmentOptions.Center;
        }

        Button GameOverButton(string name, string label, float y, UnityEngine.Events.UnityAction action)
        {
            var button = MenuStyle.Button(gameOver.transform, name, label, 548, y, 824, action, 62);
            ((RectTransform)button.transform).sizeDelta = new Vector2(824, 100);
            var text = button.transform.Find("Label").GetComponent<TMP_Text>();
            text.rectTransform.anchoredPosition = new Vector2(40, 0);
            text.rectTransform.sizeDelta = new Vector2(744 / .65f, 100);
            text.alignment = TextAlignmentOptions.Center;
            var marker = (RectTransform)button.transform.Find("Selection");
            marker.anchoredPosition = new Vector2(140, 0);
            marker.sizeDelta = new Vector2(30, 100);
            return button;
        }
        void StandardHeader(GameObject page, int index)
        {
            var region = index switch
            {
                0 => new Rect(.06f, .025f, .42f, .31f),
                1 => new Rect(.06f, .36f, .42f, .29f),
                _ => new Rect(.06f, .65f, .59f, .345f)
            };
            // Identical scale and material treatment across every heading in the shared atlas.
            Header(page, "MenuHeadings", region, 165, index == 2 ? 160 : 180,
                700 * region.width / .42f, 240 * region.height / .31f);
            MenuStyle.Image("Header rule", page.transform, 165, 438, 160, 3, MenuStyle.Rust);
        }
        void Header(GameObject page, string resource, Rect region, float x, float y, float w, float h)
        {
            var sprite = MenuStyle.Artwork(resource, region);
            artwork.Add(sprite);
            var image = MenuStyle.Image("Approved artwork", page.transform, x, y, w, h, Color.white);
            image.sprite = sprite;
            image.preserveAspect = false;
        }
        static void Footer(GameObject page) => MenuStyle.Text("Navigation", page.transform,
            "↑ ↓  NAVEGAR     ENTER  CONFIRMAR", 165, 985, 1000, 45, 21, MenuStyle.Muted);

        // Temporary shortcut: use the same fresh-session loading flow as New Game.
        void BuildMapSelection()
        {
            MenuStyle.Text("Title", mapSelection.transform, "ESCOLHER MAPA", 165, 135, 1400, 100, 72, MenuStyle.Paper);
            MenuStyle.Text("Hint", mapSelection.transform, "Selecione onde começar uma nova partida.", 165, 245, 1400, 45, 26, MenuStyle.Muted);
            var controls = new List<Selectable>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scene = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(scene)) continue;
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene);
                string label = sceneName switch
                {
                    "EntranceGarden" => "JARDIM DE ENTRADA",
                    "SampleScene" => "CAMPUS COMPLETO",
                    "Cylinder" => "CILINDRO",
                    "Library" => "BIBLIOTECA",
                    "ClassRoom" => "SALA DE AULA",
                    "DirectorRoom" => "SALA DO DIRETOR",
                    "Basement" => "PORÃO — PRÉ-ARENA",
                    _ => sceneName
                };
                var button = MenuStyle.Button(mapSelection.transform, "Map_" + sceneName, label,
                    165 + controls.Count % 2 * 830, 320 + controls.Count / 2 * 110, 760, () => StartNewGameAtScene(scene), 40);
                if (mapFirst == null) mapFirst = button;
                controls.Add(button);
                if (sceneName == "Basement")
                    controls.Add(MenuStyle.Button(mapSelection.transform, "Map_BasementBoss", "PORÃO — ARENA DO BOSS",
                        165 + controls.Count % 2 * 830, 320 + controls.Count / 2 * 110, 760,
                        () => StartNewGameAtScene(scene, true), 40));
            }
            var back = MenuStyle.Button(mapSelection.transform, "Back", "VOLTAR   [ESC]", 165, 885, 1000, Back, 40);
            if (mapFirst == null) mapFirst = back;
            controls.Add(back);
            MenuStyle.Link(controls.ToArray());
            for (int i = 0; i < controls.Count - 1; i++)
            {
                int rowStart = i / 2 * 2;
                var navigation = new Navigation { mode = Navigation.Mode.Explicit,
                    selectOnUp = i >= 2 ? controls[i - 2] : back,
                    selectOnDown = i + 2 < controls.Count - 1 ? controls[i + 2] : back,
                    selectOnLeft = i % 2 == 1 ? controls[i - 1] : null,
                    selectOnRight = i % 2 == 0 && rowStart + 1 < controls.Count - 1 ? controls[i + 1] : null };
                controls[i].navigation = navigation;
            }
            Footer(mapSelection);
        }

        void BuildOptions()
        {
            MenuStyle.Text("Title", options.transform, "OPÇÕES", 205, 150, 1200, 105, 78, MenuStyle.Paper);
            sensitivity = Slider("Sensibilidade do mouse", 320, .1f, 3, value =>
            { MouseSensitivityMenu.Instance?.SetSensitivity(value); UpdateOptionsLabels(); });
            sensitivityValue = MenuStyle.Text("Sensitivity value", options.transform, "", 1260, 320, 300, 50, 29, MenuStyle.Amber);
            volume = Slider("Volume geral", 455, 0, 1, value =>
            { AudioListener.volume = value; PlayerPrefs.SetFloat(VolumeKey, value); UpdateOptionsLabels(); });
            volumeValue = MenuStyle.Text("Volume value", options.transform, "", 1260, 455, 300, 50, 29, MenuStyle.Amber);
            invertY = Toggle("Inverter eixo Y", 600, value => MouseSensitivityMenu.Instance?.SetInvertY(value));
            fullscreen = Toggle("Tela cheia", 690, value =>
            { UnityEngine.Screen.fullScreen = value; PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0); });
            optionsBack = MenuStyle.Button(options.transform, "Back", "VOLTAR", 165, 850, 500, Back);
            MenuStyle.Link(sensitivity, volume, invertY, fullscreen, optionsBack);
        }

        Slider Slider(string label, float y, float min, float max, UnityEngine.Events.UnityAction<float> change)
        {
            MenuStyle.Text(label, options.transform, label, 205, y, 650, 50, 30, MenuStyle.Paper);
            var root = MenuStyle.Rect(label + " slider", options.transform, 760, y, 450, 50);
            root.gameObject.AddComponent<Image>().color = Color.clear;
            var slider = root.gameObject.AddComponent<Slider>();
            MenuStyle.Image("Track", root, 0, 20, 450, 8, new Color(.18f, .16f, .13f));
            var fillArea = MenuStyle.Rect("Fill area", root, 0, 20, 450, 8);
            var fill = MenuStyle.Image("Fill", fillArea, 0, 0, 0, 0, MenuStyle.Rust);
            MenuStyle.Stretch(fill.rectTransform);
            slider.fillRect = fill.rectTransform;
            var handleArea = MenuStyle.Rect("Handle area", root, 0, 7, 450, 36);
            var handle = MenuStyle.Image("Handle", handleArea, 0, 0, 18, 0, MenuStyle.Paper);
            handle.rectTransform.pivot = new Vector2(.5f, .5f);
            handle.raycastTarget = true;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = min; slider.maxValue = max;
            slider.onValueChanged.AddListener(change);
            return slider;
        }

        Toggle Toggle(string label, float y, UnityEngine.Events.UnityAction<bool> change)
        {
            var root = MenuStyle.Rect(label, options.transform, 205, y, 900, 55);
            var toggle = root.gameObject.AddComponent<Toggle>();
            var bg = MenuStyle.Image("Box", root, 555, 8, 40, 40, new Color(.2f, .18f, .14f));
            bg.raycastTarget = true;
            var tick = MenuStyle.Image("Enabled", bg.transform, 8, 8, 24, 24, MenuStyle.Amber);
            toggle.targetGraphic = bg; toggle.graphic = tick;
            var text = MenuStyle.Text("Label", root, label, 0, 0, 530, 55, 30, MenuStyle.Paper);
            text.raycastTarget = true;
            toggle.onValueChanged.AddListener(change);
            return toggle;
        }

        void UpdateOptionsLabels()
        {
            if (sensitivityValue != null) sensitivityValue.text = $"{sensitivity.value:0.00}x";
            if (volumeValue != null) volumeValue.text = $"{volume.value * 100:0}%";
        }

        void Quit()
        {
            PlayerPrefs.Save();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void OnRectTransformDimensionsChange() { if (layout != null) Fit(); }
        void Fit()
        {
            var bounds = ((RectTransform)transform).rect;
            if (bounds.width <= 0 || bounds.height <= 0) return;
            layout.localScale = Vector3.one * Mathf.Min(bounds.width / 1920, bounds.height / 1080);
            if (backdrop.texture == null) return;
            float screenAspect = bounds.width / bounds.height, imageAspect = (float)backdrop.texture.width / backdrop.texture.height;
            float uw = Mathf.Min(1, screenAspect / imageAspect), uh = Mathf.Min(1, imageAspect / screenAspect);
            backdrop.uvRect = new Rect((1 - uw) * .5f, (1 - uh) * .5f, uw, uh);
        }

        void OnApplicationFocus(bool focused) { if (!focused && CurrentScreen == Screen.Gameplay) OpenPause(); }
        void OnApplicationQuit() { PlayerPrefs.Save(); }
        void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ReleaseDocumentBackdrop();
            ReleasePause(); RestoreHUD();
            foreach (var sprite in artwork) if (sprite != null) Destroy(sprite);
            Instance = null;
        }
    }
}
