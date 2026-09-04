using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public class NewMonoBehaviour : MonoBehaviour
    {
        public float speed = 6f;
        private CharacterController controller;
        private Vector3 velocity;

        public Transform cameraTransform;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            controller = GetComponent<CharacterController>();

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
    }
}