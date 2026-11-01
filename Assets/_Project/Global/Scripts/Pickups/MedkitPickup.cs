using UnityEngine;

[DisallowMultipleComponent]
public class MedkitPickup : MonoBehaviour
{
    [Header("Heal Settings")]
    [SerializeField] private int healAmount = 50;
    private bool consumed;

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    public bool TryCollect(Collider other)
    {
        if (consumed || other == null || healAmount <= 0 || UniDoom.UI.DeathScreen.IsVisible)
            return false;

        PlayerLife life = other.GetComponentInParent<PlayerLife>();
        if (life == null || !life.CompareTag("Player") || life.IsDead || life.CurrentHealth >= life.MaxHealth)
            return false;

        // The player's multiple colliders may enter during the same physics step.
        consumed = true;
        foreach (Collider trigger in GetComponents<Collider>())
            trigger.enabled = false;
        life.Heal(healAmount);
        GameplayAudio.Instance?.PlayMedkitPickup();
        Destroy(gameObject);
        return true;
    }
}
