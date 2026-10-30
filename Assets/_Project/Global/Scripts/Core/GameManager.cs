using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager session;
    public static PlayerMovement SessionPlayer { get; private set; }
    public static CharacterController SessionPlayerControllerPrefab => session != null
        ? session.PlayerControllerPrefab : null;

    public static void EndSession()
    {
        var previous = session;
        session = null;
        SessionPlayer = null;
        if (previous != null) Destroy(previous.gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        session = null;
        SessionPlayer = null;
    }

    private void Awake()
    {
        if (session != null && session != this)
        {
            Destroy(gameObject);
            return;
        }
        session = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (session != this) return;
        session = null;
        SessionPlayer = null;
    }

    [Header("Player")]
    [SerializeField] private GameObject playerPrefab;
    public CharacterController PlayerControllerPrefab => playerPrefab != null
        ? playerPrefab.GetComponent<CharacterController>() : null;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject weaponPrefab;

    [Header("UI")]
    [SerializeField] private GameObject crosshairPrefab;
    [SerializeField] private GameObject HUDPrefab;
    [SerializeField] private GameObject KeysPrefab;

    private GameObject hudInstance;

    private void Start()
    {
        if (session != this) return;
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        player.transform.SetParent(transform, true);
        SessionPlayer = player.GetComponent<PlayerMovement>();

        if (weaponPrefab != null) Instantiate(weaponPrefab, transform);
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
            return;
        }

        Instantiate(crosshairPrefab, transform);
    }

    private void SpawnHUD()
    {
        if (HUDPrefab == null)
        {
            return;
        }

        hudInstance = Instantiate(HUDPrefab, transform);
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
            return;
        }

        if (hudInstance == null)
            return;

        HealthUI healthUI = hudInstance.GetComponentInChildren<HealthUI>();

        if (healthUI == null)
        {
            return;
        }

        healthUI.SetPlayer(playerLife);
    }

    private void SpawnKeyUI(GameObject player)
    {
        GameObject keyInstance = Instantiate(KeysPrefab, transform);

        KeysUI keysUI = keyInstance.GetComponentInChildren<KeysUI>();
        keysUI.SetPlayer(player.GetComponent<PlayerRespawn>());
        player.GetComponentInChildren<PlayerPickupKey>().SetKeysUI(keysUI);
    }
}
