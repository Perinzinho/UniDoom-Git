// Editor-only: measure and validate using the actual player prefab in the isolated project.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BasementPassageBuilder
{
    const string RoomPath="Assets/prefabs/Scenario/Basement.prefab";
    const float ClearWidth=3f, ClearHeight=3f, Thickness=.3f;
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);

    public static void Verify()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var map=Object.FindObjectsByType<Transform>().First(t=>t.name=="Basement"&&t.parent==null).gameObject;
        FitKeyDoors(map);
        EditorSceneManager.SaveScene(map.scene,"Assets/Art/Basement/PassageValidation.unity");
        EditorSceneManager.SaveScene(map.scene,"Assets/Scenes/SampleScene.unity");
        VerifyRoom(map,"MAP");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath));
        room.transform.position=Vector3.zero;VerifyRoom(room,"PREFAB");
        BasementBuilder.VerifyAndRender();
        Debug.Log("BASEMENT_INSTALLED_PASSAGE_VERIFIED");
    }

    public static void Build()
    {
        var room=PrefabUtility.LoadPrefabContents(RoomPath);
        Fix(room);
        PrefabUtility.SaveAsPrefabAsset(room,RoomPath);PrefabUtility.UnloadPrefabContents(room);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var map=Object.FindObjectsByType<Transform>().First(t=>t.name=="Basement"&&t.parent==null).gameObject;
        Fix(map);
        FitKeyDoors(map);
        // Record only the affected basement objects; installation merges the specific YAML blocks.
        foreach(var t in map.GetComponentsInChildren<Transform>(true))
        {
            if(PrefabUtility.IsPartOfPrefabInstance(t))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(t);
                PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);
            }
        }
        EditorSceneManager.SaveScene(map.scene,"Assets/Art/Basement/PassageValidation.unity");
        VerifyRoom(map,"MAP");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath));
        instance.transform.position=Vector3.zero;VerifyRoom(instance,"PREFAB");
        AssetDatabase.SaveAssets();
        Debug.Log("BASEMENT_PASSAGE_BUILD_OK");
    }

    static float Axis(Transform t,Vector3 axis)=>t.TransformVector(axis).magnitude;
    static void FitKeyDoors(GameObject room)
    {
        foreach(var door in Object.FindObjectsByType<OpenDoorWithKey>())
        {
            if(door.name!="DoorPrefab (4)"&&door.name!="DoorPrefab (6)")continue;
            var area=room.transform.Find(door.name=="DoorPrefab (4)"?"PreBossArea":"BossRoom");
            var header=area.Find("Walls/Entrance_Wall_Header");
            var floor=Box(area.Find(door.name=="DoorPrefab (4)"?"Floor/Hall_Floor":"Floor/Main_Floor"));
            var leaf=door.transform.Find("Door");var collider=leaf.GetComponent<BoxCollider>();
            float width=header.localScale.x*Axis(area,Vector3.right)-.06f;
            var scale=door.transform.lossyScale;
            leaf.localScale=V(.175f/Mathf.Abs(scale.x),ClearHeight/Mathf.Abs(scale.y),width/Mathf.Abs(scale.z));
            var center=area.TransformPoint(V(header.localPosition.x,Top(floor,area),header.localPosition.z));
            leaf.position=center+Vector3.up*ClearHeight*.5f-leaf.TransformVector(collider.center);
            PrefabUtility.RecordPrefabInstancePropertyModifications(leaf);
            Debug.Log("PASSAGE_KEY_DOOR_FIT "+door.name+" width="+width+" height="+ClearHeight);
        }
    }
    static float Top(BoxCollider c,Transform parent)=>parent.InverseTransformPoint(c.transform.TransformPoint(c.center+Vector3.up*c.size.y*.5f)).y;
    static BoxCollider Box(Transform t)=>t.GetComponent<BoxCollider>();
    static void Opening(Transform area,string prefix,BoxCollider floor,float wallZ,float wallTop)
    {
        var sides=area.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith(prefix)&&!c.name.Contains("Header")).ToArray();
        var left=sides.OrderBy(c=>area.InverseTransformPoint(c.bounds.center).x).First();
        var right=sides.OrderBy(c=>area.InverseTransformPoint(c.bounds.center).x).Last();
        float l=left.transform.localPosition.x+left.transform.localScale.x*.5f;
        float r=right.transform.localPosition.x-right.transform.localScale.x*.5f;
        float center=(l+r)*.5f,width=Mathf.Max(r-l,ClearWidth/Axis(area,Vector3.right));
        float outerLeft=left.transform.localPosition.x-left.transform.localScale.x*.5f;
        float outerRight=right.transform.localPosition.x+right.transform.localScale.x*.5f;
        float floorY=Top(floor,area);
        float bottom=floorY+ClearHeight/Axis(area,Vector3.up);
        left.transform.localScale=V(center-width/2-outerLeft,wallTop,.5f);
        left.transform.localPosition=V((outerLeft+center-width/2)/2,wallTop/2,wallZ);
        right.transform.localScale=V(outerRight-center-width/2,wallTop,.5f);
        right.transform.localPosition=V((outerRight+center+width/2)/2,wallTop/2,wallZ);
        var header=area.GetComponentsInChildren<BoxCollider>(true).First(c=>c.name==prefix+"Header");
        header.gameObject.SetActive(true);
        header.transform.localScale=V(width,wallTop-bottom,.5f);
        header.transform.localPosition=V(center,(wallTop+bottom)/2,wallZ);
    }

    static void Fix(GameObject room)
    {
        Physics.SyncTransforms();
        var arena=room.transform.Find("PreBossArea");var boss=room.transform.Find("BossRoom");
        Opening(arena,"Entrance_Wall_",Box(arena.Find("Floor/Hall_Floor")),-15,4);
        Opening(arena,"Exit_Wall_",Box(arena.Find("Floor/Hall_Floor")),15,4);
        Opening(boss,"Entrance_Wall_",Box(boss.Find("Floor/Main_Floor")),-8.73f,5);
        var entry=arena.Find("Walls/Entrance_Wall_Header");
        float entryFloor=Top(Box(arena.Find("Floor/Hall_Floor")),arena);
        float entrySlab=Thickness/Axis(arena,Vector3.up);
        Make(arena,"Player entrance threshold",V(entry.localPosition.x,entryFloor-entrySlab/2,-15.55f),
            V(entry.localScale.x,entrySlab,1.8f),"Floor");
        Physics.SyncTransforms();
        var gate=room.GetComponentInChildren<BasementGateLayout>();gate.Fit();
        Vector3 exit=boss.InverseTransformPoint(arena.TransformPoint(gate.gateFrame.localPosition));
        var header=boss.Find("Walls/Entrance_Wall_Header");
        float cx=header.localPosition.x,endZ=header.localPosition.z;
        float floorY=Top(Box(boss.Find("Floor/Main_Floor")),boss);
        float wx=ClearWidth/Axis(boss,Vector3.right),wz=ClearWidth/Axis(boss,Vector3.forward);
        float height=ClearHeight/Axis(boss,Vector3.up),slab=Thickness/Axis(boss,Vector3.up);
        // The old fragments do not form a continuous floor. Keep them for reference, disabled.
        foreach(var t in boss.GetComponentsInChildren<Transform>(true))
            if(t.name.StartsWith("Hallway",StringComparison.OrdinalIgnoreCase))t.gameObject.SetActive(false);
        var root=boss.Find("Player sized corridor");
        if(!root){root=new GameObject("Player sized corridor").transform;root.SetParent(boss,false);}
        float xMin=cx-wx/2,xMax=exit.x+.35f, zMin=exit.z-wz/2,zMax=endZ+.3f;
        float xMid=(xMin+xMax)/2,zMid=(zMin+zMax)/2;
        Make(root,"Passage Floor X",V(xMid,floorY-slab/2,exit.z),V(xMax-xMin,slab,wz),"Floor");
        Make(root,"Passage Floor Z",V(cx,floorY-slab/2,zMid),V(wx,slab,zMax-zMin),"Floor");
        var roofX=Make(root,"Passage Ceiling X",V(xMid,floorY+height+slab/2,exit.z),V(xMax-xMin,slab,wz),"Ceiling");
        var roofZ=Make(root,"Passage Ceiling Z",V(cx,floorY+height+slab/2,zMid),V(wx,slab,zMax-zMin),"Ceiling");
        float wallX=Thickness/Axis(boss,Vector3.right),wallZ=Thickness/Axis(boss,Vector3.forward);
        Make(root,"Passage South wall",V(xMid,floorY+height/2,zMin-wallZ/2),V(xMax-xMin,height,wallZ),"Brick");
        Make(root,"Passage West wall",V(xMin-wallX/2,floorY+height/2,zMid),V(wallX,height,zMax-zMin),"Brick");
        float innerX=cx+wx/2,innerZ=exit.z+wz/2;
        Make(root,"Passage North wall",V((innerX+xMax)/2,floorY+height/2,innerZ+wallZ/2),V(xMax-innerX,height,wallZ),"Brick");
        Make(root,"Passage East wall",V(innerX+wallX/2,floorY+height/2,(innerZ+zMax)/2),V(wallX,height,zMax-innerZ),"Brick");
        CloneLights(boss.Find("Floor/Hallway_Ceiling"),roofX);
        CloneLights(boss.Find("Walls/Hallway_Ceiling"),roofZ);
        Debug.Log("PASSAGE_FIT "+room.name+" clearWidth="+ClearWidth+" clearHeight="+ClearHeight+" exit="+exit+" corner="+V(cx,floorY,exit.z));
    }

    static Transform Make(Transform parent,string name,Vector3 pos,Vector3 size,string material)
    {
        var existing=parent.Find(name);
        var go=existing?existing.gameObject:GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
        go.transform.localPosition=pos;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Basement/Materials/"+material+".mat");
        return go.transform;
    }
    static void CloneLights(Transform old,Transform ceiling)
    {
        if(ceiling.childCount>0)return;
        var fixtures=old.Find("Basement corridor fixtures");if(!fixtures)return;
        var copy=Object.Instantiate(fixtures.gameObject,ceiling,false);copy.SetActive(true);
        // Horizontal ceiling uses X for its length; rotate the existing fixture strip accordingly.
        if(ceiling.name.EndsWith("X"))copy.transform.localRotation=Quaternion.Euler(0,90,0);
    }

    static void VerifyRoom(GameObject room,string label)
    {
        Physics.SyncTransforms();var arena=room.transform.Find("PreBossArea");var boss=room.transform.Find("BossRoom");
        var gate=room.GetComponentInChildren<BasementGateLayout>();gate.Fit();
        var encounter=room.GetComponentInChildren<BasementArenaEncounter>();
        Vector3 exit=boss.InverseTransformPoint(gate.gateFrame.position);
        float cx=boss.Find("Walls/Entrance_Wall_Header").localPosition.x;
        float floor=Top(Box(boss.Find("Floor/Main_Floor")),boss);
        var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Player/Player.prefab"));
        foreach(var script in player.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
        foreach(var camera in player.GetComponentsInChildren<Camera>())camera.enabled=false;
        var cc=player.GetComponent<CharacterController>();Physics.SyncTransforms();
        float half=cc.bounds.extents.y,centerOffset=cc.bounds.center.y-player.transform.position.y;
        // Key doors are tested in their open state; the combat gate is tested closed first.
        foreach(var door in Object.FindObjectsByType<OpenDoorWithKey>())
        {
            var local=room.transform.InverseTransformPoint(door.transform.position);
            if(Mathf.Abs(local.x)<45&&Mathf.Abs(local.z)<45)
                foreach(var c in door.GetComponentsInChildren<Collider>())if(!c.isTrigger)c.enabled=false;
        }
        var entrance=arena.Find("Walls/Entrance_Wall_Header");
        float arenaFloor=Top(Box(arena.Find("Floor/Hall_Floor")),arena);
        Vector3 e=gate.gateFrame.localPosition;
        var points=new[]{arena.TransformPoint(V(entrance.localPosition.x,arenaFloor,-16)),arena.TransformPoint(V(entrance.localPosition.x,arenaFloor,-13)),
            arena.TransformPoint(V(e.x,arenaFloor,12)),arena.TransformPoint(V(e.x,arenaFloor,16)),
            boss.TransformPoint(V(cx,floor,exit.z)),boss.TransformPoint(V(cx,floor,-7)),boss.TransformPoint(V(cx,floor,-5))};
        Teleport(cc,points[2],half,centerOffset);
        bool blocked=!Walk(cc,points[3],half,centerOffset);
        if(!blocked)throw new Exception(label+": closed combat gate did not block player");
        encounter.gateLeaf.localPosition+=Vector3.up*1.08f;Physics.SyncTransforms();
        Teleport(cc,points[0],half,centerOffset);
        foreach(var point in points.Skip(1))
            if(!Walk(cc,point,half,centerOffset))throw new Exception(label+": player stuck going to "+point+" at "+cc.transform.position);
        foreach(var point in points.Reverse().Skip(1))
            if(!Walk(cc,point,half,centerOffset))throw new Exception(label+": player stuck on return route at "+cc.transform.position);
        Object.DestroyImmediate(player);
        Debug.Log("PASSAGE_"+label+"_TRAVERSAL_OK actual CharacterController: entrance, arena, gate, L turn, boss room and return; closed gate blocks.");
    }
    static void Teleport(CharacterController cc,Vector3 feet,float half,float offset)
    {cc.enabled=false;cc.transform.position=feet+Vector3.up*(half-offset+.01f);cc.enabled=true;Physics.SyncTransforms();}
    static bool Walk(CharacterController cc,Vector3 feet,float half,float offset)
    {
        Vector3 target=feet+Vector3.up*(half-offset+.01f);
        int steps=Mathf.CeilToInt(Vector3.Distance(cc.transform.position,target)/.035f)+120;
        for(int i=0;i<steps;i++)
        {
            Vector3 delta=target-cc.transform.position;delta.y=0;
            if(delta.magnitude<.06f)return Mathf.Abs(cc.transform.position.y-target.y)<.2f;
            cc.Move(Vector3.ClampMagnitude(delta,.035f)+Vector3.down*.015f);
        }
        return false;
    }
    public static void Audit()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Player/Player.prefab"));
        Physics.SyncTransforms();var cc=player.GetComponent<CharacterController>();
        Debug.Log("PASSAGE_PLAYER height="+cc.bounds.size.y+" diameter="+Mathf.Max(cc.bounds.size.x,cc.bounds.size.z)+" scale="+player.transform.lossyScale+" skin="+cc.skinWidth+" step="+cc.stepOffset);
        Object.DestroyImmediate(player);
        var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Scenario/Basement.prefab"));room.transform.position=Vector3.zero;
        Report(room,"PREFAB");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        Report(Object.FindObjectsByType<Transform>().First(t=>t.name=="Basement"&&t.parent==null).gameObject,"MAP");
        Debug.Log("PASSAGE_AUDIT_OK");
    }
    static void Report(GameObject room,string label)
    {
        Physics.SyncTransforms();
        foreach(var c in room.GetComponentsInChildren<Collider>())
        {
            if(c.isTrigger||c.transform.IsChildOf(room.transform.Find("PreBossArea/BasementArenaDetails"))||c.transform.IsChildOf(room.transform.Find("BossRoom/BasementBossDetails")))continue;
            var anchor=c.transform.IsChildOf(room.transform.Find("PreBossArea"))?room.transform.Find("PreBossArea"):room.transform.Find("BossRoom");
            Debug.Log("PASSAGE_"+label+" "+anchor.name+"/"+c.name+" local="+anchor.InverseTransformPoint(c.bounds.center)+" worldSize="+c.bounds.size+" localPos="+c.transform.localPosition+" localScale="+c.transform.localScale);
        }
    }
}
