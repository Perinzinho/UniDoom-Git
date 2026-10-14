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
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class SkillPlayModeVerifier
{
    const string Key="UniDoom.SkillAtmosphere.PlayValidation";
    [Serializable] class Target {public string name;public Vector3 position,look;public float density;}
    [Serializable] class Config {public Target[] targets;public bool fog;public float density;public Color ambient,background;public int clear;public bool post;public float[] directionalIntensities;}
    static Config config;
    static int frames;
    static bool ending;
    static Camera camera;
    static RenderTexture target;
    static readonly Dictionary<Light,Vector2> observed=new Dictionary<Light,Vector2>();
    static readonly List<string> report=new List<string>();
    static SkillPlayModeVerifier()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key+".Ending",false))Finish();};
    }
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/"))throw new Exception("Run in isolated project only");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        foreach(var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if(behaviour is TCRegionAtmosphere||behaviour is TCFlickerLight||behaviour is TCMapMarker)continue;
            var script=MonoScript.FromMonoBehaviour(behaviour);
            if(script&&AssetDatabase.GetAssetPath(script).StartsWith("Assets/"))behaviour.enabled=false;
        }
        var c=Camera.main;if(!c)throw new Exception("Main camera missing");
        var classroom=Object.FindObjectsByType<Transform>().First(t=>t.name=="ClassRoom"&&t.parent==null).Find("AbandonedClassRoomInterior");
        var atrium=Object.FindObjectsByType<Transform>().First(t=>t.name=="Cylinder"&&t.parent==null).Find("Abandoned circular atrium");
        var basement=Object.FindObjectsByType<Transform>().First(t=>t.name=="Basement"&&t.parent==null);var arena=basement.Find("PreBossArea");var boss=basement.Find("BossRoom");
        float af=BasementFinalBuilder.Floor(arena,"Hall_Floor"),bf=BasementFinalBuilder.Floor(boss,"Main_Floor"),front=boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
        config=new Config{targets=new[]{
            new Target{name="ClassRoom",position=classroom.TransformPoint(V(2.1f,1.84f,-10.35f)),look=classroom.TransformPoint(V(.3f,1.5f,4.65f)),density=.05f},
            new Target{name="Cylinder",position=atrium.TransformPoint(V(8,1.84f,17.3f)),look=atrium.TransformPoint(V(0,2.1f,0)),density=.05f},
            new Target{name="Arena",position=arena.TransformPoint(V(2,af+1.84f,-12)),look=arena.TransformPoint(V(13,af+2.5f,14)),density=.06f},
            new Target{name="Boss",position=boss.TransformPoint(V(-8,bf+1.84f,front+30)),look=boss.TransformPoint(V(-10,bf+4,front+34)),density=.06f}},
            fog=RenderSettings.fog,density=RenderSettings.fogDensity,ambient=RenderSettings.ambientLight,background=c.backgroundColor,
            clear=(int)c.clearFlags,post=c.GetUniversalAdditionalCameraData().renderPostProcessing,
            directionalIntensities=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).OrderBy(l=>l.name).Select(l=>l.intensity).ToArray()};
        Directory.CreateDirectory("Preview");File.WriteAllText("Preview/Skill-play-config.json",JsonUtility.ToJson(config));
        EditorSceneManager.SaveScene(c.gameObject.scene,"Assets/Art/SkillAtmosphere/SkillRuntimeValidation.unity");
        SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(SessionState.GetBool(Key+".Ending",false)&&!EditorApplication.isPlayingOrWillChangePlaymode){Finish();return;}
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||ending)return;
        try
        {
            if(!camera){config=JsonUtility.FromJson<Config>(File.ReadAllText("Preview/Skill-play-config.json"));camera=Camera.main;target=new RenderTexture(640,360,24);camera.targetTexture=target;}
            if(frames<8){frames++;return;}
            int phase=(frames-8)/30;
            if(phase<config.targets.Length)
            {
                var current=config.targets[phase];camera.transform.position=current.position;camera.transform.LookAt(current.look);camera.Render();
                if(!RenderSettings.fog||RenderSettings.fogMode!=FogMode.ExponentialSquared||Mathf.Abs(RenderSettings.fogDensity-current.density)>.001f)throw new Exception("Runtime fog did not select "+current.name);
                var flashlight=camera.transform.Find("TC camera flashlight - F");
                if(!flashlight||!flashlight.GetComponent<Light>().enabled||flashlight.GetComponent<Light>().type!=LightType.Spot)throw new Exception("Runtime flashlight missing");
                var stack=VolumeManager.instance.stack;
                if(stack.GetComponent<Vignette>().intensity.value<.40f||stack.GetComponent<FilmGrain>().intensity.value<.50f)throw new Exception("Runtime profile missing at "+current.name);
                foreach(var flicker in Object.FindObjectsByType<TCFlickerLight>(FindObjectsSortMode.None))
                {
                    var light=flicker.GetComponent<Light>();if(!observed.TryGetValue(light,out var span))span=new Vector2(light.intensity,light.intensity);
                    observed[light]=new Vector2(Mathf.Min(span.x,light.intensity),Mathf.Max(span.y,light.intensity));
                }
                if((frames-8)%30==29){report.Add(current.name+": real camera render selected fog, local post FX and flashlight in Play Mode.");Debug.Log("SKILL_PLAYMODE_REGION_OK "+current.name);}
            }
            else
            {
                camera.transform.position=V(300,30,300);camera.Render();
                if(RenderSettings.fog!=config.fog||Mathf.Abs(RenderSettings.fogDensity-config.density)>.001f||RenderSettings.ambientLight!=config.ambient)throw new Exception("Runtime environment not restored outside rooms");
                if(camera.backgroundColor!=config.background||(int)camera.clearFlags!=config.clear||camera.GetUniversalAdditionalCameraData().renderPostProcessing!=config.post)throw new Exception("Original camera settings not restored");
                var current=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).OrderBy(l=>l.name).Select(l=>l.intensity).ToArray();
                if(!current.SequenceEqual(config.directionalIntensities))throw new Exception("Outdoor directional lights not restored");
                if(camera.transform.Find("TC camera flashlight - F").GetComponent<Light>().enabled)throw new Exception("Region flashlight remains on outside rooms");
                int animated=observed.Count(p=>p.Value.y-p.Value.x>.02f);
                if(animated<10)throw new Exception("Runtime flicker did not animate lamps, varied="+animated);
                report.Add(animated+" defective lamps varied intensity during live frames.");report.Add("Leaving the six room/corridor regions restored saved fog, ambient, sun and camera settings. Flashlight turns off outside.");
                Debug.Log("SKILL_PLAYMODE_FLICKER_OK "+animated);Debug.Log("SKILL_PLAYMODE_EXIT_RESTORES_ENVIRONMENT");
                File.WriteAllLines("Preview/Skill-playmode-validation.txt",report);
                camera.targetTexture=null;Object.Destroy(target);ending=true;SessionState.SetBool(Key+".Ending",true);EditorApplication.isPlaying=false;
            }
            frames++;
        }
        catch(Exception error)
        {
            SessionState.SetBool(Key,false);Debug.LogException(error);EditorApplication.Exit(1);
        }
    }
    static void Finish()
    {
        SessionState.SetBool(Key,false);SessionState.SetBool(Key+".Ending",false);
        File.WriteAllText("Preview/Skill-playmode-exit-code.txt","0");
        Debug.Log("SKILL_PLAYMODE_VERIFIED");EditorApplication.Exit(0);
    }
}
