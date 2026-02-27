using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Rigidbody2D rb;
    private Vector2 movement;

    void Start()
    {
        // Obtener o crear el Rigidbody2D
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; // Sin gravedad
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Sin rotación
        }
    }

    void Update()
    {
        // Capturar entrada del teclado
        movement.x = Input.GetAxis("Horizontal"); // A/D o Left/Right
        movement.y = Input.GetAxis("Vertical");   // W/S o Up/Down
    }

    void FixedUpdate()
    {
        // Aplicar movimiento
        rb.velocity = movement * moveSpeed;
    }
}
