// Execute in .utmp/BasementPreview, never in the open working project.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class CylinderBuilder
{
    const string Art="Assets/Art/Cylinder", PathName="Assets/prefabs/Scenario/Cylinder.prefab";
    static Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static Transform root; static int index; static System.Random rng=new System.Random(917);
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static float R(float a,float b)=>a+(float)rng.NextDouble()*(b-a);
    static Vector3 Oval(float a,float rx,float rz,float y=0)=>V(Mathf.Sin(a)*rx,y,Mathf.Cos(a)*rz);
    static Transform Group(string name,Vector3 p,float yaw=0,Transform parent=null)
    {var t=new GameObject(name).transform;t.SetParent(parent?parent:root,false);t.localPosition=p;t.localRotation=Quaternion.Euler(0,yaw,0);return t;}
    static GameObject Shape(Transform p,string n,Vector3 v,Vector3 s,string m,PrimitiveType type=PrimitiveType.Cube,Vector3? rot=null)
    {var g=GameObject.CreatePrimitive(type);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=v;g.transform.localScale=s;if(rot.HasValue)g.transform.localRotation=Quaternion.Euler(rot.Value);Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mats[m];return g;}
    static void Box(Transform p,string n,Vector3 v,Vector3 s,string m)=>Shape(p,n,v,s,m);
    static void Cyl(Transform p,string n,Vector3 v,float radius,float height,string m)=>Shape(p,n,v,V(radius*2,height/2,radius*2),m,PrimitiveType.Cylinder);
    static void Solid(Transform p,Vector3 v,Vector3 s){var c=p.gameObject.AddComponent<BoxCollider>();c.center=v;c.size=s;}
    static void Rod(Transform p,Vector3 a,Vector3 b,float width,string m)
    {var g=Shape(p,"Structural member",(a+b)/2,V(width,(b-a).magnitude,width),m);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void Lamp(Transform p,Vector3 pos,Color color,float intensity,float range)
    {var t=Group("Light",pos,0,p);var l=t.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;t.gameObject.AddComponent<UniversalAdditionalLightData>();}
    static void Combine(Transform p,bool includeMovingLeaf=false)
    {
        var rs=p.GetComponentsInChildren<MeshRenderer>().Where(r=>r.GetComponent<MeshFilter>()&&(includeMovingLeaf||!r.GetComponentInParent<OpenDoorWithKey>())).ToArray();
        foreach(var b in rs.GroupBy(r=>r.sharedMaterial))
        {
            var mesh=new Mesh {name=p.name+" "+b.Key.name,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(b.Select(r=>new CombineInstance {mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=p.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray());
            AssetDatabase.CreateAsset(mesh,Art+"/Meshes/Detail_"+(index++)+".asset");
            var g=new GameObject(b.Key.name);g.transform.SetParent(p,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=b.Key;
        }
        foreach(var r in rs)Object.DestroyImmediate(r.gameObject);
    }
    static void Material(string name,Color color,float metal=0,float smooth=0,Color? emission=null,string texture=null)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
        if(texture!=null)m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));
        if(emission.HasValue){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission.Value);}
        AssetDatabase.CreateAsset(m,Art+"/Materials/"+name+".mat");mats[name]=m;
    }
    static void SharedSurface(string name,string source,float? smooth=null)
    {
        var original=AssetDatabase.LoadAssetAtPath<Material>(source);
        if(!original)throw new Exception("Missing style material: "+source);
        var m=new Material(original){name=name,enableInstancing=true};m.mainTextureScale=Vector2.one;
        if(smooth.HasValue)m.SetFloat("_Smoothness",smooth.Value);
        AssetDatabase.CreateAsset(m,Art+"/Materials/"+name+".mat");mats[name]=m;
    }
    static void Label(Transform p,Vector3 pos,string words,float size,Color color,bool outward=true)
    {var t=Group(words,pos,outward?180:0,p);var text=t.gameObject.AddComponent<TextMesh>();text.text=words;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=48;text.characterSize=size;text.color=color;}
    public static void Build()
    {
        if(AssetDatabase.IsValidFolder(Art))AssetDatabase.DeleteAsset(Art);
        foreach(var folder in new[]{"Meshes","Materials","Textures"})Directory.CreateDirectory(Art+"/"+folder);AssetDatabase.Refresh();
        SharedSurface("Concrete","Assets/Art/Basement/Materials/Stone.mat");
        SharedSurface("Plaster","Assets/Art/ClassRoom/Materials/Plaster.mat");
        SharedSurface("Rust","Assets/Art/Basement/Materials/Rust.mat");
        SharedSurface("Iron","Assets/Art/Basement/Materials/Iron.mat");
        SharedSurface("Roof","Assets/Art/Basement/Materials/Steel.mat");
        SharedSurface("RedPaint","Assets/Art/DirectorOffice/Materials/BookRed.mat");
        SharedSurface("Wood","Assets/Art/DirectorOffice/Materials/DarkWood.mat");
        SharedSurface("Dark","Assets/Art/DirectorOffice/Materials/Black.mat");
        SharedSurface("Leaf","Assets/Art/DirectorOffice/Materials/Leaves.mat");
        SharedSurface("LeafLight","Assets/Art/EntranceGarden/Materials/Ivy.mat");
        SharedSurface("LeafDead","Assets/Art/EntranceGarden/Materials/DeadLeaves.mat");
        SharedSurface("Red","Assets/Art/Basement/Materials/Red.mat");
        SharedSurface("Amber","Assets/Art/Basement/Materials/Amber.mat");
        SharedSurface("Wet","Assets/Art/ClassRoom/Materials/Wet.mat");
        SharedSurface("DoorActive","Assets/Art/ClassRoom/Materials/Door.mat");
        SharedSurface("Green","Assets/Art/DirectorOffice/Materials/Screen.mat");
        SharedSurface("Rubble","Assets/Art/ClassRoom/Materials/Rubble.mat");
        PixelSurfaces();
        var room=PrefabUtility.LoadPrefabContents(PathName);
        if(room.transform.Find("Abandoned circular atrium"))throw new Exception("Use original Cylinder snapshot for regeneration");
        // The original floor and its root ID stay available to existing prefab instances.
        foreach(var pb in room.GetComponentsInChildren<ProBuilderMesh>())
        {pb.ToMesh();pb.Refresh();var mesh=Object.Instantiate(pb.GetComponent<MeshFilter>().sharedMesh);mesh.uv=mesh.vertices.Select(v=>new Vector2(v.x/4,v.z/4)).ToArray();AssetDatabase.CreateAsset(mesh,Art+"/Meshes/Shell_"+(index++)+".asset");pb.GetComponent<MeshFilter>().sharedMesh=mesh;var c=pb.GetComponent<MeshCollider>();if(c)c.sharedMesh=mesh;pb.GetComponent<Renderer>().sharedMaterial=mats["Floor"];}
        foreach(Transform child in room.transform)child.gameObject.SetActive(false);
        root=Group("Abandoned circular atrium",V(27.6f,2.5f,19.6f),0,room.transform);root.localScale=V(1,5,1);
        // All geometry below uses metres, despite the source root's 0.2 Y scale.
        AssetDatabase.StartAssetEditing();
        try {Perimeter();Kiosk();Structure();GroundDetails();Abandonment();ConsolidateVegetation();}
        finally {AssetDatabase.StopAssetEditing();}
        foreach(float x in new[]{-12f,12f})foreach(float z in new[]{-6f,6f})Lamp(root,V(x,4.7f,z),new Color(1,.78f,.49f),4.5f,16);
        Group("Player entrance - south",V(0,1.061f,18));
        PrefabUtility.SaveAsPrefabAsset(room,PathName);PrefabUtility.UnloadPrefabContents(room);AssetDatabase.SaveAssets();RemoveUnusedMeshes();Preview();Debug.Log("CYLINDER_BUILD_OK");
    }
    static float Noise(int x,int y)
    {unchecked {uint n=(uint)(x*374761393+y*668265263+917);n=(n^(n>>13))*1274126177;return ((n^(n>>16))&65535)/65535f;}}
    static void PixelSurfaces()
    {
        // Game-native pixel art, matching the filtering and palette of the existing rooms.
        foreach(string kind in new[]{"Floor","Mosaic","SealedDoor","Dust","Damp"})
        {
            int w=128,h=kind=="SealedDoor"?256:128;var t=new Texture2D(w,h,TextureFormat.RGBA32,false);
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                float n=Noise(x,y),coarse=Noise(x/7,y/9);Color c;
                if(kind=="Floor")
                {c=new Color(.26f,.28f,.26f)*(.65f+n*.25f+coarse*.24f);if((x+y/3)%53<1||Noise(x/15,y/18)<.16f)c*=.45f;}
                else if(kind=="Mosaic")
                {int cx=x/8,cy=y/8;float cell=Noise(cx,cy);bool grout=x%8<1||y%8<1;bool lost=cell<.11f;bool crack=(x+y/2)%43==0&&coarse<.6f;c=grout||lost||crack?new Color(.075f,.085f,.072f):Color.Lerp(new Color(.23f,.31f,.29f),new Color(.47f,.45f,.34f),cell)*(.70f+n*.35f);}
                else if(kind=="SealedDoor")
                {
                    bool frame=x<7||x>120||y<7||y>247;bool seam=x%62<2;bool vent=y>182&&y<226&&x>15&&x<112&&y%7<3;
                    c=frame?new Color(.085f,.10f,.092f):vent||seam?new Color(.045f,.055f,.047f):new Color(.21f,.245f,.215f)*(.6f+n*.22f+coarse*.27f);
                    if(y<60&&!frame)c*=.6f;if(Noise(x/13,y/17)<.19f)c=Color.Lerp(c,new Color(.12f,.15f,.065f),.45f);
                }
                else
                {float dx=(x-63.5f)/64,dy=(y-63.5f)/64,r=Mathf.Sqrt(dx*dx+dy*dy);float alpha=Mathf.Clamp01((.85f-r)*2.6f)*(coarse<.22f?.18f:.65f);c=kind=="Dust"?new Color(.37f,.30f,.19f,alpha):new Color(.055f,.095f,.060f,alpha);}
                t.SetPixel(x,y,c);
            }
            t.Apply();string path=Art+"/Textures/"+kind+".png";File.WriteAllBytes(path,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(path);
            var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.filterMode=FilterMode.Point;imp.anisoLevel=1;imp.maxTextureSize=128;imp.mipmapEnabled=true;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.wrapMode=TextureWrapMode.Repeat;imp.SaveAndReimport();
            Material(kind,Color.white,0,kind=="Damp"?.28f:.06f,null,path);
            if(kind=="Dust"||kind=="Damp")
            {var m=mats[kind];m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;}
        }
        SharedSurface("BandIvory","Assets/Art/DirectorOffice/Materials/Ivory.mat");
        SharedSurface("BandDark","Assets/Art/Basement/Materials/Iron.mat");
    }
    static void RemoveUnusedMeshes()
    {
        var used=new HashSet<string>(Regex.Matches(File.ReadAllText(PathName),@"guid: (\w+)").Cast<Match>().Select(m=>m.Groups[1].Value));
        foreach(var path in Directory.GetFiles(Art+"/Meshes","*.asset"))
        {string assetPath=path.Replace('\\','/');if(!used.Contains(AssetDatabase.AssetPathToGUID(assetPath)))AssetDatabase.DeleteAsset(assetPath);}
    }
    static void Perimeter()
    {
        for(int i=0;i<48;i++)
        {
            float a=i*Mathf.PI*2/48,half=Mathf.PI/48;
            Vector3 start=Oval(a-half,27.1f,19.1f),end=Oval(a+half,27.1f,19.1f),outward=Vector3.Cross(end-start,Vector3.up).normalized;
            var p=Group("Service bay "+i,(start+end)/2,Mathf.Atan2(outward.x,outward.z)*Mathf.Rad2Deg);
            float width=(end-start).magnitude+.16f;
            bool exit=i==0||i==1;
            bool interactive=i==12||i==24||i==36;
            if(!exit){
                if(interactive)
                {float side=(width-1.95f)/2;foreach(int sign in new[]{-1,1}){Vector3 v=V(sign*(.975f+side/2),2,-.03f),s=V(side+.04f,4,.42f);Box(p,"Door flank",v,s,"Plaster");Solid(p,v,s);}Box(p,"Door header",V(0,3.68f,-.03f),V(1.99f,.64f,.42f),"Plaster");Solid(p,V(0,3.68f,-.03f),V(1.99f,.64f,.42f));ActiveDoor(p,i);}
                else {Box(p,"Concrete wall",V(0,2,-.03f),V(width,4,.42f),"Plaster");Solid(p,V(0,2,-.03f),V(width,4,.42f));}
                if(!interactive)Box(p,"Stone base course",V(0,.32f,-.26f),V(width,.64f,.14f),"Concrete");
                if(!interactive)
                {Shape(p,"Sealed door image - noninteractive",V(0,1.52f,-.251f),V(Mathf.Min(1.5f,width-.35f),2.95f,1),"SealedDoor",PrimitiveType.Quad);
                    if(i%6==0)Box(p,"Unlit emergency lamp",V(0,3.55f,-.33f),V(.5f,.06f,.08f),"Dark");}
            }
            else {Box(p,"Entrance lintel",V(0,3.8f,0),V(width,.4f,.48f),"Iron");if(i==0)Label(p,V(0,3.77f,-.3f),"ATRIUM / 09",.075f,new Color(.85f,.62f,.28f),false);}
            Box(p,"Gallery slab",V(0,4.18f,-.7f),V(width,.25f,1.9f),"Concrete");
            for(int j=0;j<6;j++)Rod(p,V(-width/2+j*width/5,4.3f,-1.5f),V(-width/2+j*width/5,5.32f,-1.5f),.045f,"Rust");
            Box(p,"Gallery handrail",V(0,5.32f,-1.5f),V(width,.08f,.08f),"Rust");
            if(!exit){Box(p,"Wall buttress",V(width/2,2,0),V(.2f,4,.65f),"Concrete");Solid(p,V(width/2,2,0),V(.2f,4,.65f));}
            Combine(p);
            if(i%3==1)Vine(p,V(width/2,5.25f,-.4f),4.5f);
        }
    }
    static void ActiveDoor(Transform bay,int slot)
    {
        string name=slot==12?"Left":slot==36?"Right":"Rear";
        var rig=Group("Interactive door - "+name,V(0,0,-.06f),90,bay);
        var trigger=rig.gameObject.AddComponent<BoxCollider>();trigger.center=V(0,1.7f,0);trigger.size=V(3.6f,3.4f,2.8f);trigger.isTrigger=true;
        var body=rig.gameObject.AddComponent<Rigidbody>();body.isKinematic=true;body.useGravity=false;
        var leaf=Group("Door",Vector3.zero,0,rig);Solid(leaf,V(0,1.635f,0),V(.14f,3.25f,1.85f));
        Box(leaf,"Worn green door leaf",V(0,1.635f,0),V(.14f,3.25f,1.85f),"DoorActive");
        foreach(float z in new[]{-.8f,.8f})Box(leaf,"Raised leaf stile",V(.085f,1.6f,z),V(.035f,3,.07f),"Iron");
        foreach(float y in new[]{.18f,1.95f,3.05f})Box(leaf,"Raised leaf rail",V(.085f,y,0),V(.035f,.065f,1.6f),"Iron");
        Box(leaf,"Handle backplate",V(.1f,1.35f,.63f),V(.035f,.3f,.15f),"Rust");Box(leaf,"Usable handle",V(.15f,1.36f,.56f),V(.08f,.045f,.26f),"Concrete");
        for(int i=0;i<6;i++)Box(leaf,"Lower vent",V(.09f,.48f+i*.09f,0),V(.03f,.04f,.9f),"Dark");
        Combine(leaf,true);
        foreach(float x in new[]{-1.02f,1.02f}){Box(bay,"Door jamb",V(x,1.68f,-.15f),V(.09f,3.36f,.22f),"Iron");Solid(bay,V(x,1.68f,-.15f),V(.09f,3.36f,.22f));}
        Box(bay,"Door lintel",V(0,3.36f,-.15f),V(2.13f,.10f,.22f),"Iron");
        Box(bay,"Red access indicator",V(.8f,3.59f,-.31f),V(.18f,.10f,.06f),"Red");
        Label(bay,V(-.1f,3.60f,-.31f),"ACESSO / "+(slot/12).ToString("00"),.052f,new Color(.70f,.78f,.60f),false);
        Lamp(bay,V(0,3.25f,-.65f),new Color(1,.07f,.026f),3,7);
        var behaviour=rig.gameObject.AddComponent<OpenDoorWithKey>();var so=new SerializedObject(behaviour);
        so.FindProperty("requiresKey01").boolValue=false;so.FindProperty("requiresKey02").boolValue=false;so.FindProperty("requiresKey03").boolValue=false;
        so.FindProperty("door").objectReferenceValue=leaf;so.FindProperty("hingePoint").vector3Value=V(0,0,-.5f);so.FindProperty("openAngle").floatValue=100;
        so.FindProperty("animationDuration").floatValue=.65f;so.FindProperty("closeDelay").floatValue=.7f;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Kiosk()
    {
        var p=Group("Central canteen - warm landmark",Vector3.zero);
        Cyl(p,"Foundation",V(0,.12f,0),4.4f,.24f,"Concrete");
        var c=p.gameObject.AddComponent<CapsuleCollider>();c.center=V(0,1.4f,0);c.radius=4.25f;c.height=2.8f;
        for(int i=0;i<16;i++)
        {
            float a=i*Mathf.PI/8;var b=Group("Kiosk bay",Oval(a,4,4),a*Mathf.Rad2Deg,p);
            Box(b,"Counter plinth",V(0,.55f,-.4f),V(1.6f,1.1f,1.2f),"Wood");
            Box(b,"Continuous worktop",V(0,1.14f,-.4f),V(1.65f,.10f,1.2f),"Concrete");Solid(b,V(0,1.14f,-.4f),V(1.65f,.10f,1.2f));
            Box(b,"Counter edge",V(0,1.14f,.21f),V(1.65f,.10f,.06f),"Rust");
            foreach(float x in new[]{-.52f,.52f})Box(b,"Cabinet handle",V(x,.76f,.23f),V(.22f,.04f,.04f),"Iron");
            Box(b,"Window sill",V(0,1.28f,0),V(1.6f,.06f,.12f),"Iron");
            foreach(float x in new[]{-.76f,.76f})Box(b,"Window mullion",V(x,2,0),V(.07f,1.7f,.12f),"Iron");
            Box(b,"Header",V(0,2.9f,0),V(1.65f,.32f,.3f),"RedPaint");Box(b,"Interior strip",V(0,2.64f,-.16f),V(1.4f,.045f,.045f),"Amber");
            SupportedProp(b,"Coffee machine",V(.2f,1.19f,-.6f),V(.45f,.6f,.38f),"Iron");
            Box(b,"Machine control fascia",V(.2f,1.63f,-.399f),V(.36f,.12f,.025f),"Dark");
            Box(b,"Machine drip tray",V(.2f,1.215f,-.36f),V(.38f,.035f,.13f),"Rust");
            SupportedProp(b,"Coffee urn",V(-.35f,1.19f,-.5f),V(.26f,.46f,.26f),"Rust",true);
            for(int k=0;k<3;k++)SupportedProp(b,"Cup",V(-.55f+k*.16f,1.19f,.09f),V(.11f,.14f,.11f),"Concrete",true);
            if(i%4==0){Label(b,V(0,2.91f,.18f),"CANTEEN 09",.056f,new Color(.85f,.71f,.48f));Lamp(b,V(0,2.3f,-.8f),new Color(1,.78f,.49f),8,9);}
        }
        Cyl(p,"Flat canopy",V(0,3.14f,0),4.55f,.2f,"Roof");Cyl(p,"Extractor",V(0,3.65f,0),.65f,.85f,"Iron");Combine(p);
        for(int i=0;i<18;i++)Vine(p,Oval(i*Mathf.PI/9,4.4f,4.4f,3.22f),R(.7f,2));
        for(int i=0;i<8;i++)
        {
            float a=(i+.5f)*Mathf.PI/4;var t=Group("Cafe table cluster "+i,Oval(a,7,6.7f),a*Mathf.Rad2Deg);
            Cyl(t,"Tabletop",V(0,.8f,0),.65f,.075f,"Wood");Cyl(t,"Table stem",V(0,.4f,0),.06f,.8f,"Iron");Cyl(t,"Foot",V(0,.03f,0),.35f,.06f,"Iron");Solid(t,V(0,.45f,0),V(1.3f,.9f,1.3f));
            for(int j=0;j<3;j++)
            {var s=Group("Chair",Oval(j*Mathf.PI*2/3,1,1),j*120,t);Box(s,"Seat",V(0,.46f,0),V(.43f,.07f,.43f),"RedPaint");Box(s,"Back",V(0,.75f,.2f),V(.43f,.5f,.055f),"RedPaint");foreach(float x in new[]{-.18f,.18f})foreach(float z in new[]{-.18f,.18f})Rod(s,V(x,0,z),V(x,.45f,z),.028f,"Iron");
                if((i==2||i==6)&&j==1){s.name="Fallen cafe chair";s.localRotation*=Quaternion.Euler(0,0,82);float bottom=s.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);s.position+=Vector3.up*(root.position.y-bottom+.01f);}}
            Combine(t);
        }
    }
    static void SupportedProp(Transform p,string name,Vector3 bottom,Vector3 size,string material,bool cylinder=false)
    {
        if(cylinder)Cyl(p,name,bottom+Vector3.up*size.y/2,size.x/2,size.y,material);
        else Box(p,name,bottom+Vector3.up*size.y/2,size,material);
        Group("Support check - "+name,bottom,0,p);
    }
    static void Structure()
    {
        var roof=Group("Complete radial steel roof",Vector3.zero);
        for(int i=0;i<16;i++)
        {
            float a=(i+.5f)*Mathf.PI/8;Vector3 at=Oval(a,16.5f,12.8f);var p=Group("Column and planter "+i,at);
            Box(p,"Column",V(0,3.5f,0),V(.48f,7,.48f),"Concrete");Solid(p,V(0,3.5f,0),V(.48f,7,.48f));
            foreach(float y in new[]{.2f,1.1f,6.7f})Box(p,"Steel collar",V(0,y,0),V(.62f,.13f,.62f),"Rust");
            Rod(p,V(0,7,0),V(0,7.7f,0),.24f,"Iron");
            Rod(p,V(-.7f,6.65f,0),V(0,7.6f,0),.10f,"Rust");Rod(p,V(.7f,6.65f,0),V(0,7.6f,0),.10f,"Rust");
            Box(p,"Planter soil",V(0,.23f,0),V(2.4f,.42f,1.8f),"Dark");Solid(p,V(0,.25f,0),V(2.5f,.5f,1.9f));
            foreach(float x in new[]{-1.2f,1.2f})Box(p,"Planter curb",V(x,.28f,0),V(.13f,.56f,1.9f),"Concrete");foreach(float z in new[]{-.9f,.9f})Box(p,"Planter curb",V(0,.28f,z),V(2.5f,.56f,.13f),"Concrete");
            Combine(p);for(int j=0;j<10;j++)Plant(p,V(R(-1,1),.5f,R(-.7f,.7f)),R(.45f,1.1f));Vine(p,V(.2f,6.9f,.2f),6.2f);
            Vector3 end=Oval(a,27.7f,19.7f,6.8f),start=V(0,8.6f,0);Rod(roof,start,end,.15f,"Rust");Rod(roof,start-Vector3.up*.65f,end-Vector3.up*.65f,.12f,"Iron");
            for(int j=0;j<9;j++)
            {var v1=Vector3.Lerp(start,end,j/9f);var v2=Vector3.Lerp(start,end,(j+1)/9f);Rod(roof,v1,v2-Vector3.up*.65f,.045f,"Rust");Rod(roof,v1-Vector3.up*.65f,v2,.045f,"Rust");}
            if(i%2==0){var f=Group("Pendant",at+V(0,6.2f,0));Rod(f,V(0,1.4f,0),V(0,-.5f,0),.04f,"Iron");Cyl(f,"Shade",V(0,-.6f,0),.38f,.17f,"Iron");Cyl(f,"Bulb",V(0,-.7f,0),.21f,.04f,"Amber");Combine(f);Lamp(f,V(0,-.85f,0),new Color(1,.78f,.49f),3.5f,14);}
        }
        for(int i=0;i<96;i++)
        {
            float a=i*Mathf.PI/48,b=(i+1)*Mathf.PI/48;
            foreach(float r in new[]{.12f,.35f,.64f,1f})Rod(roof,Oval(a,27.7f*r,19.7f*r,8.6f-1.8f*r),Oval(b,27.7f*r,19.7f*r,8.6f-1.8f*r),.08f,"Iron");
            // A closed fan meets at the apex and extends past the perimeter walls.
            Vector3 v0=V(0,8.6f,0),v1=Oval(a,27.7f,19.7f,6.8f),v2=Oval(b,27.7f,19.7f,6.8f);
            var mesh=new Mesh();mesh.vertices=new[]{v0,v1,v2,v0,v1,v2};
            var uv=new[]{new Vector2(0,8),new Vector2(0,0),new Vector2((v2-v1).magnitude/2,0)};mesh.uv=uv.Concat(uv).ToArray();
            mesh.triangles=new[]{0,1,2,3,5,4};mesh.RecalculateNormals();
            var g=new GameObject("Closed metal roof panel");g.transform.SetParent(roof,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mats["Roof"];
        }
        Combine(roof);
        var skin=roof.GetComponentsInChildren<MeshRenderer>().Single(r=>r.sharedMaterial==mats["Roof"]);
        skin.gameObject.AddComponent<MeshCollider>().sharedMesh=skin.GetComponent<MeshFilter>().sharedMesh;
        var rails=Group("Inner garden rail - four wide circulation breaks",Vector3.zero);
        for(int i=0;i<96;i++)
        {if(i%24<3||i%24>20)continue;float a=i*Mathf.PI/48,b=(i+1)*Mathf.PI/48;Vector3 p1=Oval(a,21.7f,15.1f),p2=Oval(b,21.7f,15.1f);Rod(rails,p1+Vector3.up*1.03f,p2+Vector3.up*1.03f,.065f,"Rust");Rod(rails,p1+Vector3.up*.28f,p2+Vector3.up*.28f,.04f,"Iron");for(int j=0;j<3;j++){var p=Vector3.Lerp(p1,p2,j/3f);Rod(rails,p,p+Vector3.up,.028f,"Iron");}var c=rails.gameObject.AddComponent<BoxCollider>();c.center=(p1+p2)/2+Vector3.up*.5f;c.size=V(.12f,1,(p2-p1).magnitude); // rotated colliders require their own transform
            Object.DestroyImmediate(c);var t=Group("Rail collision",(p1+p2)/2,0,rails);t.localRotation=Quaternion.LookRotation(p2-p1);Solid(t,V(0,.5f,0),V(.12f,1,(p2-p1).magnitude));}
        Combine(rails);
    }
    static Mesh leafMesh;
    static void Leaf(Transform p,Vector3 pos,float length,float yaw,float tilt,string mat)
    {
        if(!leafMesh){leafMesh=new Mesh();var vs=new[]{V(0,0,-.5f),V(-.22f,0,0),V(0,.07f,0),V(.22f,0,0),V(0,0,.5f)};leafMesh.vertices=vs.Concat(vs).ToArray();var uv=new[]{new Vector2(.5f,0),new Vector2(0,.5f),new Vector2(.5f,.5f),new Vector2(1,.5f),new Vector2(.5f,1)};leafMesh.uv=uv.Concat(uv).ToArray();leafMesh.triangles=new[]{0,1,2,0,2,3,1,4,2,2,4,3,7,6,5,8,7,5,7,9,6,8,9,7};leafMesh.RecalculateNormals();}
        var t=Group("Lance leaf",pos,0,p);t.localRotation=Quaternion.Euler(tilt,yaw,0);t.localScale=Vector3.one*length;t.gameObject.AddComponent<MeshFilter>().sharedMesh=leafMesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mats[mat];
    }
    static void Plant(Transform p,Vector3 pos,float size)
    {var t=Group("Broadleaf undergrowth",pos,0,p);for(int j=0;j<7;j++){float a=j*51+R(-10,10);Leaf(t,V(Mathf.Sin(a*Mathf.Deg2Rad)*size*.24f,size*.28f,Mathf.Cos(a*Mathf.Deg2Rad)*size*.24f),size,a,R(-48,-12),j%3==0?"LeafLight":"Leaf");}Combine(t);}
    static void Vine(Transform p,Vector3 top,float length)
    {var t=Group("Hanging ivy",Vector3.zero,0,p);for(int j=0;j<length*5;j++){float y=j*.2f;var at=top+V(Mathf.Sin(j*.7f)*.12f,-y,Mathf.Cos(j*.5f)*.08f);if(j>0)Rod(t,at+V(0,.22f,0),at,.015f,"Leaf");for(int k=0;k<2;k++)Leaf(t,at+V(k==0?-.11f:.11f,0,-.08f),R(.2f,.34f),R(0,360),R(15,55),"Leaf");}Combine(t);}
    static void ConsolidateVegetation()
    {
        var plants=root.GetComponentsInChildren<Transform>().Where(t=>t.name=="Hanging ivy"||t.name=="Broadleaf undergrowth").ToArray();
        var sectors=Enumerable.Range(0,4).Select(i=>Group("Vegetation sector "+i,Vector3.zero)).ToArray();
        var oldMeshes=new HashSet<string>();
        foreach(var t in plants)
        {
            var pos=root.InverseTransformPoint(t.position);int sector=(pos.x<0?0:1)+(pos.z<0?0:2);
            foreach(var mf in t.GetComponentsInChildren<MeshFilter>())oldMeshes.Add(AssetDatabase.GetAssetPath(mf.sharedMesh));
            t.SetParent(sectors[sector],true);
        }
        foreach(var s in sectors)Combine(s);
        foreach(var path in oldMeshes)if(path.StartsWith(Art+"/Meshes/"))AssetDatabase.DeleteAsset(path);
        foreach(var s in sectors)foreach(var t in s.GetComponentsInChildren<Transform>().Reverse())
            if(t!=s&&t.childCount==0&&t.GetComponents<Component>().Length==1)Object.DestroyImmediate(t.gameObject);
    }
    static void GroundDetails()
    {
        var p=Group("Wet paving - drains debris and puddles",Vector3.zero);
        FloorRing(p,4.7f,19.9f,"Mosaic",false);
        FloorRing(p,5.0f,5.18f,"BandIvory",true);FloorRing(p,9.8f,10.1f,"BandDark",true);
        FloorRing(p,14.0f,14.16f,"BandIvory",true);FloorRing(p,19.65f,19.88f,"BandDark",true);
        for(int i=0;i<160;i++)
        {float a=i*Mathf.PI/80,b=(i+1)*Mathf.PI/80;var x=Oval(a,20.5f,14.3f,.018f);var y=Oval(b,20.5f,14.3f,.018f);Rod(p,x,y,.20f,"Dark");for(int j=0;j<4;j++){var c=Vector3.Lerp(x,y,j/4f);var n=V(c.x/20.5f,0,c.z/14.3f).normalized;Rod(p,c-n*.12f+Vector3.up*.025f,c+n*.12f+Vector3.up*.025f,.024f,"Iron");}}
        for(int i=0;i<110;i++)
        {float a=R(0,Mathf.PI*2),rad=R(.3f,.95f);var v=Oval(a,26*rad,18*rad,.008f);if(v.magnitude<5)continue;Shape(p,"Shallow rain puddle",v,V(R(.3f,1.9f),.004f,R(.3f,.95f)),"Wet",PrimitiveType.Sphere);}
        for(int i=0;i<600;i++){float a=R(0,Mathf.PI*2),rad=R(.28f,.99f);Leaf(p,Oval(a,26*rad,18*rad,.025f),R(.09f,.22f),R(0,360),0,"LeafDead");}
        Combine(p);
        for(int i=0;i<10;i++){float a=(i+.3f)*Mathf.PI/5;Plant(root,Oval(a,24.8f,17.5f),R(.6f,1.2f));}
    }
    static void FloorRing(Transform p,float inside,float outside,string mat,bool damaged)
    {
        var vs=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
        for(int i=0;i<128;i++)
        {if(damaged&&i%23>17)continue;float a=i*Mathf.PI/64,b=(i+1)*Mathf.PI/64;int n=vs.Count;
            foreach(var v in new[]{Oval(a,inside,inside*.72f,.011f),Oval(b,inside,inside*.72f,.011f),Oval(b,outside,outside*.72f,.011f),Oval(a,outside,outside*.72f,.011f)}){vs.Add(v);uv.Add(new Vector2(v.x/4,v.z/4));}
            tris.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});}
        var mesh=new Mesh();mesh.vertices=vs.ToArray();mesh.uv=uv.ToArray();mesh.triangles=tris.ToArray();mesh.RecalculateNormals();
        var g=new GameObject("Worn mosaic band");g.transform.SetParent(p,false);g.transform.localPosition=V(0,damaged?.003f:0,0);g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=mats[mat];
    }
    static void Abandonment()
    {
        var p=Group("Dust damp broken tiles and fallen debris",Vector3.zero);
        for(int i=0;i<105;i++)
        {float a=R(0,Mathf.PI*2),r=R(.25f,.97f);var pos=Oval(a,26*r,18*r,.020f);if(pos.magnitude<4.8f)continue;
            Shape(p,"Ragged damp or dust stain",pos,V(R(.8f,3.7f),R(.5f,2.5f),1),i%3==0?"Damp":"Dust",PrimitiveType.Quad,V(90,R(0,360),0));}
        for(int i=0;i<85;i++)
        {float a=R(0,Mathf.PI*2);if(Mathf.Abs(Mathf.Sin(a*2))<.28f)continue;var pos=Oval(a,R(25,26.2f),R(17.4f,18.1f),.04f);
            Shape(p,"Broken tile fragment",pos,V(R(.08f,.24f),.035f,R(.12f,.28f)),"Rubble",PrimitiveType.Cube,V(R(-10,10),R(0,360),R(-6,6)));}
        foreach(float a in new[]{.65f,2.25f,3.9f,5.5f})
        {var pos=Oval(a,24.7f,17.1f,.055f);for(int j=0;j<4;j++)Shape(p,"Fallen rotten plank",pos+V(j*.14f,0,j*.07f),V(.16f,.05f,R(.7f,1.2f)),"Wood",PrimitiveType.Cube,V(0,R(-25,25),0));}
        Combine(p);
        foreach(float a in new[]{.55f,1.1f,2.0f,2.55f,3.8f,4.3f,5.15f,5.7f})Plant(root,Oval(a,25.6f,18.0f),R(.55f,.9f));
    }
    static void Preview()
    {
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PathName));room.transform.position=Vector3.zero;
        // Runtime door hinges reparent their leaves; editor validation needs an unpacked instance.
        PrefabUtility.UnpackPrefabInstance(room,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        root=room.transform.Find("Abandoned circular atrium");
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.16f,.15f,.13f);RenderSettings.reflectionIntensity=.12f;RenderSettings.fog=false;
        var fill=new GameObject("Inspection fill").AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.78f,.76f,.69f);fill.intensity=.3f;fill.transform.rotation=Quaternion.Euler(48,-35,0);fill.shadows=LightShadows.Soft;
        var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(RenderSettings.ambientLight);RenderSettings.ambientProbe=ambient;
        Physics.SyncTransforms();Validate(room);
        Render(V(8,1.8f,17.3f),V(0,2.8f,-1),"Player");Render(V(21.5f,1.8f,5),V(0,2,0),"Walkway");Render(V(7,5.5f,17),V(0,1.6f,0),"Overview");Render(V(1.5f,1.8f,5.6f),V(0,1.4f,3.3f),"Canteen");Render(V(24,1.8f,2.5f),V(27.1f,1.7f,0),"ActiveDoor");Render(V(10,4.2f,9),V(11,.1f,2),"Floor");
        EditorSceneManager.SaveScene(scene,Art+"/CylinderPreview.unity");
    }
    public static void Verify(){Preview();Debug.Log("CYLINDER_BUILD_OK");}
    // Apply the final lighting change without regenerating geometry or asset GUIDs.
    public static void UpdateDoorLighting()
    {
        string before=File.ReadAllText(PathName);
        var room=PrefabUtility.LoadPrefabContents(PathName);
        var red=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Red.mat");
        var dark=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Dark.mat");
        try
        {
            var atrium=room.transform.Find("Abandoned circular atrium");
            foreach(Transform bay in atrium)
            {
                if(!bay.name.StartsWith("Service bay "))continue;
                bool interactive=bay.GetComponentInChildren<OpenDoorWithKey>()!=null;
                foreach(var renderer in bay.GetComponentsInChildren<MeshRenderer>())
                {
                    if(interactive&&renderer.sharedMaterial==AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/Green.mat"))renderer.sharedMaterial=red;
                    else if(!interactive&&renderer.sharedMaterial==red)renderer.sharedMaterial=dark;
                }
                foreach(var light in bay.GetComponentsInChildren<Light>())
                {
                    light.enabled=interactive;
                    if(interactive){light.color=new Color(1,.07f,.026f);light.intensity=3;light.range=7;}
                }
            }
            PrefabUtility.SaveAsPrefabAsset(room,PathName);
        }
        finally {PrefabUtility.UnloadPrefabContents(room);}
        AssetDatabase.SaveAssets();
        // ProBuilder may clear its shared mesh when prefab contents unload.
        // This revision only changes lighting, so preserve every mesh reference.
        string saved=File.ReadAllText(PathName);
        foreach(Match block in Regex.Matches(before,@"--- !u!(33|64) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\z)",RegexOptions.Singleline))
        {
            string mesh=Regex.Match(block.Value,@"  m_Mesh: [^\n]*").Value;
            string pattern=@"--- !u!"+block.Groups[1].Value+" &"+block.Groups[2].Value+@"[^\n]*\n.*?(?=--- !u!|\z)";
            saved=Regex.Replace(saved,pattern,m=>Regex.Replace(m.Value,@"  m_Mesh: [^\n]*",mesh),RegexOptions.Singleline);
        }
        File.WriteAllText(PathName,saved);AssetDatabase.ImportAsset(PathName);Verify();
    }
    static void Validate(GameObject room)
    {
        foreach(var r in room.GetComponentsInChildren<MeshRenderer>())if(r.GetComponent<MeshFilter>()&&(!r.GetComponent<MeshFilter>().sharedMesh||!r.sharedMaterial))throw new Exception("Missing asset: "+r.name);
        var player=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Player/Player.prefab"));
        foreach(var b in player.GetComponentsInChildren<MonoBehaviour>())b.enabled=false;var cc=player.GetComponent<CharacterController>();
        float feet=cc.height*player.transform.lossyScale.y/2;
        // Traverse both complete circulation loops, the only entrance, and test the sealed sides.
        foreach(var radii in new[]{new Vector2(24,17),new Vector2(11,9)})
        {player.transform.position=root.TransformPoint(V(0,feet+.06f,radii.y));Physics.SyncTransforms();for(int i=1;i<=720;i++){var target=root.TransformPoint(Oval(i*Mathf.PI/360,radii.x,radii.y,feet+.06f));cc.Move(target-player.transform.position);if(Vector3.Distance(player.transform.position,target)>.15f)throw new Exception("Player route obstructed at "+target);}}
        for(int side=0;side<4;side++)
        {float a=side*Mathf.PI/2;bool blocked=false;player.transform.position=root.TransformPoint(Oval(a,10,9,feet+.06f));Physics.SyncTransforms();for(int i=1;i<=100;i++){float t=i/100f;var target=root.TransformPoint(Oval(a,Mathf.Lerp(10,28,t),Mathf.Lerp(9,20,t),feet+.06f));cc.Move(target-player.transform.position);if(Vector3.Distance(player.transform.position,target)>.15f){blocked=true;break;}}if(side==0&&blocked)throw new Exception("Entrance obstructed");if(side!=0&&!blocked)throw new Exception("Former entrance not sealed: "+side);}
        TestDoors(room,player,cc,feet);
        Object.DestroyImmediate(player);Debug.Log("CYLINDER_PLAYER_ROUTES_OK: two loops, south entrance and three doors checked closed and open");
        var markers=root.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Support check - ")).ToArray();
        if(markers.Length!=80)throw new Exception("Canteen support marker count changed");
        foreach(var marker in markers)
        {if(!Physics.Raycast(marker.position+Vector3.up*.03f,Vector3.down,out var hit,.06f)||Mathf.Abs(hit.point.y-marker.position.y)>.01f)throw new Exception("Unsupported canteen object: "+marker.name);}
        Debug.Log("CYLINDER_CANTEEN_SUPPORT_OK: 80 props rest directly on continuous worktops");
        for(int i=0;i<96;i++)foreach(float r in new[]{0f,.15f,.5f,.97f})
        {float a=(i+.5f)*Mathf.PI/48;Vector3 pos=root.TransformPoint(Oval(a,27.1f*r,19.1f*r,4.6f));if(!Physics.Raycast(pos,Vector3.up,out var hit,5)||hit.collider.name!="Roof")throw new Exception("Incomplete roof coverage: "+pos);}
        Debug.Log("CYLINDER_ROOF_COVERAGE_OK: 384 upward rays, apex and perimeter sealed");
    }
    static void TestDoors(GameObject room,GameObject player,CharacterController cc,float feet)
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var doors=room.GetComponentsInChildren<OpenDoorWithKey>();if(doors.Length!=3)throw new Exception("Exactly three interactive doors required");
        int images=room.GetComponentsInChildren<MeshRenderer>().Count(r=>r.sharedMaterial&&r.sharedMaterial.name.StartsWith("SealedDoor"));if(images!=43)throw new Exception("Expected 43 flat sealed-door images, got "+images);
        foreach(var door in doors)
        {
            var type=door.GetType();type.GetMethod("Awake",flags).Invoke(door,null);
            var leaf=(Transform)type.GetField("door",flags).GetValue(door);var hinge=(Transform)type.GetField("hinge",flags).GetValue(door);
            player.transform.position=door.transform.TransformPoint(V(1.3f,feet+.06f,0));Physics.SyncTransforms();
            type.GetMethod("TrackPlayer",flags).Invoke(door,new object[]{cc});type.GetMethod("Update",flags).Invoke(door,null);
            if(!(bool)type.GetField("unlocked",flags).GetValue(door))throw new Exception("Door does not respond to player proximity: "+door.name);
            // Finish the animation deterministically; exercise the actual hinge and moving collider.
            hinge.localRotation=Quaternion.Euler(0,-100,0);Physics.SyncTransforms();
            for(int i=1;i<=100;i++)
            {var target=door.transform.TransformPoint(V(Mathf.Lerp(1.3f,-.8f,i/100f),feet+.06f,0));cc.Move(target-player.transform.position);if(Vector3.Distance(player.transform.position,target)>.15f)
                {var nearby=Physics.OverlapSphere(target,.75f).Where(c=>!c.isTrigger&&c!=cc).Select(c=>c.name+" parent="+c.transform.parent.name+" bounds="+c.bounds);throw new Exception("Open door blocks player: "+door.name+" step="+i+" target="+target+" actual="+player.transform.position+" nearby="+string.Join("; ",nearby));}}
            type.GetMethod("OnDisable",flags).Invoke(door,null);Physics.SyncTransforms();
            player.transform.position=door.transform.TransformPoint(V(1.3f,feet+.06f,0));bool blocked=false;
            for(int i=1;i<=100;i++){var target=door.transform.TransformPoint(V(Mathf.Lerp(1.3f,-.8f,i/100f),feet+.06f,0));cc.Move(target-player.transform.position);if(Vector3.Distance(player.transform.position,target)>.15f){blocked=true;break;}}
            if(!blocked||!leaf.GetComponent<BoxCollider>())throw new Exception("Closed door does not block: "+door.name);
            leaf.SetParent(door.transform,false);leaf.localPosition=Vector3.zero;leaf.localRotation=Quaternion.identity;leaf.localScale=Vector3.one;
            var motion=door.transform.Find("DoorMotionRoot");if(motion)Object.DestroyImmediate(motion.gameObject);
        }
        Debug.Log("CYLINDER_DOORS_OK: three proximity doors with moving colliders; 43 sealed images have no interaction");
    }
    static void Render(Vector3 pos,Vector3 target,string name)
    {
        var g=new GameObject("Preview camera");var c=g.AddComponent<Camera>();g.AddComponent<UniversalAdditionalCameraData>();c.fieldOfView=72;c.nearClipPlane=.05f;c.farClipPlane=160;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.025f,.045f,.078f);g.transform.position=root.TransformPoint(pos);g.transform.LookAt(root.TransformPoint(target));
        var rt=new RenderTexture(1600,1000,24);c.targetTexture=rt;for(int i=0;i<4;i++)c.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();Directory.CreateDirectory("Preview");File.WriteAllBytes("Preview/Cylinder-"+name+".png",tex.EncodeToPNG());RenderTexture.active=null;c.targetTexture=null;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(g);
    }
}



