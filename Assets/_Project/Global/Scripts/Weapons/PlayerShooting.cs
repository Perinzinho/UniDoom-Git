using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private Gun gun;
    private PlayerWeaponManager weapons;

    private void Awake() => weapons = GetComponent<PlayerWeaponManager>();

    public void SetGun(Gun newGun)
    {
        gun = newGun;
    }

    void Update()
    {
        if (PlayerWeaponManager.InputBlocked)
            return;

        if (weapons != null)
        {
            if (weapons.HandleSelectionInput()) return;
            if (weapons.IsAxeEquipped)
            {
                if (Input.GetButtonDown("Fire1")) weapons.Axe.TryAttack();
                return;
            }
        }

        if (gun == null)
            return;

        if (Input.GetButtonDown("Fire1"))
        {
            gun.TryShoot();
            
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            gun.TryReload();
        }
    }
    
    
}
