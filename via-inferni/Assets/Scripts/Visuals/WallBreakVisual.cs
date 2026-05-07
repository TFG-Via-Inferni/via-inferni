using UnityEngine;

[DisallowMultipleComponent]
public class WallBreakVisual : MonoBehaviour
{
    [SerializeField] private float baseLifetime = 0.3f;
    [SerializeField] private float baseSize = 0.18f;
    [SerializeField] private float baseSpeed = 1.05f;

    private ParticleSystem particleSystemRef;
    private ParticleSystemRenderer particleRenderer;

    public static void Spawn(Vector3 position, Color color, Vector2 direction, float scale = 1f, Renderer referenceRenderer = null, int sortingOrderOffset = 15)
    {
        GameObject impactObject = new GameObject("WallBreakVisual");
        impactObject.transform.position = position + new Vector3(0.5f, 0.5f, 0f);

        WallBreakVisual visual = impactObject.AddComponent<WallBreakVisual>();
        visual.Play(color, direction, scale, referenceRenderer, sortingOrderOffset);
    }

    private void Play(Color color, Vector2 direction, float scale, Renderer referenceRenderer, int sortingOrderOffset)
    {
        EnsureParticleSystem();
        particleSystemRef.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particleSystemRef.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = baseLifetime;
        main.startLifetime = baseLifetime * Mathf.Max(0.9f, scale);
        main.startSpeed = 0f;
        main.startSize = baseSize * Mathf.Max(0.9f, scale);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 48;

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
                new GradientAlphaKey(0.7f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = particleSystemRef.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.4f, 1.2f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.sortMode = ParticleSystemSortMode.Distance;
        if (referenceRenderer != null)
        {
            particleRenderer.sortingLayerID = referenceRenderer.sortingLayerID;
            particleRenderer.sortingOrder = Mathf.Max(referenceRenderer.sortingOrder + sortingOrderOffset, 5000);
        }
        else
        {
            particleRenderer.sortingOrder = 5000;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            particleRenderer.material = new Material(shader);
        }

        Vector2 forward = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector2 perpendicular = new Vector2(-forward.y, forward.x);
        int particleCount = Mathf.RoundToInt(Mathf.Lerp(12f, 18f, Mathf.Clamp01(scale)));

        for (int i = 0; i < particleCount; i++)
        {
            float side = Random.Range(-1.45f, 1.45f);
            Vector2 velocity = (forward * Random.Range(0.3f, 1f) + perpendicular * side).normalized
                * (baseSpeed * Random.Range(1f, 1.7f) * Mathf.Max(1f, scale));

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = transform.position,
                velocity = velocity,
                startLifetime = baseLifetime * Random.Range(1f, 1.6f),
                startSize = baseSize * Random.Range(1.05f, 1.7f) * Mathf.Max(1f, scale),
                startColor = color
            };

            particleSystemRef.Emit(emitParams, 1);
        }

        Destroy(gameObject, baseLifetime * 1.6f);
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