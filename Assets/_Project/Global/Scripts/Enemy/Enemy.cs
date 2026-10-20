using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.AI;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [FormerlySerializedAs("vidaMaxima")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    [Header("Hit Effect")]
    [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.15f;

    [Header("Movement")]
    [FormerlySerializedAs("velocidade")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Vision")]
    [SerializeField, Min(.5f)] private float sightRange = 12f;
    [SerializeField, Range(1f, 360f)] private float fieldOfView = 120f;
    [SerializeField, Min(0f)] private float eyeHeight = .55f;
    [SerializeField] private LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
    [SerializeField, Min(.05f)] private float perceptionInterval = .15f;
    [SerializeField, Min(.1f)] private float searchDuration = 4f;

    [Header("Wandering")]
    [SerializeField, Min(0f)] private float patrolSpeed = 1f;
    [SerializeField, Min(.5f)] private float patrolRadius = 6f;
    [SerializeField] private Vector2 patrolWait = new Vector2(.8f, 2.5f);
    [SerializeField, Min(.1f)] private float pathUpdateInterval = .25f;
    [SerializeField, Min(1f)] private float turnSpeed = 180f;

    [Header("Attack")]
    [FormerlySerializedAs("danoAtaque")]
    [SerializeField] private float attackDamage = 10f;
    [FormerlySerializedAs("alcanceAtaque")]
    [SerializeField] private float attackRange = 1.5f;
    [FormerlySerializedAs("cooldownAtaque")]
    [SerializeField] private float attackCooldown = 1f;

    [Header("Events (optional, for other scripts to listen)")]
    [FormerlySerializedAs("OnVidaAlterada")]
    public UnityEvent<float, float> OnHealthChanged; // (current, max)
    [FormerlySerializedAs("OnMorte")]
    public UnityEvent<GameObject> OnDeath;          // killer

    private Transform target;
    private PlayerLife targetLife;
    private NavMeshAgent agent;
    private NavMeshPath patrolPath;
    private Vector3 patrolOrigin, patrolDestination, lastSeenPosition, desiredDirection;
    private float desiredSpeed, nextPerception, nextTargetSearch, lastSeenAt, nextPathUpdate, waitUntil;
    private float patrolDeadline;
    private bool hasPatrolDestination;
    private readonly RaycastHit[] sightHits = new RaycastHit[32];
    private readonly RaycastHit[] movementHits = new RaycastHit[16];
    private CapsuleCollider capsule;
    private float lastAttackTime;
    private Rigidbody body;
    private Renderer[] hitRenderers;
    private MaterialPropertyBlock[] originalPropertyBlocks;
    private MaterialPropertyBlock hitPropertyBlock;
    private float hitFlashEndsAt;
    private bool isFlashing;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public bool IsDead { get; private set; }
    public enum BehaviourState { Wandering, Chasing, Searching, Dead }
    public BehaviourState State { get; private set; }
    public Vector3 FacingDirection
    {
        get
        {
            // Rigidbody/agent rotation can lag behind a turn. A chasing sprite looks at its target.
            Vector3 direction = State == BehaviourState.Chasing && target != null
                ? target.position - transform.position : transform.forward;
            direction.y = 0f;
            return direction.sqrMagnitude > .001f ? direction.normalized : transform.forward;
        }
    }
    public event System.Action Attacked;

    private void Awake()
    {
        currentHealth = maxHealth;
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        agent = GetComponent<NavMeshAgent>();
        patrolPath = new NavMeshPath();
        patrolOrigin = transform.position;
        nextPerception = Time.time + Random.Range(0f, perceptionInterval);
        hitRenderers = GetComponentsInChildren<Renderer>();
        originalPropertyBlocks = new MaterialPropertyBlock[hitRenderers.Length];
        hitPropertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < hitRenderers.Length; i++)
            originalPropertyBlocks[i] = new MaterialPropertyBlock();
    }

    private void Start()
    {
        FindTarget();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (isFlashing && Time.time >= hitFlashEndsAt)
            RestoreHitColors();

        if (IsDead) return;
        if (target == null && Time.time >= nextTargetSearch) FindTarget();
        if (Time.time >= nextPerception)
        {
            nextPerception = Time.time + perceptionInterval;
            if (target != null && (targetLife == null || !targetLife.IsDead) && CanSeeTarget())
            {
                lastSeenPosition = target.position;
                lastSeenAt = Time.time;
                ChangeState(BehaviourState.Chasing);
            }
            else if (State == BehaviourState.Chasing) ChangeState(BehaviourState.Searching);
        }

        if (target == null || (targetLife != null && targetLife.IsDead) ||
            (State == BehaviourState.Searching && Time.time - lastSeenAt >= searchDuration))
            ChangeState(BehaviourState.Wandering);

        if (State == BehaviourState.Wandering) Wander();
        else if (State == BehaviourState.Chasing &&
                 Vector3.Distance(transform.position, target.position) <= attackRange && HasClearSight())
        {
            StopMoving();
            Face(target.position - transform.position, Time.deltaTime);
            TryAttack();
        }
        else MoveTo(lastSeenPosition, moveSpeed, .35f);
    }

    private bool UsingAgent => agent != null && agent.enabled && agent.isOnNavMesh;

    private void FindTarget()
    {
        nextTargetSearch = Time.time + 1f;
        var player = GameManager.SessionPlayer;
        var found = player != null ? player.gameObject : GameObject.FindGameObjectWithTag("Player");
        target = found != null ? found.transform : null;
        targetLife = found != null ? found.GetComponent<PlayerLife>() : null;
    }

    private bool CanSeeTarget()
    {
        Vector3 toTarget = target.position - transform.position;
        if (toTarget.sqrMagnitude > sightRange * sightRange) return false;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > .001f &&
            Vector3.Angle(transform.forward, toTarget) > fieldOfView * .5f) return false;
        return HasClearSight();
    }

    private bool HasClearSight()
    {
        if (target == null) return false;
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 direction = target.position + Vector3.up * .3f - eye;
        int count = Physics.RaycastNonAlloc(eye, direction.normalized, sightHits, direction.magnitude,
            obstacleLayers, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length) return false;
        for (int i = 0; i < count; i++)
            if (!sightHits[i].transform.IsChildOf(transform) && !sightHits[i].transform.IsChildOf(target))
                return false;
        return true;
    }

    private void ChangeState(BehaviourState state)
    {
        if (State == state) return;
        State = state;
        nextPathUpdate = 0f;
        hasPatrolDestination = false;
        waitUntil = 0f;
        StopMoving();
    }

    private void Wander()
    {
        if (hasPatrolDestination &&
            (HorizontalDistance(transform.position, patrolDestination) < .4f || Time.time >= patrolDeadline))
        {
            hasPatrolDestination = false;
            waitUntil = Time.time + Random.Range(Mathf.Max(0f, patrolWait.x), Mathf.Max(patrolWait.x, patrolWait.y));
            StopMoving();
        }
        if (!hasPatrolDestination)
        {
            if (Time.time < waitUntil) return;
            if (!ChoosePatrolDestination()) { waitUntil = Time.time + 1f; return; }
        }
        MoveTo(patrolDestination, patrolSpeed, .25f);
    }

    private bool ChoosePatrolDestination()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = patrolOrigin + new Vector3(offset.x, 0f, offset.y);
            if (UsingAgent)
            {
                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                if (!NavMesh.SamplePosition(candidate, out var hit, 1.5f, filter) ||
                    !agent.CalculatePath(hit.position, patrolPath) || patrolPath.status != NavMeshPathStatus.PathComplete)
                    continue;
                candidate = hit.position;
            }
            else if (!TryGround(candidate, out candidate)) continue;
            if (HorizontalDistance(transform.position, candidate) < 1f) continue;
            patrolDestination = candidate;
            hasPatrolDestination = true;
            patrolDeadline = Time.time + Mathf.Max(4f, patrolRadius * 3f / Mathf.Max(.1f, patrolSpeed));
            return true;
        }
        return false;
    }

    private static float HorizontalDistance(Vector3 from, Vector3 to)
    {
        from.y = to.y;
        return Vector3.Distance(from, to);
    }

    private void MoveTo(Vector3 destination, float speed, float stoppingDistance)
    {
        if (UsingAgent)
        {
            desiredDirection = Vector3.zero;
            agent.speed = speed;
            agent.stoppingDistance = stoppingDistance;
            agent.isStopped = false;
            if (Time.time >= nextPathUpdate)
            {
                nextPathUpdate = Time.time + pathUpdateInterval;
                agent.SetDestination(destination);
            }
            return;
        }
        // An unbound agent must not compete with Rigidbody movement or walk off its baked surface.
        if (agent != null && agent.enabled) { StopMoving(); return; }
        desiredDirection = destination - transform.position;
        desiredDirection.y = 0f;
        desiredDirection = desiredDirection.magnitude > stoppingDistance ? desiredDirection.normalized : Vector3.zero;
        desiredSpeed = speed;
    }

    private void StopMoving()
    {
        desiredDirection = Vector3.zero;
        desiredSpeed = 0f;
        if (UsingAgent) { agent.isStopped = true; agent.ResetPath(); }
        if (body != null && !body.isKinematic)
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
    }

    private void FixedUpdate()
    {
        if (IsDead || UsingAgent || (agent != null && agent.enabled) || Time.timeScale <= 0f) return;
        Vector3 direction = desiredDirection;
        if (direction.sqrMagnitude > .001f)
        {
            if (!CanWalk(direction))
            {
                // Try both sides of furniture instead of pushing straight through a collider.
                Vector3 left = Quaternion.Euler(0, -60f, 0) * direction;
                Vector3 right = Quaternion.Euler(0, 60f, 0) * direction;
                direction = CanWalk(left) ? left : CanWalk(right) ? right : Vector3.zero;
                if (direction == Vector3.zero && State == BehaviourState.Wandering) patrolDeadline = Time.time;
            }
            Face(direction, Time.fixedDeltaTime);
        }
        if (body != null && !body.isKinematic)
            body.linearVelocity = direction * desiredSpeed + Vector3.up * body.linearVelocity.y;
        else if (direction.sqrMagnitude > .001f)
            transform.position += direction * desiredSpeed * Time.fixedDeltaTime;
    }

    private bool CanWalk(Vector3 direction)
    {
        float radius = capsule != null ? Mathf.Max(.05f, capsule.bounds.extents.x - .03f) : .4f;
        float halfHeight = capsule != null ? Mathf.Max(radius, capsule.bounds.extents.y - .03f) : .9f;
        Vector3 center = capsule != null ? capsule.bounds.center : transform.position;
        float distance = Mathf.Max(.3f, desiredSpeed * Time.fixedDeltaTime + .1f);
        Vector3 axis = Vector3.up * (halfHeight - radius);
        int count = Physics.CapsuleCastNonAlloc(center + axis, center - axis, radius, direction,
            movementHits, distance, obstacleLayers, QueryTriggerInteraction.Ignore);
        if (count == movementHits.Length) return false;
        for (int i = 0; i < count; i++)
            if (!movementHits[i].transform.IsChildOf(transform) && movementHits[i].normal.y < .65f) return false;
        return TryGround(transform.position + direction * (distance + radius), out _);
    }

    private bool TryGround(Vector3 position, out Vector3 grounded)
    {
        float height = capsule != null ? capsule.bounds.extents.y : 1f;
        if (Physics.Raycast(position + Vector3.up * .4f, Vector3.down, out var hit, height + 1f,
            obstacleLayers, QueryTriggerInteraction.Ignore) && hit.normal.y >= .65f)
        {
            grounded = hit.point + Vector3.up * height;
            return true;
        }
        grounded = position;
        return false;
    }

    private void Face(Vector3 direction, float deltaTime)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < .001f) return;
        Quaternion rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * deltaTime);
        if (body != null && !body.isKinematic) body.MoveRotation(rotation);
        else transform.rotation = rotation;
    }

    private void TryAttack()
    {
        if (IsDead || target == null || (targetLife != null && targetLife.IsDead) ||
            Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;
        Attacked?.Invoke();

        if (target.TryGetComponent<IDamageable>(out var damageableTarget))
        {
            damageableTarget.TakeDamage(attackDamage, gameObject);
        }
    }

    // ---- IDamageable implementation ----

    public void TakeDamage(float amount, GameObject source = null)
    {
        if (IsDead || amount <= 0f) return;

        FlashHit();
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die(source);
        }
    }

    private void FlashHit()
    {
        for (int i = 0; i < hitRenderers.Length; i++)
        {
            Renderer hitRenderer = hitRenderers[i];
            if (hitRenderer == null || hitRenderer.sharedMaterial == null) continue;

            // Save the original overrides only once, even with repeated pellet hits.
            if (!isFlashing)
                hitRenderer.GetPropertyBlock(originalPropertyBlocks[i]);

            hitRenderer.GetPropertyBlock(hitPropertyBlock);
            Material material = hitRenderer.sharedMaterial;
            int colorId = material.HasProperty(BaseColorId) ? BaseColorId : ColorId;
            if (!material.HasProperty(colorId)) continue;

            Color originalColor = originalPropertyBlocks[i].HasColor(colorId)
                ? originalPropertyBlocks[i].GetColor(colorId)
                : material.GetColor(colorId);
            hitPropertyBlock.SetColor(colorId, new Color(1f, 0f, 0f, originalColor.a));
            hitRenderer.SetPropertyBlock(hitPropertyBlock);
        }

        isFlashing = true;
        hitFlashEndsAt = Time.time + hitFlashDuration;
    }

    private void RestoreHitColors()
    {
        if (!isFlashing) return;

        for (int i = 0; i < hitRenderers.Length; i++)
        {
            if (hitRenderers[i] != null)
                hitRenderers[i].SetPropertyBlock(originalPropertyBlocks[i]);
        }

        isFlashing = false;
    }

    private void OnDisable()
    {
        StopMoving();
        RestoreHitColors();
    }

    private void Die(GameObject killer)
    {
        if (IsDead) return;

        IsDead = true;
        ChangeState(BehaviourState.Dead);
        if (agent != null) agent.enabled = false;
        target = null;

        // Keep the dead enemy in place, including against gravity and collisions.
        if (body != null)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
            body.isKinematic = true;
        }

        OnDeath?.Invoke(killer);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Gizmos.color = new Color(1f, .65f, .15f, .6f);
        Gizmos.DrawWireSphere(eye, sightRange);
        Gizmos.DrawRay(eye, Quaternion.Euler(0, -fieldOfView * .5f, 0) * transform.forward * sightRange);
        Gizmos.DrawRay(eye, Quaternion.Euler(0, fieldOfView * .5f, 0) * transform.forward * sightRange);
        Gizmos.color = new Color(.25f, .7f, .35f, .5f);
        Gizmos.DrawWireSphere(Application.isPlaying ? patrolOrigin : transform.position, patrolRadius);
    }
}
