// Editor-only generation source; intentionally kept outside Assets in the real project.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class EntranceGardenBuilder
{
    const string Output = "Assets/Art/EntranceGarden";
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static readonly List<EntranceGardenLayout.Placement> placements = new List<EntranceGardenLayout.Placement>();
    static readonly List<Mesh> temporaryMeshes = new List<Mesh>();
    static Transform root;
    static System.Random random = new System.Random(1903);

    public static void Build()
    {
        foreach (string folder in new[] { "Textures", "Materials", "Meshes" }) Directory.CreateDirectory(Output + "/" + folder);
        AssetDatabase.Refresh();
        Surface("WeatheredStone", new Color(.40f,.38f,.29f), "stone");
        Surface("WallMasonry", new Color(.26f,.25f,.20f), "brick");
        Surface("PathPaving", new Color(.38f,.34f,.25f), "paving");
        Surface("Soil", new Color(.10f,.095f,.06f), "soil");
        Surface("Iron", new Color(.065f,.075f,.063f), "metal", .3f);
        Surface("Bark", new Color(.13f,.10f,.063f), "bark");
        Surface("Moss", new Color(.16f,.20f,.075f), "moss");
        Surface("Cypress", new Color(.075f,.12f,.056f), "moss");
        Surface("Ivy", new Color(.12f,.19f,.067f), "moss");
        Surface("DeadLeaves", new Color(.29f,.095f,.035f), "soil");
        Surface("CrimsonFlowers", new Color(.40f,.042f,.026f), "moss");
        Surface("DeepShadow", new Color(.021f,.024f,.020f), "soil");
        Surface("LanternAmber", new Color(.90f,.48f,.095f), "glass", 0, new Color(2.6f,1.2f,.21f));
        var go = new GameObject("GothicGardenDecoration"); root = go.transform;
        var fitting = go.AddComponent<EntranceGardenLayout>();
        var path = Group("Central stone path", 0, 0, EntranceGardenLayout.Fit.Path);
        Box(path,"Paving surface",Vector3.zero,Vector3.one,"PathPaving");
        foreach (int side in new[] { -1, 1 })
        {
            string label = side < 0 ? "Left" : "Right";
            var bed = Group(label+" sunken planting bed",side*.37f,0,EntranceGardenLayout.Fit.Bed);
            Box(bed,"Earth",new Vector3(0,.5f,0),Vector3.one,"Soil");
            var curb = Group(label+" stone border",side*.26f,0,EntranceGardenLayout.Fit.Curb);
            Box(curb,"Border",new Vector3(0,.5f,0),Vector3.one,"WeatheredStone");
            var wall = Group(label+" masonry facing",side*.5f,0,side<0?EntranceGardenLayout.Fit.WallLeft:EntranceGardenLayout.Fit.WallRight);
            Box(wall,"Thin decorative wall facing",Vector3.zero,Vector3.one,"WallMasonry");
            for (int i=0;i<5;i++) Pillar(label+" lantern pier "+(i+1),side*.445f,-.42f+i*.21f,i==0||i==4);
            for (int i=0;i<4;i++)
            {
                float z=-.315f+i*.21f;
                Fence(label+" wrought iron span "+(i+1),side*.467f,z);
                if(i!=1)Hedge(label+" clipped hedge "+(i+1),side*.308f,z);
                Cypress(label+" cypress "+(i+1),side*.405f,z,new Vector3(.88f,1f+(i%2)*.09f,.88f));
                Flowers(label+" thorn roses "+(i+1),side*.37f,z+.05f);
                Ivy(label+" trailing ivy "+(i+1),side*.488f,z,side);
            }
            Statue(label+" hooded guardian",side*.399f,-.335f,side<0?-28:28);
            Bench(label+" stone bench",side*.359f,-.16f,side<0?-90:90);
            Urn(label+" flower urn",side*.364f,-.395f);
            Tree(label+" dead oak near",side*.424f,-.255f,side);
            Tree(label+" dead oak far",side*.431f,.335f,-side);
            for(int i=0;i<9;i++) LeafPatch(label+" fallen leaves "+i,side*(.258f+(i%3)*.006f),-.435f+i*.104f);
        }
        foreach(Transform group in root.Cast<Transform>().ToArray()) Combine(group);
        fitting.placements=placements.ToArray();
        PrefabUtility.SaveAsPrefabAsset(go,Output+"/GothicGardenDecoration.prefab");
        Object.DestroyImmediate(go);
        foreach(var mesh in temporaryMeshes)Object.DestroyImmediate(mesh);
        AssetDatabase.SaveAssets();
        Debug.Log("GARDEN_BUILT groups="+placements.Count);
    }

    static Transform Group(string name,float x,float z,EntranceGardenLayout.Fit fit=EntranceGardenLayout.Fit.Prop,float yaw=0,Vector3? scale=null)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);
        placements.Add(new EntranceGardenLayout.Placement {target=go.transform,fit=fit,anchor=new Vector2(x,z),scale=scale??Vector3.one,yaw=yaw});
        return go.transform;
    }
    static GameObject Shape(Transform parent,string name,Vector3 pos,Vector3 size,string mat,PrimitiveType shape,Vector3? rotation=null)
    {
        var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=size;
        if(rotation.HasValue)go.transform.localRotation=Quaternion.Euler(rotation.Value);
        Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=materials[mat];return go;
    }
    static GameObject Box(Transform p,string n,Vector3 v,Vector3 s,string m,Vector3? r=null)=>Shape(p,n,v,s,m,PrimitiveType.Cube,r);
    static GameObject Sphere(Transform p,string n,Vector3 v,Vector3 s,string m)=>Shape(p,n,v,s,m,PrimitiveType.Sphere);
    static void Collision(Transform t,Vector3 center,Vector3 size)
    {var collider=t.gameObject.AddComponent<BoxCollider>();collider.center=center;collider.size=size;}
    static void Segment(Transform p,string name,Vector3 start,Vector3 end,float width,string material)
    {
        var shape=Box(p,name,(start+end)*.5f,new Vector3(width,(end-start).magnitude,width),material);
        shape.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(end-start).normalized);
    }
    // Eight-sided lofts give the trees, robes and urns a deliberately angular silhouette.
    static void Loft(Transform parent,string name,Vector3 pos,float[] heights,float[] radii,string material,int sides=8,float oval=1)
    {
        var vertices=new List<Vector3>();var uvs=new List<Vector2>();var triangles=new List<int>();
        for(int ring=0;ring<heights.Length;ring++)for(int side=0;side<=sides;side++)
        {
            float a=side*Mathf.PI*2/sides;float radius=radii[ring]*(side%2==0?1:.94f);
            vertices.Add(new Vector3(Mathf.Sin(a)*radius,heights[ring],Mathf.Cos(a)*radius*oval));
            uvs.Add(new Vector2((float)side/sides,heights[ring]));
        }
        for(int ring=0;ring<heights.Length-1;ring++)for(int side=0;side<sides;side++)
        {
            int a=ring*(sides+1)+side,b=a+sides+1;
            triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b});
        }
        int bottom=vertices.Count;vertices.Add(new Vector3(0,heights[0],0));uvs.Add(new Vector2(.5f,.5f));
        int top=vertices.Count;vertices.Add(new Vector3(0,heights[heights.Length-1],0));uvs.Add(new Vector2(.5f,.5f));
        for(int side=0;side<sides;side++)
        {triangles.AddRange(new[]{bottom,side+1,side});int a=(heights.Length-1)*(sides+1)+side;triangles.AddRange(new[]{top,a,a+1});}
        var mesh=new Mesh {name=name};mesh.SetVertices(vertices);mesh.SetUVs(0,uvs);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();temporaryMeshes.Add(mesh);
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[material];
    }
    static void Pillar(string name,float x,float z,bool light)
    {
        var p=Group(name,x,z);
        Box(p,"Footing",new Vector3(0,.08f,0),new Vector3(.62f,.16f,.62f),"WeatheredStone");
        Box(p,"Pier shaft",new Vector3(0,.83f,0),new Vector3(.42f,1.48f,.42f),"WallMasonry");
        Box(p,"Stone inset",new Vector3(0,.83f,-.217f),new Vector3(.28f,1.16f,.026f),"WeatheredStone");
        Box(p,"Collar",new Vector3(0,1.57f,0),new Vector3(.56f,.11f,.56f),"WeatheredStone");
        Box(p,"Capstone",new Vector3(0,1.67f,0),new Vector3(.65f,.09f,.65f),"WeatheredStone");
        Lantern(p,new Vector3(0,1.73f,0));
        Collision(p,new Vector3(0,.86f,0),new Vector3(.63f,1.72f,.63f));
        if(light)
        {
            var source=new GameObject("Local amber light");source.transform.SetParent(p,false);source.transform.localPosition=new Vector3(-Mathf.Sign(x)*.24f,1.96f,0);
            var lamp=source.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=new Color(1,.48f,.15f);lamp.intensity=2.4f;lamp.range=5.2f;lamp.shadows=LightShadows.None;
            source.AddComponent<UniversalAdditionalLightData>();
        }
    }
    static void Lantern(Transform p,Vector3 origin)
    {
        Box(p,"Lantern foot",origin,new Vector3(.24f,.055f,.24f),"Iron");
        Box(p,"Amber glass",origin+new Vector3(0,.18f,0),new Vector3(.205f,.29f,.205f),"LanternAmber");
        for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2)Box(p,"Corner bar",origin+new Vector3(a*.11f,.19f,b*.11f),new Vector3(.018f,.32f,.018f),"Iron");
        Box(p,"Upper rim",origin+new Vector3(0,.345f,0),new Vector3(.27f,.035f,.27f),"Iron");
        Loft(p,"Pointed lantern roof",origin,new[]{.36f,.50f},new[]{.21f,.025f},"Iron",4);
        Loft(p,"Lantern finial",origin,new[]{.50f,.59f},new[]{.035f,0f},"Iron",6);
    }
    static void Fence(string name,float x,float z)
    {
        var p=Group(name,x,z,EntranceGardenLayout.Fit.Fence);
        for(int i=0;i<14;i++)
        {
            float at=-.47f+i*.94f/13;float height=i%2==0?1.39f:1.30f;
            Box(p,"Iron bar",new Vector3(0,height*.5f+.12f,at),new Vector3(.022f,height,.018f),"Iron");
            // Finials stretch along the span with the fence, without blocking the path.
            Box(p,"Spear diamond",new Vector3(0,height+.16f,at),new Vector3(.051f,.084f,.029f),"Iron",new Vector3(0,0,45));
            Box(p,"Cross ornament",new Vector3(0,.82f,at),new Vector3(.035f,.060f,.035f),"Iron",new Vector3(0,0,45));
        }
        for(int row=0;row<2;row++)Box(p,"Continuous rail",new Vector3(0,.38f+row*.65f,0),new Vector3(.036f,.04f,1),"Iron");
    }
    static void Hedge(string name,float x,float z)
    {
        var p=Group(name,x,z,EntranceGardenLayout.Fit.Hedge);
        Box(p,"Clipped hedge body",new Vector3(0,.48f,0),new Vector3(1,.90f,1),"Moss");
        // Broken top and leaf blocks keep the silhouette from becoming a perfect cube.
        for(int i=0;i<15;i++)Box(p,"Leaf clump",new Vector3((i%3-1)*.31f,.96f+(i%2)*.055f,-.42f+(i/3)*.21f),new Vector3(.34f,.15f,.24f),"Ivy");
        Collision(p,new Vector3(0,.52f,0),new Vector3(1,1.03f,1));
    }
    static void Cypress(string name,float x,float z,Vector3 scale)
    {
        var p=Group(name,x,z,EntranceGardenLayout.Fit.Tree,0,scale);
        Loft(p,"Cypress trunk",Vector3.zero,new[]{0f,1.2f},new[]{.105f,.075f},"Bark");
        for(int layer=0;layer<7;layer++)
        {
            float y=.45f+layer*.50f,r=.40f-layer*.043f;
            Loft(p,"Angular evergreen layer",new Vector3(0,y,0),new[]{0f,.30f,.94f},new[]{r*.82f,r,.024f},"Cypress",8);
        }
        Collision(p,new Vector3(0,1.2f,0),new Vector3(.34f,2.4f,.34f));
    }
    static void Statue(string name,float x,float z,float yaw)
    {
        var p=Group(name,x,z,EntranceGardenLayout.Fit.Prop,yaw);
        Box(p,"Plinth base",new Vector3(0,.10f,0),new Vector3(.64f,.20f,.64f),"WeatheredStone");
        Box(p,"Pedestal",new Vector3(0,.45f,0),new Vector3(.49f,.54f,.49f),"WallMasonry");
        Box(p,"Pedestal crown",new Vector3(0,.75f,0),new Vector3(.62f,.11f,.61f),"WeatheredStone");
        Loft(p,"Draped stone robe",new Vector3(0,.80f,0),new[]{0f,.13f,.83f,1.26f,1.40f},new[]{.27f,.285f,.18f,.24f,.13f},"WeatheredStone",12,.73f);
        for(int i=-2;i<=2;i++)Segment(p,"Carved robe fold",new Vector3(i*.075f,.83f,-.211f),new Vector3(i*.036f,1.85f,-.145f),.024f,"WallMasonry");
        Sphere(p,"Stone hood",new Vector3(0,2.33f,0),new Vector3(.42f,.53f,.39f),"WeatheredStone");
        Sphere(p,"Recessed hood opening",new Vector3(0,2.29f,-.170f),new Vector3(.255f,.30f,.061f),"DeepShadow");
        Sphere(p,"Weathered face",new Vector3(0,2.265f,-.20f),new Vector3(.15f,.19f,.035f),"WallMasonry");
        Segment(p,"Left sleeve",new Vector3(-.23f,1.99f,0),new Vector3(-.14f,1.66f,-.14f),.17f,"WeatheredStone");
        Segment(p,"Right sleeve",new Vector3(.23f,1.99f,0),new Vector3(.14f,1.66f,-.14f),.17f,"WeatheredStone");
        Segment(p,"Folded arms left",new Vector3(-.14f,1.66f,-.14f),new Vector3(.04f,1.72f,-.23f),.12f,"WeatheredStone");
        Segment(p,"Folded arms right",new Vector3(.14f,1.66f,-.14f),new Vector3(-.04f,1.72f,-.23f),.12f,"WeatheredStone");
        Box(p,"Stone book",new Vector3(0,1.72f,-.235f),new Vector3(.21f,.13f,.10f),"WallMasonry",new Vector3(-15,0,0));
        Collision(p,new Vector3(0,1.27f,0),new Vector3(.64f,2.54f,.64f));
    }
    static void Bench(string name,float x,float z,float yaw)
    {
        var p=Group(name,x,z,EntranceGardenLayout.Fit.Prop,yaw);
        Box(p,"Stone seat",new Vector3(0,.48f,0),new Vector3(1.60f,.14f,.46f),"WeatheredStone");
        for(int s=-1;s<=1;s+=2)
        {
            Box(p,"Carved support",new Vector3(s*.57f,.23f,0),new Vector3(.21f,.46f,.38f),"WallMasonry");
            Box(p,"Bench footing",new Vector3(s*.57f,.045f,0),new Vector3(.32f,.09f,.45f),"WeatheredStone");
        }
        Collision(p,new Vector3(0,.29f,0),new Vector3(1.6f,.58f,.46f));
    }
    static void Urn(string name,float x,float z)
    {
        var p=Group(name,x,z);
        Box(p,"Urn pedestal",new Vector3(0,.30f,0),new Vector3(.36f,.60f,.36f),"WeatheredStone");
        Box(p,"Pedestal cap",new Vector3(0,.64f,0),new Vector3(.48f,.09f,.48f),"WeatheredStone");
        Loft(p,"Garden urn",new Vector3(0,.69f,0),new[]{0f,.07f,.19f,.39f,.48f},new[]{.16f,.16f,.26f,.28f,.30f},"WeatheredStone",12);
        Loft(p,"Soil in urn",new Vector3(0,1.16f,0),new[]{0f,.013f},new[]{.25f,.25f},"Soil");
        for(int i=0;i<8;i++)
        {
            float a=i*Mathf.PI/4;var end=new Vector3(Mathf.Sin(a)*.23f,1.47f+(i%3)*.07f,Mathf.Cos(a)*.23f);
            Segment(p,"Rose stem",new Vector3(0,1.17f,0),end,.014f,"Bark");
            Box(p,"Crimson blossom",end,new Vector3(.12f,.085f,.12f),"CrimsonFlowers",new Vector3(20,i*40,15));
            Box(p,"Urn foliage",(new Vector3(0,1.17f,0)+end)*.5f,new Vector3(.10f,.018f,.23f),"Ivy",new Vector3(28,i*45,0));
        }
        Collision(p,new Vector3(0,.6f,0),new Vector3(.48f,1.2f,.48f));
    }
    static void Flowers(string name,float x,float z)
    {
        var p=Group(name,x,z);
        for(int i=0;i<16;i++)
        {
            var start=new Vector3((float)random.NextDouble()*.50f-.25f,.03f,(float)random.NextDouble()*1.25f-.625f);
            var end=start+new Vector3((float)random.NextDouble()*.17f-.085f,.73f+(float)random.NextDouble()*.44f,0);
            Segment(p,"Thorn stem",start,end,.016f,"Bark");
            Segment(p,"Thorn branch",Vector3.Lerp(start,end,.55f),end+new Vector3(.14f,-.09f,.07f),.011f,"Bark");
            Box(p,"Rose foliage",Vector3.Lerp(start,end,.55f),new Vector3(.14f,.025f,.25f),"Ivy",new Vector3(22,i*37,0));
            Box(p,"Dried red rose",end,new Vector3(.10f,.065f,.095f),"CrimsonFlowers",new Vector3(25,i*63,12));
        }
    }
    static void Ivy(string name,float x,float z,int side)
    {
        var p=Group(name,x,z);
        for(int vine=0;vine<5;vine++)
        {
            float zz=(vine-2)*.21f;float height=.70f+vine*.22f;
            Segment(p,"Trailing vine",new Vector3(0,.06f,zz),new Vector3(0,height,zz+.11f),.012f,"Bark");
            for(int leaf=0;leaf<9;leaf++)Box(p,"Ivy leaf",new Vector3(-side*.022f,(leaf+.5f)*height/9,zz+(leaf%2==0?-.037f:.061f)),new Vector3(.03f,.12f,.12f),"Ivy",new Vector3(0,0,(leaf%2==0?1:-1)*24));
        }
    }
    static void Tree(string name,float x,float z,int bend)
    {
        var p=Group(name,x,z,EntranceGardenLayout.Fit.Tree);
        Loft(p,"Gnarled trunk",Vector3.zero,new[]{0f,.28f,1.2f,2.35f,3.15f},new[]{.22f,.17f,.13f,.085f,.045f},"Bark",8);
        for(int i=0;i<6;i++)
        {
            float angle=(i*61+23)*Mathf.Deg2Rad;float span=.70f+(i%3)*.32f;
            var start=new Vector3(0,1.75f+i*.23f,0);
            var middle=new Vector3(Mathf.Sin(angle)*span,start.y+.66f,Mathf.Cos(angle)*span);
            var end=middle+new Vector3(-bend*.46f,.40f,(i%2==0?.27f:-.27f));
            Segment(p,"Crooked bough",start,middle,.062f,"Bark");Segment(p,"Bare branch",middle,end,.038f,"Bark");
            for(int twig=0;twig<3;twig++)
            {
                var twigStart=Vector3.Lerp(middle,end,(twig+1)*.23f);var twigEnd=twigStart+new Vector3((twig%2==0?1:-1)*.32f,.30f,.12f*(twig-1));
                Segment(p,"Finger twig",twigStart,twigEnd,.021f,"Bark");
                Segment(p,"Split twig",Vector3.Lerp(twigStart,twigEnd,.6f),twigEnd+new Vector3(-.13f,.14f,.13f),.011f,"Bark");
            }
        }
        for(int i=0;i<5;i++)
        {float a=i*Mathf.PI*2/5;Segment(p,"Exposed root",new Vector3(0,.14f,0),new Vector3(Mathf.Sin(a)*.42f,.01f,Mathf.Cos(a)*.42f),.055f,"Bark");}
        Collision(p,new Vector3(0,1.20f,0),new Vector3(.32f,2.40f,.32f));
    }
    static void LeafPatch(string name,float x,float z)
    {
        var p=Group(name,x,z);
        for(int leaf=0;leaf<42;leaf++)Box(p,"Dry leaf",new Vector3((float)random.NextDouble()*.42f-.21f,.021f,(float)random.NextDouble()*1.2f-.6f),new Vector3(.050f+(float)random.NextDouble()*.045f,.006f,.10f),leaf%3==0?"Moss":"DeadLeaves",new Vector3(0,random.Next(360),0));
    }
    static void Combine(Transform group)
    {
        var renderers=group.GetComponentsInChildren<MeshRenderer>();
        foreach(var bucket in renderers.GroupBy(r=>r.sharedMaterial))
        {
            var pieces=bucket.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=group.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
            var mesh=new Mesh{name=group.name+" "+bucket.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pieces,true,true);mesh.RecalculateBounds();
            var safe=string.Concat(mesh.name.Select(c=>char.IsLetterOrDigit(c)?c:'_'));AssetDatabase.CreateAsset(mesh,Output+"/Meshes/"+safe+".asset");
            var go=new GameObject(bucket.Key.name);go.transform.SetParent(group,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=bucket.Key;
        }
        foreach(var renderer in renderers)Object.DestroyImmediate(renderer.gameObject);
    }
    static float Noise(int x,int y)
    {unchecked{uint n=(uint)(x*374761393+y*668265263+1903);n=(n^(n>>13))*1274126177;return((n^(n>>16))&65535)/65535f;}}
    static Color Pixel(string kind,Color baseColor,int x,int y)
    {
        float n=Noise(x,y),patch=Noise(x/7,y/9);Color moss=new Color(.14f,.17f,.063f);
        if(kind=="stone")return Color.Lerp(baseColor,moss,patch<.14f?.64f:0)*(.70f+.28f*n+.20f*patch);
        if(kind=="brick")
        {
            int row=y/24,xx=(x+(row%2)*24)%48;bool mortar=y%24<2||xx<2;
            if(mortar)return new Color(.083f,.086f,.062f);
            return Color.Lerp(baseColor,moss,patch<.13f?.5f:0)*(.71f+.28f*n+.16f*Noise(xx/7,row));
        }
        if(kind=="paving")
        {
            int row=y/64;int xx=(x+(row%2)*32)%64;int yy=y%64;
            if(xx<3||yy<3)return new Color(.065f,.071f,.044f);
            if((xx<8||yy<8)&&patch<.22f)return moss*(.7f+.3f*n);
            bool crack=Math.Abs(xx-(19+(int)(Noise(y/8,5)*6)))<1&&yy>15&&yy<45;
            return crack?baseColor*.38f:baseColor*(.67f+.30f*n+.22f*patch);
        }
        if(kind=="bark")return baseColor*(.60f+.34f*n+.22f*patch+((x%13)<3?.28f:0));
        if(kind=="moss")return baseColor*(.60f+.31f*n+.44f*patch);
        if(kind=="glass")return baseColor*(.65f+.18f*n+.28f*Mathf.Sin(y*.018f));
        return baseColor*(.69f+.28f*n+.17f*patch);
    }
    static void Surface(string name,Color baseColor,string kind,float metallic=0,Color? emission=null)
    {
        var texture=new Texture2D(128,128,TextureFormat.RGB24,false);var pixels=new Color[128*128];
        for(int y=0;y<128;y++)for(int x=0;x<128;x++)pixels[y*128+x]=Pixel(kind,baseColor,x,y);
        texture.SetPixels(pixels);texture.Apply();string path=Output+"/Textures/"+name+".png";File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.filterMode=FilterMode.Point;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
        var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(path));mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Smoothness",metallic>0?.28f:.06f);mat.SetFloat("_Metallic",metallic);
        if(emission.HasValue){mat.EnableKeyword("_EMISSION");mat.SetTexture("_EmissionMap",mat.GetTexture("_BaseMap"));mat.SetColor("_EmissionColor",emission.Value);}
        AssetDatabase.CreateAsset(mat,Output+"/Materials/"+name+".mat");materials[name]=mat;
    }

    public static void VerifyAndRender()
    {
        // All renders inherit the existing map's sky, fog, ambient settings and lights.
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var garden=Object.FindObjectsByType<Transform>().First(t=>t.name=="EntranceGarden"&&t.parent==null);
        var layout=garden.GetComponentInChildren<EntranceGardenLayout>();if(!layout)throw new Exception("Missing garden decoration");layout.Refresh();
        Validate(garden,layout);
        Render(garden,layout,"Preview/EntranceGarden.png",false);
        Render(garden,layout,"Preview/EntranceGardenOverview.png",true);
        Debug.Log("GARDEN_MAP_VERIFIED width="+layout.LastSize.x+" length="+layout.LastSize.z+" clearPath="+layout.ClearPathWidth+" renderers="+layout.GetComponentsInChildren<Renderer>().Length);
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Scenario/EntranceGarden.prefab");
        var prefab=(GameObject)PrefabUtility.InstantiatePrefab(asset);prefab.transform.position=new Vector3(500,0,500);
        var prefabLayout=prefab.GetComponentInChildren<EntranceGardenLayout>();prefabLayout.Refresh();Validate(prefab.transform,prefabLayout);
        Debug.Log("GARDEN_PREFAB_VERIFIED width="+prefabLayout.LastSize.x+" length="+prefabLayout.LastSize.z+" clearPath="+prefabLayout.ClearPathWidth);
        Object.DestroyImmediate(prefab);
    }
    static void Validate(Transform garden,EntranceGardenLayout layout)
    {
        foreach(var renderer in layout.GetComponentsInChildren<MeshRenderer>())
        {if(!renderer.GetComponent<MeshFilter>().sharedMesh)throw new Exception("Missing mesh "+renderer.name);foreach(var mat in renderer.sharedMaterials)if(!mat||!mat.shader||mat.shader.name=="Hidden/InternalErrorShader")throw new Exception("Missing material "+renderer.name);}
        Physics.SyncTransforms();float halfClear=layout.ClearPathWidth*.5f-.08f;
        foreach(var collider in layout.GetComponentsInChildren<BoxCollider>())
        {
            Bounds local=new Bounds(layout.transform.InverseTransformPoint(collider.transform.TransformPoint(collider.center)),Vector3.zero);
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)local.Encapsulate(layout.transform.InverseTransformPoint(collider.transform.TransformPoint(collider.center+Vector3.Scale(collider.size*.5f,new Vector3(x,y,z)))));
            if(local.min.x<halfClear&&local.max.x> -halfClear)throw new Exception("Central passage blocked by "+collider.name);
            if(local.min.x < -layout.LastSize.x*.5f-.05f || local.max.x > layout.LastSize.x*.5f+.05f || local.min.z < -layout.LastSize.z*.5f-.05f || local.max.z > layout.LastSize.z*.5f+.05f)throw new Exception("Collider outside existing garden: "+collider.name);
        }
        if(layout.ClearPathWidth<1.2f)throw new Exception("Passage too narrow");
    }
    static void Render(Transform garden,EntranceGardenLayout layout,string path,bool overview)
    {
        var cam=new GameObject("Garden preview camera").AddComponent<Camera>();cam.gameObject.AddComponent<UniversalAdditionalCameraData>();cam.fieldOfView=overview?76:74;cam.nearClipPlane=.07f;cam.farClipPlane=180;cam.clearFlags=CameraClearFlags.Skybox;
        cam.transform.position=layout.transform.TransformPoint(new Vector3(overview?-.035f*layout.LastSize.x:0,overview?2.15f:1.65f,-layout.LastSize.z*.48f));
        cam.transform.LookAt(layout.transform.TransformPoint(new Vector3(0,overview?1.2f:1.65f,layout.LastSize.z*.36f)));
        var target=new RenderTexture(1440,1080,24);cam.targetTexture=target;for(int frame=0;frame<3;frame++)cam.Render();RenderTexture.active=target;
        var picture=new Texture2D(1440,1080,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,1440,1080),0,0);picture.Apply();Directory.CreateDirectory("Preview");File.WriteAllBytes(path,picture.EncodeToPNG());
        cam.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(picture);Object.DestroyImmediate(target);Object.DestroyImmediate(cam.gameObject);
    }
}
