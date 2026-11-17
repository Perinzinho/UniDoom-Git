using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniDoom.UI
{
    /// <summary>Shared presentation for in-game notices and interaction prompts.</summary>
    public sealed class GameplayNotice : MonoBehaviour
    {
        public const string LockedDoorKeyMessage = "Porta trancada. Você precisa de uma chave para abrir.";
        public const string LockedDoorMessage = "Porta trancada.";
        static GameplayNotice instance;
        static GUIStyle style;
        string message;
        float remaining;

        public static bool IsShowing => instance && instance.remaining > 0 && !Blocked;
        static bool Blocked => Time.timeScale <= 0 || GameMenus.BlocksGameplay ||
            DeathScreen.IsVisible || DoorReloadScreen.IsReloading || MouseSensitivityMenu.IsOpen;

        public static void Show(string text, float duration = 4.5f)
        {
            if (string.IsNullOrWhiteSpace(text) || !GameMenus.Instance) return;
            if (!instance) instance = GameMenus.Instance.gameObject.AddComponent<GameplayNotice>();
            // Current feedback replaces stale counts instead of queuing obsolete instructions.
            instance.message = text;
            instance.remaining = Mathf.Max(2, duration);
        }

        public static void ShowKeyAcquired(string keyName, string use = null)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return;
            string message = "Chave obtida: " + keyName + ".";
            if (!string.IsNullOrWhiteSpace(use)) message += "\n" + use;
            Show(message, 7.5f);
        }

        public static void ShowWhenGameplayReady(MonoBehaviour owner, string text, float duration = 7.5f)
        {
            if (owner && !string.IsNullOrWhiteSpace(text)) owner.StartCoroutine(WaitForGameplay(owner, text, duration));
        }

        static IEnumerator WaitForGameplay(MonoBehaviour owner, string text, float duration)
        {
            while (owner && (owner.gameObject.scene != SceneManager.GetActiveScene() ||
                GameManager.SessionPlayer == null || !GameMenus.Instance || Blocked))
                yield return null;
            if (owner) Show(text, duration);
        }

        void OnEnable() { instance = this; SceneManager.activeSceneChanged += SceneChanged; }
        void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneChanged;
            if (instance == this) instance = null;
            remaining = 0;
        }
        void SceneChanged(Scene previous, Scene next) => remaining = 0;
        void Update()
        {
            if (DeathScreen.IsVisible || DoorReloadScreen.IsReloading) { remaining = 0; return; }
            // A modal or pause must not consume the time available to read a message.
            if (!Blocked) remaining = Mathf.Max(0, remaining - Time.unscaledDeltaTime);
        }
        void OnGUI() { if (IsShowing) Draw(message); }

        // Plain text only: a layered dark shadow keeps it legible without covering the view.
        public static void Draw(string text)
        {
            if (string.IsNullOrEmpty(text) || Blocked) return;
            style ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontStyle = FontStyle.Bold
            };
            // Scale from an 18 px reference at 1080p; width also caps very narrow windows.
            float reference = Mathf.Min(Screen.height, Screen.width * .5625f);
            int size = Mathf.Clamp(Mathf.RoundToInt(18f * reference / 1080f), 13, 28);
            style.fontSize = size;
            float unit = size / 18f;
            var content = new GUIContent(text);
            float width = Mathf.Min(Screen.width * .72f,
                Mathf.Max(Screen.width * .34f, style.CalcSize(content).x + 32f * unit));
            var rect = new Rect((Screen.width - width) * .5f, 0f,
                width, style.CalcHeight(content, width));
            // Anchored near the bottom edge: long messages grow upward, leaving the play area free.
            rect.y = Mathf.Max(16f, Screen.height - Mathf.Max(24f * unit, Screen.height * .05f) - rect.height);
            Color original = style.normal.textColor;
            Shadow(text, rect, unit, 0f, .42f);
            Shadow(text, rect, -unit, 0f, .42f);
            Shadow(text, rect, 0f, unit, .42f);
            Shadow(text, rect, 0f, -unit, .42f);
            Shadow(text, rect, 0f, 2f * unit, .78f);
            Shadow(text, rect, 0f, 3.5f * unit, .38f);
            style.normal.textColor = Color.white;
            GUI.Label(rect, text, style);
            style.normal.textColor = original;
        }
        static void Shadow(string text, Rect rect, float dx, float dy, float alpha)
        {
            style.normal.textColor = new Color(0f, 0f, 0f, alpha);
            GUI.Label(new Rect(rect.x + dx, rect.y + dy, rect.width, rect.height), text, style);
        }
    }
}
