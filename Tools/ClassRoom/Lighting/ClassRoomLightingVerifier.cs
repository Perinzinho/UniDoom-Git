using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class ClassRoomLightingVerifier
{
    const string Key = "UniDoom.ClassRoomLighting.PlayCheck";
    const string Output = "Preview/ClassRoomLighting";
    static int frames;
    static Camera playCamera;
    static TCRegionAtmosphere[] regions;
    static TCRegionAtmosphere classroom;
    static Light[] roomLights;
    static float[] intensities;
    static bool originalFog, originalPost;
    static float originalDensity;
    static Color originalAmbient, originalBackground;
    static CameraClearFlags originalClear;
    static Light[] suns;
    static float[] sunIntensities;
    static float maxDrift;
    static UniversalAdditionalLightData[] shadowData;
    static int[] savedTiers;
    static int atlasReductions;
    static ClassRoomLightingVerifier()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, trace, type) =>
        {
            if (Application.isPlaying && playCamera && classroom && frames < 202 && classroom.WeightAt(playCamera.transform.position)>0 &&
                message.StartsWith("Reduced additional punctual light shadows resolution")) atlasReductions++;
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + ".Done", false))
            { SessionState.SetBool(Key, false); SessionState.SetBool(Key + ".Done", false); Debug.Log("CLASSROOM_LIGHTING_PLAYMODE_OK"); EditorApplication.Exit(0); }
        };
    }
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static void Verify()
    {
        ClassRoomLightingBuilder.RequireIsolated();
        var root = PrefabUtility.LoadPrefabContents("Assets/prefabs/Scenario/ClassRoom.prefab");
        try
        {
            var region = root.GetComponentInChildren<TCRegionAtmosphere>();
            Check(region && region.theme == "classroom" && region.blendDistance == 2, "ClassRoom does not inherit the corrected region");
            var volume = region.GetComponent<Volume>();
            Check(volume && !volume.isGlobal && volume.blendDistance == region.blendDistance, "Volume/environment transition mismatch");
            Check(volume.sharedProfile.TryGet<ColorAdjustments>(out var grading) && grading.postExposure.value == 0, "Exposure must be fixed at 0 EV");
            Check(volume.sharedProfile.TryGet<Tonemapping>(out var tone) && tone.mode.value == TonemappingMode.Neutral, "Neutral highlight rolloff missing");
            Check(root.GetComponentsInChildren<TCFlickerLight>(true).All(f => !f.enabled), "Unwanted flicker remains enabled");
            Check(root.GetComponentsInChildren<Light>().Count(l => l.intensity > 0 && l.shadows != LightShadows.None) <= 3, "Room shadow budget exceeded");
            var probe = root.GetComponentInChildren<ReflectionProbe>();
            Check(probe && probe.mode == ReflectionProbeMode.Custom && probe.customBakedTexture, "Stable room reflection missing");
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.enabled) continue;
                if (!renderer.GetComponent<TextMesh>())
                {
                    var filter = renderer.GetComponent<MeshFilter>();
                    Check(filter && filter.sharedMesh, "Missing mesh on " + renderer.name);
                }
                foreach (var mat in renderer.sharedMaterials)
                {
                    Check(mat && mat.shader && mat.shader.name != "Hidden/InternalErrorShader", "Missing material on " + renderer.name);
                    foreach (string property in new[] { "_BaseMap", "_BumpMap", "_MetallicGlossMap" })
                    {
                        if (!mat.HasProperty(property) || !mat.GetTexture(property)) continue;
                        string path = AssetDatabase.GetAssetPath(mat.GetTexture(property));
                        if (!path.StartsWith(ClassRoomLightingBuilder.Art)) continue;
                        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        Check((path.EndsWith("/Floor.png") || importer.maxTextureSize == 512) && importer.filterMode == FilterMode.Trilinear && importer.mipmapEnabled, "Texture filtering regression: " + path);
                    }
                }
            }
            Debug.Log("CLASSROOM_LIGHTING_PREFAB_OK renderers=" + root.GetComponentsInChildren<Renderer>().Length);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    public static void VerifyFinal()
    {
        Verify(); Capture("Final");
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.shadows!=LightShadows.None && l.intensity>0))
            Debug.Log("SHADOW_LIGHT_AUDIT " + light.name + " at " + light.transform.position + " range=" + light.range + " tier=" + light.GetUniversalAdditionalLightData().additionalLightsShadowResolutionTier);
    }
    public static void Capture(string label)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        ShaderUtil.allowAsyncCompilation = false; Directory.CreateDirectory(Output);
        var room = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t => t.name == "ClassRoom" && !t.parent);
        var interior = room.Find("AbandonedClassRoomInterior"); var choices = room.GetComponentsInChildren<TCRegionAtmosphere>();
        var hidden = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r => r.enabled && (!r.transform.IsChildOf(room) || (r.GetComponent<TextMesh>() && !r.transform.IsChildOf(interior)))).ToArray();
        foreach (var renderer in hidden) renderer.enabled = false;
        var go = new GameObject("ClassRoom real atmosphere capture"); var camera = go.AddComponent<Camera>(); camera.enabled = false;
        camera.fieldOfView = 70; camera.nearClipPlane = .04f; camera.farClipPlane = 100;
        camera.GetUniversalAdditionalCameraData().volumeLayerMask = 1;
        var positions = new[] { new Vector3(2.1f,1.84f,-10.35f), new Vector3(5.6f,1.84f,1.1f), new Vector3(-4.25f,1.84f,3.6f), new Vector3(2.1f,1.84f,-10.35f) };
        var targets = new[] { new Vector3(.3f,1.5f,4.65f), new Vector3(7.5f,.55f,3.5f), new Vector3(2,1.2f,-7), new Vector3(.3f,1.5f,4.65f) };
        var names = new[] { "Aisle", "Front", "Door", "Flashlight" };
        try
        {
            for (int n = 0; n < positions.Length; n++)
            {
                camera.transform.position = interior.TransformPoint(positions[n]); camera.transform.LookAt(interior.TransformPoint(targets[n]));
                TCRegionAtmosphere.ApplyForCamera(camera, choices, n == 3);
                var rt = new RenderTexture(1280,720,24,RenderTextureFormat.ARGBHalf); camera.targetTexture = rt;
                for (int i = 0; i < 8; i++) camera.Render();
                var ldr = new RenderTexture(1280,720,0,RenderTextureFormat.ARGB32); Graphics.Blit(rt, ldr);
                var previous = RenderTexture.active; RenderTexture.active = ldr;
                var image = new Texture2D(1280,720,TextureFormat.RGB24,false);
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                File.WriteAllBytes(Output + "/" + label + "-" + names[n] + ".png", image.EncodeToPNG());
                var luminance = image.GetPixels().Select(c => .2126f*c.r + .7152f*c.g + .0722f*c.b).OrderBy(v => v).ToArray();
                File.AppendAllText(Output + "/capture-metrics.txt", label + " " + names[n] + ": median=" + luminance[luminance.Length/2].ToString("F4") + ", p10=" + luminance[luminance.Length/10].ToString("F4") + ", near-black=" + (100f*luminance.Count(v => v < .02f)/luminance.Length).ToString("F2") + "%, clipped=" + (100f*luminance.Count(v => v > .98f)/luminance.Length).ToString("F2") + "% (display RGB luminance)\n");
                RenderTexture.active = previous == rt || previous == ldr ? null : previous; camera.targetTexture = null;
                Object.DestroyImmediate(image); Object.DestroyImmediate(ldr); Object.DestroyImmediate(rt);
                TCRegionAtmosphere.RestoreEnvironment();
            }
        }
        finally { TCRegionAtmosphere.RestoreEnvironment(); Object.DestroyImmediate(go); foreach (var renderer in hidden) if (renderer) renderer.enabled = true; }
    }
    public static void RunPlayMode()
    {
        ClassRoomLightingBuilder.RequireIsolated();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (behaviour is TCRegionAtmosphere || behaviour is TCFlickerLight || behaviour is TCMapMarker) continue;
            var script = MonoScript.FromMonoBehaviour(behaviour);
            if (script && AssetDatabase.GetAssetPath(script).StartsWith("Assets/")) behaviour.enabled = false;
        }
        EditorSceneManager.SaveScene(Camera.main.gameObject.scene, "Assets/ClassRoomLightingRuntimeTest.unity");
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || SessionState.GetBool(Key + ".Done", false)) return;
        try
        {
            if (!playCamera)
            {
                playCamera = Camera.main; regions = Object.FindObjectsByType<TCRegionAtmosphere>(FindObjectsSortMode.None);
                classroom = regions.First(r => r.theme == "classroom");
                roomLights = classroom.transform.parent.GetComponentsInChildren<Light>(); intensities = roomLights.Select(l => l.intensity).ToArray();
                TCRegionAtmosphere.RestoreEnvironment();
                originalFog = RenderSettings.fog; originalDensity = RenderSettings.fogDensity; originalAmbient = RenderSettings.ambientLight;
                originalBackground = playCamera.backgroundColor; originalClear = playCamera.clearFlags;
                originalPost = playCamera.GetUniversalAdditionalCameraData().renderPostProcessing;
                suns = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Directional).ToArray(); sunIntensities = suns.Select(l => l.intensity).ToArray();
                shadowData = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type != LightType.Directional && l.shadows != LightShadows.None).Select(l => l.GetUniversalAdditionalLightData()).ToArray();
                savedTiers = shadowData.Select(d => d.additionalLightsShadowResolutionTier).ToArray();
                playCamera.targetTexture = new RenderTexture(640,360,24);
            }
            if (frames < 120)
            {
                var position = frames < 40 ? new Vector3(2.1f,1.84f,-10.35f) : frames < 80 ? new Vector3(5.6f,1.84f,1.1f) : new Vector3(-4.25f,1.84f,3.6f);
                var look = frames < 40 ? new Vector3(.3f,1.5f,4.65f) : frames < 80 ? new Vector3(7.5f,.55f,3.5f) : new Vector3(2,1.2f,-7);
                playCamera.transform.position = classroom.transform.TransformPoint(position);
                playCamera.transform.LookAt(classroom.transform.TransformPoint(look)); playCamera.Render();
                Check(Mathf.Abs(RenderSettings.fogDensity - .012f) < .0001f, "Runtime fog not applied");
                var stack = VolumeManager.instance.stack;
                Check(Mathf.Abs(stack.GetComponent<ColorAdjustments>().postExposure.value) < .0001f, "Exposure drift");
                Check(stack.GetComponent<FilmGrain>().intensity.value == 0, "Temporal grain reintroduced");
                var lamp = playCamera.transform.Find("TC camera flashlight - F")?.GetComponent<Light>();
                Check(lamp && lamp.enabled && lamp.shadows == LightShadows.Soft && lamp.innerSpotAngle == 24, "Flashlight profile not selected");
                for (int i = 0; i < roomLights.Length; i++) maxDrift = Mathf.Max(maxDrift, Mathf.Abs(roomLights[i].intensity - intensities[i]));
                Check(maxDrift < .0001f, "Room lights flicker across frames");
                Check(atlasReductions == 0, "URP changed shadow resolution inside ClassRoom");
            }
            else if (frames <= 201)
            {
                // Door boundary in both directions; no time smoothing that changes with frame rate.
                float offset = (frames <= 160 ? frames - 120 : 201 - frames) * .05f;
                var p = classroom.transform.TransformPoint(new Vector3(-4.25f,1.84f,5 + offset)); playCamera.transform.position = p;
                TCRegionAtmosphere.ApplyForCamera(playCamera, regions, true); playCamera.Render();
                float w = classroom.WeightAt(p);
                float expectedDensity = w > 0 ? Mathf.Lerp(originalFog ? originalDensity : 0, .012f, w) : originalDensity;
                Check(Mathf.Abs(RenderSettings.fogDensity - expectedDensity) < .0001f, "Door transition has a discontinuity");
                for (int i = 0; i < suns.Length; i++) Check(Mathf.Abs(suns[i].intensity - sunIntensities[i]*(1-w)) < .0001f, "Sun restoration is discontinuous");
            }
            else
            {
                foreach (var region in regions.Where(r => r != classroom))
                {
                    playCamera.transform.position = region.transform.TransformPoint(region.localBounds.center);
                    Check(TCRegionAtmosphere.ApplyForCamera(playCamera, regions, true) == region, "Other map region selection regression: " + region.name);
                    Check(Mathf.Abs(RenderSettings.fogDensity - region.fogDensity) < .0001f, "Other map fog changed");
                }
                playCamera.transform.position = new Vector3(300,30,300); playCamera.Render();
                Check(RenderSettings.fog == originalFog && RenderSettings.fogDensity == originalDensity && RenderSettings.ambientLight == originalAmbient, "Environment not restored");
                Check(playCamera.clearFlags == originalClear && playCamera.backgroundColor == originalBackground && playCamera.GetUniversalAdditionalCameraData().renderPostProcessing == originalPost, "Camera not restored");
                for (int i = 0; i < suns.Length; i++) Check(suns[i].intensity == sunIntensities[i], "Sun not restored");
                for (int i = 0; i < shadowData.Length; i++) Check(shadowData[i].additionalLightsShadowResolutionTier == savedTiers[i], "Other map shadow tier not restored");
                Check(atlasReductions == 0, "Shadow atlas resized along the ClassRoom transition");
                Check(!playCamera.transform.Find("TC camera flashlight - F").GetComponent<Light>().enabled, "Flashlight remains on outside");
                Directory.CreateDirectory(Output);
                File.WriteAllText(Output + "/runtime-validation.txt", "120 rendered Play Mode frames at aisle, front and door: fixed exposure and artificial light intensity, maximum drift=" + maxDrift + ".\n82 door transition samples in both directions: fog and directional light match spatial blend; automatic shadow atlas reductions=" + atlasReductions + ".\nAll " + regions.Length + " existing regions select their own settings; leaving restores fog, ambient, sun, camera and shadow tiers.\nGameplay scripts disabled for atmosphere isolation. Combat/FPS on target hardware not tested.\n");
                var rt = playCamera.targetTexture; playCamera.targetTexture = null; Object.Destroy(rt);
                SessionState.SetBool(Key + ".Done", true); EditorApplication.isPlaying = false;
            }
            frames++;
        }
        catch (Exception error) { SessionState.SetBool(Key, false); Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
