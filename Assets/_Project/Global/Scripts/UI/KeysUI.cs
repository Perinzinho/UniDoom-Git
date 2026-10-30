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
        for (int i = 0; i < keyIcons.Length; i++)
        {
            var icon = keyIcons[i];
            icon.SetActive(false);
            var rect = icon.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(42, 42);
            rect.anchoredPosition = new Vector2(53 + i * 54, -92);
        }
        livesText = MenuStyle.Text("Remaining lives", transform, "VIDAS  3 / 3", 32, 28, 220, 36, 26, MenuStyle.Paper);
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
