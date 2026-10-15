// Reuse the preceding revision's geometry algorithms with a separate output folder.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
public static class CylinderCanteenGeometry
{
    const string Meshes = "Assets/_Project/Scenes/Cylinder/Meshes/CanteenDoors";
    public static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    static int serial;
    static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
    public static Transform Group(Transform parent,string name,Vector3 position)
    { var t = new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=position; return t; }
    static Mesh Persist(Mesh mesh, string key)
    {
        mesh.name = key; mesh.RecalculateBounds();
        if (mesh.tangents.Length == 0) mesh.RecalculateTangents();
        string path = Meshes + "/" + key + ".asset";
        AssetDatabase.CreateAsset(mesh, path); return mesh;
    }

    static void MeshObject(Transform parent, string name, Mesh mesh, Vector3 position, Quaternion rotation, string material)
    {
        var t = Group(parent, name, position); t.localRotation = rotation;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mats[material];
    }

    public static void Box(Transform parent, string name, Vector3 center, Vector3 size, string material, float bevel)
    {
        string key = "Box_" + size.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + size.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + size.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + bevel.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        if (!meshes.TryGetValue(key, out var mesh))
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var h = size * .5f; float b = Mathf.Min(bevel, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * .8f);
            var inner = h - Vector3.one * b;
            for (int axis = 0; axis < 3; axis++) foreach (int sign in new[] { -1, 1 })
            {
                int u = (axis + 1) % 3, v = (axis + 2) % 3, first = vertices.Count;
                float[] us = { -h[u], -inner[u], inner[u], h[u] }, vs = { -h[v], -inner[v], inner[v], h[v] };
                for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++)
                {
                    var p = Vector3.zero; p[axis] = sign * h[axis]; p[u] = us[i]; p[v] = vs[j];
                    var q = V(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    var n = (p - q).normalized; if (b <= 0) { n = Vector3.zero; n[axis] = sign; }
                    vertices.Add(b > 0 ? q + n * b : p); normals.Add(n); uv.Add(new Vector2(p[u], p[v]));
                }
                for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++)
                {
                    int a = first + j * 4 + i, c = a + 4;
                    if (sign > 0) triangles.AddRange(new[] { a, a + 1, c + 1, a, c + 1, c });
                    else triangles.AddRange(new[] { a, c + 1, a + 1, a, c, c + 1 });
                }
            }
            mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            meshes[key] = Persist(mesh, key);
        }
        MeshObject(parent, name, mesh, center, Quaternion.identity, material);
    }

    public static void Cylinder(Transform parent, string name, Vector3 center, float radius, float height, string material, int sides, Quaternion? rotation = null)
    {
        string key = "Cylinder_" + radius.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + height.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "_" + sides;
        if (!meshes.TryGetValue(key, out var mesh))
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            // Four rings form bevels which catch grazing light; caps are separate for hard normals.
            float bevel = Mathf.Min(radius * .16f, height * .15f);
            for (int row = 0; row < 4; row++) for (int i = 0; i <= sides; i++)
            {
                float angle = i * 2 * Mathf.PI / sides;
                float r = row == 0 || row == 3 ? radius - bevel : radius;
                float y = row == 0 ? -height / 2 : row == 1 ? -height / 2 + bevel : row == 2 ? height / 2 - bevel : height / 2;
                vertices.Add(V(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r)); uv.Add(new Vector2(i / (float)sides, row / 3f));
            }
            for (int row = 0; row < 3; row++) for (int i = 0; i < sides; i++)
            {
                int a = row * (sides + 1) + i, c = a + sides + 1;
                triangles.AddRange(new[] { a, c, c + 1, a, c + 1, a + 1 });
            }
            foreach (int sign in new[] { -1, 1 })
            {
                int first = vertices.Count; vertices.Add(V(0, sign * height / 2, 0)); uv.Add(new Vector2(.5f, .5f));
                for (int i = 0; i <= sides; i++)
                {
                    float a = i * Mathf.PI * 2 / sides; vertices.Add(V(Mathf.Cos(a) * (radius - bevel), sign * height / 2, Mathf.Sin(a) * (radius - bevel)));
                    uv.Add(new Vector2(.5f + .5f * Mathf.Cos(a), .5f + .5f * Mathf.Sin(a)));
                    if (i < sides)
                        if (sign > 0) triangles.AddRange(new[] { first, first + i + 2, first + i + 1 }); else triangles.AddRange(new[] { first, first + i + 1, first + i + 2 });
                }
            }
            mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); meshes[key] = Persist(mesh, key);
        }
        MeshObject(parent, name, mesh, center, rotation ?? Quaternion.identity, material);
    }

    public static void Pipe(Transform parent, Vector3 a, Vector3 b, float radius, string material)
        => Cylinder(parent, "Corroded pipe", (a + b) / 2, radius, Vector3.Distance(a, b), material, 20, Quaternion.FromToRotation(Vector3.up, b - a));

    public static void Batch(Transform parent, string label, bool excludeImported)
    {
        var renderers = parent.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled && r.GetComponent<MeshFilter>() &&
            (!excludeImported || !AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh).EndsWith(".fbx"))).ToArray();
        foreach (var group in renderers.GroupBy(r => r.sharedMaterial))
        {
            var combines = new List<CombineInstance>();
            foreach (var r in group)
            {
                var mf = r.GetComponent<MeshFilter>();
                // Imported chair meshes share a single PBR finish; retain every submesh.
                for (int i = 0; i < mf.sharedMesh.subMeshCount; i++)
                    combines.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = i, transform = parent.worldToLocalMatrix * r.transform.localToWorldMatrix });
            }
            var combined = new Mesh { indexFormat = IndexFormat.UInt32 };
            combined.CombineMeshes(combines.ToArray(), true, true);
            Persist(combined, "Batch_" + (++serial).ToString("D3") + "_" + group.Key.name);
            var t = Group(parent, label + " - " + group.Key.name, Vector3.zero);
            t.gameObject.AddComponent<MeshFilter>().sharedMesh = combined; t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = group.Key;
            foreach (var r in group) { Object.DestroyImmediate(r.GetComponent<MeshFilter>()); Object.DestroyImmediate(r); }
        }
    }
}
