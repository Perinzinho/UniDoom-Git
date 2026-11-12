using System.Text.RegularExpressions;
using UnityEngine;

namespace UniDoom.UI
{
    [CreateAssetMenu(menuName = "UniDoom/Menu/Credits")]
    public sealed class CreditsSettings : ScriptableObject
    {
        public const float RequiredScrollSpeed = 42;
        public const string RequiredMusicResource = "Audio/Credits/HiddenKingTheme";
        [SerializeField] TextAsset content;
        [SerializeField] AudioClip music;
        [SerializeField, Range(0, 1)] float musicVolume = .6f;
        [SerializeField, Min(1)] float scrollSpeed = RequiredScrollSpeed;
        [SerializeField, Min(0)] float startDelay = 0;
        [SerializeField, Min(0)] float endHold = 3;
        public TextAsset Content => content;
        public AudioClip Music
        {
            get
            {
                if (!music || music.name != "HiddenKingTheme")
                    music = Resources.Load<AudioClip>(RequiredMusicResource);
                return music;
            }
        }
        public float MusicVolume => musicVolume;
        public float ScrollSpeed => Mathf.Max(1, scrollSpeed);
        public float StartDelay => startDelay;
        public float EndHold => endHold;

        void OnEnable() => scrollSpeed = RequiredScrollSpeed;
        void OnValidate() => scrollSpeed = RequiredScrollSpeed;

        public static float RollOffset(float elapsed, float contentHeight, float viewportHeight,
            float speed, float startDelay)
        {
            return Mathf.Min(Mathf.Max(0, contentHeight), -Mathf.Max(0, viewportHeight) +
                Mathf.Max(0, elapsed - startDelay) * Mathf.Max(1, speed));
        }

        public static string Format(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown)) return "UNIDOOM";
            string text = markdown.Replace("\r", "");
            // Treat source text as text; only our generated TMP tags are allowed.
            text = text.Replace("<", "&lt;").Replace(">", "&gt;");
            text = Regex.Replace(text, @"\[([^\]]+)\]\((https?://[^\s)]+)\)", "$1");
            text = Regex.Replace(text, @"(?m)^### (.+)$", "<size=110%><b>$1</b></size>");
            text = Regex.Replace(text, @"(?m)^## (.+)$", "<size=135%><b>$1</b></size>");
            text = Regex.Replace(text, @"(?m)^# (.+)$", "<size=165%><b>$1</b></size>");
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<b>$1</b>");
            text = Regex.Replace(text, @"(?m)^- ", "");
            return text.Trim();
        }
    }
}
