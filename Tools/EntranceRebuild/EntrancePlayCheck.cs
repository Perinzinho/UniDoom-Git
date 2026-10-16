using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using TotalChaosMaps;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class EntrancePlayCheck
{
    static double began;
    static int index;
    static Camera camera;
    static Transform[] rooms;
    static readonly StringBuilder report=new StringBuilder();
    static string failure;
    static EntrancePlayCheck(){EditorApplication.update+=Tick;Application.logMessageReceived+=OnLog;}
    static void OnLog(string message,string stack,LogType type)
    {if(SessionState.GetBool("EntrancePlayPending",false)&&(type==LogType.Exception||type==LogType.Error))failure=message;}
    public static void Run()
    {
        if(!Application.dataPath.Replace('\\','/').Contains("/.utmp/EntranceRebuild/"))throw new Exception("Use isolated project");
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        // The incoming main scene contains an EnemySpawner with an empty spawn list.
        // Disable only these unrelated invalid spawners in this unsaved validation scene.
        foreach(var spawner in Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))spawner.enabled=false;
        SessionState.SetBool("EntrancePlayPending",true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool("EntrancePlayPending",false)||!EditorApplication.isPlaying||EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode&&!Application.isPlaying)return;
        try
        {
            if(!camera)
            {
                report.AppendLine("Unrelated EnemySpawner components disabled only in the unsaved validation scene (preexisting empty spawn list).");
                foreach(var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))c.enabled=false;
                camera=new GameObject("Entrance runtime validation").AddComponent<Camera>();camera.tag="MainCamera";
                rooms=EntranceAudit.Rooms.Select(name=>Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name==name)).ToArray();
                began=EditorApplication.timeSinceStartup;index=0;
            }
            if(failure!=null)throw new Exception(failure);
            if(index>=rooms.Length)
            {
                File.WriteAllText("Evidence/play-validation.txt","ENTRANCE_PLAY_PASS\n"+report);
                SessionState.SetBool("EntrancePlayPending",false);EditorApplication.isPlaying=false;
                EditorApplication.delayCall+=()=>EditorApplication.Exit(0);return;
            }
            var art=rooms[index].Find("Total Chaos Rebuild");var layout=art.GetComponent<EntranceDressingLayout>();layout.Refresh();
            var position=art.TransformPoint(new Vector3(0,1.65f,-layout.Size.y*.32f));camera.transform.position=position;camera.transform.forward=art.forward;
            var region=TCRegionAtmosphere.ApplyForCamera(camera,Object.FindObjectsByType<TCRegionAtmosphere>(FindObjectsSortMode.None),false);
            if(!region||!region.transform.IsChildOf(art))throw new Exception("Incorrect runtime region: "+rooms[index].name);
            foreach(var t in art.GetComponentsInChildren<Transform>())if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script");
            foreach(var renderer in art.GetComponentsInChildren<Renderer>())if(renderer.enabled&&renderer.sharedMaterials.Any(m=>!m||!m.shader||!m.shader.isSupported))throw new Exception("Unsupported shader/material");
            if(rooms[index].name=="DirectorRoom")
            {
                if(art.GetComponentsInChildren<TCFlickerLight>().Length!=0)throw new Exception("Office must have stable lighting");
                if(art.GetComponentsInChildren<Light>().Any(l=>!l.enabled||l.intensity<=0))throw new Exception("Office light lost power");
            }
            if(EditorApplication.timeSinceStartup-began>2)
            {
                report.AppendLine(rooms[index].name+": runtime fitting, shader support and regional atmosphere passed; dimensions="+layout.Size);
                index++;began=EditorApplication.timeSinceStartup;
            }
        }
        catch(Exception error)
        {
            File.WriteAllText("Evidence/play-validation.txt","FAIL\n"+error);SessionState.SetBool("EntrancePlayPending",false);
            EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(1);
        }
    }
}
