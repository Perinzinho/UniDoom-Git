using System.Collections.Generic;
using UnityEngine;

public static class PixelFontRenderer
{
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;
    private const int GlyphSpacing = 2;

    private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
    {
        { '0', new[] { "01110", "10001", "10011", "10101", "11001", "10001", "01110" } },
        { '1', new[] { "00100", "01100", "00100", "00100", "00100", "00100", "01110" } },
        { '2', new[] { "01110", "10001", "00001", "00010", "00100", "01000", "11111" } },
        { '3', new[] { "11111", "00010", "00100", "00010", "00001", "10001", "01110" } },
        { '4', new[] { "00010", "00110", "01010", "10010", "11111", "00010", "00010" } },
        { '5', new[] { "11111", "10000", "11110", "00001", "00001", "10001", "01110" } },
        { '6', new[] { "00110", "01000", "10000", "11110", "10001", "10001", "01110" } },
        { '7', new[] { "11111", "00001", "00010", "00100", "01000", "01000", "01000" } },
        { '8', new[] { "01110", "10001", "10001", "01110", "10001", "10001", "01110" } },
        { '9', new[] { "01110", "10001", "10001", "01111", "00001", "00010", "01100" } },
    };

    public static Sprite Render(string text, int scale = 4)
    {
        if (string.IsNullOrEmpty(text))
            text = "0";

        int unitW = text.Length * GlyphWidth + Mathf.Max(0, text.Length - 1) * GlyphSpacing;
        int texW = unitW * scale;
        int texH = GlyphHeight * scale;

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[texW * texH];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        int cursor = 0;
        for (int g = 0; g < text.Length; g++)
        {
            if (!Glyphs.TryGetValue(text[g], out string[] rows))
            {
                cursor += GlyphWidth + GlyphSpacing;
                continue;
            }

            for (int r = 0; r < GlyphHeight; r++)
            {
                for (int col = 0; col < GlyphWidth; col++)
                {
                    if (rows[r][col] != '1') continue;

                    int x0 = (cursor + col) * scale;
                    int y0 = texH - (r + 1) * scale;
                    for (int py = 0; py < scale; py++)
                    {
                        for (int px = 0; px < scale; px++)
                            pixels[(y0 + py) * texW + x0 + px] = Color.white;
                    }
                }
            }

            cursor += GlyphWidth + GlyphSpacing;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0f, 0f), 1f);
    }
}