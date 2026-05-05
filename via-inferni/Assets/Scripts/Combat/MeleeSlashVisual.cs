using UnityEngine;

[DisallowMultipleComponent]
public class MeleeSlashVisual : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.12f;
    [SerializeField] private float width = 0.08f;
    [SerializeField] private float crossSize = 0.22f;
    [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private int sortingOrder = 1000;

    private LineRenderer primaryLineRenderer;
    private LineRenderer secondaryLineRenderer;

    public void Initialize(Vector2 start, Vector2 end)
    {
        float halfSize = Mathf.Max(0.02f, crossSize) * 0.5f;
        Vector2 diagonalA = new Vector2(halfSize, halfSize);
        Vector2 diagonalB = new Vector2(halfSize, -halfSize);

        primaryLineRenderer = CreateLineRenderer("SlashCrossA");
        secondaryLineRenderer = CreateLineRenderer("SlashCrossB");

        primaryLineRenderer.SetPosition(0, end - diagonalA);
        primaryLineRenderer.SetPosition(1, end + diagonalA);
        secondaryLineRenderer.SetPosition(0, end - diagonalB);
        secondaryLineRenderer.SetPosition(1, end + diagonalB);

        Destroy(gameObject, lifetime);
    }

    private LineRenderer CreateLineRenderer(string childName)
    {
        GameObject lineObject = new GameObject(childName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
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

        return lineRenderer;
    }
}
