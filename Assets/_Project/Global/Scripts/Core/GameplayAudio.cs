using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniDoom.UI;

/// <summary>Persistent event audio, independent of pickups and scene SFX source lifetimes.</summary>
public sealed class GameplayAudio : MonoBehaviour
{
    public static GameplayAudio Instance { get; private set; }
    public GameplayAudioSettings Settings { get; private set; }
    AudioSource pickups, room, nearby, music, doors;
    DirectorBoss musicOwner;
    Coroutine pendingEntry;
    float nextNearby;
    readonly Dictionary<Object, float> doorContacts = new Dictionary<Object, float>();
    readonly Dictionary<Object, float> nextLockedDoorSound = new Dictionary<Object, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (Instance == null) new GameObject("[UniDoom Gameplay Audio]").AddComponent<GameplayAudio>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Settings = Resources.Load<GameplayAudioSettings>("Audio/GameplayAudioSettings");
        if (Settings == null)
        {
            Debug.LogError("Gameplay audio settings missing at Resources/Audio/GameplayAudioSettings.", this);
            enabled = false;
            return;
        }
        pickups = CreateSource("Pickups");
        room = CreateSource("Room entry");
        nearby = CreateSource("Nearby zombie");
        music = CreateSource("Boss music");
        doors = CreateSource("Doors");
        music.loop = true;
        music.priority = 32;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    AudioSource CreateSource(string label)
    {
        var child = new GameObject(label);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        // All channels obey the existing master volume and menu pause.
        source.ignoreListenerPause = false;
        return source;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingEntry != null) StopCoroutine(pendingEntry);
        pendingEntry = null;
        room.Stop();
        nearby.Stop();
        nextNearby = 0;
        doorContacts.Clear();
        nextLockedDoorSound.Clear();
        if (Settings.HasRoomCue(scene.name)) pendingEntry = StartCoroutine(PlayEntryWhenReady(scene));
    }

    IEnumerator PlayEntryWhenReady(Scene scene)
    {
        // sceneLoaded precedes player creation and the loading screen's fade-out.
        yield return null;
        while (scene.isLoaded && (DoorReloadScreen.IsReloading || GameMenus.BlocksGameplay ||
               GameManager.SessionPlayer == null)) yield return null;
        if (scene.isLoaded && SceneManager.GetActiveScene() == scene)
            Play(room, Settings.zombieRoomEntry, Settings.roomVolume);
        pendingEntry = null;
    }

    void OnSceneUnloaded(Scene scene)
    {
        // Keep the music when walking around the arena; release it when that encounter unloads.
        if (musicOwner == null || musicOwner.gameObject.scene == scene) StopBossMusic();
    }

    public void PlayAmmoPickup()
    {
        if (enabled && Settings != null) Play(pickups, Settings.ammoPickup, Settings.pickupVolume);
    }

    public void PlayMedkitPickup()
    {
        if (enabled && Settings != null) Play(pickups, Settings.medkitPickup, Settings.pickupVolume);
    }

    public bool TryPlayNearbyZombie()
    {
        if (!enabled || GameMenus.BlocksGameplay || DoorReloadScreen.IsReloading || DeathScreen.IsVisible ||
            Time.time < nextNearby || nearby.isPlaying || Settings.zombieNearby == null) return false;
        Play(nearby, Settings.zombieNearby, Settings.nearbyVolume);
        nextNearby = Time.time + Mathf.Max(Settings.nearbyCooldown, Settings.zombieNearby.length);
        return true;
    }

    public void PlayDoorOpening()
    {
        if (enabled && Settings != null && !GameMenus.BlocksGameplay && !DeathScreen.IsVisible)
            Play(doors, Settings.doorOpening, Settings.doorVolume);
    }

    public void NotifyLockedDoorContact(Object door)
    {
        if (!enabled || Settings == null || door == null || Settings.doorLocked == null ||
            GameMenus.BlocksGameplay || DoorReloadScreen.IsReloading || DeathScreen.IsVisible || Time.timeScale <= 0)
            return;
        bool continuous = doorContacts.TryGetValue(door, out float lastContact) && Time.time - lastContact < .3f;
        doorContacts[door] = Time.time;
        // Multiple colliders and continued pushing count as one bump, not a repeating alarm.
        if (continuous || (nextLockedDoorSound.TryGetValue(door, out float next) && Time.time < next)) return;
        nextLockedDoorSound[door] = Time.time + Mathf.Max(.6f, Settings.doorLocked.length);
        Play(doors, Settings.doorLocked, Settings.doorVolume);
    }

    public void StartBossMusic(DirectorBoss boss)
    {
        if (!enabled || boss == null || boss.IsDead || musicOwner == boss) return;
        StopBossMusic();
        musicOwner = boss;
        musicOwner.Defeated += StopBossMusic;
        music.clip = Settings.bossMusic;
        music.volume = Settings.musicVolume;
        if (music.clip != null) music.Play();
    }

    public void StopBossMusic()
    {
        if (musicOwner != null) musicOwner.Defeated -= StopBossMusic;
        musicOwner = null;
        if (music != null) { music.Stop(); music.clip = null; }
    }

    public void EndSession()
    {
        if (pendingEntry != null) StopCoroutine(pendingEntry);
        pendingEntry = null;
        StopBossMusic();
        if (pickups != null) pickups.Stop();
        if (room != null) room.Stop();
        if (nearby != null) nearby.Stop();
        if (doors != null) doors.Stop();
        doorContacts.Clear();
        nextLockedDoorSound.Clear();
        nextNearby = 0;
    }

    static void Play(AudioSource source, AudioClip clip, float volume)
    {
        if (source != null && clip != null) source.PlayOneShot(clip, volume);
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        StopBossMusic();
        Instance = null;
    }
}
