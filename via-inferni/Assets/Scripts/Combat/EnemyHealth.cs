using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Min(1f)] [SerializeField] private float maxHealth = 30f;
    [SerializeField] private bool destroyOnDeath = true;

    private float currentHealth;

    public event Action<EnemyHealth, GameObject> Died;

    public bool CanTakeDamage => currentHealth > 0f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
    }

    public void Configure(float newMaxHealth, bool restoreFullHealth = true)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);

        if (restoreFullHealth)
        {
            currentHealth = maxHealth;
            return;
        }

        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount, GameObject source = null)
    {
        if (amount <= 0f || !CanTakeDamage)
        {
            return;
        }

        EnemyController enemyController = GetComponent<EnemyController>();
        if (enemyController != null)
        {
            amount = enemyController.ModifyIncomingDamage(amount, source);
        }

        if (amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (currentHealth > 0f)
        {
            return;
        }

        Died?.Invoke(this, source);

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }
}
