using System;
using System.Collections.Generic;
using UniDoom.UI;
using UnityEngine;

namespace UniDoom.CampusBlock4
{
    [DisallowMultipleComponent]
    public sealed class Block4MonitorClues : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color SymbolColor = new Color(.34f, .78f, .57f, 1f);
        static readonly Color HighlightColor = new Color(1f, .85f, .28f, 1f);
        const float HighlightSeconds = 8f;

        [SerializeField] Block4PowerFluctuation power;
        [SerializeField] Transform[] displays;
        [SerializeField] Transform[] firstRowMonitors;
        [SerializeField] Transform[] photoFaces;
        [SerializeField] int[] initialDestinations;
        [SerializeField] Vector2 shiftCooldown = Vector2.zero;
        [SerializeField, Range(0, 1)] float blackoutShiftChance = 1;

        Vector3[] displayOffsets;
        Quaternion[] displayRotations;
        Renderer[] symbolScreens;
        MaterialPropertyBlock screenProperties;
        float highlightUntil;

        void OnEnable() { if (power) power.BlackoutStarted += OnBlackout; }
        void OnDisable()
        {
            if (power) power.BlackoutStarted -= OnBlackout;
            if (symbolScreens == null) return;
            foreach (var screen in symbolScreens)
                if (screen) screen.SetPropertyBlock(null);
        }
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
            CacheDisplayOffsets(Block4PuzzleProgress.MonitorDestinations);
            ApplyDestinations(Block4PuzzleProgress.MonitorDestinations);
            CacheSymbolScreens();
            highlightUntil = Time.time + HighlightSeconds;
            if (Block4PuzzleProgress.NextClueShiftAt <= 0)
                Block4PuzzleProgress.NextClueShiftAt = Time.time + UnityEngine.Random.Range(shiftCooldown.x, shiftCooldown.y);
            GameplayNotice.ShowWhenGameplayReady(this,
                "As pistas com símbolo e número ficam nas telas dos computadores da primeira fileira, junto às janelas. Elas mudam de monitor a cada apagão.", 7.5f);
        }

        void CacheSymbolScreens()
        {
            var found = new List<Renderer>();
            foreach (var display in displays)
            {
                if (!display) continue;
                foreach (var screen in display.GetComponentsInChildren<Renderer>(true))
                    if (screen.sharedMaterial && screen.sharedMaterial.name == "LCD puzzle symbols")
                        found.Add(screen);
            }
            symbolScreens = found.ToArray();
            screenProperties = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (symbolScreens == null || symbolScreens.Length == 0) return;
            screenProperties ??= new MaterialPropertyBlock();
            float wave = Mathf.PingPong(Time.time * 1.5f, 1f);
            float pulse = Mathf.SmoothStep(0f, 1f, wave) * Mathf.SmoothStep(0f, 1f, 1f - wave);
            float strength = Time.time < highlightUntil ? .35f + .65f * pulse : .12f * pulse;
            screenProperties.SetColor(BaseColorId, Color.Lerp(SymbolColor, HighlightColor, strength));
            foreach (var screen in symbolScreens)
                if (screen) screen.SetPropertyBlock(screenProperties);
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
            highlightUntil = Time.time + HighlightSeconds;
            Block4PuzzleProgress.MonitorDestinations = next;
            Block4PuzzleProgress.NextClueShiftAt = Time.time + UnityEngine.Random.Range(shiftCooldown.x, shiftCooldown.y);
        }

        void CacheDisplayOffsets(int[] destinations)
        {
            displayOffsets = new Vector3[displays.Length];
            displayRotations = new Quaternion[displays.Length];
            for (int i = 0; i < displays.Length; i++)
            {
                var anchor = firstRowMonitors[destinations[i]];
                displayOffsets[i] = anchor ? anchor.InverseTransformPoint(displays[i].position) : Vector3.zero;
                displayRotations[i] = anchor ? Quaternion.Inverse(anchor.rotation) * displays[i].rotation : Quaternion.identity;
            }
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
                if (displayOffsets != null && displayRotations != null && i < displayOffsets.Length)
                    displays[i].SetPositionAndRotation(anchor.TransformPoint(displayOffsets[i]), anchor.rotation * displayRotations[i]);
                else
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
