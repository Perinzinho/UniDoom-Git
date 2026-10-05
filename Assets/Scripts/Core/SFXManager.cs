using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Header("Sons")]
    [SerializeField] private AudioClip walkConcrete;
    [SerializeField] private AudioClip walkEco;
    [SerializeField] private AudioClip walkWood;

    [Header("Config")]
    [SerializeField] private AudioSource source;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private void Awake()
    {
        // Singleton: só existe um manager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // mantém entre cenas
    }

    public void PlayWalkConcrete() => Play(walkConcrete);
    public void PlaywalkEco() => Play(walkEco);
    public void PlayWalkWood()  => Play(walkWood);

    public void Play(AudioClip clip, float pitchVariation = 0.1f)
    {
        if (clip == null) return;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, volume);
    }
}