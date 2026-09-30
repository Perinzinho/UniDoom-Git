using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("UI")]
    [SerializeField] private GameObject crosshairPrefab;
    [SerializeField] private GameObject HUDPrefab;



    private void Start()
    {
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        SpawnCrosshair();
        SpawnHUD();
        ConnectWeapon(player);
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
        DebugUI.LogWarning($"{name}: HudPrefab não atribuiído");
        return;
    } 
    Instantiate(HUDPrefab);
    
}

    

    private void ConnectWeapon(GameObject player)
    {
        Pistol playerGun = player.GetComponentInChildren<Pistol>();
        WeaponSpriteAnimator weaponAnimator = FindFirstObjectByType<WeaponSpriteAnimator>();

        if (playerGun == null || weaponAnimator == null)
        {
            DebugUI.LogWarning($"{name}: não deu para ligar a arma ao animator (gun: {playerGun != null}, animator: {weaponAnimator != null}).");
            return;
        }

        weaponAnimator.SetGun(playerGun);
        DebugUI.Log("SetGun chamado com sucesso.");
    }
}