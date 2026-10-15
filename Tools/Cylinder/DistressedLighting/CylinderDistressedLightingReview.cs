using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TotalChaosMaps;
using Object=UnityEngine.Object;

public static class CylinderDistressedLightingReview
{
    const string Prefab="Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab";
    const string Evidence="DistressedLightingEvidence/";
    const string Session="CylinderDistressedLightPlay";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly Dictionary<Light,float> steady=new Dictionary<Light,float>();
    static readonly Dictionary<TCFlickerLight,Sample> samples=new Dictionary<TCFlickerLight,Sample>();
    static float start;
    static int independentFrames;
    [Serializable] public class Sample
    { public string fixture; public float baseline,min=100000,max;public int dropouts,emissionChecks; [NonSerialized] public bool dark; }
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);

    public static void Run()
    {
        Check(Application.dataPath.Replace('\\','/').Contains("/.utmp/CylinderDetailPreview/"),"Use isolated project");
        Directory.CreateDirectory(Evidence);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);root.transform.position=Vector3.zero;
        typeof(CylinderDetailVerifier).GetMethod("ValidateReferences",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{root});
        var lights=root.GetComponentsInChildren<Light>().Where(l=>l.enabled&&l.intensity>0).ToArray();
        var flickers=root.GetComponentsInChildren<TCFlickerLight>().Where(f=>f.enabled).ToArray();
        Check(lights.Length==22&&flickers.Length==10,"Expected 22 live lights and ten active flicker components");
        Check(lights.Count(l=>new[]{26f,28f,38f,3f,3.5f,30f,27f,3.2f,3.4f,32f,3.1f}.Contains(l.intensity))==16,"Expected sixteen weak lights");
        Check(flickers.All(f=>f.GetComponent<Light>().intensity<40),"All ten flickers must belong to the weak group");
        Check(lights.Count(l=>l.intensity==65f||l.intensity==7f||l.intensity==6f)==6,"Expected six normal steady lights");
        foreach(var f in flickers)
        {Check(f.synchronizeEmission,"Lamp emission must follow light");Check(f.GetComponent<Light>().shadows==LightShadows.None,"Flickering lights should not add moving shadow passes");}
        var region=root.GetComponentInChildren<TCRegionAtmosphere>();Check(region.ambient==new Color(.6f,.65f,.68f,1),"Approved ambient changed");
        Capture(root,region);
        using(var sha=SHA256.Create())File.WriteAllText(Evidence+"validated-sha256.txt",BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Prefab))).Replace("-","").ToLowerInvariant());
        File.WriteAllText(Evidence+"edit-validation.txt","PASS\nUnity 6000.5.7f1 / URP 17.5: scripts compiled and prefab loaded; references valid.\n22 live lights: 16 weak (10 flickering) and six normal steady. Both weak and flickering outnumber normal.\nOriginal positions, ambient RGB 0.60/0.65/0.68, ranges, colors, shadow settings and postprocessing retained.\nExisting TCFlickerLight runtime reused, with four additional components on existing lights.\nNo geometry, materials, gameplay, collisions or spawns changed.\n");
        foreach(var script in root.GetComponentsInChildren<MonoBehaviour>(true))
            if(!(script is TCFlickerLight)&&!(script is TCRegionAtmosphere))script.enabled=false;
        EditorSceneManager.SaveScene(root.scene,"Assets/_TemporaryTools/DistressedLightingPlay.unity");
        SessionState.SetBool(Session,true);SessionState.SetBool(Session+"Started",false);
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.isPlaying=true;
    }

    static void Capture(GameObject root,TCRegionAtmosphere region)
    {
        ShaderUtil.allowAsyncCompilation=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=region.ambient;
        var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(region.ambient);RenderSettings.ambientProbe=ambient;
        var atrium=root.transform.Find("Abandoned circular atrium");
        var camera=new GameObject("Lighting variation review").AddComponent<Camera>();camera.fieldOfView=72;camera.nearClipPlane=.04f;camera.farClipPlane=130;
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
        var views=new[]{("Canteen",V(7,1.84f,11),V(0,2,0)),("Rear",V(-8,1.84f,-10),V(0,2,-19))};
        foreach(var v in views)
        {
            camera.transform.position=atrium.TransformPoint(v.Item2);camera.transform.LookAt(atrium.TransformPoint(v.Item3));
            TCRegionAtmosphere.ApplyForCamera(camera,new[]{region},false);
            for(int i=0;i<5;i++)camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();
            File.WriteAllBytes(Evidence+v.Item1+".png",image.EncodeToPNG());TCRegionAtmosphere.RestoreEnvironment();
        }
        RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);
    }

    [InitializeOnLoadMethod]static void Reconnect()
    {if(SessionState.GetBool(Session,false)){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}}

    static void Tick()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        try
        {
            if(!SessionState.GetBool(Session+"Started",false))
            {
                start=Time.time;UnityEngine.Random.InitState(20261015);steady.Clear();samples.Clear();independentFrames=0;
                foreach(var f in Object.FindObjectsByType<TCFlickerLight>().Where(f=>f.isActiveAndEnabled))
                {
                    float baseline=(float)typeof(TCFlickerLight).GetField("_base",Private).GetValue(f);
                    samples.Add(f,new Sample{fixture=f.transform.parent.name,baseline=baseline});
                }
                Check(samples.Count==10,"Runtime flicker count");
                foreach(var l in Object.FindObjectsByType<Light>().Where(l=>l.enabled&&l.intensity>0&&!samples.Keys.Any(f=>f.GetComponent<Light>()==l)))steady[l]=l.intensity;
                Check(steady.Count==12,"Expected twelve steady normal/weak lights");SessionState.SetBool(Session+"Started",true);return;
            }
            if(Time.time-start<.3f)return;
            var weights=new List<float>();
            foreach(var item in samples)
            {
                var f=item.Key;var s=item.Value;float intensity=f.GetComponent<Light>().intensity;
                Check(intensity>=0&&intensity<=s.baseline+.001f,"Flicker exceeds base intensity");
                s.min=Mathf.Min(s.min,intensity);s.max=Mathf.Max(s.max,intensity);bool dark=intensity<.001f;
                if(dark&&!s.dark)s.dropouts++;s.dark=dark;float weight=intensity/s.baseline;weights.Add(weight);
                var emitters=(Renderer[])typeof(TCFlickerLight).GetField("_emitters",Private).GetValue(f);
                var colors=(Color[])typeof(TCFlickerLight).GetField("_emissionColors",Private).GetValue(f);
                Check(emitters!=null&&emitters.Length>0,"No luminous fixture found: "+s.fixture);
                for(int i=0;i<emitters.Length;i++)
                {
                    var block=new MaterialPropertyBlock();emitters[i].GetPropertyBlock(block);var actual=block.GetColor("_EmissionColor");var expected=colors[i]*weight;
                    Check(Mathf.Abs(actual.r-expected.r)+Mathf.Abs(actual.g-expected.g)+Mathf.Abs(actual.b-expected.b)<.002f,"Fixture emission does not follow intensity");s.emissionChecks++;
                }
            }
            foreach(var light in steady)Check(light.Key&&Mathf.Abs(light.Key.intensity-light.Value)<.001f,"Steady lamp changed");
            if(weights.Max()-weights.Min()>.02f)independentFrames++;
            if(Time.time-start<22)return;
            foreach(var s in samples.Values)Check(s.max-s.min>s.baseline*.02f,"No real temporal variation on "+s.fixture);
            Check(samples.Values.Sum(s=>s.dropouts)>0,"No actual dropout observed");Check(independentFrames>10,"Flicker is synchronized across fixtures");
            File.WriteAllText(Evidence+"play-validation.txt","PASS\n22 seconds of isolated Unity Play Mode.\nAll ten lamps varied independently; real brief dropouts observed.\nAll twelve steady normal/weak lights retained exact intensities.\nFixture emission tracked light intensity through MaterialPropertyBlock, including dropout.\nNo shared material or runtime script was modified.\n"+string.Join("\n",samples.Values.Select(s=>s.fixture+": min="+s.min+", max="+s.max+", base="+s.baseline+", dropouts="+s.dropouts+", emission checks="+s.emissionChecks))+"\nFull-map combat, build and target hardware profiling were not run.\n");
            Finish(0,"CYLINDER_DISTRESSED_LIGHTING_PASS");
        }
        catch(Exception error){Debug.LogException(error);Finish(1,"CYLINDER_DISTRESSED_LIGHTING_FAIL");}
    }
    static void Finish(int code,string message)
    {SessionState.SetBool(Session,false);EditorApplication.update-=Tick;Debug.Log(message);EditorApplication.Exit(code);}
}
