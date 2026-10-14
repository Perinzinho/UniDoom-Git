using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object = UnityEngine.Object;

public static class BasementSurfaceBuilder
{
    public const string Art = "Assets/Art/Basement/Surface";
    public static readonly string[] Targets = { "Assets/prefabs/Scenario/Basement.prefab",
        "Assets/Art/Basement/BasementFinalArena.prefab", "Assets/Art/Basement/BasementFinalSanctum.prefab" };
    static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
    public static void RequireIsolated()
    {
        if (!Application.dataPath.Replace('\\','/').Contains("/.utmp/")) throw new Exception("Use an isolated project");
    }
    public static void Build()
    {
        RequireIsolated(); Capture("Before"); ImportFloor();
        Directory.CreateDirectory(Art + "/Profiles"); AssetDatabase.Refresh();
        var arena = Profile("Arena", .15f, -15, .065f);
        var boss = Profile("Boss", .1f, -18, .08f);
        foreach (string path in Targets)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(renderer) || renderer.GetComponent<TextMesh>()) continue;
                    var mats = renderer.sharedMaterials;
                    for (int i=0; i<mats.Length; i++)
                    {
                        var source = mats[i];
                        if(source && source.IsKeywordEnabled("_EMISSION") && new[]{"SkillLamp","Amber"}.Contains(source.name))
                        {
                            bool warm=renderer.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("Amber caged") || t.name=="Abandoned restraint station");
                            string lampPath=Art+"/Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+(warm ? "_WarmLamp.mat" : "_ColdLamp.mat");
                            var lamp=AssetDatabase.LoadAssetAtPath<Material>(lampPath);
                            if(!lamp){lamp=new Material(source);AssetDatabase.CreateAsset(lamp,lampPath);}
                            var tint=warm ? new Color(.82f,.74f,.60f) : new Color(.66f,.78f,.80f);
                            lamp.SetColor("_BaseColor",tint);lamp.SetColor("_EmissionColor",tint*.9f);EditorUtility.SetDirty(lamp);mats[i]=lamp;
                            continue;
                        }
                        if (!TotalChaosMaterialConverter.Eligible(source)) continue;
                        string name = source.name;
                        TotalChaosSurfacePreset preset;
                        if (new[]{"Steel","Iron","Rust"}.Contains(name)) preset=TotalChaosSurfacePreset.Metal;
                        else if (name=="Wood" || name=="Door") preset=TotalChaosSurfacePreset.Wood;
                        else if (name=="Stone") preset=TotalChaosSurfacePreset.Stone;
                        else if (new[]{"Floor","Ceiling","Brick","Concrete","Plaster","Rubble"}.Contains(name)) preset=TotalChaosSurfacePreset.Concrete;
                        else continue;
                        bool floor=name=="Floor";
                        var converted=TotalChaosMaterialConverter.Convert(source,preset,Art,
                            floor ? Art : TotalChaosMaterialConverter.Art, floor ? "concrete_floor_02" : null);
                        converted.SetColor("_BaseColor", preset==TotalChaosSurfacePreset.Metal ? new Color(.68f,.70f,.68f) : new Color(.86f,.89f,.85f));
                        converted.SetVector("_SpecColor", new Vector4(.28f, floor ? .12f : preset==TotalChaosSurfacePreset.Metal ? .08f : .02f, .58f,1.02f));
                        converted.SetFloat("_Smoothness",preset==TotalChaosSurfacePreset.Metal ? .65f : .5f);
                        converted.SetFloat("_BumpScale",floor ? .45f : .52f);
                        converted.SetTextureScale("_BaseMap", Vector2.one * .5f);
                        EditorUtility.SetDirty(converted);
                        var filter=renderer.GetComponent<MeshFilter>();
                        if (filter && filter.sharedMesh && filter.sharedMesh.name=="Cube")
                        {
                            var size=renderer.transform.lossyScale;
                            float[] axes={Mathf.Abs(size.x),Mathf.Abs(size.y),Mathf.Abs(size.z)};
                            Array.Sort(axes);
                            var tiling=new Vector2(axes[2]*.5f,axes[1]*.5f);
                            if(floor) tiling=new Vector2(Mathf.Abs(size.x)*.5f,Mathf.Abs(size.z)*.5f);
                            string variant=Art+"/Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+"_"+preset+"_"+tiling.x.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+"x"+tiling.y.ToString("F2",System.Globalization.CultureInfo.InvariantCulture)+".mat";
                            var unique=AssetDatabase.LoadAssetAtPath<Material>(variant);
                            if(!unique){unique=new Material(converted);AssetDatabase.CreateAsset(unique,variant);}
                            unique.SetTextureScale("_BaseMap",tiling);EditorUtility.SetDirty(unique);converted=unique;
                        }
                        mats[i]=converted;
                    }
                    renderer.sharedMaterials=mats;
                }
                foreach(var region in root.GetComponentsInChildren<TCRegionAtmosphere>(true))
                {
                    bool isBoss=region.name.StartsWith("Boss"), corridor=region.name.StartsWith("Corridor");
                    region.theme=isBoss ? "basement-sanctum" : corridor ? "basement-service" : "basement-arena";
                    region.ambient=isBoss ? new Color(.16f,.175f,.18f) : new Color(.18f,.20f,.21f);
                    region.fogColor=new Color(.115f,.13f,.135f);
                    region.fogDensity=corridor ? .009f : isBoss ? .018f : .014f;
                    region.blendDistance=1.5f;region.stabilizeBackgroundShadows=true;
                    region.flashlightIntensity=4;region.flashlightRange=15;region.flashlightAngle=54;
                    region.flashlightInnerAngle=24;region.flashlightShadows=LightShadows.Soft;
                    var volume=region.GetComponent<Volume>();volume.sharedProfile=isBoss ? boss : arena;
                    volume.blendDistance=region.blendDistance;
                }
                foreach(var flicker in root.GetComponentsInChildren<TCFlickerLight>(true))
                { if(PrefabUtility.IsPartOfPrefabInstance(flicker))continue; flicker.enabled=false; }
                foreach(var light in root.GetComponentsInChildren<Light>(true))
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(light))continue;
                    light.shadows=LightShadows.None;
                    if(light.intensity<=0)continue;
                    string parent=light.transform.parent.name;
                    light.color=new Color(.76f,.81f,.78f);
                    if(parent.StartsWith("Hanging industrial fixture"))
                    {
                        light.type=LightType.Spot;light.transform.localRotation=Quaternion.Euler(90,0,0);
                        light.spotAngle=100;light.innerSpotAngle=65;light.range=17;light.intensity=16;
                        light.color=new Color(.66f,.78f,.80f);
                        if(parent.EndsWith(" 0"))light.shadows=LightShadows.Soft;
                    }
                    else if(parent.StartsWith("Amber caged"))
                    {light.intensity=3.8f;light.range=10;light.color=new Color(.82f,.74f,.60f);}
                    else if(light.name=="Emergency spill")
                    {
                        light.intensity=2.8f;light.range=7;light.color=new Color(.72f,.80f,.75f);
                        if(parent.Contains("Clone") && light.transform.parent.parent.name=="Passage Ceiling Z")
                        {
                            // One stable pool guides the bend; other corridor sources stay unshadowed.
                            light.type=LightType.Spot;light.transform.localRotation=Quaternion.Euler(90,0,0);
                            light.spotAngle=115;light.innerSpotAngle=75;
                        }
                    }
                    else if(parent.StartsWith("Red caged"))
                    {light.intensity=2.5f;light.range=9;light.color=new Color(.66f,.73f,.72f);}
                    else if(parent=="Abandoned restraint station")
                    {light.intensity=5;light.range=8;light.color=new Color(.92f,.76f,.55f);light.shadows=LightShadows.Soft;}
                    else if(parent=="Six shrouded bodies suspended above the altar")
                    {
                        light.intensity=18;light.range=18;light.type=LightType.Spot;
                        light.transform.localRotation=Quaternion.Euler(90,0,0);light.spotAngle=100;light.innerSpotAngle=58;
                        light.color=new Color(.67f,.77f,.79f);light.shadows=LightShadows.Soft;
                    }
                    else if(parent=="Bone reliquaries and towering ritual machinery")
                    {light.intensity=3.8f;light.range=10;light.color=new Color(.75f,.69f,.59f);}
                    else if(parent.Contains("Infernal") || parent.Contains("Colossal"))
                    {light.intensity=2.2f;light.range=7;light.color=new Color(.64f,.27f,.22f);}
                    else {light.intensity=2.5f;light.range=Mathf.Max(light.range,6);}
                    light.shadowBias=.035f;light.shadowNormalBias=.12f;
                    var data=light.GetUniversalAdditionalLightData();data.usePipelineSettings=false;
                    var serialized=new SerializedObject(data);
                    serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=1;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    if(parent=="Hanging industrial fixture 10")
                    {
                        var flicker=light.GetComponent<TCFlickerLight>();
                        if(flicker){flicker.enabled=true;flicker.flickerAmount=.10f;flicker.speed=1.3f;flicker.dropoutChancePerSecond=0;}
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Verify();Capture("After");Debug.Log("BASEMENT_SURFACE_BUILD_OK");
    }
    static VolumeProfile Profile(string name,float exposure,float saturation,float grain)
    {
        string path=Art+"/Profiles/"+name+".asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(profile)return profile;
        profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);
        var grading=profile.Add<ColorAdjustments>(true);grading.postExposure.Override(exposure);grading.contrast.Override(7);grading.saturation.Override(saturation);
        var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.Neutral);
        var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.14f);vignette.smoothness.Override(.65f);
        var film=profile.Add<FilmGrain>(true);film.type.Override(FilmGrainLookup.Thin1);film.intensity.Override(grain);film.response.Override(.85f);
        var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.07f);bloom.threshold.Override(1.2f);
        var aberration=profile.Add<ChromaticAberration>(true);aberration.intensity.Override(0);
        foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
        return profile;
    }
    static void ImportFloor()
    {
        AssetDatabase.Refresh();string dir=Art+"/Textures/concrete_floor_02/";
        foreach(string role in new[]{"Diffuse","nor_gl","arm","Displacement"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(dir+role+".png");
            importer.textureType=role=="nor_gl" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture=role=="Diffuse";importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            importer.anisoLevel=4;importer.maxTextureSize=2048;importer.isReadable=role=="arm";
            importer.textureCompression=role=="arm" ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        var arm=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"arm.png");var pixels=arm.GetPixels32();
        for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new Color32(p.b,p.r,p.g,(byte)(255-p.g));}
        var mask=new Texture2D(arm.width,arm.height,TextureFormat.RGBA32,false,true);
        mask.SetPixels32(pixels);mask.Apply();File.WriteAllBytes(dir+"UnityMask.png",mask.EncodeToPNG());Object.DestroyImmediate(mask);
        AssetDatabase.ImportAsset(dir+"UnityMask.png",ImportAssetOptions.ForceUpdate);
        var packed=(TextureImporter)AssetImporter.GetAtPath(dir+"UnityMask.png");
        packed.sRGBTexture=false;packed.mipmapEnabled=true;packed.filterMode=FilterMode.Trilinear;packed.anisoLevel=4;
        packed.maxTextureSize=2048;packed.textureCompression=TextureImporterCompression.CompressedHQ;packed.SaveAndReimport();
        var source=(TextureImporter)AssetImporter.GetAtPath(dir+"arm.png");source.isReadable=false;
        source.textureCompression=TextureImporterCompression.CompressedHQ;source.SaveAndReimport();
    }
    public static void Verify()
    {
        RequireIsolated();var shader=Shader.Find("UniDoom/TotalChaosInspiredSurface");
        if(!shader || ShaderUtil.ShaderHasError(shader))throw new Exception("Shader compilation failed");
        var report=new List<string>();
        var root=PrefabUtility.LoadPrefabContents(Targets[0]);
        try
        {
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))
                foreach(var mat in renderer.sharedMaterials)
                {
                    if(!mat || !mat.shader || mat.shader.name=="Hidden/InternalErrorShader")throw new Exception("Missing material "+renderer.name);
                    if(mat.shader!=shader)continue;
                    if(!mat.GetTexture("_BumpMap") || !mat.GetTexture("_MetallicGlossMap") || !mat.GetTexture("_OcclusionMap"))throw new Exception("Missing PBR maps");
                    if(mat.IsKeywordEnabled("_PARALLAXMAP"))throw new Exception("Unbudgeted parallax");
                }
            int converted=root.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.sharedMaterials.Any(m=>m && m.shader==shader));
            int shadows=root.GetComponentsInChildren<Light>(true).Count(l=>l.enabled && l.intensity>0 && l.shadows!=LightShadows.None);
            int flickers=root.GetComponentsInChildren<TCFlickerLight>(true).Count(f=>f.enabled);
            bool dressedArena=root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Arena horror dressing");
            if(converted<50 || shadows>4 || flickers>(dressedArena ? 3 : 1))throw new Exception("Surface/light budget mismatch");
            foreach(var region in root.GetComponentsInChildren<TCRegionAtmosphere>())
            {
                if(region.fogDensity>.02f || region.blendDistance<1 || !region.stabilizeBackgroundShadows)throw new Exception("Region calibration missing");
                var volume=region.GetComponent<Volume>();if(volume.isGlobal || !volume.sharedProfile)throw new Exception("Regional volume missing");
            }
            int reversible=root.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.sharedMaterials.Any(m=>m && AssetDatabase.GetAssetPath(m).StartsWith(Art+"/Materials/")));
            if(TotalChaosMaterialConverter.Revert(root)!=reversible)throw new Exception("Reversal did not restore all PBR references");
            report.Add("PBR renderers="+converted+"; shadowed fixtures="+shadows+" plus camera flashlight; flicker="+flickers);
            report.Add("All normal/AO/metal-smoothness references present; optional parallax disabled; material reversal verified.");
            Debug.Log("BASEMENT_SURFACE_REVERSAL_OK renderers="+converted);
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
        Directory.CreateDirectory("Preview/BasementSurface");File.WriteAllLines("Preview/BasementSurface/validation.txt",report);
        Debug.Log("BASEMENT_SURFACE_VERIFIED");
    }
    public static void Capture(string stage)
    {
        RequireIsolated();EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");ShaderUtil.allowAsyncCompilation=false;
        var room=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement" && !t.parent);
        var arena=room.Find("PreBossArea");var boss=room.Find("BossRoom");
        float af=BasementFinalBuilder.Floor(arena,"Hall_Floor"),bf=BasementFinalBuilder.Floor(boss,"Main_Floor"),front=boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
        Render(room,arena,V(2,af+1.84f,-12),V(13,af+2.5f,14),stage+"-Arena",false);
        Render(room,arena,V(26,af+1.84f,-6.2f),V(28,af+2.6f,-4),stage+"-Pipes",true);
        Render(room,boss,V(-3.5f,bf+1.84f,front+21),V(-12,bf+6.5f,front+32),stage+"-Boss",false);
        Render(room,boss,V(-8,bf+1.84f,front+30),V(-10,bf+4,front+34),stage+"-Flashlight",true);
        var floor=boss.Find("Player sized corridor/Passage Floor X").GetComponent<BoxCollider>();
        Physics.SyncTransforms();
        var worldEye=floor.bounds.center;worldEye.y=floor.bounds.max.y+1.84f;
        var eye=boss.InverseTransformPoint(worldEye);
        var region=room.GetComponentsInChildren<TCRegionAtmosphere>().First(r=>r.name.Contains("Passage Floor X"));
        File.WriteAllText("Preview/BasementSurface/corridor-camera.txt","floor camera="+worldEye+"; region center="+region.transform.TransformPoint(region.localBounds.center)+
            "; floor bounds="+floor.bounds+"; floor renderer="+floor.GetComponent<MeshRenderer>().enabled+"\n");
        Render(room,boss,eye,eye+Vector3.left*3,stage+"-Corridor",false);
        foreach(var message in ShaderUtil.GetShaderMessages(Shader.Find("UniDoom/TotalChaosInspiredSurface")))
            if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(message.message);
        Debug.Log("BASEMENT_SURFACE_CAPTURE_OK "+stage);
    }
    static void Render(Transform room,Transform coordinates,Vector3 position,Vector3 look,string name,bool flashlight)
    {
        var hidden=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled && !r.transform.IsChildOf(room)).ToArray();
        foreach(var renderer in hidden)renderer.enabled=false;
        var go=new GameObject("Basement PBR camera");var camera=go.AddComponent<Camera>();camera.enabled=false;
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=1;
        camera.fieldOfView=70;camera.nearClipPlane=.04f;camera.farClipPlane=120;
        go.transform.position=coordinates.TransformPoint(position);go.transform.LookAt(coordinates.TransformPoint(look));Physics.SyncTransforms();
        var rt=new RenderTexture(1672,941,24,RenderTextureFormat.ARGBHalf);
        var ldr=new RenderTexture(1672,941,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
        var png=new Texture2D(1672,941,TextureFormat.RGB24,false);
        bool originalFog=RenderSettings.fog;Color originalAmbient=RenderSettings.ambientLight;
        try
        {
            if(!TCRegionAtmosphere.ApplyForCamera(camera,room.GetComponentsInChildren<TCRegionAtmosphere>(),flashlight))throw new Exception("Camera outside atmosphere "+name);
            camera.targetTexture=rt;for(int i=0;i<5;i++)camera.Render();Graphics.Blit(rt,ldr);RenderTexture.active=ldr;
            png.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);png.Apply();
            Directory.CreateDirectory("Preview/BasementSurface");File.WriteAllBytes("Preview/BasementSurface/"+name+".png",png.EncodeToPNG());
            var pixels=png.GetPixels();float mean=pixels.Average(p=>p.grayscale);float black=pixels.Count(p=>p.grayscale<.025f)/(float)pixels.Length;
            File.AppendAllText("Preview/BasementSurface/capture-metrics.txt",name+": mean="+mean.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+" black="+black.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)+"\n");
            if(name.StartsWith("Final") && (mean<.045f || black>.7f))throw new Exception("Unreadable capture "+name);
        }
        finally
        {
            TCRegionAtmosphere.RestoreEnvironment();
            if(RenderSettings.fog!=originalFog || RenderSettings.ambientLight!=originalAmbient)throw new Exception("Environment restoration failed");
            camera.targetTexture=null;RenderTexture.active=previous==rt || previous==ldr ? null : previous;
            Object.DestroyImmediate(png);Object.DestroyImmediate(rt);Object.DestroyImmediate(ldr);Object.DestroyImmediate(go);
            foreach(var renderer in hidden)if(renderer)renderer.enabled=true;
        }
    }
    public static void FinalCheck()
    {
        Verify();Capture("Final");
        var shader=Shader.Find("UniDoom/TotalChaosInspiredSurface");
        var batcher=typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static,
            null,new[]{typeof(Shader),typeof(int)},null);
        if(batcher==null || (int)batcher.Invoke(null,new object[]{shader,0})!=0)throw new Exception("SRP Batcher compatibility failed");
        Debug.Log("BASEMENT_SURFACE_SRP_BATCHER_OK");
        // Reuse the existing actual CharacterController, gate and map inheritance checks without their old visual settings.
        var type=typeof(BasementFinalVerifier);var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
        var map=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement" && !t.parent).gameObject;
        foreach(string check in new[]{"CheckNeighbourRooms","VerifyDoors"})type.GetMethod(check,flags).Invoke(null,new object[]{map});
        type.GetMethod("Check",flags).Invoke(null,new object[]{map,"MAP"});
        type.GetMethod("Traverse",flags).Invoke(null,new object[]{map,"MAP"});
        var probes=map.GetComponentsInChildren<ReflectionProbe>();
        if(probes.Length!=2 || probes.Any(p=>p.mode!=ReflectionProbeMode.Custom || !p.customBakedTexture))throw new Exception("Static reflection bindings missing");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Targets[0]));instance.transform.position=Vector3.zero;
        type.GetMethod("Check",flags).Invoke(null,new object[]{instance,"PREFAB"});
        type.GetMethod("Traverse",flags).Invoke(null,new object[]{instance,"PREFAB"});
        typeof(BasementBuilder).GetMethod("TestEncounter",flags).Invoke(null,new object[]{instance});
        Debug.Log("BASEMENT_SURFACE_GAMEPLAY_OK");Debug.Log("BASEMENT_SURFACE_FINAL_OK");
    }
    public static void Polish()
    {
        RequireIsolated();
        foreach(string path in Directory.GetFiles(Art+"/Materials","*.mat"))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path.Replace('\\','/'));
            if(material.shader.name!="UniDoom/TotalChaosInspiredSurface")continue;
            if(material.name.StartsWith("Rust"))material.SetColor("_BaseColor",new Color(.5f,.45f,.39f));
            else if(material.name.StartsWith("Iron"))material.SetColor("_BaseColor",new Color(.34f,.38f,.37f));
            else if(material.name.StartsWith("Steel"))material.SetColor("_BaseColor",new Color(.49f,.53f,.51f));
            EditorUtility.SetDirty(material);
        }
        foreach(string path in Targets)
        {
            var local=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var region in local.GetComponentsInChildren<TCRegionAtmosphere>())
                    region.ambient=region.theme=="basement-sanctum" ? new Color(.115f,.13f,.14f) : new Color(.135f,.15f,.16f);
                foreach(var renderer in local.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(renderer))continue;
                    var mats=renderer.sharedMaterials;
                    for(int i=0;i<mats.Length;i++)
                    {
                        var source=mats[i];if(!source || source.name!="Amber")continue;
                        bool cold=renderer.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("Hanging industrial fixture"));
                        if(!cold)continue;
                        string lampPath=Art+"/Materials/"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+"_ColdLamp.mat";
                        var lamp=AssetDatabase.LoadAssetAtPath<Material>(lampPath);
                        if(!lamp){lamp=new Material(source);AssetDatabase.CreateAsset(lamp,lampPath);}
                        lamp.SetTexture("_BaseMap",null);lamp.SetColor("_BaseColor",new Color(.70f,.84f,.86f));EditorUtility.SetDirty(lamp);mats[i]=lamp;
                    }
                    renderer.sharedMaterials=mats;
                }
                PrefabUtility.SaveAsPrefabAsset(local,path);
            }
            finally {PrefabUtility.UnloadPrefabContents(local);}
        }
        var root=PrefabUtility.LoadPrefabContents(Targets[0]);
        try
        {
            foreach(string name in new[]{"PreBossArea","BossRoom"})
            {
                var area=root.transform.Find(name);bool first=name=="PreBossArea";
                float floor=BasementFinalBuilder.Floor(area,first ? "Hall_Floor" : "Main_Floor");
                float front=first ? -15 : area.Find("Walls/Entrance_Wall_Header").localPosition.z;
                var t=area.Find("Basement static reflection");
                if(!t){var go=new GameObject("Basement static reflection");go.transform.SetParent(area,false);t=go.transform;}
                t.localPosition=V(first ? 10 : -10,floor+1.84f,front+(first ? 17 : 18));
                var probe=t.GetComponent<ReflectionProbe>();if(!probe)probe=t.gameObject.AddComponent<ReflectionProbe>();
                probe.mode=ReflectionProbeMode.Custom;probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
                probe.size=V(first ? 40 : 42,first ? 9.5f : 12,first ? 34 : 36);
                probe.center=V(0,probe.size.y*.5f-1.84f,0);probe.boxProjection=true;probe.intensity=.3f;
                probe.blendDistance=1;probe.resolution=128;probe.farClipPlane=55;
                probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=new Color(.115f,.13f,.135f);
            }
            PrefabUtility.SaveAsPrefabAsset(root,Targets[0]);
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");ShaderUtil.allowAsyncCompilation=false;
        var room=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement" && !t.parent);
        foreach(var probe in room.GetComponentsInChildren<ReflectionProbe>())
        {
            string name=probe.transform.parent.name;
            var go=new GameObject("Basement cubemap capture");var camera=go.AddComponent<Camera>();camera.enabled=false;
            camera.transform.position=probe.transform.position;camera.nearClipPlane=.1f;camera.farClipPlane=55;
            var cubemap=new Cubemap(128,TextureFormat.RGBAHalf,true);
            try
            {
                TCRegionAtmosphere.ApplyForCamera(camera,room.GetComponentsInChildren<TCRegionAtmosphere>(),false);
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                if(!camera.RenderToCubemap(cubemap))throw new Exception("Basement reflection capture failed");
                string path=Art+"/"+name+"Reflection.asset";var asset=AssetDatabase.LoadAssetAtPath<Cubemap>(path);
                if(asset){EditorUtility.CopySerialized(cubemap,asset);Object.DestroyImmediate(cubemap);}else{AssetDatabase.CreateAsset(cubemap,path);asset=cubemap;}
                probe.customBakedTexture=asset;
                root=PrefabUtility.LoadPrefabContents(Targets[0]);
                try{root.transform.Find(name+"/Basement static reflection").GetComponent<ReflectionProbe>().customBakedTexture=asset;PrefabUtility.SaveAsPrefabAsset(root,Targets[0]);}
                finally {PrefabUtility.UnloadPrefabContents(root);}
            }
            finally{TCRegionAtmosphere.RestoreEnvironment();Object.DestroyImmediate(go);}
        }
        AssetDatabase.SaveAssets();Verify();Capture("Polished");Debug.Log("BASEMENT_SURFACE_REFLECTIONS_OK");
    }
    public static void RepairCorridorRegions()
    {
        RequireIsolated();var root=PrefabUtility.LoadPrefabContents(Targets[0]);
        try
        {
            var boss=root.transform.Find("BossRoom");
            foreach(var region in root.GetComponentsInChildren<TCRegionAtmosphere>().Where(r=>r.name.StartsWith("Corridor")))
            {
                string name=region.name.Contains("Floor X") ? "Passage Floor X" : "Passage Floor Z";
                var floor=boss.Find("Player sized corridor/"+name).GetComponent<BoxCollider>();
                region.floorReference=floor;region.floorClearHeight=5.2f;region.SyncFloorBounds();
                Debug.Log("BASEMENT_CORRIDOR_VOLUME_ALIGNED "+name+" "+region.localBounds);
            }
            PrefabUtility.SaveAsPrefabAsset(root,Targets[0]);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }
}
