using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMoveDustVisual : MonoBehaviour
{
    [SerializeField] private float moveThreshold = 0.12f;
    [SerializeField] private float emissionInterval = 0.075f;
    [SerializeField] private Vector2 emissionOffset = new Vector2(0f, -0.43f);
    [SerializeField] private float particleLifetime = 0.28f;
    [SerializeField] private float particleSize = 0.078f;
    [SerializeField] private float particleSpeed = 0.22f;
    [SerializeField] private Color particleColor = new Color(0.96f, 0.9f, 0.78f, 0.75f);

    private ParticleSystem particleSystemRef;
    private float emitTimer;

    private void Awake()
    {
        EnsureParticleSystem();
    }

    public void Tick(Vector2 movementVelocity, bool canEmit)
    {
        if (!canEmit || movementVelocity.magnitude < moveThreshold)
        {
            emitTimer = 0f;
            return;
        }

        emitTimer += Time.deltaTime;
        if (emitTimer < emissionInterval)
        {
            return;
        }

        emitTimer = 0f;

        Vector2 direction = movementVelocity.sqrMagnitude > 0.0001f
            ? movementVelocity.normalized
            : Vector2.down;

        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        Vector3 origin = transform.position + (Vector3)emissionOffset;

        EmitParticle(origin + (Vector3)(perpendicular * Random.Range(-0.08f, 0.08f)), -direction * particleSpeed * Random.Range(0.65f, 1.05f));
        EmitParticle(origin + (Vector3)(perpendicular * Random.Range(-0.08f, 0.08f)), -direction * particleSpeed * Random.Range(0.55f, 0.9f));
    }

    private void EmitParticle(Vector3 position, Vector2 velocity)
    {
        EnsureParticleSystem();

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = velocity,
            startLifetime = particleLifetime * Random.Range(0.9f, 1.15f),
            startSize = particleSize * Random.Range(0.85f, 1.2f),
            startColor = particleColor
        };

        particleSystemRef.Emit(emitParams, 1);
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

        ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
        {
            renderer = gameObject.AddComponent<ParticleSystemRenderer>();
        }

        var main = particleSystemRef.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0f;
        main.startSize = particleSize;
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
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.45f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        var sizeOverLifetime = particleSystemRef.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.9f),
            new Keyframe(0.55f, 1f),
            new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.sortingOrder = -2;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            renderer.material = new Material(shader);
        }
    }
}
