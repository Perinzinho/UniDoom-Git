using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TotalChaosStyleBuilder
{
    public const string Art = "Assets/Art/TotalChaosInspired";
    public static readonly string[] Targets = {
        "Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab",
        "Assets/prefabs/Scenario/Basement.prefab",
        "Assets/Art/Basement/BasementFinalArena.prefab",
        "Assets/Art/Basement/BasementFinalSanctum.prefab",
        "Assets/prefabs/Scenario/Cylinder.prefab" };
    const string DetailName = "Total Chaos inspired material and geometry detail";
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static int chamfers;
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    public static void Build()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/")) throw new Exception("Generate only in an isolated Unity project");
        foreach(string target in Targets)
        {
            string baseline=Path.GetFullPath(Path.Combine(Application.dataPath,"../../TotalChaos-before",target));
            if(!File.Exists(baseline))throw new Exception("Missing preserved baseline: "+baseline);
            File.Copy(baseline,target,true);AssetDatabase.ImportAsset(target,ImportAssetOptions.ForceUpdate);
        }
        Directory.CreateDirectory(Art+"/Meshes");Directory.CreateDirectory(Art+"/Materials");Directory.CreateDirectory(Art+"/Textures");AssetDatabase.Refresh();
        foreach(string room in new[]{"ClassRoom","Basement","BasementFinalArena","BasementFinalSanctum","Cylinder"})Directory.CreateDirectory(Art+"/Materials/"+room);
        AssetDatabase.Refresh();
        materials.Clear();chamfers=0;Textures();
        foreach(string path in Targets)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                string room=path.Contains("ClassRoom")?"ClassRoom":path.Contains("Cylinder")?"Cylinder":Path.GetFileNameWithoutExtension(path);
                foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true).ToArray())
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(r) || r.GetComponent<TextMesh>()) continue;
                    r.sharedMaterials=r.sharedMaterials.Select(m=>Rematerial(m,room)).ToArray();
                }
                foreach(var f in root.GetComponentsInChildren<MeshFilter>().ToArray())
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(f)||!f.sharedMesh||f.GetComponent<TextMesh>()||f.GetComponent<MonoBehaviour>()||AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Art))continue;
                    var renderer=f.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled||Family(renderer.sharedMaterial)==null)continue;
                    var mesh=Object.Instantiate(f.sharedMesh);var uv=mesh.uv;var vertices=mesh.vertices;var normals=mesh.normals;Vector3 scale=f.transform.lossyScale;
                    if(uv.Length!=vertices.Length||normals.Length!=vertices.Length){Object.DestroyImmediate(mesh);continue;}
                    // Map planar primitive faces in metres so a 40 m wall does not stretch a one-metre surface.
                    for(int i=0;i<vertices.Length;i++)
                    {
                        Vector3 n=normals[i];int planar=(Mathf.Abs(n.x)>.999f?0:Mathf.Abs(n.y)>.999f?1:Mathf.Abs(n.z)>.999f?2:-1);
                        if(planar<0)continue;Vector3 p=Vector3.Scale(vertices[i],scale)/1.4f;
                        uv[i]=planar==0?new Vector2(p.z,p.y):planar==1?new Vector2(p.x,p.z):new Vector2(p.x,p.y);
                    }
                    mesh.uv=uv;mesh.RecalculateTangents();f.sharedMesh=SaveMesh(room+"_SurfaceUV_"+StablePath(f.transform,root.transform),mesh);
                }
                foreach(var f in root.GetComponentsInChildren<MeshFilter>(true).ToArray())
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(f) || !f.sharedMesh || !ShouldChamfer(f.transform,path))continue;
                    int count;var mesh=ChaosGeometry.ChamferCubes(f.sharedMesh,.024f,out count);
                    if(count>0){chamfers+=count;f.sharedMesh=SaveMesh(room+"_Bevel_"+StablePath(f.transform,root.transform),mesh);}else Object.DestroyImmediate(mesh);
                }
                if(room=="Cylinder"){Doors(root.transform);CylinderDetails(root.transform.Find("Abandoned circular atrium"));}
                else if(room=="ClassRoom")ClassRoomDetails(root.transform);
                else if(room=="Basement")BasementDetails(root.transform);
                else if(room=="BasementFinalSanctum")SanctumCloth(root.transform);
                else ArenaCloth(root.transform);
                foreach(var group in root.GetComponentsInChildren<Transform>().Where(t=>t.name==DetailName&&!PrefabUtility.IsPartOfPrefabInstance(t)).ToArray())CombineDetails(group,room+"_"+StablePath(group,root.transform));
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();Directory.CreateDirectory("Preview");File.WriteAllText("Preview/TotalChaos-geometry.txt","Original authored geometry; chamfered primitive solids: "+chamfers+"\n");
        Finish();Debug.Log("TOTAL_CHAOS_CHAMFERS_OK solids="+chamfers);
    }
    public static void Finish()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/"))throw new Exception("Use an isolated project");
        foreach(string path in Directory.GetFiles(Art+"/Materials","*.mat",SearchOption.AllDirectories))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(path.Replace('\\','/'));
            if(m&&m.GetTexture("_MetallicGlossMap")){m.SetFloat("_Smoothness",.85f);EditorUtility.SetDirty(m);}
        }
        foreach(string path in Targets)
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var f in root.GetComponentsInChildren<MeshFilter>())
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(f)||!f.sharedMesh||!AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Art))continue;
                    var r=f.GetComponent<MeshRenderer>();if(!r||!r.enabled)continue;
                    bool drape=f.name.Contains("Folded");
                    if(f.name.Contains("_Combined_"))drape|=Family(r.sharedMaterial)=="Fabric"||r.sharedMaterial.name=="DecayPaper";
                    if(drape){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;}
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();TotalChaosStyleVerifier.VerifyVisuals();Debug.Log("TOTAL_CHAOS_STYLE_BUILD_OK");
    }
    static string StablePath(Transform t,Transform root)
    {
        var parts=new List<string>();for(;t!=root&&t;t=t.parent)parts.Add(t.GetSiblingIndex().ToString());parts.Reverse();return string.Join("_",parts);
    }
    static bool ShouldChamfer(Transform t,string path)
    {
        string ancestors="";for(var p=t;p;p=p.parent)ancestors+="/"+p.name;
        if(ancestors.Contains(DetailName)||t.GetComponent<TextMesh>()||t.GetComponent<MeshCollider>())return false;
        if(path.Contains("ClassRoom"))return ancestors.Contains("Student desk")||ancestors.Contains("Student chair")||ancestors.Contains("Teacher desk")||ancestors.Contains("workstation")||ancestors.Contains("lectern")||ancestors.Contains("projector")||ancestors.Contains("supply table");
        if(path.Contains("Cylinder"))return ancestors.Contains("Cafe table cluster")||ancestors.Contains("emergency radio")||ancestors.Contains("vending machine")||ancestors.Contains("bench and abandoned")||ancestors.Contains("Clock stopped");
        return ancestors.Contains("Electrical control cabinet")||ancestors.Contains("Storage crate")||ancestors.Contains("shrine pillar")||ancestors.Contains("machinery");
    }
    static string Family(Material m)
    {
        string n=m.name.ToLowerInvariant();
        if(n.Contains("lettering")||n.Contains("sky")||n.Contains("glass")||n=="wet"||n.Contains("tube")||n.Contains("light")||n.Contains("leaf")||n=="map"||n=="board"||n=="chalk"||n=="black"||n=="dark"||n.Contains("soot")||n.Contains("dust")||n.Contains("paper"))return null;
        if(m.IsKeywordEnabled("_EMISSION")||m.shader.name.Contains("Unlit"))return null;
        if(n.Contains("blood"))return "Blood";
        if(n.Contains("shroud")||n.Contains("coat")||n.Contains("cloth")||n.Contains("face")||n.Contains("canvas")||n.Contains("tarp")||n.Contains("trouser"))return "Fabric";
        if(n.Contains("flesh")||n.Contains("organic")||n.Contains("chitin"))return "Organic";
        if(n.Contains("wood")||n.Contains("book"))return "Wood";
        if(n.Contains("door")||n.Contains("paint"))return "Paint";
        if(n.Contains("rust"))return "Rust";
        if(n.Contains("iron")||n.Contains("steel"))return "Metal";
        if(n.Contains("mosaic")||n.Contains("band"))return null;
        return "Masonry";
    }
    static Material Rematerial(Material source,string room)
    {
        if(!source)return source;string family=Family(source);if(family==null)return source;
        string key=room+"_"+source.name;if(materials.TryGetValue(key,out var existing))return existing;
        var created=new Material(source){name=source.name,enableInstancing=true};
        created.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+family+"_Albedo.png"));
        created.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+family+"_Normal.png"));
        created.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/"+family+"_MetalRough.png"));
        created.EnableKeyword("_NORMALMAP");created.EnableKeyword("_METALLICSPECGLOSSMAP");created.SetFloat("_BumpScale",family=="Fabric"?.34f:.48f);
        created.SetTextureScale("_BaseMap",Vector2.one);created.SetTextureScale("_BumpMap",Vector2.one);created.SetTextureScale("_MetallicGlossMap",Vector2.one);
        if(source.name.StartsWith("SealedDoor")||source.name=="DoorActive")created.SetColor("_BaseColor",new Color(.48f,.53f,.45f));
        string target=Art+"/Materials/"+room+"/"+source.name+".mat";existing=AssetDatabase.LoadAssetAtPath<Material>(target);
        if(existing){EditorUtility.CopySerialized(created,existing);Object.DestroyImmediate(created);}else{AssetDatabase.CreateAsset(created,target);existing=created;}
        materials[key]=existing;return existing;
    }
    static void CombineDetails(Transform root,string key)
    {
        var filters=root.GetComponentsInChildren<MeshFilter>();
        foreach(var material in filters.Select(f=>f.GetComponent<MeshRenderer>().sharedMaterial).Distinct())
        {
            var selected=filters.Where(f=>f.GetComponent<MeshRenderer>().sharedMaterial==material).ToArray();
            var instances=selected.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray();
            var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(instances,true,true);mesh.RecalculateBounds();
            Shape(root,key+"_Combined_"+material.name,mesh,Vector3.zero,material);
        }
        foreach(var f in filters){Object.DestroyImmediate(f.GetComponent<MeshRenderer>());Object.DestroyImmediate(f);}
    }
    static Material Mat(string room,string name)
    {
        string folder=room=="ClassRoom"?"ClassRoom":room=="Cylinder"?"Cylinder":"Basement";
        return Rematerial(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/"+folder+"/Materials/"+name+".mat"),room);
    }
    public static Mesh SaveMesh(string name,Mesh created)
    {
        string path=Art+"/Meshes/"+name+".asset";created.name=name;var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(created,old);Object.DestroyImmediate(created);return old;}AssetDatabase.CreateAsset(created,path);return created;
    }
    static Transform Group(Transform parent,string name,Vector3 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static Transform Details(Transform parent)
    {var old=parent.Find(DetailName);if(old)Object.DestroyImmediate(old.gameObject);return Group(parent,DetailName,Vector3.zero);}
    static GameObject Shape(Transform parent,string name,Mesh mesh,Vector3 pos,Material material,Vector3? rotation=null,Vector3? scale=null)
    {
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=pos;
        if(rotation.HasValue)go.transform.localRotation=Quaternion.Euler(rotation.Value);if(scale.HasValue)go.transform.localScale=scale.Value;
        go.AddComponent<MeshFilter>().sharedMesh=SaveMesh(name,mesh);go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
    }
    static void Pipe(Transform parent,string name,IReadOnlyList<Vector3> points,float radius,Material mat)
    {Shape(parent,name,ChaosGeometry.Sweep(points,radius,16),Vector3.zero,mat);}
    static void Box(Transform parent,string name,Vector3 p,Vector3 size,Material mat)
    {Shape(parent,name,ChaosGeometry.BevelBox(size,.025f),p,mat);}
    static Mesh DoorSkin()
    {
        var b=new ChaosGeometry.Builder();
        Action<Vector3,Vector3> box=(p,s)=>{var m=ChaosGeometry.BevelBox(s,.009f);b.Append(m,Matrix4x4.Translate(p));Object.DestroyImmediate(m);};
        box(V(0,1.635f,0),V(.14f,3.25f,1.85f));
        foreach(float z in new[]{-.8f,.8f})box(V(.084f,1.635f,z),V(.025f,3.05f,.065f));
        foreach(float y in new[]{.18f,1.95f,3.10f})box(V(.084f,y,0),V(.025f,.055f,1.6f));
        for(int i=0;i<6;i++)box(V(.086f,.48f+i*.09f,0),V(.032f,.035f,.9f));
        return b.Mesh("Standard 1.85 x 3.25 m campus door");
    }
    static void Doors(Transform root)
    {
        var atrium=root.Find("Abandoned circular atrium");var mat=Mat("Cylinder","SealedDoor");var moving=Mat("Cylinder","DoorActive");
        var template=SaveMesh("Cylinder_StandardDoorLeaf",DoorSkin());
        foreach(Transform bay in atrium)
        {
            if(!bay.name.StartsWith("Service bay "))continue;
            var image=bay.GetComponentsInChildren<MeshRenderer>().FirstOrDefault(r=>r.sharedMaterial&&r.sharedMaterial.name.StartsWith("SealedDoor"));
            var rig=bay.GetComponentInChildren<OpenDoorWithKey>();
            if(image)
            {
                var b=new ChaosGeometry.Builder();b.Append(template,Matrix4x4.TRS(V(0,0,-.345f),Quaternion.Euler(0,90,0),Vector3.one));
                image.GetComponent<MeshFilter>().sharedMesh=SaveMesh("Cylinder_StandardSealedLeaf",b.Mesh("Sealed leaf"));image.sharedMaterial=mat;
                var frame=Group(bay,"Standard sealed door frame",Vector3.zero);
                var boxes=new ChaosGeometry.Builder();
                foreach(float x in new[]{-1.02f,1.02f}){var m=ChaosGeometry.BevelBox(V(.09f,3.36f,.22f),.008f);boxes.Append(m,Matrix4x4.Translate(V(x,1.68f,-.345f)));Object.DestroyImmediate(m);}
                var lintel=ChaosGeometry.BevelBox(V(2.13f,.10f,.22f),.008f);boxes.Append(lintel,Matrix4x4.Translate(V(0,3.36f,-.345f)));Object.DestroyImmediate(lintel);
                Shape(frame,"Cylinder_StandardFrame",boxes.Mesh("Standard frame"),Vector3.zero,Mat("Cylinder","Iron"));
            }
            if(rig)
            {
                var leaf=rig.transform.Find("Door");
                foreach(var renderer in leaf.GetComponentsInChildren<MeshRenderer>())
                {
                    if(renderer.sharedMaterial.name=="DoorActive"){renderer.GetComponent<MeshFilter>().sharedMesh=template;renderer.sharedMaterial=moving;}
                    else renderer.enabled=false;
                }
                Pipe(leaf,"Cylinder_UsableLeverHandle",ChaosGeometry.Curve(V(.09f,1.36f,.63f),V(.20f,1.36f,.63f),V(.20f,1.36f,.43f),V(.19f,1.36f,.39f),12),.022f,Mat("Cylinder","Concrete"));
                Box(leaf,"Cylinder_HandleBackplate",V(.083f,1.36f,.63f),V(.025f,.22f,.075f),Mat("Cylinder","Iron"));
            }
        }
    }
    static void ClassRoomDetails(Transform room)
    {
        var d=Details(room);var metal=Mat("ClassRoom","Iron");var cloth=Mat("ClassRoom","CoveredFace");var rubble=Mat("ClassRoom","Rubble");
        // The side wall is away from the tested aisle. Curves, connectors and smooth drapes replace the blockout impression.
        var services=Group(d,"Rounded classroom radiator and severed services",V(11.61f,0,-7.3f));
        for(int i=0;i<11;i++)
            Pipe(services,"ClassRoom_RadiatorLoop_"+i,ChaosGeometry.Curve(V(-.10f,.18f,i*.12f),V(-.10f,1.0f,i*.12f),V(-.10f,1.0f,i*.12f+.08f),V(-.10f,.18f,i*.12f+.08f)),.032f,metal);
        Pipe(d,"ClassRoom_DroopingMonitorCable",ChaosGeometry.Curve(V(9.2f,1.5f,4.13f),V(9.7f,.2f,4.2f),V(8.8f,.025f,3.9f),V(8.4f,.03f,3.25f)),.014f,Mat("ClassRoom","Black"));
        foreach(var victim in room.GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("victim")||t.name.Contains("Covered adult body")).ToArray())
        {
            foreach(var r in victim.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.name=="Tarp"))r.enabled=false;
            Shape(victim,"ClassRoom_FoldedVictimShroud_"+victim.GetSiblingIndex(),ChaosGeometry.Cloth(.95f,2.50f,victim.GetSiblingIndex()),V(0,.08f,-.30f),cloth);
        }
        Shape(d,"ClassRoom_RippedProjectionDrape",ChaosGeometry.Cloth(2.10f,1.38f,21,true),V(11.77f,2.40f,-1.25f),Mat("ClassRoom","Canvas"),V(0,-90,0));
        for(int i=0;i<22;i++)Shape(d,"ClassRoom_FracturedTile_"+i,ChaosGeometry.Rubble(901+i),V(10.2f+(i%4)*.38f,.023f,-10+(i/4)*.54f),rubble,V(0,i*39,0),V(.5f,.32f,.6f));
        // A curved shell, rolled rim and hollow interior instead of a box-shaped cup.
        foreach(int row in new[]{0,2,4,6})
            Shape(d,"ClassRoom_ColdCoffeeVessel_"+row,ChaosGeometry.Lathe(new[]{new Vector2(0,0),new Vector2(.035f,0),new Vector2(.040f,.11f),new Vector2(.034f,.11f),new Vector2(.029f,.013f),new Vector2(0,.013f)}),V(new[]{-4.4f,.4f,7.3f,-4.4f}[row/2]+.51f,1.08f,new[]{-9.65f,-5.4f,-1.15f,1.85f}[row/2]-.21f),Mat("ClassRoom","Paper"));
    }
    static void BasementDetails(Transform room)
    {
        foreach(bool boss in new[]{false,true})
        {
            var area=room.Find(boss?"BossRoom":"PreBossArea");var d=Details(area);float floor=BasementFinalBuilder.Floor(area,boss?"Main_Floor":"Hall_Floor");
            float cx=boss?-10:10,front=boss?area.Find("Walls/Entrance_Wall_Header").localPosition.z:-15;
            var network=Group(d,"Curved pumping manifold with flanges and valves",V(cx+(boss?-19.4f:18.5f),floor,front+9));
            var iron=Mat("Basement","Iron");var rust=Mat("Basement","Rust");
            for(int i=0;i<4;i++)
            {
                var points=ChaosGeometry.Curve(V(0,.3f,i*1.1f),V(0,3.8f,i*1.1f),V(-1.8f,3.8f,i*1.1f),V(-1.8f,5.8f,i*1.1f),32);
                Pipe(network,(boss?"Boss":"Arena")+"_SweptPressurePipe_"+i,points,.13f,iron);
                foreach(float y in new[]{.7f,1.6f})
                {
                    Shape(network,(boss?"Boss":"Arena")+"_PipeFlange_"+i+"_"+(int)(y*10),ChaosGeometry.Lathe(new[]{new Vector2(.12f,-.055f),new Vector2(.23f,-.055f),new Vector2(.23f,.055f),new Vector2(.12f,.055f)},28),V(0,y,i*1.1f),rust);
                    for(int bolt=0;bolt<8;bolt++){float a=bolt*Mathf.PI/4;Shape(network,(boss?"Boss":"Arena")+"_FlangeBolt_"+i+"_"+(int)(y*10)+"_"+bolt,ChaosGeometry.Lathe(new[]{new Vector2(0,-.065f),new Vector2(.026f,-.065f),new Vector2(.026f,.065f),new Vector2(0,.065f)},6),V(Mathf.Cos(a)*.18f,y,Mathf.Sin(a)*.18f+i*1.1f),iron);}
                }
                var wheel=new List<Vector3>();for(int j=0;j<=40;j++){float a=j*Mathf.PI/20;wheel.Add(V(-.24f+Mathf.Cos(a)*.26f,2.3f+Mathf.Sin(a)*.26f,i*1.1f-.2f));}
                Pipe(network,(boss?"Boss":"Arena")+"_CastValveWheel_"+i,wheel,.024f,rust);
                Pipe(network,(boss?"Boss":"Arena")+"_ValveStem_"+i,new[]{V(0,2.3f,i*1.1f),V(-.24f,2.3f,i*1.1f-.2f)},.042f,iron);
                for(int spoke=0;spoke<4;spoke++){float a=spoke*Mathf.PI/2;Pipe(network,(boss?"Boss":"Arena")+"_ValveSpoke_"+i+"_"+spoke,new[]{V(-.24f,2.3f,i*1.1f-.2f),V(-.24f+Mathf.Cos(a)*.26f,2.3f+Mathf.Sin(a)*.26f,i*1.1f-.2f)},.018f,iron);}
            }
            for(int i=0;i<20;i++)Shape(d,(boss?"Boss":"Arena")+"_FracturedConcrete_"+i,ChaosGeometry.Rubble(100+i),V(cx+(boss?-19:18)+(i%3)*.22f,floor+.02f,front+6+i*.45f),Mat("Basement","Stone"),V(0,i*31,0),V(.8f,.65f,1));
            Pipe(d,(boss?"Boss":"Arena")+"_SaggingRoofService",ChaosGeometry.Curve(V(cx-17,floor+(boss?10.7f:8.8f),front+5),V(cx-8,floor+(boss?9.6f:7.6f),front+6),V(cx+10,floor+(boss?9.7f:7.4f),front+12),V(cx+17,floor+(boss?10.7f:8.8f),front+20)),.04f,iron);
        }
        foreach(var drum in room.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Storage drum")).ToArray())
        {
            // The existing collider and scale stay authoritative. Add profiled hoops and dents in its own local space.
            foreach(var r in drum.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
            var profile=new[]{new Vector2(0,0),new Vector2(.33f,0),new Vector2(.35f,.04f),new Vector2(.35f,.09f),new Vector2(.33f,.12f),new Vector2(.33f,.34f),new Vector2(.357f,.36f),new Vector2(.357f,.41f),new Vector2(.33f,.43f),new Vector2(.33f,.78f),new Vector2(.357f,.8f),new Vector2(.357f,.85f),new Vector2(.33f,.87f),new Vector2(.33f,1.15f),new Vector2(.35f,1.17f),new Vector2(.35f,1.20f),new Vector2(0,1.20f)};
            profile=profile.Select(p=>new Vector2(p.x,p.y*1.06f/1.20f)).ToArray();
            Shape(drum,"Basement_ProfiledStorageDrum_"+drum.GetSiblingIndex(),ChaosGeometry.Lathe(profile,40,.045f,drum.GetSiblingIndex()),Vector3.zero,Mat("Basement","Rust"));
        }
    }
    static void ArenaCloth(Transform room)
    {var d=Details(room);Shape(d,"Arena_FoldedMortuarySheet",ChaosGeometry.Cloth(.95f,2.5f,3),V(-15.3f,1.10f,10),Mat("BasementFinalArena","FinalShroud"),V(0,90,0));}
    static void SanctumCloth(Transform room)
    {
        var d=Details(room);var cloth=Mat("BasementFinalSanctum","FinalShroud");
        for(int i=0;i<6;i++)Shape(d,"Boss_HangingFoldedShroud_"+i,ChaosGeometry.Cloth(.67f,2.3f,i,true),V(-8.6f+i*3.45f,7.80f+(i%2)*1.30f,28.45f),cloth,V(0,i*13,0));
        for(int i=0;i<2;i++)Shape(d,"Boss_FoldedRemainsSheet_"+i,ChaosGeometry.Cloth(.77f,2.05f,11+i),V(-9.5f+i*2.7f,.12f,12.5f+i*.4f),cloth,V(0,70,0));
        var arch=room.Find("Colossal rib cage around the infernal portal");
        foreach(var renderer in arch.GetComponentsInChildren<MeshRenderer>().Where(r=>r.sharedMaterial.name=="FinalBone"))renderer.enabled=false;
        foreach(int side in new[]{-1,1})
        {
            for(int i=0;i<7;i++)
            {
                float y=1.5f+i*1.4f;
                var points=ChaosGeometry.Curve(V(side*7.9f,y,34.85f),V(side*7.3f,y+.65f,34.75f),V(side*5.7f,y+.9f,33.75f),V(side*5.3f,y+.9f,33.6f),32);
                Shape(d,"Boss_CurvedCalcifiedRib_"+side+"_"+i,ChaosGeometry.Sweep(points,.26f,16,.11f),Vector3.zero,Mat("BasementFinalSanctum","FinalBone"));
            }
            for(int i=0;i<3;i++)Shape(d,"Boss_TaperedPortalSpike_"+side+"_"+i,ChaosGeometry.Sweep(ChaosGeometry.Curve(V(side*(1.6f+i*1.6f),10.3f,34.8f),V(side*(1.9f+i*1.9f),10.9f,34.7f),V(side*(1.9f+i*1.9f),11.6f,34.4f),V(side*(1.9f+i*1.9f),11.8f,34.25f)),.14f,12,.006f),Vector3.zero,Mat("BasementFinalSanctum","FinalBone"));
        }
    }
    static void CylinderDetails(Transform room)
    {
        var d=Details(room);var iron=Mat("Cylinder","Iron");var organic=Mat("Cylinder","DecayOrganic");
        foreach(int number in new[]{3,17,29,41})
        {
            var source=room.Find("Service bay "+number);var bay=Group(d,"Rounded maintenance niche "+number,source.localPosition);bay.localRotation=source.localRotation;
            Pipe(bay,"Cylinder_CurvedWallConduit_"+number,ChaosGeometry.Curve(V(-1.03f,.1f,-.38f),V(-1.03f,3.3f,-.38f),V(-.97f,3.5f,-.7f),V(-.7f,3.5f,-.7f)),.048f,iron);
            for(int i=0;i<6;i++)Shape(bay,"Cylinder_MasonrySpall_"+number+"_"+i,ChaosGeometry.Rubble(number*19+i),V(-.9f+i*.18f,.015f,-.9f),Mat("Cylinder","Rubble"),V(0,i*43,0),V(.4f,.3f,.55f));
            Shape(bay,"Cylinder_CrumpledWasteSack_"+number,ChaosGeometry.Lathe(new[]{new Vector2(0,0),new Vector2(.26f,.02f),new Vector2(.34f,.18f),new Vector2(.30f,.45f),new Vector2(.12f,.59f),new Vector2(.07f,.64f),new Vector2(.11f,.68f),new Vector2(0,.70f)},24,.20f,number),V(-.86f,.025f,-.95f),Mat("Cylinder","Dark"));
        }
        var nest=room.Find("Timeworn atrium stories/Creature molt nest hidden behind the upper gallery");
        if(nest)
        {
            foreach(var r in nest.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial.name=="DecayOrganic"||r.sharedMaterial.name=="DecayChitin")r.enabled=false;
            var shell=Group(d,"Curved segmented creature molt",nest.localPosition);shell.localRotation=nest.localRotation;
            Shape(shell,"Cylinder_MoltAbdomen",ChaosGeometry.Ellipsoid(V(1.08f,1.02f,.86f)),V(0,4.72f,-1.51f),organic);
            for(int segment=0;segment<5;segment++)Shape(shell,"Cylinder_MoltChitinPlate_"+segment,ChaosGeometry.Ellipsoid(V(.91f, .47f,.21f),segment),V(0,4.99f,-1.23f-segment*.14f),Mat("Cylinder","DecayChitin"));
            Shape(shell,"Cylinder_EmptyMoltHead",ChaosGeometry.Ellipsoid(V(.53f,.43f,.43f),3),V(0,4.90f,-2.09f),Mat("Cylinder","DecayChitin"));
            var eggs=new System.Random(309);
            for(int i=0;i<9;i++)Shape(shell,"Cylinder_HatchedEggShell_"+i,ChaosGeometry.Ellipsoid(V(.28f,.45f,.28f),i),V((float)eggs.NextDouble()*1.72f-.86f,4.46f,(float)eggs.NextDouble()*.77f-.73f),Mat("Cylinder","DecayChitin"));
            for(int i=0;i<8;i++)Pipe(shell,"Cylinder_DroopingNestSilk_"+i,ChaosGeometry.Curve(V(-1+i*.27f,5.5f,-.2f),V(-1+i*.28f,3.8f,-.7f),V(-.9f+i*.26f,1.4f,-.8f),V(-1.1f+i*.29f,.3f,-.3f)),.008f,organic);
            // Curved, tapered exoskeletal limbs replace the straight stick silhouette.
            for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)
            {
                var points=ChaosGeometry.Curve(V(side*.42f,4.75f,-1.22f-i*.21f),V(side*.82f,5.12f,-1.45f-i*.23f),V(side*1.28f,5.25f,-1.51f-i*.36f),V(side*(1.38f+i*.13f),4.30f,-1.33f-i*.4f));
                Shape(shell,"Cylinder_TaperedMoltLimb_"+side+"_"+i,ChaosGeometry.Sweep(points,.06f,12,.012f),Vector3.zero,organic);
            }
        }
        Shape(d,"Cylinder_FoldedAbandonedAwning",ChaosGeometry.Cloth(2.1f,1.6f,54,true),V(1.6f,2.85f,3.85f),Mat("Cylinder","DecayPaper"),V(0,0,7));
    }
    static void Textures()
    {
        const int n=512;
        foreach(string family in new[]{"Masonry","Metal","Rust","Paint","Wood","Fabric","Blood","Organic"})
        {
            var albedo=new Texture2D(n,n,TextureFormat.RGBA32,true);var normal=new Texture2D(n,n,TextureFormat.RGBA32,true);var rough=new Texture2D(n,n,TextureFormat.RGBA32,true);
            var colors=new Color[n*n];var heights=new float[n*n];var normals=new Color[n*n];var packed=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float u=x/(float)n,v=y/(float)n;
                float broad=Mathf.PerlinNoise(u*5.4f+11,v*5.4f+13),mid=Mathf.PerlinNoise(u*31+37,v*31+7),grain=Mathf.PerlinNoise(u*211+5,v*211+29);
                float value=.52f+broad*.27f+mid*.14f+grain*.11f;Color color=new Color(value,value*.98f,value*.93f);float h=mid*.045f+grain*.025f,smooth=.12f,metal=0;
                if(family=="Wood"){float veins=Mathf.Sin((u*45+Mathf.PerlinNoise(u*3,v*10)*2)*Mathf.PI)*.045f;value=.66f+mid*.13f+veins;color=new Color(value,value*.96f,value*.89f);h=veins*.3f;smooth=.15f;}
                else if(family=="Fabric"){float weave=((x%4<2?1:0)+(y%4<2?1:0))*.023f;value=.69f+mid*.19f+weave;color=new Color(value,value*.97f,value*.91f);h=weave;smooth=.02f;}
                else if(family=="Metal"||family=="Rust"||family=="Paint")
                {
                    float chips=Mathf.SmoothStep(.56f,.70f,broad+mid*.2f);color=Color.Lerp(new Color(.80f,.82f,.79f)*( .85f+grain*.15f),new Color(.55f,.36f,.21f),chips*(family=="Rust"?.9f:.53f));
                    float scratch=Mathf.Pow(Mathf.Max(0,Mathf.Sin(v*721+Mathf.PerlinNoise(u*4,v*4)*4)),38)*.11f;color+=Color.white*scratch;h=chips*.025f+scratch*.012f;smooth=.13f+(.3f-chips*.28f);metal=(1-chips)*.65f;
                }
                else if(family=="Masonry")
                {
                    var fracture=new[]{new Vector2(.12f,.98f),new Vector2(.16f,.70f),new Vector2(.11f,.53f),new Vector2(.20f,.29f),new Vector2(.27f,.05f)};
                    float distance=1;var p=new Vector2(u,v);
                    for(int i=0;i<fracture.Length-1;i++){var a=fracture[i];var edge=fracture[i+1]-a;distance=Mathf.Min(distance,Vector2.Distance(p,a+edge*Mathf.Clamp01(Vector2.Dot(p-a,edge)/edge.sqrMagnitude)));}
                    float crack=Mathf.Clamp01(1-distance/.0028f);color*=1-crack*.37f;
                    float damp=Mathf.SmoothStep(.45f,.85f,Mathf.PerlinNoise(u*16,v*1.5f));color=Color.Lerp(color,color*new Color(.52f,.60f,.46f),damp*.53f);h-=crack*.045f;smooth=damp*.21f;
                }
                else if(family=="Blood"){color=new Color(.65f+broad*.30f,.59f+mid*.21f,.58f+mid*.21f);h=broad*.025f;smooth=.23f+broad*.32f;}
                else {float vein=Mathf.Pow(Mathf.Max(0,Mathf.Sin(u*40+Mathf.PerlinNoise(u*10,v*9)*8)),13);color=new Color(value,value*.94f,value*.87f)*(1-vein*.22f);h=vein*.055f;smooth=.13f;}
                int at=y*n+x;colors[at]=color;heights[at]=h;packed[at]=new Color(metal,0,0,smooth);
            }
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){int i=y*n+x;float dx=(heights[y*n+(x+1)%n]-heights[y*n+(x+n-1)%n])*7,dy=(heights[((y+1)%n)*n+x]-heights[((y+n-1)%n)*n+x])*7;var v=new Vector3(-dx,-dy,1).normalized;normals[i]=new Color(v.x*.5f+.5f,v.y*.5f+.5f,v.z*.5f+.5f);}
            albedo.SetPixels(colors);normal.SetPixels(normals);rough.SetPixels(packed);SaveTexture(family+"_Albedo",albedo,false,false);SaveTexture(family+"_Normal",normal,true,true);SaveTexture(family+"_MetalRough",rough,false,true);
        }
    }
    static void SaveTexture(string name,Texture2D image,bool normal,bool linear)
    {
        image.Apply();string path=Art+"/Textures/"+name+".png";File.WriteAllBytes(path,image.EncodeToPNG());Object.DestroyImmediate(image);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.maxTextureSize=512;importer.filterMode=FilterMode.Bilinear;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.sRGBTexture=!linear;importer.textureCompression=TextureImporterCompression.Uncompressed;
        if(normal)importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();
    }
}
