using UnityEngine;

[DisallowMultipleComponent]
public class ProjectileParticleTrailVisual : MonoBehaviour
{
    [SerializeField] private float baseEmissionRate = 72f;
    [SerializeField] private float baseParticleLifetime = 0.26f;
    [SerializeField] private float baseParticleSize = 0.075f;
    [SerializeField] private float baseParticleSpeed = 0.18f;
    [SerializeField] private float lateralSpread = 0.065f;
    [SerializeField] private float rearOffsetMultiplier = 0.95f;

    private ParticleSystem particleSystemRef;
    private ParticleSystemRenderer particleRenderer;
    private SpriteRenderer projectileSpriteRenderer;
    private float emissionRate;
    private float particleLifetime;
    private float particleSize;
    private float particleSpeed;
    private float rearOffsetDistance = 0.12f;
    private Color particleColor = Color.white;
    private float emissionAccumulator;

    private void Awake()
    {
        EnsureParticleSystem();
    }

    public void Configure(WeaponDefinition weapon, SpriteRenderer spriteRenderer)
    {
        EnsureParticleSystem();
        projectileSpriteRenderer = spriteRenderer;

        emissionRate = baseEmissionRate;
        particleLifetime = baseParticleLifetime;
        particleSize = baseParticleSize;
        particleSpeed = baseParticleSpeed;
        particleColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        if (weapon != null)
        {
            string key = string.IsNullOrWhiteSpace(weapon.WeaponId)
                ? weapon.DisplayName
                : weapon.WeaponId;
            key = string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();

            switch (key)
            {
                case "bow":
                    emissionRate *= 0.8f;
                    particleLifetime *= 0.85f;
                    particleSize *= 0.85f;
                    particleSpeed *= 0.85f;
                    particleColor = new Color(1f, 0.95f, 0.84f, 0.92f);
                    break;

                case "magic":
                    emissionRate *= 1.2f;
                    particleLifetime *= 1.2f;
                    particleSize *= 1.05f;
                    particleSpeed *= 0.9f;
                    particleColor = new Color(0.72f, 1f, 0.96f, 0.9f);
                    break;

                case "ballista":
                    emissionRate *= 0.95f;
                    particleLifetime *= 1.15f;
                    particleSize *= 1.15f;
                    particleSpeed *= 1.1f;
                    particleColor = new Color(1f, 0.88f, 0.72f, 0.96f);
                    break;
            }
        }

        if (projectileSpriteRenderer != null)
        {
            Bounds bounds = projectileSpriteRenderer.bounds;
            rearOffsetDistance = Mathf.Max(bounds.extents.x, bounds.extents.y, 0.06f) * rearOffsetMultiplier;
        }

        ApplyRendererSettings();
        emissionAccumulator = 0f;
    }

    public void Tick(Vector2 direction)
    {
        if (particleSystemRef == null)
        {
            return;
        }

        Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector2.right;

        emissionAccumulator += emissionRate * Time.deltaTime;
        int particlesToEmit = Mathf.FloorToInt(emissionAccumulator);
        if (particlesToEmit <= 0)
        {
            return;
        }

        emissionAccumulator -= particlesToEmit;
        Vector2 perpendicular = new Vector2(-normalizedDirection.y, normalizedDirection.x);
        Vector3 tailPosition = transform.position - (Vector3)(normalizedDirection * rearOffsetDistance);

        for (int i = 0; i < particlesToEmit; i++)
        {
            float spread = Random.Range(-lateralSpread, lateralSpread);
            Vector3 emitPosition = tailPosition + (Vector3)(perpendicular * spread);
            Vector2 velocity = (-normalizedDirection * particleSpeed) + (perpendicular * Random.Range(-lateralSpread, lateralSpread) * 0.45f);

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = emitPosition,
                velocity = velocity,
                startLifetime = particleLifetime,
                startSize = particleSize * Random.Range(0.95f, 1.2f),
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
        main.maxParticles = 128;

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
                new GradientAlphaKey(0.6f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = particleSystemRef.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.6f, 0.75f),
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
    }

    private void ApplyRendererSettings()
    {
        if (particleRenderer == null)
        {
            return;
        }

        int sortingOrder = projectileSpriteRenderer != null ? projectileSpriteRenderer.sortingOrder - 1 : -1;
        particleRenderer.sortingOrder = sortingOrder;
    }
}
