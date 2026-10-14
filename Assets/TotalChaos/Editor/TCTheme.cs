using UnityEngine;

namespace TotalChaosMaps.EditorTools
{
    /// <summary>
    /// Presets de tema no espírito de Fort Oasis: paleta suja e dessaturada, fog pesado,
    /// luz escassa e motivada. Ajuste aqui os números para afinar o clima de todos os mapas.
    /// </summary>
    public class TCTheme
    {
        public string id;
        public Color wall, floor, ceiling, lamp, fog, ambient;
        public float fogDensity;      // FogMode.ExponentialSquared
        public float lightIntensity;  // Point Light (URP)
        public float lightRange;
        public float flickerChance;   // 0..1 das lâmpadas que piscam
        public float saturation;      // Color Adjustments (-100..100)
        public float exposure;        // Post Exposure (EV)
        public float sun;             // intensidade da luz direcional (0 = sem sol; só ambientes abertos)

        static Color H(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        public static TCTheme Get(string id)
        {
            switch ((id ?? "").ToLowerInvariant())
            {
                case "tunnels":
                    return new TCTheme
                    {
                        id = "tunnels",
                        wall = H("#4b504e"), floor = H("#2d302f"), ceiling = H("#3a3e3c"),
                        lamp = H("#a8d0cf"), fog = H("#0d1211"), ambient = H("#070a0a"),
                        fogDensity = 0.075f, lightIntensity = 2.0f, lightRange = 8f,
                        flickerChance = 0.5f, saturation = -40f, exposure = -0.3f, sun = 0f
                    };
                case "streets":
                    return new TCTheme
                    {
                        id = "streets",
                        wall = H("#6a6d68"), floor = H("#3f413d"), ceiling = H("#555853"),
                        lamp = H("#d9c58f"), fog = H("#5d666b"), ambient = H("#363d42"),
                        fogDensity = 0.03f, lightIntensity = 2.6f, lightRange = 10f,
                        flickerChance = 0.15f, saturation = -35f, exposure = 0f, sun = 0.45f
                    };
                case "facility":
                    return new TCTheme
                    {
                        id = "facility",
                        wall = H("#55493d"), floor = H("#2c2824"), ceiling = H("#3a332c"),
                        lamp = H("#e8903f"), fog = H("#1d150f"), ambient = H("#0e0a07"),
                        fogDensity = 0.06f, lightIntensity = 2.6f, lightRange = 9f,
                        flickerChance = 0.35f, saturation = -25f, exposure = -0.2f, sun = 0f
                    };
                default: // apartments
                    return new TCTheme
                    {
                        id = "apartments",
                        wall = H("#5c5a50"), floor = H("#3b3731"), ceiling = H("#4a473f"),
                        lamp = H("#e0b56a"), fog = H("#171916"), ambient = H("#0f100e"),
                        fogDensity = 0.05f, lightIntensity = 2.4f, lightRange = 9f,
                        flickerChance = 0.3f, saturation = -30f, exposure = -0.2f, sun = 0f
                    };
            }
        }
    }
}
