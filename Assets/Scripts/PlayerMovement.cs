using UnityEngine;

namespace Assets.Scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        public float speed = 6f;
        public float gravity = -9.81f;
        public float groundedGravity = -2f; // pequena força pra manter grudado no chão

        private CharacterController controller;
        private Vector3 velocity;

        public Transform cameraTransform;

        void Start()
        {
            controller = GetComponent<CharacterController>();
        }

        void Update()
        {
            MoveHorizontal();
            ApplyGravity();
        }

        public void MoveHorizontal()
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            // pega a direção da câmera, mas zera o eixo Y pra não "voar"
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            Vector3 move = camRight * x + camForward * z;
            controller.Move(move * speed * Time.deltaTime);
        }

        // Faz player cair no chão
        public void ApplyGravity()
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
}