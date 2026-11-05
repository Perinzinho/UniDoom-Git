using UnityEngine;

/// <summary>Shares the reticle position with the ray used by every equipped weapon.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public sealed class CrosshairAim : MonoBehaviour
{
    // Normalized viewport coordinates keep the reticle and shots aligned at any resolution.
    public static Vector2 ViewportPoint => new Vector2(0.5f, 0.47f);

    private void Awake()
    {
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = ViewportPoint;
        rect.anchoredPosition = Vector2.zero;
    }

    public static Ray CreateRay(Camera camera)
    {
        Vector2 point = ViewportPoint;
        return camera.ViewportPointToRay(new Vector3(point.x, point.y, 0f));
    }
}
