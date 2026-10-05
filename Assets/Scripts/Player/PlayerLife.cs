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

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Heal(int amount)
    {
        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        if (currentHealth != before)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int amount)
    {
        int before = currentHealth;
        currentHealth = Mathf.Max(currentHealth - amount, 0);

        if (currentHealth != before)
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

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
        Debug.Log("Player morreu!");
        OnDeath?.Invoke();
    }
}