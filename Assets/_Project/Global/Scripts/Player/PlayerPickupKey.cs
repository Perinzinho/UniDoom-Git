using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerPickupKey : MonoBehaviour
{
    public bool key01, key02, key03;

    private KeysUI keysUI;

    private void OnEnable() => SceneManager.sceneLoaded += RemoveCollectedKeys;
    private void OnDisable() => SceneManager.sceneLoaded -= RemoveCollectedKeys;

    private void RemoveCollectedKeys(Scene scene, LoadSceneMode mode)
    {
        for (int i = 0; i < 3; i++)
        {
            if (!HasKey(i)) continue;
            foreach (var key in GameObject.FindGameObjectsWithTag("Key0" + (i + 1)))
                if (key.scene == scene) Destroy(key);
        }
    }

    private bool HasKey(int index) => index == 0 ? key01 : index == 1 ? key02 : key03;

    public void SetKeysUI(KeysUI ui)
    {
        keysUI = ui;
        if (keysUI != null)
            for (int i = 0; i < 3; i++)
                if (HasKey(i)) keysUI.ShowKey(i);
    }

    private void OnTriggerEnter(Collider other)
    {
        var life = GetComponent<PlayerLife>();
        if ((life != null && life.IsDead) || UniDoom.UI.DoorReloadScreen.IsReloading ||
            UniDoom.UI.MouseSensitivityMenu.IsOpen) return;
        if (other.CompareTag("Key01")) Collect(0, other);
        else if (other.CompareTag("Key02")) Collect(1, other);
        else if (other.CompareTag("Key03")) Collect(2, other);
    }

    private void Collect(int index, Collider key)
    {
        if (HasKey(index)) { Destroy(key.gameObject); return; }
        if (index == 0) key01 = true;
        else if (index == 1) key02 = true;
        else key03 = true;
        GetComponent<PlayerRespawn>()?.SaveKeyCheckpoint(key.gameObject.scene);
        if (SFXManager.Instance != null) SFXManager.Instance.PlayKeyPickupSound();
        Destroy(key.gameObject);
        if (keysUI != null) keysUI.ShowKey(index);
    }
}
