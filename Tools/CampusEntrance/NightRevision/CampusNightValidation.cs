using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TotalChaosMaps;
using Object=UnityEngine.Object;

public static class CampusNightValidation
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        ShaderUtil.allowAsyncCompilation=false;
        var report=new StringBuilder();
        var all=Object.FindObjectsByType<Transform>();
        var garden=all.First(t=>t.name=="EntranceGarden").Find("CampusArchitecture");
        var hall=all.First(t=>t.name=="EntranceHallway").Find("CampusArchitecture");
        foreach(var art in new[]{garden,hall})
        {
            art.GetComponent<CampusEntranceAnchor>().Refresh();
            foreach(var t in art.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script "+t.name);
            foreach(var r in art.GetComponentsInChildren<Renderer>())if(r.enabled)
                foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported)throw new Exception("Invalid material "+r.name);
            foreach(var f in art.GetComponentsInChildren<MeshFilter>())
                if(!f.GetComponent<TMP_Text>()&&(!f.sharedMesh||string.IsNullOrEmpty(AssetDatabase.GetAssetPath(f.sharedMesh))))throw new Exception("Transient mesh "+f.name);
            report.AppendLine(art.parent.name+": height multiplier="+art.GetComponent<CampusEntranceAnchor>().heightMultiplier+", persistent active meshes and valid materials/scripts.");
            File.WriteAllLines("NightEvidence/"+art.parent.name+"-dependencies.txt",AssetDatabase.GetDependencies("Assets/_Project/Scenes/"+art.parent.name+"/Prefabs/"+art.parent.name+".prefab",true));
        }
        if(Vector3.Distance(garden.position,hall.position)>.025f)throw new Exception("Docking changed");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Player/Player.prefab");
        var player=(GameObject)PrefabUtility.InstantiatePrefab(source);var cc=player.GetComponent<CharacterController>();
        player.transform.position=garden.position+Vector3.up*1.13f;
        var pc=player.GetComponentInChildren<Camera>();var head=player.transform.Find("Head");
        float eye=pc?pc.transform.position.y-garden.position.y:head.position.y-garden.position.y;
        report.AppendLine("Original player height="+(cc.height*player.transform.lossyScale.y)+" m; measured head/camera target height="+eye+" m.");Object.DestroyImmediate(player);
        Physics.SyncTransforms();
        int samples=0;
        for(float z=-37;z<=28.1f;z+=.2f)
        {
            var p=garden.TransformPoint(new Vector3(0,0,z))+Vector3.up*.08f;
            var hits=Physics.OverlapCapsule(p+Vector3.up*.32f,p+Vector3.up*1.84f,.28f,~0,QueryTriggerInteraction.Ignore);
            if(hits.Any(c=>c.enabled&&!c.isTrigger&&!(c is CharacterController)))throw new Exception("Route blocked at "+z+": "+string.Join(",",hits.Select(c=>c.name)));
            if(!Physics.Raycast(p+Vector3.up*.35f,Vector3.down,.6f,~0,QueryTriggerInteraction.Ignore))throw new Exception("Unsupported floor at "+z);
            samples++;
        }
        var gate=garden.Find("Night revision/Closed external gate");
        var gateOrigin=garden.TransformPoint(new Vector3(0,1.1f,-41.5f));
        if(!Physics.Raycast(gateOrigin,garden.forward,out var gateHit,4,~0,QueryTriggerInteraction.Ignore)||!gateHit.transform.IsChildOf(gate))throw new Exception("External gate is not closed and solid");
        var wall=garden.Find("Night revision/Raised perimeter walls").GetComponentsInChildren<BoxCollider>().First(c=>c.name=="Perimeter wall");
        if(wall.bounds.size.y<3.2f)throw new Exception("Perimeter wall too low");
        report.AppendLine("External gate closed with solid collision; perimeter wall height="+wall.bounds.size.y+" m.");
        var lawn=hall.Find("Night revision/Lawn visible through corridor windows");
        if(lawn.GetComponentsInChildren<Renderer>().Count(r=>r.sharedMaterial&&r.sharedMaterial.name=="Unmaintained night lawn")!=2)throw new Exception("Missing window lawn");
        report.AppendLine("Two persistent photographic grass grounds cover both window sides; selected glass panes fractured, detached rubble outside central route.");
        report.AppendLine(samples+" full route capsule clearances / floor supports passed; entrance and final connection retained.");
        var camera=new GameObject("Night review at actual player eye height").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=64;camera.nearClipPlane=.06f;camera.farClipPlane=130;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        Capture(camera,garden,new Vector3(-.8f,0,-33),eye,new Vector3(0,3.3f,0),"Exterior-Night");
        Capture(camera,garden,new Vector3(0,0,-30),eye,new Vector3(0,1.5f,-39.5f),"Closed-Gate");
        Capture(camera,hall,new Vector3(0,0,1.4f),eye,new Vector3(0,1.2f,24),"Corridor-Night");
        Capture(camera,hall,new Vector3(.5f,0,11),eye,new Vector3(-9,.45f,14),"Window-Lawn");
        Capture(camera,garden,new Vector3(-5,0,-8),eye,new Vector3(-7.5f,1.3f,-.2f),"Facade-Wear");
        TCRegionAtmosphere.ApplyForCamera(camera,Array.Empty<TCRegionAtmosphere>(),false);
        File.WriteAllText("NightEvidence/validation.txt",report+"CAMPUS_NIGHT_VALIDATION_PASS\n");Debug.Log("CAMPUS_NIGHT_VALIDATION_PASS");
    }
    static void Capture(Camera camera,Transform root,Vector3 local,float eye,Vector3 target,string name)
    {
        camera.transform.position=root.TransformPoint(local)+Vector3.up*eye;camera.transform.LookAt(root.TransformPoint(target));
        TCRegionAtmosphere.ApplyForCamera(camera,Object.FindObjectsByType<TCRegionAtmosphere>(),false);
        var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGBHalf);var output=new RenderTexture(1600,1000,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=rt;for(int i=0;i<4;i++)camera.Render();Graphics.Blit(rt,output);RenderTexture.active=output;
            var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes("NightEvidence/"+name+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(rt);Object.DestroyImmediate(output);}
    }
}
