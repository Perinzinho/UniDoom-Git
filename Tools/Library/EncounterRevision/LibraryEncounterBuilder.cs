using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Object=UnityEngine.Object;

public static class LibraryEncounterBuilder
{
    public const string Art="Assets/_Project/Scenes/Library";
    public const string Output=Art+"/Prefabs/EncounterRevision";
    public const string Navigation=Art+"/Navigation/EncounterRevision";
    static readonly List<Transform> shelves=new List<Transform>();
    static int shelfIndex;
    static Material iron,wood;
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Transform Group(Transform p,string n,Vector3 v){var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=v;return t;}
    static Transform Module(Transform p,string id,string name)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/Modules/"+id+".prefab"),p);
        PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);go.name=name;go.transform.localPosition=Vector3.zero;return go.transform;
    }
    public static void Build()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use the isolated project");
        foreach(var p in new[]{Output,Navigation,"Evidence/Encounter"})Directory.CreateDirectory(p);AssetDatabase.Refresh();
        iron=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/DecayRevision/Iron.mat");wood=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/DecayRevision/Wood.mat");
        ZombieVariant();var root=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try
        {
            var art=root.transform.Find("Abandoned Library Interior");if(art.Find("Key retrieval encounter"))throw new Exception("Revision already built");
            StandardDoor(art);
            var encounter=Group(art,"Key retrieval encounter",Vector3.zero);
            var floor=Group(encounter,"Walkable library floor",V(0,-.06f,0));floor.gameObject.AddComponent<BoxCollider>().size=V(24.3f,.10f,39.4f);
            var desks=art.Cast<Transform>().Where(t=>t.name.StartsWith("Abandoned reading desk ")).OrderBy(t=>int.Parse(t.name.Split(' ').Last())).ToArray();
            var positions=new[]{V(-2.7f,0,15),V(-2.7f,0,5.4f),V(-2.7f,0,-4.8f),V(2.8f,0,15),V(2.8f,0,5.6f),V(2.8f,0,-5.1f),V(2,0,-14.5f),V(9.2f,0,15.8f),V(9.1f,0,4.4f),V(9.2f,0,-5),V(-2.2f,0,-16.8f)};
            for(int i=0;i<desks.Length;i++){desks[i].localPosition=positions[i];desks[i].localRotation=Quaternion.Euler(0,i%3==0?-10:i%3==1?7:15,0);}
            shelves.Clear();shelfIndex=0;
            foreach(Transform t in art)if(t.Find("Warped timber bookcase")&&Mathf.Abs(t.localPosition.x)<10.5f&&t.localPosition.z>-18.8f)shelves.Add(t);
            if(shelves.Count<18)throw new Exception("Expected reusable interior bookshelves");
            Barrier(encounter,"Collapsed reference stacks",10,-12.1f,new[]{"case","case","table","case","case","table","case","case","cabinet","cabinet"},new[]{1.7f,1.7f,3.2f,1.7f,1.7f,3.2f,1.7f,1.7f,1.35f,1.35f},false);
            Barrier(encounter,"Abandoned evacuation barricade",0,-6.4f,new[]{"table","case","case","cabinet","case","case","table","case","case"},new[]{3.2f,1.7f,1.7f,1.9f,1.7f,1.7f,3.2f,1.7f,1.7f},true);
            Barrier(encounter,"Archive collapse",-10,-12.1f,new[]{"cabinet","cabinet","case","table","case","case","table","case","case","case"},new[]{1.35f,1.35f,1.7f,3.2f,1.7f,1.7f,3.2f,1.7f,1.7f,1.7f},false);
            for(int i=shelfIndex;i<shelves.Count;i++)shelves[i].gameObject.SetActive(false);
            Objective(encounter,desks[10]);
            var surface=art.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
            surface.overrideVoxelSize=true;surface.voxelSize=.09f;surface.overrideTileSize=true;surface.tileSize=128;surface.minRegionArea=.3f;
            var moving=art.Find("Simple library entrance/Unlatched swing door/Door");var modifier=moving.gameObject.AddComponent<NavMeshModifier>();modifier.ignoreFromBuild=true;
            Physics.SyncTransforms();surface.BuildNavMesh();if(!surface.navMeshData)throw new Exception("NavMesh bake failed");
            var data=surface.navMeshData;surface.RemoveData();data.name="LibraryObstacleNavigation";AssetDatabase.CreateAsset(data,Navigation+"/LibraryObstacleNavigation.asset");surface.navMeshData=data;surface.AddData();
            Encounters(encounter);PrefabUtility.SaveAsPrefabAsset(root,LibraryBuilder.Prefab);surface.RemoveData();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("LIBRARY_ENCOUNTER_BUILD_PASS");LibraryEncounterVerifier.Run();LibraryEncounterPlay.Run();
    }
    static void StandardDoor(Transform art)
    {
        var cylinder=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab");
        var source=cylinder.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Service bay 36").GetComponentInChildren<OpenDoorWithKey>(true);
        var leaf=(Transform)new SerializedObject(source).FindProperty("door").objectReferenceValue;
        var target=art.Find("Simple library entrance/Unlatched swing door/Door");target.GetComponent<MeshRenderer>().enabled=false;
        foreach(Transform child in target)child.gameObject.SetActive(false);
        var copy=Object.Instantiate(leaf.gameObject,target).transform;copy.name="Cylinder library door model";copy.localPosition=Vector3.zero;copy.localRotation=Quaternion.identity;copy.localScale=Vector3.one;
        var collider=copy.GetComponent<BoxCollider>();Bounds body=collider?new Bounds(collider.center,collider.size):LibraryBuilder.BoundsIn(copy,copy.GetComponentsInChildren<Renderer>());
        foreach(var behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(behaviour);
        foreach(var c in copy.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
        foreach(var rb in copy.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(rb);
        var scale=V(1/body.size.x,1/body.size.y,1/body.size.z);copy.localScale=scale;copy.localPosition=-Vector3.Scale(body.center,scale);
        File.WriteAllText("Evidence/Encounter/door-source.txt","Cylinder.prefab / Service bay 36 / "+leaf.name+"\nExisting Cylinder meshes and materials reused, normalized to the library leaf collider.\n");
    }
    static void Barrier(Transform parent,string name,float z,float start,string[] kinds,float[] widths,bool reverse)
    {
        var row=Group(parent,name,V(0,0,z));float cursor=start;
        for(int i=0;i<kinds.Length;i++)
        {
            var segment=Group(row,kinds[i]+" obstacle "+i,Vector3.zero);Transform model;
            if(kinds[i]=="case")
            {
                model=shelves[shelfIndex++];model.SetParent(segment,false);model.localPosition=Vector3.zero;model.localRotation=Quaternion.Euler(0,reverse?0:180,0);
                foreach(var c in model.GetComponentsInChildren<Collider>())c.enabled=false;
            }
            else
            {
                model=Module(segment,kinds[i]=="table"?"wooden_table_02":"drawer_cabinet",kinds[i]=="table"?"Overturned solid reading table":"Swollen catalogue cabinet");
                model.localRotation=kinds[i]=="table"?Quaternion.Euler(90,0,0):Quaternion.Euler(0,reverse?0:180,0);model.localScale=kinds[i]=="table"?V(2.834f,1.04f,2.08f):Vector3.one;
                foreach(var r in model.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(kinds[i]=="table"?Art+"/Materials/DecayRevision/Table.mat":Art+"/Materials/drawer_cabinet.mat")).ToArray();
            }
            var b=LibraryBuilder.BoundsIn(segment,model.GetComponentsInChildren<Renderer>(true));model.localScale=Vector3.Scale(model.localScale,V(widths[i]/b.size.x,1,1));
            b=LibraryBuilder.BoundsIn(segment,model.GetComponentsInChildren<Renderer>(true));model.localPosition+=V(cursor-b.min.x,.015f-b.min.y,-b.center.z);
            b=LibraryBuilder.BoundsIn(segment,model.GetComponentsInChildren<Renderer>(true));var box=segment.gameObject.AddComponent<BoxCollider>();box.center=b.center;box.size=b.size;
            cursor+=widths[i]-.025f;
        }
    }
    static void ZombieVariant()
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Enemy/Enemy.prefab"));go.name="LibraryZombie";go.transform.position=Vector3.zero;
        var enemy=go.GetComponent<Enemy>();var so=new SerializedObject(enemy);so.FindProperty("moveSpeed").floatValue=0;so.ApplyModifiedPropertiesWithoutUndo();
        var rb=go.GetComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        var agent=go.AddComponent<NavMeshAgent>();agent.radius=.45f;agent.height=2;agent.baseOffset=1;agent.speed=1.8f;agent.acceleration=7;agent.angularSpeed=180;agent.stoppingDistance=.9f;
        go.AddComponent<LibraryZombieNavigator>();PrefabUtility.SaveAsPrefabAsset(go,Output+"/LibraryZombie.prefab");Object.DestroyImmediate(go);
    }
    static void Objective(Transform encounter,Transform desk)
    {
        var table=desk.Find("Worn reading table");var b=LibraryBuilder.BoundsIn(table,table.GetComponentsInChildren<Renderer>());
        var key=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Global/Prefabs/Utils/Key02.prefab"),encounter);key.name="Objective Key02";
        key.transform.position=table.TransformPoint(V(b.max.x-.08f,b.max.y+.12f,b.center.z));NormalizeKey(encounter,key);
        var lamp=desk.Find("Dead desk lamp");var light=Group(lamp,"Archive objective task light",V(-.10f,.28f,0)).gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.73f,.45f);light.intensity=1.15f;light.range=3;
        var bulb=GameObject.CreatePrimitive(PrimitiveType.Cube);bulb.name="Surviving objective lamp bulb";Object.DestroyImmediate(bulb.GetComponent<Collider>());bulb.transform.SetParent(lamp,false);bulb.transform.localPosition=V(-.11f,.31f,.01f);bulb.transform.localScale=V(.13f,.02f,.08f);bulb.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/DecayRevision/Amber.mat");
    }
    static void NormalizeKey(Transform encounter,GameObject key)
    {
        var sprite=key.GetComponent<SpriteRenderer>().sprite;float scale=.42f/sprite.bounds.size.y;key.transform.localScale=Vector3.one*scale;
        var position=key.transform.localPosition;position.y=.95f-sprite.bounds.min.y*scale;key.transform.localPosition=position;
        var box=key.GetComponent<BoxCollider>();box.size=Vector3.one*(2.2f/scale);box.center=key.transform.InverseTransformPoint(encounter.TransformPoint(V(position.x,1.1f,position.z)));
    }
    public static void Recheck()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use isolated project");
        var root=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try{var encounter=root.transform.Find("Abandoned Library Interior/Key retrieval encounter");NormalizeKey(encounter,encounter.Find("Objective Key02").gameObject);PrefabUtility.SaveAsPrefabAsset(root,LibraryBuilder.Prefab);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("LIBRARY_ENCOUNTER_BUILD_PASS");LibraryEncounterVerifier.Run();LibraryEncounterPlay.Run();
    }
    public static void RecheckPickup()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use isolated project");
        var root=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try{var encounter=root.transform.Find("Abandoned Library Interior/Key retrieval encounter");NormalizeKey(encounter,encounter.Find("Objective Key02").gameObject);PrefabUtility.SaveAsPrefabAsset(root,LibraryBuilder.Prefab);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("LIBRARY_ENCOUNTER_BUILD_PASS");LibraryEncounterPlay.Run();
    }
    static void Encounters(Transform root)
    {
        var director=root.gameObject.AddComponent<LibraryEncounterDirector>();director.zombiePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Output+"/LibraryZombie.prefab");director.objectiveKey=root.Find("Objective Key02").gameObject;
        var trigger=root.gameObject.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=V(0,1.5f,0);trigger.size=V(24.3f,4,39.3f);
        var points=Group(root,"Zombie spawn points",Vector3.zero);
        Transform[] Markers(string name,Vector3[] locations)=>locations.Select((p,i)=>Group(points,name+" "+(i+1),p)).ToArray();
        director.waves=new[]{
            new LibraryEncounterDirector.Wave{name="Reference stacks ambush",activationBounds=new Bounds(V(0,1.5f,7),V(24.3f,4,2)),points=Markers("Advance A",new[]{V(-8.5f,0,3),V(-7.7f,0,-2.5f)})},
            new LibraryEncounterDirector.Wave{name="Archive approach",activationBounds=new Bounds(V(0,1.5f,-3),V(24.3f,4,2)),points=Markers("Advance B",new[]{V(8.2f,0,-13),V(5.4f,0,-6.8f)})},
            new LibraryEncounterDirector.Wave{name="Key02 return pressure",afterKeyPickup=true,points=Markers("Return",new[]{V(7.0f,0,-6.9f),V(-7.7f,0,4.8f),V(7.9f,0,13)})}
        };
        foreach(var p in director.waves[2].points)p.localRotation=Quaternion.Euler(0,180,0);
    }
}
