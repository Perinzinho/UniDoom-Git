using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject weaponPrefab;

    [Header("UI")]
    [SerializeField] private GameObject crosshairPrefab;
    [SerializeField] private GameObject HUDPrefab;

    private GameObject hudInstance;

    private void Start()
    {
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        Instantiate(weaponPrefab);
        SpawnCrosshair();
        SpawnHUD();
        ConnectWeapon(player);
        ConnectHealth(player);
    }

    private void SpawnCrosshair()
    {
        if (crosshairPrefab == null)
        {
            DebugUI.LogWarning($"{name}: crosshairPrefab não atribuído.");
            return;
        }

        Instantiate(crosshairPrefab);
    }

    private void SpawnHUD()
    {
        if (HUDPrefab == null)
        {
            DebugUI.LogWarning($"{name}: HUDPrefab não atribuído.");
            return;
        }

        hudInstance = Instantiate(HUDPrefab);
    }

    private void ConnectWeapon(GameObject player)
    {
        Pistol playerGun = player.GetComponentInChildren<Pistol>();
        WeaponSpriteAnimator weaponAnimator = FindFirstObjectByType<WeaponSpriteAnimator>();

        if (playerGun == null)
        {
            DebugUI.LogWarning($"{name}: Pistol não encontrada no player.");
            return;
        }

        if (weaponAnimator != null)
        {
            weaponAnimator.SetGun(playerGun);
            DebugUI.Log("SetGun chamado no WeaponSpriteAnimator.");
        }
        else
        {
            DebugUI.LogWarning($"{name}: WeaponSpriteAnimator não encontrado.");
        }

        // Conecta a UI de munição
        if (hudInstance != null)
        {
            AmmoUI ammoUI = hudInstance.GetComponentInChildren<AmmoUI>();

            if (ammoUI != null)
            {
                ammoUI.SetGun(playerGun);
                DebugUI.Log("SetGun chamado no AmmoUI.");
            }
            else
            {
                DebugUI.LogWarning($"{name}: AmmoUI não encontrado no HUD.");
            }
        }
    }
    
    private void ConnectHealth(GameObject player)
    {
        PlayerLife playerLife = player.GetComponentInChildren<PlayerLife>();

        if (playerLife == null)
        {
            DebugUI.LogWarning($"{name}: PlayerLife não encontrado no player.");
            return;
        }

        if (hudInstance == null)
            return;

        HealthUI healthUI = hudInstance.GetComponentInChildren<HealthUI>();

        if (healthUI == null)
        {
            DebugUI.LogWarning($"{name}: HealthUI não encontrado no HUD.");
            return;
        }

        healthUI.SetPlayer(playerLife);
        DebugUI.Log("SetPlayer chamado no HealthUI.");
    }
}