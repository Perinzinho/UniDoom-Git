using UnityEngine;
using UnityEngine.Serialization;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Configuration")]
    [FormerlySerializedAs("prefabInimigo")]
    [SerializeField] private GameObject enemyPrefab;
    [FormerlySerializedAs("pontosDeSpawn")]
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        SpawnAllEnemies();
    }

    private void SpawnAllEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy prefab has not been assigned in the Spawner!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points have been defined in the Spawner!");
            return;
        }

        foreach (Transform point in spawnPoints)
        {
            Instantiate(enemyPrefab, point.position, point.rotation);
        }

        Debug.Log($"{spawnPoints.Length} enemy(ies) spawned.");
    }
}
