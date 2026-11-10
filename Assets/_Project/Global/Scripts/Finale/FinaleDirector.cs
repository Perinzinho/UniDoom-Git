using System.Collections;
using System.Collections.Generic;
using TMPro;
using UniDoom.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UniDoom.Finale
{
    /// <summary>Builds the rescued-teacher celebration when the prison portal returns to Unimar.</summary>
    public sealed class FinaleDirector : MonoBehaviour
    {
        const string ApplauseResourcePath = "Audio/Finale/aplausos-professores-final";
        const float CelebrationDuration = 9f;
        const float FadeDuration = 3f;
        readonly List<Material> runtimeMaterials = new();
        readonly List<AudioClip> runtimeClips = new();
        PlayerMovement player;
        bool previousMovement;
        Image fade;
        float groundReference;
        AudioSource applauseSource;
        AudioClip rocketClip, explosionClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!PrisonProgress.FinaleStarted || scene.name != "CampusExterior" ||
                FindAnyObjectByType<FinaleDirector>() != null) return;
            new GameObject("[UniDoom Finale]").AddComponent<FinaleDirector>();
        }

        void Awake()
        {
            // sceneLoaded runs after Awake but before Start. Disable authored actors and their
            // spawners here so no zombie gets a visible frame during the ending transition.
            foreach (var spawner in FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                if (spawner.gameObject.scene.name == "CampusExterior") spawner.enabled = false;
            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
                if (enemy.gameObject.scene.name == "CampusExterior") enemy.gameObject.SetActive(false);
        }

        IEnumerator Start()
        {
            while (GameManager.SessionPlayer == null) yield return null;
            // The portal places the persistent player after sceneLoaded; wait until that shared
            // transition has finished so the celebration is built at the real arrival pose.
            while (DoorReloadScreen.IsReloading) yield return null;
            player = GameManager.SessionPlayer;
            previousMovement = player.MovementEnabled;
            player.MovementEnabled = false;
            foreach (var input in player.GetComponentsInChildren<CinemachineInputAxisController>(true)) input.enabled = false;

            var settings = Resources.Load<FinaleCastSettings>("Finale/FinaleCast");
            if (settings == null || settings.TeacherSprites == null || settings.TeacherSprites.Length == 0)
            {
                Debug.LogError("[Finale] Finale/FinaleCast is missing or empty.");
                yield break;
            }

            Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .5f) forward = Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            var controller = player.GetComponent<CharacterController>();
            groundReference = controller != null ? controller.bounds.min.y : player.transform.position.y;
            Vector3 center = player.transform.position + forward * 11f;
            BuildFinaleAudio();
            SpawnTeachers(settings, center, right);
            SpawnFireworks(center + forward * 8f, right);
            BuildFade();

            yield return new WaitForSecondsRealtime(CelebrationDuration);
            float elapsed = 0;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fade.color = new Color(0, 0, 0, Mathf.SmoothStep(0, 1, elapsed / FadeDuration));
                yield return null;
            }
            GameMenus.Instance?.PlayEndingCredits();
            Destroy(gameObject);
        }

        void SpawnTeachers(FinaleCastSettings settings, Vector3 center, Vector3 right)
        {
            Sprite[] sprites = settings.TeacherSprites;
            float spacing = 1.22f;
            float start = -(sprites.Length - 1) * spacing * .5f;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Vector3 position = Ground(center + right * (start + spacing * i));
                var instance = new GameObject("Professor resgatado " + (i + 1).ToString("00"));
                instance.transform.SetParent(transform, false);
                instance.transform.SetPositionAndRotation(position, Quaternion.identity);
                var renderer = instance.AddComponent<SpriteRenderer>();
                renderer.sprite = sprites[i];
                renderer.sortingOrder = 10;
                var material = settings.MaterialFor(i);
                if (material != null) renderer.sharedMaterial = material;
                instance.AddComponent<Billboard>();
                var jump = instance.AddComponent<JumpingTeacher>();
                jump.Configure(position, i * .37f);
            }
        }

        Vector3 Ground(Vector3 position)
        {
            float bestDistance = float.PositiveInfinity;
            float bestY = groundReference;
            foreach (var hit in Physics.RaycastAll(position + Vector3.up * 10f, Vector3.down, 30f,
                         ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y <= .55f) continue;
                float distance = Mathf.Abs(hit.point.y - groundReference);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestY = hit.point.y;
            }
            position.y = bestY + .03f;
            return position;
        }

        void SpawnFireworks(Vector3 center, Vector3 right)
        {
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            Material material = null;
            if (shader != null)
            {
                material = new Material(shader) { name = "Finale firework material" };
                runtimeMaterials.Add(material);
            }
            StartCoroutine(FireworkShow(center, right, material));
        }

        IEnumerator FireworkShow(Vector3 center, Vector3 right, Material material)
        {
            Color[] colors =
            {
                new(1f, .18f, .08f), new(1f, .68f, .12f), new(.18f, .58f, 1f),
                new(.22f, 1f, .52f), new(.82f, .3f, 1f), new(1f, .28f, .62f)
            };
            float[] lanes = { -8f, 5.5f, -3f, 9f, 1.5f, -6f, 7f, 0f };
            float elapsed = 0;
            int shell = 0;
            while (elapsed < CelebrationDuration - 1.15f)
            {
                float lane = lanes[shell % lanes.Length];
                Vector3 launch = Ground(center + right * lane * .7f);
                Vector3 burst = launch + Vector3.up * (7.5f + (shell % 4) * 1.25f) +
                    right * (((shell * 3) % 5 - 2) * .55f);
                Color primary = colors[shell % colors.Length];
                Color secondary = colors[(shell + 2) % colors.Length];
                StartCoroutine(LaunchFirework(shell + 1, launch, burst, primary, secondary, material));
                float delay = .48f + (shell % 3) * .17f;
                elapsed += delay;
                shell++;
                yield return new WaitForSecondsRealtime(delay);
            }
        }

        IEnumerator LaunchFirework(int number, Vector3 origin, Vector3 target, Color primary,
            Color secondary, Material material)
        {
            var rocket = new GameObject("Firework rocket " + number);
            rocket.transform.SetParent(transform, false);
            rocket.transform.position = origin;
            var trail = rocket.AddComponent<TrailRenderer>();
            trail.time = .42f;
            trail.minVertexDistance = .035f;
            trail.startWidth = .075f;
            trail.endWidth = .008f;
            trail.numCornerVertices = 3;
            trail.numCapVertices = 3;
            if (material != null) trail.sharedMaterial = material;
            var trailGradient = new Gradient();
            trailGradient.SetKeys(new[] { new GradientColorKey(Color.white, 0),
                new GradientColorKey(new Color(1f, .55f, .08f), .35f),
                new GradientColorKey(new Color(.32f, .08f, .02f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.85f, .35f),
                    new GradientAlphaKey(0, 1) });
            trail.colorGradient = trailGradient;
            var glow = rocket.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, .55f, .16f);
            glow.range = 4f;
            glow.intensity = 2.5f;
            glow.shadows = LightShadows.None;
            var rocketAudio = rocket.AddComponent<AudioSource>();
            rocketAudio.playOnAwake = false;
            rocketAudio.spatialBlend = .72f;
            rocketAudio.minDistance = 3f;
            rocketAudio.maxDistance = 45f;
            rocketAudio.rolloffMode = AudioRolloffMode.Linear;
            rocketAudio.clip = rocketClip;
            rocketAudio.volume = .42f;
            rocketAudio.pitch = .94f + number % 4 * .035f;
            if (rocketClip != null) rocketAudio.Play();

            float duration = .78f + number % 3 * .1f;
            Vector3 wind = Vector3.Cross(Vector3.up, target - origin).normalized * ((number % 2 == 0 ? 1 : -1) * .28f);
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float rise = 1f - (1f - t) * (1f - t);
                rocket.transform.position = Vector3.LerpUnclamped(origin, target, rise) + wind * Mathf.Sin(t * Mathf.PI);
                glow.intensity = Mathf.Lerp(3.2f, 1.2f, t);
                yield return null;
            }
            rocket.transform.position = target;
            trail.emitting = false;
            glow.enabled = false;
            CreateExplosion(number, target, primary, secondary, material);
            PlayExplosionAudio(number, target);
            Destroy(rocket, .7f);
        }

        void BuildFinaleAudio()
        {
            var applause = Resources.Load<AudioClip>(ApplauseResourcePath);
            rocketClip = CreateRocketClip();
            explosionClip = CreateExplosionClip();
            runtimeClips.Add(rocketClip);
            runtimeClips.Add(explosionClip);
            if (applause == null)
                Debug.LogError("[Finale] Applause clip is missing at Resources/" +
                    ApplauseResourcePath + ".wav.");
            applauseSource = gameObject.AddComponent<AudioSource>();
            applauseSource.playOnAwake = false;
            applauseSource.loop = true;
            applauseSource.spatialBlend = 0;
            applauseSource.volume = .72f;
            applauseSource.clip = applause;
            if (applause != null) applauseSource.Play();
        }

        void PlayExplosionAudio(int number, Vector3 position)
        {
            if (explosionClip == null) return;
            var host = new GameObject("Firework boom " + number, typeof(AudioSource));
            host.transform.SetParent(transform, false);
            host.transform.position = position;
            var source = host.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = .62f;
            source.minDistance = 5f;
            source.maxDistance = 70f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.clip = explosionClip;
            source.volume = .78f;
            source.pitch = .9f + number % 5 * .045f;
            source.Play();
            Destroy(host, explosionClip.length / source.pitch + .2f);
        }

        AudioClip CreateRocketClip()
        {
            const int sampleRate = 44100;
            const float duration = 1.05f;
            int frames = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[frames];
            var random = new System.Random(9137);
            float filtered = 0;
            for (int i = 0; i < frames; i++)
            {
                float t = (float)i / frames;
                float noise = (float)random.NextDouble() * 2f - 1f;
                filtered = Mathf.Lerp(filtered, noise, .28f);
                float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t)) * (1f - .35f * t);
                float whistle = Mathf.Sin(2f * Mathf.PI * (180f + 520f * t * t) * i / sampleRate);
                samples[i] = (filtered * .7f + whistle * .22f) * envelope;
            }
            Normalize(samples, .82f);
            var clip = AudioClip.Create("Finale rocket rise", frames, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        AudioClip CreateExplosionClip()
        {
            const int sampleRate = 44100;
            const float duration = 2.25f;
            int frames = Mathf.CeilToInt(sampleRate * duration);
            var samples = new float[frames];
            var random = new System.Random(20849);
            float lowNoise = 0;
            for (int i = 0; i < frames; i++)
            {
                float time = (float)i / sampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                lowNoise = Mathf.Lerp(lowNoise, noise, .08f);
                float thump = Mathf.Sin(2f * Mathf.PI * (52f - 7f * Mathf.Min(time, 1f)) * time) *
                    Mathf.Exp(-time * 3.1f);
                float crack = noise * Mathf.Exp(-time * 18f);
                float body = lowNoise * Mathf.Exp(-time * 4.4f);
                float echoTime = time - .24f;
                float echo = echoTime > 0 ? Mathf.Sin(2f * Mathf.PI * 43f * echoTime) *
                    Mathf.Exp(-echoTime * 4.8f) * .28f : 0;
                samples[i] = thump * .78f + crack * .42f + body * .34f + echo;
            }
            Normalize(samples, .9f);
            var clip = AudioClip.Create("Finale firework boom", frames, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static void Normalize(float[] samples, float peak)
        {
            float maximum = 0;
            for (int i = 0; i < samples.Length; i++) maximum = Mathf.Max(maximum, Mathf.Abs(samples[i]));
            if (maximum <= .0001f) return;
            float gain = peak / maximum;
            for (int i = 0; i < samples.Length; i++) samples[i] *= gain;
        }

        void CreateExplosion(int number, Vector3 position, Color primary, Color secondary, Material material)
        {
            CreateSparkSystem("Main stars " + number, position, 150 + number % 3 * 25,
                new ParticleSystem.MinMaxCurve(5.5f, 10.5f), new ParticleSystem.MinMaxCurve(1.35f, 2.25f),
                new ParticleSystem.MinMaxCurve(.045f, .105f), .52f, primary, secondary, .82f, 0, material);
            CreateSparkSystem("Glitter " + number, position, 70 + number % 4 * 10,
                new ParticleSystem.MinMaxCurve(2.2f, 6f), new ParticleSystem.MinMaxCurve(1.9f, 3.1f),
                new ParticleSystem.MinMaxCurve(.018f, .055f), .72f, Color.white,
                new Color(1f, .62f, .12f), .28f, .12f, material);
            CreateSmoke(position, material);
            StartCoroutine(ExplosionFlash(position, primary));
        }

        void CreateSparkSystem(string name, Vector3 position, int count, ParticleSystem.MinMaxCurve speed,
            ParticleSystem.MinMaxCurve lifetime, ParticleSystem.MinMaxCurve size, float gravity,
            Color primary, Color secondary, float trailRatio, float delay, Material material)
        {
            var host = new GameObject(name, typeof(ParticleSystem));
            host.transform.SetParent(transform, false);
            host.transform.position = position;
            var particles = host.GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.duration = .12f;
            main.startDelay = delay;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = count + 16;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)count) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .06f;
            shape.sphericalDirectionAmount = 1f;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0),
                new GradientColorKey(primary, .12f), new GradientColorKey(secondary, .68f),
                new GradientColorKey(new Color(.18f, .06f, .02f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, .58f),
                    new GradientAlphaKey(.35f, .84f), new GradientAlphaKey(0, 1) });
            color.color = gradient;
            var trails = particles.trails;
            trails.enabled = true;
            trails.ratio = trailRatio;
            trails.lifetime = new ParticleSystem.MinMaxCurve(.24f, .55f);
            trails.dieWithParticles = false;
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = .16f;
            noise.frequency = .32f;
            noise.scrollSpeed = .12f;
            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortingOrder = 20;
            if (material != null)
            {
                renderer.sharedMaterial = material;
                renderer.trailMaterial = material;
            }
            particles.Play();
        }

        void CreateSmoke(Vector3 position, Material material)
        {
            var host = new GameObject("Firework smoke", typeof(ParticleSystem));
            host.transform.SetParent(transform, false);
            host.transform.position = position;
            var particles = host.GetComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.loop = false;
            main.duration = .1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.15f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(.25f, .7f);
            main.gravityModifier = -.035f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 14) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .2f;
            var color = particles.colorOverLifetime;
            color.enabled = true;
            var smoke = new Gradient();
            smoke.SetKeys(new[] { new GradientColorKey(new Color(.42f, .38f, .34f), 0),
                new GradientColorKey(new Color(.18f, .17f, .16f), 1) },
                new[] { new GradientAlphaKey(.18f, 0), new GradientAlphaKey(.1f, .5f),
                    new GradientAlphaKey(0, 1) });
            color.color = smoke;
            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 18;
            if (material != null) renderer.sharedMaterial = material;
            particles.Play();
        }

        IEnumerator ExplosionFlash(Vector3 position, Color color)
        {
            var host = new GameObject("Firework flash", typeof(Light));
            host.transform.SetParent(transform, false);
            host.transform.position = position;
            var flash = host.GetComponent<Light>();
            flash.type = LightType.Point;
            flash.color = Color.Lerp(Color.white, color, .35f);
            flash.range = 18f;
            flash.shadows = LightShadows.None;
            for (float elapsed = 0; elapsed < .22f; elapsed += Time.unscaledDeltaTime)
            {
                flash.intensity = Mathf.Lerp(8f, 0, elapsed / .22f);
                yield return null;
            }
            Destroy(host);
        }

        void BuildFade()
        {
            var root = new GameObject("Finale fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30900;
            fade = root.AddComponent<Image>();
            fade.color = Color.clear;
            fade.raycastTarget = true;
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var banner = MenuStyle.Image("Celebration title background", root.transform, 0, 0, 1040, 82,
                new Color(.025f, .02f, .018f, .78f));
            var bannerRect = banner.rectTransform;
            bannerRect.anchorMin = bannerRect.anchorMax = new Vector2(.5f, 1);
            bannerRect.pivot = new Vector2(.5f, 1);
            bannerRect.anchoredPosition = new Vector2(0, -36);
            banner.raycastTarget = false;
            MenuStyle.Image("Menu rust accent", banner.transform, 0, 0, 1040, 3, MenuStyle.Rust);
            var label = MenuStyle.Text("Celebration title", banner.transform,
                "OS PROFESSORES ESTÃO A SALVO", 0, 0, 1040, 82, 34, MenuStyle.Paper);
            MenuStyle.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(32, 0);
            label.rectTransform.offsetMax = new Vector2(-32, 0);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.characterSpacing = 1.5f;
            label.raycastTarget = false;
        }

        void OnDestroy()
        {
            if (applauseSource != null) applauseSource.Stop();
            foreach (var material in runtimeMaterials) if (material != null) Destroy(material);
            foreach (var clip in runtimeClips) if (clip != null) Destroy(clip);
            if (player != null && GameMenus.Instance != null && GameMenus.Instance.CurrentScreen == GameMenus.Screen.Gameplay)
                player.MovementEnabled = previousMovement;
        }
    }

    public sealed class JumpingTeacher : MonoBehaviour
    {
        Vector3 origin;
        float phase;

        public void Configure(Vector3 position, float offset) { origin = position; phase = offset; }

        void Update()
        {
            float jump = Mathf.Pow(Mathf.Max(0, Mathf.Sin(Time.unscaledTime * 4.2f + phase)), 1.7f) * .7f;
            transform.position = origin + Vector3.up * jump;
        }
    }
}
