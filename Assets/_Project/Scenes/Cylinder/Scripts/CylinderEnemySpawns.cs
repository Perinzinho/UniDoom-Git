using System.Collections;
using UnityEngine;

/// <summary>One encounter per atrium instance, activated when the player enters from the south.</summary>
[DisallowMultipleComponent]
public sealed class CylinderEnemySpawns : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Min(0)] private float delayBetweenSpawns = 1.4f;

    private bool triggered;
    private int nextSpawn;
    private Coroutine spawning;

    private void OnTriggerEnter(Collider other)
    {
        if (!isActiveAndEnabled || triggered || !IsPlayer(other) || !enemyPrefab || spawnPoints == null || spawnPoints.Length == 0)
            return;
        triggered = true;
        spawning = StartCoroutine(SpawnRemaining());
    }

    private void OnEnable()
    {
        if (triggered && spawning == null && spawnPoints != null && nextSpawn < spawnPoints.Length)
            spawning = StartCoroutine(SpawnRemaining());
    }

    private void OnDisable()
    {
        if (spawning != null) StopCoroutine(spawning);
        spawning = null;
    }

    private IEnumerator SpawnRemaining()
    {
        while (nextSpawn < spawnPoints.Length)
        {
            var point = spawnPoints[nextSpawn++];
            if (point && enemyPrefab)
                Instantiate(enemyPrefab, point.position, point.rotation);
            if (nextSpawn < spawnPoints.Length && delayBetweenSpawns > 0)
                yield return new WaitForSeconds(delayBetweenSpawns);
        }
        spawning = null;
    }

    private static bool IsPlayer(Collider other)
    {
        for (var current = other.transform; current; current = current.parent)
            if (current.CompareTag("Player")) return true;
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        Gizmos.color = new Color(.85f, .18f, .12f, .8f);
        foreach (var point in spawnPoints)
        {
            if (!point) continue;
            Gizmos.DrawWireSphere(point.position, .5f);
            Gizmos.DrawLine(point.position, point.position + point.forward);
        }
    }
#endif
}
