using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TotalChaosMaps
{
    // Applies the skill to existing rooms without changing the scene's saved environment or player prefab.
    public class TCRegionAtmosphere : MonoBehaviour
    {
        public string theme = "apartments";
        public Bounds localBounds;
        public bool oval;
        public int priority = 10;
        public Color fogColor = new Color(.09f,.098f,.086f);
        public Color ambient = new Color(.059f,.063f,.055f);
        public float fogDensity = .05f;
        [Min(0), Tooltip("World-space transition outside the region; match the local Volume blend distance.")]
        public float blendDistance;
        [Tooltip("Optional floor reference for a passage whose geometry has scene overrides.")]
        public BoxCollider floorReference;
        [Min(.1f)] public float floorClearHeight = 5.2f;
        [Min(0)] public float flashlightIntensity = 3.5f;
        [Min(.1f)] public float flashlightRange = 16;
        [Range(1, 179)] public float flashlightAngle = 60;
        [Range(0, 179)] public float flashlightInnerAngle = 35;
        public LightShadows flashlightShadows = LightShadows.Hard;
        [Tooltip("Reserve shadow atlas space for this room by capping distant scene lights to the low tier while inside.")]
        public bool stabilizeBackgroundShadows;

        static readonly HashSet<TCRegionAtmosphere> regions = new HashSet<TCRegionAtmosphere>();
        static readonly Dictionary<Light,float> directionals = new Dictionary<Light,float>();
        static readonly Dictionary<UniversalAdditionalLightData,int> shadowTiers = new Dictionary<UniversalAdditionalLightData,int>();
        static TCRegionAtmosphere active;
        static bool hooked, captured, originalFog, lampRequested = true;
        static FogMode originalFogMode;
        static Color originalFogColor, originalAmbient, originalBackground;
        static float originalDensity;
        static AmbientMode originalAmbientMode;
        static SphericalHarmonicsL2 originalProbe;
        static Camera savedCamera;
        static CameraClearFlags originalClear;
        static bool originalPost;
        static Light guideLamp;
        Matrix4x4 floorMatrix, regionMatrix;
        Vector3 floorCenter, floorSize;
        float appliedHeight;
        bool floorBoundsInitialized;

        public void SyncFloorBounds()
        {
            if (!floorReference) return;
            var fm = floorReference.transform.localToWorldMatrix;
            var rm = transform.worldToLocalMatrix;
            if (floorBoundsInitialized && fm == floorMatrix && rm == regionMatrix &&
                floorCenter == floorReference.center && floorSize == floorReference.size && appliedHeight == floorClearHeight) return;
            floorMatrix = fm; regionMatrix = rm; floorCenter = floorReference.center; floorSize = floorReference.size;
            appliedHeight = floorClearHeight; floorBoundsInitialized = true;
            var matrix = rm * fm;
            var bounds = new Bounds(matrix.MultiplyPoint3x4(floorCenter), Vector3.zero);
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        bounds.Encapsulate(matrix.MultiplyPoint3x4(floorCenter + Vector3.Scale(floorSize * .5f, new Vector3(x,y,z))));
            localBounds = new Bounds(new Vector3(bounds.center.x, bounds.max.y + floorClearHeight * .5f, bounds.center.z),
                new Vector3(bounds.size.x, floorClearHeight, bounds.size.z));
            var trigger = GetComponent<BoxCollider>();
            if (trigger && trigger.isTrigger) { trigger.center = localBounds.center; trigger.size = localBounds.size; }
        }

        void OnEnable()
        {
            SyncFloorBounds();
            if(!Application.isPlaying)return;
            regions.Add(this);
            if(!hooked){RenderPipelineManager.beginCameraRendering += BeforeCamera;hooked=true;}
        }
        void OnDisable()
        {
            regions.Remove(this);
            if(active==this)RestoreEnvironment();
            if(regions.Count==0&&hooked){RenderPipelineManager.beginCameraRendering-=BeforeCamera;hooked=false;RestoreEnvironment();}
        }
        void Update()
        {
            if(Application.isPlaying&&active==this&&Input.GetKeyDown(KeyCode.F))lampRequested=!lampRequested;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset()
        {
            if(hooked)RenderPipelineManager.beginCameraRendering-=BeforeCamera;
            RestoreEnvironment();regions.Clear();hooked=false;lampRequested=true;
            if(guideLamp)Object.Destroy(guideLamp.gameObject);guideLamp=null;
        }
        static void BeforeCamera(ScriptableRenderContext context,Camera camera)
        {
            if(!Application.isPlaying||camera!=Camera.main)return;
            ApplyForCamera(camera,regions,true);
        }
        public bool ContainsWorld(Vector3 position)
        {
            var p=transform.InverseTransformPoint(position);
            if(!localBounds.Contains(p))return false;
            if(!oval)return true;
            var q=p-localBounds.center;var e=localBounds.extents;
            return q.x*q.x/(e.x*e.x)+q.z*q.z/(e.z*e.z)<=1;
        }
        public float WeightAt(Vector3 position)
        {
            if (ContainsWorld(position)) return 1;
            if (blendDistance <= 0) return 0;
            var p = transform.InverseTransformPoint(position);
            var closest = localBounds.ClosestPoint(p);
            if (oval)
            {
                var q = p - localBounds.center;
                var e = localBounds.extents;
                float radius = Mathf.Sqrt(q.x*q.x/(e.x*e.x) + q.z*q.z/(e.z*e.z));
                if (radius > 1)
                {
                    closest.x = localBounds.center.x + q.x/radius;
                    closest.z = localBounds.center.z + q.z/radius;
                }
            }
            float distanceSquared = (position - transform.TransformPoint(closest)).sqrMagnitude;
            // Same spatial falloff as a URP local Volume, independent of frame rate.
            return Mathf.Clamp01(1 - distanceSquared/(blendDistance*blendDistance));
        }
        public static TCRegionAtmosphere ApplyForCamera(Camera camera,IEnumerable<TCRegionAtmosphere> choices,bool withGuideLamp)
        {
            TCRegionAtmosphere selected = null;
            float weight = 0;
            bool inside = false;
            foreach (var region in choices)
            {
                if (!region || !region.enabled || !region.gameObject.activeInHierarchy) continue;
                region.SyncFloorBounds();
                float candidateWeight = region.WeightAt(camera.transform.position);
                if (candidateWeight <= 0) continue;
                bool candidateInside = region.ContainsWorld(camera.transform.position);
                bool preferred = !selected || (candidateInside && !inside);
                if (selected && candidateInside == inside)
                {
                    preferred = region.priority > selected.priority ||
                        (region.priority == selected.priority &&
                        (region.localBounds.size.sqrMagnitude < selected.localBounds.size.sqrMagnitude ||
                        (region.localBounds.size.sqrMagnitude == selected.localBounds.size.sqrMagnitude && region.GetEntityId().GetHashCode() < selected.GetEntityId().GetHashCode())));
                }
                if (preferred) { selected = region; weight = candidateWeight; inside = candidateInside; }
            }
            if(!selected){RestoreEnvironment();return null;}
            if(savedCamera&&savedCamera!=camera)RestoreEnvironment();
            if(!captured)
            {
                originalFog=RenderSettings.fog;originalFogMode=RenderSettings.fogMode;originalFogColor=RenderSettings.fogColor;
                originalDensity=RenderSettings.fogDensity;originalAmbient=RenderSettings.ambientLight;
                originalAmbientMode=RenderSettings.ambientMode;originalProbe=RenderSettings.ambientProbe;
                directionals.Clear();foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                {
                    if(light.type==LightType.Directional)directionals[light]=light.intensity;
                    else if(Application.isPlaying && light.shadows!=LightShadows.None)
                    {
                        var data=light.GetUniversalAdditionalLightData();shadowTiers[data]=data.additionalLightsShadowResolutionTier;
                    }
                }
                savedCamera=camera;originalClear=camera.clearFlags;originalBackground=camera.backgroundColor;
                originalPost=camera.GetUniversalAdditionalCameraData().renderPostProcessing;captured=true;
            }
            active=selected;
            if(Application.isPlaying)
                foreach(var pair in shadowTiers)
                    if(pair.Key)pair.Key.additionalLightsShadowResolutionTier=selected.stabilizeBackgroundShadows &&
                        !pair.Key.transform.IsChildOf(selected.transform.parent) ? Mathf.Min(pair.Value,0) : pair.Value;
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=Color.Lerp(originalFogColor,selected.fogColor,weight);
            RenderSettings.fogDensity=Mathf.Lerp(originalFog ? originalDensity : 0,selected.fogDensity,weight);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.Lerp(originalAmbient,selected.ambient,weight);
            var targetProbe=new SphericalHarmonicsL2();targetProbe.AddAmbientLight(selected.ambient);
            var probe=new SphericalHarmonicsL2();
            for(int rgb=0;rgb<3;rgb++)for(int coefficient=0;coefficient<9;coefficient++)
                probe[rgb,coefficient]=Mathf.Lerp(originalProbe[rgb,coefficient],targetProbe[rgb,coefficient],weight);
            RenderSettings.ambientProbe=probe;
            foreach(var pair in directionals)if(pair.Key)pair.Key.intensity=pair.Value*(1-weight);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.Lerp(originalBackground,selected.fogColor,weight);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            if(withGuideLamp)
            {
                if(!guideLamp||guideLamp.transform.parent!=camera.transform)
                {
                    if(guideLamp)
                    {
                        guideLamp.enabled=false;
                        if(Application.isPlaying)Object.Destroy(guideLamp.gameObject);else Object.DestroyImmediate(guideLamp.gameObject);
                    }
                    var go=new GameObject("TC camera flashlight - F");go.transform.SetParent(camera.transform,false);
                    go.transform.localPosition=new Vector3(.12f,-.10f,.05f);
                    guideLamp=go.AddComponent<Light>();guideLamp.type=LightType.Spot;
                    guideLamp.color=new Color(1,.95f,.82f);
                    guideLamp.shadowBias=.06f;guideLamp.shadowNormalBias=.15f;
                }
                guideLamp.intensity=selected.flashlightIntensity*weight;
                guideLamp.range=selected.flashlightRange;
                guideLamp.spotAngle=selected.flashlightAngle;
                guideLamp.innerSpotAngle=Mathf.Min(selected.flashlightInnerAngle,selected.flashlightAngle);
                guideLamp.shadows=selected.flashlightShadows;
                guideLamp.GetUniversalAdditionalLightData().usePipelineSettings=false;
                if(Application.isPlaying)guideLamp.GetUniversalAdditionalLightData().additionalLightsShadowResolutionTier=2;
                guideLamp.enabled=lampRequested;
            }
            else if(guideLamp)guideLamp.enabled=false;
            return selected;
        }
        public static void RestoreEnvironment()
        {
            if(guideLamp)guideLamp.enabled=false;
            if(captured)
            {
                RenderSettings.fog=originalFog;RenderSettings.fogMode=originalFogMode;RenderSettings.fogColor=originalFogColor;
                RenderSettings.fogDensity=originalDensity;RenderSettings.ambientMode=originalAmbientMode;
                RenderSettings.ambientLight=originalAmbient;RenderSettings.ambientProbe=originalProbe;
                foreach(var pair in directionals)if(pair.Key)pair.Key.intensity=pair.Value;
                if(Application.isPlaying)foreach(var pair in shadowTiers)if(pair.Key)pair.Key.additionalLightsShadowResolutionTier=pair.Value;
                if(savedCamera){savedCamera.clearFlags=originalClear;savedCamera.backgroundColor=originalBackground;savedCamera.GetUniversalAdditionalCameraData().renderPostProcessing=originalPost;}
            }
            active=null;captured=false;savedCamera=null;directionals.Clear();shadowTiers.Clear();
        }
    }
}
