using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Player))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerCombatController : MonoBehaviour
{
    [Header("Attack Input")]
    [SerializeField] private InputActionAsset inputActionsAsset;
    [SerializeField] private string playerActionMapName = "Player";
    [SerializeField] private string attackActionName = "Attack";

    [Header("Combat Spawn")]
    [SerializeField] private Transform attackOrigin;
    [SerializeField] private LayerMask damageLayers = ~0;
    [SerializeField] private WeaponDefinition[] meleeWeapons = new WeaponDefinition[3];
    [SerializeField] private WeaponDefinition[] rangedWeapons = new WeaponDefinition[3];

    private Player player;
    private PlayerStats stats;
    private InputAction attackAction;
    private bool attackRequested;
    private Vector2 queuedAttackDirection = Vector2.right;
    private float lastAttackTime = -999f;

    private void Awake()
    {
        player = GetComponent<Player>();
        stats = GetComponent<PlayerStats>();
        EnsureAttackOrigin();
        ConfigureAttackInput();
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
            attackRequested = false;
            return;
        }

        if (TryReadArrowAttackInput(out Vector2 attackDirection))
        {
            attackRequested = true;
            queuedAttackDirection = attackDirection;
        }
        else if (attackAction != null && attackAction.WasPressedThisFrame())
        {
            attackRequested = true;
            queuedAttackDirection = GetDefaultAttackDirection();
        }

        if (!attackRequested)
        {
            return;
        }

        attackRequested = false;
        TryAttack(queuedAttackDirection);
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

        float finalDamage = stats != null ? stats.GetFinalDamage(player.CurrentForm) : weapon.BaseDamage;
        Vector2 facingDirection = attackDirection.sqrMagnitude > 0.0001f ? attackDirection.normalized : GetDefaultAttackDirection();

        if (weapon.AttackType == WeaponAttackType.Melee)
        {
            PerformMeleeAttack(weapon, finalDamage, facingDirection);
        }
        else
        {
            PerformProjectileAttack(weapon, finalDamage, facingDirection);
        }

        lastAttackTime = Time.time;
        return true;
    }

    private void PerformMeleeAttack(WeaponDefinition weapon, float damageAmount, Vector2 facingDirection)
    {
        if (weapon == null || player == null)
        {
            return;
        }

        SpawnMeleeSlashVisual(facingDirection, weapon);

        GameObject hitboxObject = new GameObject($"{weapon.DisplayName}_Hitbox");
        Vector2 offset = GetDirectionalOffset(weapon.HitboxOffset, facingDirection);

        hitboxObject.transform.position = player.transform.position + (Vector3)offset;
        hitboxObject.transform.rotation = Quaternion.identity;
        hitboxObject.transform.SetParent(transform, true);

        BoxCollider2D hitboxCollider = hitboxObject.AddComponent<BoxCollider2D>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.size = weapon.HitboxSize;

        MeleeHitbox hitbox = hitboxObject.AddComponent<MeleeHitbox>();
        hitbox.Initialize(damageAmount, gameObject, damageLayers);
    }

    private void PerformProjectileAttack(WeaponDefinition weapon, float damageAmount, Vector2 facingDirection)
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

        projectile.Initialize(damageAmount, weapon.ProjectileSpeed, weapon.ProjectileLifetime, facingDirection, gameObject);
    }

    private WeaponDefinition GetCurrentWeaponDefinition()
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

    private bool TryReadArrowAttackInput(out Vector2 attackDirection)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            attackDirection = Vector2.zero;
            return false;
        }

        bool pressedThisFrame = keyboard.upArrowKey.wasPressedThisFrame
            || keyboard.downArrowKey.wasPressedThisFrame
            || keyboard.leftArrowKey.wasPressedThisFrame
            || keyboard.rightArrowKey.wasPressedThisFrame;

        if (!pressedThisFrame)
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
        float slashLength = Mathf.Max(weapon.HitboxSize.x, weapon.HitboxSize.y, weapon.Range, 0.5f);
        slashVisual.Initialize(player.transform.position + (Vector3)GetDirectionalOffset(weapon.HitboxOffset, facingDirection), facingDirection, slashLength);
    }

}
