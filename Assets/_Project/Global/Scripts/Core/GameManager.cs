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
    [SerializeField] private GameObject KeysPrefab;

    private GameObject hudInstance;

    private void Start()
    {
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        Instantiate(weaponPrefab);
        SpawnCrosshair();
        SpawnHUD();
        ConnectWeapon(player);
        ConnectHealth(player);
        SpawnKeyUI(player);
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
        PlayerWeaponManager weaponManager = player.GetComponent<PlayerWeaponManager>();
        WeaponSpriteAnimator weaponAnimator = FindFirstObjectByType<WeaponSpriteAnimator>();
        AmmoUI ammoUI = hudInstance != null ? hudInstance.GetComponentInChildren<AmmoUI>() : null;

        if (weaponManager != null)
        {
            weaponManager.BindUI(weaponAnimator, ammoUI);
            return;
        }

        // Keep support for player prefabs without the swapping component.
        Gun playerGun = player.GetComponentInChildren<Gun>();
        if (weaponAnimator != null)
            weaponAnimator.SetGun(playerGun);
        if (ammoUI != null)
            ammoUI.SetGun(playerGun);
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

    private void SpawnKeyUI(GameObject player)
    {
        GameObject keyInstance = Instantiate(KeysPrefab);

        KeysUI keysUI = keyInstance.GetComponentInChildren<KeysUI>();
        player.GetComponentInChildren<PlayerPickupKey>().SetKeysUI(keysUI);
    }
}