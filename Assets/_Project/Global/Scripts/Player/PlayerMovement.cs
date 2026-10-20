using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 6f;
    public float gravity = -9.81f;
    public float groundedGravity = -2f;
    public Transform cameraTransform;

    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 horizontalMove;

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
        if (wasEnabled) controller.enabled = true;
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
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
    }
}
