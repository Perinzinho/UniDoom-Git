using System;
using System.Collections.Generic;
using UnityEngine;

// Permanent melee weapon. It has no ammunition and never enters a pickup.
[DisallowMultipleComponent]
public sealed class Axe : MonoBehaviour
{
    [SerializeField, Min(1f)] private float damage = 55f;
    [Tooltip("Maximum melee reach. Zombies use player-to-target distance; the Director uses distance to its body collider.")]
    [SerializeField, Min(.1f)] private float range = 1.5f;
    [SerializeField, Min(0f)] private float hitRadius = .28f;
    [Tooltip("Extend the swing below eye level to catch shorter, seated enemies.")]
    [SerializeField, Min(0f)] private float hitHeight = .75f;
    [SerializeField, Min(.01f)] private float swingDuration = .72f;
    [SerializeField, Min(0f)] private float impactDelay = .24f;

    [Header("Attack Audio")]
    [SerializeField] private AudioClip swingSound;
    [SerializeField, Range(0f, 1f)] private float swingVolume = .85f;
    [Tooltip("Start the whoosh as the axe leaves its resting pose, using the animation clock.")]
    [SerializeField, Min(0f)] private float swingSoundDelay = .09f;

    private Camera playerCamera;
    private AudioSource swingAudio;
    private bool swingSoundPlayed;
    private Transform owner;
    private float nextSwingAt;
    private float swingStartedAt;
    private bool impactApplied;
    public bool IsAttacking { get; private set; }
    public float SwingElapsedTime => IsAttacking ? Mathf.Max(0f, Time.time - swingStartedAt) : 0f;
    public float SwingDuration => swingDuration;
    public event Action OnSwing;
    public event Action OnSwingFinished;

    private void Awake()
    {
        swingAudio = GetComponent<AudioSource>();
        if (swingAudio == null) swingAudio = gameObject.AddComponent<AudioSource>();
        swingAudio.playOnAwake = false;
        swingAudio.loop = false;
        swingAudio.spatialBlend = 0f;
        swingAudio.ignoreListenerPause = false;
        if (swingSound != null) swingSound.LoadAudioData();
    }

    public void SetOwner(Transform player) => owner = player;

    public bool TryAttack()
    {
        if (!isActiveAndEnabled || PlayerWeaponManager.InputBlocked || IsAttacking || Time.time < nextSwingAt)
            return false;
        if (playerCamera == null || !playerCamera.isActiveAndEnabled) playerCamera = Camera.main;
        if (playerCamera == null) return false;

        IsAttacking = true;
        swingStartedAt = Time.time;
        impactApplied = false;
        swingSoundPlayed = false;
        nextSwingAt = Time.time + swingDuration;
        OnSwing?.Invoke();
        return true;
    }

    private void Hit()
    {
        Ray ray = CrosshairAim.CreateRay(playerCamera);
        // Viewport rays start on the near clip plane. Melee reach starts at the player,
        // so camera clipping settings must not add free distance to the swing.
        ray.origin = playerCamera.transform.position;
        Vector3 lower = ray.origin - Vector3.up * hitHeight;
        var contacts = new List<(Collider collider, Vector3 point, float distance)>();
        foreach (RaycastHit hit in Physics.CapsuleCastAll(ray.origin, lower, hitRadius, ray.direction,
                     range, ~0, QueryTriggerInteraction.Ignore))
            if (hit.distance > 0f) contacts.Add((hit.collider, hit.point, hit.distance));
        // Starting overlaps can be omitted or have undefined cast contact points.
        // Resolve them explicitly so point-blank swings also connect reliably.
        foreach (Collider collider in Physics.OverlapCapsule(ray.origin, lower, hitRadius, ~0,
                     QueryTriggerInteraction.Ignore))
        {
            Vector3 origin = StrikeOrigin(collider.bounds.center, ray.origin, lower);
            contacts.Add((collider, collider.ClosestPoint(origin), 0f));
        }
        contacts.Sort((a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in contacts)
        {
            if (owner != null && hit.collider.transform.IsChildOf(owner)) continue;
            if (hit.distance == 0f && Vector3.Dot(hit.collider.bounds.center - ray.origin, ray.direction) < 0f)
                continue;
            // The nearest solid object blocks the swing, including walls and doors.
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDead)
            {
                Vector3 origin = StrikeOrigin(hit.point, ray.origin, lower);
                Vector3 point = hit.collider.ClosestPoint(origin);
                var targetComponent = target as Component;
                Vector3 playerPosition = owner != null ? owner.position : ray.origin;
                // The Director's root is at its feet, below the player's root. Its large
                // solid body can be in reach while those roots remain over 1.5 m apart.
                // Measure that boss at the contact surface; keep zombie attack-range parity.
                bool withinTargetRange = targetComponent is DirectorBoss ||
                    (targetComponent != null && Vector3.Distance(playerPosition, targetComponent.transform.position) <= range);
                if (withinTargetRange &&
                    Vector3.Distance(origin, point) <= range &&
                    (!Physics.Linecast(ray.origin, point, out RaycastHit obstruction, ~0, QueryTriggerInteraction.Ignore) ||
                     obstruction.collider.GetComponentInParent<IDamageable>() == target))
                    target.TakeDamage(damage, gameObject);
            }
            break;
        }
    }

    private static Vector3 StrikeOrigin(Vector3 point, Vector3 upper, Vector3 lower)
        => new Vector3(upper.x, Mathf.Clamp(point.y, lower.y, upper.y), upper.z);

    private void Update()
    {
        if (!IsAttacking) return;
        if (PlayerWeaponManager.InputBlocked)
        {
            CancelAttack();
            return;
        }
        // Combat and the HUD use the same absolute clock, even when a frame is delayed.
        if (!swingSoundPlayed && SwingElapsedTime >= swingSoundDelay)
        {
            swingSoundPlayed = true;
            if (swingSound != null)
            {
                swingAudio.clip = swingSound;
                swingAudio.volume = swingVolume;
                swingAudio.Play();
            }
        }
        if (!impactApplied && SwingElapsedTime >= Mathf.Min(impactDelay, swingDuration))
        {
            impactApplied = true;
            if (playerCamera != null && playerCamera.isActiveAndEnabled) Hit();
        }
        if (SwingElapsedTime >= swingDuration) CancelAttack();
    }

    private void OnDisable() => CancelAttack();

    public void CancelAttack()
    {
        if (swingAudio != null) swingAudio.Stop();
        if (!IsAttacking) return;
        IsAttacking = false;
        OnSwingFinished?.Invoke();
    }
}
