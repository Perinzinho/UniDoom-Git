using UnityEngine;

[CreateAssetMenu(menuName = "UniDoom/Gameplay Audio")]
public sealed class GameplayAudioSettings : ScriptableObject
{
    public AudioClip ammoPickup;
    public AudioClip medkitPickup;
    public AudioClip zombieRoomEntry;
    public AudioClip zombieNearby;
    public AudioClip bossMusic;
    public AudioClip doorLocked;
    public AudioClip doorOpening;
    [Range(0, 1)] public float doorVolume = .8f;
    [Range(0, 1)] public float pickupVolume = .85f;
    [Range(0, 1)] public float roomVolume = .7f;
    [Range(0, 1)] public float nearbyVolume = .8f;
    [Range(0, 1)] public float musicVolume = .45f;
    [Min(.1f)] public float nearbyDistance = 2.5f;
    [Min(.1f)] public float nearbyResetDistance = 3.5f;
    [Min(0)] public float nearbyCooldown = 6f;
    [Tooltip("Play the entrance cue on each visit, even before a room's future zombie spawns exist.")]
    public string[] zombieScenes = { "Cylinder", "Library", "ClassRoom", "DirectorRoom", "Basement", "SampleScene" };

    public bool HasRoomCue(string sceneName) => zombieScenes != null &&
        System.Array.IndexOf(zombieScenes, sceneName) >= 0;
}
