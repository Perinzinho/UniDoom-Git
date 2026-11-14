using UnityEngine;
using System;

public abstract class Gun : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] protected int magazineSize = 10;
    [SerializeField] protected int currentAmmo = 10;
    // Kept serialized so existing prefabs and dropped weapons preserve their data.
    // Player firearms now expose an unlimited reserve and never consume this value.
    [SerializeField] protected int reserveAmmo = 30;
    [Tooltip("Legacy finite-reserve value kept for prefab compatibility. Player reserve is unlimited.")]
    [SerializeField, Min(0)] protected int maxReserveAmmo = 30;

    [Header("Reload")]
    protected Reloading recharging;

    public int CurrentAmmo => currentAmmo;
    public bool HasUnlimitedReserve => true;
    public int ReserveAmmo => HasUnlimitedReserve ? int.MaxValue : reserveAmmo;
    public int MagazineSize => magazineSize;
    public int MaxReserveAmmo => HasUnlimitedReserve ? int.MaxValue : maxReserveAmmo;
    public bool IsAmmoFull => currentAmmo >= magazineSize &&
        (HasUnlimitedReserve || reserveAmmo >= maxReserveAmmo);
    public int ReloadRoundCount => recharging != null ? recharging.ReloadRoundCount : 0;
    public float ReloadElapsedTime => recharging != null ? recharging.ReloadElapsedTime : 0f;

    public void SetReloadCycleDuration(float duration)
    {
        if (this is Shotgun && recharging != null)
            recharging.SetReloadCycleDuration(duration);
    }

    public event Action OnShoot;
    public static event Action<Vector3, float> Gunshot;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetGunshotListeners() => Gunshot = null;
    private bool warnedMissingCamera;

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
        if (!isActiveAndEnabled || PlayerWeaponManager.InputBlocked) return;
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
        var shooter = GetComponentInParent<PlayerLife>();
        if (shooter != null)
            Gunshot?.Invoke(shooter.transform.position, this is Shotgun ? 26f : 18f);

        OnShoot?.Invoke();
    }

    public void TryReload()
    {
        if (!isActiveAndEnabled || PlayerWeaponManager.InputBlocked) return;
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

        if (!HasUnlimitedReserve && reserveAmmo <= 0)
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
        if (amount <= 0 || HasUnlimitedReserve)
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
        if (!HasUnlimitedReserve)
        {
            // Preserve surplus ammunition collected from ordinary enemy drops.
            reserveAmmo = Mathf.Max(reserveAmmo, maxReserveAmmo);
        }
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
        int ammoToReload = Mathf.Min(maxRounds, missingAmmo);
        if (!HasUnlimitedReserve)
            ammoToReload = Mathf.Min(ammoToReload, reserveAmmo);
        if (ammoToReload <= 0)
            return;

        currentAmmo += ammoToReload;
        if (!HasUnlimitedReserve)
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
