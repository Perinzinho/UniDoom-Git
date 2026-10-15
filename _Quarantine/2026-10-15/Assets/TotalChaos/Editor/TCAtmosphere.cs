#define TC_URP // Comente ou apague esta linha se o projeto NÃO usa URP (pipeline Built-in): o pós-processamento será pulado.
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if TC_URP
using UnityEngine.Rendering.Universal;
#endif

namespace TotalChaosMaps.EditorTools
{
    /// <summary>
    /// Atmosfera "Fort Oasis": fog denso, ambient quase preto, câmera com cor de fundo = fog
    /// e um Volume global com granulado, vinheta, dessaturação e bloom discreto.
    /// </summary>
    public static class TCAtmosphere
    {
        public static void Apply(TCTheme t)
        {
            // --- fog + ambient ---
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = t.fog;
            RenderSettings.fogDensity = t.fogDensity;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = t.ambient;
            RenderSettings.skybox = null;

            // --- câmera ---
            var cam = Camera.main;
            if (cam != null)
            {
                Undo.RecordObject(cam, "TC camera");
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = t.fog;
#if TC_URP
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
#endif
            }

            // --- luz direcional: sem sol em ambientes fechados; sol frio e fraco nas ruas ---
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional || l.gameObject.name == "TC_Sun") continue;
                Undo.RecordObject(l, "TC dim directional");
                l.intensity = 0f;
            }
            var sunGo = GameObject.Find("TC_Sun");
            if (t.sun > 0f)
            {
                if (sunGo == null)
                {
                    sunGo = new GameObject("TC_Sun");
                    sunGo.AddComponent<Light>();
                    Undo.RegisterCreatedObjectUndo(sunGo, "TC sun");
                }
                sunGo.SetActive(true);
                var sun = sunGo.GetComponent<Light>();
                sun.type = LightType.Directional;
                sun.color = new Color(0.70f, 0.75f, 0.78f);
                sun.intensity = t.sun;
                sun.shadows = LightShadows.Soft;
                sunGo.transform.rotation = Quaternion.Euler(52f, -30f, 0f);
                RenderSettings.sun = sun;
            }
            else if (sunGo != null)
            {
                sunGo.SetActive(false);
            }

            ApplyPostProcessing(t);
            Debug.Log($"[TC] Atmosfera '{t.id}' aplicada (fog {t.fogDensity}, sat {t.saturation}).");
        }

        static void ApplyPostProcessing(TCTheme t)
        {
#if TC_URP
            TCAssets.EnsureFolder(TCAssets.Root + "/Profiles");
            string path = $"{TCAssets.Root}/Profiles/TC_PostFX_{t.id}.asset";
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null) AssetDatabase.DeleteAsset(path);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "TC_PostFX_" + t.id;

            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.42f);
            vig.smoothness.Override(0.6f);
            vig.color.Override(Color.black);

            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin2);
            grain.intensity.Override(0.55f);
            grain.response.Override(0.8f);

            var adj = profile.Add<ColorAdjustments>(true);
            adj.saturation.Override(t.saturation);
            adj.contrast.Override(18f);
            adj.postExposure.Override(t.exposure);

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.25f);
            bloom.scatter.Override(0.7f);

            var ca = profile.Add<ChromaticAberration>(true);
            ca.intensity.Override(0.08f);

            AssetDatabase.CreateAsset(profile, path);
            foreach (var comp in profile.components) AssetDatabase.AddObjectToAsset(comp, profile);
            AssetDatabase.SaveAssets();

            var go = GameObject.Find("TC_PostFX");
            if (go == null)
            {
                go = new GameObject("TC_PostFX");
                Undo.RegisterCreatedObjectUndo(go, "TC PostFX");
            }
            var vol = go.GetComponent<Volume>();
            if (!vol) vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 10;
            vol.sharedProfile = profile;
#else
            Debug.LogWarning("[TC] TC_URP desligado: pós-processamento pulado. Em Built-in use o Post Processing Stack v2 com valores equivalentes (veja references/unity-implementation.md).");
#endif
        }

        [MenuItem("Tools/Total Chaos Maps/Atmosphere/Apartments")]
        static void ApplyApartments() => Apply(TCTheme.Get("apartments"));

        [MenuItem("Tools/Total Chaos Maps/Atmosphere/Tunnels")]
        static void ApplyTunnels() => Apply(TCTheme.Get("tunnels"));

        [MenuItem("Tools/Total Chaos Maps/Atmosphere/Streets")]
        static void ApplyStreets() => Apply(TCTheme.Get("streets"));

        [MenuItem("Tools/Total Chaos Maps/Atmosphere/Facility")]
        static void ApplyFacility() => Apply(TCTheme.Get("facility"));
    }
}
