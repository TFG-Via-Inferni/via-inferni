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
    private Sprite fallbackSprite;

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
        fallbackSprite = definition.overrideSprite;
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

    }

    private void LateUpdate()
    {
        if (PauseMenuController.IsPaused || enemyController == null || spriteRenderer == null)
        {
            return;
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
        if (spriteRenderer == null)
        {
            return;
        }

        if (sideMoveFrames != null && sideMoveFrames.Length > 0)
        {
            int spriteIndex = enemyController != null && enemyController.IsAlerted
                ? Mathf.Clamp(currentFrameIndex, 0, sideMoveFrames.Length - 1)
                : 0;

            spriteRenderer.sprite = sideMoveFrames[spriteIndex];
        }
        else if (fallbackSprite != null)
        {
            spriteRenderer.sprite = fallbackSprite;
        }

        spriteRenderer.flipX = false;

        Transform spriteTransform = spriteRenderer.transform;
        Vector3 currentScale = spriteTransform.localScale;
        float absoluteX = Mathf.Abs(currentScale.x);
        if (absoluteX <= 0.0001f)
        {
            absoluteX = 1f;
        }

        currentScale.x = facingRight ? absoluteX : -absoluteX;
        spriteTransform.localScale = currentScale;
    }
}
