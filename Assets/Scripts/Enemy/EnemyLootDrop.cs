using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class EnemyLootDrop : MonoBehaviour
{
    [Header("Pickup Prefabs")]
    [SerializeField] private GameObject ammoPickupPrefab;
    [SerializeField] private GameObject medkitPickupPrefab;

    [Header("Drop Chances At Zero Resources")]
    [SerializeField, Range(0f, 1f)] private float maxAmmoDropChance = 0.65f;
    [SerializeField, Range(0f, 1f)] private float maxMedkitDropChance = 0.65f;

    [Header("Ammo Target")]
    [SerializeField, Min(1)] private int targetMagazines = 3;

    private Enemy enemy;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
    }

    private void OnEnable()
    {
        enemy.OnDeath.AddListener(TryDropLoot);
    }

    private void OnDisable()
    {
        enemy.OnDeath.RemoveListener(TryDropLoot);
    }

    private void TryDropLoot(GameObject _)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        PlayerLife life = player.GetComponent<PlayerLife>();
        if (life == null || life.IsDead)
            return;

        PlayerWeaponManager weapons = player.GetComponent<PlayerWeaponManager>();
        Gun gun = weapons != null ? weapons.CurrentGun : null;

        float medkitChance = 0f;
        if (medkitPickupPrefab != null && life.MaxHealth > 0)
            medkitChance = CalculateChance(life.CurrentHealth, life.MaxHealth, maxMedkitDropChance);

        float ammoChance = 0f;
        if (ammoPickupPrefab != null && gun != null && gun.MagazineSize > 0)
        {
            int targetAmmo = gun.MagazineSize * Mathf.Max(1, targetMagazines);
            int availableAmmo = gun.CurrentAmmo + gun.ReserveAmmo;
            ammoChance = CalculateChance(availableAmmo, targetAmmo, maxAmmoDropChance);
        }

        bool dropMedkit = Random.value < medkitChance;
        bool dropAmmo = Random.value < ammoChance;
        Vector3 towardPlayer = player.transform.position - transform.position;
        towardPlayer.y = 0f;
        Vector3 direction = towardPlayer.sqrMagnitude > 0.01f ? towardPlayer.normalized : Vector3.forward;
        Vector3 dropPosition = transform.position + direction * 1f;
        Vector3 side = Vector3.Cross(Vector3.up, direction) * 0.6f;

        if (dropMedkit)
            Instantiate(medkitPickupPrefab, dropPosition - side + Vector3.down * 0.4f, Quaternion.identity);

        if (dropAmmo)
            Instantiate(ammoPickupPrefab, dropPosition + side, Quaternion.identity);
    }

    private static float CalculateChance(int available, int target, float maximumChance)
    {
        float shortage = 1f - Mathf.Clamp01((float)available / target);
        return shortage * Mathf.Clamp01(maximumChance);
    }
}
