using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Asset integration for the irregular JPEG sheet: preserve the source, key its white
// background and align every pose on the same canvas. Runtime uses saved assets only.
public static class AxeSpriteSheetSetup
{
    const string Version = "unidoom-axe-v4";
    const string Source = "Assets/_Project/Global/Sprites/axe_spritesheet.jpg";
    const string Atlas = "Assets/_Project/Global/Sprites/AxeHUD.png";
    const string Prefab = "Assets/_Project/Global/Prefabs/Weapons/AxeEquipped.prefab";
    const string Definition = "Assets/_Project/Global/Prefabs/Weapons/AxeDefinition.asset";
    const string Player = "Assets/_Project/Global/Prefabs/Player/Player.prefab";
    const string SwingSound = "Assets/_Project/Global/Audio/Weapon/Axe/axe_swing_sfx_trimmed.wav";
    const int Width = 384, Height = 224;

    [InitializeOnLoadMethod]
    static void Install()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Source)) return;
            var importer = AssetImporter.GetAtPath(Source);
            if (importer != null && importer.userData == Version && File.Exists(Definition))
            {
                ConfigureAttackSound();
                return;
            }
            try { Configure(); }
            catch (Exception e) { Debug.LogException(e); }
        };
    }

    [MenuItem("Tools/UniDoom/Configure Axe Sprite Sheet")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Configure the axe outside Play Mode.");
        BuildAtlas();
        Sprite[] frames = ImportFrames();
        Axe prefab = AssetDatabase.LoadAssetAtPath<Axe>(Prefab);
        if (prefab == null)
        {
            var root = new GameObject("Axe");
            try
            {
                root.AddComponent<Axe>();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, Prefab).GetComponent<Axe>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        var definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(Definition);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            AssetDatabase.CreateAsset(definition, Definition);
        }
        var data = new SerializedObject(definition);
        data.FindProperty("axePrefab").objectReferenceValue = prefab;
        SetFrames(data.FindProperty("idleFrames"), new[] { frames[0] });
        SetFrames(data.FindProperty("shootFrames"), new[]
            { frames[0], frames[1], frames[2], frames[3], frames[4], frames[5], frames[6], frames[0] });
        float[] durations = { .07f, .12f, .08f, .07f, .06f, .07f, .11f, .14f };
        var timing = data.FindProperty("shootFrameDurations");
        timing.arraySize = durations.Length;
        for (int i = 0; i < durations.Length; i++) timing.GetArrayElementAtIndex(i).floatValue = durations[i];
        SetFrames(data.FindProperty("rechargeFrames"), Array.Empty<Sprite>());
        data.FindProperty("shootFrameRate").floatValue = 8f / .72f;
        data.FindProperty("hudSize").vector2Value = new Vector2(1280, 746.6667f);
        data.FindProperty("hudViewportLimit").vector2Value = new Vector2(.90f, .64f);
        data.ApplyModifiedPropertiesWithoutUndo();

        GameObject player = PrefabUtility.LoadPrefabContents(Player);
        try
        {
            var manager = player.GetComponent<PlayerWeaponManager>();
            if (manager == null) throw new InvalidOperationException("Player has no weapon manager.");
            var managerData = new SerializedObject(manager);
            if (managerData.FindProperty("axeWeapon").objectReferenceValue != definition)
            {
                managerData.FindProperty("axeWeapon").objectReferenceValue = definition;
                managerData.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(player, Player);
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(player); }
        var sourceImporter = AssetImporter.GetAtPath(Source);
        sourceImporter.userData = Version;
        sourceImporter.SaveAndReimport();
        AssetDatabase.SaveAssetIfDirty(definition);
        ConfigureAttackSound();
        File.WriteAllText("Temp/axe-setup-result.txt", "PASS: saved axe atlas, seven sprites, prefab, definition and player binding.");
        Debug.Log("Axe: persistent sprites and permanent player weapon configured.");
    }

    [MenuItem("Tools/UniDoom/Configure Axe Attack Sound")]
    public static void ConfigureAttackSound()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var prefab = AssetDatabase.LoadAssetAtPath<Axe>(Prefab);
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SwingSound);
        if (prefab == null || clip == null) return;
        var data = new SerializedObject(prefab);
        var sound = data.FindProperty("swingSound");
        if (sound.objectReferenceValue != null) return;
        sound.objectReferenceValue = clip;
        data.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SavePrefabAsset(prefab.gameObject);
    }

    static void SetFrames(SerializedProperty property, Sprite[] frames)
    {
        property.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
    }

    static void BuildAtlas()
    {
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var atlas = new Texture2D(Width * 4, Height * 2, TextureFormat.RGBA32, false);
        try
        {
            if (!source.LoadImage(File.ReadAllBytes(Source))) throw new InvalidOperationException("Invalid axe sheet.");
            if (source.width != 626 || source.height != 304)
                throw new InvalidOperationException("Axe sheet dimensions changed; review pose rectangles before regenerating.");
            atlas.SetPixels32(new Color32[atlas.width * atlas.height]);
            // Top-left source coordinates. The tiny world pickup in the upper right is unused.
            RectInt[] poses = {
                new RectInt(367, 103, 148, 201), new RectInt(515, 81, 111, 223),
                new RectInt(0, 0, 353, 94), new RectInt(0, 187, 134, 117),
                new RectInt(134, 247, 53, 57), new RectInt(186, 208, 71, 96),
                new RectInt(257, 150, 111, 154)
            };
            int[] left = { Width - 148, Width - 111, 0, 0, 85, 140, 195 };
            for (int i = 0; i < poses.Length; i++)
            {
                RectInt rect = poses[i];
                Color[] pixels = source.GetPixels(rect.x, source.height - rect.y - rect.height, rect.width, rect.height);
                KeyBackground(pixels, rect.width, rect.height);
                // Every cropped wrist/handle exits through the bottom of the viewport.
                int bottom = 0;
                atlas.SetPixels((i % 4) * Width + left[i], (i / 4) * Height + bottom,
                    rect.width, rect.height, pixels);
            }
            atlas.Apply();
            File.WriteAllBytes(Atlas, atlas.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(atlas);
        }
        AssetDatabase.ImportAsset(Atlas, ImportAssetOptions.ForceSynchronousImport);
    }

    static void KeyBackground(Color[] pixels, int width, int height)
    {
        // Flood only near-white pixels connected to the crop border, keeping blade highlights.
        var visited = new bool[pixels.Length];
        var queue = new Queue<int>();
        void Visit(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            int index = y * width + x;
            if (visited[index]) return;
            visited[index] = true;
            Color p = pixels[index];
            if (Mathf.Min(p.r, p.g, p.b) < .84f) return;
            pixels[index] = Color.clear;
            queue.Enqueue(index);
        }
        for (int x = 0; x < width; x++) { Visit(x, 0); Visit(x, height - 1); }
        for (int y = 0; y < height; y++) { Visit(0, y); Visit(width - 1, y); }
        while (queue.Count > 0)
        {
            int index = queue.Dequeue(), x = index % width, y = index / width;
            Visit(x - 1, y); Visit(x + 1, y); Visit(x, y - 1); Visit(x, y + 1);
        }
        // JPEG ringing can leave isolated grey specks after keying. Each pose is one
        // connected silhouette, so discard detached compression debris, not artwork edges.
        var labels = new int[pixels.Length];
        int label = 0, largestLabel = 0, largestCount = 0;
        for (int seed = 0; seed < pixels.Length; seed++)
        {
            if (pixels[seed].a == 0 || labels[seed] != 0) continue;
            label++;
            int count = 0;
            labels[seed] = label;
            queue.Enqueue(seed);
            void Connect(int x, int y)
            {
                if (x < 0 || y < 0 || x >= width || y >= height) return;
                int index = y * width + x;
                if (pixels[index].a == 0 || labels[index] != 0) return;
                labels[index] = label;
                queue.Enqueue(index);
            }
            while (queue.Count > 0)
            {
                int index = queue.Dequeue(), x = index % width, y = index / width;
                count++;
                Connect(x - 1, y); Connect(x + 1, y); Connect(x, y - 1); Connect(x, y + 1);
            }
            if (count > largestCount) { largestCount = count; largestLabel = label; }
        }
        if (largestCount == 0) throw new InvalidOperationException("Axe pose has no visible pixels.");
        for (int i = 0; i < pixels.Length; i++)
            if (labels[i] != largestLabel) pixels[i] = Color.clear;
    }

    static Sprite[] ImportFrames()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(Atlas);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = 100;
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var ids = provider.GetSpriteRects().ToDictionary(x => x.name, x => x.spriteID);
        SpriteRect[] rects = Enumerable.Range(0, 7).Select(i => new SpriteRect
        {
            name = $"Axe_{i:00}", rect = new Rect(i % 4 * Width, i / 4 * Height, Width, Height),
            alignment = SpriteAlignment.Custom, pivot = new Vector2(1, 0),
            spriteID = ids.TryGetValue($"Axe_{i:00}", out GUID id) ? id : GUID.Generate()
        }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
            rects.Select(x => new SpriteNameFileIdPair(x.name, x.spriteID)).ToList());
        provider.Apply();
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().OrderBy(x => x.name).ToArray();
    }
}
