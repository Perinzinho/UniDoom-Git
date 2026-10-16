using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object = UnityEngine.Object;

// One-off authoring tool. Run in .utmp/LibraryPreview; no runtime generation.
public static class LibraryBuilder
{
    public const string Art = "Assets/_Project/Scenes/Library";
    public const string Prefab = Art + "/Prefabs/Library.prefab";
    public static readonly Vector3 MapScale = new Vector3(.91267f, .5515947f, 1);
    public static readonly Vector3 FloorOrigin = new Vector3(-47.207268f, .99784184f, 2.9278717f);
    public static readonly Color Ambient = new Color(.30f, .315f, .29f);
    static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static readonly Dictionary<string, GameObject> prototypes = new Dictionary<string, GameObject>();
    static readonly Dictionary<string, Mesh> simple = new Dictionary<string, Mesh>();
    static int serial;
    public static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
    static Color C(float r,float g,float b) => new Color(r,g,b);

    public static void Build()
    {
        if (!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/")) throw new Exception("Use isolated LibraryPreview");
        foreach (var f in new[]{"Materials","Meshes","Profiles","Prefabs/Modules"}) Directory.CreateDirectory(Art+"/"+f);
        AssetDatabase.Refresh(); serial=0; mats.Clear(); prototypes.Clear(); simple.Clear();
        Materials(); PrepareModels();PrepareSingleBook();
        var root=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            root.transform.localScale=MapScale;
            var previous=root.transform.Find("Abandoned Library Interior");
            if(previous)Object.DestroyImmediate(previous.gameObject);
            // Persist the existing ProBuilder shell, keeping every existing object/component ID.
            foreach(var pb in root.GetComponentsInChildren<ProBuilderMesh>())
            {
                pb.ToMesh(); pb.Refresh();
                var mesh=Object.Instantiate(pb.GetComponent<MeshFilter>().sharedMesh);mesh.name="LibraryShell_"+pb.name;
                mesh=SaveMesh(mesh,mesh.name);pb.GetComponent<MeshFilter>().sharedMesh=mesh;
                var mc=pb.GetComponent<MeshCollider>();if(mc)mc.sharedMesh=mesh;
                var so=new SerializedObject(pb);var p=so.FindProperty("m_Mesh");if(p!=null){p.objectReferenceValue=mesh;so.ApplyModifiedPropertiesWithoutUndo();}
                pb.GetComponent<MeshRenderer>().sharedMaterials=new[]{mats[pb.name=="Floor"?"Stone":pb.name=="Roof"?"Plaster":"Plaster"]};
            }
            var interior=Group(root.transform,"Abandoned Library Interior",FloorOrigin);
            interior.localScale=V(1/MapScale.x,1/MapScale.y,1);
            Architecture(interior); SplitLightingSurfaces(interior);Shelving(interior); ReadingRoom(interior); FocalWall(interior); Abandonment(interior); Atmosphere(interior);
            PrefabUtility.SaveAsPrefabAsset(root,Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        foreach(var p in prototypes.Values)Object.DestroyImmediate(p);
        AssetDatabase.SaveAssets();
        Debug.Log("LIBRARY_BUILD_PASS");
        LibraryVerifier.VerifyAndRender();
    }

    static void Materials()
    {
        foreach(var path in Directory.GetFiles(Art+"/Textures","*.png",SearchOption.AllDirectories))
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(path); if(!ti)continue;
            bool normal=path.Contains("nor_gl");bool color=path.EndsWith("Diffuse.png")||path.Contains("_diff.")||path.Contains("Carpet.png");
            ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            ti.sRGBTexture=color;ti.mipmapEnabled=true;ti.maxTextureSize=path.Contains("MuseumRug")?2048:1024;
            ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.textureCompression=TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }
        foreach(var id in new[]{"wooden_bookshelf_worn","dining_chair_02","wooden_table_02","marble_bust_01","wooden_ladder","drawer_cabinet","dirty_carpet","worn_mossy_plasterwall"})
            PBR(id,Art+"/Textures/"+id+"/","Diffuse","nor_gl","arm",Color.white);
        PBR("BookCover",Art+"/Textures/book_encyclopedia_set_01/","cover_diff","cover_nor_gl","cover_arm",Color.white);
        PBR("BookPaper",Art+"/Textures/book_encyclopedia_set_01/","paper_diff","paper_nor_gl","paper_arm",C(.82f,.79f,.68f));
        mats["wooden_bookshelf_worn"].SetColor("_BaseColor",C(.85f,.72f,.56f));
        PBR("Wood","Assets/_Project/Scenes/ClassRoom/Surface/Textures/wood_table_worn/","Diffuse","nor_gl","arm",C(.43f,.29f,.18f));
        PBR("Stone","Assets/_Project/Scenes/ClassRoom/Surface/Textures/worn_tile_floor/","Diffuse","nor_gl","arm",C(.64f,.62f,.55f));
        PBR("Brass","Assets/_Project/Scenes/ClassRoom/Surface/Textures/rusty_metal_02/","Diffuse","nor_gl","arm",C(.54f,.35f,.14f));
        Clone("Plaster","worn_mossy_plasterwall",C(.70f,.65f,.53f));
        Clone("DarkWood","Wood",C(.24f,.16f,.10f));
        Clone("RugBorder","dirty_carpet",C(.21f,.025f,.025f));
        Clone("Rug","dirty_carpet",C(.65f,.55f,.43f));
        if(File.Exists(Art+"/Textures/MuseumRug/Carpet.png"))mats["Rug"].SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/MuseumRug/Carpet.png"));
        Flat("Iron",C(.07f,.065f,.05f),.3f,.45f);
        Flat("GreenGlass",C(.035f,.16f,.085f),.28f,.1f);
        Flat("LampGlow",C(.94f,.73f,.39f),.25f,0,C(1,.68f,.28f)*2.2f);
        Flat("TubeGlow",C(.72f,.77f,.57f),.15f,0,C(.8f,.84f,.57f)*2.3f);
        Flat("RedGlass",C(.15f,.015f,.022f),.35f,0,C(.22f,.012f,.023f)*.6f);
        Flat("Paper",C(.51f,.47f,.34f),.03f);
        Flat("Ink",C(.08f,.075f,.055f),.02f);
        Flat("Dust",C(.20f,.17f,.12f),.02f);
        Flat("CRT",C(.008f,.018f,.014f),.18f,0,C(.01f,.1f,.045f)*.45f);
        Flat("Beige",C(.38f,.36f,.27f),.15f);
        Flat("CrestGold",C(.58f,.40f,.17f),.3f,.45f);
        Clone("Banner","dirty_carpet",C(.23f,.018f,.025f));
        foreach(var pair in mats)EditorUtility.SetDirty(pair.Value);
    }

    static Material PBR(string name,string folder,string diff,string normal,string arm,Color tint)
    {
        var m=NewMat(name);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+diff+".png"));
        m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+normal+".png"));
        if(!m.GetTexture("_BaseMap")||!m.GetTexture("_BumpMap"))throw new Exception("Missing PBR "+name);
        string path=File.Exists(folder+"UnityMask.png")?folder+"UnityMask.png":Art+"/Textures/PackedMasks/"+name+".png";
        if(!File.Exists(folder+"UnityMask.png"))
        {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);t.LoadImage(File.ReadAllBytes(folder+arm+".png"));
        var px=t.GetPixels32();for(int i=0;i<px.Length;i++){var c=px[i];px[i]=new Color32(c.b,c.r,c.g,(byte)(255-c.g));}
        t.SetPixels32(px);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
        var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.sRGBTexture=false;ti.mipmapEnabled=true;ti.maxTextureSize=1024;ti.SaveAndReimport();
        }
        var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        m.SetTexture("_MetallicGlossMap",mask);m.SetTexture("_OcclusionMap",mask);m.SetTexture("_DirtMap",mask);
        m.SetColor("_BaseColor",tint);m.SetVector("_SpecColor",new Vector4(.16f,.015f,.88f,1));
        m.SetFloat("_BumpScale",.65f);m.SetFloat("_Smoothness",.55f);m.SetFloat("_OcclusionStrength",.75f);
        foreach(var k in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP","_TC_SHARED_MASK"})m.EnableKeyword(k);
        return m;
    }
    static Material NewMat(string name)
    {
        string path=Art+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("UniDoom/TotalChaosInspiredSurface")){name=name};AssetDatabase.CreateAsset(m,path);}
        m.enableInstancing=true;mats[name]=m;return m;
    }
    static void Clone(string name,string source,Color tint){var m=NewMat(name);m.CopyPropertiesFromMaterial(mats[source]);m.SetColor("_BaseColor",tint);}
    static void Flat(string name,Color color,float smooth,float metal=0,Color emission=default)
    {
        var m=NewMat(name);m.shaderKeywords=Array.Empty<string>();m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);
        if(emission.maxColorComponent>0){m.SetColor("_EmissionColor",emission);m.EnableKeyword("_EMISSION");}
    }

    static void PrepareModels()
    {
        foreach(var id in new[]{"wooden_bookshelf_worn","book_encyclopedia_set_01","dining_chair_02","wooden_table_02","marble_bust_01","wooden_ladder","drawer_cabinet"})
        {
            string path=Art+"/Models/"+id+"/model.fbx";var imp=(ModelImporter)AssetImporter.GetAtPath(path);
            imp.isReadable=true;imp.importAnimation=false;imp.importCameras=false;imp.importLights=false;imp.SaveAndReimport();
            var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));source.transform.position=Vector3.zero;
            var proto=new GameObject(id);var rs=source.GetComponentsInChildren<MeshRenderer>();
            var bounds=rs[0].bounds;foreach(var renderer in rs)bounds.Encapsulate(renderer.bounds);
            float height=id=="book_encyclopedia_set_01"?.29f:id=="wooden_table_02"?.79f:id=="dining_chair_02"?1.02f:id=="marble_bust_01"?.72f:id=="wooden_ladder"?3.4f:id=="drawer_cabinet"?1.18f:1.88f;
            float scale=height/bounds.size.y;
            var matrix=Matrix4x4.TRS(-V(bounds.center.x,bounds.min.y,bounds.center.z)*scale,Quaternion.identity,Vector3.one*scale);
            var byMat=new Dictionary<Material,List<CombineInstance>>();
            foreach(var r in rs)
            {
                var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;
                for(int i=0;i<f.sharedMesh.subMeshCount;i++)
                {
                    string original=r.sharedMaterials[Mathf.Min(i,r.sharedMaterials.Length-1)].name.ToLowerInvariant();
                    var m=mats[id=="book_encyclopedia_set_01"?(original.Contains("paper")?"BookPaper":"BookCover"):id];
                    if(!byMat.ContainsKey(m))byMat[m]=new List<CombineInstance>();
                    byMat[m].Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=matrix*f.transform.localToWorldMatrix});
                }
            }
            var near=new List<Renderer>();var far=new List<Renderer>();int index=0;
            foreach(var pair in byMat)
            {
                var mesh=new Mesh{name=id+"_"+index,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray());mesh.RecalculateBounds();
                if(id=="book_encyclopedia_set_01"){var reduced=Simplify(mesh,180,true);reduced.name=mesh.name;Object.DestroyImmediate(mesh);mesh=reduced;}
                mesh=SaveMesh(mesh,mesh.name);
                near.Add(MeshObject(proto.transform,"Detailed imported model",mesh,pair.Key));
                var low=Simplify(mesh,id=="book_encyclopedia_set_01"?44:38);low.name=mesh.name+"_LOD";low=SaveMesh(low,low.name);
                far.Add(MeshObject(proto.transform,"Distance model",low,pair.Key));index++;
            }
            var lod=proto.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(id=="book_encyclopedia_set_01"?.24f:.075f,near.ToArray()),new LOD(.006f,far.ToArray())});lod.RecalculateBounds();
            prototypes[id]=proto;Object.DestroyImmediate(source);
            PrefabUtility.SaveAsPrefabAsset(proto,Art+"/Prefabs/Modules/"+id+".prefab");
        }
    }
    static Mesh Simplify(Mesh source,int resolution,bool keepSeams=false)
    {
        var v=source.vertices;var n=source.normals;var uv=source.uv;var vertices=new List<Vector3>();var normals=new List<Vector3>();var tex=new List<Vector2>();
        var dict=new Dictionary<(Vector3Int,int,int,int),int>();var map=new int[v.Length];float cell=source.bounds.size.maxComponent()/resolution;
        for(int i=0;i<v.Length;i++)
        {
            var p=v[i];var cellKey=new Vector3Int(Mathf.RoundToInt(p.x/cell),Mathf.RoundToInt(p.y/cell),Mathf.RoundToInt(p.z/cell));
            var norm=n.Length>i?n[i]:Vector3.up;var tc=uv.Length>i?uv[i]:Vector2.zero;
            int normalKey=keepSeams?(Mathf.RoundToInt(norm.x*2)+2)*25+(Mathf.RoundToInt(norm.y*2)+2)*5+Mathf.RoundToInt(norm.z*2)+2:0;
            var key=(cellKey,normalKey,keepSeams?Mathf.FloorToInt(tc.x*32):0,keepSeams?Mathf.FloorToInt(tc.y*32):0);
            if(!dict.TryGetValue(key,out int j)){j=vertices.Count;dict[key]=j;vertices.Add(p);normals.Add(n.Length>i?n[i]:Vector3.up);tex.Add(uv.Length>i?uv[i]:Vector2.zero);}map[i]=j;
        }
        var triangles=new List<int>();var src=source.triangles;
        for(int i=0;i<src.Length;i+=3){int a=map[src[i]],b=map[src[i+1]],c=map[src[i+2]];if(a!=b&&b!=c&&a!=c){triangles.Add(a);triangles.Add(b);triangles.Add(c);}}
        var m=new Mesh{indexFormat=IndexFormat.UInt32};m.SetVertices(vertices);m.SetNormals(normals);m.SetUVs(0,tex);m.SetTriangles(triangles,0);m.RecalculateTangents();m.RecalculateBounds();return m;
    }
    static float maxComponent(this Vector3 v)=>Mathf.Max(v.x,Mathf.Max(v.y,v.z));
    static void PrepareSingleBook()
    {
        var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Models/book_encyclopedia_set_01/model.fbx"));source.transform.position=Vector3.zero;
        var f=source.GetComponentsInChildren<MeshFilter>().OrderBy(x=>x.name).First();var renderer=f.GetComponent<MeshRenderer>();var b=renderer.bounds;
        var matrix=Matrix4x4.TRS(-V(b.center.x,b.min.y,b.center.z)*(.29f/b.size.y),Quaternion.identity,Vector3.one*(.29f/b.size.y));
        var proto=new GameObject("single_book");
        for(int i=0;i<f.sharedMesh.subMeshCount;i++)
        {
            var mesh=new Mesh{name="SingleBook_"+i};mesh.CombineMeshes(new[]{new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=matrix*f.transform.localToWorldMatrix}});
            mesh=SaveMesh(mesh,mesh.name);MeshObject(proto.transform,"Leather binding and pages",mesh,mats[renderer.sharedMaterials[i].name.ToLowerInvariant().Contains("paper")?"BookPaper":"BookCover"]);
        }
        prototypes["single_book"]=proto;PrefabUtility.SaveAsPrefabAsset(proto,Art+"/Prefabs/Modules/single_book.prefab");Object.DestroyImmediate(source);
    }
    static Mesh SaveMesh(Mesh m,string name)
    {
        string path=Art+"/Meshes/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static MeshRenderer MeshObject(Transform parent,string name,Mesh mesh,Material mat)
    {
        var t=Group(parent,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.receiveShadows=true;return r;
    }
    public static Bounds BoundsIn(Transform parent,Renderer[] rs)
    {
        Bounds? result=null;
        foreach(var r in rs)for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
        {
            var p=parent.InverseTransformPoint(r.transform.TransformPoint(r.localBounds.center+Vector3.Scale(r.localBounds.extents,V(x,y,z))));
            if(!result.HasValue)result=new Bounds(p,Vector3.zero);else{var b=result.Value;b.Encapsulate(p);result=b;}
        }
        return result??new Bounds();
    }
    static Transform Model(Transform parent,string id,string name,Vector3 p,float yaw=0,bool collision=true)
    {
        var t=Object.Instantiate(prototypes[id],parent).transform;t.name=name;t.localPosition=p;t.localRotation=Quaternion.Euler(0,yaw,0);
        if(collision){var b=BoundsIn(t,t.GetComponentsInChildren<Renderer>());var c=t.gameObject.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;}
        return t;
    }
    public static Transform Group(Transform parent,string name,Vector3 p)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
    static Transform Box(Transform parent,string name,Vector3 p,Vector3 size,string material,bool collision=false,float yaw=0)
    {
        var t=Group(parent,name,p);t.localRotation=Quaternion.Euler(0,yaw,0);
        string key="Box_"+size.x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+size.y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+size.z.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
        if(!simple.TryGetValue(key,out var mesh))
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(cube);
            var verts=mesh.vertices;var uv=mesh.uv;var normals=mesh.normals;
            for(int i=0;i<verts.Length;i++){verts[i]=Vector3.Scale(verts[i],size);var n=normals[i];uv[i]=Vector2.Scale(uv[i],Mathf.Abs(n.y)>.5f?new Vector2(size.x,size.z):Mathf.Abs(n.x)>.5f?new Vector2(size.z,size.y):new Vector2(size.x,size.y));}
            mesh.vertices=verts;mesh.uv=uv;mesh.RecalculateBounds();mesh.RecalculateTangents();mesh.name=key;mesh=SaveMesh(mesh,key);simple[key]=mesh;
        }
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mats[material];
        if(collision)t.gameObject.AddComponent<BoxCollider>().size=size;
        return t;
    }
    static Transform Lathe(Transform parent,string name,Vector3 p,Vector2[] profile,string material,int segments=32)
    {
        var t=Group(parent,name,p);
        string key="Lathe_"+segments+"_"+string.Join("_",profile.Select(v=>v.x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+v.y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)));
        if(simple.TryGetValue(key,out var cached)){MeshObject(t,"Turned surface",cached,mats[material]);return t;}
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
        for(int j=0;j<profile.Length;j++)for(int i=0;i<=segments;i++){float a=i*Mathf.PI*2/segments;vertices.Add(V(Mathf.Cos(a)*profile[j].x,profile[j].y,Mathf.Sin(a)*profile[j].x));uv.Add(new Vector2((float)i/segments,profile[j].y));}
        for(int j=0;j<profile.Length-1;j++)for(int i=0;i<segments;i++){int a=j*(segments+1)+i,b=a+segments+1;tris.AddRange(new[]{a,b,a+1,a+1,b,b+1});}
        var mesh=new Mesh{name="Turned_"+simple.Count};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();mesh=SaveMesh(mesh,mesh.name);simple[key]=mesh;MeshObject(t,"Turned surface",mesh,mats[material]);return t;
    }
    static Vector2 P(float radius,float height)=>new Vector2(radius,height);
    static void Light(Transform parent,string name,Vector3 p,Color color,float intensity,float range,bool shadows=false,Vector3? target=null,float angle=100)
    {
        var t=Group(parent,name,p);var light=t.gameObject.AddComponent<UnityEngine.Light>();light.type=target.HasValue?LightType.Spot:LightType.Point;
        light.color=color;light.intensity=intensity;light.range=range;light.shadows=shadows?LightShadows.Soft:LightShadows.None;light.shadowBias=.025f;light.shadowNormalBias=.18f;
        if(target.HasValue){t.localRotation=Quaternion.LookRotation(target.Value-p);light.spotAngle=angle;light.innerSpotAngle=angle*.65f;}
        var so=new SerializedObject(t.gameObject.AddComponent<UniversalAdditionalLightData>());
        so.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=1;so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void Architecture(Transform a)
    {
        Box(a,"Worn stone floor",V(0,-.012f,0),V(24.35f,.025f,39.4f),"Stone",true);
        Box(a,"Stained plaster ceiling",V(0,4.43f,0),V(24.5f,.1f,39.7f),"Plaster");
        foreach(float x in new[]{-12.1f,12.1f})
        {
            Box(a,"Damp plaster",V(x,2.25f,0),V(.1f,4.4f,39.6f),"Plaster");
            Box(a,"Dark wainscot",V(x-Mathf.Sign(x)*.08f,.52f,0),V(.12f,1.04f,39.5f),"Wood");
            foreach(float y in new[]{.10f,1.04f,4.10f,4.22f})Box(a,"Carved wall moulding",V(x-Mathf.Sign(x)*.13f,y,0),V(.20f,.09f,39.5f),y>4?"Wood":"DarkWood");
            for(float z=-18;z<=18;z+=3)
            {
                var t=Group(a,"Fluted pilaster",V(x-Mathf.Sign(x)*.25f,0,z));
                Box(t,"Shaft",V(0,2.03f,0),V(.28f,3.9f,.3f),"Wood");
                for(int j=-1;j<=1;j++)Box(t,"Fluting",V(-Mathf.Sign(x)*.16f,2.1f,j*.075f),V(.025f,3.6f,.025f),"DarkWood");
                Box(t,"Capital",V(0,4.04f,0),V(.43f,.18f,.5f),"Wood");Box(t,"Foot",V(0,.12f,0),V(.4f,.24f,.48f),"DarkWood");
            }
        }
        for(float z=-16;z<20;z+=8)
        {
            Box(a,"Coffered ceiling beam",V(0,4.30f,z),V(24,.18f,.21f),"DarkWood");
            foreach(float x in new[]{-7.2f,0,7.2f})Box(a,"Ceiling longitudinal beam",V(x,4.30f,z+3.9f),V(.20f,.18f,7.7f),"DarkWood");
        }
        foreach(float z in new[]{-11.8f,-3.8f,4.2f,12.2f})
        {
            Box(a,"Persian rug red binding",V(0,.018f,z),V(10.6f,.018f,5.4f),"RugBorder");
            var rug=Box(a,"Worn reading-room carpet",V(0,.032f,z),V(10.25f,.009f,5.05f),"Rug");
            // A museum image is a whole carpet, so its top UV spans one photograph.
            var f=rug.GetComponent<MeshFilter>();var m=Object.Instantiate(f.sharedMesh);m.name="CarpetUV_"+serial++;
            var uv=m.uv;var v=m.vertices;var n=m.normals;for(int i=0;i<uv.Length;i++)if(n[i].y>.5f)uv[i]=new Vector2(.04f+.92f*(v[i].z/5.05f+.5f),.033f+.935f*(.5f-v[i].x/10.25f));
            m.uv=uv;m=SaveMesh(m,m.name);f.sharedMesh=m;
        }
        // Entry opening is the existing gap at the positive-X end of Wal-F.
        Box(a,"Entry transom",V(10.2f,3.55f,19.78f),V(3.2f,1.6f,.18f),"Wood");
        Box(a,"Entry left jamb",V(8.55f,1.45f,19.73f),V(.22f,2.9f,.26f),"Wood");
        Label(a,"BIBLIOTECA",V(10.15f,3.27f,19.60f),.24f,"CrestGold",180);
    }
    public static void SplitLightingSurfaces(Transform a)
    {
        if(a.Find("Floor and ceiling light cells"))return;
        // The existing forward renderer admits four additional lights per object.
        // Small cells let nearby physical luminaires illuminate each part of the room.
        foreach(var name in new[]{"Stone","Plaster"})if(!mats.ContainsKey(name))mats[name]=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+name+".mat");
        a.Find("Worn stone floor").GetComponent<MeshRenderer>().enabled=false;
        a.Find("Stained plaster ceiling").GetComponent<MeshRenderer>().enabled=false;
        var g=Group(a,"Floor and ceiling light cells",Vector3.zero);
        for(float x=-12.175f;x<12.17f;x+=5)for(float z=-19.7f;z<19.69f;z+=5)
        {
            float w=Mathf.Min(5,12.175f-x),d=Mathf.Min(5,19.7f-z);var p=V(x+w*.5f,0,z+d*.5f);
            Box(g,"Stone light cell",p+Vector3.down*.012f,V(w,.025f,d),"Stone");
            Box(g,"Plaster light cell",p+Vector3.up*4.43f,V(w,.1f,d),"Plaster");
        }
    }

    static void Shelving(Transform a)
    {
        for(int side=-1;side<=1;side+=2)for(int i=0;i<24;i++)
        {
            float z=-17.25f+i*1.5f;
            if(side>0&&new[]{-12f,-4f,4f,12f}.Any(w=>Mathf.Abs(w-z)<1.25f))continue;
            Shelf(a,V(side*11.5f,0,z),side>0?90:-90,"Wall collection "+side+"/"+i);
        }
        foreach(float x in new[]{-7.7f,7.7f})foreach(float z in new[]{-11.5f,-3.5f,4.5f,12.5f})
        {
            Shelf(a,V(x,0,z),90,"Archive aisle face A");Shelf(a,V(x+.65f,0,z),-90,"Archive aisle face B");
        }
    }
    static void Shelf(Transform parent,Vector3 p,float yaw,string name)
    {
        var g=Group(parent,name,p);g.localRotation=Quaternion.Euler(0,yaw,0);
        // Imported weathered joinery plus upper extension, carved cornice and plinth.
        var bottom=Model(g,"wooden_bookshelf_worn","Weathered lower bookcase",V(0,.12f,0),0,false);
        var top=Model(g,"wooden_bookshelf_worn","Weathered upper bookcase",V(0,2.0f,0),0,false);
        // The imported case is normalized, preserving its original width and depth.
        var b=BoundsIn(bottom,bottom.GetComponentsInChildren<Renderer>());float width=b.size.x;
        Box(g,"Moulded plinth",V(0,.08f,0),V(width+.12f,.16f,.55f),"DarkWood");
        Box(g,"Overhanging crown",V(0,3.97f,0),V(width+.20f,.16f,.60f),"Wood");
        Box(g,"Brass catalogue label",V(0,1.95f,-.285f),V(.22f,.065f,.012f),"Brass");
        var levels=ShelfLevels();
        for(int tier=0;tier<levels.Length*2;tier++)
        {
            float y=(tier<levels.Length?.12f:2f)+levels[tier%levels.Length]+.003f;
            var books=Model(g,"book_encyclopedia_set_01","Leather-bound volumes",V(0,y,-.02f),180,false);
            var bb=BoundsIn(books,books.GetComponentsInChildren<Renderer>());
            int level=tier%levels.Length;
            float space=(level+1<levels.Length?levels[level+1]:1.88f)-levels[level]-.025f;
            books.localScale=V((width-.13f)/bb.size.x,Mathf.Min(1,space/.29f),Mathf.Min(1,.31f/bb.size.z));
            if(tier%3==1)books.localScale=Vector3.Scale(books.localScale,new Vector3(.89f,.96f,1));
        }
        var collider=g.gameObject.AddComponent<BoxCollider>();collider.center=V(0,2,.02f);collider.size=V(width+.15f,4,.65f);
    }
    static float[] shelfLevels;
    static float[] ShelfLevels()
    {
        if(shelfLevels!=null)return shelfLevels;
        var f=prototypes["wooden_bookshelf_worn"].GetComponentsInChildren<MeshFilter>().First();
        var vertices=f.sharedMesh.vertices;var triangles=f.sharedMesh.triangles;var planes=new Dictionary<int,float>();
        for(int i=0;i<triangles.Length;i+=3)
        {
            var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];
            var cross=Vector3.Cross(b-a,c-a);if(cross.y<=0||Mathf.Abs(a.y-b.y)>.006f||Mathf.Abs(a.y-c.y)>.006f)continue;
            int bin=Mathf.RoundToInt((a.y+b.y+c.y)/3/.015f);planes.TryGetValue(bin,out float area);planes[bin]=area+cross.y*.5f;
        }
        var candidates=planes.Where(p=>p.Value>.12f&&p.Key*.015f<1.65f).OrderBy(p=>p.Key).Select(p=>p.Key*.015f).ToList();
        var levels=new List<float>();foreach(float y in candidates){if(levels.Count==0||y-levels.Last()>.1f)levels.Add(y);else levels[levels.Count-1]=Mathf.Max(y,levels.Last());}
        if(levels.Count<2)throw new Exception("Imported shelf support planes could not be identified");
        shelfLevels=levels.ToArray();File.WriteAllText("Evidence/shelf-levels.txt",string.Join(",",shelfLevels));return shelfLevels;
    }

    static void ReadingRoom(Transform a)
    {
        int index=0;
        foreach(float z in new[]{-11.8f,-3.8f,4.2f,12.2f})foreach(float x in new[]{-2.9f,2.9f})
        {
            var t=Group(a,"Reading desk "+index,V(x,0,z));
            var desk=Model(t,"wooden_table_02","Worn hardwood reading table",Vector3.zero);
            var b=BoundsIn(desk,desk.GetComponentsInChildren<Renderer>());
            desk.localScale=V(2.75f/b.size.x,1,1.25f/b.size.z);var bc=desk.GetComponent<BoxCollider>();if(bc){bc.center=b.center;bc.size=b.size;}
            foreach(float side in new[]{-1f,1f})
            {
                Box(t,"Paneled desk apron",V(0,.66f,side*.57f),V(2.54f,.20f,.06f),"Wood");
                Box(t,"Apron upper moulding",V(0,.75f,side*.61f),V(2.62f,.035f,.035f),"DarkWood");
                Box(t,"Apron lower moulding",V(0,.57f,side*.61f),V(2.57f,.035f,.035f),"DarkWood");
            }
            Model(t,"dining_chair_02","Reader chair",V(-.65f,.04f,-1.0f),index%2==0?5:-8);
            Model(t,"dining_chair_02","Pulled-back chair",V(.68f,.04f,1.1f),178+index%3*5);
            BankerLamp(t,V(.4f,.80f,0),index);
            BookStack(t,V(-.65f,.80f,0),3,index*17);BookStack(t,V(-.25f,.80f,.3f),2,index*23+12);
            for(int j=0;j<3;j++)Paper(t,V(.5f+j*.09f,.805f+j*.003f,-.36f+j*.05f),j*13);
            index++;
        }
        var reception=Group(a,"Librarian station",V(5.8f,0,17.1f));
        Model(reception,"wooden_table_02","Circulation desk",Vector3.zero,90);
        Model(reception,"drawer_cabinet","Card catalogue",V(-2.25f,0,.3f),0);
        Model(reception,"dining_chair_02","Absent librarian chair",V(.5f,0,1.05f),175);
        BankerLamp(reception,V(-.3f,.80f,0),8);Computer(reception,V(.5f,.80f,0));
        Label(a,"SALA DE LEITURA",V(-.5f,3.25f,19.8f),.28f,"CrestGold",180);
        var ladder=Model(a,"wooden_ladder","Ladder left mid-aisle",V(-10.3f,.02f,1.8f),-80);ladder.localRotation*=Quaternion.Euler(-8,0,0);
        var ladderBounds=BoundsIn(a,ladder.GetComponentsInChildren<Renderer>());ladder.localPosition+=Vector3.up*(.03f-ladderBounds.min.y);
    }
    static void BookStack(Transform p,Vector3 pos,int count,float yaw)
    {
        float y=pos.y;
        for(int i=0;i<count;i++)
        {
            var book=Model(p,"single_book","Stacked consultation volume",V(pos.x+i*.012f,0,pos.z+i*.009f),0,false);book.localScale=Vector3.one*1.2f;
            book.localRotation=Quaternion.Euler(0,yaw+i*7,90);var b=BoundsIn(p,book.GetComponentsInChildren<Renderer>());book.localPosition+=Vector3.up*(y-b.min.y);y+=b.size.y+.002f;
        }
    }
    static void BankerLamp(Transform p,Vector3 pos,int index)
    {
        var g=Group(p,"Green banker lamp",pos);
        Lathe(g,"Turned brass foot",Vector3.zero,new[]{P(0,0),P(.15f,0),P(.17f,.025f),P(.15f,.05f),P(.06f,.07f),P(.035f,.11f),P(.024f,.37f),P(.055f,.39f)},"CrestGold");
        // Curved emerald shade, open below, with rolled brass rim.
        var shade=Lathe(g,"Emerald glass shade",V(0,.38f,0),new[]{P(.21f,0),P(.23f,.025f),P(.20f,.11f),P(.12f,.17f),P(0,.18f)},"GreenGlass",40);shade.localScale=V(1.35f,1,.65f);
        var rim=Lathe(g,"Shade rolled edge",V(0,.379f,0),new[]{P(.205f,0),P(.23f,.006f),P(.231f,.016f),P(.206f,.016f)},"Brass",40);rim.localScale=V(1.35f,1,.65f);
        Lathe(g,"Warm lamp diffuser",V(0,.373f,0),new[]{P(0,0),P(.18f,0),P(.18f,.009f),P(0,.009f)},"LampGlow",32);
        if(index!=5)Light(g,"Local warm reading light",V(0,.35f,-.05f),C(1,.70f,.37f),2.5f,3.5f);
    }
    static void Computer(Transform p,Vector3 pos)
    {
        var g=Group(p,"Abandoned catalogue terminal",pos);
        Box(g,"Yellowed computer case",V(0,.09f,0),V(.53f,.18f,.46f),"Beige");
        Box(g,"CRT housing",V(0,.39f,.06f),V(.48f,.43f,.43f),"Beige");
        Box(g,"Recessed CRT glass",V(0,.40f,-.161f),V(.37f,.28f,.012f),"CRT");
        Box(g,"Keyboard",V(0,.025f,-.37f),V(.48f,.05f,.17f),"Beige");
        for(int y=0;y<4;y++)for(int x=0;x<13;x++)Box(g,"Key",V(-.215f+x*.035f,.055f,-.43f+y*.034f),V(.027f,.012f,.025f),"Paper");
        for(int i=0;i<7;i++)Box(g,"Green phosphor line",V(-.04f,.50f-i*.026f,-.169f),V(.20f-(i%3)*.036f,.004f,.002f),"GreenGlass");
        for(int i=0;i<6;i++)Box(g,"Case ventilation slot",V(.266f,.09f,.13f-i*.04f),V(.004f,.035f,.02f),"Iron");
    }

    static void FocalWall(Transform a)
    {
        Box(a,"Rear damp wall",V(0,2.2f,-19.67f),V(24.2f,4.4f,.13f),"Plaster");
        foreach(float x in new[]{-10,-7,-4,4,7,10})Shelf(a,V(x,0,-19.15f),180,"Rare books rear collection");
        Window(a,V(0,1.0f,-19.46f),0,3.2f,3.1f);
        foreach(float z in new[]{-12,-4,4,12})Window(a,V(11.88f,1.05f,z),-90,1.4f,2.85f);
        var pedestal=Group(a,"Founder memorial",V(0,0,-17.25f));
        Box(pedestal,"Carved wooden pedestal",V(0,.54f,0),V(.65f,1.08f,.65f),"Wood",true);
        Box(pedestal,"Plinth",V(0,.08f,0),V(.80f,.16f,.80f),"DarkWood");
        Box(pedestal,"Pedestal cap",V(0,1.12f,0),V(.78f,.12f,.78f),"Wood");
        Model(pedestal,"marble_bust_01","Weathered founder bust",V(0,1.18f,0),0,false);
        Label(pedestal,"FUNDADOR\n1897",V(0,.78f,.333f),.07f,"CrestGold",0);
        foreach(float x in new[]{-1.9f,1.9f})
        {
            var banner=Group(a,"Ox-blood academic banner",V(x,2.9f,-19.35f));
            Box(banner,"Aged woven cloth",Vector3.zero,V(.75f,2.25f,.028f),"Banner");
            Box(banner,"Gilt banner edge",V(-.35f,0,.02f),V(.018f,2.18f,.01f),"CrestGold");
            Box(banner,"Gilt banner edge",V(.35f,0,.02f),V(.018f,2.18f,.01f),"CrestGold");
            Box(banner,"Embroidered open book left leaf",V(-.092f,.55f,.025f),V(.17f,.23f,.01f),"CrestGold",false,-8);
            Box(banner,"Embroidered open book right leaf",V(.092f,.55f,.025f),V(.17f,.23f,.01f),"CrestGold",false,8);
            for(int line=0;line<4;line++)foreach(float side in new[]{-1f,1f})Box(banner,"Embroidered writing",V(side*.092f,.62f-line*.045f,.032f),V(.12f,.008f,.004f),"Banner");
            Label(banner,"EX\nLIBRIS",V(0,.1f,.025f),.14f,"CrestGold",0);
        }
        foreach(float z in new[]{-16,-8,0,8,16})foreach(float x in new[]{-10.9f,10.9f})
        {
            var g=Group(a,"Brass wall sconce",V(x,2.55f,z));
            Box(g,"Sconce mounting plate",V(Mathf.Sign(x)*.15f,0,0),V(.07f,.38f,.18f),"Brass");
            Lathe(g,"Caged warm lantern",V(0,-.14f,0),new[]{P(.05f,0),P(.08f,.04f),P(.08f,.22f),P(.05f,.27f)},"LampGlow",16);
            Lathe(g,"Lantern roof",V(0,.13f,0),new[]{P(.11f,0),P(.12f,.018f),P(.04f,.07f),P(0,.10f)},"Brass",24);
            for(int i=0;i<4;i++){float angle=i*Mathf.PI*.5f;Box(g,"Protective lantern bar",V(Mathf.Cos(angle)*.08f,-.01f,Mathf.Sin(angle)*.08f),V(.012f,.25f,.012f),"Iron");}
            Light(g,"Wall lamp pool",V(-Mathf.Sign(x)*.2f,0,0),C(1,.63f,.32f),2.2f,4.5f);
        }
        int n=0;foreach(float z in new[]{-12,-4,4,12})foreach(float x in new[]{-3.0f,3.0f})
        {
            var g=Group(a,"Aged fluorescent fixture",V(x,4.15f,z));
            Box(g,"Rusty metal housing",Vector3.zero,V(.38f,.10f,1.65f),"Iron");
            for(int i=-1;i<=1;i++)Box(g,n==3?"Dead fluorescent tube":"Diffused fluorescent tube",V(i*.105f,-.065f,0),V(.045f,.035f,1.46f),n==3?"Paper":"TubeGlow");
            if(n!=3)Light(g,"Ceiling light",V(0,-.13f,0),C(.88f,.86f,.72f),24f,9,n%2==0,V(0,-3.7f,0),115);
            n++;
        }
    }
    static void Window(Transform p,Vector3 pos,float yaw,float width,float height)
    {
        var g=Group(p,"Dirty crimson window",pos);g.localRotation=Quaternion.Euler(0,yaw,0);
        Box(g,"Recessed exterior darkness",V(0,height/2,0),V(width+.25f,height+.2f,.10f),"DarkWood");
        Box(g,"Dim red dusk through stained glazing",V(0,height/2,.065f),V(width,height,.015f),"RedGlass");
        // Relief silhouettes sit inside the opaque glazing recess, suggesting the old campus beyond.
        Box(g,"Distant courtyard roofline",V(0,.18f,.083f),V(width,.36f,.008f),"Iron");
        var tower=Lathe(g,"Distant campus spire",V(width*.20f,.35f,.084f),new[]{P(.17f,0),P(.17f,.55f),P(.21f,.56f),P(.21f,.60f),P(0,1.15f)},"Iron",4);tower.localScale=V(1,1,.025f);
        Box(g,"Distant narrow tower",V(-width*.25f,.45f,.083f),V(.23f,.9f,.008f),"Iron");
        for(int i=-1;i<=1;i++)Box(g,"Window upright",V(i*width/2,height/2,.10f),V(.055f,height+.12f,.11f),"DarkWood");
        for(int i=0;i<4;i++)Box(g,"Window crossbar",V(0,i*height/3,.11f),V(width+.12f,.045f,.12f),"DarkWood");
        Box(g,"Deep sill",V(0,-.05f,.16f),V(width+.45f,.14f,.42f),"Wood");
        Light(g,"Dusk spill",V(0,height*.55f,.28f),C(.56f,.10f,.12f),1.5f,6,Mathf.Approximately(yaw,0),V(0,1,4),100);
    }
    static void Label(Transform p,string text,Vector3 pos,float size,string mat,float yaw)
    {
        var t=Group(p,text.Replace('\n',' '),pos);t.localRotation=Quaternion.Euler(0,yaw,0);
        var tm=t.gameObject.AddComponent<TextMesh>();tm.text=text;tm.characterSize=size;tm.fontSize=64;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;
        tm.characterSize*=.22f;t.localRotation*=Quaternion.Euler(0,180,0);
        tm.color=mats[mat].GetColor("_BaseColor");tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;
    }
    static void Paper(Transform p,Vector3 pos,float yaw)
    {
        var g=Group(p,"Abandoned reading note",pos);g.localRotation=Quaternion.Euler(0,yaw,0);
        Box(g,"Stained paper",Vector3.zero,V(.20f,.002f,.27f),"Paper");
        for(int i=0;i<7;i++)Box(g,"Faded text",V(-.008f,.0016f,-.095f+i*.024f),V(.145f-(i%3)*.018f,.0007f,.003f),"Ink");
    }
    static void Abandonment(Transform a)
    {
        var fallen=Model(a,"dining_chair_02","Chair abandoned on its side",V(-4.5f,.2f,7.1f),25,false);
        fallen.localRotation*=Quaternion.Euler(0,0,82);var b=BoundsIn(a,fallen.GetComponentsInChildren<Renderer>());fallen.localPosition+=Vector3.up*(.05f-b.min.y);
        var bc=fallen.gameObject.AddComponent<BoxCollider>();var local=BoundsIn(fallen,fallen.GetComponentsInChildren<Renderer>());bc.center=local.center;bc.size=local.size;
        for(int i=0;i<23;i++)
        {
            float x=-9.6f+(i%5)*.23f,z=-6+(i/5)*.36f;Paper(a,V(x,.045f+(i%3)*.002f,z),i*47);
        }
        var scattered=Model(a,"book_encyclopedia_set_01","Books dropped beside the ladder",V(-10,.04f,2.6f),23,false);scattered.localRotation*=Quaternion.Euler(0,0,90);
        b=BoundsIn(a,scattered.GetComponentsInChildren<Renderer>());scattered.localPosition+=Vector3.up*(.04f-b.min.y);
        // Dirt follows wall bases, window leaks and the neglected catalogue corner.
        foreach(float x in new[]{-11.8f,11.8f})for(int i=0;i<12;i++)Box(a,"Accumulated corner dust",V(x,.043f,-17.5f+i*3),V(.18f,.012f,1.8f),"Dust");
    }
    static void Atmosphere(Transform a)
    {
        var g=Group(a,"Library regional atmosphere",V(0,2.2f,0));var c=g.gameObject.AddComponent<BoxCollider>();c.isTrigger=true;c.size=V(24.2f,4.4f,39.4f);
        var r=g.gameObject.AddComponent<TCRegionAtmosphere>();r.theme="abandoned library";r.localBounds=new Bounds(Vector3.zero,c.size);r.priority=35;r.ambient=Ambient;
        r.fogColor=C(.048f,.043f,.035f);r.fogDensity=.012f;r.blendDistance=.8f;r.flashlightIntensity=2.6f;r.stabilizeBackgroundShadows=true;
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Art+"/Profiles/LibraryAtmosphere.asset");
        if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Art+"/Profiles/LibraryAtmosphere.asset");}
        foreach(var component in profile.components.ToArray()){profile.components.Remove(component);Object.DestroyImmediate(component,true);}
        var grade=profile.Add<ColorAdjustments>(true);grade.saturation.Override(-12);grade.contrast.Override(8);grade.postExposure.Override(.25f);
        var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
        var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.12f);bloom.threshold.Override(1.15f);
        var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.14f);vignette.smoothness.Override(.5f);
        var grain=profile.Add<FilmGrain>(true);grain.intensity.Override(.04f);
        foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
        EditorUtility.SetDirty(profile);var volume=g.gameObject.AddComponent<Volume>();volume.isGlobal=false;volume.sharedProfile=profile;volume.priority=35;volume.blendDistance=.8f;
    }
}
