using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.AI.Navigation;
using UniDoom.CampusLibrary;

public static partial class CampusLibraryCorrections
{
    static Transform Named(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
    static void Shape(GameObject root, string name, Vector3 position, Vector3 size)
    {
        var t = Named(root, name); t.position = position; t.localScale = size;
    }
    static void Append(UnityEngine.Object owner, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(owner); var list = data.FindProperty(field); int i = list.arraySize++; list.GetArrayElementAtIndex(i).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void Layout()
    {
        PrepareSigns();
        Prefab(root =>
        {
            if (root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Staff hall and workshop")) throw new Exception("Staff layout already applied.");
            var mission = Named(root, "Emergency mission - annex and maintenance");
            var rooms = new GameObject("Staff hall and workshop").transform; rooms.SetParent(mission, false);
            var wall = Named(root, "Maintenance west wall").GetComponent<Renderer>().sharedMaterial;
            Shape(root, "Maintenance west wall", new Vector3(-27.05f, 2.3f, 39.15f), new Vector3(.2f, 4.6f, 10.3f));
            Shape(root, "Maintenance east wall", new Vector3(-14, 2.3f, 39.15f), new Vector3(.2f, 4.6f, 10.3f));
            Shape(root, "Maintenance front west", new Vector3(-24.825f, 2.3f, 34), new Vector3(4.45f, 4.6f, .2f));
            Shape(root, "Maintenance front east", new Vector3(-17.2f, 2.3f, 34), new Vector3(6.4f, 4.6f, .2f));
            Shape(root, "Maintenance entrance lintel", new Vector3(-21.5f, 3.9f, 34), new Vector3(2.2f, 1.4f, .2f));
            Named(root, "Maintenance entrance sign").position = new Vector3(-21.5f, 3.65f, 33.86f);
            Named(root, "Safety sign").position = new Vector3(-22.5f, 3.4f, 38.66f);
            Named(root, "Maintenance route outside").position = new Vector3(-16.8f, 2.4f, 33.86f);
            Box(rooms, "Generator partition west", new Vector3(-25.275f, 2.3f, 38.8f), new Vector3(3.55f, 4.6f, .2f), wall, true);
            Box(rooms, "Generator partition east", new Vector3(-19.75f, 2.3f, 38.8f), new Vector3(3.5f, 4.6f, .2f), wall, true);
            Box(rooms, "Generator doorway lintel", new Vector3(-22.5f, 3.9f, 38.8f), new Vector3(2, 1.4f, .2f), wall, true);
            Box(rooms, "Workshop partition west", new Vector3(-17.3f, 2.3f, 38.8f), new Vector3(1.4f, 4.6f, .2f), wall, true);
            Box(rooms, "Workshop partition east", new Vector3(-14.5f, 2.3f, 38.8f), new Vector3(1, 4.6f, .2f), wall, true);
            Box(rooms, "Workshop doorway lintel", new Vector3(-15.8f, 3.9f, 38.8f), new Vector3(1.6f, 1.4f, .2f), wall, true);
            Box(rooms, "Generator and workshop dividing wall", new Vector3(-18, 2.3f, 41.55f), new Vector3(.2f, 4.6f, 5.5f), wall, true);
            Plate(rooms, SignData.Single(s => s.key == "Workshop room plaque"), new Vector3(-15.8f, 3.4f, 38.66f), Quaternion.identity);
            var benchMaterial = Named(root, "Seven-terminal panel housing").GetComponent<Renderer>().sharedMaterial;
            Box(rooms, "Workshop steel worktop", new Vector3(-14.65f, .92f, 41.6f), new Vector3(.85f, .1f, 2.5f), benchMaterial, true);
            foreach (float z in new[] { 40.65f, 42.55f }) foreach (float x in new[] { -14.35f, -14.95f })
                Box(rooms, "Workshop bench leg", new Vector3(x, .43f, z), new Vector3(.065f, .86f, .065f), Metal, true);
            Box(rooms, "Staff document board", new Vector3(-25.9f, 1.8f, 34.16f), new Vector3(1.45f, .95f, .055f), benchMaterial).transform.localRotation = Quaternion.Euler(0, 180, 0);

            var safety = root.GetComponentInChildren<LibraryMaintenanceSafety>(true); var so = new SerializedObject(safety);
            var volume = (BoxCollider)so.FindProperty("safeVolume").objectReferenceValue;
            volume.transform.position = new Vector3(-20.525f, 2.1f, 39.15f); volume.transform.localScale = Vector3.one; volume.center = Vector3.zero; volume.size = new Vector3(12.65f, 4.2f, 10.3f);
            var barrier = (Collider)so.FindProperty("enemyBarrier").objectReferenceValue; barrier.transform.position = new Vector3(-21.5f, 1.6f, 33.6f);
            ((Transform)so.FindProperty("recoveryPoint").objectReferenceValue).position = new Vector3(-21.5f, .1f, 32.5f);
            foreach (var modifier in safety.GetComponentsInChildren<NavMeshModifierVolume>(true)) { modifier.transform.position = new Vector3(-20.525f, 2.1f, 39.15f); modifier.transform.localScale = Vector3.one; modifier.center = Vector3.zero; modifier.size = new Vector3(12.65f, 4.2f, 10.3f); }

            // Reuse the same luminaire and red safety lamp, keeping both lighting states wired.
            var lighting = root.GetComponentInChildren<LibraryLightingController>(true); var lights = new SerializedObject(lighting);
            var normal = (Light)lights.FindProperty("normalLights").GetArrayElementAtIndex(lights.FindProperty("normalLights").arraySize - 1).objectReferenceValue;
            var newNormal = UnityEngine.Object.Instantiate(normal, normal.transform.parent); newNormal.name = "Staff workshop ceiling light"; newNormal.transform.position = new Vector3(-15.8f, 4.3f, 41.2f); newNormal.range = 5;
            Append(lighting, "normalLights", newNormal);
            var fixture = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "Annex ceiling fluorescent fixture");
            var newFixture = UnityEngine.Object.Instantiate(fixture.gameObject, rooms); newFixture.name = "Workshop fluorescent fixture"; newFixture.transform.position = new Vector3(-15.8f, 4.42f, 41.2f);
            Append(lighting, "normalDiffusers", newFixture.GetComponent<Renderer>());
            var red = (Light)lights.FindProperty("emergencyLights").GetArrayElementAtIndex(0).objectReferenceValue;
            var newRed = UnityEngine.Object.Instantiate(red, red.transform.parent); newRed.name = "Staff workshop emergency light"; newRed.transform.position = new Vector3(-15.8f, 3.5f, 41.2f); newRed.range = 5;
            Append(lighting, "emergencyLights", newRed);
            var oldDiffuser = (Renderer)lights.FindProperty("emergencyDiffusers").GetArrayElementAtIndex(0).objectReferenceValue;
            var diffuser = UnityEngine.Object.Instantiate(oldDiffuser.gameObject, rooms); diffuser.name = "Workshop emergency diffuser"; diffuser.transform.position = new Vector3(-15.8f, 3.56f, 41.2f); Append(lighting, "emergencyDiffusers", diffuser.GetComponent<Renderer>());
        }, true);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Base + "CampusLibrary.unity", OpenSceneMode.Single);
        Capture(new Vector3(-21.5f, 1.85f, 31.8f), new Vector3(-21.5f, 2.8f, 34), "after-staff-entry");
        Capture(new Vector3(-24.5f, 1.85f, 35.8f), new Vector3(-19.5f, 2.1f, 39.5f), "after-staff-hall");
        Capture(new Vector3(18.5f, 1.85f, 28.6f), new Vector3(18.5f, 3.4f, 30.15f), "after-annex-plate");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        File.WriteAllText(Log + "layout.txt", "Staff area enlarged to 13.05 x 10.3m, with hall, generator room, workshop and 2.2/2/1.6m openings. Safety/barrier/recovery and both lighting states updated. Navigation still requires rebake.");
    }
}
