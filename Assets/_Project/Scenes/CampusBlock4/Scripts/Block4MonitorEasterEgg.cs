using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace UniDoom.CampusBlock4
{
    [DisallowMultipleComponent]
    public sealed class Block4MonitorEasterEgg : MonoBehaviour
    {
        const string VideoFileName = "Block4MonitorEasterEgg.mp4";
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] Block4MonitorClues puzzle;
        [SerializeField] Material screenMaterial;
        [SerializeField, Range(1, 30)] int screenCount = 15;

        readonly List<Renderer> selectedScreens = new();
        readonly List<Material> originalMaterials = new();
        VideoPlayer player;
        GameObject audioHost;
        AudioSource audioSource;
        bool audioPositioned;
        RenderTexture videoTexture;
        Material videoMaterial;

        void Start()
        {
            if (!puzzle || !puzzle.HasReservedMonitors || !screenMaterial) return;

            string path = Path.Combine(Application.streamingAssetsPath, VideoFileName);
            if (!File.Exists(path))
            {
                Debug.LogWarning("Bloco 4: vídeo dos monitores não encontrado em StreamingAssets.", this);
                return;
            }

            var candidates = new List<Renderer>();
            foreach (var screen in GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!screen.gameObject.activeInHierarchy || screen.name != "Screen, dark glossy glass") continue;
                Transform monitor = screen.transform.parent;
                if (!monitor || monitor.name != "Monitor 24 polegadas" || puzzle.ReservesMonitor(monitor)) continue;
                candidates.Add(screen);
            }

            // Use a private RNG so picking the easter-egg screens does not change puzzle randomness.
            var random = new System.Random(unchecked(Environment.TickCount ^ GetEntityId().GetHashCode()));
            int count = Mathf.Min(screenCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                int next = random.Next(i, candidates.Count);
                var chosen = candidates[next];
                candidates[next] = candidates[i];
                candidates[i] = chosen;
                selectedScreens.Add(candidates[i]);
                originalMaterials.Add(candidates[i].sharedMaterial);
            }
            if (count == 0) return;

            // Create the AudioSource before the VideoPlayer (same order as OpeningVideo,
            // which works in this project). It sits on one of the actual monitors with
            // a very short falloff, so the player hears it only when standing close.
            // ignoreListenerPause keeps the video audible across scene transitions.
            audioHost = new GameObject("Block 4 monitor video audio");
            audioHost.transform.SetParent(transform, false);
            audioHost.transform.position = selectedScreens[0].bounds.center; // refined in OnStarted
            audioSource = audioHost.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 5f;
            audioSource.volume = 1f;
            audioSource.mute = false;
            // The project can leave AudioListener.pause set during transitions; keep
            // this source independent of it (same approach as the opening video).
            audioSource.ignoreListenerPause = true;

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.Url;
            player.url = path;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.aspectRatio = VideoAspectRatio.FitInside;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.controlledAudioTrackCount = 1;
            player.EnableAudioTrack(0, true);
            player.SetTargetAudioSource(0, audioSource);
            player.isLooping = true;
            player.prepareCompleted += OnPrepared;
            player.started += OnStarted;
            player.errorReceived += OnError;
            player.Prepare();
        }

        void OnPrepared(VideoPlayer prepared)
        {
            videoTexture = new RenderTexture(512, 288, 0, RenderTextureFormat.ARGB32)
            {
                name = "Block 4 monitor easter egg"
            };
            videoTexture.Create();
            var previous = RenderTexture.active;
            RenderTexture.active = videoTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = previous;

            videoMaterial = new Material(screenMaterial) { name = "Block 4 monitor video (runtime)" };
            videoMaterial.SetTexture(BaseMapId, videoTexture);
            videoMaterial.SetTexture(MainTexId, videoTexture);
            for (int i = 0; i < selectedScreens.Count; i++)
                if (selectedScreens[i]) selectedScreens[i].sharedMaterial = videoMaterial;

            // Bind the audio track before Play(): changing the target after playback
            // has started can cut the audio stream.
            if (audioSource != null)
            {
                prepared.EnableAudioTrack(0, true);
                prepared.SetTargetAudioSource(0, audioSource);
            }
            prepared.targetTexture = videoTexture;
            prepared.Play();
        }

        void OnStarted(VideoPlayer started)
        {
            if (audioSource == null || audioHost == null) return;

            if (!audioPositioned)
            {
                // The first candidate can be in a sealed-off part of the building. Put
                // the sound on the easter-egg monitor nearest to where the player is.
                Vector3 reference = GameManager.SessionPlayer != null
                    ? GameManager.SessionPlayer.transform.position
                    : audioHost.transform.position;
                Renderer nearest = null;
                float best = float.MaxValue;
                foreach (var screen in selectedScreens)
                {
                    if (!screen) continue;
                    float distance = (screen.bounds.center - reference).sqrMagnitude;
                    if (distance >= best) continue;
                    best = distance;
                    nearest = screen;
                }
                if (nearest != null) audioHost.transform.position = nearest.bounds.center;
                audioPositioned = true;
            }
        }

        void OnError(VideoPlayer _, string message)
        {
            Debug.LogWarning("Bloco 4: falha ao reproduzir o vídeo dos monitores: " + message, this);
            ReleaseResources();
        }

        void OnDisable() => ReleaseResources();

        void ReleaseResources()
        {
            if (player)
            {
                player.prepareCompleted -= OnPrepared;
                player.started -= OnStarted;
                player.errorReceived -= OnError;
                player.Stop();
                player.targetTexture = null;
                Destroy(player);
                player = null;
            }
            if (audioSource)
            {
                audioSource.Stop();
                audioSource = null;
            }
            if (audioHost)
            {
                Destroy(audioHost);
                audioHost = null;
            }
            audioPositioned = false;
            for (int i = 0; i < selectedScreens.Count; i++)
                if (selectedScreens[i]) selectedScreens[i].sharedMaterial = originalMaterials[i];
            selectedScreens.Clear();
            originalMaterials.Clear();
            if (videoMaterial) Destroy(videoMaterial);
            videoMaterial = null;
            if (videoTexture)
            {
                videoTexture.Release();
                Destroy(videoTexture);
                videoTexture = null;
            }
        }
    }
}
