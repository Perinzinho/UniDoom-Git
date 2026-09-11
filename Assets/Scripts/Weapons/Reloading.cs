using System.Collections;
using UnityEngine;

public class Reloading : MonoBehaviour
{
    [SerializeField] private float reloadTime = 2f; // reload duration

    public bool IsReloading { get; private set; } // indicates if the weapon is currently reloading

    public void StartReload(Gun gun)  // starts reloading for the given weapon
    {
        if (gun == null) // check if weapon is null
            return;

        if (IsReloading) // prevent starting a reload if already reloading
            return;

        StartCoroutine(ReloadCoroutine(gun));  // start reload process
    }

    private IEnumerator ReloadCoroutine(Gun gun)  // controls reload timing
    {
        IsReloading = true;  // mark weapon as reloading

        DebugUI.Log($"{gun.name}: reloading...");

        yield return new WaitForSeconds(reloadTime);   // wait for reload duration without freezing the game

        gun.FinishReload();  // notify weapon that reload finished and update ammo

        IsReloading = false;  // mark reload as complete
    }
}
