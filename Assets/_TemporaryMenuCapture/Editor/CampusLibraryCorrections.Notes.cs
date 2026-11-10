using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UniDoom.CampusLibrary;
using TMPro;

public static partial class CampusLibraryCorrections
{
    static Mesh PaperMesh()
    {
        string path = Base + "Meshes/Library maintenance paper.asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh) return mesh;
        mesh = new Mesh { name = "Library maintenance paper" };
        mesh.vertices = new[] { new Vector3(-.135f, -.18f, 0), new Vector3(.135f, -.18f, 0), new Vector3(.135f, .18f, 0), new Vector3(-.135f, .18f, 0) };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }; mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, path); return mesh;
    }
    public static void Notes()
    {
        PrepareSigns(); var documents = new Texture2D[3]; var materials = new Material[3];
        for (int i = 0; i < 3; i++)
        {
            string path = Base + "Textures/Library maintenance note " + i + ".png"; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = true; importer.maxTextureSize = 1024; importer.npotScale = TextureImporterNPOTScale.None; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            documents[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(path); string materialPath = Base + "Materials/Library maintenance note " + i + ".mat";
            materials[i] = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!materials[i]) { materials[i] = new Material(SignMaterial); AssetDatabase.CreateAsset(materials[i], materialPath); }
            materials[i].name = "Library maintenance note " + i; materials[i].SetTexture("_BaseMap", documents[i]); materials[i].SetFloat("_Smoothness", 0); EditorUtility.SetDirty(materials[i]);
        }
        var mesh = PaperMesh();
        Prefab(root =>
        {
            var mission = root.GetComponentInChildren<LibraryMissionController>(true);
            if (root.GetComponentsInChildren<LibraryMissionTarget>(true).Count(t => t.Kind == LibraryTargetKind.Clue) != 1) throw new Exception("Paper clues already installed.");
            var old = root.GetComponentsInChildren<LibraryMissionTarget>(true).Single(t => t.Kind == LibraryTargetKind.Clue);
            old.GetComponent<Renderer>().enabled = false;
            var oldLabel = Named(root, "Readable circuit route").GetComponent<TMP_Text>(); oldLabel.text = "REGISTROS DE MANUTENÇÃO"; oldLabel.enabled = false;
            for (int i = 0; i < 3; i++)
            {
                var target = i == 0 ? old : new GameObject("Maintenance paper clue " + i, typeof(BoxCollider), typeof(LibraryMissionTarget)).GetComponent<LibraryMissionTarget>();
                if (i != 0) target.transform.SetParent(mission.transform, false);
                target.transform.localScale = Vector3.one;
                target.transform.position = i == 0 ? new Vector3(-25.9f, 1.8f, 34.21f) : i == 1 ? new Vector3(-14.7f, 1.005f, 41.6f) : new Vector3(-10.7f, 1.025f, 37.6f);
                target.transform.rotation = i == 0 ? Quaternion.Euler(0, 180, -3) : Quaternion.Euler(90, i == 1 ? -8 : 12, 0);
                var collider = target.GetComponent<BoxCollider>(); collider.center = Vector3.zero; collider.size = new Vector3(.28f, .37f, .035f); collider.isTrigger = false;
                var so = new SerializedObject(target); so.FindProperty("mission").objectReferenceValue = mission; so.FindProperty("kind").enumValueIndex = (int)LibraryTargetKind.Clue; so.FindProperty("id").intValue = i; so.FindProperty("document").objectReferenceValue = documents[i]; so.ApplyModifiedPropertiesWithoutUndo();
                Box(target.transform, "Paper thickness", Vector3.zero, new Vector3(.274f, .364f, .014f), materials[i]);
                var face = new GameObject("Printed maintenance note", typeof(MeshFilter), typeof(MeshRenderer)); face.transform.SetParent(target.transform, false); face.transform.localPosition = new Vector3(0, 0, -.009f); face.GetComponent<MeshFilter>().sharedMesh = mesh; face.GetComponent<MeshRenderer>().sharedMaterial = materials[i];
            }
            foreach (var button in mission.Buttons)
                Plate(mission.transform, SignData.Single(s => s.key == "Terminal role " + button.Id), new Vector3(button.transform.position.x, 1.59f, 43.63f), Quaternion.identity);
        }, true);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Base + "CampusLibrary.unity", OpenSceneMode.Single);
        Capture(new Vector3(-22.3f, 1.85f, 40.4f), new Vector3(-22.3f, 1.85f, 43.9f), "final-panel-horizontal");
        Capture(new Vector3(-25.9f, 1.85f, 35.7f), new Vector3(-25.9f, 1.85f, 34.2f), "final-hall-note-horizontal");
        Capture(new Vector3(-16.5f, 1.85f, 40.2f), new Vector3(-15, 1.85f, 41.6f), "final-workshop-horizontal");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        File.WriteAllText(Log + "notes.txt", "Three native paper documents installed with shared geometry, existing font and shared document viewer. Wall/notification numeric answers removed. Seven role labels fitted. Horizontal-player validation pending Play Mode.");
    }
}
