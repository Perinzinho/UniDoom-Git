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
            view.Refresh(mission);
            if (!correct) view.FlashError(id);
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
        static readonly Color Recess = new(.09f, .09f, .075f, 1f);       // #171816
        static readonly Color PanelMetal = new(.16f, .16f, .137f, 1f);   // #292923
        static readonly Color Steel = new(.286f, .267f, .22f, 1f);       // #494438
        static readonly Color Brass = new(.47f, .392f, .278f, 1f);       // #786447
        static readonly Color Paper = new(.71f, .655f, .514f, 1f);      // #B5A783
        static readonly Color Muted = new(.643f, .627f, .541f, 1f);     // #A4A08A
        static readonly Color Alert = new(.467f, .231f, .188f, 1f);     // #773B30
        static readonly Color Amber = new(.639f, .482f, .216f, 1f);     // #A37B37
        static readonly Color Live = new(.396f, .463f, .29f, 1f);       // #65764A
        CanvasGroup group;
        RectTransform frame;
        TMP_Text status, instruction;
        Image plate, meterNeedle, powerLamp, powerHalo;
        Image[] circuitMarkers, circuitHalos;
        LibraryGeneratorSwitchView[] switches;
        Sprite surfaceTexture, tagTexture, meterTexture, switchTexture, leverTexture, lensTexture, bloomTexture;

        internal void Build(Action<int> toggled, Action close)
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            var blocker = GetComponent<Image>();
            blocker.color = new Color(0, 0, 0, .84f);
            blocker.raycastTarget = true;
            group = GetComponent<CanvasGroup>();

            surfaceTexture = Resources.Load<Sprite>("Textures/UI/GeneratorInterface/GeneratorSwitchboardSurface");
            tagTexture = Resources.Load<Sprite>("Textures/UI/GeneratorInterface/GeneratorServiceTag");
            meterTexture = Resources.Load<Sprite>("Textures/UI/GeneratorInterface/GeneratorAnalogMeter");
            switchTexture = Resources.Load<Sprite>("Textures/UI/GeneratorInterface/GeneratorSwitchEscutcheon");
            leverTexture = Resources.Load<Sprite>("Textures/UI/GeneratorInterface/GeneratorToggleLever");
            lensTexture = Resources.Load<Sprite>("Textures/UI/GeneratorInterface/GeneratorIndicatorLens");
            bloomTexture = Resources.Load<Sprite>("Textures/UI/GeneratorInterface/GeneratorIndicatorBloom");

            frame = Rect("Abandoned distribution board", root, Vector2.zero, new Vector2(1260, 830));
            plate = frame.gameObject.AddComponent<Image>();
            plate.sprite = surfaceTexture;
            plate.color = Color.white;
            var outline = frame.gameObject.AddComponent<Outline>();
            outline.effectColor = Brass;
            outline.effectDistance = new Vector2(5, -5);
            var shadow = frame.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .82f);
            shadow.effectDistance = new Vector2(8, -10);
            MakeImage("Upper folded steel lip", frame, new Vector2(0, 401), new Vector2(1232, 12), Brass);
            MakeImage("Lower folded steel lip", frame, new Vector2(0, -401), new Vector2(1232, 12), Steel);
            MakeImage("Left cabinet seam", frame, new Vector2(-615, 0), new Vector2(10, 800), Steel);
            MakeImage("Right cabinet seam", frame, new Vector2(615, 0), new Vector2(10, 800), Steel);
            var fasteners = new[] { new Vector2(-593, 378), new Vector2(593, 378),
                new Vector2(-593, -378), new Vector2(593, -378), new Vector2(0, 378), new Vector2(0, -378) };
            for (int i = 0; i < fasteners.Length; i++)
            {
                Vector2 corner = fasteners[i];
                MakeImage("Recessed cabinet bolt", frame, corner, new Vector2(21 + i % 3, 21 + i % 3), Recess);
                MakeImage("Oxidized bolt head", frame, corner, new Vector2(13 + i % 2, 13 + i % 2), i % 2 == 0 ? Brass : Steel);
                var slot = MakeImage("Bolt slot", frame, corner, new Vector2(9, 2), Recess);
                slot.rectTransform.localRotation = Quaternion.Euler(0, 0, (i * 31 + 17) % 180);
            }

            var serviceTag = SpriteImage("Campus maintenance tag", frame, new Vector2(-421, 330),
                new Vector2(300, 84), tagTexture, Paper);
            serviceTag.rectTransform.localRotation = Quaternion.Euler(0, 0, -.7f);
            Text("Department stamp", frame, "UNIMAR  /  BIBLIOTECA", new Vector2(-421, 350),
                new Vector2(270, 18), 12, new Color(.25f, .23f, .18f), FontStyles.Bold);
            Text("Cabinet title", frame, "QUADRO DE FOR\u00C7A", new Vector2(-421, 322),
                new Vector2(270, 26), 19, new Color(.18f, .18f, .15f), FontStyles.Bold);
            Text("Cabinet serial", frame, "MANUT. EL\u00C9TRICA  /  1987", new Vector2(-421, 299),
                new Vector2(270, 15), 9, new Color(.34f, .31f, .24f), FontStyles.Normal);

            SpriteImage("Analogue cycle meter", frame, new Vector2(-36, 324), new Vector2(142, 142), meterTexture, Color.white);
            meterNeedle = MakeImage("Meter needle", frame, new Vector2(-36, 324), new Vector2(4, 54), new Color(.12f, .12f, .10f));
            meterNeedle.rectTransform.pivot = new Vector2(.5f, .08f);
            meterNeedle.rectTransform.localRotation = Quaternion.Euler(0, 0, 135);
            MakeImage("Needle spindle", frame, new Vector2(-36, 324), new Vector2(11, 11), Brass);

            SpriteImage("Mains lamp escutcheon", frame, new Vector2(443, 331), new Vector2(62, 62), switchTexture, Color.white);
            powerHalo = SpriteImage("Mains lamp bloom", frame, new Vector2(443, 331), new Vector2(48, 48), bloomTexture, Amber);
            powerLamp = SpriteImage("Mains indicator lens", frame, new Vector2(443, 331), new Vector2(23, 23), lensTexture, Amber);
            var mainsTag = SpriteImage("Mains identification tag", frame, new Vector2(526, 331),
                new Vector2(112, 28), tagTexture, Paper);
            mainsTag.rectTransform.localRotation = Quaternion.Euler(0, 0, .6f);
            Text("Mains label", frame, "REDE / OK", new Vector2(526, 331), new Vector2(108, 22),
                12, new Color(.22f, .21f, .17f), FontStyles.Bold);

            var progressStrip = MakeImage("Progress service strip", frame, new Vector2(0, 238),
                new Vector2(920, 47), Paper);
            progressStrip.sprite = tagTexture;
            progressStrip.preserveAspect = false;
            status = Text("Circuit status", frame, "", new Vector2(0, 246), new Vector2(880, 23), 17, Amber, FontStyles.Bold);
            instruction = Text("Operator instruction", frame, "", new Vector2(0, 222),
                new Vector2(880, 20), 12, new Color(.30f, .28f, .22f), FontStyles.Normal);

            Text("Cycle legend", frame, "CICLOS", new Vector2(-489, 158), new Vector2(86, 18), 11, Muted, FontStyles.Bold);
            MakeImage("Copper run shadow", frame, new Vector2(-7, 158), new Vector2(772, 12), Recess);
            MakeImage("Copper run", frame, new Vector2(-7, 158), new Vector2(760, 4), Brass);

            switches = new LibraryGeneratorSwitchView[7];
            circuitMarkers = new Image[7];
            circuitHalos = new Image[7];
            float[] progressX = { -354, -236, -118, 0, 118, 236, 354 };
            for (int i = 0; i < circuitMarkers.Length; i++)
            {
                Vector2 pos = new(progressX[i], 158);
                SpriteImage("Lamp socket " + (i + 1), frame, pos, new Vector2(34, 34), switchTexture, Recess);
                circuitHalos[i] = SpriteImage("Sequence lamp glow " + (i + 1), frame, pos,
                    new Vector2(42, 42), bloomTexture, Live);
                circuitMarkers[i] = SpriteImage("Sequence lens " + (i + 1), frame, pos,
                    new Vector2(22, 22), lensTexture, Alert);
                Text("Sequence mark " + (i + 1), frame, (i + 1).ToString("00"),
                    new Vector2(progressX[i], 130), new Vector2(32, 14), 9, Muted, FontStyles.Bold);
            }

            // Three upper terminals and a wider, uneven lower service bank.
            float[] terminalX = { -350, 12, 365, -472, -158, 166, 474 };
            float[] terminalY = { -12, -28, -5, -214, -192, -226, -205 };
            for (int i = 0; i < switches.Length; i++)
                switches[i] = CreateSwitch(i + 1, new Vector2(terminalX[i], terminalY[i]), toggled,
                    tagTexture, switchTexture, leverTexture, lensTexture, bloomTexture);

            MakeImage("Lower instruction reveal", frame, new Vector2(0, -354), new Vector2(1048, 44), PanelMetal);
            Text("Mouse engraving", frame, "CLIQUE  /  ACIONAR TERMINAL", new Vector2(-215, -354),
                new Vector2(430, 22), 12, Muted, FontStyles.Bold);
            Text("Exit engraving", frame, "ESC  /  SAIR", new Vector2(404, -354),
                new Vector2(170, 22), 12, Paper, FontStyles.Bold);
            var closeButton = MakeImage("Mechanical panel release", frame, new Vector2(504, -354), new Vector2(48, 38), Steel);
            closeButton.raycastTarget = true;
            var button = closeButton.gameObject.AddComponent<Button>();
            button.targetGraphic = closeButton;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.03f, .82f, 1);
            colors.pressedColor = new Color(.72f, .58f, .36f, 1);
            button.colors = colors;
            button.onClick.AddListener(() => close());
            MakeImage("Release lever", closeButton.rectTransform, Vector2.zero, new Vector2(11, 27), Brass);
        }

        LibraryGeneratorSwitchView CreateSwitch(int id, Vector2 position, Action<int> toggled,
            Sprite tag, Sprite escutcheon, Sprite lever, Sprite lens, Sprite bloom)
        {
            var rect = Rect("Terminal " + id, frame, position, new Vector2(136, 204));
            rect.localRotation = Quaternion.Euler(0, 0, (id % 3 - 1) * .55f);
            var item = rect.gameObject.AddComponent<LibraryGeneratorSwitchView>();
            item.Build(id, toggled, tag, escutcheon, lever, lens, bloom);
            return item;
        }


        internal void Refresh(LibraryMissionController mission)
        {
            if (mission == null) return;
            bool solved = LibraryMissionProgress.Stage >= LibraryMissionStage.PuzzleSolved;
            bool enabled = LibraryMissionProgress.Stage == LibraryMissionStage.FusesInstalled;

            for (int i = 0; i < switches.Length; i++)
                switches[i].SetState(mission.PanelTerminalOn(i + 1), enabled, solved);

            for (int i = 0; i < circuitMarkers.Length; i++)
            {
                bool energized = solved || i < LibraryMissionProgress.SequenceStep;
                circuitMarkers[i].color = energized ? Live : Alert;
                circuitHalos[i].color = energized
                    ? new Color(Live.r, Live.g, Live.b, .52f)
                    : new Color(Alert.r, Alert.g, Alert.b, .08f);
            }

            bool complete = LibraryMissionProgress.IsLibraryGeneratorPowered;
            powerLamp.color = complete ? Live : Amber;
            powerHalo.color = complete
                ? new Color(Live.r, Live.g, Live.b, .56f)
                : new Color(Amber.r, Amber.g, Amber.b, .32f);
            plate.color = Color.white;
            status.color = complete ? Live : Amber;

            float progress = solved ? 1f : Mathf.Clamp01(LibraryMissionProgress.SequenceStep / 7f);
            meterNeedle.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(135f, -135f, progress));
            status.text = complete ? "ENERGIA RESTAURADA" :
                solved ? "CIRCUITO EST\u00C1VEL / GERADOR LIGADO" :
                string.Format("SEQU\u00CANCIA DE ATIVA\u00C7\u00C3O {0:00} / 07", LibraryMissionProgress.SequenceStep);
            instruction.text = solved ? "GERADOR RESTAURADO / RETORNANDO AO JOGO" :
                "Acione os terminais e estabilize a corrente.";
        }

        internal void FlashError(int id)
        {
            if (id > 0 && id <= switches.Length) switches[id - 1].FlashError();
            status.text = "FALHA DE FASE / SEQU\u00CANCIA REINICIADA";
            status.color = Alert;
        }

        internal void SetPresentation(float alpha, float scale, bool interactive)
        {
            group.alpha = alpha;
            group.interactable = group.blocksRaycasts = interactive;
            var viewport = (RectTransform)transform;
            float fit = Mathf.Min(1f, viewport.rect.width / frame.sizeDelta.x,
                viewport.rect.height / frame.sizeDelta.y);
            frame.localScale = Vector3.one * (scale * fit);
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

        static Image SpriteImage(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color color)
        {
            var image = MakeImage(name, parent, position, size, color);
            image.sprite = sprite;
            image.preserveAspect = true;
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
        static readonly Color Off = new(.467f, .231f, .188f, 1f);
        static readonly Color On = new(.639f, .482f, .216f, 1f);
        static readonly Color Live = new(.396f, .463f, .29f, 1f);
        static readonly Color Disabled = new(.13f, .14f, .12f, 1f);
        int id;
        Button button;
        Image background, led, ledHalo, lever, escutcheon;
        TMP_Text state;
        Outline outline;
        Coroutine flash, mechanicalMove;
        bool hasVisualState, previousOn;

        internal void Build(int terminalId, Action<int> toggled, Sprite tag, Sprite flange, Sprite arm, Sprite lens, Sprite bloom)
        {
            id = terminalId;
            background = gameObject.AddComponent<Image>();
            background.color = new Color(0, 0, 0, 0);
            background.raycastTarget = true;
            button = gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => toggled(id));

            var terminalTag = MakeImage("Individual terminal tag", new Vector2(0, 86), new Vector2(112, 24), tag, Color.white);
            terminalTag.rectTransform.localRotation = Quaternion.Euler(0, 0, (id % 2 == 0 ? -.8f : .6f));
            MakeText("Terminal identification", "TERMINAL " + id.ToString("00"), new Vector2(0, 86), new Vector2(108, 19), 9,
                new Color(.20f, .19f, .16f), FontStyles.Bold);

            escutcheon = MakeImage("Worn switch escutcheon", Vector2.zero, new Vector2(112, 112), flange, Color.white);
            escutcheon.rectTransform.localRotation = Quaternion.Euler(0, 0, (id % 3 - 1) * .9f);
            outline = escutcheon.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.20f, .21f, .18f, 1f);
            outline.effectDistance = new Vector2(2, -2);
            MakeImage("Recessed lever socket", new Vector2(0, -7), new Vector2(33, 35), null, RecessColor);
            MakeImage("Switch pivot", new Vector2(0, -7), new Vector2(16, 16), null, BrassColor);
            lever = MakeImage("Bakelite toggle arm", new Vector2(0, -14), new Vector2(42, 94), arm, Color.white);
            lever.rectTransform.pivot = new Vector2(.5f, .1f);
            lever.rectTransform.localRotation = Quaternion.Euler(0, 0, -23f);

            MakeImage("Pilot lamp socket", new Vector2(56, -50), new Vector2(23, 23), null, RecessColor);
            ledHalo = MakeImage("Pilot lamp bloom", new Vector2(56, -50), new Vector2(38, 38), bloom, On);
            led = MakeImage("Coloured glass pilot lamp", new Vector2(56, -50), new Vector2(19, 19), lens, Off);
            state = MakeText("Switch state engraving", "ABERTO", new Vector2(-16, -78), new Vector2(83, 16), 9,
                new Color(.65f, .62f, .53f), FontStyles.Bold);
            MakeText("Switch interaction engraving", "ACIONAR", new Vector2(55, -78), new Vector2(54, 14), 8,
                new Color(.43f, .43f, .37f), FontStyles.Bold);
        }

        internal void SetState(bool isOn, bool enabled, bool solved)
        {
            button.interactable = enabled;
            Color signal = solved ? Live : isOn ? On : enabled ? Off : Disabled;
            led.color = signal;
            ledHalo.color = new Color(signal.r, signal.g, signal.b, isOn ? .56f : enabled ? .10f : .025f);
            Quaternion targetRotation = Quaternion.Euler(0, 0, isOn ? 23f : -23f);
            if (!hasVisualState) lever.rectTransform.localRotation = targetRotation;
            else if (previousOn != isOn)
            {
                if (mechanicalMove != null) StopCoroutine(mechanicalMove);
                mechanicalMove = StartCoroutine(ThrowLever(targetRotation));
            }
            previousOn = isOn;
            hasVisualState = true;
            lever.color = enabled ? Color.white : new Color(.54f, .52f, .45f, 1f);
            state.text = solved && isOn ? "ENERGIZADO" : isOn ? "FECHADO" : enabled ? "ABERTO" : "BLOQUEADO";
            state.color = solved && isOn ? Live : enabled ? new Color(.643f, .627f, .541f, 1f) : new Color(.42f, .43f, .39f);
            if (!enabled) SetHighlight(false);
        }

        static Color RecessColor => new(.09f, .09f, .075f, 1f);
        static Color BrassColor => new(.47f, .392f, .278f, 1f);

        IEnumerator ThrowLever(Quaternion target)
        {
            Quaternion start = lever.rectTransform.localRotation;
            for (float t = 0; t < .12f; t += Time.unscaledDeltaTime)
            {
                float eased = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .12f));
                lever.rectTransform.localRotation = Quaternion.Slerp(start, target, eased);
                yield return null;
            }
            lever.rectTransform.localRotation = target;
            mechanicalMove = null;
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
            return MakeImage(name, position, size, null, color);
        }

        Image MakeImage(string name, Vector2 position, Vector2 size, Sprite sprite, Color color)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            image.preserveAspect = sprite != null;
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
