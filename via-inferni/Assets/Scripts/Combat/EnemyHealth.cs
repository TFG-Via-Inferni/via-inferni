using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Min(1f)] [SerializeField] private float maxHealth = 30f;
    [SerializeField] private bool destroyOnDeath = true;

    private float currentHealth;
    private SpriteDamageFlash damageFlash;

    public event Action<EnemyHealth, GameObject> Died;

    public bool CanTakeDamage => currentHealth > 0f;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
        damageFlash = GetComponent<SpriteDamageFlash>();

        if (damageFlash == null)
        {
            damageFlash = gameObject.AddComponent<SpriteDamageFlash>();
        }
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

        if (damageFlash != null)
        {
            damageFlash.PlayFlash();
        }

        // Notify source owner that it dealt damage (used for Cloak special: first-damage deactivates)
        if (source != null)
        {
            DamageSourceContext context = source.GetComponent<DamageSourceContext>();
            if (context != null && context.OwnerRoot != null)
            {
                Player player = context.OwnerRoot.GetComponent<Player>();
                if (player != null)
                {
                    player.NotifyDealtDamage();
                }
            }
        }

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
