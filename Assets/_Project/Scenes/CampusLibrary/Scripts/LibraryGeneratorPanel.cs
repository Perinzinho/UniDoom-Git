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
                PuzzleCompleted?.Invoke();
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
        static readonly Color Metal = new(.075f, .08f, .075f, 1f);
        static readonly Color Recess = new(.028f, .032f, .03f, 1f);
        static readonly Color Edge = new(.26f, .27f, .24f, 1f);
        static readonly Color Paper = new(.86f, .84f, .74f, 1f);
        static readonly Color Muted = new(.53f, .54f, .48f, 1f);
        static readonly Color Amber = new(.82f, .48f, .12f, 1f);
        CanvasGroup group;
        RectTransform frame;
        TMP_Text status, instruction;
        Image progressFill;
        LibraryGeneratorSwitchView[] switches;

        internal void Build(Action<int> toggled, Action close)
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var blocker = GetComponent<Image>();
            blocker.color = new Color(0, 0, 0, .28f);
            blocker.raycastTarget = true;
            group = GetComponent<CanvasGroup>();

            frame = Rect("Industrial panel", root, Vector2.zero, new Vector2(1160, 780));
            var plate = frame.gameObject.AddComponent<Image>();
            plate.color = Metal;
            var outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = Edge;
            outline.effectDistance = new Vector2(4, -4);
            MakeImage("Top rail", frame, new Vector2(0, 354), new Vector2(1100, 10), Edge);
            MakeImage("Warning rail", frame, new Vector2(0, -350), new Vector2(1100, 8), Amber * .7f);
            foreach (var corner in new[] { new Vector2(-535, 345), new Vector2(535, 345),
                         new Vector2(-535, -345), new Vector2(535, -345) })
                MakeImage("Recessed bolt", frame, corner, new Vector2(22, 22), new Color(.12f, .13f, .12f));

            Text("Title", frame, "QUADRO DE SINCRONIA DO GERADOR", new Vector2(0, 308),
                new Vector2(1040, 52), 34, Paper, FontStyles.Bold);
            status = Text("Circuit status", frame, "", new Vector2(0, 260),
                new Vector2(920, 38), 23, Amber, FontStyles.Bold);
            instruction = Text("Instruction", frame, "", new Vector2(0, 220),
                new Vector2(980, 34), 18, Muted, FontStyles.Normal);

            var progressTrack = MakeImage("Circuit progress track", frame, new Vector2(0, 184),
                new Vector2(900, 12), Recess);
            progressFill = MakeImage("Circuit progress", progressTrack.rectTransform, Vector2.zero,
                new Vector2(900, 12), new Color(.28f, .78f, .38f));
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;

            switches = new LibraryGeneratorSwitchView[7];
            float[] top = { -390, -130, 130, 390 };
            for (int i = 0; i < 4; i++) switches[i] = CreateSwitch(i + 1, new Vector2(top[i], 45), toggled);
            float[] bottom = { -260, 0, 260 };
            for (int i = 0; i < 3; i++) switches[i + 4] = CreateSwitch(i + 5, new Vector2(bottom[i], -205), toggled);

            var closeButton = MakeImage("Exit", frame, new Vector2(465, -315), new Vector2(150, 46), Recess);
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
                new Vector2(150, 46), 18, Paper, FontStyles.Bold);
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
            float progress = solved ? 1f : LibraryMissionProgress.SequenceStep / 7f;
            progressFill.fillAmount = progress;
            status.text = LibraryMissionProgress.IsLibraryGeneratorPowered ? "ENERGIA RESTAURADA" :
                solved ? "CIRCUITO ESTÁVEL — GERADOR LIGADO" :
                $"SINCRONIA DO CIRCUITO  {LibraryMissionProgress.SequenceStep}/7";
            instruction.text = solved ? "A sequência foi aceita e o gerador foi ligado. Feche o quadro para continuar." :
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
            text.font = TMP_Settings.defaultFontAsset;
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
        static readonly Color Off = new(.62f, .055f, .025f, 1f);
        static readonly Color On = new(.18f, .76f, .28f, 1f);
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
            background.color = new Color(.035f, .04f, .037f, 1f);
            background.raycastTarget = true;
            outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.20f, .21f, .19f, 1f);
            outline.effectDistance = new Vector2(2, -2);
            button = gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => toggled(id));

            led = MakeImage("Indicator", new Vector2(0, 76), new Vector2(54, 14), Off);
            MakeText("Terminal", "T" + id, new Vector2(0, 100), new Vector2(150, 28), 20,
                new Color(.82f, .81f, .73f), FontStyles.Bold);
            MakeImage("Lever recess", new Vector2(0, 6), new Vector2(74, 104), new Color(.015f, .018f, .016f));
            lever = MakeImage("Lever", new Vector2(0, -13), new Vector2(46, 68), Off);
            state = MakeText("State", "OFF", new Vector2(0, -74), new Vector2(150, 30), 17,
                new Color(.68f, .66f, .59f), FontStyles.Bold);
        }

        internal void SetState(bool isOn, bool enabled)
        {
            button.interactable = enabled;
            Color color = enabled ? (isOn ? On : Off) : (isOn ? On * .6f : Disabled);
            led.color = color;
            lever.color = color;
            lever.rectTransform.anchoredPosition = new Vector2(0, isOn ? 14 : -13);
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
            text.font = TMP_Settings.defaultFontAsset;
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
