using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>One encounter per basement instance. The boss is outside this roster.</summary>
public class BasementArenaEncounter : MonoBehaviour
{
    public Transform arena;
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public Transform gateLeaf;
    public Renderer statusLamp;
    public Vector3 arenaCenter = new Vector3(0, 2, 0);
    public Vector3 arenaSize = new Vector3(25.4f, 4, 29.4f);
    [Min(0.01f)] public float openingDuration = 1.2f;
    public UnityEvent onArenaCleared = new UnityEvent();

    public bool Started { get; private set; }
    public bool Cleared { get; private set; }
    public int RemainingEnemies
    {
        get
        {
            int count = 0;
            foreach (Enemy enemy in enemies)
                if (enemy != null && !enemy.IsDead) count++;
            return count;
        }
    }

    readonly List<Enemy> enemies = new List<Enemy>();
    Transform player;
    Vector3 closedPosition;
    float openProgress;

    void Awake()
    {
        if (arena == null || gateLeaf == null || enemyPrefab == null ||
            enemyPrefab.GetComponentInChildren<Enemy>() == null || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Basement encounter needs arena, gate, enemy prefab and spawn points.", this);
            enabled = false;
            return;
        }
        foreach (Transform point in spawnPoints)
            if (point == null)
            {
                Debug.LogError("Basement encounter has a missing spawn point.", this);
                enabled = false;
                return;
            }
        closedPosition = gateLeaf.localPosition;
    }

    void Update()
    {
        if (!Started)
        {
            if (player == null)
            {
                GameObject found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }
            if (player != null && Contains(player.position)) BeginEncounter();
            return;
        }
        if (!Cleared && RemainingEnemies == 0)
        {
            Cleared = true;
            if (statusLamp != null)
            {
                var block = new MaterialPropertyBlock();
                statusLamp.GetPropertyBlock(block);
                block.SetColor("_BaseColor", new Color(.12f, 1, .18f));
                block.SetColor("_EmissionColor", new Color(.12f, 3, .18f));
                statusLamp.SetPropertyBlock(block);
            }
            onArenaCleared.Invoke();
        }
        if (Cleared)
        {
            openProgress = Mathf.MoveTowards(openProgress, 1, Time.deltaTime / openingDuration);
            gateLeaf.localPosition = closedPosition + Vector3.up *
                (1.08f * Mathf.SmoothStep(0, 1, openProgress));
        }
    }

    bool Contains(Vector3 worldPosition) =>
        new Bounds(arenaCenter, arenaSize).Contains(arena.InverseTransformPoint(worldPosition));

    public void BeginEncounter()
    {
        if (Started || !enabled) return;
        Started = true;
        // Collect existing scene enemies at activation, after scene spawners have run Start.
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if (!enemy.IsDead && Contains(enemy.transform.position)) RegisterEnemy(enemy);
        foreach (Transform point in spawnPoints)
        {
            var instance = Instantiate(enemyPrefab, point.position, point.rotation);
            // Keep the enemy's world dimensions when the map instance scales the basement.
            instance.transform.SetParent(arena, true);
            instance.name = "Basement arena enemy";
            RegisterEnemy(instance.GetComponentInChildren<Enemy>());
        }
    }

    // Wave spawners can register additional enemies before the last living enemy dies.
    public void RegisterEnemy(Enemy enemy)
    {
        if (!Cleared && enemy != null && !enemies.Contains(enemy)) enemies.Add(enemy);
    }
}
