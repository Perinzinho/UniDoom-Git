using System.Collections;
using System.Text;
using TMPro;
using UniDoom.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UniDoom.CampusBlock4
{
    [DisallowMultipleComponent]
    public sealed class Block4Keypad : MonoBehaviour
    {
        [SerializeField] Block4PuzzleDefinition definition;
        [SerializeField] OpenDoorWithKey lockedDoor;
        [SerializeField] Transform interactionPoint;
        [SerializeField] TMP_Text worldStatus;
        [SerializeField, Min(.5f)] float interactionDistance = 2.4f;
        GameObject panel, hud;
        RectTransform card;
        TMP_Text digits, feedback, prompt, inventory;
        Button[] keys;
        string entry = "";
        bool closing, replaceEntry;
        int openedFrame;
        float toastUntil;
        public bool IsOpen { get; private set; }
        public string Entry => entry;
        public OpenDoorWithKey LockedDoor => lockedDoor;
        public Block4PuzzleDefinition Definition => definition;

        void Start()
        {
            if (definition == null || !definition.IsValid || lockedDoor == null || interactionPoint == null)
            {
                Debug.LogError("Bloco 4: configuração incompleta do enigma.", this);
                enabled = false;
                return;
            }
            BuildHUD();
            RefreshWorld();
        }

        void Update()
        {
            if (Block4PuzzleProgress.DoorUnlocked && !lockedDoor.IsUnlocked && GameManager.SessionPlayer != null)
                lockedDoor.TryUnlock(GameManager.SessionPlayer.GetComponent<PlayerPickupKey>());
            RefreshWorld();
            bool gameplay = !GameMenus.BlocksGameplay && Time.timeScale > 0 && !DeathScreen.IsVisible && !DoorReloadScreen.IsReloading;
            if (prompt != null) prompt.gameObject.SetActive(gameplay && !Block4PuzzleProgress.DoorUnlocked && CanInteract());
            if (inventory != null)
            {
                inventory.gameObject.SetActive(gameplay && Block4PuzzleProgress.HasTeacherCellKey);
                inventory.text = Time.unscaledTime < toastUntil ? "CHAVE COLETADA\nChave da cela • guardada no inventário" : "CHAVE DA CELA  /  NO INVENTÁRIO";
            }
            if (!IsOpen || closing || Time.frameCount == openedFrame) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                for (int i = 0; i <= 9; i++)
                    if (keyboard[i == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + i - 1)].wasPressedThisFrame ||
                        keyboard[(Key)((int)Key.Numpad0 + i)].wasPressedThisFrame) EnterDigit(i);
                if (keyboard.backspaceKey.wasPressedThisFrame || keyboard.deleteKey.wasPressedThisFrame) Backspace();
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) Submit();
                if (keyboard.escapeKey.wasPressedThisFrame) Cancel();
            }
            if (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame)) Cancel();
        }

        void LateUpdate()
        {
            if (IsOpen && card != null)
            {
                var size = ((RectTransform)GameMenus.Instance.transform).rect.size;
                var safe = Screen.safeArea;
                float sx = safe.width / Mathf.Max(1, Screen.width), sy = safe.height / Mathf.Max(1, Screen.height);
                card.localScale = Vector3.one * Mathf.Min(1, Mathf.Min(size.x * sx / 690, size.y * sy / 1010));
                card.anchoredPosition = new Vector2((safe.center.x / Mathf.Max(1, Screen.width) - .5f) * size.x,
                    (safe.center.y / Mathf.Max(1, Screen.height) - .5f) * size.y);
            }
            if (!IsOpen && !closing && !Block4PuzzleProgress.DoorUnlocked && !GameMenus.BlocksGameplay &&
                ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame))) TryInteract();
        }

        public bool CanInteract()
        {
            var player = GameManager.SessionPlayer;
            if (player == null || interactionPoint == null || (player.TryGetComponent<PlayerLife>(out var life) && life.IsDead)) return false;
            Vector3 offset = player.transform.position - interactionPoint.position;
            offset.y = 0;
            if (offset.sqrMagnitude > interactionDistance * interactionDistance) return false;
            // Read from the player's head: a window or wall cannot operate a keypad from the other side.
            Vector3 from = player.transform.position + Vector3.up * .45f;
            if (Physics.Linecast(from, interactionPoint.position, out var hit, ~0, QueryTriggerInteraction.Ignore))
                return hit.collider.transform.IsChildOf(interactionPoint);
            return true;
        }

        public bool TryInteract()
        {
            if (IsOpen || closing || Block4PuzzleProgress.DoorUnlocked || !CanInteract() || definition == null || !definition.IsValid) return false;
            if (GameMenus.Instance == null) return false;
            if (panel == null) BuildPanel();
            entry = ""; replaceEntry = false; closing = false;
            foreach (var button in keys) button.interactable = true;
            RenderEntry();
            feedback.text = "Encontre as marcas junto às cinco fotografias.\nA ordem está junto ao quadro da turma.";
            feedback.color = MenuStyle.Muted;
            IsOpen = GameMenus.Instance != null && GameMenus.Instance.TryOpenInteraction(panel, this, OnClosed);
            if (IsOpen && Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
                EventSystem.current?.SetSelectedGameObject(keys[0].gameObject);
            openedFrame = Time.frameCount;
            return IsOpen;
        }

        public void EnterDigit(int digit)
        {
            if (!IsOpen || closing || digit < 0 || digit > 9) return;
            if (replaceEntry) { entry = ""; replaceEntry = false; }
            if (entry.Length >= definition.Length) return;
            entry += (char)('0' + digit);
            feedback.text = "CONFIRMAR para verificar a sequência.";
            feedback.color = MenuStyle.Muted;
            RenderEntry();
        }

        public void Backspace()
        {
            if (!IsOpen || closing) return;
            replaceEntry = false;
            if (entry.Length > 0) entry = entry.Substring(0, entry.Length - 1);
            feedback.text = "Digite os cinco números na ordem dos símbolos.";
            feedback.color = MenuStyle.Muted;
            RenderEntry();
        }

        public void Clear()
        {
            if (!IsOpen || closing) return;
            entry = ""; replaceEntry = false; RenderEntry();
            feedback.text = "Sequência limpa. Digite novamente.";
            feedback.color = MenuStyle.Muted;
        }

        public void Submit()
        {
            if (!IsOpen || closing) return;
            if (entry.Length != definition.Length)
            {
                feedback.text = "Faltam números. A sequência tem cinco dígitos.";
                feedback.color = MenuStyle.Paper;
                return;
            }
            if (!definition.Accepts(entry))
            {
                feedback.text = "SEQUÊNCIA INCORRETA\nConfira as marcas e a ordem junto ao quadro.";
                feedback.color = new Color(.94f, .58f, .34f);
                replaceEntry = true;
                return;
            }
            if (GameManager.SessionPlayer == null || !lockedDoor.TryUnlock(GameManager.SessionPlayer.GetComponent<PlayerPickupKey>())) return;
            Block4PuzzleProgress.UnlockDoor();
            RefreshWorld();
            closing = true;
            foreach (var button in keys) button.interactable = false;
            feedback.text = "ACESSO LIBERADO\nA chave está dentro da sala.";
            feedback.color = new Color(.70f, .83f, .62f);
            StartCoroutine(CloseAfterInput(.85f));
        }

        public void Cancel()
        {
            if (!IsOpen || closing) return;
            closing = true;
            StartCoroutine(CloseAfterInput(0));
        }

        IEnumerator CloseAfterInput(float delay)
        {
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            // Keep gameplay frozen for the input frame (including mouse clicks / Escape).
            yield return null;
            GameMenus.Instance?.CloseInteractionOwnedBy(this);
        }

        void OnClosed() { IsOpen = false; closing = false; entry = ""; }
        void OnDisable()
        {
            StopAllCoroutines();
            GameMenus.Instance?.CloseInteractionOwnedBy(this);
            if (hud != null) hud.SetActive(false);
        }
        void OnDestroy()
        {
            GameMenus.Instance?.CloseInteractionOwnedBy(this);
            if (panel != null) Destroy(panel);
            if (hud != null) Destroy(hud);
        }
        public void ShowKeyCollected() => toastUntil = Time.unscaledTime + 6;
        void RefreshWorld()
        {
            if (worldStatus != null) worldStatus.text = Block4PuzzleProgress.DoorUnlocked ? "LIBERADO" : "TRANCADO";
        }
        void RenderEntry()
        {
            var value = new StringBuilder();
            for (int i = 0; i < definition.Length; i++) { if (i > 0) value.Append("   "); value.Append(i < entry.Length ? entry[i] : '_'); }
            digits.text = value.ToString();
        }

        Button Control(Transform parent, string name, string label, float x, float y, float width, float height, UnityEngine.Events.UnityAction action, int size = 32)
        {
            var face = MenuStyle.Image(name, parent, x, y, width, height, Color.white);
            face.raycastTarget = true;
            var button = face.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            var colors = button.colors;
            colors.normalColor = new Color(.105f, .108f, .103f);
            colors.highlightedColor = colors.selectedColor = new Color(.29f, .24f, .15f);
            colors.pressedColor = new Color(.40f, .30f, .15f);
            colors.disabledColor = new Color(.065f, .067f, .062f);
            colors.fadeDuration = .08f;
            button.colors = colors;
            var text = MenuStyle.Text("Label", face.transform, label, 0, 0, width, height, size, MenuStyle.Paper);
            text.alignment = TextAlignmentOptions.Center;
            button.onClick.AddListener(action);
            return button;
        }
        static bool KeyboardSubmit => Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);

        void BuildPanel()
        {
            panel = MenuStyle.Rect("Block 4 access keypad", GameMenus.Instance.transform, 0, 0, 0, 0).gameObject;
            MenuStyle.Stretch((RectTransform)panel.transform);
            var blocker = panel.AddComponent<Image>(); blocker.color = new Color(0, 0, 0, .45f); blocker.raycastTarget = true;
            card = MenuStyle.Rect("Access terminal", panel.transform, 0, 0, 626, 948);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f);
            var background = card.gameObject.AddComponent<Image>(); background.color = new Color(.043f, .047f, .044f);
            MenuStyle.Image("Brass header", card, 0, 0, 626, 5, MenuStyle.Amber);
            MenuStyle.Text("Location", card, "BLOCO 4  /  SALA 423", 42, 28, 542, 34, 24, MenuStyle.Amber);
            MenuStyle.Text("Title", card, "ACESSO RESTRITO", 42, 68, 542, 62, 43, MenuStyle.Paper).fontStyle = FontStyles.Bold;
            MenuStyle.Text("Instructions", card, "Cada símbolo corresponde a um número.\nDigite os números na ordem indicada.", 42, 138, 542, 64, 25, MenuStyle.Paper);
            MenuStyle.Image("Display", card, 42, 228, 542, 96, new Color(.015f, .021f, .018f));
            digits = MenuStyle.Text("Five digits", card, "", 42, 228, 542, 96, 48, MenuStyle.Paper); digits.alignment = TextAlignmentOptions.Center;
            feedback = MenuStyle.Text("Result", card, "", 42, 340, 542, 82, 23, MenuStyle.Muted);
            keys = new Button[14];
            for (int i = 0; i < 9; i++)
            {
                int digit = i + 1;
                keys[i] = Control(card, "Digit " + digit, digit.ToString(), 42 + (i % 3) * 185, 438 + (i / 3) * 82, 172, 70,
                    () => { if (!KeyboardSubmit) EnterDigit(digit); }, 35);
            }
            keys[9] = Control(card, "Backspace", "APAGAR", 42, 684, 172, 70, () => { if (!KeyboardSubmit) Backspace(); }, 23);
            keys[10] = Control(card, "Digit 0", "0", 227, 684, 172, 70, () => { if (!KeyboardSubmit) EnterDigit(0); }, 35);
            keys[11] = Control(card, "Clear", "LIMPAR", 412, 684, 172, 70, () => { if (!KeyboardSubmit) Clear(); }, 23);
            keys[12] = Control(card, "Confirm", "CONFIRMAR", 42, 776, 542, 66, Submit, 29);
            keys[13] = Control(card, "Close", "FECHAR  /  ESC", 42, 858, 542, 50, Cancel, 22);
            // Explicit horizontal / vertical navigation keeps a controller inside this modal.
            for (int i = 0; i < 12; i++)
            {
                var n = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = keys[i / 3 * 3 + (i + 2) % 3],
                    selectOnRight = keys[i / 3 * 3 + (i + 1) % 3], selectOnUp = keys[i < 3 ? 13 : i - 3], selectOnDown = keys[i >= 9 ? 12 : i + 3] };
                keys[i].navigation = n;
            }
            var confirmNav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = keys[10], selectOnDown = keys[13] }; keys[12].navigation = confirmNav;
            keys[13].navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = keys[12], selectOnDown = keys[1] };
            panel.SetActive(false);
        }

        void BuildHUD()
        {
            hud = new GameObject("Block 4 puzzle HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            hud.transform.SetParent(transform, false);
            var canvas = hud.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 9000;
            var scaler = hud.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            prompt = MenuStyle.Text("Keypad interaction", hud.transform, "E / X   USAR TECLADO  •  SALA 423", 0, 0, 1000, 52, 27, MenuStyle.Paper);
            prompt.alignment = TextAlignmentOptions.Center;
            var pr = prompt.rectTransform; pr.anchorMin = pr.anchorMax = pr.pivot = new Vector2(.5f, 0); pr.anchoredPosition = new Vector2(0, 150);
            inventory = MenuStyle.Text("Teacher cell key inventory", hud.transform, "", 0, 0, 1000, 90, 26, MenuStyle.Paper);
            inventory.alignment = TextAlignmentOptions.Center;
            var ir = inventory.rectTransform; ir.anchorMin = ir.anchorMax = ir.pivot = new Vector2(.5f, 1); ir.anchoredPosition = new Vector2(0, -44);
            prompt.gameObject.SetActive(false); inventory.gameObject.SetActive(false);
        }
    }
}
