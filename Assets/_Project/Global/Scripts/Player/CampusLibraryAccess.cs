/// <summary>The two campus keys unlock the library; they never unlock prison cells.</summary>
public static class CampusLibraryAccess
{
    public const string LawSchoolKeyId = "campus-library-law-school";
    public const string Block4KeyId = "campus-library-block-4";

    public static int KeyCount(PlayerCellKeys inventory) => inventory == null ? 0 :
        (inventory.HasKey(LawSchoolKeyId) ? 1 : 0) + (inventory.HasKey(Block4KeyId) ? 1 : 0);
    public static bool CanEnter(PlayerCellKeys inventory) => KeyCount(inventory) == 2;
}
