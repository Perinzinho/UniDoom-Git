using System.Collections.Generic;
using TMPro;
using UniDoom.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UniDoom.CampusLibrary
{
    [DisallowMultipleComponent]
    public sealed class LibraryMissionController : MonoBehaviour
    {
        [SerializeField] Transform discoveryPoint;
        [SerializeField] LibraryMissionTarget generator;
        [SerializeField] LibraryMissionTarget[] fuses = new LibraryMissionTarget[3];
        [SerializeField] LibraryMissionTarget[] buttons = new LibraryMissionTarget[7];
        [SerializeField] Renderer[] fuseSockets = new Renderer[3];
        [SerializeField] Renderer[] circuitSegments = new Renderer[7];
        [SerializeField] TMP_Text worldStatus;
        [SerializeField] TMP_Text worldClue;
        [SerializeField] int[] sequence = { 3, 1, 5, 2, 7, 4, 6 };
        [SerializeField] bool randomizeSequence = true;
        [SerializeField, Min(.5f)] float discoveryDistance = 3.2f;
        [SerializeField, Min(.5f)] float interactionDistance = 2.8f;
        [SerializeField] AudioSource feedback;
        [SerializeField] AudioClip buttonSound, errorSound, generatorSound;
        string interactionPrompt;
        Camera view;
        int lastFrame = -1;
        float nextInput, errorUntil, nextDiscovery;
        MaterialPropertyBlock block;
        LibraryMissionTarget[] lowTargets;
        LibraryGeneratorPanel panel;
        public LibraryMissionTarget[] Fuses => (LibraryMissionTarget[])fuses.Clone();
        public LibraryMissionTarget[] Buttons => (LibraryMissionTarget[])buttons.Clone();
        public LibraryMissionTarget Generator => generator;
        public string Clue => "Consulte os bilhetes no acesso restrito, na oficina e nas mesas de estudo.";
        public static bool GameplayAvailable => Time.timeScale > 0 && !GameMenus.BlocksGameplay && !DeathScreen.IsVisible && !DoorReloadScreen.IsReloading &&
            GameManager.SessionPlayer != null && (!GameManager.SessionPlayer.TryGetComponent<PlayerLife>(out var life) || !life.IsDead);

        void Awake() { block = new MaterialPropertyBlock(); }
        void OnEnable() { LibraryMissionProgress.Changed += Refresh; }
        void Start()
        {
            if (randomizeSequence) sequence = GenerateRandomSequence();
            var ids = new HashSet<int>();
            bool valid = discoveryPoint && generator && LibraryMissionProgress.ValidSequence(sequence) && fuses.Length == 3 && buttons.Length == 7 &&
                fuseSockets.Length == 3 && circuitSegments.Length == 7 && worldStatus;
            foreach (var target in fuses) valid &= target && target.Mission == this && target.Kind == LibraryTargetKind.Fuse && target.Id >= 0 && target.Id < 3 && ids.Add(target.Id);
            ids.Clear();
            foreach (var target in buttons) valid &= target && target.Mission == this && target.Kind == LibraryTargetKind.Button && target.Id >= 1 && target.Id <= 7 && ids.Add(target.Id);
            foreach (var r in fuseSockets) valid &= r;
            foreach (var r in circuitSegments) valid &= r;
            if (!valid) { Debug.LogError("CampusLibrary: missão sem três fusíveis únicos, sete botões/segmentos, encaixes ou gerador configurados.", this); enabled = false; return; }
            var reachable = new List<LibraryMissionTarget>();
            foreach (var target in transform.root.GetComponentsInChildren<LibraryMissionTarget>(true))
                if (target.Mission == this && (target.Kind == LibraryTargetKind.Fuse || target.Kind == LibraryTargetKind.Clue)) reachable.Add(target);
            lowTargets = reachable.ToArray();
            panel = GetComponent<LibraryGeneratorPanel>();
            if (!panel) panel = gameObject.AddComponent<LibraryGeneratorPanel>();
            panel.Configure(this);
            var modelVariants = GetComponent<LibraryGeneratorModelVariants>();
            if (!modelVariants) modelVariants = gameObject.AddComponent<LibraryGeneratorModelVariants>();
            modelVariants.Configure();
            if (feedback) feedback.ignoreListenerPause = true;
            Refresh();
        }
        void Update()
        {
            if (!GameplayAvailable) { interactionPrompt = null; return; }
            if (!view) view = Camera.main;
            if (Time.time >= nextDiscovery && LibraryMissionProgress.Stage == LibraryMissionStage.NotDiscovered)
            {
                nextDiscovery = Time.time + .15f;
                Vector3 offset = GameManager.SessionPlayer.transform.position - discoveryPoint.position; offset.y = 0;
                if (offset.sqrMagnitude <= discoveryDistance * discoveryDistance && Visible(generator, discoveryDistance + 1))
                {
                    if (LibraryMissionProgress.Discover()) Notify("O gerador está sem energia. Encontre os 3 fusíveis para ligá-lo.");
                }
            }
            var focus = Focus();
            interactionPrompt = focus ? Prompt(focus) : null;
            if (focus && ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame))) TryInteract(focus);
            if (errorUntil > 0 && Time.unscaledTime >= errorUntil) { errorUntil = 0; Refresh(); }
        }
        LibraryMissionTarget Focus()
        {
            if (!view) return null;
            if (Physics.Raycast(view.transform.position, view.transform.forward, out var hit, interactionDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                var target = hit.collider.GetComponentInParent<LibraryMissionTarget>();
                if (target && target.Mission == this) return target;
                if (IsGeneratorSurface(hit.collider)) return generator;
            }
            // The player only turns horizontally. Tabletop items still require facing,
            // the original reach and a clear eye-to-object line, without pitching the camera.
            var forward = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
            LibraryMissionTarget nearest = null; float bestAlignment = .978f, bestDistance = float.MaxValue;
            foreach (var target in lowTargets)
            {
                if (!target || !target.gameObject.activeInHierarchy) continue;
                var offset = target.transform.position - view.transform.position; offset.y = 0;
                float distance = offset.sqrMagnitude;
                if (distance < .01f) continue;
                float alignment = Vector3.Dot(forward, offset.normalized);
                if (alignment < bestAlignment || (Mathf.Approximately(alignment, bestAlignment) && distance >= bestDistance) || !Visible(target, interactionDistance)) continue;
                nearest = target; bestAlignment = alignment; bestDistance = distance;
            }
            return nearest;
        }
        bool Visible(LibraryMissionTarget target, float distance)
        {
            if (!target || !target.gameObject.activeInHierarchy) return false;
            var camera = view ? view : Camera.main;
            if (!camera || Vector3.Distance(camera.transform.position, target.transform.position) > distance) return false;
            if (!Physics.Linecast(camera.transform.position, target.transform.position, out var hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            if (hit.collider.GetComponentInParent<LibraryMissionTarget>() == target) return true;
            return target == generator && IsGeneratorSurface(hit.collider);
        }
        bool IsGeneratorSurface(Collider collider)
        {
            if (!collider || !generator) return false;
            if (collider.GetComponentInParent<LibraryMissionTarget>() == generator) return true;
            string objectName = collider.gameObject.name;
            return objectName == "Library backup generator" || objectName == "Generator ignition control pedestal" ||
                objectName == "Generator install and ignition switch" || objectName.StartsWith("Generator model - ");
        }
        string Prompt(LibraryMissionTarget target)
        {
            string key = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame ? "[X] " : "[E] ";
            switch (target.Kind)
            {
                case LibraryTargetKind.Fuse: return key + "Coletar fusível";
                case LibraryTargetKind.Clue: return key + "Ler bilhete de manutenção";
                case LibraryTargetKind.Button: return key + "Usar o gerador para abrir o painel";
                case LibraryTargetKind.FuseSocket:
                    return key + "Inserir os fusíveis diretamente no gerador";
                default:
                    return key + (LibraryMissionProgress.Stage == LibraryMissionStage.FusesCollected ?
                        "Instalar fusíveis e abrir puzzle do gerador" :
                        LibraryMissionProgress.Stage == LibraryMissionStage.FusesInstalled ?
                        "Abrir puzzle do gerador  /  " + LibraryMissionProgress.SequenceStep + "/7" :
                        LibraryMissionProgress.Stage == LibraryMissionStage.PuzzleSolved ? "Ligar gerador" :
                        LibraryMissionProgress.IsLibraryGeneratorPowered ? "Energia restaurada" : "Inspecionar gerador");
            }
        }
        public bool TryInteract(LibraryMissionTarget target)
        {
            if (!isActiveAndEnabled || !GameplayAvailable || target == null || target.Mission != this || lastFrame == Time.frameCount ||
                Time.time < nextInput || !Visible(target, interactionDistance)) return false;
            lastFrame = Time.frameCount; nextInput = Time.time + .22f;
            switch (target.Kind)
            {
                case LibraryTargetKind.Fuse:
                    if (!LibraryMissionProgress.Collect(target.Id)) return false;
                    SFXManager.Instance?.PlayKeyPickupSound();
                    Notify($"Fusível coletado ({LibraryMissionProgress.FuseCount}/3)." +
                        (LibraryMissionProgress.FuseCount == 3 ? " Volte ao gerador." : ""));
                    return true;
                case LibraryTargetKind.Clue:
                    if (target.Document && GameMenus.Instance) return GameMenus.Instance.TryOpenDocument(target.Document, target);
                    Notify(Clue); return false;
                case LibraryTargetKind.FuseSocket:
                    Notify("Insira os fusíveis diretamente no gerador.");
                    return false;
                case LibraryTargetKind.Button:
                    Notify(LibraryMissionProgress.Stage < LibraryMissionStage.FusesInstalled ?
                        "Instale os 3 fusíveis e use o gerador." : "Use E diretamente no gerador para abrir o puzzle.");
                    return false;
                default:
                    if (LibraryMissionProgress.Stage == LibraryMissionStage.NotDiscovered)
                    {
                        if (LibraryMissionProgress.Discover()) Notify("O gerador está sem energia. Encontre os 3 fusíveis para ligá-lo.");
                        return true;
                    }
                    if (LibraryMissionProgress.Stage == LibraryMissionStage.FusesCollected)
                    {
                        // Keep every installed-count transition for the GLB variants, including
                        // a partially installed session, then open the puzzle in this same action.
                        bool installed = false;
                        while (LibraryMissionProgress.Install()) installed = true;
                        if (installed)
                        {
                            Sound(buttonSound);
                            Notify("3 fusíveis inseridos no gerador. Complete o puzzle para restaurar a energia.");
                        }
                    }
                    if (LibraryMissionProgress.Stage == LibraryMissionStage.FusesInstalled)
                        return panel != null && panel.Open();
                    if (LibraryMissionProgress.PowerOn()) { Sound(generatorSound); Notify("Gerador ligado! Energia da biblioteca restaurada."); return true; }
                    Notify(LibraryMissionProgress.Stage < LibraryMissionStage.FusesCollected ? $"Faltam {3 - LibraryMissionProgress.FuseCount} fusíveis para restaurar o gerador." :
                        LibraryMissionProgress.Stage == LibraryMissionStage.FusesCollected ? "Insira os três fusíveis diretamente no gerador." :
                        LibraryMissionProgress.IsLibraryGeneratorPowered ? "Energia da biblioteca restaurada." : "Use E no gerador para abrir o puzzle de energia.");
                    return false;
            }
        }
        internal bool TryPressFromPanel(int id, out bool correct)
        {
            correct = false;
            if (!isActiveAndEnabled || LibraryMissionProgress.Stage != LibraryMissionStage.FusesInstalled ||
                !LibraryMissionProgress.Press(id, sequence, out correct)) return false;
            Sound(correct ? buttonSound : errorSound);
            if (!correct)
            {
                errorUntil = Time.unscaledTime + .7f;
                Notify("Sequência incorreta. O circuito foi reiniciado.");
            }
            else if (LibraryMissionProgress.Stage == LibraryMissionStage.PuzzleSolved && LibraryMissionProgress.PowerOn())
            {
                Sound(generatorSound);
                Notify("Gerador ligado! Energia da biblioteca restaurada.");
            }
            return true;
        }
        internal bool PanelTerminalOn(int id)
        {
            if (LibraryMissionProgress.Stage >= LibraryMissionStage.PuzzleSolved) return true;
            for (int i = 0; i < LibraryMissionProgress.SequenceStep; i++)
                if (sequence[i] == id) return true;
            return false;
        }
        static int[] GenerateRandomSequence()
        {
            int[] seq = { 1, 2, 3, 4, 5, 6, 7 };
            for (int i = seq.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (seq[i], seq[j]) = (seq[j], seq[i]);
            }
            return seq;
        }
        void Sound(AudioClip clip) { if (feedback && clip) feedback.PlayOneShot(clip, .6f); }
        static void Notify(string message, float duration = 4.5f) => GameplayNotice.Show(message, duration);
        void OnGUI()
        {
            if (GameplayAvailable && !GameplayNotice.IsShowing) GameplayNotice.Draw(interactionPrompt);
        }
        void Paint(Renderer renderer, bool on)
        {
            if (!renderer) return;
            renderer.GetPropertyBlock(block);
            var color = on ? new Color(.26f, .8f, .42f) : new Color(.12f, .14f, .13f);
            block.SetColor("_BaseColor", color); block.SetColor("_EmissionColor", on ? color * .8f : Color.black); renderer.SetPropertyBlock(block);
        }
        void Refresh()
        {
            if (worldClue)
            {
                if (randomizeSequence && sequence != null && LibraryMissionProgress.ValidSequence(sequence))
                    worldClue.text = $"SEQUÊNCIA DO CIRCUITO\nT{sequence[0]} → T{sequence[1]} → T{sequence[2]} → T{sequence[3]}\nT{sequence[4]} → T{sequence[5]} → T{sequence[6]}";
                else
                    worldClue.text = "REGISTROS DE MANUTENÇÃO\nConsulte os bilhetes do plantão.";
            }
            foreach (var fuse in fuses) if (fuse) fuse.gameObject.SetActive(LibraryMissionProgress.Stage >= LibraryMissionStage.SearchingFuses && !LibraryMissionProgress.HasFuse(fuse.Id));
            for (int i = 0; i < fuseSockets.Length; i++) Paint(fuseSockets[i], LibraryMissionProgress.Stage >= LibraryMissionStage.FusesInstalled);
            for (int i = 0; i < circuitSegments.Length; i++) Paint(circuitSegments[i], i < LibraryMissionProgress.SequenceStep);
            foreach (var button in buttons) if (button) button.SetIndicator(LibraryMissionProgress.Stage >= LibraryMissionStage.FusesInstalled ? new Color(.46f, .57f, .38f) : new Color(.16f, .17f, .15f));
            if (worldStatus) worldStatus.text = LibraryMissionProgress.IsLibraryGeneratorPowered ? "ENERGIA RESTAURADA" :
                LibraryMissionProgress.Stage == LibraryMissionStage.PuzzleSolved ? "CIRCUITO OK • GERADOR LIGANDO" :
                LibraryMissionProgress.Stage == LibraryMissionStage.FusesInstalled ? $"USE O GERADOR • CIRCUITO {LibraryMissionProgress.SequenceStep}/7" : $"FUSÍVEIS {LibraryMissionProgress.FuseCount}/3 • INSTALAR";
        }
        void OnDisable() { LibraryMissionProgress.Changed -= Refresh; interactionPrompt = null; }
    }
}
