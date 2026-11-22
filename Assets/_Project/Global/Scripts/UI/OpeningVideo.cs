using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace UniDoom.UI
{
    /// <summary>One-time opening video displayed over the main menu.</summary>
    public sealed class OpeningVideo : MonoBehaviour
    {
        const string FileName = "UniDoomOpening.mp4";
        VideoPlayer player;
        RenderTexture texture;
        RawImage picture;
        AspectRatioFitter aspect;
        Action finished;
        float startedAt;
        float playbackStartedAt;
        float playbackTimeout;
        bool closing;

        public void Begin(Action onFinished)
        {
            finished = onFinished;
            var path = Path.Combine(Application.streamingAssetsPath, FileName);
            if (!File.Exists(path)) { Finish(); return; }

            var background = gameObject.AddComponent<Image>();
            background.color = Color.black;
            MenuStyle.Stretch((RectTransform)transform);

            picture = MenuStyle.Rect("Opening film", transform, 0, 0, 0, 0).gameObject.AddComponent<RawImage>();
            MenuStyle.Stretch(picture.rectTransform);
            picture.raycastTarget = false;
            aspect = picture.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 16f / 9f;

            var skip = MenuStyle.Text("Skip opening", transform, "ESC / ENTER / A   PULAR",
                0, 0, 660, 46, 24, MenuStyle.Paper);
            skip.alignment = TextAlignmentOptions.Center;
            skip.raycastTarget = false;
            skip.rectTransform.anchorMin = skip.rectTransform.anchorMax = new Vector2(.5f, 0);
            skip.rectTransform.pivot = new Vector2(.5f, 0);
            skip.rectTransform.anchoredPosition = new Vector2(0, 24);

            var audio = gameObject.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0;
            audio.ignoreListenerPause = true;

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.Url;
            player.url = path;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.controlledAudioTrackCount = 1;
            player.EnableAudioTrack(0, true);
            player.SetTargetAudioSource(0, audio);
            player.timeReference = VideoTimeReference.Freerun;
            player.isLooping = false;
            player.prepareCompleted += Prepared;
            player.loopPointReached += Ended;
            player.errorReceived += Failed;
            startedAt = Time.realtimeSinceStartup;
            player.Prepare();
        }

        void Prepared(VideoPlayer video)
        {
            if (closing) return;
            int width = Mathf.Clamp((int)video.width, 1, 4096);
            int height = Mathf.Clamp((int)video.height, 1, 4096);
            texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            texture.Create();
            picture.texture = texture;
            aspect.aspectRatio = (float)width / height;
            video.targetTexture = texture;
            playbackStartedAt = Time.realtimeSinceStartup;
            playbackTimeout = Mathf.Max(3f, (float)video.length + 2f);
            video.Play();
        }

        void Ended(VideoPlayer _) => Finish();
        void Failed(VideoPlayer _, string error)
        {
            Debug.LogWarning("Opening video could not play: " + error);
            Finish();
        }

        void Update()
        {
            if (closing) return;
            if (player && !player.isPrepared && Time.realtimeSinceStartup - startedAt > 12f)
            {
                Debug.LogWarning("Opening video preparation timed out.");
                Finish();
                return;
            }
            if (playbackStartedAt > 0 && Time.realtimeSinceStartup - playbackStartedAt > playbackTimeout)
            {
                Finish();
                return;
            }
            bool skip = Keyboard.current != null &&
                (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame ||
                 Keyboard.current.spaceKey.wasPressedThisFrame);
            skip |= Gamepad.current != null &&
                (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame);
            if (skip) Finish();
        }

        void Finish()
        {
            if (closing) return;
            closing = true;
            if (player) player.Stop();
            finished?.Invoke();
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (player)
            {
                player.prepareCompleted -= Prepared;
                player.loopPointReached -= Ended;
                player.errorReceived -= Failed;
            }
            if (texture)
            {
                texture.Release();
                Destroy(texture);
            }
        }
    }
}
