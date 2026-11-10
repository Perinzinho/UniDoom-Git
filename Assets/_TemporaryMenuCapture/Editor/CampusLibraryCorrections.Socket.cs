using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UniDoom.CampusLibrary;

public static partial class CampusLibraryCorrections
{
    public static void SocketAndNavigation()
    {
        PrepareSigns();
        Prefab(root =>
        {
            var mission = root.GetComponentInChildren<LibraryMissionController>(true);
            if (!root.GetComponentsInChildren<LibraryMissionTarget>(true).Any(t => t.Kind == LibraryTargetKind.FuseSocket))
            {
                var bank = new GameObject("Manual fuse installation - press E", typeof(BoxCollider), typeof(LibraryMissionTarget)); bank.transform.SetParent(mission.transform, false); bank.transform.position = new Vector3(-20.7f, 1.03f, 43.65f); bank.GetComponent<BoxCollider>().size = new Vector3(2.55f, .31f, .12f);
                var so = new SerializedObject(bank.GetComponent<LibraryMissionTarget>()); so.FindProperty("mission").objectReferenceValue = mission; so.FindProperty("kind").enumValueIndex = (int)LibraryTargetKind.FuseSocket; so.ApplyModifiedPropertiesWithoutUndo();
                Plate(mission.transform, SignData.Single(s => s.key == "Fuse installation plaque"), new Vector3(-20.7f, .67f, 43.79f), Quaternion.identity);
            }
            var surface = root.GetComponentInChildren<NavMeshSurface>(true); var original = surface.navMeshData;
            if (!original || !AssetDatabase.Contains(original)) throw new System.Exception("Navigation must retain its existing persistent asset.");
            var safe = root.GetComponentInChildren<LibraryMaintenanceSafety>(true); var data = new SerializedObject(safe); var volume = (BoxCollider)data.FindProperty("safeVolume").objectReferenceValue;
            foreach (var modifier in root.GetComponentsInChildren<NavMeshModifierVolume>(true))
                if (modifier.area == 1) { modifier.transform.position = volume.transform.position; modifier.transform.localScale = Vector3.one; modifier.center = Vector3.zero; modifier.size = volume.size; }
            surface.BuildNavMesh(); var generated = surface.navMeshData;
            if (!generated || generated == original) throw new System.Exception("No fresh navigation data.");
            surface.RemoveData(); EditorUtility.CopySerialized(generated, original); surface.navMeshData = original; EditorUtility.SetDirty(original); Object.DestroyImmediate(generated); surface.AddData();
        }, true);
        AssetDatabase.SaveAssets();
        // Also re-render the corrected pickup with its own alpha-aware instance of the existing shader.
        Fuses();
        File.WriteAllText(Log + "socket.txt", "Manual socket collider installed below seven terminals; persistent navigation rebuilt without changing its GUID/fileID. Runtime E interaction pending Play Mode.");
    }
}
