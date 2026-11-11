using UnityEngine;
using UnityEngine.UI;
using UniDoom.UI;

public class KeysUI : MonoBehaviour
{
    [SerializeField] private GameObject[] keyIcons; // Key01, Key02, Key03
    [SerializeField] private Sprite lifeSprite;
    private const int LifeSlots = 3;
    private const int MissionSlots = 3;
    private PlayerRespawn player;
    private Material livesMaterial;
    private Image[] missionIcons;
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
        if (livesMaterial != null)
            livesMaterial.SetFloat(RemainingLivesId, Mathf.Clamp(remaining, 0, 3));
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

    public void SetMissionItems(Sprite sprite, int count)
    {
        if (!sprite) { ClearMissionItems(); return; }
        count = Mathf.Clamp(count, 0, MissionSlots);
        if (missionIcons == null)
        {
            if (count == 0 || keyIcons == null || keyIcons.Length == 0 || !keyIcons[0]) return;
            missionIcons = new Image[MissionSlots];
            Transform parent = keyIcons[0].transform.parent;
            for (int i = 0; i < missionIcons.Length; i++)
            {
                var icon = MenuStyle.Image("Mission item " + (i + 1), parent, 0, 0, 42, 42, Color.white);
                var rect = icon.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(53 + i * 54, -146);
                icon.preserveAspect = true;
                missionIcons[i] = icon;
            }
        }
        for (int i = 0; i < missionIcons.Length; i++)
        {
            missionIcons[i].sprite = sprite;
            missionIcons[i].gameObject.SetActive(i < count);
        }
    }

    public void ClearMissionItems()
    {
        if (missionIcons == null) return;
        foreach (var icon in missionIcons) if (icon) icon.gameObject.SetActive(false);
    }
}
