using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField] private GameObject playerPrefab; //Prefab do Player
    [SerializeField] private Transform spawnPoint; // Cria um objeto vazio posiciona onde você quer que o player spawne

    void Start()
    {
        Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}

