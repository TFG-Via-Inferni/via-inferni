using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerCombatController : MonoBehaviour
{
    public event Action<WeaponDefinition, Vector2, PlayerFormType> OnAttackPerformed;

    [Header("Attack Input")]
    [SerializeField] private InputActionAsset inputActionsAsset;
    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string attackActionName = "Attack";

    [Header("Combat Spawn")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private LayerMask damageLayers = ~0;
    [SerializeField] private WeaponDefinition[] meleeWeapons = new WeaponDefinition[3];
    [SerializeField] private WeaponDefinition[] rangedWeapons = new WeaponDefinition[3];

    [Header("Combat Rolls")]
    [SerializeField] private float criticalDamageMultiplier = 1.75f;

    private Player player;
    private PlayerStats stats;
    private InputAction attackAction;
    private float lastAttackTime = -999f;
    private bool isChargingAttack;
    private float chargeStartedAt;
    private WeaponDefinition chargingWeapon;
    private Vector2 chargingDirection = Vector2.right;
    private string chargingWeaponId;
    private PlayerFormType chargingForm;

    public bool IsChargingAttack => isChargingAttack;
    public WeaponDefinition ChargingWeapon => chargingWeapon;
    public PlayerFormType ChargingForm => chargingForm;
    public Vector2 ChargingDirection => chargingDirection;
    public float CurrentChargeNormalized
    {
        get
        {
            if (!isChargingAttack || chargingWeapon == null)
            {
                return 0f;
            }

            float duration = Time.time - chargeStartedAt;
            return Mathf.Clamp01(duration / Mathf.Max(0.1f, chargingWeapon.ChargeTimeToMax));
        }
    }

    private struct AttackInputState
    {
        public bool PressedThisFrame;
        public bool Held;
        public Vector2 Direction;
    }

    private void Awake()
    {
        player = GetComponent<Player>();
        stats = GetComponent<PlayerStats>();
        EnsureAttackOrigin();
        ConfigureAttackInput();
        EnsureChargeVisual();
    }

    private void OnEnable()
    {
        attackAction?.Enable();
    }

    private void OnDisable()
    {
        attackAction?.Disable();
    }

    private void OnDestroy()
    {
        if (inputActionsAsset == null)
        {
            attackAction?.Dispose();
        }
    }

    private void Update()
    {
        if (PauseMenuController.IsPaused)
        {
            ResetChargeState();
            return;
        }

        WeaponDefinition weapon = GetCurrentWeaponDefinition();
        if (weapon == null)
        {
            ResetChargeState();
            return;
        }

        AttackInputState input = ReadAttackInputState();

        if (weapon.RequiresChargeAttack)
        {
            HandleChargeAttackInput(weapon, input);
            return;
        }

        if (isChargingAttack)
        {
            ResetChargeState();
        }

        if (!input.PressedThisFrame)
        {
            return;
        }

        TryAttack(input.Direction);
    }

    public bool TryAttack(Vector2 attackDirection)
    {
        WeaponDefinition weapon = GetCurrentWeaponDefinition();
        if (weapon == null)
        {
            return false;
        }

        if (Time.time < lastAttackTime + weapon.Cooldown)
        {
            return false;
        }

        float damageMultiplier = stats != null ? stats.DamageMultiplier : 1f;
        float finalDamage = weapon.BaseDamage * Mathf.Max(0.01f, damageMultiplier);
        bool isCritical = stats != null && stats.RollCritical();
        if (isCritical)
        {
            finalDamage *= Mathf.Max(1f, criticalDamageMultiplier);
        }
        Vector2 facingDirection = attackDirection.sqrMagnitude > 0.0001f ? attackDirection.normalized : GetDefaultAttackDirection();
        string attackWeaponId = stats != null
            ? stats.GetSelectedWeaponId(player.CurrentForm)
            : weapon.WeaponId;

        if (weapon.AttackType == WeaponAttackType.Melee)
        {
            PerformMeleeAttack(weapon, finalDamage, facingDirection, attackWeaponId, player.CurrentForm, isCritical);
        }
        else
        {
            PerformProjectileAttack(weapon, finalDamage, facingDirection, attackWeaponId, player.CurrentForm, isCritical);
        }

        OnAttackPerformed?.Invoke(weapon, facingDirection, player.CurrentForm);

        lastAttackTime = Time.time;
        return true;
    }

    private void PerformMeleeAttack(WeaponDefinition weapon, float damageAmount, Vector2 facingDirection, string weaponId, PlayerFormType form, bool isCritical)
    {
        if (weapon == null || player == null)
        {
            return;
        }

        SpawnMeleeSlashVisual(facingDirection, weapon);

        GameObject hitboxObject = new GameObject($"{weapon.DisplayName}_Hitbox");
        Vector2 offset = GetMeleeAttackOffset(weapon, facingDirection);

        hitboxObject.transform.position = player.transform.position + (Vector3)offset;
        hitboxObject.transform.rotation = Quaternion.identity;
        hitboxObject.transform.SetParent(transform, true);

        BoxCollider2D hitboxCollider = hitboxObject.AddComponent<BoxCollider2D>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.size = weapon.HitboxSize;

        DamageSourceContext context = hitboxObject.AddComponent<DamageSourceContext>();
        context.Configure(player.transform.root, weaponId, form, weapon, isCritical);

        MeleeHitbox hitbox = hitboxObject.AddComponent<MeleeHitbox>();
        hitbox.Initialize(damageAmount, hitboxObject, damageLayers, weapon.MeleeAppliesKnockback, weapon.MeleeKnockbackForce);
    }

    private void PerformProjectileAttack(WeaponDefinition weapon, float damageAmount, Vector2 facingDirection, string weaponId, PlayerFormType form, bool isCritical)
    {
        if (weapon == null || weapon.ProjectilePrefab == null || player == null)
        {
            return;
        }

        Vector2 spawnOffset = GetDirectionalOffset(weapon.ProjectileSpawnOffset, facingDirection);

        Vector2 spawnPosition = (Vector2)player.transform.position + spawnOffset;
        GameObject projectileObject = Instantiate(weapon.ProjectilePrefab, spawnPosition, Quaternion.identity);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            projectile = projectileObject.AddComponent<Projectile>();
        }

        DamageSourceContext context = projectileObject.GetComponent<DamageSourceContext>();
        if (context == null)
        {
            context = projectileObject.AddComponent<DamageSourceContext>();
        }

        context.Configure(player.transform.root, weaponId, form, weapon, isCritical);

        projectile.Initialize(
            damageAmount,
            weapon.ProjectileSpeed,
            weapon.ProjectileLifetime,
            facingDirection,
            projectileObject,
            weapon.ProjectileHoming,
            weapon.ProjectileHomingTurnSpeed,
            weapon.ProjectileHomingSearchRadius
        );
    }

    private void HandleChargeAttackInput(WeaponDefinition weapon, AttackInputState input)
    {
        if (!isChargingAttack)
        {
            if (!input.PressedThisFrame)
            {
                return;
            }

            if (Time.time < lastAttackTime + weapon.Cooldown)
            {
                return;
            }

            isChargingAttack = true;
            chargeStartedAt = Time.time;
            chargingWeapon = weapon;
            chargingDirection = input.Direction;
            chargingWeaponId = stats != null
                ? stats.GetSelectedWeaponId(player.CurrentForm)
                : weapon.WeaponId;
            chargingForm = player.CurrentForm;
            return;
        }

        if (chargingWeapon != weapon || chargingForm != player.CurrentForm)
        {
            ResetChargeState();
            return;
        }

        if (input.Held)
        {
            chargingDirection = input.Direction.sqrMagnitude > 0.0001f
                ? input.Direction.normalized
                : GetDefaultAttackDirection();
            return;
        }

        FireChargedAttack();
    }

    private void FireChargedAttack()
    {
        if (!isChargingAttack || chargingWeapon == null || player == null)
        {
            ResetChargeState();
            return;
        }

        float chargeDuration = Time.time - chargeStartedAt;
        float normalizedCharge = Mathf.Clamp01(chargeDuration / Mathf.Max(0.1f, chargingWeapon.ChargeTimeToMax));
        float chargeMultiplier = Mathf.Lerp(chargingWeapon.ChargeMinDamageMultiplier, chargingWeapon.ChargeMaxDamageMultiplier, normalizedCharge);
        float damageMultiplier = stats != null ? stats.DamageMultiplier : 1f;
        float baseDamage = chargingWeapon.BaseDamage * Mathf.Max(0.01f, damageMultiplier);
        float chargedDamage = baseDamage * chargeMultiplier;
        bool isCritical = stats != null && stats.RollCritical();
        if (isCritical)
        {
            chargedDamage *= Mathf.Max(1f, criticalDamageMultiplier);
        }

        PerformProjectileAttack(chargingWeapon, chargedDamage, chargingDirection, chargingWeaponId, chargingForm, isCritical);
        lastAttackTime = Time.time;
        ResetChargeState();
    }

    private void ResetChargeState()
    {
        isChargingAttack = false;
        chargeStartedAt = 0f;
        chargingWeapon = null;
        chargingDirection = Vector2.right;
        chargingWeaponId = string.Empty;
        chargingForm = PlayerFormType.Melee;
    }

    public WeaponDefinition GetCurrentWeaponDefinition()
    {
        if (stats == null || player == null)
        {
            return null;
        }

        int index = stats.GetSelectedWeaponIndex(player.CurrentForm);
        WeaponDefinition[] pool = player.CurrentForm == PlayerFormType.Melee ? meleeWeapons : rangedWeapons;

        if (pool == null || pool.Length == 0)
        {
            return null;
        }

        index = Mathf.Clamp(index, 0, pool.Length - 1);
        return pool[index];
    }

    private AttackInputState ReadAttackInputState()
    {
        AttackInputState state = new AttackInputState
        {
            Direction = GetDefaultAttackDirection()
        };

        bool hasDirectionalInput = TryReadArrowAttackInput(out Vector2 arrowDirection);
        if (hasDirectionalInput)
        {
            state.Direction = arrowDirection;
        }

        Keyboard keyboard = Keyboard.current;
        bool keyboardAttackHeld = false;
        bool keyboardDirectionPressedThisFrame = false;
        if (keyboard != null)
        {
            keyboardDirectionPressedThisFrame = keyboard.upArrowKey.wasPressedThisFrame
                || keyboard.downArrowKey.wasPressedThisFrame
                || keyboard.leftArrowKey.wasPressedThisFrame
                || keyboard.rightArrowKey.wasPressedThisFrame;

            keyboardAttackHeld = keyboard.upArrowKey.isPressed
                || keyboard.downArrowKey.isPressed
                || keyboard.leftArrowKey.isPressed
                || keyboard.rightArrowKey.isPressed;
        }

        bool actionPressedThisFrame = attackAction != null && attackAction.WasPressedThisFrame();
        bool actionHeld = attackAction != null && attackAction.IsPressed();

        state.PressedThisFrame = keyboardDirectionPressedThisFrame || actionPressedThisFrame;
        state.Held = keyboardAttackHeld || actionHeld;

        if (state.Direction.sqrMagnitude <= 0.0001f)
        {
            state.Direction = GetDefaultAttackDirection();
        }

        return state;
    }

    private void ConfigureAttackInput()
    {
        if (TryBindFromAsset())
        {
            return;
        }

        attackAction = new InputAction(name: "Attack", type: InputActionType.Button);
        attackAction.AddBinding("<Keyboard>/upArrow");
        attackAction.AddBinding("<Keyboard>/downArrow");
        attackAction.AddBinding("<Keyboard>/leftArrow");
        attackAction.AddBinding("<Keyboard>/rightArrow");
    }

    private bool TryBindFromAsset()
    {
        if (inputActionsAsset == null)
        {
            return false;
        }

        InputActionMap actionMap = inputActionsAsset.FindActionMap(playerActionMapName, throwIfNotFound: false);
        if (actionMap == null)
        {
            return false;
        }

        attackAction = actionMap.FindAction(attackActionName, throwIfNotFound: false);
        return attackAction != null;
    }

    private void EnsureAttackOrigin()
    {
        if (attackOrigin != null)
        {
            attackOrigin.localPosition = Vector3.zero;
            return;
        }

        Transform existing = transform.Find("AttackOrigin");
        if (existing != null)
        {
            attackOrigin = existing;
            attackOrigin.localPosition = Vector3.zero;
            return;
        }

        GameObject originObject = new GameObject("AttackOrigin");
        originObject.transform.SetParent(transform, false);
        originObject.transform.localPosition = Vector3.zero;
        attackOrigin = originObject.transform;
    }

    private void EnsureChargeVisual()
    {
        PlayerChargeAttackVisual chargeVisual = GetComponent<PlayerChargeAttackVisual>();
        if (chargeVisual == null)
        {
            gameObject.AddComponent<PlayerChargeAttackVisual>();
        }
    }

    private bool TryReadArrowAttackInput(out Vector2 attackDirection)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            attackDirection = Vector2.zero;
            return false;
        }

        bool anyDirectionPressed = keyboard.upArrowKey.isPressed
            || keyboard.downArrowKey.isPressed
            || keyboard.leftArrowKey.isPressed
            || keyboard.rightArrowKey.isPressed;

        if (!anyDirectionPressed)
        {
            attackDirection = Vector2.zero;
            return false;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (keyboard.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (keyboard.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        attackDirection = new Vector2(horizontal, vertical);
        if (attackDirection.sqrMagnitude < 0.0001f)
        {
            attackDirection = GetDefaultAttackDirection();
        }

        return true;
    }

    private Vector2 GetDefaultAttackDirection()
    {
        if (player != null && player.FacingDirection.sqrMagnitude > 0.0001f)
        {
            return player.FacingDirection.normalized;
        }

        return Vector2.right;
    }

    private static Vector2 GetDirectionalOffset(Vector2 baseOffset, Vector2 direction)
    {
        Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector2.right;

        float offsetMagnitude = Mathf.Max(Mathf.Abs(baseOffset.x), Mathf.Abs(baseOffset.y), 0.1f);
        return normalizedDirection * offsetMagnitude;
    }

    private void SpawnMeleeSlashVisual(Vector2 facingDirection, WeaponDefinition weapon)
    {
        GameObject slashObject = new GameObject($"{weapon.DisplayName}_SlashVisual");
        slashObject.transform.position = player.transform.position;
        slashObject.transform.SetParent(transform, true);

        MeleeSlashVisual slashVisual = slashObject.AddComponent<MeleeSlashVisual>();
        Vector2 slashStart = player.transform.position;
        Vector2 slashEnd = slashStart + GetMeleeSlashOffset(weapon, facingDirection);
        slashVisual.Initialize(slashStart, slashEnd, weapon);
    }

    private static Vector2 GetMeleeSlashOffset(WeaponDefinition weapon, Vector2 direction)
    {
        if (weapon == null)
        {
            return Vector2.zero;
        }

        Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector2.right;

        float fallbackReach = Mathf.Lerp(weapon.HitboxOffset.magnitude, weapon.Range, 0.35f);
        float configuredReach = weapon.MeleeSlashVisualReach;
        float reach = configuredReach > 0f ? configuredReach : fallbackReach;
        reach = Mathf.Max(reach, 0.1f);
        return normalizedDirection * reach;
    }

    private static Vector2 GetMeleeAttackOffset(WeaponDefinition weapon, Vector2 direction)
    {
        if (weapon == null)
        {
            return Vector2.zero;
        }

        Vector2 normalizedDirection = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector2.right;

        float reach = Mathf.Max(weapon.Range, weapon.HitboxOffset.magnitude, 0.1f);
        return normalizedDirection * reach;
    }

}
