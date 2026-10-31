using UnityEngine;

/// <summary>Identifies the solid panel of a permanently sealed doorway, using existing colliders.</summary>
[DisallowMultipleComponent]
public sealed class LockedDoorSurface : MonoBehaviour
{
    [SerializeField] Collider blockingCollider;
    [SerializeField] Renderer doorVisual;

    public bool Matches(Collider other, Vector3 contactPoint)
    {
        if (!isActiveAndEnabled || other != blockingCollider) return false;
        if (doorVisual == null) return true;
        // The bay collider also covers the wall beside the panel. Only the panel should sound locked.
        Bounds panel = doorVisual.localBounds;
        panel.Expand(new Vector3(.2f, .2f, 1.3f));
        return panel.Contains(doorVisual.transform.InverseTransformPoint(contactPoint));
    }
}
