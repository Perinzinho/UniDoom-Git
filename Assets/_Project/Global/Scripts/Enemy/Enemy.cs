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
    [SerializeField, Min(.5f)] private float loseSightRange = 16f;
    [SerializeField, Range(1f, 360f)] private float fieldOfView = 120f;
    [SerializeField, Min(0f)] private float eyeHeight = .55f;
    [SerializeField] private LayerMask obstacleLayers = Physics.DefaultRaycastLayers;
    [SerializeField, Min(.05f)] private float perceptionInterval = .15f;
    [SerializeField, Min(.1f)] private float searchDuration = 4f;
    [SerializeField, Min(.5f)] private float searchRadius = 3f;
    [SerializeField, Min(.1f)] private float searchPause = .6f;
    [SerializeField, Min(0f)] private float closeAwarenessRange = 3.5f;

    [Header("Group awareness")]
    [SerializeField, Min(0f)] private float alertRadius = 7f;
    [SerializeField, Min(.1f)] private float alertCooldown = 2f;
    [SerializeField, Min(0f)] private float hearingRange = 20f;
    [SerializeField, Min(.1f)] private float separationRadius = 1.2f;
    [SerializeField, Range(0f, 1f)] private float separationStrength = .65f;
    [Tooltip("How long an alerted group actively pursues the player's live position after a shot or a hit.")]
    [SerializeField, Min(.1f)] private float huntDuration = 8f;
    [Tooltip("Radius in which a hit or a visual alert turns nearby zombies into active pursuers.")]
    [SerializeField, Min(0f)] private float alertOnHitRadius = 16f;

    [Header("Wandering")]
    [SerializeField, Min(0f)] private float patrolSpeed = 1f;
    [SerializeField, Min(.5f)] private float patrolRadius = 6f;
    [SerializeField] private Vector2 patrolWait = new Vector2(.8f, 2.5f);
    [SerializeField, Min(.1f)] private float pathUpdateInterval = .25f;
    [SerializeField, Min(1f)] private float turnSpeed = 180f;

    [Header("Obstacle avoidance")]
    [SerializeField, Min(.2f)] private float obstacleProbeDistance = 1.4f;
    [SerializeField, Min(.05f)] private float avoidHoldTime = .5f;
    [Tooltip("How long the enemy commits to sliding along an obstacle face before re-evaluating. Large obstacles need longer commitments.")]
    [SerializeField, Min(.3f)] private float wallFollowTime = 1.6f;

    [Header("Attack")]
    [FormerlySerializedAs("danoAtaque")]
    [SerializeField] private float attackDamage = 10f;
    [FormerlySerializedAs("alcanceAtaque")]
    [SerializeField] private float attackRange = 1.5f;
    [FormerlySerializedAs("cooldownAtaque")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField, Min(.05f)] private float meleeWindup = .3f;
    [SerializeField, Min(0f)] private float meleeRecovery = .35f;

    [Header("Events (optional, for other scripts to listen)")]
    [FormerlySerializedAs("OnVidaAlterada")]
    public UnityEvent<float, float> OnHealthChanged = new UnityEvent<float, float>(); // (current, max)
    [FormerlySerializedAs("OnMorte")]
    public UnityEvent<GameObject> OnDeath = new UnityEvent<GameObject>(); // killer

    private Transform target;
    private PlayerLife targetLife;
    private CharacterController targetController;
    private NavMeshAgent agent;
    private UniDoom.CampusLibrary.LibraryEnemyRestriction libraryRestriction;
    private NavMeshPath patrolPath;
    private Vector3 patrolOrigin, patrolDestination, lastSeenPosition, desiredDirection;
    private float desiredSpeed, nextPerception, nextTargetSearch, searchEndsAt, nextPathUpdate, waitUntil;
    private float patrolDeadline;
    private Vector3 progressPosition;
    private float nextProgressCheck, steeringSide;
    private bool hasPatrolDestination;
    private Vector3 searchDestination;
    private bool reachedLastSeenPosition;
    private float searchWaitUntil, nextAlert;
    private float huntUntil;
    private readonly Collider[] alertHits = new Collider[48];
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
    private EnemySpriteAnimator spriteAnimator;
    private float projectileReleaseAt = -1f, attackEndsAt;
    private EnemySpriteAppearance pendingProjectile;
    private bool nearbyCuePlayed;
    private float meleeHitAt = -1f, meleeEndsAt, nextSeparation, recoveryUntil;
    private Vector3 separation, previousSteering, avoidanceDirection;
    private float avoidanceUntil;
    // Committed slide along a long obstacle face: reactive scoring anchored to the
    // player direction paces left-right along big obstacles, so once a strongly
    // sideways heading is chosen the enemy follows the surface itself instead.
    private Vector3 wallSlideDirection;
    private float wallFollowUntil;
    private Vector3 agentLastPosition;
    private float agentStuckFor, nextAgentStuckCheck, agentRecoveryUntil, nextAgentSnapAttempt, nextAgentDetour;
    private int recoveryStreak;
    // Recently failed headings stop the scoring from re-selecting directions that just
    // slammed into the same face of an obstacle, which is what makes enemies look stuck.
    private readonly Vector3[] blockedDirections = new Vector3[8];
    private readonly float[] blockedUntil = new float[8];
    private int blockedCursor;
    private const float BlockedMemorySeconds = 1.2f;
    private readonly Collider[] detourHits = new Collider[16];
    private readonly Collider[] separationHits = new Collider[24];
    private readonly System.Collections.Generic.List<Enemy> nearbyAllies = new();

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public bool IsDead { get; private set; }
    public enum BehaviourState { Wandering, Chasing, Searching, Dead }
    public BehaviourState State { get; private set; }
    public Vector3 FacingDirection
    {
        get
        {
            // Presentation and perception must agree, including during turns and searches.
            Vector3 direction = transform.forward;
            direction.y = 0f;
            return direction.sqrMagnitude > .001f ? direction.normalized : transform.forward;
        }
    }
    public event System.Action Attacked;

    private void Awake()
    {
        currentHealth = maxHealth;
        // Shared shortest-route field for steering scenes; harmless in NavMesh scenes.
        EnemyFlowField.Ensure();
        body = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        agent = GetComponent<NavMeshAgent>();
        libraryRestriction = GetComponent<UniDoom.CampusLibrary.LibraryEnemyRestriction>();
        spriteAnimator = GetComponentInChildren<EnemySpriteAnimator>();
        patrolPath = new NavMeshPath();
        patrolOrigin = transform.position;
        progressPosition = transform.position;
        agentLastPosition = transform.position;
        steeringSide = Random.value < .5f ? -1f : 1f;
        nextPerception = Time.time + Random.Range(0f, perceptionInterval);
        hitRenderers = GetComponentsInChildren<Renderer>();
        originalPropertyBlocks = new MaterialPropertyBlock[hitRenderers.Length];
        hitPropertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < hitRenderers.Length; i++)
            originalPropertyBlocks[i] = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (gameObject.scene.name == "CampusExterior")
        {
            // Outdoor steering owns the capsule. Kinematic movement avoids launch
            // impulses when the persistent player and actors overlap during scene load.
            if (agent) agent.enabled = false;
            if (body)
            {
                body.isKinematic = true;
                body.useGravity = false;
                body.constraints &= ~RigidbodyConstraints.FreezePosition;
                body.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
            // Probe from the root: an authored spawn may already have its feet below
            // the road. Starting a ray below that surface cannot recover the actor.
            if (capsule && UniDoom.Utils.CampusGrounding.TryFloor(transform.position, out var floor))
            {
                float offset = CampusFootOffset();
                var grounded = new Vector3(transform.position.x, floor.point.y + offset + .02f, transform.position.z);
                if (body) body.position = grounded;
                else transform.position = grounded;
                patrolOrigin = grounded;
                progressPosition = grounded;
            }
        }
        if (agent != null && agent.enabled)
        {
            // A spawn placed slightly off the baked surface would otherwise freeze the
            // agent: snap it back, or fall back to local steering when that is impossible.
            EnsureAgentOnNavMesh();
            // Configure the agent even while it is waiting for the navmesh, so a late
            // snap does not start fighting the visual rotation.
            agent.updateRotation = false;
            agent.angularSpeed = turnSpeed;
            agent.autoRepath = true;
            agent.avoidancePriority = Random.Range(25, 75);
            if (UsingAgent && body != null)
            {
                // Navigation is the sole owner of the transform on baked surfaces.
                body.isKinematic = true;
                body.useGravity = false;
            }
        }
        agentLastPosition = transform.position;
        FindTarget();
    }

    private void OnEnable() => Gun.Gunshot += HearGunshot;

    private void HearGunshot(Vector3 position, float loudness)
    {
        if (IsDead || Time.timeScale <= 0f) return;
        float range = Mathf.Min(hearingRange, loudness);
        if (Vector3.Distance(transform.position, position) > range) return;
        // Walls muffle distant shots; a nearby shot still sends a clear alarm.
        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 sound = position + Vector3.up * eyeHeight;
        int count = Physics.RaycastNonAlloc(origin, (sound - origin).normalized, sightHits,
            Vector3.Distance(origin, sound), obstacleLayers, QueryTriggerInteraction.Ignore);
        bool muffled = count == sightHits.Length;
        for (int i = 0; i < count; i++)
            if (!sightHits[i].transform.IsChildOf(transform) &&
                sightHits[i].collider.GetComponentInParent<IDamageable>() == null)
                muffled = true;
        if (muffled && Vector3.Distance(transform.position, position) > range * .4f) return;
        // A gunshot is an alarm, not just a clue: the shooter is hunted live by the
        // whole group, with the shared shortest-route field guiding the pursuit.
        ReceiveHunt(position, huntDuration);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (isFlashing && Time.time >= hitFlashEndsAt)
            RestoreHitColors();

        if (IsDead) return;
        UpdateNearbyAudio();
        if (meleeHitAt >= 0f || Time.time < meleeEndsAt)
        {
            StopMoving();
            // Follow the approach during windup; commit the swing once it lands.
            if (meleeHitAt >= 0f && target != null)
                Face(target.position - transform.position, Time.deltaTime);
            if (meleeHitAt >= 0f && Time.time >= meleeHitAt)
            {
                meleeHitAt = -1f;
                // Recheck at impact: backing away, taking cover or circling can avoid a hit.
                if (target != null && (targetLife == null || !targetLife.IsDead) &&
                    Vector3.Distance(transform.position, target.position) <= attackRange &&
                    Vector3.Angle(FacingDirection, target.position - transform.position) <= 65f &&
                    HasClearSight() && target.TryGetComponent<IDamageable>(out var victim))
                    victim.TakeDamage(attackDamage, gameObject);
            }
            return;
        }
        if (pendingProjectile != null)
        {
            StopMoving();
            if (target != null) Face(target.position - transform.position, Time.deltaTime);
            if (projectileReleaseAt >= 0f && Time.time >= projectileReleaseAt)
            {
                projectileReleaseAt = -1f;
                if (target != null && (targetLife == null || !targetLife.IsDead) && HasClearSight())
                {
                    Vector3 origin = transform.position + Vector3.up * eyeHeight;
                    Vector3 aim = targetController != null ? targetController.bounds.center : target.position;
                    EnemyFanProjectile.Launch(this, pendingProjectile, origin, aim - origin, attackDamage);
                }
            }
            if (Time.time < attackEndsAt) return;
            pendingProjectile = null;
        }
        if (target == null && Time.time >= nextTargetSearch) FindTarget();
        // An alerted zombie keeps hunting the player's live position after sight is
        // lost; the shared field routes it around geometry to get there.
        bool hunting = Time.time < huntUntil && target != null && (targetLife == null || !targetLife.IsDead);
        if (Time.time >= nextPerception)
        {
            nextPerception = Time.time + perceptionInterval;
            if (target != null && (targetLife == null || !targetLife.IsDead) && (hunting || CanSeeTarget()))
            {
                lastSeenPosition = target.position;
                ChangeState(BehaviourState.Chasing);
                AlertNearby();
            }
            else if (State == BehaviourState.Chasing) ChangeState(BehaviourState.Searching);
        }

        if (target == null || (targetLife != null && targetLife.IsDead) ||
            (State == BehaviourState.Searching && Time.time >= searchEndsAt))
            ChangeState(BehaviourState.Wandering);

        if (State == BehaviourState.Wandering) Wander();
        else if (State == BehaviourState.Chasing &&
                 Vector3.Distance(transform.position, target.position) <= EffectiveAttackRange && HasClearSight())
        {
            StopMoving();
            Face(target.position - transform.position, Time.deltaTime);
            TryAttack();
        }
        else if (State == BehaviourState.Searching) Search();
        else
        {
            RecoverPursuit();
            MoveTo(hunting ? target.position : lastSeenPosition, moveSpeed, .35f);
        }
        if (UsingAgent) MonitorAgentProgress();
    }

    private bool UsingAgent => agent != null && agent.enabled && agent.isOnNavMesh;

    private void UpdateNearbyAudio()
    {
        var audio = GameplayAudio.Instance;
        if (audio == null || audio.Settings == null || target == null ||
            (targetLife != null && targetLife.IsDead)) return;
        float distance = Vector3.Distance(transform.position, target.position);
        var settings = audio.Settings;
        if (distance >= Mathf.Max(settings.nearbyDistance + .1f, settings.nearbyResetDistance))
            nearbyCuePlayed = false;
        // Require an unobstructed approach, including from behind. A wall must not trigger the cue.
        if (!nearbyCuePlayed && distance <= settings.nearbyDistance && HasClearSight() &&
            audio.TryPlayNearbyZombie()) nearbyCuePlayed = true;
    }
    private EnemySpriteAppearance RangedAppearance => spriteAnimator != null &&
        spriteAnimator.Appearance != null && spriteAnimator.Appearance.projectileSprite != null
        ? spriteAnimator.Appearance : null;
    private float EffectiveAttackRange => RangedAppearance != null ? RangedAppearance.projectileAttackRange : attackRange;

    private void FindTarget()
    {
        nextTargetSearch = Time.time + 1f;
        var player = GameManager.SessionPlayer;
        var found = player != null ? player.gameObject : GameObject.FindGameObjectWithTag("Player");
        target = found != null ? found.transform : null;
        targetLife = found != null ? found.GetComponent<PlayerLife>() : null;
        targetController = found != null ? found.GetComponent<CharacterController>() : null;
    }

    private bool CanSeeTarget()
    {
        Vector3 toTarget = target.position - transform.position;
        bool chasing = State == BehaviourState.Chasing;
        float range = chasing ? Mathf.Max(sightRange, loseSightRange) : sightRange;
        if (toTarget.sqrMagnitude > range * range) return false;
        toTarget.y = 0f;
        // Once acquired, a visible target can run around the enemy without cancelling pursuit.
        if (!chasing && toTarget.sqrMagnitude > closeAwarenessRange * closeAwarenessRange &&
            Vector3.Angle(transform.forward, toTarget) > fieldOfView * .5f) return false;
        return HasClearSight();
    }

    private bool HasClearSight()
    {
        if (target == null) return false;
        if (libraryRestriction != null && !libraryRestriction.CanTarget(target.position)) return false;
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 aim = targetController != null ? targetController.bounds.center : target.position;
        Vector3 direction = aim - eye;
        if (direction.sqrMagnitude < .0001f) return true;
        int count = Physics.RaycastNonAlloc(eye, direction.normalized, sightHits, direction.magnitude,
            obstacleLayers, QueryTriggerInteraction.Ignore);
        if (count == sightHits.Length) return false;
        for (int i = 0; i < count; i++)
            if (!sightHits[i].transform.IsChildOf(transform) && !sightHits[i].transform.IsChildOf(target) &&
                sightHits[i].collider.GetComponentInParent<Enemy>() == null)
                return false;
        return true;
    }

    private void ChangeState(BehaviourState state)
    {
        if (State == state) return;
        State = state;
        if (state == BehaviourState.Searching)
        {
            searchEndsAt = Time.time + searchDuration + Mathf.Min(6f,
                HorizontalDistance(transform.position, lastSeenPosition) / Mathf.Max(.1f, moveSpeed));
            searchDestination = lastSeenPosition;
            reachedLastSeenPosition = false;
            searchWaitUntil = 0f;
        }
        nextPathUpdate = 0f;
        hasPatrolDestination = false;
        waitUntil = 0f;
        wallFollowUntil = 0f;
        wallSlideDirection = Vector3.zero;
        progressPosition = transform.position;
        nextProgressCheck = Time.time + 1.5f;
        StopMoving();
    }

    private void Search()
    {
        bool arrived = HorizontalDistance(transform.position, searchDestination) < .5f;
        bool unreachable = UsingAgent && !agent.pathPending && agent.hasPath &&
            agent.pathStatus != NavMeshPathStatus.PathComplete && agent.remainingDistance < .5f;
        if (!arrived && !unreachable && Time.time >= nextProgressCheck)
        {
            unreachable = HorizontalDistance(transform.position, progressPosition) < .15f;
            progressPosition = transform.position;
            nextProgressCheck = Time.time + 1.5f;
        }
        if (!arrived && !unreachable && searchWaitUntil <= 0f)
        {
            MoveTo(searchDestination, moveSpeed, .35f);
            return;
        }
        StopMoving();
        if (!reachedLastSeenPosition)
        {
            reachedLastSeenPosition = true;
            // Allow time to investigate after reaching the remembered location.
            searchEndsAt = Time.time + searchDuration;
        }
        if (searchWaitUntil <= 0f) searchWaitUntil = Time.time + searchPause;
        Face(Quaternion.Euler(0f, steeringSide * 90f * Time.deltaTime, 0f) * transform.forward, Time.deltaTime);
        if (Time.time < searchWaitUntil) return;
        searchWaitUntil = 0f;
        for (int i = 0; i < 6; i++)
        {
            Vector2 offset = Random.insideUnitCircle * searchRadius;
            Vector3 candidate = lastSeenPosition + new Vector3(offset.x, 0f, offset.y);
            if (UsingAgent)
            {
                var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                if (!NavMesh.SamplePosition(candidate, out var hit, 1f, filter) ||
                    !agent.CalculatePath(hit.position, patrolPath) || patrolPath.status != NavMeshPathStatus.PathComplete)
                    continue;
                candidate = hit.position;
                if (!PointClear(candidate)) continue;
            }
            else if (!TryGround(candidate, out candidate) || !IsStraightWalkable(candidate)) continue;
            if (HorizontalDistance(transform.position, candidate) < .75f) continue;
            searchDestination = candidate;
            nextPathUpdate = 0f;
            progressPosition = transform.position;
            nextProgressCheck = Time.time + 1.5f;
            return;
        }
        searchWaitUntil = Time.time + searchPause;
    }

    private void AlertNearby()
    {
        if (alertRadius <= 0f || Time.time < nextAlert) return;
        nextAlert = Time.time + alertCooldown;
        int count = Physics.OverlapSphereNonAlloc(transform.position, alertRadius, alertHits,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Enemy ally = alertHits[i].GetComponentInParent<Enemy>();
            if (ally == null || ally == this || !ally.isActiveAndEnabled || ally.IsDead) continue;
            // Seeing the player (or being hit) turns the nearby group into active
            // pursuers, not just investigators of a stale position. Each ally has
            // its own cooldown, so the alarm spreads through the horde safely.
            ally.ReceiveHunt(lastSeenPosition, huntDuration * .6f);
        }
    }

    private void AlertAlliesToHunt(Vector3 position)
    {
        if (alertOnHitRadius <= 0f) return;
        int count = Physics.OverlapSphereNonAlloc(transform.position, alertOnHitRadius, alertHits,
            Physics.AllLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Enemy ally = alertHits[i].GetComponentInParent<Enemy>();
            if (ally == null || ally == this || !ally.isActiveAndEnabled || ally.IsDead) continue;
            ally.ReceiveHunt(position, huntDuration);
        }
    }

    private void ReceiveHunt(Vector3 position, float duration)
    {
        if (IsDead) return;
        if (target == null) FindTarget();
        huntUntil = Mathf.Max(huntUntil, Time.time + duration);
        lastSeenPosition = position;
        if (target != null && (targetLife == null || !targetLife.IsDead))
            ChangeState(BehaviourState.Chasing);
        else
        {
            ChangeState(BehaviourState.Searching);
            searchDestination = position;
            reachedLastSeenPosition = false;
            searchWaitUntil = 0f;
            searchEndsAt = Time.time + searchDuration;
        }
        nextPathUpdate = 0f;
        wallFollowUntil = 0f;
        wallSlideDirection = Vector3.zero;
        progressPosition = transform.position;
        nextProgressCheck = Time.time + 1.5f;
    }

    private void Wander()
    {
        if (hasPatrolDestination && Time.time >= nextProgressCheck)
        {
            // Abandon unreachable furniture corners rather than walking in place indefinitely.
            if (HorizontalDistance(transform.position, progressPosition) < .15f) patrolDeadline = Time.time;
            progressPosition = transform.position;
            nextProgressCheck = Time.time + 1.5f;
        }
        if (hasPatrolDestination &&
            (HorizontalDistance(transform.position, patrolDestination) < .4f || Time.time >= patrolDeadline))
        {
            hasPatrolDestination = false;
            waitUntil = Time.time + Random.Range(Mathf.Max(0f, patrolWait.x), Mathf.Max(patrolWait.x, patrolWait.y));
            StopMoving();
        }
        if (!hasPatrolDestination)
        {
            if (Time.time < waitUntil)
            {
                Face(Quaternion.Euler(0f, steeringSide * 60f * Time.deltaTime, 0f) * transform.forward, Time.deltaTime);
                return;
            }
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
                if (!PointClear(candidate)) continue;
            }
            else if (!TryGround(candidate, out candidate) || !IsStraightWalkable(candidate)) continue;
            if (HorizontalDistance(transform.position, candidate) < 1f) continue;
            patrolDestination = candidate;
            hasPatrolDestination = true;
            patrolDeadline = Time.time + Mathf.Max(4f, patrolRadius * 3f / Mathf.Max(.1f, patrolSpeed));
            progressPosition = transform.position;
            nextProgressCheck = Time.time + 1.5f;
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
            agent.stoppingDistance = stoppingDistance;
            agent.isStopped = false;
            Vector3 heading = agent.desiredVelocity;
            if (heading.sqrMagnitude < .001f)
                heading = (agent.hasPath ? agent.steeringTarget : destination) - transform.position;
            // Keep the player in view when avoidance or a detour points the path away.
            Face(ChaseFacing(heading), Time.deltaTime);
            // The agent already eases into turns; scaling by facing made chases crawl.
            agent.speed = speed;
            if (Time.time >= agentRecoveryUntil && Time.time >= nextAgentDetour)
                ProbeAgentDetour(heading);
            if (Time.time >= agentRecoveryUntil && Time.time >= nextPathUpdate)
            {
                nextPathUpdate = Time.time + pathUpdateInterval;
                if (!agent.SetDestination(destination) && State == BehaviourState.Wandering)
                    patrolDeadline = Time.time;
                else if (State == BehaviourState.Wandering && !agent.pathPending &&
                         agent.pathStatus == NavMeshPathStatus.PathPartial)
                    patrolDeadline = Time.time;
            }
            return;
        }
        // A disabled/off-mesh agent must not freeze the enemy. Retry the snap first so a
        // partially loaded navmesh can recover, then fall back to local steering.
        if (agent != null && agent.enabled && EnsureAgentOnNavMesh())
        {
            StopMoving();
            return;
        }
        desiredDirection = destination - transform.position;
        desiredDirection.y = 0f;
        desiredDirection = desiredDirection.magnitude > stoppingDistance ? desiredDirection.normalized : Vector3.zero;
        // Global route: when pursuing the live player without a clear straight line,
        // follow the shared field instead of trusting 1.4 m local probes. That field
        // routes around whole buildings; local steering still rejects real geometry.
        if (desiredDirection.sqrMagnitude > .001f && State == BehaviourState.Chasing &&
            target != null && (targetLife == null || !targetLife.IsDead) &&
            HorizontalDistance(destination, target.position) < .5f &&
            !HasClearSight() && EnemyFlowField.TryGetDirection(transform.position, out Vector3 flow))
            desiredDirection = flow;
        desiredSpeed = speed;
    }

    private void StopMoving()
    {
        desiredDirection = Vector3.zero;
        desiredSpeed = 0f;
        if (UsingAgent)
        {
            agent.isStopped = true;
            if (agent.hasPath || agent.pathPending) agent.ResetPath();
        }
        if (body != null && !body.isKinematic)
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
    }

    private void FixedUpdate()
    {
        if (IsDead || UsingAgent || Time.timeScale <= 0f) return;
        Vector3 direction = desiredDirection;
        if (direction.sqrMagnitude > .001f)
        {
            UpdateSeparation();
            Vector3 desired = direction;
            if (separation.sqrMagnitude > .001f)
                desired = (desired + separation * separationStrength).normalized;
            if (Time.time < recoveryUntil)
                desired = Quaternion.Euler(0f, steeringSide * 90f, 0f) * desired;
            direction = ChooseWalkDirection(desired);
            if (direction.sqrMagnitude < .001f)
            {
                if (State == BehaviourState.Wandering) patrolDeadline = Time.time;
                StopMoving();
                return;
            }
            previousSteering = direction;
            Face(ChaseFacing(direction), Time.fixedDeltaTime);
        }
        // Pursuit keeps full speed while the body turns; patrol still eases into turns.
        float alignedSpeed = desiredSpeed * (State == BehaviourState.Wandering
            ? MovementAlignment(direction) : 1f);
        if (gameObject.scene.name == "CampusExterior" && capsule)
        {
            // Ground the capsule before physics, and step onto curbs without impulses.
            // Sweeps still reject walls and other actors; only the lower 35 cm is stepped over.
            Vector3 position = transform.position + direction * alignedSpeed * Time.fixedDeltaTime;
            if (State == BehaviourState.Wandering && HorizontalDistance(position, patrolOrigin) > patrolRadius)
            {
                hasPatrolDestination = false;
                position = transform.position;
            }
            float offset = CampusFootOffset();
            if (UniDoom.Utils.CampusGrounding.TryFloor(position - Vector3.up * offset, out var floor))
            {
                position.y = floor.point.y + offset + .015f;
                if (body) body.MovePosition(position); else transform.position = position;
            }
            return;
        }
        if (body != null && !body.isKinematic)
            body.linearVelocity = direction * alignedSpeed + Vector3.up * body.linearVelocity.y;
        else if (direction.sqrMagnitude > .001f)
        {
            Vector3 position = transform.position + direction * alignedSpeed * Time.fixedDeltaTime;
            if (body != null) body.MovePosition(position);
            else transform.position = position;
        }
    }

    private void RecoverPursuit()
    {
        if (Time.time < nextProgressCheck) return;
        if (HorizontalDistance(transform.position, progressPosition) < .15f)
        {
            recoveryStreak++;
            steeringSide = -steeringSide;
            // Repeated failures mean the plain flip is not enough: commit to the turn
            // longer so the enemy walks itself out of the pocket instead of re-reading
            // the same spot every 1.5 s.
            recoveryUntil = Time.time + .7f + .4f * Mathf.Min(recoveryStreak - 1, 3);
            nextPathUpdate = 0f;
            if (UsingAgent && !agent.pathPending) agent.ResetPath();
        }
        else recoveryStreak = 0;
        progressPosition = transform.position;
        nextProgressCheck = Time.time + 1.5f;
    }

    private Vector3 ChooseWalkDirection(Vector3 desired)
    {
        if (desired.sqrMagnitude < .001f) return Vector3.zero;
        desired.Normalize();
        float probe = Mathf.Max(obstacleProbeDistance, desiredSpeed * Time.fixedDeltaTime + CapsuleProbeRadius);

        // Large obstacles need commitment. Re-scoring every hold is anchored to the
        // player direction, so along a long face the sideways candidate keeps rotating
        // into the wall, failing CanWalk and flipping sides: the enemy paces left and
        // right instead of travelling around the block. While committed, slide parallel
        // to the blocking surface until the direct route opens again.
        if (Time.time < wallFollowUntil && wallSlideDirection.sqrMagnitude > .001f)
        {
            if (TrySlideAlongSurface(desired, wallSlideDirection, out Vector3 slide) && CanWalk(slide))
            {
                if (!ObstacleAhead(desired, probe * 2f))
                {
                    // Rounded the end of the obstacle: hand control back to the chase.
                    wallFollowUntil = 0f;
                    wallSlideDirection = Vector3.zero;
                }
                else
                {
                    // Follow the (possibly curved) face and keep the commitment alive.
                    wallSlideDirection = slide;
                    wallFollowUntil = Time.time + wallFollowTime;
                    avoidanceDirection = slide;
                    avoidanceUntil = Time.time + avoidHoldTime;
                    return slide;
                }
            }
            else
            {
                // The slide fouled a corner or nothing is ahead to follow: fall through
                // to scoring, whose momentum keeps the same side around the corner.
                wallFollowUntil = 0f;
                wallSlideDirection = Vector3.zero;
            }
        }

        // Finish a detour before snapping back to the direct line. Otherwise a concave
        // corner makes the enemy alternate sides and visibly vibrate in place.
        if (Time.time < avoidanceUntil && avoidanceDirection.sqrMagnitude > .001f)
        {
            if (CanWalk(avoidanceDirection) && ObstacleAhead(desired, probe) &&
                !ObstacleAhead(avoidanceDirection, probe * .75f))
                return avoidanceDirection;
            // The held detour no longer works: retire it so scoring picks a fresh side.
            RememberBlockedDirection(avoidanceDirection);
        }
        avoidanceUntil = 0f;

        if (CanWalk(desired) && !ObstacleAhead(desired, probe))
        {
            avoidanceDirection = Vector3.zero;
            return desired;
        }
        RememberBlockedDirection(desired);

        // Probe ahead of the capsule instead of only reacting after contact. Score both
        // sides by how far they can actually travel (not a binary clear/blocked flag), so
        // a near heading that dies against the obstacle loses to a wider heading with
        // room to move. That is what makes the enemy flow around furniture instead of
        // pushing straight into it.
        Vector3 bestDirection = Vector3.zero;
        float bestScore = float.NegativeInfinity;
        float reach = probe * 1.5f;
        for (int step = 1; step <= 10; step++)
        {
            float angle = step * 15f;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 candidate = Quaternion.Euler(0f, side * angle, 0f) * desired;
                if (!CanWalk(candidate)) continue;
                float room = Mathf.Clamp01(ObstacleDistance(candidate, reach) / reach);
                float score = Vector3.Dot(candidate, desired) +
                    .45f * Vector3.Dot(candidate, previousSteering) +
                    (side == steeringSide ? .2f : 0f) +
                    room * .9f - angle / 180f * .3f - BlockedPenalty(candidate);
                if (score <= bestScore) continue;
                bestScore = score;
                bestDirection = candidate;
            }
        }
        if (bestDirection.sqrMagnitude > .001f)
        {
            avoidanceDirection = bestDirection;
            avoidanceUntil = Time.time + avoidHoldTime;
            float turn = Vector3.SignedAngle(desired, bestDirection, Vector3.up);
            if (Mathf.Abs(turn) > 5f) steeringSide = Mathf.Sign(turn);
            // A strongly sideways choice marks a long obstacle face: commit to sliding
            // along the surface so the enemy rounds the block on one side.
            if (Mathf.Abs(turn) >= 45f)
                TryArmWallFollow(desired, bestDirection);
            return bestDirection;
        }

        // Every heading is fouled within one step. Geometry can end up sharing the
        // enemy's space (a door closes onto it, a prop appears in front of it), so push
        // out of the overlap before considering a straight retreat.
        if (TryEscapeContact(desired, out Vector3 escape))
        {
            avoidanceDirection = escape;
            avoidanceUntil = Time.time + avoidHoldTime;
            return escape;
        }

        // Fully boxed in: retreat from the obstacle and re-evaluate next frame.
        Vector3 retreat = -desired;
        avoidanceDirection = retreat;
        avoidanceUntil = Time.time + avoidHoldTime;
        return CanWalk(retreat) ? retreat : Vector3.zero;
    }

    private void TryArmWallFollow(Vector3 desired, Vector3 anchor)
    {
        if (!TrySlideAlongSurface(desired, anchor, out Vector3 slide)) return;
        wallSlideDirection = slide;
        wallFollowUntil = Time.time + wallFollowTime;
    }

    // Slide parallel to the blocking surface. The ±15° candidate grid is anchored to the
    // player direction, which keeps rotating while the enemy travels along a wall; a
    // tangent taken from the actual hit normal stays on the face and rounds its shape.
    private bool TrySlideAlongSurface(Vector3 desired, Vector3 anchor, out Vector3 slide)
    {
        slide = Vector3.zero;
        if (desired.sqrMagnitude < .001f) return false;
        CapsuleProbe(out Vector3 top, out Vector3 bottom, out float radius);
        int count = Physics.CapsuleCastNonAlloc(top, bottom, radius * .9f, desired, movementHits,
            obstacleProbeDistance, obstacleLayers, QueryTriggerInteraction.Ignore);
        if (count == movementHits.Length) return false;
        float nearest = float.PositiveInfinity;
        Vector3 normal = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            var hit = movementHits[i];
            if (hit.transform.IsChildOf(transform) ||
                hit.collider.GetComponentInParent<IDamageable>() != null) continue;
            if (hit.normal.y >= .65f) continue;
            if (hit.distance >= nearest) continue;
            nearest = hit.distance;
            normal = hit.normal;
        }
        if (nearest == float.PositiveInfinity) return false;
        normal.y = 0f;
        if (normal.sqrMagnitude < .0001f) return false;
        normal.Normalize();
        Vector3 tangent = Vector3.Cross(Vector3.up, normal);
        // Orient the tangent toward the side already committed to, so the commitment
        // survives the refresh instead of flipping at every new cast.
        slide = tangent * (Vector3.Dot(tangent, anchor) >= 0f ? 1f : -1f);
        return slide.sqrMagnitude > .0001f;
    }

    private void RememberBlockedDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude < .001f) return;
        blockedDirections[blockedCursor] = direction.normalized;
        blockedUntil[blockedCursor] = Time.time + BlockedMemorySeconds;
        blockedCursor = (blockedCursor + 1) % blockedDirections.Length;
    }

    private float BlockedPenalty(Vector3 candidate)
    {
        float penalty = 0f;
        for (int i = 0; i < blockedDirections.Length; i++)
        {
            float remaining = blockedUntil[i] - Time.time;
            if (remaining <= 0f) continue;
            if (Vector3.Dot(candidate, blockedDirections[i]) < .8f) continue;
            penalty = Mathf.Max(penalty, .55f * Mathf.Clamp01(remaining / BlockedMemorySeconds));
        }
        return penalty;
    }

    private bool TryEscapeContact(Vector3 desired, out Vector3 escape)
    {
        escape = Vector3.zero;
        CapsuleProbe(out Vector3 top, out Vector3 bottom, out float radius);
        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius * .95f, separationHits,
            obstacleLayers, QueryTriggerInteraction.Ignore);
        Vector3 push = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            var collider = separationHits[i];
            if (collider.transform.IsChildOf(transform) ||
                collider.GetComponentInParent<IDamageable>() != null) continue;
            Vector3 away = transform.position - collider.bounds.ClosestPoint(transform.position);
            away.y = 0f;
            float strength = 1f - Mathf.Clamp01(away.magnitude / Mathf.Max(.01f, radius));
            if (away.sqrMagnitude < .0001f)
            {
                // The collider contains us entirely: push away from its volume instead.
                away = transform.position - collider.bounds.center;
                away.y = 0f;
            }
            if (away.sqrMagnitude < .0001f) away = -desired;
            if (away.sqrMagnitude < .0001f) away = transform.forward;
            push += away.normalized * Mathf.Max(.15f, strength);
        }
        if (push.sqrMagnitude < .0001f) return false;
        escape = push.normalized;
        return CanWalk(escape);
    }

    private bool ObstacleAhead(Vector3 direction, float distance) =>
        ObstacleDistance(direction, distance) <= distance;

    private float ObstacleDistance(Vector3 direction, float distance)
    {
        if (direction.sqrMagnitude < .001f || distance <= 0f) return float.PositiveInfinity;
        direction.Normalize();
        CapsuleProbe(out Vector3 top, out Vector3 bottom, out float radius);
        int count = Physics.CapsuleCastNonAlloc(top, bottom, radius * .9f, direction, movementHits, distance,
            obstacleLayers, QueryTriggerInteraction.Ignore);
        if (count == movementHits.Length) return 0f;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            var hit = movementHits[i];
            if (hit.transform.IsChildOf(transform) ||
                hit.collider.GetComponentInParent<IDamageable>() != null) continue;
            if (hit.normal.y >= .65f) continue;
            if (hit.distance < nearest) nearest = hit.distance;
        }
        return nearest;
    }

    private bool PathClear(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        if (distance < .01f) return true;
        CapsuleProbe(out Vector3 top, out Vector3 bottom, out float radius);
        int count = Physics.CapsuleCastNonAlloc(top, bottom, radius * .85f, offset / distance, movementHits,
            distance, obstacleLayers, QueryTriggerInteraction.Ignore);
        if (count == movementHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = movementHits[i];
            if (hit.distance <= .05f || hit.transform.IsChildOf(transform) ||
                hit.collider.GetComponentInParent<IDamageable>() != null || hit.normal.y >= .65f) continue;
            return false;
        }
        return true;
    }

    private bool PointClear(Vector3 point)
    {
        CapsuleProbe(out Vector3 top, out Vector3 bottom, out float radius);
        Vector3 center = point + (capsule != null ? capsule.bounds.center - transform.position : Vector3.up);
        float halfHeight = Mathf.Max(.01f, (top - bottom).magnitude * .5f - radius * .1f);
        Vector3 axis = Vector3.up * halfHeight;
        int count = Physics.OverlapCapsuleNonAlloc(center - axis, center + axis, radius * .9f,
            detourHits, obstacleLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var hit = detourHits[i];
            if (hit.transform.IsChildOf(transform) ||
                hit.GetComponentInParent<IDamageable>() != null) continue;
            if (hit.bounds.max.y <= point.y + .15f) continue;
            return false;
        }
        return true;
    }

    private bool IsStraightWalkable(Vector3 destination)
    {
        if (UsingAgent) return true;
        Vector3 offset = destination - transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        if (distance < .01f) return true;
        Vector3 direction = offset / distance;
        CapsuleProbe(out Vector3 top, out Vector3 bottom, out float radius);
        int count = Physics.CapsuleCastNonAlloc(top, bottom, radius * .9f, direction, movementHits, distance,
            obstacleLayers, QueryTriggerInteraction.Ignore);
        if (count == movementHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = movementHits[i];
            if (hit.transform.IsChildOf(transform) ||
                hit.collider.GetComponentInParent<IDamageable>() != null) continue;
            if (hit.normal.y < .65f) return false;
        }
        return true;
    }

    private void ProbeAgentDetour(Vector3 heading)
    {
        nextAgentDetour = Time.time + .1f;
        heading.y = 0f;
        if (heading.sqrMagnitude < .001f) return;
        float reach = obstacleProbeDistance;
        if (agent.hasPath && !agent.pathPending)
            reach = Mathf.Min(reach, HorizontalDistance(transform.position, agent.steeringTarget));
        if (reach < .2f) return;
        if (ObstacleDistance(heading, reach) >= reach - .15f) return;
        nextAgentDetour = Time.time + .5f;
        TryAgentDetour(false);
    }

    private void MonitorAgentProgress()
    {
        if (Time.time < nextAgentStuckCheck) return;
        nextAgentStuckCheck = Time.time + .5f;
        Vector3 position = transform.position;
        float moved = HorizontalDistance(position, agentLastPosition);
        agentLastPosition = position;
        if (Time.time < agentRecoveryUntil)
        {
            // A detour that cannot even start moving is wasted commitment: cut it short
            // and flip sides so the next probe tries the other way while the block is
            // still fresh, instead of sitting out the full recovery window.
            if (moved < .05f)
            {
                agentRecoveryUntil = Time.time;
                steeringSide = -steeringSide;
            }
            agentStuckFor = 0f;
            return;
        }
        bool travelling = !agent.isStopped && agent.hasPath && !agent.pathPending &&
            agent.remainingDistance > agent.stoppingDistance + .2f;
        if (!travelling)
        {
            agentStuckFor = 0f;
            return;
        }
        agentStuckFor = moved < .08f ? agentStuckFor + .5f : Mathf.Max(0f, agentStuckFor - .5f);
        if (agentStuckFor < 1f) return;
        agentStuckFor = 0f;
        TryAgentDetour(true);
    }

    private void TryAgentDetour(bool alternateSide)
    {
        Vector3 forward = agent.hasPath && !agent.pathPending
            ? agent.steeringTarget - transform.position
            : agent.destination - transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < .001f) forward = FacingDirection;
        forward.Normalize();
        if (alternateSide) steeringSide = -steeringSide;
        // Score ring candidates by how much they keep approaching the goal, so the
        // recovery slides around the blocking face instead of wandering backwards.
        Vector3 toGoal = agent.destination - transform.position;
        toGoal.y = 0f;
        toGoal = toGoal.sqrMagnitude > .001f ? toGoal.normalized : forward;

        // Side steps nearest first and progressively sharper: the enemy slides around
        // the obstacle face instead of hopping to a random far point. Alternating sides
        // prevents the recovery itself from repeating the same failed heading.
        for (int i = 0; i < 12; i++)
        {
            float side = i % 2 == 0 ? steeringSide : -steeringSide;
            float angle = 40f + 12.5f * (i / 2);
            float reach = 1.1f + .55f * (i / 2);
            Vector3 direction = Quaternion.Euler(0f, side * angle, 0f) * forward;
            Vector3 candidate = transform.position + direction * reach;
            if (!TrySampleNavMesh(candidate, 2f, out Vector3 point)) continue;
            if (HorizontalDistance(point, transform.position) < .75f) continue;
            if (!PointClear(point) || !PathClear(point)) continue;
            if (!agent.CalculatePath(point, patrolPath) || patrolPath.status != NavMeshPathStatus.PathComplete)
                continue;
            steeringSide = side;
            MoveAgentToDetour(point, 1.2f);
            return;
        }

        for (int ring = 0; ring < 2; ring++)
        {
            float radius = ring == 0 ? 2.5f : 4f;
            Vector3 bestPoint = Vector3.zero;
            float bestAlignment = float.NegativeInfinity;
            bool found = false;
            for (int i = 0; i < 12; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, i * 30f, 0f) * forward;
                Vector3 candidate = transform.position + direction * radius;
                if (!TrySampleNavMesh(candidate, 2.5f, out Vector3 point)) continue;
                if (HorizontalDistance(point, transform.position) < 1f) continue;
                if (!PointClear(point) || !PathClear(point)) continue;
                if (!agent.CalculatePath(point, patrolPath) || patrolPath.status != NavMeshPathStatus.PathComplete)
                    continue;
                Vector3 offset = point - transform.position;
                offset.y = 0f;
                float alignment = offset.sqrMagnitude > .001f
                    ? Vector3.Dot(offset.normalized, toGoal) : 0f;
                if (alignment < bestAlignment) continue;
                bestAlignment = alignment;
                bestPoint = point;
                found = true;
                float turn = Vector3.SignedAngle(forward, direction, Vector3.up);
                if (Mathf.Abs(turn) > 5f) steeringSide = Mathf.Sign(turn);
            }
            if (found)
            {
                MoveAgentToDetour(bestPoint, 1.2f);
                return;
            }
        }

        Vector3 retreat = transform.position - forward * Random.Range(1.5f, 2.5f);
        if (TrySampleNavMesh(retreat, 2f, out Vector3 fallback) && PointClear(fallback) && PathClear(fallback) &&
            agent.CalculatePath(fallback, patrolPath) && patrolPath.status == NavMeshPathStatus.PathComplete)
            MoveAgentToDetour(fallback, .8f);
    }

    private void MoveAgentToDetour(Vector3 point, float duration)
    {
        agent.ResetPath();
        agent.isStopped = false;
        agent.SetDestination(point);
        agentRecoveryUntil = Time.time + duration;
        nextPathUpdate = Time.time + duration * .6f;
        progressPosition = transform.position;
        nextProgressCheck = Time.time + duration + .5f;
        if (State != BehaviourState.Wandering) return;
        patrolDestination = point;
        hasPatrolDestination = true;
        patrolDeadline = Time.time + duration + 1.5f;
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;
        if (Time.time < nextAgentSnapAttempt) return false;
        nextAgentSnapAttempt = Time.time + 1f;
        if (!TrySampleNavMesh(transform.position, 3f, out Vector3 point)) return false;
        if (!agent.Warp(point) || !agent.isOnNavMesh) return false;
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
        return true;
    }

    private bool TrySampleNavMesh(Vector3 position, float radius, out Vector3 point)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(position, out var hit, radius, filter))
        {
            point = position;
            return false;
        }
        point = hit.position;
        return true;
    }

    private float CapsuleProbeRadius => capsule != null
        ? Mathf.Max(.05f, Mathf.Max(capsule.bounds.extents.x, capsule.bounds.extents.z) - .03f) : .4f;

    private void CapsuleProbe(out Vector3 top, out Vector3 bottom, out float radius)
    {
        radius = CapsuleProbeRadius;
        float halfHeight = capsule != null ? Mathf.Max(radius, capsule.bounds.extents.y - .03f) : .9f;
        Vector3 center = capsule != null ? capsule.bounds.center : transform.position;
        Vector3 axis = Vector3.up * (halfHeight - radius);
        if (gameObject.scene.name == "CampusExterior")
        {
            // Raise the bottom endpoint only: retain head clearance above a curb.
            center += Vector3.up * .175f;
            axis -= Vector3.up * Mathf.Min(.175f, axis.y);
        }
        top = center + axis;
        bottom = center - axis;
    }

    private void UpdateSeparation()
    {
        if (Time.time < nextSeparation) return;
        nextSeparation = Time.time + .2f;
        separation = Vector3.zero;
        nearbyAllies.Clear();
        int count = Physics.OverlapSphereNonAlloc(transform.position, separationRadius,
            separationHits, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var ally = separationHits[i].GetComponentInParent<Enemy>();
            if (ally == null || ally == this || ally.IsDead || nearbyAllies.Contains(ally)) continue;
            nearbyAllies.Add(ally);
            Vector3 away = transform.position - ally.transform.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance > .01f)
                separation += away / distance * Mathf.Clamp01(1f - distance / separationRadius);
        }
        separation = Vector3.ClampMagnitude(separation, 1f);
    }

    private bool CanWalk(Vector3 direction)
    {
        if (direction.sqrMagnitude < .001f) return false;
        direction.Normalize();
        CapsuleProbe(out Vector3 top, out Vector3 bottom, out float radius);
        float distance = Mathf.Max(.3f, desiredSpeed * Time.fixedDeltaTime + .1f);
        int count = Physics.CapsuleCastNonAlloc(top, bottom, radius, direction,
            movementHits, distance, obstacleLayers, QueryTriggerInteraction.Ignore);
        if (count == movementHits.Length) return false;
        for (int i = 0; i < count; i++)
            if (!movementHits[i].transform.IsChildOf(transform) && movementHits[i].normal.y < .65f) return false;
        return TryGround(transform.position + direction * (distance + radius), out _);
    }

    private bool TryGround(Vector3 position, out Vector3 grounded)
    {
        if (gameObject.scene.name == "CampusExterior")
        {
            float campusFootOffset = CampusFootOffset();
            grounded = position;
            if (!UniDoom.Utils.CampusGrounding.TryFloor(position - Vector3.up * campusFootOffset, out var support) ||
                Mathf.Abs(support.point.y - (position.y - campusFootOffset)) > .35f) return false;
            grounded.y = support.point.y + campusFootOffset;
            return true;
        }
        float footOffset = capsule != null ? transform.position.y - capsule.bounds.min.y : 1f;
        Vector3 origin = position + Vector3.up * .4f;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, movementHits, footOffset + .9f,
            obstacleLayers, QueryTriggerInteraction.Ignore);
        float nearest = float.PositiveInfinity;
        grounded = position;
        for (int i = 0; i < count; i++)
        {
            var hit = movementHits[i];
            if (hit.transform.IsChildOf(transform) || hit.collider.GetComponentInParent<IDamageable>() != null ||
                hit.normal.y < .65f || hit.distance >= nearest) continue;
            // Do not use tabletops as a floor or walk over ledges taller than a small step.
            if (Mathf.Abs(hit.point.y - (position.y - footOffset)) > .35f) continue;
            nearest = hit.distance;
            grounded = hit.point + Vector3.up * footOffset;
        }
        return nearest < float.PositiveInfinity;
    }

    private float CampusFootOffset()
    {
        if (!capsule) return 1f;
        // Derive feet from the collider geometry, not the interpolated physics bounds.
        // Appearance variants can change height/center while preserving the same feet.
        Vector3 scale = transform.lossyScale;
        float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float halfHeight = Mathf.Max(capsule.height * Mathf.Abs(scale.y) * .5f, radius);
        return halfHeight - transform.TransformVector(capsule.center).y;
    }

    private void Face(Vector3 direction, float deltaTime)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < .001f) return;
        Quaternion rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * deltaTime);
        if (body != null && !body.isKinematic) body.MoveRotation(rotation);
        else transform.rotation = rotation;
    }

    private Vector3 ChaseFacing(Vector3 movement)
    {
        movement.y = 0f;
        if (State == BehaviourState.Wandering) return movement;
        Vector3 towardsPlayer = (State == BehaviourState.Chasing && target != null
            ? target.position : lastSeenPosition) - transform.position;
        towardsPlayer.y = 0f;
        if (towardsPlayer.sqrMagnitude < .001f) return movement;
        // Chasing and searching keep the player (or the clue) in front; only
        // wandering follows the walk direction.
        return towardsPlayer;
    }

    private float MovementAlignment(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude < .001f ? 1f :
            Mathf.Lerp(.6f, 1f, Mathf.Clamp01(Vector3.Dot(FacingDirection, direction.normalized)));
    }

    private void TryAttack()
    {
        EnemySpriteAppearance ranged = RangedAppearance;
        if (IsDead || target == null || (targetLife != null && targetLife.IsDead) ||
            Time.time - lastAttackTime < (ranged != null ? ranged.projectileCooldown : attackCooldown) ||
            Vector3.Angle(FacingDirection, target.position - transform.position) > 45f) return;
        lastAttackTime = Time.time;
        Attacked?.Invoke();

        if (ranged != null)
        {
            pendingProjectile = ranged;
            projectileReleaseAt = Time.time + ranged.projectileReleaseDelay;
            attackEndsAt = Time.time + Mathf.Max(ranged.attackDuration, ranged.projectileReleaseDelay);
            return;
        }

        meleeHitAt = Time.time + meleeWindup;
        meleeEndsAt = meleeHitAt + meleeRecovery;
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
        else if (source != null && source.GetComponentInParent<PlayerLife>() is PlayerLife attacker)
        {
            // Being hit raises the alarm: this zombie and the whole nearby group
            // actively pursue the shooter instead of merely investigating a clue.
            ReceiveHunt(attacker.transform.position, huntDuration);
            AlertNearby();
            AlertAlliesToHunt(attacker.transform.position);
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
        Gun.Gunshot -= HearGunshot;
        meleeHitAt = -1f;
        meleeEndsAt = 0f;
        pendingProjectile = null;
        projectileReleaseAt = -1f;
        StopMoving();
        RestoreHitColors();
    }

    private void Die(GameObject killer)
    {
        if (IsDead) return;

        IsDead = true;
        meleeHitAt = -1f;
        meleeEndsAt = 0f;
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
