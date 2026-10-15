using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
using static CylinderMacabreGeometry;

public static class CylinderMacabreBuilder
{
    public const string Root="Assets/_Project/Scenes/Cylinder";
    public const string Prefab=Root+"/Prefabs/Cylinder.prefab";
    const string Meshes=Root+"/Meshes/MacabreRevision";
    const string Textures=Root+"/Textures/MacabreRevision";
    const string Materials=Root+"/Materials/MacabreRevision";
    static Transform atrium,revision;
    static int serial,removedRadio,removedCanteen;
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static readonly string[] models={"vintage_radio_transceiver","vintage_electric_kettle","vintage_microwave","russian_food_cans_01","wooden_bowl_01"};
    static readonly string[] identities={"Classroom","Library","Director","Entrance","Electrical","Storage","Maintenance","Washroom"};

    public static void Build()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/CylinderDetailPreview/"))throw new Exception("Isolated workspace required");
        Directory.CreateDirectory(Materials);Directory.CreateDirectory(Meshes);AssetDatabase.Refresh();LoadMaterials();
        var room=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            atrium=room.transform.Find("Abandoned circular atrium");
            if(atrium.Find("Macabre texture and prop revision"))throw new Exception("Already applied");
            revision=Group(atrium,"Macabre texture and prop revision",Vector3.zero);
            DownloadedSigns(room);ReplaceRadio();CanteenVariety();WallStories();
            PrefabUtility.SaveAsPrefabAsset(room,Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(room);}
        AssetDatabase.SaveAssets();Directory.CreateDirectory("MacabreEvidence");
        File.WriteAllText("MacabreEvidence/build.txt","Old radio triangles replaced: "+removedRadio+"\nOld repetitive coffee-station triangles replaced: "+removedCanteen+"\nOriginal objects/components retained. TextMesh and TMP remain empty.\n");
        Debug.Log("CYLINDER_MACABRE_BUILD_PASS");
    }

    static void ImportTexture(string path,bool normal,bool srgb,int max=1024)
    {
        var i=(TextureImporter)AssetImporter.GetAtPath(path);i.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        i.sRGBTexture=srgb;i.alphaSource=TextureImporterAlphaSource.FromInput;i.alphaIsTransparency=srgb;
        i.mipmapEnabled=true;i.maxTextureSize=max;i.filterMode=FilterMode.Trilinear;i.anisoLevel=4;i.wrapMode=TextureWrapMode.Clamp;
        i.textureCompression=TextureImporterCompression.CompressedHQ;i.SaveAndReimport();
    }

    static Material Flat(string name,Color color,float metal=0,float smooth=.15f)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
        m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);
        AssetDatabase.CreateAsset(m,Materials+"/"+name+".mat");return m;
    }

    static void LoadMaterials()
    {
        foreach(var key in new[]{"Iron","Rust","Wood","Concrete","Plaster","Dark","Amber"})
            mats[key]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/DetailRevision/"+key+".mat");
        foreach(var key in new[]{"Steel","Rubber","Ceramic","Classroom","Library","Director","Service"})
        {
            var name=new Dictionary<string,string>{{"Steel","Aged satin steel"},{"Rubber","Aged rubber"},{"Ceramic","Stained ivory porcelain"},{"Classroom","Classroom slate enamel"},{"Library","Library oxblood enamel"},{"Director","Director aged brass"},{"Service","Service grey enamel"}}[key];
            mats[key]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/CanteenDoors/"+name+".mat");
        }
        mats["Ivory"]=new Material(mats["Plaster"]){name="Pitted old enamel sign face"};
        mats["Ivory"].SetColor("_BaseColor",new Color(.79f,.76f,.64f));mats["Ivory"].SetFloat("_BumpScale",.19f);
        mats["Ivory"].SetFloat("_Smoothness",.18f);AssetDatabase.CreateAsset(mats["Ivory"],Materials+"/Pitted old enamel sign face.mat");
        mats["ApplianceGlass"]=Flat("Dark aged appliance window",new Color(.025f,.036f,.031f),.05f,.57f);
        foreach(var id in models)
        {
            var folder=Textures+"/"+id+"/";
            foreach(var file in Directory.GetFiles(folder,"*.png"))
            {var f=file.Replace('\\','/');ImportTexture(f,f.Contains("nor_gl"),f.EndsWith("Diffuse.png")||f.EndsWith("_diff.png"));}
            CreatePBR(id,folder,"Diffuse","nor_gl","arm");
            if(id=="vintage_radio_transceiver")CreatePBR(id+"_accessories",folder,"accessories_diff","accessories_nor_gl","accessories_arm");
        }
        foreach(var id in identities)
        {
            string p=Textures+"/DoorPNGs/"+id+".png";ImportTexture(p,false,true);
            mats["PNG_"+id]=Alpha("Downloaded PNG - "+id,p,new Color(.12f,.10f,.07f,1),true);
        }
        string atlas=Textures+"/WallResidueAtlas.png";ImportTexture(atlas,false,true,2048);
        mats["Residue"]=Alpha("Embedded dried pigment and charcoal",atlas,new Color(.55f,.49f,.44f,.82f),false);
        var decal=mats["Residue"];decal.SetTexture("_BumpMap",mats["Plaster"].GetTexture("_BumpMap"));
        decal.SetTextureScale("_BumpMap",new Vector2(3,3));decal.SetFloat("_BumpScale",.17f);decal.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(decal);
    }

    static void CreatePBR(string id,string folder,string diffuse,string normal,string arm)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);t.LoadImage(File.ReadAllBytes(folder+arm+".png"));
        var c=t.GetPixels32();for(int j=0;j<c.Length;j++)c[j]=new Color32(c[j].b,c[j].r,c[j].g,(byte)(255-c[j].g));
        t.SetPixels32(c);t.Apply();string maskPath=folder+arm+"_UnityMask.png";File.WriteAllBytes(maskPath,t.EncodeToPNG());Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(maskPath);ImportTexture(maskPath,false,false);
        var m=new Material(Shader.Find("UniDoom/TotalChaosInspiredSurface")){name=id,enableInstancing=true};
        m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+diffuse+".png"));
        m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+normal+".png"));
        var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);m.SetTexture("_MetallicGlossMap",mask);m.SetTexture("_OcclusionMap",mask);m.SetTexture("_DirtMap",mask);
        m.SetColor("_BaseColor",Color.white);m.SetVector("_SpecColor",new Vector4(.12f,.02f,.78f,1));
        m.SetFloat("_BumpScale",.7f);m.SetFloat("_Smoothness",.65f);m.SetFloat("_OcclusionStrength",.6f);
        foreach(var key in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP","_TC_SHARED_MASK"})m.EnableKeyword(key);
        AssetDatabase.CreateAsset(m,Materials+"/"+id+".mat");mats[id]=m;
    }

    static Material Alpha(string name,string png,Color tint,bool clip)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,enableInstancing=true};
        m.SetColor("_BaseColor",tint);m.SetFloat("_Metallic",0);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(png);if(!texture)throw new Exception("Texture missing: "+png);
        m.SetTexture("_BaseMap",texture);
        m.SetFloat("_BlendModePreserveSpecular",0);m.SetFloat("_SpecularHighlights",0);m.SetFloat("_EnvironmentReflections",0);
        m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
        m.SetFloat("_Cull",2);m.SetFloat("_Smoothness",.05f);
        if(clip){m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.32f);m.EnableKeyword("_ALPHATEST_ON");m.renderQueue=(int)RenderQueue.AlphaTest;m.SetOverrideTag("RenderType","TransparentCutout");}
        else
        {
            m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=(int)RenderQueue.Transparent;m.SetOverrideTag("RenderType","Transparent");
        }
        AssetDatabase.CreateAsset(m,Materials+"/"+name+".mat");return m;
    }

    static void Quad(Transform parent,string label,Vector3 center,Vector2 size,string material,Rect uv)
    {
        var mesh=new Mesh{name=label};float x=size.x/2,y=size.y/2;
        mesh.vertices=new[]{V(-x,-y,0),V(-x,y,0),V(x,y,0),V(x,-y,0)};
        mesh.uv=new[]{new Vector2(uv.xMin,uv.yMin),new Vector2(uv.xMin,uv.yMax),new Vector2(uv.xMax,uv.yMax),new Vector2(uv.xMax,uv.yMin)};
        mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,Meshes+"/Quad_"+(++serial).ToString("D3")+".asset");
        var t=Group(parent,label,center);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mats[material];r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=true;
    }

    static void Plate(Transform p,string id,bool inset=false)
    {
        float s=inset?.72f:1;var g=Group(p,"Downloaded PNG door plaque - "+id,Vector3.zero);g.localScale=Vector3.one*s;
        Box(g,"Chipped metal sign rim",Vector3.zero,V(.70f,.48f,.035f),"Steel",.025f);
        Box(g,"Coloured identity border",V(0,0,-.027f),V(.65f,.43f,.018f),mats.ContainsKey(id)?id:"Service",.024f);
        Box(g,"Pitted ivory enamel",V(0,0,-.042f),V(.59f,.375f,.012f),"Ivory",.018f);
        foreach(float x in new[]{-.313f,.313f})Cylinder(g,"Recessed sign screw",V(x,0,-.044f),.012f,.008f,"Iron",12,Quaternion.Euler(90,0,0));
        Batch(g,"Worn sign backing",true);
        Quad(g,"Original internet PNG - "+id,V(0,0,-.052f),new Vector2(.40f,.36f),"PNG_"+id,new Rect(0,0,1,1));
    }

    static void DownloadedSigns(GameObject room)
    {
        var old=atrium.Find("Wordless door identities");foreach(var r in old.GetComponentsInChildren<MeshRenderer>())r.enabled=false;
        var signs=Group(revision,"Internet PNG door signage",Vector3.zero);
        foreach(var marker in old.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Wordless ")&&t.name.EndsWith(" plaque")).ToArray())
        {
            string id=marker.name.Substring(9).Replace(" plaque","");var t=Group(signs,"PNG sign anchor - "+id,atrium.InverseTransformPoint(marker.position));
            t.rotation=marker.rotation;t.localScale=marker.lossyScale;Plate(t,id);
        }
        var front=atrium.Find("Service bay 0/Larger front entrance/Wordless Entrance plaque");
        var cover=Group(front,"Internet entrance sign",V(0,0,-.065f));Plate(cover,"Entrance");
        foreach(var inset in room.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Room-specific door inset - Library"||t.name=="Room-specific door inset - Director").ToArray())
        {string id=inset.name.EndsWith("Library")?"Library":"Director";var t=Group(inset,"Texture inset cover",V(0,0,-.12f));Plate(t,id,true);}
    }

    static Transform Model(Transform parent,string id,string label,Vector3 feet,float yaw,float height)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Meshes+"/PolyHaven/"+id+"/model.fbx");
        var anchor=Group(parent,label,feet);var go=(GameObject)PrefabUtility.InstantiatePrefab(source,anchor);
        PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        go.transform.localPosition=Vector3.zero;go.transform.localRotation=source.transform.localRotation;
        var renderers=go.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;
        foreach(var r in renderers)
        {
            bounds.Encapsulate(r.bounds);
            r.sharedMaterials=r.sharedMaterials.Select(m=>m&&m.name.EndsWith("_glass")?mats["ApplianceGlass"]:id=="vintage_radio_transceiver"&&m&&m.name.ToLowerInvariant().Contains("accessor")?mats[id+"_accessories"]:mats[id]).ToArray();
        }
        foreach(var r in renderers)if(!r.GetComponent<MeshFilter>()||!r.GetComponent<MeshFilter>().sharedMesh||r.GetComponent<MeshFilter>().sharedMesh.vertexCount==0)Object.DestroyImmediate(r.gameObject);
        renderers=go.GetComponentsInChildren<MeshRenderer>();
        go.transform.localScale*=height/bounds.size.y;bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
        go.transform.position+=anchor.position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        anchor.localRotation=Quaternion.Euler(0,yaw,0);return anchor;
    }

    // Keep existing MeshFilter bindings and fileIDs: revised copies are new siblings, old renderers are disabled.
    static int RetainWithout(Transform root,Func<Vector3,bool> remove,string label,bool allVertices)
    {
        int count=0;var output=Group(root,label,Vector3.zero);
        foreach(var r in root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&!r.transform.IsChildOf(output)&&!r.name.StartsWith("Skill")).ToArray())
        {
            var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)continue;var source=f.sharedMesh;var vertices=source.vertices;var mesh=Object.Instantiate(source);int removed=0;
            for(int s=0;s<source.subMeshCount;s++)
            {
                var indices=source.GetTriangles(s);var kept=new List<int>();
                for(int j=0;j<indices.Length;j+=3)
                {
                    var a=root.InverseTransformPoint(r.transform.TransformPoint(vertices[indices[j]]));
                    var b=root.InverseTransformPoint(r.transform.TransformPoint(vertices[indices[j+1]]));
                    var c=root.InverseTransformPoint(r.transform.TransformPoint(vertices[indices[j+2]]));
                    if(allVertices?(remove(a)&&remove(b)&&remove(c)):remove((a+b+c)/3)){removed++;continue;}
                    kept.Add(indices[j]);kept.Add(indices[j+1]);kept.Add(indices[j+2]);
                }
                mesh.SetTriangles(kept,s);
            }
            if(removed==0){Object.DestroyImmediate(mesh);continue;}
            count+=removed;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Meshes+"/Retained_"+(++serial).ToString("D3")+".asset");
            var t=Group(output,"Retained geometry - "+r.name,root.InverseTransformPoint(r.transform.position));t.rotation=r.transform.rotation;t.localScale=r.transform.lossyScale;
            t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var replacement=t.gameObject.AddComponent<MeshRenderer>();replacement.sharedMaterials=r.sharedMaterials;replacement.shadowCastingMode=r.shadowCastingMode;
            r.enabled=false;
        }
        return count;
    }

    static void Solid(Transform p,Vector3 center,Vector3 size){var c=p.gameObject.AddComponent<BoxCollider>();c.center=center;c.size=size;}

    static void ReplaceRadio()
    {
        var old=atrium.Find("Timeworn atrium stories/Abandoned emergency radio and campus map");
        removedRadio=RetainWithout(old,p=>p.x>-.55f&&p.x<.36f&&p.y>1.025f&&p.y<1.92f&&p.z>-.88f&&p.z<-.44f,"Retained original shelf and map",true);
        if(removedRadio<20)throw new Exception("Old radio not identified: "+removedRadio);
        var bay=atrium.Find("Service bay 34");var station=Group(revision,"Relocated detailed radio near library",atrium.InverseTransformPoint(bay.position));station.rotation=bay.rotation;
        Box(station,"Radio desk top",V(0,1.02f,-.82f),V(1.65f,.09f,.72f),"Wood",.025f);
        foreach(float x in new[]{-.70f,.70f})
        {Box(station,"Desk side leg",V(x,.50f,-.78f),V(.06f,1,.06f),"Iron",.009f);Box(station,"Desk rear leg",V(x,.50f,-1.10f),V(.06f,1,.06f),"Iron",.009f);}
        Box(station,"Desk bottom shelf",V(0,.24f,-.85f),V(1.5f,.04f,.56f),"Wood",.012f);
        Box(station,"Radio service drawer",V(.45f,.88f,-.78f),V(.53f,.20f,.62f),"Iron",.02f);
        Pipe(station,V(.30f,.89f,-1.115f),V(.58f,.89f,-1.115f),.012f,"Steel");
        Solid(station,V(0,.53f,-.82f),V(1.65f,1.06f,.72f));Batch(station,"Radio desk finish",true);
        Model(station,"vintage_radio_transceiver","Poly Haven military radio and accessories",V(-.05f,1.065f,-.82f),180,.67f);
        var lamp=Group(station,"Shielded desk light",V(.58f,2.05f,-1.1f));
        Pipe(lamp,V(0,0,0),V(0,.75f,0),.013f,"Iron");Cylinder(lamp,"Small utility shade",V(0,.08f,0),.13f,.06f,"Iron",24);
        Cylinder(lamp,"Warm recessed bulb",V(0,.04f,0),.075f,.012f,"Amber",20);Batch(lamp,"Desk lamp fixture",true);
        var light=lamp.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.74f,.46f);light.intensity=4.0f;light.range=3.8f;light.shadows=LightShadows.None;
        lamp.gameObject.AddComponent<UniversalAdditionalLightData>();
    }

    static void CanteenVariety()
    {
        var canteen=atrium.Find("Central canteen - warm landmark");var refined=canteen.Find("Refined canteen models");
        var bays=canteen.Cast<Transform>().Where(t=>t.name=="Kiosk bay").ToArray();
        int[] replace={0,3,6,10,13};
        removedCanteen=RetainWithout(refined,p=>
        {
            var world=refined.TransformPoint(p);
            foreach(int i in replace)
            {var q=bays[i].InverseTransformPoint(world);if(q.y>1.192f&&q.y<2.03f&&q.z<-.16f&&q.z> -1.18f&&Mathf.Abs(q.x)<.80f)return true;}
            return false;
        },"Retained cafe counters with varied appliances",false);
        var props=Group(canteen,"New varied cafeteria inventory",Vector3.zero);
        foreach(int i in replace)
        {
            var bay=bays[i];var p=Group(props,"Distinct service counter "+i,canteen.InverseTransformPoint(bay.position));p.rotation=bay.rotation;
            if(i==0||i==10)Model(p,"vintage_microwave","Old enamel microwave",V(0,1.19f,-.52f),0,.34f);
            else if(i==6)Model(p,"russian_food_cans_01","Assorted preserved-food tins",V(0,1.19f,-.52f),110,.24f);
            else
            {Model(p,"vintage_electric_kettle","Stained electric kettle",V(.27f,1.19f,-.53f),180,.30f);Model(p,"wooden_bowl_01","Worn serving bowl",V(-.28f,1.19f,-.48f),i*17,.13f);}
        }
        var island=Group(props,"Interior prep island and storage",V(0,0,0));
        Box(island,"Stainless prep top",V(0,.92f,0),V(2.65f,.08f,1.30f),"Steel",.025f);Solid(island,V(0,.49f,0),V(2.65f,.98f,1.30f));
        foreach(float x in new[]{-1.15f,1.15f})foreach(float z in new[]{-.50f,.50f})Box(island,"Braced prep leg",V(x,.45f,z),V(.055f,.9f,.055f),"Steel",.012f);
        Box(island,"Under-counter shelf",V(0,.24f,0),V(2.42f,.045f,1.13f),"Steel",.015f);
        Box(island,"Butcher block cutting board",V(-.24f,.977f,.15f),V(.60f,.035f,.44f),"Wood",.022f);
        Box(island,"Battered serving tray",V(.78f,.974f,.05f),V(.60f,.025f,.64f),"Steel",.014f);
        foreach(float z in new[]{-.26f,.36f})Box(island,"Turned tray lip",V(.78f,1.0f,z),V(.60f,.045f,.025f),"Steel",.008f);
        Model(island,"wooden_bowl_01","Prep bowl on tray",V(.78f,.99f,.05f),10,.16f);
        Model(island,"russian_food_cans_01","Stored tins under worktop",V(-.65f,.264f,0),90,.25f);
        Model(island,"vintage_electric_kettle","Spare kettle on preparation island",V(-.88f,.96f,0),170,.31f);
        Batch(island,"Preparation island finish",true);
        var rack=Group(props,"Interior dish rack",V(1.72f,0,-1.5f));rack.localRotation=Quaternion.Euler(0,-25,0);
        foreach(float x in new[]{-.52f,.52f})foreach(float z in new[]{-.22f,.22f})Box(rack,"Wire rack upright",V(x,.78f,z),V(.035f,1.5f,.035f),"Iron",.008f);
        foreach(float y in new[]{.18f,.72f,1.25f})Box(rack,"Storage rack shelf",V(0,y,0),V(1.12f,.035f,.53f),"Steel",.01f);
        Model(rack,"russian_food_cans_01","Mixed pantry tins",V(0,.738f,0),180,.24f);Model(rack,"wooden_bowl_01","Empty stockroom bowl",V(.17f,1.268f,0),0,.16f);
        Solid(rack,V(0,.77f,0),V(1.14f,1.54f,.55f));Batch(rack,"Storage rack finish",true);
    }

    static void WallStories()
    {
        var wall=Group(revision,"Pigment embedded in wall surfaces",Vector3.zero);
        // Atlas has a naturally unequal division: top clusters occupy 60%, bottom 40%.
        var regions=new[]{new Rect(0,.40f,.5f,.60f),new Rect(.51f,.40f,.49f,.60f),new Rect(0,0,.51f,.40f),new Rect(.51f,0,.49f,.40f)};
        var placements=new[]{(30,0,1.42f,.56f,1.7f),(32,1,2.05f,.57f,1.85f),(29,2,3.65f,1.85f,.50f),(33,3,1.23f,.54f,1.10f),(19,1,2.00f,.57f,1.7f),(9,0,1.16f,.54f,1.35f)};
        foreach(var item in placements)
        {
            var bay=atrium.Find("Service bay "+item.Item1);var t=Group(wall,"Wall residue story - bay "+item.Item1,atrium.InverseTransformPoint(bay.position));t.rotation=bay.rotation;
            // Side plaster beside sealed leaf: outside the door silhouette, facing into the atrium.
            Quad(t,"Baked pigment decal - "+item.Item2,V(item.Item2==2?0:-1.29f,item.Item3,-.263f),new Vector2(item.Item4,item.Item5),"Residue",regions[item.Item2]);
        }
        // A small related stain sits on the old radio shelf's wall; a second beside the relocated radio.
        var radio=revision.Find("Relocated detailed radio near library");
        Quad(radio,"Scratched marks beside radio",V(-1.30f,2.02f,-.267f),new Vector2(.55f,.90f),"Residue",regions[3]);
    }
}
