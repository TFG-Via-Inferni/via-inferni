using UnityEngine;

[DisallowMultipleComponent]
public class EnemyCombatStatus : MonoBehaviour
{
    [Header("Mark")]
    [SerializeField] private Color markedTint = new Color(0.55f, 1f, 0.95f, 1f);
    [SerializeField] private float markedPulseStrength = 0.12f;
    [SerializeField] private float markedPulseSpeed = 10f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] baseColors;
    private float markedUntil = -999f;
    private bool wasMarkedLastFrame;

    public bool IsMarked => Time.time < markedUntil;

    private void Awake()
    {
        CacheSpriteRenderers();
    }

    private void OnEnable()
    {
        CacheSpriteRenderers();
        ApplyVisualState(1f);
    }

    private void LateUpdate()
    {
        bool isMarked = IsMarked;
        if (!isMarked && !wasMarkedLastFrame)
        {
            return;
        }

        float pulse = isMarked
            ? 0.5f + (0.5f * Mathf.Abs(Mathf.Sin(Time.time * Mathf.Max(0.01f, markedPulseSpeed))))
            : 0f;

        ApplyVisualState(pulse);
        wasMarkedLastFrame = isMarked;
    }

    public void ApplyMark(float duration)
    {
        markedUntil = Mathf.Max(markedUntil, Time.time + Mathf.Max(0.1f, duration));
        wasMarkedLastFrame = true;
    }

    public bool ConsumeMark()
    {
        if (!IsMarked)
        {
            return false;
        }

        markedUntil = -999f;
        ApplyVisualState(0f);
        wasMarkedLastFrame = false;
        return true;
    }

    private void CacheSpriteRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (spriteRenderers == null)
        {
            spriteRenderers = new SpriteRenderer[0];
        }

        baseColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            baseColors[i] = spriteRenderers[i] != null ? spriteRenderers[i].color : Color.white;
        }
    }

    private void ApplyVisualState(float pulse)
    {
        if (spriteRenderers == null || baseColors == null)
        {
            return;
        }

        float tintAmount = Mathf.Clamp01(pulse * Mathf.Max(0f, markedPulseStrength));
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            Color baseColor = i < baseColors.Length ? baseColors[i] : Color.white;
            spriteRenderer.color = Color.Lerp(baseColor, markedTint, tintAmount);
        }
    }
}
