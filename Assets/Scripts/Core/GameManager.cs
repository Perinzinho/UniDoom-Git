using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("UI")]
    [SerializeField] private GameObject crosshairPrefab; // Canvas inteiro do CrossHair

    [Header("HUD de vida")]
    [SerializeField] private Sprite[] heartFrames;  // frames do life_sprt.png, na ordem
    [SerializeField] private TMP_FontAsset heartFont; // sua fonte (.asset do TextMeshPro)


    private void Start()
    {
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        SpawnCrosshair();
        SpawnHeartHud(player);
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

    private void SpawnHeartHud(GameObject player)
    {
        if (!player.TryGetComponent(out PlayerLife playerLife))
        {
            DebugUI.LogWarning($"{name}: PlayerLife não encontrado no player.");
            return;
        }

        HeartAnimator heart = new GameObject("HealthHeart").AddComponent<HeartAnimator>();
        heart.Init(playerLife, heartFrames, heartFont);

        DebugUI.Log("HeartAnimator criado e conectado ao PlayerLife.");
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