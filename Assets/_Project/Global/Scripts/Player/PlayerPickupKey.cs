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
        if (other.CompareTag("Key01")) { key01 = true; Collect(0, other); }
        else if (other.CompareTag("Key02")) { key02 = true; Collect(1, other); }
        else if (other.CompareTag("Key03")) { key03 = true; Collect(2, other); }
    }

    private void Collect(int index, Collider key)
    {
        SFXManager.Instance.PlayKeyPickupSound();
        Destroy(key.gameObject);
        keysUI.ShowKey(index);
    }
}