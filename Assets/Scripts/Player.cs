using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
<<<<<<< HEAD:Assets/Scripts/PlayerMovement.cs
    public float speed = 6f;
    public float gravity = -9.81f;
    public float groundedGravity = -2f; // pequena força pra manter "grudado" no chão

    private CharacterController controller;
    private Vector3 velocity;

    public Transform cameraTransform;

=======
    
    public float speed = 6f;
    
    
    private CharacterController controller;
    private Vector3 velocity;
    
    public Transform cameraTransform;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
>>>>>>> 132f82477be2dc9cf10e6c343fc7ad5e4153fa62:Assets/Scripts/Player.cs
    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

<<<<<<< HEAD:Assets/Scripts/PlayerMovement.cs
    void Update()
    {
        MoveHorizontal();
        ApplyGravity();
    }

=======
    // Update is called once per frame
    void Update()
    {
        MoveHorizontal();
    }
    
>>>>>>> 132f82477be2dc9cf10e6c343fc7ad5e4153fa62:Assets/Scripts/Player.cs
    void MoveHorizontal()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

<<<<<<< HEAD:Assets/Scripts/PlayerMovement.cs
=======
        // pega a direção da câmera, mas zera o eixo Y pra não "voar"
>>>>>>> 132f82477be2dc9cf10e6c343fc7ad5e4153fa62:Assets/Scripts/Player.cs
        Vector3 camForward = cameraTransform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraTransform.right;
        camRight.y = 0f;
        camRight.Normalize();

        Vector3 move = camRight * x + camForward * z;
        controller.Move(move * speed * Time.deltaTime);
    }
<<<<<<< HEAD:Assets/Scripts/PlayerMovement.cs

    //Faz player cair no chão
    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            // reseta a velocidade vertical quando está no chão
            velocity.y = groundedGravity;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }
}
=======
}

>>>>>>> 132f82477be2dc9cf10e6c343fc7ad5e4153fa62:Assets/Scripts/Player.cs
