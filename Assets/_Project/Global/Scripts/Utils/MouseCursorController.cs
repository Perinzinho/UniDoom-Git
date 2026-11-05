using UnityEngine;

public class MouseCursorController : MonoBehaviour
{

    void Start()
    {
        if (UniDoom.UI.GameMenus.Instance != null)
            UniDoom.UI.GameMenus.Instance.ApplyCursorState();
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

}
    
