using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object = UnityEngine.Object;

public static class BasementRitualBuilder
{
    const string Room="Assets/prefabs/Scenario/Basement.prefab";
    const string Art="Assets/Art/Basement/Ritual";
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    static readonly Dictionary<string,List<CombineInstance>> combined=new Dictionary<string,List<CombineInstance>>();
    static Transform dressing;
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static T Load<T>(string path) where T:Object=>AssetDatabase.LoadAssetAtPath<T>(path);
    static Transform Group(Transform parent,string name,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static Material Save(Material m)
    {
        string path=Art+"/Materials/"+m.name+".mat";var old=Load<Material>(path);
        if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static Material Simple(string name,Color color,Material source=null)
    {
        var m=source?new Material(source):new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.18f);
        return materials[name]=Save(m);
    }
    static Material Pbr(string name,string prefix,Color tint,float tiles=1)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};
        m.SetTexture("_BaseMap",Load<Texture2D>(prefix+"diff.png"));m.SetColor("_BaseColor",tint);
        m.SetTexture("_BumpMap",Load<Texture2D>(prefix+"nor_gl.png"));m.SetFloat("_BumpScale",.7f);m.EnableKeyword("_NORMALMAP");
        var arm=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        if(!ImageConversion.LoadImage(arm,File.ReadAllBytes(prefix+"arm.png")))throw new Exception("ARM decode");
        var pixels=arm.GetPixels32();var packed=new Texture2D(arm.width,arm.height,TextureFormat.RGBA32,false,true);
        for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color32(c.b,c.r,c.g,(byte)(255-c.g));}
        packed.SetPixels32(pixels);packed.Apply();string mask=Art+"/Textures/"+name+"_UnityMask.png";
        File.WriteAllBytes(mask,packed.EncodeToPNG());Object.DestroyImmediate(arm);Object.DestroyImmediate(packed);
        AssetDatabase.ImportAsset(mask);var ti=(TextureImporter)AssetImporter.GetAtPath(mask);ti.sRGBTexture=false;ti.SaveAndReimport();
        m.SetTexture("_MetallicGlossMap",Load<Texture2D>(mask));m.SetTexture("_OcclusionMap",Load<Texture2D>(mask));
        m.EnableKeyword("_METALLICSPECGLOSSMAP");m.EnableKeyword("_OCCLUSIONMAP");m.SetFloat("_Smoothness",.55f);m.SetFloat("_OcclusionStrength",.8f);
        foreach(string prop in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap","_OcclusionMap"})m.SetTextureScale(prop,Vector2.one*tiles);
        return materials[name]=Save(m);
    }
    static void ImportAssets()
    {
        foreach(string path in Directory.GetFiles(Art,"*.png",SearchOption.AllDirectories))
        {
            var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=path.Contains("nor_gl")?TextureImporterType.NormalMap:TextureImporterType.Default;
            i.sRGBTexture=!(path.Contains("nor_gl")||path.Contains("arm")||path.Contains("UnityMask"));
            i.mipmapEnabled=true;i.filterMode=FilterMode.Trilinear;i.anisoLevel=4;i.maxTextureSize=2048;i.textureCompression=TextureImporterCompression.CompressedHQ;
            if(path.Contains("Seal")||path.Contains("flame")){i.alphaIsTransparency=true;i.wrapMode=TextureWrapMode.Clamp;i.textureCompression=TextureImporterCompression.Uncompressed;}
            i.SaveAndReimport();
        }
        foreach(string id in new[]{"brass_candleholders","brass_goblets"})
        {
            string dir=Art+"/Models/"+id+"/";
            var importer=(ModelImporter)AssetImporter.GetAtPath(dir+id+".fbx");
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
            importer.addCollider=false;importer.isReadable=true;importer.SaveAndReimport();
            foreach(string file in Directory.GetFiles(dir,"*_diff.png"))
            {
                string stem=Path.GetFileNameWithoutExtension(file).Replace("_diff","");
                if(stem=="flame")
                {
                    var f=Simple("Candle_flame",Color.white);f.SetTexture("_BaseMap",Load<Texture2D>(file));f.SetTexture("_EmissionMap",Load<Texture2D>(file));
                    f.SetColor("_EmissionColor",new Color(3.5f,2.7f,1.6f));f.EnableKeyword("_EMISSION");f.SetFloat("_AlphaClip",1);f.SetFloat("_Cutoff",.15f);
                    f.EnableKeyword("_ALPHATEST_ON");f.SetFloat("_Cull",0);f.renderQueue=2450;f.SetOverrideTag("RenderType","TransparentCutout");EditorUtility.SetDirty(f);
                }
                else Pbr(stem,dir+stem+"_",stem.Contains("candles")?new Color(.7f,.64f,.46f):new Color(.65f,.57f,.4f));
            }
        }
        Pbr("Worn_monastery_limestone",Art+"/Textures/monastery_stone_floor_",new Color(.48f,.43f,.35f),.34f);
        Pbr("Old_crypt_masonry",Art+"/Textures/old_stone_wall_",new Color(.48f,.45f,.39f),.28f);
        Simple("Dry_ochre_inscription",new Color(.29f,.20f,.105f));
        var cloth=Load<Material>("Assets/Art/Basement/Materials/FinalShroud.mat");
        if(!cloth)cloth=AssetDatabase.FindAssets("FinalShroud t:Material").Select(g=>Load<Material>(AssetDatabase.GUIDToAssetPath(g))).First();
        Simple("Funerary_oxblood_cloth",new Color(.16f,.055f,.038f),cloth);
        var seal=new Material(Load<Material>("Assets/Art/Basement/ArenaPolish/Materials/AltarReferenceSeal.mat")){name="Altar_fine_line_seal"};
        seal.SetTexture("_BaseMap",Load<Texture2D>(Art+"/Textures/AltarFineLineSeal.png"));seal.SetTexture("_EmissionMap",Load<Texture2D>(Art+"/Textures/AltarFineLineSeal.png"));
        seal.SetColor("_BaseColor",new Color(.9f,.72f,.6f));seal.SetColor("_EmissionColor",new Color(.7f,.18f,.095f));seal.SetFloat("_Cutoff",.15f);
        materials[seal.name]=Save(seal);
    }
    static void Piece(Mesh mesh,Vector3 p,Quaternion q,Material mat)
    {
        if(!combined.TryGetValue(mat.name,out var list))combined[mat.name]=list=new List<CombineInstance>();
        list.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(p,q,Vector3.one)});
    }
    static void Box(Vector3 p,Vector3 size,Material mat)=>Piece(ChaosGeometry.BevelBox(size,.025f),p,Quaternion.identity,mat);
    static void Beam(Vector3 a,Vector3 b,float width,Material mat)
    {Piece(ChaosGeometry.BevelBox(V(width,(a-b).magnitude,width),.025f),(a+b)/2,Quaternion.FromToRotation(Vector3.up,b-a),mat);}
    static void Arc(float radius,float y,float start,float end,Material mat,float width=.04f)
    {
        int count=Mathf.CeilToInt((end-start)/3);var path=new Vector3[count+1];
        for(int i=0;i<=count;i++){float a=Mathf.Lerp(start,end,i/(float)count)*Mathf.Deg2Rad;path[i]=V(-10+Mathf.Sin(a)*radius,y,18.5f+Mathf.Cos(a)*radius);}
        Piece(ChaosGeometry.Sweep(path,width,5),Vector3.zero,Quaternion.identity,mat);
    }
    static void FinishCombined()
    {
        foreach(var entry in combined)
        {
            var mesh=new Mesh{name="Ritual_"+entry.Key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(entry.Value.ToArray(),true,true);mesh.RecalculateBounds();
            string path=Art+"/Meshes/"+mesh.name+".asset";var old=Load<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var go=Group(dressing,"Ritual masonry and inlay - "+entry.Key,Vector3.zero).gameObject;go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=materials[entry.Key];
            foreach(var part in entry.Value)Object.DestroyImmediate(part.mesh);
        }
        combined.Clear();
    }
    static Transform Model(Transform parent,string id,Vector3 p,float height,float yaw)
    {
        var holder=Group(parent,"CC0 "+id,p);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(Art+"/Models/"+id+"/"+id+".fbx"));go.transform.SetParent(holder,false);
        PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var renderers=go.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        go.transform.localScale*=height/bounds.size.y;
        bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
        go.transform.position+=holder.position-V(bounds.center.x,bounds.min.y,bounds.center.z);
        foreach(var r in renderers)
        {
            r.sharedMaterials=r.sharedMaterials.Select(source=>{
                string n=source?source.name.ToLowerInvariant():r.name.ToLowerInvariant();
                string key=n.Contains("flame")?"Candle_flame":n.Contains("candles")?"candles":null;
                if(key==null){string number=n.Contains("03")?"03":n.Contains("02")?"02":"01";key=id=="brass_goblets"?"brass_goblet_"+number:number;}
                if(!materials.ContainsKey(key))throw new Exception("Unmapped model material "+n+" -> "+key);
                return materials[key];}).ToArray();
        }
        holder.localRotation=Quaternion.Euler(0,yaw,0);return holder;
    }
    static Light Light(Transform parent,string name,Vector3 p,Color color,float intensity,float range,Vector3? target=null,bool shadow=false)
    {
        var t=Group(parent,name,p);var l=t.gameObject.AddComponent<Light>();l.color=color;l.intensity=intensity;l.range=range;
        l.shadows=shadow?LightShadows.Soft:LightShadows.None;l.shadowBias=.035f;l.shadowNormalBias=.12f;
        if(target.HasValue){l.type=LightType.Spot;t.LookAt(parent.TransformPoint(target.Value));l.spotAngle=100;l.innerSpotAngle=60;}else l.type=LightType.Point;
        var d=l.GetUniversalAdditionalLightData();d.usePipelineSettings=false;
        var serialized=new SerializedObject(d);serialized.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;serialized.ApplyModifiedPropertiesWithoutUndo();
        return l;
    }
    static void Banner(Transform parent,Vector3 p,float width,float height,int seed)
    {
        var b=new ChaosGeometry.Builder();int nx=12,ny=16;
        Func<int,int,Vector3> point=(x,y)=>V((x/(float)nx-.5f)*width,-y/(float)ny*height+(y==ny?.14f*Mathf.Sin(x*2.3f+seed):0),.11f*Mathf.Sin(x*.9f+seed)+.12f*y/ny);
        for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){var a=point(x,y);var c=point(x+1,y);var d=point(x+1,y+1);var e=point(x,y+1);b.Face(a,c,d,e);b.Face(e,d,c,a);}
        var mesh=b.Mesh("Funerary_banner_"+seed);AssetDatabase.CreateAsset(mesh,Art+"/Meshes/"+mesh.name+".asset");
        var t=Group(parent,"Frayed funerary hanging",p);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=materials["Funerary_oxblood_cloth"];
    }
    public static void Build()
    {
        BasementSurfaceBuilder.RequireIsolated();materials.Clear();combined.Clear();
        foreach(string folder in new[]{"Materials","Meshes","Profiles"})Directory.CreateDirectory(Art+"/"+folder);AssetDatabase.Refresh();
        Capture("BeforeRitual");ImportAssets();
        var root=PrefabUtility.LoadPrefabContents(Room);
        try
        {
            var boss=root.transform.Find("BossRoom");if(boss.Find("Ritual chamber dressing"))throw new Exception("Restore snapshot before rebuilding");
            float floor=BasementFinalBuilder.Floor(boss,"Main_Floor"),front=boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
            var anchor=boss.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Boss altar and infernal engine");
            dressing=Group(boss,"Ritual chamber dressing",V(0,floor,front));
            var altar=Group(anchor,"Ritual altar offerings and funerary hangings",Vector3.zero);
            altar.position=boss.TransformPoint(V(0,floor,front));altar.rotation=boss.rotation;
            altar.localScale=V(boss.lossyScale.x/anchor.lossyScale.x,boss.lossyScale.y/anchor.lossyScale.y,boss.lossyScale.z/anchor.lossyScale.z);
            var stone=materials["Old_crypt_masonry"];var limestone=materials["Worn_monastery_limestone"];var ink=materials["Dry_ochre_inscription"];
            foreach(var r in boss.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(r.name=="Main_Floor")r.sharedMaterial=limestone;
                else if(r.transform.parent && r.transform.parent.name=="Walls" && (r.name.Contains("Wall")&&!r.name.Contains("Ceiling")))r.sharedMaterial=stone;
                if(r.sharedMaterials.Any(m=>m && m.name=="AltarReferenceSeal"))r.sharedMaterials=r.sharedMaterials.Select(m=>m&&m.name=="AltarReferenceSeal"?materials["Altar_fine_line_seal"]:m).ToArray();
                // The original red beacons sit in the combined Red material. Keep every body mesh intact.
                if(r.sharedMaterial && r.sharedMaterial.name=="Red" && r.transform.IsChildOf(boss.Find("BasementFinalSanctum")))r.enabled=false;
            }
            for(int side=-1;side<=1;side+=2)
            {
                float x=-10+side*20.2f;
                for(int j=0;j<3;j++)
                {
                    float z=7+j*10.5f;
                    foreach(float dz in new[]{-2.4f,2.4f}){Box(V(x,2.8f,z+dz),V(.65f,5.6f,.5f),stone);Box(V(x,.28f,z+dz),V(.9f,.55f,.8f),stone);}
                    var points=new[]{V(x,5.6f,z-2.4f),V(x,6.6f,z-1.5f),V(x,7.35f,z),V(x,6.6f,z+1.5f),V(x,5.6f,z+2.4f)};
                    for(int i=0;i<points.Length-1;i++)Beam(points[i],points[i+1],.5f,stone);
                    Model(dressing,"brass_candleholders",V(-10+side*12.3f,0,8+j*9.4f),1.1f,side*(15+j*37));
                    Light(dressing,"Warm votive cluster",V(-10+side*12.3f,1.1f,8+j*9.4f),new Color(1,.51f,.19f),3.2f,6.3f);
                }
                for(int i=0;i<2;i++)Banner(altar,V(-10+side*(10.6f+i*3.6f),9.5f,35.1f),2.5f,5.8f+i*.5f,3+i+(side+1)*2);
                Model(altar,"brass_candleholders",V(-10+side*2,3.08f,31.6f),1.05f,side*20);
                Light(altar,"Altar candle pool",V(-10+side*2,4.1f,31),new Color(1,.58f,.27f),3.3f,5.5f);
                Model(altar,"brass_goblets",V(-10+side*.85f,3.08f,31.3f),.42f,side*25);
            }
            for(int i=0;i<12;i++){Arc(6.25f,.045f,i*30+1,i*30+28,ink);Arc(6.62f,.045f,i*30+2,i*30+27,ink,.025f);}
            for(int i=0;i<24;i++)
            {
                float a=i*Mathf.PI/12;var u=V(Mathf.Sin(a),0,Mathf.Cos(a));var c=V(-10,.048f,18.5f)+u*6.95f;
                Beam(c-u*.16f,c+u*.16f,.035f,ink);
            }
            FinishCombined();
            foreach(var l in boss.GetComponentsInChildren<Light>(true).Where(l=>!l.transform.IsChildOf(dressing)&&!l.transform.IsChildOf(altar)))
            {
                string path=AnimationUtility.CalculateTransformPath(l.transform,boss);
                if(path.Contains("Six shrouded")){l.color=new Color(.45f,.58f,.65f);l.intensity=12;l.range=20;l.shadows=LightShadows.Soft;}
                else if(path.Contains("Bone reliquaries")){l.color=new Color(1,.52f,.21f);l.intensity=4;l.range=8.5f;l.shadows=LightShadows.None;}
                else if(path.Contains("Colossal rib")){l.color=new Color(.8f,.28f,.1f);l.intensity=3.4f;l.range=10;l.shadows=LightShadows.None;}
                else if(path.Contains("Boss altar") && l.color.r>l.color.b*1.5f){l.color=new Color(1,.52f,.24f);l.intensity=Mathf.Min(l.intensity,3.8f);}
            }
            foreach(var region in root.GetComponentsInChildren<TCRegionAtmosphere>(true).Where(r=>r.name.StartsWith("Boss")))
            {
                region.ambient=new Color(.105f,.115f,.12f);region.fogColor=new Color(.065f,.071f,.075f);region.fogDensity=.011f;
                var volume=region.GetComponent<Volume>();if(volume&&volume.sharedProfile){var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Ritual_chamber";
                    foreach(var original in volume.sharedProfile.components)profile.components.Add(Object.Instantiate(original));
                    if(profile.TryGet<ColorAdjustments>(out var color)){color.postExposure.Override(.25f);color.saturation.Override(-14);}
                    if(profile.TryGet<Bloom>(out var bloom)){bloom.intensity.Override(.16f);bloom.threshold.Override(1.15f);}
                    AssetDatabase.CreateAsset(profile,Art+"/Profiles/Ritual_chamber.asset");foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);volume.sharedProfile=profile;}
            }
            PrefabUtility.SaveAsPrefabAsset(root,Room);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Capture("FinalRitual");Debug.Log("BASEMENT_RITUAL_BUILD_OK");
    }
    public static void Capture(string prefix)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");ShaderUtil.allowAsyncCompilation=false;
        var root=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement"&&!t.parent);var boss=root.Find("BossRoom");
        float f=BasementFinalBuilder.Floor(boss,"Main_Floor"),z=boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
        var render=typeof(BasementSurfaceBuilder).GetMethod("Render",BindingFlags.NonPublic|BindingFlags.Static);
        render.Invoke(null,new object[]{root,boss,V(0,f+1.84f,z+2),V(-10,f+4.8f,z+32),prefix+"-Entrance",false});
        render.Invoke(null,new object[]{root,boss,V(-3.5f,f+1.84f,z+21),V(-12,f+6.5f,z+32),prefix+"-Altar",false});
        render.Invoke(null,new object[]{root,boss,V(-8,f+1.84f,z+30),V(-10,f+4,z+34),prefix+"-Flashlight",true});
        render.Invoke(null,new object[]{root,boss,V(-24,f+1.84f,z+8),V(-10,f+4,z+29),prefix+"-Side",false});
    }
    public static void FineTune()
    {
        BasementSurfaceBuilder.RequireIsolated();materials.Clear();combined.Clear();
        foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{Art+"/Materials"}))
        {var m=Load<Material>(AssetDatabase.GUIDToAssetPath(guid));materials[m.name]=m;}
        var iron=Simple("Soot_blackened_iron",new Color(.12f,.10f,.075f),Load<Material>("Assets/Art/Basement/ArenaPolish/Materials/Oxidized_structural_steel.mat"));
        iron.DisableKeyword("_EMISSION");iron.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(iron);
        var diffuser=Simple("Cold_inspection_diffuser",new Color(.32f,.42f,.45f));diffuser.SetColor("_EmissionColor",new Color(.55f,.78f,.85f)*1.5f);diffuser.EnableKeyword("_EMISSION");EditorUtility.SetDirty(diffuser);
        var root=PrefabUtility.LoadPrefabContents(Room);
        try
        {
            var boss=root.transform.Find("BossRoom");dressing=boss.Find("Ritual chamber dressing");var sanctum=boss.Find("BasementFinalSanctum");
            var holders=dressing.Cast<Transform>().Where(t=>t.name=="CC0 brass_candleholders"&&t.localPosition.y>4).ToArray();
            foreach(var holder in holders)
            {
                float side=Mathf.Sign(holder.localPosition.x+10);int index=Mathf.RoundToInt((holder.localPosition.z-6.8f)/10.6f);
                holder.position=sanctum.TransformPoint(V(side*17.4f,4.825f,7+index*10.6f));
            }
            foreach(var r in boss.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(r.sharedMaterials.Any(m=>m&&m.name=="Red"))r.sharedMaterials=r.sharedMaterials.Select(m=>m&&m.name=="Red"?iron:m).ToArray();
            }
            foreach(var l in sanctum.GetComponentsInChildren<Light>(true))
            {
                string path=AnimationUtility.CalculateTransformPath(l.transform,sanctum);
                if(path.Contains("Six shrouded")){l.color=new Color(.48f,.64f,.72f);l.intensity=10;l.range=18;l.shadows=LightShadows.None;}
                else if(path.Contains("Bone reliquaries")){l.color=new Color(1,.52f,.21f);l.intensity=4;l.range=8.5f;l.shadows=LightShadows.None;}
                else if(path.Contains("Colossal rib")){l.color=new Color(.8f,.28f,.1f);l.intensity=3.4f;l.range=10;l.shadows=LightShadows.None;}
            }
            for(int side=-1;side<=1;side+=2)
            {
                var p=V(-10+side*6.8f,10.85f,26.4f);
                Box(p,V(1.9f,.16f,.45f),iron);Box(p+V(0,-.1f,0),V(1.65f,.055f,.32f),diffuser);
                Light(dressing,"Surviving overhead inspection lamp",p+V(0,-.18f,0),new Color(.48f,.66f,.76f),21,13,V(-10+side*4,7,29),side<0);
            }
            FinishCombined();
            foreach(var c in boss.GetComponentsInChildren<Component>(true))
                if((c is Renderer||c is Light||c is UniversalAdditionalLightData)&&PrefabUtility.IsPartOfPrefabInstance(c))PrefabUtility.RecordPrefabInstancePropertyModifications(c);
            PrefabUtility.SaveAsPrefabAsset(root,Room);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Capture("PolishedRitual");Debug.Log("BASEMENT_RITUAL_TUNED");
    }
    public static void Verify()
    {
        BasementSurfaceBuilder.RequireIsolated();Capture("VerifiedRitual");
        var root=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement"&&!t.parent);var boss=root.Find("BossRoom");
        if(!boss.Find("Ritual chamber dressing"))throw new Exception("Missing ritual dressing");
        int seals=boss.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.sharedMaterials.Any(m=>m&&m.name=="Altar_fine_line_seal"));if(seals!=1)throw new Exception("Expected one fine-line seal");
        foreach(var renderer in boss.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(!renderer.enabled)continue;var filter=renderer.GetComponent<MeshFilter>();
            if(!renderer.GetComponent<TextMesh>()&&(!filter||!filter.sharedMesh))throw new Exception("Missing mesh: "+renderer.name);
            foreach(var m in renderer.sharedMaterials){if(!m||!m.shader||m.shader.name.Contains("Error"))throw new Exception("Invalid material "+renderer.name);
                foreach(var msg in ShaderUtil.GetShaderMessages(m.shader))if(msg.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(msg.message);}
        }
        var bodies=boss.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Six shrouded bodies suspended above the altar");
        if(bodies.GetComponentsInChildren<Renderer>(true).Count(r=>r.enabled)==0)throw new Exception("Hanging bodies hidden");
        string report="URP renders: entrance, altar, flashlight and side.\nFine-line seals: "+seals+"\nHanging-body renderers: "+bodies.GetComponentsInChildren<Renderer>(true).Count(r=>r.enabled)+"\nRitual dressing colliders: "+boss.Find("Ritual chamber dressing").GetComponentsInChildren<Collider>(true).Length+"\nRitual shader errors: 0\nBoss active lights: "+boss.GetComponentsInChildren<Light>().Count(l=>l.enabled)+"\nBoss shadowed lights: "+boss.GetComponentsInChildren<Light>().Count(l=>l.enabled&&l.shadows!=LightShadows.None)+"\n";
        File.WriteAllText("Preview/Ritual-validation.txt",report);Debug.Log("BASEMENT_RITUAL_VERIFIED\n"+report);
    }
    public static void RemoveUnsupportedCandleSets()
    {
        BasementSurfaceBuilder.RequireIsolated();var root=PrefabUtility.LoadPrefabContents(Room);
        try
        {
            var d=root.transform.Find("BossRoom/Ritual chamber dressing");
            foreach(var t in d.Cast<Transform>().Where(t=>t.name=="CC0 brass_candleholders"&&t.localPosition.y>4).ToArray())Object.DestroyImmediate(t.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root,Room);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();Debug.Log("BASEMENT_RITUAL_SUPPORTS_CORRECTED");
    }
}
