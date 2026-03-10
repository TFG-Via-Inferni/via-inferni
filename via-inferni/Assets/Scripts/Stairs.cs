using UnityEngine;

public class Stairs : MonoBehaviour
{
    private bool hasBeenUsed = false;
    private bool isActive = false;

    [Header("Activation Settings")]
    [Tooltip("Si es true, la escalera está activa desde el inicio. Si es false, necesita activarse (ej: matando al boss)")]
    [SerializeField] private bool activeFromStart = false;

    private SpriteRenderer spriteRenderer;
    private Collider2D triggerCollider;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<Collider2D>();
        
        SetActive(activeFromStart);
    }

    public void SetActive(bool active)
    {
        isActive = active;
        
        // Visualmente mostrar si está activa o no
        if (spriteRenderer != null)
        {
            var color = spriteRenderer.color;
            color.a = active ? 1f : 0.3f; // Transparente si está inactiva
            spriteRenderer.color = color;
        }

        if (triggerCollider != null)
        {
            triggerCollider.enabled = active;
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;
        
        
        if (other.CompareTag("Player") && !hasBeenUsed)
        {
            hasBeenUsed = true;
            
            if (CircleManager.instance != null)
            {
                CircleManager.instance.DescendToNextCircle();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            hasBeenUsed = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}
