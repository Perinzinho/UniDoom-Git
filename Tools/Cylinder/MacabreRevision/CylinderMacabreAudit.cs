using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class CylinderMacabreAudit
{
    public static void Run()
    {
        AssetDatabase.Refresh();
        var output="";
        foreach(var file in Directory.GetFiles("Assets/_Project/Scenes/Cylinder/Meshes/MacabreRevision/PolyHaven","*.fbx",SearchOption.AllDirectories))
        {
            var p=file.Replace('\\','/');var importer=(ModelImporter)AssetImporter.GetAtPath(p);
            importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.SaveAndReimport();
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(p));
            output+=p+"\n";
            foreach(var r in go.GetComponentsInChildren<MeshRenderer>())
                output+=r.name+" | "+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"null"))+" | "+r.bounds+"\n";
            Object.DestroyImmediate(go);
        }
        Directory.CreateDirectory("MacabreEvidence");File.WriteAllText("MacabreEvidence/model-import.txt",output);Debug.Log("MACABRE_AUDIT_PASS");
    }
}
