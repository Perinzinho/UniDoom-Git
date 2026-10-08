using UnityEngine;
using UnityEditor;

public class MedkitPrefabCreator : EditorWindow
{
    [MenuItem("Tools/Create Medkit Prefab")]
    static void CreateMedkitPrefab()
    {
        GameObject medkit = new GameObject("Medkit");

        SpriteRenderer spriteRenderer = medkit.AddComponent<SpriteRenderer>();
        Sprite medkitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/medkit_sprt.png");
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

        string prefabPath = "Assets/Prefabs/Medkit.prefab";
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        PrefabUtility.SaveAsPrefabAsset(medkit, prefabPath);
        Object.DestroyImmediate(medkit);

        Debug.Log("Medkit prefab criado em: " + prefabPath);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }
}