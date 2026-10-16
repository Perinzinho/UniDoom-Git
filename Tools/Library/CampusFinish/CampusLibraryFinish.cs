using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using TotalChaosMaps;
using Object=UnityEngine.Object;

public static class CampusLibraryFinish
{
    const string Art="Assets/_Project/Scenes/Library";
    const string Mat=Art+"/Materials/CampusFinish";
    const string Meshes=Art+"/Meshes/CampusFinish";
    const string Textures=Art+"/Textures/CampusFinish";
    const string Reflections=Art+"/Reflections/CampusFinish";
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    static Material wall,ceiling,grid,stain;
    static readonly List<CombineInstance> rails=new List<CombineInstance>();
    public static void BuildAndVerify()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/LibraryPreview/"))throw new Exception("Use isolated project");
        foreach(var p in new[]{Mat,Meshes,Textures+"/Packed",Reflections})Directory.CreateDirectory(p);
        AssetDatabase.Refresh();
        foreach(var path in Directory.GetFiles(Textures,"*.png",SearchOption.AllDirectories))
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=path.Contains("nor_gl")?TextureImporterType.NormalMap:TextureImporterType.Default;
            ti.sRGBTexture=path.EndsWith("Diffuse.png");ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.filterMode=FilterMode.Trilinear;ti.anisoLevel=4;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
        }
        wall=Surface("UniversityPaintedPlaster","painted_plaster_wall",new Color(.85f,.84f,.77f),.23f,.5f);
        ceiling=Surface("MineralCeilingPanels","white_plaster_02",new Color(.82f,.82f,.76f),.18f,1);
        grid=new Material(Shader.Find("UniDoom/TotalChaosInspiredSurface")){name="AgedCeilingGrid",enableInstancing=true};grid.SetColor("_BaseColor",new Color(.22f,.24f,.22f));grid.SetFloat("_Smoothness",.18f);grid.SetFloat("_Metallic",.2f);grid=SaveMaterial(grid);
        stain=new Material(wall){name="LocalizedWaterStain"};stain.SetColor("_BaseColor",new Color(.48f,.43f,.31f));stain=SaveMaterial(stain);
        var root=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try
        {
            var art=root.transform.Find("Abandoned Library Interior");
            foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string parent=r.transform.parent?r.transform.parent.name:r.name;
                bool overhead=parent.Contains("ceiling slab")||r.name.IndexOf("roof",StringComparison.OrdinalIgnoreCase)>=0;
                bool largeWall=parent=="Peeling institutional wall"||parent=="Rear wall";
                r.sharedMaterials=r.sharedMaterials.Select(m=>m&&m.name=="Mold"?(overhead?ceiling:wall):m&&m.name=="BrokenPlaster"&&largeWall?wall:m).ToArray();
            }
            CeilingGrid(art);
            int count=0;
            foreach(var light in root.GetComponentsInChildren<Light>())
            {
                string fixture=light.transform.parent?light.transform.parent.name:"";
                if(fixture!="Damaged fluorescent fixture 2"&&fixture!="Damaged fluorescent fixture 6"&&fixture!="Damaged fluorescent fixture 8")continue;
                var flicker=light.GetComponent<TCFlickerLight>();if(!flicker)flicker=light.gameObject.AddComponent<TCFlickerLight>();
                flicker.synchronizeEmission=true;flicker.flickerAmount=count==1?.62f:.38f;flicker.speed=3.2f+count*1.9f;flicker.dropoutChancePerSecond=count==1?.32f:.20f;count++;
            }
            if(count!=3)throw new Exception("Expected three defective fixtures");
            PrefabUtility.SaveAsPrefabAsset(root,LibraryBuilder.Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();BakeReflections();AssetDatabase.SaveAssets();Debug.Log("CAMPUS_LIBRARY_BUILD_PASS");
        LibraryDecayVerifier.VerifyAndRender();SessionState.SetBool("CampusFlickerPending",true);LibraryDecayPlayCheck.Run();
    }
    static Material SaveMaterial(Material m)
    {
        string path=Mat+"/"+m.name+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(old){EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);return old;}
        AssetDatabase.CreateAsset(m,path);return m;
    }
    static Material Surface(string name,string source,Color tint,float normal,float tiling)
    {
        var m=new Material(AssetDatabase.LoadAssetAtPath<Material>(Art+"/Materials/DecayRevision/BrokenPlaster.mat")){name=name,enableInstancing=true};
        string folder=Textures+"/"+source+"/";var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);t.LoadImage(File.ReadAllBytes(folder+"arm.png"));var pixels=t.GetPixels32();
        for(int i=0;i<pixels.Length;i++){var c=pixels[i];pixels[i]=new Color32(c.b,c.r,c.g,(byte)(255-c.g));}t.SetPixels32(pixels);t.Apply();
        string packed=Textures+"/Packed/"+name+".png";File.WriteAllBytes(packed,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(packed);
        var ti=(TextureImporter)AssetImporter.GetAtPath(packed);ti.sRGBTexture=false;ti.maxTextureSize=2048;ti.mipmapEnabled=true;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
        var mask=AssetDatabase.LoadAssetAtPath<Texture2D>(packed);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"Diffuse.png"));m.SetTextureScale("_BaseMap",Vector2.one*tiling);
        m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"nor_gl.png"));m.SetTexture("_MetallicGlossMap",mask);m.SetTexture("_OcclusionMap",mask);m.SetTexture("_DirtMap",mask);
        m.SetColor("_BaseColor",tint);m.SetFloat("_BumpScale",normal);m.SetFloat("_OcclusionStrength",.35f);m.SetVector("_SpecColor",new Vector4(.06f,0,.95f,.95f));return SaveMaterial(m);
    }
    static void CeilingGrid(Transform art)
    {
        var old=art.Find("University suspended ceiling grid");if(old)Object.DestroyImmediate(old.gameObject);
        var g=new GameObject("University suspended ceiling grid").transform;g.SetParent(art,false);rails.Clear();
        var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var source=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(cube);
        for(float x=-12;x<=12.01f;x+=1.2f)Rail(source,V(x,4.354f,0),V(.024f,.018f,39.3f));
        for(float z=-19.2f;z<=19.21f;z+=.6f)Rail(source,V(0,4.354f,z),V(24.2f,.018f,.022f));
        var mesh=new Mesh{name="SuspendedCeilingRails"};mesh.CombineMeshes(rails.ToArray());Object.DestroyImmediate(source);SaveMesh(mesh);
        g.gameObject.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Meshes+"/SuspendedCeilingRails.asset");g.gameObject.AddComponent<MeshRenderer>().sharedMaterial=grid;
        foreach(var p in new[]{V(10.2f,4.353f,9.9f),V(10.2f,4.353f,-6.3f),V(9,4.353f,-6.3f),V(1.8f,4.353f,6.3f)})
        {
            var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);panel.name="Localized old ceiling water stain";panel.transform.SetParent(g,false);panel.transform.localPosition=p;panel.transform.localScale=V(1.17f,.025f,.57f);panel.GetComponent<MeshRenderer>().sharedMaterial=stain;Object.DestroyImmediate(panel.GetComponent<Collider>());
        }
    }
    static void Rail(Mesh source,Vector3 pos,Vector3 size)=>rails.Add(new CombineInstance{mesh=source,transform=Matrix4x4.TRS(pos,Quaternion.identity,size)});
    static void SaveMesh(Mesh mesh)
    {
        string path=Meshes+"/"+mesh.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,path);
    }
    static void BakeReflections()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(LibraryBuilder.Prefab));root.transform.position=-Vector3.Scale(LibraryBuilder.FloorOrigin,LibraryBuilder.MapScale);
        var region=root.GetComponentInChildren<TCRegionAtmosphere>();RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=region.ambient;RenderSettings.fog=true;RenderSettings.fogColor=region.fogColor;RenderSettings.fogDensity=region.fogDensity;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),"Assets/_TemporaryTools/CampusReflections.unity");ShaderUtil.allowAsyncCompilation=false;
        int index=0;foreach(var probe in root.GetComponentsInChildren<ReflectionProbe>()){probe.mode=ReflectionProbeMode.Baked;probe.clearFlags=ReflectionProbeClearFlags.SolidColor;probe.backgroundColor=region.fogColor;if(!Lightmapping.BakeReflectionProbe(probe,Reflections+"/Library_"+index+++".exr"))throw new Exception("Reflection bake failed");}
        AssetDatabase.Refresh();var prefab=PrefabUtility.LoadPrefabContents(LibraryBuilder.Prefab);
        try{index=0;foreach(var probe in prefab.GetComponentsInChildren<ReflectionProbe>())probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Cubemap>(Reflections+"/Library_"+index+++".exr");PrefabUtility.SaveAsPrefabAsset(prefab,LibraryBuilder.Prefab);}
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
    }
}

[InitializeOnLoad]
public static class CampusFlickerCheck
{
    static double began;
    static Light[] lights;
    static float[] minimum,maximum;
    static bool emissionChanged;
    static CampusFlickerCheck(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(!SessionState.GetBool("CampusFlickerPending",false)||!EditorApplication.isPlaying)return;
        try
        {
            if(lights==null)
            {
                lights=Object.FindObjectsByType<TCFlickerLight>(FindObjectsSortMode.None).Select(f=>f.GetComponent<Light>()).ToArray();
                if(lights.Length!=3)throw new Exception("Expected three flickering lights in Play Mode");
                minimum=Enumerable.Repeat(float.MaxValue,3).ToArray();maximum=new float[3];began=EditorApplication.timeSinceStartup;
            }
            for(int i=0;i<lights.Length;i++)
            {
                minimum[i]=Mathf.Min(minimum[i],lights[i].intensity);maximum[i]=Mathf.Max(maximum[i],lights[i].intensity);
                foreach(var r in lights[i].transform.parent.GetComponentsInChildren<Renderer>())
                {
                    var m=r.sharedMaterial;if(!m||!m.IsKeywordEnabled("_EMISSION"))continue;
                    var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);if(block.GetColor("_EmissionColor").maxColorComponent<m.GetColor("_EmissionColor").maxColorComponent*.95f)emissionChanged=true;
                }
            }
            if(EditorApplication.timeSinceStartup-began<4) return;
            if(Enumerable.Range(0,3).Any(i=>maximum[i]-minimum[i]<.05f)||!emissionChanged)throw new Exception("Flicker or tube emission did not change");
            File.WriteAllText("Evidence/Decay/flicker-validation.txt","PASS\nThree independent fluorescent fixtures vary intensity in real Play Mode.\nTube emission follows the existing TCFlickerLight component via MaterialPropertyBlock.\nMeasured intensity ranges: "+string.Join(", ",Enumerable.Range(0,3).Select(i=>minimum[i].ToString("F2")+".."+maximum[i].ToString("F2")))+"\n");
            SessionState.SetBool("CampusFlickerPending",false);Debug.Log("CAMPUS_FLICKER_PLAY_PASS");
        }
        catch(Exception e){SessionState.SetBool("CampusFlickerPending",false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
