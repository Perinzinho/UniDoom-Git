using System;
using UnityEngine;

public static class CellKeySprites
{
    static Sprite[] sprites;
    public static Sprite Get(int cellId)
    {
        if (cellId < 1 || cellId > 8) return null;
        if (sprites == null)
        {
            sprites = new Sprite[8];
            foreach (var sprite in Resources.LoadAll<Sprite>("Textures/Pickups/CellKeys"))
                if (sprite.name.StartsWith("CellKey", StringComparison.Ordinal) &&
                    int.TryParse(sprite.name.Substring(7), out int id) && id >= 1 && id <= 8)
                    sprites[id - 1] = sprite;
        }
        return sprites[cellId - 1];
    }
}
