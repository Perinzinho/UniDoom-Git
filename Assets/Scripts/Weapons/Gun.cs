using UnityEngine;
using System;

public abstract class Gun : MonoBehaviour
{
    [Header("Ammo")]
    [SerializeField] protected int magazineSize = 10;
    [SerializeField] protected int currentAmmo = 10;
    [SerializeField] protected int reserveAmmo = 30;

    [Header("Reload")]
    protected Reloading recharging;

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MagazineSize => magazineSize;

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
            DebugUI.Log($"{name}: não pode atirar enquanto recarrega.");
            return;
        }

        if (!HasAmmo())
        {
            DebugUI.Log($"{name}: sem munição no pente.");
            return;
        }

        Shoot();
        ConsumeAmmo();

        OnShoot?.Invoke();
    }

    public void TryReload()
    {
        if (recharging == null)
        {
            DebugUI.LogWarning($"{name}: componente Recharging não encontrado.");
            return;
        }

        if (recharging.IsReloading)
            return;

        if (currentAmmo >= magazineSize)
        {
            DebugUI.Log($"{name}: pente já está cheio.");
            return;
        }

        if (reserveAmmo <= 0)
        {
            DebugUI.Log($"{name}: sem munição reserva.");
            return;
        }

        recharging.StartReload(this);
    }

    protected bool HasAmmo()
    {
        return currentAmmo > 0;
    }

    protected void ConsumeAmmo()
    {
        currentAmmo--;
        DebugUI.Log($"{name}: {currentAmmo}/{magazineSize} | Reserva: {reserveAmmo}");
    }

    public void AddAmmo(int amount)
    {
        if (amount <= 0)
            return;

        reserveAmmo += amount;
        DebugUI.Log($"{name}: pegou {amount} munições. Reserva: {reserveAmmo}");
    }

    public void FinishReload()
    {
        int missingAmmo = magazineSize - currentAmmo;
        int ammoToReload = Mathf.Min(missingAmmo, reserveAmmo);

        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;

        DebugUI.Log($"{name}: recarga concluída. Pente: {currentAmmo}/{magazineSize} | Reserva: {reserveAmmo}");
    }

    protected abstract void Shoot();
}