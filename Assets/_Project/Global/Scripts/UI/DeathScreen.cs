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
        RectTransform toastRect;
        static readonly Vector2 ToastPosition = new Vector2(38, 206);
        float toastStarted, toastUntil, openedAt;
        bool deathActive, transitioning;

        void Awake()
        {
            Instance = this;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = gameObject.AddComponent<GameplayCanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = .5f;
            EnsureCheckpointNotification();
        }

        void OnEnable()
        {
            Instance = this;
            EnsureCheckpointNotification();
        }

        void EnsureCheckpointNotification()
        {
            if (toastRect != null) return;
            // Rebuild only this owned widget when scripts reload during an existing run.
            if (toast != null) Destroy(toast.gameObject);
            toastRect = MenuStyle.Rect("Checkpoint notification", transform, 0, 0, 380, 38);
            toastRect.anchorMin = toastRect.anchorMax = toastRect.pivot = Vector2.zero;
            toastRect.anchoredPosition = ToastPosition;
            toast = toastRect.gameObject.AddComponent<CanvasGroup>();
            toast.alpha = 0;
            toast.blocksRaycasts = toast.interactable = false;
            var mark = MenuStyle.Image("Return mark", toastRect, 0, 9, 2, 14, new Color(.48f, .23f, .13f, .8f));
            mark.rectTransform.localRotation = Quaternion.Euler(0, 0, -17);
            MenuStyle.Image("Fading rule", toastRect, 14, 33, 64, 1, new Color(.48f, .44f, .36f, .25f));
            toastLabel = MenuStyle.Text("Message", toastRect, "", 14, 0, 350, 31, 22, new Color(.74f, .70f, .61f));
            toastLabel.characterSpacing = 1.8f;
            var shadow = toastLabel.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .9f);
            shadow.effectDistance = new Vector2(1, -1);
        }

        public void Bind(PlayerRespawn owner) => player = owner;

        public void ShowCheckpoint(int index)
        {
            EnsureCheckpointNotification();
            toastLabel.text = "UM LUGAR PARA VOLTAR";
            toast.alpha = 0;
            toastStarted = Time.unscaledTime;
            toastUntil = toastStarted + 3.2f;
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
            EnsureCheckpointNotification();
            float remaining = toastUntil - Time.unscaledTime;
            float appearing = Mathf.SmoothStep(0, 1, Mathf.Clamp01((Time.unscaledTime - toastStarted) / .5f));
            float fading = Mathf.SmoothStep(0, 1, Mathf.Clamp01(remaining / .9f));
            bool visible = remaining > 0 && !deathActive && Time.timeScale > 0 && !GameMenus.BlocksGameplay && !DoorReloadScreen.IsReloading;
            toast.alpha = Mathf.MoveTowards(toast.alpha, visible ? .82f * appearing * fading : 0, Time.unscaledDeltaTime * 3.5f);
            toastRect.anchoredPosition = ToastPosition + Vector2.up * (Mathf.Lerp(-4, 0, appearing) + (1 - fading) * 3);
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
