using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerShooting))]
public class PlayerWeaponManager : MonoBehaviour
{
    [SerializeField] private WeaponDefinition initialWeapon;
    [SerializeField] private WeaponDefinition axeWeapon;
    [SerializeField] private Transform weaponHolder;

    private Gun currentGun;
    private WeaponDefinition currentWeapon;
    private PlayerShooting shooting;
    private AmmoUI ammoUI;
    private WeaponSpriteAnimator weaponAnimator;
    private Axe axe;

    // CurrentGun/CurrentWeapon describe the single carried firearm, even while holstered.
    public Gun CurrentGun => currentGun;
    public WeaponDefinition CurrentWeapon => currentWeapon;
    public Axe Axe => axe;
    public bool IsAxeEquipped { get; private set; }
    public static bool InputBlocked => Time.timeScale <= 0f || UniDoom.UI.DeathScreen.IsVisible ||
        UniDoom.UI.DoorReloadScreen.IsReloading || UniDoom.UI.MouseSensitivityMenu.IsOpen;

    private void Awake()
    {
        shooting = GetComponent<PlayerShooting>();
        currentGun = GetComponentInChildren<Gun>();
        currentWeapon = initialWeapon;
        if (weaponHolder == null)
            weaponHolder = currentGun != null ? currentGun.transform.parent : transform;
        if (axeWeapon != null && axeWeapon.AxePrefab != null)
        {
            axe = Instantiate(axeWeapon.AxePrefab, weaponHolder);
            axe.name = "Axe";
            axe.SetOwner(transform);
            IsAxeEquipped = currentGun == null;
            axe.gameObject.SetActive(IsAxeEquipped);
        }
        shooting.SetGun(currentGun);
    }

    public bool EquipAxe() => SelectWeapon(true);
    public bool EquipFirearm() => SelectWeapon(false);

    private bool SelectWeapon(bool useAxe)
    {
        if (InputBlocked || (axe != null && axe.IsAttacking) ||
            (useAxe ? axe == null : currentGun == null) || useAxe == IsAxeEquipped)
            return false;
        if (currentGun != null)
        {
            currentGun.GetComponent<Reloading>()?.CancelReload();
            currentGun.gameObject.SetActive(!useAxe);
        }
        IsAxeEquipped = useAxe;
        if (axe != null) axe.gameObject.SetActive(useAxe);
        RefreshBindings();
        return true;
    }

    // Called by the shooting handler first, so switching never also attacks in that frame.
    public bool HandleSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return EquipAxe();
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return EquipFirearm();
        if (Input.mouseScrollDelta.y != 0f) return SelectWeapon(!IsAxeEquipped);
        return false;
    }

    public void BindUI(WeaponSpriteAnimator animator, AmmoUI ammo)
    {
        weaponAnimator = animator;
        ammoUI = ammo;
        RefreshBindings();
    }

    public bool TryPickup(WeaponPickup pickup)
    {
        if (InputBlocked || pickup == null || !pickup.CanBePickedUpBy(this))
            return false;

        // Validate before consuming the pickup or removing the equipped weapon.
        if (currentGun != null && (currentWeapon == null || currentWeapon.PickupPrefab == null))
        {
            Debug.LogWarning("PlayerWeaponManager: configure o pickup da arma atual.", this);
            return false;
        }

        WeaponDefinition nextWeapon = pickup.Definition;
        // This slot accepts only the game's two firearms. The axe can never be replaced.
        if (!(nextWeapon.GunPrefab is Pistol) && !(nextWeapon.GunPrefab is Shotgun))
            return false;
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
        currentGun.gameObject.SetActive(!IsAxeEquipped);
        // Replacing the holstered firearm must not restart an axe swing on the HUD.
        if (!IsAxeEquipped) RefreshBindings();
        else if (ammoUI != null) ammoUI.SetGun(currentGun);
        pickup.Consume();
        return true;
    }

    private void RefreshBindings()
    {
        shooting.SetGun(IsAxeEquipped ? null : currentGun);
        if (ammoUI != null)
            ammoUI.SetGun(currentGun);
        if (weaponAnimator != null)
        {
            if (IsAxeEquipped) weaponAnimator.SetAxe(axe, axeWeapon);
            else weaponAnimator.SetGun(currentGun, currentWeapon);
        }
    }
}
