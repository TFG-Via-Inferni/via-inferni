using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpriteAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Playback")]
    [Min(0.01f)] [SerializeField] private float idleFramesPerSecond = 5f;
    [Min(0f)] [SerializeField] private float moveDeadZone = 0.05f;
    [Min(0f)] [SerializeField] private float horizontalDeadZone = 0.02f;

    [Header("Sprites")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite moveRightSprite;
    [SerializeField] private Sprite moveLeftSprite;

    private int currentIdleFrameIndex;
    private float idleFrameTimer;
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

        idleFrames = definition.idleFrames;
        moveRightSprite = definition.moveRightSprite;
        moveLeftSprite = definition.moveLeftSprite;
        currentIdleFrameIndex = 0;
        idleFrameTimer = 0f;
        ApplyCurrentVisual();
    }

    public bool HasConfiguredAnimation()
    {
        return (idleFrames != null && idleFrames.Length > 0)
            || moveRightSprite != null
            || moveLeftSprite != null;
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
        currentIdleFrameIndex = 0;
        idleFrameTimer = 0f;
        ApplyCurrentVisual();
    }

    private void Update()
    {
        if (PauseMenuController.IsPaused || enemyController == null || spriteRenderer == null)
        {
            return;
        }

        Vector2 visualVelocity = enemyController.VisualVelocity;
        bool isMoving = visualVelocity.sqrMagnitude > moveDeadZone * moveDeadZone;

        if (Mathf.Abs(visualVelocity.x) > horizontalDeadZone)
        {
            facingRight = visualVelocity.x >= 0f;
        }
        else if (Mathf.Abs(enemyController.FacingDirection.x) > horizontalDeadZone)
        {
            facingRight = enemyController.FacingDirection.x >= 0f;
        }

        if (!isMoving)
        {
            AdvanceIdleFrames();
        }
        else
        {
            idleFrameTimer = 0f;
            currentIdleFrameIndex = 0;
        }

        ApplyCurrentVisual();
    }

    private void AdvanceIdleFrames()
    {
        if (idleFrames == null || idleFrames.Length <= 1)
        {
            return;
        }

        float frameDuration = 1f / idleFramesPerSecond;
        idleFrameTimer += Time.deltaTime;

        while (idleFrameTimer >= frameDuration)
        {
            idleFrameTimer -= frameDuration;
            currentIdleFrameIndex = (currentIdleFrameIndex + 1) % idleFrames.Length;
        }
    }

    private void ApplyCurrentVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite targetSprite = ResolveSprite();
        if (targetSprite != null)
        {
            spriteRenderer.sprite = targetSprite;
        }

        spriteRenderer.flipX = false;
    }

    private Sprite ResolveSprite()
    {
        if (enemyController != null && enemyController.VisualVelocity.sqrMagnitude > moveDeadZone * moveDeadZone)
        {
            Sprite moveSprite = facingRight ? moveRightSprite : moveLeftSprite;
            if (moveSprite != null)
            {
                return moveSprite;
            }

            return facingRight ? moveRightSprite : moveLeftSprite;
        }

        if (idleFrames != null && idleFrames.Length > 0)
        {
            currentIdleFrameIndex = Mathf.Clamp(currentIdleFrameIndex, 0, idleFrames.Length - 1);
            return idleFrames[currentIdleFrameIndex];
        }

        return facingRight ? moveRightSprite : moveLeftSprite;
    }
}
