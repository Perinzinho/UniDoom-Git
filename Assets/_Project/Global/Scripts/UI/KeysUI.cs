using TMPro;
using UnityEngine;
using UniDoom.UI;

public class KeysUI : MonoBehaviour
{
    [SerializeField] private GameObject[] keyIcons; // Key01, Key02, Key03
    private PlayerRespawn player;
    private TMP_Text livesText;

    private void Awake()
    {
        foreach (var icon in keyIcons)
        {
            icon.SetActive(false);
            var rect = icon.GetComponent<RectTransform>();
            rect.anchoredPosition += Vector2.down * 42f;
        }
        livesText = MenuStyle.Text("Remaining lives", transform, "VIDAS  3 / 3", 12, 6, 180, 30, 23, MenuStyle.Paper);
    }

    public void SetPlayer(PlayerRespawn newPlayer)
    {
        if (player != null) player.OnLivesChanged -= UpdateLives;
        player = newPlayer;
        if (player != null) player.OnLivesChanged += UpdateLives;
        UpdateLives(player != null ? player.RemainingLives : 0);
    }

    private void UpdateLives(int remaining) => livesText.text = $"VIDAS  {remaining} / 3";

    private void OnDestroy()
    {
        if (player != null) player.OnLivesChanged -= UpdateLives;
    }

    public void ShowKey(int index)
    {
        keyIcons[index].SetActive(true);
    }
}
