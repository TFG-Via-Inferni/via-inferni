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
        if (source != null && other.transform.root == source.transform.root)
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
}
