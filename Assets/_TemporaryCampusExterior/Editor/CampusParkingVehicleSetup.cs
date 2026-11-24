using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Parking-bay mesh utility used by the campus bench and fountain editor setup.
// Vehicle placement was removed from the campus scene; keep only the markings builder.
public static class CampusParkingVehicleSetup
{
    const string MeshPath = "Assets/_Project/Scenes/CampusExterior/Meshes/StreetParkingBays.asset";

    readonly struct Bay
    {
        public readonly Vector3 center;
        public readonly float yaw;
        public Bay(float x, float z, float yaw) { center = new Vector3(x, 0, z); this.yaw = yaw; }
    }

    static readonly Bay[] Bays = Enumerable.Range(0, 4).Select(i => new Bay(-91 + 6 * i, -1.55f, 90))
        .Concat(Enumerable.Range(0, 4).Select(i => new Bay(16.35f, 55 + 6 * i, 0)))
        .Concat(Enumerable.Range(0, 4).Select(i => new Bay(75.15f, -56 + 6 * i, 0))).ToArray();

    public static Mesh BuildParkingMesh(Transform campus)
    {
        var road = FindRoad(campus);
        var vertices = new List<Vector3>(); var indices = new List<int>(); var uv = new List<Vector2>();
        foreach (int start in new[] { 0, 4, 8 })
        {
            Bay first = Bays[start]; Quaternion q = Quaternion.Euler(0, first.yaw, 0);
            Vector3 origin = first.center - q * new Vector3(0, 0, 3);
            AddLine(-1.25f, 0, -1.25f, 24); AddLine(1.25f, 0, 1.25f, 24);
            for (int i = 0; i <= 4; i++) AddLine(-1.25f, i * 6, 1.25f, i * 6);
            void AddLine(float x1, float z1, float x2, float z2)
            {
                Vector3 a = origin + q * new Vector3(x1, 0, z1), b = origin + q * new Vector3(x2, 0, z2);
                Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized) * .05f;
                int segments = Mathf.CeilToInt(Vector3.Distance(a, b) / .5f);
                for (int j = 0; j < segments; j++)
                {
                    Vector3 p = Vector3.Lerp(a, b, (float)j / segments), r = Vector3.Lerp(a, b, (float)(j + 1) / segments);
                    int k = vertices.Count;
                    foreach (Vector3 v in new[] { p - side, r - side, r + side, p + side })
                        vertices.Add(RoadHit(road, v).point + Vector3.up * .006f);
                    indices.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                    uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
                }
            }
        }
        var mesh = new Mesh { name = "Street Parking Bays - 12" };
        mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.SetUVs(0, uv);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        return SaveAsset(mesh, MeshPath);
    }

    static MeshCollider FindRoad(Transform root) => root.GetComponentsInChildren<MeshCollider>(true)
        .FirstOrDefault(c => c.name == "Campus asphalt" && c.enabled && c.sharedMesh != null)
        ?? throw new InvalidOperationException("Active campus asphalt collider is missing.");

    static RaycastHit RoadHit(MeshCollider road, Vector3 position)
    {
        if (!road.Raycast(new Ray(new Vector3(position.x, 5f, position.z), Vector3.down), out var hit, 10f))
            throw new InvalidOperationException("Parking extends outside asphalt at " + position);
        return hit;
    }

    static T SaveAsset<T>(T generated, string path) where T : UnityEngine.Object
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing == null) { AssetDatabase.CreateAsset(generated, path); return generated; }
        EditorUtility.CopySerialized(generated, existing); UnityEngine.Object.DestroyImmediate(generated);
        EditorUtility.SetDirty(existing); return existing;
    }
}
