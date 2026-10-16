using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TotalChaosMaps;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class CampusNightPlayCheck
{
    const string Pending="CampusNightPlayPending";
    static CharacterController player;
    static Transform garden;
    static int steps;
    static float startTime;
    static string failure;
    static readonly StringBuilder report=new StringBuilder();
    static CampusNightPlayCheck(){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}
    static void Log(string message,string stack,LogType type)
    {if(SessionState.GetBool(Pending,false)&&(type==LogType.Error||type==LogType.Exception))failure=message;}
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        foreach(var spawner in Object.FindObjectsByType<EnemySpawner>())spawner.enabled=false;
        report.AppendLine("Existing empty EnemySpawner disabled in unsaved test scene only.");
        SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Pending,false)||!Application.isPlaying||EditorApplication.isCompiling)return;
        try
        {
            if(failure!=null)throw new Exception(failure);
            if(!player)
            {
                player=Object.FindFirstObjectByType<CharacterController>();if(!player)return;
                var movement=player.GetComponent<Player>();if(movement)movement.enabled=false;
                garden=Object.FindObjectsByType<Transform>().First(t=>t.name=="EntranceGarden").Find("CampusArchitecture");
                var local=garden.InverseTransformPoint(player.transform.position);
                if(Mathf.Abs(local.z+37)>.25f||Mathf.Abs(local.x)>.1f)throw new Exception("Player did not spawn at exterior marker: "+local);
                report.AppendLine("GameManager spawned original player at exterior marker: "+local);
                report.AppendLine("Actual controller height="+(player.height*player.transform.lossyScale.y)+" m; radius="+(player.radius*Mathf.Max(player.transform.lossyScale.x,player.transform.lossyScale.z))+" m.");
                var originalPosition=player.transform.position;
                player.Move(-garden.forward*4);
                var blockedAt=garden.InverseTransformPoint(player.transform.position);
                if(blockedAt.z< -39.4f||blockedAt.z> -38)throw new Exception("Closed gate failed to stop original player: "+blockedAt);
                report.AppendLine("Closed external gate stopped original CharacterController at "+blockedAt+"; returned to original exterior start for traversal test.");
                player.enabled=false;player.transform.position=originalPosition;player.enabled=true;
                startTime=Time.realtimeSinceStartup;steps=0;
            }
            var p=garden.InverseTransformPoint(player.transform.position);
            // Drive the original CharacterController through real fixed-open doors and the reception.
            player.Move(garden.forward*.13f+Vector3.down*.12f);
            var q=garden.InverseTransformPoint(player.transform.position);steps++;
            if(q.y<-.25f||q.y>2)throw new Exception("Player lost supported floor at "+q);
            if(steps%50==0)
            {
                var camera=Camera.main;
                if(!camera)throw new Exception("Missing active scene MainCamera");
                // This collision test advances faster than normal gameplay. Sample the
                // region at the camera's player-head target, without Cinemachine lag.
                camera.transform.position=player.transform.Find("Head").position;
                var region=TCRegionAtmosphere.ApplyForCamera(camera,Object.FindObjectsByType<TCRegionAtmosphere>(),false);
                string expected=q.z<0?"night_abandoned_campus":"night_campus_reception";
                if(!region||region.theme!=expected)throw new Exception("Incorrect night atmosphere at "+q);
                report.AppendLine("Walk sample: "+q+"; atmosphere="+region.theme);
            }
            if(steps>20&&q.z-p.z<.025f)throw new Exception("Controller stopped at "+q);
            if(q.z>=27.7f)
            {
                report.AppendLine("Original player traversed gate approach, entrance, lobby and corridor; final local position="+q+"; steps="+steps+"; elapsed="+(Time.realtimeSinceStartup-startTime).ToString("F2")+" s.");
                File.WriteAllText("NightEvidence/play-validation.txt",report+"CAMPUS_NIGHT_PLAY_PASS\n");
                SessionState.SetBool(Pending,false);EditorApplication.isPlaying=false;
                EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
            }
            if(Time.realtimeSinceStartup-startTime>100)throw new Exception("Play traversal timed out");
        }
        catch(Exception ex)
        {
            File.WriteAllText("NightEvidence/play-validation.txt",report+"FAIL "+ex+"\n");
            SessionState.SetBool(Pending,false);EditorApplication.isPlaying=false;
            Debug.LogException(ex);EditorApplication.delayCall+=()=>EditorApplication.Exit(1);
        }
    }
}
