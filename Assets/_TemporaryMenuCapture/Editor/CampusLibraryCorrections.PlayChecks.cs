using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UniDoom.CampusLibrary;
using UniDoom.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class CampusLibraryCorrectionPlayChecks
{
    const string Active = "UniDoom.LibraryCorrections.Play";
    const string Report = "Logs/CampusLibraryCorrections-play";
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    sealed class Trial { public string label; public Action begin, verify; public Func<bool> ready; public float timeout; }
    static readonly List<Trial> trials = new();
    static readonly List<string> results = new();
    static LibraryMissionController mission;
    static LibraryZombieSpawner spawner;
    static PlayerMovement player;
    static Camera camera;
    static Keyboard keyboard;
    static InputSettings.BackgroundBehavior background;
    static InputSettings.EditorInputBehaviorInPlayMode editorInput;
    static int index = -1, frame;
    static double deadline, at, trialDeadline;
    static float gameAt;
    static bool loading, started, captured, finishing;
    static GameObject obstruction;
    static Enemy victim;
    static float killedAt;

    static CampusLibraryCorrectionPlayChecks() => EditorApplication.update += Tick;
    public static void Begin()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity", OpenSceneMode.Single);
        SessionState.SetBool(Active, true); EditorApplication.isPlaying = true;
    }
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Add(string label, Action begin, Action verify, Func<bool> ready = null, float timeout = 15)
        => trials.Add(new Trial { label = label, begin = begin, verify = verify, ready = ready, timeout = timeout });
    static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || finishing || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!EditorApplication.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate(); Application.runInBackground = true;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 600;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Overall timeout, trial " + index);
            if (!loading)
            {
                if (!GameMenus.Instance || GameMenus.Instance.IsBusy) return;
                loading = true;
                typeof(GameMenus).GetMethod("StartNewGameAtScene", Hidden).Invoke(GameMenus.Instance,
                    new object[] { "Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity", false, false, false });
                return;
            }
            if (!started)
            {
                if (!LibraryMissionController.GameplayAvailable || GameMenus.Instance.IsBusy) return;
                started = true; BuildTrials();
            }
            if (index < 0) { index = 0; StartTrial(); return; }
            if (Time.frameCount < frame + 3 || EditorApplication.timeSinceStartup < at || (Time.timeScale > 0 && Time.time < gameAt)) return;
            var trial = trials[index];
            if (EditorApplication.timeSinceStartup > trialDeadline) throw new Exception("Timeout: " + trial.label);
            if (trial.ready != null && !trial.ready()) return;
            trial.verify?.Invoke(); results.Add("PASS: " + trial.label);
            File.WriteAllLines(Report + "-progress.txt", results);
            index++; if (index == trials.Count) { Finish(true); return; } StartTrial();
        }
        catch (Exception e) { results.Add("FAIL: " + e); Finish(false); }
    }
    static void StartTrial()
    {
        File.WriteAllText(Report + "-state.txt", index + ": " + trials[index].label);
        trials[index].begin?.Invoke(); frame = Time.frameCount; at = EditorApplication.timeSinceStartup + .25; gameAt = Time.time + .3f;
        trialDeadline = EditorApplication.timeSinceStartup + trials[index].timeout;
    }
    static void Aim(LibraryMissionTarget target, Vector3? stance = null)
    {
        var position = stance ?? (target.transform.position + new Vector3(0, 0, -1.9f)); position.y = 1.07f;
        if (target.Kind == LibraryTargetKind.Fuse && target.Id == 2) position.y += .64f;
        var direction = target.transform.position - position; direction.y = 0;
        var rotation = Quaternion.LookRotation(direction);
        player.Teleport(position, rotation); camera.transform.rotation = rotation; Physics.SyncTransforms();
    }
    static LibraryMissionTarget Focus() => (LibraryMissionTarget)typeof(LibraryMissionController).GetMethod("Focus", Hidden).Invoke(mission, null);
    static void Press() => InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
    static void Release() => InputSystem.QueueStateEvent(keyboard, new KeyboardState());
    static void Use(string label, LibraryMissionTarget target, Action verify, Vector3? stance = null)
    {
        Add("Horizontal focus: " + label, () => Aim(target, stance), () =>
        {
            Check(Mathf.Abs(camera.transform.forward.y) < .001f, "Camera was tilted");
            Check(Focus() == target, "Wrong horizontal target for " + label + ": " + (Focus() ? Focus().name : "none") + " eye=" + camera.transform.position);
        });
        Add("Keyboard E: " + label, Press, verify);
        Add("Release E: " + label, Release, null);
    }
    static void BuildTrials()
    {
        mission = Object.FindAnyObjectByType<LibraryMissionController>(); spawner = Object.FindAnyObjectByType<LibraryZombieSpawner>();
        player = GameManager.SessionPlayer; camera = Camera.main; player.MovementEnabled = false; spawner.enabled = false;
        foreach (var enemy in Object.FindObjectsByType<Enemy>()) Object.Destroy(enemy.gameObject);
        background = InputSystem.settings.backgroundBehavior; editorInput = InputSystem.settings.editorInputBehaviorInPlayMode; captured = true;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>("Library horizontal input test");
        Add("Undiscovered fuses remain inactive", null, () => Check(mission.Fuses.All(f => !f.gameObject.activeSelf), "Fuses active before discovery"));
        Add("Generator discovered with horizontal view", () => Aim(mission.Generator), () => Check(LibraryMissionProgress.Stage == LibraryMissionStage.SearchingFuses, "Generator did not discover"));
        var fuses = mission.Fuses.OrderBy(f => f.Id).ToArray();
        Add("Approach tabletop for occlusion check", () => Aim(fuses[0]), null);
        Add("Tabletop focus respects walls", () =>
        {
            obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube); obstruction.name = "Temporary line of sight obstruction";
            obstruction.transform.position = (camera.transform.position + fuses[0].transform.position) * .5f; obstruction.transform.localScale = new Vector3(1, 3, .1f); Physics.SyncTransforms();
        }, () => { Check(Focus() != fuses[0], "Pickup through wall"); Object.Destroy(obstruction); });
        for (int i = 0; i < fuses.Length; i++)
        {
            int expected = i + 1; var fuse = fuses[i];
            Use("fuse " + fuse.Id, fuse, () =>
            {
                Check(LibraryMissionProgress.FuseCount == expected && !fuse.gameObject.activeSelf, "Pickup not registered uniquely");
                var notice = GameMenus.Instance.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "Fuse pickup notice");
                Check(notice.gameObject.activeInHierarchy && notice.text.Contains(expected + "/3"), "Immediate pickup feedback missing");
            });
        }
        Add("Third pickup does not install automatically", null, () => Check(LibraryMissionProgress.Stage == LibraryMissionStage.FusesCollected, "Automatic installation"));
        var bank = Object.FindObjectsByType<LibraryMissionTarget>().Single(t => t.Kind == LibraryTargetKind.FuseSocket);
        Use("manual fuse bank", bank, () => Check(LibraryMissionProgress.Stage == LibraryMissionStage.FusesInstalled, "E did not install"), new Vector3(-25.25f, 0, 41));
        var clues = Object.FindObjectsByType<LibraryMissionTarget>().Where(t => t.Kind == LibraryTargetKind.Clue).OrderBy(t => t.Id).ToArray();
        Check(clues.Length == 3, "Expected three physical notes");
        var stances = new[] { new Vector3(-25.9f, 0, 36.3f), new Vector3(-16.5f, 0, 41.6f), new Vector3(-10.7f, 0, 35.5f) };
        foreach (var clue in clues)
        {
            Use("paper note " + clue.Id, clue, () => Check(GameMenus.Instance.IsReadingDocument && Time.timeScale == 0, "Note did not open shared viewer"), stances[clue.Id]);
            Add("Close note " + clue.Id, () => GameMenus.Instance.CloseDocument(), () => Check(LibraryMissionController.GameplayAvailable, "Document did not restore gameplay"));
        }
        var buttons = mission.Buttons.ToDictionary(t => t.Id);
        Use("wrong terminal resets attempt", buttons[1], () => Check(LibraryMissionProgress.SequenceStep == 0 && LibraryMissionProgress.Stage == LibraryMissionStage.FusesInstalled, "Wrong sequence changed progression"));
        int[] sequence = { 3, 1, 5, 2, 7, 4, 6 };
        for (int i = 0; i < sequence.Length; i++)
        {
            int expected = i + 1; var button = buttons[sequence[i]];
            Use("terminal " + button.Id, button, () => Check(LibraryMissionProgress.SequenceStep == expected, "Sequence did not advance"));
        }
        Add("Solved panel awaits separate ignition", null, () => Check(LibraryMissionProgress.Stage == LibraryMissionStage.PuzzleSolved, "Generator started automatically"));
        Add("Capture world presentation after live frames", () => CampusLibraryCorrections.RuntimeVisuals(), null);
        Add("Four safe reinforcements spawn", () =>
        {
            player.Teleport(new Vector3(-21.5f, 1.07f, 36.4f), Quaternion.identity); camera.transform.rotation = Quaternion.identity; spawner.enabled = true;
        }, () =>
        {
            var safe = Object.FindAnyObjectByType<LibraryMaintenanceSafety>();
            Check(Object.FindObjectsByType<LibraryEnemyRestriction>().All(e => !safe.Contains(e.transform.position)), "Enemy inside safe area");
            Check(player.GetComponent<PlayerLife>().CurrentHealth == 100, "Damage inside safe area");
        }, () => spawner.LivingCount == 4, 100);
        Add("Four-enemy cap remains stable", null, () => Check(spawner.LivingCount == 4, "Spawn cap exceeded"));
        Add("Dead enemy is replaced after delay", () =>
        {
            victim = Object.FindObjectsByType<Enemy>().First(e => !e.IsDead); victim.TakeDamage(100000); killedAt = Time.time;
        }, () => Check(Time.time - killedAt >= 4, "Replacement bypassed delay"), () => spawner.LivingCount == 4, 40);
        Use("separate generator ignition", mission.Generator, () =>
        {
            Check(LibraryMissionProgress.IsLibraryGeneratorPowered, "Generator did not start");
            var lights = new SerializedObject(Object.FindAnyObjectByType<LibraryLightingController>());
            foreach (var name in new[] { "normalLights", "emergencyLights" })
            {
                var list = lights.FindProperty(name); for (int i = 0; i < list.arraySize; i++)
                    Check(((Light)list.GetArrayElementAtIndex(i).objectReferenceValue).enabled == (name == "normalLights"), "Wrong lighting state");
            }
        });
        Add("No reinforcements after power restored", () =>
        {
            player.Teleport(new Vector3(-21.5f, 1.07f, 36.4f), Quaternion.identity);
            Object.FindObjectsByType<Enemy>().First(e => !e.IsDead).TakeDamage(100000); killedAt = Time.time;
        }, () => Check(spawner.LivingCount == 3, "Spawned after mission completion"), () => Time.time - killedAt > 8, 40);
        Add("No numeric answer on world labels", null, () => Check(!mission.GetComponentsInParent<Transform>().Last().GetComponentsInChildren<TMP_Text>(true).Any(t => t.text.Contains("3 → 1")), "World solution leaked"));
    }
    static void Finish(bool success)
    {
        finishing = true; SessionState.SetBool(Active, false);
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        if (captured) { InputSystem.settings.backgroundBehavior = background; InputSystem.settings.editorInputBehaviorInPlayMode = editorInput; }
        File.WriteAllLines(Report + "-result.txt", results.Concat(new[] { success ? "ALL PASS" : "FAILED" }));
        EditorApplication.isPlaying = false;
        EditorApplication.delayCall += () => EditorApplication.Exit(success ? 0 : 1);
    }
}
