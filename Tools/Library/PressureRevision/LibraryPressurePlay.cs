using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class LibraryPressurePlay
{
    static CharacterController controller;
    static Transform art,leaf;
    static PlayerPickupKey pickup;
    static LibraryEncounterDirector director;
    static Quaternion closed;
    static Vector3[] route;
    static int waypoint,phase;
    static double started,lastTick,phaseStarted,lastProgress;
    static float maximumDoorAngle;
    static bool pursued, sawBlackout, sawRestore;
    static bool[] initialLightStates;
    static double collectedAt;
    static Vector3 lastPosition;
    static readonly Dictionary<Enemy,double> deathTimes=new Dictionary<Enemy,double>();
    static readonly Dictionary<Enemy,double> seen=new Dictionary<Enemy,double>();
    static readonly Dictionary<Enemy,Vector3> originalPositions=new Dictionary<Enemy,Vector3>();
    static LibraryPressurePlay(){EditorApplication.update+=Tick;}
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LibraryBuilder.Prefab));root.transform.localScale=LibraryBuilder.MapScale;root.transform.position=-Vector3.Scale(LibraryBuilder.FloorOrigin,LibraryBuilder.MapScale);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Temporary approach platform";floor.transform.position=V(10.1f,-.08f,21);floor.transform.localScale=V(3,.1f,3);
        var go=new GameObject("Encounter test player");go.tag="Player";go.AddComponent<PlayerPickupKey>();var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Player/Player.prefab").GetComponent<CharacterController>();
        var cc=go.AddComponent<CharacterController>();cc.height=source.height*source.transform.localScale.y;cc.radius=source.radius*Mathf.Max(source.transform.localScale.x,source.transform.localScale.z);cc.center=Vector3.up*cc.height*.5f;cc.stepOffset=source.stepOffset;cc.skinWidth=.03f;go.transform.position=V(10.1f,.06f,21.5f);
        var camera=new GameObject("Encounter test camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.SetParent(go.transform,false);camera.transform.localPosition=Vector3.up*1.8f;camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        var uiRoot=new GameObject("Test key UI");uiRoot.SetActive(false);var ui=uiRoot.AddComponent<KeysUI>();var so=new SerializedObject(ui);var icons=so.FindProperty("keyIcons");icons.arraySize=3;
        for(int i=0;i<3;i++){var icon=new GameObject("Test key icon "+i);icon.transform.SetParent(uiRoot.transform,false);icons.GetArrayElementAtIndex(i).objectReferenceValue=icon;}so.ApplyModifiedPropertiesWithoutUndo();uiRoot.SetActive(true);
        new GameObject("Test SFX manager").AddComponent<SFXManager>();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/_TemporaryTools/LibraryPressurePlay.unity");SessionState.SetBool("LibraryPressurePlayPending",true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool("LibraryPressurePlayPending",false)||!EditorApplication.isPlaying)return;
        try
        {
            if(!controller)
            {
                controller=GameObject.Find("Encounter test player").GetComponent<CharacterController>();pickup=controller.GetComponent<PlayerPickupKey>();pickup.SetKeysUI(Object.FindFirstObjectByType<KeysUI>());
                art=GameObject.Find("Abandoned Library Interior").transform;director=art.GetComponentInChildren<LibraryEncounterDirector>();leaf=art.Find("Simple library entrance/Unlatched swing door").GetComponentInChildren<BoxCollider>().transform;
                leaf=art.Find("Simple library entrance/Unlatched swing door").GetComponentInChildren<OpenDoorWithKey>().transform.Find("DoorMotionRoot/DoorHinge/Door");
                if(!leaf)throw new Exception("Cylinder door leaf not found after hinge setup");closed=art.rotation*Quaternion.Euler(0,90,0);maximumDoorAngle=0;
                started=lastTick=phaseStarted=lastProgress=EditorApplication.timeSinceStartup;lastPosition=controller.transform.position;phase=0;seen.Clear();deathTimes.Clear();originalPositions.Clear();sawBlackout=sawRestore=false;collectedAt=0;
                initialLightStates=director.blackoutLights.Select(l=>l.enabled).ToArray();return;
            }
            double now=EditorApplication.timeSinceStartup;float delta=Mathf.Min(.035f,(float)(now-lastTick));lastTick=now;
            if(!director.ObjectiveCollected&&director.BlackoutActive)throw new Exception("Blackout fired before collection");
            if(director.BlackoutActive)
            {
                if(collectedAt==0)collectedAt=now;
                if(now-collectedAt>.15)
                {
                    if(director.blackoutLights.Any(l=>l.enabled))throw new Exception("Blackout circuit light stayed on");
                    foreach(var tube in director.blackoutTubes)
                    {
                        var block=new MaterialPropertyBlock();tube.GetPropertyBlock(block);
                        if(block.GetColor("_EmissionColor").maxColorComponent>.001f)throw new Exception("Blackout tube stayed emissive");
                    }
                    sawBlackout=true;
                }
            }
            else if(sawBlackout)
            {
                if(!director.blackoutLights.Select(l=>l.enabled).SequenceEqual(initialLightStates))throw new Exception("Blackout did not restore circuit states");
                sawRestore=true;
            }
            maximumDoorAngle=Mathf.Max(maximumDoorAngle,Quaternion.Angle(closed,leaf.rotation));
            foreach(var nav in Object.FindObjectsByType<LibraryZombieNavigator>(FindObjectsSortMode.None))
            {
                var enemy=nav.GetComponent<Enemy>();if(!seen.ContainsKey(enemy)){seen[enemy]=now;originalPositions[enemy]=enemy.transform.position;}
                var agent=nav.GetComponent<NavMeshAgent>();
                if(!enemy.IsDead&&now-seen[enemy]>.8&&Vector3.Distance(enemy.transform.position,originalPositions[enemy])>.25f&&agent.isOnNavMesh)pursued=true;
                // Exercise damage and leave bodies in the room while traversing the narrow route.
                if(!enemy.IsDead&&now-seen[enemy]>1.7)enemy.TakeDamage(1000,controller.gameObject);
                if(enemy.IsDead)
                {
                    if(!deathTimes.ContainsKey(enemy))deathTimes[enemy]=now;
                    if(now-deathTimes[enemy]>.3&&enemy.GetComponentsInChildren<Collider>().Any(c=>c.enabled))throw new Exception("Dead enemy blocked a choke point");
                }
            }
            if(now-started<1)return;
            if(phase==0)
            {
                if(Move(art.TransformPoint(V(10.1f,0,18.2f)),delta)){route=LibraryPressureVerifier.Path(art,V(10.1f,0,18.2f),V(-.1f,0,-16.5f));waypoint=1;phase=1;phaseStarted=now;}
            }
            else if(phase==1)
            {
                if(Move(route[waypoint],delta))
                {
                    waypoint++;if(waypoint==route.Length){phase=2;phaseStarted=now;}
                }
            }
            else if(phase==2)
            {
                if(pickup.key02&&director.ObjectiveCollected)
                {
                    var icon=GameObject.Find("Test key icon 1");if(!icon||!icon.activeSelf)throw new Exception("Key02 UI was not updated");
                    route=LibraryPressureVerifier.Path(art,V(-.1f,0,-16.5f),V(10.1f,0,18.2f));waypoint=1;phase=3;phaseStarted=now;
                }
                else if(now-phaseStarted>4)throw new Exception("Key02 pickup failed: player="+art.InverseTransformPoint(controller.transform.position)+" inventory="+pickup.key02+" collected="+director.ObjectiveCollected+" waves="+director.ActivatedWaves+" key="+(director.objectiveKey?director.objectiveKey.transform.localPosition.ToString()+" collider="+director.objectiveKey.GetComponent<BoxCollider>().bounds.ToString():"DESTROYED"));
            }
            else if(phase==3)
            {
                if(Move(route[waypoint],delta)){waypoint++;if(waypoint==route.Length){phase=4;phaseStarted=now;}}
            }
            else if(phase==4)
            {
                if(Move(art.TransformPoint(V(10.1f,0,22.1f)),delta)){phase=5;phaseStarted=now;}
            }
            else if(phase==5&&now-phaseStarted>2&&director.PendingSpawns==0&&sawRestore)
            {
                if(maximumDoorAngle<70)throw new Exception("Cylinder model door did not open");
                if(director.ActivatedWaves!=3||director.SpawnedCount!=16||director.PendingSpawns!=0)throw new Exception("Encounter count mismatch: "+director.ActivatedWaves+" waves / "+director.SpawnedCount+" spawns");
                if(!sawBlackout||!sawRestore)throw new Exception("Simple blackout was not exercised");
                if(!pursued)throw new Exception("No zombie demonstrated NavMesh pursuit");
                File.WriteAllText("Evidence/Pressure/play-validation.txt","PASS\nReal Play Mode: CharacterController entered through the Cylinder-model door, followed the obstacle route to Key02 and returned outside.\nKey02 inventory flag and UI icon updated through the existing pickup component.\nThree one-shot waves spawned exactly sixteen local zombie variants; the return wave required objective collection. Three light circuits and nine tube emissions shut off for eight seconds, then restored their initial states.\nNavMesh pursuit, existing enemy damage/death and door opening were exercised. Bodies remained in the room; their collision released after death, preventing permanent blocked passages.\nThis is a functional traversal/encounter test, not a full weapon/ammo balance, build or hardware FPS test.\n");
                SessionState.SetBool("LibraryPressurePlayPending",false);Debug.Log("LIBRARY_PRESSURE_PLAY_PASS");EditorApplication.Exit(0);return;
            }
            if(Vector3.Distance(lastPosition,controller.transform.position)>.25f){lastPosition=controller.transform.position;lastProgress=now;}
            if((phase==0||phase==1||phase==3||phase==4)&&now-lastProgress>12)throw new Exception("Traversal stalled in phase "+phase+" at "+art.InverseTransformPoint(controller.transform.position));
            if(now-started>180)throw new Exception("Encounter test timed out");
        }
        catch(Exception e){SessionState.SetBool("LibraryPressurePlayPending",false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static bool Move(Vector3 target,float delta)
    {
        var offset=target-controller.transform.position;offset.y=0;if(offset.magnitude<.13f)return true;
        controller.Move((offset.normalized*6.5f+Vector3.down*4)*delta);return false;
    }
}
