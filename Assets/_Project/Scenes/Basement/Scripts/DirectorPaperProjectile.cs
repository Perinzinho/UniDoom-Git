using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>A straight, swept projectile: scenery blocks it and each paper can hit only once.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class DirectorPaperProjectile : MonoBehaviour
{
    [SerializeField] Sprite[] flightFrames;
    [SerializeField] Sprite[] impactFrames;
    [SerializeField] Sprite screenPaper;
    [SerializeField, Min(0)] float damage = 20f;
    [SerializeField, Min(.1f)] float blindnessDuration = 2f;
    [SerializeField, Min(.1f)] float speed = 11f;
    [SerializeField, Min(.01f)] float hitRadius = .23f;
    [SerializeField, Min(.1f)] float lifetime = 5f;
    [SerializeField, Min(1)] float framesPerSecond = 14f;

    DirectorBoss owner;
    PlayerLife target;
    SpriteRenderer visual;
    Camera viewCamera;
    Vector3 direction;
    float age, impactAge;
    bool launched, impacted;
    readonly RaycastHit[] hits = new RaycastHit[32];
    readonly Collider[] overlaps = new Collider[32];

    public void Launch(DirectorBoss source, PlayerLife player, Vector3 heading)
    {
        owner = source;
        target = player;
        direction = heading.normalized;
        visual = GetComponent<SpriteRenderer>();
        viewCamera = Camera.main;
        launched = true;
        // The encounter can live in an additively loaded room. Unloading that room
        // must remove its projectiles even when the player survives the transition.
        if (owner != null && gameObject.scene != owner.gameObject.scene)
            SceneManager.MoveGameObjectToScene(gameObject, owner.gameObject.scene);
        SetFrame(flightFrames, 0);
    }

    void Update()
    {
        if (!launched) return;
        if (owner == null || owner.IsDead || !owner.isActiveAndEnabled || target == null ||
            target.IsDead || !target.gameObject.activeInHierarchy || UniDoom.UI.DoorReloadScreen.IsReloading)
        {
            Destroy(gameObject);
            return;
        }
        if (Time.deltaTime <= 0) return;
        if (impacted)
        {
            impactAge += Time.deltaTime;
            SetFrame(impactFrames, Mathf.FloorToInt(impactAge * framesPerSecond));
            if (impactFrames == null || impactAge >= impactFrames.Length / framesPerSecond) Destroy(gameObject);
            return;
        }

        age += Time.deltaTime;
        if (age >= lifetime) { Destroy(gameObject); return; }

        // Casts alone can miss a collider already overlapping the starting sphere.
        int overlapCount = Physics.OverlapSphereNonAlloc(transform.position, hitRadius, overlaps, ~0, QueryTriggerInteraction.Ignore);
        Collider[] candidates = overlapCount == overlaps.Length
            ? Physics.OverlapSphere(transform.position, hitRadius, ~0, QueryTriggerInteraction.Ignore) : overlaps;
        if (candidates != overlaps) overlapCount = candidates.Length;
        Collider initialHit = null;
        for (int i = 0; i < overlapCount; i++)
        {
            Collider candidate = candidates[i];
            if (Ignored(candidate)) continue;
            // Prefer solid scenery over a player touching the same wall.
            if (initialHit == null || candidate.GetComponentInParent<PlayerLife>() != target) initialHit = candidate;
        }
        if (initialHit != null) { Impact(initialHit); return; }

        float distance = speed * Time.deltaTime;
        int count = Physics.SphereCastNonAlloc(transform.position, hitRadius, direction, hits, distance, ~0, QueryTriggerInteraction.Ignore);
        RaycastHit[] sweep = count == hits.Length
            ? Physics.SphereCastAll(transform.position, hitRadius, direction, distance, ~0, QueryTriggerInteraction.Ignore) : hits;
        if (sweep != hits) count = sweep.Length;
        float nearest = float.PositiveInfinity;
        Collider collision = null;
        for (int i = 0; i < count; i++)
        {
            if (Ignored(sweep[i].collider) || sweep[i].distance >= nearest) continue;
            nearest = sweep[i].distance;
            collision = sweep[i].collider;
        }
        if (collision != null)
        {
            transform.position += direction * nearest;
            Impact(collision);
            return;
        }
        transform.position += direction * distance;
        // First launch frame leads into a loop of the approaching paper frames.
        int frame = Mathf.FloorToInt(age * framesPerSecond);
        int length = flightFrames == null ? 0 : flightFrames.Length;
        SetFrame(flightFrames, length > 1 && frame > 0 ? 1 + (frame - 1) % (length - 1) : 0);
    }

    bool Ignored(Collider collider) => collider == null || collider.transform.IsChildOf(transform) ||
        (owner != null && collider.transform.IsChildOf(owner.transform));

    void Impact(Collider collision)
    {
        if (impacted) return;
        impacted = true;
        PlayerLife victim = collision.GetComponentInParent<PlayerLife>();
        if (victim == target && !victim.IsDead)
        {
            int before = victim.CurrentHealth;
            ((IDamageable)victim).TakeDamage(damage, owner.gameObject);
            // Respect respawn protection and door transitions; blocked damage must
            // not leave a paper on screen. A fatal hit goes directly to the death UI.
            if (!victim.IsDead && victim.CurrentHealth < before && screenPaper != null)
            {
                DirectorPaperBlindness overlay = victim.GetComponent<DirectorPaperBlindness>();
                if (overlay == null) overlay = victim.gameObject.AddComponent<DirectorPaperBlindness>();
                overlay.Show(screenPaper, blindnessDuration);
            }
        }
        SetFrame(impactFrames, 0);
    }

    void LateUpdate()
    {
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera != null) transform.rotation = viewCamera.transform.rotation;
    }

    void SetFrame(Sprite[] frames, int index)
    {
        if (visual != null && frames != null && frames.Length > 0)
            visual.sprite = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
    }
}
