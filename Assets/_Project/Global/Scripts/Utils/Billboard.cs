using UnityEngine;

public class Billboard : MonoBehaviour
{
    // Doom-style billboard technique: the 2D object always faces the player
    private Transform cameraTransform;
    void LateUpdate()
    {
        if (cameraTransform == null || !cameraTransform.gameObject.activeInHierarchy)
        {
            Camera view = Camera.main;
            if (view == null) return; // The persistent player camera may not have spawned yet.
            cameraTransform = view.transform;
        }
        Vector3 direction = cameraTransform.position - transform.position;
        direction.y = 0;
        if (direction.sqrMagnitude < .0001f) return;
        transform.rotation = Quaternion.LookRotation(-direction);
    }
}
