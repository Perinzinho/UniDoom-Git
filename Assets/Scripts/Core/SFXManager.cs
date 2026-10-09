using UnityEngine;

public class SFXManager : SFXPlayer
{
    public static SFXManager Instance { get; private set; }

    [Header("Sons globais")] [SerializeField]
    private SoundBank KeyPickupSound;

    [Header("Som Pistola")] 
    [SerializeField] private SoundBank PistolShootSound;
    [SerializeField] private SoundBank PistolRechargeSound;
    [SerializeField] private SoundBank PistolEmptySound;
    
    [Header("Som Shotgun")]
    [SerializeField] private SoundBank ShotGunShootSound;
    [SerializeField] private SoundBank ShotgunRechargeSound;
    [SerializeField] private SoundBank ShotgunEmptySound;
    

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
    public void PlayPistolShootSound() => Play(PistolShootSound);
    public void PlayPistolRechargeSound() => Play(PistolRechargeSound);
    public void PlayEmptyShootSound() => Play(PistolEmptySound);
    public void PlayShotgunShootSound() => Play(ShotGunShootSound);
    public void PlayShotgunRechargeSound() => Play(ShotgunRechargeSound);
    public void PlayShotgunEmptySound() => Play(ShotgunEmptySound);
}