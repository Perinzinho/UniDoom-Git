using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>The first local key triggers one persistent alarm and one finite group of zombies.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(11000)]
public sealed class LawSchoolAlarmPrototype : MonoBehaviour
{
    [SerializeField] Transform building;
    [SerializeField] LawSchoolLighting lighting;
    [SerializeField] LawSchoolCellKeyPickup key;
    [SerializeField] AudioSource siren;
    [SerializeField] GameObject zombiePrefab;
    [SerializeField] GameObject[] zombieVariants;
    [SerializeField] Transform[] arrivalPoints;
    [SerializeField] Transform[] keyLocations;
    [SerializeField] Transform[] excludedKeyAreas;
    [SerializeField, Min(.1f)] float initialRedSeconds = 2;
    [SerializeField, Min(.1f)] float redSeconds = 3;
    [SerializeField, Min(.1f)] float blackoutSeconds = 1.6f;
    [SerializeField, Min(.2f)] float arrivalInterval = .55f;
    [SerializeField, Min(3)] float minimumArrivalDistance = 10;
    [SerializeField, Min(1)] float nearbyVisibilityRange = 4.5f;
    [SerializeField, Min(0)] float nearbyVisibilityIntensity = 8.5f;

    public bool IsActive { get; private set; }
    public event System.Action StateChanged;
    public bool IsBlackout { get; private set; }
    public float Elapsed { get; private set; }
    public int SpawnedCount { get; private set; }
    public bool IsResolved { get; private set; }
    public string SelectedKeyRoom { get; private set; }
    public Transform SelectedKeyLocation { get; private set; }
    public const string EventId = "law-school-cell-key-alarm";
    public const string TriggeredEventId = "law-school-local-key-collected";

    public static void NotifyKeyCollected(Component pickup, PlayerCellKeys inventory)
    {
        if (!pickup || !inventory || inventory.HasCompletedEvent(TriggeredEventId) ||
            inventory.HasCompletedEvent(EventId)) return;
        foreach (var alarm in FindObjectsByType<LawSchoolAlarmPrototype>())
        {
            // Shared pickups in the library and Block 4 must not trigger this building.
            if (!alarm.isActiveAndEnabled || alarm.gameObject.scene != pickup.gameObject.scene) continue;
            inventory.CompleteEvent(TriggeredEventId);
            alarm.BeginAlarm();
            return;
        }
    }

    readonly List<LightState> lights = new List<LightState>();
    readonly List<Surface> surfaces = new List<Surface>();
    readonly List<SpriteState> actors = new List<SpriteState>();
    bool[] arrived;
    float nextArrival;
    int variantOffset;
    bool audioPaused;
    Camera view;
    Light nearbyLight;
    bool flashlightOn = true;
    UnityEngine.UI.Image weaponHud;
    Color weaponHudColor;

    static readonly Color AlarmRed = new Color(1, .025f, .008f, 1);
    static readonly Color SpriteRed = new Color(1, .15f, .09f, 1);
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int TintColor = Shader.PropertyToID("_Color");
    static readonly int Emission = Shader.PropertyToID("_EmissionColor");

    sealed class LightState
    {
        public Light light;
        public Color color;
        public bool enabled, temperature;
        public float intensity;
    }

    sealed class SpriteState
    {
        public SpriteRenderer renderer;
        public Color color;
    }

    sealed class Surface
    {
        public Renderer renderer;
        public Material material;
        public int index;
        public readonly MaterialPropertyBlock original = new MaterialPropertyBlock();
        public readonly MaterialPropertyBlock working = new MaterialPropertyBlock();

        public void Apply(bool dark)
        {
            if (!renderer || !material) return;
            renderer.SetPropertyBlock(original.isEmpty ? null : original, index);
            renderer.GetPropertyBlock(working, index);
            if (dark && (material.shader.name.Contains("Unlit") || renderer is SpriteRenderer))
            {
                if (material.HasProperty(BaseColor)) working.SetColor(BaseColor, OpaqueBlack(material.GetColor(BaseColor)));
                if (material.HasProperty(TintColor)) working.SetColor(TintColor, OpaqueBlack(material.GetColor(TintColor)));
            }
            else if (material.shader.name.Contains("Unlit") || renderer is SpriteRenderer)
            {
                if (material.HasProperty(BaseColor)) working.SetColor(BaseColor, material.GetColor(BaseColor) * SpriteRed);
                if (material.HasProperty(TintColor)) working.SetColor(TintColor, material.GetColor(TintColor) * SpriteRed);
            }
            if (material.HasProperty(Emission))
                working.SetColor(Emission, dark ? Color.black : material.GetColor(Emission) * AlarmRed);
            renderer.SetPropertyBlock(working, index);
        }

        static Color OpaqueBlack(Color source) => new Color(0, 0, 0, source.a);
        public void Restore() { if (renderer) renderer.SetPropertyBlock(original.isEmpty ? null : original, index); }
    }

    IEnumerator Start()
    {
        // Returning to this scene keeps the specific key owned by the persistent player.
        while (!GameManager.SessionPlayer) yield return null;
        var player = GameManager.SessionPlayer;
        if (!key || !building)
        {
            Debug.LogError("LawSchool: configure a chave da biblioteca e a raiz do prédio.", this);
            yield break;
        }
        var keys = player.GetComponent<PlayerCellKeys>();
        if (!keys) keys = player.gameObject.AddComponent<PlayerCellKeys>();
        ChooseKeyRoom(keys);
        IsResolved = keys.HasCompletedEvent(EventId);
        // Publish the initial placement too, so maintenance is available in this same initialization.
        StateChanged?.Invoke();
        if (keys.HasCompletedEvent(TriggeredEventId) && !IsResolved) BeginAlarm();
    }

    void ChooseKeyRoom(PlayerCellKeys inventory)
    {
        if (keyLocations == null) return;
        var available = new List<Transform>();
        var names = new List<string>();
        foreach (var location in keyLocations)
        {
            if (!location || !location.parent) continue;
            bool maintenance = false;
            if (excludedKeyAreas != null)
                foreach (var area in excludedKeyAreas)
                    if (area && location.IsChildOf(area)) { maintenance = true; break; }
            for (var parent = location; parent && parent != building; parent = parent.parent)
                if (parent.name.StartsWith("MANUTEN",System.StringComparison.OrdinalIgnoreCase)) { maintenance = true; break; }
            if (maintenance || names.Contains(location.name)) continue;
            available.Add(location); names.Add(location.name);
        }
        string chosen = inventory.GetOrChooseKeyLocation(key.CellId,names);
        int index = names.IndexOf(chosen);
        if (index < 0)
        {
            Debug.LogError("LawSchool: nenhum local válido para a chave da biblioteca.", this);
            return;
        }
        SelectedKeyLocation = available[index];
        SelectedKeyRoom = SelectedKeyLocation.parent.name;
        key.transform.SetParent(building,true);
        key.transform.position = available[index].position;
        key.RefreshAvailability(inventory);
    }

    public bool TryResolve(PlayerPickupKey player)
    {
        if (!IsActive || IsResolved || !player || Time.timeScale <= 0 || UniDoom.UI.GameMenus.BlocksGameplay) return false;
        var life = player.GetComponent<PlayerLife>();
        if (life && life.IsDead) return false;
        var inventory = player.GetComponent<PlayerCellKeys>();
        if (!inventory || !inventory.HasKey(LawSchoolMaintenanceAccess.KeyId)) return false;
        inventory.CompleteEvent(EventId);
        IsResolved = true;
        RestorePresentation();
        return true;
    }

    public void BeginAlarm()
    {
        if (IsActive || IsResolved || !isActiveAndEnabled || !building || !lighting) return;
        IsActive = true;
        StateChanged?.Invoke();
        Elapsed = 0;
        SpawnedCount = 0;
        variantOffset = zombieVariants != null && zombieVariants.Length > 0 ? Random.Range(0,zombieVariants.Length) : 0;
        arrived = new bool[arrivalPoints != null ? arrivalPoints.Length : 0];
        nextArrival = initialRedSeconds;
        foreach (var light in building.GetComponentsInChildren<Light>())
            lights.Add(new LightState { light = light, color = light.color, intensity = light.intensity,
                enabled = light.enabled, temperature = light.useColorTemperature });
        foreach (var renderer in building.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled) continue;
            if (renderer is SpriteRenderer sprite)
            {
                actors.Add(new SpriteState { renderer = sprite, color = sprite.color });
                continue;
            }
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (!materials[i]) continue;
                var surface = new Surface { renderer = renderer, material = materials[i], index = i };
                renderer.GetPropertyBlock(surface.original, i);
                surfaces.Add(surface);
            }
        }
        BindPlayerPresentation();
        ApplyPhase(false);
        if (siren) siren.Play();
    }

    void Update()
    {
        if (!IsActive) return;
        bool paused = Time.timeScale <= 0 || UniDoom.UI.GameMenus.BlocksGameplay;
        if (siren && paused != audioPaused)
        {
            if (paused) siren.Pause(); else siren.UnPause();
            audioPaused = paused;
        }
        if (paused || UniDoom.UI.DeathScreen.IsVisible || UniDoom.UI.DoorReloadScreen.IsReloading) return;
        if ((Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame))
            flashlightOn = !flashlightOn;
        if (nearbyLight) nearbyLight.enabled = flashlightOn;
        Elapsed += Time.deltaTime;
        bool dark = Elapsed >= initialRedSeconds &&
            Mathf.Repeat(Elapsed - initialRedSeconds, blackoutSeconds + redSeconds) < blackoutSeconds;
        if (dark != IsBlackout) ApplyPhase(dark);
        if (Elapsed >= nextArrival && SpawnedCount < arrived.Length)
        {
            TryNextArrival();
            nextArrival = Elapsed + arrivalInterval;
        }
    }

    void ApplyPhase(bool dark)
    {
        IsBlackout = dark;
        foreach (var state in lights)
        {
            if (!state.light) continue;
            state.light.enabled = state.enabled && !dark;
            state.light.color = AlarmRed;
            state.light.useColorTemperature = false;
            state.light.intensity = state.intensity;
        }
        foreach (var surface in surfaces) surface.Apply(dark);
        lighting.SetAlarmPhase(true, dark);
        if (nearbyLight) nearbyLight.enabled = flashlightOn;
        TintActors();
    }

    void LateUpdate()
    {
        if (!IsActive) return;
        if (!nearbyLight || !weaponHud) BindPlayerPresentation();
        if (weaponHud) weaponHud.color = weaponHudColor * (IsBlackout ? new Color(.17f,.20f,.25f,1) : SpriteRed);
        TintActors();
    }

    void BindPlayerPresentation()
    {
        if (!view) view = Camera.main;
        if (!nearbyLight && view)
        {
            var source = new GameObject("LawSchool — visão próxima no apagão");
            source.transform.SetParent(view.transform,false);
            source.transform.localPosition = new Vector3(0,-.08f,.12f);
            nearbyLight = source.AddComponent<Light>();
            nearbyLight.type = LightType.Spot;
            nearbyLight.spotAngle = 58;
            nearbyLight.innerSpotAngle = 30;
            nearbyLight.range = 18;
            nearbyLight.intensity = 55;
            nearbyLight.color = new Color(1f,.92f,.78f);
            nearbyLight.shadows = LightShadows.Soft;
            nearbyLight.shadowNearPlane = .05f;
            nearbyLight.enabled = flashlightOn;
            nearbyLight.GetUniversalAdditionalLightData().additionalLightsShadowResolutionTier = 0;
        }
        if (!weaponHud)
        {
            var animator = Object.FindAnyObjectByType<WeaponSpriteAnimator>();
            if (animator && animator.WeaponImage)
            {
                weaponHud = animator.WeaponImage;
                weaponHudColor = weaponHud.color;
            }
        }
    }

    void TintActors()
    {
        foreach (var actor in actors)
            if (actor.renderer)
            {
                // Proximity and flashlight occlusion only affect the blackout tint.
                if (!IsBlackout)
                {
                    actor.renderer.color = actor.color * SpriteRed;
                    continue;
                }
                float proximity = view ? Mathf.Clamp01(1-Vector3.Distance(actor.renderer.bounds.center,view.transform.position)/nearbyVisibilityRange) : 0;
                if (flashlightOn && view && nearbyLight)
                {
                    Vector3 delta = actor.renderer.bounds.center - view.transform.position;
                    float cone = Mathf.InverseLerp(Mathf.Cos(29 * Mathf.Deg2Rad), Mathf.Cos(15 * Mathf.Deg2Rad),
                        Vector3.Dot(view.transform.forward, delta.normalized));
                    float beam = cone * Mathf.Clamp01(1 - delta.magnitude / nearbyLight.range);
                    bool blocked = Physics.Linecast(view.transform.position, actor.renderer.bounds.center,
                        out var obstruction, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    if (!blocked || (obstruction.collider &&
                        obstruction.collider.GetComponentInParent<Enemy>() == actor.renderer.GetComponentInParent<Enemy>()))
                        proximity = Mathf.Max(proximity, beam);
                }
                actor.renderer.color = actor.color * (IsBlackout ? new Color(.50f,.55f,.65f,1)*new Color(proximity,proximity,proximity,1) : SpriteRed);
            }
    }

    void TryNextArrival()
    {
        var prefab = NextZombiePrefab();
        if (!prefab) return;
        var player = GameManager.SessionPlayer;
        if (!player) return;
        if (!view) view = Camera.main;
        for (int i = 0; i < arrived.Length; i++)
        {
            var point = arrivalPoints[i];
            if (arrived[i] || !point || !SafeArrival(point.position, player.transform.position,prefab)) continue;
            var enemy = Instantiate(prefab, point.position, point.rotation);
            SceneManager.MoveGameObjectToScene(enemy, gameObject.scene);
            enemy.transform.SetParent(transform, true); // Keep the enemy's original world scale, not the building's 1.25x.
            enemy.name = "Alarme — zumbi " + (i + 1);
            foreach (var sprite in enemy.GetComponentsInChildren<SpriteRenderer>())
                actors.Add(new SpriteState { renderer = sprite, color = sprite.color });
            arrived[i] = true;
            SpawnedCount++;
            TintActors();
            return;
        }
    }

    GameObject NextZombiePrefab()
    {
        if (zombieVariants != null)
            for (int i = 0; i < zombieVariants.Length; i++)
            {
                var prefab = zombieVariants[(SpawnedCount+variantOffset+i)%zombieVariants.Length];
                if (prefab) return prefab;
            }
        return zombiePrefab;
    }

    bool SafeArrival(Vector3 position, Vector3 player, GameObject prefab)
    {
        var offset = position - player;
        offset.y = 0;
        if (offset.sqrMagnitude < minimumArrivalDistance * minimumArrivalDistance) return false;
        var capsule = prefab.GetComponent<CapsuleCollider>();
        float radius = capsule ? capsule.radius+.03f : .62f;
        float segment = capsule ? Mathf.Max(0,capsule.height*.5f-radius-.04f) : .40f;
        var center = position + (capsule ? capsule.center : Vector3.zero);
        if (Physics.CheckCapsule(center-Vector3.up*segment,center+Vector3.up*segment,
                radius,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)) return false;
        // Tall/wide variants need full-body coverage; other zombies cannot hide a visible spawn.
        return EnemySpawnSafety.CanSpawn(position,GameManager.SessionPlayer.transform,minimumArrivalDistance);
    }

    void OnDisable() => RestorePresentation();

    void RestorePresentation()
    {
        if (!IsActive) return;
        if (siren) siren.Stop();
        if (nearbyLight) Destroy(nearbyLight.gameObject);
        if (weaponHud) weaponHud.color = weaponHudColor;
        foreach (var state in lights)
        {
            if (!state.light) continue;
            state.light.color = state.color;
            state.light.intensity = state.intensity;
            state.light.useColorTemperature = state.temperature;
            state.light.enabled = state.enabled;
        }
        foreach (var surface in surfaces) surface.Restore();
        foreach (var actor in actors) if (actor.renderer) actor.renderer.color = actor.color;
        if (lighting) lighting.SetAlarmPhase(false, false);
        lights.Clear(); surfaces.Clear(); actors.Clear();
        IsActive = false;
        StateChanged?.Invoke();
    }
}
