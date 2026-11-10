using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static partial class CampusLibraryCorrections
{
    [Serializable] public class MeshInfo
    {
        public string name;
        public Vector3 min, max;
        public List<string> yLevels = new List<string>(), components = new List<string>();
    }
    [Serializable] public class MeshReport { public List<MeshInfo> meshes = new List<MeshInfo>(); }
    static int[] Connected(Mesh mesh)
    {
        var vertices = mesh.vertices; var ids = Enumerable.Range(0, vertices.Length).ToArray();
        int Find(int i) { while (ids[i] != i) { ids[i] = ids[ids[i]]; i = ids[i]; } return i; }
        void Join(int a, int b) { a = Find(a); b = Find(b); if (a != b) ids[b] = a; }
        var positions = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < vertices.Length; i++)
        {
            var key = Vector3Int.RoundToInt(vertices[i] * 100000);
            if (positions.TryGetValue(key, out int prior)) Join(i, prior); else positions.Add(key, i);
        }
        var triangles = mesh.triangles;
        for (int i = 0; i < triangles.Length; i += 3) { Join(triangles[i], triangles[i + 1]); Join(triangles[i], triangles[i + 2]); }
        for (int i = 0; i < ids.Length; i++) ids[i] = Find(i);
        return ids;
    }
    public static void VisualInspection()
    {
        var report = new MeshReport();
        for (int index = 0; index < 3; index++)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/_Project/Global/Meshes/CampusInterior/Campus full bookcase_" + index + ".asset");
            var info = new MeshInfo { name = mesh.name, min = mesh.bounds.min, max = mesh.bounds.max }; var vertices = mesh.vertices;
            foreach (var group in vertices.GroupBy(v => Mathf.Round(v.y * 1000) / 1000).OrderByDescending(g => g.Count()).Take(25)) info.yLevels.Add(group.Key.ToString("F3") + " count=" + group.Count());
            var groups = Connected(mesh); foreach (var group in Enumerable.Range(0, vertices.Length).GroupBy(i => groups[i]))
            {
                var bounds = new Bounds(vertices[group.First()], Vector3.zero); foreach (int i in group) bounds.Encapsulate(vertices[i]);
                info.components.Add("id=" + group.Key + " min=" + bounds.min.ToString("F4") + " max=" + bounds.max.ToString("F4") + " size=" + bounds.size.ToString("F4"));
            }
            report.meshes.Add(info);
        }
        Write("mesh-analysis", report);
        var scene = EditorSceneManager.OpenScene(Base + "CampusLibrary.unity", OpenSceneMode.Single);
        Capture(new Vector3(13, 1.9f, 33.5f), new Vector3(12, 1.4f, 37), "before-annex-stacks");
        Capture(new Vector3(1, 1.85f, -1), new Vector3(3.45f, 1.4f, -.6f), "before-book-fill");
        Capture(new Vector3(-21.5f, 1.85f, 34), new Vector3(-23, 1.7f, 42), "before-maintenance");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
    }
    static void Capture(Vector3 position, Vector3 target, string suffix)
    {
        bool asynchronousShaders = ShaderUtil.allowAsyncCompilation; ShaderUtil.allowAsyncCompilation = false;
        var go = new GameObject("Temporary correction camera"); var camera = go.AddComponent<Camera>(); camera.fieldOfView = 60; camera.nearClipPlane = .03f; camera.farClipPlane = 120;
        camera.transform.position = position; camera.transform.LookAt(target); camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var previous = RenderTexture.active; var texture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try { camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply(); File.WriteAllBytes(Log + suffix + ".png", image.EncodeToPNG()); }
        finally { camera.targetTexture = null; RenderTexture.active = previous; Object.DestroyImmediate(image); Object.DestroyImmediate(texture); Object.DestroyImmediate(go); ShaderUtil.allowAsyncCompilation = asynchronousShaders; }
    }
}
