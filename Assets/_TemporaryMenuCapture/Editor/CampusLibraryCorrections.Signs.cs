using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;

public static partial class CampusLibraryCorrections
{
    [Serializable] public class SignSpec { public string key, text, theme; public float w, h; public int x, y, pw, ph; }
    [Serializable] public class SignSpecs { public SignSpec[] items; }
    static SignSpec[] SignData => JsonUtility.FromJson<SignSpecs>("{\"items\":" + File.ReadAllText(Log + "sign-specs.json") + "}").items;
    static Material SignMaterial;
    static Material Metal => AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Global/Materials/CampusInterior/Research equipment charcoal.mat");
    static Mesh Cube => AssetDatabase.LoadAssetAtPath<Mesh>(Base + "Meshes/Library picture frame cube.asset");
    static void Folder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
    static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collision = false)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = size;
        go.GetComponent<MeshFilter>().sharedMesh = Cube; go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (collision) go.AddComponent<BoxCollider>(); return go;
    }
    static void PrepareSigns()
    {
        string path = Base + "Textures/Library maintenance signs.png"; AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.sRGBTexture = true; importer.mipmapEnabled = true; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 2048; importer.alphaSource = TextureImporterAlphaSource.None; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        SignMaterial = AssetDatabase.LoadAssetAtPath<Material>(Base + "Materials/Library maintenance printed enamel.mat");
        if (!SignMaterial)
        {
            SignMaterial = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Global/Materials/CampusInterior/Campus blue enamel.mat"));
            AssetDatabase.CreateAsset(SignMaterial, Base + "Materials/Library maintenance printed enamel.mat");
        }
        SignMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path)); SignMaterial.SetColor("_BaseColor", Color.white); SignMaterial.SetFloat("_Smoothness", .22f); EditorUtility.SetDirty(SignMaterial);
        Folder(Base.TrimEnd('/'), "Meshes"); Folder(Base + "Meshes", "Printed signs");
    }
    static Mesh SignMesh(SignSpec spec)
    {
        string path = Base + "Meshes/Printed signs/" + spec.key + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (!mesh) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); } else mesh.Clear();
        mesh.name = spec.key; mesh.vertices = new[] { new Vector3(-spec.w * .5f, -spec.h * .5f, 0), new Vector3(spec.w * .5f, -spec.h * .5f, 0), new Vector3(spec.w * .5f, spec.h * .5f, 0), new Vector3(-spec.w * .5f, spec.h * .5f, 0) };
        var texture = SignMaterial.GetTexture("_BaseMap"); float x = spec.x / (float)texture.width, y = 1 - (spec.y + spec.ph) / (float)texture.height, width = spec.pw / (float)texture.width, height = spec.ph / (float)texture.height;
        mesh.uv = new[] { new Vector2(x, y), new Vector2(x + width, y), new Vector2(x + width, y + height), new Vector2(x, y + height) }; mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 }; mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh); return mesh;
    }
    static GameObject Plate(Transform parent, SignSpec spec, Vector3 position, Quaternion rotation)
    {
        var group = new GameObject("Physical plate - " + spec.key); group.transform.SetParent(parent, false); group.transform.localPosition = position; group.transform.localRotation = rotation;
        Box(group.transform, "Worn metal backing", new Vector3(0, 0, .014f), new Vector3(spec.w + .035f, spec.h + .035f, .035f), Metal);
        var face = new GameObject("Lettering baked into enamel", typeof(MeshFilter), typeof(MeshRenderer)); face.transform.SetParent(group.transform, false); face.transform.localPosition = new Vector3(0, 0, -.005f);
        face.GetComponent<MeshFilter>().sharedMesh = SignMesh(spec); face.GetComponent<MeshRenderer>().sharedMaterial = SignMaterial; return group;
    }
    public static void Signs()
    {
        PrepareSigns(); int converted = 0;
        Prefab(root =>
        {
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!label.enabled || label.name == "Generator mission status") continue;
                if (label.name == "Readable circuit route") { label.enabled = false; if (label.GetComponent<Renderer>()) label.GetComponent<Renderer>().enabled = false; continue; }
                var spec = SignData.SingleOrDefault(s => s.key == label.name); if (spec == null) throw new Exception("No physical plate for " + label.name);
                if (!label.transform.Find("Physical plate - " + spec.key)) Plate(label.transform, spec, Vector3.zero, Quaternion.identity);
                label.enabled = false; if (label.GetComponent<Renderer>()) label.GetComponent<Renderer>().enabled = false; converted++;
            }
            var remaining = root.GetComponentsInChildren<TMP_Text>(true).Where(t => t.enabled).ToArray();
            if (remaining.Length != 1 || remaining[0].name != "Generator mission status") throw new Exception("Unexpected enabled world TMP labels");
        }, true);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(Base + "CampusLibrary.unity", OpenSceneMode.Single);
        Capture(new Vector3(18.5f, 1.9f, 26), new Vector3(18.5f, 3.5f, 30.08f), "after-annex-sign");
        Capture(new Vector3(-21.5f, 1.9f, 32.7f), new Vector3(-21.5f, 3.25f, 35.83f), "after-staff-sign");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        File.WriteAllText(Log + "signs.txt", converted + " static TMP labels replaced by thick physical plates sharing one atlas. Direct-answer wall text disabled. Dynamic status remains only on the generator screen.");
    }
}
