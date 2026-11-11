using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>Stable indirect illumination for the law building, including persistent-camera scene changes.</summary>
[ExecuteAlways,DisallowMultipleComponent]
public sealed class LawSchoolLighting : MonoBehaviour
{
    [SerializeField] Color indirectLight=new Color(.24f,.23f,.20f);
    bool refresh;
    bool alarm, blackout;
    float previousReflection;
    readonly Dictionary<Camera,float> previousClip=new Dictionary<Camera,float>();
    void OnEnable(){refresh=true;RenderPipelineManager.beginCameraRendering+=Apply;}
    void OnDisable(){RenderPipelineManager.beginCameraRendering-=Apply;foreach(var pair in previousClip)if(pair.Key&&Mathf.Approximately(pair.Key.nearClipPlane,.06f))pair.Key.nearClipPlane=pair.Value;previousClip.Clear();}
    public void SetAlarmPhase(bool active,bool dark)
    {
        if(active&&!alarm)previousReflection=RenderSettings.reflectionIntensity;
        if(!active&&alarm)RenderSettings.reflectionIntensity=previousReflection;
        alarm=active;blackout=dark;refresh=true;
    }
    void Apply(ScriptableRenderContext context,Camera camera)
    {
        if(SceneManager.GetActiveScene()!=gameObject.scene)return;
        // The default .3 m near plane can cross a wall before the player's .237 m capsule touches it.
        if(camera.cameraType==CameraType.Game&&camera.nearClipPlane>.08f){if(!previousClip.ContainsKey(camera))previousClip.Add(camera,camera.nearClipPlane);camera.nearClipPlane=.06f;}
        Color ambient=alarm?(blackout?Color.black:new Color(.22f,.006f,.003f)):indirectLight;
        if(!refresh&&RenderSettings.ambientMode==AmbientMode.Flat&&RenderSettings.ambientLight==ambient&&!RenderSettings.fog)return;
        if(alarm)RenderSettings.reflectionIntensity=0;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=ambient;RenderSettings.ambientIntensity=1;var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(ambient);RenderSettings.ambientProbe=probe;RenderSettings.fog=false;refresh=false;
    }
}
