using UnityEngine;

namespace UniDoom.CampusBlock4
{
    // Run inventory: survives scene transitions and checkpoint reloads, never a new game.
    // The puzzle reward grants only the Block 4 library access key.
    public static class Block4PuzzleProgress
    {
        public const string LibraryAccessKeyId = CampusLibraryAccess.Block4KeyId;
        public static bool DoorUnlocked { get; private set; }
        public static bool HasLibraryAccessKey { get; private set; }
        internal static int[] MonitorDestinations;
        internal static float NextClueShiftAt;
        internal static void UnlockDoor() => DoorUnlocked = true;
        internal static bool CollectLibraryAccessKey()
        {
            if (!DoorUnlocked || HasLibraryAccessKey) return false;
            HasLibraryAccessKey = true;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetSession()
        {
            DoorUnlocked = false;
            HasLibraryAccessKey = false;
            MonitorDestinations = null;
            NextClueShiftAt = 0;
        }
    }
}
