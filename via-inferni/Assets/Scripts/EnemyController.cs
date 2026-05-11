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
    [SerializeField] private float tankAttackLungeForce = 6.5f;
    [SerializeField] private float tankAttackLungeDuration = 0.12f;
    [SerializeField] private float normalAttackRangeMultiplier = 0.72f;
    [SerializeField] private float normalAttackEngageRangeMultiplier = 1.45f;
    [SerializeField] private float normalAttackWindup = 0.1f;
    [SerializeField] private float normalWindupChaseSpeedMultiplier = 0.48f;
    [SerializeField] private float normalAttackCommitRangeMultiplier = 1.35f;
    [SerializeField] private float normalAttackLungeForce = 5.1f;
    [SerializeField] private float normalAttackLungeDuration = 0.1f;
    [SerializeField] private float flyShotWindup = 0.18f;
    [SerializeField] private float flyAttackCooldownMultiplier = 1.45f;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private float telegraphScaleMultiplier = 1.12f;
    [SerializeField] private float knockbackRecoverDuration = 0.16f;
    [SerializeField] private Color dashTrailColor = new Color(1f, 0.92f, 0.92f, 1f);
    [SerializeField] private float normalDashTrailAlphaMultiplier = 1.65f;
    [SerializeField] private float normalDashTrailSpawnIntervalMultiplier = 0.68f;
    [SerializeField] private float normalDashTrailLifetimeMultiplier = 1.35f;
    [SerializeField] private float normalDashTrailScaleMultiplier = 1.05f;

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
    private bool normalIsWindingUp;
    private float normalWindupReadyAt;
    private Vector2 normalCommittedDirection = Vector2.right;
    private bool tankIsWindingUp;
    private float tankWindupReadyAt;
    private bool flyIsWindingUp;
    private float flyWindupReadyAt;
    private Color spriteBaseColor = Color.white;
    private Vector3 visualBaseScale = Vector3.one;
    private EnemyDashTrailVisual dashTrailVisual;
    private EnemySpriteAnimator spriteAnimator;
    private Vector2 knockbackVelocity;
    private float knockbackUntil = -999f;
    private Vector2 attackLungeVelocity;
    private float attackLungeUntil = -999f;
    private float currentAttackLungeDuration = 0.1f;
    private Vector2 facingDirection = Vector2.right;

    private EnemyType CurrentEnemyType => runtimeDefinition != null ? runtimeDefinition.enemyType : EnemyType.Normal;
    public Vector2 VisualVelocity => Time.time < attackLungeUntil
        ? attackLungeVelocity
        : Time.time < knockbackUntil
            ? knockbackVelocity
            : movement;
    public Vector2 FacingDirection => facingDirection;
    public bool IsInAttackVisualState => currentState == EnemyState.Attack
        || normalIsWindingUp
        || tankIsWindingUp
        || flyIsWindingUp
        || Time.time < attackLungeUntil;

    private void TryFindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        player = playerObject != null ? playerObject.transform : null;
        playerDamageable = playerObject != null ? playerObject.GetComponent<IDamageable>() : null;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        parentRoom = GetComponentInParent<Room>();
        health = GetComponent<EnemyHealth>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        EnsureVisualComponents();

        if (spriteRenderer != null)
        {
            spriteBaseColor = spriteRenderer.color;
            visualBaseScale = spriteRenderer.transform.localScale;
        }

        if (health != null)
        {
            health.Died += HandleDeath;
        }
    }

    void Start()
    {
        EnsureVisualComponents();

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

        EnsureVisualComponents();

        if (spriteAnimator != null)
        {
            spriteAnimator.Bind(this, spriteRenderer);
            spriteAnimator.ApplyDefinition(definition);

            if (!spriteAnimator.HasConfiguredAnimation() && spriteRenderer != null && definition.overrideSprite != null)
            {
                spriteRenderer.sprite = definition.overrideSprite;
            }
        }

        if (dashTrailVisual != null)
        {
            dashTrailVisual.Configure(spriteRenderer);
        }

        // Aplicar escalado de dificultad por círculo
        float difficultyMultiplier = GetCircleDifficultyMultiplier();

        speed = definition.moveSpeed * difficultyMultiplier;
        detectionRadius = definition.detectionRadius;
        attackRange = definition.attackRange;
        attackCooldown = definition.attackCooldown / difficultyMultiplier; // Ataque más frecuente
        attackDamage = definition.attackDamage * difficultyMultiplier;
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
            float scaledHealth = definition.maxHealth * difficultyMultiplier;
            health.Configure(scaledHealth, true);
        }
    }

    private float GetCircleDifficultyMultiplier()
    {
        if (CircleManager.instance == null)
        {
            return 1f; // Sin multiplicador si no hay CircleManager
        }

        int currentCircle = CircleManager.instance.CurrentCircle;
        return currentCircle switch
        {
            1 => 1.0f,
            2 => 1.15f,
            3 => 1.30f,
            4 => 1.45f,
            5 => 1.60f,
            6 => 1.75f,
            7 => 1.90f,
            8 => 2.05f,
            9 => 2.20f,
            _ => 1.0f
        };
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
        normalIsWindingUp = false;
        normalCommittedDirection = Vector2.right;
        tankIsWindingUp = false;
        flyIsWindingUp = false;
        attackLungeVelocity = Vector2.zero;
        attackLungeUntil = -999f;
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

        if (Time.time < attackLungeUntil)
        {
            rb.MovePosition(rb.position + (attackLungeVelocity * Time.fixedDeltaTime));
            attackLungeVelocity = Vector2.Lerp(attackLungeVelocity, Vector2.zero, Time.fixedDeltaTime / Mathf.Max(0.01f, currentAttackLungeDuration));
            return;
        }

        // Mover al enemigo
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);

        // Aplicar repulsión suave para evitar que se agrupen demasiado
        ApplyMinimalEnemyRepulsion();
    }

    private void ApplyMinimalEnemyRepulsion()
    {
        if (rb == null)
        {
            return;
        }
        // Ajuste reducido: separación equilibrada
        const float repulsionRadius = 3.0f;        // Detectar algo más lejos
        const float repulsionStrength = 1.8f;      // Fuerza de separación moderada
        const float minDistanceThreshold = 1.6f;   // Activar separación a distancia moderada

        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(rb.position, repulsionRadius);
        Vector2 repulsionForce = Vector2.zero;
        int enemyCount = 0;

        foreach (Collider2D collider in nearbyColliders)
        {
            if (collider.gameObject == gameObject)
            {
                continue;
            }

            EnemyController otherEnemy = collider.GetComponent<EnemyController>();
            if (otherEnemy == null)
            {
                continue;
            }

            Vector2 toOther = rb.position - otherEnemy.rb.position;
            float distance = toOther.magnitude;

            if (distance < 0.001f)
            {
                // Si están exactamente en el mismo punto, empujar en una dirección aleatoria pequeña
                repulsionForce += UnityEngine.Random.insideUnitCircle.normalized * repulsionStrength;
                enemyCount++;
                continue;
            }

            Vector2 directionAway = toOther / distance;

            // Aplicar repulsión proporcional a cuán cercanos están (más cerca => más fuerza)
            if (distance < minDistanceThreshold)
            {
                float falloff = 1f - (distance / minDistanceThreshold); // 0..1
                repulsionForce += directionAway * (repulsionStrength * falloff);
                enemyCount++;
            }
        }

        if (enemyCount > 0 && repulsionForce.sqrMagnitude > 0.0001f)
        {
            // Si la fuerza es grande, aplicar un pequeño ajuste posicional directo para despegar instantáneamente
            float forceMag = repulsionForce.magnitude;
            Vector2 dir = repulsionForce.normalized;

            if (forceMag > 0.6f)
            {
                // Mover un paso aún más pequeño en la dirección opuesta a la multitud
                Vector2 step = dir * 0.08f; // ajuste posicional muy pequeño
                rb.MovePosition(rb.position + step);
            }

            // Ajustar el movimiento para que la IA persiga pero mantenga separación (menos agresivo)
            movement += repulsionForce * Time.fixedDeltaTime * 1.0f;
        }
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
        normalIsWindingUp = false;
        normalCommittedDirection = Vector2.right;
        tankIsWindingUp = false;
        flyIsWindingUp = false;
        attackLungeVelocity = Vector2.zero;
        attackLungeUntil = -999f;
    }

    private float GetCurrentAttackCooldown()
    {
        if (CurrentEnemyType == EnemyType.Fly)
        {
            return attackCooldown * Mathf.Max(1f, flyAttackCooldownMultiplier);
        }

        return attackCooldown;
    }

    private void StartAttackLunge(Vector2 direction, float force, float duration)
    {
        if (force <= 0f || duration <= 0f)
        {
            return;
        }

        attackLungeVelocity = direction.normalized * force;
        currentAttackLungeDuration = duration;
        attackLungeUntil = Time.time + duration;

        if (dashTrailVisual != null)
        {
            if (CurrentEnemyType == EnemyType.Normal)
            {
                dashTrailVisual.Play(
                    duration,
                    dashTrailColor,
                    normalDashTrailAlphaMultiplier,
                    normalDashTrailSpawnIntervalMultiplier,
                    normalDashTrailLifetimeMultiplier,
                    normalDashTrailScaleMultiplier);
            }
            else
            {
                dashTrailVisual.Play(duration, dashTrailColor, 1f, 1f, 1f, 1f);
            }
        }

    }

    void UpdateState(float distanceToPlayer)
    {
        float effectiveAttackRange = attackRange;
        if (CurrentEnemyType == EnemyType.Fly)
        {
            effectiveAttackRange *= Mathf.Max(1f, flyAttackRangeMultiplier);
        }
        else if (CurrentEnemyType == EnemyType.Normal)
        {
            effectiveAttackRange *= Mathf.Clamp(normalAttackRangeMultiplier, 0.1f, 1f);
            effectiveAttackRange *= Mathf.Max(1f, normalAttackEngageRangeMultiplier);
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

        if (newState != EnemyState.Attack)
        {
            normalIsWindingUp = false;
            normalCommittedDirection = Vector2.right;
            tankIsWindingUp = false;
            flyIsWindingUp = false;
            ResetTelegraphVisual();
        }

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

        UpdateFacingDirection(direction);
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

        HandleNormalAttack();
    }

    private void HandleNormalAttack()
    {
        if (player == null)
        {
            return;
        }

        Vector2 directionToPlayer = ((Vector2)player.position - rb.position);
        float distanceToPlayer = directionToPlayer.magnitude;
        if (directionToPlayer.sqrMagnitude <= 0.0001f)
        {
            directionToPlayer = Vector2.right;
            distanceToPlayer = 0f;
        }

        float effectiveAttackRange = attackRange * Mathf.Clamp(normalAttackRangeMultiplier, 0.1f, 1f);
        float engageRange = effectiveAttackRange * Mathf.Max(1f, normalAttackEngageRangeMultiplier);

        if (!normalIsWindingUp)
        {
            movement = Vector2.zero;

            if (Time.time < lastAttackTime + GetCurrentAttackCooldown())
            {
                return;
            }

            normalIsWindingUp = true;
            normalCommittedDirection = directionToPlayer.normalized;
            UpdateFacingDirection(normalCommittedDirection);
            normalWindupReadyAt = Time.time + Mathf.Max(0.04f, normalAttackWindup);
            return;
        }

        if (directionToPlayer.sqrMagnitude > 0.0001f)
        {
            normalCommittedDirection = Vector2.Lerp(
                normalCommittedDirection,
                directionToPlayer.normalized,
                0.35f).normalized;
        }

        movement = normalCommittedDirection * (speed * normalWindupChaseSpeedMultiplier);
        UpdateFacingDirection(normalCommittedDirection);

        if (Time.time < normalWindupReadyAt)
        {
            return;
        }

        normalIsWindingUp = false;

        if (distanceToPlayer > engageRange * Mathf.Max(1.05f, normalAttackCommitRangeMultiplier))
        {
            return;
        }

        StartAttackLunge(normalCommittedDirection, normalAttackLungeForce, normalAttackLungeDuration);
        if (playerDamageable != null && playerDamageable.CanTakeDamage)
        {
            playerDamageable.TakeDamage(attackDamage, gameObject);
        }

        lastAttackTime = Time.time;
    }

    private void HandleTankAttack()
    {
        movement = Vector2.zero;

        if (!tankIsWindingUp)
        {
            if (Time.time < lastAttackTime + GetCurrentAttackCooldown())
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
        Vector2 directionToPlayer = ((Vector2)player.position - rb.position);
        if (directionToPlayer.sqrMagnitude <= 0.0001f)
        {
            directionToPlayer = Vector2.right;
        }

        UpdateFacingDirection(directionToPlayer.normalized);
        StartAttackLunge(directionToPlayer.normalized, tankAttackLungeForce, tankAttackLungeDuration);
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
        UpdateFacingDirection(direction);

        if (!flyIsWindingUp)
        {
            if (Time.time < lastAttackTime + GetCurrentAttackCooldown())
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
        UpdateFacingDirection(directionToPlayer);
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

    private void UpdateFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        facingDirection = direction.normalized;
    }

    private void EnsureVisualComponents()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteAnimator == null)
        {
            spriteAnimator = GetComponent<EnemySpriteAnimator>();
            if (spriteAnimator == null)
            {
                spriteAnimator = gameObject.AddComponent<EnemySpriteAnimator>();
            }
        }

        spriteAnimator.Bind(this, spriteRenderer);

        if (dashTrailVisual == null)
        {
            dashTrailVisual = GetComponent<EnemyDashTrailVisual>();
            if (dashTrailVisual == null)
            {
                dashTrailVisual = gameObject.AddComponent<EnemyDashTrailVisual>();
            }
        }

        dashTrailVisual.Configure(spriteRenderer);

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
