using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using Object=UnityEngine.Object;

public static class LibraryPressureVerifier
{
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static void Require(bool b,string m){if(!b)throw new Exception(m);}
    public static Vector3[] Path(Transform art,Vector3 from,Vector3 to)
    {
        Require(NavMesh.SamplePosition(art.TransformPoint(from),out var a,2,NavMesh.AllAreas),"No navmesh at "+from);
        Require(NavMesh.SamplePosition(art.TransformPoint(to),out var b,2,NavMesh.AllAreas),"No navmesh at "+to);
        var path=new NavMeshPath();Require(NavMesh.CalculatePath(a.position,b.position,NavMesh.AllAreas,path)&&path.status==NavMeshPathStatus.PathComplete,"Incomplete path "+from+" -> "+to);
        return path.corners;
    }
    public static void Run()
    {
        Directory.CreateDirectory("Evidence/Pressure");EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LibraryBuilder.Prefab));root.transform.localScale=LibraryBuilder.MapScale;root.transform.position=-Vector3.Scale(LibraryBuilder.FloorOrigin,LibraryBuilder.MapScale);
        Inspect(root);var art=root.transform.Find("Abandoned Library Interior");var surface=art.GetComponent<NavMeshSurface>();surface.RemoveData();surface.AddData();Physics.SyncTransforms();
        var route=Path(art,V(10.1f,0,18.2f),V(-.1f,0,-16.5f));float length=0;
        for(int i=1;i<route.Length;i++)length+=Vector3.Distance(route[i-1],route[i]);Require(length>65,"Route still permits a straight shortcut: "+length);
        for(int i=1;i<route.Length;i++)
        {
            int steps=Mathf.CeilToInt(Vector3.Distance(route[i-1],route[i])/.20f);
            for(int j=0;j<=steps;j++)
            {
                var p=Vector3.Lerp(route[i-1],route[i],(float)j/steps);var hits=Physics.OverlapCapsule(p+Vector3.up*.45f,p+Vector3.up*1.85f,.30f,~0,QueryTriggerInteraction.Ignore).Where(c=>!c.GetComponentInParent<OpenDoorWithKey>()).ToArray();
                Require(hits.Length==0,"Route intersects "+string.Join(",",hits.Select(h=>h.name))+" at "+art.InverseTransformPoint(p));
            }
        }
        var director=root.GetComponentInChildren<LibraryEncounterDirector>();Require(director&&director.objectiveKey&&director.objectiveKey.CompareTag("Key02"),"Objective Key02 missing");
        Require(director.waves.Sum(w=>w.points.Length)==16,"Expected sixteen configured spawns");
        Require(director.blackoutLights.Length==3&&director.blackoutTubes.Length==9&&director.blackoutDuration==8,"Invalid simple blackout configuration");
        Require(art.Find("Key retrieval encounter/Narrow catalogue chicanes").childCount==6,"Expected six physical choke groups");
        foreach(var point in director.waves.SelectMany(w=>w.points))
        {
            var local=art.InverseTransformPoint(point.position);Path(art,local,V(-.1f,0,-16.5f));
            var hit=Physics.OverlapCapsule(point.position+Vector3.up*.55f,point.position+Vector3.up*1.5f,.45f,~0,QueryTriggerInteraction.Ignore);Require(hit.Length==0,"Spawn inside obstacle: "+point.name+" / "+string.Join(",",hit.Select(c=>c.name)));
        }
        File.WriteAllText("Evidence/Pressure/route.json",JsonUtility.ToJson(new RouteData{length=length,points=route.Select(p=>art.InverseTransformPoint(p)).ToArray()},true));
        var report="PASS\nObstacle route length: "+length.ToString("F1")+" m, with capsule clearance.\nSixteen spawn markers have complete paths to the objective and clear capsule volumes.\nPersistent NavMesh, objective Key02, Cylinder door meshes and serialized references verified.\n";
        File.WriteAllText("Evidence/Pressure/validation.txt",report);Debug.Log("LIBRARY_PRESSURE_VERIFY_PASS");
        PressureCapture.Render(root,art,"Library");surface.RemoveData();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");var inMap=GameObject.Find("Library");Inspect(inMap);var mapArt=inMap.transform.Find("Abandoned Library Interior");foreach(var s in inMap.GetComponentsInChildren<NavMeshSurface>()){s.RemoveData();s.AddData();}Physics.SyncTransforms();Path(mapArt,V(10.1f,0,18.2f),V(-.1f,0,-16.5f));PressureCapture.Render(inMap,mapArt,"InMap");
        File.WriteAllLines("Evidence/Pressure/dependencies.txt",AssetDatabase.GetDependencies(LibraryBuilder.Prefab,true));Debug.Log("LIBRARY_PRESSURE_MAP_PASS");
    }
    static void Inspect(GameObject root)
    {
        Require(root,"Missing room");
        foreach(var t in root.GetComponentsInChildren<Transform>(true))Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Missing script "+t.name);
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(!r.enabled)continue;var f=r.GetComponent<MeshFilter>();if(f)Require(f.sharedMesh,"Missing mesh "+r.name);
            foreach(var m in r.sharedMaterials)Require(m&&m.shader&&m.shader.isSupported,"Invalid material "+r.name);
        }
        foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var it=new SerializedObject(c).GetIterator();while(it.Next(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&!it.objectReferenceValue&&!it.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))throw new Exception("Missing reference "+c.name+" / "+it.propertyPath);
        }
    }
    [Serializable] public class RouteData{public float length;public Vector3[] points;}
}
