using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;
using UniDoom.UI;

/// <summary>A nearby keyed picture latch reveals the archive without precision aiming.</summary>
[DisallowMultipleComponent]
public sealed class DirectorSecretPassage : MonoBehaviour
{
    [SerializeField] OpenDoorWithKey concealedDoor;
    [FormerlySerializedAs("bookTarget"), SerializeField] Collider interactionTarget;
    [FormerlySerializedAs("bookLever"), SerializeField] Transform pictureLever;
    [SerializeField] Light passageLight;
    [SerializeField] AudioSource mechanism;
    [SerializeField, Min(.5f)] float interactionDistance = 2.1f;
    [SerializeField, Min(.1f)] float latchDuration = .65f;
    [SerializeField, Min(0)] float revealedIntensity = 1.6f;

    public bool Revealed { get; private set; }
    public Collider InteractionTarget => interactionTarget;
    public bool IsNearby { get; private set; }
    readonly RaycastHit[] obstructionHits = new RaycastHit[16];
    PlayerMovement currentPlayer;
    DirectorSecretMemory memory;
    Quaternion closedLever;
    Coroutine opening;
    string message;
    float messageUntil;
    bool hadDeskKey;
    GUIStyle promptStyle;

    static bool GameplayBlocked => GameMenus.BlocksGameplay || DoorReloadScreen.IsReloading ||
        DeathScreen.IsVisible || MouseSensitivityMenu.IsOpen || Time.timeScale == 0;

    public string HintText
    {
        get
        {
            if (GameplayBlocked || DirectorRitualBook.HasInteractionFocus) return string.Empty;
            if (Revealed || opening != null) return string.Empty;
            if (Time.unscaledTime < messageUntil) return message;
            var keys = currentPlayer != null ? currentPlayer.GetComponent<PlayerPickupKey>() : null;
            if (IsNearby) return keys != null && keys.key03
                ? "[E]  Usar a chave no cadeado do quadro"
                : "[E]  Examinar o cadeado do quadro";
            return keys != null && keys.key03
                ? "Na parede, o brasão guarda mais do que um nome.\nLiberte o que o prende."
                : string.Empty;
        }
    }

    void Awake()
    {
        if (concealedDoor == null || interactionTarget == null || pictureLever == null)
        {
            Debug.LogError("Secret archive needs its door, picture collider and latch.", this);
            enabled = false;
            return;
        }
        closedLever = pictureLever.localRotation;
        if (passageLight != null) passageLight.intensity = 0;
    }

    void Update()
    {
        IsNearby = false;
        if (GameplayBlocked) return;
        PlayerMovement player = GameManager.SessionPlayer;
        if (player == null) return;
        if (player != currentPlayer)
        {
            currentPlayer = player;
            memory = player.GetComponent<DirectorSecretMemory>();
            if (memory == null) memory = player.gameObject.AddComponent<DirectorSecretMemory>();
        }
        var keys = player.GetComponent<PlayerPickupKey>();
        bool hasDeskKey = keys != null && keys.key03;
        if (hasDeskKey && !hadDeskKey) messageUntil = 0;
        hadDeskKey = hasDeskKey;
        if (!Revealed && memory.ArchiveOpened && keys != null && keys.key03)
        {
            if (concealedDoor.TryUnlock(keys))
            {
                Revealed = true;
                pictureLever.localRotation = closedLever * Quaternion.Euler(0, 12, 0);
            }
        }
        if (Revealed)
        {
            if (passageLight != null) passageLight.intensity = Mathf.MoveTowards(passageLight.intensity,
                revealedIntensity, Time.deltaTime * Mathf.Max(.65f, revealedIntensity));
            if (mechanism != null && concealedDoor.IsFullyOpen && mechanism.isPlaying) mechanism.Stop();
            return;
        }
        IsNearby = CanInteract(player);
        bool usePressed = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
            Input.GetKeyDown(KeyCode.E);
        if (IsNearby && usePressed) TryActivate(player);
    }

    public bool CanInteract(PlayerMovement player)
    {
        if (!isActiveAndEnabled || player == null || interactionTarget == null ||
            !interactionTarget.enabled || !interactionTarget.gameObject.activeInHierarchy) return false;
        var controller = player.GetComponent<CharacterController>();
        Vector3 origin = controller != null ? controller.bounds.center : player.transform.position;
        Vector3 target = interactionTarget.bounds.center;
        Vector3 offset = target - origin;
        if (Mathf.Abs(offset.y) > 2f || new Vector2(offset.x, offset.z).sqrMagnitude >
            interactionDistance * interactionDistance) return false;
        float distance = offset.magnitude;
        if (distance < .01f) return true;
        int count = Physics.RaycastNonAlloc(origin, offset / distance, obstructionHits, distance,
            ~0, QueryTriggerInteraction.Ignore);
        if (count == obstructionHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = obstructionHits[i].collider;
            if (hit != interactionTarget && hit.GetComponentInParent<PlayerMovement>() != player)
                return false;
        }
        return true;
    }

    public bool TryActivate(PlayerMovement player)
    {
        if (Revealed || opening != null || GameplayBlocked || !CanInteract(player)) return false;
        var life = player.GetComponent<PlayerLife>();
        if (life != null && life.IsDead) return false;
        var keys = player.GetComponent<PlayerPickupKey>();
        if (keys == null || !keys.key03)
        {
            message = "O cadeado precisa da chave sobre a mesa do diretor.";
            messageUntil = Time.unscaledTime + 3;
            return false;
        }
        opening = StartCoroutine(ReleaseLatch(keys));
        return true;
    }

    IEnumerator ReleaseLatch(PlayerPickupKey keys)
    {
        if (mechanism != null) mechanism.Play();
        float elapsed = 0;
        while (elapsed < latchDuration)
        {
            elapsed += Time.deltaTime;
            pictureLever.localRotation = closedLever * Quaternion.Euler(0, 12 * Mathf.SmoothStep(0, 1,
                Mathf.Clamp01(elapsed / latchDuration)), 0);
            yield return null;
        }
        if (concealedDoor.TryUnlock(keys))
        {
            Revealed = true;
            var remembered = keys.GetComponent<DirectorSecretMemory>();
            if (remembered == null) remembered = keys.gameObject.AddComponent<DirectorSecretMemory>();
            remembered.RememberArchive();
            message = null;
            messageUntil = 0;
        }
        else
        {
            pictureLever.localRotation = closedLever;
            if (mechanism != null) mechanism.Stop();
        }
        opening = null;
    }

    void OnDisable()
    {
        IsNearby = false;
        if (opening != null)
        {
            StopCoroutine(opening);
            opening = null;
            if (!Revealed && pictureLever != null) pictureLever.localRotation = closedLever;
        }
        if (mechanism != null) mechanism.Stop();
    }

    void OnGUI()
    {
        string words = HintText;
        if (string.IsNullOrEmpty(words)) return;
        if (promptStyle == null) promptStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
        promptStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 52f), 14, 24);
        Rect rect = new Rect(Screen.width * .23f, Screen.height * .79f, Screen.width * .54f, 64);
        Color previous = GUI.color;
        GUI.color = new Color(0, 0, 0, .85f);
        GUI.Label(new Rect(rect.x + 1, rect.y + 2, rect.width, rect.height), words, promptStyle);
        GUI.color = new Color(.80f, .77f, .65f);
        GUI.Label(rect, words, promptStyle);
        GUI.color = previous;
    }
}
