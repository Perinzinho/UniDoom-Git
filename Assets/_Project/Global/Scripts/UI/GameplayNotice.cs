using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniDoom.UI
{
    /// <summary>Transient feedback using the director's room hint presentation.</summary>
    public sealed class GameplayNotice : MonoBehaviour
    {
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

        // Same font, position, color and shadow as DirectorSecretPassage.
        public static void Draw(string text)
        {
            if (string.IsNullOrEmpty(text) || Blocked) return;
            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            style.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 52f), 14, 24);
            var rect = new Rect(Screen.width * .23f, Screen.height * .79f, Screen.width * .54f, 64);
            rect.height = Mathf.Max(rect.height, style.CalcHeight(new GUIContent(text), rect.width));
            Color previous = GUI.color;
            GUI.color = new Color(0, 0, 0, .85f);
            GUI.Label(new Rect(rect.x + 1, rect.y + 2, rect.width, rect.height), text, style);
            GUI.color = new Color(.80f, .77f, .65f);
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }
    }
}
