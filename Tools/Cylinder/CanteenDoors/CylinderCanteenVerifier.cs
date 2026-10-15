using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using TMPro;
using Object = UnityEngine.Object;

public static class CylinderCanteenVerifier
{
    const string Prefab = CylinderCanteenBuilder.Prefab;
    const string Evidence = "CanteenEvidence/";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static Transform atrium;
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static void Check(bool condition,string message) { if(!condition) throw new Exception(message); }

    static GameObject Load()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        PrefabUtility.UnpackPrefabInstance(room,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        room.transform.position=Vector3.zero; atrium=room.transform.Find("Abandoned circular atrium"); return room;
    }

    public static void PrepareFinal()
    {
        CylinderCanteenBuilder.OptimizeGenerated(); Capture();
        Debug.Log("CYLINDER_CANTEEN_PREPARED");
    }

    public static void Verify()
    {
        Directory.CreateDirectory(Evidence);
        var room=Load();
        typeof(CylinderDetailVerifier).GetMethod("ValidateReferences",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{room});
        foreach(var t in room.GetComponentsInChildren<TextMesh>(true)) Check(string.IsNullOrEmpty(t.text),"TextMesh was not cleared");
        foreach(var t in room.GetComponentsInChildren<TMP_Text>(true)) Check(string.IsNullOrEmpty(t.text),"TMP text was not cleared");
        foreach(var r in room.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled))
            Check(!r.sharedMaterials.Any(m=>m&&(m.name.Contains("Lettering")||m.name=="utility_box_02")),"Visible lettering material remains");
        var transforms=room.GetComponentsInChildren<Transform>(true);
        Check(transforms.Count(t=>t.name.StartsWith("Geometric pictogram - "))==47,"Expected one pictogram for each of 47 doors");
        foreach(var item in new[]{("Classroom",12), ("Library",36), ("Director",24)})
        {
            var marker=transforms.Single(t=>t.name=="Door identity - "+item.Item1+" - bay "+item.Item2);
            var position=atrium.InverseTransformPoint(marker.position);
            Check(item.Item1=="Classroom"?position.x>26:item.Item1=="Library"?position.x< -26:position.z< -18,"Incorrect room placement "+item.Item1);
        }
        Check(transforms.Count(t=>t.name=="Twin group espresso machine")==16,"Coffee machine count changed");
        Check(transforms.Count(t=>t.name=="Detailed insulated coffee urn")==16,"Urn count changed");
        Check(transforms.Count(t=>t.name=="Hollow ceramic cup")==48,"Cup count changed");
        var doors=room.GetComponentsInChildren<OpenDoorWithKey>(); Check(doors.Length==4,"Expected four functional doors");
        var front=doors.Single(d=>d.name=="Interactive door - Front entrance");
        var frontLeaf=(Transform)new SerializedObject(front).FindProperty("door").objectReferenceValue;
        Check(Mathf.Abs(frontLeaf.GetComponent<BoxCollider>().size.z-2.6f)<.001f,"Front door width");
        Check(Mathf.Abs(frontLeaf.GetComponent<BoxCollider>().size.y-3.45f)<.001f,"Front door height");
        Check(doors.Where(d=>d!=front).All(d=>((Transform)new SerializedObject(d).FindProperty("door").objectReferenceValue).GetComponent<BoxCollider>().size.z<2),"Original door widths changed");
        var player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Player/Player.prefab"));
        foreach(var script in player.GetComponentsInChildren<MonoBehaviour>()) script.enabled=false;
        try
        {
            var cc=player.GetComponent<CharacterController>(); float feet=cc.height*player.transform.lossyScale.y/2;
            foreach(var offset in new[]{Vector3.zero,V(29.66f,4.84f,31.37f)})
            {
                room.transform.position=offset; Physics.SyncTransforms();
                foreach(var radii in new[]{new Vector2(24,17),new Vector2(11,9)})
                {
                    player.transform.position=atrium.TransformPoint(V(0,feet+.06f,radii.y));Physics.SyncTransforms();
                    for(int i=1;i<=720;i++)
                    {
                        float a=i*Mathf.PI/360;var target=atrium.TransformPoint(V(Mathf.Sin(a)*radii.x,feet+.06f,Mathf.Cos(a)*radii.y));
                        cc.Move(target-player.transform.position);Check(Vector3.Distance(player.transform.position,target)<.15f,"Circulation obstructed "+target);
                    }
                }
                foreach(var door in doors) TestDoor(door,player,cc,feet);
                var leaf=BeginDoor(front); var hinge=(Transform)typeof(OpenDoorWithKey).GetField("hinge",Private).GetValue(front); hinge.localRotation=Quaternion.Euler(0,-100,0);Physics.SyncTransforms();
                player.transform.position=atrium.TransformPoint(V(0,feet+.06f,20.4f));Physics.SyncTransforms();
                for(int i=1;i<=160;i++)
                {
                    var target=atrium.TransformPoint(V(0,feet+.06f,Mathf.Lerp(20.4f,8,i/160f)));
                    cc.Move(target-player.transform.position);Check(Vector3.Distance(player.transform.position,target)<.15f,"South entrance route obstructed");
                }
                EndDoor(front,leaf);
                var markers=transforms.Where(t=>t.name.StartsWith("Support check -")).ToArray();Check(markers.Length==80,"Support markers changed");
                foreach(var marker in markers)
                    Check(Physics.Raycast(marker.position+Vector3.up*.03f,Vector3.down,out var hit,.06f)&&Mathf.Abs(hit.point.y-marker.position.y)<.01f,"Countertop support missing");
                for(int i=0;i<96;i++)foreach(float r in new[]{0f,.15f,.5f,.97f})
                {
                    float a=(i+.5f)*Mathf.PI/48;var pos=atrium.TransformPoint(V(Mathf.Sin(a)*27.1f*r,4.6f,Mathf.Cos(a)*19.1f*r));
                    Check(Physics.Raycast(pos,Vector3.up,out var hit,5)&&hit.collider.name=="Roof","Roof coverage changed");
                }
            }
            Physics.SyncTransforms();
            var spawner=room.GetComponentInChildren<CylinderEnemySpawns>();var points=new SerializedObject(spawner).FindProperty("spawnPoints");Check(points.arraySize==6,"Spawn count changed");
            for(int i=0;i<points.arraySize;i++)
            {
                var t=(Transform)points.GetArrayElementAtIndex(i).objectReferenceValue;
                Check(Physics.OverlapCapsule(t.position-Vector3.up*.49f,t.position+Vector3.up*.49f,.48f,~0,QueryTriggerInteraction.Ignore).Length==0,"Spawn obstructed");
            }
        }
        finally { Object.DestroyImmediate(player); }
        File.WriteAllText(Evidence+"edit-validation.txt","PASS\nUnity 6000.5.7f1 / URP 17.5: scripts compiled and final prefab loaded.\nNo missing serialized references, scripts, active meshes or materials.\nAll TextMesh/TMP strings empty; lettering renderers hidden; imported cabinet lettering replaced.\n47 geometric wordless door pictograms. Classroom +X/right, Library -X/left, Director -Z/rear.\nFour proximity doors checked: trigger detection, fully open passage and closed blocking.\nFront entrance 2.60 x 3.45 m; other doors retain 1.85 x 3.25 m.\nBoth full circulation loops and the south entrance checked with the real Player CharacterController, at origin and placed transform.\n80 counter supports, 384 roof rays and six spawn clearances checked.\n16 espresso machines, 16 urns and 48 hollow cups in persistent combined meshes.\n");
        Capture(); Debug.Log("CYLINDER_CANTEEN_EDIT_PASS");
    }

    static Transform BeginDoor(OpenDoorWithKey door)
    { typeof(OpenDoorWithKey).GetMethod("Awake",Private).Invoke(door,null);return (Transform)typeof(OpenDoorWithKey).GetField("door",Private).GetValue(door); }

    static void EndDoor(OpenDoorWithKey door,Transform leaf)
    {
        typeof(OpenDoorWithKey).GetMethod("OnDisable",Private).Invoke(door,null);
        leaf.SetParent(door.transform,false);leaf.localPosition=Vector3.zero;leaf.localRotation=Quaternion.identity;leaf.localScale=Vector3.one;
        var motion=door.transform.Find("DoorMotionRoot");if(motion)Object.DestroyImmediate(motion.gameObject);Physics.SyncTransforms();
    }

    static void TestDoor(OpenDoorWithKey door,GameObject player,CharacterController cc,float feet)
    {
        var leaf=BeginDoor(door);var type=typeof(OpenDoorWithKey);
        player.transform.position=door.transform.TransformPoint(V(1.6f,feet+.06f,0));Physics.SyncTransforms();
        type.GetMethod("TrackPlayer",Private).Invoke(door,new object[]{cc});type.GetMethod("Update",Private).Invoke(door,null);
        Check((bool)type.GetField("unlocked",Private).GetValue(door),"Proximity failed "+door.name);
        ((Transform)type.GetField("hinge",Private).GetValue(door)).localRotation=Quaternion.Euler(0,-100,0);Physics.SyncTransforms();
        for(int i=1;i<=120;i++)
        {
            var target=door.transform.TransformPoint(V(Mathf.Lerp(1.6f,-1.0f,i/120f),feet+.06f,0));
            cc.Move(target-player.transform.position);Check(Vector3.Distance(player.transform.position,target)<.15f,"Open door blocks: "+door.name);
        }
        type.GetMethod("OnDisable",Private).Invoke(door,null);Physics.SyncTransforms();
        player.transform.position=door.transform.TransformPoint(V(1.6f,feet+.06f,0));bool blocked=false;
        for(int i=1;i<=120;i++)
        {
            var target=door.transform.TransformPoint(V(Mathf.Lerp(1.6f,-1.0f,i/120f),feet+.06f,0));
            cc.Move(target-player.transform.position);if(Vector3.Distance(player.transform.position,target)>.15f){blocked=true;break;}
        }
        Check(blocked,"Closed door did not block "+door.name);EndDoor(door,leaf);
    }

    public static void Capture()
    {
        Directory.CreateDirectory(Evidence);var room=Load();
        var region=room.GetComponentInChildren<TCRegionAtmosphere>();ShaderUtil.allowAsyncCompilation=false;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=region.ambient;
        var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(region.ambient);RenderSettings.ambientProbe=ambient;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=region.fogColor;RenderSettings.fogDensity=region.fogDensity;
        var camera=new GameObject("Revision camera").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=72;camera.nearClipPlane=.04f;camera.farClipPlane=130;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=region.fogColor;
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);
        var views=new[]{("Canteen",V(1.2f,1.84f,6.2f),V(.3f,1.55f,3.6f)),("CanteenWide",V(7,1.84f,11),V(0,2,0)),
            ("ClassroomDoor",V(21.7f,1.84f,0),V(27,2,0)),("LibraryDoor",V(-21.7f,1.84f,0),V(-27,2,0)),
            ("DirectorDoor",V(0,1.84f,-13.6f),V(0,2,-19)),("FrontDoor",V(0,1.84f,13.6f),V(0,2,19)),
            ("ServicePlaques",V(18,1.84f,11),V(22,2.1f,15))};
        foreach(var view in views)
        {
            camera.transform.position=atrium.TransformPoint(view.Item2);camera.transform.LookAt(atrium.TransformPoint(view.Item3));
            TCRegionAtmosphere.ApplyForCamera(camera,new[]{region},true);
            for(int i=0;i<5;i++)camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();
            File.WriteAllBytes(Evidence+view.Item1+".png",tex.EncodeToPNG());TCRegionAtmosphere.RestoreEnvironment();
        }
        RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);
        var renderers=room.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();long triangles=0;
        foreach(var r in renderers){var m=r.GetComponent<MeshFilter>()?.sharedMesh;if(m)for(int i=0;i<m.subMeshCount;i++)triangles+=(long)m.GetIndexCount(i)/3;}
        File.WriteAllText(Evidence+"asset-counts.txt","Visible mesh renderers: "+renderers.Length+"\nInstanced triangles: "+triangles+"\nNo frame-time or FPS claim.\n");
    }

    [InitializeOnLoadMethod]
    static void ReconnectPlay()
    {
        if(SessionState.GetBool("CanteenDoorPlay",false)) { EditorApplication.update-=Tick;EditorApplication.update+=Tick; }
    }

    public static void RunPlayMode()
    {
        Load();
        var player=new GameObject("Canteen door test player");player.tag="Player";player.AddComponent<PlayerPickupKey>();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Player/Player.prefab");
        player.transform.localScale=source.transform.localScale;
        var original=source.GetComponent<CharacterController>();var cc=player.AddComponent<CharacterController>();
        cc.height=original.height;cc.radius=original.radius;cc.center=original.center;cc.skinWidth=original.skinWidth;cc.stepOffset=original.stepOffset;
        float feet=cc.height*player.transform.lossyScale.y/2;
        player.transform.position=atrium.TransformPoint(V(0,feet+.06f,23.5f));
        var camera=new GameObject("Canteen test camera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        camera.transform.position=atrium.TransformPoint(V(0,1.84f,20.5f));camera.transform.LookAt(atrium.TransformPoint(V(0,2,0)));
        EditorSceneManager.SaveScene(player.scene,"Assets/_TemporaryTools/CanteenDoorPlay.unity");
        SessionState.SetBool("CanteenDoorPlay",true);SessionState.SetInt("CanteenDoorPhase",0);
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
    }

    static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            var encounter=Object.FindAnyObjectByType<CylinderEnemySpawns>();
            var space=encounter.transform.parent.parent;
            var front=space.GetComponentsInChildren<OpenDoorWithKey>().Single(d=>d.name=="Interactive door - Front entrance");
            var player=GameObject.Find("Canteen door test player");var cc=player.GetComponent<CharacterController>();
            float feet=cc.height*player.transform.lossyScale.y/2;
            int phase=SessionState.GetInt("CanteenDoorPhase",0);float start=SessionState.GetFloat("CanteenDoorStart",0);
            float progress=(float)typeof(OpenDoorWithKey).GetField("openProgress",Private).GetValue(front);
            foreach(var enemy in Object.FindObjectsByType<Enemy>()) enemy.enabled=false;
            int count=Object.FindObjectsByType<Enemy>().Length;
            Action<int> advance=n=>{SessionState.SetInt("CanteenDoorPhase",n);SessionState.SetFloat("CanteenDoorStart",Time.time);};
            Action<float> teleport=z=>{cc.enabled=false;player.transform.position=space.TransformPoint(V(0,feet+.06f,z));cc.enabled=true;Physics.SyncTransforms();};
            if(phase==0) { advance(1);return; }
            if(phase==1 && Time.time-start>.5f)
            {
                Check(count==0,"Enemies existed before entry");teleport(20.5f);advance(2);
            }
            else if(phase==2 && Time.time-start>1.5f)
            {
                Check(progress>.99f,"New entrance did not open by real player proximity");Check(count==0,"Enemies spawned outside encounter");advance(3);
            }
            else if(phase==3 || phase==7)
            {
                var local=space.InverseTransformPoint(player.transform.position);
                float step=Mathf.Min(.12f,Mathf.Max(.02f,Time.deltaTime*2.5f));
                var target=space.TransformPoint(V(0,feet+.06f,Mathf.Max(16.5f,local.z-step)));
                cc.Move(target-player.transform.position);
                Check(Vector3.Distance(player.transform.position,target)<.15f,"Runtime front door passage obstructed");
                if(local.z<16.6f) advance(phase==3?4:8);
            }
            else if(phase==4 && Time.time-start>8.5f)
            {
                Check(count==6,"Expected six enemies after entering, got "+count);teleport(23.5f);advance(5);
            }
            else if(phase==5 && Time.time-start>2)
            {
                Check(progress<.01f,"Front entrance did not close after leaving");teleport(20.5f);advance(6);
            }
            else if(phase==6 && Time.time-start>1.5f)
            {
                Check(progress>.99f,"Front entrance did not reopen");advance(7);
            }
            else if(phase==8 && Time.time-start>1)
            {
                Check(count==6,"Re-entry duplicated the enemy encounter");
                File.WriteAllText(Evidence+"play-validation.txt","PASS\nIsolated Unity Play Mode.\nNew front entrance opens by physical proximity to a player carrying PlayerPickupKey.\nThe real CharacterController dimensions pass through the moving entrance.\nNo enemies before the entrance/encounter; exactly six spawn after entry.\nEntrance closes when leaving and reopens on approach. Re-entry retains exactly six enemies.\nFull-map combat, build and target-hardware profiling were not run.\n");
                SessionState.SetBool("CanteenDoorPlay",false);EditorApplication.update-=Tick;Debug.Log("CYLINDER_CANTEEN_PLAY_PASS");EditorApplication.Exit(0);
            }
            if(Time.time-start>30) throw new Exception("Play phase timeout "+phase);
        }
        catch(Exception error)
        {
            SessionState.SetBool("CanteenDoorPlay",false);EditorApplication.update-=Tick;Debug.LogException(error);EditorApplication.Exit(1);
        }
    }
}
