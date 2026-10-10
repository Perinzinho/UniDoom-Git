// Editor-only source. Run in the isolated preview project, then install the generated assets.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class DirectorRoomInteriorBuilder
{
    const string Output = "Assets/Art/DirectorOffice";
    static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
    static System.Random rng = new System.Random(1987);
    static Transform interior;

    public static void Build()
    {
        Directory.CreateDirectory(Output + "/Textures");
        Directory.CreateDirectory(Output + "/Materials");
        Directory.CreateDirectory(Output + "/Meshes");
        AssetDatabase.Refresh();
        Surface("Walnut", new Color(.30f,.16f,.075f), "wood");
        Surface("DarkWood", new Color(.15f,.075f,.04f), "wood");
        Surface("Plaster", new Color(.49f,.45f,.34f), "plaster");
        Surface("Ceiling", new Color(.37f,.35f,.28f), "plaster");
        Surface("Floor", new Color(.38f,.37f,.32f), "floor");
        Surface("Leather", new Color(.28f,.048f,.052f), "leather");
        Surface("Brass", new Color(.53f,.34f,.095f), "noise", .55f);
        Surface("Metal", new Color(.26f,.29f,.27f), "noise", .35f);
        Surface("Ivory", new Color(.63f,.58f,.43f), "noise");
        Surface("Black", new Color(.026f,.029f,.026f), "noise");
        Surface("Leaves", new Color(.13f,.24f,.10f), "noise");
        Surface("BookRed", new Color(.30f,.045f,.043f), "noise");
        Surface("BookGreen", new Color(.095f,.19f,.12f), "noise");
        Surface("BookBlue", new Color(.07f,.11f,.20f), "noise");
        Surface("BookTan", new Color(.39f,.29f,.15f), "noise");
        Surface("Rug", Color.white, "rug");
        Surface("Diploma", Color.white, "diploma");
        Surface("Portrait", Color.white, "portrait");
        Surface("Banner", Color.white, "banner");
        Surface("Window", Color.white, "window", 0, new Color(.31f,.024f,.017f));
        Surface("Screen", Color.white, "screen", 0, new Color(.13f,.36f,.10f));
        Surface("LampGlow", new Color(1,.74f,.35f), "noise", 0, new Color(2.6f,1.4f,.47f));
        var root = new GameObject("HauntedOfficeInterior"); interior = root.transform;
        var rug = Group("Persian rug", new Vector3(0,.112f,-.05f));
        Box(rug,"Woven carpet",Vector3.zero,new Vector3(4.1f,.018f,3.5f),"Rug");
        Wainscot();
        Desk();
        Chair("Director leather chair", new Vector3(0,.12f,1.84f), 0, true);
        Chair("Visitor chair left", new Vector3(-.69f,.12f,-.62f), 180, false);
        Chair("Visitor chair right", new Vector3(.72f,.12f,-.62f), 173, false);
        Bookshelf("West bookcase", new Vector3(-2.64f,.1f,.95f), -90, 1.30f);
        Bookshelf("Rear bookcase", new Vector3(-1.86f,.1f,2.16f), 0, 1.24f);
        FilingCabinet(new Vector3(-2.62f,.1f,-.69f));
        Window();
        Frame("School crest",new Vector3(-2.875f,1.99f,-.65f),-90,.68f,1.08f,"Banner");
        Frame("Diploma one",new Vector3(-1.65f,2.35f,2.365f),0,.50f,.34f,"Diploma");
        Frame("Diploma two",new Vector3(-.95f,2.47f,2.365f),0,.46f,.33f,"Diploma");
        Frame("School portrait",new Vector3(2.875f,1.8f,.30f),90,1.03f,.73f,"Portrait");
        Clock();
        Plant("Entrance fern",new Vector3(-2.45f,.11f,-1.9f),.8f);
        Plant("Portrait fern",new Vector3(2.48f,.11f,.90f),.72f);
        Plant("Shelf ivy",new Vector3(-1.86f,2.13f,2.13f),.31f);
        Trophy(); Flag(); CeilingLamp();
        LightAt("Desk warm pool",new Vector3(.70f,1.40f,.62f),new Color(1,.64f,.29f),2.1f,3.1f,true);
        LightAt("Red window spill",new Vector3(.66f,1.85f,1.97f),new Color(1,.07f,.03f),1.5f,3.1f,false);
        LightAt("Ceiling warm fill",new Vector3(0,2.65f,0),new Color(1,.78f,.49f),2.6f,7f,false);
        // Merge each furniture piece by material. Tiny books and mouldings do not remain separate renderers.
        foreach (Transform child in interior.Cast<Transform>().ToArray()) Combine(child);
        PrefabUtility.SaveAsPrefabAsset(root,Output + "/HauntedOfficeInterior.prefab");
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("DIRECTOR_OFFICE_BUILT");
    }

    static Transform Group(string name, Vector3 p, float yaw=0)
    {
        var go=new GameObject(name); go.transform.SetParent(interior,false);
        go.transform.localPosition=p; go.transform.localRotation=Quaternion.Euler(0,yaw,0);
        return go.transform;
    }
    static GameObject Shape(Transform parent,string name,Vector3 pos,Vector3 size,string material,PrimitiveType type,Vector3? rotation=null)
    {
        var go=GameObject.CreatePrimitive(type); go.name=name;
        go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=size;
        if(rotation.HasValue)go.transform.localRotation=Quaternion.Euler(rotation.Value);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().sharedMaterial=Mats[material];
        return go;
    }
    static GameObject Box(Transform p,string n,Vector3 v,Vector3 s,string m,Vector3? r=null) => Shape(p,n,v,s,m,PrimitiveType.Cube,r);
    static GameObject Cylinder(Transform p,string n,Vector3 v,Vector3 s,string m,Vector3? r=null) => Shape(p,n,v,s,m,PrimitiveType.Cylinder,r);
    static void Collision(Transform t,Vector3 center,Vector3 size)
    { var c=t.gameObject.AddComponent<BoxCollider>(); c.center=center;c.size=size; }

    static void Wainscot()
    {
        var p=Group("Wood wall panels and cornice",Vector3.zero);
        // Decoration lies inside the original walls. The doorway between x=-.6 and x=.6 stays clear.
        for(int wall=0;wall<4;wall++)
        {
            var holder=new GameObject("Panel wall "+wall).transform;holder.SetParent(p,false);
            holder.localPosition=wall==0?new Vector3(0,0,2.37f):wall==1?new Vector3(-2.87f,0,0):wall==2?new Vector3(2.87f,0,0):new Vector3(0,0,-2.37f);
            holder.localRotation=Quaternion.Euler(0,wall==1?-90:wall==2?90:wall==3?180:0,0);
            float width=wall==0||wall==3?5.8f:4.75f;
            for(float x=-width/2+.28f;x<width/2;x+=.56f)
            {
                if(wall==3&&Mathf.Abs(x)<.91f)continue;
                Box(holder,"Panel",new Vector3(x,.61f,0),new Vector3(.54f,.94f,.038f),"DarkWood");
                Box(holder,"Raised inset",new Vector3(x,.59f,-.026f),new Vector3(.41f,.73f,.029f),"Walnut");
                Box(holder,"Panel stile",new Vector3(x-.255f,.61f,-.028f),new Vector3(.036f,.98f,.055f),"Walnut");
                Box(holder,"Dado rail",new Vector3(x,1.10f,-.023f),new Vector3(.56f,.075f,.079f),"Walnut");
                Box(holder,"Skirting",new Vector3(x,.18f,-.021f),new Vector3(.56f,.11f,.072f),"Walnut");
            }
            if(wall!=3)Box(holder,"Upper cornice",new Vector3(0,2.92f,.008f),new Vector3(width,.13f,.071f),"DarkWood");
        }
    }
    static void Desk()
    {
        var p=Group("Director desk",new Vector3(0,.12f,.83f));
        Box(p,"Top",new Vector3(0,.80f,0),new Vector3(2.30f,.105f,1.0f),"Walnut");
        Box(p,"Top edge",new Vector3(0,.737f,0),new Vector3(2.23f,.032f,.95f),"Brass");
        Box(p,"Modesty panel",new Vector3(0,.39f,-.385f),new Vector3(2.15f,.69f,.10f),"DarkWood");
        for(int i=-1;i<=1;i++)
        {
            Box(p,"Carved front inset",new Vector3(i*.69f,.4f,-.444f),new Vector3(.58f,.49f,.018f),"Walnut");
            Box(p,"Inset lower edge",new Vector3(i*.69f,.135f,-.455f),new Vector3(.60f,.033f,.028f),"DarkWood");
            Box(p,"Inset upper edge",new Vector3(i*.69f,.66f,-.455f),new Vector3(.60f,.033f,.028f),"DarkWood");
        }
        for(int side=-1;side<=1;side+=2)
        {
            Box(p,"Pedestal",new Vector3(side*.82f,.39f,.09f),new Vector3(.52f,.73f,.68f),"Walnut");
            for(int d=0;d<3;d++)
            {
                Box(p,"Drawer",new Vector3(side*.82f,.18f+d*.21f,.44f),new Vector3(.46f,.19f,.045f),"DarkWood");
                Box(p,"Drawer handle",new Vector3(side*.82f,.18f+d*.21f,.474f),new Vector3(.13f,.025f,.036f),"Brass");
            }
        }
        Collision(p,new Vector3(0,.41f,0),new Vector3(2.3f,.82f,1));
        Box(p,"Leather blotter",new Vector3(-.10f,.860f,0),new Vector3(.75f,.009f,.44f),"Leather");
        for(int i=0;i<6;i++)Box(p,"Paper stack",new Vector3(-.76f,.863f+i*.016f,-.10f),new Vector3(.44f,.014f,.30f),i==0?"BookTan":"Ivory",new Vector3(0,i*2-4,0));
        Box(p,"Open ledger left",new Vector3(-.08f,.868f,-.22f),new Vector3(.22f,.025f,.24f),"Ivory",new Vector3(0,6,-3));
        Box(p,"Open ledger right",new Vector3(.15f,.868f,-.20f),new Vector3(.22f,.025f,.24f),"Ivory",new Vector3(0,6,3));
        Cylinder(p,"Pen cup",new Vector3(-.41f,.928f,.1f),new Vector3(.10f,.068f,.10f),"Black");
        for(int i=0;i<5;i++)Cylinder(p,"Pencil",new Vector3(-.44f+i*.013f,1.027f,.1f),new Vector3(.009f,.10f,.009f),"Brass",new Vector3(0,0,(i-2)*6));
        Box(p,"Name plate",new Vector3(.12f,.912f,-.40f),new Vector3(.40f,.10f,.026f),"Brass",new Vector3(-18,0,0));
        // CRT, with the screen facing the director's seat behind the desk.
        Box(p,"Computer base",new Vector3(.75f,.90f,.21f),new Vector3(.46f,.085f,.38f),"Ivory");
        Box(p,"CRT case",new Vector3(.76f,1.09f,.20f),new Vector3(.43f,.34f,.38f),"Ivory");
        Box(p,"Dark screen bezel",new Vector3(.76f,1.10f,.401f),new Vector3(.365f,.275f,.018f),"Black");
        Box(p,"Green terminal screen",new Vector3(.76f,1.10f,.413f),new Vector3(.312f,.221f,.008f),"Screen");
        Box(p,"Keyboard",new Vector3(.70f,.883f,.54f),new Vector3(.49f,.039f,.18f),"Ivory");
        for(int row=0;row<3;row++)for(int k=0;k<11;k++)Box(p,"Keyboard key",new Vector3(.49f+k*.039f,.91f,.486f+row*.043f),new Vector3(.03f,.013f,.030f),"Metal");
        Cylinder(p,"Lamp foot",new Vector3(.38f,.88f,-.10f),new Vector3(.23f,.017f,.23f),"Brass");
        Cylinder(p,"Lamp stem",new Vector3(.38f,1.10f,-.10f),new Vector3(.025f,.21f,.025f),"Brass");
        Cylinder(p,"Lamp shade",new Vector3(.38f,1.31f,-.10f),new Vector3(.38f,.047f,.28f),"Brass");
        Cylinder(p,"Lamp light diffuser",new Vector3(.38f,1.264f,-.10f),new Vector3(.32f,.004f,.235f),"LampGlow");
    }
    static void Chair(string name,Vector3 position,float angle,bool director)
    {
        var p=Group(name,position,angle);float w=director?.63f:.51f;
        Box(p,"Seat",new Vector3(0,.43f,0),new Vector3(w,.13f,.53f),"Leather");
        Box(p,"Seat wood rim",new Vector3(0,.345f,0),new Vector3(w+.04f,.045f,.57f),"Walnut");
        for(int side=-1;side<=1;side+=2)
        {
            for(int end=-1;end<=1;end+=2)Box(p,"Leg",new Vector3(side*(w/2-.05f),.17f,end*.21f),new Vector3(.055f,.34f,.055f),"Walnut");
            Box(p,"Arm",new Vector3(side*(w/2+.02f),.67f,0),new Vector3(.063f,.063f,.58f),"Walnut");
            Box(p,"Arm support",new Vector3(side*(w/2+.02f),.56f,-.20f),new Vector3(.042f,.22f,.042f),"Walnut");
        }
        float h=director?.75f:.51f;
        Box(p,"Back frame",new Vector3(0,.51f+h/2,.24f),new Vector3(w+.09f,h+.08f,.105f),"Walnut",new Vector3(7,0,0));
        Box(p,"Padded back",new Vector3(0,.51f+h/2,.179f),new Vector3(w-.02f,h,.095f),"Leather",new Vector3(7,0,0));
        if(director)for(int row=0;row<3;row++)for(int col=0;col<3;col++)
            Shape(p,"Tuft button",new Vector3((col-1)*.18f,.65f+row*.20f,.113f+row*.024f),new Vector3(.026f,.026f,.016f),"Brass",PrimitiveType.Sphere);
        Collision(p,new Vector3(0,.50f,0),new Vector3(w+.12f,1,.60f));
    }
    static void Bookshelf(string name,Vector3 position,float angle,float width)
    {
        var p=Group(name,position,angle);float height=2.04f;
        Box(p,"Back",new Vector3(0,height/2,.19f),new Vector3(width,height,.06f),"DarkWood");
        for(int s=-1;s<=1;s+=2)Box(p,"Upright",new Vector3(s*(width/2-.045f),height/2,0),new Vector3(.09f,height,.43f),"Walnut");
        for(int row=0;row<6;row++)
        {
            float y=.10f+row*.365f;
            Box(p,"Shelf",new Vector3(0,y,0),new Vector3(width,.065f,.43f),"Walnut");
            if(row==5)continue;
            float x=-width/2+.12f;
            while(x<width/2-.10f)
            {
                float bw=.045f+(float)rng.NextDouble()*.037f,bh=.22f+(float)rng.NextDouble()*.075f;
                string color=new[]{"BookRed","BookGreen","BookBlue","BookTan"}[rng.Next(4)];
                Box(p,"Book spine",new Vector3(x+bw/2,y+.04f+bh/2,-.015f),new Vector3(bw,bh,.27f),color);
                for(int band=0;band<2;band++)Box(p,"Gilt spine band",new Vector3(x+bw/2,y+.08f+band*(bh-.075f),-.155f),new Vector3(bw*.75f,.008f,.005f),"Brass");
                x+=bw+.008f;
            }
        }
        Box(p,"Crown",new Vector3(0,2.01f,-.015f),new Vector3(width+.09f,.10f,.48f),"DarkWood");
        Collision(p,new Vector3(0,height/2,0),new Vector3(width,height,.43f));
    }
    static void FilingCabinet(Vector3 position)
    {
        var p=Group("Twin steel filing cabinets",position,-90);
        for(int c=0;c<2;c++)
        {
            float x=c*.51f-.255f,h=c==0?1.47f:1.22f;
            Box(p,"Steel case",new Vector3(x,h/2,0),new Vector3(.47f,h,.48f),"Metal");
            for(int drawer=0;drawer<4;drawer++)
            {
                float y=.08f+(drawer+.5f)*(h-.13f)/4;
                Box(p,"Drawer face",new Vector3(x,y,-.249f),new Vector3(.43f,(h-.16f)/4-.016f,.027f),"Metal");
                Box(p,"Label holder",new Vector3(x,y+.045f,-.268f),new Vector3(.12f,.042f,.015f),"Brass");
                Box(p,"Paper label",new Vector3(x,y+.046f,-.279f),new Vector3(.095f,.024f,.009f),"Ivory");
                Box(p,"Pull",new Vector3(x,y-.039f,-.276f),new Vector3(.13f,.018f,.04f),"Black");
            }
        }
        Collision(p,new Vector3(0,.74f,0),new Vector3(1.0f,1.48f,.52f));
    }
    static void Window()
    {
        var p=Group("Decorative sealed window - original wall intact",new Vector3(.61f,1.96f,2.34f));
        Box(p,"Red skyline",Vector3.zero,new Vector3(1.89f,1.43f,.025f),"Window");
        for(int s=-1;s<=1;s+=2)
        {
            Box(p,"Vertical frame",new Vector3(s*.99f,0,-.018f),new Vector3(.11f,1.63f,.11f),"DarkWood");
            Box(p,"Horizontal frame",new Vector3(0,s*.76f,-.018f),new Vector3(2.06f,.11f,.11f),"DarkWood");
        }
        for(int i=0;i<15;i++)Box(p,"Blind slat",new Vector3(0,-.64f+i*.091f,-.07f),new Vector3(1.86f,.047f,.068f),"DarkWood",new Vector3(-12,0,0));
        Box(p,"Sill",new Vector3(0,-.85f,-.08f),new Vector3(2.17f,.09f,.22f),"Walnut");
    }
    static void Frame(string name,Vector3 position,float yaw,float width,float height,string face)
    {
        var p=Group(name,position,yaw);
        Box(p,"Art",Vector3.zero,new Vector3(width,height,.018f),face);
        for(int s=-1;s<=1;s+=2)
        {
            Box(p,"Frame side",new Vector3(s*(width/2+.022f),0,-.014f),new Vector3(.048f,height+.09f,.06f),"DarkWood");
            Box(p,"Frame top bottom",new Vector3(0,s*(height/2+.022f),-.014f),new Vector3(width,.048f,.06f),"Walnut");
            Box(p,"Gilt edge",new Vector3(s*(width/2-.008f),0,-.031f),new Vector3(.012f,height,.017f),"Brass");
        }
    }
    static void Clock()
    {
        var p=Group("Wall clock",new Vector3(2.855f,2.53f,.30f),90);
        Cylinder(p,"Rim",Vector3.zero,new Vector3(.48f,.024f,.48f),"Black",new Vector3(90,0,0));
        Cylinder(p,"Face",new Vector3(0,0,-.030f),new Vector3(.425f,.009f,.425f),"Ivory",new Vector3(90,0,0));
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6;
            Box(p,"Hour mark",new Vector3(Mathf.Sin(a)*.179f,Mathf.Cos(a)*.179f,-.044f),new Vector3(.014f,.035f,.007f),"Black",new Vector3(0,0,-i*30));
        }
        Box(p,"Minute hand",new Vector3(-.035f,.057f,-.05f),new Vector3(.012f,.15f,.008f),"Black",new Vector3(0,0,32));
        Box(p,"Hour hand",new Vector3(.043f,-.024f,-.058f),new Vector3(.012f,.102f,.008f),"Black",new Vector3(0,0,62));
    }
    static void Plant(string name,Vector3 position,float height)
    {
        var p=Group(name,position);
        Cylinder(p,"Pot",new Vector3(0,height*.19f,0),new Vector3(height*.44f,height*.19f,height*.44f),"Metal");
        Cylinder(p,"Soil",new Vector3(0,height*.381f,0),new Vector3(height*.40f,.008f,height*.40f),"DarkWood");
        for(int i=0;i<18;i++)
        {
            float a=i*137.5f*Mathf.Deg2Rad,rad=height*(.12f+(i%4)*.06f);
            Cylinder(p,"Stem",new Vector3(Mathf.Sin(a)*rad*.35f,height*.53f,Mathf.Cos(a)*rad*.35f),new Vector3(.012f,height*.22f,.012f),"Leaves",new Vector3(Mathf.Cos(a)*24,0,-Mathf.Sin(a)*24));
            Box(p,"Pixel foliage",new Vector3(Mathf.Sin(a)*rad,height*(.59f+(i%5)*.075f),Mathf.Cos(a)*rad),new Vector3(height*.14f,height*.035f,height*.32f),"Leaves",new Vector3(15+i%3*13,i*137.5f,i%2*22));
        }
        if(height>.5f)Collision(p,new Vector3(0,height*.2f,0),new Vector3(height*.45f,height*.4f,height*.45f));
    }
    static void Trophy()
    {
        var p=Group("School trophy",new Vector3(-2.22f,2.14f,2.13f));
        Box(p,"Base",Vector3.zero,new Vector3(.20f,.058f,.17f),"DarkWood");
        Cylinder(p,"Stem",new Vector3(0,.08f,0),new Vector3(.035f,.062f,.035f),"Brass");
        Shape(p,"Cup",new Vector3(0,.18f,0),new Vector3(.16f,.15f,.16f),"Brass",PrimitiveType.Sphere);
        Cylinder(p,"Lip",new Vector3(0,.24f,0),new Vector3(.18f,.018f,.18f),"Brass");
    }
    static void Flag()
    {
        var p=Group("School standard",new Vector3(2.50f,.12f,1.98f));
        Cylinder(p,"Foot",new Vector3(0,.035f,0),new Vector3(.36f,.035f,.36f),"Black");
        Cylinder(p,"Pole",new Vector3(0,1.1f,0),new Vector3(.025f,1.1f,.025f),"Brass");
        Shape(p,"Finial",new Vector3(0,2.23f,0),new Vector3(.065f,.10f,.065f),"Brass",PrimitiveType.Sphere);
        Box(p,"Cloth",new Vector3(-.22f,1.53f,0),new Vector3(.42f,1.16f,.018f),"Banner");
        for(int i=0;i<9;i++)Box(p,"Gold fringe",new Vector3(-.02f-i*.049f,.924f,0),new Vector3(.018f,.083f,.023f),"Brass");
    }
    static void CeilingLamp()
    {
        var p=Group("Suspended fluorescent fixture",new Vector3(0,2.85f,-.07f));
        Box(p,"Housing",Vector3.zero,new Vector3(1.17f,.10f,.43f),"Metal");
        Box(p,"Underside",new Vector3(0,-.056f,0),new Vector3(1.10f,.023f,.37f),"Black");
        for(int i=0;i<3;i++)Cylinder(p,"Warm tube",new Vector3(0,-.084f,(i-1)*.12f),new Vector3(.044f,.52f,.044f),"LampGlow",new Vector3(0,0,90));
        for(int s=-1;s<=1;s+=2)Cylinder(p,"Suspension cable",new Vector3(s*.43f,1.105f,0),new Vector3(.008f,1.07f,.008f),"Black");
    }
    static void LightAt(string name,Vector3 pos,Color color,float intensity,float range,bool shadows)
    {
        var p=Group(name,pos);var l=p.gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;
        l.shadows=shadows?LightShadows.Soft:LightShadows.None;l.shadowBias=.03f;l.shadowNormalBias=.15f;
        p.gameObject.AddComponent<UniversalAdditionalLightData>();
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
            AssetDatabase.CreateAsset(mesh,Output+"/Meshes/"+safe+".asset");
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
        texture.SetPixels(pixels);texture.Apply();string path=Output+"/Textures/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
        mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",metallic>0?.28f:.09f);mat.SetFloat("_Metallic",metallic);
        if(emission.HasValue){mat.EnableKeyword("_EMISSION");mat.SetTexture("_EmissionMap",mat.GetTexture("_BaseMap"));mat.SetColor("_EmissionColor",emission.Value);mat.globalIlluminationFlags=MaterialGlobalIlluminationFlags.RealtimeEmissive;}
        if(name=="Floor")mat.SetTextureScale("_BaseMap",new Vector2(3,3));
        if(name=="Plaster")mat.SetTextureScale("_BaseMap",new Vector2(3,2));
        AssetDatabase.CreateAsset(mat,Output+"/Materials/"+name+".mat");Mats[name]=mat;
    }

    public static void VerifyAndRender()
    {
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Scenario/DirectorRoom.prefab");
        var room=(GameObject)PrefabUtility.InstantiatePrefab(asset);room.transform.position=Vector3.zero;
        if(!room.transform.Find("HauntedOfficeInterior"))throw new Exception("Missing interior instance");
        foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>())
        {
            foreach(var material in renderer.sharedMaterials)if(!material||!material.shader||material.shader.name=="Hidden/InternalErrorShader")throw new Exception("Missing material: "+renderer.name);
            if(renderer.transform.IsChildOf(room.transform.Find("HauntedOfficeInterior"))&&!renderer.GetComponent<MeshFilter>().sharedMesh)throw new Exception("Missing generated mesh");
        }
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.17f,.15f,.11f);RenderSettings.fog=false;
        RenderPreview(room,"Preview/DirectorRoom.png");
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,"Assets/Art/DirectorOffice/DirectorOfficePreview.unity");
        var colliders=room.transform.Find("HauntedOfficeInterior").GetComponentsInChildren<Collider>();
        // Door approach must remain unobstructed, using the original south-wall opening.
        foreach(var collider in colliders)
        {
            Vector3 local=room.transform.InverseTransformPoint(collider.bounds.center);
            if(Mathf.Abs(local.x)<.61f&&local.z< -1.2f)throw new Exception("Door corridor obstructed by "+collider.name);
        }
        Debug.Log("DIRECTOR_OFFICE_VERIFIED renderers="+room.transform.Find("HauntedOfficeInterior").GetComponentsInChildren<Renderer>().Length+" colliders="+colliders.Length);
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var mapRoom=Object.FindObjectsByType<Transform>().First(t=>t.name=="DirectorRoom"&&t.parent==null).gameObject;
        if(!mapRoom.transform.Find("HauntedOfficeInterior"))throw new Exception("Interior not inherited by map instance");
        RenderPreview(mapRoom,"Preview/DirectorRoomInMap.png");
        Debug.Log("DIRECTOR_OFFICE_MAP_INSTANCE_VERIFIED");
        AssetDatabase.SaveAssets();
    }
    static void RenderPreview(GameObject room,string path)
    {
        var cameraGo=new GameObject("Office preview camera");var camera=cameraGo.AddComponent<Camera>();
        cameraGo.AddComponent<UniversalAdditionalCameraData>();camera.fieldOfView=66;camera.nearClipPlane=.04f;camera.farClipPlane=45;
        camera.transform.position=room.transform.TransformPoint(new Vector3(2.44f,1.65f,-2.12f));
        camera.transform.LookAt(room.transform.TransformPoint(new Vector3(-.42f,1.16f,1.04f)));
        camera.backgroundColor=new Color(.028f,.02f,.016f);camera.clearFlags=CameraClearFlags.SolidColor;
        var target=new RenderTexture(1440,1080,24);camera.targetTexture=target;
        // Warm the newly loaded URP materials before capturing the frame.
        for(int frame=0;frame<3;frame++)camera.Render();
        RenderTexture.active=target;
        var picture=new Texture2D(1440,1080,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,1440,1080),0,0);picture.Apply();
        Directory.CreateDirectory("Preview");File.WriteAllBytes(path,picture.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(picture);Object.DestroyImmediate(target);
    }
}
