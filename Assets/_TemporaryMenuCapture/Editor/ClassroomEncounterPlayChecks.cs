using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Opt-in isolated runtime validation. Never teleports or changes the session player.
[InitializeOnLoad]
public static class ClassroomEncounterPlayChecks
{
    const string Active = "UniDoom.ClassroomChecks";
    const string Started = "UniDoom.ClassroomChecks.StartedPlay";
    const string Request = "Temp/run-classroom-encounter-checks.txt";
    const string Map = "Assets/_Project/Scenes/ClassRoom/Prefabs/ClassroomEncounter.prefab";
    static readonly List<string> results = new();
    static GameObject host;
    static ClassroomEncounterDirector director;
    static PlayerPickupKey keys;
    static Enemy killed;
    static UnityEngine.Random.State random;
    static bool background;
    static int stage;
    static double ready, deadline;

    static ClassroomEncounterPlayChecks() => EditorApplication.update += Tick;

    static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (!SessionState.GetBool(Active, false))
        {
            if (!File.Exists(Request)) return;
            File.Move(Request, "Temp/classroom-runtime-" + DateTime.UtcNow.Ticks + ".consumed");
            SessionState.SetBool(Active, true);
            SessionState.SetBool(Started, !EditorApplication.isPlaying);
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
            return;
        }
        if (!Application.isPlaying) return;
        EditorApplication.QueuePlayerLoopUpdate();
        if (ready == 0) { ready = EditorApplication.timeSinceStartup + 3; deadline = ready + 60; }
        if (EditorApplication.timeSinceStartup < ready) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Classroom runtime validation timed out at " + stage);
            Run();
        }
        catch (Exception e) { results.Add("FAIL: " + e); Finish(); }
    }

    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        results.Add("PASS: " + message);
    }

    static void SetField(object instance, string name, object value)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(instance, value);

    static void Run()
    {
        switch (stage)
        {
            case 0:
                random = UnityEngine.Random.state;
                background = Application.runInBackground;
                Application.runInBackground = true;
                host = new GameObject("[Temporary classroom runtime validation]");
                host.hideFlags = HideFlags.HideAndDontSave;
                var map = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Map),
                    new Vector3(3051.9f, .6f, 2957.4f), Quaternion.identity, host.transform);
                director = map.GetComponentInChildren<ClassroomEncounterDirector>();
                var fake = new GameObject("Isolated key owner");
                fake.transform.SetParent(host.transform);
                fake.transform.position = map.transform.TransformPoint(new Vector3(-40.135f, 2.25f, 26));
                keys = fake.AddComponent<PlayerPickupKey>();
                SetField(director, "player", keys);
                SetField(director, "boundPlayer", true);
                SetField(director, "keyOwnedOnArrival", false);
                stage++;
                ready = EditorApplication.timeSinceStartup + .4;
                break;
            case 1:
                if (!director.InitialDeploymentComplete) return;
                Check(director.SpawnedCount == 7, "Seven initial guards deploy from the saved map");
                Check(!director.ReturnWaveTriggered && director.PendingSpawns == 0, "Return guards stay inactive before collecting Key01");
                var goal = director.transform.parent.TransformPoint(new Vector3(-27.1f, 1.15f, 13.55f));
                Check(NavMesh.SamplePosition(goal, out var projected, .65f, NavMesh.AllAreas), "Objective route exists at the map's translated runtime position");
                foreach (var enemy in director.GetComponentsInChildren<Enemy>())
                {
                    var loot = enemy.GetComponent<EnemyLootDrop>();
                    if (loot != null) loot.enabled = false;
                    var agent = enemy.GetComponent<NavMeshAgent>();
                    Check(agent != null && agent.isOnNavMesh, "Guard is registered on navigation: " + enemy.name);
                    var path = new NavMeshPath();
                    Check(agent.CalculatePath(projected.position, path) && path.status == NavMeshPathStatus.PathComplete,
                        "Runtime guard can navigate toward the objective");
                    enemy.enabled = false;
                }
                killed = director.GetComponentInChildren<Enemy>();
                killed.TakeDamage(1000);
                keys.transform.position = director.transform.parent.TransformPoint(new Vector3(-27, 2.25f, 13.85f));
                keys.key01 = true;
                UnityEngine.Object.Destroy(director.objectiveKey);
                stage++;
                ready = EditorApplication.timeSinceStartup + .15;
                break;
            case 2:
                if (!director.ReturnWaveTriggered) return;
                Check(killed.IsDead && killed.GetComponentsInChildren<Collider>().All(c => !c.enabled),
                    "Dead guards keep their visuals and release their blocking colliders");
                Check(director.ReturnWaveTriggered, "Collecting Key01 activates the return encounter");
                // Advance only this coroutine. The user's pause/time scale is preserved.
                director.StopAllCoroutines();
                var wave = (IEnumerator)typeof(ClassroomEncounterDirector).GetMethod("DeployReturnGuards",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(director, null);
                while (wave.MoveNext()) { }
                Check(director.SpawnedCount == 10 && director.PendingSpawns == 0, "Three return guards deploy at safe saved markers");
                typeof(ClassroomEncounterDirector).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(director, null);
                Check(director.SpawnedCount == 10, "Keeping the key cannot duplicate the return wave");
                foreach (var enemy in director.GetComponentsInChildren<Enemy>())
                    Check(enemy.IsDead || enemy.GetComponent<NavMeshAgent>().isOnNavMesh, "All living return guards stay on the navigation surface");
                Finish();
                break;
        }
    }

    static void Finish()
    {
        if (host != null) UnityEngine.Object.DestroyImmediate(host);
        Application.runInBackground = background;
        UnityEngine.Random.state = random;
        File.WriteAllLines("Temp/classroom-runtime-validation.txt", results);
        SessionState.SetBool(Active, false);
        bool stop = SessionState.GetBool(Started, false);
        SessionState.SetBool(Started, false);
        if (stop) EditorApplication.isPlaying = false;
        results.Clear(); stage = 0; ready = deadline = 0;
    }
}
