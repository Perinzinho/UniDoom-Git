using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.ProBuilder;
using TotalChaosMaps;
using Object=UnityEngine.Object;

// One-off generator: compile only in .utmp/EntranceRebuild/Assets/_TemporaryTools/Editor.
public static class EntranceRebuilder
{
    static string room;
    static string Art=>"Assets/_Project/Scenes/"+room;
    static string Output=>Art+"/Materials/EntranceRebuild";
    static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static readonly List<string> meshes=new List<string>();
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Color C(float r,float g,float b)=>new Color(r,g,b);
    static string Shared="Assets/_Project/Global";
    static string Source(string asset,string type="Textures")
    {
        if(new[]{"wooden_bookshelf_worn","book_encyclopedia_set_01","marble_bust_01","drawer_cabinet","dining_chair_02"}.Contains(asset))return "Assets/_Project/Scenes/Library/"+type+"/"+asset;
        string owner=asset=="industrial_wall_lamp"?null:
            new[]{"painted_wooden_bench","fern_02","dead_tree_trunk_02","tree_small_02","cobblestone_floor_08","mossy_brick"}.Contains(asset)?"EntranceGarden":
            new[]{"vintage_telephone_wall_clock","korean_fire_extinguisher_01"}.Contains(asset)?"EntranceHallway":"DirectorRoom";
        return (owner==null?Shared:"Assets/_Project/Scenes/"+owner)+"/"+type+"/EntranceRebuild/"+asset;
    }
    public static void Build()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/EntranceRebuild/"))throw new Exception("Use isolated project");
        Directory.CreateDirectory("Evidence");
        ConfigureImports();
        foreach(var name in EntranceAudit.Rooms)
        {
            room=name;mats.Clear();
            Directory.CreateDirectory(Output);
            Directory.CreateDirectory(Art+"/Meshes/EntranceRebuild");
            Directory.CreateDirectory(Art+"/Textures/EntranceRebuild/Packed");
            AssetDatabase.Refresh();
            var root=PrefabUtility.LoadPrefabContents(EntranceAudit.Prefab(room));
            try
            {
                foreach(var pb in root.GetComponentsInChildren<ProBuilderMesh>()){pb.ToMesh();pb.Refresh();}
                if(room=="EntranceGarden")Garden(root);
                else if(room=="EntranceHallway")Hallway(root);
                else Office(root);
                RegisterLayout(root);
                PrefabUtility.SaveAsPrefabAsset(root,EntranceAudit.Prefab(room));
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        File.WriteAllLines("Evidence/generated-meshes.txt",meshes);
        VerifyAndRender();
        Debug.Log("ENTRANCE_REBUILD_PASS");
        EntrancePlayCheck.Run();
    }
    static void ConfigureImports()
    {
        var conversions=new List<string>();
        foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.Contains("/EntranceRebuild/")&&!p.Contains("/Packed/")&&p.EndsWith(".png")))
        {
            var original=File.ReadAllBytes(path);if(original.Length<25||original[24]!=16)continue;
            // The runtime textures use BC compression. Store their 8-bit channel inputs at full resolution.
            var texture=Read(path);var encoded=texture.EncodeToPNG();Object.DestroyImmediate(texture);
            File.WriteAllBytes(path,encoded);AssetDatabase.ImportAsset(path);
            conversions.Add(path+" | "+original.Length+" -> "+encoded.Length+" bytes; full resolution retained");
        }
        if(conversions.Count>0)File.AppendAllLines("Evidence/source-png-transforms.txt",conversions);
        foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.Contains("/EntranceRebuild/")&&!p.Contains("/Packed/")&&p.EndsWith(".png")))
        {
            var t=AssetImporter.GetAtPath(path) as TextureImporter;if(!t)continue;
            var type=path.Contains("nor_gl")?TextureImporterType.NormalMap:TextureImporterType.Default;
            bool srgb=path.Contains("Diffuse")||path.Contains("_diff")||path.Contains("emissive");
            if(t.textureType==type&&t.sRGBTexture==srgb&&t.maxTextureSize==2048&&t.mipmapEnabled&&t.filterMode==FilterMode.Trilinear&&t.anisoLevel==4&&t.textureCompression==TextureImporterCompression.CompressedHQ)continue;
            t.textureType=path.Contains("nor_gl")?TextureImporterType.NormalMap:TextureImporterType.Default;
            t.sRGBTexture=path.Contains("Diffuse")||path.Contains("_diff")||path.Contains("emissive");
            t.maxTextureSize=2048;t.mipmapEnabled=true;t.filterMode=FilterMode.Trilinear;t.anisoLevel=4;
            t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
        }
        foreach(var path in AssetDatabase.GetAllAssetPaths().Where(p=>p.Contains("/Models/EntranceRebuild/")&&p.EndsWith(".fbx")))
        {
            var t=AssetImporter.GetAtPath(path) as ModelImporter;
            if(t.materialImportMode==ModelImporterMaterialImportMode.ImportStandard&&!t.importCameras&&!t.importLights&&!t.isReadable&&!t.addCollider)continue;
            t.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;t.importCameras=false;t.importLights=false;
            t.isReadable=false;t.addCollider=false;t.SaveAndReimport();
        }
    }
    static Material Save(Material m)
    {
        string path=Output+"/"+m.name+".mat";
        var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);m=old;}else AssetDatabase.CreateAsset(m,path);
        mats[m.name]=m;return m;
    }
    static Material Plain(string name,Color color,float metal=0,float smooth=.2f)
    {
        var m=new Material(Shader.Find("UniDoom/TotalChaosInspiredSurface")){name=name,enableInstancing=true};
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
        m.SetVector("_SpecColor",new Vector4(0,0,.92f,1));return Save(m);
    }
    static Texture2D Read(string path)
    {var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);t.LoadImage(File.ReadAllBytes(path));return t;}
    static Texture2D ImportPNG(string path,Texture2D t,bool srgb)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.sRGBTexture=srgb;
        ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Surface(string name,string folder,Color tint,string diff="Diffuse",string normal="nor_gl",string arm="arm",string alpha=null)
    {
        var m=Plain(name,tint);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+diff+".png"));
        var n=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+normal+".png");
        if(n){m.SetTexture("_BumpMap",n);m.SetFloat("_BumpScale",.55f);m.EnableKeyword("_NORMALMAP");}
        if(File.Exists(folder+"/"+arm+".png"))
        {
            var t=Read(folder+"/"+arm+".png");var p=t.GetPixels32();
            for(int i=0;i<p.Length;i++){var c=p[i];p[i]=new Color32(c.b,c.r,c.g,(byte)(255-c.g));}
            t.SetPixels32(p);t.Apply();var packed=ImportPNG(Art+"/Textures/EntranceRebuild/Packed/"+name+".png",t,false);
            m.SetTexture("_MetallicGlossMap",packed);m.SetTexture("_OcclusionMap",packed);m.SetFloat("_Smoothness",.65f);
            m.SetFloat("_OcclusionStrength",.65f);m.EnableKeyword("_METALLICSPECGLOSSMAP");m.EnableKeyword("_OCCLUSIONMAP");
        }
        if(alpha!=null&&File.Exists(folder+"/"+alpha+".png"))
        {
            var d=Read(folder+"/"+diff+".png");var a=Read(folder+"/"+alpha+".png");var p=d.GetPixels32();var q=a.GetPixels32();
            if(p.Length!=q.Length)throw new Exception("Alpha dimensions");
            for(int i=0;i<p.Length;i++)p[i].a=q[i].r;
            d.SetPixels32(p);d.Apply();Object.DestroyImmediate(a);
            m.SetTexture("_BaseMap",ImportPNG(Art+"/Textures/EntranceRebuild/Packed/"+name+"RGBA.png",d,true));
            m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.42f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=2450;m.SetOverrideTag("RenderType","TransparentCutout");
        }
        EditorUtility.SetDirty(m);return m;
    }
    static Material Reuse(string name,string source,Color tint)
    {return Surface(name,source,tint);}
    static Material AssetMaterial(string asset,string prefix="",string name=null)
    {
        string key=name??asset+(prefix==""?"":"_"+prefix);
        if(mats.TryGetValue(key,out var found))return found;
        string diff=prefix==""?"Diffuse":prefix+"_diff",normal=prefix==""?"nor_gl":prefix+"_nor_gl",arm=prefix==""?"arm":prefix+"_arm";
        string alpha=asset=="fern_02"?"Alpha":prefix=="leaves"?"leaves_alpha":null;
        return Surface(key,Source(asset),Color.white,diff,normal,arm,alpha);
    }
    static Transform Group(Transform parent,string name,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static GameObject Box(Transform parent,string name,Vector3 pos,Vector3 size,Material material,bool collision=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material;if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());return go;
    }
    static void StripOldArt(Transform root)
    {
        foreach(var r in root.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        foreach(var l in root.GetComponentsInChildren<Light>(true))l.enabled=false;
        foreach(var c in root.GetComponentsInChildren<Collider>(true))if(!c.isTrigger)c.enabled=false;
    }
    static Bounds RenderBounds(Transform t)
    {
        var renderers=t.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("No renderers: "+t.name);
        var b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static Transform Model(Transform parent,string asset,Vector3 position,float height,float yaw=0,bool collider=true)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Source(asset,"Models")+"/model.fbx");
        if(!source)throw new Exception("Missing model "+asset);
        var wrap=Group(parent,asset,position);var obj=(GameObject)PrefabUtility.InstantiatePrefab(source,wrap);
        PrefabUtility.UnpackPrefabInstance(obj,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        obj.transform.localPosition=Vector3.zero;
        if(asset=="fern_02")
        {
            // The download is a four-plant collection. Use one natural variant per placement.
            var variants=obj.GetComponentsInChildren<MeshRenderer>();int pick=Mathf.Abs(Mathf.RoundToInt(position.z*10))%variants.Length;
            for(int i=0;i<variants.Length;i++)if(i!=pick)variants[i].gameObject.SetActive(false);
        }
        foreach(var r in obj.GetComponentsInChildren<Renderer>())
        {
            r.sharedMaterials=r.sharedMaterials.Select(old=>
            {
                string n=old?old.name.ToLowerInvariant():"";
                if(asset=="potted_plant_02")return AssetMaterial(asset,n.Contains("leave")||n.Contains("leaf")?"leaves":"pot");
                if(asset=="industrial_wall_lamp")return AssetMaterial(asset,n.Contains("glass")?"glass":"");
                if(asset=="korean_fire_extinguisher_01")return AssetMaterial(asset,n.Contains("paper")?"paper":n.Contains("glass")?"glass":"body");
                if(asset=="book_encyclopedia_set_01")return AssetMaterial(asset,n.Contains("paper")?"paper":"cover");
                if(asset=="tree_small_02")
                {
                    if(n.Contains("leav")||n.Contains("leaf"))
                    {
                        if(mats.TryGetValue("Natural tree leaves",out var leaves))return leaves;
                        return Surface("Natural tree leaves","Assets/_Project/Scenes/Library/Textures/DecayRevision/tree_small_02",C(.68f,.74f,.56f),"leaves_diff","leaves_nor_gl","unused","leaves_alpha");
                    }
                    return AssetMaterial(asset,n.Contains("branch")?"branch":"");
                }
                return AssetMaterial(asset);
            }).ToArray();
        }
        // Normalize in world metres and place the lowest point on the support surface.
        var b=RenderBounds(obj.transform);float factor=height/Mathf.Max(.001f,b.size.y);
        obj.transform.localScale*=factor;
        obj.transform.localRotation=Quaternion.Euler(0,yaw,0)*obj.transform.localRotation;b=RenderBounds(obj.transform);
        obj.transform.position+=wrap.position-new Vector3(b.center.x,b.min.y,b.center.z);
        if(collider)
        {
            b=RenderBounds(obj.transform);var c=wrap.gameObject.AddComponent<BoxCollider>();c.center=wrap.InverseTransformPoint(b.center);
            c.size=V(b.size.x/Mathf.Abs(wrap.lossyScale.x),b.size.y/Mathf.Abs(wrap.lossyScale.y),b.size.z/Mathf.Abs(wrap.lossyScale.z));
        }
        return wrap;
    }
    static void LightAt(Transform parent,string name,Vector3 p,Color color,float intensity,float range,bool shadow=false)
    {
        var t=Group(parent,name,p);var light=t.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;
        light.shadows=shadow?LightShadows.Soft:LightShadows.None;light.shadowBias=.025f;light.shadowNormalBias=.15f;
        var data=light.GetUniversalAdditionalLightData();var serialized=new SerializedObject(data);
        serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=1;serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    static Material Emission(string name,Color color,float strength)
    {var m=Plain(name,color);m.SetColor("_EmissionColor",color*strength);m.EnableKeyword("_EMISSION");return m;}
    static void Label(Transform parent,string title,Vector3 p,float yaw,float size,Material panel)
    {
        var t=Group(parent,title,p);t.localRotation=Quaternion.Euler(0,yaw,0);
        Box(t,"Enamel sign",Vector3.zero,V(1.1f,.3f,.025f),panel);
        var label=Group(t,"Lettering",V(0,0,-.018f));
        var text=label.gameObject.AddComponent<TextMeshPro>();text.text=title;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.color=C(.72f,.71f,.61f);
        text.rectTransform.sizeDelta=new Vector2(1.05f,.28f);text.enableAutoSizing=true;text.fontSizeMin=.7f;text.fontSizeMax=1.8f;text.enableWordWrapping=false;
    }
    static Transform MetricSpace(GameObject root,Vector3 localOrigin)
    {
        var old=root.transform.Find("Total Chaos Rebuild");if(old)Object.DestroyImmediate(old.gameObject);
        var t=Group(root.transform,"Total Chaos Rebuild",localOrigin);var scale=root.transform.lossyScale;t.localScale=V(1/scale.x,1/scale.y,1/scale.z);return t;
    }
    static void RegisterLayout(GameObject root)
    {
        var art=root.transform.Find("Total Chaos Rebuild");var floor=root.transform.Find("Floor/Photographic surface").GetComponent<MeshFilter>();
        var b=floor.sharedMesh.bounds;var s=Vector3.Scale(floor.transform.lossyScale,b.size);
        var layout=art.gameObject.AddComponent<EntranceDressingLayout>();layout.floor=floor;
        layout.clipToSideWalls=room=="EntranceGarden";
        layout.referenceSize=room=="EntranceGarden"?new Vector2(7.3f,24.6f):room=="EntranceHallway"?new Vector2(7.66f,28.7f):new Vector2(Mathf.Abs(s.x),Mathf.Abs(s.z));
        layout.placements=art.Cast<Transform>().Select(t=>new EntranceDressingLayout.Placement{target=t,position=t.localPosition,scale=t.localScale,sideWall=t.name=="Left photographic masonry"?-1:t.name=="Right photographic masonry"?1:0,
            stretchX=t.name=="Local atmosphere"||t.name=="Preserved coffered ceiling"||t.name=="Rear timber wainscot"||t.name=="Rear cornice",
            stretchZ=t.name=="Local atmosphere"||t.name=="Preserved coffered ceiling"||t.name=="Recessed planting border"||t.name=="Weathered coping"||t.name=="Segmented wall finish"||t.name=="Preserved timber wainscot"||t.name=="Wainscot brass bead"||t.name=="Ceiling timber beam"}).ToArray();
        layout.Refresh();EditorUtility.SetDirty(layout);
    }
    static void SurfaceShell(Transform shell,Material material,float metres=2)
    {
        var f=shell.GetComponent<MeshFilter>();var r=shell.GetComponent<MeshRenderer>();if(!f||!f.sharedMesh||!r)return;
        // Keep the existing ProBuilder component, topology, collider, transforms and fileIDs.
        // The new persistent mesh supplies metre-scaled UVs to an independent renderer.
        var t=shell.Find("Photographic surface");if(t)Object.DestroyImmediate(t.gameObject);
        t=Group(shell,"Photographic surface",Vector3.zero);
        var mesh=Object.Instantiate(f.sharedMesh);mesh.name=room+"_"+shell.name;
        var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
        for(int i=0;i<uv.Length;i++)
        {
            var p=shell.TransformVector(vertices[i]);var n=shell.TransformDirection(normals[i]);
            if(room=="EntranceHallway")p=Vector3.Scale(p,V(.4787097f,.35984954f,.61977f));
            if(room=="EntranceGarden"&&shell.name=="Floor")p=Vector3.Scale(p,V(7.3f/3,1,24.6f/8));
            if(Mathf.Abs(n.y)>.7f)uv[i]=new Vector2(p.x,p.z)/metres;
            else if(Mathf.Abs(n.x)>.7f)uv[i]=new Vector2(p.z,p.y)/metres;
            else uv[i]=new Vector2(p.x,p.y)/metres;
        }
        mesh.uv=uv;mesh.RecalculateTangents();
        string path=Art+"/Meshes/EntranceRebuild/"+mesh.name+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
        meshes.Add(path);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var mr=t.gameObject.AddComponent<MeshRenderer>();mr.sharedMaterials=Enumerable.Repeat(material,mesh.subMeshCount).ToArray();r.enabled=false;
    }
    static void Atmosphere(Transform art,Vector3 size,Color ambient,Color fog,float density,bool office=false)
    {
        var t=Group(art,"Local atmosphere",V(0,size.y*.5f,0));
        var region=t.gameObject.AddComponent<TCRegionAtmosphere>();region.localBounds=new Bounds(Vector3.zero,size);region.ambient=ambient;region.fogColor=fog;region.fogDensity=density;region.priority=office?50:25;region.blendDistance=office?.35f:1;
        region.theme=office?"preserved_director_office":"abandoned_entrance";
        region.flashlightIntensity=2.4f;region.flashlightRange=12;region.stabilizeBackgroundShadows=true;
        var c=t.gameObject.AddComponent<BoxCollider>();c.isTrigger=true;c.size=size;
        var volume=t.gameObject.AddComponent<Volume>();volume.isGlobal=false;volume.blendDistance=region.blendDistance;volume.priority=region.priority;
        string path=Art+"/Profiles/EntranceRebuild.asset";Directory.CreateDirectory(Path.GetDirectoryName(path));
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
        foreach(var component in profile.components.ToArray()){profile.Remove(component.GetType());Object.DestroyImmediate(component,true);}
        var tone=profile.Add<Tonemapping>(true);tone.mode.value=TonemappingMode.ACES;
        var color=profile.Add<ColorAdjustments>(true);color.postExposure.value=office?.15f:.25f;color.saturation.value=office?-7:-20;color.contrast.value=office?5:8;
        var bloom=profile.Add<Bloom>(true);bloom.intensity.value=.08f;bloom.threshold.value=1.4f;
        var vignette=profile.Add<Vignette>(true);vignette.intensity.value=office?.10f:.16f;vignette.smoothness.value=.35f;
        foreach(var component in profile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);
        EditorUtility.SetDirty(profile);volume.sharedProfile=profile;
    }
    static void Garden(GameObject root)
    {
        var old=root.GetComponentsInChildren<EntranceGardenLayout>(true).FirstOrDefault();
        if(old)StripOldArt(old.transform);
        // Existing floor fitting remains active for the scene's garden width/depth overrides.
        var floor=root.transform.Find("Floor");
        var paving=AssetMaterial("cobblestone_floor_08");SurfaceShell(floor,paving,2);
        var wall=AssetMaterial("mossy_brick");foreach(Transform s in root.transform)if(s.name.StartsWith("Wall"))SurfaceShell(s,wall,2);
        var art=MetricSpace(root,V(0,.1f,0));
        BuildGardenArt(art,root);
    }
    static void BuildGardenArt(Transform art,GameObject root)
    {
        var floor=root.transform.Find("Floor").GetComponent<Renderer>().bounds;
        float width=7.3f,depth=24.6f;
        float left=-width*.5f+.32f,right=width*.5f-.32f;
        var iron=Plain("Oxidized garden iron",C(.10f,.12f,.105f),.45f,.17f);
        var stone=Reuse("Damp stone edging","Assets/_Project/Scenes/ClassRoom/Surface/Textures/concrete_wall_006",C(.52f,.56f,.48f));
        var dirt=Reuse("Planting soil","Assets/_Project/Scenes/Library/Textures/worn_mossy_plasterwall",C(.24f,.27f,.19f));
        dirt.SetTextureScale("_BaseMap",new Vector2(.52f,depth));
        var facing=Save(new Material(AssetMaterial("mossy_brick")){name="Mossy wall facing"});facing.SetTextureScale("_BaseMap",new Vector2(depth/2,1.8f));
        foreach(int side in new[]{-1,1})Box(art,side<0?"Left photographic masonry":"Right photographic masonry",V(side*width*.5f,1.8f,0),V(.035f,3.6f,depth),facing);
        foreach(float x in new[]{left,right})
        {
            Box(art,"Recessed planting border",V(x,.05f,0),V(.52f,.1f,depth*.96f),dirt);
            Box(art,"Weathered coping",V(x-Mathf.Sign(x)*.28f,.085f,0),V(.09f,.17f,depth*.96f),stone,true);
            for(float z=-depth*.43f;z<=depth*.43f;z+=Mathf.Max(2.5f,depth/7))
            {
                Model(art,"fern_02",V(x,.1f,z),.65f+(Mathf.Abs(z)%1)*.18f,Mathf.Sign(x)*42+z*17,false);
            }
            foreach(float z in new[]{-depth*.3f,depth*.33f})Model(art,"dead_tree_trunk_02",V(x,.09f,z),.30f,x<0?85:100,false);
            Model(art,"tree_small_02",V(x,.08f,x<0?-depth*.18f:depth*.29f),3.2f,x<0?30:175,false);
            // Narrow garden: benches sit along the wall, preserving the central route.
            var bench=Model(art,"painted_wooden_bench",V(x,.1f,-depth*.04f),.8f,x<0?90:-90,true);
            foreach(float z in new[]{-depth*.38f,depth*.31f})
            {
                var lamp=Model(art,"industrial_wall_lamp",V(x,1.65f,z),.45f,x<0?90:-90,false);
                LightAt(art,"Caged garden lamp",V(x-Mathf.Sign(x)*.12f,1.9f,z),C(1,.68f,.35f),3.0f,5f,true);
            }
        }
        Atmosphere(art,V(width,4.8f,depth),C(.42f,.46f,.43f),C(.15f,.18f,.18f),.018f);
    }
    static void Hallway(GameObject root)
    {
        var floor=root.transform.Find("Floor");var bounds=floor.GetComponent<Renderer>().bounds;
        var local=root.transform.InverseTransformPoint(V(bounds.center.x,bounds.max.y,bounds.center.z));
        var art=MetricSpace(root,local);
        var plaster=Reuse("Peeling university plaster","Assets/_Project/Scenes/Library/Textures/DecayRevision/damaged_plaster",C(.70f,.73f,.65f));
        var tile=Reuse("Worn institutional tiles","Assets/_Project/Scenes/ClassRoom/Surface/Textures/worn_tile_floor",C(.64f,.67f,.61f));
        var ceiling=Reuse("Neglected mineral ceiling","Assets/_Project/Scenes/Library/Textures/CampusFinish/white_plaster_02",C(.55f,.59f,.55f));
        foreach(Transform s in root.transform){if(s==art)continue;if(s.name=="Floor")SurfaceShell(s,tile,2.4f);else if(s.name=="Roof")SurfaceShell(s,ceiling,2);else if(s.name.StartsWith("Wall"))SurfaceShell(s,plaster,2);}
        BuildHallArt(art,root,bounds);
    }
    static void BuildHallArt(Transform art,GameObject root,Bounds floor)
    {
        float w=7.66f,d=28.7f;
        var dark=Reuse("Faded institutional enamel","Assets/_Project/Scenes/Library/Textures/CampusFinish/painted_plaster_wall",C(.35f,.42f,.38f));
        dark.SetTextureScale("_BaseMap",new Vector2(4,.7f));
        var trim=Plain("Scuffed rubber skirting",C(.07f,.085f,.075f),0,.12f);
        var glow=Emission("Cool fluorescent phosphor",C(.67f,.79f,.72f),2.2f);
        foreach(Transform shell in root.transform)
        {
            if(!shell.name.StartsWith("Wall"))continue;
            var r=shell.GetComponent<Renderer>();var p=art.InverseTransformPoint(r.bounds.center);
            float x=p.x*.4787097f-Mathf.Sign(p.x)*.07f,z=p.z*.61977f,length=r.bounds.size.z*.61977f;
            var segment=Group(art,"Segmented wall finish",V(x,0,z));
            Box(segment,"Continuous skirting",V(0,.09f,0),V(.08f,.18f,length),trim);
            Box(segment,"Institutional lower wall",V(0,.67f,0),V(.045f,1.1f,length),dark);
            Box(segment,"Wall protection rail",V(-Mathf.Sign(x)*.025f,1.25f,0),V(.07f,.06f,length),trim);
        }
        for(float z=-d*.4f;z<d*.45f;z+=5.5f)
        {
            Box(art,"Fluorescent metal tray",V(0,3.15f,z),V(1.25f,.08f,.22f),dark);
            Box(art,"Fluorescent suspension",V(0,3.32f,z),V(.035f,.27f,.035f),trim);
            Box(art,"Working fluorescent tube",V(0,3.09f,z-.06f),V(1.12f,.035f,.035f),glow);
            Box(art,"Working fluorescent tube",V(0,3.09f,z+.06f),V(1.12f,.035f,.035f),glow);
            LightAt(art,"Fluorescent pool",V(0,2.93f,z),C(.72f,.84f,.77f),4.8f,8,Mathf.Abs(z)<8);
        }
        Model(art,"vintage_telephone_wall_clock",V(-w*.5f+.20f,1.2f,-d*.25f),.9f,90,false);
        Model(art,"korean_fire_extinguisher_01",V(w*.5f-.20f,.12f,d*.22f),.65f,-90,false);
        Model(art,"drawer_cabinet",V(-w*.5f+.44f,0,d*.23f),1.3f,90,true);
        foreach(float z in new[]{-d*.22f,-d*.22f+1.05f})Model(art,"dining_chair_02",V(-w*.5f+.52f,0,z),.89f,90,true);
        foreach(float z in new[]{-d*.31f,d*.32f})
        {
            Model(art,"industrial_wall_lamp",V(w*.5f-.20f,1.9f,z),.42f,-90,false);
            LightAt(art,"Dim caged wall light",V(w*.5f-.42f,2.12f,z),C(.85f,.67f,.4f),.8f,3.6f);
        }
        Label(art,"DIRETORIA",V(w*.5f-.12f,2.1f,d*.30f),-90,2.5f,dark);
        Atmosphere(art,V(w,4.5f,d),C(.22f,.25f,.23f),C(.075f,.09f,.082f),.012f);
    }
    static void Office(GameObject root)
    {
        var old=root.transform.Find("HauntedOfficeInterior");if(old)StripOldArt(old);
        var art=MetricSpace(root,V(0,.1f,0));
        var plaster=Reuse("Preserved warm plaster","Assets/_Project/Scenes/Library/Textures/CampusFinish/painted_plaster_wall",C(.94f,.89f,.76f));
        var ceiling=Reuse("Preserved ceiling","Assets/_Project/Scenes/Library/Textures/CampusFinish/white_plaster_02",C(.93f,.90f,.79f));
        var parquet=AssetMaterial("herringbone_parquet");var wood=AssetMaterial("wood_table");
        foreach(Transform s in root.transform){if(s.name=="Floor")SurfaceShell(s,parquet,2);else if(s.name.StartsWith("Wall"))SurfaceShell(s,plaster,2);else if(s.name=="Celing")SurfaceShell(s,ceiling,2);}
        BuildOfficeArt(art,root,wood,ceiling);
    }
    static void BuildOfficeArt(Transform art,GameObject root,Material wood,Material ceiling)
    {
        var fb=root.transform.Find("Floor").GetComponent<Renderer>().bounds;float w=fb.size.x,d=fb.size.z;
        var brass=Plain("Polished old brass",C(.44f,.30f,.13f),.72f,.48f);
        var green=Plain("Director office green",C(.12f,.18f,.135f),0,.27f);
        var paper=Plain("Ivory correspondence",C(.83f,.80f,.67f));
        // An impeccably aligned composition contrasts with all surrounding spaces.
        foreach(float x in new[]{-w*.5f+.13f,w*.5f-.13f})
        {
            Box(art,"Preserved timber wainscot",V(x,.60f,0),V(.12f,1.20f,d-.12f),wood);
            Box(art,"Wainscot brass bead",V(x-Mathf.Sign(x)*.065f,1.22f,0),V(.035f,.025f,d-.12f),brass);
            for(float z=-d*.5f+.45f;z<d*.5f;z+=.8f)Box(art,"Raised wall panel stile",V(x-Mathf.Sign(x)*.07f,.6f,z),V(.035f,1.15f,.045f),wood);
        }
        Box(art,"Rear timber wainscot",V(0,.6f,d*.5f-.13f),V(w-.2f,1.2f,.12f),wood);
        Box(art,"Rear cornice",V(0,3.65f,d*.5f-.16f),V(w-.2f,.12f,.15f),wood);
        // Lower suspended ceiling gives the original tall shell a believable office scale.
        Box(art,"Preserved coffered ceiling",V(0,3.8f,0),V(w-.1f,.12f,d-.1f),ceiling);
        foreach(float x in new[]{-w*.3f,0,w*.3f})Box(art,"Ceiling timber beam",V(x,3.7f,0),V(.075f,.08f,d-.1f),wood);
        float deskZ=d*.13f;
        const float furnitureScale=1.3f;
        var desk=Model(art,"metal_office_desk",V(0,0,deskZ),.78f*furnitureScale,180,true);
        var db=RenderBounds(desk);float top=db.max.y-art.position.y;
        Model(art,"desk_lamp_arm_01",V(-.58f*furnitureScale,top,deskZ+.12f*furnitureScale),.52f*furnitureScale,35,false);
        LightAt(art,"Stable warm desk lamp",V(-.48f*furnitureScale,top+.4f*furnitureScale,deskZ-.1f*furnitureScale),C(1,.77f,.48f),1.7f,3.6f,true);
        Box(art,"Precisely aligned leather blotter",V(0,top+.009f*furnitureScale,deskZ-.10f*furnitureScale),V(.70f,.014f,.36f)*furnitureScale,green);
        for(int i=0;i<4;i++)Box(art,"Ordered correspondence",V(.48f*furnitureScale,top+(.012f+i*.004f)*furnitureScale,deskZ+.03f*furnitureScale),V(.21f,.003f,.28f)*furnitureScale,paper);
        Model(art,"mid_century_lounge_chair",V(0,0,deskZ+1.12f*furnitureScale),1.02f*furnitureScale,180,true);
        foreach(float x in new[]{-.85f,.85f})Model(art,"mid_century_lounge_chair",V(x*furnitureScale,0,deskZ-1.5f*furnitureScale),.88f*furnitureScale,0,true);
        foreach(float x in new[]{-w*.5f+.65f,w*.5f-.65f})
        {
            Model(art,"potted_plant_02",V(x,0,d*.5f-.70f),1.45f,20,false);
        }
        foreach(float x in new[]{-w*.27f,w*.27f})
        {
            Model(art,"wooden_bookshelf_worn",V(x,0,d*.5f-.40f),2.15f,0,true);
            Model(art,"book_encyclopedia_set_01",V(x,.68f,d*.5f-.50f),.28f,0,false);
            Model(art,"book_encyclopedia_set_01",V(x,1.20f,d*.5f-.50f),.28f,0,false);
        }
        Box(art,"Director's sculpture pedestal",V(w*.5f-.48f,.55f,-d*.17f),V(.52f,1.1f,.52f),wood,true);
        Model(art,"marble_bust_01",V(w*.5f-.48f,1.10f,-d*.17f),.59f,245,false);
        var glow=Emission("Director pendant opal glass",C(.95f,.86f,.66f),1.5f);
        foreach(float z in new[]{-d*.26f,d*.25f})
        {
            Box(art,"Pendant bronze suspension",V(0,3.50f,z),V(.035f,.45f,.035f),brass);
            Box(art,"Opal pendant diffuser",V(0,3.25f,z),V(.80f,.07f,.46f),glow);
            LightAt(art,"Steady office pendant",V(0,3.05f,z),C(1,.87f,.67f),4.3f,7,true);
        }
        Label(art,"DIRETORIA",V(0,2.55f,d*.5f-.15f),0,3.4f,green);
        Atmosphere(art,V(w,3.8f,d),C(.29f,.275f,.23f),C(.13f,.14f,.125f),.002f,true);
    }
    public static void VerifyAndRender()
    {
        Directory.CreateDirectory("Evidence");var report=new StringBuilder();
        ShaderUtil.allowAsyncCompilation=false;
        foreach(var name in EntranceAudit.Rooms)
        {
            room=name;EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(EntranceAudit.Prefab(room)));
            root.transform.position=Vector3.zero;root.transform.rotation=Quaternion.identity;
            var art=root.transform.Find("Total Chaos Rebuild");if(!art)throw new Exception("Missing new art");
            foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.enabled&&r.sharedMaterials.Any(m=>!m))throw new Exception("Null material "+r.name);
            foreach(var t in root.GetComponentsInChildren<Transform>())if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script "+t.name);
            report.AppendLine(room+": renderers="+root.GetComponentsInChildren<Renderer>().Count(r=>r.enabled)+", lights="+root.GetComponentsInChildren<Light>().Count(l=>l.enabled)+", missing scripts/materials=0");
            long triangles=0;foreach(var f in root.GetComponentsInChildren<MeshFilter>()){var renderer=f.GetComponent<Renderer>();if(f.sharedMesh&&renderer&&renderer.enabled)for(int i=0;i<f.sharedMesh.subMeshCount;i++)triangles+=(long)f.sharedMesh.GetIndexCount(i)/3;}
            report.AppendLine("  Instanced triangles="+triangles);
            File.WriteAllLines("Evidence/"+room+"-dependencies.txt",AssetDatabase.GetDependencies(EntranceAudit.Prefab(room),true));
            var region=art.GetComponentInChildren<TCRegionAtmosphere>();
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=region.ambient;
            var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(region.ambient);RenderSettings.ambientProbe=probe;
            RenderSettings.fog=true;RenderSettings.fogColor=region.fogColor;RenderSettings.fogDensity=region.fogDensity;RenderSettings.fogMode=FogMode.ExponentialSquared;
            var camera=new GameObject("Review camera").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=65;camera.nearClipPlane=.06f;camera.farClipPlane=150;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=region.fogColor;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            foreach(var layout in root.GetComponentsInChildren<EntranceDressingLayout>())layout.Refresh();
            var b=root.transform.Find("Floor/Photographic surface").GetComponent<Renderer>().bounds;
            Vector3 origin=V(b.center.x,b.max.y,b.center.z);
            Vector3 from=room=="DirectorRoom"?origin+V(-b.size.x*.28f,1.65f,-b.size.z*.33f):origin+V(0,1.65f,-b.size.z*.40f);
            Vector3 target=room=="DirectorRoom"?origin+V(.2f,1.2f,b.size.z*.17f):origin+V(0,1.45f,b.size.z*.40f);
            camera.transform.position=from;camera.transform.LookAt(target);
            Capture(camera,"Evidence/"+room+".png");
            report.AppendLine("  Floor bounds="+b+" camera="+from+" look="+target);
        }
        File.WriteAllText("Evidence/validation.txt",report.ToString());Debug.Log("ENTRANCE_VERIFY_PASS");
        VerifyMap();
    }
    static void VerifyMap()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        ShaderUtil.allowAsyncCompilation=false;
        var report=new StringBuilder();
        var camera=new GameObject("Entrance map review").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=65;camera.nearClipPlane=.06f;camera.farClipPlane=180;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        foreach(var name in EntranceAudit.Rooms)
        {
            var root=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name==name);
            var art=root.Find("Total Chaos Rebuild");var layout=art.GetComponent<EntranceDressingLayout>();layout.Refresh();
            var size=layout.Size;
            camera.transform.position=art.TransformPoint(name=="DirectorRoom"?V(-size.x*.28f,1.65f,-size.y*.33f):V(0,1.65f,-size.y*.40f));
            camera.transform.LookAt(art.TransformPoint(name=="DirectorRoom"?V(.2f,1.2f,size.y*.17f):V(0,1.45f,size.y*.40f)));
            var choices=Object.FindObjectsByType<TCRegionAtmosphere>(FindObjectsSortMode.None);
            var region=TCRegionAtmosphere.ApplyForCamera(camera,choices,false);
            if(!region||!region.transform.IsChildOf(art))throw new Exception("Incorrect regional atmosphere "+name);
            camera.backgroundColor=region.fogColor;
            Capture(camera,"Evidence/InMap-"+name+".png");
            // Keep a clear capsule through the central travel route; furniture stays in the side bands.
            Physics.SyncTransforms();int blocked=0;
            for(int i=0;i<15;i++)
            {
                float z=Mathf.Lerp(-size.y*.43f,size.y*.43f,i/14f);
                if(name=="DirectorRoom"&&z>size.y*.13f-1.9f)continue;
                var p=art.TransformPoint(V(0,.04f,z));
                var hits=Physics.OverlapCapsule(p+Vector3.up*.3f,p+Vector3.up*1.5f,.25f,~0,QueryTriggerInteraction.Ignore);
                if(hits.Any(c=>c.enabled&&c.transform.IsChildOf(art)))blocked++;
            }
            if(blocked!=0)throw new Exception("New decoration blocks route: "+name+" "+blocked);
            report.AppendLine(name+": size="+size+", atmosphere="+region.theme+", central capsule samples clear");
        }
        TCRegionAtmosphere.ApplyForCamera(camera,Array.Empty<TCRegionAtmosphere>(),false);
        File.WriteAllText("Evidence/map-validation.txt",report.ToString());Debug.Log("ENTRANCE_MAP_PASS");
    }
    static void Capture(Camera camera,string path)
    {
        var rt=new RenderTexture(1440,900,24,RenderTextureFormat.ARGBHalf);var ldr=new RenderTexture(1440,900,0,RenderTextureFormat.ARGB32);
        var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=rt;for(int i=0;i<5;i++)camera.Render();Graphics.Blit(rt,ldr);RenderTexture.active=ldr;
            var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(rt);Object.DestroyImmediate(ldr);}
    }
}
