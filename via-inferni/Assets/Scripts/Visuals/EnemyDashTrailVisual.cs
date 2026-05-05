using UnityEngine;

[DisallowMultipleComponent]
public class EnemyDashTrailVisual : MonoBehaviour
{
    [SerializeField] private float spawnInterval = 0.028f;
    [SerializeField] private float ghostLifetime = 0.12f;
    [SerializeField] private float baseAlpha = 0.22f;
    [SerializeField] private float scaleMultiplier = 1.03f;
    [SerializeField] private int sortingOrderOffset = -1;

    private SpriteRenderer targetSpriteRenderer;
    private float emitTimer;
    private float emitUntil;
    private Color activeColor = Color.white;
    private float activeAlphaMultiplier = 1f;
    private float activeSpawnIntervalMultiplier = 1f;
    private float activeLifetimeMultiplier = 1f;
    private float activeScaleMultiplier = 1f;

    public void Configure(SpriteRenderer spriteRenderer)
    {
        targetSpriteRenderer = spriteRenderer;
    }

    public void Play(
        float duration,
        Color color,
        float alphaMultiplier = 1f,
        float spawnIntervalMultiplier = 1f,
        float lifetimeMultiplier = 1f,
        float scaleBoost = 1f)
    {
        if (duration <= 0f)
        {
            return;
        }

        activeColor = color;
        activeAlphaMultiplier = Mathf.Max(0.1f, alphaMultiplier);
        activeSpawnIntervalMultiplier = Mathf.Max(0.25f, spawnIntervalMultiplier);
        activeLifetimeMultiplier = Mathf.Max(0.25f, lifetimeMultiplier);
        activeScaleMultiplier = Mathf.Max(0.5f, scaleBoost);
        emitTimer = 0f;
        emitUntil = Time.time + duration;
        SpawnGhost();
    }

    private void LateUpdate()
    {
        if (Time.time >= emitUntil || targetSpriteRenderer == null || !targetSpriteRenderer.enabled)
        {
            return;
        }

        emitTimer += Time.deltaTime;
        if (emitTimer < spawnInterval * activeSpawnIntervalMultiplier)
        {
            return;
        }

        emitTimer = 0f;
        SpawnGhost();
    }

    private void SpawnGhost()
    {
        if (targetSpriteRenderer == null || targetSpriteRenderer.sprite == null)
        {
            return;
        }

        GameObject ghostObject = new GameObject("EnemyDashGhost");
        ghostObject.transform.position = targetSpriteRenderer.transform.position;
        ghostObject.transform.rotation = targetSpriteRenderer.transform.rotation;
        ghostObject.transform.localScale = targetSpriteRenderer.transform.lossyScale * (scaleMultiplier * activeScaleMultiplier);

        SpriteRenderer ghostRenderer = ghostObject.AddComponent<SpriteRenderer>();
        ghostRenderer.sprite = targetSpriteRenderer.sprite;
        ghostRenderer.flipX = targetSpriteRenderer.flipX;
        ghostRenderer.flipY = targetSpriteRenderer.flipY;
        ghostRenderer.sortingLayerID = targetSpriteRenderer.sortingLayerID;
        ghostRenderer.sortingOrder = targetSpriteRenderer.sortingOrder + sortingOrderOffset;

        Color color = activeColor;
        color.a *= baseAlpha * activeAlphaMultiplier;
        ghostRenderer.color = color;

        EnemyDashTrailGhost ghost = ghostObject.AddComponent<EnemyDashTrailGhost>();
        ghost.Initialize(ghostRenderer, ghostLifetime * activeLifetimeMultiplier);
    }

    private sealed class EnemyDashTrailGhost : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float lifetime;
        private float timer;
        private Color startColor;

        public void Initialize(SpriteRenderer renderer, float fadeLifetime)
        {
            spriteRenderer = renderer;
            lifetime = Mathf.Max(0.02f, fadeLifetime);
            startColor = renderer != null ? renderer.color : Color.white;
        }

        private void Update()
        {
            if (spriteRenderer == null)
            {
                Destroy(gameObject);
                return;
            }

            timer += Time.deltaTime;
            float normalized = Mathf.Clamp01(timer / lifetime);
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, normalized);
            spriteRenderer.color = color;

            if (normalized >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
