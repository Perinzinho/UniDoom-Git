using UnityEngine;

public class WeaponInputHandler : MonoBehaviour
{
    // Weapon currently equipped by the player.
    [SerializeField] private Gun currentGun;

    public Gun CurrentGun => currentGun;


    // Executed every frame to check player input.
    private void Update()
    {
        if (currentGun == null)
            return;

        // Left mouse button attempts to shoot.
        if (Input.GetMouseButtonDown(0))
        {
            currentGun.TryShoot();
        }

        // R key attempts to reload the current weapon.
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentGun.TryReload();
        }
    }


    // Changes the currently equipped weapon.
    public void ChangeGun(Gun newGun)
    {
        currentGun = newGun;
    }
}
