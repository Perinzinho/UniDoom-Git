using UnityEngine;
using System.Collections.Generic;

namespace TotalChaosMaps
{
    /// <summary>
    /// Lâmpada defeituosa: oscila com ruído suave e, de vez em quando, "morre" por frações de segundo.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class TCFlickerLight : MonoBehaviour
    {
        [Range(0f, 1f)] public float flickerAmount = 0.45f;
        public float speed = 9f;
        [Tooltip("Chance (por segundo) de a luz apagar por um instante.")]
        [Range(0f, 1f)] public float dropoutChancePerSecond = 0.15f;
        public bool synchronizeEmission;

        Light _light;
        float _base;
        float _seed;
        float _deadUntil;
        Renderer[] _emitters;
        Color[] _emissionColors, _baseColors;
        MaterialPropertyBlock _properties;

        void Awake()
        {
            _light = GetComponent<Light>();
            _base = _light.intensity;
            _seed = Random.value * 100f;
            if (synchronizeEmission)
            {
                var emitters = new List<Renderer>(GetComponentsInChildren<Renderer>());
                if (transform.parent)
                    foreach (var renderer in transform.parent.GetComponentsInChildren<Renderer>())
                    {
                        var material = renderer.sharedMaterial;
                        if (!material || emitters.Contains(renderer)) continue;
                        bool luminous = material.IsKeywordEnabled("_EMISSION") ||
                            (material.HasProperty("_EmissionColor") && material.GetColor("_EmissionColor").maxColorComponent > 0);
                        if (luminous) emitters.Add(renderer);
                    }
                _emitters = emitters.ToArray();
                _emissionColors = new Color[_emitters.Length];
                _baseColors = new Color[_emitters.Length];
                _properties = new MaterialPropertyBlock();
                for (int i=0; i<_emitters.Length; i++)
                {
                    var material = _emitters[i].sharedMaterial;
                    if (!material) continue;
                    if (material.HasProperty("_EmissionColor")) _emissionColors[i] = material.GetColor("_EmissionColor");
                    if (material.HasProperty("_BaseColor")) _baseColors[i] = material.GetColor("_BaseColor");
                }
            }
        }

        void Update()
        {
            if (Time.time < _deadUntil)
            {
                _light.intensity = 0f;
                SyncEmission();
                return;
            }

            if (Random.value < dropoutChancePerSecond * Time.deltaTime)
            {
                _deadUntil = Time.time + Random.Range(0.05f, 0.35f);
                _light.intensity = 0f;
                SyncEmission();
                return;
            }

            float n = Mathf.PerlinNoise(_seed, Time.time * speed);
            _light.intensity = _base * Mathf.Lerp(1f - flickerAmount, 1f, n);
            SyncEmission();
        }
        void SyncEmission()
        {
            if (!synchronizeEmission || _emitters == null) return;
            float weight = _base > 0 ? _light.intensity / _base : 0;
            for (int i=0; i<_emitters.Length; i++)
            {
                if (!_emitters[i]) continue;
                _emitters[i].GetPropertyBlock(_properties);
                _properties.SetColor("_EmissionColor", _emissionColors[i] * weight);
                _properties.SetColor("_BaseColor", _baseColors[i] * Mathf.Lerp(.08f, 1, weight));
                _emitters[i].SetPropertyBlock(_properties);
            }
        }
    }
}
