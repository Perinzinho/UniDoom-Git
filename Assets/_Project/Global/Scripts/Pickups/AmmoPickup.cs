using UnityEngine;

[DisallowMultipleComponent]
public class AmmoPickup : MonoBehaviour
{
    [SerializeField] private int ammoAmount = 10;
    [Tooltip("Fill the equipped weapon's magazine; if it is already full, add a fixed amount to the reserve.")]
    [SerializeField] private bool refillEquippedWeapon;
    private bool consumed;
    public event System.Action Collected;

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnTriggerStay(Collider other)
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

        bool refilled = refillEquippedWeapon && currentGun.RefillAmmo();
        if (!refilled)
        {
            if (ammoAmount <= 0)
                return false;
            currentGun.AddAmmo(ammoAmount);
        }

        // Destroy is deferred; multiple player colliders must not consume twice.
        consumed = true;
        GameplayAudio.Instance?.PlayAmmoPickup();
        foreach (Collider trigger in GetComponents<Collider>())
            trigger.enabled = false;
        Collected?.Invoke();
        Destroy(gameObject);
        return true;
    }
}
