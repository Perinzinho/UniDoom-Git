using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class ClassRoomSurfaceBuilder
{
    const string Art = TotalChaosMaterialConverter.Art;
    public static readonly string[] Targets = { ClassRoomLightingBuilder.Interior, "Assets/prefabs/Scenario/ClassRoom.prefab" };
    public static void Build()
    {
        ClassRoomLightingBuilder.RequireIsolated();
        ClassRoomLightingVerifier.Capture("PBRBefore");
        foreach (string asset in new[] { "concrete_wall_006", "rusty_metal_02", "wood_table_worn", "worn_tile_floor", "rock_face" }) Import(asset);
        foreach (string path in Targets)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!renderer.enabled || renderer.GetComponent<TextMesh>() || PrefabUtility.IsPartOfPrefabInstance(renderer)) continue;
                    var mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        var original = mats[i]; if (!TotalChaosMaterialConverter.Eligible(original)) continue;
                        var name = original.name;
                        TotalChaosSurfacePreset preset;
                        if (name == "Floor") preset = TotalChaosSurfacePreset.Tiles;
                        else if (name == "Wood" || name == "Door") preset = TotalChaosSurfacePreset.Wood;
                        else if (name == "Steel" || name == "Iron") preset = TotalChaosSurfacePreset.Metal;
                        else if (name == "Plaster" || name == "Ceiling" || name == "Rubble") preset = TotalChaosSurfacePreset.Concrete;
                        else continue;
                        var converted = TotalChaosMaterialConverter.Convert(original, preset);
                        if (name == "Floor") converted.SetTextureScale("_BaseMap", new Vector2(15, 13.333333f));
                        else if (path == Targets[1] && name == "Plaster") converted.SetTextureScale("_BaseMap", new Vector2(8,2));
                        else if (name == "Wood" || name == "Door") converted.SetTextureScale("_BaseMap", Vector2.one * 2);
                        else if (path == Targets[0]) converted.SetTextureScale("_BaseMap", Vector2.one * .7f);
                        EditorUtility.SetDirty(converted); mats[i] = converted;
                    }
                    renderer.sharedMaterials = mats;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets(); ClassRoomLightingBuilder.BakeReflection(Art + "/ClassRoomReflection.asset"); Verify(); ClassRoomLightingVerifier.Capture("PBRFinal");
        Debug.Log("CLASSROOM_PBR_BUILD_OK");
    }
    public static void Finish()
    {
        ClassRoomLightingBuilder.RequireIsolated();
        Import("rock_face");
        foreach(string path in Directory.GetFiles(Art + "/Materials", "*_Stone.mat"))
            if(AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<Material>(path.Replace('\\','/')).GetTexture("_BaseMap")).Contains("worn_tile_floor"))
            {
                string target = path.Replace("_Stone.mat", "_Tiles.mat").Replace('\\','/');
                string error = AssetDatabase.MoveAsset(path.Replace('\\','/'), target);
                if (!string.IsNullOrEmpty(error)) throw new Exception(error);
            }
        foreach(string path in Directory.GetFiles(Art + "/Materials", "*.mat"))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path.Replace('\\','/'));
            if (path.EndsWith("_Wood.mat")) { mat.SetColor("_BaseColor", new Color(1.35f,1.4f,1.32f)); mat.SetFloat("_Smoothness", .5f); }
            if (path.EndsWith("_Metal.mat")) mat.SetColor("_BaseColor", new Color(.65f,.67f,.63f));
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets(); ClassRoomLightingBuilder.BakeReflection(Art + "/ClassRoomReflection.asset"); VerifyAndRender();
    }
    static void Import(string asset)
    {
        string directory = Art + "/Textures/" + asset + "/"; int size = asset == "concrete_wall_006" ? 2048 : 1024;
        foreach (string role in new[] { "Diffuse", "nor_gl", "arm", "Displacement" })
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(directory + role + ".png");
            importer.textureType = role == "nor_gl" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = role == "Diffuse"; importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear; importer.anisoLevel = 4; importer.maxTextureSize = size;
            importer.isReadable = role == "arm"; importer.textureCompression = role == "arm" ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            var settings = importer.GetDefaultPlatformTextureSettings(); settings.maxTextureSize = size; settings.textureCompression = importer.textureCompression;
            importer.SetPlatformTextureSettings(settings); importer.SaveAndReimport();
        }
        var arm = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + "arm.png"); var pixels = arm.GetPixels32();
        for (int i = 0; i < pixels.Length; i++) { var p = pixels[i]; pixels[i] = new Color32(p.b, p.r, p.g, (byte)(255-p.g)); }
        var mask = new Texture2D(arm.width, arm.height, TextureFormat.RGBA32, false, true);
        mask.SetPixels32(pixels); mask.Apply(); File.WriteAllBytes(directory + "UnityMask.png", mask.EncodeToPNG()); Object.DestroyImmediate(mask);
        AssetDatabase.ImportAsset(directory + "UnityMask.png", ImportAssetOptions.ForceUpdate);
        var packed = (TextureImporter)AssetImporter.GetAtPath(directory + "UnityMask.png");
        packed.sRGBTexture = false; packed.mipmapEnabled = true; packed.filterMode = FilterMode.Trilinear; packed.anisoLevel = 4;
        packed.maxTextureSize = size; packed.textureCompression = TextureImporterCompression.CompressedHQ; packed.SaveAndReimport();
        var armImporter = (TextureImporter)AssetImporter.GetAtPath(directory + "arm.png");
        armImporter.isReadable = false; armImporter.textureCompression = TextureImporterCompression.CompressedHQ; armImporter.SaveAndReimport();
    }
    public static void Verify()
    {
        ClassRoomLightingBuilder.RequireIsolated();
        var shader = Shader.Find("UniDoom/TotalChaosInspiredSurface");
        if (!shader || ShaderUtil.ShaderHasError(shader)) throw new Exception("Surface shader compilation failed");
        var batcher = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static,
            null, new[] { typeof(Shader), typeof(int) }, null);
        if (batcher == null) throw new Exception("SRP Batcher diagnostic unavailable");
        int compatibility = (int)batcher.Invoke(null, new object[] { shader, 0 });
        if (compatibility != 0)
        {
            var reason = typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            throw new Exception("SRP Batcher incompatibility: " + reason?.Invoke(null,new object[] { shader,0,compatibility }));
        }
        Debug.Log("CLASSROOM_PBR_SRP_BATCHER_OK");
        int converted = 0;
        foreach (string path in Directory.GetFiles(Art + "/Materials", "*.mat"))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path.Replace('\\','/'));
            if (mat.shader != shader || !mat.GetTexture("_BumpMap") || !mat.GetTexture("_OcclusionMap") || !mat.GetTexture("_MetallicGlossMap")) throw new Exception("PBR bindings missing: " + path);
            if (!mat.IsKeywordEnabled("_NORMALMAP") || mat.IsKeywordEnabled("_PARALLAXMAP")) throw new Exception("Unbudgeted shader feature: " + path);
            var normal = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(mat.GetTexture("_BumpMap")));
            if (normal.textureType != TextureImporterType.NormalMap || normal.sRGBTexture) throw new Exception("Normal import mismatch");
            converted++;
        }
        var root = PrefabUtility.LoadPrefabContents(Targets[1]);
        try
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            var before = renderers.ToDictionary(r => r, r => r.sharedMaterials);
            int replaced = TotalChaosMaterialConverter.Revert(root);
            if (replaced < 50) throw new Exception("Material reversal did not reach the converted classroom surfaces");
            if (root.GetComponentInChildren<ReflectionProbe>().customBakedTexture != AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/Art/ClassRoom/Lighting/ClassRoomReflection.asset"))
                throw new Exception("Original reflection not restored by reversal");
            foreach (var pair in before) pair.Key.sharedMaterials = pair.Value;
            if (TotalChaosMaterialConverter.Eligible(root.GetComponentInChildren<TMPro.TMP_Text>()?.fontSharedMaterial)) throw new Exception("UI material eligible");
            Debug.Log("CLASSROOM_PBR_REVERSAL_OK renderers=" + replaced);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        ClassRoomLightingVerifier.Verify();
        Debug.Log("CLASSROOM_PBR_VERIFIED materials=" + converted);
    }
    public static void VerifyAndRender()
    {
        ClassRoomLightingVerifier.Capture("PBRFinal"); Verify();
        VerifySurfaceControls();
        var shader = Shader.Find("UniDoom/TotalChaosInspiredSurface");
        foreach (var message in ShaderUtil.GetShaderMessages(shader))
            if (message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error) throw new Exception(message.message);
        Debug.Log("CLASSROOM_PBR_RENDER_OK");
    }
    public static void VerifyAll()
    {
        VerifyAndRender(); ClassRoomHorrorVerifier.VerifyAndRender();
        Debug.Log("CLASSROOM_PBR_ALL_CHECKS_OK");
    }
    static void VerifySurfaceControls()
    {
        var source = Directory.GetFiles(Art + "/Materials", "*_Wood.mat").First();
        var material = new Material(AssetDatabase.LoadAssetAtPath<Material>(source.Replace('\\','/')));
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.transform.position = new Vector3(300,30,304); cube.transform.localScale = Vector3.one * 2;
        cube.GetComponent<MeshRenderer>().sharedMaterial = material;
        var go = new GameObject("Surface shader GPU check"); var camera = go.AddComponent<Camera>(); camera.enabled = false;
        camera.transform.position = new Vector3(300,30,300); camera.nearClipPlane = .1f; camera.farClipPlane = 8;
        var lamp = new GameObject("Surface shader GPU key light"); var light = lamp.AddComponent<Light>(); light.type = LightType.Spot;
        lamp.transform.position = new Vector3(299.5f,30.8f,301.5f); lamp.transform.LookAt(cube.transform); light.intensity = 5; light.range = 8; light.spotAngle = 90;
        var rt = new RenderTexture(96,96,24,RenderTextureFormat.ARGBFloat); camera.targetTexture = rt;
        var image = new Texture2D(96,96,TextureFormat.RGBAFloat,false,true); var previous = RenderTexture.active;
        Color[] Render(Vector4 controls)
        {
            material.SetVector("_SpecColor", controls); for(int i=0;i<3;i++) camera.Render();
            RenderTexture.active = rt; image.ReadPixels(new Rect(0,0,96,96),0,0); image.Apply(); return image.GetPixels();
        }
        float Difference(Color[] a, Color[] b) => a.Zip(b,(x,y)=>Mathf.Abs(x.r-y.r)+Mathf.Abs(x.g-y.g)+Mathf.Abs(x.b-y.b)).Average();
        try
        {
            var baseline = Render(new Vector4(0,0,1,1));
            if (Difference(baseline,Render(new Vector4(1,0,1,1))) < .000001f) throw new Exception("Dirt control has no rendered effect");
            if (Difference(baseline,Render(new Vector4(0,1,1,1))) < .000001f) throw new Exception("Wetness control has no rendered effect");
            if (Difference(baseline,Render(new Vector4(0,0,0,1))) < .000001f) throw new Exception("Saturation control has no rendered effect");
            if (Difference(baseline,Render(new Vector4(0,0,1,1.25f))) < .000001f) throw new Exception("Contrast control has no rendered effect");
            material.SetTexture("_DetailNormalMap", material.GetTexture("_BumpMap")); material.EnableKeyword("_DETAIL_MULX2");
            material.EnableKeyword("_PARALLAXMAP"); Render(new Vector4(0,0,1,1));
            if (ShaderUtil.ShaderHasError(material.shader)) throw new Exception("Optional detail/parallax variant failed");
            Debug.Log("CLASSROOM_PBR_GPU_CONTROLS_OK dirt wetness saturation contrast detail parallax");
        }
        finally
        {
            RenderTexture.active = previous == rt ? null : previous; camera.targetTexture = null;
            Object.DestroyImmediate(image); Object.DestroyImmediate(rt); Object.DestroyImmediate(material);
            Object.DestroyImmediate(cube); Object.DestroyImmediate(lamp); Object.DestroyImmediate(go);
        }
    }
}
