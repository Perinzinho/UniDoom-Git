using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static partial class CampusLibraryCirculation
{
    static bool Within(Bounds outer,Bounds inner){outer.Expand(.008f);return outer.Contains(inner.min)&&outer.Contains(inner.max);}
    static Bounds Area(Vector3 min,Vector3 max){var b=new Bounds();b.SetMinMax(min,max);return b;}
    static bool Under(Transform t,HashSet<Transform> roots){for(var p=t;p;p=p.parent)if(roots.Contains(p))return true;return false;}
    static int ModifyNative(GameObject root,Func<MeshFilter,Bounds,bool> select,Func<Vector3,Bounds,Vector3> map,Quaternion rotation=default)
    {
        int count=0;bool rotate=rotation!=default&&rotation!=Quaternion.identity;
        foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if(!mf.sharedMesh)continue;string path=AssetDatabase.GetAssetPath(mf.sharedMesh),name=Path.GetFileName(path);if(!path.StartsWith("Assets/_Project/Scenes/CampusLibrary/Meshes/")||!(name.StartsWith("CampusLibrary_")||name.StartsWith("NorthWing")||name.StartsWith("Contact supports")))continue;
            var mesh=mf.sharedMesh;var vv=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;bool changed=false;
            foreach(var part in Parts(mesh)){var b=PartBounds(mf,vv,part);if(!select(mf,b))continue;count++;changed=true;foreach(int i in part){vv[i]=mf.transform.InverseTransformPoint(map(mf.transform.TransformPoint(vv[i]),b));if(rotate){if(normals.Length==vv.Length)normals[i]=mf.transform.InverseTransformDirection(rotation*mf.transform.TransformDirection(normals[i])).normalized;if(tangents.Length==vv.Length){var tangent=tangents[i];var n=mf.transform.InverseTransformDirection(rotation*mf.transform.TransformDirection(new Vector3(tangent.x,tangent.y,tangent.z))).normalized;tangents[i]=new Vector4(n.x,n.y,n.z,tangent.w);}}}}
            if(changed){mesh.vertices=vv;if(rotate){mesh.normals=normals;mesh.tangents=tangents;}mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);}
        }return count;
    }
    static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Global/Materials/CampusInterior/"+name+".mat")??throw new Exception("Missing material "+name);
    static Mesh Cuboid(string name,Vector3 size)
    {
        string path="Assets/_Project/Scenes/CampusLibrary/Meshes/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved)return saved;var go=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(go);mesh.name=name;var vv=mesh.vertices;var nn=mesh.normals;var uv=new Vector2[vv.Length];for(int i=0;i<vv.Length;i++){vv[i]=Vector3.Scale(vv[i],size);var v=vv[i];uv[i]=Mathf.Abs(nn[i].y)>.5f?new Vector2(v.x,v.z):Mathf.Abs(nn[i].x)>.5f?new Vector2(v.z,v.y):new Vector2(v.x,v.y);}mesh.vertices=vv;mesh.uv=uv;mesh.RecalculateTangents();mesh.RecalculateBounds();mesh.UploadMeshData(false);AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static GameObject Solid(Transform parent,string name,Vector3 position,Vector3 size,Material material,string meshName=null)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;go.AddComponent<MeshFilter>().sharedMesh=Cuboid((meshName??name)+" mesh",size);go.AddComponent<MeshRenderer>().sharedMaterial=material;go.AddComponent<BoxCollider>().size=size;return go;
    }
    static void MetricUV(Mesh mesh){var vv=mesh.vertices;var nn=mesh.normals;var uv=new Vector2[vv.Length];for(int i=0;i<vv.Length;i++){var v=vv[i];uv[i]=Mathf.Abs(nn[i].y)>.5f?new Vector2(v.x,v.z):Mathf.Abs(nn[i].x)>.5f?new Vector2(v.z,v.y):new Vector2(v.x,v.y);}mesh.uv=uv;mesh.RecalculateTangents();}
    static void FitWalls()
    {
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);try
        {
            if(root.transform.Find("Library wall junction closures"))throw new Exception("Wall closures already applied");
            int panes=ModifyNative(root,(mf,b)=>mf.GetComponent<Renderer>().sharedMaterials.Any(m=>m.name=="Night blue glazing")&&Mathf.Abs(Mathf.Abs(b.center.x)-28.15f)<.02f&&b.center.z>12&&Mathf.Abs(b.size.z-2.88f)<.02f,(v,b)=>new Vector3(v.x,v.y,b.center.z+(v.z-b.center.z)*(2.92f/2.88f)+.05f));if(panes!=12)throw new Exception("Expected twelve north wing window panes");
            var group=new GameObject("Library wall junction closures").transform;group.SetParent(root.transform,false);var plaster=Mat("Ivory institutional plaster");var steel=Mat("Brushed dark steel");
            Solid(group,"Study west wall junction",new Vector3(-27.97f,2.3f,24),new Vector3(.46f,4.6f,.18f),plaster);
            Solid(group,"Study front upper closure",new Vector3(-19.585f,3.845f,24),new Vector3(17.23f,1.53f,.16f),plaster);
            foreach(float x in new[]{-19.45f,-11.05f}){Solid(group,"Study partition upper closure "+x.ToString(System.Globalization.CultureInfo.InvariantCulture),new Vector3(x,3.845f,27.075f),new Vector3(.16f,1.53f,6.19f),plaster,"Study partition upper closure");Solid(group,"Study partition corner post "+x.ToString(System.Globalization.CultureInfo.InvariantCulture),new Vector3(x,2.3f,24),new Vector3(.24f,4.6f,.20f),steel,"Study partition corner post");}
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();File.WriteAllText(Folder+"walls-applied.txt","12 glazing panes: +0.05 m z alignment and width 2.88 ->2.92 m, overlapping existing frames. Study west 0.25 m junction and three 0.04 m corner gaps closed; partitions continue to 4.60 m ceiling. Existing entrances and glazing preserved.");
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void RigidGroup(GameObject root,Bounds area,Vector3 anchor,Vector3 destination,float yaw,string name,StringBuilder log)
    {
        Quaternion rot=Quaternion.Euler(0,yaw,0);Vector3 Map(Vector3 p)=>destination+rot*(p-anchor);var all=root.GetComponentsInChildren<Transform>(true);var roots=all.Where(t=>(t.name=="Campus reading chair"||t.name=="North wing reading chair")&&area.Contains(RenderBounds(t.gameObject).center)).ToArray();var skip=new HashSet<Transform>(roots);
        int parts=ModifyNative(root,(mf,b)=>Within(area,b),(v,b)=>Map(v),rot);int boxes=0;foreach(var box in root.GetComponentsInChildren<BoxCollider>(true)){if(Under(box.transform,skip)||!Within(area,WorldBox(box)))continue;box.transform.position=Map(box.transform.position);box.transform.rotation=rot*box.transform.rotation;boxes++;}foreach(var chair in roots){chair.position=Map(chair.position);chair.rotation=rot*chair.rotation;}foreach(var label in root.GetComponentsInChildren<TMPro.TextMeshPro>(true))if(area.Contains(label.transform.position)){label.transform.position=Map(label.transform.position);label.transform.rotation=rot*label.transform.rotation;}log.AppendLine(name+": native parts="+parts+" independent boxes="+boxes+" chair roots="+roots.Length);
    }
    static void FitPassage()
    {
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);var log=new StringBuilder();try
        {
            var bench=root.GetComponentsInChildren<BoxCollider>().Single(b=>b.name=="Research bench"&&Mathf.Abs(b.transform.position.x+2.21f)<.02f);var b=WorldBox(bench);RigidGroup(root,Area(new Vector3(b.min.x-.08f,-.03f,8.7f),new Vector3(b.max.x+.08f,2.1f,11.55f)),new Vector3(b.center.x,0,b.center.z),new Vector3(-27.2f,0,3.8f),-90,"Research workstation beside west wall",log);
            RigidGroup(root,Area(new Vector3(.9f,-.03f,9.8f),new Vector3(2.1f,2.4f,11.2f)),new Vector3(1.5f,0,10.5f),new Vector3(27.1f,0,-4.2f),0,"Plant moved away from passage",log);
            RigidGroup(root,Area(new Vector3(1.75f,-.03f,13.2f),new Vector3(2.95f,1.6f,14.8f)),new Vector3(2.35f,0,14),new Vector3(5.0f,0,13.4f),0,"Trolley beside stack wing wall",log);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();File.WriteAllText(Folder+"passage-applied.txt",log.ToString());
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void MoveStack(GameObject root,BoxCollider box,Vector3 destination,float yaw,StringBuilder log,Transform[] owned=null)
    {
        var b=WorldBox(box);var centre=new Vector3(b.center.x,0,b.center.z);var rot=Quaternion.Euler(0,yaw,0);Vector3 Map(Vector3 p)=>destination+rot*(p-centre);var shelves=owned??root.GetComponentsInChildren<Transform>(true).Where(t=>(t.name=="Campus full bookcase"||t.name=="North wing full bookcase")&&b.Contains(t.position+Vector3.up)).ToArray();if(shelves.Length!=2)throw new Exception("Expected two shelf roots: "+box.name+" at "+V(b.center)+" count="+shelves.Length);var board=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Library stack board"&&Mathf.Abs(t.position.x-centre.x)<.015f).OrderBy(t=>Vector2.Distance(new Vector2(t.position.x,t.position.z),new Vector2(centre.x,centre.z))).First();if(Vector2.Distance(new Vector2(board.position.x,board.position.z),new Vector2(centre.x,centre.z))>2.3f)throw new Exception("Cannot identify shelf board");foreach(var t in shelves.Append(board)){t.position=Map(t.position);t.rotation=rot*t.rotation;}box.transform.position=Map(box.transform.position);box.transform.rotation=rot*box.transform.rotation;log.AppendLine(box.name+" "+V(centre)+" -> "+V(destination)+" yaw="+yaw);
    }
    static void FitBathroom()
    {
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);var log=new StringBuilder();try
        {
            var boxes=root.GetComponentsInChildren<BoxCollider>();var originalShelves=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Campus full bookcase"||t.name=="North wing full bookcase").ToArray();var ownership=boxes.Where(b=>b.name.StartsWith("Bookcase row ")).ToDictionary(b=>b,b=>originalShelves.Where(t=>WorldBox(b).Contains(t.position+Vector3.up)).ToArray());foreach(var pair in ownership)if(pair.Value.Length!=2)throw new Exception("Initial stack ownership "+pair.Key.name+" count="+pair.Value.Length);var column=boxes.Single(b=>b.name=="Column"&&Mathf.Abs(b.transform.position.x-17.5f)<.02f&&Mathf.Abs(b.transform.position.z-8)<.02f);RigidGroup(root,Area(new Vector3(17.10f,-.02f,7.60f),new Vector3(17.90f,4.3f,8.4f)),new Vector3(17.5f,0,8),new Vector3(15.83f,0,8),0,"Pillar absorbed by west wall",log);
            var wall=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Vestibule west privacy wall");var mesh=wall.GetComponent<MeshFilter>().sharedMesh;var vv=mesh.vertices;for(int i=0;i<vv.Length;i++)vv[i]=new Vector3(vv[i].x*(.5f/mesh.bounds.size.x),vv[i].y*(4.6f/mesh.bounds.size.y),vv[i].z);mesh.vertices=vv;MetricUV(mesh);mesh.RecalculateBounds();mesh.UploadMeshData(false);EditorUtility.SetDirty(mesh);wall.position=new Vector3(15.83f,2.3f,7.375f);wall.GetComponent<BoxCollider>().size=new Vector3(.5f,4.6f,9.55f);
            foreach(string name in new[]{"Vestibule entrance west wall","Vestibule entrance east wall","Vestibule entrance lintel"}){var t=root.GetComponentsInChildren<Transform>().Single(x=>x.name==name);var m=t.GetComponent<MeshFilter>().sharedMesh;var vertices=m.vertices;float oldH=name.EndsWith("lintel")?.6f:3.2f,newH=name.EndsWith("lintel")?2:4.6f;for(int i=0;i<vertices.Length;i++)vertices[i].y*=newH/m.bounds.size.y;m.vertices=vertices;MetricUV(m);m.RecalculateBounds();m.UploadMeshData(false);EditorUtility.SetDirty(m);t.position=new Vector3(t.position.x,name.EndsWith("lintel")?3.6f:2.3f,t.position.z);var size=t.GetComponent<BoxCollider>().size;size.y=newH;t.GetComponent<BoxCollider>().size=size;}
            var skirting=root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Vestibule skirting");skirting.position=new Vector3(15.565f,.07f,7.375f);
            var oldStacks=boxes.Where(b=>b.name.StartsWith("Bookcase row ")&&b.transform.position.z<10&&b.transform.position.x<15).ToArray();if(oldStacks.Length!=12)throw new Exception("Expected twelve western original pairs");foreach(var stack in oldStacks){var b=WorldBox(stack);int col=Mathf.RoundToInt((b.center.x-3.82f)/3.4f);MoveStack(root,stack,new Vector3(3.8f+col*3,0,b.center.z),0,log,ownership[stack]);}
            for(int i=0;i<2;i++){float x=17.42f+i*3.4f;var stack=boxes.Single(b=>b.name.StartsWith("Bookcase row ")&&Mathf.Abs(b.transform.position.x-x)<.02f&&Mathf.Abs(b.transform.position.z+.6f)<.02f);MoveStack(root,stack,new Vector3(21.3f+i*4.5f,0,-.8f),90,log,ownership[stack]);}
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);AssetDatabase.SaveAssets();File.WriteAllText(Folder+"bathroom-applied.txt",log.ToString());
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
