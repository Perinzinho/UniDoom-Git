using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Keeps the outdoor fill light consistent when the persistent camera leaves the basement.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class CampusExteriorEnvironment : MonoBehaviour
{
    [SerializeField] Color ambient = new Color(.32f,.39f,.46f);
    [SerializeField] Color haze = new Color(.17f,.22f,.29f);
    void OnEnable() => RenderPipelineManager.beginCameraRendering += Apply;
    void OnDisable() => RenderPipelineManager.beginCameraRendering -= Apply;
    void Apply(ScriptableRenderContext context, Camera camera)
    {
        if (SceneManager.GetActiveScene() != gameObject.scene) return;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        var probe = new SphericalHarmonicsL2();
        probe.AddAmbientLight(ambient);
        RenderSettings.ambientProbe = probe;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = haze;
        RenderSettings.fogDensity = .0015f;
    }
}
