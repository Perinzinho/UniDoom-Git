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

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    // Só calcula a direção, não move ainda
    public void MoveHorizontal()
    {
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
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = groundedGravity;
        else
            velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = horizontalMove + Vector3.up * velocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }
}