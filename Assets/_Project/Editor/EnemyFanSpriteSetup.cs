using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>Import integration for the green-screen Wanessa sheet, including isolated sprite outlines.</summary>
public static class EnemyFanSpriteSetup
{
    const string TexturePath = "Assets/_Project/Global/Textures/Enemy/ZombieWanessaWolf.png";
    const string AppearancePath = "Assets/_Project/Global/Prefabs/Enemy/FanZombieAppearance.asset";
    const string MaterialPath = "Assets/_Project/Global/Materials/Enemy/UniDoomFanZombie.mat";
    const string PrefabPath = "Assets/_Project/Global/Prefabs/Enemy/Enemy.prefab";
    const string Version = "unidoom-wanessa-zombie-v2";

    [InitializeOnLoadMethod]
    static void OnReload()
    {
        EditorApplication.delayCall += ConfigureIfNeeded;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += ConfigureIfNeeded;
        };
    }

    public static void ConfigureIfNeeded()
    {
        var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null || importer.userData == Version || EditorApplication.isPlayingOrWillChangePlaymode) return;
        try { Configure(); }
        catch (Exception exception) { Debug.LogException(exception); }
    }

    [MenuItem("Tools/UniDoom/Configure Fan Zombie")]
    public static void Configure()
    {
        var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Save the fan sheet at " + TexturePath);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width != 1448 || height != 1086)
            throw new InvalidOperationException("Wanessa zombie sheet must be the original 1448x1086 image.");

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 125;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.Tight;
        settings.spriteExtrude = 0;
        importer.SetTextureSettings(settings);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var ids = provider.GetSpriteRects().ToDictionary(rect => rect.name, rect => rect.spriteID);
        var slices = new List<SpriteRect>();
        var outlines = new Dictionary<GUID, Vector2[]>();
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(TexturePath));
        Color32[] pixels = source.GetPixels32();
        UnityEngine.Object.DestroyImmediate(source);
        // Bounds are measured in source pixels from the top-left, not a uniform grid.
        void Add(string name, int left, int top, int right, int bottom, bool isolate = true)
        {
            var rect = new Rect(left, height - bottom, right - left, bottom - top);
            GUID spriteId = ids.TryGetValue(name, out GUID id) ? id : GUID.Generate();
            slices.Add(new SpriteRect { name = name, rect = rect, pivot = new Vector2(.5f, 0),
                alignment = SpriteAlignment.Custom,
                spriteID = spriteId });
            if (isolate) outlines[spriteId] = BuildOutline(pixels, width, rect);
        }
        int[] frontLeft = { 20, 197, 373, 549, 728, 908, 1089, 1268 };
        int[] frontRight = { 177, 352, 525, 706, 880, 1063, 1245, 1420 };
        int[] backLeft = { 20, 203, 383, 559, 734, 911, 1092, 1267 };
        int[] backRight = { 175, 359, 535, 714, 886, 1066, 1241, 1424 };
        int[] backBottom = { 537, 535, 537, 536, 536, 537, 541, 539 };
        for (int i = 0; i < 8; i++)
        {
            Add($"Fan_Front_{i:00}", frontLeft[i], 31, frontRight[i], 276);
            Add($"Fan_Back_{i:00}", backLeft[i], 286, backRight[i], backBottom[i]);
        }
        int[] attackLeft = { 19, 180, 353, 554, 898, 1105, 1287 };
        int[] attackRight = { 171, 359, 545, 718, 1101, 1264, 1433 };
        int[] attackTop = { 549, 539, 529, 554, 554, 554, 554 };
        for (int i = 0; i < attackLeft.Length; i++)
            Add($"Fan_Attack_{i:00}", attackLeft[i], attackTop[i], attackRight[i], 794);
        int[] deathLeft = { 21, 216, 370, 518, 676, 851, 1118 };
        int[] deathTop = { 797, 801, 814, 821, 840, 859, 867 };
        int[] deathRight = { 201, 352, 504, 665, 838, 1108, 1435 };
        int[] deathBottom = { 989, 989, 967, 963, 958, 950, 951 };
        for (int i = 0; i < deathLeft.Length; i++)
            Add($"Fan_Death_{i:00}", deathLeft[i], deathTop[i], deathRight[i], deathBottom[i]);
        Add("Fan_Projectile", 1238, 965, 1407, 1074);
        slices[slices.Count - 1].pivot = new Vector2(.5f, .5f);
        provider.SetSpriteRects(slices.ToArray());
        var outlineProvider = provider.GetDataProvider<ISpriteOutlineDataProvider>();
        foreach (var outline in outlines)
            outlineProvider.SetOutlines(outline.Key, new List<Vector2[]> { outline.Value });
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
            slices.Select(slice => new SpriteNameFileIdPair(slice.name, slice.spriteID)).ToList());
        provider.Apply();
        importer.SaveAndReimport();

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            var shader = Shader.Find("UniDoom/Characters/Zombie Sprite");
            if (shader == null) throw new InvalidOperationException("Zombie Sprite shader missing.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        material.SetFloat("_UseSilhouetteMask", 0);
        material.SetFloat("_UseGreenScreen", 1);
        material.SetFloat("_BlackThreshold", .012f);
        EditorUtility.SetDirty(material);

        var frames = AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().ToDictionary(sprite => sprite.name);
        var appearance = AssetDatabase.LoadAssetAtPath<EnemySpriteAppearance>(AppearancePath);
        if (appearance == null)
        {
            appearance = ScriptableObject.CreateInstance<EnemySpriteAppearance>();
            AssetDatabase.CreateAsset(appearance, AppearancePath);
        }
        appearance.material = material;
        appearance.frontWalkFrames = Enumerable.Range(0, 8).Select(i => frames[$"Fan_Front_{i:00}"]).ToArray();
        appearance.backWalkFrames = Enumerable.Range(0, 8).Select(i => frames[$"Fan_Back_{i:00}"]).ToArray();
        appearance.idleFrames = new[] { appearance.frontWalkFrames[0] };
        appearance.attackFrames = Enumerable.Range(0, 7).Select(i => frames[$"Fan_Attack_{i:00}"]).ToArray();
        appearance.deathFrames = Enumerable.Range(0, 7).Select(i => frames[$"Fan_Death_{i:00}"]).ToArray();
        appearance.deathDuration = 1.2f;
        appearance.projectileSprite = frames["Fan_Projectile"];
        appearance.projectileScale = 1.1f;
        appearance.colliderHeight = 1.95f;
        appearance.colliderRadius = .5f;
        appearance.attackDuration = .84f;
        appearance.projectileReleaseDelay = .36f;
        EditorUtility.SetDirty(appearance);

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var animator = root.GetComponentInChildren<EnemySpriteAnimator>(true);
            if (animator == null) throw new InvalidOperationException("Enemy prefab has no sprite animator.");
            var serialized = new SerializedObject(animator);
            var alternates = serialized.FindProperty("alternateAppearances");
            bool exists = false;
            for (int i = 0; i < alternates.arraySize; i++)
                exists |= alternates.GetArrayElementAtIndex(i).objectReferenceValue == appearance;
            if (!exists)
            {
                alternates.arraySize++;
                alternates.GetArrayElementAtIndex(alternates.arraySize - 1).objectReferenceValue = appearance;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        importer.userData = Version;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        Debug.Log("Fan zombie configured and included in the shared enemy spawn variations.");
    }

    // A rectangular cut alone includes neighbouring hands/feet near the raised fan.
    // Keep the largest connected character, then trace its horizontal envelope.
    // The green-screen shader removes the gaps inside that envelope without erasing black clothing.
    static Vector2[] BuildOutline(Color32[] pixels, int textureWidth, Rect rect)
    {
        int w = (int)rect.width, h = (int)rect.height;
        var visited = new bool[w * h];
        var largest = new List<int>();
        for (int start = 0; start < visited.Length; start++)
        {
            if (visited[start]) continue;
            var component = new List<int>();
            var queue = new Queue<int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                int p = queue.Dequeue();
                if (visited[p]) continue;
                visited[p] = true;
                int x = p % w, y = p / w;
                Color32 c = pixels[((int)rect.y + y) * textureWidth + (int)rect.x + x];
                if (c.g > c.r + 40 && c.g > c.b + 40) continue;
                component.Add(p);
                if (x > 0) queue.Enqueue(p - 1);
                if (x + 1 < w) queue.Enqueue(p + 1);
                if (y > 0) queue.Enqueue(p - w);
                if (y + 1 < h) queue.Enqueue(p + w);
            }
            if (component.Count > largest.Count) largest = component;
        }
        if (largest.Count == 0) throw new InvalidOperationException("Empty Wanessa sprite: " + rect);
        int[] left = Enumerable.Repeat(w, h).ToArray(), right = Enumerable.Repeat(-1, h).ToArray();
        foreach (int p in largest)
        {
            int x = p % w, y = p / w;
            left[y] = Mathf.Min(left[y], x);
            right[y] = Mathf.Max(right[y], x + 1);
        }
        var points = new List<Vector2>();
        for (int y = 0; y < h; y++)
            if (right[y] >= 0) { points.Add(new Vector2(left[y], y)); points.Add(new Vector2(left[y], y + 1)); }
        for (int y = h - 1; y >= 0; y--)
            if (right[y] >= 0) { points.Add(new Vector2(right[y], y + 1)); points.Add(new Vector2(right[y], y)); }
        var reduced = new List<Vector2>();
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 previous = points[(i + points.Count - 1) % points.Count];
            Vector2 current = points[i], next = points[(i + 1) % points.Count];
            Vector2 a = current - previous, b = next - current;
            if (a == Vector2.zero || b == Vector2.zero || (a.x * b.y == a.y * b.x && Vector2.Dot(a, b) >= 0)) continue;
            reduced.Add(current - new Vector2(w, h) * .5f);
        }
        return reduced.ToArray();
    }
}

sealed class EnemyFanSheetPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] previous)
    {
        if (imported.Contains("Assets/_Project/Global/Textures/Enemy/ZombieWanessaWolf.png"))
            EditorApplication.delayCall += EnemyFanSpriteSetup.ConfigureIfNeeded;
    }
}
