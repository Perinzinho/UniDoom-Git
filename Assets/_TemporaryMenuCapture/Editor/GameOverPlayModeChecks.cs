using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniDoom.UI;

// Opt-in checks for the approved Game Over menu. Never saves scenes or preferences.
[InitializeOnLoad]
public static class GameOverPlayModeChecks
{
    const string ActiveKey = "UniDoom.GameOverChecks.Active";
    static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../.codex/menus"));
    static readonly List<string> results = new();
    static int step, deaths;
    static GameViewPreviewState previewBefore;
    static double next, deadline;
    static bool backgroundBefore, initialized, restartChecked;
    static float volumeBefore, sensitivityBefore;
    static bool invertBefore;
    static Vector3 checkpoint;
    static PlayerMovement exhaustedPlayer;

    static GameOverPlayModeChecks()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.ExitingPlayMode || !SessionState.GetBool(ActiveKey, false)) return;
            results.Add("INTERRUPTED: Editor exited Play Mode before checks completed at step " + step);
            Finish();
        };
    }

    [MenuItem("Tools/UniDoom/Game Over Checks Status")]
    public static void Status() => File.WriteAllText(Path.Combine(Folder, "game-over-check-status.txt"),
        "Playing=" + EditorApplication.isPlaying + "\nPaused=" + EditorApplication.isPaused +
        "\nActive=" + SessionState.GetBool(ActiveKey, false) + "\nStep=" + step +
        "\nScreen=" + (GameMenus.Instance == null ? "none" : GameMenus.Instance.CurrentScreen.ToString()) +
        "\nLoading=" + DoorReloadScreen.IsReloading + "\nTime=" + Time.timeScale);

    [MenuItem("Tools/UniDoom/Validate Game Over")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Start Game Over validation from Edit Mode.");
        SessionState.SetBool(ActiveKey, true);
        EditorApplication.isPlaying = true;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
            !EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        if (next == 0) { next = EditorApplication.timeSinceStartup + 7; deadline = next + 600; }
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout at step " + step);
            Run();
        }
        catch (Exception e) { results.Add("FAIL: " + e); Finish(); }
    }

    static void Check(bool success, string message)
    {
        if (!success) throw new Exception(message);
        results.Add("PASS: " + message);
    }

    static void Go(int target, double delay = .5)
    {
        step = target;
        next = EditorApplication.timeSinceStartup + delay;
        File.WriteAllText(Path.Combine(Folder, "game-over-check-progress.txt"),
            "Step=" + step + " Deaths=" + deaths + " RestartChecked=" + restartChecked + "\n" + string.Join("\n", results));
    }

    static Button Button(string page, string name) => GameMenus.Instance.GetComponentsInChildren<Button>(true)
        .Single(x => x.name == name && x.transform.parent.name == page);

    static void Click(Button button)
    {
        Check(button.IsInteractable() && button.gameObject.activeInHierarchy, button.name + " accepts pointer input");
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
    }

    static void SetCaptureSize()
    {
        previewBefore = GameViewPreviewState.Capture();
        var assembly = typeof(Editor).Assembly;
        var type = assembly.GetType("UnityEditor.GameView");
        var window = EditorWindow.GetWindow(type);
        var selected = type.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        var sizes = sizesType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).GetValue(null);
        var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
        for (int i = 0; i < (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null); i++)
        {
            var size = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            if ((int)size.GetType().GetProperty("width").GetValue(size) == 1920 &&
                (int)size.GetType().GetProperty("height").GetValue(size) == 1080)
            { selected.SetValue(window, i); break; }
        }
        window.Repaint();
    }

    static void Run()
    {
        var menu = GameMenus.Instance;
        var player = GameManager.SessionPlayer;
        var respawn = player != null ? player.GetComponent<PlayerRespawn>() : null;
        switch (step)
        {
            case 0:
                Check(menu != null && menu.CurrentScreen == GameMenus.Screen.Main, "Main menu starts normally");
                backgroundBefore = Application.runInBackground;
                Application.runInBackground = true;
                volumeBefore = AudioListener.volume;
                sensitivityBefore = MouseSensitivityMenu.CurrentSensitivity;
                invertBefore = MouseSensitivityMenu.Instance.InvertY;
                SetCaptureSize();
                initialized = true;
                menu.StartNewGame();
                Go(1, 2);
                break;
            case 1:
                if (menu.IsBusy || player == null) return;
                Check(respawn.RemainingLives == 3 && !player.GetComponent<PlayerLife>().IsDead, "New session begins alive with three lives");
                Check(menu.CurrentScreen == GameMenus.Screen.Gameplay && player.MovementEnabled && Time.timeScale == 1,
                    "New session restores gameplay and movement");
                Go(2);
                break;
            case 2:
                deaths = 0;
                player.GetComponent<PlayerPickupKey>().key01 = true;
                respawn.SaveCheckpoint(0);
                checkpoint = respawn.RespawnPosition;
                player.GetComponent<PlayerLife>().TakeDamage(10000);
                deaths++;
                Go(3, 1.4);
                break;
            case 3:
                Check(respawn.RemainingLives == 3 - deaths, "Death consumes exactly one life: " + deaths);
                if (!respawn.HasLives) { Go(6); break; }
                Check(menu.CurrentScreen == GameMenus.Screen.Death && DeathScreen.Instance.CanRespawn,
                    "Remaining lives use ordinary death screen with checkpoint retry");
                Click(Button("Death", "Retry"));
                Go(4, 3);
                break;
            case 4:
                if (DoorReloadScreen.IsReloading || DeathScreen.IsVisible) return;
                Check(!player.GetComponent<PlayerLife>().IsDead && respawn.RemainingLives == 3 - deaths,
                    "Checkpoint restores life without resetting remaining lives");
                Check(Vector3.Distance(checkpoint, player.transform.position) < 1 && player.GetComponent<PlayerPickupKey>().key01,
                    "Checkpoint retry preserves saved position and collected key");
                Go(5, 2.2); // Let the real respawn protection expire before the next fatal hit.
                break;
            case 5:
                player.GetComponent<PlayerLife>().TakeDamage(10000);
                Check(player.GetComponent<PlayerLife>().IsDead, "Next fatal hit reaches death after protection expires");
                deaths++;
                Go(3, 1.4);
                break;
            case 6:
                Check(menu.CurrentScreen == GameMenus.Screen.GameOver && respawn.RemainingLives == 0,
                    "Third death opens dedicated Game Over screen at zero lives");
                Check(GameMenus.BlocksGameplay && Time.timeScale == 0 && !player.MovementEnabled,
                    "Game Over freezes simulation and movement");
                Check(player.GetComponentsInChildren<CinemachineInputAxisController>().All(x => !x.enabled), "Game Over freezes camera input");
                Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "Game Over releases cursor for buttons");
                Check(!DeathScreen.Instance.CanRespawn, "Zero lives cannot retry checkpoint");
                menu.RestartCheckpoint();
                Check(menu.CurrentScreen == GameMenus.Screen.GameOver && player.GetComponent<PlayerLife>().IsDead && !DoorReloadScreen.IsReloading,
                    "Direct checkpoint action cannot bypass Game Over");
                var page = menu.GetComponentsInChildren<RectTransform>(true).Single(x => x.name == "Game Over");
                Check(page.GetComponentsInChildren<Button>().Length == 2, "Game Over has exactly Restart From Beginning and Main Menu");
                Check(!page.GetComponentsInChildren<TMP_Text>().Any(x => x.text.Contains("Todas as vidas") || x.text.Contains("CHAVE")),
                    "Approved Game Over has no deleted sentence or key inventory");
                var background = menu.GetComponentsInChildren<RawImage>().Single(x => x.name == "Real campus photograph");
                var canvas = menu.GetComponent<RectTransform>();
                Check(background.texture != null && background.rectTransform.rect.size == canvas.rect.size,
                    "Real game background fills the entire canvas edge to edge");
                var restart = Button("Game Over", "RestartFromBeginning");
                Check(EventSystem.current.currentSelectedGameObject == restart.gameObject, "Game Over defaults to Restart From Beginning");
                var pointer = new PointerEventData(EventSystem.current)
                { position = RectTransformUtility.WorldToScreenPoint(null, restart.transform.TransformPoint(((RectTransform)restart.transform).rect.center)) };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == restart,
                    "Visible restart button is reachable by pointer raycast");
                if (!restartChecked)
                {
                    EditorApplication.ExecuteMenuItem("Window/General/Game");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "10-fim-de-jogo-implementado.png"));
                }
                Go(7, 1.5);
                break;
            case 7:
                var down = new AxisEventData(EventSystem.current) { moveDir = MoveDirection.Down, moveVector = Vector2.down };
                ExecuteEvents.Execute(Button("Game Over", "RestartFromBeginning").gameObject, down, ExecuteEvents.moveHandler);
                Check(EventSystem.current.currentSelectedGameObject == Button("Game Over", "MainMenu").gameObject,
                    "Keyboard/gamepad navigation reaches Main Menu");
                menu.Back();
                Check(menu.CurrentScreen == GameMenus.Screen.GameOver, "Escape cannot resume an exhausted session");
                Click(Button("Game Over", "MainMenu"));
                Check(menu.CurrentScreen == GameMenus.Screen.Confirm && EventSystem.current.currentSelectedGameObject.name == "Cancel",
                    "Return to menu asks confirmation and defaults to Cancel");
                Click(Button("Confirm", "Cancel"));
                Check(menu.CurrentScreen == GameMenus.Screen.GameOver && respawn.RemainingLives == 0,
                    "Cancel returns to Game Over without reviving or changing lives");
                if (restartChecked)
                {
                    Click(Button("Game Over", "MainMenu"));
                    Click(Button("Confirm", "ConfirmReturn"));
                    Go(9, 1);
                }
                else
                {
                    exhaustedPlayer = player;
                    Click(Button("Game Over", "RestartFromBeginning"));
                    Check(menu.IsBusy, "Restart button starts new-game transition");
                    Go(8, 2);
                }
                break;
            case 8:
                if (menu.IsBusy || player == null) return;
                Check(exhaustedPlayer == null && respawn.RemainingLives == 3 && !respawn.HasCheckpoint,
                    "Restart replaces exhausted session with three lives and no saved checkpoint");
                Check(!player.GetComponent<PlayerPickupKey>().key01 && player.GetComponent<PlayerLife>().CurrentHealth == player.GetComponent<PlayerLife>().MaxHealth,
                    "Restart clears collected keys and restores full health");
                Check(SceneManager.GetActiveScene().path == SceneUtility.GetScenePathByBuildIndex(0), "Restart begins in the game's first scene");
                Check(Time.timeScale == 1 && player.MovementEnabled && !Cursor.visible && Cursor.lockState == CursorLockMode.Locked,
                    "Restart restores gameplay, movement and locked hidden cursor");
                Check(UnityEngine.Object.FindObjectsByType<GameManager>().Length == 1 && UnityEngine.Object.FindObjectsByType<PlayerRespawn>().Length == 1,
                    "Restart leaves one session and one player");
                restartChecked = true;
                Go(2);
                break;
            case 9:
                Check(menu.CurrentScreen == GameMenus.Screen.Main && GameManager.SessionPlayer == null,
                    "Confirmed return destroys exhausted session and opens main menu");
                Check(Time.timeScale == 0 && Cursor.visible && Cursor.lockState == CursorLockMode.None, "Main menu retains paused world and free cursor");
                Check(AudioListener.volume == volumeBefore && MouseSensitivityMenu.CurrentSensitivity == sensitivityBefore &&
                    MouseSensitivityMenu.Instance.InvertY == invertBefore, "Menu flows preserve volume, sensitivity and inversion preferences");
                Finish();
                break;
        }
    }

    static void Finish()
    {
        SessionState.SetBool(ActiveKey, false);
        File.WriteAllLines(Path.Combine(Folder, "validacao-fim-de-jogo.txt"), results);
        if (initialized)
        {
            Application.runInBackground = backgroundBefore;
            previewBefore?.Restore();
        }
        EditorApplication.isPlaying = false;
    }
}
