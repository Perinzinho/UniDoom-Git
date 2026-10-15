using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object = UnityEngine.Object;

public static class BasementPolishBuilder
{
    const string Room = "Assets/prefabs/Scenario/Basement.prefab";
    const string Art = "Assets/Art/Basement/ArenaPolish";
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static readonly List<Mesh> temporaryMeshes=new List<Mesh>();
    static int meshIndex;
    public static void Build()
    {
        BasementSurfaceBuilder.RequireIsolated();
        mats.Clear();meshIndex=0;
        foreach(string folder in new[]{"Materials","Meshes","Textures"})Directory.CreateDirectory(Art+"/"+folder);
        AssetDatabase.Refresh();
        ImportModels();
        var root=PrefabUtility.LoadPrefabContents(Room);
        try
        {
            var arena=root.transform.Find("PreBossArea");var boss=root.transform.Find("BossRoom");
            if(arena.Find("Industrial maintenance dressing"))throw new Exception("Restore the snapshot before rebuilding");
            var metal=arena.Find("BasementArenaDetails").GetComponentsInChildren<MeshRenderer>(true).SelectMany(r=>r.sharedMaterials)
                .First(m=>m && m.name.Contains("_Metal"));
            var stone=arena.Find("Floor/Hall_Floor").GetComponent<MeshRenderer>().sharedMaterial;
            MakeMaterial("Oxidized structural steel",metal,new Color(.47f,.45f,.38f));
            MakeMaterial("Aged painted equipment",metal,new Color(.24f,.31f,.29f));
            MakeMaterial("Dry chipped masonry",stone,new Color(.58f,.56f,.49f));
            MakeMaterial("Wet mineral deposits",stone,new Color(.23f,.26f,.23f));
            MakeMaterial("Black rubber cable",metal,new Color(.045f,.05f,.047f));
            MakeMaterial("Worn brass fasteners",metal,new Color(.55f,.43f,.24f));
            var lamp=new Material(Shader.Find("Universal Render Pipeline/Lit"));lamp.name="Aged lamp diffuser";
            lamp.SetColor("_BaseColor",new Color(.75f,.66f,.46f));lamp.SetColor("_EmissionColor",new Color(1.1f,.84f,.47f));lamp.EnableKeyword("_EMISSION");
            Store(lamp);mats["Aged lamp diffuser"]=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Aged_lamp_diffuser.mat");
            float af=BasementFinalBuilder.Floor(arena,"Hall_Floor"),bf=BasementFinalBuilder.Floor(boss,"Main_Floor");
            float front=boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
            var a=Group(arena,"Industrial maintenance dressing",V(0,af,0));
            DressArena(a);
            ReplaceDrums(arena,af);
            // The map overrides the altar's depth. Attach the dressing to that same anchor.
            var altarAnchor=boss.GetComponentsInChildren<Transform>().First(t=>t.name=="Boss altar and infernal engine");
            var b=Group(altarAnchor,"Sanctum architectural polish",Vector3.zero);
            b.position=boss.TransformPoint(V(0,bf,front));b.rotation=boss.rotation;
            b.localScale=V(boss.lossyScale.x/altarAnchor.lossyScale.x,boss.lossyScale.y/altarAnchor.lossyScale.y,boss.lossyScale.z/altarAnchor.lossyScale.z);
            DressBoss(b);
            Combine(a);Combine(b);
            PrefabUtility.SaveAsPrefabAsset(root,Room);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);foreach(var mesh in temporaryMeshes)if(mesh)Object.DestroyImmediate(mesh);temporaryMeshes.Clear();}
        AssetDatabase.SaveAssets();
        Capture("Polished");
        Debug.Log("BASEMENT_POLISH_BUILD_OK");
    }
    static void Store(Material m)
    {
        string p=Art+"/Materials/"+m.name.Replace(' ','_')+".mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(p);
        if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);}
        else AssetDatabase.CreateAsset(m,p);
    }
    static void MakeMaterial(string name,Material source,Color tint)
    {
        var m=new Material(source);m.name=name;m.SetColor("_BaseColor",tint);
        m.SetTextureScale("_BaseMap",Vector2.one*.65f);m.SetFloat("_Smoothness",.24f);
        if(m.HasProperty("_SpecColor"))m.SetVector("_SpecColor",new Vector4(.17f,name.StartsWith("Wet")?.22f:.02f,.7f,1));
        Store(m);mats[name]=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+name.Replace(' ','_')+".mat");
    }
    static Texture2D LoadReadable(string path)
    {
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path)))throw new Exception("Texture decode failed: "+path);
        return texture;
    }
    static void ImportModels()
    {
        foreach(string id in new[]{"industrial_storage_cart","barrel_03","metal_tool_chest","steel_frame_shelves_02"})
        {
            string folder=Art+"/Models/"+id;
            var ready=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+id+".mat");
            if(ready && ready.GetTexture("_BumpMap") && ready.GetTexture("_OcclusionMap")){mats[id]=ready;continue;}
            var importer=(ModelImporter)AssetImporter.GetAtPath(folder+"/"+id+".fbx");
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
            importer.addCollider=false;importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();
            foreach(string map in new[]{"Diffuse","nor_gl","arm"})
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+map+".png");
                t.textureType=map=="nor_gl"?TextureImporterType.NormalMap:TextureImporterType.Default;
                t.sRGBTexture=map=="Diffuse";t.maxTextureSize=2048;t.mipmapEnabled=true;t.filterMode=FilterMode.Trilinear;t.anisoLevel=4;
                t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
            }
            var arm=LoadReadable(folder+"/arm.png");var pixels=arm.GetPixels32();
            for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new Color32(p.b,p.r,p.g,(byte)(255-p.g));}
            var mask=new Texture2D(arm.width,arm.height,TextureFormat.RGBA32,false,true);mask.SetPixels32(pixels);mask.Apply();
            string maskPath=Art+"/Textures/"+id+"_UnityMask.png";
            File.WriteAllBytes(maskPath,mask.EncodeToPNG());Object.DestroyImmediate(mask);Object.DestroyImmediate(arm);
            AssetDatabase.ImportAsset(maskPath);
            var ti=(TextureImporter)AssetImporter.GetAtPath(maskPath);ti.sRGBTexture=false;ti.maxTextureSize=2048;ti.mipmapEnabled=true;
            ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.name=id;
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Diffuse.png"));mat.SetColor("_BaseColor",new Color(.78f,.79f,.76f));
            mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/nor_gl.png"));mat.SetFloat("_BumpScale",.8f);mat.EnableKeyword("_NORMALMAP");
            var packed=AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
            mat.SetTexture("_MetallicGlossMap",packed);mat.SetTexture("_OcclusionMap",packed);mat.SetFloat("_Metallic",1);mat.SetFloat("_Smoothness",.72f);
            mat.SetFloat("_OcclusionStrength",.8f);mat.EnableKeyword("_METALLICSPECGLOSSMAP");mat.EnableKeyword("_OCCLUSIONMAP");mat.enableInstancing=true;
            Store(mat);mats[id]=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+id+".mat");
        }
    }
    static Transform Group(Transform parent,string name,Vector3 position,float yaw=0)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.localRotation=Quaternion.Euler(0,yaw,0);return t;
    }
    static Transform Model(Transform parent,string id,Vector3 feet,float height,float yaw,bool solid=true)
    {
        var holder=Group(parent,"Detailed "+id,feet,yaw);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/"+id+"/"+id+".fbx");
        var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent.gameObject.scene);go.transform.SetParent(holder,false);
        PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var renderers=go.GetComponentsInChildren<MeshRenderer>();
        if(renderers.Length==0)throw new Exception("Empty model "+id);
        var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        float scale=height/bounds.size.y;go.transform.localScale*=scale;
        bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        go.transform.position+=holder.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        foreach(var r in renderers){r.sharedMaterials=Enumerable.Repeat(mats[id],r.sharedMaterials.Length).ToArray();r.shadowCastingMode=ShadowCastingMode.On;}
        if(solid)
        {
            var local=new Bounds();bool first=true;
            foreach(var f in go.GetComponentsInChildren<MeshFilter>())
                foreach(var corner in Corners(f.sharedMesh.bounds))
                {var p=holder.InverseTransformPoint(f.transform.TransformPoint(corner));if(first){local=new Bounds(p,Vector3.zero);first=false;}else local.Encapsulate(p);}
            var c=holder.gameObject.AddComponent<BoxCollider>();c.center=local.center;c.size=local.size;
        }
        if(id=="steel_frame_shelves_02")
        {
            for(int row=0;row<3;row++)for(int col=0;col<2;col++)
            {
                var box=Group(holder,"Sealed maintenance parts",V(col==0?-.52f:.48f,.3f+row*.63f,.04f),col==0?-3:6);
                Box(box,"Worn metal case",V(0,.17f,0),V(.62f,.34f,.42f),"Aged painted equipment",.025f);
                Box(box,"Case lid seam",V(0,.3f,-.02f),V(.64f,.035f,.44f),"Oxidized structural steel");
                Box(box,"Case closure",V(0,.19f,-.23f),V(.1f,.13f,.04f),"Worn brass fasteners");
            }
        }
        return holder;
    }
    static IEnumerable<Vector3> Corners(Bounds b)
    {for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)yield return b.center+Vector3.Scale(b.extents,V(x,y,z));}
    static void ReplaceDrums(Transform arena,float floor)
    {
        foreach(var t in arena.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Storage drum")).ToArray())
        {
            foreach(var r in t.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            var bounds=t.GetComponent<BoxCollider>().bounds;
            var root=Group(arena,"Scanned replacement for "+t.name,V(0,floor,0));
            var feet=arena.InverseTransformPoint(V(bounds.center.x,bounds.min.y,bounds.center.z));feet.y-=floor;
            Model(root,"barrel_03",feet,bounds.size.y,t.localEulerAngles.y,false);
        }
    }
    static GameObject MeshObject(Transform parent,string name,Mesh mesh,Vector3 position,string material)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mats[material];return go;
    }
    static Mesh Beveled(Vector3 size,float radius)
    {
        radius=Mathf.Min(radius,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.45f);
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        var half=size*.5f;var core=half-Vector3.one*radius;
        foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
        {
            Vector3 u=Mathf.Abs(normal.y)>.5f?Vector3.right:Vector3.up;
            Vector3 v=Vector3.Cross(normal,u);float a=Vector3.Dot(half,Abs(u)),b=Vector3.Dot(half,Abs(v)),h=Vector3.Dot(half,Abs(normal));
            float[] xs={-a,-a+radius,a-radius,a},ys={-b,-b+radius,b-radius,b};int start=vertices.Count;
            for(int y=0;y<4;y++)for(int x=0;x<4;x++)
            {
                var p=normal*h+u*xs[x]+v*ys[y];var clamp=V(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));
                var n=(p-clamp).normalized;vertices.Add(clamp+n*radius);normals.Add(n);uv.Add(new Vector2(xs[x],ys[y]));
            }
            for(int y=0;y<3;y++)for(int x=0;x<3;x++){int i=start+y*4+x;triangles.AddRange(new[]{i,i+1,i+5,i,i+5,i+4});}
        }
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateTangents();mesh.RecalculateBounds();temporaryMeshes.Add(mesh);return mesh;
    }
    static Vector3 Abs(Vector3 p)=>V(Mathf.Abs(p.x),Mathf.Abs(p.y),Mathf.Abs(p.z));
    static GameObject Box(Transform p,string name,Vector3 position,Vector3 size,string material,float bevel=.015f)
        =>MeshObject(p,name,Beveled(size,bevel),position,material);
    static void Beam(Transform p,string name,Vector3 a,Vector3 b,float width,string material)
    {var go=Box(p,name,(a+b)*.5f,V(width,(b-a).magnitude,width),material);go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void Tube(Transform p,string name,Vector3[] points,float radius,string material)
    {
        const int sides=12;var vs=new List<Vector3>();var ns=new List<Vector3>();var uv=new List<Vector2>();var ts=new List<int>();float length=0;
        for(int i=0;i<points.Length;i++)
        {
            if(i>0)length+=Vector3.Distance(points[i-1],points[i]);
            var tangent=(points[Mathf.Min(points.Length-1,i+1)]-points[Mathf.Max(0,i-1)]).normalized;
            var a=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.9f?Vector3.forward:Vector3.up).normalized;var b=Vector3.Cross(tangent,a);
            for(int j=0;j<=sides;j++){float angle=j*2*Mathf.PI/sides;var n=a*Mathf.Cos(angle)+b*Mathf.Sin(angle);vs.Add(points[i]+n*radius);ns.Add(n);uv.Add(new Vector2(j/(float)sides,length));}
            if(i>0)for(int j=0;j<sides;j++){int k=i*(sides+1)+j;ts.AddRange(new[]{k,k-sides,k+1,k,k-sides-1,k-sides});}
        }
        var mesh=new Mesh();mesh.SetVertices(vs);mesh.SetNormals(ns);mesh.SetUVs(0,uv);mesh.SetTriangles(ts,0);mesh.RecalculateTangents();mesh.RecalculateBounds();temporaryMeshes.Add(mesh);
        MeshObject(p,name,mesh,Vector3.zero,material);
    }
    static void Ring(Transform p,string name,Vector3 center,float radius,float thickness,string mat)
        =>Tube(p,name,Enumerable.Range(0,49).Select(i=>center+V(Mathf.Cos(i*Mathf.PI/24)*radius,Mathf.Sin(i*Mathf.PI/24)*radius,0)).ToArray(),thickness,mat);
    static void Fixture(Transform p,Vector3 position,float yaw,bool cool=false)
    {
        var g=Group(p,cool?"Service inspection luminaire":"Caged sanctum luminaire",position,yaw);
        Box(g,"Die cast housing",Vector3.zero,V(.38f,.72f,.2f),"Oxidized structural steel",.035f);
        Box(g,"Frosted practical diffuser",V(0,0,-.13f),V(.24f,.54f,.06f),"Aged lamp diffuser",.025f);
        foreach(float x in new[]{-.14f,.14f})Beam(g,"Protective cage",V(x,-.3f,-.19f),V(x,.3f,-.19f),.025f,"Oxidized structural steel");
        foreach(float y in new[]{-.27f,0,.27f})Beam(g,"Cage crossbar",V(-.16f,y,-.19f),V(.16f,y,-.19f),.025f,"Oxidized structural steel");
        var l=Group(g,"Light from diffuser",V(0,0,-.3f)).gameObject.AddComponent<Light>();l.type=LightType.Point;
        l.color=cool?new Color(.65f,.78f,.76f):new Color(1,.72f,.39f);l.intensity=cool?5:6;l.range=cool?8:10;l.shadows=LightShadows.None;
    }
    static void DressArena(Transform a)
    {
        var west=Group(a,"Interrupted pump maintenance",V(-6.6f,1,-3.8f),90);
        Model(west,"steel_frame_shelves_02",V(-1.8f,0,0),2.45f,0);
        Model(west,"metal_tool_chest",V(.1f,0,.35f),1.05f,-5);
        Model(west,"industrial_storage_cart",V(2.6f,0,1.15f),1.2f,12);
        Model(west,"barrel_03",V(-1.8f,0,1.2f),1.15f,28);
        Model(west,"barrel_03",V(-.5f,0,1.45f),1.15f,-16);
        var east=Group(a,"Contaminated storage bay",V(26.9f,1,6.4f),-90);
        Model(east,"steel_frame_shelves_02",V(-2.5f,0,0),2.6f,0);
        Model(east,"steel_frame_shelves_02",V(.1f,0,0),2.6f,0);
        Model(east,"barrel_03",V(1.3f,0,1.2f),1.1f,45);
        Model(east,"barrel_03",V(.4f,0,1.3f),1.1f,15);
        Model(east,"industrial_storage_cart",V(-1.2f,0,1.8f),1.2f,-14);
        Model(a,"metal_tool_chest",V(14.5f,0,15.6f),1.05f,173);
        Model(a,"industrial_storage_cart",V(17.5f,0,-11.8f),1.2f,24);
        // High services add depth without occupying the combat floor or spawn lanes.
        var services=Group(a,"Braced service gantry and cable routes",Vector3.zero);
        foreach(float z in new[]{-6f,6f})
        {
            Box(services,"Gantry lower flange",V(10,7.7f,z),V(37,.13f,.36f),"Oxidized structural steel");
            Box(services,"Gantry upper flange",V(10,8.35f,z),V(37,.13f,.36f),"Oxidized structural steel");
            for(int i=0;i<18;i++)Beam(services,"Triangular web stiffener",V(-8+i*2,7.8f,z),V(-6+i*2,8.25f,z),.055f,"Aged painted equipment");
            foreach(float x in new[]{-8.4f,28.4f})
            {Box(services,"Wall fixing plate",V(x,7.9f,z),V(.14f,1.3f,.9f),"Oxidized structural steel");Beam(services,"Gantry haunch",V(x,6.7f,z),V(x-Mathf.Sign(x-10)*1.7f,7.65f,z),.1f,"Oxidized structural steel");}
        }
        foreach(int side in new[]{-1,1})
        {
            float x=10+side*19.1f;
            for(int k=0;k<3;k++)Tube(services,"Sagged insulated feed",Enumerable.Range(0,37).Select(i=>V(x-side*k*.16f,6.1f-Mathf.Sin(i*Mathf.PI/36)*.32f,-14+i*.89f)).ToArray(),.035f,"Black rubber cable");
            foreach(float z in new[]{-8f,3f,14f})
            {
                Box(services,"Cable saddle",V(x-side*.2f,6,z),V(.8f,.08f,.22f),"Oxidized structural steel");
                var reel=Group(services,"Coiled maintenance hose",V(x-side*.25f,1.7f,z),side<0?90:-90);
                for(int j=0;j<4;j++)Ring(reel,"Rubber hose winding",V(0,0,-j*.055f),.43f,.032f,"Black rubber cable");
                Beam(reel,"Hose cradle",V(-.4f,-.35f,0),V(.4f,-.35f,0),.07f,"Aged painted equipment");
            }
        }
        // Distinct groups of fallen masonry, localized at wall junctions.
        var debris=Group(a,"Masonry from damp service walls",Vector3.zero);
        var rng=new System.Random(141026);
        foreach(var center in new[]{V(-7.8f,0,12.2f),V(27.6f,0,-10.5f),V(25.6f,0,15.7f)})
        for(int i=0;i<28;i++)
        {
            float dx=(float)rng.NextDouble()*2.5f-1.25f,dz=(float)rng.NextDouble()*2.8f-1.4f;
            var size=V(.1f+(float)rng.NextDouble()*.32f,.04f+(float)rng.NextDouble()*.11f,.1f+(float)rng.NextDouble()*.38f);
            var go=Box(debris,"Spalled concrete fragment",center+V(dx,size.y*.5f,dz),size,"Dry chipped masonry",.014f);go.transform.localRotation=Quaternion.Euler(0,rng.Next(360),rng.Next(-12,13));
        }
        Fixture(a,V(-8.9f,3.3f,-3.8f),90,true);Fixture(a,V(28.9f,3.3f,6.4f),-90,true);
        MaintenanceIsland(a);
    }
    static void MaintenanceIsland(Transform a)
    {
        var island=Group(a,"Abandoned repair in the combat hall",V(14,0,1),-14);
        Model(island,"industrial_storage_cart",Vector3.zero,1.12f,0);
        Model(island,"metal_tool_chest",V(-1.4f,0,1.1f),.48f,27);
        Model(island,"barrel_03",V(1.1f,0,1.5f),1.05f,56);
        for(int i=0;i<5;i++)
        {
            var go=Box(island,"Discarded pipe offcut",V(-.7f+i*.18f,.035f,-.6f-i*.13f),V(.055f,.05f,.42f),"Oxidized structural steel");
            go.transform.localRotation=Quaternion.Euler(0,i*22,0);
        }
    }
    public static void Refine()
    {
        BasementSurfaceBuilder.RequireIsolated();meshIndex=100;mats.Clear();
        foreach(var path in Directory.GetFiles(Art+"/Materials","*.mat"))
        {var m=AssetDatabase.LoadAssetAtPath<Material>(path);mats[m.name]=m;mats[m.name.Replace('_',' ')]=m;}
        var room=PrefabUtility.LoadPrefabContents(Room);
        try
        {
            var a=room.transform.Find("PreBossArea/Industrial maintenance dressing");
            if(a.Find("Abandoned repair in the combat hall"))throw new Exception("Island already installed");
            MaintenanceIsland(a);Combine(a);PrefabUtility.SaveAsPrefabAsset(room,Room);
        }
        finally{PrefabUtility.UnloadPrefabContents(room);foreach(var m in temporaryMeshes)if(m)Object.DestroyImmediate(m);temporaryMeshes.Clear();}
        AssetDatabase.SaveAssets();Capture("Polished");Debug.Log("BASEMENT_POLISH_BUILD_OK");
    }
    static void DressBoss(Transform b)
    {
        var frame=Group(b,"Riveted reliquary frame",V(-10,0,35.15f));
        foreach(float side in new[]{-1f,1f})
        {
            float x=side*5.3f;
            Box(frame,"Weathered reliquary jamb",V(x,5.65f,0),V(.32f,9.8f,.48f),"Oxidized structural steel",.035f);
            Box(frame,"Jamb base shoe",V(x,.42f,0),V(.62f,.8f,.68f),"Oxidized structural steel",.045f);
            for(int j=0;j<7;j++)
            {
                Box(frame,"Riveted junction strap",V(x,1.5f+j*1.2f,-.28f),V(.5f,.14f,.08f),"Aged painted equipment");
                foreach(float dx in new[]{-.15f,.15f})Box(frame,"Aged bolt head",V(x+dx,1.5f+j*1.2f,-.335f),V(.055f,.055f,.025f),"Worn brass fasteners",.01f);
            }
            Beam(frame,"Upper corner brace",V(x,7.5f,-.02f),V(x-side*1.8f,9.3f,-.02f),.15f,"Oxidized structural steel");
            Fixture(frame,V(x-side*.6f,4.5f,-.58f),0);
        }
        Box(frame,"Deep lintel flange",V(0,10.65f,0),V(11.2f,.32f,.72f),"Oxidized structural steel",.04f);
        for(int i=0;i<20;i++)Box(frame,"Lintel fastener",V(-5+i*.52f,10.65f,-.38f),V(.065f,.065f,.04f),"Worn brass fasteners");
        var altar=Group(b,"Altar metalwork and drainage",V(-10,0,31.5f));
        foreach(float y in new[]{1.28f,2.98f})Box(altar,"Beveled altar face rail",V(0,y,-.61f),V(5.65f,.12f,.15f),"Oxidized structural steel",.035f);
        foreach(float x in new[]{-2.73f,2.73f})Box(altar,"Altar face corner strap",V(x,2.12f,-.61f),V(.13f,1.7f,.15f),"Oxidized structural steel",.025f);
        foreach(float x in new[]{-2.73f,-1.72f,-.86f,0,.86f,1.72f,2.73f})foreach(float y in new[]{1.28f,2.98f})
            Box(altar,"Altar face rivet",V(x,y,-.7f),V(.065f,.065f,.035f),"Worn brass fasteners");
        foreach(float side in new[]{-1f,1f})
        {
            float x=side*3.2f;
            Box(altar,"Altar edging",V(x,1.03f,-.2f),V(.12f,.14f,3.9f),"Oxidized structural steel");
            for(int i=0;i<5;i++)Box(altar,"Plinth worn joint",V(x,1.1f,-1.8f+i*.8f),V(.2f,.06f,.1f),"Worn brass fasteners");
            Tube(altar,"Engine pressure feed",new[]{V(side*4.3f,1.1f,.3f),V(side*4.3f,1.1f,1.2f),V(side*4.65f,1.1f,1.65f),V(side*4.65f,2.5f,1.65f),V(side*4.7f,3.1f,2.2f)},.09f,"Oxidized structural steel");
            Ring(altar,"Pressure feed bolted flange",V(side*4.3f,1.1f,.7f),.19f,.035f,"Worn brass fasteners");
        }
        // Side inspection bays keep the complete center and lateral boss loop open.
        Model(b,"industrial_storage_cart",V(-27.1f,0,18),1.2f,55);
        Model(b,"metal_tool_chest",V(7.7f,0,13),1.1f,-85);
        var panels=Group(b,"Industrial wall recess frames",Vector3.zero);
        foreach(int side in new[]{-1,1})foreach(float z in new[]{5f,16f,27f})
        {
            float x=-10+side*20.45f;var panel=Group(panels,"Recessed service grille",V(x,3.4f,z),side<0?90:-90);
            Box(panel,"Grille backplate",Vector3.zero,V(2.9f,3.1f,.09f),"Black rubber cable");
            foreach(float sx in new[]{-1.5f,1.5f})Box(panel,"Grille frame upright",V(sx,0,-.09f),V(.12f,3.3f,.12f),"Oxidized structural steel");
            for(int j=0;j<13;j++)Box(panel,"Angled louvre",V(0,-1.4f+j*.23f,-.12f),V(2.9f,.09f,.16f),"Aged painted equipment");
            Tube(panel,"Mineral runoff cable",new[]{V(1.65f,4.2f,0),V(1.65f,.2f,0),V(1.8f,-.7f,-.15f),V(1.8f,-3.25f,-.15f)},.035f,"Black rubber cable");
        }
    }
    static void Combine(Transform root)
    {
        // Imported models retain shared meshes. Consolidate only authored static surfaces by material.
        var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>temporaryMeshes.Contains(f.sharedMesh)).ToArray();
        foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
        {
            var mesh=new Mesh();mesh.name=root.name+"_"+group.Key.name;mesh.indexFormat=IndexFormat.UInt32;
            mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
            string path=Art+"/Meshes/"+(meshIndex++)+"_"+group.Key.name.Replace(' ','_')+".asset";
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject("Consolidated "+group.Key.name);go.transform.SetParent(root,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
        }
        foreach(var filter in filters)Object.DestroyImmediate(filter.gameObject);
    }
    public static void Capture(string stage)
    {
        BasementSurfaceBuilder.Capture(stage);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var room=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement"&&!t.parent);
        var a=room.Find("PreBossArea");var b=room.Find("BossRoom");
        float af=BasementFinalBuilder.Floor(a,"Hall_Floor"),bf=BasementFinalBuilder.Floor(b,"Main_Floor"),front=b.Find("Walls/Entrance_Wall_Header").localPosition.z;
        var method=typeof(BasementSurfaceBuilder).GetMethod("Render",BindingFlags.NonPublic|BindingFlags.Static);
        method.Invoke(null,new object[]{room,a,V(2,af+1.84f,-12),V(-6,af+1.5f,-2),stage+"-Maintenance",false});
        method.Invoke(null,new object[]{room,a,V(18,af+1.84f,-4),V(27,af+1.8f,7),stage+"-Storage",false});
        method.Invoke(null,new object[]{room,a,V(11,af+1.84f,-3),V(14,af+.9f,2),stage+"-Equipment",true});
        method.Invoke(null,new object[]{room,b,V(0,bf+1.84f,front+2),V(-10,bf+4.8f,front+32),stage+"-BossWide",false});
    }
    public static void Verify()
    {
        BasementSurfaceBuilder.RequireIsolated();
        BasementSurfaceBuilder.FinalCheck();
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var room=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement"&&!t.parent);
        var arena=room.Find("PreBossArea/Industrial maintenance dressing");var boss=room.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Sanctum architectural polish");
        if(!arena || !boss)throw new Exception("New dressing absent from map instance");
        var encounter=room.GetComponentInChildren<BasementArenaEncounter>();
        var newColliders=arena.GetComponentsInChildren<Collider>().Concat(boss.GetComponentsInChildren<Collider>()).ToArray();
        Physics.SyncTransforms();
        foreach(var point in encounter.spawnPoints)foreach(var collider in newColliders)
            if(Vector3.Distance(collider.ClosestPoint(point.position),point.position)<1.1f)throw new Exception("New decoration obstructs spawn "+point.name);
        int triangles=0,draws=0;
        foreach(var t in new[]{arena,boss})foreach(var f in t.GetComponentsInChildren<MeshFilter>())
        {
            var r=f.GetComponent<MeshRenderer>();if(!r || !r.enabled)continue;
            if(!f.sharedMesh)throw new Exception("Missing added mesh "+f.name);
            triangles+=f.sharedMesh.triangles.Length/3;draws+=r.sharedMaterials.Length;
            foreach(var m in r.sharedMaterials)if(!m || !m.shader || m.shader.name=="Hidden/InternalErrorShader")throw new Exception("Invalid added material");
        }
        foreach(string id in new[]{"industrial_storage_cart","barrel_03","metal_tool_chest","steel_frame_shelves_02"})
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+id+".mat");
            if(!m || !m.GetTexture("_BaseMap") || !m.GetTexture("_BumpMap") || !m.GetTexture("_MetallicGlossMap") || !m.GetTexture("_OcclusionMap"))throw new Exception("Incomplete PBR model "+id);
        }
        if(triangles>350000 || draws>80)throw new Exception("Added decoration exceeds budget: "+triangles+" tris, "+draws+" slots");
        foreach(var shader in new[]{Shader.Find("Universal Render Pipeline/Lit"),Shader.Find("UniDoom/TotalChaosInspiredSurface")})
        foreach(var message in ShaderUtil.GetShaderMessages(shader))if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(message.message);
        Directory.CreateDirectory("Preview/Polish");
        File.WriteAllText("Preview/Polish/validation.txt","Unity "+Application.unityVersion+" / URP\nActual CharacterController routes, key doors and arena encounter passed in prefab and map.\nAll 12 spawn points clear of new colliders (1.1m).\nAdded arena/boss triangles: "+triangles+"; material slots: "+draws+"; new lights: 4 shadowless.\nFour imported FBX assets have complete albedo, GL normal, AO and metallic/smoothness bindings.\nMain scene and original gameplay components are preserved by the installer.\n");
        Capture("FinalPolish");
        Debug.Log("BASEMENT_POLISH_VERIFIED");
    }
    public static void Inspect()
    {
        BasementSurfaceBuilder.RequireIsolated();
        Directory.CreateDirectory("Preview/Polish");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var root=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement" && !t.parent);
        var lines=new List<string>();
        foreach(string areaName in new[]{"PreBossArea","BossRoom"})
        {
            var area=root.Find(areaName);
            foreach(var t in area.GetComponentsInChildren<Transform>(true))
            {
                int depth=0;var p=t;while(p!=area){depth++;p=p.parent;}
                if(depth>3)continue;
                var r=t.GetComponent<Renderer>();var c=t.GetComponent<Collider>();
                lines.Add(areaName+"/"+new string(' ',depth*2)+t.name+" p="+t.localPosition+" s="+t.localScale+" active="+t.gameObject.activeInHierarchy+(r?" renderer="+r.enabled+" bounds="+r.bounds+" mat="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL")):"")+(c?" collider="+c.bounds:"") );
            }
        }
        File.WriteAllLines("Preview/Polish/hierarchy.txt",lines);
        BasementSurfaceBuilder.Capture("Current");
        foreach(string id in new[]{"industrial_storage_cart","barrel_03","metal_tool_chest","steel_frame_shelves_02"})
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/"+id+"/"+id+".fbx");
            if(!model)continue;
            lines.Add("MODEL "+id+" scale "+model.transform.localScale);
            foreach(var f in model.GetComponentsInChildren<MeshFilter>())lines.Add(f.name+" bounds="+f.sharedMesh.bounds+" tris="+f.sharedMesh.triangles.Length/3);
        }
        File.WriteAllLines("Preview/Polish/hierarchy.txt",lines);
        Debug.Log("BASEMENT_POLISH_INSPECT_OK");
    }
}
