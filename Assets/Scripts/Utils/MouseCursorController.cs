using UnityEngine;

public class MouseCursorController : MonoBehaviour
{

    void Start()
    {
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false; // hides the OS cursor, the crosshair is now 100% UI
    }

}
    
