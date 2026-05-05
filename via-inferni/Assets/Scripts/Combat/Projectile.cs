using UnityEngine;

[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    private const float HomingTurnDegreesPerSpeedUnit = 45f;
    private const float HomingMinTurnFactor = 0.3f;
    private const float HomingRetargetInterval = 0.18f;
    private const float HomingAimPointVariance = 0.65f;

    [SerializeField] private float hitRadius = 0.2f;
    [SerializeField] private bool destroyOnFirstHit = true;
    [SerializeField] private float visualRotationOffset = -90f;

    private float damage;
    private float speed;
    private float lifetime;
    private Vector2 direction = Vector2.right;
    private GameObject source;
    private float spawnTime;
    private bool homingEnabled;
    private float homingTurnSpeed;
    private float homingSearchRadius;
    private Collider2D homingTarget;
    private Vector2 homingAimPoint;
    private float nextHomingRetargetTime;
    private ProjectileParticleTrailVisual trailVisual;
    private SpriteRenderer projectileSpriteRenderer;

    private void Awake()
    {
        projectileSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        trailVisual = GetComponent<ProjectileParticleTrailVisual>();
        if (trailVisual == null)
        {
            trailVisual = gameObject.AddComponent<ProjectileParticleTrailVisual>();
        }
    }

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

        if (trailVisual == null)
        {
            trailVisual = GetComponent<ProjectileParticleTrailVisual>();
            if (trailVisual == null)
            {
                trailVisual = gameObject.AddComponent<ProjectileParticleTrailVisual>();
            }
        }

        if (projectileSpriteRenderer == null)
        {
            projectileSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        DamageSourceContext context = GetComponent<DamageSourceContext>();
        WeaponDefinition weapon = context != null ? context.Weapon : null;
        trailVisual.Configure(weapon, projectileSpriteRenderer);
        UpdateVisualRotation();
    }

    private void Update()
    {
        UpdateHomingDirection();
        UpdateVisualRotation();
        transform.position += (Vector3)(direction * (speed * Time.deltaTime));
        trailVisual?.Tick(direction);

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

        Vector2 currentPosition = transform.position;
        if (ShouldRefreshHomingTarget(currentPosition))
        {
            RefreshHomingTarget(currentPosition);
        }

        if (homingTarget == null)
        {
            return;
        }

        Vector2 toAimPoint = homingAimPoint - currentPosition;
        if (toAimPoint.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector2 desiredDirection = toAimPoint.normalized;
        float distanceFactor = Mathf.Clamp01(toAimPoint.magnitude / homingSearchRadius);
        distanceFactor = Mathf.Lerp(HomingMinTurnFactor, 1f, distanceFactor);

        float currentAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float desiredAngle = Mathf.Atan2(desiredDirection.y, desiredDirection.x) * Mathf.Rad2Deg;
        float maxTurnDegrees = homingTurnSpeed * HomingTurnDegreesPerSpeedUnit * distanceFactor * Time.deltaTime;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, desiredAngle, maxTurnDegrees);
        float radians = newAngle * Mathf.Deg2Rad;
        direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)).normalized;
    }

    private bool ShouldRefreshHomingTarget(Vector2 currentPosition)
    {
        if (homingTarget == null || !homingTarget.gameObject.activeInHierarchy)
        {
            return true;
        }

        if (Time.time >= nextHomingRetargetTime)
        {
            return true;
        }

        float maxRangeSqr = homingSearchRadius * homingSearchRadius;
        return ((Vector2)homingTarget.bounds.center - currentPosition).sqrMagnitude > maxRangeSqr;
    }

    private void RefreshHomingTarget(Vector2 currentPosition)
    {
        homingTarget = FindClosestHomingTarget(currentPosition);
        nextHomingRetargetTime = Time.time + HomingRetargetInterval;

        if (homingTarget == null)
        {
            return;
        }

        Bounds bounds = homingTarget.bounds;
        Vector2 center = bounds.center;
        Vector2 extents = bounds.extents * HomingAimPointVariance;
        Vector2 aimOffset = new Vector2(
            Random.Range(-extents.x, extents.x),
            Random.Range(-extents.y, extents.y)
        );
        homingAimPoint = center + aimOffset;
    }

    private Collider2D FindClosestHomingTarget(Vector2 currentPosition)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(currentPosition, homingSearchRadius);
        if (hits == null || hits.Length == 0)
        {
            return null;
        }

        Collider2D closestTarget = null;
        float closestDistance = float.MaxValue;

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

        return closestTarget;
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
