using UnityEngine;

[DisallowMultipleComponent]
public class MeleeSlashVisual : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.12f;
    [SerializeField] private float width = 0.08f;
    [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private int sortingOrder = 1000;

    private LineRenderer lineRenderer;

    public void Initialize(Vector2 origin, Vector2 direction, float length)
    {
        Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 perpendicular = new Vector2(-normalizedDirection.y, normalizedDirection.x);
        Vector2 start = origin + normalizedDirection * (length * 0.15f) - perpendicular * (length * 0.15f);
        Vector2 end = origin + normalizedDirection * length + perpendicular * (length * 0.15f);

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.sortingOrder = sortingOrder;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader != null)
        {
            lineRenderer.material = new Material(shader);
        }

        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        Destroy(gameObject, lifetime);
    }
}
