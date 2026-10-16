using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object=UnityEngine.Object;

public static class EncounterCapture
{
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static void Require(bool c,string m){if(!c)throw new Exception(m);}
    public static void Render(GameObject room,Transform art,string prefix)
    {
        ShaderUtil.allowAsyncCompilation=false;
        var region=room.GetComponentInChildren<TCRegionAtmosphere>();
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=region.ambient;
        var sh=new SphericalHarmonicsL2();sh.AddAmbientLight(region.ambient);RenderSettings.ambientProbe=sh;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=region.fogDensity;RenderSettings.fogColor=region.fogColor;
        // Rendering the main map uses its real lights; TCRegionAtmosphere manages external directionals.
        var camera=new GameObject("Library review camera").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=68;camera.nearClipPlane=.04f;camera.farClipPlane=95;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=region.fogColor;camera.allowHDR=true;
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;
        var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        var views=new[]{("Obstacle",V(8.5f,1.72f,12.8f),V(-2.0f,1.5f,9)),("KeyApproach",V(7.8f,1.72f,-12.8f),V(-1.4f,1.4f,-16.5f)),("Door",V(10.1f,1.72f,24f),V(10.1f,1.72f,19.67f)),("Entrance",V(9.7f,1.72f,18.4f),V(-.5f,2.0f,-7)),("ReadingRoom",V(.7f,1.72f,8.8f),V(-2.2f,1.8f,-10)),
            ("BookAisle",V(-5.7f,1.72f,9.0f),V(-7.6f,1.9f,-5)),("Quarantine",V(.7f,1.72f,14.6f),V(3.8f,1.7f,11)),
            ("WindowTables",V(7.1f,1.72f,7.0f),V(11.5f,1.7f,-6))};
        foreach(var view in views)
        {
            camera.transform.position=art.TransformPoint(view.Item2);camera.transform.LookAt(art.TransformPoint(view.Item3));
            TCRegionAtmosphere.ApplyForCamera(camera,new[]{region},false);
            for(int i=0;i<5;i++)camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
            File.WriteAllBytes("Evidence/Encounter/"+prefix+"-"+view.Item1+".png",tex.EncodeToPNG());TCRegionAtmosphere.RestoreEnvironment();
        }
        RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);
        foreach(var shader in room.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.shader).Distinct())
            Require(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Shader compile error: "+shader.name);
    }
}
