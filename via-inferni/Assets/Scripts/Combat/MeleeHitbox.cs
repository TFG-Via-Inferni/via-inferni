using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MeleeHitbox : MonoBehaviour
{
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private float lifetime = 0.12f;

    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private float damage;
    private GameObject source;
    private bool appliesKnockback;
    private float knockbackForce;

    public void Initialize(float damageAmount, GameObject damageSource, LayerMask allowedLayers, bool applyKnockback, float knockbackAmount)
    {
        damage = damageAmount;
        source = damageSource;
        hittableLayers = allowedLayers;
        appliesKnockback = applyKnockback;
        knockbackForce = knockbackAmount;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ShouldIgnoreSelfHit(other))
        {
            return;
        }

        if (((1 << other.gameObject.layer) & hittableLayers) == 0)
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null && damageable.CanTakeDamage)
        {
            if (!hitTargets.Add(damageable))
            {
                return;
            }

            damageable.TakeDamage(damage, source);
            ApplyKnockback(other);
        }
    }

    private void ApplyKnockback(Collider2D other)
    {
        if (!appliesKnockback || knockbackForce <= 0f || other == null || source == null)
        {
            return;
        }

        Rigidbody2D body = other.attachedRigidbody != null ? other.attachedRigidbody : other.GetComponentInParent<Rigidbody2D>();
        if (body == null)
        {
            return;
        }

        Transform knockbackOrigin = source.transform;
        DamageSourceContext context = source.GetComponent<DamageSourceContext>();
        if (context != null && context.OwnerRoot != null)
        {
            knockbackOrigin = context.OwnerRoot;
        }

        Vector2 knockbackDirection = (body.worldCenterOfMass - (Vector2)knockbackOrigin.position);
        if (knockbackDirection.sqrMagnitude <= 0.0001f)
        {
            knockbackDirection = (Vector2)body.transform.position - (Vector2)knockbackOrigin.position;
        }

        if (knockbackDirection.sqrMagnitude <= 0.0001f)
        {
            knockbackDirection = Vector2.up;
        }

        EnemyController enemyController = body.GetComponent<EnemyController>();
        if (enemyController == null)
        {
            enemyController = body.GetComponentInParent<EnemyController>();
        }

        if (enemyController != null)
        {
            enemyController.ApplyKnockback(knockbackDirection, knockbackForce);
            return;
        }

        body.AddForce(knockbackDirection.normalized * knockbackForce, ForceMode2D.Impulse);
    }

    private bool ShouldIgnoreSelfHit(Collider2D other)
    {
        if (source == null)
        {
            return false;
        }

        if (other.transform.root == source.transform.root)
        {
            return true;
        }

        DamageSourceContext context = source.GetComponent<DamageSourceContext>();
        return context != null && context.OwnerRoot != null && other.transform.root == context.OwnerRoot;
    }
}
