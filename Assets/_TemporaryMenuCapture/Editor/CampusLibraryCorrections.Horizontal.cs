using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UniDoom.CampusLibrary;

public static partial class CampusLibraryCorrections
{
    public static void HorizontalControls()
    {
        PrepareSigns();
        Prefab(root =>
        {
            var mission = root.GetComponentInChildren<LibraryMissionController>(true);
            var marker = Named(root, "Manual fuse installation - press E");
            if (marker.position.x < -24) throw new System.Exception("Horizontal controls already applied.");
            // One horizontal row per control bank, separated laterally for yaw-only aiming.
            marker.position = new Vector3(-25.25f, 1.85f, 43.65f); marker.GetComponent<BoxCollider>().size = new Vector3(1.9f, .34f, .12f);
            var sockets = new SerializedObject(mission).FindProperty("fuseSockets");
            for (int i = 0; i < sockets.arraySize; i++) ((Renderer)sockets.GetArrayElementAtIndex(i).objectReferenceValue).transform.position = new Vector3(-25.85f + i * .6f, 1.85f, 43.78f);
            Box(mission.transform, "Eye level fuse enclosure", new Vector3(-25.25f, 1.85f, 43.98f), new Vector3(2.05f, .65f, .3f), Metal, true);
            Named(root, "Physical plate - Fuse installation plaque").position = new Vector3(-25.25f, 2.4f, 43.79f);
            foreach (var button in mission.Buttons) { var p = button.transform.position; p.y = 1.85f; button.transform.position = p; }
            var panel = Named(root, "Seven-terminal panel housing"); panel.position += Vector3.up * .3f;
            foreach (var segment in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Circuit progress segment "))) segment.position += Vector3.up * .3f;
            Named(root, "Generator mission status").position += Vector3.up * .3f;
            var ignition = mission.Generator.transform; var pIgnition = ignition.position; pIgnition.y = 1.85f; ignition.position = pIgnition;
            Box(mission.transform, "Generator ignition control pedestal", new Vector3(pIgnition.x, 1.03f, pIgnition.z + .08f), new Vector3(.22f, 1.7f, .24f), Metal, true);
        }, true);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Base + "CampusLibrary.unity", OpenSceneMode.Single);
        Capture(new Vector3(-22.5f, 1.85f, 40.1f), new Vector3(-22.5f, 1.85f, 43.9f), "horizontal-controls");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        File.WriteAllText(Log + "horizontal.txt", "Ignition, seven buttons and fuse bank at y=1.85m. Fuse bank separated horizontally. Pickup/document focus supports yaw-only facing with eye-to-object occlusion and original 2.8m reach; camera/player unchanged. Await Play Mode input verification.");
    }
}
