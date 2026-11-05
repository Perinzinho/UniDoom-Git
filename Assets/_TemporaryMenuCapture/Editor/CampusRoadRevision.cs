using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Temporary, explicitly requested road and orphaned-house geometry revision.
[InitializeOnLoad]
public static class CampusRoadRevision
{
    const string Campus="Assets/_Project/Scenes/CampusExterior/", ScenePath=Campus+"CampusExterior.unity";
    static readonly string Work=Path.Combine(Path.GetTempPath(),"UniDoomCampusRoads");
    static double next;
    static readonly HashSet<string> changed=new HashSet<string>();
    static CampusRoadRevision()=>EditorApplication.update+=Tick;
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        next=EditorApplication.timeSinceStartup+1;string request=Path.Combine(Work,"request.txt");if(!File.Exists(request))return;
        string command=File.ReadAllText(request).Trim();File.Delete(request);
        try
        {
            if(command=="inspect")Inspect();else if(command=="apply")Apply();else if(command=="capture")Capture();else if(command=="validate")Validate();else if(command=="build")Build();else if(command=="cleanup")Cleanup();else throw new Exception(command);
            File.WriteAllText(Path.Combine(Work,"status.txt"),"PASS "+command);
        }
        catch(Exception e){File.WriteAllText(Path.Combine(Work,"status.txt"),"FAIL "+e);Debug.LogException(e);}
    }
    static Scene CampusScene()=>SceneManager.GetSceneByPath(ScenePath);
    static GameObject Root(Scene scene)=>scene.GetRootGameObjects().Single(g=>g.GetComponent<CampusExteriorEnvironment>()!=null);
    static string Hierarchy(Transform t)=>t.parent==null?t.name:Hierarchy(t.parent)+"/"+t.name;
    static T Require<T>(GameObject go) where T:Component {var component=go.GetComponent<T>();if(component==null)component=go.AddComponent<T>();return component;}
    static void Inspect()
    {
        var scene=CampusScene();if(!scene.IsValid()||!scene.isLoaded)throw new Exception("Open Campus Exterior first");
        var root=Root(scene);var report=new StringBuilder("Active="+SceneManager.GetActiveScene().path+" dirty="+scene.isDirty+" playing="+EditorApplication.isPlaying+"\n");
        foreach(var t in root.GetComponentsInChildren<Transform>(true))
        {
            if(t.parent!=null&&(t.parent.name.StartsWith("02 ")||t.parent.name.StartsWith("08 ")))
            {
                var mf=t.GetComponent<MeshFilter>();if(mf!=null&&mf.sharedMesh!=null)report.AppendLine("MESH "+Hierarchy(t)+" triangles="+mf.sharedMesh.triangles.Length/3+" bounds="+t.GetComponent<Renderer>().bounds);
                foreach(var c in t.GetComponents<Collider>())report.AppendLine("COLLIDER "+Hierarchy(t)+" "+c.bounds);
            }
        }
        File.WriteAllText(Path.Combine(Work,"inspection.txt"),report.ToString());
    }
    sealed class Geometry
    {
        public readonly List<Vector3> vertices=new List<Vector3>();public readonly List<Vector2> uv=new List<Vector2>();public readonly List<int> indices=new List<int>();
        public void Triangle(Vector3 a,Vector3 b,Vector3 c,float density=.47619f)
        {
            if(Vector3.Cross(b-a,c-a).y<0){var swap=b;b=c;c=swap;}
            int start=vertices.Count;vertices.AddRange(new[]{a,b,c});uv.AddRange(new[]{new Vector2(a.x,a.z)*density,new Vector2(b.x,b.z)*density,new Vector2(c.x,c.z)*density});indices.AddRange(new[]{start,start+1,start+2});
        }
        public void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float density=.5f){Triangle(a,b,c,density);Triangle(a,c,d,density);}
        public void Wall(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float u0,float u1)
        {
            int i=vertices.Count;vertices.AddRange(new[]{a,b,c,d});uv.AddRange(new[]{new Vector2(u0,a.y*2),new Vector2(u1,b.y*2),new Vector2(u1,c.y*2),new Vector2(u0,d.y*2)});indices.AddRange(new[]{i,i+2,i+1,i,i+3,i+2});
        }
        public Mesh Mesh(Transform filter=null,Transform root=null)
        {
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(filter==null?vertices:vertices.Select(v=>filter.InverseTransformPoint(root.TransformPoint(v))).ToList());mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
    }
    static Mesh SaveMesh(string path,Mesh mesh)
    {
        mesh.name=Path.GetFileNameWithoutExtension(path);var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}
        EditorUtility.CopySerialized(mesh,saved);saved.UploadMeshData(false);EditorUtility.SetDirty(saved);UnityEngine.Object.DestroyImmediate(mesh);return saved;
    }
    static Material CloneMaterial(string source,string name,Color tint,float bump=.35f)
    {
        string path=Campus+"Materials/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);var original=AssetDatabase.LoadAssetAtPath<Material>(Campus+"Materials/"+source+".mat");
        if(material==null){material=new Material(original);AssetDatabase.CreateAsset(material,path);}else material.CopyPropertiesFromMaterial(original);
        material.name=name;material.SetColor("_BaseColor",tint);material.SetFloat("_BumpScale",bump);material.SetFloat("_Smoothness",.1f);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
    }
    static Material Asphalt()
    {
        string dir=Campus+"Textures/clean_asphalt/";
        foreach(string file in new[]{"diff.png","nor_gl.png","arm.png"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(dir+file);importer.textureType=file=="nor_gl.png"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=file=="diff.png";importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            if(file=="arm.png"&&importer.userData!="UniDoomUnityMask")
            {
                var raw=new Texture2D(2,2,TextureFormat.RGBA32,false,true);raw.LoadImage(File.ReadAllBytes(dir+file));var pixels=raw.GetPixels32();
                for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new Color32(p.b,p.r,0,(byte)(255-p.g));}raw.SetPixels32(pixels);raw.Apply();File.WriteAllBytes(dir+file,raw.EncodeToPNG());UnityEngine.Object.DestroyImmediate(raw);importer.userData="UniDoomUnityMask";
            }
            importer.SaveAndReimport();
        }
        var mat=CloneMaterial("Scanned worn campus asphalt","Fine aggregate campus asphalt",new Color(.95f,.95f,.92f),.55f);mat.shader=Shader.Find("Universal Render Pipeline/Lit");mat.DisableKeyword("_TC_SHARED_MASK");
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"diff.png"));mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"nor_gl.png"));var arm=AssetDatabase.LoadAssetAtPath<Texture2D>(dir+"arm.png");mat.SetTexture("_MetallicGlossMap",arm);mat.SetTexture("_OcclusionMap",arm);mat.SetTexture("_DirtMap",arm);mat.SetTextureScale("_BaseMap",Vector2.one);mat.SetFloat("_OcclusionStrength",.65f);mat.SetFloat("_Smoothness",.18f);EditorUtility.SetDirty(mat);return mat;
    }
    static List<Vector2> Outline()
    {
        var p=new List<Vector2>{new Vector2(73.5f,-65),new Vector2(73.5f,-14)};
        void Arc(Vector2 c,float radius,float begin,float end,int count){for(int i=1;i<=count;i++){float angle=Mathf.Lerp(begin,end,i/(float)count)*Mathf.Deg2Rad;p.Add(c+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);}}
        Arc(new Vector2(68,-14),5.5f,0,90,18);p.Add(new Vector2(-99,-8.5f));p.Add(new Vector2(-99,.5f));p.Add(new Vector2(6,.5f));Arc(new Vector2(6,4.5f),4,-90,0,12);p.Add(new Vector2(10,102));p.Add(new Vector2(18,102));p.Add(new Vector2(18,4.5f));Arc(new Vector2(22,4.5f),4,180,270,12);p.Add(new Vector2(68,.5f));Arc(new Vector2(68,-14),14.5f,90,0,24);p.Add(new Vector2(82.5f,-65));return p;
    }
    static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    static float Area(List<Vector2> p){float result=0;for(int i=0;i<p.Count;i++)result+=Cross(p[i],p[(i+1)%p.Count]);return result*.5f;}
    static List<int> Triangulate(List<Vector2> polygon)
    {
        var remaining=Enumerable.Range(0,polygon.Count).ToList();if(Area(polygon)<0)remaining.Reverse();var result=new List<int>();int budget=remaining.Count*remaining.Count;
        while(remaining.Count>3&&budget-->0)
        {
            bool removed=false;
            for(int i=0;i<remaining.Count;i++)
            {
                int a=remaining[(i+remaining.Count-1)%remaining.Count],b=remaining[i],c=remaining[(i+1)%remaining.Count];var x=polygon[a];var y=polygon[b];var z=polygon[c];if(Cross(y-x,z-y)<=.00001f)continue;
                bool inside=remaining.Any(k=>k!=a&&k!=b&&k!=c&&Cross(y-x,polygon[k]-x)>.00001f&&Cross(z-y,polygon[k]-y)>.00001f&&Cross(x-z,polygon[k]-z)>.00001f);if(inside)continue;
                result.AddRange(new[]{a,b,c});remaining.RemoveAt(i);removed=true;break;
            }
            if(!removed)throw new Exception("Road outline could not triangulate");
        }
        if(remaining.Count!=3)throw new Exception("Road triangulation exceeded budget");result.AddRange(remaining);return result;
    }
    static List<Vector2> Clip(List<Vector2> polygon,int axis,float edge,bool greater)
    {
        var result=new List<Vector2>();if(polygon.Count==0)return result;var previous=polygon.Last();bool was=greater?previous[axis]>=edge-.00001f:previous[axis]<=edge+.00001f;
        foreach(var current in polygon){bool inside=greater?current[axis]>=edge-.00001f:current[axis]<=edge+.00001f;if(inside!=was){float t=(edge-previous[axis])/(current[axis]-previous[axis]);result.Add(Vector2.LerpUnclamped(previous,current,t));}if(inside)result.Add(current);previous=current;was=inside;}return result;
    }
    static float Height(Vector2 p)
    {
        float distance=1000;if(p.x<=68)distance=Mathf.Min(distance,Mathf.Abs(p.y+4));if(p.y<=-14)distance=Mathf.Min(distance,Mathf.Abs(p.x-78));if(p.y>=.5f)distance=Mathf.Min(distance,Mathf.Abs(p.x-14));if(p.x>=68&&p.y>=-14)distance=Mathf.Min(distance,Mathf.Abs(Vector2.Distance(p,new Vector2(68,-14))-10));return .015f+.035f*(1-Mathf.Clamp01(distance/4.25f));
    }
    static Vector3 Point(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
    static Geometry RoadSurface(List<Vector2> outline,out Dictionary<Vector2Int,Geometry> cells)
    {
        var total=new Geometry();cells=new Dictionary<Vector2Int,Geometry>();var triangles=Triangulate(outline);
        float sourceArea=0;for(int i=0;i<triangles.Count;i+=3)sourceArea+=Mathf.Abs(Cross(outline[triangles[i+1]]-outline[triangles[i]],outline[triangles[i+2]]-outline[triangles[i]]))*.5f;if(Mathf.Abs(sourceArea-Mathf.Abs(Area(outline)))>.02f)throw new Exception("Road triangulation area mismatch");
        for(int i=0;i<triangles.Count;i+=3)
        {
            var tri=new List<Vector2>{outline[triangles[i]],outline[triangles[i+1]],outline[triangles[i+2]]};float minX=tri.Min(v=>v.x),maxX=tri.Max(v=>v.x),minZ=tri.Min(v=>v.y),maxZ=tri.Max(v=>v.y);
            for(int x=Mathf.FloorToInt(minX/4);x<=Mathf.FloorToInt(maxX/4);x++)for(int z=Mathf.FloorToInt(minZ/4);z<=Mathf.FloorToInt(maxZ/4);z++)
            {
                var clipped=Clip(Clip(Clip(Clip(tri,0,x*4,true),0,(x+1)*4,false),1,z*4,true),1,(z+1)*4,false);if(clipped.Count<3)continue;
                var key=new Vector2Int(Mathf.FloorToInt((x*4+2)/20f),Mathf.FloorToInt((z*4+2)/20f));if(!cells.TryGetValue(key,out var cell)){cell=new Geometry();cells.Add(key,cell);}
                for(int j=1;j<clipped.Count-1;j++){var a=clipped[0];var b=clipped[j];var c=clipped[j+1];if(Mathf.Abs(Cross(b-a,c-a))<.00001f)continue;total.Triangle(Point(a,Height(a)),Point(b,Height(b)),Point(c,Height(c)));cell.Triangle(Point(a,Height(a)),Point(b,Height(b)),Point(c,Height(c)));}
            }
        }
        return total;
    }
    static bool Ramp(Vector2 p)=>Mathf.Abs(p.x+43)<1.8f&&(Mathf.Abs(p.y-.5f)<.2f||Mathf.Abs(p.y+8.5f)<.2f)||Mathf.Abs(p.y-60)<1.5f&&Mathf.Abs(p.x-10)<.2f||Mathf.Abs(p.y-23)<1.8f&&Mathf.Abs(p.x-18)<.2f||Mathf.Abs(p.y+44)<2&&p.x>70||Mathf.Abs(p.y-8.4f)<1.7f&&Mathf.Abs(Mathf.Abs(p.x-14)-4)<.2f;
    static void Borders(List<Vector2> polygon,Geometry gutter,Geometry curb,Geometry walk)
    {
        bool ccw=Area(polygon)>0;var outward=new Vector2[polygon.Count];
        for(int i=0;i<polygon.Count;i++){var a=(polygon[i]-polygon[(i+polygon.Count-1)%polygon.Count]).normalized;var b=(polygon[(i+1)%polygon.Count]-polygon[i]).normalized;var n1=ccw?new Vector2(a.y,-a.x):new Vector2(-a.y,a.x);var n2=ccw?new Vector2(b.y,-b.x):new Vector2(-b.y,b.x);var m=(n1+n2).normalized;outward[i]=m/Mathf.Max(.3f,Vector2.Dot(m,n2));}
        float u=0;
        for(int i=0;i<polygon.Count;i++)
        {
            int nextIndex=(i+1)%polygon.Count;var a=polygon[i];var b=polygon[nextIndex];int steps=Mathf.CeilToInt(Vector2.Distance(a,b));
            for(int j=0;j<steps;j++)
            {
                float t0=j/(float)steps,t1=(j+1)/(float)steps;var p=Vector2.Lerp(a,b,t0);var q=Vector2.Lerp(a,b,t1);var n=Vector2.Lerp(outward[i],outward[nextIndex],t0);var m=Vector2.Lerp(outward[i],outward[nextIndex],t1);bool ramp=Ramp((p+q)*.5f);float top=ramp?.035f:.16f;float end=u+Vector2.Distance(p,q)*.5f;
                gutter.Quad(Point(p,.015f),Point(q,.015f),Point(q+m*.28f,.025f),Point(p+n*.28f,.025f));
                curb.Wall(Point(p+n*.28f,.025f),Point(q+m*.28f,.025f),Point(q+m*.32f,top),Point(p+n*.32f,top),u,end);
                curb.Quad(Point(p+n*.32f,top),Point(q+m*.32f,top),Point(q+m*.5f,top),Point(p+n*.5f,top));
                walk.Quad(Point(p+n*.5f,top),Point(q+m*.5f,top),Point(q+m*2.1f,.16f),Point(p+n*2.1f,.16f));walk.Wall(Point(q+m*2.1f,-.075f),Point(p+n*2.1f,-.075f),Point(p+n*2.1f,.16f),Point(q+m*2.1f,.16f),end,u);u=end;
            }
        }
        void Path(float x0,float x1,float z0,float z1){var points=new[]{new Vector2(x0,z0),new Vector2(x1,z0),new Vector2(x1,z1),new Vector2(x0,z1)};walk.Quad(Point(points[0],.16f),Point(points[1],.16f),Point(points[2],.16f),Point(points[3],.16f));for(int i=0;i<4;i++){var a=points[i];var b=points[(i+1)%4];walk.Wall(Point(b,-.075f),Point(a,-.075f),Point(a,.16f),Point(b,.16f),0,Vector2.Distance(a,b)*.5f);}}
        Path(-44.5f,-41.5f,2.6f,10.7f);Path(-44.3f,7.9f,58.9f,61.1f);Path(-44.3f,-41.7f,61.1f,66.3f);Path(20.1f,64.5f,21.7f,24.3f);Path(61.5f,64.5f,24.3f,25.5f);
    }
    static Geometry Markings(bool yellow)
    {
        var geometry=new Geometry();
        void Mark(float x0,float x1,float z0,float z1){var a=new Vector2(x0,z0);var b=new Vector2(x1,z0);var c=new Vector2(x1,z1);var d=new Vector2(x0,z1);geometry.Quad(Point(a,Height(a)+.002f),Point(b,Height(b)+.002f),Point(c,Height(c)+.002f),Point(d,Height(d)+.002f),1);}
        if(yellow){for(float x=-94;x<61;x+=8)if(Mathf.Abs(x+43)>5&&Mathf.Abs(x-14)>12)Mark(x,x+2,-4.055f,-3.945f);for(float z=-60;z<-18;z+=8)if(Mathf.Abs(z+44)>5)Mark(77.945f,78.055f,z,z+2);}
        else
        {
            for(float x=74;x<82;x+=.9f)Mark(x,x+.45f,-45.4f,-42.6f);
            for(float z=-8;z<0;z+=.9f)Mark(-44.4f,-41.6f,z,z+.45f);
            for(float x=10.4f;x<17.6f;x+=.9f)Mark(x,x+.45f,7,9.8f);
        }
        return geometry;
    }
    static Geometry Repairs()
    {
        var geometry=new Geometry();
        foreach(var center in new[]{new Vector2(-68,-5.4f),new Vector2(-14,-6.2f),new Vector2(76.5f,-30),new Vector2(15.5f,74)})
        {
            var offsets=new[]{new Vector2(-1.4f,-.5f),new Vector2(-.6f,-.65f),new Vector2(1.3f,-.55f),new Vector2(1.5f,-.1f),new Vector2(1.2f,.6f),new Vector2(-1.2f,.55f)};
            for(int i=0;i<offsets.Length;i++){var a=center;var b=center+offsets[i];var c=center+offsets[(i+1)%offsets.Length];geometry.Triangle(Point(a,Height(a)+.0035f),Point(b,Height(b)+.0035f),Point(c,Height(c)+.0035f),1/3f);}
        }
        return geometry;
    }
    static void SurfaceCells(MeshFilter filter,GameObject root,Geometry geometry,Material material,string prefix)
    {
        filter.GetComponent<MeshRenderer>().enabled=false;var parent=filter.transform.Find("Surface lighting cells");if(parent==null){parent=new GameObject("Surface lighting cells").transform;parent.SetParent(filter.transform,false);}foreach(Transform child in parent)child.GetComponent<MeshRenderer>().enabled=false;
        var cells=new Dictionary<Vector2Int,Geometry>();
        for(int i=0;i<geometry.indices.Count;i+=3)
        {
            var center=(geometry.vertices[geometry.indices[i]]+geometry.vertices[geometry.indices[i+1]]+geometry.vertices[geometry.indices[i+2]])/3;var key=new Vector2Int(Mathf.FloorToInt(center.x/20),Mathf.FloorToInt(center.z/20));if(!cells.TryGetValue(key,out var cell)){cell=new Geometry();cells.Add(key,cell);}
            for(int j=0;j<3;j++){int index=geometry.indices[i+j];cell.indices.Add(cell.vertices.Count);cell.vertices.Add(geometry.vertices[index]);cell.uv.Add(geometry.uv[index]);}
        }
        foreach(var pair in cells){string name=prefix+"_"+pair.Key.x+"_"+pair.Key.y;string path=Campus+"Meshes/"+name+".asset";var child=parent.Find(name);if(child==null){child=new GameObject(name).transform;child.SetParent(parent,false);}if(changed.Add(path))SaveMesh(path,pair.Value.Mesh(child,root.transform));var go=MeshObject(parent,name,AssetDatabase.LoadAssetAtPath<Mesh>(path),material);go.isStatic=true;}
    }
    static Mesh Compact(Mesh original,List<int>[] sets)
    {
        var used=sets.SelectMany(s=>s).Distinct().OrderBy(i=>i).ToArray();var remap=new int[original.vertexCount];for(int i=0;i<used.Length;i++)remap[used[i]]=i;var result=new Mesh{indexFormat=IndexFormat.UInt32};result.vertices=used.Select(i=>original.vertices[i]).ToArray();
        var normals=original.normals;if(normals.Length==original.vertexCount)result.normals=used.Select(i=>normals[i]).ToArray();var tangents=original.tangents;if(tangents.Length==original.vertexCount)result.tangents=used.Select(i=>tangents[i]).ToArray();var colors=original.colors32;if(colors.Length==original.vertexCount)result.colors32=used.Select(i=>colors[i]).ToArray();
        for(int channel=0;channel<8;channel++){var uv=new List<Vector4>();original.GetUVs(channel,uv);if(uv.Count==original.vertexCount)result.SetUVs(channel,used.Select(i=>uv[i]).ToList());}result.subMeshCount=sets.Length;for(int i=0;i<sets.Length;i++)result.SetTriangles(sets[i].Select(k=>remap[k]).ToArray(),i);result.RecalculateBounds();return result;
    }
    static void RemoveHouseFragments(GameObject root,StringBuilder report)
    {
        var group=root.transform.Find("08 Water tower and western annexes");var tower=new Bounds(new Vector3(99,15,65),new Vector3(12,34,12));
        foreach(var filter in group.GetComponentsInChildren<MeshFilter>(true))
        {
            string path=AssetDatabase.GetAssetPath(filter.sharedMesh);if(string.IsNullOrEmpty(path))continue;
            if(changed.Add(path))
            {
                var mesh=filter.sharedMesh;var points=mesh.vertices.Select(v=>root.transform.InverseTransformPoint(filter.transform.TransformPoint(v))).ToArray();var sets=new List<int>[mesh.subMeshCount];int before=mesh.triangles.Length/3;
                for(int sub=0;sub<sets.Length;sub++){sets[sub]=new List<int>();var indices=mesh.GetTriangles(sub);for(int i=0;i<indices.Length;i+=3)if(tower.Contains(points[indices[i]])&&tower.Contains(points[indices[i+1]])&&tower.Contains(points[indices[i+2]]))sets[sub].AddRange(new[]{indices[i],indices[i+1],indices[i+2]});}
                var replacement=Compact(mesh,sets);SaveMesh(path,replacement);report.AppendLine("House fragments: "+filter.name+" removed triangles="+(before-filter.sharedMesh.triangles.Length/3));
            }
            if(filter.sharedMesh.vertexCount==0)filter.GetComponent<MeshRenderer>().enabled=false;
        }
    }
    static void Assign(MeshFilter filter,GameObject root,Geometry geometry,Material material)
    {
        string path=AssetDatabase.GetAssetPath(filter.sharedMesh);if(changed.Add(path))SaveMesh(path,geometry.Mesh(filter.transform,root.transform));var renderer=filter.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.enabled=geometry.indices.Count>0;renderer.shadowCastingMode=ShadowCastingMode.Off;
    }
    static GameObject MeshObject(Transform parent,string name,Mesh mesh,Material material,bool collider=false)
    {
        var child=parent.Find(name);var go=child==null?new GameObject(name):child.gameObject;if(child==null)go.transform.SetParent(parent,false);
        var filter=Require<MeshFilter>(go);filter.sharedMesh=mesh;var renderer=Require<MeshRenderer>(go);renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.enabled=mesh.vertexCount>0;
        if(collider){var collision=Require<MeshCollider>(go);collision.sharedMesh=null;collision.sharedMesh=mesh;collision.enabled=true;}return go;
    }
    static void ReviseRoot(GameObject root,Material asphalt,Material paving,Material curbMaterial,Material gutterMaterial,Material white,Material yellow,StringBuilder report)
    {
        RemoveHouseFragments(root,report);var roadGroup=root.transform.Find("02 Avenues and sidewalks");var outline=Outline();var road=RoadSurface(outline,out var cells);var gutter=new Geometry();var curb=new Geometry();var walk=new Geometry();Borders(outline,gutter,curb,walk);
        // Existing gameplay colliders on the roads are simple static surfaces and are replaced by the saved geometry.
        foreach(var collider in roadGroup.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        var asphaltFilter=roadGroup.Find("Campus asphalt").GetComponent<MeshFilter>();Assign(asphaltFilter,root,road,asphalt);asphaltFilter.GetComponent<MeshRenderer>().enabled=false;
        var roadCollider=Require<MeshCollider>(asphaltFilter.gameObject);roadCollider.sharedMesh=null;roadCollider.sharedMesh=asphaltFilter.sharedMesh;roadCollider.enabled=true;
        var cellRoot=asphaltFilter.transform.Find("Road lighting cells");foreach(Transform cell in cellRoot)cell.GetComponent<MeshRenderer>().enabled=false;
        foreach(var pair in cells)
        {
            string name="RoadCell_"+pair.Key.x+"_"+pair.Key.y;var child=cellRoot.Find(name);var target=child==null?new GameObject(name).transform:child;if(child==null)target.SetParent(cellRoot,false);
            string path=Campus+"Meshes/"+name+".asset";if(changed.Add(path))SaveMesh(path,pair.Value.Mesh(target,root.transform));var go=MeshObject(cellRoot,name,AssetDatabase.LoadAssetAtPath<Mesh>(path),asphalt);go.isStatic=true;
        }
        var pavingFilter=roadGroup.Find("Damp concrete edging").GetComponent<MeshFilter>();Assign(pavingFilter,root,walk,paving);var pavingCollider=Require<MeshCollider>(pavingFilter.gameObject);pavingCollider.sharedMesh=null;pavingCollider.sharedMesh=pavingFilter.sharedMesh;pavingCollider.enabled=true;
        var curbFilter=roadGroup.Find("Rain stained ivory plaster").GetComponent<MeshFilter>();Assign(curbFilter,root,curb,curbMaterial);var curbCollider=Require<MeshCollider>(curbFilter.gameObject);curbCollider.sharedMesh=null;curbCollider.sharedMesh=curbFilter.sharedMesh;curbCollider.enabled=true;
        SurfaceCells(pavingFilter,root,walk,paving,"WalkwayCell");SurfaceCells(curbFilter,root,curb,curbMaterial,"CurbCell");
        var paintFilter=roadGroup.Find("Road ivory paint").GetComponent<MeshFilter>();Assign(paintFilter,root,Markings(false),white);
        foreach(var data in new[]{("Road drainage gutter",gutter,gutterMaterial,true),("Restrained centre markings",Markings(true),yellow,false)})
        {
            string path=Campus+"Meshes/"+data.Item1+".asset";if(changed.Add(path))SaveMesh(path,data.Item2.Mesh());MeshObject(roadGroup,data.Item1,AssetDatabase.LoadAssetAtPath<Mesh>(path),data.Item3,data.Item4);
        }
        var repairMaterial=CloneMaterial("Scanned worn campus asphalt","Localized asphalt repairs",new Color(.48f,.49f,.47f),.3f);string repairPath=Campus+"Meshes/Localized asphalt repairs.asset";if(changed.Add(repairPath))SaveMesh(repairPath,Repairs().Mesh());MeshObject(roadGroup,"Localized asphalt repairs",AssetDatabase.LoadAssetAtPath<Mesh>(repairPath),repairMaterial);
        report.AppendLine("Roads: unified outline, mild 3.5cm crown, 28cm gutter, 18cm curb top, 1.6m sidewalks, three crossings, five pedestrian links; "+cells.Count+" render cells; "+road.indices.Count/3+" road triangles.");
    }
    static void Apply()
    {
        var scene=CampusScene();if(!scene.IsValid()||!scene.isLoaded||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Campus scene must be loaded, saved and outside Play Mode");changed.Clear();var report=new StringBuilder();
        var asphalt=Asphalt();var paving=CloneMaterial("Worn campus paving","Campus sidewalk concrete",new Color(.73f,.72f,.66f));var curb=CloneMaterial("Worn campus paving","Campus curb concrete",new Color(.86f,.85f,.77f),.25f);var gutter=CloneMaterial("Worn campus paving","Campus gutter concrete",new Color(.46f,.47f,.43f),.25f);
        var white=CloneMaterial("Road ivory paint","Restrained white road paint",new Color(.72f,.72f,.66f));white.SetTexture("_BaseMap",null);white.SetFloat("_AlphaClip",0);white.DisableKeyword("_ALPHATEST_ON");white.renderQueue=2000;white.SetOverrideTag("RenderType","Opaque");
        var yellow=CloneMaterial("Road ivory paint","Restrained yellow road paint",new Color(.64f,.48f,.18f));yellow.SetTexture("_BaseMap",null);yellow.SetFloat("_AlphaClip",0);yellow.DisableKeyword("_ALPHATEST_ON");yellow.renderQueue=2000;yellow.SetOverrideTag("RenderType","Opaque");
        var prefab=PrefabUtility.LoadPrefabContents(Campus+"Prefabs/CampusExterior.prefab");try{ReviseRoot(prefab,asphalt,paving,curb,gutter,white,yellow,report);PrefabUtility.SaveAsPrefabAsset(prefab,Campus+"Prefabs/CampusExterior.prefab");}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        ReviseRoot(Root(scene),asphalt,paving,curb,gutter,white,yellow,report);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();File.WriteAllText(Path.Combine(Work,"changes.txt"),report.ToString());EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);Validate();
    }
    static void Validate()
    {
        var scene=CampusScene();var root=Root(scene);var tower=new Bounds(new Vector3(99,15,65),new Vector3(12,34,12));var report=new StringBuilder();int remaining=0;
        foreach(var filter in root.transform.Find("08 Water tower and western annexes").GetComponentsInChildren<MeshFilter>(true))foreach(var v in filter.sharedMesh.vertices)if(!tower.Contains(root.transform.InverseTransformPoint(filter.transform.TransformPoint(v))))remaining++;
        if(remaining!=0)throw new Exception("House vertices outside tower remain: "+remaining);
        if(root.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("Missing script");
        foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))foreach(var material in renderer.sharedMaterials)if(material==null||material.shader==null||!material.shader.isSupported)throw new Exception("Missing material/shader "+Hierarchy(renderer.transform));
        report.AppendLine("PASS: no house-mesh vertices remain outside the preserved water tower. No missing shaders or scripts.");
        Physics.SyncTransforms();int samples=0,missing=0;float previous=0;int jumps=0;var roads=root.transform.Find("02 Avenues and sidewalks");
        void Line(Vector3 a,Vector3 b)
        {
            bool first=true;int count=Mathf.CeilToInt(Vector3.Distance(a,b)*10);
            for(int i=0;i<=count;i++){var p=Vector3.Lerp(a,b,i/(float)count);var hits=Physics.RaycastAll(p+Vector3.up*1.4f,Vector3.down,3,~0,QueryTriggerInteraction.Ignore).Where(h=>h.transform.IsChildOf(roads)).OrderByDescending(h=>h.point.y).ToArray();samples++;if(hits.Length==0){missing++;report.AppendLine("NO ROAD GROUND "+p);continue;}float height=hits[0].point.y;if(!first&&Mathf.Abs(height-previous)>.3f){jumps++;report.AppendLine("STEP "+p+" "+previous+" -> "+height);}previous=height;first=false;}
        }
        Line(new Vector3(-98,0,-4),new Vector3(68,0,-4));Line(new Vector3(14,0,-4),new Vector3(14,0,101));Line(new Vector3(78,0,-65),new Vector3(78,0,-14));Line(new Vector3(-43,0,-4),new Vector3(-43,0,10.5f));Line(new Vector3(14,0,60),new Vector3(-43,0,60));Line(new Vector3(14,0,23),new Vector3(63,0,23));
        report.AppendLine("Ground samples="+samples+" missing="+missing+" steps>30cm="+jumps);File.WriteAllText(Path.Combine(Work,"validation.txt"),report.ToString());if(missing+jumps>0)throw new Exception("Ground validation failed");
    }
    static void Capture()
    {
        var scene=CampusScene();if(SceneManager.sceneCount!=1||SceneManager.GetActiveScene()!=scene)throw new Exception("Keep only the campus scene open for capture");
        var go=new GameObject("[Temporary road camera]");var cam=go.AddComponent<Camera>();cam.fieldOfView=65;cam.farClipPlane=500;cam.nearClipPlane=.08f;cam.allowHDR=true;cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;var rt=new RenderTexture(1600,900,24);var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
        var positions=new[]{new Vector3(78,1.8f,-46),new Vector3(36,1.8f,-4),new Vector3(14,1.8f,35),new Vector3(-94,2,54),new Vector3(98,2,93),new Vector3(28,85,-65)};var targets=new[]{new Vector3(56,2,-4),new Vector3(10,2,0),new Vector3(14,2,90),new Vector3(-94,3,80),new Vector3(99,2,101),new Vector3(0,0,20)};
        try{cam.targetTexture=rt;for(int i=0;i<positions.Length;i++){cam.transform.SetPositionAndRotation(positions[i],Quaternion.LookRotation(targets[i]-positions[i]));for(int j=0;j<3;j++)cam.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Work,"view-"+i+".png"),texture.EncodeToPNG());}}
        finally{RenderTexture.active=null;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(go);}
    }
    static void Build(){Validate();var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=Path.Combine(Work,"build/Campus.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});File.WriteAllText(Path.Combine(Work,"build.txt"),report.summary.result+" errors="+report.summary.totalErrors);if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed");}
    static void Cleanup(){Validate();string[] paths={"Assets/_TemporaryMenuCapture/Editor/CampusRoadRevision.cs","Assets/_TemporaryMenuCapture/CampusRoadReviewPlayer.cs"};var assets=AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/_Project/")&&new[]{".asset",".unity",".prefab",".mat"}.Contains(Path.GetExtension(p))).ToArray();if(AssetDatabase.GetDependencies(assets,true).Intersect(paths).Any())throw new Exception("Tool still referenced");foreach(string p in paths)if(File.Exists(p)&&!AssetDatabase.DeleteAsset(p))throw new Exception("Could not remove "+p);File.WriteAllText(Path.Combine(Work,"cleanup.txt"),"PASS temporary review tools removed after asset dependency verification");AssetDatabase.Refresh();}
}
