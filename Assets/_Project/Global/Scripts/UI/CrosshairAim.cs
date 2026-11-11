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
        Ray ray = camera.ViewportPointToRay(new Vector3(point.x, point.y, 0f));
        // The camera can be closer to a target than its near clip plane.
        // Retain the viewport ray line while starting before that clipping plane.
        ray.origin -= ray.direction * Vector3.Dot(ray.origin - camera.transform.position, ray.direction);
        return ray;
    }
}
