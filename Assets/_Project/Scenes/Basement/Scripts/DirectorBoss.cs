using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>The Director's pursuit, claw attack and dodgeable paper special.</summary>
[RequireComponent(typeof(CharacterController))]
public sealed class DirectorBoss : MonoBehaviour, IDamageable
{
    [Header("Combat")]
    [SerializeField, Min(1)] float maxHealth = 1600;
    [SerializeField, Min(0)] float moveSpeed = 3.65f;
    [SerializeField, Min(0)] float attackDamage = 30;
    [SerializeField, Min(.1f)] float attackRange = 2.7f;
    [SerializeField, Min(.1f)] float attackWindup = .7f;
    [SerializeField, Min(.1f)] float attackRecovery = 1.1f;
    [SerializeField, Min(0)] float awakeningDuration = 1.1f;
    [Header("Paper special")]
    [SerializeField] DirectorPaperProjectile paperProjectilePrefab;
    [SerializeField, Min(.1f)] float paperCooldownMin = 5f;
    [SerializeField, Min(.1f)] float paperCooldownMax = 9f;
    [SerializeField, Min(.1f)] float paperRange = 30f;
    [SerializeField, Min(.1f)] float paperWindup = .85f;
    [SerializeField, Min(.1f)] float paperRecovery = .65f;
    [Header("Presentation")]
    [SerializeField] DirectorBossAnimator presentation;
    public UnityEvent onDefeated = new UnityEvent();
    public UnityEvent onDeathAnimationFinished = new UnityEvent();

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }
    public bool DeathAnimationFinished { get; private set; }
    public bool IsAttacking { get; private set; }
    public bool IsCastingPaper { get; private set; }
    public float PaperCastProgress => !IsCastingPaper ? 0 : Time.time - paperStarted < paperWindup
        ? .5f * Mathf.Clamp01((Time.time - paperStarted) / paperWindup)
        : .5f + .5f * Mathf.Clamp01((Time.time - paperStarted - paperWindup) / paperRecovery);
    public float AttackProgress => !IsAttacking ? 0 : Time.time - attackStarted < attackWindup
        ? .5f * Mathf.Clamp01((Time.time - attackStarted) / attackWindup)
        : .5f + .5f * Mathf.Clamp01((Time.time - attackStarted - attackWindup) / attackRecovery);
    public float AwakeningProgress => Mathf.Clamp01((Time.time - bornAt) / Mathf.Max(.01f, awakeningDuration));
    public event Action<float, float> HealthChanged;
    public event Action Defeated;

    CharacterController controller;
    Transform target;
    IDamageable targetLife;
    Camera viewCamera;
    float bornAt, attackStarted, nextAttack, verticalSpeed;
    float nextPaper, paperStarted;
    bool paperReleased;
    bool strikeDelivered;
    readonly RaycastHit[] hits = new RaycastHit[24];
    static readonly float[] SteeringAngles = { 0, 35, -35, 70, -70, 110, -110 };

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (presentation == null) presentation = GetComponentInChildren<DirectorBossAnimator>();
        CurrentHealth = maxHealth;
        bornAt = Time.time;
        SchedulePaper(awakeningDuration);
    }

    public void SetTarget(Transform player)
    {
        target = player;
        targetLife = player != null ? player.GetComponentInParent<IDamageable>() : null;
    }

    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) SetTarget(player.transform);
        }
        viewCamera = Camera.main;
    }

    void Update()
    {
        if (IsDead) return;
        Vector3 movement = Vector3.zero;
        // PlayerMovement currently remains active at zero HP. Keep tracking that player;
        // health controls whether an attack can hurt them, not whether the boss can walk.
        bool hasTarget = target != null && target.gameObject.activeInHierarchy;
        bool aliveTarget = hasTarget && targetLife != null && !targetLife.IsDead;
        if (AwakeningProgress >= 1 && hasTarget)
        {
            Vector3 delta = target.position - transform.position;
            delta.y = 0;
            if (IsCastingPaper)
            {
                if (!aliveTarget)
                {
                    IsCastingPaper = false;
                    SchedulePaper();
                }
                else
                {
                    if (!paperReleased && Time.time - paperStarted >= paperWindup)
                    {
                        paperReleased = true;
                        ThrowPaper();
                    }
                    if (Time.time - paperStarted >= paperWindup + paperRecovery)
                    {
                        IsCastingPaper = false;
                        SchedulePaper();
                    }
                }
            }
            else if (IsAttacking)
            {
                if (!strikeDelivered && Time.time - attackStarted >= attackWindup)
                {
                    strikeDelivered = true;
                    if (aliveTarget && delta.magnitude <= attackRange + .35f && HasLineOfSight())
                        targetLife.TakeDamage(attackDamage, gameObject);
                }
                if (Time.time - attackStarted >= attackWindup + attackRecovery)
                    IsAttacking = false;
            }
            else if (delta.magnitude <= attackRange && HasLineOfSight())
            {
                if (aliveTarget && Time.time >= nextAttack)
                {
                    IsAttacking = true;
                    strikeDelivered = false;
                    attackStarted = Time.time;
                    nextAttack = Time.time + attackWindup + attackRecovery + .25f;
                }
            }
            else if (aliveTarget && paperProjectilePrefab != null && Time.time >= nextPaper &&
                     delta.magnitude <= paperRange && HasLineOfSight())
            {
                IsCastingPaper = true;
                paperReleased = false;
                paperStarted = Time.time;
                // Attacks share recovery, so a paper cast cannot also deliver a claw hit.
                nextAttack = Time.time + paperWindup + paperRecovery + .25f;
            }
            else movement = Steer(delta.normalized) * moveSpeed;
        }
        verticalSpeed = controller.isGrounded ? -2 : Mathf.Max(-25, verticalSpeed + Physics.gravity.y * Time.deltaTime);
        if (controller.enabled) controller.Move((movement + Vector3.up * verticalSpeed) * Time.deltaTime);
        if (presentation != null) presentation.IsWalking = movement.sqrMagnitude > .01f;
    }

    void SchedulePaper(float delay = 0)
    {
        float minimum = Mathf.Max(.1f, paperCooldownMin);
        nextPaper = Time.time + delay + UnityEngine.Random.Range(minimum, Mathf.Max(minimum, paperCooldownMax));
    }

    void ThrowPaper()
    {
        if (paperProjectilePrefab == null || target == null || targetLife == null || targetLife.IsDead ||
            UniDoom.UI.DoorReloadScreen.IsReloading) return;
        if (viewCamera == null) viewCamera = Camera.main;
        Vector3 aim = viewCamera != null && viewCamera.transform.IsChildOf(target)
            ? viewCamera.transform.position : target.position + Vector3.up;
        Vector3 origin = transform.TransformPoint(controller.center);
        Vector3 direction = (aim - origin).normalized;
        // Start inside the owner's capsule and sweep outwards: walls immediately beside
        // the boss must block the paper too. The projectile ignores its owner's colliders.
        DirectorPaperProjectile paper = Instantiate(paperProjectilePrefab, origin, Quaternion.identity);
        paper.Launch(this, target.GetComponentInParent<PlayerLife>(), direction);
    }

    Vector3 Steer(Vector3 desired)
    {
        // Capsule casts respect walls and furniture even when the room has no baked NavMesh.
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        float radius = controller.radius * scale * .85f;
        Vector3 bottom = transform.position + Vector3.up * (radius + .3f);
        Vector3 top = transform.position + Vector3.up * (controller.height * transform.lossyScale.y - radius);
        float bestScore = float.NegativeInfinity;
        Vector3 best = Vector3.zero;
        foreach (float angle in SteeringAngles)
        {
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * desired;
            float clearance = 1.4f;
            int count = Physics.CapsuleCastNonAlloc(bottom, top, radius, direction, hits, clearance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Transform hit = hits[i].transform;
                if (hit.IsChildOf(transform) || (target != null && (hit == target || hit.IsChildOf(target)))) continue;
                clearance = Mathf.Min(clearance, hits[i].distance);
            }
            float score = Vector3.Dot(desired, direction) + clearance * 2;
            if (clearance > .18f && score > bestScore) { bestScore = score; best = direction; }
        }
        return best;
    }

    bool HasLineOfSight()
    {
        if (target == null) return false;
        Vector3 origin = transform.position + Vector3.up * (controller.height * transform.lossyScale.y * .42f);
        Vector3 destination = viewCamera != null && viewCamera.transform.IsChildOf(target)
            ? viewCamera.transform.position : target.position;
        Vector3 delta = destination - origin;
        int count = Physics.RaycastNonAlloc(origin, delta.normalized, hits, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Transform hit = hits[i].transform;
            if (hit.IsChildOf(transform) || hit == target || hit.IsChildOf(target)) continue;
            return false;
        }
        return true;
    }

    public void TakeDamage(float amount, GameObject source = null)
    {
        if (IsDead || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        if (CurrentHealth > 0) { if (presentation != null) presentation.FlashHit(); return; }
        IsDead = true;
        IsAttacking = false;
        IsCastingPaper = false;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        if (presentation != null) presentation.PlayDeath();
        else FinishDeathAnimation();
        Defeated?.Invoke();
        onDefeated.Invoke();
    }

    public void FinishDeathAnimation()
    {
        if (!IsDead || DeathAnimationFinished) return;
        DeathAnimationFinished = true;
        onDeathAnimationFinished.Invoke();
    }
}
