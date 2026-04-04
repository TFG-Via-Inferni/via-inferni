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

        if (health != null)
        {
            health.Died += HandleDeath;
        }

        // Buscar al player por tag
        TryFindPlayer();
        SetDormant(startDormant);
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

        // Mover al enemigo
        rb.MovePosition(rb.position + movement * Time.fixedDeltaTime);
    }

    void UpdateState(float distanceToPlayer)
    {
        if (distanceToPlayer <= attackRange)
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
        Vector2 direction = (player.position - transform.position).normalized;
        movement = direction * speed;
    }

    void HandleAttack()
    {
        movement = Vector2.zero;

        // Atacar si ha pasado el cooldown
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
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

        if (parentRoom != null)
        {
            parentRoom.OnEnemyDestroyed(this);
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
