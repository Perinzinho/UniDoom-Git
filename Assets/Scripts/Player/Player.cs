using UnityEngine;

public class Player : MonoBehaviour
{
    private PlayerMovement playerMovement;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        playerMovement.MoveHorizontal();
        playerMovement.ApplyGravity();
    }
}
