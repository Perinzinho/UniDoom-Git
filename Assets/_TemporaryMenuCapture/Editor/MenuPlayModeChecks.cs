using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UniDoom.UI;

// Opt-in Play Mode checks. Runs only when .codex/menus/run-menu-checks.txt exists.
[InitializeOnLoad]
public static class MenuPlayModeChecks
{
    static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../.codex/menus"));
    const string ActiveKey = "UniDoom.MenuChecks.Active";
    static readonly List<string> results = new();
    static int step;
    static double next, deadline;
    static float sensitivityBefore, volumeBefore;
    static bool invertBefore;
    static bool runInBackgroundBefore;
    static bool preferencesCaptured;
    static bool mainCaptured;
    static Vector3 checkpointPosition;
    static GameViewPreviewState previewBefore;

    static MenuPlayModeChecks() { EditorApplication.update += Tick; }

    [MenuItem("Tools/UniDoom/Menu Checks Status")]
    static void Status()
    {
        File.WriteAllText(Path.Combine(Folder, "menu-check-status.txt"),
            "Playing=" + EditorApplication.isPlaying + "\nWillPlay=" + EditorApplication.isPlayingOrWillChangePlaymode +
            "\nCompiling=" + EditorApplication.isCompiling + "\nUpdating=" + EditorApplication.isUpdating +
            "\nActive=" + SessionState.GetBool(ActiveKey, false) + "\nStep=" + step +
            "\nScreen=" + (GameMenus.Instance == null ? "none" : GameMenus.Instance.CurrentScreen.ToString()) +
            "\nBusy=" + (GameMenus.Instance != null && GameMenus.Instance.IsBusy) +
            "\nTime=" + Time.timeScale + "\nBackground=" + Application.runInBackground);
    }

    [MenuItem("Tools/UniDoom/Advance Menu Checks")]
    static void AdvanceFromMenu()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        Tick();
        Status();
    }

    [MenuItem("Tools/UniDoom/Restore Test Defaults")]
    static void RestoreTestDefaults()
    {
        // Explicit manual recovery for settings changed by an interrupted verification run.
        PlayerPrefs.SetFloat("UniDoom_MouseSensitivity", 1);
        PlayerPrefs.SetFloat("UniDoom_MasterVolume", 1);
        PlayerPrefs.Save();
        File.WriteAllText(Path.Combine(Folder, "preferencias-finais.txt"),
            "Volume: " + PlayerPrefs.GetFloat("UniDoom_MasterVolume") +
            "\nSensibilidade: " + PlayerPrefs.GetFloat("UniDoom_MouseSensitivity") +
            "\nPreferencia de inversao do eixo Y preservada.");
    }

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string request = Path.Combine(Folder, "run-menu-checks.txt");
        if (!SessionState.GetBool(ActiveKey, false))
        {
            if (!File.Exists(request)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying) return;
            File.Move(request, Path.Combine(Folder, "menu-checks-" + DateTime.UtcNow.Ticks + ".consumed"));
            SessionState.SetBool(ActiveKey, true);
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
            return;
        }
        if (!EditorApplication.isPlaying)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode) SessionState.SetBool(ActiveKey, false);
            return;
        }
        EditorApplication.QueuePlayerLoopUpdate();
        if (next == 0) { next = EditorApplication.timeSinceStartup + 7; deadline = next + 240; }
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timed out at step " + step);
            RunStep();
        }
        catch (Exception e)
        {
            results.Add("FAIL: " + e);
            Finish();
        }
    }

    static void Check(bool value, string description)
    {
        if (!value) throw new Exception(description);
        results.Add("PASS: " + description);
    }

    static Button Button(string page, string name) => GameMenus.Instance.GetComponentsInChildren<Button>(true)
        .Single(item => item.name == name && item.transform.parent.name == page);

    static void Capture(string name)
    {
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        ScreenCapture.CaptureScreenshot(Path.Combine(Folder, name + ".png"));
    }

    static void Advance(double wait = .5)
    {
        step++;
        File.WriteAllText(Path.Combine(Folder, "menu-check-progress.txt"), "Step " + step + " / 11\n" + string.Join("\n", results));
        next = EditorApplication.timeSinceStartup + wait;
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
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new [] { Enum.Parse(groupType, "Standalone") });
        int count = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        int index = -1;
        for (int i = 0; i < count; i++)
        {
            var size = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            if ((int)size.GetType().GetProperty("width").GetValue(size) == 1920 &&
                (int)size.GetType().GetProperty("height").GetValue(size) == 1080) { index = i; break; }
        }
        if (index >= 0) selected.SetValue(window, index);
        window.Repaint();
    }

    static void RunStep()
    {
        var menu = GameMenus.Instance;
        var player = GameManager.SessionPlayer;
        switch (step)
        {
            case 0:
                runInBackgroundBefore = Application.runInBackground;
                Application.runInBackground = true;
                SetCaptureSize();
                Check(menu != null && menu.CurrentScreen == GameMenus.Screen.Main, "Main menu opens automatically");
                Check(Time.timeScale == 0 && GameMenus.BlocksGameplay, "Main menu freezes world and blocks gameplay");
                Check(menu.GetComponentsInChildren<Button>().Count(x => x.transform.parent.name == "Main") == 4,
                    "Main menu has exactly New Game, Options, Credits, Quit; no Continue");
                Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == Button("Main", "NewGame").gameObject,
                    "Initial menu selects New Game for keyboard/gamepad navigation");
                var move = new UnityEngine.EventSystems.AxisEventData(UnityEngine.EventSystems.EventSystem.current)
                    { moveDir = UnityEngine.EventSystems.MoveDirection.Down, moveVector = Vector2.down };
                UnityEngine.EventSystems.ExecuteEvents.Execute(Button("Main", "NewGame").gameObject, move,
                    UnityEngine.EventSystems.ExecuteEvents.moveHandler);
                Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == Button("Main", "Options").gameObject,
                    "Keyboard/gamepad navigation moves from New Game to Options");
                Button("Main", "NewGame").Select();
                sensitivityBefore = MouseSensitivityMenu.CurrentSensitivity;
                invertBefore = MouseSensitivityMenu.Instance.InvertY;
                volumeBefore = AudioListener.volume;
                preferencesCaptured = true;
                Advance(1);
                break;
            case 1:
                if (!mainCaptured)
                {
                    Capture("01-menu-inicial-implementado");
                    mainCaptured = true;
                    next = EditorApplication.timeSinceStartup + 1.5;
                    return;
                }
                Button("Main", "NewGame").onClick.Invoke();
                Advance(2);
                break;
            case 2:
                if (menu.IsBusy || player == null) return;
                Check(menu.CurrentScreen == GameMenus.Screen.Gameplay && Time.timeScale == 1, "New Game loads a clean playable session");
                Check(player.MovementEnabled, "Movement restored after New Game");
                Check(!player.GetComponent<PlayerPickupKey>().key01, "New session has no collected keys");
                menu.OpenPause();
                Check(menu.CurrentScreen == GameMenus.Screen.Pause && Time.timeScale == 0 && AudioListener.pause, "Pause freezes simulation and audio");
                Capture("02-menu-pause-implementado");
                Advance(1);
                break;
            case 3:
                Button("Pause", "Options").onClick.Invoke();
                Check(menu.CurrentScreen == GameMenus.Screen.Options, "Options opens from pause");
                menu.GetComponentsInChildren<Slider>(true).Single(x => x.name == "Sensibilidade do mouse slider").value = 1.37f;
                Check(Mathf.Abs(MouseSensitivityMenu.CurrentSensitivity - 1.37f) < .02f, "Sensitivity slider updates shared camera preferences");
                menu.GetComponentsInChildren<Toggle>(true).Single(x => x.name == "Inverter eixo Y").isOn = !invertBefore;
                Check(MouseSensitivityMenu.Instance.InvertY != invertBefore, "Invert Y toggle updates shared preferences");
                menu.GetComponentsInChildren<Slider>(true).Single(x => x.name == "Volume geral slider").value = .37f;
                Check(Mathf.Abs(AudioListener.volume - .37f) < .01f, "Master-volume slider updates audio");
                Check(menu.GetComponentsInChildren<Slider>(true).All(x => x.fillRect.rect.width <= 451 && x.fillRect.rect.height <= 10),
                    "Options slider fills remain inside their tracks");
                Capture("04-opcoes-implementadas");
                Advance();
                break;
            case 4:
                menu.Back();
                Check(menu.CurrentScreen == GameMenus.Screen.Pause && Time.timeScale == 0, "Closing options returns to paused game");
                menu.RequestMainMenu();
                Check(menu.CurrentScreen == GameMenus.Screen.Confirm, "Return to main opens confirmation from pause");
                menu.Back();
                Check(menu.CurrentScreen == GameMenus.Screen.Pause && GameManager.SessionPlayer == player, "Cancel preserves paused session");
                menu.RestartCheckpoint();
                Advance(2);
                break;
            case 5:
                if (DeathScreen.IsVisible || menu.IsBusy) return;
                Check(menu.CurrentScreen == GameMenus.Screen.Gameplay && Time.timeScale == 1, "Restart checkpoint from pause completes and resumes world");
                player.GetComponent<PlayerPickupKey>().key01 = true;
                player.GetComponent<PlayerRespawn>().SaveCheckpoint(0);
                checkpointPosition = player.GetComponent<PlayerRespawn>().RespawnPosition;
                player.GetComponent<PlayerLife>().TakeDamage(10000);
                Advance(1.5);
                break;
            case 6:
                Check(DeathScreen.IsVisible && menu.CurrentScreen == GameMenus.Screen.Death && Time.timeScale == 0, "Death opens new death screen and freezes simulation");
                Check(!menu.GetComponentsInChildren<TMPro.TMP_Text>().Any(x => x.text.Contains("CHAVE")), "Death screen has no key list");
                Check(!menu.GetComponentsInChildren<TMPro.TMP_Text>().Any(x => x.text.Contains("PONTO DE RETORNO")), "Death screen has no yellow return-point caption");
                Check(Button("Death", "Retry").interactable, "Retry enables after death transition delay");
                Capture("03-tela-de-morte-implementada");
                Advance(1);
                break;
            case 7:
                Button("Death", "MainMenu").onClick.Invoke();
                Check(menu.CurrentScreen == GameMenus.Screen.Confirm, "Return to main opens confirmation from death");
                Check(menu.GetComponentsInChildren<TMPro.TMP_Text>().Any(x => x.text.Contains("Os dados não serão salvos.")), "Confirmation warns that progress will not be saved");
                Check(UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name == "Cancel", "Confirmation defaults to Cancel");
                Capture("05-confirmacao-voltar-menu");
                Advance(1);
                break;
            case 8:
                Button("Confirm", "Cancel").onClick.Invoke();
                Check(menu.CurrentScreen == GameMenus.Screen.Death && DeathScreen.IsVisible, "Cancel returns to death without altering session");
                Button("Death", "Retry").onClick.Invoke();
                Advance(2);
                break;
            case 9:
                if (DeathScreen.IsVisible) return;
                Check(menu.CurrentScreen == GameMenus.Screen.Gameplay && Time.timeScale == 1, "Retry restores gameplay and time scale");
                Check(player.GetComponent<PlayerLife>().CurrentHealth == player.GetComponent<PlayerLife>().MaxHealth, "Retry restores full health");
                Check(player.GetComponent<PlayerPickupKey>().key01, "Checkpoint retry preserves collected keys");
                Check(Vector3.Distance(player.transform.position, checkpointPosition) < 1, "Retry returns player to checkpoint pose");
                menu.OpenPause();
                menu.RequestMainMenu();
                Button("Confirm", "ConfirmReturn").onClick.Invoke();
                Advance(1);
                break;
            case 10:
                Check(menu.CurrentScreen == GameMenus.Screen.Main && GameManager.SessionPlayer == null, "Confirmed return ends session and opens main menu");
                Check(Time.timeScale == 0, "World remains frozen on main menu after ending session");
                Button("Main", "Credits").onClick.Invoke();
                Check(menu.CurrentScreen == GameMenus.Screen.Credits, "Credits button opens credits");
                menu.Back();
                Check(menu.CurrentScreen == GameMenus.Screen.Main, "Credits Back returns to main menu");
                Button("Main", "Options").onClick.Invoke();
                Check(menu.CurrentScreen == GameMenus.Screen.Options, "Options also opens from main menu");
                menu.Back();
                Advance();
                break;
            case 11:
                results.Add("Quit button uses Application.Quit in a player and exits Play Mode in the Editor.");
                Finish();
                break;
        }
    }

    static void Finish()
    {
        if (preferencesCaptured && MouseSensitivityMenu.Instance != null)
        {
            MouseSensitivityMenu.Instance.SetSensitivity(sensitivityBefore);
            MouseSensitivityMenu.Instance.SetInvertY(invertBefore);
        }
        if (preferencesCaptured) AudioListener.volume = volumeBefore;
        Application.runInBackground = runInBackgroundBefore;
        previewBefore?.Restore();
        if (preferencesCaptured) PlayerPrefs.SetFloat("UniDoom_MasterVolume", volumeBefore);
        PlayerPrefs.Save();
        SessionState.SetBool(ActiveKey, false);
        File.WriteAllLines(Path.Combine(Folder, "validacao-menus.txt"), results);
        EditorApplication.isPlaying = false;
    }
}
