using System;
using System.Collections.Generic;
using UniDoom.UI;
using UnityEngine;

namespace UniDoom.CampusBlock4
{
    [DisallowMultipleComponent]
    public sealed class Block4MonitorClues : MonoBehaviour
    {
        [SerializeField] Block4PowerFluctuation power;
        [SerializeField] Transform[] displays;
        [SerializeField] Transform[] firstRowMonitors;
        [SerializeField] Transform[] photoFaces;
        [SerializeField] int[] initialDestinations;
        [SerializeField] Vector2 shiftCooldown = Vector2.zero;
        [SerializeField, Range(0, 1)] float blackoutShiftChance = 1;

        void OnEnable() { if (power) power.BlackoutStarted += OnBlackout; }
        void OnDisable() { if (power) power.BlackoutStarted -= OnBlackout; }
        void Start()
        {
            if (displays == null || displays.Length != 5 || firstRowMonitors == null || firstRowMonitors.Length != 8 ||
                photoFaces == null || photoFaces.Length != 4 || initialDestinations == null ||
                !ValidDestinations(initialDestinations))
            {
                Debug.LogError("Bloco 4: configure cinco pistas, oito monitores e quatro fotografias.", this);
                enabled = false;
                return;
            }
            PlacePhotographs();
            if (!ValidDestinations(Block4PuzzleProgress.MonitorDestinations))
                Block4PuzzleProgress.MonitorDestinations = ChooseDestinations(firstRowMonitors.Length, initialDestinations);
            ApplyDestinations(Block4PuzzleProgress.MonitorDestinations);
            if (Block4PuzzleProgress.NextClueShiftAt <= 0)
                Block4PuzzleProgress.NextClueShiftAt = Time.time + UnityEngine.Random.Range(shiftCooldown.x, shiftCooldown.y);
            GameplayNotice.ShowWhenGameplayReady(this,
                "Bloco 4: entre oito computadores com fotos, encontre cinco pares de s\u00edmbolo e n\u00famero. Siga a ordem do quadro para abrir a sala 423 e pegar a chave da Biblioteca do Campus.", 7.5f);
        }

        bool ValidDestinations(int[] destinations)
        {
            if (destinations == null || destinations.Length != displays.Length) return false;
            var used = new HashSet<int>();
            foreach (int id in destinations)
                if (id < 0 || id >= firstRowMonitors.Length || !used.Add(id)) return false;
            return true;
        }

        void PlacePhotographs()
        {
            for (int i = 0; i < firstRowMonitors.Length; i++)
                if (!firstRowMonitors[i]) throw new ArgumentException("All eight photo monitors are required.");
            for (int i = 0; i < photoFaces.Length; i++)
                if (!photoFaces[i]) throw new ArgumentException("All four original photo faces are required.");

            // The first portrait is already fitted to monitor 402. Reuse that screen-space pose
            // and the four existing atlas UV meshes, twice each, without duplicating assets.
            var reference = firstRowMonitors[0];
            Vector3 offset = reference.InverseTransformPoint(photoFaces[0].position);
            Quaternion rotation = Quaternion.Inverse(reference.rotation) * photoFaces[0].rotation;
            for (int i = 0; i < firstRowMonitors.Length; i++)
            {
                Transform source = photoFaces[i % photoFaces.Length];
                Transform anchor = firstRowMonitors[i];
                Transform portrait = Instantiate(source, anchor, true);
                portrait.name = "Crew photo " + (i % photoFaces.Length + 1);
                portrait.gameObject.isStatic = false;
                portrait.SetPositionAndRotation(anchor.TransformPoint(offset), anchor.rotation * rotation);
            }
            foreach (var original in photoFaces) original.gameObject.SetActive(false);
        }

        void OnBlackout()
        {
            if (!isActiveAndEnabled || Time.time < Block4PuzzleProgress.NextClueShiftAt ||
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
