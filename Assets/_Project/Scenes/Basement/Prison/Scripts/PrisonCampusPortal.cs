using UnityEngine;

namespace UniDoom.Utils
{
    /// <summary>Animated, texture-free portal using the prison's existing transition volume.</summary>
    public sealed class PrisonCampusPortal : MonoBehaviour
    {
        readonly LineRenderer[] rings = new LineRenderer[3];
        Material material;
        const int Segments = 72;

        void Start()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) { enabled = false; return; }
            material = new Material(shader);
            material.SetColor("_BaseColor", new Color(.18f, .72f, .78f));
            var box = GetComponent<BoxCollider>();
            Vector3 center = box ? box.center : Vector3.zero;
            Vector3 size = box ? box.size : new Vector3(2.7f, 3.1f, 1);
            for (int i = 0; i < rings.Length; i++)
            {
                var go = new GameObject("Portal ring " + i);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = center;
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = material;
                line.useWorldSpace = false;
                line.loop = true;
                line.positionCount = Segments;
                line.widthMultiplier = .035f;
                for (int k = 0; k < Segments; k++)
                {
                    float a = k * Mathf.PI * 2 / Segments;
                    float r = .43f - i * .055f;
                    line.SetPosition(k, new Vector3(Mathf.Cos(a) * size.x * r, Mathf.Sin(a) * size.y * r, i * .04f));
                }
                rings[i] = line;
            }
        }
        void Update()
        {
            for (int i = 0; i < rings.Length; i++)
                if (rings[i]) rings[i].transform.localRotation = Quaternion.Euler(0, 0, Time.time * (i % 2 == 0 ? 14 : -11));
        }
        void OnDestroy() { if (material) Destroy(material); }
    }
}
