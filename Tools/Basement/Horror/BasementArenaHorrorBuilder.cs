using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object=UnityEngine.Object;

public static class BasementArenaHorrorBuilder
{
    public const string Art="Assets/Art/Basement/ArenaHorror";
    public const string Room="Assets/prefabs/Scenario/Basement.prefab";
    public const string Arena="Assets/Art/Basement/BasementFinalArena.prefab";
    [Serializable] class Geometry { public Vector3[] vertices,normals; public Vector2[] uv; public int[] triangles; }
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Geometry drape;
    public static void Build()
    {
        BasementSurfaceBuilder.RequireIsolated();
        foreach(string folder in new[]{"Materials","Meshes","Profiles"})Directory.CreateDirectory(Art+"/"+folder);
        AssetDatabase.Refresh();ImportLinen();materials.Clear();meshes.Clear();
        var root=PrefabUtility.LoadPrefabContents(Arena);
        try
        {
            if(root.transform.Find("Arena horror dressing"))throw new Exception("Arena dressing already installed; restore the arena snapshot before rebuilding");
            var floor=FindSurface(root,"Floor");var rust=FindSurface(root,"Rust");var iron=FindSurface(root,"Iron");
            if(!floor)floor=AssetDatabase.LoadAssetAtPath<GameObject>(Room).transform.Find("PreBossArea/Floor/Hall_Floor").GetComponent<MeshRenderer>().sharedMaterial;
            MakeMaterial("Mortuary linen",floor,new Color(.88f,.89f,.81f),true);
            MakeMaterial("Dirty winding cloth",floor,new Color(.64f,.66f,.59f),true);
            MakeMaterial("Discarded canvas",floor,new Color(.25f,.32f,.28f),true);
            MakeMaterial("Evidence sacks",floor,new Color(.052f,.065f,.055f),true);
            MakeMaterial("Rubber restraints",floor,new Color(.043f,.034f,.03f),false);
            MakeMaterial("Drain sludge",floor,new Color(.24f,.235f,.18f),false);
            MakeMaterial("Damp concrete",floor,new Color(.35f,.36f,.29f),false);
            MakeMaterial("Dried seepage",floor,new Color(.23f,.095f,.073f),false);
            MakeMaterial("Corroded tray",rust,new Color(.32f,.30f,.26f),false);
            SkinMaterial();
            materials["Structural iron"]=iron;materials["Rusted steel"]=rust;
            var station=root.transform.Find("Abandoned restraint station");
            foreach(var renderer in station.GetComponentsInChildren<MeshRenderer>(true))
                if(!renderer.name.StartsWith("Skill"))renderer.enabled=false;
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Arena_FoldedMortuarySheet" || r.name=="BasementFinalArena_8_Combined_FinalShroud"))renderer.enabled=false;
            Corpse(station);
            var dressing=Group(root.transform,"Arena horror dressing",Vector3.zero);
            Dress(dressing);CombineDressing(dressing);
            PrefabUtility.SaveAsPrefabAsset(root,Arena);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        root=PrefabUtility.LoadPrefabContents(Room);
        try
        {
            var arena=root.transform.Find("PreBossArea");
            var region=arena.GetComponentInChildren<TCRegionAtmosphere>();region.ambient=new Color(.105f,.12f,.13f);
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Art+"/Profiles/ArenaVisibility.asset");
            if(!profile)
            {
                profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Arena lower exposure";
                AssetDatabase.CreateAsset(profile,Art+"/Profiles/ArenaVisibility.asset");
            }
            Configure<ColorAdjustments>(profile,c=>{c.postExposure.Override(-.04f);c.contrast.Override(6);c.saturation.Override(-12);});
            Configure<Tonemapping>(profile,c=>c.mode.Override(TonemappingMode.Neutral));
            Configure<Vignette>(profile,c=>{c.intensity.Override(.12f);c.smoothness.Override(.65f);});
            Configure<FilmGrain>(profile,c=>{c.type.Override(FilmGrainLookup.Thin1);c.intensity.Override(.035f);});
            Configure<Bloom>(profile,c=>{c.intensity.Override(.05f);c.threshold.Override(1.2f);});
            Configure<ChromaticAberration>(profile,c=>c.intensity.Override(0));
            region.GetComponent<Volume>().sharedProfile=profile;
            foreach(var light in arena.GetComponentsInChildren<Light>(true))
            {
                if(PrefabUtility.IsPartOfPrefabInstance(light) || light.intensity<=0)continue;
                light.intensity*=.82f;
            }
            var selected=arena.GetComponentsInChildren<Light>().Where(l=>
                l.transform.parent.name=="Hanging industrial fixture -10" ||
                l.transform.parent.name=="Hanging industrial fixture 10" ||
                l.transform.parent.name=="Amber caged wall light (-12.25, 2.60, 10.00)").ToArray();
            if(selected.Length!=3)throw new Exception("Three intended defective fixtures were not found");
            for(int i=0;i<selected.Length;i++)
            {
                var flicker=selected[i].GetComponent<TCFlickerLight>();if(!flicker)flicker=selected[i].gameObject.AddComponent<TCFlickerLight>();
                flicker.enabled=true;flicker.flickerAmount=i==2 ? .28f : .45f;
                flicker.speed=2.6f+i*.8f;flicker.dropoutChancePerSecond=i==2 ? .06f : .12f;
                flicker.synchronizeEmission=true;
            }
            PrefabUtility.SaveAsPrefabAsset(root,Room);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();BakeArenaReflection();Capture();Verify();Debug.Log("BASEMENT_ARENA_HORROR_BUILD_OK");
    }
    static Material FindSurface(GameObject root,string name)=>root.GetComponentsInChildren<MeshRenderer>(true)
        .SelectMany(r=>r.sharedMaterials).FirstOrDefault(m=>m && SourceName(m)==name);
    static string SourceName(Material material)
    {
        string path=AssetDatabase.GetAssetPath(material);
        if(!path.StartsWith(BasementSurfaceBuilder.Art+"/Materials/"))return material.name;
        string guid=Path.GetFileNameWithoutExtension(path).Split('_')[0];
        var source=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));return source ? source.name : material.name;
    }
    static void Configure<T>(VolumeProfile profile,Action<T> configure) where T:VolumeComponent
    {
        if(!profile.TryGet<T>(out var component)){component=profile.Add<T>(true);AssetDatabase.AddObjectToAsset(component,profile);}
        configure(component);EditorUtility.SetDirty(component);EditorUtility.SetDirty(profile);
    }
    static void SkinMaterial()
    {
        string path=Art+"/Materials/Corpse_skin.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/SkinDiffuse.png"));
        material.SetTextureScale("_BaseMap",Vector2.one);material.SetTextureOffset("_BaseMap",Vector2.zero);
        material.SetColor("_BaseColor",new Color(.62f,.69f,.65f));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.08f);
        material.SetFloat("_Cull",0);material.enableInstancing=true;EditorUtility.SetDirty(material);materials["Corpse skin"]=material;
    }
    static Material MakeMaterial(string name,Material source,Color color,bool linen)
    {
        if(!source)throw new Exception("Source material missing for "+name);
        var material=new Material(source){name=name,enableInstancing=true};
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",name=="Damp concrete" ? .72f : .25f);
        material.SetFloat("_BumpScale",linen ? .38f : .22f);material.SetFloat("_OcclusionStrength",.65f);
        material.SetVector("_SpecColor",new Vector4(linen ? .65f : .4f,name=="Damp concrete" ? .48f : .02f,.42f,1.03f));
        material.SetTextureScale("_BaseMap",linen ? Vector2.one*1.67f : Vector2.one*.5f);
        if(linen)
        {
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/Diffuse.png"));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/nor_gl.png"));
            foreach(string property in new[]{"_MetallicGlossMap","_OcclusionMap","_DirtMap"})material.SetTexture(property,AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/UnityMask.png"));
            material.SetFloat("_Cull",0);
            material.SetTexture("_DirtMap",source.GetTexture("_OcclusionMap"));
        }
        material.DisableKeyword("_PARALLAXMAP");material.SetTexture("_DetailNormalMap",null);
        if(name=="Mortuary linen" || name=="Dirty winding cloth")
        {
            material.SetTexture("_DetailAlbedoMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/DirtDetail.png"));
            material.SetTextureScale("_DetailAlbedoMap",new Vector2(.35f,.45f));material.SetFloat("_DetailAlbedoMapScale",.18f);
            material.DisableKeyword("_DETAIL_MULX2");material.EnableKeyword("_DETAIL_SCALED");
        }
        string path=Art+"/Materials/"+name.Replace(' ','_')+".mat";var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(saved){EditorUtility.CopySerialized(material,saved);Object.DestroyImmediate(material);material=saved;}else AssetDatabase.CreateAsset(material,path);
        materials[name]=material;return material;
    }
    static Transform Group(Transform parent,string name,Vector3 position)
    {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;return go.transform;}
    static Mesh Save(string name,Mesh mesh)
    {
        if(meshes.TryGetValue(name,out var saved)){Object.DestroyImmediate(mesh);return saved;}
        mesh.name=name;string path=Art+"/Meshes/"+name+".asset";saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(saved){EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
        meshes[name]=saved;return saved;
    }
    static Mesh Read(string name)
    {
        var data=JsonUtility.FromJson<Geometry>(File.ReadAllText("Preview/ArenaHorror/Sources/"+name+".json"));
        if(name=="GravityDrapedMortuaryLinen")drape=data;
        var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.vertices=data.vertices;mesh.uv=data.uv;mesh.triangles=data.triangles;
        if(data.normals!=null && data.normals.Length==data.vertices.Length)mesh.normals=data.normals;else mesh.RecalculateNormals();
        mesh.RecalculateTangents();mesh.RecalculateBounds();return Save(name,mesh);
    }
    static MeshRenderer Shape(Transform parent,string name,Mesh mesh,Vector3 position,string material,Vector3? rotation=null,Vector3? scale=null)
    {
        var t=Group(parent,name,position);if(rotation.HasValue)t.localRotation=Quaternion.Euler(rotation.Value);
        if(scale.HasValue)t.localScale=scale.Value;
        t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=materials[material];return r;
    }
    static MeshRenderer Box(Transform p,string name,Vector3 position,Vector3 size,string mat,Vector3? rotation=null)
        =>Shape(p,name,Save(name,ChaosGeometry.BevelBox(size,.008f)),position,mat,rotation);
    static void Pipe(Transform p,string name,IReadOnlyList<Vector3> points,float radius,string material)
        =>Shape(p,name,Save(name,ChaosGeometry.Sweep(points,radius,12)),Vector3.zero,material);
    static float ClothHeight(float x,float z)
    {
        int iz=Mathf.Clamp(Mathf.RoundToInt((z+1.285f)/1.76f*152),0,152),ix=0;
        for(int i=1;i<=84;i++)if(Mathf.Abs(drape.vertices[iz*85+i].x-x)<Mathf.Abs(drape.vertices[iz*85+ix].x-x))ix=i;
        return drape.vertices[iz*85+ix].y;
    }
    static void Corpse(Transform station)
    {
        var t=Group(station,"Rebuilt mortuary body and restraint table",Vector3.zero);
        Box(t,"Rolled mortuary slab",V(0,.99f,0),V(1.10f,.095f,2.70f),"Rusted steel");
        Box(t,"Stained absorbent table liner",V(0,1.045f,0),V(1.055f,.018f,2.60f),"Dirty winding cloth");
        Shape(t,"Compressed cloth head pad",Save("CompressedHeadPad",ChaosGeometry.Ellipsoid(V(.39f,.10f,.37f),19)),V(.025f,1.097f,.765f),"Dirty winding cloth");
        foreach(int x in new[]{-1,1})foreach(int z in new[]{-1,1})
        {
            Box(t,"Gurney leg "+x+" "+z,V(x*.43f,.51f,z*1.09f),V(.072f,.94f,.072f),"Structural iron");
            var wheel=ChaosGeometry.Lathe(new[]{new Vector2(0,-.024f),new Vector2(.071f,-.024f),new Vector2(.085f,-.014f),new Vector2(.085f,.014f),new Vector2(.071f,.024f),new Vector2(0,.024f)},24);
            Shape(t,"Braked caster "+x+" "+z,Save("CasterWheel",wheel),V(x*.43f,.086f,z*1.09f),"Rubber restraints",V(0,0,90));
            Box(t,"Caster fork "+x+" "+z,V(x*.43f,.17f,z*1.09f),V(.042f,.105f,.115f),"Corroded tray");
        }
        foreach(int side in new[]{-1,1})Pipe(t,"Table side rail "+side,new[]{V(side*.48f,.7f,-1.1f),V(side*.48f,.7f,1.1f)},.022f,"Structural iron");
        var body=Group(t,"Adult corpse with draped linen",V(0,1.055f,0));
        var high=Group(body,"Near anatomical detail",Vector3.zero);var low=Group(body,"Distant anatomical silhouette",Vector3.zero);
        var hiBody=Shape(high,"Anatomical human head and hands",Read("AnatomicalAdultBody"),Vector3.zero,"Corpse skin");
        var hiSheet=Shape(high,"Continuous gravity draped sheet",Read("GravityDrapedMortuaryLinen"),Vector3.zero,"Mortuary linen");
        var loBody=Shape(low,"Adult corpse LOD",Read("AdultCorpseLOD"),Vector3.zero,"Corpse skin");
        var loSheet=Shape(low,"Draped sheet LOD",Read("DrapedLinenLOD"),Vector3.zero,"Mortuary linen");
        var lod=body.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.10f,new Renderer[]{hiBody,hiSheet}),new LOD(.015f,new Renderer[]{loBody,loSheet})});lod.RecalculateBounds();
        foreach(float z in new[]{.22f,-.35f,-.86f})
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int i=0;i<=40;i++)
            {
                float x=Mathf.Lerp(-.61f,.61f,i/40f);
                foreach(float dz in new[]{-.024f,.024f}){vertices.Add(V(x,ClothHeight(x,z+dz)+.014f,z+dz));uv.Add(new Vector2(i*.025f,dz));}
                if(i>0){int a=(i-1)*2;tris.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
            }
            var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();
            Shape(body,"Tensioned restraint "+z,Save("Restraint_"+z.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),mesh),Vector3.zero,"Rubber restraints");
        }
        Box(t,"Dropped evidence tray",V(.32f,.049f,1.67f),V(.55f,.065f,.32f),"Corroded tray",V(0,-18,0));
        for(int i=0;i<4;i++)Box(t,"Abandoned surgical instrument "+i,V(.15f+i*.095f,.086f,1.64f),V(.012f,.009f,.16f),"Rusted steel",V(0,i*22,0));
        var iv=Group(t,"Empty IV stand",V(.90f,0,.96f));
        Pipe(iv,"IV upright",new[]{V(0,.09f,0),V(0,2.3f,0)},.018f,"Corroded tray");
        for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Pipe(iv,"IV support "+i,new[]{V(0,.12f,0),V(Mathf.Sin(a)*.35f,.023f,Mathf.Cos(a)*.35f)},.022f,"Structural iron");}
        Pipe(iv,"Bent IV hook",ChaosGeometry.Curve(V(0,2.27f,0),V(.22f,2.42f,0),V(.35f,2.35f,0),V(.26f,2.23f,0)),.012f,"Rusted steel");
        Pipe(iv,"Abandoned gravity tube",ChaosGeometry.Curve(V(.27f,2.23f,0),V(.30f,1.4f,.1f),V(-.1f,.7f,.3f),V(-.18f,.065f,.32f)),.006f,"Rubber restraints");
        Patch(t,"Dried table runoff",V(.12f,.012f,.4f),V(.75f,0,1.05f),"Dried seepage",21);
        Pipe(t,"Pendant lamp suspension",new[]{V(0,3.34f,0),V(0,9.15f,0)},.007f,"Structural iron");
        var shade=ChaosGeometry.Lathe(new[]{new Vector2(.15f,-.025f),new Vector2(.16f,.01f),new Vector2(.10f,.105f),new Vector2(.035f,.13f)},32);
        Shape(t,"Aged mortuary task lamp shade",Save("MortuaryLampShade",shade),V(0,3.24f,0),"Rusted steel");
        var light=station.GetComponentInChildren<Light>();light.color=new Color(.78f,.84f,.82f);light.intensity=4.2f;
        light.shadowNormalBias=.035f;light.shadowBias=.02f;
        var emitter=light.GetComponentInChildren<MeshRenderer>();
        if(emitter)
        {
            string path=Art+"/Materials/MortuaryLamp.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(emitter.sharedMaterial);AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",new Color(.78f,.84f,.82f));material.SetColor("_EmissionColor",new Color(.78f,.84f,.82f)*.9f);EditorUtility.SetDirty(material);emitter.sharedMaterial=material;
        }
        CombineDressing(t,t.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.transform.IsChildOf(body)).ToArray());
    }
    static void Patch(Transform parent,string name,Vector3 position,Vector3 size,string material,int seed,Vector3? rotation=null)
    {
        var b=new ChaosGeometry.Builder();var random=new System.Random(seed);var points=new Vector3[18];
        for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/points.Length,r=.78f+(float)random.NextDouble()*.22f;points[i]=V(Mathf.Cos(a)*size.x*.5f*r,0,Mathf.Sin(a)*size.z*.5f*r);}
        for(int i=0;i<points.Length;i++)b.Face(Vector3.zero,points[(i+1)%points.Length],points[i]);
        Shape(parent,name,Save(name,b.Mesh(name)),position,material,rotation);
    }
    static void Dress(Transform root)
    {
        var cart=Group(root,"Overturned transport cart and abandoned linen",V(-4.7f,0,-8.3f));cart.localRotation=Quaternion.Euler(0,-17,-64);
        Box(cart,"Tipped transport bed",V(0,.62f,0),V(.70f,.07f,1.72f),"Corroded tray");
        Shape(cart,"Discarded folded cart linen",Save("CartLinen",ChaosGeometry.Cloth(.65f,1.3f,42)),V(0,.67f,0),"Discarded canvas",null,V(1,.18f,1));
        foreach(int side in new[]{-1,1})
        {
            Pipe(cart,"Transport safety rail "+side,new[]{V(side*.32f,.67f,-.72f),V(side*.32f,.87f,-.72f),V(side*.32f,.87f,.72f),V(side*.32f,.67f,.72f)},.018f,"Rusted steel");
            foreach(int end in new[]{-1,1})
            {
                Box(cart,"Transport leg "+side+" "+end,V(side*.27f,.34f,end*.68f),V(.045f,.55f,.045f),"Structural iron");
                Shape(cart,"Transport wheel "+side+" "+end,meshes["CasterWheel"],V(side*.27f,.084f,end*.68f),"Rubber restraints",V(0,0,90));
            }
        }
        float min=float.PositiveInfinity;
        foreach(var filter in cart.GetComponentsInChildren<MeshFilter>())foreach(var vertex in filter.sharedMesh.vertices)
            min=Mathf.Min(min,root.InverseTransformPoint(filter.transform.TransformPoint(vertex)).y);
        cart.localPosition+=Vector3.up*(.015f-min);
        foreach(int side in new[]{-1,1})
        {
            var pile=Group(root,"Collapsed masonry and maintenance waste "+side,V(side*18.25f,0,side<0 ? -8 : 13));
            for(int i=0;i<17;i++)
            {
                float x=(i%4)*.26f-.45f,z=(i/4)*.32f-.5f;
                Shape(pile,"Irregular masonry "+side+" "+i,Save("Debris_"+i,ChaosGeometry.Rubble(i+37)),V(x,.012f,z),"Damp concrete",V(0,i*37,0),V(.6f,.45f,.7f));
            }
            for(int i=0;i<3;i++)
            {
                var sack=Save("EvidenceSack",ChaosGeometry.Ellipsoid(V(.65f,.42f,.8f),17));
                Shape(pile,"Abandoned sealed sack "+side+" "+i,sack,V(-.3f+i*.38f,.22f,.8f+i*.10f),"Evidence sacks",V(0,i*41,0));
                Pipe(pile,"Gathered sack tie "+side+" "+i,new[]{V(-.3f+i*.38f,.34f,1.18f+i*.1f),V(-.3f+i*.38f,.35f,1.24f+i*.1f)},.019f,"Rubber restraints");
            }
            Patch(root,"Low wall rising damp "+side,V(side*19.72f,.95f,side<0 ? -7 : 12),V(1.5f,0,3.5f),"Drain sludge",side+43,V(0,0,side<0 ? -90 : 90));
            Patch(root,"Corner drainage residue "+side,V(side*18.7f,.009f,side<0 ? -7.1f : 12.3f),V(2,0,3.0f),"Drain sludge",side+57);
        }
        var station=Group(root,"Interrupted cleanup and contaminated tools",V(-17.1f,0,7.2f));
        var bucket=ChaosGeometry.Lathe(new[]{new Vector2(.15f,0),new Vector2(.19f,.37f),new Vector2(.20f,.40f),new Vector2(.18f,.40f),new Vector2(.17f,.05f),new Vector2(0,.05f)},32,.03f);
        Shape(station,"Oxidized cleanup pail",Save("CleanupPail",bucket),V(0,.012f,0),"Corroded tray");
        Pipe(station,"Pail handle",ChaosGeometry.Curve(V(-.18f,.30f,0),V(-.17f,.65f,0),V(.17f,.65f,0),V(.18f,.30f,0)),.009f,"Structural iron");
        Patch(station,"Localized cleanup spill",V(.3f,.014f,.5f),V(1.35f,0,1.55f),"Dried seepage",84);
        for(int i=0;i<5;i++)Patch(station,"Drag trace "+i,V(.45f+i*.14f,.016f,.7f+i*.37f),V(.06f,0,.42f),"Dried seepage",i+73);
        var bags=Group(root,"Unclaimed wrapped remains at the cold perimeter",V(17.35f,0,-10));
        for(int i=0;i<2;i++)
        {
            Shape(bags,"Heavy discarded wrapped bundle "+i,Save("WrappedBundle",ChaosGeometry.Cloth(.67f,1.8f,34)),V(i*.48f,.025f,i*1.35f),"Evidence sacks",V(0,-28+i*40,0));
            Patch(bags,"Bundle seepage "+i,V(i*.48f,.008f,i*1.35f),V(.95f,0,1.6f),"Drain sludge",i+92);
        }
        var curtain=Group(root,"Torn service isolation curtain",V(-18.9f,3.1f,13.1f));
        Pipe(curtain,"Wall anchored curtain rail",new[]{V(-.8f,.08f,-1.2f),V(.15f,.08f,-1.2f),V(.15f,.08f,1.2f)},.027f,"Rusted steel");
        Shape(curtain,"Collapsed hanging isolation fabric",Save("TornCurtain",ChaosGeometry.Cloth(2.2f,2.55f,41,true)),V(.16f,0,0),"Discarded canvas",V(0,90,0));
        Patch(root,"Pump leakage localized wet floor",V(18,.012f,7),V(2.45f,0,3.1f),"Damp concrete",112);
        for(int i=0;i<5;i++)Patch(root,"Low pipe runoff "+i,V(19.7f,1.55f,5.8f+i*.52f),V(.20f,0,2.4f),"Drain sludge",i+101,V(0,0,90));
        for(int i=0;i<20;i++)Patch(root,"Dirty cleanup footprint "+i,V(-14.7f+i*.72f,.010f,7.35f+(i%2==0 ? -.14f : .14f)),V(.13f,0,.27f),"Drain sludge",i+121,V(0,86+(i%3)*4,0));
    }
    static void CombineDressing(Transform root,MeshRenderer[] selection=null)
    {
        var renderers=selection ?? root.GetComponentsInChildren<MeshRenderer>();
        string prefix=root.name=="Arena horror dressing" ? "Dressing_" : "Workstation_";
        foreach(var group in renderers.GroupBy(r=>r.sharedMaterial))
        {
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray());
            mesh.RecalculateBounds();
            var go=Group(root,"Combined dressing "+group.Key.name,Vector3.zero);
            go.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(prefix+group.Key.name.Replace(' ','_'),mesh);
            go.gameObject.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
        }
        foreach(var renderer in renderers)Object.DestroyImmediate(renderer.gameObject);
    }
    static void ImportLinen()
    {
        foreach(string role in new[]{"Diffuse","nor_gl","arm"})
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"/Textures/"+role+".png");
            importer.textureType=role=="nor_gl" ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture=role=="Diffuse";importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            importer.anisoLevel=4;importer.maxTextureSize=1024;importer.isReadable=role=="arm";
            importer.textureCompression=role=="arm" ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        var arm=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/arm.png");var pixels=arm.GetPixels32();
        for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new Color32(p.b,p.r,p.g,(byte)(255-p.g));}
        var mask=new Texture2D(arm.width,arm.height,TextureFormat.RGBA32,false,true);mask.SetPixels32(pixels);mask.Apply();
        File.WriteAllBytes(Art+"/Textures/UnityMask.png",mask.EncodeToPNG());Object.DestroyImmediate(mask);AssetDatabase.ImportAsset(Art+"/Textures/UnityMask.png");
        var packed=(TextureImporter)AssetImporter.GetAtPath(Art+"/Textures/UnityMask.png");packed.sRGBTexture=false;packed.mipmapEnabled=true;
        packed.filterMode=FilterMode.Trilinear;packed.anisoLevel=4;packed.maxTextureSize=1024;packed.textureCompression=TextureImporterCompression.CompressedHQ;packed.SaveAndReimport();
        var source=(TextureImporter)AssetImporter.GetAtPath(Art+"/Textures/arm.png");source.isReadable=false;source.textureCompression=TextureImporterCompression.CompressedHQ;source.SaveAndReimport();
        var detail=(TextureImporter)AssetImporter.GetAtPath(Art+"/Textures/DirtDetail.png");detail.sRGBTexture=true;detail.mipmapEnabled=true;
        detail.filterMode=FilterMode.Trilinear;detail.anisoLevel=4;detail.maxTextureSize=1024;detail.textureCompression=TextureImporterCompression.CompressedHQ;detail.SaveAndReimport();
        var skin=(TextureImporter)AssetImporter.GetAtPath(Art+"/Textures/SkinDiffuse.png");skin.sRGBTexture=true;skin.mipmapEnabled=true;
        skin.filterMode=FilterMode.Trilinear;skin.anisoLevel=4;skin.maxTextureSize=2048;skin.textureCompression=TextureImporterCompression.CompressedHQ;skin.SaveAndReimport();
    }
    static void BakeArenaReflection()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");ShaderUtil.allowAsyncCompilation=false;
        var room=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement" && !t.parent);
        var area=room.Find("PreBossArea");var probe=area.GetComponentInChildren<ReflectionProbe>();
        var go=new GameObject("Arena reflection update");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.transform.position=probe.transform.position;
        camera.nearClipPlane=.1f;camera.farClipPlane=55;var cubemap=new Cubemap(128,TextureFormat.RGBAHalf,true);
        try
        {
            TCRegionAtmosphere.ApplyForCamera(camera,room.GetComponentsInChildren<TCRegionAtmosphere>(),false);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            if(!camera.RenderToCubemap(cubemap))throw new Exception("Arena cubemap failed");
            var saved=AssetDatabase.LoadAssetAtPath<Cubemap>(Art+"/ArenaReflection.asset");
            if(saved){EditorUtility.CopySerialized(cubemap,saved);Object.DestroyImmediate(cubemap);cubemap=saved;}else AssetDatabase.CreateAsset(cubemap,Art+"/ArenaReflection.asset");
            var root=PrefabUtility.LoadPrefabContents(Room);
            try{root.transform.Find("PreBossArea").GetComponentInChildren<ReflectionProbe>().customBakedTexture=cubemap;PrefabUtility.SaveAsPrefabAsset(root,Room);}
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        finally{TCRegionAtmosphere.RestoreEnvironment();Object.DestroyImmediate(go);}
        AssetDatabase.SaveAssets();
    }
    public static void Capture()
    {
        BasementSurfaceBuilder.Capture("Horror");
        var room=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement" && !t.parent);
        var station=room.Find("PreBossArea/BasementFinalArena/Abandoned restraint station");
        var area=room.Find("PreBossArea");var nested=area.Find("BasementFinalArena");
        var details=nested.Find("Arena horror dressing");
        File.WriteAllLines("Preview/BasementSurface/arena-dressing-bounds.txt",new[]{"Nested arena position="+area.InverseTransformPoint(nested.position)+" scale="+nested.lossyScale}
            .Concat(details.GetComponentsInChildren<MeshRenderer>().Select(r=>r.name+": enabled="+r.enabled+" area center="+area.InverseTransformPoint(r.bounds.center)+" world size="+r.bounds.size)));
        var render=typeof(BasementSurfaceBuilder).GetMethod("Render",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
        var lod=station.GetComponentInChildren<LODGroup>();lod.ForceLOD(0);
        try
        {
            render.Invoke(null,new object[]{room,station,V(2.45f,2.05f,1.6f),V(0,1.18f,-.08f),"Horror-Body",false});
            render.Invoke(null,new object[]{room,station,V(.32f,1.88f,1.28f),V(.025f,1.28f,.7f),"Horror-BodyClose",false});
        }
        finally{lod.ForceLOD(-1);}
    }
    public static void Verify()
    {
        var root=PrefabUtility.LoadPrefabContents(Room);
        try
        {
            var dressing=root.transform.Find("PreBossArea/BasementFinalArena/Arena horror dressing");
            if(!dressing || dressing.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Decorative dressing missing or blocks movement");
            var corpse=root.transform.Find("PreBossArea/BasementFinalArena/Abandoned restraint station/Rebuilt mortuary body and restraint table/Adult corpse with draped linen");
            if(!corpse || !corpse.GetComponent<LODGroup>())throw new Exception("Detailed body/LOD missing");
            var flickers=root.GetComponentsInChildren<TCFlickerLight>(true).Where(f=>f.enabled).ToArray();
            if(flickers.Length!=3 || flickers.Any(f=>!f.transform.IsChildOf(root.transform.Find("PreBossArea")) || !f.synchronizeEmission))throw new Exception("Arena flicker configuration invalid");
            if(dressing.GetComponentsInChildren<MeshRenderer>().Length>12)throw new Exception("Decoration not consolidated");
            Debug.Log("BASEMENT_ARENA_HORROR_VERIFIED body LOD, three independent faulty lamps, static combined dressing, unchanged solid collision");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void FinalCheck(){Verify();BasementSurfaceBuilder.FinalCheck();Capture();Debug.Log("BASEMENT_ARENA_HORROR_FINAL_OK");}
}
