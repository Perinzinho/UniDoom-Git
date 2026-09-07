using UnityEngine;

public class BillBoard : MonoBehaviour
{
    //Técnica do Doom que o objeto 2d fica sempre olhando para  o player
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
