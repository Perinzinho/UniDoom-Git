using UnityEngine;
using UnityEngine.UI;

namespace UniDoom.UI
{
    /// <summary>Static, batched wear and framing; no textures, shaders or per-frame allocations.</summary>
    public sealed class HorrorLoadingBackdrop : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var r = rectTransform.rect;
            var random = new System.Random(1979);
            for (int i = 0; i < 750; i++)
            {
                float x = r.xMin + (float)random.NextDouble() * r.width;
                float y = r.yMin + (float)random.NextDouble() * r.height;
                float width = i % 13 == 0 ? 24 + (float)random.NextDouble() * 110 : 1 + (float)random.NextDouble() * 3;
                Quad(mesh, new Rect(x, y, width, 1 + (float)random.NextDouble() * 2),
                    new Color(.42f, .34f, .21f, .035f + (float)random.NextDouble() * .055f));
            }
            Quad(mesh, new Rect(r.xMin, r.yMax - 54, r.width, 2), new Color(.32f,.14f,.07f,.7f));
            Quad(mesh, new Rect(r.xMin, r.yMin + 54, r.width, 2), new Color(.32f,.14f,.07f,.7f));
            for (int side = 0; side < 2; side++)
                for (int i = 0; i < 12; i++)
                    Quad(mesh, new Rect(side == 0 ? r.xMin + 42 : r.xMax - 48, r.center.y - 180 + i * 30, 6, 18),
                        new Color(.34f,.20f,.10f,.4f));
        }

        private static void Quad(VertexHelper mesh, Rect rect, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.up);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.one);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.right);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
