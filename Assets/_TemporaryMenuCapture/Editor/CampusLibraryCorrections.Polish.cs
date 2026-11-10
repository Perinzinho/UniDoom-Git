using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UniDoom.CampusLibrary;

public static partial class CampusLibraryCorrections
{
    public static void PolishAndPlay()
    {
        Prefab(root =>
        {
            var ignition = root.GetComponentInChildren<LibraryMissionController>(true).Generator.transform;
            ignition.localScale = new Vector3(.5f, .36f, .12f);
            // Very thin physical backings must not self-shadow the printed face.
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name == "Worn metal backing" || r.name == "Paper thickness")) renderer.shadowCastingMode = ShadowCastingMode.Off;
        }, true);
        AssetDatabase.SaveAssets(); ValidateAndPlay();
    }
}
