using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab; // Prefab do Player
    [SerializeField] private Transform spawnPoint; // Onde o player spawna

    void Start()
    {
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        Pistol playerGun = playerInstance.GetComponentInChildren<Pistol>();
        WeaponSpriteAnimator weaponAnimator = FindObjectOfType<WeaponSpriteAnimator>();

        Debug.Log($"playerGun encontrado: {playerGun != null}");
        Debug.Log($"weaponAnimator encontrado: {weaponAnimator != null}");

        if (weaponAnimator != null && playerGun != null)
        {
            weaponAnimator.SetGun(playerGun);
            Debug.Log("SetGun foi chamado com sucesso!");
        }
        else
        {
            Debug.LogWarning("GameManager: não foi possível conectar a arma ao animador do sprite.");
        }
    }
}