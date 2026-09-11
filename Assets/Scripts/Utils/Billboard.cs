using UnityEngine;

public class Billboard : MonoBehaviour
{
    // Doom-style billboard technique: the 2D object always faces the player
    private Transform cameraTransform;
    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void LateUpdate()
    {
        Vector3 direction = cameraTransform.position - transform.position;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(-direction);
    }
}
