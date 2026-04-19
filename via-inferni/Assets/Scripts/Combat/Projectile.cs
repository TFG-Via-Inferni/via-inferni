using UnityEngine;

[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.2f;
    [SerializeField] private bool destroyOnFirstHit = true;

    private float damage;
    private float speed;
    private float lifetime;
    private Vector2 direction = Vector2.right;
    private GameObject source;
    private float spawnTime;
    private bool homingEnabled;
    private float homingTurnSpeed;
    private float homingSearchRadius;

    public void Initialize(
        float damageAmount,
        float projectileSpeed,
        float projectileLifetime,
        Vector2 travelDirection,
        GameObject damageSource,
        bool enableHoming,
        float homingTurnSpeedAmount,
        float homingRadius)
    {
        damage = damageAmount;
        speed = projectileSpeed;
        lifetime = projectileLifetime;
        direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector2.right;
        source = damageSource;
        homingEnabled = enableHoming;
        homingTurnSpeed = homingTurnSpeedAmount;
        homingSearchRadius = homingRadius;
        spawnTime = Time.time;
    }

    private void Update()
    {
        UpdateHomingDirection();
        transform.position += (Vector3)(direction * (speed * Time.deltaTime));

        if (Time.time >= spawnTime + lifetime)
        {
            Destroy(gameObject);
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, hitRadius);
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

            if (ShouldIgnoreSelfHit(hit))
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

                continue;
            }

            // Ignore trigger volumes (room/camera/zone colliders) so projectiles only
            // break on solid world geometry when not hitting a damageable target.
            if (hit.isTrigger)
            {
                continue;
            }

            Destroy(gameObject);
            return;
        }
    }

    private void UpdateHomingDirection()
    {
        if (!homingEnabled || homingTurnSpeed <= 0f || homingSearchRadius <= 0f)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, homingSearchRadius);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        Collider2D closestTarget = null;
        float closestDistance = float.MaxValue;
        Vector2 currentPosition = transform.position;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || ShouldIgnoreSelfHit(hit))
            {
                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.CanTakeDamage)
            {
                continue;
            }

            float distance = ((Vector2)hit.bounds.center - currentPosition).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hit;
            }
        }

        if (closestTarget == null)
        {
            return;
        }

        Vector2 desiredDirection = ((Vector2)closestTarget.bounds.center - currentPosition).normalized;
        float blend = Mathf.Clamp01(homingTurnSpeed * Time.deltaTime);
        direction = Vector2.Lerp(direction, desiredDirection, blend).normalized;
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
