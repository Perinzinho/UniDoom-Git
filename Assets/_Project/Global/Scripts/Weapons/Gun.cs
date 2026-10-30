using UnityEngine;
using System;

public abstract class Gun : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] protected int magazineSize = 10;
    [SerializeField] protected int currentAmmo = 10;
    [SerializeField] protected int reserveAmmo = 30;
    [Tooltip("Reserve restored by a full ammo box. Existing surplus from ordinary drops is preserved.")]
    [SerializeField, Min(0)] protected int maxReserveAmmo = 30;

    [Header("Reload")]
    protected Reloading recharging;

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MagazineSize => magazineSize;
    public int MaxReserveAmmo => maxReserveAmmo;
    public bool IsAmmoFull => currentAmmo >= magazineSize && reserveAmmo >= maxReserveAmmo;
    public int ReloadRoundCount => recharging != null ? recharging.ReloadRoundCount : 0;
    public float ReloadElapsedTime => recharging != null ? recharging.ReloadElapsedTime : 0f;

    public void SetReloadCycleDuration(float duration)
    {
        if (this is Shotgun && recharging != null)
            recharging.SetReloadCycleDuration(duration);
    }

    public event Action OnShoot;

    // Novos eventos: repassam o que acontece no Recharging para quem escuta o Gun.
    public event Action OnReloadStarted;
    public event Action OnReloadFinished;

    protected virtual void Awake()
    {
        if (recharging == null)
        {
            recharging = GetComponent<Reloading>();
        }

        if (recharging != null)
        {
            recharging.OnReloadStarted += HandleReloadStarted;
            recharging.OnReloadFinished += HandleReloadFinished;
        }
    }

    protected virtual void OnDestroy()
    {
        if (recharging != null)
        {
            recharging.OnReloadStarted -= HandleReloadStarted;
            recharging.OnReloadFinished -= HandleReloadFinished;
        }
    }
    
    

    private void HandleReloadStarted()
    {
        OnReloadStarted?.Invoke();
    }
    private void HandleReloadFinished() => OnReloadFinished?.Invoke();

    public void TryShoot()
    {
        if (recharging != null && recharging.IsReloading)
        {
            return;
        }

        if (!HasAmmo())
        {
            if (this is Pistol && SFXManager.Instance != null)
                SFXManager.Instance.PlayEmptyShootSound();


            if(this is Shotgun && SFXManager.Instance != null)
                SFXManager.Instance.PlayShotgunEmptySound();
            return;
                
        }

        if (!CanShoot()) return;

        Shoot();
        ConsumeAmmo();

        OnShoot?.Invoke();
    }

    public void TryReload()
    {
        if (recharging == null)
        {
            return;
        }

        if (recharging.IsReloading)
            return;

        if (currentAmmo >= magazineSize)
        {
            return;
        }

        if (reserveAmmo <= 0)
        {
            return;
        }

        recharging.StartReload(this);
    }

    protected bool HasAmmo()
    {
        return currentAmmo > 0;
    }

    public event Action OnAmmoChanged;

    protected void ConsumeAmmo()
    {
        currentAmmo--;
        OnAmmoChanged?.Invoke();
    }

    public void AddAmmo(int amount)
    {
        if (amount <= 0)
            return;

        reserveAmmo += amount;
        OnAmmoChanged?.Invoke();
    }

    public void FinishReload()
    {
        TransferReloadAmmo(magazineSize);
    }

    public bool RefillAmmo()
    {
        if (IsAmmoFull)
            return false;

        // Stop any pending magazine/shell transfer and return the HUD to idle.
        if (recharging != null)
            recharging.CancelReload();

        currentAmmo = magazineSize;
        // Preserve surplus ammunition collected from ordinary enemy drops.
        reserveAmmo = Mathf.Max(reserveAmmo, maxReserveAmmo);
        OnAmmoChanged?.Invoke();
        return true;
    }

    public void ReloadOneRound()
    {
        TransferReloadAmmo(1);
    }

    private void TransferReloadAmmo(int maxRounds)
    {
        int missingAmmo = magazineSize - currentAmmo;
        int ammoToReload = Mathf.Min(maxRounds, Mathf.Min(missingAmmo, reserveAmmo));
        if (ammoToReload <= 0)
            return;

        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;

        OnAmmoChanged?.Invoke();
    }

    protected virtual bool CanShoot() => true;

    protected bool TryResolvePlayerCamera(ref Camera playerCamera)
    {
        // The equipped gun survives scene travel, while the scene camera does not.
        // Also covers guns instantiated before the camera becomes available.
        if (playerCamera == null || !playerCamera.isActiveAndEnabled)
            playerCamera = Camera.main;
        if (playerCamera != null && playerCamera.isActiveAndEnabled)
        {
            return true;
        }
        return false;
    }

    protected abstract void Shoot();
}
