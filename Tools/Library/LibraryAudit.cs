using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;

public static class LibraryAudit
{
    public static void Run()
    {
        Directory.CreateDirectory("Evidence");
        var lines = new System.Collections.Generic.List<string>();
        foreach (var path in Directory.GetFiles("Assets/_Project/Scenes/Library/Models", "*.fbx", SearchOption.AllDirectories))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.isReadable = true; importer.importAnimation = false;
            importer.importCameras = false; importer.importLights = false;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var obj = UnityEngine.Object.Instantiate(model);
            var rs = obj.GetComponentsInChildren<Renderer>();
            var bounds = rs[0].bounds; foreach (var r in rs) bounds.Encapsulate(r.bounds);
            lines.Add(path + " bounds=" + bounds.ToString("F4"));
            foreach (var f in obj.GetComponentsInChildren<MeshFilter>())
                lines.Add("  " + f.name + " tris=" + f.sharedMesh.triangles.Length / 3 + " materials=" + string.Join(",", f.GetComponent<Renderer>().sharedMaterials.Select(m => m ? m.name : "NULL")));
            UnityEngine.Object.DestroyImmediate(obj);
        }
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        var library = GameObject.Find("Library");
        lines.Add("LIBRARY WORLD " + library.transform.position + " scale=" + library.transform.lossyScale);
        foreach (Transform t in library.transform)
        {
            var pb=t.GetComponent<ProBuilderMesh>(); if(pb){pb.ToMesh();pb.Refresh();}
            var r=t.GetComponent<Renderer>();
            lines.Add(t.name+" local="+t.localPosition+" worldBounds="+(r?r.bounds.ToString("F4"):"none"));
        }
        File.WriteAllLines("Evidence/audit.txt", lines);
        Debug.Log("LIBRARY_AUDIT_PASS");
    }
}
