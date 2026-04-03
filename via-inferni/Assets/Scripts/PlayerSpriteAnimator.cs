using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerSpriteAnimator : MonoBehaviour
{
    private enum FacingMode
    {
        Side,
        Front,
        Back
    }

    [Header("References")]
    [SerializeField] private Player player;

    [Header("Playback")]
    [Min(0.01f)] [SerializeField] private float framesPerSecond = 8f;
    [Min(0f)] [SerializeField] private float inputDeadZone = 0.15f;

    [Header("Side")]
    [SerializeField] private Sprite[] sideIdleFrames;
    [SerializeField] private Sprite[] sideWalkFrames;

    [Header("Front")]
    [SerializeField] private Sprite[] frontIdleFrames;
    [SerializeField] private Sprite[] frontWalkFrames;

    [Header("Back")]
    [SerializeField] private Sprite[] backIdleFrames;
    [SerializeField] private Sprite[] backWalkFrames;

    private SpriteRenderer spriteRenderer;
    private Sprite[] currentFrames;
    private int currentFrameIndex;
    private float frameTimer;
    private FacingMode lastFacingMode = FacingMode.Side;
    private bool facingRight = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (player == null)
        {
            player = GetComponentInParent<Player>();
        }
    }

    private void OnEnable()
    {
        currentFrames = null;
        currentFrameIndex = 0;
        frameTimer = 0f;
        ApplyCurrentFrame();
    }

    private void Update()
    {
        if (PauseMenuController.IsPaused || player == null || spriteRenderer == null)
        {
            return;
        }

        Vector2 input = player.MovementInput;
        bool isMoving = input.sqrMagnitude > inputDeadZone * inputDeadZone;

        if (isMoving)
        {
            UpdateFacingMode(input);
        }

        Sprite[] frames = GetActiveFrames(isMoving);
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        if (frames != currentFrames)
        {
            currentFrames = frames;
            currentFrameIndex = 0;
            frameTimer = 0f;
            ApplyCurrentFrame();
            return;
        }

        if (currentFrames.Length == 1)
        {
            ApplyCurrentFrame();
            return;
        }

        float frameDuration = 1f / framesPerSecond;
        frameTimer += Time.deltaTime;

        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            currentFrameIndex = (currentFrameIndex + 1) % currentFrames.Length;
            ApplyCurrentFrame();
        }
    }

    private void UpdateFacingMode(Vector2 input)
    {
        if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
        {
            lastFacingMode = FacingMode.Side;
            facingRight = input.x >= 0f;
            return;
        }

        lastFacingMode = input.y >= 0f ? FacingMode.Back : FacingMode.Front;
    }

    private Sprite[] GetActiveFrames(bool isMoving)
    {
        return lastFacingMode switch
        {
            FacingMode.Side => isMoving ? sideWalkFrames : sideIdleFrames,
            FacingMode.Front => isMoving ? frontWalkFrames : frontIdleFrames,
            FacingMode.Back => isMoving ? backWalkFrames : backIdleFrames,
            _ => sideIdleFrames
        };
    }

    private void ApplyCurrentFrame()
    {
        if (spriteRenderer == null || currentFrames == null || currentFrames.Length == 0)
        {
            return;
        }

        currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, currentFrames.Length - 1);
        spriteRenderer.sprite = currentFrames[currentFrameIndex];
        spriteRenderer.flipX = lastFacingMode == FacingMode.Side && !facingRight;
    }
}