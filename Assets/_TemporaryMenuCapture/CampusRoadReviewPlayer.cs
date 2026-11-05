using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Temporary standalone measurement harness; inactive without an explicit command-line argument.
public sealed class CampusRoadReviewPlayer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (!Environment.GetCommandLineArgs().Contains("-campusBenchmark")) return;
        var go = new GameObject("[Temporary Campus Benchmark]");
        DontDestroyOnLoad(go);
        go.AddComponent<CampusRoadReviewPlayer>();
    }
    IEnumerator Start()
    {
        string[] args = Environment.GetCommandLineArgs();
        string output = args[Array.IndexOf(args,"-campusBenchmark")+1];
        Directory.CreateDirectory(output);
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=-1;
        Application.runInBackground=true;
        yield return new WaitForSecondsRealtime(2);
        UniDoom.UI.GameMenus.Instance.StartNewGame();
        yield return new WaitForSecondsRealtime(10);
        var original = Camera.main;
        var go = new GameObject("Benchmark Camera");
        var cam=go.AddComponent<Camera>();
        cam.CopyFrom(original);
        cam.enabled=true;
        var data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing=true;
        original.enabled=false;
        foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled=false;
        var positions = new[]{new Vector3(78,1.8f,-46),new Vector3(48,1.8f,-19),new Vector3(14,1.8f,2),new Vector3(14,1.8f,43)};
        var targets = new[]{new Vector3(58,2,-16),new Vector3(-43,5,18),new Vector3(-43,6,34),new Vector3(-43,6,83)};
        using var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count");
        using var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");
        using var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread");
        using var render = ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Render Thread");
        using var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame");
        var rows = new List<string>{"view,frames,frameMedianMs,frameP95Ms,mainMedianMs,renderMedianMs,gpuMedianMs,batchesMedian,trianglesMedian,gcMedianBytes"};
        var timings=new FrameTiming[1];
        for(int view=0;view<positions.Length;view++)
        {
            cam.transform.SetPositionAndRotation(positions[view],Quaternion.LookRotation(targets[view]-positions[view]));
            yield return new WaitForSecondsRealtime(3);
            var frameValues=new List<double>(); var mainValues=new List<double>(); var renderValues=new List<double>(); var gpuValues=new List<double>(); var batchValues=new List<double>(); var triangleValues=new List<double>(); var gcValues=new List<double>();
            for(int i=0;i<150;i++)
            {
                FrameTimingManager.CaptureFrameTimings();
                yield return null;
                frameValues.Add(Time.unscaledDeltaTime*1000);
                mainValues.Add(main.LastValue/1000000.0); renderValues.Add(render.LastValue/1000000.0);
                batchValues.Add(batches.LastValue); triangleValues.Add(triangles.LastValue); gcValues.Add(gc.LastValue);
                if(FrameTimingManager.GetLatestTimings(1,timings)>0 && timings[0].gpuFrameTime>0) gpuValues.Add(timings[0].gpuFrameTime);
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"view-"+view+".png"));
            rows.Add(string.Join(",",view,frameValues.Count,P(frameValues,.5),P(frameValues,.95),P(mainValues,.5),P(renderValues,.5),P(gpuValues,.5),P(batchValues,.5),P(triangleValues,.5),P(gcValues,.5)));
            File.WriteAllLines(Path.Combine(output,"timings.csv"),rows);
            yield return null;
        }
        yield return WalkRoutes(cam,output);
        File.WriteAllText(Path.Combine(output,"complete.txt"),SystemInfo.graphicsDeviceName+"\n"+SystemInfo.processorType+"\n"+Screen.width+"x"+Screen.height+"\n"+SceneManager.GetActiveScene().path);
        yield return new WaitForSecondsRealtime(1);
        Application.Quit();
    }
    IEnumerator WalkRoutes(Camera cam,string output)
    {
        var player=GameManager.SessionPlayer;var controller=player.GetComponent<CharacterController>();player.MovementEnabled=false;player.Teleport(new Vector3(78,2,-65),Quaternion.identity);
        var points=new List<Vector3>{new Vector3(78,0,-14)};
        for(int i=1;i<=18;i++){float a=i/18f*Mathf.PI/2;points.Add(new Vector3(68+10*Mathf.Cos(a),0,-14+10*Mathf.Sin(a)));}
        points.AddRange(new[]{new Vector3(-43,0,-4),new Vector3(-43,0,14),new Vector3(-43,0,-4),new Vector3(-98,0,-4),new Vector3(14,0,-4),new Vector3(14,0,101),new Vector3(14,0,60),new Vector3(-43,0,60),new Vector3(-43,0,68),new Vector3(-43,0,60),new Vector3(14,0,60),new Vector3(14,0,23),new Vector3(63,0,23),new Vector3(63,0,29)});
        float vertical=0;var report=new List<string>();
        for(int index=0;index<points.Count;index++)
        {
            float lastProgress=Time.realtimeSinceStartup;float closest=float.PositiveInfinity;
            while(true)
            {
                if(Time.timeScale==0){lastProgress=Time.realtimeSinceStartup;yield return null;continue;}
                var delta=points[index]-player.transform.position;delta.y=0;float distance=delta.magnitude;if(distance<.18f)break;
                if(distance<closest-.025f){closest=distance;lastProgress=Time.realtimeSinceStartup;}
                if(Time.realtimeSinceStartup-lastProgress>6||player.transform.position.y<-3)
                {report.Add("FAIL route "+index+" target="+points[index]+" player="+player.transform.position+" timeScale="+Time.timeScale);File.WriteAllLines(Path.Combine(output,"walk.txt"),report);ScreenCapture.CaptureScreenshot(Path.Combine(output,"walk-failure.png"));yield return new WaitForEndOfFrame();yield break;}
                vertical=controller.isGrounded?-2:vertical-9.81f*Time.deltaTime;
                controller.Move((delta.normalized*(distance<1?2.5f:6f)+Vector3.up*vertical)*Time.deltaTime);
                cam.transform.position=new Vector3(controller.bounds.center.x,controller.bounds.min.y+1.75f,controller.bounds.center.z);cam.transform.rotation=Quaternion.LookRotation(delta.normalized+Vector3.down*.035f);yield return null;
            }
            report.Add("PASS route "+index+" target="+points[index]+" foot="+controller.bounds.min.y);File.WriteAllLines(Path.Combine(output,"walk.txt"),report);
            if(index==20||index==27||index==32){ScreenCapture.CaptureScreenshot(Path.Combine(output,"walk-"+index+".png"));yield return null;}
        }
        report.Add("PASS complete road and pedestrian traversal using actual player CharacterController.Move, automatic gate and all building stairs.");File.WriteAllLines(Path.Combine(output,"walk.txt"),report);
    }
    static string P(List<double> values,double p) { if(values.Count==0)return "unavailable"; values.Sort(); return values[(int)((values.Count-1)*p)].ToString("F3",System.Globalization.CultureInfo.InvariantCulture); }
}
