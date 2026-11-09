using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class PrisonKeyPickup : MonoBehaviour
{
    [SerializeField, Range(1, 8)] int cellId = 1;
    void OnEnable()
    {
        var player = GameManager.SessionPlayer;
        if (player && PrisonProgress.HasCellKey(cellId, player.GetComponent<PlayerCellKeys>())) gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (UniDoom.UI.GameMenus.BlocksGameplay || Time.timeScale <= 0) return;
        var player = other.GetComponentInParent<PlayerPickupKey>();
        if (player == null) return;
        var life = player.GetComponent<PlayerLife>();
        if (life != null && life.IsDead) return;
        var inventory = player.GetComponent<PlayerCellKeys>();
        if (!inventory) inventory = player.gameObject.AddComponent<PlayerCellKeys>();
        if (inventory.TryAddKey(PrisonProgress.CellKeyId(cellId))) SFXManager.Instance?.PlayKeyPickupSound();
        gameObject.SetActive(false);
    }
}
