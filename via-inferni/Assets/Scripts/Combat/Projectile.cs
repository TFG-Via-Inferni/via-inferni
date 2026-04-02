using UnityEngine;

[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private float hitRadius = 0.2f;
    [SerializeField] private bool destroyOnFirstHit = true;

    private float damage;
    private float speed;
    private float lifetime;
    private Vector2 direction = Vector2.right;
    private GameObject source;
    private float spawnTime;

    public void Initialize(float damageAmount, float projectileSpeed, float projectileLifetime, Vector2 travelDirection, GameObject damageSource)
    {
        damage = damageAmount;
        speed = projectileSpeed;
        lifetime = projectileLifetime;
        direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector2.right;
        source = damageSource;
        spawnTime = Time.time;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * (speed * Time.deltaTime));

        if (Time.time >= spawnTime + lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius, hittableLayers);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            if (source != null && hit.transform.root == source.transform.root)
            {
                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.CanTakeDamage)
            {
                damageable.TakeDamage(damage, source);
                if (destroyOnFirstHit)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
