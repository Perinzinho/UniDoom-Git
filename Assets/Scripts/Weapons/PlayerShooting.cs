using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [SerializeField] private Gun gun;

    void Update()
    {
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