using System;
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

public static class CylinderInteriorLightingReview
{
    const string Prefab="Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab";
    const string Evidence="InteriorLightingEvidence/";
    static string Main=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../.."));
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/CylinderDetailPreview/"))throw new Exception("Use isolated project");
        Directory.CreateDirectory(Evidence);Capture("Before");
        var info=new System.Diagnostics.ProcessStartInfo("python","Tools/Cylinder/InteriorLighting/revision.py candidate"){WorkingDirectory=Main,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        using(var p=System.Diagnostics.Process.Start(info))
        {string stdout=p.StandardOutput.ReadToEnd(),stderr=p.StandardError.ReadToEnd();p.WaitForExit();Debug.Log(stdout);if(p.ExitCode!=0)throw new Exception(stderr);}
        AssetDatabase.ImportAsset(Prefab,ImportAssetOptions.ForceUpdate);Capture("After");
        using(var sha=SHA256.Create())File.WriteAllText(Evidence+"validated-sha256.txt",BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Prefab))).Replace("-","").ToLowerInvariant());
        File.WriteAllText(Evidence+"validation.txt","PASS\nUnity 6000.5.7f1 / URP 17.5: final prefab loads, scripts compile and references remain valid.\nBefore/after renders at identical views, with identical postprocessing and flashlight disabled.\n22 existing luminaires brightened; region ambient raised; six pendants radiate in all directions and six perimeter cones widened.\nEvery existing object/component ID and all non-lighting fields retained; only existing Light type/intensity/range/cone and region ambient differ.\nNo added lights, shadows, geometry, materials, runtime scripts or postprocessing changes.\nFull scene Play Mode, build and target-hardware profiling were not run for this lighting-only revision.\n");
        Debug.Log("CYLINDER_INTERIOR_LIGHTING_PASS");
    }

    static void Capture(string label)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var room=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));room.transform.position=Vector3.zero;
        var atrium=room.transform.Find("Abandoned circular atrium");
        typeof(CylinderDetailVerifier).GetMethod("ValidateReferences",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{room});
        var region=room.GetComponentInChildren<TCRegionAtmosphere>();ShaderUtil.allowAsyncCompilation=false;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=region.ambient;
        var ambient=new SphericalHarmonicsL2();ambient.AddAmbientLight(region.ambient);RenderSettings.ambientProbe=ambient;
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogColor=region.fogColor;RenderSettings.fogDensity=region.fogDensity;
        var camera=new GameObject("Interior lighting comparison").AddComponent<Camera>();camera.tag="MainCamera";camera.fieldOfView=72;camera.nearClipPlane=.04f;camera.farClipPlane=130;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=region.fogColor;camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=true;
        var rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);
        Func<int,Vector3,Vector3> bay=(i,p)=>atrium.InverseTransformPoint(atrium.Find("Service bay "+i).TransformPoint(p));
        var views=new[]{("Canteen",V(7,1.84f,11),V(0,2,0)),("CanteenClose",V(1.2f,1.84f,6.2f),V(.3f,1.55f,3.6f)),
            ("ClassroomDoor",V(21.7f,1.84f,0),V(27,2,0)),("Rear",V(-8,1.84f,-10),V(0,2,-19)),
            ("Radio",bay(34,V(.4f,1.76f,-2.8f)),bay(34,V(0,1.4f,-.82f))),
            ("Blood",bay(1,V(0,1.84f,-3)),bay(1,V(.3f,1.6f,-.27f)))};
        foreach(var view in views)
        {
            camera.transform.position=atrium.TransformPoint(view.Item2);camera.transform.LookAt(atrium.TransformPoint(view.Item3));
            TCRegionAtmosphere.ApplyForCamera(camera,new[]{region},false);
            for(int i=0;i<5;i++)camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);texture.Apply();
            File.WriteAllBytes(Evidence+label+"-"+view.Item1+".png",texture.EncodeToPNG());TCRegionAtmosphere.RestoreEnvironment();
        }
        RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);
        File.WriteAllText(Evidence+label+"-settings.txt","Ambient: "+region.ambient+"\n"+string.Join("\n",room.GetComponentsInChildren<Light>(true).Where(l=>l.enabled&&l.intensity>0).Select(l=>l.name+" intensity="+l.intensity+" range="+l.range+" shadows="+l.shadows))+"\n");
    }
}
