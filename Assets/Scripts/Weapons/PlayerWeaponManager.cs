using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerShooting))]
public class PlayerWeaponManager : MonoBehaviour
{
    [SerializeField] private WeaponDefinition initialWeapon;
    [SerializeField] private Transform weaponHolder;

    private Gun currentGun;
    private WeaponDefinition currentWeapon;
    private PlayerShooting shooting;
    private AmmoUI ammoUI;
    private WeaponSpriteAnimator weaponAnimator;

    public Gun CurrentGun => currentGun;
    public WeaponDefinition CurrentWeapon => currentWeapon;

    private void Awake()
    {
        shooting = GetComponent<PlayerShooting>();
        currentGun = GetComponentInChildren<Gun>();
        currentWeapon = initialWeapon;
        if (weaponHolder == null)
            weaponHolder = currentGun != null ? currentGun.transform.parent : transform;
        shooting.SetGun(currentGun);
    }

    public void BindUI(WeaponSpriteAnimator animator, AmmoUI ammo)
    {
        weaponAnimator = animator;
        ammoUI = ammo;
        RefreshBindings();
    }

    public bool TryPickup(WeaponPickup pickup)
    {
        if (pickup == null || !pickup.CanBePickedUpBy(this))
            return false;

        // Validate before consuming the pickup or removing the equipped weapon.
        if (currentGun != null && (currentWeapon == null || currentWeapon.PickupPrefab == null))
        {
            Debug.LogWarning("PlayerWeaponManager: configure o pickup da arma atual.", this);
            return false;
        }

        WeaponDefinition nextWeapon = pickup.Definition;
        Gun nextGun = pickup.TakeGun(weaponHolder);
        if (nextGun == null)
            return false;

        if (currentGun != null)
        {
            Reloading reload = currentGun.GetComponent<Reloading>();
            if (reload != null)
                reload.CancelReload();

            WeaponPickup dropped = Instantiate(currentWeapon.PickupPrefab,
                pickup.transform.position, pickup.transform.rotation);
            dropped.StoreGun(currentGun, currentWeapon, this);
        }

        currentGun = nextGun;
        currentWeapon = nextWeapon;
        currentGun.transform.SetParent(weaponHolder, false);
        currentGun.transform.localPosition = Vector3.zero;
        currentGun.transform.localRotation = Quaternion.identity;
        currentGun.gameObject.SetActive(true);
        RefreshBindings();
        pickup.Consume();
        return true;
    }

    private void RefreshBindings()
    {
        shooting.SetGun(currentGun);
        if (ammoUI != null)
            ammoUI.SetGun(currentGun);
        if (weaponAnimator != null)
            weaponAnimator.SetGun(currentGun, currentWeapon);
    }
}
