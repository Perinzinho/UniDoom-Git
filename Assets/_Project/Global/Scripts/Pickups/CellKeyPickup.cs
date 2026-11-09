using System.Collections;
using UnityEngine;

/// <summary>One numbered teacher-cell key in the existing persistent player inventory.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(SphereCollider))]
public sealed class CellKeyPickup : MonoBehaviour
{
    [SerializeField, Range(1, 8)] int cellId = 1;
    bool collected;
    public int CellId => cellId;

    void Awake() => GetComponent<SphereCollider>().isTrigger = true;

    IEnumerator Start()
    {
        while (GameManager.SessionPlayer == null) yield return null;
        var keys = GameManager.SessionPlayer.GetComponent<PlayerCellKeys>();
        if (PrisonProgress.HasCellKey(cellId, keys)) Hide();
    }

    void OnTriggerEnter(Collider other) => TryCollect(other);
    void OnTriggerStay(Collider other) => TryCollect(other);

    void TryCollect(Collider other)
    {
        if (collected || Time.timeScale <= 0 || UniDoom.UI.GameMenus.BlocksGameplay ||
            UniDoom.UI.DeathScreen.IsVisible || UniDoom.UI.DoorReloadScreen.IsReloading) return;
        var player = other.GetComponentInParent<PlayerPickupKey>();
        if (!player || !GameManager.SessionPlayer || player.gameObject != GameManager.SessionPlayer.gameObject) return;
        var life = player.GetComponent<PlayerLife>();
        if (life && life.IsDead) return;
        var keys = player.GetComponent<PlayerCellKeys>();
        if (!keys) keys = player.gameObject.AddComponent<PlayerCellKeys>();
        if (keys.TryAddKey(PrisonProgress.CellKeyId(cellId))) SFXManager.Instance?.PlayKeyPickupSound();
        Hide();
    }

    void Hide() { collected = true; gameObject.SetActive(false); }
}
