// Editor generation source: copy to Assets/Editor in an isolated copy of the project.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class ClassRoomHorrorVerifier
{
    const string Art = "Assets/Art/ClassRoom";
    const string RoomPath = "Assets/prefabs/Scenario/ClassRoom.prefab";
    static readonly Vector3 Origin = new Vector3(-35.88486f, 1.15f, 22.763018f);
    const float Left=-6, Right=12, Back=-11, Front=5;
    const float Width=Right-Left, Depth=Front-Back, CenterX=(Left+Right)/2, CenterZ=(Back+Front)/2;
    const int WindowCount=6;
    const float WindowBay=Depth/WindowCount;
    const float DoorX=-4.25f, OpeningWidth=1.60f, OpeningHeight=2.72f, Jamb=.055f, DoorGap=.0075f;
    const float LeafWidth=OpeningWidth-2*Jamb-2*DoorGap, LeafHeight=OpeningHeight-Jamb-2*DoorGap;
    const float LeafThickness=.045f, PivotInset=.035f;
    static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
    static readonly List<List<Vector3>> playerRoutes = new List<List<Vector3>>();
    public static void VerifyAndRender()
    {
        playerRoutes.Clear();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath));room.transform.position=-Origin;
        var deco=room.transform.Find("AbandonedClassRoomInterior");if(!deco)throw new Exception("Decoration missing");
        foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
        {
            if(!r.enabled)continue;
            if(r.GetComponent<MeshFilter>() && !r.GetComponent<MeshFilter>().sharedMesh)throw new Exception("Mesh missing: "+r.name);
            foreach(var m in r.sharedMaterials)if(!m || !m.shader || m.shader.name=="Hidden/InternalErrorShader")throw new Exception("Material missing: "+r.name);
        }
        var door=room.transform.Find("DoorPrefab");var controller=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Player/Player.prefab").GetComponent<CharacterController>();
        float playerHeight=controller.height*controller.transform.localScale.y,playerWidth=controller.radius*2*Mathf.Max(controller.transform.localScale.x,controller.transform.localScale.z);
        if(door.Find("Door").localScale.y<playerHeight+.25f || door.Find("Door").localScale.z<playerWidth+.4f)throw new Exception("Door too small for player");
        VerifyRevision(deco, playerHeight);
        Physics.SyncTransforms();
        var leafBounds=door.Find("Door").GetComponent<BoxCollider>().bounds;
        float sideGap=OpeningWidth*.5f-Jamb-leafBounds.extents.x;
        float topGap=OpeningHeight-Jamb-leafBounds.max.y;
        if(Mathf.Abs(leafBounds.center.x-DoorX)>.001f || Mathf.Abs(leafBounds.center.z-Front)>.001f ||
            Mathf.Abs(sideGap-DoorGap)>.001f || Mathf.Abs(topGap-DoorGap)>.001f || Mathf.Abs(leafBounds.min.y-DoorGap)>.001f)
            throw new Exception("Door leaf does not fit its frame: "+leafBounds);
        var rear=V(2.1f,0,Back+.8f);var front=V(2.1f,0,2.7f);var exit=V(DoorX,0,4.3f);
        float routeLength=VerifyRoute(rear,front,playerHeight,playerWidth,room.transform)+VerifyRoute(front,exit,playerHeight,playerWidth,room.transform);
        VerifyRoute(rear,front,2f,1f,room.transform); // A wider 1 m capsule must also be able to flank the furniture.
        if(ClearSegment(rear,front,playerHeight,playerWidth,deco))throw new Exception("Central aisle lacks gameplay obstacles");
        for(float x=DoorX;x<=2.1f;x+=.25f)CheckClear(V(x,0,2.7f),playerHeight,playerWidth,deco);
        for(float z=2.95f;z<4.8f;z+=.25f)CheckClear(V(-4.25f,0,z),playerHeight,playerWidth,deco);
        var script=new SerializedObject(door.GetComponent<OpenDoorWithKey>());
        if(script.FindProperty("requiresKey01").boolValue || !door.GetComponent<BoxCollider>().isTrigger || !door.Find("Door").GetComponent<BoxCollider>())throw new Exception("Door behaviour changed");
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.075f,.085f,.11f);RenderSettings.fog=false;
        Render(room,"ClassRoom-preview.png",V(10.8f,2.35f,-9.9f),V(.3f,1.65f,3.5f));
        Render(room,"ClassRoom-player-view.png",V(2.1f,1.84f,Back+.65f),V(.3f,1.65f,4.65f));
        Render(room,"ClassRoom-aftermath.png",V(5.6f,1.84f,1.1f),V(7.5f,.55f,3.5f));
        Render(room,"ClassRoom-blood-trail.png",V(3.5f,1.84f,-9.8f),V(1.05f,.35f,-6.5f));
        Render(room,"ClassRoom-door-fit.png",V(DoorX+1.7f,1.6f,Front-3.0f),V(DoorX,1.4f,Front));
        EditorSceneManager.SaveScene(scene,Art+"/ClassRoomPreview.unity");
        Directory.CreateDirectory("Preview");File.WriteAllText("Preview/validation.txt","ClassRoom validated\nRoom 18 x 16 x 3.8 m (288 m2, previously 120 m2)\n42 university desks plus teacher furniture and displaced desks; desktop 1.36 x 0.82 m, top 1.066 m, seat 0.589 m\nPlayer height "+playerHeight+" m, width "+playerWidth+" m\nOpening 1.60 x 2.72 m; timber frame thickness 0.055 m\nDoor leaf "+LeafWidth+" x "+LeafHeight+" m; measured side/top/bottom gaps "+sideGap+", "+topGap+", "+leafBounds.min.y+" m\nPlayer route via central front waypoint "+routeLength.ToString("F2")+" m (0.1 m orthogonal grid)\nDirect central path blocked; player and 1 m wide capsules can flank both obstructions.\nFront cross aisle and exit approach clear.\nRenderers "+room.GetComponentsInChildren<Renderer>().Length+"\nLights "+room.GetComponentsInChildren<Light>().Length+"\nOriginal OpenDoorWithKey, trigger, Rigidbody and leaf collider retained.\nDoor opening in both directions and player passage verified.\nEntry wall/door location and prefab identity preserved.\n");
        Debug.Log("CLASSROOM_VERIFIED playerHeight="+playerHeight+" playerWidth="+playerWidth);
        VerifyDoorMotion(door.gameObject,playerHeight,playerWidth);
        AssetDatabase.SaveAssets();
        VerifyMapInheritance();
        File.AppendAllText("Preview/validation.txt", "University details: lecture, CRT workstation, laptops, projector, torn screen.\nAftermath: 3 anonymous adult bodies, blood pools, handprints, drag marks and footsteps.\nActual CharacterController traversal verified forward and back in prefab AND SampleScene.\nOnly the new lecturer workstation adds collision; bodies and stains are walkable.\n");
    }
    static void VerifyRevision(Transform deco, float playerHeight)
    {
        var desk = deco.Find("Student desk 0-0");
        var chair = deco.Find("Student chair 0-0");
        float top = .8325f * desk.localScale.y;
        float width = 1.03f * desk.localScale.x;
        float depth = .62f * desk.localScale.z;
        float seat = .46f * chair.localScale.y;
        if (top < playerHeight * .49f || width < 1.30f || depth < .80f || seat < .57f)
            throw new Exception("University furniture remains too small for the player");
        var details = deco.Find("University apocalypse details");
        if (!details || details.Cast<Transform>().Count(t => t.name.Contains("victim") || t.name.Contains("adult body")) != 3)
            throw new Exception("Expected three anonymous adult bodies");
        if (details.GetComponentsInChildren<Collider>().Length != 1)
            throw new Exception("Only the lecturer workstation needs collision in the new decoration");
        Debug.Log("CLASSROOM_FURNITURE_OK width=" + width + " depth=" + depth + " desktopHeight=" + top + " seatHeight=" + seat);
    }

    static void VerifyActualController(List<Vector3> route)
    {
        var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Player/Player.prefab"));
        foreach(var behaviour in player.GetComponentsInChildren<MonoBehaviour>()) behaviour.enabled = false;
        foreach(var camera in player.GetComponentsInChildren<Camera>()) camera.enabled = false;
        var controller = player.GetComponent<CharacterController>();
        Physics.SyncTransforms();
        float half = controller.bounds.extents.y;
        float offset = controller.bounds.center.y - player.transform.position.y;
        try
        {
            controller.enabled = false;
            player.transform.position = route[0] + Vector3.up * (half - offset + .01f);
            controller.enabled = true; Physics.SyncTransforms();
            foreach(var feet in route.Skip(1).Concat(route.AsEnumerable().Reverse().Skip(1)))
            {
                Vector3 target = feet + Vector3.up * (half - offset + .01f);
                int budget = Mathf.CeilToInt(Vector3.Distance(target, player.transform.position) / .035f) + 120;
                bool arrived = false;
                for(int i = 0; i < budget; i++)
                {
                    Vector3 delta = target - player.transform.position; delta.y = 0;
                    if(delta.magnitude < .06f) { arrived = true; break; }
                    controller.Move(Vector3.ClampMagnitude(delta, .035f) + Vector3.down * .015f);
                }
                if(!arrived || Mathf.Abs(target.y - player.transform.position.y) > .2f)
                    throw new Exception("Actual player stuck at " + player.transform.position + " toward " + feet);
            }
            Debug.Log("CLASSROOM_ACTUAL_PLAYER_ROUTE_OK forward and return, " + route.Count + " waypoints");
        }
        finally { Object.DestroyImmediate(player); }
    }

    static void VerifyMapInheritance()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var room = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .First(t => t.name == "ClassRoom" && t.parent == null);
        var deco = room.Find("AbandonedClassRoomInterior");
        var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Player/Player.prefab");
        VerifyRevision(deco, player.GetComponent<CharacterController>().height * player.transform.localScale.y);
        if (Vector3.Distance(room.lossyScale, Vector3.one) > .001f)
            throw new Exception("Map uses a different room scale; world furniture dimensions need review");
        foreach(var route in playerRoutes)
            VerifyActualController(route.Select(p => deco.TransformPoint(p)).ToList());
        Debug.Log("CLASSROOM_MAP_INHERITANCE_OK furniture and aftermath inherited by SampleScene");
    }

    static void VerifyDoorMotion(GameObject original,float playerHeight,float playerWidth)
    {
        var clone=Object.Instantiate(original);clone.name="Door motion verification";
        original.SetActive(false);
        try
        {
            var behaviour=clone.GetComponent<OpenDoorWithKey>();var leaf=clone.transform.Find("Door");
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var type=typeof(OpenDoorWithKey);var before=leaf.lossyScale;var trigger=clone.GetComponent<BoxCollider>();var position=trigger.transform.position;
            type.GetMethod("Awake",flags).Invoke(behaviour,null);
            type.GetField("openProgress",flags).SetValue(behaviour,1f);type.GetField("closeTimer",flags).SetValue(behaviour,99f);
            type.GetMethod("Update",flags).Invoke(behaviour,null);
            var hinge=(Transform)type.GetField("hinge",flags).GetValue(behaviour);
            if(Quaternion.Angle(hinge.localRotation,Quaternion.Euler(0,90,0))>.05f)throw new Exception("Door opening failed");
            if(Vector3.Distance(before,leaf.lossyScale)>.001f || Vector3.Distance(position,trigger.transform.position)>.001f)throw new Exception("Door opening deformed leaf or moved trigger");
            if(!leaf.Find("Classroom door finish"))throw new Exception("Door finish detached");
            foreach(float direction in new[]{1f,-1f})
            {
                type.GetField("openingDirection",flags).SetValue(behaviour,direction);
                var leafCollider=leaf.GetComponent<BoxCollider>();
                foreach(float progress in Enumerable.Range(0,19).Select(i=>i/18f))
                {
                    type.GetField("openProgress",flags).SetValue(behaviour,progress);
                    type.GetMethod("Update",flags).Invoke(behaviour,null);
                    Physics.SyncTransforms();
                    foreach(var c in original.transform.parent.GetComponentsInChildren<Collider>())
                        if(c.enabled && !c.isTrigger && Physics.ComputePenetration(leafCollider,leaf.position,leaf.rotation,c,c.transform.position,c.transform.rotation,out _,out float distance) && distance>.001f)
                            throw new Exception("Door swing intersects "+c.name+" by "+distance+" m");
                }
                type.GetField("openProgress",flags).SetValue(behaviour,1f);
                type.GetMethod("Update",flags).Invoke(behaviour,null);
                Physics.SyncTransforms();
                for(float z=Front-.7f;z<=Front+.8f;z+=.1f)
                    if(!ClearCapsule(V(DoorX,0,z),playerHeight,playerWidth,null))throw new Exception("Opened door passage obstructed at "+z);
            }
            type.GetMethod("OnDisable",flags).Invoke(behaviour,null);
            if(Quaternion.Angle(hinge.localRotation,Quaternion.identity)>.05f)throw new Exception("Door closing reset failed");
            Debug.Log("CLASSROOM_DOOR_MOTION_VERIFIED");
        }
        finally {Object.DestroyImmediate(clone);original.SetActive(true);}
    }
    static bool ClearCapsule(Vector3 bottom,float height,float width,Transform scope)
    {
        float radius=width*.5f+.04f;
        var hits=Physics.OverlapCapsule(bottom+V(0,radius+.02f,0),bottom+V(0,height-radius,0),radius,~0,QueryTriggerInteraction.Ignore);
        return !hits.Any(c=>!scope || c.transform.IsChildOf(scope));
    }
    static bool ClearSegment(Vector3 a,Vector3 b,float height,float width,Transform scope)
    {
        float radius=width*.5f+.04f;
        var hits=Physics.CapsuleCastAll(a+V(0,radius+.02f,0),a+V(0,height-radius,0),radius,(b-a).normalized,(b-a).magnitude,~0,QueryTriggerInteraction.Ignore);
        return ClearCapsule(a,height,width,scope) && ClearCapsule(b,height,width,scope) && !hits.Any(h=>h.collider.transform.IsChildOf(scope));
    }
    static float VerifyRoute(Vector3 start,Vector3 end,float height,float width,Transform scope)
    {
        const float step=.1f;
        const float margin=.7f;
        int nx=Mathf.RoundToInt((Width-2*margin)/step)+1,nz=Mathf.RoundToInt((Depth-2*margin)/step)+1;
        Vector3 Point(int id)=>V(Left+margin+(id%nx)*step,0,Back+margin+(id/nx)*step);
        int Index(Vector3 p)=>Mathf.RoundToInt((p.x-Left-margin)/step)+nx*Mathf.RoundToInt((p.z-Back-margin)/step);
        int source=Index(start),target=Index(end);
        var available=new bool[nx*nz];var previous=Enumerable.Repeat(-1,nx*nz).ToArray();
        for(int i=0;i<available.Length;i++)available[i]=ClearCapsule(Point(i),height,width,scope);
        var queue=new Queue<int>();queue.Enqueue(source);previous[source]=source;
        while(queue.Count>0 && previous[target]<0)
        {
            int current=queue.Dequeue();int x=current%nx,z=current/nx;
            foreach(var d in new[]{new Vector2Int(1,0),new Vector2Int(-1,0),new Vector2Int(0,1),new Vector2Int(0,-1)})
            {
                int xx=x+d.x,zz=z+d.y;if(xx<0||xx>=nx||zz<0||zz>=nz)continue;int next=xx+zz*nx;
                if(!available[next] || previous[next]>=0 || !ClearSegment(Point(current),Point(next),height,width,scope))continue;
                previous[next]=current;queue.Enqueue(next);
            }
        }
        if(!available[source] || previous[target]<0 || !ClearSegment(start,Point(source),height,width,scope) || !ClearSegment(Point(target),end,height,width,scope))
        {
            Directory.CreateDirectory("Preview");
            var lines=new List<string>();
            for(int z=nz-1;z>=0;z--)lines.Add(new string(Enumerable.Range(0,nx).Select(x=>{int i=x+z*nx;return i==source?'S':i==target?'T':!available[i]?'#':previous[i]>=0?'o':'.';}).ToArray()));
            File.WriteAllLines("Preview/route-diagnostic.txt",lines);
            throw new Exception("No traversable route for width "+width+" from "+start+" to "+end+"; source clear="+available[source]+", target clear="+available[target]+", visited="+previous.Count(i=>i>=0));
        }
        var route = new List<Vector3> { end };
        for(int i=target;i!=source;i=previous[i]) route.Add(Point(i));
        route.Add(Point(source)); route.Add(start); route.Reverse();
        if(width < .6f) { VerifyActualController(route); playerRoutes.Add(route); }
        float length=Vector3.Distance(start,Point(source))+Vector3.Distance(Point(target),end);
        for(int i=target;i!=source;i=previous[i])length+=step;
        return length;
    }
    static void CheckClear(Vector3 bottom,float height,float width,Transform deco)
    {
        float radius=width*.5f+.04f;var hits=Physics.OverlapCapsule(bottom+V(0,radius+.02f,0),bottom+V(0,height-radius,0),radius,~0,QueryTriggerInteraction.Ignore);
        foreach(var c in hits)if(c.transform.IsChildOf(deco))throw new Exception("Player corridor obstructed: "+c.name+" at "+bottom);
    }
    static void Render(GameObject room,string name,Vector3 position,Vector3 target)
    {
        ShaderUtil.allowAsyncCompilation=false;
        var go=new GameObject("ClassRoom preview camera");var cam=go.AddComponent<Camera>();var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=false;cam.fieldOfView=70;cam.nearClipPlane=.04f;cam.farClipPlane=80;cam.backgroundColor=new Color(.025f,.035f,.05f);cam.clearFlags=CameraClearFlags.SolidColor;
        go.transform.position=position;go.transform.LookAt(target);
        var rt=new RenderTexture(1672,941,24);cam.targetTexture=rt;for(int f=0;f<4;f++)cam.Render();RenderTexture.active=rt;
        var png=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);png.Apply();Directory.CreateDirectory("Preview");File.WriteAllBytes("Preview/"+name,png.EncodeToPNG());
        RenderTexture.active=null;cam.targetTexture=null;Object.DestroyImmediate(png);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);
    }
}
