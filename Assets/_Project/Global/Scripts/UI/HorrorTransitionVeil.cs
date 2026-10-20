using UnityEngine;
using UnityEngine.UI;

namespace UniDoom.UI
{
    /// <summary>Soft peripheral darkness and a legible floor for destination text, in one static UI mesh.</summary>
    public sealed class HorrorTransitionVeil : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            const int rings = 12, segments = 64;
            for (int ring = 0; ring <= rings; ring++)
            {
                float radius = ring * 1.5f / rings;
                float opacity = Mathf.SmoothStep(0f, .85f, Mathf.InverseLerp(.35f, 1.4f, radius));
                for (int segment = 0; segment <= segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    mesh.AddVert(rect.center + new Vector2(Mathf.Cos(angle) * rect.width * .5f,
                        Mathf.Sin(angle) * rect.height * .5f) * radius,
                        new Color(0f, 0f, 0f, opacity), Vector2.zero);
                    if (ring == 0 || segment == 0) continue;
                    int current = ring * (segments + 1) + segment;
                    int previous = current - segments - 1;
                    mesh.AddTriangle(previous - 1, current - 1, current);
                    mesh.AddTriangle(previous - 1, current, previous);
                }
            }

            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector2(rect.xMin, rect.yMin), new Color(0, 0, 0, .8f), Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMin, rect.yMin + rect.height * .38f), Color.clear, Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax, rect.yMin + rect.height * .38f), Color.clear, Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax, rect.yMin), new Color(0, 0, 0, .8f), Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
