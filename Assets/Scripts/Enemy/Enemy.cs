using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [FormerlySerializedAs("vidaMaxima")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

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

    public bool IsDead { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            target = player.transform;
    }

    private void Update()
    {
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
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
            transform.forward = direction;
    }

    private void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
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

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die(source);
        }
    }

    private void Die(GameObject killer)
    {
        IsDead = true;
        OnDeath?.Invoke(killer);
        Destroy(gameObject, 0.1f);
    }
}
