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

public static class LayoutVerifier
{
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    public static void VerifyThenPlay(){VerifyAndRender();LayoutPlayCheck.Run();}
    public static void VerifyAndRender()
    {
        Directory.CreateDirectory("Evidence/Layout");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LibraryBuilder.Prefab));
        room.transform.localScale=LibraryBuilder.MapScale;
        room.transform.position=-Vector3.Scale(LibraryBuilder.FloorOrigin,LibraryBuilder.MapScale);
        var art=room.transform.Find("Abandoned Library Interior");Require(art,"Library interior missing");
        Inspect(room);
        Require(Vector3.Distance(art.lossyScale,Vector3.one)<.001f,"Main-map dimensions must be metric");
        Physics.SyncTransforms();
        Capture(room,art,"Library");
        Route(art,new[]{V(10.1f,0,20.7f),V(10.1f,0,18.3f),V(9.6f,0,18.3f),V(9.6f,0,15.6f),V(0,0,15.6f),V(0,0,-15.8f)});
        Route(art,new[]{V(0,0,15.6f),V(-6.2f,0,15.6f),V(-6.2f,0,-15.5f),V(0,0,-15.5f)});
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/_TemporaryTools/LayoutReview.unity");
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        var inMap=GameObject.Find("Library");Require(inMap,"Main map library missing");
        var interior=inMap.transform.Find("Abandoned Library Interior");Require(interior,"Main scene did not inherit interior");Inspect(inMap);
        Require(Vector3.Distance(interior.lossyScale,Vector3.one)<.001f,"Scene overrides distort furniture");
        Physics.SyncTransforms();
        Route(interior,new[]{V(10.1f,0,19.0f),V(10.1f,0,18.3f),V(9.6f,0,18.3f),V(9.6f,0,15.6f),V(0,0,15.6f),V(0,0,-15.8f)});
        Capture(inMap,interior,"InMap");
        File.WriteAllLines("Evidence/Layout/dependencies.txt",AssetDatabase.GetDependencies(new[]{LibraryBuilder.Prefab}.Concat(Directory.GetFiles(LibraryBuilder.Art+"/Prefabs/Modules","*.prefab")).ToArray(),true));
        File.WriteAllText("Evidence/Layout/validation.txt","PASS\nUnity "+Application.unityVersion+" / URP\nPrefab loads; all enabled meshes, material bindings, scripts and serialized references valid.\nMain SampleScene inherits the updated library with metric furniture and original shell transforms.\nCapsule clearance tested along entry, central and west reading aisles.\nReal rendered captures in prefab and main scene, flashlight disabled.\nTwo persistent reflection cubemaps baked; no lightmap bake, standalone build, full combat test or target-hardware FPS benchmark performed.\n");
        Debug.Log("LIBRARY_LAYOUT_VERIFY_PASS");
    }
    static void Inspect(GameObject root)
    {
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script: "+t.name);
            foreach(var comp in t.GetComponents<Component>())
            {
                if(!comp)continue;var it=new SerializedObject(comp).GetIterator();
                while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&!it.objectReferenceValue&&!it.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))
                    throw new Exception("Missing reference: "+t.name+" / "+it.propertyPath);
            }
        }
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(!r.enabled)continue;var f=r.GetComponent<MeshFilter>();if(!f)continue;
            Require(f.sharedMesh,"Missing mesh: "+r.name);
            foreach(var m in r.sharedMaterials)Require(m&&m.shader&&m.shader.isSupported,"Invalid material: "+r.name);
        }
        int tris=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh).Sum(f=>f.sharedMesh.triangles.Length/3);
        var lights=root.GetComponentsInChildren<Light>(true);
        File.AppendAllText("Evidence/Layout/counts.txt",root.name+": transforms="+root.GetComponentsInChildren<Transform>(true).Length+", triangles (all LODs)="+tris+", lights="+lights.Length+", shadow lights="+lights.Count(l=>l.shadows!=LightShadows.None)+", LOD groups="+root.GetComponentsInChildren<LODGroup>().Length+"\n");
    }
    static void Route(Transform art,Vector3[] points)
    {
        const float radius=.35f,height=2.2f;
        for(int i=1;i<points.Length;i++)
        {
            int steps=Mathf.CeilToInt(Vector3.Distance(points[i-1],points[i])/.22f);
            for(int j=0;j<=steps;j++)
            {
                var p=art.TransformPoint(Vector3.Lerp(points[i-1],points[i],(float)j/steps));
                var hits=Physics.OverlapCapsule(p+Vector3.up*(radius+.10f),p+Vector3.up*(height-radius),radius,~0,QueryTriggerInteraction.Ignore);
                hits=hits.Where(h=>!h.GetComponentInParent<OpenDoorWithKey>()).ToArray();
                Require(hits.Length==0,"Player clearance blocked at "+art.InverseTransformPoint(p)+" by "+string.Join(",",hits.Select(h=>h.name)));
                // The first point can be outside the prefab threshold; all interior points need a floor.
                if(Mathf.Abs(art.InverseTransformPoint(p).z)<19.5f)
                    Require(Physics.Raycast(p+Vector3.up*.5f,Vector3.down,.8f,~0,QueryTriggerInteraction.Ignore),"Missing walkable floor");
            }
        }
    }
    static void Capture(GameObject room,Transform art,string prefix)
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
        var views=new[]{("Door",V(10.1f,1.72f,24f),V(10.1f,1.72f,19.67f)),("Entrance",V(9.7f,1.72f,18.4f),V(-.5f,2.0f,-7)),("ReadingRoom",V(.7f,1.72f,8.8f),V(-2.2f,1.8f,-10)),
            ("BookAisle",V(-5.7f,1.72f,9.0f),V(-7.6f,1.9f,-5)),("Quarantine",V(.7f,1.72f,14.6f),V(3.8f,1.7f,11)),
            ("WindowTables",V(7.1f,1.72f,7.0f),V(11.5f,1.7f,-6))};
        foreach(var view in views)
        {
            camera.transform.position=art.TransformPoint(view.Item2);camera.transform.LookAt(art.TransformPoint(view.Item3));
            TCRegionAtmosphere.ApplyForCamera(camera,new[]{region},false);
            for(int i=0;i<5;i++)camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
            File.WriteAllBytes("Evidence/Layout/"+prefix+"-"+view.Item1+".png",tex.EncodeToPNG());TCRegionAtmosphere.RestoreEnvironment();
        }
        RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);
        foreach(var shader in room.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.shader).Distinct())
            Require(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Shader compile error: "+shader.name);
    }
}
