using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class CampusFinalCheck
{
    public static void Run()
    {
        var report=new StringBuilder();
        foreach(var name in new[]{"EntranceGarden","EntranceHallway"})
        {
            var root=PrefabUtility.LoadPrefabContents(CampusBuilder.Prefab(name));
            try
            {
                var art=root.transform.Find("CampusArchitecture");
                if(!art)throw new Exception("Missing campus root");
                art.GetComponent<CampusEntranceAnchor>().Refresh();
                foreach(var mesh in art.GetComponentsInChildren<MeshFilter>())
                    if(!mesh.GetComponent<TMPro.TMP_Text>()&&(!mesh.sharedMesh||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh.sharedMesh))))throw new Exception("Transient mesh "+mesh.name);
                foreach(var renderer in art.GetComponentsInChildren<Renderer>())
                    foreach(var material in renderer.sharedMaterials)
                        if(!material||!material.shader||!material.shader.isSupported||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))throw new Exception("Invalid or transient material "+renderer.name);
                foreach(var t in root.GetComponentsInChildren<Transform>(true))
                    if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script "+t.name);
                report.AppendLine(name+": prefab opens, persistent meshes/materials, supported shaders, no missing scripts; GUID="+AssetDatabase.AssetPathToGUID(CampusBuilder.Prefab(name)));
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        CampusValidation.ReferencesAndRoute();
        report.AppendLine("CAMPUS_FINAL_PASS");
        File.WriteAllText("CampusEvidence/final-validation.txt",report.ToString());
        Debug.Log("CAMPUS_FINAL_PASS");
    }
}
