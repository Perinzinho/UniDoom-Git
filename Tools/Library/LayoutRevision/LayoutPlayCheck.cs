using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;

[InitializeOnLoad]
public static class LayoutPlayCheck
{
    static CharacterController controller;
    static Camera camera;
    static Transform interior;
    static int waypoint;
    static Transform leaf;
    static Quaternion closedRotation;
    static float maxDoorAngle;
    static double started,lastTick;
    static readonly Vector3[] points={new Vector3(10.1f,0,21.5f),new Vector3(10.1f,0,18.3f),new Vector3(9.6f,0,15.6f),new Vector3(0,0,15.6f),new Vector3(0,0,-15.8f),new Vector3(-6.2f,0,-15.5f),new Vector3(-6.2f,0,15.6f),new Vector3(0,0,15.6f)};
    static LayoutPlayCheck(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LibraryBuilder.Prefab));
        root.transform.localScale=LibraryBuilder.MapScale;root.transform.position=-Vector3.Scale(LibraryBuilder.FloorOrigin,LibraryBuilder.MapScale);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary external approach floor";floor.transform.position=new Vector3(10.1f,-.08f,21);floor.transform.localScale=new Vector3(3,.1f,3);
        var go=new GameObject("Library traversal test capsule");go.AddComponent<PlayerPickupKey>();var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Player/Player.prefab").GetComponent<CharacterController>();
        var cc=go.AddComponent<CharacterController>();cc.height=source.height*source.transform.localScale.y;
        cc.radius=source.radius*Mathf.Max(source.transform.localScale.x,source.transform.localScale.z);cc.center=Vector3.up*cc.height*.5f;
        cc.stepOffset=source.stepOffset;cc.skinWidth=.03f;go.transform.position=points[0]+Vector3.up*.06f;
        var cam=new GameObject("Library traversal camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.SetParent(go.transform,false);cam.transform.localPosition=Vector3.up*1.8f;
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/_TemporaryTools/LayoutPlayCheck.unity");
        SessionState.SetBool("LibraryLayoutPlayPending",true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool("LibraryLayoutPlayPending",false)||!EditorApplication.isPlaying)return;
        try
        {
            if(!controller)
            {
                controller=GameObject.Find("Library traversal test capsule").GetComponent<CharacterController>();
                camera=GameObject.Find("Library traversal camera").GetComponent<Camera>();interior=GameObject.Find("Abandoned Library Interior").transform;
                leaf=interior.Find("Simple library entrance/Unlatched swing door").GetComponent<OpenDoorWithKey>().GetComponentInChildren<MeshRenderer>().transform;closedRotation=interior.rotation*Quaternion.Euler(0,90,0);maxDoorAngle=0;
                started=lastTick=EditorApplication.timeSinceStartup;waypoint=1;return;
            }
            double now=EditorApplication.timeSinceStartup;float delta=Mathf.Min(.03f,(float)(now-lastTick));lastTick=now;
            maxDoorAngle=Mathf.Max(maxDoorAngle,Quaternion.Angle(closedRotation,leaf.rotation));
            if(now-started<1.0)return;
            var target=interior.TransformPoint(points[waypoint]);var distance=target-controller.transform.position;distance.y=0;
            if(distance.magnitude<.14f)
            {
                waypoint++;if(waypoint>=points.Length)
                {
                    if(maxDoorAngle<70)throw new Exception("Plain door did not open sufficiently");
                    if(Quaternion.Angle(closedRotation,leaf.rotation)>3)throw new Exception("Plain door failed to close after traversal");
                    var active=TCRegionAtmosphere.ApplyForCamera(camera,new[]{interior.GetComponentInChildren<TCRegionAtmosphere>()},false);
                    if(!active)throw new Exception("Regional atmosphere was not selected at the player camera");
                    TCRegionAtmosphere.RestoreEnvironment();
                    File.WriteAllText("Evidence/Layout/play-validation.txt","PASS\nReal Play Mode in isolated Unity project.\nCharacterController uses world height/radius from the existing Player prefab.\nMoved physically along entry, central and west aisles with gravity and collision.\nLocal library atmosphere selected and restored successfully.\nPlain entrance door opened beyond 70 degrees without keys, admitted the player and closed afterwards.\nThis checks traversal and atmosphere, not full player controls, weapons, combat or hardware FPS.\n");
                    SessionState.SetBool("LibraryLayoutPlayPending",false);Debug.Log("LIBRARY_LAYOUT_PLAY_PASS");EditorApplication.Exit(0);return;
                }
            }
            else
            {
                camera.transform.rotation=Quaternion.LookRotation(distance);
                controller.Move((distance.normalized*9+Vector3.down*4)*delta);
            }
            if(now-started>90)throw new Exception("Traversal stalled at waypoint "+waypoint+" position "+controller.transform.position);
        }
        catch(Exception e){SessionState.SetBool("LibraryLayoutPlayPending",false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
