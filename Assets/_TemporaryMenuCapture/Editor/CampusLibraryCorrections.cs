using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UniDoom.CampusLibrary;

// Explicit one-off scene correction tools. Saved assets never depend on this class.
public static partial class CampusLibraryCorrections
{
    const string Base = "Assets/_Project/Scenes/CampusLibrary/";
    const string PrefabPath = Base + "Prefabs/CampusLibrary.prefab";
    const string Log = "Logs/CampusLibraryCorrections-";
    [Serializable] public class Item
    {
        public string path, name, details;
        public Vector3 position, rotation, scale, boundsCenter, boundsSize;
        public List<string> children = new List<string>();
    }
    [Serializable] public class Inspection
    {
        public List<Item> shelves = new List<Item>(), labels = new List<Item>(), mission = new List<Item>();
        public List<string> overlaps = new List<string>(), roots = new List<string>();
    }
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
    static Bounds BoundsOf(Transform t)
    {
        var meshes = t.GetComponentsInChildren<MeshRenderer>(true).Where(r => !r.GetComponent<TMP_Text>() && r.GetComponent<MeshFilter>() && r.GetComponent<MeshFilter>().sharedMesh).ToArray();
        if (meshes.Length == 0) return new Bounds(t.position, Vector3.zero);
        var b = meshes[0].bounds; foreach (var renderer in meshes.Skip(1)) b.Encapsulate(renderer.bounds); return b;
    }
    static Item Describe(Transform t)
    {
        var b = BoundsOf(t); return new Item { name = t.name, path = PathOf(t), position = t.position, rotation = t.eulerAngles, scale = t.lossyScale, boundsCenter = b.center, boundsSize = b.size };
    }
    static void Prefab(Action<GameObject> action, bool save)
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try { action(root); if (save) PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void SetField(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Write(string stage, object data) => File.WriteAllText(Log + stage + ".json", JsonUtility.ToJson(data, true));
    public static void Inspect()
    {
        var data = new Inspection();
        Prefab(root =>
        {
            foreach (Transform child in root.transform) { data.roots.Add(child.name); foreach (Transform nested in child) data.roots.Add("  " + nested.name); }
            var shelves = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.EndsWith("full bookcase")).ToArray();
            foreach (var shelf in shelves)
            {
                var item = Describe(shelf);
                foreach (var mesh in shelf.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (!mesh.sharedMesh) continue;
                    var renderer = mesh.GetComponent<Renderer>();
                    item.children.Add(mesh.name + " mesh=" + AssetDatabase.GetAssetPath(mesh.sharedMesh) + " local=" + mesh.transform.localPosition + " size=" + mesh.sharedMesh.bounds.size + " materials=" + (renderer ? string.Join(";", renderer.sharedMaterials.Select(m => m ? AssetDatabase.GetAssetPath(m) : "MISSING")) : "none"));
                }
                data.shelves.Add(item);
            }
            for (int i = 0; i < shelves.Length; i++) for (int j = i + 1; j < shelves.Length; j++)
            {
                var a = BoundsOf(shelves[i]); var b = BoundsOf(shelves[j]); var extent = Vector3.Min(a.max, b.max) - Vector3.Max(a.min, b.min);
                if (extent.x > .03f && extent.z > .03f && extent.y > .5f) data.overlaps.Add(PathOf(shelves[i]) + " <> " + PathOf(shelves[j]) + " overlap=" + extent.ToString("F3"));
            }
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                var item = Describe(label.transform); item.details = "active=" + label.gameObject.activeInHierarchy + " text=" + label.text + " rect=" + label.rectTransform.sizeDelta; data.labels.Add(item);
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(true).Where(t => PathOf(t).Contains("Emergency mission") && !PathOf(t).Contains("full bookcase") && !PathOf(t).Contains("Annex chair")))
            {
                if (t.GetComponent<MeshFilter>() || t.GetComponent<LibraryMissionTarget>() || t.GetComponent<LibraryMaintenanceSafety>() || t.GetComponent<LibraryZombieSpawner>() || t.GetComponent<BoxCollider>() || t.name.ToLowerInvariant().Contains("spawn") || t.name.ToLowerInvariant().Contains("entry"))
                {
                    var item = Describe(t); var collider = t.GetComponent<BoxCollider>();
                    if (collider) item.details += " collider center=" + collider.center + " size=" + collider.size + " trigger=" + collider.isTrigger;
                    var target = t.GetComponent<LibraryMissionTarget>(); if (target) item.details += " target=" + target.Kind + " id=" + target.Id;
                    data.mission.Add(item);
                }
            }
        }, false);
        Write("inspection", data);
        Debug.Log("Library inspection: " + data.shelves.Count + " bookcases, " + data.overlaps.Count + " overlap candidates, " + data.labels.Count + " TMP labels.");
    }
}
