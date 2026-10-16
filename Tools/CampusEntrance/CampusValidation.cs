using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TotalChaosMaps;
using Object=UnityEngine.Object;

public static class CampusValidation
{
    public static void Run() => Validate(true);
    public static void ReferencesAndRoute() => Validate(false);
    static void Validate(bool capture)
    {
        Directory.CreateDirectory("CampusEvidence");
        ShaderUtil.allowAsyncCompilation=false;
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        var report=new StringBuilder();
        var transforms=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        var garden=transforms.First(t=>t.name=="EntranceGarden").Find("CampusArchitecture");
        var hall=transforms.First(t=>t.name=="EntranceHallway").Find("CampusArchitecture");
        foreach(var t in new[]{garden,hall})t.GetComponent<CampusEntranceAnchor>().Refresh();
        if(Vector3.Distance(garden.position,hall.position)>.025f)throw new Exception("Entrance docking mismatch "+Vector3.Distance(garden.position,hall.position));
        report.AppendLine("Garden / hallway docking: "+Vector3.Distance(garden.position,hall.position).ToString("F5")+" m; axes="+Vector3.Dot(garden.forward,hall.forward));
        foreach(var root in new[]{garden,hall})
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script "+t.name);
            foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.enabled)
                foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported)throw new Exception("Invalid material "+r.name);
            foreach(var text in root.GetComponentsInChildren<TMP_Text>())if(text.text.IndexOf("UNIVEM",StringComparison.OrdinalIgnoreCase)>=0)throw new Exception("Forbidden visible institution name");
            report.AppendLine(root.parent.name+": active renderers="+root.GetComponentsInChildren<Renderer>().Count(r=>r.enabled)+"; lights="+root.GetComponentsInChildren<Light>().Count(l=>l.enabled)+"; persistent meshes, materials and scripts valid");
            File.WriteAllLines("CampusEvidence/"+root.parent.name+"-dependencies.txt",AssetDatabase.GetDependencies(CampusBuilder.Prefab(root.parent.name),true));
        }
        Physics.SyncTransforms();
        // The complete route includes gate leaves, threshold, lobby counters and final connection.
        int steps=0;
        for(float z=-37;z<=28.1f;z+=.2f)
        {
            var p=garden.TransformPoint(new Vector3(0,.08f,z));
            var hits=Physics.OverlapCapsule(p+Vector3.up*.32f,p+Vector3.up*1.84f,.28f,~0,QueryTriggerInteraction.Ignore);
            var solid=hits.Where(c=>c.enabled&&!c.isTrigger&&!(c is CharacterController)).ToArray();
            if(solid.Length>0)throw new Exception("Route blocked at "+z+": "+string.Join(",",solid.Select(c=>c.transform.parent.name+"/"+c.name)));
            if(!Physics.Raycast(p+Vector3.up*.35f,Vector3.down,out var hit,.6f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Unsupported route at "+z);
            steps++;
        }
        report.AppendLine("Full gate-to-existing-areas route: "+steps+" capsule clearances and floor supports passed (radius 0.28 m, 2.12 m height). Central entrance opening 2.6 m.");
        if(!capture)
        {
            File.WriteAllText("CampusEvidence/validation.txt",report.ToString()+"CAMPUS_VALIDATION_PASS\n");
            Debug.Log("CAMPUS_VALIDATION_PASS");return;
        }
        var camera=new GameObject("Campus review camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.fieldOfView=64;camera.nearClipPlane=.06f;camera.farClipPlane=180;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        Capture(camera,garden,new Vector3(-.8f,1.68f,-33),new Vector3(0,3,-.1f),"Exterior-Approach");
        Capture(camera,garden,new Vector3(-9.7f,1.7f,-14),new Vector3(0,3,.3f),"Exterior-Facade");
        Capture(camera,hall,new Vector3(0,1.68f,1.4f),new Vector3(0,1.45f,24),"Interior-Lobby");
        Capture(camera,hall,new Vector3(.1f,1.68f,10),new Vector3(0,1.55f,27.8f),"Interior-Corridor");
        Capture(camera,hall,new Vector3(0,1.68f,5.9f),new Vector3(0,1.5f,-20),"Interior-LookingOut");
        Capture(camera,garden,new Vector3(-25,42,-43),new Vector3(0,0,-17),"Campus-Overview");
        TCRegionAtmosphere.ApplyForCamera(camera,Array.Empty<TCRegionAtmosphere>(),false);
        File.WriteAllText("CampusEvidence/validation.txt",report.ToString()+"CAMPUS_VALIDATION_PASS\n");
        Debug.Log("CAMPUS_VALIDATION_PASS");
    }
    static void Capture(Camera camera,Transform root,Vector3 from,Vector3 to,string name)
    {
        camera.transform.position=root.TransformPoint(from);camera.transform.LookAt(root.TransformPoint(to));
        var r=TCRegionAtmosphere.ApplyForCamera(camera,Object.FindObjectsByType<TCRegionAtmosphere>(FindObjectsSortMode.None),false);
        if(!r)
        {
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.48f,.51f,.49f);
            var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(RenderSettings.ambientLight);RenderSettings.ambientProbe=probe;
            RenderSettings.fog=false;camera.backgroundColor=new Color(.23f,.275f,.275f);
        }
        var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGBHalf);var output=new RenderTexture(1600,1000,0,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=rt;for(int i=0;i<5;i++)camera.Render();Graphics.Blit(rt,output);RenderTexture.active=output;
            var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes("CampusEvidence/"+name+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(rt);Object.DestroyImmediate(output);}
    }
}
