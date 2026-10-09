using UnityEngine;

public abstract class SFXPlayer : MonoBehaviour
{
    [Header("Áudio (base)")]
    [Tooltip("Opcional: usada só para copiar o Mixer Group.")]
    [SerializeField] private AudioSource template;
    [SerializeField, Min(1)] private int poolSize = 6;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float spatialBlend = 0f; // 0 = 2D

    private AudioSource[] pool;
    private int poolIndex;

    protected virtual void Awake()
    {
        pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = spatialBlend;
            if (template != null)
                src.outputAudioMixerGroup = template.outputAudioMixerGroup;
            pool[i] = src;
        }
    }

    public virtual void Play(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        var src = pool[poolIndex];
        poolIndex = (poolIndex + 1) % pool.Length;

        src.clip = clip;
        src.volume = volume * masterVolume;
        src.pitch = pitch;
        src.Play();
    }

    public virtual void Play(SoundBank bank, float volumeMultiplier = 1f)
    {
        if (bank == null || bank.IsEmpty) return;
        Play(bank.Next(), bank.RandomVolume() * volumeMultiplier, bank.RandomPitch());
    }
}