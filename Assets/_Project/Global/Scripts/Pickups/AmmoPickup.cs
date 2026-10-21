using UnityEngine;

[DisallowMultipleComponent]
public class AmmoPickup : MonoBehaviour
{
    [SerializeField] private int ammoAmount = 10;
    [Tooltip("Fill the equipped weapon's magazine and reserve instead of adding a fixed amount.")]
    [SerializeField] private bool refillEquippedWeapon;
    private bool consumed;

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    public bool TryCollect(Collider other)
    {
        if (consumed || other == null || UniDoom.UI.DeathScreen.IsVisible)
            return false;

        PlayerWeaponManager manager = other.GetComponentInParent<PlayerWeaponManager>();
        PlayerMovement player = other.GetComponentInParent<PlayerMovement>();
        Transform playerRoot = manager != null ? manager.transform : player != null ? player.transform : null;
        if (playerRoot == null || !playerRoot.CompareTag("Player"))
            return false;

        Gun currentGun = manager != null ? manager.CurrentGun : playerRoot.GetComponentInChildren<Gun>();
        if (currentGun == null)
            return false;

        if (refillEquippedWeapon)
        {
            if (!currentGun.RefillAmmo())
                return false;
        }
        else
        {
            if (ammoAmount <= 0)
                return false;
            currentGun.AddAmmo(ammoAmount);
        }

        // Destroy is deferred; multiple player colliders must not consume twice.
        consumed = true;
        foreach (Collider trigger in GetComponents<Collider>())
            trigger.enabled = false;
        Destroy(gameObject);
        return true;
    }
}
