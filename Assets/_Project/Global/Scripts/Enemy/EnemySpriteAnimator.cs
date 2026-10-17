using UnityEngine;

/// <summary>Camera-facing zombie frames, shared by ordinary enemies and navigation variants.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemySpriteAnimator : MonoBehaviour
{
    [SerializeField] Enemy enemy;
    [SerializeField] Sprite[] frontWalkFrames;
    [SerializeField] Sprite[] backWalkFrames;
    [SerializeField] Sprite[] deathFrames;
    [SerializeField, Min(1)] float walkFramesPerSecond = 9;
    [SerializeField, Min(.1f)] float deathDuration = 1.35f;
    [SerializeField] EnemySpriteAppearance alternateAppearance;
    [SerializeField, Range(0, 1)] float alternateChance = .15f;

    public EnemySpriteAppearance Appearance { get; private set; }
    public bool ShowingBack { get; private set; }
    public bool DeathFinished { get; private set; }
    public int FrameIndex { get; private set; }
    SpriteRenderer spriteRenderer;
    Camera viewCamera;
    Vector3 lastPosition;
    float walkClock, deathStarted = -1;
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
            if (alternateAppearance != null && Random.value < alternateChance)
            {
                Appearance = alternateAppearance;
                transform.localScale *= Appearance.visualScale;
                spriteRenderer.sharedMaterial = Appearance.material;
                if (Appearance.idleFrames.Length > 0) spriteRenderer.sprite = Appearance.idleFrames[0];
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
        if (viewCamera == null) viewCamera = Camera.main;
        if (viewCamera != null)
        {
            Vector3 toCamera = viewCamera.transform.position - enemy.transform.position;
            toCamera.y = 0;
            Vector3 heading = enemy.transform.forward;
            heading.y = 0;
            if (toCamera.sqrMagnitude > .001f)
            {
                ShowingBack = Vector3.Dot(heading, toCamera) < 0;
                facing = Vector3.Dot(heading.normalized, toCamera.normalized);
                side = Vector3.Dot(Vector3.Cross(Vector3.up, heading.normalized), toCamera.normalized);
                transform.rotation = Quaternion.LookRotation(-toCamera);
            }
        }
        // The existing enemy may pitch toward the player's height. Keep the sprite upright.
        transform.position = enemy.transform.position + Vector3.up * (footOffset.y * enemy.transform.lossyScale.y);
        Vector3 displacement = enemy.transform.position - lastPosition;
        displacement.y = 0;
        lastPosition = enemy.transform.position;
        if (enemy.IsDead)
        {
            if (deathStarted < 0) PlayDeath(null);
            float progress = Mathf.Clamp01((Time.time - deathStarted) / deathDuration);
            FrameIndex = Mathf.Min(deathFrames.Length - 1, Mathf.FloorToInt(progress * deathFrames.Length));
            SetFrame(deathFrames);
            DeathFinished = progress >= 1;
        }
        else
        {
            if (Appearance != null)
            {
                AnimateAlternate(displacement.sqrMagnitude > .000001f);
                return;
            }
            var frames = ShowingBack && backWalkFrames != null && backWalkFrames.Length > 0 ? backWalkFrames : frontWalkFrames;
            if (frames == null || frames.Length == 0) return;
            // Use actual displacement so NavMesh-driven library enemies animate too.
            bool walking = displacement.sqrMagnitude > .000001f;
            if (walking) walkClock += Time.deltaTime * walkFramesPerSecond;
            FrameIndex = walking ? Mathf.FloorToInt(walkClock) % frames.Length : 0;
            SetFrame(frames);
        }
    }
    void AnimateAlternate(bool walking)
    {
        spriteRenderer.flipX = false;
        if (Time.time - painStarted < Appearance.painDuration && Appearance.painFrames.Length > 0)
        {
            FrameIndex = Mathf.FloorToInt((Time.time - painStarted) / Appearance.painDuration * Appearance.painFrames.Length);
            SetFrame(Appearance.painFrames);
            return;
        }
        if (Time.time - attackStarted < Appearance.attackDuration && Appearance.attackFrames.Length > 0)
        {
            FrameIndex = Mathf.FloorToInt((Time.time - attackStarted) / Appearance.attackDuration * Appearance.attackFrames.Length);
            SetFrame(Appearance.attackFrames);
            return;
        }
        Sprite[] frames = Appearance.idleFrames;
        if (facing < -.7f) frames = Appearance.backWalkFrames;
        else if (facing < .7f)
        {
            if (Mathf.Abs(facing) < .35f)
            {
                frames = Appearance.sideWalkFrames;
                // The source side-walk strip faces left.
                spriteRenderer.flipX = side < 0;
            }
            else frames = side > 0 ? Appearance.leftWalkFrames : Appearance.rightWalkFrames;
        }
        walkClock += Time.deltaTime * (walking ? Appearance.walkFramesPerSecond : Appearance.idleFramesPerSecond);
        FrameIndex = walking || frames == Appearance.idleFrames ? Mathf.FloorToInt(walkClock) % Mathf.Max(1, frames.Length) : 0;
        SetFrame(frames);
    }
    void SetFrame(Sprite[] frames)
    {
        if (frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[Mathf.Clamp(FrameIndex, 0, frames.Length - 1)];
    }
}
