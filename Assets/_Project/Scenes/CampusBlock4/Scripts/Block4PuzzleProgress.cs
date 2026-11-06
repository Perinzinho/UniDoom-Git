using UnityEngine;

namespace UniDoom.CampusBlock4
{
    // Run inventory: survives scene transitions and checkpoint reloads, never a new game.
    // Kept separate from PrisonProgress and Key01..03; no professor's cell is connected yet.
    public static class Block4PuzzleProgress
    {
        public const string TeacherCellKeyId = "block4.teacher-cell";
        public static bool DoorUnlocked { get; private set; }
        public static bool HasTeacherCellKey { get; private set; }
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
        }
    }
}
