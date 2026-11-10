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
        [SerializeField, Min(.5f)] float discoveryDistance = 3.2f;
        [SerializeField, Min(.5f)] float interactionDistance = 2.8f;
        [SerializeField] AudioSource feedback;
        [SerializeField] AudioClip buttonSound, errorSound, generatorSound;
        GameObject hud;
        TMP_Text prompt;
        Camera view;
        int lastFrame = -1;
        float nextInput, errorUntil, nextDiscovery;
        MaterialPropertyBlock block;
        public LibraryMissionTarget[] Fuses => (LibraryMissionTarget[])fuses.Clone();
        public LibraryMissionTarget[] Buttons => (LibraryMissionTarget[])buttons.Clone();
        public LibraryMissionTarget Generator => generator;
        public string Clue => "Percurso de partida: " + string.Join(" → ", sequence) + ". Siga as setas, um terminal de cada vez.";
        public static bool GameplayAvailable => Time.timeScale > 0 && !GameMenus.BlocksGameplay && !DeathScreen.IsVisible && !DoorReloadScreen.IsReloading &&
            GameManager.SessionPlayer != null && (!GameManager.SessionPlayer.TryGetComponent<PlayerLife>(out var life) || !life.IsDead);

        void Awake() { block = new MaterialPropertyBlock(); }
        void OnEnable() { LibraryMissionProgress.Changed += Refresh; }
        void Start()
        {
            var ids = new HashSet<int>();
            bool valid = discoveryPoint && generator && LibraryMissionProgress.ValidSequence(sequence) && fuses.Length == 3 && buttons.Length == 7 &&
                fuseSockets.Length == 3 && circuitSegments.Length == 7 && worldStatus;
            foreach (var target in fuses) valid &= target && target.Mission == this && target.Kind == LibraryTargetKind.Fuse && target.Id >= 0 && target.Id < 3 && ids.Add(target.Id);
            ids.Clear();
            foreach (var target in buttons) valid &= target && target.Mission == this && target.Kind == LibraryTargetKind.Button && target.Id >= 1 && target.Id <= 7 && ids.Add(target.Id);
            foreach (var r in fuseSockets) valid &= r;
            foreach (var r in circuitSegments) valid &= r;
            if (!valid) { Debug.LogError("CampusLibrary: missão sem três fusíveis únicos, sete botões/segmentos, encaixes ou gerador configurados.", this); enabled = false; return; }
            Refresh();
        }
        void Update()
        {
            if (!GameplayAvailable) { if (hud) hud.SetActive(false); return; }
            if (!hud && GameMenus.Instance)
            {
                hud = MenuStyle.Rect("Library interaction prompt", GameMenus.Instance.transform, 0, 0, 800, 40).gameObject;
                var rect = (RectTransform)hud.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0); rect.anchoredPosition = new Vector2(0, 102);
                prompt = MenuStyle.Text("Prompt", hud.transform, "", 0, 0, 800, 40, 23, MenuStyle.Paper);
                prompt.alignment = TextAlignmentOptions.Center;
            }
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
            if (hud) { hud.SetActive(focus); prompt.text = focus ? Prompt(focus) : ""; }
            if (focus && ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame))) TryInteract(focus);
            if (errorUntil > 0 && Time.time >= errorUntil) { errorUntil = 0; Refresh(); }
        }
        LibraryMissionTarget Focus()
        {
            if (!view || !Physics.Raycast(view.transform.position, view.transform.forward, out var hit, interactionDistance, ~0, QueryTriggerInteraction.Ignore)) return null;
            var target = hit.collider.GetComponentInParent<LibraryMissionTarget>();
            return target && target.Mission == this ? target : null;
        }
        bool Visible(LibraryMissionTarget target, float distance)
        {
            if (!target || !target.gameObject.activeInHierarchy) return false;
            var camera = view ? view : Camera.main;
            if (!camera || Vector3.Distance(camera.transform.position, target.transform.position) > distance) return false;
            if (!Physics.Linecast(camera.transform.position, target.transform.position, out var hit, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.collider.GetComponentInParent<LibraryMissionTarget>() == target;
        }
        string Prompt(LibraryMissionTarget target)
        {
            string key = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame ? "[X] " : "[E] ";
            switch (target.Kind)
            {
                case LibraryTargetKind.Fuse: return key + "Coletar fusível";
                case LibraryTargetKind.Clue: return key + "Ler esquema de manutenção";
                case LibraryTargetKind.Button: return key + "Terminal " + target.Id + "  /  " + LibraryMissionProgress.SequenceStep + "/7";
                default:
                    return key + (LibraryMissionProgress.Stage == LibraryMissionStage.FusesCollected ? "Instalar fusíveis" :
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
                    Notify($"Fusível coletado ({LibraryMissionProgress.FuseCount}/3).");
                    if (LibraryMissionProgress.FuseCount == 3) Notify("Todos os fusíveis encontrados! Volte ao gerador.");
                    return true;
                case LibraryTargetKind.Clue: Notify(Clue, 7); return true;
                case LibraryTargetKind.Button:
                    if (LibraryMissionProgress.Stage < LibraryMissionStage.FusesInstalled) { Notify("Instale os 3 fusíveis antes de usar o painel."); return false; }
                    if (!LibraryMissionProgress.Press(target.Id, sequence, out bool correct)) return false;
                    Sound(correct ? buttonSound : errorSound);
                    if (!correct) { errorUntil = Time.time + .7f; target.SetIndicator(new Color(.9f, .08f, .025f)); Notify("Sequência incorreta. Tente novamente."); }
                    else if (LibraryMissionProgress.Stage == LibraryMissionStage.PuzzleSolved) Notify("Circuito restaurado. Acione o gerador.");
                    return true;
                default:
                    if (LibraryMissionProgress.Stage == LibraryMissionStage.NotDiscovered)
                    {
                        if (LibraryMissionProgress.Discover()) Notify("O gerador está sem energia. Encontre os 3 fusíveis para ligá-lo.");
                        return true;
                    }
                    if (LibraryMissionProgress.Install()) { Sound(buttonSound); Notify("3 fusíveis instalados. Resolva o painel de energia."); return true; }
                    if (LibraryMissionProgress.PowerOn()) { Sound(generatorSound); Notify("Gerador ligado! Energia da biblioteca restaurada."); return true; }
                    Notify(LibraryMissionProgress.Stage < LibraryMissionStage.FusesCollected ? $"Faltam {3 - LibraryMissionProgress.FuseCount} fusíveis para restaurar o gerador." :
                        LibraryMissionProgress.IsLibraryGeneratorPowered ? "Energia da biblioteca restaurada." : "Resolva o painel de energia. Consulte o esquema na parede.");
                    return false;
            }
        }
        void Sound(AudioClip clip) { if (feedback && clip) feedback.PlayOneShot(clip, .6f); }
        static void Notify(string message, float duration = 4.5f) => DeathScreen.Instance?.ShowNotification(message, duration);
        void Paint(Renderer renderer, bool on)
        {
            if (!renderer) return;
            renderer.GetPropertyBlock(block);
            var color = on ? new Color(.26f, .8f, .42f) : new Color(.12f, .14f, .13f);
            block.SetColor("_BaseColor", color); block.SetColor("_EmissionColor", on ? color * .8f : Color.black); renderer.SetPropertyBlock(block);
        }
        void Refresh()
        {
            if (worldClue) worldClue.text = "PARTIDA DE EMERGÊNCIA\n\n" + string.Join(" → ", sequence) + "\n\nSIGA O PERCURSO DOS TERMINAIS\nInstale os três fusíveis antes da partida.";
            foreach (var fuse in fuses) if (fuse) fuse.gameObject.SetActive(LibraryMissionProgress.Stage >= LibraryMissionStage.SearchingFuses && !LibraryMissionProgress.HasFuse(fuse.Id));
            for (int i = 0; i < fuseSockets.Length; i++) Paint(fuseSockets[i], LibraryMissionProgress.Stage >= LibraryMissionStage.FusesInstalled);
            for (int i = 0; i < circuitSegments.Length; i++) Paint(circuitSegments[i], i < LibraryMissionProgress.SequenceStep);
            foreach (var button in buttons) if (button) button.SetIndicator(LibraryMissionProgress.Stage >= LibraryMissionStage.FusesInstalled ? new Color(.46f, .57f, .38f) : new Color(.16f, .17f, .15f));
            if (worldStatus) worldStatus.text = LibraryMissionProgress.IsLibraryGeneratorPowered ? "ENERGIA RESTAURADA" :
                LibraryMissionProgress.Stage == LibraryMissionStage.PuzzleSolved ? "CIRCUITO OK • ACIONE O GERADOR" :
                LibraryMissionProgress.Stage == LibraryMissionStage.FusesInstalled ? $"CIRCUITO {LibraryMissionProgress.SequenceStep}/7" : $"FUSÍVEIS {LibraryMissionProgress.FuseCount}/3 • INSTALAR";
        }
        void OnDisable() { LibraryMissionProgress.Changed -= Refresh; if (hud) hud.SetActive(false); }
        void OnDestroy() { DeathScreen.Instance?.ClearNotifications(); if (hud) Destroy(hud); }
    }
}
