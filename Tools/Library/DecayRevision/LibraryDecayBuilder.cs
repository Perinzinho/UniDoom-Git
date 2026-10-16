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

public static partial class LibraryDecayBuilder
{
    static Mesh fanMesh;
    public static void Build()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use isolated LibraryPreview");
        foreach(var path in new[]{MeshDir,MatDir,Art+"/Profiles/DecayRevision",Art+"/Reflections/DecayRevision",Evidence})Directory.CreateDirectory(path);
        AssetDatabase.Refresh();rng=new System.Random(72149);meshSerial=0;meshCache.Clear();mats.Clear();Materials();PrepareFan();
        var root=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try
        {
            var old=root.transform.Find("Abandoned Library Interior");if(!old)throw new Exception("Unexpected library baseline");Object.DestroyImmediate(old.gameObject);
            foreach(Transform t in root.transform)
            {
                var r=t.GetComponent<MeshRenderer>();if(r)r.sharedMaterial=mats[t.name=="Floor"?"Floor":"Mold"];
            }
            var a=Group(root.transform,"Abandoned Library Interior",LibraryBuilder.FloorOrigin);a.localScale=V(1/LibraryBuilder.MapScale.x,1/LibraryBuilder.MapScale.y,1);
            Structure(a);Collections(a);Furniture(a);Windows(a);Ceiling(a);Quarantine(a);Debris(a);RuinAccents(a);Atmosphere(a);
            PrefabUtility.SaveAsPrefabAsset(root,LibraryBuilder.Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();BakeReflections();AssetDatabase.SaveAssets();Debug.Log("LIBRARY_DECAY_BUILD_PASS");LibraryDecayVerifier.VerifyAndRender();
    }
    static void PrepareFan()
    {
        string path=Art+"/Models/DecayRevision/ceiling_fan/model.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.importAnimation=false;importer.importLights=false;importer.importCameras=false;importer.SaveAndReimport();
        var source=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));source.transform.position=Vector3.zero;
        var rs=source.GetComponentsInChildren<MeshRenderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
        float scale=1.8f/bounds.size.x;var matrix=Matrix4x4.TRS(-V(bounds.center.x,bounds.max.y,bounds.center.z)*scale,Quaternion.identity,Vector3.one*scale);
        var instances=new List<CombineInstance>();foreach(var f in source.GetComponentsInChildren<MeshFilter>())for(int i=0;i<f.sharedMesh.subMeshCount;i++)instances.Add(new CombineInstance{mesh=f.sharedMesh,subMeshIndex=i,transform=matrix*f.transform.localToWorldMatrix});
        var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(instances.ToArray());fanMesh=Save(mesh,"AbandonedFan");Object.DestroyImmediate(source);
    }
    static void Structure(Transform a)
    {
        var floor=Group(a,"Cracked institutional tile floor",Vector3.zero);
        for(float x=-12.175f;x<12.17f;x+=5)for(float z=-19.7f;z<19.69f;z+=5)
        {
            float w=Mathf.Min(5,12.175f-x),d=Mathf.Min(5,19.7f-z);var p=V(x+w/2,0,z+d/2);
            Box(floor,"Weathered tile section",p-V(0,.014f,0),V(w,.025f,d),"Floor");
            Box(a,"Cracked concrete ceiling slab",p+V(0,4.43f,0),V(w,.10f,d),"Mold");
        }
        foreach(float z in new[]{-17f,-9f,-1f,7f,15f})Box(a,"Exposed concrete crossbeam",V(0,4.22f,z),V(24.2f,.36f,.4f),"Mold");
        foreach(float x in new[]{-3.8f,3.8f})
        {
            Box(a,"Load bearing concrete beam",V(x,4.19f,0),V(.48f,.42f,39.4f),"Mold");
            foreach(float z in new[]{-13f,-5f,3f,11f})
            {
                var column=Group(a,"Spalled concrete column",V(x,0,z));Column(column);Box(column,"Damp column foot",V(0,.11f,0),V(.93f,.22f,.93f),"DamagedConcrete");
                if(x>0&&z==11){NoticeBoard(column);BloodMark(column,V(-.20f,.75f,.471f),new Vector2(.48f,1.0f));}
                if(x<0&&z==-5)BloodMark(column,V(.08f,1.1f,.471f),new Vector2(.60f,.87f));
            }
        }
        foreach(float x in new[]{-12.12f,12.12f})
        {
            Box(a,"Peeling institutional wall",V(x,2.2f,0),V(.12f,4.4f,39.5f),"BrokenPlaster");
            Box(a,"Damp painted dado",V(x-Mathf.Sign(x)*.08f,.40f,0),V(.09f,.80f,39.5f),"Paint");
            Box(a,"Missing and rusted skirting",V(x-Mathf.Sign(x)*.14f,.055f,0),V(.05f,.11f,39.5f),"Rust");
        }
        Box(a,"Rear wall",V(0,2.2f,-19.65f),V(24.3f,4.4f,.16f),"BrokenPlaster");
        Box(a,"Entry transom",V(10.15f,3.65f,19.77f),V(3.6f,1.4f,.23f),"Mold");
        Box(a,"Entry reveal",V(8.4f,1.4f,19.75f),V(.20f,2.8f,.32f),"Mold");
        var entry=Group(a,"Damaged library entrance sign",V(10.1f,3.10f,19.58f));entry.localRotation=Quaternion.Euler(0,180,0);
        Sign(entry,"BIBLIOTECA",Vector3.zero,new Vector2(2.8f,.38f),.25f);
        var s=Group(a,"Suspended periodicals sign",V(-1.8f,3.40f,7.0f));Sign(s,"Periódicos   →",Vector3.zero,new Vector2(2.0f,.38f),.23f);
        foreach(float x in new[]{-.83f,.83f})Cable(s,"Suspension wire",new[]{V(x,.2f,0),V(x,1,0)},.009f,"Iron");
    }
    static void Column(Transform p)
    {
        var ring=new[]{new Vector2(-.40f,-.45f),new Vector2(.40f,-.45f),new Vector2(.45f,-.40f),new Vector2(.45f,.40f),new Vector2(.40f,.45f),new Vector2(-.40f,.45f),new Vector2(-.45f,.40f),new Vector2(-.45f,-.40f)};
        var v=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
        for(int i=0;i<ring.Length;i++)
        {
            var aa=ring[i];var bb=ring[(i+1)%ring.Length];int start=v.Count;
            v.AddRange(new[]{V(aa.x,0,aa.y),V(bb.x,0,bb.y),V(bb.x,4.4f,bb.y),V(aa.x,4.4f,aa.y)});
            float width=Vector2.Distance(aa,bb);uv.AddRange(new[]{Vector2.zero,new Vector2(width*.55f,0),new Vector2(width*.55f,2.42f),new Vector2(0,2.42f)});tr.AddRange(new[]{start,start+2,start+1,start,start+3,start+2});
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh=Save(mesh,"BeveledColumn_"+meshSerial++);Draw(p,"Mold and chipped concrete",mesh,"Mold");
        var collider=p.gameObject.AddComponent<BoxCollider>();collider.center=V(0,2.2f,0);collider.size=V(.90f,4.4f,.90f);
    }
    static void Collections(Transform a)
    {
        int index=0;
        foreach(float z in new[]{-17f,-14.3f,-11.6f,-8.9f,-6.2f,-3.5f,-.8f,1.9f,4.6f,7.3f,10f,12.7f,15.4f,18.1f})Shelf(a,V(-11.5f,0,z),-90,index++);
        foreach(float center in new[]{-12f,-4f,4f,12f})foreach(float dz in new[]{-1.72f,0,1.72f})
        {
            var g=Shelf(a,V(-8.0f,0,center+dz),-90,index++);
            if(center==12&&dz==0)
            {
                g.name="Collapsed reference bookcase";g.localRotation=Quaternion.AngleAxis(-12,Vector3.forward)*Quaternion.Euler(0,-90,0);Ground(a,g,.025f);
                var hazard=g.GetComponent<BoxCollider>();if(hazard)hazard.size=V(1.65f,2.55f,.70f);
            }
        }
        foreach(float z in new[]{-15f,-10f,-5f,0,5f,10f,13.8f})Shelf(a,V(6.1f,0,z),90,index++);
        foreach(float z in new[]{-12f,-7f,-2f,3f,8f,13f})Shelf(a,V(-4.8f,0,z),-90,index++);
        foreach(float x in new[]{-10,-7,-4,0,3,6,9})Shelf(a,V(x,0,-19.12f),180,index++);
        Sign(a,"Ciências Humanas",V(6.1f,2.86f,10),new Vector2(1.7f,.26f),.17f);
    }
    static Transform Shelf(Transform a,Vector3 pos,float yaw,int index)
    {
        var g=Group(a,"Neglected collection "+index,pos);g.localRotation=Quaternion.Euler(0,yaw,0);
        Model(g,"wooden_bookshelf_worn","Warped timber bookcase",Vector3.zero,scale:Vector3.one*1.35f);
        float[] heights={.105f,.36f,.615f,.87f,1.17f,1.515f};
        for(int i=0;i<heights.Length;i++)
        {
            if((index*7+i*3)%13<3)continue;
            var books=Model(g,"book_encyclopedia_set_01","Uncatalogued dusty volumes",V(R(-.05f,.05f),heights[i]*1.35f+.005f,-.05f),V(0,0,0),V(R(1.65f,2.0f),R(.83f,.98f),1));
        }
        var c=g.gameObject.AddComponent<BoxCollider>();c.center=V(0,1.27f,0);c.size=V(1.72f,2.54f,.72f);
        if(index%5==0)Sign(g,index%2==0?"Referência":"Acervo Geral",V(0,2.34f,-.37f),new Vector2(1.4f,.23f),.16f,180);
        if(index%9==3)
        {
            for(int j=0;j<3;j++)Box(g,"Splintered shelf",V(R(-.65f,.65f),.10f+j*.035f,-.52f),V(R(.4f,.9f),.035f,.10f),"Wood",euler:V(R(-9,9),R(-30,30),R(-10,10)));
        }
        return g;
    }
    static void Furniture(Transform a)
    {
        int i=0;foreach(float z in new[]{-9f,-1f,7f})Desk(a,V(-2.7f,0,z),i++);
        foreach(float z in new[]{-9f,-1f,7f,13.4f})Desk(a,V(2.8f,0,z),i++);
        foreach(float z in new[]{-13f,-5f,3f,11f})Desk(a,V(9.0f,0,z),i++);
        Model(a,"drawer_cabinet","Rusted card index",V(5.2f,0,18.0f),V(0,180,0),collision:true);
        var reception=Group(a,"Abandoned returns desk",V(6.9f,0,17.8f));Model(reception,"wooden_table_02","Library returns counter",Vector3.zero,scale:V(1.8f,1,1.25f),collision:true);
        Books(reception,V(-.35f,.8f,0),7);Papers(reception,V(.35f,.8f,0),5,.48f);Sign(a,"DEVOLUÇÕES",V(6.9f,1.2f,17.4f),new Vector2(1.2f,.24f),.16f);
        var cart=Group(a,"Abandoned book trolley",V(10.6f,0,-1.0f));
        foreach(float y in new[]{.23f,.70f})Box(cart,"Trolley tray",V(0,y,0),V(.68f,.05f,.92f),"Rust");
        foreach(float x in new[]{-.31f,.31f})foreach(float z in new[]{-.43f,.43f})Box(cart,"Tubular trolley frame",V(x,.55f,z),V(.025f,1.05f,.025f),"Iron");
        Box(cart,"Trolley handle",V(0,1.08f,.43f),V(.66f,.027f,.027f),"Iron");Books(cart,V(0,.735f,0),5);Batch(cart,"Trolley");
        foreach(var pos in new[]{V(-4.5f,0,5.5f),V(9.6f,0,-8.5f),V(1.9f,0,-4.3f)})
        {
            var chair=Model(a,"dining_chair_02","Overturned reading chair",pos,V(0,R(-30,30),82));Ground(a,chair,.03f);
            var b=LibraryBuilder.BoundsIn(chair,chair.GetComponentsInChildren<Renderer>());var c=chair.gameObject.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;
        }
    }
    static void Desk(Transform a,Vector3 pos,int index)
    {
        var g=Group(a,"Abandoned reading desk "+index,pos);g.localRotation=Quaternion.Euler(0,index%3==0?-3:2,0);
        Model(g,"wooden_table_02","Worn reading table",Vector3.zero,scale:V(2.18f,1,1.6f),collision:true);
        Model(g,"dining_chair_02","Pulled out chair",V(-.70f,0,-1.05f),V(0,R(-18,18),0),collision:true);
        if(index%3!=1)Model(g,"dining_chair_02","Discarded reader chair",V(.68f,0,1.02f),V(0,R(160,195),0),collision:true);
        Books(g,V(R(-.7f,-.2f),.795f,R(-.2f,.2f)),3+index%4);Papers(g,V(.28f,.80f,.03f),9,.8f);
        var lamp=Group(g,"Dead desk lamp",V(.45f,.80f,.1f));
        Box(lamp,"Lamp foot",V(0,.015f,0),V(.22f,.03f,.15f),"Iron");Cable(lamp,"Bent lamp stem",new[]{V(0,.03f,0),V(.02f,.22f,0),V(-.10f,.34f,.01f)},.018f,"Rust");
        Box(lamp,"Enamel reflector",V(-.11f,.36f,.01f),V(.25f,.08f,.18f),"Paint",euler:V(0,0,index%3==0?17:-10));
        if(index==2||index==7)
        {
            Box(lamp,"Surviving lamp bulb",V(-.11f,.315f,.01f),V(.13f,.02f,.08f),"Amber");Light(lamp,"Weak amber desk light",V(-.1f,.29f,0),C(1,.62f,.31f),1.4f,3);
        }
        else if(index%3==0){lamp.localRotation=Quaternion.Euler(0,15,75);Ground(g,lamp,.81f);}
        Batch(g,"DeskDetail"+index);
    }
    static void Books(Transform p,Vector3 pos,int count)
    {
        float y=pos.y;
        for(int i=0;i<count;i++)
        {
            var book=Model(p,"single_book","Water damaged loose volume",V(pos.x+R(-.15f,.15f),0,pos.z+R(-.1f,.1f)),V(0,R(-30,30),90),Vector3.one*R(1.0f,1.35f));
            Ground(p,book,y);y+=LibraryBuilder.BoundsIn(book,book.GetComponentsInChildren<Renderer>()).size.x*book.localScale.x+.003f;
            // Local X becomes vertical after laying the book onto its broad cover.
        }
    }
    static void Papers(Transform p,Vector3 center,int count,float spread)
    {
        for(int i=0;i<count;i++)
        {
            var g=Group(p,"Damp discarded document",center+V(R(-spread,spread),.0015f*i,R(-spread*.6f,spread*.6f)));g.localRotation=Quaternion.Euler(0,R(-180,180),0);
            float w=R(.17f,.25f),d=R(.23f,.33f);Box(g,"Paper fibers",Vector3.zero,V(w,.0015f,d),i%4==0?"Pages":"Paper");
            for(int line=0;line<7;line++)Box(g,"Faded print",V(-.006f,.0011f,-d*.36f+line*d*.105f),V(w*R(.52f,.83f),.0005f,.002f),"Ink");
        }
    }
    static void Windows(Transform a)
    {
        foreach(float z in new[]{-14f,-6f,2f,10f})
        {
            var g=Group(a,"Broken ivy covered daylight window",V(11.90f,.88f,z));g.localRotation=Quaternion.Euler(0,-90,0);const float width=3.7f,height=3.25f;
            var outside=Face(g,"Overgrown courtyard beyond",V(0,height*.5f,0),new Vector2(width,height),"Sky");
            var image=outside.GetComponentInChildren<MeshFilter>();var crop=Object.Instantiate(image.sharedMesh);var uv=crop.uv;
            float start=.10f+(z+14)/8*.105f;for(int j=0;j<uv.Length;j++)uv[j]=new Vector2(start+uv[j].x*.15f,.29f+uv[j].y*.35f);crop.uv=uv;crop=Save(crop,"CourtyardUV_"+meshSerial++);image.sharedMesh=crop;
            // Bare branches and ivy overlap the far glazing, creating a courtyard silhouette.
            for(int b=0;b<4;b++)
            {
                float x=-1.5f+b*.9f;Cable(g,"Distant leafless branch",new[]{V(x,0,.03f),V(x+.15f,1.1f,.035f),V(x-.1f,2.2f,.03f),V(x+.3f,3.2f,.03f)},.025f,"Wood");
                Face(g,"Invasive ivy beyond glass",V(x,2.8f,.045f),new Vector2(.8f,1.0f),"Ivy");
            }
            for(int ix=0;ix<4;ix++)for(int iy=0;iy<3;iy++)
            {
                float x=-width*.5f+(ix+.5f)*width/4,y=(iy+.5f)*height/3;
                if((ix+iy+(int)z+20)%3!=0)
                    Polygon(g,"Jagged glass remnant",new[]{V(x-.43f,y-.50f,.18f),V(x+.43f,y-.50f,.18f),V(x+.43f,y+.20f,.18f),V(x+.13f,y+.48f,.18f),V(x-.05f,y+.17f,.18f),V(x-.43f,y+.50f,.18f)},"Glass");
            }
            foreach(float x in new[]{-width/2,-width/4,0,width/4,width/2})Box(g,"Corroded window upright",V(x,height*.5f,.23f),V(.045f,height+.16f,.12f),"Paint");
            for(int y=0;y<=3;y++)Box(g,"Window crossbar",V(0,y*height/3,.23f),V(width+.15f,.05f,.14f),"Paint");
            Box(g,"Spalled window sill",V(0,-.04f,.20f),V(width+.38f,.13f,.48f),"Mold");
            Light(g,"Diffuse overcast window light",V(0,1.7f,.36f),C(.67f,.75f,.80f),38,10,V(-1,1,6),shadow:z==2,angle:125);
            for(int i=0;i<4;i++)Face(g,"Ivy entering the broken frame",V(R(-1.6f,1.6f),R(2.2f,3.25f),.28f),new Vector2(R(.35f,.65f),R(.5f,1.1f)),"Ivy",V(0,0,R(-25,25)));
        }
    }
    static void Ceiling(Transform a)
    {
        int index=0;foreach(float z in new[]{-15f,-7f,1f,9f,17f})foreach(float x in new[]{-1.4f,7.4f})
        {
            var g=Group(a,"Damaged fluorescent fixture "+index,V(x,index==6?3.80f:4.01f,z));if(index==6)g.localRotation=Quaternion.Euler(0,0,11);
            Box(g,"Corroded fluorescent housing",Vector3.zero,V(.38f,.10f,1.65f),"Rust");
            bool lit=index==0||index==2||index==5||index==6||index==8;
            foreach(float side in new[]{-.11f,0,.11f})Box(g,"Fluorescent tube",V(side,-.07f,0),V(.035f,.035f,1.46f),lit?"ColdTube":"DeadTube");
            if(lit)Light(g,"Flicker scarred cold light",V(0,-.14f,0),C(.79f,.84f,.82f),38,8,V(0,-3.7f,0),shadow:index==2||index==6||index==8,angle:112);
            for(int j=0;j<3;j++)
            {
                float xx=-.14f+j*.13f;Cable(g,"Loose fluorescent wiring",new[]{V(xx,.36f,-.4f+j*.26f),V(xx+.07f,.04f,-.2f+j*.23f),V(xx+.13f,-R(.25f,.80f),.12f+j*.15f),V(xx+.08f,-.12f,.5f)},.008f,"Iron");
            }
            index++;
        }
        foreach(float z in new[]{-11f,1f,13f})
        {
            var fan=Group(a,"Seized ceiling fan",V(.2f,3.96f,z));fan.localRotation=Quaternion.Euler(0,R(0,180),z==1?4:0);Draw(fan,"Detailed CC0 fan",fanMesh,"Fan");
            Cable(a,"Fan hanging rod",new[]{V(.2f,4.43f,z),V(.2f,3.96f,z)},.026f,"Rust");
        }
        foreach(float x in new[]{-10.5f,4.4f})Cable(a,"Sagging ceiling conduit",new[]{V(x,4.39f,-19),V(x+.2f,4.13f,-10),V(x-.25f,3.82f,0),V(x+.18f,4.12f,10),V(x,4.39f,19)},.018f,"Iron");
    }
    static void Sign(Transform p,string text,Vector3 pos,Vector2 size,float fontHeight,float yaw=0)
    {
        var g=Group(p,"Aged sign "+text,pos);g.localRotation=Quaternion.Euler(0,yaw,0);
        Box(g,"Dented sign backing",Vector3.zero,V(size.x,size.y,.035f),"Iron");Box(g,"Painted sign face",V(0,0,.022f),V(size.x-.04f,size.y-.04f,.01f),"Paint");
        Label(g,text,V(0,0,.032f),fontHeight,"SignLetters");
    }
    static void NoticeBoard(Transform column)
    {
        var g=Group(column,"Quarantine notice board",V(0,1.80f,.475f));
        Box(g,"Rusted notice board frame",Vector3.zero,V(1.06f,1.43f,.06f),"Rust");Box(g,"Stained cork backing",V(0,0,.035f),V(.96f,1.33f,.015f),"Wood");
        Sign(g,"BIBLIOTECA",V(0,.87f,0),new Vector2(1.15f,.27f),.19f);
        for(int i=0;i<5;i++)
        {
            float x=-.32f+(i%3)*.30f,y=.42f-(i/3)*.45f;Box(g,"Torn old catalogue notice",V(x,y,.052f),V(.25f,.39f,.004f),"Paper",euler:V(0,0,R(-9,9)));
            for(int line=0;line<8;line++)Box(g,"Illegible aged type",V(x,y+.13f-line*.033f,.056f),V(.19f-(line%3)*.02f,.006f,.002f),"Ink");
        }
        Box(g,"Quarantine declaration",V(.03f,-.29f,.068f),V(.69f,.56f,.004f),"Paper",euler:V(0,0,-5));
        Label(g,"QUARENTENA",V(.03f,-.12f,.074f),.13f,"RedInk");Label(g,"ÁREA INTERDITADA\nRISCO BIOLÓGICO",V(.03f,-.34f,.074f),.069f,"Ink");
        // Three broken rings and a central ring form the printed biological hazard symbol.
        var symbol=Group(g,"Faded biological hazard emblem",V(.03f,-.48f,.077f));
        for(int arm=0;arm<3;arm++)
        {
            float angle=(90+arm*120)*Mathf.Deg2Rad;
            var center=V(Mathf.Cos(angle)*.046f,Mathf.Sin(angle)*.046f,0);
            Arc(symbol,center,.045f,.011f,angle+Mathf.PI*.25f,Mathf.PI*1.5f);
        }
        Arc(symbol,Vector3.zero,.021f,.008f,0,Mathf.PI*2);
    }
    static void Arc(Transform p,Vector3 center,float radius,float width,float start,float length)
    {
        const int segments=24;var vertices=new Vector3[(segments+1)*2];var triangles=new int[segments*6];
        for(int i=0;i<=segments;i++){float angle=start+length*i/segments;var dir=V(Mathf.Cos(angle),Mathf.Sin(angle),0);vertices[i*2]=center+dir*(radius-width/2);vertices[i*2+1]=center+dir*(radius+width/2);}
        for(int i=0;i<segments;i++){int v=i*2,t=i*6;triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v+1;triangles[t+4]=v+3;triangles[t+5]=v+2;}
        var mesh=new Mesh();mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();Draw(p,"Worn printed arc",Save(mesh,"HazardArc_"+meshSerial++),"RedInk");
    }
    static void Quarantine(Transform a)
    {
        var g=Group(a,"Barricaded archive door",V(11.90f,0,17f));g.localRotation=Quaternion.Euler(0,-90,0);
        Box(g,"Rotted archive door",V(0,1.4f,.08f),V(2.0f,2.8f,.16f),"Wood",true);
        foreach(float y in new[]{.6f,1.8f})Box(g,"Hastily nailed door barricade",V(0,y,.21f),V(2.4f,.15f,.055f),"Shelf",euler:V(0,0,y<1?16:-22));
        Box(g,"Torn keep out notice",V(0,1.8f,.27f),V(1.0f,.70f,.01f),"Paper",euler:V(0,0,7));Label(g,"NÃO\nENTRE",V(0,1.8f,.28f),.29f,"RedInk");
        Tape(g,V(0,.9f,.32f),25,2.6f);Tape(g,V(0,2.35f,.32f),-23,2.6f);
        Label(g,"AQUI JÁ NÃO\nHÁ AJUDA",V(1.45f,1.50f,.04f),.13f,"Ink");
        Light(g,"Failing archive warning lamp",V(-1.15f,2.50f,.40f),C(1,.43f,.19f),1.0f,3.5f);Box(g,"Weak warning bulb",V(-1.15f,2.50f,.35f),V(.08f,.17f,.08f),"Amber");
        BloodMark(g,V(-.62f,.9f,.175f),new Vector2(.50f,1.3f));
    }
    static void Tape(Transform p,Vector3 pos,float angle,float length)
    {
        var g=Group(p,"Old quarantine tape",pos);g.localRotation=Quaternion.Euler(0,0,angle);Box(g,"Faded yellow cloth tape",Vector3.zero,V(length,.12f,.009f),"Hazard");
        for(float x=-length/2+.08f;x<length/2-.08f;x+=.20f)Box(g,"Black caution stripe",V(x,0,.006f),V(.07f,.125f,.003f),"Iron",euler:V(0,0,-25));Batch(g,"Tape");
    }
    static void BloodMark(Transform p,Vector3 pos,Vector2 size)
    {
        var g=Group(p,"Old bloody hand transfer",pos);g.localScale=V(size.x,size.y,1);g.localRotation=Quaternion.Euler(0,0,R(-12,12));
        void Oval(string name,float x,float y,float rx,float ry)
        {
            var outline=new Vector3[12];for(int i=0;i<outline.Length;i++){float angle=i*Mathf.PI*2/outline.Length;outline[i]=V(x+Mathf.Cos(angle)*rx*R(.88f,1.04f),y+Mathf.Sin(angle)*ry*R(.92f,1.05f),0);}
            Polygon(g,name,outline,"DriedBlood");
        }
        Oval("Palm transfer",0,.10f,.18f,.115f);
        for(int i=0;i<4;i++)Oval("Finger transfer",-.13f+i*.082f,.28f-R(0,.04f),.025f,.13f);
        Oval("Thumb transfer",-.20f,.11f,.08f,.035f);
        for(int i=0;i<5;i++){float x=R(-.17f,.17f),length=R(.12f,.52f);Oval("Downward blood run",x,-length*.5f,.004f+R(0,.004f),length*.5f);}
        Batch(g,"Handprint");
    }
    static void Debris(Transform a)
    {
        // Localized collapse heaps keep the established entry, central and west paths free.
        foreach(var center in new[]{V(-9.2f,0,12),V(-9.8f,0,3),V(-10.4f,0,-11),V(10.4f,0,-8),V(10.5f,0,7),V(4.7f,0,-15),V(-4.5f,0,-17),V(-4.6f,0,10.5f),V(-4.6f,0,5f),V(1.9f,0,10.4f)})
        {
            var g=Group(a,"Accumulated books plaster and splinters",center);
            Papers(g,V(0,.034f,0),38,1.0f);
            for(int i=0;i<16;i++)
            {
                var book=Model(g,"single_book","Collapsed book pile",V(R(-.95f,.95f),0,R(-1.5f,1.5f)),V(R(-14,14),R(0,360),90+R(-15,15)),Vector3.one*R(1,1.4f));Ground(g,book,R(.04f,.15f));
            }
            for(int i=0;i<26;i++)Box(g,"Crumbled plaster fragment",V(R(-1,1),R(.045f,.1f),R(-1.5f,1.5f)),V(R(.07f,.25f),R(.025f,.07f),R(.05f,.19f)),"BrokenPlaster",euler:V(R(-9,9),R(0,180),R(-10,10)));
            for(int i=0;i<9;i++)Box(g,"Split wet timber",V(R(-.9f,.9f),R(.05f,.13f),R(-1.4f,1.4f)),V(R(.35f,.95f),R(.015f,.045f),R(.05f,.12f)),"Wood",euler:V(R(-9,9),R(0,180),R(-4,4)));
            Batch(g,"Collapse");
        }
        for(int i=0;i<20;i++)
        {
            var g=Group(a,"Wind blown floor papers",V(R(-5.6f,4.7f),0,R(-18,18)));Papers(g,V(0,.025f,0),6,.8f);Batch(g,"FloorPaper");
        }
        foreach(var p in new[]{V(2.1f,0,7),V(1.9f,0,-1),V(8.8f,0,10),V(9.8f,0,1),V(10,0,-6),V(-.4f,0,-12),V(-5.0f,0,4)})Puddle(a,p,R(.6f,1.5f),R(.7f,2.3f));
        foreach(float z in new[]{-14f,-6f,2f,10f})
        {
            var g=Group(a,"Leaking sill debris",V(10.7f,0,z));
            for(int i=0;i<12;i++)Polygon(g,"Glass shard",new[]{V(R(-.7f,.7f),.035f,R(-1,1)),V(R(-.7f,.7f),.036f,R(-1,1)),V(R(-.7f,.7f),.037f,R(-1,1))},"Glass",true);
            Batch(g,"Glass");
        }
    }
    public static void Polish()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use the isolated project");
        rng=new System.Random(72149);mats.Clear();meshCache.Clear();
        foreach(var path in Directory.GetFiles(MatDir,"*.mat")){var m=AssetDatabase.LoadAssetAtPath<Material>(path);mats[m.name]=m;}
        var prefab=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try
        {
            var a=prefab.transform.Find("Abandoned Library Interior");var old=a.Find("Localized collapse accents");if(old)Object.DestroyImmediate(old.gameObject);
            RuinAccents(a);PrefabUtility.SaveAsPrefabAsset(prefab,LibraryBuilder.Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        AssetDatabase.SaveAssets();BakeReflections();AssetDatabase.SaveAssets();Debug.Log("LIBRARY_DECAY_BUILD_PASS");LibraryDecayVerifier.VerifyAndRender();
    }
    static void RuinAccents(Transform a)
    {
        meshSerial=10000;var accents=Group(a,"Localized collapse accents",Vector3.zero);
        foreach(var center in new[]{V(-1.25f,0,-12),V(1.3f,0,-7),V(-1.2f,0,1),V(1.3f,0,6),V(-1.2f,0,12),V(4.9f,0,12),V(10.6f,0,12),V(10.6f,0,5)})
        {
            var g=Group(accents,"Wet scattered collections",center);Papers(g,V(0,.018f,0),22,.8f);
            for(int i=0;i<9;i++){var book=Model(g,"single_book","Volume dropped during evacuation",V(R(-.6f,.6f),0,R(-1.2f,1.2f)),V(R(-4,4),R(0,360),90),Vector3.one*R(1,1.35f));Ground(g,book,R(.024f,.07f));}
            for(int i=0;i<16;i++)Box(g,"Fallen ceiling plaster",V(R(-.55f,.55f),.035f,R(-1.3f,1.3f)),V(R(.04f,.14f),R(.02f,.05f),R(.04f,.16f)),"BrokenPlaster",euler:V(R(-8,8),R(0,180),R(-8,8)));
            Batch(g,"ScatteredCollection");
        }
        foreach(var pos in new[]{V(2.1f,3.85f,6.2f),V(9.5f,3.87f,-6)})
        {
            var g=Group(accents,"Hanging fractured plaster section",pos);g.localRotation=Quaternion.Euler(18,15,-13);
            Polygon(g,"Broken underside",new[]{V(-.7f,0,-.4f),V(-.4f,0,.6f),V(.2f,0,.8f),V(.7f,0,.2f),V(.36f,0,-.13f),V(.65f,0,-.5f),V(-.1f,0,-.68f)},"BrokenPlaster",true);
            // Two faces give the dislodged plaster slab thickness at grazing angles.
            Box(g,"Exposed supporting lath",V(0,.035f,.1f),V(.95f,.06f,.8f),"DamagedConcrete");
            foreach(float x in new[]{-.35f,.35f})Cable(accents,"Exposed corroded ceiling tie",new[]{pos+V(x,.5f,.3f),pos+V(x,.09f,.3f)},.012f,"Rust");
        }
    }
    static void Puddle(Transform a,Vector3 center,float rx,float rz)
    {
        var outline=new Vector3[18];for(int i=0;i<outline.Length;i++){float angle=i*Mathf.PI*2/outline.Length;outline[i]=center+V(Mathf.Cos(angle)*rx*R(.80f,1.05f),.007f,Mathf.Sin(angle)*rz*R(.80f,1.05f));}
        Polygon(a,"Rainwater collected in cracked tiles",outline,"Wet",true);
    }
    static void Atmosphere(Transform a)
    {
        var g=Group(a,"Library regional atmosphere",V(0,2.2f,0));var c=g.gameObject.AddComponent<BoxCollider>();c.isTrigger=true;c.size=V(24.2f,4.4f,39.4f);
        var region=g.gameObject.AddComponent<TCRegionAtmosphere>();region.theme="quarantined derelict library";region.localBounds=new Bounds(Vector3.zero,c.size);region.priority=35;region.ambient=C(.40f,.42f,.40f);region.fogColor=C(.075f,.09f,.10f);region.fogDensity=.018f;region.blendDistance=.8f;region.flashlightIntensity=2.6f;region.stabilizeBackgroundShadows=true;
        string path=Art+"/Profiles/DecayRevision/QuarantinedLibrary.asset";var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
        foreach(var comp in profile.components.ToArray()){profile.components.Remove(comp);Object.DestroyImmediate(comp,true);}
        var grade=profile.Add<ColorAdjustments>(true);grade.saturation.Override(-22);grade.contrast.Override(9);grade.postExposure.Override(.75f);
        var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.14f);bloom.threshold.Override(1.2f);
        var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.16f);var grain=profile.Add<FilmGrain>(true);grain.intensity.Override(.055f);
        foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);EditorUtility.SetDirty(profile);
        var volume=g.gameObject.AddComponent<Volume>();volume.isGlobal=false;volume.sharedProfile=profile;volume.priority=35;volume.blendDistance=.8f;
        foreach(float z in new[]{-10f,10f})
        {
            var p=Group(a,"Persistent wet floor reflection",V(2,1.6f,z));var probe=p.gameObject.AddComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Custom;probe.size=V(23.5f,4.4f,21);probe.center=V(-2,.6f,0);probe.blendDistance=3;probe.boxProjection=true;probe.resolution=128;probe.intensity=.35f;probe.importance=20;
        }
    }
    static void BakeReflections()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LibraryBuilder.Prefab));root.transform.position=-Vector3.Scale(LibraryBuilder.FloorOrigin,LibraryBuilder.MapScale);
        var region=root.GetComponentInChildren<TCRegionAtmosphere>();RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=region.ambient;RenderSettings.fog=true;RenderSettings.fogColor=region.fogColor;RenderSettings.fogDensity=region.fogDensity;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/_TemporaryTools/LibraryDecayReflection.unity");
        ShaderUtil.allowAsyncCompilation=false;int i=0;
        foreach(var probe in root.GetComponentsInChildren<ReflectionProbe>())
        {
            probe.mode=ReflectionProbeMode.Baked;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=region.fogColor;probe.cullingMask=~0;
            string path=Art+"/Reflections/DecayRevision/LibraryWet_"+i+++".exr";
            if(!Lightmapping.BakeReflectionProbe(probe,path))throw new Exception("Reflection snapshot failed");
        }
        AssetDatabase.Refresh();var prefab=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try
        {
            i=0;foreach(var probe in prefab.GetComponentsInChildren<ReflectionProbe>()){probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(Art+"/Reflections/DecayRevision/LibraryWet_"+i+++".exr");if(!probe.customBakedTexture)throw new Exception("Persistent reflection missing");}
            PrefabUtility.SaveAsPrefabAsset(prefab,LibraryBuilder.Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        Debug.Log("LIBRARY_DECAY_REFLECTION_PASS");
    }
}
