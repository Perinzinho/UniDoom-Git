using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UniDoom.CampusLibrary;
using Unity.AI.Navigation;

public static partial class CampusLibraryCorrections
{
    public static void Zombies()
    {
        Prefab(root =>
        {
            var spawner = root.GetComponentInChildren<LibraryZombieSpawner>(true); var so = new SerializedObject(spawner); var entries = so.FindProperty("additionalEntryPoints");
            if (entries.arraySize != 0) throw new System.Exception("Additional entries already configured.");
            var points = new[] { new Vector3(-8, 0, 23), new Vector3(18.5f, 0, 32.5f), new Vector3(3, 0, 40) };
            var surface = root.GetComponentInChildren<NavMeshSurface>(true);
            var navigation = NavMesh.AddNavMeshData(surface.navMeshData, surface.transform.position, surface.transform.rotation);
            try {
            entries.arraySize = points.Length;
            for (int i = 0; i < points.Length; i++)
            {
                if (!NavMesh.SamplePosition(points[i], out var hit, 1.5f, NavMesh.AllAreas)) throw new System.Exception("No navigation at reinforcement point " + i);
                var marker = new GameObject("Library reinforcement entrance " + (i + 2)).transform; marker.SetParent(spawner.transform, false); marker.position = hit.position;
                entries.GetArrayElementAtIndex(i).objectReferenceValue = marker;
            }
            so.FindProperty("aliveLimit").intValue = 4; so.ApplyModifiedPropertiesWithoutUndo();
            } finally { navigation.Remove(); }
        }, true);
        AssetDatabase.SaveAssets(); File.WriteAllText(Log + "zombies.txt", "Alive limit set to four; original entry retained plus three navigation-tested markers. Delay 4s, original enemy stats, 8m/visibility/safe area checks and end-of-mission stop retained. Runtime validation pending.");
    }
}
