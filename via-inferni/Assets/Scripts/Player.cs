using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum PlayerFormType
{
    Melee,
    Ranged
}

public class Player : MonoBehaviour, IDamageable
{
    private const int SoulHealCost = 10;
    private const float SoulHealAmount = 1f;
    private const int SoulDebugStep = 10;
    private static readonly Color DodgeTextColor = new Color(0.45f, 1f, 0.95f, 1f);

    [SerializeField] private float moveSpeed = 5f;
    [Header("Movement Feel")]
    [SerializeField] private float acceleration = 28f;
    [SerializeField] private float deceleration = 36f;

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
    [SerializeField] private string interactActionName = "Interact";

    [Header("Inventory Input")]
    [SerializeField] private float pickupInteractionRadius = 1.5f;
    [SerializeField] private float dropSpawnDistance = 0.8f;
    [SerializeField] private GameObject inventoryPickupPrefab;

    [Header("Special Items - Dash")]
    [SerializeField] private float dashSpeedMultiplier = 4.5f;
    [SerializeField] private float dashDuration = 0.14f;
    [SerializeField] private float dashCooldown = 2f;
    [SerializeField] private Vector3 dodgeTextOffset = new Vector3(0f, 1.1f, 0f);

    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 currentVelocity;
    private InputAction moveAction;
    private InputAction swapAction;
    private InputAction interactAction;
    private bool swapRequested;
    private float lastSwapTime = -999f;
    private PlayerStats playerStats;
    private PlayerInventory playerInventory;
    private SpriteDamageFlash damageFlash;
    private PlayerDashTrailVisual dashTrailVisual;
    private PlayerMoveDustVisual moveDustVisual;
    private ShieldVisual shieldVisual;
    private Vector2 facingDirection = Vector2.right;

    // Dash state
    private float dashActiveUntil = -999f;
    private float dashCooldownUntil = -999f;
    private bool wasDashingLastFrame;

    // Cloak state
    private bool cloakActive = false;

    // Shield state: one absorb per room
    private bool shieldAvailable = false;

    public bool HasShieldAvailable => shieldAvailable;

    public void ActivateShield()
    {
        shieldAvailable = true;
        if (shieldVisual != null)
        {
            shieldVisual.ShowShield();
        }
    }

    public void DeactivateShield()
    {
        shieldAvailable = false;
        if (shieldVisual != null)
        {
            shieldVisual.HideShield();
        }
    }

    public void RechargeShieldIfOwned()
    {
        if (playerInventory != null && playerInventory.HasSpecialType(SpecialItemType.Shield))
        {
            ActivateShield();
        }
    }

    public bool IsCloaked => cloakActive;

    public void ActivateCloak()
    {
        cloakActive = true;
    }

    public void DeactivateCloak()
    {
        cloakActive = false;
    }

    public void RechargeCloakIfOwned()
    {
        if (playerInventory != null && playerInventory.HasSpecialType(SpecialItemType.Cloak))
        {
            ActivateCloak();
        }
    }

    public void NotifyDealtDamage()
    {
        if (cloakActive)
        {
            DeactivateCloak();
        }
    }

    public event Action<PlayerFormType> OnFormChanged;

    public PlayerFormType CurrentForm { get; private set; }
    public float CurrentHealth => playerStats != null ? playerStats.CurrentHealth : 0f;
    public float MaxHealth => playerStats != null ? playerStats.MaxHealth : 0f;
    public PlayerStats Stats => playerStats;
    public bool CanTakeDamage => playerStats != null && playerStats.IsAlive;
    public Vector2 MovementInput => movement;
    public bool IsMoving => movement.sqrMagnitude > 0.0001f;
    public Vector2 FacingDirection => facingDirection;

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

        playerInventory = GetComponent<PlayerInventory>();
        if (playerInventory == null)
        {
            playerInventory = gameObject.AddComponent<PlayerInventory>();
        }

        damageFlash = GetComponent<SpriteDamageFlash>();
        if (damageFlash == null)
        {
            damageFlash = gameObject.AddComponent<SpriteDamageFlash>();
        }

        dashTrailVisual = GetComponent<PlayerDashTrailVisual>();
        if (dashTrailVisual == null)
        {
            dashTrailVisual = gameObject.AddComponent<PlayerDashTrailVisual>();
        }

        moveDustVisual = GetComponent<PlayerMoveDustVisual>();
        if (moveDustVisual == null)
        {
            moveDustVisual = gameObject.AddComponent<PlayerMoveDustVisual>();
        }

        shieldVisual = GetComponent<ShieldVisual>();
        if (shieldVisual == null)
        {
            shieldVisual = gameObject.AddComponent<ShieldVisual>();
        }

        ConfigureInputActions();
        SetForm(startingForm, force: true);
    }

    private void OnEnable()
    {
        moveAction?.Enable();
        swapAction?.Enable();
        interactAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
        swapAction?.Disable();
        interactAction?.Disable();
        wasDashingLastFrame = false;
    }

    private void OnDestroy()
    {
        // Dispose solo si son acciones runtime creadas por fallback.
        if (inputActionsAsset == null)
        {
            moveAction?.Dispose();
            swapAction?.Dispose();
            interactAction?.Dispose();
        }
    }

    private void Update()
    {
        if (PauseMenuController.IsPaused)
        {
            movement = Vector2.zero;
            return;
        }

        movement = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        UpdateFacingDirection(movement);

        HandleWeaponSlotInput();
        HandleInventoryInput();
        HandleSoulInput();
        HandleDashInput();
        HandleDebugInput();
        HandleDashTrail();

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
        if (PauseMenuController.IsPaused)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float moveSpeedMultiplier = playerStats != null ? playerStats.MoveSpeedMultiplier : 1f;
        bool isDashing = Time.time < dashActiveUntil;
        float dashMult = isDashing ? dashSpeedMultiplier : 1f;
        float finalMoveSpeed = moveSpeed * moveSpeedMultiplier * dashMult;

        Vector2 inputDir;
        if (movement.sqrMagnitude > 0.0001f)
        {
            inputDir = Vector2.ClampMagnitude(movement, 1f);
        }
        else if (isDashing)
        {
            // If dash starts without movement input, dash in the facing direction.
            inputDir = facingDirection.sqrMagnitude > 0.0001f ? facingDirection.normalized : Vector2.right;
        }
        else
        {
            inputDir = Vector2.zero;
        }

        Vector2 targetVelocity = inputDir * finalMoveSpeed;
        float rate = targetVelocity.sqrMagnitude > 0.0001f ? acceleration : deceleration;

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            rate * Time.fixedDeltaTime
        );

        rb.linearVelocity = currentVelocity;
        moveDustVisual?.Tick(currentVelocity, !isDashing && inputDir.sqrMagnitude > 0.0001f);
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

    private void HandleDashInput()
    {
        if (playerInventory == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool shiftPressed = keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;
        if (!shiftPressed)
        {
            return;
        }

        // Check if player has dash special
        if (!playerInventory.HasSpecialType(SpecialItemType.Dash))
        {
            return;
        }

        // Cooldown check
        if (Time.time < dashCooldownUntil)
        {
            return;
        }

        // Activate dash
        dashActiveUntil = Time.time + dashDuration;
        dashCooldownUntil = Time.time + dashCooldown;
    }

    private void HandleDashTrail()
    {
        if (dashTrailVisual == null)
        {
            return;
        }

        bool isDashing = Time.time < dashActiveUntil;
        if (!isDashing)
        {
            wasDashingLastFrame = false;
            return;
        }

        if (!wasDashingLastFrame)
        {
            dashTrailVisual.Play(facingDirection, dashDuration);
        }

        wasDashingLastFrame = true;
    }

    private void HandleSoulInput()
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

        if (keyboard.zKey.wasPressedThisFrame)
        {
            playerStats.AddSoul(SoulDebugStep);
        }

        if (keyboard.xKey.wasPressedThisFrame)
        {
            playerStats.SpendSoul(SoulDebugStep);
        }

        bool ctrlPressedThisFrame = keyboard.leftCtrlKey.wasPressedThisFrame || keyboard.rightCtrlKey.wasPressedThisFrame;
        if (!ctrlPressedThisFrame)
        {
            return;
        }

        if (CurrentHealth >= MaxHealth)
        {
            return;
        }

        if (playerStats.SpendSoul(SoulHealCost))
        {
            playerStats.RestoreHealth(SoulHealAmount);
        }
    }

    private void HandleDebugInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // Ctrl + N: Debug - Pasar al siguiente círculo
        bool ctrlPressed = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        if (ctrlPressed && keyboard.nKey.wasPressedThisFrame)
        {
            if (CircleManager.instance != null)
            {
                CircleManager.instance.DescendToNextCircle();
                Debug.Log("[DEBUG] Saltando al siguiente círculo (Ctrl+N)");
            }
        }
    }

    private void HandleInventoryInput()
    {
        if (playerInventory == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool altPressedThisFrame = keyboard.leftAltKey.wasPressedThisFrame || keyboard.rightAltKey.wasPressedThisFrame;
        if (altPressedThisFrame)
        {
            playerInventory.SelectNextSlotCyclic();
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            TryDropSelectedInventoryItem();
        }

        bool interactPressed = (interactAction != null && interactAction.WasPressedThisFrame()) || keyboard.eKey.wasPressedThisFrame;
        if (!interactPressed)
        {
            return;
        }

        TryCollectNearestInventoryPickup();
    }

    private bool TryCollectNearestInventoryPickup()
    {
        if (playerInventory == null)
        {
            return false;
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, pickupInteractionRadius);
        if (hitColliders == null || hitColliders.Length == 0)
        {
            return false;
        }

        IInventoryPickup nearestPickup = null;
        float nearestDistanceSq = float.MaxValue;

        for (int i = 0; i < hitColliders.Length; i++)
        {
            Collider2D hit = hitColliders[i];
            if (hit == null || !hit.TryGetInventoryPickup(out IInventoryPickup pickup))
            {
                continue;
            }

            Vector3 pickupPosition = pickup.PickupTransform != null
                ? pickup.PickupTransform.position
                : hit.transform.position;

            float distanceSq = (pickupPosition - transform.position).sqrMagnitude;
            if (distanceSq >= nearestDistanceSq)
            {
                continue;
            }

            nearestDistanceSq = distanceSq;
            nearestPickup = pickup;
        }

        if (nearestPickup == null)
        {
            return false;
        }

        return nearestPickup.TryPickup(playerInventory, out _);
    }

    private bool TryDropSelectedInventoryItem()
    {
        if (playerInventory == null)
        {
            return false;
        }

        if (!playerInventory.TryDropSelected(out InventoryItemDefinition droppedItem, out _))
        {
            return false;
        }

        Vector3 spawnOffset = (Vector3)(facingDirection.sqrMagnitude > 0.001f ? facingDirection.normalized : Vector2.right) * Mathf.Max(0.1f, dropSpawnDistance);
        Vector3 spawnPosition = transform.position + spawnOffset;

        WorldInventoryPickup.Spawn(droppedItem, spawnPosition, inventoryPickupPrefab);
        return true;
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

    public void TakeDamage(float amount, GameObject source = null)
    {
        if (playerStats == null || amount <= 0f)
        {
            return;
        }

        // If player is cloaked, they should not receive damage
        if (cloakActive)
        {
            return;
        }

        // If shield is available this room, absorb the hit and mark shield used
        if (shieldAvailable)
        {
            shieldAvailable = false;
            DeactivateShield();
            return;
        }

        if (playerStats.RollDodge())
        {
            CombatFeedbackText.Spawn("DODGE", transform.position + dodgeTextOffset, DodgeTextColor, 1f);
            return;
        }

        // Apply damage and capture actual damage taken (PlayerStats.TakeDamage returns actual amount)
        float damageTaken = playerStats.TakeDamage(amount);

        if (damageTaken > 0f && damageFlash != null)
        {
            damageFlash.PlayFlash();
        }

        // Cursed Coin: gain souls when taking damage while owning the special
        if (damageTaken > 0f && playerInventory != null && playerInventory.HasSpecialType(SpecialItemType.CursedCoin))
        {
            int soulsGained = Mathf.FloorToInt(damageTaken) * 5; // 5 souls per 1 HP lost
            if (soulsGained > 0)
            {
                playerStats.AddSoul(soulsGained);
            }
        }
    }

    public void ApplyDamage(float amount)
    {
        TakeDamage(amount);
    }

    public void TeleportTo(Vector2 position)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        movement = Vector2.zero;
        currentVelocity = Vector2.zero;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = position;
            return;
        }

        transform.position = position;
    }

    public void RestoreHealth(float amount)
    {
        if (playerStats == null || amount <= 0f)
        {
            return;
        }

        playerStats.RestoreHealth(amount);
    }

    public bool IsDead()
    {
        return playerStats == null || !playerStats.IsAlive;
    }

    public void HealToFull()
    {
        if (playerStats != null)
        {
            playerStats.HealToFull();
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
        RefreshDashTrailVisual();
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

    private void RefreshDashTrailVisual()
    {
        if (dashTrailVisual == null)
        {
            return;
        }

        dashTrailVisual.Configure(GetCurrentFormSpriteRenderer());
    }

    private SpriteRenderer GetCurrentFormSpriteRenderer()
    {
        GameObject currentVisual = CurrentForm == PlayerFormType.Melee ? meleeVisual : rangedVisual;
        if (currentVisual == null)
        {
            currentVisual = meleeVisual != null ? meleeVisual : rangedVisual;
        }

        if (currentVisual == null)
        {
            return null;
        }

        return currentVisual.GetComponent<SpriteRenderer>();
    }

    private void UpdateFacingDirection(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        if (Mathf.Abs(input.x) >= Mathf.Abs(input.y))
        {
            facingDirection = input.x >= 0f ? Vector2.right : Vector2.left;
            return;
        }

        facingDirection = input.y >= 0f ? Vector2.up : Vector2.down;
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
        moveAction.AddBinding("<Gamepad>/leftStick");

        swapAction = new InputAction(name: "Swap", type: InputActionType.Button);
        swapAction.AddBinding("<Keyboard>/tab");
        swapAction.AddBinding("<Gamepad>/rightShoulder");

        interactAction = new InputAction(name: "Interact", type: InputActionType.Button);
        interactAction.AddBinding("<Keyboard>/e");
        interactAction.AddBinding("<Gamepad>/buttonNorth");
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
        interactAction = actionMap.FindAction(interactActionName, throwIfNotFound: false);

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

        if (interactAction == null)
        {
            Debug.LogWarning($"Player: action '{interactActionName}' no encontrada. Se usará fallback runtime para Interact.");
            interactAction = new InputAction(name: "Interact", type: InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonNorth");
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, pickupInteractionRadius);
    }
}
