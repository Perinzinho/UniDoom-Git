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

[InitializeOnLoad]
public static class BasementSurfacePlayVerifier
{
    const string Key="UniDoom.BasementSurfacePlay";
    [Serializable] class Target { public string name; public Vector3 position,look; public float density; }
    [Serializable] class Config { public Target[] targets; public bool fog,post; public float density; public Color ambient,background; public int clear; public float[] directionals; }
    static Config config; static Camera camera; static RenderTexture texture; static int frames;
    static readonly List<string> report=new List<string>();
    static readonly Dictionary<TCFlickerLight,Vector2> flickerSpans=new Dictionary<TCFlickerLight,Vector2>();
    static BasementSurfacePlayVerifier()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+"Ending",false))Finish();};
    }
    public static void Run()
    {
        BasementSurfaceBuilder.RequireIsolated();EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        foreach(var script in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if(script is TCRegionAtmosphere || script is TCFlickerLight || script is TCMapMarker)continue;
            var source=MonoScript.FromMonoBehaviour(script);
            if(source && AssetDatabase.GetAssetPath(source).StartsWith("Assets/"))script.enabled=false;
        }
        var room=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement" && !t.parent);
        var c=Camera.main;if(!c)throw new Exception("Main camera absent");
        foreach(var region in room.GetComponentsInChildren<TCRegionAtmosphere>())region.SyncFloorBounds();
        var targets=room.GetComponentsInChildren<TCRegionAtmosphere>().Select(r=>new Target{name=r.name,
            position=r.transform.TransformPoint(new Vector3(r.localBounds.center.x,r.localBounds.min.y+1.84f,r.localBounds.center.z)),
            look=r.transform.TransformPoint(new Vector3(r.localBounds.center.x,r.localBounds.min.y+1.84f,r.localBounds.center.z+3)),density=r.fogDensity}).ToArray();
        config=new Config{targets=targets,fog=RenderSettings.fog,density=RenderSettings.fogDensity,ambient=RenderSettings.ambientLight,
            background=c.backgroundColor,clear=(int)c.clearFlags,post=c.GetUniversalAdditionalCameraData().renderPostProcessing,
            directionals=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).OrderBy(l=>l.name).Select(l=>l.intensity).ToArray()};
        Directory.CreateDirectory("Preview/BasementSurface");File.WriteAllText("Preview/BasementSurface/runtime-config.json",JsonUtility.ToJson(config));
        EditorSceneManager.SaveScene(c.gameObject.scene,"Assets/BasementSurfaceRuntimeValidation.unity");
        SessionState.SetBool(Key+"Ending",false);SessionState.SetBool(Key+"Failed",false);SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || SessionState.GetBool(Key+"Ending",false))return;
        try
        {
            if(!camera){config=JsonUtility.FromJson<Config>(File.ReadAllText("Preview/BasementSurface/runtime-config.json"));camera=Camera.main;texture=new RenderTexture(640,360,24);camera.targetTexture=texture;}
            if(frames++<8)return;
            foreach(var flicker in Object.FindObjectsByType<TCFlickerLight>(FindObjectsSortMode.None).Where(f=>f.enabled && f.synchronizeEmission))
            {
                float intensity=flicker.GetComponent<Light>().intensity;
                if(!flickerSpans.TryGetValue(flicker,out var span))span=new Vector2(intensity,intensity);
                flickerSpans[flicker]=new Vector2(Mathf.Min(span.x,intensity),Mathf.Max(span.y,intensity));
                float basis=(float)typeof(TCFlickerLight).GetField("_base",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(flicker);
                var emitters=(Renderer[])typeof(TCFlickerLight).GetField("_emitters",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(flicker);
                foreach(var emitter in emitters)
                {
                    var material=emitter.sharedMaterial;if(!material || !material.HasProperty("_EmissionColor"))continue;
                    var properties=new MaterialPropertyBlock();emitter.GetPropertyBlock(properties);
                    var expected=material.GetColor("_EmissionColor")*(basis>0 ? intensity/basis : 0);
                    if(Vector4.Distance(properties.GetColor("_EmissionColor"),expected)>.001f)throw new Exception("Fixture emission did not follow its light");
                }
            }
            int phase=(frames-9)/30;
            if(phase<config.targets.Length)
            {
                var t=config.targets[phase];camera.transform.position=t.position;camera.transform.LookAt(t.look);camera.Render();
                if(!RenderSettings.fog || Mathf.Abs(RenderSettings.fogDensity-t.density)>.001f)throw new Exception("Wrong runtime atmosphere "+t.name);
                var lamp=camera.transform.Find("TC camera flashlight - F");
                if(!lamp || !lamp.GetComponent<Light>().enabled || lamp.GetComponent<Light>().shadows!=LightShadows.Soft)throw new Exception("Flashlight not applied");
                var stack=VolumeManager.instance.stack;
                if(stack.GetComponent<Vignette>().intensity.value>.2f || stack.GetComponent<FilmGrain>().intensity.value>.12f)throw new Exception("Incorrect regional post FX");
                if((frames-9)%30==29){report.Add(t.name+": runtime camera, fog, local volume and flashlight verified.");Debug.Log("BASEMENT_SURFACE_RUNTIME_REGION_OK "+t.name);}
            }
            else
            {
                camera.transform.position=new Vector3(300,30,300);camera.Render();
                if(RenderSettings.fog!=config.fog || Mathf.Abs(RenderSettings.fogDensity-config.density)>.001f || RenderSettings.ambientLight!=config.ambient)throw new Exception("Environment not restored");
                if(camera.backgroundColor!=config.background || (int)camera.clearFlags!=config.clear || camera.GetUniversalAdditionalCameraData().renderPostProcessing!=config.post)throw new Exception("Camera not restored");
                var directionals=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).OrderBy(l=>l.name).Select(l=>l.intensity).ToArray();
                if(!directionals.SequenceEqual(config.directionals))throw new Exception("Exterior lighting not restored");
                report.Add("Leaving Basement restores exterior environment, directional lights and original camera settings.");
                if(flickerSpans.Count>0)
                {
                    if(flickerSpans.Count!=3 || flickerSpans.Values.Any(s=>s.y-s.x<.02f))throw new Exception("Three independent arena flickers did not vary in Play Mode");
                    var observations=flickerSpans.Select(p=>p.Key.transform.parent.name+": min="+p.Value.x.ToString("F3")+" max="+p.Value.y.ToString("F3")).ToArray();
                    File.WriteAllLines("Preview/BasementSurface/arena-flicker-validation.txt",observations);
                    report.Add("Three defective arena lamps varied independently; visible emitter property blocks followed light intensity.");
                    Debug.Log("BASEMENT_ARENA_FLICKER_RUNTIME_OK");
                }
                SessionState.SetBool(Key+"Ending",true);EditorApplication.isPlaying=false;
            }
        }
        catch(Exception e){Debug.LogException(e);SessionState.SetBool(Key+"Failed",true);SessionState.SetBool(Key+"Ending",true);EditorApplication.isPlaying=false;}
    }
    static void Finish()
    {
        if(!SessionState.GetBool(Key+"Ending",false))return;
        SessionState.SetBool(Key,false);SessionState.SetBool(Key+"Ending",false);
        if(camera)camera.targetTexture=null;if(texture)Object.DestroyImmediate(texture);
        File.WriteAllLines("Preview/BasementSurface/runtime-validation.txt",report);
        bool failed=SessionState.GetBool(Key+"Failed",false);
        if(!failed)Debug.Log("BASEMENT_SURFACE_PLAYMODE_OK");EditorApplication.Exit(failed ? 1 : 0);
    }
}
