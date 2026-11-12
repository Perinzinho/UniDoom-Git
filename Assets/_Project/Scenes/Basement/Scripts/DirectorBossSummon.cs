using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Five floor-emerging zombies for each 200 health lost by the living Director.</summary>
[RequireComponent(typeof(DirectorBoss))]
public sealed class DirectorBossSummon : MonoBehaviour
{
    [SerializeField] Enemy zombiePrefab;
    [SerializeField] Sprite[] portalFrames;
    [SerializeField, Min(1)] int damagePerWave = 200;
    [SerializeField, Min(1)] int zombiesPerWave = 5;
    [SerializeField, Min(.1f)] float summonDuration = 2.1f;
    [SerializeField, Min(.1f)] float emergeDuration = 1.2f;

    DirectorBoss boss;
    DirectorBossEncounter encounter;
    readonly List<Enemy> summoned = new();
    readonly List<GameObject> portals = new();
    int thresholdsReached, queuedWaves;
    Coroutine waves;

    void Awake() { boss = GetComponent<DirectorBoss>(); }
    void OnEnable()
    {
        boss.Damaged += OnDamaged;
        boss.Defeated += OnDefeated;
    }
    void OnDisable()
    {
        boss.Damaged -= OnDamaged;
        boss.Defeated -= OnDefeated;
        ClearSummons();
    }

    void OnDamaged(float amount)
    {
        if (boss.CurrentHealth <= 0 || zombiePrefab == null || portalFrames == null || portalFrames.Length == 0) return;
        int reached = Mathf.FloorToInt((boss.MaxHealth - boss.CurrentHealth) / damagePerWave);
        if (reached <= thresholdsReached) return;
        queuedWaves += reached - thresholdsReached;
        thresholdsReached = reached;
        if (waves == null) waves = StartCoroutine(RunWaves());
    }

    IEnumerator RunWaves()
    {
        encounter = GetComponentInParent<DirectorBossEncounter>();
        if (encounter == null)
        {
            Debug.LogError("Director summon needs its arena encounter.", this);
            waves = null;
            yield break;
        }
        bool waitingForSpace = false;
        while (queuedWaves > 0 && !boss.IsDead)
        {
            var positions = new List<Vector3>(zombiesPerWave);
            for (int i = 0; i < zombiesPerWave; i++)
            {
                if (!TryFindFloor(positions, out Vector3 floor))
                {
                    positions.Clear();
                    break;
                }
                positions.Add(floor);
            }
            if (positions.Count != zombiesPerWave)
            {
                if (!waitingForSpace)
                    Debug.LogWarning("Director summon is waiting for clear floor positions.", this);
                waitingForSpace = true;
                yield return new WaitForSeconds(1f);
                continue;
            }
            waitingForSpace = false;
            queuedWaves--;

            boss.BeginSummon(summonDuration);
            yield return new WaitForSeconds(.55f);
            foreach (Vector3 floor in positions)
            {
                if (boss.IsDead) yield break;
                StartCoroutine(Emerge(floor));
                yield return new WaitForSeconds(.2f);
            }
            yield return new WaitForSeconds(Mathf.Max(0, summonDuration - .55f - positions.Count * .2f) + emergeDuration);
        }
        waves = null;
    }

    bool TryFindFloor(List<Vector3> selected, out Vector3 floor)
    {
        Bounds bounds = encounter.ArenaBounds;
        var player = GameObject.FindGameObjectWithTag("Player");
        Physics.SyncTransforms();
        for (int attempt = 0; attempt < 300; attempt++)
        {
            float x = Random.Range(bounds.min.x + 2, bounds.max.x - 2);
            float z = Random.Range(bounds.min.z + 2, bounds.max.z - 2);
            var ray = new Vector3(x, boss.transform.position.y + 3, z);
            if (!Physics.Raycast(ray, Vector3.down, out var hit, 7, ~0, QueryTriggerInteraction.Ignore) || hit.normal.y < .8f)
                continue;
            Vector3 point = hit.point;
            if (Mathf.Abs(point.y - boss.transform.position.y) > .4f) continue;
            if (!encounter.Contains(point + Vector3.up) || FlatDistance(point, boss.transform.position) < 3.5f)
                continue;
            if (player && FlatDistance(point, player.transform.position) < 3.5f) continue;
            bool close = false;
            foreach (Vector3 other in selected)
                if (FlatDistance(point, other) < 2.4f) { close = true; break; }
            if (close) continue;
            if (Physics.CheckCapsule(point + Vector3.up * .6f, point + Vector3.up * 1.4f,
                .48f, ~0, QueryTriggerInteraction.Ignore)) continue;
            floor = point;
            return true;
        }
        floor = default;
        return false;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0;
        return Vector3.Distance(a, b);
    }

    IEnumerator Emerge(Vector3 floor)
    {
        var portal = new GameObject("Director summon portal");
        portal.transform.position = floor + Vector3.up * .02f;
        var portalRenderer = portal.AddComponent<SpriteRenderer>();
        portalRenderer.sprite = portalFrames[0];
        portalRenderer.sortingOrder = 2;
        portal.AddComponent<Billboard>();
        portals.Add(portal);

        var enemy = Instantiate(zombiePrefab, floor + Vector3.up, Quaternion.identity);
        enemy.name = "Director summoned zombie";
        summoned.Add(enemy);
        enemy.enabled = false;
        var agent = enemy.GetComponent<NavMeshAgent>();
        bool useAgent = agent != null && agent.enabled;
        if (agent) agent.enabled = false;
        var capsule = enemy.GetComponent<CapsuleCollider>();
        if (capsule) capsule.enabled = false;
        var body = enemy.GetComponent<Rigidbody>();
        bool kinematic = body && body.isKinematic;
        bool gravity = body && body.useGravity;
        if (body) { body.isKinematic = true; body.useGravity = false; }
        var visual = enemy.GetComponentInChildren<EnemySpriteAnimator>();
        if (visual) visual.VerticalOffset = -1.7f;
        float start = Time.time;
        while (Time.time - start < emergeDuration && enemy && !boss.IsDead)
        {
            float progress = Mathf.Clamp01((Time.time - start) / emergeDuration);
            portalRenderer.sprite = portalFrames[Mathf.Min(portalFrames.Length - 1,
                Mathf.FloorToInt(progress * portalFrames.Length))];
            if (visual) visual.VerticalOffset = -1.7f * (1 - Mathf.SmoothStep(0, 1, progress));
            yield return null;
        }
        portals.Remove(portal);
        Destroy(portal);
        if (!enemy || boss.IsDead) yield break;
        if (visual) visual.VerticalOffset = 0;
        if (body) { body.isKinematic = kinematic; body.useGravity = gravity; }
        if (capsule) capsule.enabled = true;
        if (useAgent) agent.enabled = true;
        enemy.enabled = true;
    }

    void OnDefeated()
    {
        ClearSummons();
    }

    void ClearSummons()
    {
        StopAllCoroutines();
        queuedWaves = 0;
        waves = null;
        foreach (GameObject portal in portals) if (portal) Destroy(portal);
        portals.Clear();
        foreach (Enemy enemy in summoned) if (enemy) Destroy(enemy.gameObject);
        summoned.Clear();
    }
}
