using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniDoom.UI;

/// <summary>Session key checkpoints that follow the persistent player between rooms.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerLife), typeof(CharacterController))]
public class PlayerRespawn : MonoBehaviour
{
    private const int StartingLives = 3;
    [SerializeField, Min(0f)] private float respawnProtection = 2f;

    public int RemainingLives { get; private set; } = StartingLives;
    public bool HasLives => RemainingLives > 0;

    public int CheckpointKeyIndex { get; private set; } = -1;
    public Vector3 RespawnPosition { get; private set; }
    public Quaternion RespawnRotation { get; private set; }
    public string RespawnScenePath { get; private set; }
    public bool HasCheckpoint => CheckpointKeyIndex >= 0;

    private PlayerLife life;
    private PlayerMovement movement;
    private CharacterController controller;
    private CinemachinePanTilt[] cameraAxes;
    private Vector2[] savedLook;
    private DeathScreen screen;
    private readonly List<CinemachineInputAxisController> frozenInputs = new();
    private float previousTimeScale;
    private bool previousMovementEnabled;
    private bool ownsPause;
    private bool respawning;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    private void Awake()
    {
        life = GetComponent<PlayerLife>();
        movement = GetComponent<PlayerMovement>();
        controller = GetComponent<CharacterController>();
        cameraAxes = GetComponentsInChildren<CinemachinePanTilt>(true);
        savedLook = new Vector2[cameraAxes.Length];
        // Cinemachine uses world pan: the prefab's zero would ignore the spawn's facing.
        foreach (var aim in cameraAxes)
        {
            aim.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;
            aim.PanAxis.Value = transform.eulerAngles.y;
            aim.TiltAxis.Value = 0f;
        }
        CapturePose(); // Instantiate has already applied GameManager's initial spawn pose.
        var ui = new GameObject("Death & Checkpoint UI", typeof(RectTransform));
        DontDestroyOnLoad(ui);
        screen = ui.AddComponent<DeathScreen>();
        screen.Bind(this);
        life.OnDeath += HandleDeath;
    }

    private void CapturePose()
    {
        RespawnPosition = transform.position;
        RespawnRotation = transform.rotation;
        RespawnScenePath = SceneManager.GetActiveScene().path;
        for (int i = 0; i < cameraAxes.Length; i++)
            savedLook[i] = new Vector2(cameraAxes[i].PanAxis.Value, cameraAxes[i].TiltAxis.Value);
    }

    public void SaveCheckpoint(int keyIndex)
    {
        if (life.IsDead || respawning || keyIndex < 0 || keyIndex > 2) return;
        CapturePose();
        // Keys float. Ground the player's pose without ever using the key's visual height.
        // Keep the current position if no floor is within reach (e.g. a custom moving platform).
        Vector3 origin = controller.bounds.center;
        float nearest = float.PositiveInfinity;
        foreach (RaycastHit hit in Physics.RaycastAll(origin, Vector3.down, controller.bounds.extents.y + 3f,
                     ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<IDamageable>() != null ||
                hit.normal.y < 0.6f || hit.distance >= nearest) continue;
            nearest = hit.distance;
            float rootToFeet = transform.position.y - controller.bounds.min.y;
            RespawnPosition = new Vector3(transform.position.x, hit.point.y + rootToFeet + controller.skinWidth, transform.position.z);
        }
        CheckpointKeyIndex = keyIndex;
        screen.ShowCheckpoint(keyIndex);
    }

    private void HandleDeath()
    {
        EnterDeath(true);
    }

    private void EnterDeath(bool consumeLife)
    {
        if (ownsPause) return;
        if (consumeLife) RemainingLives = Mathf.Max(0, RemainingLives - 1);
        GameMenus.Instance?.CloseForDeath();
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        if (MouseSensitivityMenu.Instance != null && MouseSensitivityMenu.Instance.CloseForDeath())
        {
            previousCursorLock = CursorLockMode.Locked;
            previousCursorVisible = false;
        }
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        Time.timeScale = 0f;
        if (movement != null)
        {
            previousMovementEnabled = movement.MovementEnabled || DoorReloadScreen.IsReloading;
            movement.MovementEnabled = false;
            movement.ResetMotion();
        }
        foreach (Reloading reload in GetComponentsInChildren<Reloading>()) reload.CancelReload();
        frozenInputs.Clear();
        foreach (var input in GetComponentsInChildren<CinemachineInputAxisController>())
        {
            if (input.enabled || DoorReloadScreen.IsReloading) frozenInputs.Add(input);
            input.enabled = false;
        }
        screen.ShowDeath();
        UnlockCursor();
    }

    private void LateUpdate()
    {
        if (!ownsPause) return;
        UnlockCursor();
        // Other overlays can finish a transition during death; keep ownership of the camera.
        foreach (var input in frozenInputs)
            if (input != null) input.enabled = false;
    }

    private static void UnlockCursor()
    {
        if (GameMenus.Instance != null)
        {
            GameMenus.Instance.ApplyCursorState();
            return;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void TryRespawn()
    {
        if (!life.IsDead || !HasLives || respawning || DoorReloadScreen.IsReloading || !screen.CanRespawn) return;
        respawning = true;
        StartCoroutine(RespawnRoutine());
    }

    public void RestartCheckpoint()
    {
        if (!HasLives || respawning || DoorReloadScreen.IsReloading) return;
        if (!ownsPause) EnterDeath(false);
        respawning = true;
        StartCoroutine(RespawnRoutine());
    }

    public void PrepareForMainMenu()
    {
        StopAllCoroutines();
        if (screen != null) screen.HideDeath();
        ResumeWorld();
        respawning = false;
    }

    private IEnumerator RespawnRoutine()
    {
        bool restored = false;
        yield return DoorReloadScreen.PlayCheckpointTransition(RespawnScenePath, RestoreCheckpoint(() => restored = true));
        if (restored) ResumeWorld();
        respawning = false;
    }

    private IEnumerator RestoreCheckpoint(System.Action onRestored)
    {
        if (SceneManager.GetActiveScene().path != RespawnScenePath)
        {
            AsyncOperation load = null;
            string loadError = null;
            try
            {
                if (string.IsNullOrWhiteSpace(RespawnScenePath) || !Application.CanStreamedLevelBeLoaded(RespawnScenePath))
                    loadError = $"Cena do checkpoint indisponível no Build Settings: '{RespawnScenePath}'.";
                else
                    load = SceneManager.LoadSceneAsync(RespawnScenePath, LoadSceneMode.Single);
            }
            catch (System.Exception error) { loadError = error.Message; }
            if (load == null)
            {
                Debug.LogError($"[PlayerRespawn] Não foi possível retornar ao checkpoint. {loadError}");
                screen.ShowDeath();
                yield break;
            }
            while (!load.isDone) yield return null;
            // Allow the room's Start methods and the duplicate GameManager cleanup to finish under black.
            yield return null;
        }
        bool wasEnabled = controller.enabled;
        controller.enabled = false;
        Vector3 displacement = RespawnPosition - transform.position;
        transform.SetPositionAndRotation(RespawnPosition, RespawnRotation);
        if (movement != null) movement.ResetMotion();
        for (int i = 0; i < cameraAxes.Length; i++)
        {
            cameraAxes[i].PanAxis.Value = savedLook[i].x;
            cameraAxes[i].TiltAxis.Value = savedLook[i].y;
        }
        foreach (var camera in GetComponentsInChildren<CinemachineCamera>())
        {
            camera.OnTargetObjectWarped(transform, displacement);
            // Discard cached damping/collision correction from the death location.
            camera.PreviousStateIsValid = false;
        }
        Physics.SyncTransforms();
        controller.enabled = wasEnabled;
        life.Revive(respawnProtection);
        life.RestoreHealth();
        screen.HideDeath();
        onRestored();
    }

    private void ResumeWorld()
    {
        if (!ownsPause) return;
        ownsPause = false;
        Time.timeScale = previousTimeScale;
        if (movement != null)
            movement.MovementEnabled = previousMovementEnabled && !DoorReloadScreen.IsReloading;
        foreach (var input in frozenInputs)
            if (input != null) input.enabled = !DoorReloadScreen.IsReloading && !MouseSensitivityMenu.IsOpen;
        frozenInputs.Clear();
        if (GameMenus.Instance != null) GameMenus.Instance.ApplyCursorState();
        else
        {
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (screen != null) screen.HideDeath();
        ResumeWorld();
        respawning = false;
    }

    private void OnDestroy()
    {
        if (life != null) life.OnDeath -= HandleDeath;
        if (screen != null) Destroy(screen.gameObject);
        ResumeWorld();
    }
}
