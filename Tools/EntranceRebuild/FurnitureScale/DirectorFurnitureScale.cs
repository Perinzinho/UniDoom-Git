using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object=UnityEngine.Object;

public static class DirectorFurnitureScale
{
    const string Prefab="Assets/_Project/Scenes/DirectorRoom/Prefabs/DirectorRoom.prefab";
    const string Evidence="Evidence/FurnitureScale";
    static readonly string[] Names={"metal_office_desk","mid_century_lounge_chair","desk_lamp_arm_01","Stable warm desk lamp","Precisely aligned leather blotter","Ordered correspondence"};
    public static void Apply()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/EntranceRebuild/"))throw new Exception("Use isolated project");
        Directory.CreateDirectory(Evidence);Review("Before");
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var art=root.transform.Find("Total Chaos Rebuild");var layout=art.GetComponent<EntranceDressingLayout>();
            var desk=layout.placements.Single(p=>p.target.name=="metal_office_desk");
            var pivot=new Vector3(desk.position.x,0,desk.position.z);int changed=0;
            for(int i=0;i<layout.placements.Length;i++)
            {
                var p=layout.placements[i];if(!Names.Contains(p.target.name))continue;
                p.position=pivot+(p.position-pivot)*1.3f;p.scale*=1.3f;
                p.target.localPosition=p.position;p.target.localScale=p.scale;
                layout.placements[i]=p;changed++;
            }
            if(changed!=11)throw new Exception("Unexpected furniture selection: "+changed);
            EditorUtility.SetDirty(layout);PrefabUtility.SaveAsPrefabAsset(root,Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Review("After");Debug.Log("DIRECTOR_FURNITURE_SCALE_PASS");
        EntrancePlayCheck.Run();
    }
    public static void Verify()
    {
        Directory.CreateDirectory(Evidence);Review("Verified");Debug.Log("DIRECTOR_FURNITURE_VERIFY_PASS");EntrancePlayCheck.Run();
    }
    static Bounds BoundsOf(Transform target)
    {
        var renderers=target.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
        var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static void Review(string stage)
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        var root=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="DirectorRoom");
        var art=root.Find("Total Chaos Rebuild");var layout=art.GetComponent<EntranceDressingLayout>();layout.Refresh();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Player/Player.prefab");
        var controller=source.GetComponent<CharacterController>();
        float playerHeight=controller.height*source.transform.lossyScale.y;
        float eyeHeight=source.transform.Find("Head").position.y-source.transform.position.y+playerHeight*.5f;
        var report=new StringBuilder();report.AppendLine("Player controller world height="+playerHeight+"; eye height from feet="+eyeHeight);
        foreach(Transform t in art)if(t.name=="metal_office_desk"||t.name=="mid_century_lounge_chair")
        {
            var b=BoundsOf(t);report.AppendLine(t.name+": world size="+b.size+"; bottom above floor="+(b.min.y-art.position.y));
            if(Mathf.Abs(b.min.y-art.position.y)>.015f)throw new Exception("Floating furniture: "+t.name);
            var collider=t.GetComponent<BoxCollider>();if(!collider||!collider.enabled)throw new Exception("Missing furniture collider");
        }
        var desk=art.Cast<Transform>().Single(t=>t.name=="metal_office_desk");var top=BoundsOf(desk).max.y;
        var lamp=art.Cast<Transform>().Single(t=>t.name=="desk_lamp_arm_01");
        if(Mathf.Abs(BoundsOf(lamp).min.y-top)>.015f)throw new Exception("Desk lamp lost surface contact");
        foreach(var p in layout.placements)if(Names.Contains(p.target.name))
            if((p.target.localScale-p.scale).sqrMagnitude>.0001f)throw new Exception("Layout overrides furniture scale");
        Physics.SyncTransforms();
        for(int i=0;i<9;i++)
        {
            var p=art.TransformPoint(new Vector3(0,.03f,Mathf.Lerp(-layout.Size.y*.44f,-1.2f,i/8f)));
            var hits=Physics.OverlapCapsule(p+Vector3.up*.29f,p+Vector3.up*(playerHeight-.29f),.28f,~0,QueryTriggerInteraction.Ignore);
            if(hits.Any(c=>c.enabled&&c.transform.IsChildOf(art)))throw new Exception("Furniture blocks entry route");
        }
        report.AppendLine("PASS: support contact, colliders, persistent layout scales and entry capsule clearance.");
        File.WriteAllText(Evidence+"/"+stage+".txt",report.ToString());
        var camera=new GameObject("Office player-scale review").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=60;camera.nearClipPlane=.06f;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        camera.transform.position=art.TransformPoint(new Vector3(-layout.Size.x*.21f,eyeHeight,-layout.Size.y*.31f));
        camera.transform.LookAt(art.TransformPoint(new Vector3(0,1.1f,layout.Size.y*.13f)));
        TCRegionAtmosphere.ApplyForCamera(camera,Object.FindObjectsByType<TCRegionAtmosphere>(FindObjectsSortMode.None),false);
        ShaderUtil.allowAsyncCompilation=false;var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGBHalf);var ldr=new RenderTexture(1440,900,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=rt;for(int i=0;i<3;i++)camera.Render();Graphics.Blit(rt,ldr);RenderTexture.active=ldr;
            var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes(Evidence+"/"+stage+".png",image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(rt);Object.DestroyImmediate(ldr);}
        TCRegionAtmosphere.RestoreEnvironment();
    }
}
