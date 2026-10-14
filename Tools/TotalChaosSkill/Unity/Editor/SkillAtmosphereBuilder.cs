using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using TotalChaosMaps.EditorTools;
using Object=UnityEngine.Object;

public static class SkillAtmosphereBuilder
{
    public const string Art="Assets/Art/SkillAtmosphere";
    public static readonly string[] Targets=TotalChaosStyleBuilder.Targets;
    static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static string Room(string path)=>path.Contains("ClassRoom")?"ClassRoom":path.Contains("Cylinder")?"Cylinder":Path.GetFileNameWithoutExtension(path);
    public static void Build()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/"))throw new Exception("Generate in an isolated project");
        materials.Clear();Directory.CreateDirectory(Art+"/Materials");Directory.CreateDirectory(Art+"/Profiles");Directory.CreateDirectory(Art+"/Meshes");AssetDatabase.Refresh();
        foreach(var path in Directory.GetFiles("Assets/Art/TotalChaosInspired/Textures","*.png"))
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            importer.filterMode=FilterMode.Point;importer.maxTextureSize=128;importer.anisoLevel=1;importer.mipmapEnabled=true;importer.SaveAndReimport();
        }
        foreach(string path in Targets)
        {
            string baseline=Path.GetFullPath(Path.Combine(Application.dataPath,"../../SkillAtmosphere-before",path));
            File.Copy(baseline,path,true);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            string room=Room(path);string theme=room.Contains("Basement")?"facility":"apartments";var preset=TCTheme.Get(theme);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(r)||r.GetComponent<TextMesh>())continue;
                    r.sharedMaterials=r.sharedMaterials.Select(m=>Surface(m,room,preset)).ToArray();
                }
                Lighting(root.transform,room,preset);
                if(room=="ClassRoom")
                {
                    Region(root.transform,"Classroom skill atmosphere",new Bounds(V(3,1.9f,-3),V(18,3.8f,16)),preset);
                    Note(root.transform.Find("University apocalypse details/Last university lecture and warning"),"note_last_lecture");
                }
                else if(room=="Cylinder")
                {
                    var atrium=root.transform.Find("Abandoned circular atrium");
                    ReadableRadio(atrium.Find("Timeworn atrium stories/Abandoned emergency radio and campus map"));
                    Region(atrium,"Atrium skill atmosphere",new Bounds(V(0,4.3f,0),V(57,8.6f,41)),preset,true);
                    Note(atrium.Find("Timeworn atrium stories/Abandoned emergency radio and campus map"),"note_radio_silence");
                    Note(atrium.Find("Timeworn atrium stories/Faded warning beside the rear access"),"note_inside_the_walls");
                }
                else if(room=="Basement")
                {
                    var arena=root.transform.Find("PreBossArea");var boss=root.transform.Find("BossRoom");
                    float af=BasementFinalBuilder.Floor(arena,"Hall_Floor"),bf=BasementFinalBuilder.Floor(boss,"Main_Floor"),front=boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
                    Region(arena,"Arena skill atmosphere",new Bounds(V(10,af+4.75f,2),V(40,9.5f,34)),preset);
                    Region(boss,"Boss skill atmosphere",new Bounds(V(-10,bf+6,front+18),V(42,12,36)),preset);
                    var corridor=boss.Find("Player sized corridor");
                    foreach(var floor in corridor.GetComponentsInChildren<BoxCollider>().Where(c=>c.name.StartsWith("Passage Floor")))
                    {
                        var b=floor.bounds;var c=boss.InverseTransformPoint(b.center);
                        var s=V(b.size.x/boss.lossyScale.x,5.2f,b.size.z/boss.lossyScale.z);
                        var region=Region(boss,"Corridor skill atmosphere "+floor.name,new Bounds(V(c.x,bf+2.6f,c.z),s),preset);
                        region.priority=11;
                    }
                }
                else if(room=="BasementFinalArena")Note(root.transform.Find("Final sector evacuation graffiti"),"note_failed_evacuation");
                else Note(root.transform.Find("Last warning at the threshold"),"note_last_threshold_warning");
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();SkillAtmosphereVerifier.Verify();Debug.Log("SKILL_ATMOSPHERE_BUILD_OK");
    }
    static Color Hex(string text){ColorUtility.TryParseHtmlString(text,out var color);return color;}
    static Material Surface(Material source,string room,TCTheme theme)
    {
        if(!source||!source.HasProperty("_BaseColor")||source.name.Contains("Sky"))return source;
        string key=room+"/"+source.name;if(materials.TryGetValue(key,out var mat))return mat;
        Directory.CreateDirectory(Art+"/Materials/"+room);AssetDatabase.Refresh();
        var created=new Material(source){name=source.name};var color=source.GetColor("_BaseColor");
        string name=source.name.ToLowerInvariant();
        if(name.Contains("blood"))color=Hex("#5a1414");
        else if(name.Contains("floor"))color=theme.floor;
        else if(name.Contains("ceiling")||name=="roof")color=theme.ceiling;
        else if(name.Contains("plaster")||name.Contains("concrete")||name.Contains("brick")||name.Contains("stone"))color=theme.wall;
        else if(name.Contains("lettering"))color*=.65f;
        else color=Color.Lerp(color,Color.gray*color.grayscale,.25f);
        created.SetColor("_BaseColor",color);
        if(created.HasProperty("_Smoothness"))created.SetFloat("_Smoothness",name=="wet"?.8f:.10f);
        if(source.GetTexture("_BaseMap")&&AssetDatabase.GetAssetPath(source.GetTexture("_BaseMap")).StartsWith("Assets/Art/TotalChaosInspired"))
        {
            created.SetTextureScale("_BaseMap",Vector2.one*(1.4f/3));
            if(created.HasProperty("_BumpMap"))created.SetTextureScale("_BumpMap",Vector2.one*(1.4f/3));
            if(created.HasProperty("_MetallicGlossMap"))created.SetTextureScale("_MetallicGlossMap",Vector2.one*(1.4f/3));
        }
        if(source.IsKeywordEnabled("_EMISSION"))
        {
            var old=source.GetColor("_EmissionColor");bool red=old.r>old.g*1.4f;
            created.SetColor("_EmissionColor",red?Hex("#5a1414")*.6f:theme.lamp*.22f);
        }
        string path=Art+"/Materials/"+key+".mat";mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(mat){EditorUtility.CopySerialized(created,mat);Object.DestroyImmediate(created);}else{AssetDatabase.CreateAsset(created,path);mat=created;}
        materials[key]=mat;return mat;
    }
    static void Lighting(Transform root,string room,TCTheme preset)
    {
        var candidates=new List<Light>();
        foreach(var light in root.GetComponentsInChildren<Light>())
        {
            if(PrefabUtility.IsPartOfPrefabInstance(light))continue;
            string name=light.name,parent=light.transform.parent.name;
            bool window=name.Contains("window");bool dead=light.intensity<=0;
            bool fill=name.Contains("bounce")||name.Contains("reflection")||(room=="Cylinder"&&parent=="Abandoned circular atrium")||parent=="Dim warm arena readability"||parent=="BasementArenaDetails"||parent=="BasementBossDetails";
            bool red=light.color.r>light.color.g*1.5f;
            if(fill||dead){light.intensity=0;light.shadows=LightShadows.None;continue;}
            light.color=red?Hex("#a38a72"):window?Hex("#6b8fa3"):preset.lamp;
            light.intensity=window?.45f:Mathf.Min(preset.lightIntensity,light.intensity);
            light.range=window?4.2f:Mathf.Min(preset.lightRange,light.range);
            if(red){light.intensity=1.35f;light.range=Mathf.Min(light.range,4.8f);}
            light.shadows=LightShadows.None;
            if(room=="Cylinder"&&parent=="Kiosk bay"){light.intensity=2.4f;light.range=5.2f;}
            if(name=="Warm desk lamp"||parent.Contains("Abandoned restraint station")||parent.Contains("Six shrouded bodies"))light.shadows=LightShadows.Soft;
            if(!window&&!light.GetComponentInParent<OpenDoorWithKey>())candidates.Add(light);
            if(!window)VisibleSource(light,room,preset);
        }
        int count=Mathf.RoundToInt(candidates.Count*.4f);
        var ordered=candidates.OrderBy(l=>Hierarchy(l.transform)).ToArray();
        for(int i=0;i<count;i++)
        {
            int index=i*ordered.Length/Mathf.Max(1,count);var light=ordered[index];
            var flicker=light.gameObject.AddComponent<TCFlickerLight>();flicker.flickerAmount=.45f;flicker.speed=7+i%3;flicker.dropoutChancePerSecond=.15f;
        }
        Debug.Log("SKILL_LIGHTS_OK "+room+": "+count+" / "+candidates.Count+" artificial lamps flicker; indirect fills disabled");
    }
    static string Hierarchy(Transform t){string name=t.name;while(t.parent){t=t.parent;name=t.name+"/"+name;}return name;}
    static void VisibleSource(Light light,string room,TCTheme preset)
    {
        // A small physical diffuser marks each surviving artificial source. It has no collider.
        var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Skill visible lamp diffuser";
        Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(light.transform,false);go.transform.localScale=V(.09f,.055f,.09f);
        string key=room+"/SkillLamp";
        if(!materials.TryGetValue(key,out var mat))
        {
            var created=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="SkillLamp"};created.SetColor("_BaseColor",preset.lamp);
            created.EnableKeyword("_EMISSION");created.SetColor("_EmissionColor",preset.lamp*.22f);
            string folder=Art+"/Materials/"+room;Directory.CreateDirectory(folder);AssetDatabase.Refresh();string path=folder+"/SkillLamp.mat";
            mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat){EditorUtility.CopySerialized(created,mat);Object.DestroyImmediate(created);}else{AssetDatabase.CreateAsset(created,path);mat=created;}materials[key]=mat;
        }
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        if(light.transform.parent.name=="Abandoned emergency radio and campus map")
        {
            var iron=materials["Cylinder/Iron"];
            var shade=GameObject.CreatePrimitive(PrimitiveType.Cylinder);shade.name="Skill hanging radio lamp shade";Object.DestroyImmediate(shade.GetComponent<Collider>());
            shade.transform.SetParent(light.transform,false);shade.transform.localPosition=V(0,.045f,0);shade.transform.localScale=V(.18f,.035f,.18f);shade.GetComponent<MeshRenderer>().sharedMaterial=iron;
            var cable=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cable.name="Skill radio lamp suspension";Object.DestroyImmediate(cable.GetComponent<Collider>());
            cable.transform.SetParent(light.transform,false);cable.transform.localPosition=V(0,.84f,0);cable.transform.localScale=V(.014f,.80f,.014f);cable.GetComponent<MeshRenderer>().sharedMaterial=iron;
        }
    }
    static void ReadableRadio(Transform radio)
    {
        foreach(var filter in radio.GetComponentsInChildren<MeshFilter>())
        {
            if(filter.name.StartsWith("Skill"))continue;
            var mesh=Object.Instantiate(filter.sharedMesh);var vertices=mesh.vertices;bool changed=false;
            for(int i=0;i<vertices.Length;i++)if(vertices[i].y>1.9f){vertices[i].z-=.20f;changed=true;}
            if(!changed){Object.DestroyImmediate(mesh);continue;}
            mesh.vertices=vertices;mesh.RecalculateBounds();string path=Art+"/Meshes/Cylinder_Radio_"+filter.name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,path);existing=mesh;}
            filter.sharedMesh=existing;
        }
    }
    static VolumeProfile Profile(TCTheme t)
    {
        string path=Art+"/Profiles/"+t.id+".asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(profile)return profile;
        profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name=t.id;
        var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.42f);vignette.smoothness.Override(.6f);vignette.color.Override(Color.black);
        var grain=profile.Add<FilmGrain>(true);grain.type.Override(FilmGrainLookup.Thin2);grain.intensity.Override(.55f);grain.response.Override(.8f);
        var grading=profile.Add<ColorAdjustments>(true);grading.saturation.Override(t.saturation);grading.contrast.Override(18);grading.postExposure.Override(t.exposure);
        var bloom=profile.Add<Bloom>(true);bloom.threshold.Override(1);bloom.intensity.Override(.25f);bloom.scatter.Override(.7f);
        var aberration=profile.Add<ChromaticAberration>(true);aberration.intensity.Override(.08f);
        AssetDatabase.CreateAsset(profile,path);foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);return profile;
    }
    static TCRegionAtmosphere Region(Transform parent,string name,Bounds bounds,TCTheme t,bool oval=false)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);
        var region=go.AddComponent<TCRegionAtmosphere>();region.localBounds=bounds;region.oval=oval;region.theme=t.id;region.fogColor=t.fog;region.fogDensity=t.fogDensity;region.ambient=t.ambient;
        var volume=go.AddComponent<Volume>();volume.isGlobal=false;volume.priority=40;volume.blendDistance=0;volume.sharedProfile=Profile(t);
        if(oval)
        {
            var shape=GameObject.CreatePrimitive(PrimitiveType.Cylinder);var mesh=shape.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(shape);var child=new GameObject("Oval atmosphere boundary");child.transform.SetParent(go.transform,false);
            child.transform.localPosition=bounds.center;child.transform.localScale=V(bounds.size.x,bounds.size.y/2,bounds.size.z);
            var collider=child.AddComponent<MeshCollider>();collider.sharedMesh=mesh;collider.convex=true;collider.isTrigger=true;
            // Volume expects its colliders on the same object; the region uses metre coordinates independently.
            volume.enabled=false;var childVolume=child.AddComponent<Volume>();childVolume.isGlobal=false;childVolume.priority=40;childVolume.blendDistance=0;childVolume.sharedProfile=volume.sharedProfile;
        }
        else{var collider=go.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size;collider.isTrigger=true;}
        return region;
    }
    static void Note(Transform t,string label)
    {if(!t)throw new Exception("Existing narrative anchor missing: "+label);var marker=t.gameObject.AddComponent<TCMapMarker>();marker.type=TCMarkerType.ItemSpawn;marker.label=label;}
}
