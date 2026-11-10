using System;
using UnityEngine;

namespace UniDoom.CampusLibrary
{
    public enum LibraryMissionStage { NotDiscovered, SearchingFuses, FusesCollected, FusesInstalled, PuzzleSolved, GeneratorOn }

    // Session inventory, like Block4PuzzleProgress: survives doors/checkpoints, not a new game.
    public static class LibraryMissionProgress
    {
        public static LibraryMissionStage Stage { get; private set; }
        static int fuseMask;
        static int installedFuseCount;
        public static int SequenceStep { get; private set; }
        public static int FuseCount => (HasFuse(0) ? 1 : 0) + (HasFuse(1) ? 1 : 0) + (HasFuse(2) ? 1 : 0);
        public static int InstalledFuseCount => installedFuseCount;
        public static bool IsLibraryGeneratorPowered => Stage == LibraryMissionStage.GeneratorOn;
        public static event Action Changed;
        public static event Action OnLibraryGeneratorPowered;

        public static bool HasFuse(int id) => id >= 0 && id < 3 && (fuseMask & (1 << id)) != 0;
        internal static bool Discover()
        {
            if (Stage != LibraryMissionStage.NotDiscovered) return false;
            Stage = LibraryMissionStage.SearchingFuses; Changed?.Invoke(); return true;
        }
        internal static bool Collect(int id)
        {
            if (Stage != LibraryMissionStage.SearchingFuses || id < 0 || id > 2 || HasFuse(id)) return false;
            fuseMask |= 1 << id;
            if (fuseMask == 7) Stage = LibraryMissionStage.FusesCollected;
            Changed?.Invoke(); return true;
        }
        internal static bool Install()
        {
            if (Stage != LibraryMissionStage.FusesCollected || fuseMask != 7 || installedFuseCount >= 3) return false;
            installedFuseCount++;
            if (installedFuseCount == 3) Stage = LibraryMissionStage.FusesInstalled;
            Changed?.Invoke(); return true;
        }
        internal static bool Press(int button, int[] sequence, out bool correct)
        {
            correct = false;
            if (Stage != LibraryMissionStage.FusesInstalled || !ValidSequence(sequence) || button < 1 || button > 7) return false;
            correct = button == sequence[SequenceStep];
            SequenceStep = correct ? SequenceStep + 1 : 0;
            if (SequenceStep == 7) Stage = LibraryMissionStage.PuzzleSolved;
            Changed?.Invoke(); return true;
        }
        internal static bool PowerOn()
        {
            if (Stage != LibraryMissionStage.PuzzleSolved || fuseMask != 7) return false;
            Stage = LibraryMissionStage.GeneratorOn;
            Changed?.Invoke(); OnLibraryGeneratorPowered?.Invoke(); return true;
        }
        public static bool ValidSequence(int[] sequence)
        {
            if (sequence == null || sequence.Length != 7) return false;
            int mask = 0;
            foreach (int id in sequence)
            {
                if (id < 1 || id > 7 || (mask & (1 << id)) != 0) return false;
                mask |= 1 << id;
            }
            return mask == 254;
        }
        public static void ResetSession()
        {
            Stage = LibraryMissionStage.NotDiscovered; fuseMask = installedFuseCount = SequenceStep = 0;
            Changed?.Invoke();
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            Changed = null; OnLibraryGeneratorPowered = null; ResetSession();
        }
    }
}
