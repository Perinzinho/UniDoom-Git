using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using TotalChaosMaps;
using Object=UnityEngine.Object;

public static class NightWindowDiagnostic
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");ShaderUtil.allowAsyncCompilation=false;
        var hall=Object.FindObjectsByType<Transform>().First(t=>t.name=="EntranceHallway");var art=hall.Find("CampusArchitecture");art.GetComponent<CampusEntranceAnchor>().Refresh();
        var text=new StringBuilder();
        foreach(Transform child in hall)if(child.name.StartsWith("Wall"))foreach(var r in child.GetComponentsInChildren<Renderer>(true))text.AppendLine(child.name+"/"+r.name+" enabled="+r.enabled+" bounds="+r.bounds);
        var camera=new GameObject("Window diagnostic").AddComponent<Camera>();camera.fieldOfView=64;camera.nearClipPlane=.06f;camera.farClipPlane=100;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        camera.transform.position=art.TransformPoint(new Vector3(.5f,0,11))+Vector3.up*1.91f;camera.transform.LookAt(art.TransformPoint(new Vector3(-9,.45f,14)));
        TCRegionAtmosphere.ApplyForCamera(camera,Object.FindObjectsByType<TCRegionAtmosphere>(),false);
        foreach(var r in art.GetComponentsInChildren<Renderer>())if(r.sharedMaterial&&(r.sharedMaterial.name=="Dusty entrance glazing"||r.sharedMaterial.name=="Night clear dusty glazing"))r.enabled=false;
        Capture(camera,"Window-NoGlass");
        foreach(Transform child in hall)if(child.name.StartsWith("Wall"))foreach(var r in child.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        Capture(camera,"Window-NoLegacyWall");
        var lawn=art.Find("Night revision/Lawn visible through corridor windows");
        foreach(var r in lawn.GetComponentsInChildren<MeshRenderer>())if(r.sharedMaterial.name=="Unmaintained night lawn")
        {
            text.AppendLine("Lawn "+r.bounds+" material="+r.sharedMaterial.name+" texture="+r.sharedMaterial.GetTexture("_BaseMap").name+" normals="+r.GetComponent<MeshFilter>().sharedMesh.normals[0]);
            var m=new Material(Shader.Find("Universal Render Pipeline/Unlit"));m.SetTexture("_BaseMap",r.sharedMaterial.GetTexture("_BaseMap"));m.SetColor("_BaseColor",new Color(.20f,.26f,.20f,1));r.sharedMaterial=m;
        }
        Capture(camera,"Window-UnlitControl");
        File.WriteAllText("NightEvidence/window-diagnostic.txt",text.ToString());Debug.Log("NIGHT_WINDOW_DIAGNOSTIC_PASS");
    }
    static void Capture(Camera camera,string name)
    {
        var rt=new RenderTexture(1200,750,24);camera.targetTexture=rt;for(int i=0;i<3;i++)camera.Render();RenderTexture.active=rt;
        var image=new Texture2D(1200,750,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,750),0,0);image.Apply();File.WriteAllBytes("NightEvidence/"+name+".png",image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);
    }
}
