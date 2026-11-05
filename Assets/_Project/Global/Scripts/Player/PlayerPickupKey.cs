using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPickupKey : MonoBehaviour
{
    public bool key01, key02, key03;

    private KeysUI keysUI;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RemoveOwnedKeys();
    }

    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RemoveOwnedKeys();

    private void RemoveOwnedKeys()
    {
        // The player survives room changes; the room's pickups do not.
        // Hide owned keys before the newly loaded room can render or run physics.
        if (key01) RemoveKeysWithTag("Key01");
        if (key02) RemoveKeysWithTag("Key02");
        if (key03) RemoveKeysWithTag("Key03");
    }

    private void RemoveKeysWithTag(string tag)
    {
        foreach (GameObject key in GameObject.FindGameObjectsWithTag(tag))
        {
            key.SetActive(false);
            Destroy(key);
        }
    }

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
            // Duplicate pickups must never move the saved checkpoint.
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
