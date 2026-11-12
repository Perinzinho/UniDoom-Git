using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UniDoom.UI
{
    public sealed partial class GameMenus
    {
        CreditsSettings creditsSettings;
        RectTransform creditsViewport;
        TMP_Text creditsRollText;
        AudioSource creditsMusic;
        Vector2 creditsViewportSize;
        float creditsStartedAt, creditsEndAt = -1;

        void BuildCredits()
        {
            creditsSettings = Resources.Load<CreditsSettings>("Menus/Credits");
            credits.transform.SetParent(canvas.transform, false);
            credits.transform.localScale = Vector3.one;
            MenuStyle.Stretch((RectTransform)credits.transform);
            var black = MenuStyle.Image("Cinema black background", credits.transform, 0, 0, 0, 0, Color.black);
            MenuStyle.Stretch(black.rectTransform);
            black.raycastTarget = true;
            creditsViewport = MenuStyle.Rect("Full-screen credits viewport", credits.transform, 0, 0, 0, 0);
            MenuStyle.Stretch(creditsViewport);
            creditsViewport.gameObject.AddComponent<RectMask2D>();
            creditsRollText = MenuStyle.Text("Centered white credits roll", creditsViewport,
                CreditsSettings.Format(creditsSettings && creditsSettings.Content ? creditsSettings.Content.text : "# UniDoom"),
                0, 0, 1100, 0, 32, Color.white);
            creditsRollText.alignment = TextAlignmentOptions.Top;
            creditsRollText.textWrappingMode = TextWrappingModes.Normal;
            creditsRollText.overflowMode = TextOverflowModes.Overflow;
            creditsRollText.lineSpacing = 8;
            creditsRollText.paragraphSpacing = 16;
            creditsRollText.richText = true;
            creditsRollText.raycastTarget = false;
            var textRect = creditsRollText.rectTransform;
            textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(.5f, 1);
            textRect.anchoredPosition = Vector2.zero;
            FitCreditsRoll(true);
            PositionCreditsRoll(0);
        }

        void FitCreditsRoll(bool force = false)
        {
            if (!creditsViewport || !creditsRollText) return;
            Vector2 size = creditsViewport.rect.size;
            if (size.x <= 0 || size.y <= 0 || (!force && size == creditsViewportSize)) return;
            creditsViewportSize = size;
            float width = Mathf.Max(1, Mathf.Min(1100, size.x * .78f));
            float height = creditsRollText.GetPreferredValues(creditsRollText.text, width, Mathf.Infinity).y;
            creditsRollText.rectTransform.sizeDelta = new Vector2(width, Mathf.Ceil(height));
        }

        void PositionCreditsRoll(float elapsed)
        {
            FitCreditsRoll();
            if (!creditsRollText || !creditsViewport) return;
            float offset = CreditsSettings.RollOffset(elapsed, creditsRollText.rectTransform.rect.height,
                creditsViewport.rect.height, creditsSettings ? creditsSettings.ScrollSpeed : CreditsSettings.RequiredScrollSpeed,
                creditsSettings ? creditsSettings.StartDelay : 0);
            creditsRollText.rectTransform.anchoredPosition = new Vector2(0, offset);
        }

        void SetCreditsVisible(bool visible)
        {
            if (!visible)
            {
                if (creditsMusic) creditsMusic.Stop();
                creditsEndAt = -1;
                return;
            }
            creditsStartedAt = Time.unscaledTime;
            creditsEndAt = -1;
            Canvas.ForceUpdateCanvases();
            FitCreditsRoll(true);
            PositionCreditsRoll(0);
            if (!Application.isPlaying) return;
            var music = creditsSettings ? creditsSettings.Music :
                Resources.Load<AudioClip>(CreditsSettings.RequiredMusicResource);
            if (!music) return;
            if (!creditsMusic)
            {
                creditsMusic = gameObject.AddComponent<AudioSource>();
                creditsMusic.playOnAwake = false;
                creditsMusic.loop = true;
                creditsMusic.spatialBlend = 0;
                creditsMusic.ignoreListenerPause = true;
            }
            creditsMusic.clip = music;
            creditsMusic.volume = creditsSettings ? creditsSettings.MusicVolume : .6f;
            creditsMusic.Play();
        }

        public bool PlayEndingCredits()
        {
            if (busy) return false;
            if (CurrentScreen == Screen.Credits)
            {
                creditsFinishSession = true;
                return credits.activeInHierarchy;
            }
            creditsFinishSession = true;
            AcquirePause();
            SetScreen(Screen.Credits);
            return CurrentScreen == Screen.Credits && credits.activeInHierarchy;
        }

        void UpdateCreditsScroll()
        {
            if (CurrentScreen != Screen.Credits || !creditsRollText) return;
            // The menu pauses scaled time. Mouse buttons and wheel never stop this roll.
            PositionCreditsRoll(Time.unscaledTime - creditsStartedAt);
            if (creditsRollText.rectTransform.anchoredPosition.y < creditsRollText.rectTransform.rect.height)
            {
                creditsEndAt = -1;
                return;
            }
            if (creditsEndAt < 0) creditsEndAt = Time.unscaledTime + (creditsSettings ? creditsSettings.EndHold : 3);
            if (Time.unscaledTime >= creditsEndAt)
            {
                if (creditsFinishSession) { creditsFinishSession = false; ReturnToMain(); }
                else SetScreen(Screen.Main);
            }
        }
    }
}
