using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class WeaponSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Gun gun;
    [SerializeField] private Image weaponImage;
    public Image WeaponImage => weaponImage;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] shootFrames;
    [SerializeField] private Sprite[] rechargeFrames;
    [SerializeField] private float idleFrameRate = 12f;
    [SerializeField] private float shootFrameRate = 20f;
    [SerializeField] private float rechargeFrameRate = 5f;
    private Axe axe;
    private float[] shootFrameDurations;
    private Vector2 hudViewportLimit = new Vector2(.36f, .42f);
    private float minimumRightMargin = 16f;

    private Sprite[] currentAnimation;
    private float currentFrameRate;
    private int currentFrame;
    private float timer;
    private bool isShooting;
    private bool isReloading;
    private Vector2 defaultHudPosition;
    private Vector2 defaultHudSize;
    private bool hasDefaultHudLayout;
    private Vector2 requestedHudPosition, requestedHudSize, fittedParentSize;
    private RectTransform fittedParent;
    private bool hudLayoutDirty;
    private Sprite fittedSprite;
    private bool attachToScreenEdges;
    private WeaponDefinition hudDefinition;
    private Vector2 animationCanvas;
    private Vector2 fittedHudPosition;
    private Vector2 weaponBobOffset;
    private PlayerMovement bobPlayer;
    private readonly Dictionary<Sprite, Sprite> hudFrames = new Dictionary<Sprite, Sprite>();

    [Header("Doom-style walk bob")]
    [SerializeField] private Vector2 walkBobAmplitude = new Vector2(14f, 8f);
    [SerializeField, Min(.01f)] private float walkBobBlendSpeed = 14f;

    void Start()
    {
        if (!hasDefaultHudLayout) ApplyHudLayout(null);
        currentAnimation = idleFrames;
        currentFrameRate = idleFrameRate;
        if (currentAnimation != null && currentAnimation.Length > 0 && weaponImage != null)
            weaponImage.sprite = currentAnimation[0];

        UnsubscribeFromGun(gun); // evita duplicar inscrição se SetGun já rodou antes deste Start()
        SubscribeToGun(gun);
        SubscribeToAxe();
    }

    // Chamado de fora (pelo Spawner/GameManager) depois que o Player é instanciado.
    public void SetGun(Gun newGun)
    {
        SetGun(newGun, null);
    }

    public void SetGun(Gun newGun, WeaponDefinition definition)
    {
        UnsubscribeFromAxe();
        axe = null;
        UnsubscribeFromGun(gun);
        gun = newGun;
        if (definition != null)
        {
            idleFrames = definition.IdleFrames;
            shootFrames = definition.ShootFrames;
            rechargeFrames = definition.RechargeFrames;
            idleFrameRate = definition.IdleFrameRate;
            shootFrameRate = definition.ShootFrameRate;
            shootFrameDurations = definition.ShootFrameDurations;
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

    public void SetAxe(Axe newAxe, WeaponDefinition definition)
    {
        SetGun(null, definition);
        axe = newAxe;
        if (isActiveAndEnabled) SubscribeToAxe();
    }

    void OnEnable()
    {
        UnsubscribeFromGun(gun);
        SubscribeToGun(gun);
        SubscribeToAxe();
    }

    private void SubscribeToAxe()
    {
        if (axe == null) return;
        UnsubscribeFromAxe();
        axe.OnSwing += PlayShootAnimation;
        axe.OnSwingFinished += StopRechargeAnimation;
    }

    private void UnsubscribeFromAxe()
    {
        if (axe == null) return;
        axe.OnSwing -= PlayShootAnimation;
        axe.OnSwingFinished -= StopRechargeAnimation;
    }

    private void ApplyHudLayout(WeaponDefinition definition)
    {
        if (weaponImage == null)
            return;
        ClearHudFrames();
        hudDefinition = definition;
        animationCanvas = Vector2.zero;
        foreach (var frames in new[] { idleFrames, shootFrames, rechargeFrames })
            if (frames != null)
                foreach (var frame in frames) if (frame) animationCanvas = Vector2.Max(animationCanvas, frame.rect.size);

        RectTransform imageTransform = weaponImage.rectTransform;
        if (!hasDefaultHudLayout)
        {
            defaultHudPosition = imageTransform.anchoredPosition;
            defaultHudSize = imageTransform.sizeDelta;
            hasDefaultHudLayout = true;
        }

        // Keep every animation frame inside the lower-right safe area, including reload hands.
        imageTransform.anchorMin = imageTransform.anchorMax = imageTransform.pivot = new Vector2(1, 0);
        weaponImage.preserveAspect = true;
        weaponImage.raycastTarget = false;
        hudViewportLimit = definition != null ? definition.HudViewportLimit : new Vector2(.36f, .42f);
        attachToScreenEdges = definition != null && (definition.HudAttachToScreenEdges || definition.AxePrefab != null);
        minimumRightMargin = attachToScreenEdges ? 0f : 16f;
        // Melee poses are cut at the original screen edge; a firearm's inset exposes that cut.
        requestedHudPosition = (definition != null && definition.AxePrefab != null ? Vector2.zero : defaultHudPosition) +
            (definition != null ? definition.HudOffset : Vector2.zero);
        if (attachToScreenEdges) requestedHudPosition = Vector2.zero;
        Vector2 configuredSize = definition != null ? definition.HudSize : Vector2.zero;
        requestedHudSize = configuredSize.x > 0f && configuredSize.y > 0f
            ? configuredSize
            : defaultHudSize;
        hudLayoutDirty = true;
        FitHudLayout();
    }

    private void LateUpdate()
    {
        FitHudLayout();
        ApplyWalkBob();
    }

    private void FitHudLayout()
    {
        if (!hasDefaultHudLayout || weaponImage == null) return;
        var imageRect = weaponImage.rectTransform;
        var parent = imageRect.parent as RectTransform;
        if (parent == null) return;
        Vector2 available = parent.rect.size;
        if (available.x <= 0 || available.y <= 0) return;
        if (!hudLayoutDirty && fittedParent == parent && fittedParentSize == available && fittedSprite == weaponImage.sprite) return;
        fittedParent = parent;
        fittedParentSize = available;
        hudLayoutDirty = false;
        fittedSprite = weaponImage.sprite;

        // Reserve the centre for aiming and cap width independently on narrower monitors.
        var displaySprite = HudSprite(fittedSprite);
        weaponImage.overrideSprite = displaySprite;
        Vector2 frame = displaySprite ? displaySprite.rect.size : requestedHudSize;
        Rect fit = CalculateHudRect(available, requestedHudSize, frame, hudViewportLimit,
            requestedHudPosition, minimumRightMargin, animationCanvas);
        Vector2 size = fit.size;
        imageRect.sizeDelta = size;
        // The original shotgun art cuts the receiver above the bottom of the arm.
        // Hide that entire cut below the screen, instead of exposing it in the HUD.
        float crop = hudDefinition ? hudDefinition.HudBottomCropFraction : 0f;
        imageRect.localScale = Vector3.one;
        fittedHudPosition = new Vector2(fit.xMax - available.x, fit.yMin - size.y * crop);
        imageRect.anchoredPosition = fittedHudPosition + weaponBobOffset;
    }

    private void ApplyWalkBob()
    {
        if (!hasDefaultHudLayout || weaponImage == null) return;
        if (bobPlayer == null) bobPlayer = GameManager.SessionPlayer;

        Vector2 target = Vector2.zero;
        if (bobPlayer != null)
        {
            float phase = bobPlayer.WalkBobPhase;
            float weight = bobPlayer.WalkBobWeight;
            // Keep the configured weapon position as the rest pose; walking only swings and dips it.
            target.x = Mathf.Sin(phase) * walkBobAmplitude.x * weight;
            target.y = (Mathf.Cos(phase * 2f) - 1f) * walkBobAmplitude.y * weight;
        }

        float blend = 1f - Mathf.Exp(-walkBobBlendSpeed * Time.unscaledDeltaTime);
        weaponBobOffset = Vector2.Lerp(weaponBobOffset, target, blend);
        weaponImage.rectTransform.anchoredPosition = fittedHudPosition + weaponBobOffset;
    }

    // Match the actual sprite aspect before fitting: preserveAspect alone centres a smaller
    // drawing inside a mismatched rectangle, exposing a gap beneath cropped first-person arms.
    public static Rect CalculateHudRect(Vector2 available, Vector2 requested, Vector2 frame,
        Vector2 viewportLimit, Vector2 position, float rightMargin, Vector2 animationCanvas = default)
    {
        if (animationCanvas.x <= 0 || animationCanvas.y <= 0) animationCanvas = frame;
        float pixelScale = Mathf.Min(requested.x / Mathf.Max(1, animationCanvas.x),
            requested.y / Mathf.Max(1, animationCanvas.y));
        Vector2 envelope = animationCanvas * pixelScale;
        float scale = Mathf.Min(1, Mathf.Min(available.x * viewportLimit.x / Mathf.Max(1, envelope.x),
            available.y * viewportLimit.y / Mathf.Max(1, envelope.y)));
        var size = frame * (pixelScale * scale);
        float right = Mathf.Clamp(-position.x, rightMargin, Mathf.Max(rightMargin, available.x - size.x));
        float bottom = Mathf.Clamp(position.y, 0, Mathf.Max(0, available.y - size.y));
        return new Rect(available.x - right - size.x, bottom, size.x, size.y);
    }

    Sprite HudSprite(Sprite source)
    {
        if (!source || !hudDefinition) return source;
        Rect rect = hudDefinition.HudFrameRect(source);
        if (rect == source.rect) return source;
        if (!hudFrames.TryGetValue(source, out var cropped))
        {
            cropped = Sprite.Create(source.texture, rect, new Vector2(.5f, .5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            cropped.name = source.name + " — HUD opaque bounds";
            cropped.hideFlags = HideFlags.DontSave;
            hudFrames.Add(source, cropped);
        }
        return cropped;
    }

    void ClearHudFrames()
    {
        if (weaponImage) weaponImage.overrideSprite = null;
        foreach (var frame in hudFrames.Values)
            if (frame) { if (Application.isPlaying) Destroy(frame); else DestroyImmediate(frame); }
        hudFrames.Clear();
    }

    void OnDestroy() => ClearHudFrames();

    void OnDisable()
    {
        UnsubscribeFromGun(gun);
        UnsubscribeFromAxe();
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

        if (axe != null)
        {
            UpdateAxePose();
            return;
        }

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
            int advance = Mathf.FloorToInt(timer / frameInterval);
            timer -= advance * frameInterval;
            currentFrame += advance;

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
            if (currentAnimation != null && currentAnimation.Length > 0)
                weaponImage.sprite = currentAnimation[currentFrame];
        }
    }

    private void UpdateAxePose()
    {
        if (!axe.IsAttacking || shootFrames == null || shootFrames.Length == 0)
        {
            if (idleFrames != null && idleFrames.Length > 0) weaponImage.sprite = idleFrames[0];
            return;
        }
        float elapsed = axe.SwingElapsedTime;
        int pose = 0;
        if (shootFrameDurations != null && shootFrameDurations.Length == shootFrames.Length)
        {
            float end = Mathf.Max(.001f, shootFrameDurations[0]);
            while (pose < shootFrames.Length - 1 && elapsed >= end)
                end += Mathf.Max(.001f, shootFrameDurations[++pose]);
        }
        else pose = Mathf.Min(Mathf.FloorToInt(elapsed * shootFrameRate), shootFrames.Length - 1);
        weaponImage.sprite = shootFrames[pose];
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
        isShooting = false;
        currentAnimation = idleFrames;
        currentFrameRate = idleFrameRate;
        currentFrame = 0;
        timer = 0;

        if (currentAnimation != null && currentAnimation.Length > 0 && weaponImage != null)
            weaponImage.sprite = currentAnimation[0];
    }
}
