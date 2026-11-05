using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Keeps the outdoor fill light consistent when the persistent camera leaves the basement.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class CampusExteriorEnvironment : MonoBehaviour
{
    [SerializeField] Color ambient = new Color(.32f,.39f,.46f);
    [SerializeField] Color haze = new Color(.17f,.22f,.29f);
    bool refreshProbe;
    void OnEnable()
    {
        refreshProbe = true;
        RenderPipelineManager.beginCameraRendering += Apply;
    }
    void OnDisable() => RenderPipelineManager.beginCameraRendering -= Apply;
    void Apply(ScriptableRenderContext context, Camera camera)
    {
        if (SceneManager.GetActiveScene() != gameObject.scene) return;
        // Scene settings normally persist between cameras. Rebuild the ambient probe only
        // after activation or an actual lighting change, including edits in the Inspector.
        Color skyAmbient = ambient * .72f;
        Color equatorAmbient = ambient * .34f;
        Color groundAmbient = ambient * .12f;
        skyAmbient.a = equatorAmbient.a = groundAmbient.a = 1;
        bool ambientChanged = RenderSettings.ambientMode != AmbientMode.Trilight ||
            RenderSettings.ambientSkyColor != skyAmbient || RenderSettings.ambientEquatorColor != equatorAmbient || RenderSettings.ambientGroundColor != groundAmbient;
        if (!refreshProbe && !ambientChanged && RenderSettings.fog &&
            RenderSettings.fogMode == FogMode.ExponentialSquared &&
            RenderSettings.fogColor == haze && Mathf.Approximately(RenderSettings.fogDensity, .0015f)) return;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = skyAmbient;
        RenderSettings.ambientEquatorColor = equatorAmbient;
        RenderSettings.ambientGroundColor = groundAmbient;
        if (refreshProbe || ambientChanged)
        {
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(groundAmbient);
            probe.AddDirectionalLight(Vector3.up, skyAmbient, .8f);
            probe.AddDirectionalLight(Vector3.forward, equatorAmbient, .35f);
            probe.AddDirectionalLight(Vector3.back, equatorAmbient, .35f);
            probe.AddDirectionalLight(Vector3.left, equatorAmbient, .35f);
            probe.AddDirectionalLight(Vector3.right, equatorAmbient, .35f);
            RenderSettings.ambientProbe = probe;
            refreshProbe = false;
        }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = haze;
        RenderSettings.fogDensity = .0015f;
    }
}
