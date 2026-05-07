using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class BaptismalFont : MonoBehaviour
{
    [Header("Soul Storage")]
    [Min(0)] [SerializeField] private int maxStoredSoul = 50;
    [Min(1)] [SerializeField] private int transferAmount = 10;

    [Header("Interaction")]
    [Min(0.1f)] [SerializeField] private float interactionRadius = 1.5f;

    [Header("Visuals")]
    [SerializeField] private Sprite fullSprite;
    [SerializeField] private Sprite emptySprite;

    private int currentStoredSoul;
    private SpriteRenderer spriteRenderer;
    private Player currentPlayer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        maxStoredSoul = Mathf.Max(0, maxStoredSoul);
        transferAmount = Mathf.Max(1, transferAmount);
        currentStoredSoul = maxStoredSoul;

        UpdateVisual();
    }

    private void OnValidate()
    {
        if (transferAmount < 1)
        {
            transferAmount = 1;
        }

        if (interactionRadius < 0.1f)
        {
            interactionRadius = 0.1f;
        }

        maxStoredSoul = Mathf.Max(0, maxStoredSoul);
        currentStoredSoul = Mathf.Clamp(currentStoredSoul, 0, maxStoredSoul);
        UpdateVisual();
    }

    private void Update()
    {
        if (PauseMenuController.IsPaused)
        {
            return;
        }

        if (currentPlayer == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                currentPlayer = playerObject.GetComponent<Player>();
            }
        }

        if (currentPlayer == null || currentStoredSoul <= 0)
        {
            return;
        }

        if (!IsPlayerInRange(currentPlayer))
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.eKey.wasPressedThisFrame)
        {
            return;
        }

        TryTransferSoul(currentPlayer);
    }

    private bool IsPlayerInRange(Player player)
    {
        if (player == null)
        {
            return false;
        }

        float maxDistance = interactionRadius;
        Vector3 fontPosition = transform.position;
        Vector3 playerPosition = player.transform.position;
        return (playerPosition - fontPosition).sqrMagnitude <= maxDistance * maxDistance;
    }

    private void TryTransferSoul(Player player)
    {
        if (player == null || currentStoredSoul <= 0)
        {
            return;
        }

        PlayerStats stats = player.Stats != null ? player.Stats : player.GetComponent<PlayerStats>();
        if (stats == null)
        {
            return;
        }

        int amountToGive = Mathf.Min(transferAmount, currentStoredSoul);
        int actualAdded = stats.AddSoul(amountToGive);
        if (actualAdded <= 0)
        {
            return;
        }

        currentStoredSoul = Mathf.Max(0, currentStoredSoul - actualAdded);
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = currentStoredSoul > 0 ? fullSprite : emptySprite;
    }
}