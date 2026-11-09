using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniDoom.CampusBlock4
{
    [DisallowMultipleComponent]
    public sealed class Block4MonitorClues : MonoBehaviour
    {
        [SerializeField] Block4PowerFluctuation power;
        [SerializeField] Transform[] displays;
        [SerializeField] Transform[] firstRowMonitors;
        [SerializeField] int[] initialDestinations;
        [SerializeField] Vector2 shiftCooldown = new Vector2(180, 300);
        [SerializeField, Range(0, 1)] float blackoutShiftChance = .25f;

        void OnEnable() { if (power) power.BlackoutStarted += OnBlackout; }
        void OnDisable() { if (power) power.BlackoutStarted -= OnBlackout; }
        void Start()
        {
            if (Block4PuzzleProgress.MonitorDestinations == null)
                Block4PuzzleProgress.MonitorDestinations = (int[])initialDestinations.Clone();
            ApplyDestinations(Block4PuzzleProgress.MonitorDestinations);
            if (Block4PuzzleProgress.NextClueShiftAt <= 0)
                Block4PuzzleProgress.NextClueShiftAt = Time.time + UnityEngine.Random.Range(shiftCooldown.x, shiftCooldown.y);
        }

        void OnBlackout()
        {
            if (!isActiveAndEnabled || Block4PuzzleProgress.DoorUnlocked || Time.time < Block4PuzzleProgress.NextClueShiftAt ||
                UnityEngine.Random.value >= blackoutShiftChance) return;
            var next = ChooseDestinations(firstRowMonitors.Length, Block4PuzzleProgress.MonitorDestinations);
            ApplyDestinations(next);
            Block4PuzzleProgress.MonitorDestinations = next;
            Block4PuzzleProgress.NextClueShiftAt = Time.time + UnityEngine.Random.Range(shiftCooldown.x, shiftCooldown.y);
        }

        // Placement changes only; the symbol, digit, photographs and solution are never rewritten.
        public void ApplyDestinations(int[] destinations)
        {
            if (destinations == null || destinations.Length != displays.Length) throw new ArgumentException("One destination per clue is required.");
            var used = new HashSet<int>();
            for (int i = 0; i < destinations.Length; i++)
            {
                int id = destinations[i];
                if (id < 0 || id >= firstRowMonitors.Length || !used.Add(id) || !firstRowMonitors[id] || !displays[i])
                    throw new ArgumentException("Clue destinations must be distinct, valid first-row monitors.");
            }
            for (int i = 0; i < destinations.Length; i++)
            {
                var anchor = firstRowMonitors[destinations[i]];
                displays[i].SetPositionAndRotation(anchor.position, anchor.rotation);
            }
        }

        public static int[] ChooseDestinations(int anchorCount, int[] current)
        {
            if (current == null || anchorCount <= current.Length) throw new ArgumentException("Additional monitors are required.");
            var available = new List<int>();
            for (int i = 0; i < anchorCount; i++) available.Add(i);
            var next = new int[current.Length];
            for (int i = 0; i < next.Length; i++)
            {
                var candidates = available.FindAll(id => id != current[i]);
                int chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                next[i] = chosen;
                available.Remove(chosen);
            }
            return next;
        }
    }
}
