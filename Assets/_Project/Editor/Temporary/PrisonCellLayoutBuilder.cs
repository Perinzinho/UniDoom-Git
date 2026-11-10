using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

internal static class PrisonCellLayoutBuilder
{
    private const string BasementPrefab = "Assets/_Project/Scenes/Basement/Prefabs/Basement.prefab";
    private const float StartZ = 4f;
    private const float CellWidth = 3.2f;
    private const float EndZ = 26.4f;
    private const string BiancaSpritePath = "Assets/_Project/Global/Sprites/teachers/sprite-bianca-professora.png";
    private const string BiancaAudioPath = "Assets/_Project/Scenes/Basement/Prison/Audios/Teachers/bianca_line.wav";
    private const string BiancaPrefabPath = "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-bianca.prefab";
    private const string TeacherBlackMaterialPath = "Assets/_Project/Global/Materials/Teachers/TeacherBlackBackground.mat";

    private readonly struct TeacherSpec
    {
        public readonly string Name;
        public readonly string Path;
        public TeacherSpec(string name, string path) { Name = name; Path = path; }
    }

    private static readonly TeacherSpec[] Teachers =
    {
        new("Caio", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-caio.prefab"),
        new("Fábio", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-fabio.prefab"),
        new("Henrique", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-henrique.prefab"),
        new("Cesar", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-santi.prefab"),
        new("Navarro", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-navarro.prefab"),
        new("Matheus Ferraroni", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-matheus-ferraroni.prefab"),
        new("Paulinho", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-paulinho.prefab"),
        new("Rafa", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-rafa.prefab"),
        new("Gustavo Camossi", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-gustavo-camossi.prefab"),
        new("Matheus Santi", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-matheus-santi.prefab"),
        new("Moacyr", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-moacyr.prefab"),
        new("Querino", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-querino.prefab"),
        new("William", "Assets/_Project/Scenes/Basement/Prison/Prefabs/Teacher-william.prefab"),
        new("Bianca", BiancaPrefabPath)
    };

    [MenuItem("Tools/UniDoom/Temporary/Rebuild Prison Cells")]
    public static void Build()
    {
        ConfigureBiancaAssets();
        var root = PrefabUtility.LoadPrefabContents(BasementPrefab);
        try
        {
            var prison = Find(root.transform, "Prison - ritual holding cells") ??
                throw new InvalidOperationException("Prison root not found.");
            var leftOriginal = FindDirect(prison, "Cela 07 - Paulinho") ??
                throw new InvalidOperationException("Left cell template not found.");
            var rightOriginal = FindDirect(prison, "Cela 08 - Rafa") ??
                throw new InvalidOperationException("Right cell template not found.");
            var leftPrototype = UnityEngine.Object.Instantiate(leftOriginal.gameObject);
            var rightPrototype = UnityEngine.Object.Instantiate(rightOriginal.gameObject);
            leftPrototype.name = "[Temporary left cell prototype]";
            rightPrototype.name = "[Temporary right cell prototype]";

            var leftSign = FindDirect(prison, "01")?.gameObject;
            var rightSign = FindDirect(prison, "02")?.gameObject;
            var leftBacking = DirectChildren(prison).FirstOrDefault(t => t.name == "Worn custody sign backing" && t.localPosition.x < 0)?.gameObject;
            var rightBacking = DirectChildren(prison).FirstOrDefault(t => t.name == "Worn custody sign backing" && t.localPosition.x > 0)?.gameObject;
            var leftSignPrototype = leftSign != null ? UnityEngine.Object.Instantiate(leftSign) : null;
            var rightSignPrototype = rightSign != null ? UnityEngine.Object.Instantiate(rightSign) : null;
            var leftBackingPrototype = leftBacking != null ? UnityEngine.Object.Instantiate(leftBacking) : null;
            var rightBackingPrototype = rightBacking != null ? UnityEngine.Object.Instantiate(rightBacking) : null;

            RemoveOldCells(prison);
            ResizeShellAndTransfer(prison);
            RepositionExistingProps(prison);
            RepositionServiceLights(prison);

            for (int index = 0; index < Teachers.Length; index++)
            {
                int cellId = index + 1;
                bool left = cellId % 2 == 1;
                var cell = UnityEngine.Object.Instantiate(left ? leftPrototype : rightPrototype, prison);
                cell.SetActive(true);
                cell.name = $"Cela {cellId:00} - {Teachers[index].Name}";
                ConfigureCell(cell.transform, cellId, left, Teachers[index]);
                CreateNumberSign(prison, cellId, left, left ? leftSignPrototype : rightSignPrototype,
                    left ? leftBackingPrototype : rightBackingPrototype);
            }

            if (Teachers.Length % 2 != 0) CreateBlindEndWall(prison);
            UnityEngine.Object.DestroyImmediate(leftPrototype);
            UnityEngine.Object.DestroyImmediate(rightPrototype);
            if (leftSignPrototype != null) UnityEngine.Object.DestroyImmediate(leftSignPrototype);
            if (rightSignPrototype != null) UnityEngine.Object.DestroyImmediate(rightSignPrototype);
            if (leftBackingPrototype != null) UnityEngine.Object.DestroyImmediate(leftBackingPrototype);
            if (rightBackingPrototype != null) UnityEngine.Object.DestroyImmediate(rightBackingPrototype);

            PrefabUtility.SaveAsPrefabAsset(root, BasementPrefab);
            WriteValidation(root);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void Capture()
    {
        var scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Basement/Basement.unity", OpenSceneMode.Single);
        Transform prison = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            prison = Find(root.transform, "Prison - ritual holding cells");
            if (prison != null) break;
        }
        if (prison == null) throw new InvalidOperationException("Prison root not found in scene.");
        var arrival = Find(prison, "PrisonArrivalAfterBoss") ?? throw new InvalidOperationException("Prison arrival not found.");
        CaptureView(arrival.position + Vector3.up * .65f, arrival.rotation, "prison-compact-entry.png");
        var middlePosition = prison.TransformPoint(new Vector3(0, 1.7f, 13.7f));
        var middleRotation = Quaternion.LookRotation(prison.TransformDirection(Vector3.forward), Vector3.up);
        CaptureView(middlePosition, middleRotation, "prison-compact-middle.png");
    }

    private static void CaptureView(Vector3 position, Quaternion rotation, string fileName)
    {
        var cameraObject = new GameObject("[Temporary prison review camera]");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.SetPositionAndRotation(position, rotation);
        camera.fieldOfView = 65f;
        camera.nearClipPlane = .05f;
        camera.farClipPlane = 300f;
        camera.allowHDR = true;
        camera.clearFlags = CameraClearFlags.Skybox;
        var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = target;
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        image.Apply(false, false);
        var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp", fileName));
        File.WriteAllBytes(output, image.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(cameraObject);
    }

    private static void ConfigureCell(Transform cell, int cellId, bool left, TeacherSpec teacherSpec)
    {
        cell.localPosition = Vector3.zero;
        cell.localRotation = Quaternion.identity;
        cell.localScale = Vector3.one;
        float side = left ? -1f : 1f;
        float start = StartZ + ((cellId - 1) / 2) * CellWidth;
        float center = start + CellWidth * .5f;
        float end = start + CellWidth;

        var divider = FindContains(cell, "Divisoria traseira") ?? FindContains(cell, "Divisória traseira");
        var gateTransform = FindContains(cell, "Porta da cela");
        var fixedBars = FindContains(cell, "Grade fixa");
        var lintel = FindContains(cell, "Verga sobre grades");
        var bench = FindContains(cell, "Banco de concreto");
        var oldTeacher = cell.GetComponentsInChildren<SpriteRenderer>(true)
            .Select(r => r.transform).FirstOrDefault(t => t.GetComponent<Billboard>() != null);
        if (divider == null || gateTransform == null || fixedBars == null || lintel == null || bench == null || oldTeacher == null)
            throw new InvalidOperationException($"Incomplete template for cell {cellId}.");
        UnityEngine.Object.DestroyImmediate(oldTeacher.gameObject);

        divider.name = $"Divisória traseira - Cela {cellId:00}";
        divider.localPosition = new Vector3(side * 3.35f, 1.7f, end);
        divider.localScale = new Vector3(3.65f, 3.4f, .2f);

        gateTransform.name = $"Porta da cela {cellId:00}";
        gateTransform.localPosition = new Vector3(side * 1.55f, 0f, start + .23f);
        gateTransform.localScale = new Vector3(1f, 1f, .64f);
        gateTransform.GetComponent<BoxCollider>().size = new Vector3(.13f, 2.9f, 2.365f);
        fixedBars.name = $"Grade fixa da cela {cellId:00}";
        fixedBars.localPosition = new Vector3(side * 1.55f, 0f, start + 1.75f);
        fixedBars.localScale = new Vector3(1f, 1f, .60f);
        fixedBars.GetComponent<BoxCollider>().size = new Vector3(.13f, 2.9f, 2.415f);
        lintel.localPosition = new Vector3(side * 1.55f, 3.15f, center);
        lintel.localScale = new Vector3(.2f, .5f, CellWidth);
        bench.localPosition = new Vector3(side * 4.25f, .25f, center + .35f);
        bench.localScale = new Vector3(.75f, .5f, 1.25f);
        bench.gameObject.SetActive(false);

        var teacherAsset = AssetDatabase.LoadAssetAtPath<GameObject>(teacherSpec.Path) ??
            throw new InvalidOperationException($"Missing teacher prefab: {teacherSpec.Path}");
        var teacher = (GameObject)PrefabUtility.InstantiatePrefab(teacherAsset, cell);
        teacher.name = $"Professor {teacherSpec.Name} - Cela {cellId:00}";
        teacher.transform.localPosition = new Vector3(side * 3.1f, .02f, start + 1.1f);
        teacher.transform.localRotation = Quaternion.identity;
        teacher.transform.localScale = Vector3.one;

        var gate = gateTransform.GetComponent<PrisonCellGate>() ??
            throw new InvalidOperationException($"Missing PrisonCellGate in cell {cellId}.");
        var serializedGate = new SerializedObject(gate);
        serializedGate.FindProperty("cellId").intValue = cellId;
        serializedGate.FindProperty("teacher").objectReferenceValue = teacher.transform;
        serializedGate.FindProperty("openingAngle").floatValue = left ? -90f : 90f;
        serializedGate.FindProperty("openingDuration").floatValue = 1.4f;
        serializedGate.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveOldCells(Transform prison)
    {
        var remove = DirectChildren(prison).Where(t =>
            (t.name.StartsWith("Cela ", StringComparison.Ordinal) && !t.name.EndsWith("imported props", StringComparison.Ordinal)) ||
            t.name.StartsWith("Teacher-", StringComparison.Ordinal) ||
            t.name.StartsWith("Professor ", StringComparison.Ordinal) ||
            t.name.StartsWith("Porta da cela ", StringComparison.Ordinal) ||
            t.name == "Divisória de cela" || t.name == "Colisão da grade" ||
            t.name == "Estrado enferrujado" || t.name == "Colchão abandonado" ||
            t.name == "Banco de concreto" ||
            t.name.StartsWith("Fundo gasto - Cela ", StringComparison.Ordinal) ||
            t.name.StartsWith("Fechamento compacto após a cela", StringComparison.Ordinal) ||
            (t.name.Length == 2 && int.TryParse(t.name, out _)) ||
            t.name == "Worn custody sign backing").ToArray();
        foreach (var item in remove) UnityEngine.Object.DestroyImmediate(item.gameObject);
    }

    private static void ResizeShellAndTransfer(Transform prison)
    {
        foreach (var item in DirectChildren(prison))
        {
            if (item.name == "Piso da custódia") { item.localPosition = new Vector3(0, -.15f, 15.2f); item.localScale = new Vector3(10.6f, .3f, 22.8f); }
            else if (item.name == "Teto baixo") { item.localPosition = new Vector3(0, 3.55f, 15.2f); item.localScale = new Vector3(10.6f, .3f, 23.2f); }
            else if (item.name == "Parede externa da custódia") { item.localPosition = new Vector3(item.localPosition.x, 1.7f, 15.2f); item.localScale = new Vector3(.3f, 3.4f, 23.2f); }
            else if (item.name == "Parede do registro") item.localPosition = new Vector3(0, 1.7f, 26.6f);
        }

        foreach (var name in new[] { "Exit to CampusExterior", "Return from campus", "Campus transfer door surround" })
        {
            var item = FindDirect(prison, name);
            if (item != null && item.GetComponent<UniDoom.Utils.ParallaxPortalView>() == null)
                item.localPosition += new Vector3(0, 0, 1.6f);
        }
        foreach (var item in DirectChildren(prison))
            if (item.name.StartsWith("CUSTÓDIA", StringComparison.Ordinal) ||
                (item.name == "Worn custody sign backing" && item.localPosition.x == 0 && item.localPosition.z > 20f))
                item.localPosition += new Vector3(0, 0, 1.6f);
    }

    private static void RepositionExistingProps(Transform prison)
    {
        foreach (var item in DirectChildren(prison))
        {
            int id = ParseTrailingCellId(item.name);
            if (id < 1 || id > 6) continue;
            float side = id % 2 == 1 ? -1f : 1f;
            float start = StartZ + ((id - 1) / 2) * CellWidth;
            if (item.name.StartsWith("Medkit - Cela", StringComparison.Ordinal))
                item.localPosition = new Vector3(side * 2.4f, .046f, start + 1.25f);
            else if (item.name.StartsWith("Caixa de municao - Cela", StringComparison.Ordinal))
                item.localPosition = new Vector3(side * 2.5f, .34f, start + 2.35f);
        }
        var imported = FindDirect(prison, "Imported custody props");
        if (imported == null) return;
        foreach (Transform group in imported)
        {
            int id = ParseCellId(group.name);
            if (id < 1 || id > 6) continue;
            float oldCenter = 5.05f + ((id - 1) / 2) * 5f;
            float newCenter = StartZ + ((id - 1) / 2) * CellWidth + CellWidth * .5f;
            group.localPosition += new Vector3(0, 0, newCenter - oldCenter);
            group.gameObject.SetActive(false);
        }
    }

    private static void RepositionServiceLights(Transform prison)
    {
        var lights = DirectChildren(prison).Where(t => t.name == "Luz de serviço" || t.name == "Luz de servico - Celas 7 e 8" ||
                t.name.StartsWith("Luz de serviço - trecho ", StringComparison.Ordinal))
            .OrderBy(t => t.localPosition.z).ToArray();
        float[] positions = { 6.2f, 10.4f, 14.6f, 18.8f, 23f };
        for (int i = 0; i < Math.Min(lights.Length, positions.Length); i++)
        {
            lights[i].name = $"Luz de serviço - trecho {i + 1:00}";
            lights[i].localPosition = new Vector3(0, 2.62f, positions[i]);
        }
        var support = FindDirect(prison, "Suporte da luminaria");
        if (support != null) support.localPosition = new Vector3(0, 3.35f, 23f);
    }

    private static void CreateNumberSign(Transform prison, int cellId, bool left, GameObject signPrototype, GameObject backingPrototype)
    {
        float side = left ? -1f : 1f;
        float center = StartZ + ((cellId - 1) / 2) * CellWidth + CellWidth * .5f;
        if (signPrototype != null)
        {
            var sign = UnityEngine.Object.Instantiate(signPrototype, prison);
            sign.name = cellId.ToString("00");
            sign.transform.localPosition = new Vector3(side * 1.45f, 2.45f, center);
            sign.transform.localRotation = signPrototype.transform.localRotation;
            sign.transform.localScale = signPrototype.transform.localScale;
            foreach (var text in sign.GetComponentsInChildren<TMP_Text>(true)) text.text = cellId.ToString("00");
        }
        if (backingPrototype != null)
        {
            var backing = UnityEngine.Object.Instantiate(backingPrototype, prison);
            backing.name = $"Fundo gasto - Cela {cellId:00}";
            backing.transform.localPosition = new Vector3(side * 1.49f, 2.45f, center);
            backing.transform.localRotation = backingPrototype.transform.localRotation;
            backing.transform.localScale = backingPrototype.transform.localScale;
        }
    }

    private static void CreateBlindEndWall(Transform prison)
    {
        var source = DirectChildren(prison).FirstOrDefault(t => t.name == "Parede frontal" && t.localPosition.x > 0);
        if (source == null) return;
        var wall = UnityEngine.Object.Instantiate(source.gameObject, prison);
        wall.name = $"Fechamento compacto após a cela {Teachers.Length - 1:00}";
        wall.transform.localPosition = new Vector3(1.55f, 1.7f, 24.8f);
        wall.transform.localRotation = Quaternion.identity;
        wall.transform.localScale = new Vector3(.25f, 3.4f, CellWidth);
    }

    private static void ConfigureBiancaAssets()
    {
        AssetDatabase.ImportAsset(BiancaSpritePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(BiancaSpritePath) as TextureImporter ??
            throw new InvalidOperationException("Bianca sprite sheet not found at " + BiancaSpritePath);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width != 1536 || height != 1024)
            throw new InvalidOperationException($"Bianca sprite sheet must be 1536x1024, found {width}x{height}.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 186.666667f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        var textureSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(textureSettings);
        textureSettings.spriteMeshType = SpriteMeshType.FullRect;
        textureSettings.spriteExtrude = 1;
        importer.SetTextureSettings(textureSettings);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var old = provider.GetSpriteRects().FirstOrDefault(rect => rect.name == "bianca_Idle");
        var spriteRect = new SpriteRect
        {
            name = "bianca_Idle",
            rect = new Rect(192, 24, 144, 336),
            alignment = SpriteAlignment.Custom,
            pivot = new Vector2(.5f, .012f),
            spriteID = old != null ? old.spriteID : GUID.Generate()
        };
        provider.SetSpriteRects(new[] { spriteRect });
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(
            new List<SpriteNameFileIdPair> { new("bianca_Idle", spriteRect.spriteID) });
        provider.Apply();
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAllAssetsAtPath(BiancaSpritePath).OfType<Sprite>()
            .FirstOrDefault(item => item.name == "bianca_Idle") ??
            throw new InvalidOperationException("Bianca idle sprite was not imported.");
        var audio = AssetDatabase.LoadAssetAtPath<AudioClip>(BiancaAudioPath) ??
            throw new InvalidOperationException("Bianca audio not found at " + BiancaAudioPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(TeacherBlackMaterialPath) ??
            throw new InvalidOperationException("Teacher black-background material not found.");
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BiancaPrefabPath);
        var teacher = existing != null ? PrefabUtility.LoadPrefabContents(BiancaPrefabPath) : new GameObject("Teacher-bianca");
        try
        {
            teacher.name = "Teacher-bianca";
            var renderer = teacher.GetComponent<SpriteRenderer>();
            if (!renderer) renderer = teacher.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            if (teacher.GetComponent<Billboard>() == null) teacher.AddComponent<Billboard>();
            var source = teacher.GetComponent<AudioSource>();
            if (!source) source = teacher.AddComponent<AudioSource>();
            source.clip = audio;
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0;
            PrefabUtility.SaveAsPrefabAsset(teacher, BiancaPrefabPath);
        }
        finally
        {
            if (existing != null) PrefabUtility.UnloadPrefabContents(teacher);
            else UnityEngine.Object.DestroyImmediate(teacher);
        }
    }

    private static void WriteValidation(GameObject root)
    {
        var gates = root.GetComponentsInChildren<PrisonCellGate>(true).OrderBy(g => g.CellId).ToArray();
        var report = new StringBuilder();
        report.AppendLine($"gates={gates.Length}; ids={string.Join(",", gates.Select(g => g.CellId))}");
        foreach (var gate in gates)
        {
            var voice = gate.Voice;
            report.AppendLine($"cell={gate.CellId:00}; teacher={(voice != null ? voice.name : "NO_AUDIO_SOURCE")}; clip={(voice != null && voice.clip != null ? voice.clip.name : "NO_CLIP")}; position={gate.transform.localPosition}");
        }
        var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/prison-layout-validation.txt"));
        File.WriteAllText(output, report.ToString());
        Debug.Log($"[PrisonCellLayoutBuilder] {output}");
    }

    private static int ParseTrailingCellId(string name)
    {
        int marker = name.LastIndexOf("Cela ", StringComparison.Ordinal);
        if (marker < 0) return -1;
        return int.TryParse(name.Substring(marker + 5).Trim(), out int id) ? id : -1;
    }

    private static int ParseCellId(string name)
    {
        int marker = name.IndexOf("Cela ", StringComparison.Ordinal);
        if (marker < 0) return -1;
        string digits = new string(name.Skip(marker + 5).TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out int id) ? id : -1;
    }

    private static IEnumerable<Transform> DirectChildren(Transform parent)
    {
        foreach (Transform child in parent) yield return child;
    }

    private static Transform FindDirect(Transform parent, string name) => DirectChildren(parent).FirstOrDefault(t => t.name == name);

    private static Transform FindContains(Transform root, string value)
    {
        if (root.name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0) return root;
        foreach (Transform child in root)
        {
            var found = FindContains(child, value);
            if (found != null) return found;
        }
        return null;
    }

    private static Transform Find(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = Find(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
