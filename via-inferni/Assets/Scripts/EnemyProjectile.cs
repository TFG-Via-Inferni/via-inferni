using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.2f;
    [SerializeField] private float visualRotationOffset = -90f;
    [SerializeField] private Color impactColor = new Color(1f, 0.56f, 0.56f, 0.95f);

    private float damage;
    private float speed;
    private float lifetime;
    private Vector2 direction = Vector2.right;
    private float spawnTime;
    private Transform ownerRoot;

    private void Awake()
    {
        EnsureNonPhysicalProjectile();
    }

    public void Initialize(float damageAmount, float projectileSpeed, float projectileLifetime, Vector2 travelDirection, Transform owner)
    {
        damage = Mathf.Max(0f, damageAmount);
        speed = Mathf.Max(0f, projectileSpeed);
        lifetime = Mathf.Max(0.1f, projectileLifetime);
        direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector2.right;
        ownerRoot = owner != null ? owner.root : null;
        spawnTime = Time.time;
        EnsureNonPhysicalProjectile();
        UpdateVisualRotation();
    }

    private void Update()
    {
        UpdateVisualRotation();
        Vector2 previousPosition = transform.position;
        Vector2 displacement = direction * (speed * Time.deltaTime);
        float travelDistance = displacement.magnitude;

        if (travelDistance > 0.0001f)
        {
            RaycastHit2D[] pathHits = Physics2D.CircleCastAll(previousPosition, hitRadius, direction, travelDistance);
            if (TryResolvePathHits(pathHits))
            {
                return;
            }
        }

        transform.position = previousPosition + displacement;

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

            if (ownerRoot != null && hit.transform.root == ownerRoot)
            {
                continue;
            }

            if (!hit.CompareTag("Player"))
            {
                if (!hit.isTrigger)
                {
                    ProjectileImpactVisual.Spawn(hit.ClosestPoint(transform.position), impactColor, direction, 0.8f);
                    Destroy(gameObject);
                    return;
                }

                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.CanTakeDamage)
            {
                damageable.TakeDamage(damage, gameObject);
                ProjectileImpactVisual.Spawn(hit.ClosestPoint(transform.position), impactColor, direction, 0.95f);
                Destroy(gameObject);
                return;
            }
        }
    }

    private bool TryResolvePathHits(RaycastHit2D[] pathHits)
    {
        if (pathHits == null || pathHits.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < pathHits.Length; i++)
        {
            Collider2D hit = pathHits[i].collider;
            if (hit == null)
            {
                continue;
            }

            if (ownerRoot != null && hit.transform.root == ownerRoot)
            {
                continue;
            }

            if (!hit.CompareTag("Player"))
            {
                if (!hit.isTrigger)
                {
                    ProjectileImpactVisual.Spawn(pathHits[i].point, impactColor, direction, 0.8f);
                    Destroy(gameObject);
                    return true;
                }

                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.CanTakeDamage)
            {
                damageable.TakeDamage(damage, gameObject);
                ProjectileImpactVisual.Spawn(pathHits[i].point, impactColor, direction, 0.95f);
                Destroy(gameObject);
                return true;
            }
        }

        return false;
    }

    private void UpdateVisualRotation()
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + visualRotationOffset);
    }

    private void EnsureNonPhysicalProjectile()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            collider.isTrigger = true;
            collider.enabled = false;
        }

        Rigidbody2D[] rigidbodies = GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody2D body = rigidbodies[i];
            if (body == null)
            {
                continue;
            }

            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = false;
        }
    }
}
