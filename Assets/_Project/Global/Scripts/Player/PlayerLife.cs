using System;
using UnityEngine;

public class PlayerLife : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;
    private float invulnerableUntil;

    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamaged;
    public event Action OnDeath;

    private void Awake()
    {
        maxHealth = Mathf.Max(1, maxHealth);
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;
        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        if (currentHealth != before)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0 || Time.time < invulnerableUntil ||
            UniDoom.UI.DoorReloadScreen.IsReloading) return;
        int before = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        if (currentHealth != before)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            OnDamaged?.Invoke(before - currentHealth);
            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }

    void IDamageable.TakeDamage(float amount, GameObject source)
    {
        TakeDamage(Mathf.RoundToInt(amount));
    }

    private void Die()
    {
        OnDeath?.Invoke();
    }

    public void Revive(float protectionSeconds = 2f)
    {
        if (!IsDead) return;
        currentHealth = maxHealth;
        invulnerableUntil = Time.time + Mathf.Max(0f, protectionSeconds);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void RestoreHealth()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
