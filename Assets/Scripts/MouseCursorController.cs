using UnityEngine;

public class MouseController : MonoBehaviour
{

    void Start()
    {
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false; // esconde o cursor do SO, o crosshair vira UI agora
    }

}

    
