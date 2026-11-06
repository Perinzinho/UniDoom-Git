using System;
using UnityEngine;

public enum FootstepSurfaceType { Concrete, Wood, Interior, Asphalt, Grass }

/// <summary>Optional explicit override for floors; existing maps also resolve without new components.</summary>
[DisallowMultipleComponent]
public sealed class FootstepSurface : MonoBehaviour
{
    public FootstepSurfaceType surface = FootstepSurfaceType.Concrete;

    public static FootstepSurfaceType Resolve(Collider floor)
    {
        var explicitSurface = floor.GetComponentInParent<FootstepSurface>();
        if (explicitSurface != null) return explicitSurface.surface;
        // Collision-only supports have no renderer. These are actual floor names in the
        // saved campus/garden maps, not the names of their decorative grass meshes.
        string name = floor.name;
        if (name == "Campus continuous ground" || name == "Broad garden end" || name == "Narrow garden tip")
            return FootstepSurfaceType.Grass;
        if (floor.gameObject.scene.name == "LawSchool")
        {
            if (Contains(name, "floor collision")) return FootstepSurfaceType.Interior;
            if (name == "Low stage collision" || name == "Stage step collision") return FootstepSurfaceType.Wood;
        }
        if (TryClassify(name, out var named)) return named;
        // Preserve the original map's authored surface tags, without requiring new tags.
        switch (floor.tag)
        {
            case "Wood": return FootstepSurfaceType.Wood;
            case "Concrete": return FootstepSurfaceType.Concrete;
            case "Eco": return FootstepSurfaceType.Interior;
        }
        // Only inspect the collider's own renderer. A parent's decorative materials do
        // not describe the floor; multi-material meshes need an explicit override.
        var renderer = floor.GetComponent<Renderer>();
        if (renderer != null)
        {
            var materials = renderer.sharedMaterials;
            if (materials.Length == 1 && materials[0] != null && TryClassify(materials[0].name, out var material))
                return material;
        }
        return FootstepSurfaceType.Concrete;
    }

    static bool Contains(string text, string word) => text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0;

    public static bool TryClassify(string name, out FootstepSurfaceType surface)
    {
        surface = FootstepSurfaceType.Concrete;
        if (string.IsNullOrEmpty(name)) return false;
        if (Contains(name, "asphalt") || name.StartsWith("RoadCell_", StringComparison.Ordinal))
            surface = FootstepSurfaceType.Asphalt;
        else if (Contains(name, "grass") || Contains(name, "lawn"))
            surface = FootstepSurfaceType.Grass;
        else if (Contains(name, "wood") || Contains(name, "parquet") || Contains(name, "walnut"))
            surface = FootstepSurfaceType.Wood;
        else if (Contains(name, "granite") || Contains(name, "tile") || Contains(name, "marble") ||
                 Contains(name, "terrazzo") || Contains(name, "vinyl") || Contains(name, "ceramic"))
            surface = FootstepSurfaceType.Interior;
        else if (!(Contains(name, "concrete") || Contains(name, "paving") || Contains(name, "walkway") || Contains(name, "gutter")))
            return false;
        return true;
    }
}
