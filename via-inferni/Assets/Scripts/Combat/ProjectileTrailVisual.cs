using UnityEngine;

[DisallowMultipleComponent]
public class ProjectileTrailVisual : MonoBehaviour
{
    [SerializeField] private int pointCount = 7;
    [SerializeField] private float defaultTrailLength = 0.55f;
    [SerializeField] private float trailWidth = 0.12f;
    [SerializeField] private int sortingOrderOffset = -1;

    private LineRenderer lineRenderer;
    private Vector3[] points;
    private float configuredTrailLength;
    private Color configuredColor = Color.white;

    private void Awake()
    {
        EnsureLineRenderer();
    }

    private void LateUpdate()
    {
        if (lineRenderer == null)
        {
            return;
        }

        Vector3 head = transform.position;
        if (points == null || points.Length != pointCount)
        {
            points = new Vector3[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                points[i] = head;
            }
        }

        for (int i = points.Length - 1; i > 0; i--)
        {
            float maxStep = Mathf.Max(0.01f, configuredTrailLength / Mathf.Max(1, pointCount - 1));
            points[i] = Vector3.MoveTowards(points[i], points[i - 1], maxStep);
        }

        points[0] = head;

        for (int i = 0; i < points.Length; i++)
        {
            lineRenderer.SetPosition(i, points[i]);
        }
    }

    public void Configure(WeaponDefinition weapon, SpriteRenderer projectileSpriteRenderer)
    {
        EnsureLineRenderer();

        float lengthMultiplier = 1f;
        float widthMultiplier = 1f;
        Color color = projectileSpriteRenderer != null ? projectileSpriteRenderer.color : configuredColor;

        if (weapon != null)
        {
            string key = string.IsNullOrWhiteSpace(weapon.WeaponId)
                ? weapon.DisplayName
                : weapon.WeaponId;
            key = string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();

            switch (key)
            {
                case "bow":
                    lengthMultiplier = 0.9f;
                    widthMultiplier = 0.8f;
                    color = new Color(1f, 0.95f, 0.82f, 0.85f);
                    break;

                case "magic":
                    lengthMultiplier = 1.15f;
                    widthMultiplier = 1f;
                    color = new Color(0.72f, 1f, 0.95f, 0.8f);
                    break;

                case "ballista":
                    lengthMultiplier = 1.35f;
                    widthMultiplier = 1.25f;
                    color = new Color(1f, 0.88f, 0.7f, 0.92f);
                    break;
            }
        }

        configuredTrailLength = defaultTrailLength * lengthMultiplier;
        configuredColor = color;
        lineRenderer.startWidth = trailWidth * widthMultiplier;
        lineRenderer.endWidth = 0f;
        lineRenderer.startColor = configuredColor;
        lineRenderer.endColor = new Color(configuredColor.r, configuredColor.g, configuredColor.b, 0f);
        lineRenderer.sortingOrder = projectileSpriteRenderer != null
            ? projectileSpriteRenderer.sortingOrder + sortingOrderOffset
            : sortingOrderOffset;

        if (points != null)
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = transform.position;
            }
        }
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer != null)
        {
            return;
        }

        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = pointCount;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 3;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.sortingOrder = sortingOrderOffset;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader != null)
        {
            lineRenderer.material = new Material(shader);
        }
    }
}
