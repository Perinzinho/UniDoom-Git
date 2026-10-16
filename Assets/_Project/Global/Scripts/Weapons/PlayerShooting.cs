using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private Gun gun;

    public void SetGun(Gun newGun)
    {
        gun = newGun;
    }

    void Update()
    {
        if (UniDoom.UI.DoorReloadScreen.IsReloading || UniDoom.UI.MouseSensitivityMenu.IsOpen)
            return;

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