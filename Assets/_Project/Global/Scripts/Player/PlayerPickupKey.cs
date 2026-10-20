using UnityEngine;

public class PlayerPickupKey : MonoBehaviour
{
    public bool key01, key02, key03;

    private KeysUI keysUI;

    public void SetKeysUI(KeysUI ui)
    {
        keysUI = ui;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerLife life = GetComponent<PlayerLife>();
        if ((life != null && life.IsDead) || UniDoom.UI.DeathScreen.IsVisible) return;
        int index;
        bool owned;
        if (other.CompareTag("Key01")) { index = 0; owned = key01; key01 = true; }
        else if (other.CompareTag("Key02")) { index = 1; owned = key02; key02 = true; }
        else if (other.CompareTag("Key03")) { index = 2; owned = key03; key03 = true; }
        else return;

        if (!owned) Collect(index, other);
        else
        {
            // Returning to a room recreates its pickups, but must not move the saved checkpoint.
            other.enabled = false;
            Destroy(other.gameObject);
        }
    }

    private void Collect(int index, Collider key)
    {
        if (SFXManager.Instance != null) SFXManager.Instance.PlayKeyPickupSound();
        // Register only a newly collected key. Its floating visual is not a spawn point.
        GetComponent<PlayerRespawn>()?.SaveCheckpoint(index);
        key.enabled = false;
        Destroy(key.gameObject);
        if (keysUI != null) keysUI.ShowKey(index);
    }
}
