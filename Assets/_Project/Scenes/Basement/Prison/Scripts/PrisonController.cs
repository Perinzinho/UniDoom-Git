using TMPro;
using UniDoom.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>Prison interaction, cell-specific voices and the post-boss menu arrival.</summary>
[DisallowMultipleComponent]
public sealed class PrisonController : MonoBehaviour
{
    [SerializeField] DirectorBossEncounter encounter;
    [SerializeField] BasementPrisonPassage passage;
    [SerializeField] Transform arrivalPoint;
    [SerializeField] Sprite keySprite;
    [SerializeField, Min(1)] float interactionDistance = 3f;
    Canvas hud;
    TMP_Text prompt;
    Image keyIcon;
    TMP_Text keyList;
    PlayerCellKeys shownInventory;
    int shownRevision = -1;
    AudioSource activeVoice;
    PlayerMovement player;
    Camera view;

    void OnEnable()
    {
        if (encounter != null) encounter.onEncounterCompleted.AddListener(RememberVictory);
    }

    void Start()
    {
        if (PrisonProgress.BossDefeated)
        {
            encounter?.RestoreCompleted();
            passage?.RevealImmediately();
        }
        BuildHUD();
        if (PrisonProgress.AllCellsOpened) CompleteCellRescue(false);
    }

    void RememberVictory() => PrisonProgress.BossDefeated = true;

    public bool TryConfigurePlayerStart(Transform start, CharacterController controller)
    {
        if (start == null || controller == null || arrivalPoint == null || encounter == null || passage == null)
            return false;
        // Fit the same scaled capsule that GameManager instantiates, before its Start/checkpoint capture.
        Vector3 scale = controller.transform.lossyScale;
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(controller.height * Mathf.Abs(scale.y), radius * 2);
        float clearance = Mathf.Max(.1f, controller.skinWidth * Mathf.Abs(scale.y));
        Physics.SyncTransforms();
        Vector3 candidate = arrivalPoint.position;
        // Start below the low ceiling so the roof cannot be mistaken for the floor.
        if (!Physics.Raycast(candidate + Vector3.up * .5f, Vector3.down, out var floor, 4,
                ~0, QueryTriggerInteraction.Ignore) || floor.normal.y < .7f) return false;
        if (Physics.CheckCapsule(floor.point + Vector3.up * (clearance + radius),
                floor.point + Vector3.up * (clearance + height - radius), radius,
                ~0, QueryTriggerInteraction.Ignore)) return false;
        Quaternion rotation = arrivalPoint.rotation;
        Vector3 centerOffset = Vector3.Scale(controller.center, scale);
        start.SetPositionAndRotation(floor.point + Vector3.up * (height * .5f + clearance) -
            rotation * centerOffset, rotation);
        PrisonProgress.BossDefeated = true;
        encounter.RestoreCompleted();
        passage.RevealImmediately();
        return true;
    }

    void BuildHUD()
    {
        var root = new GameObject("Prison interaction HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.transform.SetParent(transform, false);
        hud = root.GetComponent<Canvas>();
        hud.renderMode = RenderMode.ScreenSpaceOverlay;
        hud.sortingOrder = 15;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        prompt = MenuStyle.Text("Cell interaction", root.transform, "", 0, 0, 1000, 60, 27, MenuStyle.Paper);
        var rect = prompt.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
        rect.pivot = new Vector2(.5f, 0);
        rect.anchoredPosition = new Vector2(0, 100);
        prompt.alignment = TextAlignmentOptions.Center;
        prompt.raycastTarget = false;
        keyIcon = MenuStyle.Image("Prison key", root.transform, 0, 0, 52, 70, Color.white);
        keyIcon.rectTransform.anchorMin = keyIcon.rectTransform.anchorMax = keyIcon.rectTransform.pivot = Vector2.one;
        keyIcon.rectTransform.anchoredPosition = new Vector2(-28, -28);
        keyIcon.sprite = keySprite;
        keyIcon.preserveAspect = true;
        keyIcon.raycastTarget = false;
        keyIcon.gameObject.SetActive(false);
        keyList = MenuStyle.Text("Numbered cell keys", root.transform, "", 0, 0, 500, 40, 20, MenuStyle.Paper);
        var listRect = keyList.rectTransform;
        listRect.anchorMin = listRect.anchorMax = listRect.pivot = Vector2.one;
        listRect.anchoredPosition = new Vector2(-28, -106);
        keyList.alignment = TextAlignmentOptions.Right;
        keyList.raycastTarget = false;

    }

    void Update()
    {
        if (hud == null) return;
        player = GameManager.SessionPlayer;
        bool playing = player != null && !GameMenus.BlocksGameplay && !DeathScreen.IsVisible &&
            !DoorReloadScreen.IsReloading && Time.timeScale > 0;
        hud.enabled = playing;
        if (!playing) return;
        var inventory = player.GetComponent<PlayerCellKeys>();
        if (inventory != shownInventory || (inventory ? inventory.Revision : -1) != shownRevision)
            RefreshKeys(inventory);
        if (PrisonProgress.AllCellsOpened)
        {
            prompt.text = "Uma surpresa te espera! Retorne para Unimar";
            return;
        }
        prompt.text = "";
        if (view == null || !view.gameObject.activeInHierarchy) view = Camera.main;
        if (view == null || !Physics.Raycast(view.transform.position, view.transform.forward,
                out var hit, interactionDistance, ~0, QueryTriggerInteraction.Ignore)) return;
        var gate = hit.collider.GetComponentInParent<PrisonCellGate>();
        if (gate == null || gate.IsOpen) return;
        bool hasCellKey = PrisonProgress.HasCellKey(gate.CellId, inventory);
        bool canOpen = PrisonProgress.CanOpenCell(gate.CellId, inventory);
        if (hasCellKey) keyIcon.sprite = CellKeySprites.Get(gate.CellId) ?? keySprite;
        prompt.text = canOpen ? "[E] ABRIR CELA " + gate.CellId :
            "CELA " + gate.CellId + " TRANCADA — ENCONTRE A CHAVE " + gate.CellId;
        bool interact = (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
        if (!interact) return;
        if (!canOpen)
        {
            GameplayAudio.Instance?.NotifyLockedDoorContact(gate);
            return;
        }
        if (!TryOpenCell(gate, player.GetComponent<PlayerPickupKey>())) return;
        prompt.text = "";
    }

    void RefreshKeys(PlayerCellKeys inventory)
    {
        shownInventory = inventory;
        shownRevision = inventory ? inventory.Revision : -1;
        var ids = new List<string>();
        int first = 0;
        for (int id = 1; id <= PrisonProgress.CellCount; id++)
            if (PrisonProgress.HasCellKey(id, inventory)) { ids.Add(id.ToString()); if (first == 0) first = id; }
        keyList.text = ids.Count > 0 ? "CHAVES DAS CELAS: " + string.Join(" · ", ids) : "";
        keyIcon.gameObject.SetActive(first != 0);
        if (first != 0) keyIcon.sprite = CellKeySprites.Get(first) ?? keySprite;
    }

    public bool TryOpenCell(PrisonCellGate gate, PlayerPickupKey playerKeys)
    {
        if (gate == null || !gate.transform.IsChildOf(transform) || !gate.TryOpen(playerKeys)) return false;
        if (activeVoice != null) activeVoice.Stop();
        activeVoice = gate.Voice;
        if (activeVoice != null && activeVoice.clip != null) activeVoice.Play();
        if (PrisonProgress.AllCellsOpened) CompleteCellRescue(true);
        return true;
    }

    void CompleteCellRescue(bool stopVoice)
    {
        if (stopVoice && activeVoice != null) activeVoice.Stop();
        foreach (var cell in GetComponentsInChildren<PrisonCellGate>(true)) cell.SetTeacherVisible(false);
        if (prompt != null)
        {
            var rect = prompt.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, -400);
            rect.sizeDelta = new Vector2(1200, 70);
            prompt.alignment = TextAlignmentOptions.Center;
            prompt.text = "Uma surpresa te espera! Retorne para Unimar";
        }
    }

    void OnDisable()
    {
        if (encounter != null) encounter.onEncounterCompleted.RemoveListener(RememberVictory);
        if (activeVoice != null) activeVoice.Stop();
        if (hud != null) hud.enabled = false;
    }
}
