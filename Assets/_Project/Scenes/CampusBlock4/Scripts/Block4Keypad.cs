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
                inventory.gameObject.SetActive(gameplay && Block4PuzzleProgress.HasLibraryAccessKey);
                inventory.text = Time.unscaledTime < toastUntil ? "CHAVE COLETADA\nBiblioteca • Bloco 4 • guardada no inventário" : "CHAVE DA BIBLIOTECA • BLOCO 4  /  NO INVENTÁRIO";
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
                card.localScale = Vector3.one * Mathf.Min(1, Mathf.Min(size.x * sx / 690, size.y * sy / 930));
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
            feedback.text = "Digite a senha.";
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
            feedback.text = "";
            feedback.color = MenuStyle.Muted;
            RenderEntry();
        }

        public void Backspace()
        {
            if (!IsOpen || closing) return;
            replaceEntry = false;
            if (entry.Length > 0) entry = entry.Substring(0, entry.Length - 1);
            feedback.text = "";
            feedback.color = MenuStyle.Muted;
            RenderEntry();
        }

        public void Clear()
        {
            if (!IsOpen || closing) return;
            entry = ""; replaceEntry = false; RenderEntry();
            feedback.text = "";
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
                feedback.text = "Senha incorreta";
                feedback.color = new Color(.94f, .58f, .34f);
                replaceEntry = true;
                return;
            }
            if (GameManager.SessionPlayer == null || !lockedDoor.TryUnlock(GameManager.SessionPlayer.GetComponent<PlayerPickupKey>())) return;
            Block4PuzzleProgress.UnlockDoor();
            RefreshWorld();
            closing = true;
            foreach (var button in keys) button.interactable = false;
            feedback.text = "ACESSO LIBERADO";
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
            colors.normalColor = new Color(.27f, .285f, .28f);
            colors.highlightedColor = colors.selectedColor = new Color(.42f, .45f, .40f);
            colors.pressedColor = new Color(.13f, .16f, .13f);
            colors.disabledColor = new Color(.12f, .13f, .12f);
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
            card = MenuStyle.Rect("Access terminal", panel.transform, 0, 0, 626, 870);
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f);
            var background = card.gameObject.AddComponent<Image>(); background.color = new Color(.075f, .085f, .08f);
            MenuStyle.Image("Metal housing bevel", card, 5, 5, 616, 860, new Color(.34f, .36f, .33f));
            MenuStyle.Image("Brushed metal face", card, 12, 12, 602, 846, new Color(.20f, .22f, .205f));
            foreach(float x in new[]{24f,580f})foreach(float y in new[]{24f,824f})
            {
                MenuStyle.Image("Mounting screw",card,x,y,18,18,new Color(.105f,.115f,.11f));
                MenuStyle.Image("Screw slot",card,x+3,y+8,12,2,new Color(.37f,.39f,.36f));
            }
            MenuStyle.Image("Serial plate",card,64,34,498,46,new Color(.075f,.08f,.075f));
            var serial=MenuStyle.Text("Location",card,"CONTROLE DE ACESSO  •  423",76,42,474,28,22,MenuStyle.Paper);serial.alignment=TextAlignmentOptions.Center;
            MenuStyle.Image("Power indicator",card,540,103,12,12,new Color(.47f,.66f,.34f));
            MenuStyle.Text("Device label",card,"TECLADO NUMÉRICO",64,98,420,28,20,new Color(.65f,.68f,.62f));
            MenuStyle.Image("LCD recessed frame", card, 52, 143, 522, 166, new Color(.035f, .045f, .035f));
            MenuStyle.Image("LCD glass", card, 62, 153, 502, 146, new Color(.12f, .18f, .12f));
            MenuStyle.Text("LCD header",card,"SALA 423     /     ACESSO RESTRITO",78,165,470,24,18,new Color(.55f,.68f,.45f));
            digits = MenuStyle.Text("Five digits", card, "", 72, 191, 482, 68, 48, new Color(.74f,.86f,.59f)); digits.alignment = TextAlignmentOptions.Center;
            feedback = MenuStyle.Text("Result", card, "", 76, 266, 474, 26, 21, new Color(.63f,.75f,.53f));feedback.alignment=TextAlignmentOptions.Center;
            for(int i=0;i<4;i++)MenuStyle.Image("Speaker slot",card,217,329+i*7,192,2,new Color(.075f,.085f,.075f));
            keys = new Button[14];
            for (int i = 0; i < 9; i++)
            {
                int digit = i + 1;
                MenuStyle.Image("Key socket",card,57+(i%3)*176,379+(i/3)*82,160,76,new Color(.06f,.07f,.06f));
                keys[i] = Control(card, "Digit " + digit, digit.ToString(), 60 + (i % 3) * 176, 380 + (i / 3) * 82, 154, 68,
                    () => { if (!KeyboardSubmit) EnterDigit(digit); }, 35);
            }
            keys[9] = Control(card, "Backspace", "APAGAR", 60, 626, 154, 68, () => { if (!KeyboardSubmit) Backspace(); }, 22);
            keys[10] = Control(card, "Digit 0", "0", 236, 626, 154, 68, () => { if (!KeyboardSubmit) EnterDigit(0); }, 35);
            keys[11] = Control(card, "Clear", "LIMPAR", 412, 626, 154, 68, () => { if (!KeyboardSubmit) Clear(); }, 22);
            keys[12] = Control(card, "Confirm", "CONFIRMAR", 60, 716, 506, 58, Submit, 26);
            keys[13] = Control(card, "Close", "AFASTAR-SE  /  ESC", 60, 793, 506, 32, Cancel, 19);
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
