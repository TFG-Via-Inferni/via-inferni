using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float hitRadius = 0.2f;
    [SerializeField] private float visualRotationOffset = -90f;

    private float damage;
    private float speed;
    private float lifetime;
    private Vector2 direction = Vector2.right;
    private float spawnTime;
    private Transform ownerRoot;

    public void Initialize(float damageAmount, float projectileSpeed, float projectileLifetime, Vector2 travelDirection, Transform owner)
    {
        damage = Mathf.Max(0f, damageAmount);
        speed = Mathf.Max(0f, projectileSpeed);
        lifetime = Mathf.Max(0.1f, projectileLifetime);
        direction = travelDirection.sqrMagnitude > 0.0001f ? travelDirection.normalized : Vector2.right;
        ownerRoot = owner != null ? owner.root : null;
        spawnTime = Time.time;
        UpdateVisualRotation();
    }

    private void Update()
    {
        UpdateVisualRotation();
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

            if (ownerRoot != null && hit.transform.root == ownerRoot)
            {
                continue;
            }

            if (!hit.CompareTag("Player"))
            {
                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.CanTakeDamage)
            {
                damageable.TakeDamage(damage, gameObject);
                Destroy(gameObject);
                return;
            }
        }
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
}
