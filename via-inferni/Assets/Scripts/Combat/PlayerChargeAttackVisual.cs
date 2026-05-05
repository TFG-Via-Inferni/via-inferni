using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerCombatController))]
[RequireComponent(typeof(Player))]
public class PlayerChargeAttackVisual : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float anchorDistance = 1.2f;
    [SerializeField] private float minRadius = 0.18f;
    [SerializeField] private float maxRadius = 0.72f;
    [SerializeField] private float innerRadiusRatio = 0.72f;
    [SerializeField] private int outerArcPoints = 18;
    [SerializeField] private int innerArcPoints = 12;

    [Header("Style")]
    [SerializeField] private float minArcDegrees = 35f;
    [SerializeField] private float maxArcDegrees = 320f;
    [SerializeField] private float minWidth = 0.05f;
    [SerializeField] private float maxWidth = 0.12f;
    [SerializeField] private Color baseColor = new Color(0.8f, 0.95f, 1f, 0.45f);
    [SerializeField] private Color maxChargeColor = new Color(1f, 0.92f, 0.55f, 0.95f);
    [SerializeField] private float pulseSpeed = 7f;
    [SerializeField] private int sortingOrder = 900;

    private Player player;
    private PlayerCombatController combatController;
    private LineRenderer outerArc;
    private LineRenderer innerArc;

    private void Awake()
    {
        player = GetComponent<Player>();
        combatController = GetComponent<PlayerCombatController>();
        outerArc = CreateLineRenderer("ChargeArcOuter");
        innerArc = CreateLineRenderer("ChargeArcInner");
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (PauseMenuController.IsPaused || player == null || combatController == null)
        {
            SetVisible(false);
            return;
        }

        if (!combatController.IsChargingAttack || combatController.ChargingWeapon == null)
        {
            SetVisible(false);
            return;
        }

        float charge = combatController.CurrentChargeNormalized;
        Vector2 direction = combatController.ChargingDirection.sqrMagnitude > 0.0001f
            ? combatController.ChargingDirection.normalized
            : player.FacingDirection.normalized;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.right;
        }

        float pulse = 0.75f + (0.25f * Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed)));
        float radius = Mathf.Lerp(minRadius, maxRadius, charge);
        float arcDegrees = Mathf.Lerp(minArcDegrees, maxArcDegrees, charge);
        float width = Mathf.Lerp(minWidth, maxWidth, charge);
        Color arcColor = Color.Lerp(baseColor, maxChargeColor, charge);
        arcColor.a *= pulse;

        Vector2 anchor = (Vector2)transform.position + (direction * anchorDistance);
        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        outerArc.startWidth = width;
        outerArc.endWidth = width * 0.4f;
        outerArc.startColor = arcColor;
        outerArc.endColor = new Color(arcColor.r, arcColor.g, arcColor.b, 0f);
        SetArc(outerArc, anchor, radius, baseAngle, arcDegrees, outerArcPoints);

        Color innerColor = new Color(arcColor.r, arcColor.g, arcColor.b, arcColor.a * 0.7f);
        innerArc.startWidth = width * 0.55f;
        innerArc.endWidth = 0f;
        innerArc.startColor = innerColor;
        innerArc.endColor = new Color(innerColor.r, innerColor.g, innerColor.b, 0f);
        SetArc(innerArc, anchor, radius * innerRadiusRatio, baseAngle, arcDegrees * 0.82f, innerArcPoints);

        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (outerArc != null)
        {
            outerArc.enabled = visible;
        }

        if (innerArc != null)
        {
            innerArc.enabled = visible;
        }
    }

    private LineRenderer CreateLineRenderer(string objectName)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(transform, false);

        LineRenderer lineRenderer = child.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.numCapVertices = 5;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

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

    private static void SetArc(LineRenderer lineRenderer, Vector2 center, float radius, float baseAngle, float arcDegrees, int pointCount)
    {
        if (lineRenderer == null)
        {
            return;
        }

        pointCount = Mathf.Max(3, pointCount);
        lineRenderer.positionCount = pointCount;

        float startAngle = baseAngle - (arcDegrees * 0.5f);
        float step = arcDegrees / (pointCount - 1f);

        for (int i = 0; i < pointCount; i++)
        {
            float angle = startAngle + (step * i);
            float radians = angle * Mathf.Deg2Rad;
            Vector2 point = center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
            lineRenderer.SetPosition(i, point);
        }
    }
}
