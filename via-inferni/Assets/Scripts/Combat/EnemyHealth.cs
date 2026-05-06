using System;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Min(1f)] [SerializeField] private float maxHealth = 30f;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private Vector3 combatTextOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private Color criticalTextColor = new Color(1f, 0.85f, 0.25f, 1f);

    private float currentHealth;
    private SpriteDamageFlash damageFlash;
    private EnemyCombatStatus combatStatus;

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

        combatStatus = GetComponent<EnemyCombatStatus>();
        if (combatStatus == null)
        {
            combatStatus = gameObject.AddComponent<EnemyCombatStatus>();
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

        WeaponDefinition weapon = ResolveWeaponDefinition(source);
        DamageSourceContext damageContext = ResolveDamageContext(source);
        if (combatStatus != null && weapon != null && weapon.IdentityEffect == WeaponIdentityEffect.ConsumeMarkBonus)
        {
            if (combatStatus.ConsumeMark())
            {
                amount *= Mathf.Max(1f, weapon.MarkedDamageMultiplier);
            }
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (damageFlash != null)
        {
            damageFlash.PlayFlash();
        }

        if (weapon != null && RoomCameraController.Instance != null)
        {
            RoomCameraController.Instance.TriggerImpactFeedback(weapon.HitstopDuration, weapon.CameraShakeDistance, weapon.CameraShakeDuration);
        }

        if (damageContext != null && damageContext.IsCritical)
        {
            CombatFeedbackText.Spawn("CRIT!", transform.position + combatTextOffset, criticalTextColor, 1.15f);
        }

        if (combatStatus != null && weapon != null && weapon.IdentityEffect == WeaponIdentityEffect.ApplyMark && currentHealth > 0f)
        {
            combatStatus.ApplyMark(weapon.MarkDuration);
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

    private static WeaponDefinition ResolveWeaponDefinition(GameObject source)
    {
        DamageSourceContext context = ResolveDamageContext(source);
        return context != null ? context.Weapon : null;
    }

    private static DamageSourceContext ResolveDamageContext(GameObject source)
    {
        if (source == null)
        {
            return null;
        }

        return source.GetComponent<DamageSourceContext>();
    }
}
