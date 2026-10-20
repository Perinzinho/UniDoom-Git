using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Imports the hand-authored enemy sheets with stable frame names and wires them into
/// the shared enemy prefab. Kept as an integration tool so the sheets can be reimported.
/// </summary>
public static class EnemySpriteSheetSetup
{
    const string ImportVersion = "unidoom-enemy-sprites-v1";
    const string WheelchairTexturePath = "Assets/_Project/Global/Textures/Enemy/ZombieWheelchair.png";
    const string WomanTexturePath = "Assets/_Project/Global/Textures/Enemy/ZombieWoman.png";
    const string WheelchairAppearancePath = "Assets/_Project/Global/Prefabs/Enemy/WheelchairZombieAppearance.asset";
    const string WomanAppearancePath = "Assets/_Project/Global/Prefabs/Enemy/WomanZombieAppearance.asset";
    const string WheelchairMaterialPath = "Assets/_Project/Global/Materials/Enemy/UniDoomWheelchairZombie.mat";
    const string WomanMaterialPath = "Assets/_Project/Global/Materials/Enemy/UniDoomZombieWoman.mat";
    const string EnemyPrefabPath = "Assets/_Project/Global/Prefabs/Enemy/Enemy.prefab";

    // Boundaries deliberately include a little black padding so wide death poses stay intact.
    static readonly int[] FrameLefts = { 165, 323, 480, 638, 795, 953, 1110, 1268, 1438 };

    [InitializeOnLoadMethod]
    static void ConfigureNewSheetsOnce()
    {
        var wheelchair = AssetImporter.GetAtPath(WheelchairTexturePath);
        var woman = AssetImporter.GetAtPath(WomanTexturePath);
        if (wheelchair == null || woman == null ||
            (wheelchair.userData == ImportVersion && woman.userData == ImportVersion))
            return;

        EditorApplication.delayCall += () =>
        {
            try { Configure(); }
            catch (Exception exception) { Debug.LogException(exception); }
        };
    }

    [MenuItem("Tools/UniDoom/Configure Enemy Sprite Sheets")]
    public static void Configure()
    {
        ConfigureImporter(WheelchairTexturePath, "Wheelchair", 145);
        ConfigureImporter(WomanTexturePath, "Woman", 135);

        Material wheelchairMaterial = ConfigureMaterial(WheelchairMaterialPath, WheelchairTexturePath);
        Material womanMaterial = ConfigureMaterial(WomanMaterialPath, WomanTexturePath);

        EnemySpriteAppearance wheelchair = AssetDatabase.LoadAssetAtPath<EnemySpriteAppearance>(WheelchairAppearancePath);
        if (wheelchair == null)
            throw new InvalidOperationException($"Missing appearance: {WheelchairAppearancePath}");

        EnemySpriteAppearance woman = AssetDatabase.LoadAssetAtPath<EnemySpriteAppearance>(WomanAppearancePath);
        if (woman == null)
        {
            woman = ScriptableObject.CreateInstance<EnemySpriteAppearance>();
            AssetDatabase.CreateAsset(woman, WomanAppearancePath);
        }

        ConfigureAppearance(wheelchair, WheelchairTexturePath, "Wheelchair", wheelchairMaterial, 1.55f, .56f);
        ConfigureAppearance(woman, WomanTexturePath, "Woman", womanMaterial, 1.95f, .5f);
        ConfigurePrefab(wheelchair, woman);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("UniDoom enemy sprite sheets configured: base, wheelchair student and woman zombie now spawn randomly.");
    }

    static void ConfigureImporter(string path, string prefix, float pixelsPerUnit)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException($"Texture not found: {path}");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        Dictionary<string, GUID> existingIds = provider.GetSpriteRects()
            .ToDictionary(sprite => sprite.name, sprite => sprite.spriteID);
        SpriteRect[] slices = BuildSlices(prefix, existingIds).ToArray();
        provider.SetSpriteRects(slices);
        ISpriteNameFileIdDataProvider names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names.SetNameFileIdPairs(slices.Select(slice => new SpriteNameFileIdPair(slice.name, slice.spriteID)).ToList());
        provider.Apply();
        importer.userData = ImportVersion;
        importer.SaveAndReimport();
    }

    static IEnumerable<SpriteRect> BuildSlices(string prefix, Dictionary<string, GUID> existingIds)
    {
        // Source image is 1448x1086. Rect coordinates use Unity's bottom-left origin.
        foreach (SpriteRect frame in BuildRow(prefix + "_Front", 746, 270, .055f, existingIds)) yield return frame;
        foreach (SpriteRect frame in BuildRow(prefix + "_Back", 426, 270, .08f, existingIds)) yield return frame;
        foreach (SpriteRect frame in BuildRow(prefix + "_Death", 106, 270, .055f, existingIds)) yield return frame;
    }

    static IEnumerable<SpriteRect> BuildRow(
        string name,
        int y,
        int height,
        float pivotY,
        Dictionary<string, GUID> existingIds)
    {
        for (int i = 0; i < 8; i++)
        {
            string frameName = $"{name}_{i:00}";
            yield return new SpriteRect
            {
                name = frameName,
                rect = new Rect(FrameLefts[i], y, FrameLefts[i + 1] - FrameLefts[i], height),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(.5f, pivotY),
                border = Vector4.zero,
                spriteID = existingIds.TryGetValue(frameName, out GUID id) ? id : GUID.Generate()
            };
        }
    }

    static Material ConfigureMaterial(string materialPath, string texturePath)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("UniDoom/Characters/Zombie Sprite");
            if (shader == null) throw new InvalidOperationException("Zombie Sprite shader was not found.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }

        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        material.SetFloat("_UseSilhouetteMask", 0);
        material.SetFloat("_BlackThreshold", .012f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void ConfigureAppearance(
        EnemySpriteAppearance appearance,
        string texturePath,
        string prefix,
        Material material,
        float colliderHeight,
        float colliderRadius)
    {
        Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name);

        appearance.material = material;
        appearance.idleFrames = Frames(sprites, prefix + "_Front", 1);
        appearance.frontWalkFrames = Frames(sprites, prefix + "_Front", 8);
        appearance.backWalkFrames = Frames(sprites, prefix + "_Back", 8);
        appearance.deathFrames = Frames(sprites, prefix + "_Death", 8);
        appearance.sideWalkFrames = Array.Empty<Sprite>();
        appearance.leftWalkFrames = Array.Empty<Sprite>();
        appearance.rightWalkFrames = Array.Empty<Sprite>();
        appearance.attackFrames = Array.Empty<Sprite>();
        appearance.painFrames = Array.Empty<Sprite>();
        appearance.visualScale = 1;
        appearance.colliderHeight = colliderHeight;
        appearance.colliderRadius = colliderRadius;
        appearance.idleFramesPerSecond = 1;
        appearance.walkFramesPerSecond = 9;
        appearance.deathDuration = 1.35f;
        EditorUtility.SetDirty(appearance);
    }

    static Sprite[] Frames(Dictionary<string, Sprite> sprites, string prefix, int count)
    {
        var result = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            string name = $"{prefix}_{i:00}";
            if (!sprites.TryGetValue(name, out result[i]))
                throw new InvalidOperationException($"Missing imported sprite: {name}");
        }
        return result;
    }

    static void ConfigurePrefab(EnemySpriteAppearance wheelchair, EnemySpriteAppearance woman)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);
        try
        {
            EnemySpriteAnimator animator = root.GetComponentInChildren<EnemySpriteAnimator>(true);
            if (animator == null) throw new InvalidOperationException("Enemy prefab has no EnemySpriteAnimator.");

            var serialized = new SerializedObject(animator);
            serialized.FindProperty("alternateAppearance").objectReferenceValue = null;
            SerializedProperty appearances = serialized.FindProperty("alternateAppearances");
            appearances.arraySize = 2;
            appearances.GetArrayElementAtIndex(0).objectReferenceValue = wheelchair;
            appearances.GetArrayElementAtIndex(1).objectReferenceValue = woman;
            serialized.FindProperty("alternateChance").floatValue = .66f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
