using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static partial class CampusLibraryCirculation
{
    const string Folder="Temp/CampusLibraryCirculation/";
    const string PrefabPath="Assets/_Project/Scenes/CampusLibrary/Prefabs/CampusLibrary.prefab";
    const string ScenePath="Assets/_Project/Scenes/CampusLibrary/CampusLibrary.unity";
    static CampusLibraryCirculation(){EditorApplication.update+=Tick;EditorApplication.update+=PlayTick;}
    static void Tick()
    {
        string request=Folder+"request.txt";if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
        string command=File.ReadAllText(request).Trim();File.Delete(request);if(File.Exists(Folder+"error.txt"))File.Delete(Folder+"error.txt");
        try{if(command=="inspect")Inspect();else if(command=="capture")Capture("Antes");else if(command=="after")Capture("Depois");else if(command=="walls")FitWalls();else if(command=="passage")FitPassage();else if(command=="bathroom")FitBathroom();else if(command=="wallprobe")WallProbe();else if(command=="validate")Validate();else if(command=="playreview")StartPlayReview();else throw new Exception("Unknown request "+command);File.WriteAllText(Folder+"done.txt",command+" "+DateTime.UtcNow.ToString("o"));}catch(Exception e){File.WriteAllText(Folder+"error.txt",e.ToString());Debug.LogException(e);}
    }
    static string V(Vector3 v)=>string.Format(CultureInfo.InvariantCulture,"({0:F4},{1:F4},{2:F4})",v.x,v.y,v.z);
    static Bounds WorldBox(BoxCollider box){var b=new Bounds(box.transform.TransformPoint(box.center),Vector3.zero);foreach(float x in new[]{-.5f,.5f})foreach(float y in new[]{-.5f,.5f})foreach(float z in new[]{-.5f,.5f})b.Encapsulate(box.transform.TransformPoint(box.center+Vector3.Scale(box.size,new Vector3(x,y,z))));return b;}
    static Bounds RenderBounds(GameObject go){var rr=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();var b=rr[0].bounds;foreach(var r in rr)b.Encapsulate(r.bounds);return b;}
    static List<List<int>> Parts(Mesh mesh)
    {
        var vv=mesh.vertices;var parent=Enumerable.Range(0,vv.Length).ToArray();int Find(int i){while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;}void Join(int a,int b){a=Find(a);b=Find(b);if(a!=b)parent[a]=b;}
        var weld=new Dictionary<Vector3Int,int>();for(int i=0;i<vv.Length;i++){var v=vv[i];var key=new Vector3Int(Mathf.RoundToInt(v.x*10000),Mathf.RoundToInt(v.y*10000),Mathf.RoundToInt(v.z*10000));if(weld.TryGetValue(key,out int j))Join(i,j);else weld.Add(key,i);}var tt=mesh.triangles;for(int i=0;i<tt.Length;i+=3){Join(tt[i],tt[i+1]);Join(tt[i],tt[i+2]);}var groups=new Dictionary<int,List<int>>();foreach(int i in tt.Distinct()){int id=Find(i);if(!groups.TryGetValue(id,out var list))groups[id]=list=new List<int>();list.Add(i);}return groups.Values.ToList();
    }
    static Bounds PartBounds(MeshFilter mf,Vector3[] vv,List<int> part){var b=new Bounds(mf.transform.TransformPoint(vv[part[0]]),Vector3.zero);foreach(int i in part)b.Encapsulate(mf.transform.TransformPoint(vv[i]));return b;}
    static void Inspect()
    {
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);var sb=new StringBuilder();var parts=new StringBuilder();
        try
        {
            sb.AppendLine("Open scenes: "+string.Join(", ",Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path+" dirty="+SceneManager.GetSceneAt(i).isDirty)));
            foreach(var b in root.GetComponentsInChildren<BoxCollider>(true)){var bounds=WorldBox(b);sb.AppendLine("BOX "+AnimationUtility.CalculateTransformPath(b.transform,root.transform)+" center="+V(bounds.center)+" size="+V(bounds.size)+" enabled="+b.enabled);}
            foreach(var mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!mf.sharedMesh)continue;string path=AssetDatabase.GetAssetPath(mf.sharedMesh);if(!path.StartsWith("Assets/_Project/Scenes/CampusLibrary/Meshes/"))continue;var vv=mf.sharedMesh.vertices;foreach(var part in Parts(mf.sharedMesh)){var b=PartBounds(mf,vv,part);parts.AppendLine(path+"\t"+string.Join(";",mf.GetComponent<Renderer>().sharedMaterials.Select(m=>m?m.name:"NULL"))+"\t"+V(b.center)+"\t"+V(b.size));}
            }
            File.WriteAllText(Folder+"inspection.txt",sb.ToString());File.WriteAllText(Folder+"parts.tsv",parts.ToString());
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    static void Capture(string phase)
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);try
        {
            SceneManager.SetActiveScene(scene);var go=new GameObject("Temporary circulation camera");SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.fieldOfView=60;camera.nearClipPlane=.03f;camera.farClipPlane=100;camera.allowHDR=true;camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            try
            {
                var views=new[]{("Passagem",new Vector3(0,1.84f,8),new Vector3(0,1.8f,14)),("Retorno",new Vector3(0,1.84f,15),new Vector3(0,1.8f,10)),("Sala1",new Vector3(-23.65f,1.84f,22.4f),new Vector3(-23.65f,1.7f,26.4f)),("Sala2",new Vector3(-15.25f,1.84f,22.4f),new Vector3(-15.25f,1.7f,26.4f)),("Banheiros",new Vector3(17.6f,1.84f,3.1f),new Vector3(17.6f,1.65f,10)),("EstantesWC",new Vector3(12.6f,1.84f,.8f),new Vector3(17,1.7f,7)),("ParedeOeste",new Vector3(-12,1.84f,8),new Vector3(-12,2.5f,12.15f)),("ParedeLeste",new Vector3(12,1.84f,8),new Vector3(12,2.5f,12.15f)),("Norte",new Vector3(-11,1.84f,23),new Vector3(-11,2.4f,30.3f))};var details=new[]{("Janela",new Vector3(26.2f,1.84f,14.5f),new Vector3(28.15f,2.25f,15.115f)),("Canto",new Vector3(-26,1.84f,22.5f),new Vector3(-27.9f,2.1f,24)),("Fechamento",new Vector3(-18,1.84f,20.5f),new Vector3(-19.45f,3.65f,24))};foreach(var v in views.Concat(details)){camera.transform.position=v.Item2;camera.transform.LookAt(v.Item3);Render(camera,"output/CampusLibrary-Circulacao-"+phase+"-"+v.Item1+".png");}
            }finally{Object.DestroyImmediate(go);}
        }finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
    }
    static void Render(Camera camera,string file)
    {
        var previous=RenderTexture.active;var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);var old=camera.targetTexture;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();File.WriteAllBytes(file,texture.EncodeToPNG());}finally{camera.targetTexture=old;RenderTexture.active=previous;Object.DestroyImmediate(texture);Object.DestroyImmediate(target);}
    }
}
