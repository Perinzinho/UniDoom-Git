using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CylinderDetailAudit
{
    public const string Prefab = "Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab";
    public static void Inspect()
    {
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            Directory.CreateDirectory("Evidence");
            using (var writer = new StreamWriter("Evidence/hierarchy.txt"))
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    string path = AnimationUtility.CalculateTransformPath(t, root.transform);
                    if (t.childCount > 0 || t.GetComponent<Light>() || t.name.Contains("Chair"))
                        writer.WriteLine(path + " | local " + t.localPosition.ToString("F3") + " scale " + t.localScale.ToString("F3") + " world " + t.position.ToString("F3"));
                }
                foreach (var m in root.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m).Distinct())
                    writer.WriteLine("MAT " + m.name + " | " + AssetDatabase.GetAssetPath(m));
            }
            Debug.Log("CYLINDER_DETAIL_INSPECT_OK");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
