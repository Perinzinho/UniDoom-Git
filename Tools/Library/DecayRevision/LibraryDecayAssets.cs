using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

public static partial class LibraryDecayBuilder
{
    const string Art="Assets/_Project/Scenes/Library";
    const string MeshDir=Art+"/Meshes/DecayRevision";
    const string MatDir=Art+"/Materials/DecayRevision";
    const string Evidence="Evidence/Decay";
    static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
    static readonly Dictionary<string,Mesh> meshCache=new Dictionary<string,Mesh>();
    static readonly System.Globalization.CultureInfo CI=System.Globalization.CultureInfo.InvariantCulture;
    static System.Random rng;
    static int meshSerial;
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Color C(float r,float g,float b)=>new Color(r,g,b);
    static float R(float min,float max)=>min+(float)rng.NextDouble()*(max-min);
    static Transform Group(Transform p,string name,Vector3 pos){var t=new GameObject(name).transform;t.SetParent(p,false);t.localPosition=pos;return t;}
    static Mesh Save(Mesh mesh,string name)
    {
        mesh.name=name;string path=MeshDir+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static MeshRenderer Draw(Transform p,string name,Mesh mesh,string material)
    {
        var t=Group(p,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
        var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mats[material];return r;
    }
    static Transform Box(Transform p,string name,Vector3 pos,Vector3 size,string mat,bool collision=false,Vector3 euler=default)
    {
        var g=Group(p,name,pos);g.localRotation=Quaternion.Euler(euler);
        bool shared=size.y<.005f||Mathf.Max(size.x,Mathf.Max(size.y,size.z))<.36f||new[]{"Iron","Ink","Paper","Paint","Hazard","DeadTube","ColdTube","Amber"}.Contains(mat);
        if(shared){g.localScale=size;size=Vector3.one;}
        string key="Box_"+size.x.ToString("F3",CI)+"_"+size.y.ToString("F3",CI)+"_"+size.z.ToString("F3",CI);
        if(!meshCache.TryGetValue(key,out var mesh))
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(cube);
            var v=mesh.vertices;var uv=mesh.uv;var n=mesh.normals;
            for(int i=0;i<v.Length;i++){v[i]=Vector3.Scale(v[i],size);uv[i]=Vector2.Scale(uv[i],Mathf.Abs(n[i].y)>.5f?new Vector2(size.x,size.z):Mathf.Abs(n[i].x)>.5f?new Vector2(size.z,size.y):new Vector2(size.x,size.y));}
            mesh.vertices=v;mesh.uv=uv;mesh.RecalculateBounds();mesh.RecalculateTangents();mesh=Save(mesh,key);meshCache[key]=mesh;
        }
        Draw(g,"Surface",mesh,mat);if(collision)g.gameObject.AddComponent<BoxCollider>().size=size;return g;
    }
    static Transform Face(Transform p,string name,Vector3 pos,Vector2 size,string mat,Vector3 euler=default)
    {
        var g=Group(p,name,pos);g.localRotation=Quaternion.Euler(euler);
        string key="Face_"+size.x.ToString("F3",CI)+"_"+size.y.ToString("F3",CI);
        if(!meshCache.TryGetValue(key,out var mesh))
        {
            mesh=new Mesh();mesh.vertices=new[]{V(-size.x/2,-size.y/2,0),V(size.x/2,-size.y/2,0),V(size.x/2,size.y/2,0),V(-size.x/2,size.y/2,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();mesh.RecalculateTangents();mesh=Save(mesh,key);meshCache[key]=mesh;
        }
        Draw(g,"Surface",mesh,mat);return g;
    }
    static void Polygon(Transform p,string name,Vector3[] outline,string mat,bool floor=false)
    {
        Vector3 center=Vector3.zero;foreach(var v in outline)center+=v;center/=outline.Length;
        var vertices=new List<Vector3>{center};vertices.AddRange(outline);var uv=new List<Vector2>();
        foreach(var v in vertices)uv.Add(floor?new Vector2(v.x,v.z):new Vector2(v.x,v.y));
        var triangles=new List<int>();
        for(int i=0;i<outline.Length;i++){int a=i+1,b=(i+1)%outline.Length+1;if(floor)triangles.AddRange(new[]{0,b,a});else triangles.AddRange(new[]{0,a,b});}
        var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh=Save(mesh,"Polygon_"+meshSerial++);Draw(p,name,mesh,mat);
    }
    static void Cable(Transform p,string name,Vector3[] points,float radius,string mat)
    {
        var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();const int sides=6;
        for(int i=0;i<points.Length;i++)
        {
            Vector3 tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;
            var right=Vector3.Cross(tangent,Mathf.Abs(tangent.y)<.9f?Vector3.up:Vector3.right).normalized;var up=Vector3.Cross(right,tangent);
            for(int j=0;j<=sides;j++){float angle=j*Mathf.PI*2/sides;v.Add(points[i]+radius*(right*Mathf.Cos(angle)+up*Mathf.Sin(angle)));uv.Add(new Vector2((float)j/sides,(float)i/(points.Length-1)));}
        }
        for(int i=0;i<points.Length-1;i++)for(int j=0;j<sides;j++){int a=i*(sides+1)+j,b=a+sides+1;tris.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh=Save(mesh,"Cable_"+meshSerial++);Draw(p,name,mesh,mat);
    }
    static void Label(Transform p,string text,Vector3 pos,float height,string mat,float yaw=0)
    {
        var g=Group(p,text.Replace('\n',' '),pos);g.localRotation=Quaternion.Euler(0,yaw+180,0);
        var tm=g.gameObject.AddComponent<TextMesh>();tm.text=text;tm.fontSize=64;tm.characterSize=height*.14f;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;
        tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.color=mats[mat].GetColor("_BaseColor");g.GetComponent<MeshRenderer>().sharedMaterial=tm.font.material;
    }
    static Material NewMat(string name)
    {
        string path=MatDir+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("UniDoom/TotalChaosInspiredSurface")){name=name};AssetDatabase.CreateAsset(m,path);}
        m.enableInstancing=true;mats[name]=m;return m;
    }
    static Material Copy(string name,Material source,Color tint)
    {var m=NewMat(name);m.CopyPropertiesFromMaterial(source);m.SetColor("_BaseColor",tint);EditorUtility.SetDirty(m);return m;}
    static void Flat(string name,Color color,float smooth=0,float metal=0,Color emission=default)
    {
        var m=NewMat(name);m.shaderKeywords=Array.Empty<string>();m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);
        if(emission.maxColorComponent>0){m.SetColor("_EmissionColor",emission);m.EnableKeyword("_EMISSION");}EditorUtility.SetDirty(m);
    }
    static void PBR(string name,string folder,Color tint)
    {
        var m=NewMat(name);string maskPath=Art+"/Textures/DecayRevision/Packed/"+name+".png";Directory.CreateDirectory(Path.GetDirectoryName(maskPath));
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);t.LoadImage(File.ReadAllBytes(folder+"arm.png"));var px=t.GetPixels32();
        for(int i=0;i<px.Length;i++){var c=px[i];px[i]=new Color32(c.b,c.r,c.g,(byte)(255-c.g));}t.SetPixels32(px);t.Apply();File.WriteAllBytes(maskPath,t.EncodeToPNG());Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(maskPath);var ti=(TextureImporter)AssetImporter.GetAtPath(maskPath);ti.sRGBTexture=false;ti.mipmapEnabled=true;ti.SaveAndReimport();
        var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Diffuse.png"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"nor_gl.png"));
        m.SetTexture("_MetallicGlossMap",mask);m.SetTexture("_OcclusionMap",mask);m.SetTexture("_DirtMap",mask);m.SetColor("_BaseColor",tint);
        m.SetVector("_SpecColor",new Vector4(.22f,.025f,.70f,1));m.SetFloat("_Smoothness",.32f);m.SetFloat("_BumpScale",.8f);m.SetFloat("_OcclusionStrength",.65f);
        foreach(var k in new[]{"_NORMALMAP","_METALLICSPECGLOSSMAP","_OCCLUSIONMAP","_TC_SHARED_MASK"})m.EnableKeyword(k);EditorUtility.SetDirty(m);
    }
    static void Materials()
    {
        foreach(var path in Directory.GetFiles(Art+"/Textures/DecayRevision","*.png",SearchOption.AllDirectories).Where(p=>!p.Contains("Packed")))
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=path.Contains("nor_gl")?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=path.EndsWith("Diffuse.png");
            ti.maxTextureSize=path.Contains("ceiling_fan")?1024:2048;ti.mipmapEnabled=true;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
        }
        PBR("BrokenPlaster",Art+"/Textures/DecayRevision/damaged_plaster/",C(.71f,.72f,.69f));
        PBR("DamagedConcrete",Art+"/Textures/DecayRevision/concrete_floor_damaged_01/",C(.56f,.59f,.57f));
        PBR("Fan",Art+"/Textures/DecayRevision/ceiling_fan/",C(.43f,.40f,.33f));
        Material Old(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/"+name+".mat");
        Copy("Mold",Old("Plaster"),C(.84f,.86f,.81f));Copy("Wood",Old("Wood"),C(.35f,.32f,.25f));Copy("Shelf",Old("wooden_bookshelf_worn"),C(.52f,.46f,.35f));
        Copy("Chair",Old("dining_chair_02"),C(.48f,.44f,.33f));Copy("Table",Old("wooden_table_02"),C(.56f,.48f,.33f));
        Copy("Cover",Old("BookCover"),C(.78f,.72f,.61f));Copy("Pages",Old("BookPaper"),C(.38f,.36f,.29f));
        Copy("Floor",Old("Stone"),C(.50f,.53f,.50f));Copy("Rust",Old("Brass"),C(.31f,.29f,.22f));
        var wet=Copy("Wet",Old("Stone"),C(.28f,.30f,.29f));wet.DisableKeyword("_METALLICSPECGLOSSMAP");wet.DisableKeyword("_TC_SHARED_MASK");wet.SetFloat("_Smoothness",.77f);wet.SetFloat("_Metallic",0);wet.SetFloat("_BumpScale",.16f);
        Flat("Iron",C(.055f,.06f,.058f),.25f,.55f);Flat("Paper",C(.52f,.51f,.42f));Flat("Ink",C(.055f,.065f,.058f));Flat("RedInk",C(.27f,.035f,.026f));
        Flat("Paint",C(.30f,.34f,.30f),.1f);Flat("SignLetters",C(.27f,.30f,.25f));Flat("Hazard",C(.53f,.38f,.08f),.1f);
        Flat("ColdTube",C(.72f,.83f,.85f),.1f,0,C(.75f,.86f,.90f)*2.6f);Flat("DeadTube",C(.22f,.24f,.22f),.12f);
        Flat("Amber",C(.59f,.30f,.08f),.1f,0,C(1,.38f,.07f)*1.5f);Flat("Sky",C(.24f,.31f,.35f),0,0,C(.24f,.32f,.39f)*1.1f);
        Flat("Glass",C(.10f,.15f,.15f),.32f,0);mats["Glass"].SetFloat("_Cull",0);
        mats["Glass"].SetTexture("_BaseMap",mats["Mold"].GetTexture("_BaseMap"));mats["Glass"].SetTexture("_EmissionMap",mats["Mold"].GetTexture("_BaseMap"));mats["Glass"].SetTexture("_BumpMap",mats["Mold"].GetTexture("_BumpMap"));mats["Glass"].SetFloat("_BumpScale",.12f);mats["Glass"].EnableKeyword("_NORMALMAP");
        var exteriorImporter=(TextureImporter)AssetImporter.GetAtPath(Art+"/Textures/DecayRevision/Courtyard/abandoned_pathway.hdr");exteriorImporter.textureShape=TextureImporterShape.Texture2D;exteriorImporter.sRGBTexture=false;exteriorImporter.maxTextureSize=2048;exteriorImporter.SaveAndReimport();
        var courtyard=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"/Textures/DecayRevision/Courtyard/abandoned_pathway.hdr");if(!courtyard)throw new Exception("Courtyard panorama missing");
        mats["Sky"].SetTexture("_BaseMap",courtyard);mats["Sky"].SetTexture("_EmissionMap",courtyard);mats["Sky"].SetColor("_BaseColor",C(.18f,.20f,.21f));mats["Sky"].SetColor("_EmissionColor",C(.32f,.37f,.40f));
        string leafFolder=Art+"/Textures/DecayRevision/tree_small_02/";var colors=new Texture2D(2,2,TextureFormat.RGBA32,false);colors.LoadImage(File.ReadAllBytes(leafFolder+"leaves_diff.png"));
        var opacity=new Texture2D(2,2,TextureFormat.RGBA32,false,true);opacity.LoadImage(File.ReadAllBytes(leafFolder+"leaves_alpha.png"));
        var leafPixels=colors.GetPixels32();var alphaPixels=opacity.GetPixels32();if(leafPixels.Length!=alphaPixels.Length)throw new Exception("Leaf opacity dimensions mismatch");
        for(int i=0;i<leafPixels.Length;i++)leafPixels[i].a=alphaPixels[i].r;colors.SetPixels32(leafPixels);colors.Apply();
        string leafPath=leafFolder+"LeavesRGBA.png";File.WriteAllBytes(leafPath,colors.EncodeToPNG());Object.DestroyImmediate(colors);Object.DestroyImmediate(opacity);AssetDatabase.ImportAsset(leafPath);
        var leafImport=(TextureImporter)AssetImporter.GetAtPath(leafPath);leafImport.sRGBTexture=true;leafImport.alphaIsTransparency=true;leafImport.mipmapEnabled=true;leafImport.maxTextureSize=1024;leafImport.SaveAndReimport();
        var leaf=NewMat("Ivy");leaf.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(leafPath));leaf.SetColor("_BaseColor",C(.40f,.43f,.31f));
        leaf.SetFloat("_AlphaClip",1);leaf.SetFloat("_Cutoff",.35f);leaf.SetFloat("_Cull",0);leaf.EnableKeyword("_ALPHATEST_ON");leaf.SetOverrideTag("RenderType","TransparentCutout");leaf.renderQueue=2450;EditorUtility.SetDirty(leaf);
        leaf.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(leafFolder+"leaves_nor_gl.png"));leaf.SetFloat("_BumpScale",.45f);leaf.EnableKeyword("_NORMALMAP");
        Flat("DriedBlood",C(.17f,.024f,.012f),.25f);mats["DriedBlood"].SetTexture("_BaseMap",null);
        mats["DriedBlood"].SetTexture("_BumpMap",mats["Shelf"].GetTexture("_BumpMap"));mats["DriedBlood"].SetFloat("_BumpScale",.16f);mats["DriedBlood"].EnableKeyword("_NORMALMAP");
    }
    static Transform Model(Transform p,string id,string name,Vector3 pos,Vector3 euler=default,Vector3? scale=null,bool collision=false)
    {
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Art+"/Prefabs/Modules/"+id+".prefab");if(!source)throw new Exception("Missing library module "+id);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(source,p);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
        var t=go.transform;t.name=name;t.localPosition=pos;t.localRotation=Quaternion.Euler(euler);t.localScale=scale??Vector3.one;
        foreach(var r in go.GetComponentsInChildren<MeshRenderer>())r.sharedMaterials=r.sharedMaterials.Select(m=>
        {string key=m.name=="BookCover"?"Cover":m.name=="BookPaper"?"Pages":id=="wooden_bookshelf_worn"?"Shelf":id=="wooden_table_02"?"Table":id=="dining_chair_02"?"Chair":"Wood";return mats[key];}).ToArray();
        if(collision){var b=LibraryBuilder.BoundsIn(t,go.GetComponentsInChildren<Renderer>());var c=go.AddComponent<BoxCollider>();c.center=b.center;c.size=b.size;}
        return t;
    }
    static void Ground(Transform parent,Transform t,float y)
    {var b=LibraryBuilder.BoundsIn(parent,t.GetComponentsInChildren<Renderer>());t.localPosition+=Vector3.up*(y-b.min.y);}
    static void Light(Transform p,string name,Vector3 pos,Color color,float intensity,float range,Vector3? target=null,bool shadow=false,float angle=110)
    {
        var t=Group(p,name,pos);var l=t.gameObject.AddComponent<UnityEngine.Light>();l.type=target.HasValue?LightType.Spot:LightType.Point;l.color=color;l.intensity=intensity;l.range=range;l.shadows=shadow?LightShadows.Soft:LightShadows.None;l.shadowBias=.025f;l.shadowNormalBias=.15f;
        if(target.HasValue){t.localRotation=Quaternion.LookRotation(target.Value-pos);l.spotAngle=angle;l.innerSpotAngle=angle*.6f;}
        var so=new SerializedObject(t.gameObject.AddComponent<UniversalAdditionalLightData>());so.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue=1;so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Batch(Transform parent,string name)
    {
        var renderers=parent.GetComponentsInChildren<MeshRenderer>().Where(r=>r.GetComponent<MeshFilter>()&&r.sharedMaterials.Length==1&&r.GetComponentInParent<LODGroup>()==null).ToArray();
        foreach(var group in renderers.GroupBy(r=>r.sharedMaterial))
        {
            var entries=group.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=parent.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(entries);mesh=Save(mesh,"Batch_"+name+"_"+group.Key.name+"_"+meshSerial++);
            var t=Group(parent,"Combined "+group.Key.name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
            foreach(var r in group)Object.DestroyImmediate(r.gameObject);
        }
    }
}
