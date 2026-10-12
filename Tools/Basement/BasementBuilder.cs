// Run in an isolated Unity project. install_basement.py preserves original YAML blocks.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;
using Math = System.Math;

public static class BasementBuilder
{
    const string Art = "Assets/Art/Basement";
    const string RoomPath = "Assets/prefabs/Scenario/Basement.prefab";
    static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
    static Transform interior;
    static int meshIndex;
    static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);

    public static void Build()
    {
        if(File.Exists("Preview/verify-passages.flag"))
        {
            Type.GetType("BasementPassageBuilder, Assembly-CSharp-Editor").GetMethod("Verify").Invoke(null,null);
            return;
        }
        if(File.Exists("Preview/verify-only.flag")){VerifyAndRender();return;}
        foreach (string folder in new[] {"Textures", "Materials", "Meshes"}) Directory.CreateDirectory(Art+"/"+folder);
        AssetDatabase.Refresh();
        Surface("Brick",new Color(.31f,.28f,.23f),"brick");
        Surface("Stone",new Color(.32f,.34f,.32f),"stone");
        Surface("Floor",new Color(.30f,.31f,.29f),"floor");
        Surface("Ceiling",new Color(.22f,.23f,.21f),"stone");
        Surface("Rust",new Color(.35f,.19f,.10f),"rust",.35f);
        Surface("Iron",new Color(.13f,.15f,.145f),"rust",.45f);
        Surface("Steel",new Color(.34f,.38f,.36f),"rust",.5f);
        Surface("Bone",new Color(.60f,.53f,.36f),"stone");
        Surface("Blood",new Color(.24f,.026f,.018f),"noise");
        Surface("Hazard",Color.white,"hazard");
        Surface("Red",new Color(.7f,.035f,.014f),"noise",0,new Color(2.5f,.04f,.012f));
        Surface("Amber",new Color(1,.49f,.12f),"noise",0,new Color(2.3f,.88f,.2f));
        Surface("Screen",Color.white,"screen",0,new Color(.08f,.6f,.19f));
        Surface("Seal",Color.white,"seal",0,new Color(.75f,.015f,.008f));
        Surface("Status",Color.white,"noise",0,new Color(2.5f,.04f,.012f));
        Mats["Status"].SetColor("_BaseColor",new Color(.7f,.035f,.014f));
        var room=PrefabUtility.LoadPrefabContents(RoomPath);
        if(room.transform.Find("PreBossArea/BasementArenaDetails"))throw new Exception("Basement already decorated");
        var arena=room.transform.Find("PreBossArea");var boss=room.transform.Find("BossRoom");
        // Bake missing ProBuilder mesh references without changing transforms or collision shapes.
        foreach(var pb in room.GetComponentsInChildren<ProBuilderMesh>())
        {
            pb.ToMesh();pb.Refresh();
            var mesh=Object.Instantiate(pb.GetComponent<MeshFilter>().sharedMesh);
            mesh.name="Original "+pb.name;AssetDatabase.CreateAsset(mesh,Art+"/Meshes/Shell_"+(meshIndex++)+".asset");
            pb.GetComponent<MeshFilter>().sharedMesh=mesh;
            var mc=pb.GetComponent<MeshCollider>();if(mc)mc.sharedMesh=mesh;
        }
        foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>())
        {
            string name=renderer.name.ToLowerInvariant();
            renderer.sharedMaterial=Mats[name.Contains("ceiling")?"Ceiling":name.Contains("floor")?"Floor":name.Contains("plataform")?"Iron":name.Contains("pillar")?"Stone":"Brick"];
        }
        interior=Root(arena,"BasementArenaDetails"); Arena();
        var encounter=interior.gameObject.AddComponent<BasementArenaEncounter>();encounter.arena=arena;
        encounter.enemyPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Enemy/Enemy.prefab");
        var spawnRoot=Group("Arena enemy spawn points",Vector3.zero);
        var spawnPoints=new List<Transform>();
        foreach(var p in new[] {V(-9,1.25f,-8),V(9,1.25f,-7),V(-3,1.25f,-5),V(3,1.25f,-3),V(-9,1.25f,2),V(9,1.25f,-1),V(-3,1.25f,5),V(3,1.25f,7),V(-9,1.25f,10),V(9,1.25f,11),V(-2,1.25f,12),V(2,1.25f,11)})
        {var marker=new GameObject("Enemy spawn "+(spawnPoints.Count+1)).transform;marker.SetParent(spawnRoot,false);marker.localPosition=p;spawnPoints.Add(marker);}
        encounter.spawnPoints=spawnPoints.ToArray(); Gate(arena,encounter);
        interior=Root(boss,"BasementBossDetails"); Boss(); Corridor(boss);
        PrefabUtility.SaveAsPrefabAsset(room,RoomPath);PrefabUtility.UnloadPrefabContents(room);
        AssetDatabase.SaveAssets();VerifyAndRender();
        Debug.Log("BASEMENT_BUILD_OK");
    }

    static Transform Root(Transform parent,string name)
    {var root=new GameObject(name).transform;root.SetParent(parent,false);return root;}
    static Transform Group(string name,Vector3 p,float yaw=0)
    {var t=Root(interior,name);t.localPosition=p;t.localRotation=Quaternion.Euler(0,yaw,0);return t;}
    static GameObject Shape(Transform p,string name,Vector3 pos,Vector3 size,string mat,PrimitiveType type,Vector3? rot=null)
    {
        var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(p,false);
        go.transform.localPosition=pos;go.transform.localScale=size;
        if(rot.HasValue)go.transform.localRotation=Quaternion.Euler(rot.Value);
        Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=Mats[mat];return go;
    }
    static GameObject Box(Transform p,string n,Vector3 v,Vector3 s,string m,Vector3? r=null)=>Shape(p,n,v,s,m,PrimitiveType.Cube,r);
    static GameObject Cylinder(Transform p,string n,Vector3 v,Vector3 s,string m,Vector3? r=null)=>Shape(p,n,v,s,m,PrimitiveType.Cylinder,r);
    static void Collision(Transform p,Vector3 c,Vector3 s)
    {var col=p.gameObject.AddComponent<BoxCollider>();col.center=c;col.size=s;}
    static void Light(Transform p,Vector3 pos,Color color,float power,float range)
    {var t=Root(p,"Local light");t.localPosition=pos;var light=t.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=power;light.range=range;light.shadows=LightShadows.None;t.gameObject.AddComponent<UniversalAdditionalLightData>();}

    static void Arena()
    {
        for(int side=-1;side<=1;side+=2)
        {
            var trim=Group("Arena wall trim "+side,V(side*12.69f,0,0));
            Box(trim,"Stone foundation",V(0,.58f,0),V(.12f,.66f,29.4f),"Stone");
            Box(trim,"Rust cornice",V(0,3.62f,0),V(.17f,.20f,29.4f),"Rust");
            for(int i=0;i<7;i++)
            {
                Box(trim,"Iron wall rib",V(-side*.06f,2,-12+i*4),V(.16f,3.35f,.22f),"Iron");
                Box(trim,"Rib foot",V(-side*.10f,.43f,-12+i*4),V(.24f,.25f,.38f),"Steel");
            }
            Combine(trim);
            Pipe("Steam feed "+side,V(side*12.28f,3.12f,0),29,"Rust",true);
            for(int z=-10;z<=10;z+=10) Sconce(V(side*12.25f,2.6f,z),side>0?90:-90,false);
            Boiler(V(side*11.5f,.25f,side>0?-10:9),side>0?-90:90);
            Cabinet(V(side*11.9f,.25f,side>0?10:-10),side>0?-90:90);
            for(int i=0;i<2;i++) Barrel(V(side*(10.5f+i*.9f),.25f,side>0?-4:4));
        }
        foreach(float x in new[]{-6.5f,6.5f})foreach(float z in new[]{-9f,0f,9f})
        {
            var p=Group("Pillar iron collar "+x+" "+z,V(x,0,z));
            foreach(float y in new[]{.48f,1.05f,3.53f})Box(p,"Reinforcement",V(0,y,0),V(.78f,.12f,.78f),"Iron");Combine(p);
        }
        for(int z=-10;z<=10;z+=10)
        {
            var beam=Group("Ceiling joist "+z,V(0,3.63f,z));
            Box(beam,"Steel beam",Vector3.zero,V(25.5f,.22f,.20f),"Iron");Combine(beam);
            var light=Group("Hanging industrial fixture "+z,V(0,3.45f,z));
            Box(light,"Housing",Vector3.zero,V(1.9f,.14f,.65f),"Iron");
            Box(light,"Warm diffuser",V(0,-.09f,0),V(1.65f,.03f,.45f),"Amber");
            for(int i=-4;i<=4;i++)Box(light,"Cage strip",V(i*.20f,-.12f,0),V(.035f,.05f,.5f),"Iron");Combine(light);
            Light(light,V(0,-.4f,0),new Color(1,.66f,.34f),4.5f,15);
        }
        for(int x=-1;x<=1;x+=2)
        {
            var drain=Group("Arena drainage "+x,V(x*4,.264f,0));
            Box(drain,"Drain channel",Vector3.zero,V(.38f,.025f,28),"Iron");
            for(int z=0;z<140;z++)Box(drain,"Drain crossbar",V(0,.023f,-13.8f+z*.2f),V(.37f,.025f,.045f),"Steel");Combine(drain);
        }
        Sign("Arena warning",V(-2.35f,2.05f,14.69f),180,"SECTOR 09", "PURGE LOCK");
        var cables=Group("Overhead cable trays",V(0,3.48f,0));
        for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)Cylinder(cables,"Cable",V(side*8+i*.12f,0,0),V(.075f,14.5f,.075f),"Iron",V(90,0,0));Combine(cables);
    }

    static void Pipe(string name,Vector3 pos,float length,string mat,bool alongZ)
    {
        var p=Group(name,pos);Cylinder(p,"Pipe",Vector3.zero,V(.23f,length/2,.23f),mat,alongZ?V(90,0,0):V(0,0,90));
        for(float f=-length/2+.5f;f<length/2;f+=2.5f)
        {Cylinder(p,"Flange",alongZ?V(0,0,f):V(f,0,0),V(.34f,.045f,.34f),"Iron",alongZ?V(90,0,0):V(0,0,90));}
        Combine(p);
    }
    static void Sconce(Vector3 pos,float yaw,bool red)
    {
        var p=Group((red?"Red":"Amber")+" caged wall light "+pos,pos,yaw);
        Box(p,"Backplate",V(0,0,.04f),V(.48f,.7f,.12f),"Iron");
        Cylinder(p,"Light tube",V(0,0,-.15f),V(.22f,.23f,.22f),red?"Red":"Amber");
        for(int i=0;i<4;i++){float a=i*Mathf.PI/2;Box(p,"Cage upright",V(Mathf.Cos(a)*.15f,0,-.15f+Mathf.Sin(a)*.15f),V(.03f,.55f,.03f),"Iron");}
        foreach(float y in new[]{-.25f,.25f})Cylinder(p,"Cage rim",V(0,y,-.15f),V(.35f,.026f,.35f),"Iron");
        Combine(p);Light(p,V(0,0,-.45f),red?new Color(1,.07f,.025f):new Color(1,.6f,.25f),red?3:2.2f,7);
    }
    static void Boiler(Vector3 pos,float yaw)
    {
        var p=Group("Riveted pressure boiler "+pos,pos,yaw);
        Cylinder(p,"Tank",V(0,1.28f,0),V(1.35f,1.13f,1.35f),"Rust");
        foreach(float y in new[]{.25f,.55f,2.03f,2.37f})Cylinder(p,"Steel hoop",V(0,y,0),V(1.43f,.055f,1.43f),"Iron");
        Cylinder(p,"Chimney",V(0,2.75f,0),V(.4f,.4f,.4f),"Iron");
        Cylinder(p,"Pressure gauge rim",V(.27f,1.88f,-.69f),V(.28f,.035f,.28f),"Steel",V(90,0,0));
        Cylinder(p,"Pressure gauge face",V(.27f,1.88f,-.73f),V(.22f,.01f,.22f),"Bone",V(90,0,0));
        Box(p,"Gauge needle",V(.27f,1.88f,-.75f),V(.015f,.16f,.01f),"Blood",V(0,0,-30));
        Cylinder(p,"Furnace port",V(0,.75f,-.65f),V(.67f,.07f,.67f),"Iron",V(90,0,0));
        Cylinder(p,"Furnace glow",V(0,.75f,-.73f),V(.52f,.02f,.52f),"Red",V(90,0,0));
        for(int i=-2;i<=2;i++)Box(p,"Port bars",V(i*.095f,.75f,-.76f),V(.035f,.52f,.035f),"Iron");
        for(int i=0;i<12;i++){float a=i*Mathf.PI/6;foreach(float y in new[]{.55f,2.03f})Shape(p,"Rivet",V(Mathf.Sin(a)*.72f,y,Mathf.Cos(a)*.72f),V(.065f,.065f,.065f),"Steel",PrimitiveType.Sphere);}
        Collision(p,V(0,1.35f,0),V(1.44f,2.7f,1.44f));Combine(p);Light(p,V(0,.8f,-1),new Color(1,.13f,.02f),1.8f,4);
    }
    static void Cabinet(Vector3 pos,float yaw)
    {
        var p=Group("Electrical control cabinet "+pos,pos,yaw);
        Box(p,"Housing",V(0,1.05f,0),V(1.1f,2.1f,.65f),"Steel");
        Box(p,"Recessed front",V(0,1.15f,-.345f),V(.94f,1.65f,.04f),"Iron");
        Box(p,"CRT bezel",V(0,1.57f,-.38f),V(.74f,.5f,.06f),"Rust");
        Box(p,"Terminal",V(0,1.57f,-.416f),V(.61f,.35f,.016f),"Screen");
        for(int i=0;i<4;i++){Box(p,"Breaker",V(-.3f+i*.2f,.78f,-.40f),V(.1f,.2f,.05f),"Bone");Box(p,"Toggle",V(-.3f+i*.2f,.82f,-.45f),V(.025f,.1f,.06f),"Iron");}
        Box(p,"Warning stripe",V(0,.26f,-.34f),V(1,.16f,.025f),"Hazard");
        Collision(p,V(0,1.05f,0),V(1.1f,2.1f,.65f));Combine(p);
    }
    static void Barrel(Vector3 pos)
    {
        var p=Group("Storage drum "+pos,pos);
        Cylinder(p,"Drum",V(0,.52f,0),V(.67f,.50f,.67f),"Rust");
        foreach(float y in new[]{.04f,.28f,.78f,1.02f})Cylinder(p,"Band",V(0,y,0),V(.71f,.022f,.71f),"Iron");
        Cylinder(p,"Cap",V(.14f,1.035f,.10f),V(.1f,.01f,.1f),"Steel");
        Collision(p,V(0,.53f,0),V(.71f,1.06f,.71f));Combine(p);
    }

    static void Gate(Transform arena,BasementArenaEncounter encounter)
    {
        var frame=Group("Combat bulkhead",V(0,.25f,15));
        var layout=interior.gameObject.AddComponent<BasementGateLayout>();layout.arena=arena;layout.gateFrame=frame;
        layout.leftWall=arena.Find("Walls/Exit_Wall_Left").GetComponent<BoxCollider>();
        layout.rightWall=arena.Find("Walls/Exit_Wall_Right").GetComponent<BoxCollider>();
        layout.header=arena.Find("Walls/Exit_Wall_Header").GetComponent<BoxCollider>();
        layout.floor=arena.Find("Floor/Hall_Floor").GetComponent<BoxCollider>();layout.Fit();
        var fixedParts=Root(frame,"Fixed frame");
        foreach(float x in new[]{-.48f,.48f})Box(fixedParts,"Armored jamb",V(x,.5f,-.10f),V(.04f,1,.28f),"Iron");
        Box(fixedParts,"Motor housing",V(0,1.04f,-.09f),V(1.16f,.12f,.39f),"Rust");
        Box(fixedParts,"Header stripes",V(0,1.03f,-.3f),V(1.05f,.075f,.025f),"Hazard");Combine(fixedParts);
        var leaf=Root(frame,"Sliding gate leaf");
        Box(leaf,"Solid steel shutter",V(0,.50f,0),V(.92f,1,.20f),"Iron");
        for(int i=0;i<7;i++)Box(leaf,"Rusted armor plate",V(0,.08f+i*.14f,-.13f),V(.87f,.125f,.07f),"Steel");
        Box(leaf,"Central locking rail",V(0,.5f,-.18f),V(.055f,.98f,.035f),"Rust");
        foreach(float x in new[]{-.35f,.35f})for(int i=0;i<7;i++)Shape(leaf,"Bolt",V(x,.08f+i*.14f,-.178f),V(.025f,.025f,.022f),"Iron",PrimitiveType.Sphere);
        Box(leaf,"Warning band",V(0,.47f,-.185f),V(.86f,.09f,.024f),"Hazard");
        Collision(leaf,V(0,.5f,0),V(.96f,1,.24f));Combine(leaf);encounter.gateLeaf=leaf;
        var lamp=Box(frame,"Combat lock status",V(.40f,1.04f,-.315f),V(.08f,.04f,.035f),"Status");encounter.statusLamp=lamp.GetComponent<Renderer>();
    }

    static void Boss()
    {
        var trim=Group("Boss sanctum masonry",Vector3.zero);
        foreach(float side in new[]{-1f,1f})
        {
            Box(trim,"Stone dado",V(side*8.70f,.62f,0),V(.13f,.74f,17.4f),"Stone");
            Box(trim,"Iron cornice",V(side*8.67f,4.62f,0),V(.18f,.21f,17.4f),"Iron");
            for(int i=-1;i<=1;i++)
            {Box(trim,"Buttress",V(side*8.57f,2.5f,i*5.8f),V(.32f,4.4f,.60f),"Stone");Box(trim,"Inset red slit",V(side*8.38f,2.5f,i*5.8f),V(.04f,1.9f,.075f),"Red");}
        }
        Box(trim,"Back stone dado",V(0,.62f,8.69f),V(17.4f,.74f,.13f),"Stone");Combine(trim);
        foreach(float side in new[]{-1f,1f})for(int z=-5;z<=5;z+=5)Sconce(V(side*8.32f,2.7f,z),side>0?90:-90,true);
        foreach(float x in new[]{-4.988306f,4.988306f})foreach(float z in new[]{-2.88f,2.88f})
        {
            var p=Group("Boss column reinforcement "+x+" "+z,V(x,0,z));
            foreach(float y in new[]{.48f,1.3f,4.57f})Box(p,"Collar",V(0,y,0),V(.96f,.18f,.96f),"Iron");
            Box(p,"Vertical red seal",V(0,2.85f,-.43f),V(.07f,1.6f,.035f),"Red");Combine(p);
        }
        var altar=Group("Boss altar and infernal engine",V(0,1.2f,6.5f));
        Box(altar,"Altar plinth",V(0,.12f,0),V(3.3f,.24f,1.6f),"Stone");
        Box(altar,"Carved altar",V(0,.70f,.22f),V(2.7f,1.1f,1),"Stone");
        Box(altar,"Top slab",V(0,1.3f,.22f),V(3.05f,.15f,1.3f),"Iron");
        for(int i=-2;i<=2;i++)Box(altar,"Red altar grooves",V(i*.43f,.70f,-.30f),V(.045f,.65f,.03f),"Red");
        for(int side=-1;side<=1;side+=2)
        {Cylinder(altar,"Engine tower",V(side*2.15f,.95f,0),V(.68f,.95f,.68f),"Iron");foreach(float y in new[]{.20f,.7f,1.2f,1.7f})Cylinder(altar,"Power coil",V(side*2.15f,y,0),V(.83f,.08f,.83f),"Rust");Cylinder(altar,"Hot core",V(side*2.15f,2.13f,0),V(.42f,.2f,.42f),"Red");}
        Collision(altar,V(0,.75f,.2f),V(3.1f,1.5f,1.4f));Combine(altar);
        var seal=Group("Infernal seal wall",V(0,3.2f,8.66f));
        Box(seal,"Seal backing",Vector3.zero,V(3.2f,2.7f,.09f),"Iron");
        Box(seal,"Pixel carved sigil",V(0,0,-.06f),V(2.7f,2.35f,.025f),"Seal");
        foreach(float side in new[]{-1f,1f}){Box(seal,"Stone jamb",V(side*1.7f,0,0),V(.24f,3.1f,.27f),"Stone");}
        Box(seal,"Lintel",V(0,1.5f,0),V(3.65f,.24f,.27f),"Stone");Combine(seal);
        Light(seal,V(0,-.4f,-1.2f),new Color(1,.055f,.018f),6,13);
        var circle=Group("Boss ritual floor",V(0,.273f,.5f));
        for(int i=0;i<48;i++)
        {float a=i*Mathf.PI*2/48;Box(circle,"Outer iron ring",V(Mathf.Sin(a)*3.2f,0,Mathf.Cos(a)*3.2f),V(.11f,.024f,.43f),"Rust",V(0,i*7.5f,0));if(i%3==0)Box(circle,"Blood rune",V(Mathf.Sin(a)*2.85f,.004f,Mathf.Cos(a)*2.85f),V(.09f,.012f,.3f),"Blood",V(0,i*7.5f,0));}
        Combine(circle);
        var spawn=Group("FinalBossSpawn",V(0,1.25f,.5f));
        // This marker deliberately does not instantiate a replacement for the game's final boss.
        for(int side=-1;side<=1;side+=2)
        {Pipe("Boss overhead pipe "+side,V(side*7.7f,4.35f,0),17,"Rust",true);Cabinet(V(side*7.5f,.25f,6.8f),180);}
        Light(interior,V(0,4.2f,-2),new Color(.60f,.65f,.72f),4.2f,20);
        Sign("Final chamber",V(2.8f,2.9f,-8.40f),0,"RESTRICTED", "CORE / 666");
    }

    static void Corridor(Transform boss)
    {
        // Parent fixtures to the original ceiling segments so scene position/scale overrides follow.
        foreach(string path in new[]{"Walls/Hallway_Ceiling","Floor/Hallway_Ceiling"})
        {
            var ceiling=boss.Find(path);var p=Root(ceiling,"Basement corridor fixtures");
            for(int i=-1;i<=1;i++)
            {
                Box(p,"Recessed lamp",V(0,-.57f,i*.28f),V(.52f,.12f,.055f),"Iron");
                Box(p,"Emergency diffuser",V(0,-.645f,i*.28f),V(.43f,.035f,.041f),"Red");
                var l=Root(p,"Emergency spill");l.localPosition=V(0,-1.1f,i*.28f);
                var light=l.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.11f,.035f);light.intensity=2.5f;light.range=5;l.gameObject.AddComponent<UniversalAdditionalLightData>();
            }
            foreach(float x in new[]{-.32f,.32f})Box(p,"Cable conduit",V(x,-.62f,0),V(.028f,.055f,.94f),"Rust");
            Combine(p);
        }
    }
    static void Sign(string name,Vector3 pos,float yaw,string line1,string line2)
    {
        var p=Group(name,pos,yaw);Box(p,"Riveted sign",Vector3.zero,V(1.65f,.65f,.045f),"Iron");
        Box(p,"Hazard border",V(0,-.26f,-.032f),V(1.48f,.075f,.018f),"Hazard");Combine(p);
        foreach(var entry in new[]{(line1,.13f),(line2,-.09f)})
        {var go=Root(p,"Lettering "+entry.Item1);go.localPosition=V(0,entry.Item2,-.037f);go.localRotation=Quaternion.Euler(0,180,0);var text=go.gameObject.AddComponent<TextMesh>();text.text=entry.Item1;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=48;text.characterSize=.052f;text.color=new Color(.72f,.65f,.46f);}
    }

    public static void VerifyAndRender()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(RoomPath);
        var room=(GameObject)PrefabUtility.InstantiatePrefab(asset);room.transform.position=Vector3.zero;
        Validate(room);
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.16f,.15f,.13f);RenderSettings.fog=false;
        Render(room.transform.Find("PreBossArea"),V(2,1.85f,-12.8f),V(-2,1.5f,5),"Arena");
        Render(room.transform.Find("PreBossArea"),V(3,1.7f,9.5f),V(0,1.7f,15),"Gate");
        Render(room.transform.Find("BossRoom"),V(0,1.85f,-7.2f),V(0,2,6.5f),"BossRoom");
        EditorSceneManager.SaveScene(scene,Art+"/BasementPreview.unity");
        TestEncounter(room);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var map=Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name=="Basement"&&t.parent==null);
        Validate(map.gameObject);
        foreach(var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            var local=map.Find("PreBossArea").InverseTransformPoint(r.bounds.center);
            if(Mathf.Abs(local.x)<14&&Mathf.Abs(local.z)<17&&r.sharedMaterial)
            {
                var m=r.sharedMaterial;var c=m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.white;
                if(c.g>c.r*1.5f)Debug.Log("BASEMENT_GREEN_OBJECT "+r.name+" parent="+r.transform.parent+" local="+local+" material="+m.name);
            }
        }
        Render(map.Find("PreBossArea"),V(2,1.85f,-12.8f),V(-2,1.5f,5),"ArenaInMap");
        Render(map.Find("BossRoom"),V(0,1.85f,-7.2f),V(0,2,6.5f),"BossRoomInMap");
        Debug.Log("BASEMENT_MAP_VERIFIED");
    }
    static void Validate(GameObject room)
    {
        foreach(var r in room.GetComponentsInChildren<MeshRenderer>())
        {
            foreach(var m in r.sharedMaterials)if(!m||!m.shader||m.shader.name=="Hidden/InternalErrorShader")throw new Exception("Missing material: "+r.name);
            var f=r.GetComponent<MeshFilter>();if(f&&!f.sharedMesh)throw new Exception("Missing mesh: "+r.name);
        }
        var layout=room.GetComponentInChildren<BasementGateLayout>();layout.Fit();
        var gate=room.GetComponentInChildren<BasementArenaEncounter>();
        if(gate.spawnPoints.Length!=12||!gate.enemyPrefab||!gate.gateLeaf.GetComponent<BoxCollider>())throw new Exception("Encounter not configured");
        foreach(var c in room.transform.Find("PreBossArea/BasementArenaDetails").GetComponentsInChildren<BoxCollider>())
        {
            if(c.transform==gate.gateLeaf)continue;
            var p=gate.arena.InverseTransformPoint(c.bounds.center);
            if(Mathf.Abs(p.x)<2&&Mathf.Abs(p.z)>12)throw new Exception("Arena approach blocked by "+c.name);
        }
        Debug.Log("BASEMENT_ASSETS_VERIFIED renderers="+room.GetComponentsInChildren<Renderer>().Length+" gateHeight="+layout.gateFrame.localScale.y);
    }
    static void TestEncounter(GameObject room)
    {
        var encounter=room.GetComponentInChildren<BasementArenaEncounter>();
        Call(encounter,"Awake");
        Call(encounter,"Update");
        if(encounter.Started||encounter.Cleared)throw new Exception("Empty arena unlocked before activation");
        var boss=Object.Instantiate(encounter.enemyPrefab,room.transform.Find("BossRoom"));
        boss.name="Test boss excluded from arena";boss.transform.localPosition=V(0,1.25f,.5f);
        Call(boss.GetComponent<Enemy>(),"Awake");
        // Enemy Awake is invoked manually in this edit-mode simulation.
        encounter.BeginEncounter();
        if(encounter.RemainingEnemies!=12||encounter.Cleared)throw new Exception("Wrong initial enemy roster");
        var enemies=encounter.arena.GetComponentsInChildren<Enemy>();
        foreach(var e in enemies)Call(e,"Awake");
        for(int i=0;i<enemies.Length-1;i++)enemies[i].TakeDamage(10000);
        Call(encounter,"Update");
        if(encounter.Cleared||encounter.RemainingEnemies!=1)throw new Exception("Gate unlocked too early");
        enemies.Last().TakeDamage(10000);Call(encounter,"Update");
        if(!encounter.Cleared||encounter.RemainingEnemies!=0)throw new Exception("Gate did not unlock");
        if(boss.GetComponent<Enemy>().IsDead)throw new Exception("Boss affected by arena encounter");
        encounter.GetType().GetField("openProgress",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(encounter,1f);
        Call(encounter,"Update");
        if(encounter.gateLeaf.localPosition.y<1.07f)throw new Exception("Gate collider did not lift clear of opening");
        foreach(var e in enemies)Object.DestroyImmediate(e.gameObject);
        Object.DestroyImmediate(boss);
        Debug.Log("BASEMENT_ENCOUNTER_TEST_OK: activation, 12 enemies, one survivor locks gate, final death opens it, boss excluded, collider lifted");
    }
    static void Call(object target,string method)=>target.GetType().GetMethod(method,System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(target,null);
    static void Render(Transform anchor,Vector3 position,Vector3 look,string name)
    {
        var go=new GameObject("Preview camera");var camera=go.AddComponent<Camera>();go.AddComponent<UniversalAdditionalCameraData>();
        camera.fieldOfView=76;camera.nearClipPlane=.05f;camera.farClipPlane=150;
        go.transform.position=anchor.TransformPoint(position);go.transform.LookAt(anchor.TransformPoint(look));
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.018f,.015f);
        var rt=new RenderTexture(1440,960,24);camera.targetTexture=rt;
        for(int i=0;i<4;i++)camera.Render();RenderTexture.active=rt;
        var picture=new Texture2D(1440,960,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,1440,960),0,0);picture.Apply();
        Directory.CreateDirectory("Preview");File.WriteAllBytes("Preview/Basement-"+name+".png",picture.EncodeToPNG());
        RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(picture);Object.DestroyImmediate(go);
    }

    static void Combine(Transform group)
    {
        var renderers=group.GetComponentsInChildren<MeshRenderer>();
        foreach(var bucket in renderers.GroupBy(r=>r.sharedMaterial))
        {
            var pieces=bucket.Select(r=>new CombineInstance {mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=group.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
            var mesh=new Mesh {name=group.name+" "+bucket.Key.name,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(pieces,true,true);mesh.RecalculateBounds();
            string safe=string.Concat(mesh.name.Select(c=>char.IsLetterOrDigit(c)?c:'_'));
            AssetDatabase.CreateAsset(mesh,Art+"/Meshes/"+safe+"_"+(meshIndex++)+".asset");
            var go=new GameObject(bucket.Key.name);go.transform.SetParent(group,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=bucket.Key;
        }
        foreach(var r in renderers)Object.DestroyImmediate(r.gameObject);
        foreach(var t in group.GetComponentsInChildren<Transform>().Reverse())if(t!=group&&t.childCount==0&&t.GetComponents<Component>().Length==1)Object.DestroyImmediate(t.gameObject);
    }

    static float Noise(int x,int y)
    { unchecked { uint n=(uint)(x*374761393+y*668265263+1987);n=(n^(n>>13))*1274126177;return ((n^(n>>16))&65535)/65535f; } }
    static Color Pixel(string kind,Color baseColor,int x,int y)
    {
        float n=Noise(x,y),coarse=Noise(x/5,y/5);
        if(kind=="brick")
        {
            int row=y/16,offset=(row%2)*16,xx=(x+offset)%32;
            if(y%16<2||xx<2)return new Color(.085f,.078f,.065f)*(.8f+.3f*n);
            float brick=Noise((x+offset)/32,row);
            float stain=Noise(x/11,y/23)<.23f?.52f:1;
            return baseColor*(.72f+.36f*brick+.17f*n)*stain;
        }
        if(kind=="stone")
        {
            int row=y/32,xx=(x+(row%2)*16)%32;
            bool seam=xx<2||y%32<2;
            bool crack=(x+y/3)%49==0&&coarse<.55f;
            return baseColor*(seam||crack?.32f:.65f+.22f*n+.30f*coarse);
        }
        if(kind=="rust")
        {
            bool rust=Noise(x/7,y/9)<.36f;
            bool scratch=x%19<1&&y%27<19;
            var c=rust?Color.Lerp(baseColor,new Color(.38f,.17f,.065f),.62f):baseColor;
            return c*(scratch?1.35f:.68f+.36f*n+.15f*coarse);
        }
        if(kind=="hazard")
        {
            bool yellow=(x+y)%32<16;
            return (yellow?new Color(.70f,.46f,.10f):new Color(.042f,.046f,.037f))*(.70f+.31f*n);
        }
        if(kind=="seal")
        {
            float dx=x-63.5f,dy=y-63.5f,r=Mathf.Sqrt(dx*dx+dy*dy);
            bool ring=Mathf.Abs(r-49)<2||Mathf.Abs(r-43)<1;
            bool sigil=Mathf.Abs(dx)<2&&dy>-27&&dy<32||Mathf.Abs(dy-12)<2&&Mathf.Abs(dx)<25||Mathf.Abs(Mathf.Abs(dx)-Mathf.Abs(dy)*.75f)<2&&dy<0&&dy>-31;
            bool rune=r>43&&r<51&&((int)(Mathf.Atan2(dy,dx)*12))%3==0;
            return ring||sigil||rune?new Color(.66f,.07f,.025f)*(.7f+.3f*n):new Color(.05f,.044f,.035f)*(.7f+.4f*coarse);
        }
        if(kind=="wood")return baseColor*(.65f+.32f*n+.24f*Mathf.Sin(y*.5f+Mathf.Sin(x*.13f)*2f)+.10f*coarse);
        if(kind=="plaster")return baseColor*(.70f+.27f*n+.23f*coarse-((coarse<.16f)? .32f:0));
        if(kind=="floor")
        {
            int tx=x/32,ty=y/32;bool seam=x%32<2||y%32<2;
            return seam?new Color(.13f,.12f,.10f):baseColor*((tx+ty)%2==0?1.24f:.71f)*(.8f+.27f*n+.1f*coarse);
        }
        if(kind=="leather")return baseColor*(.7f+.30f*n+.18f*coarse);
        if(kind=="rug")
        {
            int edge=Math.Min(Math.Min(x,127-x),Math.Min(y,127-y));
            Color red=new Color(.35f,.055f,.045f),gold=new Color(.46f,.31f,.12f),dark=new Color(.055f,.045f,.038f);
            bool band=edge<3||edge>8&&edge<11||edge>17&&edge<20;
            float diamond=Math.Abs(x-63.5f)*.8f+Math.Abs(y-63.5f);
            Color c=band?gold:edge<20?((x/3+y/3)%3==0?gold:dark):diamond<32?(diamond%9<3?gold:dark):red;
            if(edge>22&&((x%18-9)*(x%18-9)+(y%18-9)*(y%18-9)<10))c=gold;
            return c*(.73f+.38f*n);
        }
        if(kind=="window")
        {
            Color sky=Color.Lerp(new Color(.60f,.07f,.025f),new Color(.11f,.018f,.027f),y/128f)*(.82f+.20f*coarse);
            int building=18+(int)(Noise(x/11,0)*37);bool tower=Math.Abs(x-77)<8&&y<77||Math.Abs(x-77)<3&&y<92;
            if(y<building||tower)return (x%9<2&&y%13<3&&y>8)?new Color(.35f,.11f,.04f):new Color(.027f,.022f,.027f);
            return sky;
        }
        if(kind=="screen")
        {
            bool letters=y>35&&y<105&&y%11<3&&x>9&&x<85&&x%7<4;
            return letters?new Color(.22f,.48f,.11f):new Color(.025f,.085f,.035f)*(y%2==0?1:.8f);
        }
        if(kind=="diploma")
        {
            int edge=Math.Min(Math.Min(x,127-x),Math.Min(y,127-y));
            if(edge<8)return edge%3==0?new Color(.27f,.20f,.10f):new Color(.72f,.65f,.45f);
            bool text=y>30&&y<96&&y%12<2&&x>22&&x<106&&x%7<5;
            bool seal=(x-64)*(x-64)+(y-20)*(y-20)<40;
            return text||seal?new Color(.20f,.15f,.09f):new Color(.68f,.61f,.43f)*(.9f+.14f*n);
        }
        if(kind=="banner")
        {
            Color bg=new Color(.22f,.028f,.033f),gold=new Color(.60f,.41f,.12f);
            int dx=Math.Abs(x-64);bool shield=dx<29&&y>34&&y<89&&dx<(y-26)*.6f;
            bool wreath=Math.Abs((x-64)*(x-64)+(y-64)*(y-64)-1700)<240&&y<89&&x%4<3;
            bool book=y>52&&y<73&&dx<17;bool crown=y>90&&y<98&&dx<23;
            return (wreath||crown||shield&&dx>24||book&&(x%16==0||y==53||y==72))?gold:bg*(.75f+.35f*n);
        }
        if(kind=="portrait")
        {
            Color bg=y<33?new Color(.14f,.20f,.10f):new Color(.36f,.37f,.30f);
            bool building=x>21&&x<108&&y>31&&y<76||Math.Abs(x-66)<12&&y<105&&y>40;
            if(building)bg=(x%13<5&&y%18<9)?new Color(.075f,.085f,.066f):new Color(.44f,.34f,.20f);
            return bg*(.75f+.32f*n+.09f*coarse);
        }
        return baseColor*(.84f+.21f*n);
    }
    static void Surface(string name,Color baseColor,string kind,float metallic=0,Color? emission=null)
    {
        var texture=new Texture2D(128,128,TextureFormat.RGB24,false);
        var pixels=new Color[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++)pixels[y*128+x]=Pixel(kind,baseColor,x,y);
        texture.SetPixels(pixels);texture.Apply();string path=Art+"/Textures/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",metallic>0?.28f:.09f);mat.SetFloat("_Metallic",metallic);
        if(emission.HasValue){mat.EnableKeyword("_EMISSION");mat.SetTexture("_EmissionMap",mat.GetTexture("_BaseMap"));mat.SetColor("_EmissionColor",emission.Value);mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}
        if(name=="Floor")mat.SetTextureScale("_BaseMap",new Vector2(8,8));
        if(name=="Brick")mat.SetTextureScale("_BaseMap",new Vector2(8,2));
        if(name=="Ceiling")mat.SetTextureScale("_BaseMap",new Vector2(7,7));
        AssetDatabase.CreateAsset(mat,Art+"/Materials/"+name+".mat");Mats[name]=mat;
    }


}

