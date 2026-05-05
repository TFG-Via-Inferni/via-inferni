using UnityEngine;

[DisallowMultipleComponent]
public class MeleeSlashVisual : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.1f;
    [SerializeField] private int swordArcPoints = 12;
    [SerializeField] private int axeArcPoints = 10;
    [SerializeField] private float swordWidth = 0.13f;
    [SerializeField] private float axeWidth = 0.2f;
    [SerializeField] private float spearWidth = 0.08f;
    [SerializeField] private float swordArcRadiusMultiplier = 0.95f;
    [SerializeField] private float swordEchoRadiusOffset = 0.16f;
    [SerializeField] private float axeArcRadiusMultiplier = 0.82f;
    [SerializeField] private float axeForwardOffset = 0.16f;
    [SerializeField] private float spearTipSize = 0.1f;
    [SerializeField] private Color swordColor = new Color(1f, 1f, 1f, 0.92f);
    [SerializeField] private Color axeColor = new Color(1f, 0.92f, 0.8f, 0.95f);
    [SerializeField] private Color spearColor = new Color(0.9f, 1f, 1f, 0.95f);
    [SerializeField] private int sortingOrder = 1000;

    public void Initialize(Vector2 start, Vector2 end, WeaponDefinition weapon)
    {
        WeaponAttackMotionStyle motionStyle = weapon != null
            ? weapon.GetResolvedAttackMotionStyle()
            : WeaponAttackMotionStyle.Sweep;

        switch (motionStyle)
        {
            case WeaponAttackMotionStyle.Thrust:
                CreateSpearVisual(start, end);
                break;

            case WeaponAttackMotionStyle.Chop:
                CreateAxeVisual(start, end);
                break;

            case WeaponAttackMotionStyle.Sweep:
            default:
                CreateSwordVisual(start, end);
                break;
        }

        Destroy(gameObject, lifetime);
    }

    private void CreateSwordVisual(Vector2 start, Vector2 end)
    {
        Vector2 direction = GetDirection(start, end);
        float radius = Mathf.Max(0.15f, Vector2.Distance(start, end) * swordArcRadiusMultiplier);
        float arcDegrees = 110f;

        LineRenderer main = CreateLineRenderer("SwordSlashMain", swordColor, swordWidth, swordWidth * 0.35f);
        SetArcPositions(main, start, direction, radius, arcDegrees, Mathf.Max(4, swordArcPoints));

        Color echoColor = new Color(swordColor.r, swordColor.g, swordColor.b, swordColor.a * 0.45f);
        LineRenderer echo = CreateLineRenderer("SwordSlashEcho", echoColor, swordWidth * 0.55f, swordWidth * 0.08f);
        SetArcPositions(
            echo,
            start - (direction * 0.05f),
            direction,
            radius - Mathf.Max(0.02f, swordEchoRadiusOffset),
            arcDegrees * 0.88f,
            Mathf.Max(4, swordArcPoints - 2));
    }

    private void CreateAxeVisual(Vector2 start, Vector2 end)
    {
        Vector2 direction = GetDirection(start, end);
        Vector2 center = start + (direction * axeForwardOffset);
        float radius = Mathf.Max(0.12f, Vector2.Distance(start, end) * axeArcRadiusMultiplier);
        float arcDegrees = 78f;

        LineRenderer main = CreateLineRenderer("AxeChopMain", axeColor, axeWidth, axeWidth * 0.6f);
        SetArcPositions(main, center, direction, radius, arcDegrees, Mathf.Max(4, axeArcPoints));

        Color echoColor = new Color(axeColor.r, axeColor.g, axeColor.b, axeColor.a * 0.55f);
        LineRenderer echo = CreateLineRenderer("AxeChopEcho", echoColor, axeWidth * 0.65f, axeWidth * 0.15f);
        SetArcPositions(
            echo,
            center - (direction * 0.08f),
            direction,
            radius - 0.12f,
            arcDegrees * 0.72f,
            Mathf.Max(4, axeArcPoints - 2));
    }

    private void CreateSpearVisual(Vector2 start, Vector2 end)
    {
        Vector2 direction = GetDirection(start, end);
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        Vector2 tip = end;
        float tipSize = Mathf.Max(0.02f, spearTipSize);

        Color trailColor = new Color(spearColor.r, spearColor.g, spearColor.b, spearColor.a * 0.5f);
        LineRenderer trail = CreateLineRenderer("SpearTrail", trailColor, spearWidth, spearWidth * 0.15f);
        trail.positionCount = 2;
        trail.SetPosition(0, Vector2.Lerp(start, end, 0.35f));
        trail.SetPosition(1, tip);

        LineRenderer sparkleA = CreateLineRenderer("SpearTipA", spearColor, spearWidth * 0.8f, 0f);
        sparkleA.positionCount = 2;
        sparkleA.SetPosition(0, tip - (perpendicular * tipSize));
        sparkleA.SetPosition(1, tip + (perpendicular * tipSize));

        LineRenderer sparkleB = CreateLineRenderer("SpearTipB", spearColor, spearWidth * 0.6f, 0f);
        sparkleB.positionCount = 2;
        sparkleB.SetPosition(0, tip - (direction * tipSize * 0.65f));
        sparkleB.SetPosition(1, tip + (direction * tipSize * 0.35f));
    }

    private static Vector2 GetDirection(Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        return direction.normalized;
    }

    private LineRenderer CreateLineRenderer(string childName, Color color, float startWidth, float endWidth)
    {
        GameObject lineObject = new GameObject(childName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
        lineRenderer.startColor = color;
        lineRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
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

    private static void SetArcPositions(LineRenderer lineRenderer, Vector2 center, Vector2 facing, float radius, float arcDegrees, int pointCount)
    {
        if (lineRenderer == null)
        {
            return;
        }

        pointCount = Mathf.Max(2, pointCount);
        lineRenderer.positionCount = pointCount;

        float baseAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        float startAngle = baseAngle - (arcDegrees * 0.5f);
        float step = pointCount > 1 ? arcDegrees / (pointCount - 1f) : 0f;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = startAngle + (step * i);
            float radians = angle * Mathf.Deg2Rad;
            Vector2 point = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
            lineRenderer.SetPosition(i, point);
        }
    }
}
