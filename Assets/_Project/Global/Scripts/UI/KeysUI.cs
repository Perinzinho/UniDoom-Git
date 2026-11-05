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
}
