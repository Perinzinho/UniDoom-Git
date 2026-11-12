using System;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class DirectorSummonSetup
{
    const string BossSheet = "Assets/_Project/Scenes/Basement/Boss/Textures/DirectorSummonCast.png";
    const string PortalSheet = "Assets/_Project/Scenes/Basement/Boss/Textures/DirectorSummonPortal.png";
    const string BossPrefab = "Assets/_Project/Scenes/Basement/Boss/Prefabs/DirectorBoss.prefab";
    const string EnemyPrefab = "Assets/_Project/Global/Prefabs/Enemy/Enemy.prefab";

    public static void Apply()
    {
        Sprite[] bossFrames = Import(BossSheet, "DirectorSummon", 95.5f, 0);
        Sprite[] portalFrames = Import(PortalSheet, "DirectorPortal", 130, .11f);
        var root = PrefabUtility.LoadPrefabContents(BossPrefab);
        try
        {
            var animator = root.GetComponentInChildren<DirectorBossAnimator>();
            var summon = root.GetComponent<DirectorBossSummon>();
            if (!summon) summon = root.AddComponent<DirectorBossSummon>();
            var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefab)?.GetComponent<Enemy>();
            if (!animator || !enemy) throw new Exception("Boss animator or zombie prefab missing.");

            var animationData = new SerializedObject(animator);
            SetSprites(animationData.FindProperty("summonFrames"), bossFrames);
            animationData.ApplyModifiedPropertiesWithoutUndo();
            var summonData = new SerializedObject(summon);
            summonData.FindProperty("zombiePrefab").objectReferenceValue = enemy;
            SetSprites(summonData.FindProperty("portalFrames"), portalFrames);
            summonData.FindProperty("damagePerWave").intValue = 200;
            summonData.FindProperty("zombiesPerWave").intValue = 5;
            summonData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, BossPrefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Debug.Log("DIRECTOR_SUMMON_READY|cast=8|portal=8|damage=200|zombies=5");
    }

    static void SetSprites(SerializedProperty property, Sprite[] sprites)
    {
        property.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    static Sprite[] Import(string path, string prefix, float ppu, float pivotY)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (!importer) throw new Exception("Missing image: " + path);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var sprites = new SpriteRect[8];
        var names = new SpriteNameFileIdPair[8];
        for (int row = 0; row < 2; row++)
            for (int col = 0; col < 4; col++)
            {
                int index = row * 4 + col;
                int left = Mathf.RoundToInt(col * width / 4f);
                int right = Mathf.RoundToInt((col + 1) * width / 4f);
                int top = Mathf.RoundToInt(row * height / 2f);
                int bottom = Mathf.RoundToInt((row + 1) * height / 2f);
                var name = $"{prefix}_{index:00}";
                var id = GUID.Generate();
                sprites[index] = new SpriteRect {
                    name = name,
                    spriteID = id,
                    rect = new Rect(left, height - bottom, right - left, bottom - top),
                    pivot = new Vector2(.5f, pivotY),
                    alignment = SpriteAlignment.Custom
                };
                names[index] = new SpriteNameFileIdPair(name, id);
            }
        provider.SetSpriteRects(sprites);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(names.ToList());
        provider.Apply();
        importer.SaveAndReimport();
        var result = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (result.Length != 8) throw new Exception("Expected eight frames in " + path);
        return result;
    }
}
