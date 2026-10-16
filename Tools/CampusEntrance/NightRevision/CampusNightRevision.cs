using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TotalChaosMaps;
using Object=UnityEngine.Object;

// One-off revision: compile only in the isolated project's _TemporaryTools/Editor.
public static class CampusNightRevision
{
    const string Garden="Assets/_Project/Scenes/EntranceGarden";
    const string Hall="Assets/_Project/Scenes/EntranceHallway";
    const string Global="Assets/_Project/Global";
    static string owner;
    static Material stone, plaster, iron, grass, glass, broken, pale;
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Color C(float r,float g,float b)=>new Color(r,g,b);
    public static void Build()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/EntranceRebuild/"))throw new Exception("Isolated workspace required");
        Directory.CreateDirectory("NightEvidence");
        AssetDatabase.Refresh();
        Materials();
        foreach(var name in new[]{"EntranceGarden","EntranceHallway"})
        {
            owner=name=="EntranceGarden"?Garden:Hall;
            var path=owner+"/Prefabs/"+name+".prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var art=root.transform.Find("CampusArchitecture");
                if(art.Find("Night revision"))throw new Exception("Revision already applied; restore isolated baseline before rebuilding");
                var anchor=art.GetComponent<CampusEntranceAnchor>();anchor.heightMultiplier=1.3f;anchor.Refresh();
                foreach(var t in art.GetComponentsInChildren<Transform>())
                {
                    if(t.name=="Garden bench")t.localScale*=1.2f;
                    if(t.name=="Ornamental potted plant")t.localScale*=1.12f;
                    if(t.name=="Ornamental fern border")t.localScale*=1.12f;
                }
                var revision=Group(art,"Night revision",Vector3.zero);
                if(name=="EntranceGarden")Exterior(art,revision);else Interior(root.transform,art,revision);
                ClearGlazing(art);
                Atmosphere(art,name=="EntranceGarden");
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
        CampusNightValidation.Run();
        Debug.Log("CAMPUS_NIGHT_BUILD_PASS");
    }
    public static void Polish()
    {
        Materials();
        foreach(var name in new[]{"EntranceGarden","EntranceHallway"})
        {
            owner=name=="EntranceGarden"?Garden:Hall;
            var path=owner+"/Prefabs/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var art=root.transform.Find("CampusArchitecture");
                if(name=="EntranceGarden")GateLighting(art.Find("Night revision"));
                else CourtyardMoonlight(art.Find("Night revision/Lawn visible through corridor windows"));
                ClearGlazing(art);
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();CampusNightValidation.Run();Debug.Log("CAMPUS_NIGHT_BUILD_PASS");
    }
    static Material Load(string path)=>AssetDatabase.LoadAssetAtPath<Material>(path);
    static Material Save(Material material,string directory)
    {
        string path=directory+"/Materials/CampusNight/"+material.name+".mat";Directory.CreateDirectory(Path.GetDirectoryName(path));
        var old=Load(path);if(old){EditorUtility.CopySerialized(material,old);Object.DestroyImmediate(material);return old;}
        AssetDatabase.CreateAsset(material,path);return material;
    }
    static Material Copy(string name,string path,Color color,string directory)
    {
        var source=Load(path);if(!source)throw new Exception("Missing source material "+path);
        var m=new Material(source){name=name};m.SetColor("_BaseColor",color);return Save(m,directory);
    }
    static void Materials()
    {
        stone=Load(Global+"/Materials/CampusReference/Damp concrete edging.mat");
        plaster=Load(Global+"/Materials/CampusReference/Weathered campus ochre.mat");
        iron=Load(Global+"/Materials/CampusReference/Scuffed bronze frames.mat");
        glass=Copy("Night clear dusty glazing",Global+"/Materials/CampusReference/Dusty entrance glazing.mat",new Color(.25f,.29f,.29f,.065f),Global);
        broken=Copy("Localized fractured concrete","Assets/_Project/Scenes/Library/Materials/DecayRevision/DamagedConcrete.mat",C(.61f,.59f,.50f),Global);
        pale=Copy("Aged exposed plaster chips",Global+"/Materials/CampusReference/Rain stained ivory plaster.mat",C(.57f,.56f,.46f),Global);
        grass=Copy("Unmaintained night lawn",Global+"/Materials/CampusReference/Weathered campus ochre.mat",C(.56f,.64f,.43f),Global);
        string folder=Global+"/Textures/CampusNight/grass_ground/";
        foreach(var role in new[]{"Diffuse","nor_gl","arm"})
        {
            string path=folder+role+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=role=="nor_gl"?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=role=="Diffuse";importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.anisoLevel=4;importer.filterMode=FilterMode.Trilinear;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
        }
        grass.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Diffuse.png"));
        grass.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"nor_gl.png"));grass.SetFloat("_BumpScale",.55f);
        var raw=new Texture2D(2,2,TextureFormat.RGBA32,false,true);raw.LoadImage(File.ReadAllBytes(folder+"arm.png"));var pixels=raw.GetPixels32();
        for(int i=0;i<pixels.Length;i++){var p=pixels[i];pixels[i]=new Color32(p.b,p.r,p.g,(byte)(255-p.g));}
        raw.SetPixels32(pixels);raw.Apply();string packed=folder+"PackedSurface.png";File.WriteAllBytes(packed,raw.EncodeToPNG());Object.DestroyImmediate(raw);
        AssetDatabase.ImportAsset(packed);var ti=(TextureImporter)AssetImporter.GetAtPath(packed);ti.sRGBTexture=false;ti.maxTextureSize=2048;ti.SaveAndReimport();
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(packed);grass.SetTexture("_MetallicGlossMap",texture);grass.SetTexture("_OcclusionMap",texture);grass.SetTexture("_DirtMap",texture);
        grass.SetFloat("_Smoothness",.12f);grass.SetFloat("_OcclusionStrength",.35f);grass.SetTextureScale("_BaseMap",Vector2.one);
        grass.EnableKeyword("_NORMALMAP");grass.EnableKeyword("_METALLICSPECGLOSSMAP");grass.EnableKeyword("_OCCLUSIONMAP");grass.EnableKeyword("_TC_SHARED_MASK");
        // The requested PNG is background scenery beyond the playable windows. Calibrate
        // the photographic backdrop independently so a dark interior does not erase it.
        grass.shader=Shader.Find("Universal Render Pipeline/Unlit");grass.shaderKeywords=Array.Empty<string>();
        grass.SetColor("_BaseColor",C(.55f,.68f,.50f));grass.SetFloat("_Cull",2);
        EditorUtility.SetDirty(grass);
    }
    static void ClearGlazing(Transform art)
    {
        foreach(var renderer in art.GetComponentsInChildren<Renderer>(true))
        {
            if(!renderer.sharedMaterials.Any(m=>m&&(m.name=="Dusty entrance glazing"||m.name=="Night clear dusty glazing")))continue;
            renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m&&(m.name=="Dusty entrance glazing"||m.name=="Night clear dusty glazing")?glass:m).ToArray();
            if(PrefabUtility.IsPartOfPrefabInstance(renderer))PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        }
    }
    static Transform Group(Transform parent,string name,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collider=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;
        go.GetComponent<Renderer>().sharedMaterial=material;if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
    static void DisableFinish(Transform root,string material)
    {foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())if(renderer.sharedMaterial&&renderer.sharedMaterial.name==material){renderer.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}}
    static Mesh SaveMesh(Mesh mesh,string name)
    {
        string path=owner+"/Meshes/CampusNight/"+name+".asset";Directory.CreateDirectory(Path.GetDirectoryName(path));mesh.name=name;
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void Batch(Transform root,string name)
    {
        var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>!PrefabUtility.IsPartOfPrefabInstance(r)).ToArray();
        foreach(var group in renderers.GroupBy(r=>r.sharedMaterial))
        {
            var combines=group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines,true,true);mesh.RecalculateBounds();
            var t=Group(root,group.Key.name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,name+"_"+group.Key.name);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
        }
        foreach(var r in renderers)
        {
            var go=r.gameObject;Object.DestroyImmediate(go.GetComponent<MeshFilter>());Object.DestroyImmediate(r);
            if(go.GetComponents<Component>().Length==1&&go.transform.childCount==0)Object.DestroyImmediate(go);
        }
    }
    static void Ground(Transform parent,string name,Vector3 center,float width,float depth)
    {
        var vertices=new[]{V(-width/2,0,-depth/2),V(-width/2,0,depth/2),V(width/2,0,depth/2),V(width/2,0,-depth/2)};
        var mesh=new Mesh();mesh.vertices=vertices;mesh.uv=vertices.Select(v=>new Vector2(v.x,v.z)/2.51f).ToArray();mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateBounds();
        var t=Group(parent,name,center);t.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,name);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=grass;
    }
    static void Exterior(Transform art,Transform revision)
    {
        var originalGate=art.Find("02 External gate");
        DisableFinish(originalGate,"Scuffed bronze frames");DisableFinish(originalGate,"Weathered campus ochre");
        foreach(var leaf in originalGate.Cast<Transform>().Where(t=>t.name=="Fixed open gate leaf"))leaf.gameObject.SetActive(false);
        foreach(var collider in art.GetComponentsInChildren<Collider>())
            if(collider.name=="Boundary low wall"||collider.name=="Outer garden boundary")collider.enabled=false;
        DisableFinish(art.Find("01 Exterior gardens"),"Weathered campus ochre");
        var walls=Group(revision,"Raised perimeter walls",Vector3.zero);
        foreach(int side in new[]{-1,1})
        {
            Box(walls,"Perimeter wall",V(side*18,1.25f,-20),V(.30f,2.5f,40),plaster,true);
            Box(walls,"Weathered wall coping",V(side*18,2.53f,-20),V(.38f,.08f,40),stone);
            Box(walls,"Front boundary wall",V(side*10.7f,1.25f,-39.5f),V(13.8f,2.5f,.32f),plaster,true);
            Box(walls,"Gate pier",V(side*3.4f,1.6f,-39.5f),V(.65f,3.2f,.7f),plaster,true);
            Ground(revision,"Outer lawn "+side,V(side*21,-.055f,-18),5.7f,43);
        }
        Batch(walls,"RaisedWalls");
        var gate=Group(revision,"Closed external gate",V(0,0,-39.5f));
        foreach(int side in new[]{-1,1})
        {
            var leaf=Group(gate,"Closed gate leaf",V(side*3.1f,0,0));
            for(int i=0;i<=16;i++)Box(leaf,"Vertical gate bar",V(-side*i*3.1f/16,1.3f,0),V(.036f,2.6f,.065f),iron);
            foreach(float y in new[]{.10f,1.05f,2.53f})Box(leaf,"Gate rail",V(-side*1.55f,y,0),V(3.1f,.065f,.10f),iron);
            var collider=leaf.gameObject.AddComponent<BoxCollider>();collider.center=V(-side*1.55f,1.3f,0);collider.size=V(3.1f,2.6f,.13f);
        }
        Box(gate,"Gate lock housing",V(0,1.1f,-.07f),V(.17f,.24f,.10f),iron);
        Batch(gate,"ClosedGate");
        var damage=Group(revision,"Weathered facade repairs",Vector3.zero);
        var panels=art.Find("03 UniDoom facade").Cast<Transform>().Where(t=>t.name=="White geometric cobogo").ToArray();
        // Two missing concrete modules, with rubble directly below the missing apertures.
        foreach(var panel in panels.Where((t,i)=>i==3||i==30))
        {
            var p=panel.localPosition;panel.gameObject.SetActive(false);
            Scatter(damage,V(p.x,0,-.65f),7,Mathf.RoundToInt(p.x*13));
            Box(damage,"Exposed chipped screen jamb",V(p.x+.51f,p.y,-.26f),V(.13f,.85f,.09f),broken);
        }
        foreach(int side in new[]{-1,1})
        {
            Box(damage,"Damp wall base",V(side*17.83f,.27f,-28),V(.035f,.53f,4.2f),broken);
            Box(damage,"Chipped gate pier corner",V(side*3.05f,.36f,-39.88f),V(.15f,.71f,.06f),broken);
        }
        Batch(damage,"FacadeWear");
        foreach(var light in art.GetComponentsInChildren<Light>())
        {
            if(light.type==LightType.Point){light.intensity=4.2f;light.range=8.5f;light.color=C(.78f,.76f,.54f);}
            else if(light.type==LightType.Spot){light.intensity=16;light.range=7;}
        }
        GateLighting(revision);
    }
    static void GateLighting(Transform revision)
    {
        if(revision.Find("Gate practical lighting"))return;
        var group=Group(revision,"Gate practical lighting",V(0,0,-39.5f));
        var brown=Load(Global+"/Materials/CampusReference/Aged brown fascia.mat");
        var glow=Load(Global+"/Materials/CampusReference/Aged opal lamp glass.mat");
        Box(group,"Opaque backing behind external gate sign",V(0,2.85f,-.64f),V(3.3f,.48f,.10f),brown);
        foreach(int side in new[]{-1,1})
        {
            Box(group,"Gate canopy light casing",V(side*2.55f,3.10f,.28f),V(.33f,.09f,.25f),iron);
            Box(group,"Gate canopy light diffuser",V(side*2.55f,3.04f,.28f),V(.26f,.025f,.18f),glow);
            var t=Group(group,"Gate canopy practical",V(side*2.55f,2.97f,.35f));var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;
            l.color=C(.84f,.76f,.54f);l.intensity=5.4f;l.range=7;l.shadows=LightShadows.None;
        }
        Batch(group,"GatePracticalLighting");
    }
    static void CourtyardMoonlight(Transform landscape)
    {
        if(landscape.Find("Diffuse courtyard moonlight"))return;
        foreach(int side in new[]{-1,1})
        {
            var t=Group(landscape,"Diffuse courtyard moonlight",V(side*15,10.2f,14.5f));t.localRotation=Quaternion.Euler(90,0,0);
            var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=C(.46f,.58f,.75f);l.intensity=110;l.range=34;l.spotAngle=92;l.innerSpotAngle=54;l.shadows=LightShadows.None;
        }
    }
    static void Scatter(Transform parent,Vector3 center,int count,int seed)
    {
        var random=new System.Random(Mathf.Abs(seed));
        for(int i=0;i<count;i++)
        {
            float size=.10f+(float)random.NextDouble()*.17f;
            var go=Box(parent,"Detached concrete fragment",center+V(((float)random.NextDouble()-.5f)*1.1f,size*.22f,((float)random.NextDouble()-.5f)*.8f),V(size,size*.43f,size*.76f),i%2==0?broken:pale);
            go.transform.localRotation=Quaternion.Euler(0,(float)random.NextDouble()*180,0);
        }
    }
    static void Interior(Transform root,Transform art,Transform revision)
    {
        var roof=root.Find("Roof");if(roof)foreach(var collider in roof.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        var ceiling=revision.gameObject.AddComponent<BoxCollider>();ceiling.center=V(0,3.44f,14.35f);ceiling.size=V(7.66f,.12f,28.7f);
        var landscape=Group(revision,"Lawn visible through corridor windows",Vector3.zero);
        var palm=AssetDatabase.LoadAssetAtPath<GameObject>(Garden+"/Prefabs/CampusReference/CampusPalm.prefab");
        foreach(int side in new[]{-1,1})
        {
            Ground(landscape,"Window lawn "+side,V(side*17.0f,-.035f,14.5f),26,35);
            foreach(float z in new[]{6f,18f,27f})
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(palm,landscape);instance.name="Distant courtyard palm";
                instance.transform.localPosition=V(side*(9.2f+z*.08f),0,z);instance.transform.localRotation=Quaternion.Euler(0,z*19+side*47,0);instance.transform.localScale=Vector3.one*.78f;
                foreach(var collider in instance.GetComponentsInChildren<Collider>())collider.enabled=false;
            }
        }
        var damage=Group(revision,"Localized corridor damage",Vector3.zero);
        var bays=art.GetComponentsInChildren<Transform>().Where(t=>t.name=="Glazed side bay").ToArray();
        foreach(int index in new[]{4,19})
        {
            var bay=bays[index];DisableFinish(bay,"Dusty entrance glazing");
            var brokenGlass=Group(bay,"Fractured pane",V(0,0,-.025f));
            var mesh=new Mesh();
            mesh.vertices=new[]{V(-1.08f,.46f,0),V(-1.08f,2.95f,0),V(-.12f,2.05f,0),V(.30f,1.44f,0),V(1.08f,.46f,0),V(1.08f,2.95f,0),V(.48f,2.22f,0)};
            mesh.uv=mesh.vertices.Select(v=>new Vector2((v.x+1.1f)/2.2f,(v.y-.45f)/2.5f)).ToArray();
            mesh.triangles=new[]{0,1,2,0,2,3,0,3,4,4,6,5};mesh.RecalculateNormals();mesh.RecalculateBounds();
            brokenGlass.gameObject.AddComponent<MeshFilter>().sharedMesh=SaveMesh(mesh,"BrokenWindow_"+index);brokenGlass.gameObject.AddComponent<MeshRenderer>().sharedMaterial=glass;
            for(int i=0;i<3;i++)
            {
                var crack=Box(brokenGlass,"Glass fracture edge",V(-.16f+i*.33f,1.48f+i*.26f,-.007f),V(.012f,.43f,.008f),pale);
                crack.transform.localRotation=Quaternion.Euler(0,0,29+i*28);
            }
            Batch(brokenGlass,"WindowCracks_"+index);
            var center=art.InverseTransformPoint(bay.position);
            Scatter(damage,V(Mathf.Sign(center.x)*3.28f,0,center.z),5,index*17);
        }
        foreach(int side in new[]{-1,1})foreach(float z in new[]{8.4f,20.4f})
            Box(damage,"Exposed worn column corner",V(side*3.355f,.39f,z),V(.035f,.70f,.16f),broken);
        Batch(damage,"CorridorWear");
        foreach(var light in art.GetComponentsInChildren<Light>()){light.intensity=7.2f;light.range=8.6f;light.color=C(.68f,.78f,.70f);}
        CourtyardMoonlight(landscape);
    }
    static void Atmosphere(Transform art,bool exterior)
    {
        var region=art.GetComponentInChildren<TCRegionAtmosphere>();region.theme=exterior?"night_abandoned_campus":"night_campus_reception";
        region.ambient=exterior?C(.09f,.12f,.17f):C(.11f,.14f,.125f);region.fogColor=C(.008f,.014f,.025f);region.fogDensity=exterior?.014f:.008f;
        region.directionalLightMultiplier=exterior?.008f:0;region.flashlightIntensity=2.4f;region.flashlightRange=13;
        var volume=region.GetComponent<Volume>();var profile=Object.Instantiate(volume.sharedProfile);profile.name="CampusNight";
        // Clone the overrides as persistent subassets; the previous profile stays untouched.
        profile.components=volume.sharedProfile.components.Select(c=>Object.Instantiate(c)).ToList();
        string path=owner+"/Profiles/CampusNight.asset";AssetDatabase.CreateAsset(profile,path);
        foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
        if(profile.TryGet<ColorAdjustments>(out var color)){color.postExposure.value=.12f;color.saturation.value=-22;}
        volume.sharedProfile=profile;EditorUtility.SetDirty(profile);
    }
}
