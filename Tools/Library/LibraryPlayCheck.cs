using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;

[InitializeOnLoad]
public static class LibraryPlayCheck
{
    static CharacterController controller;
    static Camera camera;
    static Transform interior;
    static int waypoint;
    static double started,lastTick;
    static readonly Vector3[] points={new Vector3(10.1f,0,18.7f),new Vector3(9.6f,0,15.6f),new Vector3(0,0,15.6f),new Vector3(0,0,-15.8f),new Vector3(-6.2f,0,-15.5f),new Vector3(-6.2f,0,8.3f),new Vector3(0,0,8.3f)};
    static LibraryPlayCheck(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LibraryBuilder.Prefab));
        root.transform.localScale=LibraryBuilder.MapScale;root.transform.position=-Vector3.Scale(LibraryBuilder.FloorOrigin,LibraryBuilder.MapScale);
        var go=new GameObject("Library traversal test capsule");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Player/Player.prefab").GetComponent<CharacterController>();
        var cc=go.AddComponent<CharacterController>();cc.height=source.height*source.transform.localScale.y;
        cc.radius=source.radius*Mathf.Max(source.transform.localScale.x,source.transform.localScale.z);cc.center=Vector3.up*cc.height*.5f;
        cc.stepOffset=source.stepOffset;cc.skinWidth=.03f;go.transform.position=points[0]+Vector3.up*.06f;
        var cam=new GameObject("Library traversal camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.SetParent(go.transform,false);cam.transform.localPosition=Vector3.up*1.8f;
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/_TemporaryTools/LibraryPlayCheck.unity");
        SessionState.SetBool("LibraryPlayPending",true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool("LibraryPlayPending",false)||!EditorApplication.isPlaying)return;
        try
        {
            if(!controller)
            {
                controller=GameObject.Find("Library traversal test capsule").GetComponent<CharacterController>();
                camera=GameObject.Find("Library traversal camera").GetComponent<Camera>();interior=GameObject.Find("Abandoned Library Interior").transform;
                started=lastTick=EditorApplication.timeSinceStartup;waypoint=1;return;
            }
            double now=EditorApplication.timeSinceStartup;float delta=Mathf.Min(.03f,(float)(now-lastTick));lastTick=now;
            var target=interior.TransformPoint(points[waypoint]);var distance=target-controller.transform.position;distance.y=0;
            if(distance.magnitude<.14f)
            {
                waypoint++;if(waypoint>=points.Length)
                {
                    var active=TCRegionAtmosphere.ApplyForCamera(camera,new[]{interior.GetComponentInChildren<TCRegionAtmosphere>()},false);
                    if(!active)throw new Exception("Regional atmosphere was not selected at the player camera");
                    TCRegionAtmosphere.RestoreEnvironment();
                    File.WriteAllText("Evidence/play-validation.txt","PASS\nReal Play Mode in isolated Unity project.\nCharacterController uses world height/radius from the existing Player prefab.\nMoved physically along entry, central and west aisles with gravity and collision, approximately 80 m.\nLocal library atmosphere selected and restored successfully.\nThis checks traversal and atmosphere, not full player controls, weapons, combat or hardware FPS.\n");
                    SessionState.SetBool("LibraryPlayPending",false);Debug.Log("LIBRARY_PLAY_PASS");EditorApplication.Exit(0);return;
                }
            }
            else
            {
                camera.transform.rotation=Quaternion.LookRotation(distance);
                controller.Move((distance.normalized*9+Vector3.down*4)*delta);
            }
            if(now-started>90)throw new Exception("Traversal stalled at waypoint "+waypoint+" position "+controller.transform.position);
        }
        catch(Exception e){SessionState.SetBool("LibraryPlayPending",false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
