using System;
using System.Collections.Generic;
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
        const string PreviousFuseSpawnsKey = "UniDoom.CampusLibrary.PreviousFuseSpawns";
        static int[] fuseSpawnIndices;
        public static int SequenceStep { get; private set; }
        public static int FuseCount => (HasFuse(0) ? 1 : 0) + (HasFuse(1) ? 1 : 0) + (HasFuse(2) ? 1 : 0);
        public static int InstalledFuseCount => installedFuseCount;
        public static bool IsLibraryGeneratorPowered => Stage == LibraryMissionStage.GeneratorOn;
        public static event Action Changed;
        public static event Action OnLibraryGeneratorPowered;

        public static bool HasFuse(int id) => id >= 0 && id < 3 && (fuseMask & (1 << id)) != 0;

        internal static int[] GetFuseSpawnIndices(Vector3[] positions, float minimumDistance)
        {
            if (fuseSpawnIndices != null) return (int[])fuseSpawnIndices.Clone();
            if (positions == null || positions.Length < 3 || positions.Length > 30) return null;

            int previousMask = PlayerPrefs.GetInt(PreviousFuseSpawnsKey, 0);
            float squaredDistance = minimumDistance * minimumDistance;
            var layouts = new List<Vector3Int>();
            for (int first = 0; first < positions.Length - 2; first++)
            {
                for (int second = first + 1; second < positions.Length - 1; second++)
                {
                    for (int third = second + 1; third < positions.Length; third++)
                    {
                        int mask = (1 << first) | (1 << second) | (1 << third);
                        if ((mask & previousMask) == 0 &&
                            Separated(positions[first], positions[second], squaredDistance) &&
                            Separated(positions[first], positions[third], squaredDistance) &&
                            Separated(positions[second], positions[third], squaredDistance))
                            layouts.Add(new Vector3Int(first, second, third));
                    }
                }
            }
            if (layouts.Count == 0) return null;

            var selected = layouts[UnityEngine.Random.Range(0, layouts.Count)];
            fuseSpawnIndices = new[] { selected.x, selected.y, selected.z };
            PlayerPrefs.SetInt(PreviousFuseSpawnsKey, (1 << selected.x) | (1 << selected.y) | (1 << selected.z));
            PlayerPrefs.Save();
            return (int[])fuseSpawnIndices.Clone();
        }

        static bool Separated(Vector3 first, Vector3 second, float squaredDistance)
        {
            Vector3 offset = first - second;
            offset.y = 0;
            return offset.sqrMagnitude >= squaredDistance;
        }

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
            fuseSpawnIndices = null;
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
