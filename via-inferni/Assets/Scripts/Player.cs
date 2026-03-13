using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [Header("Movement Feel")]
    [SerializeField] private float acceleration = 28f;
    [SerializeField] private float deceleration = 36f;
    [Header("Input System (optional)")]
    [SerializeField] private InputActionAsset inputActionsAsset;
    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string swapActionName = "Swap";

    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 currentVelocity;
    private InputAction moveAction;
    private InputAction swapAction;
    private bool swapRequested;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        ConfigureInputActions();
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        swapAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        swapAction?.Disable();
    }

    private void OnDestroy()
    {
        // Dispose solo si son acciones runtime creadas por fallback.
        if (inputActionsAsset == null)
        {
            moveAction?.Dispose();
            swapAction?.Dispose();
        }
    }

    private void Update()
    {
        movement = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        if (swapAction != null && swapAction.WasPressedThisFrame())
        {
            swapRequested = true;
        }
    }

    private void FixedUpdate()
    {
        Vector2 targetVelocity = Vector2.ClampMagnitude(movement, 1f) * moveSpeed;
        float rate = targetVelocity.sqrMagnitude > 0.0001f ? acceleration : deceleration;

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            rate * Time.fixedDeltaTime
        );

        rb.linearVelocity = currentVelocity;
    }

    public bool ConsumeSwapRequested()
    {
        if (!swapRequested)
        {
            return false;
        }

        swapRequested = false;
        return true;
    }

    private void ConfigureInputActions()
    {
        if (TryBindFromAsset())
        {
            return;
        }

        // Fallback robusto para no depender de config en escena.
        moveAction = new InputAction(name: "Move", type: InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        swapAction = new InputAction(name: "Swap", type: InputActionType.Button);
        swapAction.AddBinding("<Keyboard>/tab");
        swapAction.AddBinding("<Gamepad>/rightShoulder");
    }

    private bool TryBindFromAsset()
    {
        if (inputActionsAsset == null)
        {
            return false;
        }

        var actionMap = inputActionsAsset.FindActionMap(playerActionMapName, throwIfNotFound: false);
        if (actionMap == null)
        {
            Debug.LogWarning($"Player: action map '{playerActionMapName}' no encontrado. Se usará fallback runtime.");
            return false;
        }

        moveAction = actionMap.FindAction(moveActionName, throwIfNotFound: false);
        swapAction = actionMap.FindAction(swapActionName, throwIfNotFound: false);

        if (moveAction == null)
        {
            Debug.LogWarning($"Player: action '{moveActionName}' no encontrada. Se usará fallback runtime.");
            return false;
        }

        if (swapAction == null)
        {
            Debug.LogWarning($"Player: action '{swapActionName}' no encontrada. Se usará fallback runtime para Swap.");
            // Solo hacemos fallback de swap para mantener consistencia con el asset actual.
            swapAction = new InputAction(name: "Swap", type: InputActionType.Button);
            swapAction.AddBinding("<Keyboard>/tab");
            swapAction.AddBinding("<Gamepad>/rightShoulder");
        }

        return true;
    }
}
