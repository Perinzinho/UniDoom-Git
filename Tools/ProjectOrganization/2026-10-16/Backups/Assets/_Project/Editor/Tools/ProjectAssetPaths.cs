using System.Collections.Generic;

// Canonical aliases for paths constructed by maintained Editor tools.
public static class ProjectAssetPaths
{
    static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
    {
        { "Assets/_Project/Scenes/Basement/ArenaPolish/RecoveredTextures/concrete_wall_006_Diffuse_ff744c67.png", "Assets/_Project/Global/Textures/Basement/ArenaPolish/RecoveredTextures/concrete_wall_006_Diffuse_ff744c67.png" },
        { "Assets/_Project/Scenes/Basement/ArenaPolish/RecoveredTextures/concrete_wall_006_nor_gl_05795be7.png", "Assets/_Project/Global/Textures/Basement/ArenaPolish/RecoveredTextures/concrete_wall_006_nor_gl_05795be7.png" },
        { "Assets/_Project/Scenes/Basement/ArenaPolish/RecoveredTextures/rusty_metal_02_Diffuse_8f8da05e.png", "Assets/_Project/Global/Textures/Basement/ArenaPolish/RecoveredTextures/rusty_metal_02_Diffuse_8f8da05e.png" },
        { "Assets/_Project/Scenes/Basement/ArenaPolish/RecoveredTextures/rusty_metal_02_Displacement_89f93e1e.png", "Assets/_Project/Global/Textures/Basement/ArenaPolish/RecoveredTextures/rusty_metal_02_Displacement_89f93e1e.png" },
        { "Assets/_Project/Scenes/Basement/ArenaPolish/RecoveredTextures/rusty_metal_02_nor_gl_125d9199.png", "Assets/_Project/Global/Textures/Basement/ArenaPolish/RecoveredTextures/rusty_metal_02_nor_gl_125d9199.png" },
        { "Assets/_Project/Scenes/Basement/ArenaPolish/RecoveredTextures/rusty_metal_02_UnityMask_4584ba83.png", "Assets/_Project/Global/Textures/Basement/ArenaPolish/RecoveredTextures/rusty_metal_02_UnityMask_4584ba83.png" },
        { "Assets/_Project/Scenes/Basement/ArenaPolish/RecoveredTextures/Textures_Masonry_Albedo_01f1621c.png", "Assets/_Project/Global/Textures/Basement/ArenaPolish/RecoveredTextures/Textures_Masonry_Albedo_01f1621c.png" },
        { "Assets/_Project/Scenes/Basement/ArenaPolish/RecoveredTextures/Textures_Metal_Albedo_863d30ef.png", "Assets/_Project/Global/Textures/Basement/ArenaPolish/RecoveredTextures/Textures_Metal_Albedo_863d30ef.png" },
        { "Assets/_Project/Scenes/Basement/ArenaPolish/RecoveredTextures/Textures_Rust_Albedo_2280c9f9.png", "Assets/_Project/Global/Textures/Basement/ArenaPolish/RecoveredTextures/Textures_Rust_Albedo_2280c9f9.png" },
        { "Assets/_Project/Scenes/Basement/Textures/Red.png", "Assets/_Project/Global/Textures/Basement/Textures/Red.png" },
        { "Assets/_Project/Scenes/ClassRoom/Surface/Textures/concrete_wall_006/Displacement.png", "Assets/_Project/Global/Textures/ClassRoom/Surface/Textures/concrete_wall_006/Displacement.png" },
        { "Assets/_Project/Scenes/ClassRoom/Surface/Textures/concrete_wall_006/UnityMask.png", "Assets/_Project/Global/Textures/ClassRoom/Surface/Textures/concrete_wall_006/UnityMask.png" },
        { "Assets/_Project/Scenes/ClassRoom/Textures/Chalkboard.png", "Assets/_Project/Global/Textures/ClassRoom/Textures/Chalkboard.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/Black.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/Black.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/BookBlue.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/BookBlue.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/BookRed.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/BookRed.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/Ceiling.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/Ceiling.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/DarkWood.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/DarkWood.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/Floor.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/Floor.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/Ivory.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/Ivory.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/LampGlow.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/LampGlow.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/Leaves.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/Leaves.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/Metal.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/Metal.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/Plaster.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/Plaster.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/Screen.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/Screen.png" },
        { "Assets/_Project/Scenes/DirectorRoom/Textures/Walnut.png", "Assets/_Project/Global/Textures/DirectorOffice/Textures/Walnut.png" },
        { "Assets/_Project/Scenes/EntranceGarden/Textures/DeadLeaves.png", "Assets/_Project/Global/Textures/EntranceGarden/Textures/DeadLeaves.png" },
        { "Assets/_Project/Scenes/EntranceGarden/Textures/Ivy.png", "Assets/_Project/Global/Textures/EntranceGarden/Textures/Ivy.png" },
    };
    public static string Resolve(string path) => Aliases.TryGetValue(path, out var actual) ? actual : path;
}
