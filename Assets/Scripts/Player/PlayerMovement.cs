using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 6f;
    public float gravity = -9.81f;
    public float groundedGravity = -2f; // small force to keep grounded

    private CharacterController controller;
    private Vector3 velocity;

    public Transform cameraTransform; // Link to player camera

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }
        

    public void MoveHorizontal()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        // get camera direction but zero Y axis to avoid flying
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 move = camRight * x + camForward * z;

        // Fix diagonal speed stacking
        if (move.magnitude > 1f)
        {
            move.Normalize();
        }

        controller.Move(move * speed * Time.deltaTime);
    }

    // Apply gravity to the player
    public void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            // reset vertical velocity when grounded
            velocity.y = groundedGravity;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }
}
