using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UniDoom.UI
{
    /// <summary>Death/respawn lifecycle and checkpoint notifications; GameMenus owns the interactive screen.</summary>
    public class DeathScreen : MonoBehaviour
    {
        public static DeathScreen Instance { get; private set; }
        public static bool IsVisible => Instance != null && Instance.deathActive;
        public bool IsReady => deathActive && !transitioning && Time.unscaledTime - openedAt >= 1.15f;
        public bool CanRespawn => IsReady && player != null && player.HasLives;

        PlayerRespawn player;
        CanvasGroup toast;
        TMP_Text toastLabel;
        float toastUntil, openedAt;
        bool deathActive, transitioning;

        void Awake()
        {
            Instance = this;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var rect = MenuStyle.Rect("Checkpoint notification", transform, 0, 0, 640, 82);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -55);
            toast = rect.gameObject.AddComponent<CanvasGroup>();
            toast.alpha = 0;
            toast.blocksRaycasts = toast.interactable = false;
            MenuStyle.Image("Background", rect, 0, 0, 640, 82, new Color(.025f, .025f, .022f, .97f));
            MenuStyle.Image("Accent", rect, 0, 0, 3, 82, MenuStyle.Amber);
            toastLabel = MenuStyle.Text("Message", rect, "", 25, 8, 600, 36, 23, MenuStyle.Amber);
            MenuStyle.Text("Detail", rect, "Seu ponto de retorno foi atualizado.", 25, 45, 600, 25, 20, MenuStyle.Paper);
        }

        public void Bind(PlayerRespawn owner) => player = owner;

        public void ShowCheckpoint(int index)
        {
            toastLabel.text = "CHECKPOINT ATUALIZADO";
            toastUntil = Time.unscaledTime + 3.4f;
        }

        public void ShowDeath()
        {
            deathActive = true;
            transitioning = false;
            openedAt = Time.unscaledTime;
            toastUntil = 0;
            toast.alpha = 0;
            GameMenus.Instance.ShowDeath(player, this);
        }

        public void HideDeath()
        {
            deathActive = transitioning = false;
            GameMenus.Instance?.HideDeath();
        }

        void Update()
        {
            float remaining = toastUntil - Time.unscaledTime;
            float target = remaining > 0 && !deathActive ? Mathf.Clamp01(remaining / .4f) : 0;
            toast.alpha = Mathf.MoveTowards(toast.alpha, target, Time.unscaledDeltaTime * 5);
        }

        public IEnumerator FadeToBlack()
        {
            transitioning = true;
            for (float t = 0; t < .3f; t += Time.unscaledDeltaTime)
            {
                GameMenus.Instance.SetFade(t / .3f);
                yield return null;
            }
            GameMenus.Instance.SetFade(1);
        }

        public IEnumerator FadeFromBlack()
        {
            GameMenus.Instance.HideDeath();
            for (float t = 0; t < .45f; t += Time.unscaledDeltaTime)
            {
                GameMenus.Instance.SetFade(1 - t / .45f);
                yield return null;
            }
            GameMenus.Instance.SetFade(0);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
