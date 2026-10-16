using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using TotalChaosMaps;
using Object = UnityEngine.Object;

// One-off generator. Compile only in the isolated project's _TemporaryTools/Editor.
public static class CampusBuilder
{
    const string Garden = "Assets/_Project/Scenes/EntranceGarden";
    const string Hall = "Assets/_Project/Scenes/EntranceHallway";
    const string Shared = "Assets/_Project/Global";
    const string Scene = "Assets/_Project/Scenes/Main/SampleScene.unity";
    static string owner;
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static Material beige, red, white, brown, black, stone, paving, mosaicLight, mosaicDark, soil, blue, tile, grout, glass, glow;
    static GameObject palm, lamp, screen, window, plant, bench, shrub;
    static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
    static Color C(float r,float g,float b) => new Color(r,g,b);
    public static string Prefab(string room) => "Assets/_Project/Scenes/"+room+"/Prefabs/"+room+".prefab";

    public static void Build()
    {
        if (!Application.dataPath.Replace('\\','/').Contains("/.utmp/EntranceRebuild/")) throw new Exception("Use isolated workspace");
        Directory.CreateDirectory("CampusEvidence");
        ImportTextures();
        owner=Shared;
        SetupMaterials();
        BuildModules();
        foreach(var name in new[]{"EntranceGarden","EntranceHallway"})
        {
            owner=name=="EntranceGarden"?Garden:Hall;
            var root=PrefabUtility.LoadPrefabContents(Prefab(name));
            try
            {
                PreserveOld(root,name=="EntranceGarden");
                var old=root.transform.Find("CampusArchitecture");
                if(old)Object.DestroyImmediate(old.gameObject);
                var art=Group(root.transform,"CampusArchitecture",Vector3.zero);
                var anchor=art.gameObject.AddComponent<CampusEntranceAnchor>();
                anchor.floor=root.transform.Find("Floor/Photographic surface").GetComponent<MeshFilter>();
                anchor.exterior=name=="EntranceGarden";
                anchor.heightOffset=anchor.exterior?.205f:0;
                anchor.Refresh();
                if(anchor.exterior)BuildExterior(art); else BuildInterior(art);
                PrefabUtility.SaveAsPrefabAsset(root,Prefab(name));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        IntegrateScene();
        AssetDatabase.SaveAssets();
        CampusValidation.Run();
        Debug.Log("CAMPUS_BUILD_PASS");
    }
    public static void FinalizeScene()
    {
        owner=Garden;
        var prefab=PrefabUtility.LoadPrefabContents(Prefab("EntranceGarden"));
        try
        {
            black=AssetDatabase.LoadAssetAtPath<Material>(Shared+"/Materials/CampusReference/Scuffed bronze frames.mat");
            glow=AssetDatabase.LoadAssetAtPath<Material>(Shared+"/Materials/CampusReference/Aged opal lamp glass.mat");
            FacadeLighting(prefab.transform.Find("CampusArchitecture/03 UniDoom facade"));
            PrefabUtility.SaveAsPrefabAsset(prefab,Prefab("EntranceGarden"));
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        IntegrateScene();
        AssetDatabase.SaveAssets();
        CampusValidation.Run();
        Debug.Log("CAMPUS_BUILD_PASS");
    }

    static void ImportTextures()
    {
        AssetDatabase.Refresh();
        foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.Contains("/CampusReference/")&&p.EndsWith(".png")))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            bool normal=path.Contains("nor_gl")||path.EndsWith("normal.png");
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=path.Contains("Diffuse")||path.Contains("diffus");
            importer.alphaIsTransparency=path.Contains("diffus");
            importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
    }
    static Material Save(Material material)
    {
        var path=owner+"/Materials/CampusReference/"+material.name+".mat";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old){EditorUtility.CopySerialized(material,old);Object.DestroyImmediate(material);material=old;}
        else AssetDatabase.CreateAsset(material,path);
        materials[material.name]=material;
        return material;
    }
    static Material Plain(string name,Color color,float metallic=0,float smooth=.2f)
    {
        var material=new Material(Shader.Find("UniDoom/TotalChaosInspiredSurface")){name=name};
        material.SetColor("_BaseColor",color);material.SetFloat("_Metallic",metallic);material.SetFloat("_Smoothness",smooth);
        material.SetVector("_SpecColor",new Vector4(.12f,.08f,.85f,1));
        return Save(material);
    }
    static Material Clone(string name,string path,Color tint,float smooth=.2f)
    {
        var source=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!source)throw new Exception("Missing material "+path);
        var material=new Material(source){name=name};
        material.SetColor("_BaseColor",tint);material.SetFloat("_Smoothness",smooth);
        material.SetTextureScale("_BaseMap",Vector2.one);
        return Save(material);
    }
    static void SetupMaterials()
    {
        // Reuse existing photographic maps and their packed normal/AO/smoothness channels.
        var existing="Assets/_Project/Scenes/Library/Materials/CampusFinish/UniversityPaintedPlaster.mat";
        beige=Clone("Weathered campus ochre",existing,C(.62f,.56f,.43f));
        white=Clone("Rain stained ivory plaster",existing,C(.78f,.78f,.69f));
        red=Clone("Faded entrance oxide red",existing,C(.37f,.105f,.075f));
        brown=Clone("Aged brown fascia",existing,C(.17f,.115f,.075f));
        black=Clone("Scuffed bronze frames",Hall+"/Materials/EntranceRebuild/Scuffed rubber skirting.mat",C(.065f,.074f,.066f),.3f);
        black.SetFloat("_Metallic",.6f);
        stone=Clone("Damp concrete edging",Garden+"/Materials/EntranceRebuild/Damp stone edging.mat",C(.53f,.52f,.43f));
        blue=Clone("Worn blue tactile concrete",existing,C(.15f,.58f,.86f),.12f);
        owner=Hall;
        tile=Clone("Worn polished limestone",Hall+"/Materials/EntranceRebuild/Worn institutional tiles.mat",C(.73f,.72f,.64f),.48f);
        grout=Clone("Dark granite grid",Garden+"/Materials/EntranceRebuild/Damp stone edging.mat",C(.11f,.125f,.12f),.28f);
        owner=Garden;
        soil=Clone("Unmaintained garden soil",Garden+"/Materials/EntranceRebuild/Planting soil.mat",C(.32f,.35f,.24f));
        paving=Plain("Portuguese patterned stone",Color.white);
        string folder=Garden+"/Textures/CampusReference/patterned_cobblestone_02/";
        paving.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Diffuse.png"));
        paving.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"nor_gl.png"));
        paving.SetFloat("_BumpScale",.65f);paving.EnableKeyword("_NORMALMAP");
        var source=new Texture2D(2,2,TextureFormat.RGBA32,false,true);source.LoadImage(File.ReadAllBytes(folder+"arm.png"));
        var pixels=source.GetPixels32();for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new Color32(p.b,p.r,p.g,(byte)(255-p.g));}
        source.SetPixels32(pixels);source.Apply();string packed=folder+"PackedSurface.png";File.WriteAllBytes(packed,source.EncodeToPNG());Object.DestroyImmediate(source);
        AssetDatabase.ImportAsset(packed);var importer=(TextureImporter)AssetImporter.GetAtPath(packed);importer.sRGBTexture=false;importer.maxTextureSize=2048;importer.SaveAndReimport();
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(packed);paving.SetTexture("_MetallicGlossMap",tex);paving.SetTexture("_OcclusionMap",tex);paving.SetFloat("_Smoothness",.35f);paving.EnableKeyword("_METALLICSPECGLOSSMAP");paving.EnableKeyword("_OCCLUSIONMAP");
        mosaicLight=Save(new Material(paving){name="Weathered limestone mosaic tesserae"});mosaicLight.SetColor("_BaseColor",C(1.38f,1.32f,1.14f));
        mosaicDark=Save(new Material(paving){name="Weathered basalt mosaic tesserae"});mosaicDark.SetColor("_BaseColor",C(.19f,.205f,.195f));
        owner=Shared;
        glass=Plain("Dusty entrance glazing",new Color(.29f,.38f,.36f,.16f),0,.64f);
        glass.SetFloat("_Surface",1);glass.SetFloat("_Blend",0);glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);glass.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);glass.SetFloat("_ZWrite",0);glass.SetFloat("_Cull",0);
        glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");glass.SetOverrideTag("RenderType","Transparent");glass.renderQueue=3000;glass.SetShaderPassEnabled("ShadowCaster",false);
        glow=Plain("Aged opal lamp glass",C(.72f,.78f,.66f));glow.SetColor("_EmissionColor",C(.72f,.78f,.66f)*1.8f);glow.EnableKeyword("_EMISSION");
    }
    static Transform Group(Transform parent,string name,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static GameObject Box(Transform parent,string name,Vector3 p,Vector3 size,Material material,bool collider=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material;
        if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());
        var mesh=Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;
        for(int i=0;i<vertices.Length;i++)
        {
            var v=Vector3.Scale(vertices[i],size);var n=normals[i];
            uv[i]=Mathf.Abs(n.y)>.5f?new Vector2(v.x,v.z)/2:Mathf.Abs(n.x)>.5f?new Vector2(v.z,v.y)/2:new Vector2(v.x,v.y)/2;
        }
        mesh.uv=uv;go.GetComponent<MeshFilter>().sharedMesh=mesh;
        return go;
    }
    static GameObject Primitive(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static void Light(Transform parent,string name,Vector3 p,Color color,float intensity,float range,bool shadows=false)
    {
        var l=Group(parent,name,p).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=shadows?LightShadows.Soft:LightShadows.None;
        l.shadowBias=.025f;l.shadowNormalBias=.12f;
        var data=l.GetComponent<UniversalAdditionalLightData>();if(!data)data=l.gameObject.AddComponent<UniversalAdditionalLightData>();
        var serialized=new SerializedObject(data);serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=1;serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Sign(Transform parent,string label,Vector3 p,float width,float height,float size,Color color)
    {
        var t=Group(parent,label,p);var text=t.gameObject.AddComponent<TextMeshPro>();
        text.text=label;text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.color=color;text.rectTransform.sizeDelta=new Vector2(width,height);
        text.enableWordWrapping=false;
    }
    static void PreserveOld(GameObject root,bool exterior)
    {
        foreach(Transform t in root.transform)
        {
            if(t.name=="CampusArchitecture")continue;
            if(t.name=="Total Chaos Rebuild"||t.GetComponent<EntranceGardenLayout>()){t.gameObject.SetActive(false);continue;}
            if(t.name=="Floor"||t.name=="Roof"||t.name.StartsWith("Wall"))
            {
                foreach(var r in t.GetComponentsInChildren<Renderer>(true))r.enabled=false;
                if(exterior)foreach(var c in t.GetComponentsInChildren<Collider>(true))c.enabled=false;
            }
        }
    }
    static Bounds Bounds(Transform t)
    {var rs=t.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
    static GameObject Module(Transform root,string name,string directory)
    {
        Bake(root,name);
        var path=directory+"/Prefabs/CampusReference/"+name+".prefab";Directory.CreateDirectory(Path.GetDirectoryName(path));
        var result=PrefabUtility.SaveAsPrefabAsset(root.gameObject,path);Object.DestroyImmediate(root.gameObject);return result;
    }
    static Transform Place(Transform parent,GameObject prefab,string name,Vector3 position,float yaw=0,float scale=1)
    {
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);go.name=name;go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=Vector3.one*scale;return go.transform;
    }
    static void Bake(Transform root,string name)
    {
        var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.GetComponent<TMP_Text>()&&!PrefabUtility.IsPartOfPrefabInstance(r)).ToArray();
        var groups=new Dictionary<Material,List<CombineInstance>>();
        foreach(var r in renderers)
        {
            var mf=r.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;
            for(int i=0;i<r.sharedMaterials.Length;i++)
            {
                var m=r.sharedMaterials[i];if(!groups.ContainsKey(m))groups[m]=new List<CombineInstance>();
                groups[m].Add(new CombineInstance{mesh=mf.sharedMesh,subMeshIndex=i,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix});
            }
        }
        foreach(var pair in groups)
        {
            var mesh=new Mesh{name=name+"_"+pair.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
            string path=owner+"/Meshes/CampusReference/"+mesh.name.Replace('/','_')+".asset";Directory.CreateDirectory(Path.GetDirectoryName(path));
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var child=Group(root,pair.Key.name,Vector3.zero);child.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;child.gameObject.AddComponent<MeshRenderer>().sharedMaterial=pair.Key;
        }
        foreach(var r in renderers)
        {
            var mf=r.GetComponent<MeshFilter>();if(!mf)continue;
            Object.DestroyImmediate(mf);Object.DestroyImmediate(r);
        }
        foreach(var t in root.GetComponentsInChildren<Transform>().Reverse())
            if(t!=root&&t.childCount==0&&t.GetComponents<Component>().Length==1)Object.DestroyImmediate(t.gameObject);
    }
    static void BuildModules()
    {
        owner=Garden;
        var p=Group(null,"PalmTree",Vector3.zero);
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Garden+"/Models/CampusReference/PalmTree/palm_tree.obj");
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source,p);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var material=Plain("Weathered palm atlas",C(.70f,.74f,.57f),0,.12f);
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Garden+"/Textures/CampusReference/PalmTree/diffus.png"));
        material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Garden+"/Textures/CampusReference/PalmTree/normal.png"));material.SetFloat("_BumpScale",.45f);material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.42f);material.SetFloat("_Cull",0);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;material.SetOverrideTag("RenderType","TransparentCutout");
        foreach(var r in p.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
        var b=Bounds(p);model.transform.localScale*=10.8f/b.size.y;b=Bounds(p);model.transform.position-=V(b.center.x,b.min.y,b.center.z);
        var trunk=p.gameObject.AddComponent<CapsuleCollider>();trunk.center=V(0,4.15f,0);trunk.height=8.3f;trunk.radius=.25f;
        palm=Module(p,"CampusPalm",Garden);
        p=Group(null,"GlobeLamp",Vector3.zero);
        Primitive(p,"Cast iron post",PrimitiveType.Cylinder,V(0,1.45f,0),V(.07f,1.45f,.07f),black);
        Primitive(p,"Concrete foot",PrimitiveType.Cylinder,V(0,.1f,0),V(.32f,.1f,.32f),stone);
        Primitive(p,"Opal globe",PrimitiveType.Sphere,V(0,3.02f,0),Vector3.one*.42f,glow);
        Light(p,"Globe practical",V(0,2.91f,0),C(.83f,.80f,.60f),1.9f,6);
        var col=p.gameObject.AddComponent<CapsuleCollider>();col.center=V(0,1.5f,0);col.height=3;col.radius=.1f;
        lamp=Module(p,"CampusGlobeLamp",Garden);
        p=Group(null,"Cobogo",Vector3.zero);
        foreach(float x in new[]{-.6f,.6f})Box(p,"Concrete edge",V(x,0,0),V(.055f,1.2f,.14f),white);
        foreach(float y in new[]{-.6f,.6f})Box(p,"Concrete edge",V(0,y,0),V(1.2f,.055f,.14f),white);
        for(int x=0;x<2;x++)for(int y=0;y<2;y++)
        {
            var cell=Group(p,"Diamond aperture",V(-.3f+x*.6f,-.3f+y*.6f,0));
            foreach(float a in new[]{45f,135f})
            {
                var bar=Box(cell,"Diagonal concrete",Vector3.zero,V(.055f,.8f,.14f),white);bar.transform.localRotation=Quaternion.Euler(0,0,a);
            }
        }
        screen=Module(p,"CampusCobogo",Garden);
        p=Group(null,"GlazedBay",Vector3.zero);
        Box(p,"Glazing",V(0,1.7f,0),V(2.2f,2.5f,.035f),glass,true);
        foreach(float x in new[]{-1.1f,0,1.1f})Box(p,"Metal mullion",V(x,1.7f,0),V(.045f,2.56f,.09f),black);
        foreach(float y in new[]{.43f,1.22f,2.97f})Box(p,"Metal transom",V(0,y,0),V(2.24f,.045f,.09f),black);
        owner=Shared;window=Module(p,"CampusGlazedBay",Shared);owner=Garden;
        plant=MakeExistingModule("CampusPottedPlant","Assets/_Project/Scenes/DirectorRoom/Models/EntranceRebuild/potted_plant_02/model.fbx",1.45f,Hall,(r)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Scenes/DirectorRoom/Materials/EntranceRebuild/potted_plant_02_"+(r.sharedMaterial.name.ToLowerInvariant().Contains("leav")?"leaves":"pot")+".mat"));
        bench=MakeExistingModule("CampusGardenBench",Garden+"/Models/EntranceRebuild/painted_wooden_bench/model.fbx",.85f,Garden,(r)=>AssetDatabase.LoadAssetAtPath<Material>(Garden+"/Materials/EntranceRebuild/painted_wooden_bench.mat"));
        shrub=MakeExistingModule("CampusFernBed",Garden+"/Models/EntranceRebuild/fern_02/model.fbx",.65f,Garden,(r)=>AssetDatabase.LoadAssetAtPath<Material>(Garden+"/Materials/EntranceRebuild/fern_02.mat"));
    }
    static GameObject MakeExistingModule(string name,string path,float height,string directory,Func<Renderer,Material> assign)
    {
        var wrap=Group(null,name,Vector3.zero);var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var model=(GameObject)PrefabUtility.InstantiatePrefab(source,wrap);PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        if(name=="CampusFernBed")
        {
            var variants=model.GetComponentsInChildren<MeshRenderer>();
            foreach(var variant in variants.Skip(1))Object.DestroyImmediate(variant.gameObject);
        }
        foreach(var r in model.GetComponentsInChildren<Renderer>())r.sharedMaterial=assign(r);
        var b=Bounds(wrap);model.transform.localScale*=height/b.size.y;b=Bounds(wrap);model.transform.position-=V(b.center.x,b.min.y,b.center.z);
        var c=wrap.gameObject.AddComponent<BoxCollider>();b=Bounds(wrap);c.center=b.center;c.size=b.size;
        var previous=owner;owner=directory;var module=Module(wrap,name,directory);owner=previous;return module;
    }
    static void Surface(Transform parent,string name,Vector3 p,float width,float depth,Material material,float metres,bool collider=true)
    {
        var go=Box(parent,name,p,V(width,.04f,depth),material,collider);
        // Persistent planar UVs preserve physical texture scale independently of each slab's transform.
        var original=go.GetComponent<MeshFilter>().sharedMesh;var mesh=Object.Instantiate(original);mesh.name=name;
        var verts=mesh.vertices;var uv=mesh.uv;for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(verts[i].x*width/metres,verts[i].z*depth/metres);mesh.uv=uv;
        string path=owner+"/Meshes/CampusReference/"+name.Replace('/','_')+".asset";Directory.CreateDirectory(Path.GetDirectoryName(path));
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
        go.GetComponent<MeshFilter>().sharedMesh=mesh;
    }
    static void Tactile(Transform parent,float from,float to,bool exterior)
    {
        var t=Group(parent,"Blue tactile route",Vector3.zero);
        Surface(t,"Tactile paving",V(.62f,.035f,(from+to)/2),.38f,to-from,blue,.4f,false);
        for(int i=0;i<4;i++)Box(t,"Raised directional rib",V(.485f+i*.09f,.061f,(from+to)/2),V(.028f,.014f,to-from),blue);
        foreach(float z in new[]{from+.3f,to-.3f})for(int x=0;x<4;x++)for(int j=0;j<4;j++)
            Primitive(t,"Warning stud",PrimitiveType.Sphere,V(.485f+x*.09f,.065f,z-.15f+j*.1f),V(.045f,.013f,.045f),blue);
        Bake(t,exterior?"ExteriorTactile":"InteriorTactile");
    }
    static void Mosaic(Transform parent,string name,Vector3 center,float width,float depth)
    {
        Surface(parent,name+" support",center,width,depth,paving,.5f);
        const float cell=.10f;
        int nx=Mathf.RoundToInt(width/cell),nz=Mathf.RoundToInt(depth/cell);
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var light=new List<int>();var dark=new List<int>();
        for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
        {
            float px=-width*.5f+(x+.5f)*width/nx,pz=-depth*.5f+(z+.5f)*depth/nz;
            float worldX=px+center.x,worldZ=pz+center.z;
            float dx=Mathf.Repeat(worldX+1.2f,2.4f)-1.2f,dz=Mathf.Repeat(worldZ+1.2f,2.4f)-1.2f;
            float radius=Mathf.Sqrt(dx*dx+dz*dz);bool blackStone=radius>.47f&&radius<.78f||Mathf.Abs(dx)+Mathf.Abs(dz)<.39f;
            var triangles=blackStone?dark:light;int i=vertices.Count;float sx=width/nx*.465f,sz=depth/nz*.465f;
            foreach(var q in new[]{new Vector2(-sx,-sz),new Vector2(-sx,sz),new Vector2(sx,sz),new Vector2(sx,-sz)})
            {vertices.Add(V(worldX+q.x,center.y+.023f,worldZ+q.y));uv.Add(new Vector2(worldX+q.x,worldZ+q.y)/.5f);}
            triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
        }
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.subMeshCount=2;mesh.SetTriangles(light,0);mesh.SetTriangles(dark,1);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Garden+"/Meshes/CampusReference/"+name+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
        var t=Group(parent,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=new[]{mosaicLight,mosaicDark};
    }
    static void GardenIsland(Transform bed,int side)
    {
        var points=new[]{V(-side*5.1f,.02f,-12.5f),V(-side*1.0f,.02f,-12.5f),V(side*5.1f,.02f,12.5f),V(-side*5.1f,.02f,12.5f)};
        var mesh=new Mesh{name="Tapered garden island "+side};mesh.vertices=points;
        mesh.uv=points.Select(p=>new Vector2(p.x,p.z)/1.8f).ToArray();
        mesh.triangles=side<0?new[]{0,1,2,0,2,3}:new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
        var path=Garden+"/Meshes/CampusReference/"+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
        var t=Group(bed,"Tapered planting soil",Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=soil;
        Box(bed,"Straight inner garden edging",V(-side*5.1f,.07f,0),V(.12f,.16f,25),stone,true);
        var diagonal=Box(bed,"Diagonal outer garden edging",V(side*2.05f,.07f,0),V(.12f,.16f,Mathf.Sqrt(25*25+6.1f*6.1f)),stone,true);
        diagonal.transform.localRotation=Quaternion.Euler(0,side*Mathf.Atan2(6.1f,25)*Mathf.Rad2Deg,0);
        Box(bed,"Narrow garden tip",V(-side*3.05f,.07f,-12.5f),V(4.1f,.16f,.12f),stone,true);
        Box(bed,"Broad garden end",V(0,.07f,12.5f),V(10.2f,.16f,.12f),stone,true);
    }
    static void BuildExterior(Transform art)
    {
        var outside=Group(art,"01 Exterior gardens",Vector3.zero);
        Surface(outside,"Exterior foundation",V(0,-.07f,-20),36,42,stone,2);
        Mosaic(outside,"Portuguese central walk",V(0,-.005f,-24),5.8f,31);
        Mosaic(outside,"Entrance plaza",V(0,-.004f,-4.5f),17,8);
        foreach(int side in new[]{-1,1})
        {
            Surface(outside,"Side path "+side,V(side*12.5f,-.005f,-22),3,33,paving,2.1f);
            outside.Find("Side path "+side).localRotation=Quaternion.Euler(0,side*Mathf.Atan2(6.1f,25)*Mathf.Rad2Deg,0);
            var bed=Group(outside,"Landscaped island "+side,V(side*8.9f,0,-22));
            GardenIsland(bed,side);
            for(int i=0;i<13;i++)Place(bed,shrub,"Ornamental fern border",V(-side*4.4f,.04f,-11.4f+i*1.85f),side*43+i*61,.86f+(i%3)*.07f);
            foreach(float z in new[]{-31f,-21f,-11f})
            {
                Place(outside,palm,"Tall campus palm",V(side*6.7f,.02f,z),z*13+side*37,1+(z+31)*.006f);
                Place(outside,palm,"Perimeter palm",V(side*15.8f,.02f,z-2),z*17-side*22,.85f);
                Place(outside,lamp,"Globe path light",V(side*3.45f,0,z+3));
            }
            Place(outside,palm,"Facade palm",V(side*12.1f,0,-5.5f),side*63,1.2f);
            Place(outside,bench,"Garden bench",V(side*3.9f,0,-17),side<0?90:-90);
        }
        var gate=Group(art,"02 External gate",V(0,0,-39.5f));
        foreach(int side in new[]{-1,1})
        {
            Box(gate,"Ochre gate pier",V(side*3.4f,1.5f,0),V(.65f,3,.7f),beige,true);
            var leaf=Group(gate,"Fixed open gate leaf",V(side*3.1f,0,0));leaf.localRotation=Quaternion.Euler(0,side*82,0);
            for(int i=0;i<14;i++)Box(leaf,"Vertical steel bar",V(side*i*.21f,1.2f,0),V(.027f,2.4f,.04f),black,true);
            foreach(float y in new[]{.15f,2.3f})Box(leaf,"Gate steel rail",V(side*1.4f,y,0),V(2.85f,.05f,.07f),black,true);
            Box(gate,"Boundary low wall",V(side*10.7f,.55f,0),V(13.8f,1.1f,.28f),beige,true);
        }
        Box(gate,"Gate canopy",V(0,3.25f,-.2f),V(8,.18f,2),brown,true);
        Sign(gate,"UniDoom",V(0,2.85f,-.75f),3,.4f,3.2f,C(.8f,.8f,.7f));
        var facade=Group(art,"03 UniDoom facade",Vector3.zero);
        foreach(int side in new[]{-1,1})
        {
            Box(facade,"Wide ochre building wing",V(side*11.5f,2.15f,2.0f),V(13.4f,4.3f,4),beige,true);
            Box(facade,"Brown continuous fascia",V(side*11.5f,3.65f,-.09f),V(13.6f,.42f,.25f),brown);
            Box(facade,"Upper ivory parapet",V(side*11.5f,4.5f,1.8f),V(13.6f,.6f,4.1f),white);
            Box(facade,"Weathered roof coping",V(side*11.5f,4.85f,1.8f),V(13.8f,.12f,4.3f),brown);
            Box(facade,"Recess behind geometric screens",V(side*11.5f,2.18f,-.025f),V(13.2f,2.4f,.10f),black);
            for(int i=0;i<10;i++)for(int row=0;row<2;row++)Place(facade,screen,"White geometric cobogo",V(side*(5.9f+i*1.2f),1.55f+row*1.2f,-.18f));
            Box(facade,"Facade damp plinth",V(side*11.5f,.28f,-.13f),V(13.6f,.55f,.20f),brown);
            Box(facade,"Red entrance upright",V(side*4.45f,3.0f,.12f),V(.55f,6,.65f),red,true);
        }
        Box(facade,"Red crown",V(0,6.02f,.14f),V(9.5f,.20f,.70f),red);
        Box(facade,"UniDoom sign panel",V(0,4.72f,.08f),V(8.4f,2.25f,.40f),white,true);
        Sign(facade,"UniDoom",V(0,4.86f,-.14f),7.7f,1.3f,11,C(.055f,.065f,.06f));
        Sign(facade,"CENTRO UNIVERSITÁRIO",V(0,4.16f,-.15f),7.6f,.30f,1.6f,C(.15f,.16f,.14f));
        Box(facade,"Red entrance canopy",V(0,3.15f,-1),V(8.6f,.24f,2.4f),red,true);
        GlazedEntrance(facade);
        FacadeLighting(facade);
        Tactile(outside,-39.4f,-.3f,true);
        // Low perimeter boundaries keep the enlarged plaza playable without reinstating high tunnel walls.
        foreach(int side in new[]{-1,1})Box(outside,"Outer garden boundary",V(side*18,.42f,-20),V(.22f,.84f,40),beige,true);
        Group(art,"PlayerStart_Exterior",V(0,.1f,-37));
        Group(art,"Connection_ToEntranceHallway",Vector3.zero);
        Region(art,"Overcast abandoned campus",V(0,6,-20),V(38,12,43),C(.48f,.51f,.49f),C(.23f,.275f,.275f),.009f);
        Bake(outside,"ExteriorHardscape");Bake(gate,"EntranceGate");Bake(facade,"UniDoomFacade");
    }
    static void FacadeLighting(Transform facade)
    {
        if(facade.Find("Sign luminaires"))return;
        var group=Group(facade,"Sign luminaires",Vector3.zero);
        foreach(int side in new[]{-1,1})
        {
            Box(group,"Sign light bracket",V(side*2.4f,5.93f,-.45f),V(.06f,.06f,.68f),black);
            Box(group,"Weathered sign light casing",V(side*2.4f,5.91f,-.73f),V(.44f,.10f,.24f),black);
            Box(group,"Sign light diffuser",V(side*2.4f,5.85f,-.73f),V(.34f,.025f,.18f),glow);
            var t=Group(group,"UniDoom sign spotlight",V(side*2.4f,5.8f,-.75f));
            t.localRotation=Quaternion.LookRotation(V(-side*.25f,-1.25f,.65f));
            var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=C(.87f,.88f,.73f);l.intensity=14;l.range=5;l.spotAngle=115;l.innerSpotAngle=75;l.shadows=LightShadows.None;
        }
        Bake(group,"FacadeSignLighting");
    }
    static void GlazedEntrance(Transform parent)
    {
        var entrance=Group(parent,"Glass entrance - central opening 2.6 metres",Vector3.zero);
        foreach(int side in new[]{-1,1})
        {
            Place(entrance,window,"Fixed entrance sidelight",V(side*2.8f,0,-.025f));
            var leaf=Group(entrance,"Door leaf held open",V(side*1.3f,0,-.025f));leaf.localRotation=Quaternion.Euler(0,-side*84,0);
            Box(leaf,"Door glass",V(side*.65f,1.32f,0),V(1.3f,2.6f,.035f),glass,true);
            foreach(float x in new[]{0f,side*1.3f})Box(leaf,"Bronze door stile",V(x,1.32f,0),V(.05f,2.65f,.10f),black,true);
            foreach(float y in new[]{.04f,2.64f})Box(leaf,"Bronze door rail",V(side*.65f,y,0),V(1.35f,.05f,.10f),black);
            Box(leaf,"Pull handle",V(side*1.05f,1.2f,-.10f),V(.03f,.55f,.04f),black);
        }
        Box(entrance,"Upper glazed transom",V(0,2.9f,0),V(2.6f,.42f,.035f),glass,true);
        Box(entrance,"Threshold sill",V(0,.013f,-.05f),V(2.65f,.026f,.35f),stone,false);
    }
    static void BuildInterior(Transform art)
    {
        var lobby=Group(art,"04 Reception lobby",Vector3.zero);
        var corridor=Group(art,"05 Main corridor",Vector3.zero);
        Surface(corridor,"Hall limestone floor",V(0,-.005f,14.35f),7.66f,28.7f,tile,2.4f,false);
        Surface(corridor,"Mineral plaster ceiling",V(0,3.42f,14.35f),7.66f,28.7f,white,2,false);
        // Thin geometric stone bands echo the reference without raising obstacles above the polished floor.
        for(float z=.2f;z<28.7f;z+=1.2f)Box(corridor,"Cross floor granite band",V(0,.019f,z),V(7.66f,.006f,.055f),grout);
        foreach(float x in new[]{-3.1f,-1.6f,1.6f,3.1f})Box(corridor,"Long floor granite band",V(x,.019f,14.35f),V(.06f,.006f,28.7f),grout);
        foreach(int side in new[]{-1,1})
        {
            for(int i=0;i<12;i++)
            {
                float z=1.22f+i*2.4f;
                var bay=Group(corridor,"Glazed side bay",V(side*3.71f,0,z));bay.localRotation=Quaternion.Euler(0,90,0);
                Place(bay,window,"Bronze glazed frame",Vector3.zero);
                Box(bay,"Lower neutral wall",V(0,.21f,0),V(2.4f,.42f,.16f),beige);
                Box(bay,"Upper neutral wall",V(0,3.2f,0),V(2.4f,.4f,.16f),white);
                Box(corridor,"Structural column",V(side*3.51f,1.7f,z-1.2f),V(.28f,3.4f,.28f),i%3==0?red:beige,true);
            }
            // Reception is a shallow service counter at each side of the existing lobby footprint.
            Box(lobby,"Reception counter body",V(side*2.84f,.51f,3.1f),V(1.15f,1.02f,2.7f),red,true);
            Box(lobby,"Worn stone service top",V(side*2.84f,1.06f,3.1f),V(1.3f,.09f,2.85f),stone,true);
            var label=Group(lobby,"Reception identification",V(side*2.84f,.73f,1.72f));
            Sign(label,"ATENDIMENTO",Vector3.zero,1,.25f,1.1f,C(.76f,.76f,.64f));
            foreach(float z in new[]{.9f,7.5f,14.7f,23.2f})Place(corridor,plant,"Ornamental potted plant",V(side*3.08f,.025f,z),side*60+z*7,.88f+(z%2)*.04f);
            Box(corridor,"Scuffed skirting",V(side*3.64f,.09f,14.35f),V(.08f,.18f,28.7f),brown);
        }
        for(int i=0;i<7;i++)
        {
            float z=2.3f+i*4.1f;
            Box(corridor,"Recessed luminaire casing",V(0,3.31f,z),V(.76f,.08f,.31f),black);
            Box(corridor,i==4?"Dead fluorescent diffuser":"Aged fluorescent diffuser",V(0,3.26f,z),V(.66f,.025f,.24f),i==4?white:glow);
            if(i!=4)Light(corridor,"Fluorescent practical",V(0,3.05f,z),C(.76f,.84f,.76f),6.2f,7.3f,i==1||i==5);
        }
        Box(corridor,"Connection portal lintel",V(0,3.14f,28.5f),V(7.66f,.30f,.18f),brown);
        Sign(corridor,"ACESSO INTERNO",V(0,3.14f,28.40f),2.6f,.26f,1.6f,C(.69f,.71f,.62f));
        Tactile(corridor,.1f,28.5f,false);
        Group(art,"Connection_FromEntranceGarden",Vector3.zero);
        Group(art,"Connection_ToExistingAreas",V(0,0,28.7f));
        Region(art,"Abandoned campus reception",V(0,1.8f,14.35f),V(7.66f,3.6f,28.7f),C(.25f,.28f,.255f),C(.12f,.15f,.14f),.008f);
        Bake(lobby,"ReceptionLobby");Bake(corridor,"MainCorridor");
    }
    static void Region(Transform parent,string theme,Vector3 center,Vector3 size,Color ambient,Color fog,float density)
    {
        var t=Group(parent,"Local campus atmosphere",center);var r=t.gameObject.AddComponent<TCRegionAtmosphere>();r.theme=theme;r.localBounds=new Bounds(Vector3.zero,size);
        r.ambient=ambient;r.fogColor=fog;r.fogDensity=density;r.priority=35;r.blendDistance=1.2f;r.flashlightIntensity=2.4f;r.flashlightRange=12;r.stabilizeBackgroundShadows=true;
        r.directionalLightMultiplier=owner==Garden?.45f:0;
        var collider=t.gameObject.AddComponent<BoxCollider>();collider.isTrigger=true;collider.size=size;
        var volume=t.gameObject.AddComponent<Volume>();volume.isGlobal=false;volume.priority=35;volume.blendDistance=1.2f;
        var path=owner+"/Profiles/CampusReference.asset";Directory.CreateDirectory(Path.GetDirectoryName(path));
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
        if(profile.components.Count==0)
        {
            var tone=profile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;
            var c=profile.Add<ColorAdjustments>(true);c.postExposure.value=.35f;c.saturation.value=-18;c.contrast.value=7;
            var b=profile.Add<Bloom>(true);b.intensity.value=.07f;b.threshold.value=1.3f;
            var v=profile.Add<Vignette>(true);v.intensity.value=.12f;v.smoothness.value=.4f;
            foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
        }
        EditorUtility.SetDirty(profile);volume.sharedProfile=profile;
    }
    static void IntegrateScene()
    {
        // Start from the preserved scene on each one-off generation, so replacement art IDs
        // cannot leave stale overrides from a previous isolated preview.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        File.Copy(Path.GetFullPath("../../Tools/CampusEntrance/Before/"+Scene),Scene,true);
        AssetDatabase.ImportAsset(Scene,ImportAssetOptions.ForceUpdate);
        EditorSceneManager.OpenScene(Scene);
        var transforms=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        var garden=transforms.First(t=>t.name=="EntranceGarden");var hall=transforms.First(t=>t.name=="EntranceHallway");
        var ga=garden.Find("CampusArchitecture");var ha=hall.Find("CampusArchitecture");
        garden.rotation=Quaternion.Euler(0,hall.eulerAngles.y+180,0);
        ga.GetComponent<CampusEntranceAnchor>().Refresh();ha.GetComponent<CampusEntranceAnchor>().Refresh();
        garden.position+=ha.position-ga.position;
        ga.GetComponent<CampusEntranceAnchor>().Refresh();
        foreach(var door in transforms.Where(t=>!t.parent&&t.name.StartsWith("DoorPrefab")&&Vector3.Distance(t.position,ha.position)<2))
            door.gameObject.SetActive(false);
        var end=ha.TransformPoint(V(0,0,28.7f));
        foreach(var door in transforms.Where(t=>!t.parent&&t.name.StartsWith("DoorPrefab")&&Vector3.Distance(t.position,end)<2))
        {
            var finish=AssetDatabase.LoadAssetAtPath<Material>(Shared+"/Materials/CampusReference/Aged brown fascia.mat");
            foreach(var renderer in door.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>finish).ToArray();
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        }
        // The second garden wall exists only as a scene override, so disable it here as well.
        foreach(Transform child in garden)if(child.name.StartsWith("Wall"))
        {
            foreach(var renderer in child.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
            foreach(var collider in child.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        }
        var manager=Object.FindFirstObjectByType<GameManager>();
        var settings=new SerializedObject(manager);
        var spawnPoint=(Transform)settings.FindProperty("spawnPoint").objectReferenceValue;
        if(!spawnPoint)throw new Exception("Missing existing GameManager spawn point");
        var spawn=ga.Find("PlayerStart_Exterior");spawnPoint.position=spawn.position+Vector3.up*1.05f;spawnPoint.rotation=ga.rotation;
        PrefabUtility.RecordPrefabInstancePropertyModifications(garden);
        foreach(var child in garden.GetComponentsInChildren<Collider>(true))if(child.transform.parent&&child.transform.parent.name.StartsWith("Wall"))PrefabUtility.RecordPrefabInstancePropertyModifications(child);
        foreach(var renderer in garden.GetComponentsInChildren<Renderer>(true))if(renderer.transform.name.StartsWith("Wall")||renderer.transform.parent&&renderer.transform.parent.name.StartsWith("Wall"))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        EditorSceneManager.SaveScene(garden.gameObject.scene);
    }
}
