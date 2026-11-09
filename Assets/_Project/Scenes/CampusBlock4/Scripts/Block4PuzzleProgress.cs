using UnityEngine;

namespace UniDoom.CampusBlock4
{
    // Run inventory: survives scene transitions and checkpoint reloads, never a new game.
    // The existing puzzle key also grants cell 07 in PlayerCellKeys; Key01..03 stay separate.
    public static class Block4PuzzleProgress
    {
        public const string TeacherCellKeyId = "professor-cell-07";
        public static bool DoorUnlocked { get; private set; }
        public static bool HasTeacherCellKey { get; private set; }
        internal static int[] MonitorDestinations;
        internal static float NextClueShiftAt;
        internal static void UnlockDoor() => DoorUnlocked = true;
        internal static bool CollectTeacherCellKey()
        {
            if (!DoorUnlocked || HasTeacherCellKey) return false;
            HasTeacherCellKey = true;
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetSession()
        {
            DoorUnlocked = false;
            HasTeacherCellKey = false;
            MonitorDestinations = null;
            NextClueShiftAt = 0;
        }
    }
}
