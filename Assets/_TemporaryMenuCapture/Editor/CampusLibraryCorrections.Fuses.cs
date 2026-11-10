using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UniDoom.CampusLibrary;

public static partial class CampusLibraryCorrections
{
    public static void Fuses()
    {
        string path = Base + "Textures/Library maintenance fuse.png"; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 512;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 1024; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        Prefab(root =>
        {
            string materialPath = Base + "Materials/Library maintenance fuse sprite.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material) { material = new Material(Shader.Find("UniDoom/Characters/Zombie Sprite")); AssetDatabase.CreateAsset(material, materialPath); }
            material.SetTexture("_MainTex", sprite.texture); material.SetTexture("_SilhouetteTex", Texture2D.whiteTexture); material.SetFloat("_UseSilhouetteMask", 0); material.SetFloat("_UseSourceAlpha", 1); material.SetFloat("_UseGreenScreen", 0); material.SetFloat("_BlackThreshold", 0); material.SetColor("_Color", Color.white); EditorUtility.SetDirty(material);
            var fuses = root.GetComponentsInChildren<LibraryMissionTarget>(true).Where(t => t.Kind == LibraryTargetKind.Fuse).ToArray();
            if (fuses.Length != 3) throw new Exception("Expected three fuses.");
            foreach (var fuse in fuses)
            {
                foreach (var mesh in fuse.GetComponentsInChildren<MeshRenderer>(true)) mesh.enabled = false;
                if (!fuse.transform.Find("Ceramic cartridge fuse sprite"))
                {
                    var go = new GameObject("Ceramic cartridge fuse sprite", typeof(SpriteRenderer), typeof(Billboard)); go.transform.SetParent(fuse.transform, false);
                    go.transform.localScale = Vector3.one * (.4f / sprite.bounds.size.x); var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material;
                }
                fuse.GetComponentInChildren<SpriteRenderer>(true).sharedMaterial = material;
                if (fuse.GetComponentsInChildren<Collider>(true).Length != 1) throw new Exception("Fuse interaction collider changed.");
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Lettering baked into enamel")) t.localPosition = new Vector3(0, 0, -.012f);
        }, true);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Base + "CampusLibrary.unity", OpenSceneMode.Single);
        var mission = UnityEngine.Object.FindAnyObjectByType<LibraryMissionController>();
        foreach (var fuse in mission.Fuses) { fuse.gameObject.SetActive(true); fuse.GetComponentInChildren<SpriteRenderer>().transform.rotation = Quaternion.identity; }
        Capture(new Vector3(9.4f, 1.25f, 33.8f), new Vector3(9.4f, .825f, 34.5f), "after-fuse-sprite");
        Capture(new Vector3(-21.5f, 1.85f, 36.3f), new Vector3(-22.5f, 2.8f, 38.8f), "after-generator-plate");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        File.WriteAllText(Log + "fuses.txt", "Three fuse targets use one transparent sprite and existing billboard/material. Original target IDs and colliders retained; old mesh renderers disabled.");
    }
}
