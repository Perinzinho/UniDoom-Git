using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;
using Object=UnityEngine.Object;

public static class LibraryPressureBuilder
{
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Transform Group(Transform p,string n,Vector3 v){var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=v;return t;}
    public static void Build()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use isolated project");
        Directory.CreateDirectory("Evidence/Pressure");
        var root=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try
        {
            var art=root.transform.Find("Abandoned Library Interior");
            var encounter=art.Find("Key retrieval encounter");
            if(encounter.Find("Narrow catalogue chicanes"))throw new Exception("Revision already built");
            var chicanes=Group(encounter,"Narrow catalogue chicanes",Vector3.zero);
            var sourceCase=encounter.Find("Collapsed reference stacks/case obstacle 0");
            for(int i=0;i<3;i++)
            {
                bool left=i==1;float z=10-i*10;
                var gate=Group(chicanes,"Catalogue choke "+(i+1),V(0,0,z));
                // Extend each old barrier, then force a second turn around the shelving opposite it.
                for(int j=0;j<2;j++)Cabinet(gate,(left?-1:1)*((left?7.125f:7.70f)+j*1.48f),1.5f);
                var lip=Group(chicanes,"Blind return shelving "+(i+1),V(0,0,z-2.9f));
                for(int j=0;j<2;j++)
                {
                    var segment=Object.Instantiate(sourceCase.gameObject,lip).transform;segment.name="Displaced reference stack "+(j+1);
                    segment.localPosition=Vector3.zero;segment.localRotation=Quaternion.Euler(0,left?0:180,0);
                    // The copied group already has a tight collider; use its complete bounds to ground it.
                    var b=LibraryBuilder.BoundsIn(lip,segment.GetComponentsInChildren<Renderer>(true));
                    segment.localPosition+=V((left?-1:1)*(9.5f+j*1.6f)-b.center.x,.015f-b.min.y,-b.center.z);
                }
            }
            var director=encounter.GetComponent<LibraryEncounterDirector>();director.spawnInterval=1.1f;
            var points=encounter.Find("Zombie spawn points");
            Transform[] Add(string name,Vector3[] positions)=>positions.Select((p,i)=>Group(points,name+" "+(i+1),p)).ToArray();
            director.waves[0].points=director.waves[0].points.Concat(Add("Advance A reinforcement",new[]{V(-8,0,5.4f),V(5.9f,0,3)})).ToArray();
            director.waves[1].points=director.waves[1].points.Concat(Add("Advance B reinforcement",new[]{V(-5.6f,0,-6.5f),V(6,0,-15),V(-7.7f,0,-14.5f)})).ToArray();
            director.waves[2].points=director.waves[2].points.Concat(Add("Return reinforcement",new[]{V(-8.6f,0,-6),V(5.8f,0,3),V(-7.5f,0,14.5f),V(4.6f,0,17.4f)})).ToArray();
            foreach(var t in director.waves[2].points)t.localRotation=Quaternion.Euler(0,180,0);
            // Only three real ceiling circuits fail. Windows, other lights and the objective lamp remain.
            var circuits=new[]{0,5,6}.Select(i=>art.Find("Damaged fluorescent fixture "+i)).ToArray();
            director.blackoutLights=circuits.SelectMany(t=>t.GetComponentsInChildren<Light>()).ToArray();
            director.blackoutTubes=circuits.SelectMany(t=>t.GetComponentsInChildren<Renderer>()).Where(r=>r.transform.parent&&r.transform.parent.name=="Fluorescent tube").ToArray();
            director.blackoutDuration=8;
            if(director.blackoutLights.Length!=3||director.blackoutTubes.Length!=9)throw new Exception("Missing blackout circuit");
            var surface=art.GetComponent<NavMeshSurface>();var saved=surface.navMeshData;
            Physics.SyncTransforms();surface.BuildNavMesh();var generated=surface.navMeshData;surface.RemoveData();
            EditorUtility.CopySerialized(generated,saved);saved.name="LibraryObstacleNavigation";EditorUtility.SetDirty(saved);
            surface.navMeshData=saved;Object.DestroyImmediate(generated);surface.AddData();
            PrefabUtility.SaveAsPrefabAsset(root,LibraryBuilder.Prefab);surface.RemoveData();
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("LIBRARY_PRESSURE_BUILD_PASS");LibraryPressureVerifier.Run();LibraryPressurePlay.Run();
    }
    static void Cabinet(Transform parent,float x,float width)
    {
        var g=Group(parent,"Displaced catalogue cabinet",Vector3.zero);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LibraryBuilder.Art+"/Prefabs/Modules/drawer_cabinet.prefab"),g);
        PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var model=go.transform;model.localPosition=Vector3.zero;model.localRotation=Quaternion.identity;
        foreach(var c in go.GetComponentsInChildren<Collider>())c.enabled=false;
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>AssetDatabase.LoadAssetAtPath<Material>(LibraryBuilder.Art+"/Materials/drawer_cabinet.mat")).ToArray();
        var b=LibraryBuilder.BoundsIn(g,go.GetComponentsInChildren<Renderer>(true));model.localScale=Vector3.Scale(model.localScale,V(width/b.size.x,1,1));
        b=LibraryBuilder.BoundsIn(g,go.GetComponentsInChildren<Renderer>(true));model.localPosition+=V(x-b.center.x,.015f-b.min.y,-b.center.z);
        b=LibraryBuilder.BoundsIn(g,go.GetComponentsInChildren<Renderer>(true));var collider=g.gameObject.AddComponent<BoxCollider>();collider.center=b.center;collider.size=b.size;
    }
    public static void Recheck()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use isolated project");
        Debug.Log("LIBRARY_PRESSURE_BUILD_PASS");LibraryPressureVerifier.Run();LibraryPressurePlay.Run();
    }
}
