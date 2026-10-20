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

    [Header("Resource Priority")]
    [Tooltip("Resources at or below this fraction are low. Ammo takes priority when both are low.")]
    [SerializeField, Range(0.01f, 0.99f)] private float lowResourceThreshold = 0.3f;

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

        float healthRatio = 1f;
        if (medkitPickupPrefab != null && life.MaxHealth > 0)
            healthRatio = (float)life.CurrentHealth / life.MaxHealth;

        float ammoRatio = 1f;
        if (ammoPickupPrefab != null && gun != null && gun.MagazineSize > 0)
        {
            float targetAmmo = (float)gun.MagazineSize * Mathf.Max(1, targetMagazines);
            float availableAmmo = (float)gun.CurrentAmmo + gun.ReserveAmmo;
            ammoRatio = availableAmmo / targetAmmo;
        }

        CalculateDropChances(healthRatio, ammoRatio, out float ammoChance, out float medkitChance);

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

    private void CalculateDropChances(float healthRatio, float ammoRatio,
        out float ammoChance, out float medkitChance)
    {
        healthRatio = Mathf.Clamp01(healthRatio);
        ammoRatio = Mathf.Clamp01(ammoRatio);
        ammoChance = (1f - ammoRatio) * Mathf.Clamp01(maxAmmoDropChance);
        medkitChance = (1f - healthRatio) * Mathf.Clamp01(maxMedkitDropChance);

        // Keep gradual shortage-based chances, but make the priority at least twice as likely.
        // Check ammo first so it wins when both resources are low.
        float threshold = Mathf.Clamp01(lowResourceThreshold);
        if (ammoRatio <= threshold && ammoChance > 0f)
            medkitChance = Mathf.Min(medkitChance, ammoChance * 0.5f);
        else if (healthRatio <= threshold && medkitChance > 0f)
            ammoChance = Mathf.Min(ammoChance, medkitChance * 0.5f);
    }
}
