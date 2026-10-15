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
using Object=UnityEngine.Object;

public static class BasementRitualExpansion
{
    const string Art="Assets/Art/Basement/RitualExpansion";
    const string Room="Assets/prefabs/Scenario/Basement.prefab";
    const string Arena="Assets/Art/Basement/BasementFinalArena.prefab";
    const string Sanctum="Assets/Art/Basement/BasementFinalSanctum.prefab";
    static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static readonly Dictionary<Material,List<CombineInstance>> geometry=new Dictionary<Material,List<CombineInstance>>();
    static readonly List<Mesh> temporary=new List<Mesh>();
    static Transform group;
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static T Load<T>(string p) where T:Object=>AssetDatabase.LoadAssetAtPath<T>(p);
    static Transform Group(Transform p,string n,Vector3 v)
    {var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=v;return t;}
    static Material Material(string name,string source,Color color,float smoothness=.15f)
    {
        var original=string.IsNullOrEmpty(source)?null:Load<Material>(source);
        var m=original?new Material(original):new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;
        m.SetColor("_BaseColor",color);m.SetColor("_Color",color);m.SetFloat("_Smoothness",smoothness);
        m.DisableKeyword("_EMISSION");m.SetColor("_EmissionColor",Color.black);
        if(m.shader.name=="UniDoom/TotalChaosInspiredSurface")m.SetVector("_SpecColor",new Vector4(.23f,.02f,.5f,1.02f));
        string path=Art+"/Materials/"+name+".mat";var old=Load<Material>(path);
        if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);m=old;}else AssetDatabase.CreateAsset(m,path);
        mats[name]=m;return m;
    }
    static void Materials()
    {
        mats.Clear();
        string blood="Assets/Art/Basement/ArenaHorror/Materials/Dried_seepage.mat";
        Material("Oxidized_ritual_blood",null,new Color(.46f,.085f,.045f),.05f);
        Material("Smudged_blood",blood,new Color(.15f,.038f,.025f));
        Material("Scraped_bone_chalk",null,new Color(.64f,.51f,.33f),.04f);
        Material("Charcoal_residue",blood,new Color(.035f,.03f,.024f));
        Material("Ritual_iron","Assets/Art/Basement/ArenaPolish/Materials/Oxidized_structural_steel.mat",new Color(.23f,.19f,.135f),.22f);
        Material("Worn_offering_stone","Assets/Art/Basement/Ritual/Materials/Old_crypt_masonry.mat",new Color(.34f,.29f,.23f));
        Material("Unbleached_wax","Assets/Art/Basement/Ritual/Materials/candles.mat",new Color(.72f,.59f,.36f),.16f);
        Material("Scorched_funeral_cloth","Assets/Art/Basement/Materials/FinalShroud.mat",new Color(.12f,.065f,.044f));
        Material("Bone_relic","Assets/Art/Basement/DetailPolish/Materials/Aged_anatomical_bone.mat",new Color(.5f,.42f,.29f));
        foreach(var key in new[]{"Oxidized_ritual_blood","Scraped_bone_chalk"})
        {var m=mats[key];m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",key=="Oxidized_ritual_blood"?new Color(.035f,.003f,.001f):new Color(.025f,.018f,.009f));EditorUtility.SetDirty(m);}
        var flame=Material("Small_candle_flame",null,new Color(1,.56f,.16f));flame.EnableKeyword("_EMISSION");flame.SetColor("_EmissionColor",new Color(3.2f,1.55f,.35f));EditorUtility.SetDirty(flame);
    }
    static void Add(Mesh mesh,Vector3 p,Quaternion q,Vector3 scale,string material,bool transient=true)
    {
        var mat=mats[material];if(!geometry.TryGetValue(mat,out var list))geometry[mat]=list=new List<CombineInstance>();
        list.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(p,q,scale)});if(transient)temporary.Add(mesh);
    }
    static void Box(Vector3 p,Vector3 size,string mat)=>Add(ChaosGeometry.BevelBox(size,.018f),p,Quaternion.identity,Vector3.one,mat);
    static void Beam(Vector3 a,Vector3 b,float width,string mat)
    {Add(ChaosGeometry.BevelBox(V(width,(b-a).magnitude,width),.01f),(a+b)*.5f,Quaternion.FromToRotation(Vector3.up,b-a),Vector3.one,mat);}
    static void Tube(Vector3[] points,float width,string mat)=>Add(ChaosGeometry.Sweep(points,width,7),Vector3.zero,Quaternion.identity,Vector3.one,mat);
    static void Ring(Vector3 c,float radius,float width,string mat,Quaternion rotation,int seed=0)
    {
        var path=Enumerable.Range(0,81).Select(i=>{float a=i*Mathf.PI/40;float r=radius*(1+.009f*Mathf.Sin(a*9+seed));return c+rotation*V(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);}).ToArray();Tube(path,width,mat);
    }
    static void Flush(string name)
    {
        foreach(var entry in geometry)
        {
            var mesh=new Mesh{name=name+"_"+entry.Key.name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(entry.Value.ToArray(),true,true);mesh.RecalculateBounds();
            string path=Art+"/Meshes/"+mesh.name+".asset";var old=Load<Mesh>(path);
            if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);mesh=old;}else AssetDatabase.CreateAsset(mesh,path);
            var t=Group(group,mesh.name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=entry.Key;
        }
        geometry.Clear();foreach(var mesh in temporary.Distinct())if(mesh)Object.DestroyImmediate(mesh);temporary.Clear();
    }
    // Flat strokes follow the surface. Their rough edges and broken dabs read as paint, not metal rods.
    static void Stroke(Vector3[] path,float width,string mat,Quaternion plane,int seed,bool broken=false)
    {
        var build=new ChaosGeometry.Builder();var normal=plane*Vector3.up;int index=0;
        for(int k=0;k<path.Length-1;k++)
        {
            int steps=Mathf.Max(2,Mathf.CeilToInt(Vector3.Distance(path[k],path[k+1])/.11f));
            var tangent=(path[k+1]-path[k]).normalized;var across=Vector3.Cross(normal,tangent).normalized;
            for(int j=0;j<steps;j++,index++)
            {
                if(broken&&(index+seed)%29==0)continue;
                var a=Vector3.Lerp(path[k],path[k+1],j/(float)steps);var b=Vector3.Lerp(path[k],path[k+1],(j+1)/(float)steps);
                float wa=width*(.46f+.12f*Mathf.Sin(index*2.7f+seed)),wb=width*(.46f+.12f*Mathf.Sin((index+1)*2.7f+seed));
                build.Face(a-across*wa,b-across*wb,b+across*wb,a+across*wa);
            }
        }
        Add(build.Mesh("Surface inscription"),Vector3.zero,Quaternion.identity,Vector3.one,mat);
    }
    static void PaintRing(Vector3 center,float radius,float width,string material,Quaternion plane,int seed,bool broken=false)
    {
        var path=Enumerable.Range(0,145).Select(i=>{float a=i*Mathf.PI/72;return center+plane*V(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius);}).ToArray();Stroke(path,width,material,plane,seed,broken);
    }
    static void Glyph(Vector3 c,float size,Quaternion q,int variant,string material)
    {
        Func<float,float,Vector3> P=(x,z)=>c+q*V(x*size,0,z*size);
        Stroke(new[]{P(-.28f,-.45f),P(.06f,.45f),P(.3f,-.4f)},size*.065f,material,q,variant);
        Stroke(new[]{P(-.38f,-.06f),P(.38f,.06f)},size*.06f,material,q,variant+4);
        if(variant%3==0)PaintRing(P(0,.05f),size*.22f,size*.045f,material,q,variant);
        else Stroke(new[]{P(variant%2==0?-.36f:.36f,.42f),P(0,.15f),P(0,-.5f)},size*.05f,material,q,variant);
        Stroke(new[]{P(-.18f,-.55f),P(.2f,-.55f)},size*.045f,material,q,variant);
    }
    static void Seal(Vector3 center,float radius,Quaternion q,bool star,int seed)
    {
        PaintRing(center,radius,.065f,"Oxidized_ritual_blood",q,seed);
        PaintRing(center+q*V(0,.002f,0),radius*.94f,.035f,"Scraped_bone_chalk",q,seed+3,true);
        PaintRing(center,radius*.82f,.05f,"Oxidized_ritual_blood",q,seed+7,true);
        if(star)for(int i=0;i<5;i++)
        {
            float a=i*Mathf.PI*2/5,b=(i+2)*Mathf.PI*2/5;
            Stroke(new[]{center+q*V(Mathf.Sin(a)*radius*.8f,0,Mathf.Cos(a)*radius*.8f),center+q*V(Mathf.Sin(b)*radius*.8f,0,Mathf.Cos(b)*radius*.8f)},radius*.024f,"Oxidized_ritual_blood",q,seed+i);
        }
        for(int i=0;i<18;i++)
        {float a=i*360f/18;Glyph(center+q*(Quaternion.Euler(0,a,0)*V(0,0,radius*.885f)),radius*.12f,q*Quaternion.Euler(0,a,0),i+seed,"Scraped_bone_chalk");}
    }
    static void Skull(Vector3 p,Quaternion q,float scale=.92f)
    {Add(Load<Mesh>("Assets/Art/Basement/DetailPolish/Meshes/HumanSkullLOD.asset"),p,q,Vector3.one*scale,"Bone_relic",false);}
    static void Wax(Vector3 p,int seed,int count=5,float size=1)
    {
        for(int j=0;j<count;j++)
        {
            float a=j*2.399f+seed;float r=j==0?0:.12f+.055f*(j%3);var c=p+V(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r)*size;
            float h=(.21f+.065f*((j+seed)%5))*size,w=(.043f+.01f*(j%3))*size;
            var profile=new[]{new Vector2(0,0),new Vector2(w*.8f,0),new Vector2(w,.025f),new Vector2(w,h-.035f),new Vector2(w*.84f,h),new Vector2(w*.53f,h-.015f),new Vector2(0,h-.018f)};
            Add(ChaosGeometry.Lathe(profile,14,.05f,seed+j),c,Quaternion.identity,Vector3.one,"Unbleached_wax");
            Add(ChaosGeometry.Ellipsoid(V(w*2.5f,.018f,w*2.1f),seed+j),c+V(.016f,.007f,0),Quaternion.identity,Vector3.one,"Unbleached_wax");
            Beam(c+V(0,h-.02f,0),c+V(.005f,h+.03f,0),.009f,"Charcoal_residue");
            if(j%3!=2)Add(ChaosGeometry.Lathe(new[]{new Vector2(0,0),new Vector2(.021f*size,.035f*size),new Vector2(.012f*size,.068f*size),new Vector2(0,.10f*size)},9),c+V(0,h,0),Quaternion.Euler(0,j*41,8),Vector3.one,"Small_candle_flame");
            for(int d=0;d<2;d++)Beam(c+V(w*.83f,h-.055f-d*.04f,0),c+V(w*.91f,h*.25f,0),.012f*size,"Unbleached_wax");
        }
    }
    static void Bowl(Vector3 p,float scale=1)
    {
        var profile=new[]{new Vector2(0,0),new Vector2(.12f,0),new Vector2(.2f,.04f),new Vector2(.3f,.18f),new Vector2(.31f,.21f),new Vector2(.28f,.2f),new Vector2(.17f,.055f),new Vector2(0,.04f)};
        Add(ChaosGeometry.Lathe(profile,20,.035f,13),p,Quaternion.identity,Vector3.one*scale,"Ritual_iron");
        Add(ChaosGeometry.Ellipsoid(V(.34f,.012f,.34f),17),p+V(0,.05f*scale,0),Quaternion.identity,Vector3.one*scale,"Smudged_blood");
    }
    static void Light(Vector3 p,Color color,float intensity,float range,string name)
    {
        var l=Group(group,name,p).gameObject.AddComponent<Light>();l.type=LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=LightShadows.None;
        l.GetUniversalAdditionalLightData().usePipelineSettings=false;
    }
    static void Cloth(Vector3 p,float w,float h,Quaternion q,int seed,bool mark=true)
    {
        Add(ChaosGeometry.Cloth(w,h,seed,true),p,q,Vector3.one,"Scorched_funeral_cloth");
        if(mark)
        {
            var plane=q*Quaternion.Euler(-90,0,0);var c=p+q*V(0,-h*.45f,-.10f);
            Seal(c,w*.31f,plane,false,seed);
        }
    }
    static void Crown(Vector3 c,float radius,Quaternion q,int seed)
    {
        Ring(c,radius,.025f,"Ritual_iron",q,seed);Ring(c+q*V(0,.045f,0),radius*.97f,.017f,"Ritual_iron",q,seed+3);
        for(int i=0;i<18;i++)
        {float a=i*2*Mathf.PI/18;var u=V(Mathf.Sin(a),0,Mathf.Cos(a));Beam(c+q*(u*radius),c+q*(u*(radius+.11f)+V(0,.10f,0)),.018f,"Ritual_iron");}
    }
    static void Pentagram()
    {
        var root=PrefabUtility.LoadPrefabContents(Sanctum);
        try
        {
            if(root.transform.Find("Expanded sacrificial seal"))throw new Exception("Restore original snapshot first");
            var old=root.transform.Find("Blood pentagram bone heaps and sacrificial procession");
            foreach(var r in old.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="FinalDriedBlood"))r.enabled=false;
            foreach(var r in root.transform.Find("Blood flooded sacrificial gutters").GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Iron"))r.enabled=false;
            group=Group(root.transform,"Expanded sacrificial seal",Vector3.zero);var center=V(0,.055f,18.5f);
            Seal(center,7.15f,Quaternion.identity,true,3);
            PaintRing(center+V(0,.002f,0),2.22f,.08f,"Oxidized_ritual_blood",Quaternion.identity,15,true);
            PaintRing(center,1.82f,.035f,"Scraped_bone_chalk",Quaternion.identity,8);
            Crown(center+V(0,.008f,0),.66f,Quaternion.identity,8);
            for(int i=0;i<6;i++)
            {
                float a=(30+i*60)*Mathf.Deg2Rad;var p=center+V(Mathf.Sin(a)*7.75f,0,Mathf.Cos(a)*7.75f);p.y=.02f;
                var u=(center-p).normalized;u.y=0;PaintRing(p+V(0,.015f,0),.52f,.04f,"Scraped_bone_chalk",Quaternion.identity,i+4);
                Wax(p+u*.08f,i+7,5,.92f);Bowl(p-u*.37f,.6f);Skull(p+V(-u.z,0,u.x)*.37f,Quaternion.Euler(0,-a*Mathf.Rad2Deg,0),.75f);
                if(i%2==0)Light(p+V(0,.55f,0),new Color(1,.43f,.15f),2.3f,4.4f,"Sixfold offering candle pool");
                Glyph(p+u*.9f+V(0,.015f,0),.6f,Quaternion.Euler(0,-a*Mathf.Rad2Deg,0),i,"Oxidized_ritual_blood");
            }
            for(int side=-1;side<=1;side+=2)
            {
                Cloth(V(side*12,9.8f,34.85f),2.8f,5.4f,Quaternion.identity,4+side);
                Box(V(side*12,9.9f,34.85f),V(3.1f,.09f,.12f),"Ritual_iron");
                var p=V(side*11.8f,.015f,30);Wax(p,side+5,8,1.15f);Bowl(p+V(-side*.6f,0,.2f));
                Light(p+V(0,.6f,0),new Color(1,.43f,.17f),2.7f,5.8f,"Rear funeral candle shrine");
            }
            Flush("Sanctum");PrefabUtility.SaveAsPrefabAsset(root,Sanctum);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Rack(Vector3 c,int side,int seed)
    {
        Quaternion q=Quaternion.Euler(0,side<0?90:-90,0);Func<Vector3,Vector3> P=p=>c+q*p;
        foreach(float x in new[]{-.95f,.95f})Beam(P(V(x,0,0)),P(V(x,3.3f,0)),.06f,"Ritual_iron");
        Beam(P(V(-1.05f,3.15f,0)),P(V(1.05f,3.15f,0)),.07f,"Ritual_iron");
        Add(ChaosGeometry.BevelBox(V(1.8f,2.55f,.07f),.015f),P(V(0,1.6f,.06f)),q,Vector3.one,"Ritual_iron");
        for(int i=0;i<3;i++)
        {
            float x=-.6f+i*.6f,y=2.25f+(i%2)*.3f;
            Tube(new[]{P(V(x,3.15f,0)),P(V(x+.035f,2.8f,.1f)),P(V(x,y+.18f,.21f))},.012f,"Ritual_iron");
            Skull(P(V(x,y,.21f)),q*Quaternion.Euler(0,180+i*9-12,0),.85f);
            var center=P(V(x,y-.3f,.28f));Crown(center,.18f,q*Quaternion.Euler(90,0,0),i+seed);
        }
        Wax(P(V(.25f,.01f,-.65f)),seed,7,1.2f);Bowl(P(V(-.5f,.01f,-.65f)),.7f);
        var paintPlane=q*Quaternion.Euler(90,0,0);Seal(P(V(0,1.35f,.12f)),.62f,paintPlane,false,seed);
    }
    static void PreparationHall()
    {
        var root=PrefabUtility.LoadPrefabContents(Arena);
        try
        {
            if(root.transform.Find("Ritual preparation and votive procession"))throw new Exception("Restore snapshot first");
            group=Group(root.transform,"Ritual preparation and votive procession",Vector3.zero);
            Seal(V(0,.052f,2.4f),3.6f,Quaternion.identity,true,5);
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<3;i++)
                {
                    float z=-8+i*10.3f;float support=side<0&&z> -8.5f&&z< -.5f || side>0&&z>.5f&&z<8.5f?1:0;
                    Rack(V(side*18.8f,.02f+support,z),side,i+6);
                    Light(V(side*18.15f,.75f+support,z),new Color(1,.47f,.19f),2.5f,5,"Ritual preparation waxlight");
                }
                for(int i=0;i<2;i++)
                {
                    float z=-3+i*12;var q=Quaternion.Euler(0,side<0?-90:90,0);
                    Cloth(V(side*19.2f,7.5f,z),2.8f,3.5f,q,i+11+side);
                }
                var table=V(side*15.3f,1.02f,side<0?-4.2f:7.1f);
                Box(table+V(0,.9f,0),V(2.25f,.13f,1.15f),"Worn_offering_stone");
                foreach(float dx in new[]{-.9f,.9f})foreach(float dz in new[]{-.4f,.4f})Beam(table+V(dx,.02f,dz),table+V(dx,.87f,dz),.07f,"Ritual_iron");
                Bowl(table+V(-.45f,.98f,0));Wax(table+V(.62f,.98f,.13f),7+side,5,.75f);Skull(table+V(.1f,.98f,-.12f),Quaternion.Euler(0,side*28,0));
                Crown(table+V(-.1f,.99f,.35f),.27f,Quaternion.identity,3);
            }
            for(int i=0;i<11;i++)
            {
                float z=-11+i*2.6f;float x=-1.1f+Mathf.Sin(i*.42f)*.38f;
                Stroke(new[]{V(x,.05f,z-.55f),V(x+.05f,.05f,z+.55f)},.085f,"Oxidized_ritual_blood",Quaternion.identity,i,true);
                if(i%2==0)Glyph(V(x+.7f,.052f,z),.52f,Quaternion.Euler(0,i*7,0),i,"Scraped_bone_chalk");
            }
            var overhead=V(0,7.55f,4.8f);Crown(overhead,1.05f,Quaternion.identity,13);
            for(int i=0;i<3;i++)
            {float a=i*Mathf.PI*2/3;var p=overhead+V(Mathf.Sin(a),0,Mathf.Cos(a));Tube(new[]{p,p+V(0,.5f,0),V(p.x,9.1f,p.z)},.014f,"Ritual_iron");}
            Flush("Preparation");PrefabUtility.SaveAsPrefabAsset(root,Arena);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Passages()
    {
        var root=PrefabUtility.LoadPrefabContents(Room);
        try
        {
            var boss=root.transform.Find("BossRoom");var passage=boss.Find("Player sized corridor");
            var floorRenderer=root.transform.Find("PreBossArea/Floor/Hall_Floor").GetComponent<MeshRenderer>();
            var previous=floorRenderer.sharedMaterial;var worn=Material("Trampled_ritual_concrete",AssetDatabase.GetAssetPath(previous),previous.GetColor("_BaseColor")*new Color(.75f,.70f,.65f));
            floorRenderer.sharedMaterial=worn;
            var pre=root.transform.Find("PreBossArea/BasementFinalArena");
            if(!pre.Find("Ritual preparation and votive procession"))
            {
                var source=Load<GameObject>(Arena).transform.Find("Ritual preparation and votive procession");
                var copy=Object.Instantiate(source.gameObject,pre,false);copy.name=source.name;
            }
            if(passage.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Ritual passage thresholds"))throw new Exception("Restore snapshot first");
            Physics.SyncTransforms();int i=0;
            foreach(var floor in passage.GetComponentsInChildren<BoxCollider>(true).Where(c=>c.name.StartsWith("Passage Floor")))
            {
                // The map overrides each floor's pose. Anchor to that floor's top surface.
                var scale=floor.transform.lossyScale;var g=Group(floor.transform,"Ritual passage thresholds",floor.center+V(0,floor.size.y*.5f+.012f/Mathf.Abs(scale.y),0));group=g;
                g.localScale=V(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
                var size=Vector3.Scale(floor.size,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z)));
                float radius=Mathf.Min(size.x,size.z)*.24f;Seal(V(0,.018f,0),radius,Quaternion.identity,false,i+5);
                bool alongX=size.x>size.z;float side=(alongX?size.z:size.x)*.5f-.25f;
                Vector3 p=alongX?V(0,0,side):V(side,0,0);Wax(p,i+3,4,.65f);Bowl(p+V(0,0,-.32f),.5f);
                Light(p+V(0,.42f,0),new Color(1,.43f,.16f),1.9f,3.6f,"Corridor votive candles");
                var q=alongX?Quaternion.identity:Quaternion.Euler(0,90,0);var plane=q*Quaternion.Euler(-90,0,0);
                Seal(p+q*V(0,1.55f,.025f),.52f,plane,false,i+8);
                Flush("Passage_"+i++);
            }
            // Replace the subtle ochre tubes around the old star with the new painted seal.
            var oldInk=Load<Material>("Assets/Art/Basement/Ritual/Materials/Dry_ochre_inscription.mat");
            foreach(var r in boss.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.sharedMaterial==oldInk))r.enabled=false;
            foreach(var t in boss.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Boss ritual floor"))
                foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
            PrefabUtility.SaveAsPrefabAsset(root,Room);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    public static void Build()
    {
        BasementSurfaceBuilder.RequireIsolated();
        foreach(string dir in new[]{"Materials","Meshes"})Directory.CreateDirectory(Art+"/"+dir);AssetDatabase.Refresh();
        Capture("BeforeExpansion");Materials();Pentagram();PreparationHall();Passages();AssetDatabase.SaveAssets();Capture("FinalExpansion");
        Debug.Log("BASEMENT_EXPANSION_BUILD_OK");
    }
    public static void Refine()
    {
        BasementSurfaceBuilder.RequireIsolated();
        foreach(string target in new[]{Room,Arena,Sanctum})
        {
            string original=Path.GetFullPath("../BasementRitualExpansion/before/"+target);
            File.WriteAllBytes(target,File.ReadAllBytes(original));AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceUpdate);
        }
        Capture("BeforeExpansion");Materials();Pentagram();PreparationHall();Passages();AssetDatabase.SaveAssets();Capture("FinalExpansion");Debug.Log("BASEMENT_EXPANSION_REFINED");
    }
    public static void FixPassageAnchors()
    {
        BasementSurfaceBuilder.RequireIsolated();File.WriteAllBytes(Room,File.ReadAllBytes(Path.GetFullPath("../BasementRitualExpansion/before/"+Room)));
        AssetDatabase.ImportAsset(Room,ImportAssetOptions.ForceUpdate);Materials();Passages();AssetDatabase.SaveAssets();Capture("FinalExpansion");Debug.Log("BASEMENT_EXPANSION_PASSAGES_FIXED");
    }
    public static void Capture(string prefix)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");ShaderUtil.allowAsyncCompilation=false;
        var room=Object.FindObjectsByType<Transform>().First(t=>t.name=="Basement"&&!t.parent);var arena=room.Find("PreBossArea");var boss=room.Find("BossRoom");
        float af=BasementFinalBuilder.Floor(arena,"Hall_Floor"),bf=BasementFinalBuilder.Floor(boss,"Main_Floor"),z=boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
        var method=typeof(BasementSurfaceBuilder).GetMethod("Render",BindingFlags.NonPublic|BindingFlags.Static);
        Action<Transform,Vector3,Vector3,string,bool> R=(coords,eye,look,name,flash)=>method.Invoke(null,new object[]{room,coords,eye,look,prefix+"-"+name,flash});
        R(boss,V(0,bf+1.84f,z+2),V(-10,bf+2.8f,z+23),"Arena",false);
        R(boss,V(-4,bf+5.7f,z+10),V(-10,bf,z+18.5f),"Pentagram",false);
        R(boss,V(-10,bf+1.84f,z+10),V(-10,bf+.04f,z+20),"PentagramPlayer",true);
        R(arena,V(2,af+1.84f,-12),V(13,af+2.2f,14),"PreArena",false);
        R(arena,V(24,af+2.84f,10.5f),V(27.8f,af+2.4f,3.2f),"Preparation",true);
        var floor=boss.Find("Player sized corridor/Passage Floor Z").GetComponent<BoxCollider>();Physics.SyncTransforms();
        var eye=floor.bounds.center;eye.y=floor.bounds.max.y+1.84f;R(boss,boss.InverseTransformPoint(eye),boss.InverseTransformPoint(eye+floor.transform.forward*3),"Passage",false);
        var b=floor.bounds;bool alongX=b.size.x>b.size.z;var axis=alongX?Vector3.right:Vector3.forward;
        var detailEye=V(b.center.x,b.max.y+1.7f,b.center.z)-axis*Mathf.Min(2.0f,(alongX?b.size.x:b.size.z)*.3f);
        var detailLook=V(b.center.x,b.max.y+.45f,b.center.z)+(alongX?Vector3.forward:Vector3.right)*.6f;
        R(boss,boss.InverseTransformPoint(detailEye),boss.InverseTransformPoint(detailLook),"PassageDetail",true);
    }
    public static void Verify()
    {
        BasementSurfaceBuilder.RequireIsolated();Capture("VerifiedExpansion");
        var room=Object.FindObjectsByType<Transform>().First(t=>t.name=="Basement"&&!t.parent);
        foreach(var name in new[]{"Expanded sacrificial seal","Ritual preparation and votive procession","Ritual passage thresholds"})
            if(!room.GetComponentsInChildren<Transform>(true).Any(t=>t.name==name))throw new Exception("Missing "+name);
        var report=new List<string>();
        foreach(var t in room.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Expanded sacrificial seal"||t.name=="Ritual preparation and votive procession"||t.name=="Ritual passage thresholds"))
        {
            if(t.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("New blocker "+t.name);
            int triangles=0;
            foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true))
            {
                var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)throw new Exception("Mesh absent");triangles+=f.sharedMesh.triangles.Length/3;
                foreach(var m in r.sharedMaterials){if(!m||!m.shader)throw new Exception("Material absent");foreach(var msg in ShaderUtil.GetShaderMessages(m.shader))if(msg.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(msg.message);}
            }
            if(triangles==0)throw new Exception("Empty dressing group: "+t.name);
            report.Add(t.name+" ("+t.parent.name+"): "+triangles+" triangles; "+t.GetComponentsInChildren<MeshRenderer>().Length+" material batches; 0 colliders.");
        }
        File.WriteAllLines("Preview/Expansion-validation.txt",report);Debug.Log("BASEMENT_EXPANSION_VERIFIED\n"+string.Join("\n",report));
    }
}
