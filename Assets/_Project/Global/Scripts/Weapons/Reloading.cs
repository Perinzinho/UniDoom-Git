using System;
using System.Collections;
using UnityEngine;

public class Reloading : MonoBehaviour
{
    [Tooltip("Whole-magazine reload duration. Shotguns use one animation cycle per shell instead.")]
    [SerializeField] private float reloadTime = 2f;
    [Tooltip("Fallback duration per shotgun shell; the bound animation supplies its cycle duration.")]
    [SerializeField, Min(0.01f)] private float shotgunShellTime = 0.6f;

    private float reloadStartedAt;
    private float activeShellTime;

    public bool IsReloading { get; private set; }
    public int ReloadRoundCount { get; private set; }
    public float ReloadElapsedTime => IsReloading ? Mathf.Max(0f, Time.time - reloadStartedAt) : 0f;

    public void SetReloadCycleDuration(float duration)
    {
        if (!IsReloading && duration > 0f)
            shotgunShellTime = duration;
    }

    public event Action OnReloadStarted;
    public event Action OnReloadFinished;

    public void StartReload(Gun gun)
    {
        if (gun == null)
            return;

        if (IsReloading)
            return;

        ReloadRoundCount = Mathf.Min(gun.MagazineSize - gun.CurrentAmmo, gun.ReserveAmmo);
        if (ReloadRoundCount <= 0)
            return;

        reloadStartedAt = Time.time;
        activeShellTime = Mathf.Max(0.01f, shotgunShellTime);
        IsReloading = true;
        StartCoroutine(ReloadCoroutine(gun));
    }

    public void CancelReload()
    {
        if (!IsReloading)
            return;

        StopAllCoroutines();
        IsReloading = false;
        OnReloadFinished?.Invoke();
    }

    private void OnDisable()
    {
        CancelReload();
    }

    private IEnumerator ReloadCoroutine(Gun gun)
    {
        OnReloadStarted?.Invoke();

        DebugUI.Log($"{gun.name}: recarregando...");
        
        if (gun is Shotgun)
        {
            for (int round = 0; round < ReloadRoundCount; round++)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlayShotgunRechargeSound();

                // Use cumulative deadlines so frame delays never add time per shell.
                float roundFinishesAt = (round + 1) * activeShellTime;
                while (ReloadElapsedTime < roundFinishesAt)
                    yield return null;

                gun.ReloadOneRound();
                if (!IsReloading)
                    yield break;
            }
        }
        else
        {
            if (gun is Pistol && SFXManager.Instance != null)
                SFXManager.Instance.PlayPistolRechargeSound();

            yield return new WaitForSeconds(reloadTime);
            gun.FinishReload();
        }

        IsReloading = false;
        OnReloadFinished?.Invoke();
    }
}
