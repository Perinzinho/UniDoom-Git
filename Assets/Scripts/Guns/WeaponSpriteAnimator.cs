using UnityEngine;
using UnityEngine.UI;

public class WeaponSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Gun gun;
    [SerializeField] private Image weaponImage;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] shootFrames;
    [SerializeField] private Sprite[] rechargeFrames;
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

        // Se a arma já existir na cena (não instanciada em runtime), inscreve direto.
        if (gun != null)
            gun.OnShoot += PlayShootAnimation;
    }

    // Chamado de fora (pelo Spawner/GameManager) depois que o Player é instanciado.
    public void SetGun(Gun newGun)
    {
        // Se já tinha uma arma inscrita antes, desinscreve pra não duplicar eventos.
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