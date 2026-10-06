using UnityEngine;

public class SFXManager : SFXPlayer
{
    public static SFXManager Instance { get; private set; }

    [Header("Sons globais")] [SerializeField]
    private SoundBank KeyPickupSound;

    private SoundBank PistolShootSound;
    private SoundBank PistolRechargeSound;
    private SoundBank ShotGunShootSound;
    private SoundBank ShotgunRechargeSound;
    

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
    public void PlayShotgunShootSound() => Play(ShotGunShootSound);
    public void PlayShotgunRechargeSound() => Play(ShotgunRechargeSound);
}