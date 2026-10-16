using UnityEngine;

/// <summary>Fits the persistent metric entrance to existing scene floor overrides.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class CampusEntranceAnchor : MonoBehaviour
{
    public MeshFilter floor;
    public bool exterior;
    public float heightOffset;
    [Min(.1f)] public float heightMultiplier = 1;
    Matrix4x4 previousFloor, previousParent;
    Bounds previousBounds;
    bool initialized;
    float previousHeightMultiplier;

    void OnEnable() => Refresh();
    void OnValidate() { initialized = false; Refresh(); }
    void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (!floor || !floor.sharedMesh || !transform.parent) return;
        var parent = transform.parent;
        var fm = floor.transform.localToWorldMatrix;
        var pm = parent.localToWorldMatrix;
        var bounds = floor.sharedMesh.bounds;
        if (initialized && fm == previousFloor && pm == previousParent && bounds == previousBounds && heightMultiplier == previousHeightMultiplier) return;
        previousFloor = fm; previousParent = pm; previousBounds = bounds; initialized = true;
        previousHeightMultiplier = heightMultiplier;
        var local = new Bounds(parent.InverseTransformPoint(floor.transform.TransformPoint(bounds.center)), Vector3.zero);
        for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                    local.Encapsulate(parent.InverseTransformPoint(floor.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, new Vector3(x,y,z)))));
        var scale = parent.lossyScale;
        transform.localPosition = new Vector3(local.center.x, local.max.y + heightOffset / Mathf.Max(.001f, Mathf.Abs(scale.y)), local.min.z);
        transform.localRotation = Quaternion.Euler(0, exterior ? 180 : 0, 0);
        transform.localScale = new Vector3(1 / Mathf.Max(.001f, Mathf.Abs(scale.x)), Mathf.Max(.1f, heightMultiplier) / Mathf.Max(.001f, Mathf.Abs(scale.y)), 1 / Mathf.Max(.001f, Mathf.Abs(scale.z)));
    }
}
