using UnityEngine;

public sealed class AK47 : Gun
{
    [Header("AK-47")]
    [SerializeField] private float damage = 18f;
    [SerializeField] private float range = 100f;
    [SerializeField, Min(.01f)] private float fireInterval = .1f;
    [SerializeField] private Camera playerCamera;

    private float nextShotAt;

    protected override void Awake()
    {
        base.Awake();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    protected override bool CanShoot()
    {
        return Time.time >= nextShotAt &&
            TryResolvePlayerCamera(ref playerCamera);
    }

    protected override void Shoot()
    {
        nextShotAt = Time.time + fireInterval;
        Ray ray = CrosshairAim.CreateRay(playerCamera);

        if (WeaponHitQuery.TryRaycast(ray, range, GetComponentInParent<PlayerLife>()?.transform,
            out RaycastHit hit))
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null && !damageable.IsDead)
                damageable.TakeDamage(damage, gameObject);
        }

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayPistolShootSound();
    }
}
