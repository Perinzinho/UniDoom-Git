using UnityEngine;

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

        Light _light;
        float _base;
        float _seed;
        float _deadUntil;

        void Awake()
        {
            _light = GetComponent<Light>();
            _base = _light.intensity;
            _seed = Random.value * 100f;
        }

        void Update()
        {
            if (Time.time < _deadUntil)
            {
                _light.intensity = 0f;
                return;
            }

            if (Random.value < dropoutChancePerSecond * Time.deltaTime)
            {
                _deadUntil = Time.time + Random.Range(0.05f, 0.35f);
                _light.intensity = 0f;
                return;
            }

            float n = Mathf.PerlinNoise(_seed, Time.time * speed);
            _light.intensity = _base * Mathf.Lerp(1f - flickerAmount, 1f, n);
        }
    }
}
