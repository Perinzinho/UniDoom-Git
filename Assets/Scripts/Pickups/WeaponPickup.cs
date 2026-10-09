using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponDefinition definition;

    private Gun storedGun;
    private PlayerWeaponManager blockedPlayer;
    private BoxCollider pickupTrigger;
    private bool consumed;

    public WeaponDefinition Definition => definition;

    private void Awake()
    {
        pickupTrigger = GetComponent<BoxCollider>();
        pickupTrigger.isTrigger = true;
        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
    }

    public bool CanBePickedUpBy(PlayerWeaponManager player)
    {
        return player != null && !consumed && player != blockedPlayer &&
            definition != null && (storedGun != null || definition.GunPrefab != null);
    }

    public Gun TakeGun(Transform holder)
    {
        if (storedGun != null)
        {
            Gun result = storedGun;
            storedGun = null;
            result.transform.SetParent(holder, false);
            return result;
        }

        if (definition == null || definition.GunPrefab == null)
            return null;

        Gun gun = Instantiate(definition.GunPrefab, holder);
        gun.name = definition.GunPrefab.name;
        return gun;
    }

    public void StoreGun(Gun gun, WeaponDefinition weapon, PlayerWeaponManager owner)
    {
        definition = weapon;
        storedGun = gun;
        blockedPlayer = owner;
        gun.gameObject.SetActive(false);
        gun.transform.SetParent(transform, false);
    }

    public void Consume()
    {
        // Destroy is deferred, so block additional callbacks in the same frame.
        consumed = true;
        pickupTrigger.enabled = false;
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerWeaponManager player = other.GetComponentInParent<PlayerWeaponManager>();
        if (player != null && player.CompareTag("Player"))
            player.TryPickup(this);
    }

    private void FixedUpdate()
    {
        if (blockedPlayer == null)
            return;

        // The player has multiple colliders. Unlock only after ALL have left.
        // Checking bounds also works when the pickup spawned inside the player.
        foreach (Collider playerCollider in blockedPlayer.GetComponentsInChildren<Collider>())
        {
            if (playerCollider.enabled && pickupTrigger.bounds.Intersects(playerCollider.bounds))
                return;
        }
        blockedPlayer = null;
    }
}
