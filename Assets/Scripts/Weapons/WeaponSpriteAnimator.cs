using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class WeaponSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Gun gun;
    [SerializeField] private Image weaponImage;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] shootFrames;
    [FormerlySerializedAs("rechargeFrames")]
    [SerializeField] private Sprite[] reloadFrames;
    [SerializeField] private float frameRate = 12f;

    private Sprite[] currentAnimation;
    private int currentFrame;
    private float timer;
    private bool isShooting;

    void Start()
    {
        currentAnimation = idleFrames;
        if (currentAnimation.Length > 0)
            weaponImage.sprite = currentAnimation[0];

        // If weapon already exists in scene (not instantiated at runtime), subscribe directly.
        if (gun != null)
            gun.OnShoot += PlayShootAnimation;
    }

    // Called from outside (by Spawner/GameManager) after Player is instantiated.
    public void SetGun(Gun newGun)
    {
        // If previously subscribed, unsubscribe to avoid duplicate events.
        if (gun != null)
            gun.OnShoot -= PlayShootAnimation;

        gun = newGun;

        if (gun != null)
            gun.OnShoot += PlayShootAnimation;
    }

    void OnDisable()
    {
        if (gun != null)
            gun.OnShoot -= PlayShootAnimation;
    }

    void Update()
    {
        if (currentAnimation == null || currentAnimation.Length == 0)
            return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer = 0;

            weaponImage.sprite = currentAnimation[currentFrame];

            currentFrame++;

            if (currentFrame >= currentAnimation.Length)
            {
                currentFrame = 0;

                if (isShooting)
                {
                    isShooting = false;
                    currentAnimation = idleFrames;
                }
            }
        }
    }

    void PlayShootAnimation()
    {
        isShooting = true;
        currentAnimation = shootFrames;
        currentFrame = 0;
        timer = 0;

        weaponImage.sprite = currentAnimation[currentFrame];
    }
    
}
