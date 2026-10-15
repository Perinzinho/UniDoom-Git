using UnityEngine;
using UnityEditor;

public class MedkitPrefabCreator : EditorWindow
{
    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    [MenuItem("Tools/Create Medkit Prefab")]
    static void CreateMedkitPrefab()
    {
        GameObject medkit = new GameObject("Medkit");

        SpriteRenderer spriteRenderer = medkit.AddComponent<SpriteRenderer>();
        Sprite medkitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Global/Sprites/medkit_sprt.png");
        if (medkitSprite != null)
        {
            spriteRenderer.sprite = medkitSprite;
        }

        BoxCollider boxCollider = medkit.AddComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.size = new Vector3(1f, 1f, 1f);

        medkit.AddComponent<MedkitPickup>();
        medkit.tag = "Untagged";
        medkit.layer = LayerMask.NameToLayer("Default");

        string prefabPath = "Assets/_Project/Global/Prefabs/Pickups/Medkit.prefab";
        EnsureFolder("Assets/_Project/Global/Prefabs/Pickups");

        PrefabUtility.SaveAsPrefabAsset(medkit, prefabPath);
        Object.DestroyImmediate(medkit);

        Debug.Log("Medkit prefab criado em: " + prefabPath);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }
}