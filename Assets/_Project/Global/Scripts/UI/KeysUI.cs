using UnityEngine;
using UnityEngine.UI;
using UniDoom.UI;

public class KeysUI : MonoBehaviour
{
    [SerializeField] private GameObject[] keyIcons; // Key01, Key02, Key03
    [SerializeField] private Sprite lifeSprite;
    private const int LifeSlots = 3;
    private PlayerRespawn player;
    private readonly Image[] lifeIcons = new Image[LifeSlots];
    private readonly Outline[] lifeOutlines = new Outline[LifeSlots];

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
        // Preserve the former label's top-left anchor, position and layout area.
        var livesRoot = MenuStyle.Rect("Remaining lives", transform, 32, 28, 220, 36);
        for (int i = 0; i < LifeSlots; i++)
        {
            // The existing anatomical sprite includes transparent padding around the heart.
            var heart = MenuStyle.Image("Life " + (i + 1), livesRoot, i * 48 - 6, -12, 48, 62, Color.white);
            heart.sprite = lifeSprite;
            heart.preserveAspect = true;
            lifeIcons[i] = heart;
            lifeOutlines[i] = heart.gameObject.AddComponent<Outline>();
            lifeOutlines[i].effectDistance = new Vector2(1, -1);
            lifeOutlines[i].useGraphicAlpha = false;
        }
        UpdateLives(LifeSlots);
    }

    public void SetPlayer(PlayerRespawn newPlayer)
    {
        if (player != null) player.OnLivesChanged -= UpdateLives;
        player = newPlayer;
        if (player != null) player.OnLivesChanged += UpdateLives;
        UpdateLives(player != null ? player.RemainingLives : 0);
    }

    private void UpdateLives(int remaining)
    {
        remaining = Mathf.Clamp(remaining, 0, LifeSlots);
        for (int i = 0; i < LifeSlots; i++)
        {
            bool available = i < remaining;
            lifeIcons[i].color = available ? Color.white : new Color(.10f, .10f, .10f, .7f);
            lifeOutlines[i].effectColor = available
                ? new Color(0, 0, 0, .85f) : new Color(.49f, .46f, .40f, .65f);
        }
    }

    private void OnDestroy()
    {
        if (player != null) player.OnLivesChanged -= UpdateLives;
    }

    public void ShowKey(int index)
    {
        keyIcons[index].SetActive(true);
    }
}
