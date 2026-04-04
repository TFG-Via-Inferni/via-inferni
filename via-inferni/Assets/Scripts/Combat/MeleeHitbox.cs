using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MeleeHitbox : MonoBehaviour
{
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private float lifetime = 0.12f;

    private readonly HashSet<Collider2D> hitColliders = new HashSet<Collider2D>();
    private float damage;
    private GameObject source;

    public void Initialize(float damageAmount, GameObject damageSource, LayerMask allowedLayers)
    {
        damage = damageAmount;
        source = damageSource;
        hittableLayers = allowedLayers;
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

        if (!hitColliders.Add(other))
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null && damageable.CanTakeDamage)
        {
            damageable.TakeDamage(damage, source);
        }
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
