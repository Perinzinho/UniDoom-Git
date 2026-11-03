using UnityEngine;
using UnityEngine.UI;
using UniDoom.UI;

public class KeysUI : MonoBehaviour
{
    [SerializeField] private GameObject[] keyIcons; // Key01, Key02, Key03
    [SerializeField] private Sprite lifeSprite;
    private const int LifeSlots = 3;
    private PlayerRespawn player;
    private Material livesMaterial;
    private static readonly int RemainingLivesId = Shader.PropertyToID("_RemainingLives");

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
<<<<<<< HEAD
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
=======
        var hearts = MenuStyle.Image("Remaining lives", transform, 20, 8, 198, 66, Color.white);
        hearts.sprite = Resources.Load<Sprite>("Textures/UI/RemainingLives");
        hearts.preserveAspect = true;
        var template = Resources.Load<Material>("Materials/UI/RemainingLives");
        if (hearts.sprite == null || template == null)
        {
            Debug.LogError("Missing remaining-lives HUD sprite or material.", this);
            hearts.enabled = false;
            return;
        }
        // A separate instance keeps this HUD's state out of the persistent material asset.
        livesMaterial = new Material(template);
        hearts.material = livesMaterial;
        UpdateLives(player != null ? player.RemainingLives : 3);
>>>>>>> ef89cee4cd73b40eb9f6af170b90bc282a50c62d
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
<<<<<<< HEAD
        remaining = Mathf.Clamp(remaining, 0, LifeSlots);
        for (int i = 0; i < LifeSlots; i++)
        {
            bool available = i < remaining;
            lifeIcons[i].color = available ? Color.white : new Color(.10f, .10f, .10f, .7f);
            lifeOutlines[i].effectColor = available
                ? new Color(0, 0, 0, .85f) : new Color(.49f, .46f, .40f, .65f);
        }
=======
        if (livesMaterial != null)
            livesMaterial.SetFloat(RemainingLivesId, Mathf.Clamp(remaining, 0, 3));
>>>>>>> ef89cee4cd73b40eb9f6af170b90bc282a50c62d
    }

    private void OnDestroy()
    {
        if (player != null) player.OnLivesChanged -= UpdateLives;
        if (livesMaterial != null) Destroy(livesMaterial);
    }

    public void ShowKey(int index)
    {
        keyIcons[index].SetActive(true);
    }
}
