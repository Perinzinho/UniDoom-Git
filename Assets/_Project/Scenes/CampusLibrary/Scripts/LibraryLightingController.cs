using UnityEngine;

namespace UniDoom.CampusLibrary
{
    [DisallowMultipleComponent]
    public sealed class LibraryLightingController : MonoBehaviour
    {
        [SerializeField] Light[] normalLights;
        [SerializeField] Light[] emergencyLights;
        [SerializeField] Renderer[] normalDiffusers;
        [SerializeField] Renderer[] emergencyDiffusers;
        [SerializeField, Range(.2f, 1)] float pulseFrequency = .55f;
        float[] intensity;
        bool[] normalEnabled;
        MaterialPropertyBlock[] originalDiffusers;
        MaterialPropertyBlock block;
        Color ambientSky, ambientEquator, ambientGround;
        bool powered, applied;
        public bool IsUnpowered => applied && !powered;
        void Awake()
        {
            block = new MaterialPropertyBlock();
            ambientSky = RenderSettings.ambientSkyColor; ambientEquator = RenderSettings.ambientEquatorColor; ambientGround = RenderSettings.ambientGroundColor;
            normalEnabled = new bool[normalLights.Length];
            for (int i = 0; i < normalLights.Length; i++) normalEnabled[i] = normalLights[i] && normalLights[i].enabled;
            intensity = new float[emergencyLights.Length];
            for (int i = 0; i < emergencyLights.Length; i++) intensity[i] = emergencyLights[i] ? emergencyLights[i].intensity : 0;
            originalDiffusers = new MaterialPropertyBlock[normalDiffusers.Length];
            for (int i = 0; i < normalDiffusers.Length; i++)
            {
                originalDiffusers[i] = new MaterialPropertyBlock();
                if (normalDiffusers[i]) normalDiffusers[i].GetPropertyBlock(originalDiffusers[i]);
            }
        }
        void OnEnable() { LibraryMissionProgress.Changed += Apply; }
        void Start()
        {
            if (normalLights.Length == 0 || emergencyLights.Length == 0) Debug.LogError("CampusLibrary: configure as luzes normais e de emergência.", this);
            Apply();
        }
        void Apply()
        {
            if (normalEnabled == null) return;
            bool next = LibraryMissionProgress.IsLibraryGeneratorPowered;
            if (applied && next == powered) return;
            powered = next; applied = true;
            for (int i = 0; i < normalLights.Length; i++) if (normalLights[i]) normalLights[i].enabled = powered && normalEnabled[i];
            foreach (var light in emergencyLights) if (light) light.enabled = !powered;
            for (int i = 0; i < normalDiffusers.Length; i++) if (normalDiffusers[i])
            {
                if (powered) normalDiffusers[i].SetPropertyBlock(originalDiffusers[i]);
                else
                {
                    normalDiffusers[i].GetPropertyBlock(block);
                    block.SetColor("_EmissionColor", Color.black); block.SetColor("_BaseColor", new Color(.12f, .13f, .12f)); normalDiffusers[i].SetPropertyBlock(block);
                }
            }
            RenderSettings.ambientSkyColor = powered ? ambientSky : new Color(.20f, .16f, .145f);
            RenderSettings.ambientEquatorColor = powered ? ambientEquator : new Color(.14f, .105f, .095f);
            RenderSettings.ambientGroundColor = powered ? ambientGround : new Color(.08f, .065f, .06f);
            if (powered) foreach (var diffuser in emergencyDiffusers) SetEmergency(diffuser, .025f);
        }
        void Update()
        {
            if (powered || Time.timeScale <= 0) return;
            for (int i = 0; i < emergencyLights.Length; i++)
            {
                // Slow staggered emergency pulse. The ambient floor never flickers.
                float pulse = Mathf.Lerp(.35f, 1, Mathf.SmoothStep(0, 1, (Mathf.Sin((Time.time * pulseFrequency + i * .13f) * Mathf.PI * 2) + 1) * .5f));
                if (emergencyLights[i]) emergencyLights[i].intensity = intensity[i] * pulse;
                if (i < emergencyDiffusers.Length) SetEmergency(emergencyDiffusers[i], pulse);
            }
        }
        void SetEmergency(Renderer renderer, float pulse)
        {
            if (!renderer) return;
            renderer.GetPropertyBlock(block); block.SetColor("_EmissionColor", new Color(1, .025f, .008f) * pulse);
            renderer.SetPropertyBlock(block);
        }
        void OnDisable() { LibraryMissionProgress.Changed -= Apply; }
    }
}
