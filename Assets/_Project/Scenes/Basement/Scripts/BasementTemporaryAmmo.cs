using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Temporary basement testing supply. Remove this script to restore normal ammunition.</summary>
public sealed class BasementTemporaryAmmo : MonoBehaviour
{
    const int StartingReserve = 5000;
    readonly HashSet<Gun> suppliedWeapons = new HashSet<Gun>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetHook() => SceneManager.sceneLoaded -= OnSceneLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InstallHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.path != "Assets/_Project/Scenes/Basement/Basement.unity") return;
        var host = new GameObject("[Temporary basement ammo]");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<BasementTemporaryAmmo>();
    }

    void Update()
    {
        var player = GameManager.SessionPlayer;
        if (player == null || SceneManager.GetActiveScene() != gameObject.scene) return;
        var weapons = player.GetComponent<PlayerWeaponManager>();
        var gun = weapons != null ? weapons.CurrentGun : player.GetComponentInChildren<Gun>();
        if (gun == null || !suppliedWeapons.Add(gun)) return;
        // Supply each weapon once, including pickups, without making ammunition infinite.
        if (gun.ReserveAmmo < StartingReserve)
            gun.AddAmmo(StartingReserve - gun.ReserveAmmo);
    }
}
