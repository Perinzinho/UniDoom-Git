using UnityEngine;
using UnityEngine.Serialization;
using System;

public abstract class Gun : MonoBehaviour
{
    [Header("Ammo")]

    // Maximum ammo that fits in the magazine.
    [SerializeField] protected int magazineSize = 10;

    // Current ammo in the magazine.
    [SerializeField] protected int currentAmmo = 10;

    // Reserve ammo outside the magazine.
    [SerializeField] protected int reserveAmmo = 30;


    [Header("Reload")]

    // Reference to the component responsible for reload timing.
    [FormerlySerializedAs("recharging")]
    protected Reloading reloading;


    // Allow other scripts to query ammo values without direct modification.
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MagazineSize => magazineSize;


    // Called when the weapon is initialized.
    protected virtual void Awake()
    {
        if (reloading == null)
        {
            reloading = GetComponent<Reloading>();
        }
    }


    // Attempt to fire.
    public event Action OnShoot;

    public void TryShoot()
    {
        if (reloading != null && reloading.IsReloading)
        {
            DebugUI.Log($"{name}: cannot shoot while reloading.");
            return;
        }

        if (!HasAmmo())
        {
            DebugUI.Log($"{name}: no ammo in magazine.");
            return;
        }

        Shoot();
        ConsumeAmmo();

        OnShoot?.Invoke();
    }


    // Attempt to start reloading.
    public void TryReload()
    {
        // Without Reloading component we cannot reload.
        if (reloading == null)
        {
            DebugUI.LogWarning($"{name}: Reloading component not found.");
            return;
        }

        // If already reloading, don't start another.
        if (reloading.IsReloading)
            return;

        // If magazine is already full, no need to reload.
        if (currentAmmo >= magazineSize)
        {
            DebugUI.Log($"{name}: magazine already full.");
            return;
        }

        // Without reserve ammo we have nothing to reload.
        if (reserveAmmo <= 0)
        {
            DebugUI.Log($"{name}: no reserve ammo.");
            return;
        }

        // Start reload process.
        reloading.StartReload(this);
    }


    // Check if magazine still has ammo.
    protected bool HasAmmo()
    {
        return currentAmmo > 0;
    }


    // Consume one round from the magazine after firing.
    protected void ConsumeAmmo()
    {
        currentAmmo--;

        DebugUI.Log(
            $"{name}: {currentAmmo}/{magazineSize} | Reserve: {reserveAmmo}"
        );
    }


    // Add ammo to the reserve.
    public void AddAmmo(int amount)
    {
        // Prevent invalid or negative values.
        if (amount <= 0)
            return;

        // Add amount to reserve ammo.
        reserveAmmo += amount;

        DebugUI.Log(
            $"{name}: picked up {amount} ammo. Reserve: {reserveAmmo}"
        );
    }


    // Finish reloading by transferring ammo from reserve to magazine.
    public void FinishReload()
    {
        // Calculate how many rounds are missing to fill the magazine.
        int missingAmmo = magazineSize - currentAmmo;

        // Decide how many rounds can actually be loaded.
        int ammoToReload = Mathf.Min(missingAmmo, reserveAmmo);

        // Add rounds to magazine.
        currentAmmo += ammoToReload;

        // Remove the same amount from reserve.
        reserveAmmo -= ammoToReload;

        DebugUI.Log(
            $"{name}: reload complete. " +
            $"Magazine: {currentAmmo}/{magazineSize} | Reserve: {reserveAmmo}"
        );
    }


    // Every weapon inheriting from Gun must implement its own shooting behavior.
    protected abstract void Shoot();
}
