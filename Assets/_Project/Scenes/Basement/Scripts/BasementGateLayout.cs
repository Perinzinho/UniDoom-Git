using UnityEngine;

/// <summary>Fit the gate to the existing opening, including scene header overrides.</summary>
[ExecuteAlways]
public class BasementGateLayout : MonoBehaviour
{
    public Transform arena;
    public BoxCollider leftWall;
    public BoxCollider rightWall;
    public BoxCollider header;
    public BoxCollider floor;
    public Transform gateFrame;

    void OnEnable() => Fit();
    void LateUpdate()
    {
        if (!Application.isPlaying) Fit();
    }

    public void Fit()
    {
        if (!arena || !leftWall || !rightWall || !header || !floor || !gateFrame) return;
        Vector3 left = arena.InverseTransformPoint(leftWall.transform.TransformPoint(
            leftWall.center + Vector3.right * leftWall.size.x * .5f));
        Vector3 right = arena.InverseTransformPoint(rightWall.transform.TransformPoint(
            rightWall.center - Vector3.right * rightWall.size.x * .5f));
        Vector3 top = arena.InverseTransformPoint(header.transform.TransformPoint(
            header.center - Vector3.up * header.size.y * .5f));
        Vector3 bottom = arena.InverseTransformPoint(floor.transform.TransformPoint(
            floor.center + Vector3.up * floor.size.y * .5f));
        gateFrame.localPosition = new Vector3((left.x + right.x) * .5f, bottom.y, top.z);
        gateFrame.localScale = new Vector3(Mathf.Max(.1f, right.x - left.x),
            Mathf.Max(.1f, top.y - bottom.y), 1);
    }
}
