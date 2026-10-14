using System.IO;
using UnityEditor;
using UnityEngine;

namespace TotalChaosMaps.EditorTools
{
    /// <summary>
    /// Cria texturas "encardidas" 100% procedurais (sem copiar nenhum asset de terceiros)
    /// e materiais URP/Standard que as usam. Materiais já existentes NÃO são sobrescritos,
    /// então você pode trocar a textura por uma sua e rebuildar o mapa sem perder a troca.
    /// </summary>
    public static class TCAssets
    {
        public const string Root = "Assets/TotalChaosMaps/Generated";

        // ---------- pastas ----------
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ---------- ruído tileável ----------
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        // value noise que "dá a volta" (tileável) com lattice fx x fy
        static float VN(float u, float v, int fx, int fy, int seed)
        {
            float x = u * fx, y = v * fy;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float tx = x - x0, ty = y - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            int xa = ((x0 % fx) + fx) % fx, xb = (xa + 1) % fx;
            int ya = ((y0 % fy) + fy) % fy, yb = (ya + 1) % fy;
            float a = Hash(xa, ya, seed), b = Hash(xb, ya, seed);
            float c = Hash(xa, yb, seed), d = Hash(xb, yb, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        static float Edge(float a, float b, float t)
        {
            t = Mathf.Clamp01((t - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <param name="streak">0..1 manchas verticais de umidade escorrendo</param>
        /// <param name="dirt">0..1 manchas largas de sujeira</param>
        /// <param name="seams">linhas de junta (painéis de concreto / azulejo)</param>
        public static Texture2D Grime(int size, int seed, float streak, float dirt, bool seams)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point, // pixel duro = textura "de jogo antigo"
                name = "TC_Grime"
            };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size, v = (float)y / size;
                    float n = 0.5f * VN(u, v, 4, 4, seed) + 0.3f * VN(u, v, 8, 8, seed + 1) + 0.2f * VN(u, v, 16, 16, seed + 2);
                    float stain = Edge(0.55f, 0.95f, VN(u, v, 20, 2, seed + 5)) * streak;
                    float blotch = 1f - VN(u, v, 5, 5, seed + 7);
                    float speck = (Hash(x, y, seed + 9) - 0.5f) * 0.14f;

                    float g = 0.62f + 0.5f * (n - 0.5f) - stain * 0.35f - dirt * blotch * 0.22f + speck;
                    if (seams && (x == 0 || y == 0 || y == size / 2)) g *= 0.55f;
                    byte b8 = (byte)(Mathf.Clamp01(g) * 255f);
                    px[y * size + x] = new Color32(b8, b8, b8, 255);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(true, false);
            return tex;
        }

        // ---------- materiais ----------
        static Shader LitShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (!s) s = Shader.Find("Standard");
            return s;
        }

        static void SetMatte(Material m)
        {
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.08f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.08f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        }

        /// <summary>Superfície suja com tint (cria uma vez; reaproveita depois).</summary>
        public static Material Surface(string name, Color tint, int seed, float streak, float dirt, bool seams)
        {
            EnsureFolder(Root + "/Materials");
            EnsureFolder(Root + "/Textures");
            string matPath = $"{Root}/Materials/TC_{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing) return existing;

            var tex = Grime(128, seed, streak, dirt, seams);
            AssetDatabase.CreateAsset(tex, $"{Root}/Textures/TC_{name}.asset");

            var m = new Material(LitShader()) { name = "TC_" + name };
            m.color = tint;
            m.mainTexture = tex;
            SetMatte(m);
            AssetDatabase.CreateAsset(m, matPath);
            return m;
        }

        /// <summary>Material emissivo para lâmpadas e itens brilhantes.</summary>
        public static Material Glow(string name, Color color, float intensity = 2.5f)
        {
            EnsureFolder(Root + "/Materials");
            string matPath = $"{Root}/Materials/TC_Glow_{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing) return existing;

            var m = new Material(LitShader()) { name = "TC_Glow_" + name };
            m.color = color;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            SetMatte(m);
            AssetDatabase.CreateAsset(m, matPath);
            return m;
        }

        public static void SaveMesh(Mesh mesh)
        {
            EnsureFolder(Root + "/Meshes");
            string p = $"{Root}/Meshes/{mesh.name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(p) != null) AssetDatabase.DeleteAsset(p);
            AssetDatabase.CreateAsset(mesh, p);
        }
    }
}
