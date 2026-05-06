using UnityEngine;

[DisallowMultipleComponent]
public class ProjectileImpactVisual : MonoBehaviour
{
    [SerializeField] private float baseLifetime = 0.18f;
    [SerializeField] private float baseSize = 0.11f;
    [SerializeField] private float baseSpeed = 0.7f;

    private ParticleSystem particleSystemRef;
    private ParticleSystemRenderer particleRenderer;

    public static void Spawn(Vector3 position, Color color, Vector2 direction, float scale = 1f)
    {
        GameObject impactObject = new GameObject("ProjectileImpact");
        impactObject.transform.position = position;

        ProjectileImpactVisual visual = impactObject.AddComponent<ProjectileImpactVisual>();
        visual.Play(color, direction, scale);
    }

    private void Play(Color color, Vector2 direction, float scale)
    {
        EnsureParticleSystem();
        particleSystemRef.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particleSystemRef.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = baseLifetime;
        main.startLifetime = baseLifetime * Mathf.Max(0.6f, scale);
        main.startSpeed = 0f;
        main.startSize = baseSize * Mathf.Max(0.7f, scale);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;

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
                new GradientAlphaKey(0.5f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = particleSystemRef.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.65f),
            new Keyframe(0.35f, 1f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortMode = ParticleSystemSortMode.Distance;
        particleRenderer.sortingOrder = 8;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            particleRenderer.material = new Material(shader);
        }

        Vector2 forward = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 perpendicular = new Vector2(-forward.y, forward.x);
        int particleCount = Mathf.RoundToInt(Mathf.Lerp(4f, 7f, Mathf.Clamp01(scale)));

        for (int i = 0; i < particleCount; i++)
        {
            float side = Random.Range(-0.75f, 0.75f);
            Vector2 velocity = (forward * Random.Range(0.25f, 1f) + perpendicular * side).normalized
                * (baseSpeed * Random.Range(0.7f, 1.2f) * Mathf.Max(0.7f, scale));

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = transform.position,
                velocity = velocity,
                startLifetime = baseLifetime * Random.Range(0.8f, 1.1f),
                startSize = baseSize * Random.Range(0.85f, 1.15f) * Mathf.Max(0.7f, scale),
                startColor = color
            };

            particleSystemRef.Emit(emitParams, 1);
        }

        Destroy(gameObject, baseLifetime * 1.5f);
    }

    private void EnsureParticleSystem()
    {
        if (particleSystemRef == null)
        {
            particleSystemRef = GetComponent<ParticleSystem>();
            if (particleSystemRef == null)
            {
                particleSystemRef = gameObject.AddComponent<ParticleSystem>();
            }
        }

        if (particleRenderer == null)
        {
            particleRenderer = GetComponent<ParticleSystemRenderer>();
            if (particleRenderer == null)
            {
                particleRenderer = particleSystemRef.GetComponent<ParticleSystemRenderer>();
            }
        }
    }
}
