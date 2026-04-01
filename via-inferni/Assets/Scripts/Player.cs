using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerFormType
{
    Melee,
    Ranged
}

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [Header("Movement Feel")]
    [SerializeField] private float acceleration = 28f;
    [SerializeField] private float deceleration = 36f;

    [Header("Vital")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Form Swap")]
    [SerializeField] private PlayerFormType startingForm = PlayerFormType.Melee;
    [SerializeField] private float swapCooldown = 0.35f;
    [SerializeField] private GameObject meleeVisual;
    [SerializeField] private GameObject rangedVisual;

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
    private float lastSwapTime = -999f;
    private PlayerStats playerStats;

    public event Action<PlayerFormType> OnFormChanged;

    public PlayerFormType CurrentForm { get; private set; }
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public PlayerStats Stats => playerStats;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            playerStats = gameObject.AddComponent<PlayerStats>();
        }

        ConfigureInputActions();
        InitializeHealth();
        SetForm(startingForm, force: true);
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

        HandleWeaponSlotInput();

        if (swapAction != null && swapAction.WasPressedThisFrame())
        {
            swapRequested = true;
        }

        if (ConsumeSwapRequested())
        {
            TrySwapForm();
        }
    }

    private void FixedUpdate()
    {
        float moveSpeedMultiplier = playerStats != null ? playerStats.MoveSpeedMultiplier : 1f;
        float finalMoveSpeed = moveSpeed * moveSpeedMultiplier;
        Vector2 targetVelocity = Vector2.ClampMagnitude(movement, 1f) * finalMoveSpeed;
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

    private void HandleWeaponSlotInput()
    {
        if (playerStats == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            playerStats.SelectWeaponSlot(CurrentForm, 1);
            return;
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            playerStats.SelectWeaponSlot(CurrentForm, 2);
            return;
        }

        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            playerStats.SelectWeaponSlot(CurrentForm, 3);
        }
    }

    public bool TrySwapForm()
    {
        if (Time.time < lastSwapTime + swapCooldown)
        {
            return false;
        }

        PlayerFormType nextForm = CurrentForm == PlayerFormType.Melee
            ? PlayerFormType.Ranged
            : PlayerFormType.Melee;

        SetForm(nextForm, force: false);
        lastSwapTime = Time.time;
        return true;
    }

    public float GetSwapCooldownRemaining()
    {
        float remaining = (lastSwapTime + swapCooldown) - Time.time;
        return Mathf.Max(0f, remaining);
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
    }

    public void RestoreHealth(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    public bool IsDead()
    {
        return currentHealth <= 0f;
    }

    public void HealToFull()
    {
        currentHealth = maxHealth;
    }

    private void InitializeHealth()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }
    }

    private void SetForm(PlayerFormType form, bool force)
    {
        if (!force && CurrentForm == form)
        {
            return;
        }

        CurrentForm = form;
        ApplyFormVisuals(form);
        OnFormChanged?.Invoke(form);
    }

    private void ApplyFormVisuals(PlayerFormType form)
    {
        if (meleeVisual != null)
        {
            meleeVisual.SetActive(form == PlayerFormType.Melee);
        }

        if (rangedVisual != null)
        {
            rangedVisual.SetActive(form == PlayerFormType.Ranged);
        }
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
