using UnityEngine;

public class SpawnerInimigos : MonoBehaviour
{
    [Header("Configuração do Spawn")]
    [SerializeField] private GameObject prefabInimigo;
    [SerializeField] private Transform[] pontosDeSpawn;

    private void Start()
    {
        SpawnarTodosInimigos();
    }

    private void SpawnarTodosInimigos()
    {
        if (prefabInimigo == null)
        {
            Debug.LogError("Prefab do Inimigo não foi atribuído no Spawner!");
            return;
        }

        if (pontosDeSpawn == null || pontosDeSpawn.Length == 0)
        {
            Debug.LogError("Nenhum ponto de spawn foi definido no Spawner!");
            return;
        }

        foreach (Transform ponto in pontosDeSpawn)
        {
            Instantiate(prefabInimigo, ponto.position, ponto.rotation);
        }

        Debug.Log($"{pontosDeSpawn.Length} inimigo(s) spawnado(s).");
    }
}