using UnityEngine;

/// <summary>Camera-facing zombie frames, shared by ordinary enemies and navigation variants.</summary>
[RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(10000)]
public sealed class EnemySpriteAnimator : MonoBehaviour
{
    [SerializeField] Enemy enemy;
    [SerializeField] Sprite[] frontWalkFrames;
    [SerializeField] Sprite[] backWalkFrames;
    [SerializeField] Sprite[] deathFrames;
    [SerializeField, Min(1)] float walkFramesPerSecond = 9;
    [SerializeField, Min(.1f)] float deathDuration = 1.35f;
    [SerializeField] EnemySpriteAppearance alternateAppearance;
    [SerializeField] EnemySpriteAppearance[] alternateAppearances;
    [SerializeField, Range(0, 1)] float alternateChance = .15f;

    public EnemySpriteAppearance Appearance { get; private set; }
    public bool ShowingBack { get; private set; }
    public bool DeathFinished { get; private set; }
    public int FrameIndex { get; private set; }
    SpriteRenderer spriteRenderer;
    Camera viewCamera;
    Vector3 lastPosition;
    float walkClock, deathStarted = -1;
    float lastMovedAt = -1;
    int viewDirection;
    Vector3 footOffset;
    float attackStarted = -100, painStarted = -100;
    float facing = 1, side;

    void Awake()
    {
        if (enemy == null) enemy = GetComponentInParent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        footOffset = transform.localPosition;
        if (enemy != null)
        {
            lastPosition = enemy.transform.position;
            // Roll once per instance. All spawners and prefab variants inherit the same mixture.
            if (Random.value < alternateChance)
            {
                Appearance = PickAlternateAppearance();
            }
            if (Appearance != null)
            {
                transform.localScale *= Appearance.visualScale;
                if (Appearance.material != null) spriteRenderer.sharedMaterial = Appearance.material;
                Sprite[] initialFrames = HasFrames(Appearance.idleFrames)
                    ? Appearance.idleFrames
                    : Appearance.frontWalkFrames;
                if (HasFrames(initialFrames)) spriteRenderer.sprite = initialFrames[0];
                deathFrames = Appearance.deathFrames;
                deathDuration = Appearance.deathDuration;
                var capsule = enemy.GetComponent<CapsuleCollider>();
                if (capsule != null)
                {
                    // Retain the original foot position and root used by Rigidbody/NavMesh movement.
                    float bottom = capsule.center.y - capsule.height * .5f;
                    capsule.radius = Appearance.colliderRadius;
                    capsule.height = Appearance.colliderHeight;
                    Vector3 center = capsule.center;
                    center.y = bottom + capsule.height * .5f;
                    capsule.center = center;
                }
                var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null)
                {
                    agent.height = Appearance.colliderHeight;
                    agent.radius = Appearance.colliderRadius;
                }
            }
        }
    }

    EnemySpriteAppearance PickAlternateAppearance()
    {
        if (alternateAppearances != null && alternateAppearances.Length > 0)
        {
            int start = Random.Range(0, alternateAppearances.Length);
            for (int offset = 0; offset < alternateAppearances.Length; offset++)
            {
                EnemySpriteAppearance candidate = alternateAppearances[(start + offset) % alternateAppearances.Length];
                if (candidate != null) return candidate;
            }
        }
        return alternateAppearance;
    }

    void OnEnable()
    {
        if (enemy != null && enemy.OnDeath != null) enemy.OnDeath.AddListener(PlayDeath);
        if (enemy != null)
        {
            enemy.Attacked += PlayAttack;
            if (enemy.OnHealthChanged != null) enemy.OnHealthChanged.AddListener(PlayPain);
        }
    }
    void OnDisable()
    {
        if (enemy != null && enemy.OnDeath != null) enemy.OnDeath.RemoveListener(PlayDeath);
        if (enemy != null)
        {
            enemy.Attacked -= PlayAttack;
            if (enemy.OnHealthChanged != null) enemy.OnHealthChanged.RemoveListener(PlayPain);
        }
    }
    void PlayAttack() { attackStarted = Time.time; }
    void PlayPain(float health, float maximum) { if (health > 0) painStarted = Time.time; }
    void PlayDeath(GameObject killer)
    {
        if (deathStarted >= 0) return;
        deathStarted = Time.time;
        spriteRenderer.flipX = false;
        // Keep the corpse visible without blocking the player, other enemies or later shots.
        foreach (var collider in enemy.GetComponentsInChildren<Collider>()) collider.enabled = false;
    }

    void LateUpdate()
    {
        if (enemy == null) return;
        if (viewCamera == null || !viewCamera.isActiveAndEnabled) viewCamera = Camera.main;
        if (viewCamera != null)
        {
            Vector3 toCamera = viewCamera.transform.position - enemy.transform.position;
            toCamera.y = 0;
            Vector3 heading = enemy.FacingDirection;
            heading.y = 0;
            if (toCamera.sqrMagnitude > .001f)
            {
                facing = Vector3.Dot(heading.normalized, toCamera.normalized);
                // Avoid front/back flicker when a wandering zombie is seen almost side-on.
                if (facing > .12f) ShowingBack = false;
                else if (facing < -.12f) ShowingBack = true;
                side = Vector3.Dot(Vector3.Cross(Vector3.up, heading.normalized), toCamera.normalized);
                // Quantise with a margin so diagonal views do not alternate sprite strips.
                float angle = Mathf.Atan2(side, facing) * Mathf.Rad2Deg;
                float previousAngle = viewDirection * 45f;
                if (Mathf.Abs(Mathf.DeltaAngle(previousAngle, angle)) > 28f)
                    viewDirection = Mathf.RoundToInt(angle / 45f);
                transform.rotation = Quaternion.LookRotation(-toCamera);
            }
        }
        transform.position = enemy.transform.TransformPoint(footOffset);
        Vector3 displacement = enemy.transform.position - lastPosition;
        displacement.y = 0;
        lastPosition = enemy.transform.position;
        if (Time.timeScale <= 0f) return;
        if (displacement.sqrMagnitude > .00000001f) lastMovedAt = Time.time;
        // Rigidbody moves at the physics rate, which may be lower than the rendering rate.
        bool walking = Time.time - lastMovedAt < .12f;
        if (enemy.IsDead)
        {
            if (deathStarted < 0) PlayDeath(null);
            float progress = Mathf.Clamp01((Time.time - deathStarted) / Mathf.Max(.1f, deathDuration));
            if (HasFrames(deathFrames))
            {
                FrameIndex = Mathf.Min(deathFrames.Length - 1, Mathf.FloorToInt(progress * deathFrames.Length));
                SetFrame(deathFrames);
            }
            else
            {
                // Sheets without a death strip keep their own character and collapse at the feet.
                Vector3 scale = transform.localScale;
                scale.y = Mathf.Lerp(Appearance != null ? Appearance.visualScale : 1f, .08f, progress);
                transform.localScale = scale;
            }
            DeathFinished = progress >= 1;
        }
        else
        {
            if (Appearance != null)
            {
                AnimateAlternate(walking);
                return;
            }
            var frames = ShowingBack && backWalkFrames != null && backWalkFrames.Length > 0 ? backWalkFrames : frontWalkFrames;
            if (frames == null || frames.Length == 0) return;
            // Use actual displacement so NavMesh-driven library enemies animate too.
            spriteRenderer.flipX = false;
            if (walking) walkClock += Time.deltaTime * walkFramesPerSecond;
            else walkClock = 0f;
            FrameIndex = walking ? Mathf.FloorToInt(walkClock) % frames.Length : 0;
            SetFrame(frames);
        }
    }
    void AnimateAlternate(bool walking)
    {
        spriteRenderer.flipX = false;
        if (Time.time - painStarted < Appearance.painDuration && HasFrames(Appearance.painFrames))
        {
            FrameIndex = Mathf.FloorToInt((Time.time - painStarted) / Appearance.painDuration * Appearance.painFrames.Length);
            SetFrame(Appearance.painFrames);
            return;
        }
        if (Time.time - attackStarted < Appearance.attackDuration && HasFrames(Appearance.attackFrames))
        {
            FrameIndex = Mathf.FloorToInt((Time.time - attackStarted) / Appearance.attackDuration * Appearance.attackFrames.Length);
            SetFrame(Appearance.attackFrames);
            return;
        }
        Sprite[] frontFrames = HasFrames(Appearance.frontWalkFrames)
            ? Appearance.frontWalkFrames
            : Appearance.idleFrames;
        Sprite[] frames = walking ? frontFrames : Appearance.idleFrames;
        if (!HasFrames(frames)) frames = frontFrames;
        int direction = Mathf.Abs(viewDirection);
        if (direction >= 3 && HasFrames(Appearance.backWalkFrames)) frames = Appearance.backWalkFrames;
        else if (direction > 0 && direction < 3)
        {
            if (direction == 2 && HasFrames(Appearance.sideWalkFrames))
            {
                frames = Appearance.sideWalkFrames;
                // The source side-walk strip faces left.
                spriteRenderer.flipX = viewDirection < 0;
            }
            else
            {
                Sprite[] diagonalFrames = viewDirection > 0 ? Appearance.leftWalkFrames : Appearance.rightWalkFrames;
                if (HasFrames(diagonalFrames)) frames = diagonalFrames;
            }
        }
        if (!HasFrames(frames)) frames = HasFrames(Appearance.idleFrames) ? Appearance.idleFrames : frontWalkFrames;
        if (!HasFrames(frames)) return;
        walkClock += Time.deltaTime * (walking ? Appearance.walkFramesPerSecond : Appearance.idleFramesPerSecond);
        FrameIndex = walking || frames == Appearance.idleFrames
            ? Mathf.FloorToInt(walkClock) % Mathf.Max(1, frames.Length)
            : 0;
        SetFrame(frames);
    }
    void SetFrame(Sprite[] frames)
    {
        if (HasFrames(frames))
        {
            Sprite frame = frames[Mathf.Clamp(FrameIndex, 0, frames.Length - 1)];
            if (frame != null) spriteRenderer.sprite = frame;
        }
    }

    static bool HasFrames(Sprite[] frames) => frames != null && frames.Length > 0;
}
