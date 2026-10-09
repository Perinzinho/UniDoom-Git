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
    private Vector2 defaultHudPosition;
    private Vector2 defaultHudSize;
    private bool hasDefaultHudLayout;

    void Start()
    {
        currentAnimation = idleFrames;
        currentFrameRate = idleFrameRate;
        if (currentAnimation != null && currentAnimation.Length > 0 && weaponImage != null)
            weaponImage.sprite = currentAnimation[0];

        UnsubscribeFromGun(gun); // evita duplicar inscrição se SetGun já rodou antes deste Start()
        SubscribeToGun(gun);
    }

    // Chamado de fora (pelo Spawner/GameManager) depois que o Player é instanciado.
    public void SetGun(Gun newGun)
    {
        SetGun(newGun, null);
    }

    public void SetGun(Gun newGun, WeaponDefinition definition)
    {
        UnsubscribeFromGun(gun);
        gun = newGun;
        if (definition != null)
        {
            idleFrames = definition.IdleFrames;
            shootFrames = definition.ShootFrames;
            rechargeFrames = definition.RechargeFrames;
            idleFrameRate = definition.IdleFrameRate;
            shootFrameRate = definition.ShootFrameRate;
            rechargeFrameRate = definition.RechargeFrameRate;
        }
        ApplyHudLayout(definition);
        isShooting = false;
        isReloading = false;
        currentFrame = 0;
        timer = 0;
        currentAnimation = idleFrames;
        currentFrameRate = idleFrameRate;
        if (weaponImage != null)
            weaponImage.sprite = idleFrames != null && idleFrames.Length > 0 ? idleFrames[0] : null;
        if (isActiveAndEnabled)
            SubscribeToGun(gun);
    }

    void OnEnable()
    {
        UnsubscribeFromGun(gun);
        SubscribeToGun(gun);
    }

    private void ApplyHudLayout(WeaponDefinition definition)
    {
        if (weaponImage == null)
            return;

        RectTransform imageTransform = weaponImage.rectTransform;
        if (!hasDefaultHudLayout)
        {
            defaultHudPosition = imageTransform.anchoredPosition;
            defaultHudSize = imageTransform.sizeDelta;
            hasDefaultHudLayout = true;
        }

        // Always start from the prefab position so switching weapons never accumulates offsets.
        imageTransform.anchoredPosition = defaultHudPosition +
            (definition != null ? definition.HudOffset : Vector2.zero);
        Vector2 configuredSize = definition != null ? definition.HudSize : Vector2.zero;
        imageTransform.sizeDelta = configuredSize.x > 0f && configuredSize.y > 0f
            ? configuredSize
            : defaultHudSize;
    }

    void OnDisable()
    {
        UnsubscribeFromGun(gun);
    }

    private void SubscribeToGun(Gun targetGun)
    {
        if (targetGun == null)
            return;

        if (rechargeFrames != null && rechargeFrames.Length > 0)
            targetGun.SetReloadCycleDuration(rechargeFrames.Length / Mathf.Max(rechargeFrameRate, 1f));

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
        if (currentAnimation == null || currentAnimation.Length == 0 || weaponImage == null)
            return;

        float frameInterval = 1f / Mathf.Max(currentFrameRate, 1f);

        timer += Time.deltaTime;
        if (isReloading)
        {
            if (gun is Shotgun && gun.ReloadRoundCount > 0)
            {
                // One complete cycle per shell, using the same clock as ammo insertion.
                // Hold the final frame instead of starting an extra cycle at the deadline.
                int totalFrames = gun.ReloadRoundCount * currentAnimation.Length;
                int reloadFrame = Mathf.Min(
                    Mathf.FloorToInt(gun.ReloadElapsedTime * currentFrameRate), totalFrames - 1);
                currentFrame = reloadFrame % currentAnimation.Length;
                weaponImage.sprite = currentAnimation[currentFrame];
                return;
            }

            // Loop at the configured speed until OnReloadFinished stops the animation.
            int framesToAdvance = Mathf.FloorToInt(timer / frameInterval);
            timer -= framesToAdvance * frameInterval;
            currentFrame = (currentFrame + framesToAdvance) % currentAnimation.Length;
            weaponImage.sprite = currentAnimation[currentFrame];
            return;
        }

        if (timer >= frameInterval)
        {
            timer = 0;

            weaponImage.sprite = currentAnimation[currentFrame];

            currentFrame++;

            if (currentFrame >= currentAnimation.Length)
            {
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
        if (isReloading || shootFrames == null || shootFrames.Length == 0)
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
        if (isReloading || rechargeFrames == null || rechargeFrames.Length == 0)
            return;

        isReloading = true;
        isShooting = false;
        currentAnimation = rechargeFrames;
        currentFrameRate = rechargeFrameRate;
        currentFrame = 0;
        timer = 0;

        if (currentAnimation != null && currentAnimation.Length > 0 && weaponImage != null)
            weaponImage.sprite = currentAnimation[0];
    }

    void StopRechargeAnimation()
    {
        isReloading = false;
        currentAnimation = idleFrames;
        currentFrameRate = idleFrameRate;
        currentFrame = 0;
        timer = 0;

        if (currentAnimation != null && currentAnimation.Length > 0 && weaponImage != null)
            weaponImage.sprite = currentAnimation[0];
    }
}
