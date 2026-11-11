using System;
using UnityEngine;

public static class CellKeySprites
{
    static Sprite[] sprites;
    public static Sprite Get(int cellId)
    {
        if (cellId < 1 || cellId > PrisonProgress.CellCount) return null;
        if (sprites == null)
        {
            sprites = new Sprite[PrisonProgress.CellCount];
            Load("Textures/Pickups/CellKeys");
            Load("Textures/Pickups/CellKeys09-13");
        }
        return sprites[cellId - 1];
    }

    static void Load(string path)
    {
        foreach (var sprite in Resources.LoadAll<Sprite>(path))
            if (sprite.name.StartsWith("CellKey", StringComparison.Ordinal) &&
                int.TryParse(sprite.name.Substring(7), out int id) &&
                id >= 1 && id <= PrisonProgress.CellCount)
                sprites[id - 1] = sprite;
    }
}
