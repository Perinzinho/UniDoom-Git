using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

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

    private void Awake()
    {
        currentHealth = maxHealth;
        body = GetComponent<Rigidbody>();
        hitRenderers = GetComponentsInChildren<Renderer>();
        originalPropertyBlocks = new MaterialPropertyBlock[hitRenderers.Length];
        hitPropertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < hitRenderers.Length; i++)
            originalPropertyBlocks[i] = new MaterialPropertyBlock();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    private void Update()
    {
        if (isFlashing && Time.time >= hitFlashEndsAt)
            RestoreHitColors();

        if (IsDead || target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= attackRange)
        {
            TryAttack();
        }
        else
        {
            MoveTowardsTarget();
        }
    }

    private void MoveTowardsTarget()
    {
        if (IsDead || target == null) return;

        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
            transform.forward = direction;
    }

    private void TryAttack()
    {
        if (IsDead || target == null || Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;

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
        RestoreHitColors();
    }

    private void Die(GameObject killer)
    {
        if (IsDead) return;

        IsDead = true;
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
}
