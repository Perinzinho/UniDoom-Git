using UnityEngine;
using UnityEngine.InputSystem;
using UniDoom.UI;

/// <summary>A persistent readable prop opens its original image in the shared paused viewer.</summary>
[DisallowMultipleComponent]
public sealed class DirectorRitualBook : MonoBehaviour
{
    [SerializeField] Texture2D document;
    [SerializeField] Collider interactionTarget;
    [SerializeField] Renderer pages;
    [SerializeField] Light attentionLight;
    [SerializeField, Min(.5f)] float interactionDistance = 2.8f;
    [SerializeField] string interactionHint = "[E]  Examinar o livro";

    readonly RaycastHit[] hits = new RaycastHit[24];
    MaterialPropertyBlock pageProperties;
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    static DirectorRitualBook focusedBook;
    public static bool HasInteractionFocus => focusedBook != null && focusedBook.IsNearby;
    public bool IsNearby { get; private set; }
    public Texture2D Document => document;
    public Collider InteractionTarget => interactionTarget;

    void Awake()
    {
        if (document == null || interactionTarget == null)
        {
            Debug.LogError("The readable prop needs its document and interaction collider.", this);
            enabled = false;
            return;
        }
        pageProperties = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (pages != null)
        {
            pageProperties ??= new MaterialPropertyBlock();
            float pulse = .5f + .5f * Mathf.Sin(Time.time * 2.1f);
            pages.GetPropertyBlock(pageProperties);
            pageProperties.SetColor(EmissionColor, Color.white * Mathf.Lerp(.14f, .42f, pulse));
            pages.SetPropertyBlock(pageProperties);
            if (attentionLight != null) attentionLight.intensity = Mathf.Lerp(.15f, .35f, pulse);
        }
        IsNearby = CanInteract(GameManager.SessionPlayer);
        if (IsNearby) focusedBook = this;
        else if (focusedBook == this) focusedBook = null;
        if (IsNearby && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) TryOpen();
    }

    public bool CanInteract(PlayerMovement player)
    {
        if (!isActiveAndEnabled || document == null || player == null || interactionTarget == null ||
            !interactionTarget.enabled || GameMenus.BlocksGameplay || DoorReloadScreen.IsReloading ||
            DeathScreen.IsVisible || MouseSensitivityMenu.IsOpen || Time.timeScale <= 0) return false;
        if (player.TryGetComponent<PlayerLife>(out var life) && life.IsDead) return false;
        var camera = Camera.main;
        if (camera == null) return false;
        Vector3 origin = camera.transform.position;
        Vector3 offset = interactionTarget.bounds.center - origin;
        float distance = offset.magnitude;
        if (distance > interactionDistance || distance < .01f ||
            Vector3.Dot(camera.transform.forward, offset / distance) < .7f) return false;
        int count = Physics.RaycastNonAlloc(origin, offset / distance, hits, distance, ~0, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i].collider;
            if (hit == interactionTarget || hit.transform.IsChildOf(transform) ||
                hit.GetComponentInParent<PlayerMovement>() == player) continue;
            return false;
        }
        return true;
    }

    public bool TryOpen()
    {
        if (!CanInteract(GameManager.SessionPlayer) || GameMenus.Instance == null) return false;
        IsNearby = false;
        return GameMenus.Instance.TryOpenDocument(document, this);
    }

    void OnDisable()
    {
        IsNearby = false;
        if (focusedBook == this) focusedBook = null;
        GameMenus.Instance?.CloseDocumentOwnedBy(this);
    }

    void OnGUI()
    {
        if (!IsNearby || GameMenus.BlocksGameplay) return;
        GameplayNotice.Draw(interactionHint);
    }
}
