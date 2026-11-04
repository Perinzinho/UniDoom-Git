using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class PrisonKeyPickup : MonoBehaviour
{
    void OnEnable()
    {
        if (PrisonProgress.HasKey) gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (PrisonProgress.HasKey || UniDoom.UI.GameMenus.BlocksGameplay || Time.timeScale <= 0) return;
        var player = other.GetComponentInParent<PlayerPickupKey>();
        if (player == null) return;
        var life = player.GetComponent<PlayerLife>();
        if (life != null && life.IsDead) return;
        PrisonProgress.HasKey = true;
        SFXManager.Instance?.PlayKeyPickupSound();
        gameObject.SetActive(false);
    }
}
