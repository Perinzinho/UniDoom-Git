using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Stable indirect illumination for the law building, including persistent-camera scene changes.</summary>
[ExecuteAlways,DisallowMultipleComponent]
public sealed class LawSchoolLighting : MonoBehaviour
{
    [SerializeField] Color indirectLight=new Color(.24f,.23f,.20f);
    bool refresh;
    void OnEnable(){refresh=true;RenderPipelineManager.beginCameraRendering+=Apply;}
    void OnDisable()=>RenderPipelineManager.beginCameraRendering-=Apply;
    void Apply(ScriptableRenderContext context,Camera camera)
    {
        if(SceneManager.GetActiveScene()!=gameObject.scene)return;
        if(!refresh&&RenderSettings.ambientMode==AmbientMode.Flat&&RenderSettings.ambientLight==indirectLight&&!RenderSettings.fog)return;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=indirectLight;RenderSettings.ambientIntensity=1;var probe=new SphericalHarmonicsL2();probe.AddAmbientLight(indirectLight);RenderSettings.ambientProbe=probe;RenderSettings.fog=false;refresh=false;
    }
}
