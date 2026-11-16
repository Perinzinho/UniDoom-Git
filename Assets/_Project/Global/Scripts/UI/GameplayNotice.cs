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

        // Keep messages readable over both dark rooms and bright scenery.
        public static void Draw(string text)
        {
            if (string.IsNullOrEmpty(text) || Blocked) return;
            style ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = Color.white;
            style.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 44f), 18, 30);
            var content = new GUIContent(text);
            float width = Mathf.Min(Screen.width * .72f,
                Mathf.Max(Screen.width * .34f, style.CalcSize(content).x + 48f));
            var rect = new Rect((Screen.width - width) * .5f, Screen.height * .78f,
                width, Mathf.Max(54f, style.CalcHeight(content, width) + 12f));
            rect.y = Mathf.Max(16f, Mathf.Min(rect.y, Screen.height - rect.height - 24f));
            var panel = new Rect(rect.x - 18f, rect.y - 8f, rect.width + 36f, rect.height + 16f);
            Color previous = GUI.color;
<<<<<<< HEAD
            GUI.color = new Color(0, 0, 0, .92f);
            GUI.Label(new Rect(rect.x + 1, rect.y + 2, rect.width, rect.height), text, style);
            GUI.color = new Color(.98f, .97f, .90f);
=======
            GUI.color = new Color(.025f, .03f, .035f, .87f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = new Color(.88f, .78f, .56f, .9f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 2f), Texture2D.whiteTexture);
            GUI.color = new Color(0f, 0f, 0f, .9f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 2f, rect.width, rect.height), text, style);
            GUI.color = Color.white;
>>>>>>> d6473ee12dcc529d2ba3c3251543af3664205c03
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }
    }
}
