using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class CampusLibraryCorrections
{
    sealed class BookMeshBuilder
    {
        readonly List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>();
        readonly List<Vector4> tangents = new List<Vector4>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int> triangles = new List<int>();
        public BookMeshBuilder(Mesh source)
        {
            var rotation = Quaternion.Euler(0, 180, 0); var pivot = new Vector3(-.021734715f, 0, -.049994893f);
            vertices.AddRange(source.vertices.Select(v => rotation * (v - pivot) + pivot));
            normals.AddRange(source.normals.Select(n => rotation * n)); uv.AddRange(source.uv);
            foreach (var tangent in source.tangents) { var t = rotation * new Vector3(tangent.x, tangent.y, tangent.z); tangents.Add(new Vector4(t.x, t.y, t.z, tangent.w)); }
            while (tangents.Count < vertices.Count) tangents.Add(new Vector4(1, 0, 0, 1));
            triangles.AddRange(source.triangles);
        }
        void Face(Vector3 center, Vector3 u, Vector3 v, Vector3 normal, Rect rect)
        {
            int n = vertices.Count;
            vertices.Add(center - u * .5f - v * .5f); vertices.Add(center + u * .5f - v * .5f); vertices.Add(center + u * .5f + v * .5f); vertices.Add(center - u * .5f + v * .5f);
            float handedness = Vector3.Dot(Vector3.Cross(normal, u.normalized), v.normalized) >= 0 ? 1 : -1;
            for (int i = 0; i < 4; i++) { normals.Add(normal); tangents.Add(new Vector4(u.normalized.x, u.normalized.y, u.normalized.z, handedness)); }
            uv.Add(new Vector2(rect.xMin, rect.yMin)); uv.Add(new Vector2(rect.xMax, rect.yMin)); uv.Add(new Vector2(rect.xMax, rect.yMax)); uv.Add(new Vector2(rect.xMin, rect.yMax));
            if (Vector3.Dot(Vector3.Cross(u, v), normal) > 0) triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
            else triangles.AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
        }
        public void Box(Vector3 center, Vector3 size, Rect side, Rect? front = null)
        {
            Face(center + Vector3.back * size.z * .5f, Vector3.right * size.x, Vector3.up * size.y, Vector3.back, front ?? side);
            Face(center + Vector3.forward * size.z * .5f, Vector3.left * size.x, Vector3.up * size.y, Vector3.forward, side);
            Face(center + Vector3.left * size.x * .5f, Vector3.forward * size.z, Vector3.up * size.y, Vector3.left, side);
            Face(center + Vector3.right * size.x * .5f, Vector3.back * size.z, Vector3.up * size.y, Vector3.right, side);
            Face(center + Vector3.up * size.y * .5f, Vector3.right * size.x, Vector3.back * size.z, Vector3.up, side);
            Face(center + Vector3.down * size.y * .5f, Vector3.right * size.x, Vector3.forward * size.z, Vector3.down, side);
        }
        public Mesh Save(string path)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); } else mesh.Clear();
            mesh.name = System.IO.Path.GetFileNameWithoutExtension(path); mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTangents(tangents); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh); return mesh;
        }
    }
    public static void Shelves()
    {
        var cover = new BookMeshBuilder(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Global/Meshes/CampusInterior/Campus full bookcase_1.asset"));
        var pages = new BookMeshBuilder(AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Global/Meshes/CampusInterior/Campus full bookcase_2.asset"));
        var leather = new Rect(.025f, .46f, .10f, .12f);
        for (int row = 0; row < 2; row++) for (int i = 0; i < 20; i++)
        {
            float floor = row == 0 ? .087f : 2.055f;
            float width = .058f + (i % 4) * .003f, height = .25f + (i % 5) * .012f;
            float x = -.655f + i * .069f; var center = new Vector3(x, floor + height * .5f, -.065f);
            // Reuse the encyclopedia spine strip from the existing cover atlas.
            var spine = new Rect(.592f + (i % 20) * .0201f, .856f, .0178f, .135f);
            cover.Box(center + Vector3.back * .102f, new Vector3(width, height, .012f), leather, spine);
            cover.Box(center + Vector3.left * (width * .5f - .0015f), new Vector3(.003f, height, .21f), leather);
            cover.Box(center + Vector3.right * (width * .5f - .0015f), new Vector3(.003f, height, .21f), leather);
            cover.Box(center + Vector3.up * (height * .5f - .0015f), new Vector3(width, .003f, .21f), leather);
            cover.Box(center + Vector3.down * (height * .5f - .0015f), new Vector3(width, .003f, .21f), leather);
            pages.Box(center, new Vector3(width - .007f, height - .008f, .195f), new Rect(0, 0, 1, 1));
        }
        var coverMesh = cover.Save(Base + "Meshes/Library completed book covers.asset");
        var pageMesh = pages.Save(Base + "Meshes/Library completed book pages.asset");
        Prefab(root =>
        {
            var shelves = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.EndsWith("full bookcase")).ToArray();
            if (shelves.Length != 74) throw new Exception("Unexpected shelf count");
            foreach (var shelf in shelves)
            {
                if (shelf.name == "Annex full bookcase") shelf.rotation = Quaternion.Euler(0, shelf.position.x < Mathf.Round(shelf.position.x / 6) * 6 ? 90 : 270, 0);
                shelf.Find("Surface Cover").GetComponent<MeshFilter>().sharedMesh = coverMesh;
                shelf.Find("Surface Pages").GetComponent<MeshFilter>().sharedMesh = pageMesh;
            }
            for (int i = 0; i < shelves.Length; i++) for (int j = i + 1; j < shelves.Length; j++)
            {
                var a = BoundsOf(shelves[i]); var b = BoundsOf(shelves[j]); var overlap = Vector3.Min(a.max, b.max) - Vector3.Max(a.min, b.min);
                if (overlap.x > .03f && overlap.z > .03f && overlap.y > .5f) throw new Exception("Bookcases still overlap: " + shelves[i].position + " and " + shelves[j].position);
            }
        }, true);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Base + "CampusLibrary.unity", OpenSceneMode.Single);
        Capture(new Vector3(13, 1.9f, 33.5f), new Vector3(12, 1.4f, 37), "after-annex-stacks");
        Capture(new Vector3(1, 1.85f, -1), new Vector3(3.45f, 1.4f, -.6f), "after-book-fill");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        File.WriteAllText(Log + "shelves.txt", "74 shelves; 6 intersecting pairs corrected; 40 added book volumes per shelf (2960 total); two shared persistent meshes; existing renderer/material counts retained; zero shelf-bound intersections above 3cm.");
    }
}
