using System;
using System.Collections;
using UnityEngine;

public class Recharging : MonoBehaviour
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

    private IEnumerator ReloadCoroutine(Gun gun)
    {
        OnReloadStarted?.Invoke();

        DebugUI.Log($"{gun.name}: recarregando...");

        yield return new WaitForSeconds(reloadTime);

        gun.FinishReload();

        IsReloading = false;
        OnReloadFinished?.Invoke();
    }
}