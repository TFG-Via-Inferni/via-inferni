using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Estados del enemigo
    private enum EnemyState
    {
        Idle,
        Chase,
        Attack
    }

    [Header("Detection")]
    public float detectionRadius = 10f;
    public float attackRange = 1.5f;

    [Header("Movement")]
    public float speed = 2f;

    [Header("Attack")]
    public float attackCooldown = 1f;
    public float attackDamage = 10f;

    [Header("Runtime/Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool startDormant = true;

    [Header("Type Behavior")]
    [SerializeField] private float flyPreferredDistance = 2.0f;
    [SerializeField] private float flyOrbitStrength = 0.65f;
    [SerializeField] private float flyAttackRangeMultiplier = 3.0f;
    [SerializeField] private float flyProjectileSpeed = 8.5f;
    [SerializeField] private float flyProjectileLifetime = 2.2f;
    [SerializeField] private float flyProjectileSpawnOffset = 0.5f;
    [SerializeField] private GameObject flyProjectilePrefab;
    [SerializeField] private float tankSpeedMultiplier = 0.7f;
    [SerializeField] private float tankAttackWindup = 0.45f;
    [SerializeField] private float tankAttackDamageMultiplier = 2f;
    [SerializeField] private float flyShotWindup = 0.18f;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private float telegraphScaleMultiplier = 1.12f;
    [SerializeField] private float knockbackRecoverDuration = 0.16f;

    private EnemyState currentState = EnemyState.Idle;
    private Transform player;
    private IDamageable playerDamageable;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private Vector2 movement;
    private float lastAttackTime;
    private Room parentRoom;
    private bool aiEnabled;
    private bool isDead;
    private EnemyDefinition runtimeDefinition;
    private float flyOrbitSign = 1f;
    private bool tankIsWindingUp;
    private float tankWindupReadyAt;
    private bool flyIsWindingUp;
    private float flyWindupReadyAt;
    private Color spriteBaseColor = Color.white;
    private Vector3 visualBaseScale = Vector3.one;
    private Vector2 knockbackVelocity;
    private float knockbackUntil = -999f;

    private EnemyType CurrentEnemyType => runtimeDefinition != null ? runtimeDefinition.enemyType : EnemyType.Normal;

    private void TryFindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
        playerDamageable = playerObject != null ? playerObject.GetComponent<IDamageable>() : null;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        parentRoom = GetComponentInParent<Room>();
        health = GetComponent<EnemyHealth>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            spriteBaseColor = spriteRenderer.color;
            visualBaseScale = spriteRenderer.transform.localScale;
        }

        if (health != null)
        {
            health.Died += HandleDeath;
        }

        // Buscar al player por tag
        TryFindPlayer();
        SetDormant(startDormant);
        flyOrbitSign = Random.value < 0.5f ? -1f : 1f;
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Died -= HandleDeath;
        }
    }

    public void Configure(EnemyDefinition definition, Room room)
    {
        runtimeDefinition = definition;
        parentRoom = room != null ? room : parentRoom;

        if (definition == null)
        {
            if (health != null)
            {
                health.Configure(health.MaxHealth, true);
            }

            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null && definition.overrideSprite != null)
        {
            spriteRenderer.sprite = definition.overrideSprite;
        }

        speed = definition.moveSpeed;
        detectionRadius = definition.detectionRadius;
        attackRange = definition.attackRange;
        attackCooldown = definition.attackCooldown;
        attackDamage = definition.attackDamage;
        startDormant = definition.startDormant;

        // If this is a flying enemy, make its preferred orbit distance match its effective attack range
        if (CurrentEnemyType == EnemyType.Fly)
        {
            flyPreferredDistance = attackRange * Mathf.Max(1f, flyAttackRangeMultiplier);
        }

        if (rb != null)
        {
            rb.gravityScale = definition.ignoreGravity ? 0f : rb.gravityScale;
        }

        if (health != null)
        {
            health.Configure(definition.maxHealth, true);
        }
    }

    public void SetDormant(bool dormant)
    {
        aiEnabled = !dormant;

        if (dormant)
        {
            movement = Vector2.zero;

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            currentState = EnemyState.Idle;
        }
    }

    public void ActivateAI()
    {
        if (isDead)
        {
            return;
        }

        aiEnabled = true;
    }

    public void DeactivateAI()
    {
        aiEnabled = false;
        movement = Vector2.zero;
        tankIsWindingUp = false;
        flyIsWindingUp = false;
        ResetTelegraphVisual();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void Update()
    {
        if (PauseMenuController.IsPaused)
        {
            movement = Vector2.zero;
            return;
        }

        if (isDead)
        {
            movement = Vector2.zero;
            return;
        }

        if (!aiEnabled)
        {
            movement = Vector2.zero;
            return;
        }

        if (player == null)
        {
            TryFindPlayer();
            if (player == null)
            {
                movement = Vector2.zero;
                ChangeState(EnemyState.Idle);
                return;
            }
        }

        // If player has an active cloak, ignore them completely
        Player playerComp = player.GetComponent<Player>();
        if (playerComp != null && playerComp.IsCloaked)
        {
            movement = Vector2.zero;
            ChangeState(EnemyState.Idle);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Cambiar de estado según la distancia
        UpdateState(distanceToPlayer);

        // Ejecutar comportamiento del estado actual
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Chase:
                HandleChase();
                break;
            case EnemyState.Attack:
                HandleAttack();
                break;
        }

        UpdateTelegraphVisual();
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        if (PauseMenuController.IsPaused || isDead || !aiEnabled)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (Time.time < knockbackUntil)
        {
            rb.MovePosition(rb.position + (knockbackVelocity * Time.fixedDeltaTime));
            knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, Time.fixedDeltaTime / Mathf.Max(0.01f, knockbackRecoverDuration));
            return;
        }

        // Mover al enemigo
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (force <= 0f)
        {
            return;
        }

        Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector2.up;

        knockbackVelocity = normalizedDirection * force;
        knockbackUntil = Time.time + Mathf.Max(0.05f, knockbackRecoverDuration);
        tankIsWindingUp = false;
        flyIsWindingUp = false;
    }

    void UpdateState(float distanceToPlayer)
    {
        float effectiveAttackRange = attackRange;
        if (CurrentEnemyType == EnemyType.Fly)
        {
            effectiveAttackRange *= Mathf.Max(1f, flyAttackRangeMultiplier);
        }

        if (distanceToPlayer <= effectiveAttackRange)
        {
            ChangeState(EnemyState.Attack);
        }
        else if (distanceToPlayer <= detectionRadius)
        {
            ChangeState(EnemyState.Chase);
        }
        else
        {
            ChangeState(EnemyState.Idle);
        }
    }

    void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;
        
        currentState = newState;
    }

    void HandleIdle()
    {
        movement = Vector2.zero;
    }

    void HandleChase()
    {
        Vector2 toPlayer = (player.position - transform.position);
        float distance = toPlayer.magnitude;
        Vector2 direction = distance > 0.0001f ? toPlayer / distance : Vector2.zero;

        switch (CurrentEnemyType)
        {
            case EnemyType.Fly:
                Vector2 tangent = new Vector2(-direction.y, direction.x) * flyOrbitSign;
                Vector2 radial = distance > flyPreferredDistance
                    ? direction
                    : -direction * 0.35f;
                Vector2 flyVector = (radial + tangent * flyOrbitStrength).normalized;
                movement = flyVector * speed;
                break;

            case EnemyType.Tank:
                movement = direction * (speed * tankSpeedMultiplier);
                break;

            default:
                movement = direction * speed;
                break;
        }
    }

    void HandleAttack()
    {
        if (CurrentEnemyType == EnemyType.Fly)
        {
            HandleFlyAttack();
            return;
        }

        if (CurrentEnemyType == EnemyType.Tank)
        {
            HandleTankAttack();
            return;
        }

        movement = Vector2.zero;

        // Atacar si ha pasado el cooldown
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;

            if (CurrentEnemyType == EnemyType.Fly)
            {
                flyOrbitSign *= -1f;
            }
        }
    }

    void PerformAttack()
    {
        Debug.Log("¡Enemigo atacando!");

        if (playerDamageable != null && playerDamageable.CanTakeDamage)
        {
            playerDamageable.TakeDamage(attackDamage, gameObject);
        }
    }

    private void HandleTankAttack()
    {
        movement = Vector2.zero;

        if (!tankIsWindingUp)
        {
            if (Time.time < lastAttackTime + attackCooldown)
            {
                return;
            }

            tankIsWindingUp = true;
            tankWindupReadyAt = Time.time + Mathf.Max(0.05f, tankAttackWindup);
            return;
        }

        if (Time.time < tankWindupReadyAt)
        {
            return;
        }

        tankIsWindingUp = false;
        lastAttackTime = Time.time;

        if (player == null)
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer > attackRange * 1.25f)
        {
            return;
        }

        float heavyDamage = attackDamage * tankAttackDamageMultiplier;
        if (playerDamageable != null && playerDamageable.CanTakeDamage)
        {
            playerDamageable.TakeDamage(heavyDamage, gameObject);
        }
    }

    private void HandleFlyAttack()
    {
        if (player == null)
        {
            return;
        }

        Vector2 toPlayer = (player.position - transform.position);
        float distance = toPlayer.magnitude;
        Vector2 direction = distance > 0.0001f ? toPlayer / distance : Vector2.right;

        // Cuando está atacando, mantiene una distancia dentro del rango de ataque
        float effectiveAttackRange = attackRange * Mathf.Max(1f, flyAttackRangeMultiplier);
        float targetDistance = effectiveAttackRange * 0.8f; // Mantén el 80% del rango de ataque

        // Mantiene distancia y strafea mientras prepara disparo.
        Vector2 tangent = new Vector2(-direction.y, direction.x) * flyOrbitSign;
        Vector2 radial = distance > targetDistance
            ? direction
            : -direction * 0.35f;
        movement = (radial + tangent * flyOrbitStrength).normalized * speed;

        if (!flyIsWindingUp)
        {
            if (Time.time < lastAttackTime + attackCooldown)
            {
                return;
            }

            flyIsWindingUp = true;
            flyWindupReadyAt = Time.time + Mathf.Max(0.05f, flyShotWindup);
            return;
        }

        if (Time.time < flyWindupReadyAt)
        {
            return;
        }

        flyIsWindingUp = false;
        ShootAtPlayer(direction);
        lastAttackTime = Time.time;
        flyOrbitSign *= -1f;
    }

    private void ShootAtPlayer(Vector2 directionToPlayer)
    {
        Vector3 spawnPos = transform.position + (Vector3)(directionToPlayer * Mathf.Max(0.05f, flyProjectileSpawnOffset));
        GameObject projectileObject = null;

        if (flyProjectilePrefab != null)
        {
            projectileObject = Instantiate(flyProjectilePrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            projectileObject = new GameObject("EnemyProjectile");
            projectileObject.transform.position = spawnPos;

            CircleCollider2D collider = projectileObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.14f;
        }

        EnemyProjectile projectile = projectileObject.GetComponent<EnemyProjectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<EnemyProjectile>();
        }

        projectile.Initialize(
            attackDamage,
            flyProjectileSpeed,
            flyProjectileLifetime,
            directionToPlayer,
            transform
        );
        Debug.Log($"[EnemyController] ShootAtPlayer spawned projectile towards {directionToPlayer} dmg={attackDamage} speed={flyProjectileSpeed}");
    }

    public float ModifyIncomingDamage(float baseDamage, GameObject source)
    {
        if (baseDamage <= 0f)
        {
            return 0f;
        }

        if (!TryResolveWeaponContext(source, out string weaponId, out PlayerFormType form))
        {
            return baseDamage;
        }

        float multiplier = GetWeaponMultiplier(CurrentEnemyType, weaponId, form);
        return baseDamage * Mathf.Max(0f, multiplier);
    }

    private static bool TryResolveWeaponContext(GameObject source, out string weaponId, out PlayerFormType form)
    {
        weaponId = string.Empty;
        form = PlayerFormType.Melee;

        if (source == null)
        {
            return false;
        }

        DamageSourceContext context = source.GetComponent<DamageSourceContext>();
        if (context != null && !string.IsNullOrWhiteSpace(context.WeaponId))
        {
            weaponId = context.WeaponId;
            form = context.Form;
            return true;
        }

        Transform root = source.transform.root;
        Player player = root != null ? root.GetComponent<Player>() : null;
        PlayerStats stats = root != null ? root.GetComponent<PlayerStats>() : null;

        if (player == null || stats == null)
        {
            return false;
        }

        form = player.CurrentForm;
        weaponId = stats.GetSelectedWeaponId(form);
        return !string.IsNullOrWhiteSpace(weaponId);
    }

    private static float GetWeaponMultiplier(EnemyType enemyType, string weaponId, PlayerFormType form)
    {
        string key = string.IsNullOrWhiteSpace(weaponId)
            ? string.Empty
            : weaponId.Trim().ToLowerInvariant();

        switch (enemyType)
        {
            case EnemyType.Fly:
                return key switch
                {
                    "bow" => 1.25f,
                    "magic" => 1.15f,
                    "spear" => 1.2f,
                    "axe" => 0.8f,
                    "ballista" => 0.9f,
                    _ => form == PlayerFormType.Ranged ? 1.1f : 1f
                };

            case EnemyType.Tank:
                return key switch
                {
                    "axe" => 1.3f,
                    "ballista" => 1.35f,
                    "magic" => 1.1f,
                    "bow" => 0.8f,
                    "spear" => 0.9f,
                    _ => 1f
                };

            default:
                return key switch
                {
                    "sword" => 1.1f,
                    "bow" => 1.05f,
                    _ => 1f
                };
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // La colision directa con el Player ya no destruye al enemigo.
        // El daño se resuelve por ataque y por vida.
    }

    private void HandleDeath(EnemyHealth source, GameObject damageSource)
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        movement = Vector2.zero;
        DeactivateAI();

        if (CircleManager.instance != null)
        {
            CircleManager.instance.TrySpawnGlobalEnemyDrop(transform.position, runtimeDefinition != null ? runtimeDefinition.displayName : gameObject.name);
        }

        if (parentRoom != null)
        {
            parentRoom.OnEnemyDestroyed(this);
        }
    }

    private void UpdateTelegraphVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        bool telegraphActive = tankIsWindingUp || flyIsWindingUp;
        if (!telegraphActive)
        {
            ResetTelegraphVisual();
            return;
        }

        float pulse = 0.55f + (0.45f * Mathf.Abs(Mathf.Sin(Time.time * 28f)));
        spriteRenderer.color = Color.Lerp(spriteBaseColor, telegraphColor, pulse);

        if (spriteRenderer.transform != null)
        {
            float scale = Mathf.Lerp(1f, telegraphScaleMultiplier, pulse);
            spriteRenderer.transform.localScale = visualBaseScale * scale;
        }
    }

    private void ResetTelegraphVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = spriteBaseColor;
        if (spriteRenderer.transform != null)
        {
            spriteRenderer.transform.localScale = visualBaseScale;
        }
    }

    void OnDrawGizmosSelected()
    {
        // Radio de detección (amarillo)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Rango de ataque (rojo)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
