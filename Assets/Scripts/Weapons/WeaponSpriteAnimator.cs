using UnityEngine;
using UnityEngine.UI;

public class WeaponSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Gun gun;
    [SerializeField] private Image weaponImage;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] shootFrames;
    [SerializeField] private Sprite[] rechargeFrames;
    [SerializeField] private float idleFrameRate = 12f;
    [SerializeField] private float shootFrameRate = 20f;
    [SerializeField] private float rechargeFrameRate = 5f;

    private Sprite[] currentAnimation;
    private float currentFrameRate;
    private int currentFrame;
    private float timer;
    private bool isShooting;
    private bool isReloading;

    void Start()
    {
        currentAnimation = idleFrames;
        currentFrameRate = idleFrameRate;
        if (currentAnimation.Length > 0)
            weaponImage.sprite = currentAnimation[0];

        UnsubscribeFromGun(gun); // evita duplicar inscrição se SetGun já rodou antes deste Start()
        SubscribeToGun(gun);
    }

    // Chamado de fora (pelo Spawner/GameManager) depois que o Player é instanciado.
    public void SetGun(Gun newGun)
    {
        UnsubscribeFromGun(gun);
        gun = newGun;
        SubscribeToGun(gun);
    }

    void OnDisable()
    {
        UnsubscribeFromGun(gun);
    }

    private void SubscribeToGun(Gun targetGun)
    {
        if (targetGun == null)
            return;

        targetGun.OnShoot += PlayShootAnimation;
        targetGun.OnReloadStarted += PlayRechargeAnimation;
        targetGun.OnReloadFinished += StopRechargeAnimation;
    }

    private void UnsubscribeFromGun(Gun targetGun)
    {
        if (targetGun == null)
            return;

        targetGun.OnShoot -= PlayShootAnimation;
        targetGun.OnReloadStarted -= PlayRechargeAnimation;
        targetGun.OnReloadFinished -= StopRechargeAnimation;
    }

    void Update()
    {
        if (currentAnimation == null || currentAnimation.Length == 0)
            return;

        float frameInterval = 1f / Mathf.Max(currentFrameRate, 1f);

        timer += Time.deltaTime;
        if (timer >= frameInterval)
        {
            timer = 0;

            weaponImage.sprite = currentAnimation[currentFrame];

            currentFrame++;

            if (currentFrame >= currentAnimation.Length)
            {
                if (isReloading)
                {
                    // Toca uma vez e segura o último frame até a recarga terminar.
                    currentFrame = currentAnimation.Length - 1;
                    return;
                }

                if (isShooting)
                {
                    isShooting = false;
                    currentAnimation = idleFrames;
                    currentFrameRate = idleFrameRate;
                }

                currentFrame = 0;
            }
        }
    }

    void PlayShootAnimation()
    {
        if (isReloading)
            return;

        isShooting = true;
        currentAnimation = shootFrames;
        currentFrameRate = shootFrameRate;
        currentFrame = 0;
        timer = 0;

        weaponImage.sprite = currentAnimation[currentFrame];
    }

    void PlayRechargeAnimation()
    {
        if (isReloading)
            return;

        isReloading = true;
        isShooting = false;
        currentAnimation = rechargeFrames;
        currentFrameRate = rechargeFrameRate;
        currentFrame = 0;
        timer = 0;

        if (currentAnimation.Length > 0)
            weaponImage.sprite = currentAnimation[0];
    }

    void StopRechargeAnimation()
    {
        isReloading = false;
        currentAnimation = idleFrames;
        currentFrameRate = idleFrameRate;
        currentFrame = 0;
        timer = 0;

        if (currentAnimation.Length > 0)
            weaponImage.sprite = currentAnimation[0];
    }
}