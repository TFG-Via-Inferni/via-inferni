using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDashTrailVisual : MonoBehaviour
{
    [SerializeField] private float baseEmissionRate = 160f;
    [SerializeField] private float baseParticleLifetime = 0.32f;
    [SerializeField] private float baseParticleSize = 0.13f;
    [SerializeField] private float baseParticleSpeed = 0.14f;
    [SerializeField] private float lateralSpread = 0.085f;
    [SerializeField] private float rearOffsetMultiplier = 1.15f;
    [SerializeField] private int sortingOrderOffset = 2;
    [SerializeField] private Color defaultTrailColor = new Color(0.86f, 0.97f, 1f, 1f);

    private ParticleSystem particleSystemRef;
    private ParticleSystemRenderer particleRenderer;
    private SpriteRenderer targetSpriteRenderer;
    private float emissionRate;
    private float particleLifetime;
    private float particleSize;
    private float particleSpeed;
    private float rearOffsetDistance = 0.12f;
    private Color particleColor = Color.white;
    private float emissionAccumulator;
    private float emitUntil;
    private Vector2 activeDirection = Vector2.right;

    private void Awake()
    {
        EnsureParticleSystem();
    }

    public void Configure(SpriteRenderer spriteRenderer)
    {
        EnsureParticleSystem();
        targetSpriteRenderer = spriteRenderer;

        emissionRate = baseEmissionRate;
        particleLifetime = baseParticleLifetime;
        particleSize = baseParticleSize;
        particleSpeed = baseParticleSpeed;
        particleColor = spriteRenderer != null
            ? Color.Lerp(defaultTrailColor, spriteRenderer.color, 0.2f)
            : defaultTrailColor;

        if (targetSpriteRenderer != null)
        {
            Bounds bounds = targetSpriteRenderer.bounds;
            rearOffsetDistance = Mathf.Max(bounds.extents.x, bounds.extents.y, 0.06f) * rearOffsetMultiplier;
        }

        ApplyRendererSettings();
        emissionAccumulator = 0f;
    }

    public void Play(Vector2 direction, float duration)
    {
        if (duration <= 0f)
        {
            return;
        }

        EnsureParticleSystem();

        activeDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        emitUntil = Mathf.Max(emitUntil, Time.time + duration);
        emissionAccumulator = 0f;
        SpawnBurst(5);
    }

    private void LateUpdate()
    {
        if (particleSystemRef == null || Time.time >= emitUntil)
        {
            return;
        }

        if (targetSpriteRenderer != null && !targetSpriteRenderer.enabled)
        {
            return;
        }

        emissionAccumulator += emissionRate * Time.deltaTime;
        int particlesToEmit = Mathf.FloorToInt(emissionAccumulator);
        if (particlesToEmit <= 0)
        {
            return;
        }

        emissionAccumulator -= particlesToEmit;
        SpawnParticles(particlesToEmit);
    }

    private void SpawnBurst(int particles)
    {
        if (particles <= 0)
        {
            return;
        }

        SpawnParticles(particles);
    }

    private void SpawnParticles(int particles)
    {
        Vector2 normalizedDirection = activeDirection.sqrMagnitude > 0.0001f
            ? activeDirection.normalized
            : Vector2.right;

        Vector2 perpendicular = new Vector2(-normalizedDirection.y, normalizedDirection.x);
        Vector3 tailPosition = transform.position - (Vector3)(normalizedDirection * rearOffsetDistance);

        for (int i = 0; i < particles; i++)
        {
            float spread = Random.Range(-lateralSpread, lateralSpread);
            Vector3 emitPosition = tailPosition + (Vector3)(perpendicular * spread);
            Vector2 velocity = (-normalizedDirection * particleSpeed) + (perpendicular * Random.Range(-lateralSpread, lateralSpread) * 0.35f);

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = emitPosition,
                velocity = velocity,
                startLifetime = particleLifetime,
                startSize = particleSize * Random.Range(0.9f, 1.15f),
                startColor = particleColor
            };

            particleSystemRef.Emit(emitParams, 1);
        }
    }

    private void EnsureParticleSystem()
    {
        if (particleSystemRef != null)
        {
            return;
        }

        particleSystemRef = GetComponent<ParticleSystem>();
        if (particleSystemRef == null)
        {
            particleSystemRef = gameObject.AddComponent<ParticleSystem>();
        }

        particleRenderer = GetComponent<ParticleSystemRenderer>();
        if (particleRenderer == null)
        {
            particleRenderer = gameObject.AddComponent<ParticleSystemRenderer>();
        }

        var main = particleSystemRef.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = baseParticleLifetime;
        main.startSpeed = 0f;
        main.startSize = baseParticleSize;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 160;

        var emission = particleSystemRef.emission;
        emission.enabled = false;

        var shape = particleSystemRef.shape;
        shape.enabled = false;

        var colorOverLifetime = particleSystemRef.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.35f),
                new GradientAlphaKey(0.35f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = particleSystemRef.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.55f, 0.95f),
            new Keyframe(0.8f, 0.6f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortMode = ParticleSystemSortMode.Distance;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader != null)
        {
            particleRenderer.material = new Material(shader);
        }

        emitUntil = -999f;
    }

    private void ApplyRendererSettings()
    {
        if (particleRenderer == null)
        {
            return;
        }

        int sortingOrder = targetSpriteRenderer != null ? targetSpriteRenderer.sortingOrder + sortingOrderOffset : sortingOrderOffset;
        particleRenderer.sortingOrder = sortingOrder;
        particleRenderer.sortingLayerID = targetSpriteRenderer != null ? targetSpriteRenderer.sortingLayerID : particleRenderer.sortingLayerID;
    }
}