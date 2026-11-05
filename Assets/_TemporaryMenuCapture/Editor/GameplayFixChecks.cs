using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UniDoom.UI;

// Temporary, opt-in regression checks. Does not save scenes, assets or preferences.
[InitializeOnLoad]
public static class GameplayFixChecks
{
    const string Active = "UniDoom.GameplayFixChecks";
    static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../.codex/menus"));
    static readonly List<string> Results = new();
    static int step;
    static double next, deadline;
    static bool backgroundBefore;
    static Vector3 checkpoint;
    static float checkpointPan;
    static AsyncOperation travel;

    static GameplayFixChecks() => EditorApplication.update += Tick;

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        string request = Path.Combine(Folder, "run-gameplay-fix-checks.txt");
        if (!SessionState.GetBool(Active, false))
        {
            if (!File.Exists(request)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Move(request, Path.Combine(Folder, "gameplay-fix-" + DateTime.UtcNow.Ticks + ".consumed"));
            SessionState.SetBool(Active, true);
            EditorApplication.isPlaying = true;
            return;
        }
        if (!EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        if (next == 0) { next = EditorApplication.timeSinceStartup + 7; deadline = next + 240; }
        if (EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Timeout at step " + step);
            Run();
        }
        catch (Exception e) { Results.Add("FAIL: " + e); Finish(); }
    }

    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Results.Add("PASS: " + message);
    }

    static void Advance(double wait = .5)
    {
        step++;
        next = EditorApplication.timeSinceStartup + wait;
        File.WriteAllText(Path.Combine(Folder, "gameplay-fix-progress.txt"), "Step=" + step + "\n" + string.Join("\n", Results));
    }

    static void CursorIs(bool visible, string context)
        => Check(Cursor.visible == visible && Cursor.lockState == (visible ? CursorLockMode.None : CursorLockMode.Locked),
            context + ": cursor " + (visible ? "visible and free" : "hidden and locked"));

    static void Capture(string name)
    {
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        ScreenCapture.CaptureScreenshot(Path.Combine(Folder, name + ".png"));
    }

    static void CheckLoading()
    {
        Check(DoorReloadScreen.IsReloading, "Retry displays the existing loading screen");
        var image = DoorReloadScreen.Instance.GetComponentsInChildren<RawImage>(true).Single(x => x.name == "The other side");
        Check(image.texture == Resources.Load<Texture2D>("Textures/UI/Transitions/EntranceGarden"),
            "Loading artwork belongs to checkpoint destination EntranceGarden");
    }

    static void CheckRestored(PlayerMovement player)
    {
        var life = player.GetComponent<PlayerLife>();
        Check(!life.IsDead && life.CurrentHealth == life.MaxHealth, "Checkpoint restores living player with full health");
        Check(Time.timeScale == 1 && player.MovementEnabled, "Checkpoint restores time and movement");
        Check(Vector3.Distance(player.transform.position, checkpoint) < 1, "Checkpoint restores saved position");
        Check(Mathf.Abs(Mathf.DeltaAngle(player.GetComponentInChildren<CinemachinePanTilt>().PanAxis.Value, checkpointPan)) < 1,
            "Checkpoint restores saved camera facing");
        Check(player.GetComponentsInChildren<CinemachineInputAxisController>().All(x => x.enabled), "Camera input resumes after checkpoint loading");
        CursorIs(false, "After checkpoint loading");
    }

    static void Run()
    {
        var menu = GameMenus.Instance;
        var player = GameManager.SessionPlayer;
        switch (step)
        {
            case 0:
                backgroundBefore = Application.runInBackground;
                Application.runInBackground = true;
                CursorIs(true, "Main menu");
                menu.StartNewGame();
                Advance(2);
                break;
            case 1:
                if (menu.IsBusy || player == null) return;
                Check(SceneManager.GetActiveScene().name == "EntranceGarden", "New game starts in EntranceGarden");
                CursorIs(false, "New game after all fades");
                var facade = UnityEngine.Object.FindObjectsByType<CampusEntranceAnchor>().Single(x => x.exterior);
                Vector3 toward = facade.transform.position - player.transform.position;
                Vector3 forward = Camera.main.transform.forward;
                toward.y = forward.y = 0;
                Check(Vector3.Dot(toward.normalized, forward.normalized) > .98f, "Initial camera faces the campus building facade");
                Check(Vector3.Dot(toward.normalized, player.transform.forward) > .98f, "Player root faces the campus building facade");
                Capture("06-spawn-frente-predio");
                Advance(1);
                break;
            case 2:
                menu.OpenPause();
                Advance();
                break;
            case 3:
                CursorIs(true, "Pause menu");
                menu.Resume();
                Advance();
                break;
            case 4:
                CursorIs(false, "Resume gameplay");
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Advance();
                break;
            case 5:
                CursorIs(false, "Gameplay recovers an unexpectedly released cursor");
                var respawn = player.GetComponent<PlayerRespawn>();
                respawn.SaveCheckpoint(0);
                checkpoint = respawn.RespawnPosition;
                checkpointPan = player.GetComponentInChildren<CinemachinePanTilt>().PanAxis.Value;
                player.GetComponent<PlayerLife>().TakeDamage(10000);
                Advance(1.5);
                break;
            case 6:
                CursorIs(true, "Death screen");
                menu.RestartCheckpoint();
                Advance(.65);
                break;
            case 7:
                CheckLoading();
                Capture("07-carregamento-checkpoint-mesmo-local");
                Advance(3);
                break;
            case 8:
                if (DoorReloadScreen.IsReloading) return;
                CheckRestored(player);
                player.MovementEnabled = false;
                travel = SceneManager.LoadSceneAsync("Cylinder");
                Advance();
                break;
            case 9:
                if (!travel.isDone) return;
                Check(SceneManager.GetActiveScene().name == "Cylinder", "Player travels away from saved checkpoint scene");
                player.MovementEnabled = true;
                player.GetComponent<PlayerLife>().TakeDamage(10000);
                if (!player.GetComponent<PlayerLife>().IsDead) return; // Respect respawn protection.
                Advance(1.5);
                break;
            case 10:
                CursorIs(true, "Death in another scene");
                menu.RestartCheckpoint();
                Advance(.65);
                break;
            case 11:
                CheckLoading();
                Capture("08-carregamento-checkpoint-outro-local");
                Advance(3);
                break;
            case 12:
                if (DoorReloadScreen.IsReloading) return;
                Check(SceneManager.GetActiveScene().name == "EntranceGarden", "Cross-scene retry returns to checkpoint scene");
                CheckRestored(player);
                menu.OpenPause();
                Advance();
                break;
            case 13:
                menu.RestartCheckpoint();
                Advance(.65);
                break;
            case 14:
                CheckLoading();
                Advance(3);
                break;
            case 15:
                if (DoorReloadScreen.IsReloading) return;
                CheckRestored(player);
                Finish();
                break;
        }
    }

    static void Finish()
    {
        Application.runInBackground = backgroundBefore;
        File.WriteAllLines(Path.Combine(Folder, "validacao-correcoes-gameplay.txt"), Results);
        SessionState.SetBool(Active, false);
        EditorApplication.isPlaying = false;
    }
}
