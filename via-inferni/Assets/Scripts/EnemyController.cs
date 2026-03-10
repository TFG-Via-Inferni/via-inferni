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

    private EnemyState currentState = EnemyState.Idle;
    private Transform player;
    private Rigidbody2D rb;
    private Vector2 movement;
    private float lastAttackTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Buscar al player por tag
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    void Update()
    {

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
        Debug.Log($"Enemy cambió a estado: {currentState}");
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
        // Aquí irá la lógica de daño cuando tengas sistema de vida
        // Por ejemplo: player.GetComponent<PlayerHealth>()?.TakeDamage(10);
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
