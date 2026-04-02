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

    private EnemyState currentState = EnemyState.Idle;
    private Transform player;
    private IDamageable playerDamageable;
    private Rigidbody2D rb;
    private Vector2 movement;
    private float lastAttackTime;
    private Room parentRoom;

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

        // Buscar al player por tag
        TryFindPlayer();
    }

    void Update()
    {
        if (PauseMenuController.IsPaused)
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
        if (PauseMenuController.IsPaused)
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
        // Si toca al Player, notificar a la sala y destruirse
        if (collision.CompareTag("Player"))
        {
            if (parentRoom != null)
            {
                parentRoom.OnEnemyDestroyed(this);
            }
            Destroy(gameObject);
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
