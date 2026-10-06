using UnityEngine;

public class SFXManager : SFXPlayer
{
    public static SFXManager Instance { get; private set; }

    [Header("Sons globais")] [SerializeField]
    private SoundBank KeyPickupSound;

    protected override void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        base.Awake();
    }
    public void PlayKeyPickupSound()  => Play(KeyPickupSound);
}