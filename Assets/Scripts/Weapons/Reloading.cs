using System;
using System.Collections;
using UnityEngine;

public class Reloading : MonoBehaviour
{
    [SerializeField] private float reloadTime = 2f;

    public bool IsReloading { get; private set; }

    public event Action OnReloadStarted;
    public event Action OnReloadFinished;

    public void StartReload(Gun gun)
    {
        if (gun == null)
            return;

        if (IsReloading)
            return;

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
        
        if (gun is Pistol)
        {
            SFXManager.Instance.PlayPistolRechargeSound();
        }
        else if (gun is Shotgun)
        {
            SFXManager.Instance.PlayShotgunRechargeSound();
        }
        
        

        yield return new WaitForSeconds(reloadTime);
        
        
        

        gun.FinishReload();

        IsReloading = false;
        OnReloadFinished?.Invoke();
    }
}