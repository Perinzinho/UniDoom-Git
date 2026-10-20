using UnityEngine;

public class KeysUI : MonoBehaviour
{
    [SerializeField] private GameObject[] keyIcons; // Key01, Key02, Key03

    private void Awake()
    {
        foreach (var icon in keyIcons)
            icon.SetActive(false);
    }

    public void ShowKey(int index)
    {
        keyIcons[index].SetActive(true);
    }
}