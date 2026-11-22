using System;
using System.Collections;
using TMPro;
using UniDoom.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UniDoom.CampusLibrary
{
    public enum LibraryPanelState { Closed, Opening, Open, Closing }

    [DisallowMultipleComponent]
    public sealed class LibraryGeneratorPanel : MonoBehaviour
    {
        LibraryMissionController mission;
        LibraryGeneratorPanelView view;
        Coroutine transition;
        Coroutine completion;
        public LibraryPanelState State { get; private set; } = LibraryPanelState.Closed;
        public event Action PanelOpened;
        public event Action PanelClosed;
        public event Action<int, bool> SwitchToggled;
        public event Action PuzzleCompleted;

        internal void Configure(LibraryMissionController owner)
        {
            if (mission == owner) return;
            if (mission != null) LibraryMissionProgress.Changed -= Refresh;
            mission = owner;
            LibraryMissionProgress.Changed += Refresh;
        }

        public bool Open()
        {
            if (mission == null || State != LibraryPanelState.Closed || GameMenus.Instance == null ||
                LibraryMissionProgress.Stage < LibraryMissionStage.FusesInstalled) return false;
            EnsureView();
            if (!GameMenus.Instance.TryOpenInteraction(view.gameObject, this, ClosedExternally)) return false;
            State = LibraryPanelState.Opening;
            view.Refresh(mission);
            view.SetPresentation(0f, .96f, false);
            transition = StartCoroutine(OpenRoutine());
            PanelOpened?.Invoke();
            return true;
        }

        public void Close()
        {
            if (State == LibraryPanelState.Closed || State == LibraryPanelState.Closing) return;
            if (completion != null) { StopCoroutine(completion); completion = null; }
            if (transition != null) StopCoroutine(transition);
            State = LibraryPanelState.Closing;
            view.SetInteractive(false);
            transition = StartCoroutine(CloseRoutine());
        }

        void Update()
        {
            if (State != LibraryPanelState.Open && State != LibraryPanelState.Opening) return;
            if ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame ||
                    Gamepad.current.startButton.wasPressedThisFrame))) Close();
        }

        IEnumerator OpenRoutine()
        {
            const float duration = .13f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                view.SetPresentation(t, Mathf.Lerp(.96f, 1f, t), false);
                yield return null;
            }
            view.SetPresentation(1f, 1f, true);
            State = LibraryPanelState.Open;
            transition = null;
            view.SelectFirstAvailable();
        }

        IEnumerator CloseRoutine()
        {
            const float duration = .1f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                view.SetPresentation(1f - t, Mathf.Lerp(1f, .98f, t), false);
                yield return null;
            }
            transition = null;
            GameMenus.Instance?.CloseInteractionOwnedBy(this);
        }

        void Toggle(int id)
        {
            if (State != LibraryPanelState.Open || mission == null) return;
            var before = LibraryMissionProgress.Stage;
            if (!mission.TryPressFromPanel(id, out bool correct)) return;
            SwitchToggled?.Invoke(id, correct);
            if (!correct) view.FlashError(id);
            view.Refresh(mission);
            if (before < LibraryMissionStage.PuzzleSolved &&
                LibraryMissionProgress.Stage >= LibraryMissionStage.PuzzleSolved)
            {
                PuzzleCompleted?.Invoke();
                view.SetInteractive(false);
                completion = StartCoroutine(CloseAfterCompletion());
            }
        }

        IEnumerator CloseAfterCompletion()
        {
            yield return new WaitForSecondsRealtime(.9f);
            completion = null;
            Close();
        }

        void Refresh()
        {
            if (view != null && State != LibraryPanelState.Closed) view.Refresh(mission);
        }

        void EnsureView()
        {
            if (view != null) return;
            var root = new GameObject("Generator switch panel modal", typeof(RectTransform),
                typeof(Image), typeof(CanvasGroup), typeof(LibraryGeneratorPanelView));
            root.SetActive(false);
            view = root.GetComponent<LibraryGeneratorPanelView>();
            view.Build(Toggle, Close);
        }

        void ClosedExternally()
        {
            if (completion != null) StopCoroutine(completion);
            completion = null;
            if (transition != null) StopCoroutine(transition);
            transition = null;
            State = LibraryPanelState.Closed;
            PanelClosed?.Invoke();
        }

        void OnDisable()
        {
            if (State != LibraryPanelState.Closed) GameMenus.Instance?.CloseInteractionOwnedBy(this);
        }

        void OnDestroy()
        {
            LibraryMissionProgress.Changed -= Refresh;
            if (view != null) Destroy(view.gameObject);
        }
    }

    public sealed class LibraryGeneratorPanelView : MonoBehaviour
    {
        static readonly Color Metal = new(.095f, .075f, .068f, 1f);
        static readonly Color Recess = new(.018f, .017f, .016f, 1f);
        static readonly Color Edge = new(.38f, .29f, .23f, 1f);
        static readonly Color Paper = new(.92f, .86f, .72f, 1f);
        static readonly Color Muted = new(.53f, .54f, .48f, 1f);
        static readonly Color Amber = new(.82f, .48f, .12f, 1f);
        CanvasGroup group;
        RectTransform frame;
        TMP_Text status, instruction;
        Image progressFill, plate, powerLamp;
        Image[] circuitMarkers;
        LibraryGeneratorSwitchView[] switches;

        internal void Build(Action<int> toggled, Action close)
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var blocker = GetComponent<Image>();
            blocker.color = new Color(0, 0, 0, .72f);
            blocker.raycastTarget = true;
            group = GetComponent<CanvasGroup>();

            frame = Rect("Industrial panel", root, Vector2.zero, new Vector2(1160, 780));
            plate = frame.gameObject.AddComponent<Image>();
            plate.color = Metal;
            var outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = Edge;
            outline.effectDistance = new Vector2(4, -4);
            MakeImage("Inner cavity", frame, new Vector2(0, -18), new Vector2(1092, 702), Recess);
            MakeImage("Left frame", frame, new Vector2(-544, -18), new Vector2(10, 710), Edge);
            MakeImage("Right frame", frame, new Vector2(544, -18), new Vector2(10, 710), Edge);
            MakeImage("Top rail", frame, new Vector2(0, 354), new Vector2(1100, 12), Edge);
            MakeImage("Bottom rail", frame, new Vector2(0, -354), new Vector2(1100, 12), Edge);
            foreach (var corner in new[] { new Vector2(-535, 345), new Vector2(535, 345),
                         new Vector2(-535, -345), new Vector2(535, -345) })
            {
                MakeImage("Bolt socket", frame, corner, new Vector2(25, 25), Recess);
                MakeImage("Steel bolt", frame, corner, new Vector2(15, 15), new Color(.48f, .42f, .35f));
                MakeImage("Bolt slot", frame, corner, new Vector2(10, 2), Recess);
            }

            MakeImage("Header band", frame, new Vector2(0, 294), new Vector2(1040, 94), new Color(.055f, .05f, .045f));
            Text("Title", frame, "GERADOR  /  QUADRO DE SINCRONIA", new Vector2(0, 320),
                new Vector2(980, 42), 31, Paper, FontStyles.Bold);
            powerLamp = MakeImage("Power lamp", frame, new Vector2(-466, 270), new Vector2(20, 20), Amber);
            status = Text("Circuit status", frame, "", new Vector2(10, 270),
                new Vector2(900, 34), 22, Amber, FontStyles.Bold);
            instruction = Text("Instruction", frame, "", new Vector2(0, 230),
                new Vector2(990, 30), 17, Paper, FontStyles.Normal);

            var progressTrack = MakeImage("Circuit progress track", frame, new Vector2(0, 198),
                new Vector2(920, 16), new Color(.20f, .17f, .14f));
            progressFill = MakeImage("Circuit progress", progressTrack.rectTransform, Vector2.zero,
                new Vector2(916, 12), new Color(.30f, .92f, .23f));
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;

            switches = new LibraryGeneratorSwitchView[7];
            // Conduits and the central indicator strip sit behind the clickable modules.
            for (int i = 0; i < 4; i++)
                MakeImage("Upper cable " + i, frame, new Vector2(-390 + i * 260, 155),
                    new Vector2(5, 62), i % 2 == 0 ? new Color(.25f, .065f, .045f) : new Color(.075f, .15f, .22f));
            MakeImage("Indicator bridge", frame, new Vector2(0, -80), new Vector2(1030, 32), Edge);
            MakeImage("Indicator bridge inset", frame, new Vector2(0, -80), new Vector2(1012, 23), Recess);
            circuitMarkers = new Image[7];
            for (int i = 0; i < circuitMarkers.Length; i++)
                circuitMarkers[i] = MakeImage("Circuit marker " + (i + 1), frame, new Vector2(-420 + i * 140, -80),
                    new Vector2(15, 15), new Color(.18f, .18f, .16f));
            for (int i = 0; i < 3; i++)
                MakeImage("Lower cable " + i, frame, new Vector2(-260 + i * 260, -115),
                    new Vector2(5, 42), i == 1 ? new Color(.075f, .15f, .22f) : new Color(.25f, .065f, .045f));
            float[] top = { -390, -130, 130, 390 };
            for (int i = 0; i < 4; i++) switches[i] = CreateSwitch(i + 1, new Vector2(top[i], 45), toggled);
            float[] bottom = { -260, 0, 260 };
            for (int i = 0; i < 3; i++) switches[i + 4] = CreateSwitch(i + 5, new Vector2(bottom[i], -205), toggled);

            var closeButton = MakeImage("Exit", frame, new Vector2(448, -315), new Vector2(165, 46), Recess);
            closeButton.raycastTarget = true;
            var button = closeButton.gameObject.AddComponent<Button>();
            button.targetGraphic = closeButton;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.45f, 1.25f, .85f, 1);
            colors.pressedColor = new Color(.72f, .55f, .34f, 1);
            button.colors = colors;
            button.onClick.AddListener(() => close());
            Text("Exit label", closeButton.rectTransform, "SAIR  [ESC]", Vector2.zero,
                new Vector2(165, 46), 18, Paper, FontStyles.Bold);
        }

        LibraryGeneratorSwitchView CreateSwitch(int id, Vector2 position, Action<int> toggled)
        {
            var rect = Rect("Terminal " + id, frame, position, new Vector2(174, 218));
            var item = rect.gameObject.AddComponent<LibraryGeneratorSwitchView>();
            item.Build(id, toggled);
            return item;
        }

        internal void Refresh(LibraryMissionController mission)
        {
            if (mission == null) return;
            bool solved = LibraryMissionProgress.Stage >= LibraryMissionStage.PuzzleSolved;
            bool enabled = LibraryMissionProgress.Stage == LibraryMissionStage.FusesInstalled;
            for (int i = 0; i < switches.Length; i++)
                switches[i].SetState(mission.PanelTerminalOn(i + 1), enabled);
            for (int i = 0; i < circuitMarkers.Length; i++)
                circuitMarkers[i].color = solved || i < LibraryMissionProgress.SequenceStep ?
                    new Color(.35f, 1f, .25f) : new Color(.18f, .18f, .16f);
            float progress = solved ? 1f : LibraryMissionProgress.SequenceStep / 7f;
            progressFill.fillAmount = progress;
            powerLamp.color = solved ? new Color(.32f, 1f, .24f) : Amber;
            plate.color = solved ? new Color(.075f, .12f, .068f) : Metal;
            status.color = solved ? new Color(.43f, 1f, .33f) : Amber;
            status.text = LibraryMissionProgress.IsLibraryGeneratorPowered ? "ENERGIA RESTAURADA" :
                solved ? "CIRCUITO ESTÁVEL — GERADOR LIGADO" :
                $"SINCRONIA DO CIRCUITO  {LibraryMissionProgress.SequenceStep}/7";
            instruction.text = solved ? "GERADOR RESTAURADO  •  RETORNANDO AO JOGO" :
                "Clique nos terminais na ordem indicada pelos três registros de manutenção.";
        }

        internal void FlashError(int id)
        {
            if (id > 0 && id <= switches.Length) switches[id - 1].FlashError();
            status.text = "SEQUÊNCIA REJEITADA — CIRCUITO REINICIADO";
        }

        internal void SetPresentation(float alpha, float scale, bool interactive)
        {
            group.alpha = alpha;
            group.interactable = group.blocksRaycasts = interactive;
            frame.localScale = Vector3.one * scale;
        }

        internal void SetInteractive(bool value)
        {
            group.interactable = group.blocksRaycasts = value;
        }

        internal void SelectFirstAvailable()
        {
            foreach (var item in switches)
                if (item.SelectIfAvailable()) return;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        static Image MakeImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text Text(string name, Transform parent, string value, Vector2 position,
            Vector2 size, float fontSize, Color color, FontStyles style)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) text.font = defaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }
    }

    public sealed class LibraryGeneratorSwitchView : MonoBehaviour, IPointerEnterHandler,
        IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        static readonly Color Off = new(.92f, .085f, .045f, 1f);
        static readonly Color On = new(.28f, .96f, .20f, 1f);
        static readonly Color Disabled = new(.17f, .18f, .16f, 1f);
        int id;
        Button button;
        Image background, led, lever;
        TMP_Text state;
        Outline outline;
        Coroutine flash;

        internal void Build(int terminalId, Action<int> toggled)
        {
            id = terminalId;
            background = gameObject.AddComponent<Image>();
            background.color = new Color(.20f, .17f, .15f, 1f);
            background.raycastTarget = true;
            outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.45f, .37f, .30f, 1f);
            outline.effectDistance = new Vector2(3, -3);
            button = gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => toggled(id));

            MakeImage("Inset", Vector2.zero, new Vector2(158, 200), new Color(.04f, .038f, .034f));
            MakeText("Terminal", "T" + id, new Vector2(0, 94), new Vector2(145, 25), 18,
                new Color(.82f, .81f, .73f), FontStyles.Bold);
            MakeImage("Glass bezel", new Vector2(0, 8), new Vector2(104, 135), new Color(.36f, .32f, .27f));
            MakeImage("Glass well", new Vector2(0, 8), new Vector2(88, 119), new Color(.012f, .013f, .012f));
            lever = MakeImage("Illuminated rocker", new Vector2(0, 8), new Vector2(68, 99), Off);
            MakeImage("Rocker shine", new Vector2(-24, 8), new Vector2(5, 90), new Color(1f, 1f, 1f, .16f));
            led = MakeImage("Indicator", new Vector2(0, -77), new Vector2(18, 18), Off);
            state = MakeText("State", "OFF", new Vector2(0, -99), new Vector2(150, 19), 14,
                new Color(.68f, .66f, .59f), FontStyles.Bold);
            foreach (float x in new[] { -69f, 69f })
                foreach (float y in new[] { -91f, 91f })
                {
                    MakeImage("Fastener", new Vector2(x, y), new Vector2(9, 9), new Color(.50f, .46f, .39f));
                    MakeImage("Fastener slot", new Vector2(x, y), new Vector2(6, 1), new Color(.08f, .07f, .06f));
                }
        }

        internal void SetState(bool isOn, bool enabled)
        {
            button.interactable = enabled;
            Color color = isOn ? On : enabled ? Off : Disabled;
            led.color = color;
            lever.color = color;
            lever.rectTransform.anchoredPosition = new Vector2(0, isOn ? 12 : 4);
            state.text = enabled ? (isOn ? "ON" : "OFF") : (isOn ? "OK" : "TRAVADO");
            state.color = enabled ? new Color(.82f, .81f, .73f) : new Color(.42f, .43f, .39f);
            if (!enabled) SetHighlight(false);
        }

        internal bool SelectIfAvailable()
        {
            if (!button.interactable) return false;
            button.Select();
            return true;
        }

        internal void FlashError()
        {
            if (flash != null) StopCoroutine(flash);
            flash = StartCoroutine(ErrorRoutine());
        }

        IEnumerator ErrorRoutine()
        {
            for (int i = 0; i < 4; i++)
            {
                outline.effectColor = i % 2 == 0 ? new Color(1f, .08f, .02f) : new Color(.20f, .21f, .19f);
                yield return new WaitForSecondsRealtime(.08f);
            }
            flash = null;
        }

        public void OnPointerEnter(PointerEventData eventData) { if (button.interactable) { button.Select(); SetHighlight(true); } }
        public void OnPointerExit(PointerEventData eventData) { if (EventSystem.current?.currentSelectedGameObject != gameObject) SetHighlight(false); }
        public void OnSelect(BaseEventData eventData) => SetHighlight(button.interactable);
        public void OnDeselect(BaseEventData eventData) => SetHighlight(false);
        void SetHighlight(bool value) => outline.effectColor = value ? new Color(.82f, .48f, .12f) : new Color(.20f, .21f, .19f);

        Image MakeImage(string name, Vector2 position, Vector2 size, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        TMP_Text MakeText(string name, string value, Vector2 position, Vector2 size, float fontSize,
            Color color, FontStyles style)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null) text.font = defaultFont;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }
    }
}
