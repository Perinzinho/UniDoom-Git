using UnityEngine;
using UniDoom.UI;

[DisallowMultipleComponent, RequireComponent(typeof(SphereCollider))]
public sealed class LawSchoolMaintenanceKeyPickup : MonoBehaviour
{
    void Awake() => GetComponent<SphereCollider>().isTrigger = true;
    void OnTriggerEnter(Collider other) => Collect(other);
    void OnTriggerStay(Collider other) => Collect(other);

    void Collect(Collider other)
    {
        if (Time.timeScale <= 0 || GameMenus.BlocksGameplay || DeathScreen.IsVisible || DoorReloadScreen.IsReloading) return;
        var player = other.GetComponentInParent<PlayerPickupKey>();
        if (!player || !GameManager.SessionPlayer || player.gameObject != GameManager.SessionPlayer.gameObject) return;
        if (player.TryGetComponent<PlayerLife>(out var life) && life.IsDead) return;
        var inventory = player.GetComponent<PlayerCellKeys>();
        if (!inventory) inventory = player.gameObject.AddComponent<PlayerCellKeys>();
        if (inventory.TryAddKey(LawSchoolMaintenanceAccess.KeyId)) SFXManager.Instance?.PlayKeyPickupSound();
        gameObject.SetActive(false);
    }
}
