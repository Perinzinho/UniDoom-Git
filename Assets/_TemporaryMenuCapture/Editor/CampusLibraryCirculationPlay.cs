using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class CampusLibraryCirculation
{
    const string PlayKey="CampusLibraryCirculationReview";
    static Vector3 RouteStart(int route)=>route<3?new Vector3(-2.4f+2.4f*route,1.081f,9.4f):new Vector3(17.6f,1.081f,3.2f);
    static void StartPlayReview()
    {
        if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Play review requires clean open scenes");var previous=SceneManager.GetActiveScene();SessionState.SetString(PlayKey+"previous",previous.name);var scene=SceneManager.GetSceneByPath(ScenePath);bool added=!scene.IsValid()||!scene.isLoaded;if(added)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);SessionState.SetBool(PlayKey+"added",added);SessionState.SetString(PlayKey,"waiting");SessionState.SetInt(PlayKey+"route",0);SessionState.SetFloat(PlayKey+"started",(float)EditorApplication.timeSinceStartup);File.WriteAllText(Folder+"play-validation.txt","Controlled Play Mode requested.\n");EditorApplication.isPlaying=true;
    }
    static void PlayTick()
    {
        string phase=SessionState.GetString(PlayKey,"");if(phase==""||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try
        {
            if(!EditorApplication.isPlaying){if(EditorApplication.isPlayingOrWillChangePlaymode||phase=="waiting")return;var previous=SceneManager.GetSceneByName(SessionState.GetString(PlayKey+"previous",""));if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(SessionState.GetBool(PlayKey+"added",false)){var scene=SceneManager.GetSceneByPath(ScenePath);if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);}SessionState.EraseString(PlayKey);File.AppendAllText(Folder+"play-validation.txt","Exited Play Mode; editor scenes restored.\n");return;}
            double elapsed=EditorApplication.timeSinceStartup-SessionState.GetFloat(PlayKey+"started",0);if(elapsed>180)throw new Exception("Controlled passage review timed out");var player=GameManager.SessionPlayer;if(!player)return;var cc=player.GetComponent<CharacterController>();
            if(phase=="waiting"){if(elapsed<10)return;player.MovementEnabled=false;Time.timeScale=1;player.Teleport(RouteStart(0),Quaternion.identity);Physics.SyncTransforms();File.AppendAllText(Folder+"play-validation.txt","Player count="+UnityEngine.Object.FindObjectsByType<PlayerMovement>().Length+"; real capsule="+V(cc.bounds.size)+"\n");SessionState.SetString(PlayKey,"walking");return;}
            if(phase=="walking")
            {
                player.MovementEnabled=false;int route=SessionState.GetInt(PlayKey+"route",0);cc.Move(new Vector3(0,-.015f,.10f));if(player.transform.position.z<(route<3?14.6f:11.4f))return;File.AppendAllText(Folder+"play-validation.txt","PASS route "+route+" from "+V(RouteStart(route))+" to "+V(player.transform.position)+"\n");route++;SessionState.SetInt(PlayKey+"route",route);if(route<4){player.Teleport(RouteStart(route),Quaternion.identity);Physics.SyncTransforms();return;}
                var camera=Camera.main;if(!camera)throw new Exception("No gameplay camera");var head=player.transform.Find("Head");player.Teleport(new Vector3(0,1.081f,9.4f),Quaternion.identity);Physics.SyncTransforms();camera.transform.position=head.position;camera.transform.LookAt(new Vector3(0,1.7f,15));camera.fieldOfView=60;Render(camera,"output/CampusLibrary-Circulacao-Player-Passagem.png");player.Teleport(new Vector3(17.6f,1.081f,3.2f),Quaternion.identity);Physics.SyncTransforms();camera.transform.position=head.position;camera.transform.LookAt(new Vector3(17.6f,1.65f,11.5f));Render(camera,"output/CampusLibrary-Circulacao-Player-Banheiros.png");File.AppendAllText(Folder+"play-validation.txt","Three main opening lanes and straight sanitary corridor passed with CharacterController.Move. Two actual gameplay-camera captures at Head. Controlled movement/pose; manual input and Cinemachine follow not tested.\n");SessionState.SetString(PlayKey,"finished");EditorApplication.isPlaying=false;
            }
        }catch(Exception e){File.AppendAllText(Folder+"play-validation.txt",e+"\n");SessionState.SetString(PlayKey,"failed");EditorApplication.isPlaying=false;}
    }
}
