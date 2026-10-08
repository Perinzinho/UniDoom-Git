using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [SerializeField] private int ammoAmount = 10;

    private void OnTriggerEnter(Collider other)
    {
        DebugUI.Log($"Trigger detected with: {other.name}, tag: {other.tag}");
        if (!other.CompareTag("Player"))
            return;

        // Search for Gun anywhere on the object that entered the trigger (or its children).
        Gun currentGun = other.GetComponentInChildren<Gun>();

        if (currentGun == null)
        {
            DebugUI.LogWarning("AmmoPickup: no weapon found on Player.");
            return;
        }

        currentGun.AddAmmo(ammoAmount);
        Destroy(gameObject);
    }
}
