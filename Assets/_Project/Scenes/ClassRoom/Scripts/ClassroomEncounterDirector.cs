using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// This room's difficulty comes from its occupied aisles and the return trip with Key01.
[DisallowMultipleComponent]
public sealed class ClassroomEncounterDirector : MonoBehaviour
{
    public GameObject zombiePrefab;
    public GameObject objectiveKey;
    public Transform[] initialGuards;
    public Transform[] returnGuards;
    [Min(.1f)] public float returnSpawnInterval = .9f;
    [Min(1f)] public float minimumSpawnDistance = 3f;

    readonly List<Enemy> living = new List<Enemy>();
    PlayerPickupKey player;
    bool boundPlayer, keyOwnedOnArrival;
    public bool InitialDeploymentComplete { get; private set; }
    public bool ReturnWaveTriggered { get; private set; }
    public int SpawnedCount { get; private set; }
    public int PendingSpawns { get; private set; }

    IEnumerator Start()
    {
        // All scene NavMesh surfaces register before the first guards are instantiated.
        yield return null;
        BindPlayer();
        if (initialGuards != null)
            foreach (var point in initialGuards) Spawn(point);
        InitialDeploymentComplete = true;
    }

    void BindPlayer()
    {
        if (boundPlayer) return;
        var session = GameManager.SessionPlayer;
        if (session == null) return;
        player = session.GetComponent<PlayerPickupKey>();
        if (player == null) return;
        boundPlayer = true;
        keyOwnedOnArrival = player.key01;
    }

    void Update()
    {
        BindPlayer();
        if (InitialDeploymentComplete && player != null && !keyOwnedOnArrival && player.key01 && !ReturnWaveTriggered)
        {
            ReturnWaveTriggered = true;
            StartCoroutine(DeployReturnGuards());
        }
        for (int i = living.Count - 1; i >= 0; i--)
        {
            var enemy = living[i];
            if (enemy != null && !enemy.IsDead) continue;
            // Bodies remain visible, but cannot seal the narrow routes through the desks.
            if (enemy != null)
                foreach (var collider in enemy.GetComponentsInChildren<Collider>()) collider.enabled = false;
            living.RemoveAt(i);
        }
    }

    IEnumerator DeployReturnGuards()
    {
        if (returnGuards == null) yield break;
        PendingSpawns = returnGuards.Length;
        yield return new WaitForSeconds(.75f);
        foreach (var point in returnGuards)
        {
            float waited = 0;
            while (point != null && player != null && Vector3.Distance(player.transform.position, point.position) < minimumSpawnDistance && waited < 4f)
            {
                yield return new WaitForSeconds(.25f);
                waited += .25f;
            }
            Spawn(point);
            PendingSpawns--;
            yield return new WaitForSeconds(returnSpawnInterval);
        }
    }

    void Spawn(Transform point)
    {
        if (point == null || zombiePrefab == null) return;
        if (player != null && Vector3.Distance(player.transform.position, point.position) < minimumSpawnDistance) return;
        if (!NavMesh.SamplePosition(point.position, out var floor, .65f, NavMesh.AllAreas))
        {
            Debug.LogWarning("Classroom guard has no reachable floor: " + point.name, point);
            return;
        }
        var agent = zombiePrefab.GetComponent<NavMeshAgent>();
        float baseOffset = agent != null ? agent.baseOffset : 1f;
        var instance = Instantiate(zombiePrefab, floor.position + Vector3.up * baseOffset, point.rotation, transform);
        var enemy = instance.GetComponent<Enemy>();
        if (enemy != null) living.Add(enemy);
        SpawnedCount++;
    }

    void OnDisable()
    {
        StopAllCoroutines();
        PendingSpawns = 0;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(.8f, .25f, .1f);
        if (initialGuards != null) foreach (var p in initialGuards) if (p != null) Gizmos.DrawWireSphere(p.position, .6f);
        Gizmos.color = new Color(.9f, .65f, .15f);
        if (returnGuards != null) foreach (var p in returnGuards) if (p != null) Gizmos.DrawWireSphere(p.position, .6f);
    }
#endif
}
