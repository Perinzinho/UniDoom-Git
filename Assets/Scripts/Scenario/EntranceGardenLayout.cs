using System;
using UnityEngine;

/// <summary>Fits only the garden decoration to the existing floor and side walls.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class EntranceGardenLayout : MonoBehaviour
{
    public enum Fit { Prop, Bed, Hedge, Fence, Path, Curb, WallLeft, WallRight, Tree }
    [Serializable] public struct Placement
    {
        public Transform target;
        public Fit fit;
        public Vector2 anchor;
        public Vector3 scale;
        public float yaw;
    }
    public Placement[] placements = Array.Empty<Placement>();
    public Vector3 LastSize { get; private set; }
    public float PropScale { get; private set; }
    public float ClearPathWidth => LastSize.x * .52f;
    Matrix4x4 previousFloor, previousGarden, previousLeft, previousRight;
    Bounds previousBounds;
    bool initialized;

    void OnEnable() => Refresh();
    void OnValidate() { initialized = false; Refresh(); }
    void LateUpdate() => Refresh();

    public void Refresh()
    {
        var garden = transform.parent;
        if (!garden) return; // The decoration prefab also opens on its own.
        var floor = garden.Find("Floor");
        if (!floor) return;
        Transform left = null, right = null;
        foreach (Transform child in garden)
            if (child.name.StartsWith("Wall", StringComparison.Ordinal))
            { if (child.localPosition.x < 0) left = child; else right = child; }
        var filter = floor.GetComponent<MeshFilter>();
        var bounds = filter && filter.sharedMesh ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        var leftMatrix = left ? left.localToWorldMatrix : Matrix4x4.identity;
        var rightMatrix = right ? right.localToWorldMatrix : Matrix4x4.identity;
        if (initialized && previousGarden == garden.localToWorldMatrix && previousFloor == floor.localToWorldMatrix && previousLeft == leftMatrix && previousRight == rightMatrix && previousBounds == bounds) return;
        previousGarden = garden.localToWorldMatrix; previousFloor = floor.localToWorldMatrix;
        previousLeft = leftMatrix; previousRight = rightMatrix; previousBounds = bounds; initialized = true;
        var min = garden.InverseTransformPoint(floor.TransformPoint(bounds.min));
        var max = garden.InverseTransformPoint(floor.TransformPoint(bounds.max));
        var wallLeft = left ? LocalBounds(left, garden) : default;
        var wallRight = right ? LocalBounds(right, garden) : default;
        if (left) min.x = Mathf.Max(min.x, wallLeft.max.x);
        if (right) max.x = Mathf.Min(max.x, wallRight.min.x);
        var s = garden.lossyScale;
        s = new Vector3(Mathf.Max(Mathf.Abs(s.x), .001f), Mathf.Max(Mathf.Abs(s.y), .001f), Mathf.Max(Mathf.Abs(s.z), .001f));
        transform.localRotation = Quaternion.identity;
        transform.localPosition = new Vector3((min.x + max.x) * .5f, max.y, (min.z + max.z) * .5f);
        transform.localScale = new Vector3(1 / s.x, 1 / s.y, 1 / s.z);
        LastSize = new Vector3((max.x - min.x) * s.x, 0, (max.z - min.z) * s.z);
        PropScale = Mathf.Clamp(LastSize.x / 6, .35f, 1);
        foreach (var placement in placements)
        {
            var t = placement.target; if (!t) continue;
            bool wall = placement.fit == Fit.WallLeft || placement.fit == Fit.WallRight;
            bool present = placement.fit != Fit.WallLeft && placement.fit != Fit.WallRight || (placement.fit == Fit.WallLeft ? left : right);
            t.gameObject.SetActive(present);
            if (!present) continue;
            var position = new Vector3(placement.anchor.x * LastSize.x, 0, placement.anchor.y * LastSize.z);
            Vector3 scale;
            switch (placement.fit)
            {
                case Fit.Bed: scale = new Vector3(LastSize.x * .175f, .045f, LastSize.z * .94f); break;
                case Fit.Hedge: scale = new Vector3(LastSize.x * .095f, .82f * PropScale, LastSize.z * .137f); break;
                case Fit.Fence: scale = new Vector3(PropScale, PropScale, LastSize.z * .18f); break;
                case Fit.Path: scale = new Vector3(ClearPathWidth, .012f, LastSize.z * .993f); position.y = .007f; break;
                case Fit.Curb: scale = new Vector3(.10f * PropScale, .14f * PropScale, LastSize.z * .96f); break;
                case Fit.Tree: scale = new Vector3(PropScale, Mathf.Max(.85f, PropScale), PropScale); break;
                case Fit.WallLeft:
                case Fit.WallRight:
                    var wb = placement.fit == Fit.WallLeft ? wallLeft : wallRight;
                    float height = Mathf.Max(.1f, (wb.max.y - max.y) * s.y);
                    scale = new Vector3(.022f, height, LastSize.z);
                    position = new Vector3((placement.fit == Fit.WallLeft ? -.5f : .5f) * LastSize.x + (placement.fit == Fit.WallLeft ? .013f : -.013f), height * .5f, 0);
                    break;
                default: scale = Vector3.one * PropScale; break;
            }
            scale = Vector3.Scale(scale, placement.scale);
            t.localPosition = position; t.localRotation = Quaternion.Euler(0, placement.yaw, 0); t.localScale = scale;
            if (wall || placement.fit == Fit.Path || placement.fit == Fit.Hedge || placement.fit == Fit.Bed)
            {
                var block = new MaterialPropertyBlock();
                var tiling = wall ? new Vector2(LastSize.z / 1.6f, scale.y / 1.4f) : placement.fit == Fit.Path ? new Vector2(scale.x / 1.4f, scale.z / 1.4f) : new Vector2(1, Mathf.Max(1, scale.z));
                block.SetVector("_BaseMap_ST", new Vector4(tiling.x, tiling.y, 0, 0));
                foreach (var renderer in t.GetComponentsInChildren<MeshRenderer>()) renderer.SetPropertyBlock(block);
            }
        }
    }
    static Bounds LocalBounds(Transform item, Transform parent)
    {
        var filter = item.GetComponent<MeshFilter>();
        var local = filter && filter.sharedMesh ? filter.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        var result = new Bounds(parent.InverseTransformPoint(item.TransformPoint(local.center)), Vector3.zero);
        for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            result.Encapsulate(parent.InverseTransformPoint(item.TransformPoint(local.center + Vector3.Scale(local.extents, new Vector3(x, y, z)))));
        return result;
    }
}
