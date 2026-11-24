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
            var random = new System.Random(unchecked(Environment.TickCount ^ GetInstanceID()));
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

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.Url;
            player.url = path;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.aspectRatio = VideoAspectRatio.FitInside;
            player.audioOutputMode = VideoAudioOutputMode.None;
            player.isLooping = true;
            player.prepareCompleted += OnPrepared;
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

            prepared.targetTexture = videoTexture;
            prepared.Play();
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
                player.errorReceived -= OnError;
                player.Stop();
                player.targetTexture = null;
                Destroy(player);
                player = null;
            }
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
