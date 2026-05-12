using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpriteAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Playback")]
    [Min(0.01f)] [SerializeField] private float moveFramesPerSecond = 4f;
    [Min(0f)] [SerializeField] private float moveDeadZone = 0.05f;
    [Min(0f)] [SerializeField] private float horizontalDeadZone = 0.02f;

    [Header("Side Movement")]
    [SerializeField] private Sprite[] sideMoveFrames;

    private int currentFrameIndex;
    private float frameTimer;
    private bool facingRight = true;

    public void Bind(EnemyController controller, SpriteRenderer targetRenderer)
    {
        enemyController = controller;
        spriteRenderer = targetRenderer;
    }

    public void ApplyDefinition(EnemyDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        sideMoveFrames = definition.sideMoveFrames;
        currentFrameIndex = 0;
        frameTimer = 0f;
        ApplyCurrentVisual();
    }

    public bool HasConfiguredAnimation()
    {
        return sideMoveFrames != null && sideMoveFrames.Length > 0;
    }

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }
    }

    private void OnEnable()
    {
        currentFrameIndex = 0;
        frameTimer = 0f;
        ApplyCurrentVisual();
    }

    private void Update()
    {
        if (PauseMenuController.IsPaused || enemyController == null || spriteRenderer == null)
        {
            return;
        }

        Vector2 visualVelocity = enemyController.VisualVelocity;
        Vector2 facingVector = visualVelocity.sqrMagnitude > moveDeadZone * moveDeadZone
            ? visualVelocity
            : enemyController.FacingDirection;

        if (Mathf.Abs(facingVector.x) > horizontalDeadZone)
        {
            facingRight = facingVector.x >= 0f;
        }

        if (enemyController.IsAlerted)
        {
            AdvanceMoveFrames();
        }
        else
        {
            currentFrameIndex = 0;
            frameTimer = 0f;
        }

        ApplyCurrentVisual();
    }

    private void AdvanceMoveFrames()
    {
        if (sideMoveFrames == null || sideMoveFrames.Length == 0)
        {
            return;
        }

        if (sideMoveFrames.Length == 1)
        {
            currentFrameIndex = 0;
            return;
        }

        float frameDuration = 1f / moveFramesPerSecond;
        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrameIndex = (currentFrameIndex + 1) % sideMoveFrames.Length;
        }
    }

    private void ApplyCurrentVisual()
    {
        if (spriteRenderer == null || sideMoveFrames == null || sideMoveFrames.Length == 0)
        {
            return;
        }

        int spriteIndex = enemyController != null && enemyController.IsAlerted
            ? Mathf.Clamp(currentFrameIndex, 0, sideMoveFrames.Length - 1)
            : 0;

        spriteRenderer.sprite = sideMoveFrames[spriteIndex];
        spriteRenderer.flipX = !facingRight;
    }
}
