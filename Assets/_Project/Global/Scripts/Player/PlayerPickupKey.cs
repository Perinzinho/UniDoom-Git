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
        if (other.CompareTag("Key01") && !key01) { key01 = true; Collect(0, other); }
        else if (other.CompareTag("Key02") && !key02) { key02 = true; Collect(1, other); }
        else if (other.CompareTag("Key03") && !key03) { key03 = true; Collect(2, other); }
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
