using System;
using System.Collections.Generic;
using System.IO;
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
    const string SilhouetteVersion = "unidoom-silhouette-black8-speckles96-v1";

    // Boundaries deliberately include a little black padding so wide death poses stay intact.
    static readonly int[] FrameLefts = { 165, 323, 480, 638, 795, 953, 1110, 1268, 1438 };

    [InitializeOnLoadMethod]
    static void ConfigureNewSheetsOnce()
    {
        var wheelchair = AssetImporter.GetAtPath(WheelchairTexturePath);
        var woman = AssetImporter.GetAtPath(WomanTexturePath);
        if (wheelchair == null || woman == null) return;
        if (wheelchair.userData == ImportVersion && woman.userData == ImportVersion)
        {
            if (!HasCurrentSilhouette(WheelchairMaterialPath, WheelchairTexturePath) ||
                !HasCurrentSilhouette(WomanMaterialPath, WomanTexturePath))
                EditorApplication.delayCall += RefreshTransparencyMasks;
            return;
        }

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
        material.SetTexture("_SilhouetteTex", ConfigureSilhouette(texturePath));
        material.SetFloat("_UseSilhouetteMask", 1);
        material.SetFloat("_UseSourceAlpha", 0);
        material.SetFloat("_BlackThreshold", .012f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static string SilhouettePath(string texturePath) => Path.ChangeExtension(texturePath, null) + "Silhouette.asset";
    static string SilhouetteStamp(string texturePath) => SilhouetteVersion + ":" + AssetDatabase.GetAssetDependencyHash(texturePath);

    static bool HasCurrentSilhouette(string materialPath, string texturePath)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        string path = SilhouettePath(texturePath);
        var importer = AssetImporter.GetAtPath(path);
        return material != null && material.GetFloat("_UseSilhouetteMask") > .5f &&
            material.GetTexture("_SilhouetteTex") == AssetDatabase.LoadAssetAtPath<Texture2D>(path) &&
            importer != null && importer.userData == SilhouetteStamp(texturePath);
    }

    [MenuItem("Tools/UniDoom/Refresh Enemy Transparency Masks")]
    public static void RefreshTransparencyMasks()
    {
        ConfigureMaterial(WheelchairMaterialPath, WheelchairTexturePath);
        ConfigureMaterial(WomanMaterialPath, WomanTexturePath);
        AssetDatabase.SaveAssets();
    }

    static Texture2D ConfigureSilhouette(string texturePath)
    {
        string path = SilhouettePath(texturePath);
        string stamp = SilhouetteStamp(texturePath);
        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var importer = AssetImporter.GetAtPath(path);
        if (mask != null && importer != null && importer.userData == stamp) return mask;

        // Read the source without changing its importer, sprite rectangles or fileIDs.
        var readable = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(readable, File.ReadAllBytes(texturePath)))
                throw new InvalidOperationException("Could not read sprite sheet: " + texturePath);
            int width = readable.width, height = readable.height;
            Color32[] pixels = readable.GetPixels32();
            var silhouette = new byte[pixels.Length];
            var dark = new bool[pixels.Length];
            var visited = new bool[pixels.Length];
            var queue = new int[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                var color = pixels[i];
                dark[i] = color.a < 128 || Mathf.Max(color.r, Mathf.Max(color.g, color.b)) <= 8;
                silhouette[i] = dark[i] ? (byte)0 : (byte)255;
            }

            for (int start = 0; start < pixels.Length; start++)
            {
                if (!dark[start] || visited[start]) continue;
                int head = 0, tail = 0;
                bool touchesBorder = false, hasTransparentSource = false;
                queue[tail++] = start; visited[start] = true;
                while (head < tail)
                {
                    int index = queue[head++], x = index % width, y = index / width;
                    touchesBorder |= x == 0 || x == width - 1 || y == 0 || y == height - 1;
                    hasTransparentSource |= pixels[index].a < 128;
                    if (x > 0) Enqueue(index - 1);
                    if (x < width - 1) Enqueue(index + 1);
                    if (y > 0) Enqueue(index - width);
                    if (y < height - 1) Enqueue(index + width);
                }
                // Repair enclosed dark speckles; retain larger gaps between limbs and chair parts.
                if (!touchesBorder && !hasTransparentSource && tail <= 96)
                    for (int i = 0; i < tail; i++) silhouette[queue[i]] = 255;

                void Enqueue(int index)
                {
                    if (visited[index] || !dark[index]) return;
                    visited[index] = true; queue[tail++] = index;
                }
            }

            if (mask == null)
            {
                mask = new Texture2D(width, height, TextureFormat.R8, false, true);
                mask.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(mask, path);
            }
            else mask.Reinitialize(width, height, TextureFormat.R8, false);
            mask.filterMode = FilterMode.Point;
            mask.wrapMode = TextureWrapMode.Clamp;
            mask.SetPixelData(silhouette, 0);
            mask.Apply(false, false);
            EditorUtility.SetDirty(mask);
            AssetDatabase.SaveAssetIfDirty(mask);
            importer = AssetImporter.GetAtPath(path);
            importer.userData = stamp;
            importer.SaveAndReimport();
            return mask;
        }
        finally { UnityEngine.Object.DestroyImmediate(readable); }
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
            var variants = new List<UnityEngine.Object> { wheelchair, woman };
            for (int i = 0; i < appearances.arraySize; i++)
            {
                var existing = appearances.GetArrayElementAtIndex(i).objectReferenceValue;
                if (existing != null && !variants.Contains(existing)) variants.Add(existing);
            }
            appearances.arraySize = variants.Count;
            for (int i = 0; i < variants.Count; i++)
                appearances.GetArrayElementAtIndex(i).objectReferenceValue = variants[i];
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
