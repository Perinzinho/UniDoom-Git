using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerMovement playerMoviment;
    [SerializeField] private float speed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerMoviment = GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        playerMoviment.MoveHorizontal();
    }

    // Added to fix CS1061
    private void MoveHorizontal()
    {
        float h = Input.GetAxis("Horizontal");
        transform.Translate(Vector3.right * h * speed * Time.deltaTime);
    }
}

