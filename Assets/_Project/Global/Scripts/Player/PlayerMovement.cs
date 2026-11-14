using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private const string LockedDoorMessage = "Porta trancada! Procure pela chave.";

    public float speed = 6f;
    public float gravity = -9.81f;
    public float groundedGravity = -2f;
    public Transform cameraTransform;

    [Header("Doom-style walk bob")]
    [SerializeField, Min(0f)] private float walkBobFrequency = 8.5f;
    [SerializeField] private Vector2 cameraBobAmplitude = new Vector2(.035f, .025f);
    [SerializeField, Min(.01f)] private float bobBlendSpeed = 10f;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 horizontalMove;
    private Transform viewBobTarget;
    private Vector3 viewBobOrigin;
    private float walkBobPhase;
    private float walkBobWeight;
    private bool viewBobReady;
    private Object lastLockedDoorNotice;
    private float nextLockedDoorNoticeTime;

    public float WalkBobPhase => walkBobPhase;
    public float WalkBobWeight => walkBobWeight;

    private bool movementEnabled = true;
    public bool MovementEnabled { get => movementEnabled; set => movementEnabled = value; }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        bool wasEnabled = controller != null && controller.enabled;
        if (wasEnabled) controller.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        velocity = Vector3.zero;
        horizontalMove = Vector3.zero;
        walkBobWeight = 0f;
        RestoreViewOrigin();
        if (wasEnabled) controller.enabled = true;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        ResolveViewBobTarget();
    }

    void LateUpdate()
    {
        ResolveViewBobTarget();
        if (!viewBobReady) return;

        float planarSpeed = 0f;
        if (controller != null && controller.enabled && controller.isGrounded && movementEnabled &&
            !UniDoom.UI.DeathScreen.IsVisible && !UniDoom.UI.DoorReloadScreen.IsReloading &&
            !UniDoom.UI.MouseSensitivityMenu.IsOpen && Time.timeScale > 0f)
        {
            Vector3 controllerVelocity = controller.velocity;
            controllerVelocity.y = 0f;
            planarSpeed = controllerVelocity.magnitude;
        }

        float targetWeight = speed > .01f ? Mathf.Clamp01(planarSpeed / speed) : 0f;
        float blend = 1f - Mathf.Exp(-bobBlendSpeed * Time.unscaledDeltaTime);
        walkBobWeight = Mathf.Lerp(walkBobWeight, targetWeight, blend);
        if (targetWeight > .01f)
            walkBobPhase = Mathf.Repeat(walkBobPhase + walkBobFrequency * targetWeight *
                Time.unscaledDeltaTime, Mathf.PI * 2f);

        // One lateral sweep and two vertical steps per stride reproduce Doom's readable gait.
        float side = Mathf.Sin(walkBobPhase) * cameraBobAmplitude.x;
        float rise = Mathf.Sin(walkBobPhase * 2f) * cameraBobAmplitude.y;
        viewBobTarget.localPosition = viewBobOrigin + new Vector3(side, rise, 0f) * walkBobWeight;
    }

    void ResolveViewBobTarget()
    {
        if (viewBobReady || cameraTransform == null) return;
        // Cinemachine hard-locks to Head, so the tracked parent is the correct bob pivot.
        viewBobTarget = cameraTransform.parent != null ? cameraTransform.parent : cameraTransform;
        viewBobOrigin = viewBobTarget.localPosition;
        viewBobReady = true;
    }

    void RestoreViewOrigin()
    {
        if (viewBobReady && viewBobTarget != null) viewBobTarget.localPosition = viewBobOrigin;
    }

    void OnDisable()
    {
        walkBobWeight = 0f;
        RestoreViewOrigin();
    }

    // Só calcula a direção, não move ainda
    public void MoveHorizontal()
    {
        if (!movementEnabled || UniDoom.UI.DeathScreen.IsVisible || UniDoom.UI.DoorReloadScreen.IsReloading || UniDoom.UI.MouseSensitivityMenu.IsOpen)
        {
            horizontalMove = Vector3.zero;
            return;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 move = camRight * x + camForward * z;
        if (move.magnitude > 1f) move.Normalize();

        horizontalMove = move * speed;
    }

    // Aplica gravidade e faz UM único Move com tudo junto
    public void ApplyGravity()
    {
        if (controller == null || !controller.enabled) return;

        if (!movementEnabled || UniDoom.UI.DeathScreen.IsVisible || UniDoom.UI.DoorReloadScreen.IsReloading)
        {
            velocity = Vector3.zero;
            return;
        }

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = groundedGravity;
        else
            velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = horizontalMove + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }

    public void ResetMotion()
    {
        velocity = Vector3.zero;
        horizontalMove = Vector3.zero;
        walkBobWeight = 0f;
        RestoreViewOrigin();
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!movementEnabled || Mathf.Abs(hit.normal.y) > .65f ||
            Vector3.Dot(horizontalMove, hit.normal) >= -.1f) return;

        PlayerPickupKey keys = GetComponent<PlayerPickupKey>();
        var door = hit.collider.GetComponentInParent<OpenDoorWithKey>();
        if (door != null && door.TryOpenByContact(keys, hit.collider)) return;

        var simpleDoor = hit.collider.GetComponentInParent<TotalChaosMaps.TCSimpleDoor>();
        if (simpleDoor != null && simpleDoor.TryOpenByContact()) return;

        var audio = GameplayAudio.Instance;
        if (simpleDoor != null && simpleDoor.IsLocked)
        {
            audio?.NotifyLockedDoorContact(simpleDoor);
            ShowLockedDoorNotice(simpleDoor);
            return;
        }

        var sealedDoor = hit.collider.GetComponent<LockedDoorSurface>();
        if (sealedDoor != null && sealedDoor.Matches(hit.collider, hit.point))
        {
            audio?.NotifyLockedDoorContact(sealedDoor);
            return;
        }
        if (door != null && door.BlocksPlayer(hit.collider, keys))
        {
            audio?.NotifyLockedDoorContact(door);
            if (door.NeedsKey(keys)) ShowLockedDoorNotice(door);
        }
    }

    private void ShowLockedDoorNotice(Object door)
    {
        if (door == null || (door == lastLockedDoorNotice && Time.unscaledTime < nextLockedDoorNoticeTime)) return;
        lastLockedDoorNotice = door;
        nextLockedDoorNoticeTime = Time.unscaledTime + 2f;
        UniDoom.UI.GameplayNotice.Show(LockedDoorMessage, 3.5f);
    }
}
