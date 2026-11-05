using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>A dodgeable, straight-flying enemy shot with swept collision against the world.</summary>
public sealed class EnemyFanProjectile : MonoBehaviour
{
    Enemy owner;
    Vector3 velocity;
    float damage, radius, expiresAt;
    SpriteRenderer visual;
    Camera viewCamera;
    readonly RaycastHit[] hits = new RaycastHit[32];
    readonly Collider[] overlaps = new Collider[32];

    public static EnemyFanProjectile Launch(Enemy owner, EnemySpriteAppearance appearance,
        Vector3 origin, Vector3 direction, float damage)
    {
        var shot = new GameObject("Enemy Fan Projectile").AddComponent<EnemyFanProjectile>();
        shot.transform.position = origin;
        SceneManager.MoveGameObjectToScene(shot.gameObject, owner.gameObject.scene);
        shot.owner = owner;
        shot.velocity = direction.normalized * appearance.projectileSpeed;
        shot.damage = damage;
        shot.radius = appearance.projectileRadius;
        shot.expiresAt = Time.time + appearance.projectileLifetime;
        shot.visual = shot.gameObject.AddComponent<SpriteRenderer>();
        shot.visual.sprite = appearance.projectileSprite;
        shot.visual.sharedMaterial = appearance.material;
        shot.transform.localScale = Vector3.one * appearance.projectileScale;
        return shot;
    }

    bool Ignore(Collider collider) => collider.GetComponentInParent<Enemy>() != null;

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (UniDoom.UI.DoorReloadScreen.IsReloading) { Destroy(gameObject); return; }
        if (Time.time >= expiresAt) { Destroy(gameObject); return; }

        // Check the starting volume too: casts alone miss a player already overlapping the shot.
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, overlaps,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Collider initialHit = null;
        for (int i = 0; i < count; i++)
        {
            if (Ignore(overlaps[i])) continue;
            if (initialHit == null || overlaps[i].GetComponentInParent<PlayerLife>() == null)
                initialHit = overlaps[i];
        }
        if (count == overlaps.Length) { Destroy(gameObject); return; }
        if (initialHit != null) { Impact(initialHit); return; }

        Vector3 step = velocity * Time.deltaTime;
        count = Physics.SphereCastNonAlloc(transform.position, radius, velocity.normalized, hits,
            step.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Collider nearest = null;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            if (Ignore(hits[i].collider) || hits[i].distance >= distance) continue;
            nearest = hits[i].collider;
            distance = hits[i].distance;
        }
        // On overflow, stop rather than risk passing through an omitted wall.
        if (count == hits.Length) { Destroy(gameObject); return; }
        if (nearest != null) { Impact(nearest); return; }
        transform.position += step;
    }

    void Impact(Collider collider)
    {
        var player = collider.GetComponentInParent<PlayerLife>();
        if (player != null && !player.IsDead)
            ((IDamageable)player).TakeDamage(damage, owner != null ? owner.gameObject : null);
        Destroy(gameObject);
    }

    void LateUpdate()
    {
        if (viewCamera == null || !viewCamera.isActiveAndEnabled) viewCamera = Camera.main;
        if (viewCamera == null) return;
        Vector3 toCamera = viewCamera.transform.position - transform.position;
        if (toCamera.sqrMagnitude > .001f)
            transform.rotation = Quaternion.LookRotation(-toCamera);
        visual.flipX = Vector3.Dot(viewCamera.transform.right, velocity) < 0f;
    }
}
