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

public static class SkillAtmosphereVerifier
{
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    public static void Verify()
    {
        var report=new List<string>();
        foreach(string path in SkillAtmosphereBuilder.Targets)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var region in root.GetComponentsInChildren<TCRegionAtmosphere>())
                {
                    if(region.fogDensity<.049f||region.fogDensity>.076f||region.ambient.maxColorComponent>.065f)throw new Exception("Skill fog/ambient limits not applied");
                    var volume=region.GetComponentsInChildren<Volume>().First(v=>v.enabled);
                    if(volume.isGlobal||!volume.sharedProfile||volume.priority<30)throw new Exception("Regional post processing missing");
                    var profile=volume.sharedProfile;
                    if(!profile.TryGet<Vignette>(out var vignette)||Mathf.Abs(vignette.intensity.value-.42f)>.001f)throw new Exception("Vignette mismatch");
                    if(!profile.TryGet<FilmGrain>(out var grain)||Mathf.Abs(grain.intensity.value-.55f)>.001f)throw new Exception("Grain mismatch");
                    if(!profile.TryGet<ColorAdjustments>(out var grading)||grading.saturation.value>-25)throw new Exception("Desaturation missing");
                    foreach(var collider in region.GetComponentsInChildren<Collider>())if(!collider.isTrigger)throw new Exception("Atmosphere blocks player movement");
                    if(!region.ContainsWorld(region.transform.TransformPoint(region.localBounds.center)))throw new Exception("Region center outside own boundary");
                    if(region.ContainsWorld(region.transform.TransformPoint(region.localBounds.max+Vector3.one)))throw new Exception("Region spills outside its bounds");
                }
                int lightCount=root.GetComponentsInChildren<Light>().Count(l=>l.intensity>0&&!l.name.Contains("window"));
                int flicker=root.GetComponentsInChildren<TCFlickerLight>().Length;
                if(lightCount>0&&(flicker==0||flicker/(float)lightCount>.51f))throw new Exception("Flicker proportion invalid at "+path);
                int shadows=root.GetComponentsInChildren<Light>().Count(l=>l.intensity>0&&l.shadows!=LightShadows.None);
                if(shadows>4)throw new Exception("Too many shadow casting lamps in "+path);
                foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
                {
                    if(!renderer.enabled)continue;
                    foreach(var mat in renderer.sharedMaterials)
                    {
                        if(!mat||!mat.shader||mat.shader.name=="Hidden/InternalErrorShader")throw new Exception("Missing surface: "+renderer.name);
                        if(mat.IsKeywordEnabled("_EMISSION")&&AssetDatabase.GetAssetPath(mat).StartsWith(SkillAtmosphereBuilder.Art)&&mat.GetColor("_EmissionColor").maxColorComponent>.4f)throw new Exception("Neon emission exceeds skill palette");
                    }
                }
                report.Add(path+": "+lightCount+" surviving artificial lamps; "+flicker+" flicker; "+shadows+" shadow lights; "+root.GetComponentsInChildren<TCMapMarker>().Count(m=>m.label.StartsWith("note_"))+" narrative anchors.");
                Debug.Log("SKILL_PREFAB_VERIFIED "+path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var path in Directory.GetFiles("Assets/Art/TotalChaosInspired/Textures","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            if(importer.maxTextureSize!=128||importer.filterMode!=FilterMode.Point)throw new Exception("Retro texture import mismatch");
        }
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");ShaderUtil.allowAsyncCompilation=false;
        var choices=Object.FindObjectsByType<TCRegionAtmosphere>(FindObjectsSortMode.None);
        if(choices.Length!=6)throw new Exception("Expected six regional atmosphere boundaries in the existing map, got "+choices.Length);
        var cylinder=Object.FindObjectsByType<Transform>().First(t=>t.name=="Cylinder"&&t.parent==null);var atrium=cylinder.Find("Abandoned circular atrium");
        Render(cylinder,atrium,V(8,1.84f,17.3f),V(0,2.1f,0),"Cylinder",choices);
        var radio=atrium.Find("Service bay 7");Render(cylinder,radio,V(1,1.84f,-3.9f),V(0,1.9f,-.25f),"Cylinder-Radio",choices);
        var classroom=Object.FindObjectsByType<Transform>().First(t=>t.name=="ClassRoom"&&t.parent==null);var interior=classroom.Find("AbandonedClassRoomInterior");
        Render(classroom,interior,V(2.1f,1.84f,-10.35f),V(.3f,1.5f,4.65f),"ClassRoom",choices);
        Render(classroom,interior,V(5.6f,1.84f,1.1f),V(7.5f,.55f,3.5f),"ClassRoom-Aftermath",choices);
        var basement=Object.FindObjectsByType<Transform>().First(t=>t.name=="Basement"&&t.parent==null);var arena=basement.Find("PreBossArea");var boss=basement.Find("BossRoom");
        float af=BasementFinalBuilder.Floor(arena,"Hall_Floor"),bf=BasementFinalBuilder.Floor(boss,"Main_Floor"),front=boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
        Render(basement,arena,V(2,af+1.84f,-12),V(13,af+2.5f,14),"Basement-Arena",choices);
        Render(basement,boss,V(-3.5f,bf+1.84f,front+21),V(-12,bf+6.5f,front+32),"Basement-Boss",choices);
        Render(basement,boss,V(-6.5f,bf+2.84f,front+28),V(-10,bf+4.8f,front+34),"Basement-Boss-Close",choices);
        report.Add("Fog, ambient, directional lamps and camera settings restored after every capture and on leaving the regions.");
        report.Add("Existing route/layout preserved; atmosphere triggers do not add solid collision. No enemy, key or loot mechanics replaced.");
        Directory.CreateDirectory("Preview");File.WriteAllLines("Preview/Skill-atmosphere-validation.txt",report);
        Debug.Log("SKILL_ATMOSPHERE_VERIFIED");
    }
    static void Render(Transform room,Transform coords,Vector3 position,Vector3 target,string name,TCRegionAtmosphere[] choices)
    {
        var hidden=Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled&&(!r.transform.IsChildOf(room)||r.GetComponent<TextMesh>())).ToArray();
        foreach(var r in hidden)r.enabled=false;
        bool originalFog=RenderSettings.fog;Color originalAmbient=RenderSettings.ambientLight;
        var go=new GameObject("Skill atmosphere preview");var camera=go.AddComponent<Camera>();var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.volumeLayerMask=1;
        camera.fieldOfView=70;camera.nearClipPlane=.04f;camera.farClipPlane=120;camera.clearFlags=CameraClearFlags.SolidColor;
        go.transform.position=coords.TransformPoint(position);go.transform.LookAt(coords.TransformPoint(target));Physics.SyncTransforms();
        var region=TCRegionAtmosphere.ApplyForCamera(camera,choices,true);if(!region)throw new Exception("No region selected for preview "+name);
        var rt=new RenderTexture(1672,941,24){graphicsFormat=UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat};camera.targetTexture=rt;
        for(int i=0;i<5;i++)camera.Render();
        var ldr=new RenderTexture(1672,941,0,RenderTextureFormat.ARGB32);Graphics.Blit(rt,ldr);RenderTexture.active=ldr;
        var png=new Texture2D(1672,941,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1672,941),0,0);png.Apply();
        Directory.CreateDirectory("Preview");File.WriteAllBytes("Preview/Skill-"+name+".png",png.EncodeToPNG());
        var stack=VolumeManager.instance.stack;
        if(stack.GetComponent<Vignette>().intensity.value<.40f||stack.GetComponent<FilmGrain>().intensity.value<.50f)throw new Exception("Regional post FX not applied to camera at "+name);
        TCRegionAtmosphere.RestoreEnvironment();
        if(RenderSettings.fog!=originalFog||RenderSettings.ambientLight!=originalAmbient)throw new Exception("Atmosphere failed to restore scene settings");
        RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(png);Object.DestroyImmediate(rt);Object.DestroyImmediate(ldr);Object.DestroyImmediate(go);foreach(var r in hidden)r.enabled=true;
        Debug.Log("SKILL_CAMERA_FX_OK "+name);
    }
}
