using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using TotalChaosMaps;
using Object=UnityEngine.Object;

public static class CampusImmersionRevision
{
    const string Garden="Assets/_Project/Scenes/EntranceGarden";
    const string Hall="Assets/_Project/Scenes/EntranceHallway";
    const string Global="Assets/_Project/Global";
    static Material Load(string path)=>AssetDatabase.LoadAssetAtPath<Material>(path);
    static Transform Group(Transform parent,string name,Vector3 p)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Material Save(Material m)
    {
        string path=Hall+"/Materials/ImmersionRevision/"+m.name+".mat";Directory.CreateDirectory(Path.GetDirectoryName(path));
        var old=Load(path);if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static void Box(Transform parent,string name,Vector3 p,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());
    }
    static void Batch(Transform group)
    {
        var renderers=group.GetComponentsInChildren<MeshRenderer>().ToArray();
        foreach(var materialGroup in renderers.GroupBy(r=>r.sharedMaterial))
        {
            var mesh=new Mesh {name=group.parent.name+"_"+group.name+"_"+materialGroup.Key.name,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(materialGroup.Select(r=>new CombineInstance {mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=group.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray(),true,true);
            mesh.RecalculateBounds();string path=Hall+"/Meshes/ImmersionRevision/"+mesh.name+".asset";Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.CreateAsset(mesh,path);
            var t=Group(group,materialGroup.Key.name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=materialGroup.Key;
        }
        foreach(var r in renderers)Object.DestroyImmediate(r.gameObject);
    }
    public static void Build()
    {
        var path=Garden+"/Prefabs/EntranceGarden.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var art=root.transform.Find("CampusArchitecture");
            foreach(var light in art.GetComponentsInChildren<Light>())
            {
                if(light.name=="Globe practical") {light.intensity=10f;light.range=10.5f;PrefabUtility.RecordPrefabInstancePropertyModifications(light);}
                if(light.name=="Gate canopy practical"){light.intensity=8.2f;light.range=8.5f;}
            }
            art.GetComponentInChildren<TCRegionAtmosphere>().ambient=new Color(.115f,.145f,.195f);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        path=Hall+"/Prefabs/EntranceHallway.prefab";root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var art=root.transform.Find("CampusArchitecture");
            if(art.Find("Night revision/Enclosed courtyard scenery"))throw new Exception("Revision already applied; use current saved assets for validation.");
            var scenery=Group(art.Find("Night revision"),"Enclosed courtyard scenery",Vector3.zero);
            var wall=Save(new Material(Load(Global+"/Materials/CampusReference/Rain stained ivory plaster.mat")){name="Courtyard rain stained plaster"});
            // A low emission floor preserves texture in the unlit scenery without adding fill lights.
            wall.EnableKeyword("_EMISSION");wall.SetTexture("_EmissionMap",wall.GetTexture("_BaseMap"));wall.SetColor("_EmissionColor",new Color(.025f,.033f,.043f));
            var sky=Save(new Material(Shader.Find("Universal Render Pipeline/Unlit")){name="Enclosed cold night sky"});
            var vista=Load("Assets/_Project/Scenes/Library/Materials/DecayRevision/Sky.mat");
            sky.SetTexture("_BaseMap",vista.GetTexture("_BaseMap"));sky.SetColor("_BaseColor",new Color(.075f,.105f,.15f));
            var stone=Load(Global+"/Materials/CampusReference/Damp concrete edging.mat");
            foreach(int side in new[]{-1,1})
            {
                var court=Group(scenery,"Courtyard "+side,Vector3.zero);
                var shell=Group(court,"Continuous opaque enclosure",Vector3.zero);
                // Ground overlaps the existing lawn; all five outward directions have an opaque backing.
                Box(shell,"Opaque foundation",V(side*17,-.12f,14.5f),V(26.6f,.16f,35.7f),stone);
                Box(shell,"Outer night horizon",V(side*30.15f,6,14.5f),V(.3f,12.4f,35.7f),sky);
                foreach(float z in new[]{-3.2f,32.2f})Box(shell,"Night horizon end return",V(side*17,6,z),V(26.6f,12.4f,.3f),sky);
                Box(shell,"Opaque night sky overhead",V(side*17,12.1f,14.5f),V(26.6f,.3f,35.7f),sky);
                Batch(shell);
                var boundary=Group(court,"Weathered courtyard boundary",Vector3.zero);
                Box(boundary,"Garden perimeter plaster",V(side*15,1.85f,14.4f),V(.32f,3.8f,30.6f),wall);
                Box(boundary,"Garden perimeter coping",V(side*15,3.79f,14.4f),V(.43f,.13f,30.6f),stone);
                foreach(float z in new[]{-.8f,29.6f})
                {
                    Box(boundary,"Closed courtyard end wall",V(side*9.45f,1.85f,z),V(11.45f,3.8f,.32f),wall);
                    Box(boundary,"End wall coping",V(side*9.45f,3.79f,z),V(11.45f,.13f,.43f),stone);
                }
                for(int i=0;i<9;i++)Box(boundary,"Masonry pier",V(side*14.79f,2f,.6f+i*3.45f),V(.36f,4.1f,.4f),wall);
                Batch(boundary);
                var fern=AssetDatabase.LoadAssetAtPath<GameObject>(Garden+"/Prefabs/CampusReference/CampusFernBed.prefab");
                var lamp=AssetDatabase.LoadAssetAtPath<GameObject>(Garden+"/Prefabs/CampusReference/CampusGlobeLamp.prefab");
                for(int i=0;i<10;i++)
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(fern,court);instance.name="Overgrown courtyard planting";
                    instance.transform.localPosition=V(side*(13.8f-(i%3)*.35f),0,1+i*2.9f);instance.transform.localRotation=Quaternion.Euler(0,i*61+side*37,0);instance.transform.localScale=Vector3.one*(1.15f+(i%3)*.15f);
                    foreach(var collider in instance.GetComponentsInChildren<Collider>())collider.enabled=false;
                }
                foreach(float z in new[]{4f,14.4f,25f})
                {
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(lamp,court);instance.name="Courtyard wall practical";instance.transform.localPosition=V(side*13.4f,0,z);
                    foreach(var collider in instance.GetComponentsInChildren<Collider>())collider.enabled=false;
                    foreach(var light in instance.GetComponentsInChildren<Light>()){light.intensity=6.5f;light.range=10;light.shadows=LightShadows.None;PrefabUtility.RecordPrefabInstancePropertyModifications(light);}
                }
            }
            foreach(var light in art.GetComponentsInChildren<Light>())if(light.name=="Diffuse courtyard moonlight")light.intensity=200;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();CampusImmersionValidation.Run();Debug.Log("CAMPUS_IMMERSION_BUILD_PASS");
    }
}
