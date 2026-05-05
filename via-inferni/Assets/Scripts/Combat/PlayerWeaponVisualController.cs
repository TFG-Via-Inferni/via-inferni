using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerWeaponVisualController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private PlayerCombatController combatController;

    [Header("Motion")]
    [Min(0f)] [SerializeField] private float movementSwayOffset = 0.04f;
    [Min(0f)] [SerializeField] private float movementSwayRotation = 10f;
    [Min(0f)] [SerializeField] private float attackPulseDistance = 0.09f;
    [Min(0.01f)] [SerializeField] private float attackPulseDuration = 0.35f;
    [Min(0f)] [SerializeField] private float attackPulseScale = 0.05f;
    [Min(0f)] [SerializeField] private float weaponForwardOffset = 2f;
    [Min(0f)] [SerializeField] private float swordDefaultArc = 90f;
    [Min(0f)] [SerializeField] private float axeDefaultArc = 45f;
    [Min(0f)] [SerializeField] private float spearDefaultLungeMultiplier = 1.35f;

    private SpriteRenderer spriteRenderer;
    private WeaponDefinition currentWeapon;
    private Vector2 currentBaseOffset;
    private Vector2 currentBaseScale = Vector2.one;
    private float currentBaseRotation;
    private Vector2 attackDirection = Vector2.right;
    private float attackPulseTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (player == null)
        {
            player = GetComponentInParent<Player>();
        }

        if (combatController == null)
        {
            combatController = GetComponentInParent<PlayerCombatController>();
        }
    }

    private void OnEnable()
    {
        if (combatController != null)
        {
            combatController.OnAttackPerformed += HandleAttackPerformed;
        }

        RefreshWeaponVisual(force: true);
    }

    private void OnDisable()
    {
        if (combatController != null)
        {
            combatController.OnAttackPerformed -= HandleAttackPerformed;
        }
    }

    private void Update()
    {
        if (PauseMenuController.IsPaused)
        {
            return;
        }

        RefreshWeaponVisual(force: false);

        if (attackPulseTimer > 0f)
        {
            attackPulseTimer = Mathf.Max(0f, attackPulseTimer - Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (spriteRenderer == null || player == null)
        {
            return;
        }

        if (currentWeapon == null || currentWeapon.VisualSprite == null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.enabled = true;

        Vector2 facing = GetFacingDirection();
        // Calculate rotation angle based on facing direction
        float facingAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        Vector2 directionalBaseOffset = GetDirectionalBaseOffset(facing);
        
        Vector2 attackOffset = Vector2.zero;
        float attackScale = 1f;
        float attackAngle = 0f;

        if (attackPulseTimer > 0f)
        {
            float duration = Mathf.Max(0.01f, Mathf.Max(currentWeapon.AttackLungeDuration, attackPulseDuration));
            float normalized = 1f - Mathf.Clamp01(attackPulseTimer / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 2f);

            float lunge = Mathf.Max(0f, currentWeapon.AttackLungeDistance + attackPulseDistance);
            float lungeMultiplier = Mathf.Max(0f, currentWeapon.AttackLungeMultiplier);
            float attackProgress = eased;
            WeaponAttackMotionStyle motionStyle = currentWeapon.GetResolvedAttackMotionStyle();
            Vector2 attackFacing = attackDirection.sqrMagnitude > 0.0001f ? attackDirection.normalized : facing;

            switch (motionStyle)
            {
                case WeaponAttackMotionStyle.Thrust:
                    attackOffset = attackFacing * (lunge * Mathf.Max(1f, spearDefaultLungeMultiplier) * attackProgress);
                    attackAngle = 0f;
                    break;
                case WeaponAttackMotionStyle.Chop:
                {
                    float arc = currentWeapon.AttackArcDegrees > 0f ? currentWeapon.AttackArcDegrees : axeDefaultArc;
                    float sweepAngle = Mathf.Lerp(-arc * 0.5f, arc * 0.5f, attackProgress);
                    Vector2 baseForRotation = Vector2.zero;
                    attackOffset = RotateVector(baseForRotation, sweepAngle);
                    attackOffset += attackFacing * (lunge * 0.45f * lungeMultiplier * attackProgress);
                    attackAngle = sweepAngle;
                    break;
                }
                case WeaponAttackMotionStyle.Sweep:
                default:
                {
                    float arc = currentWeapon.AttackArcDegrees > 0f ? currentWeapon.AttackArcDegrees : swordDefaultArc;
                    float sweepAngle = Mathf.Lerp(-arc * 0.5f, arc * 0.5f, attackProgress);
                    Vector2 baseForRotation = Vector2.zero;
                    attackOffset = RotateVector(baseForRotation, sweepAngle);
                    attackOffset += attackFacing * (lunge * 0.6f * lungeMultiplier * attackProgress);
                    attackAngle = sweepAngle;
                    break;
                }
            }

            attackScale = 1f + (Mathf.Max(0f, attackPulseScale) * (1f - normalized));
        }

        Vector2 forwardOffset = GetForwardOffset(facing);

        // Keep a stable hand anchor, then push the weapon forward in the facing direction.
        transform.localPosition = directionalBaseOffset + forwardOffset + attackOffset;
        transform.localRotation = Quaternion.Euler(0f, 0f, currentBaseRotation + facingAngle + attackAngle);

        Vector2 scale = currentBaseScale * attackScale;
        transform.localScale = new Vector3(scale.x, scale.y, 1f);
    }

    private void RefreshWeaponVisual(bool force)
    {
        if (combatController == null || player == null)
        {
            return;
        }

        WeaponDefinition weapon = combatController.GetCurrentWeaponDefinition();
        if (!force && weapon == currentWeapon)
        {
            return;
        }

        currentWeapon = weapon;
        attackPulseTimer = 0f;

        if (currentWeapon == null || currentWeapon.VisualSprite == null)
        {
            spriteRenderer.sprite = null;
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.sprite = currentWeapon.VisualSprite;
        spriteRenderer.enabled = true;
        currentBaseOffset = currentWeapon.VisualOffset;
        currentBaseScale = new Vector2(
            Mathf.Max(0.01f, currentWeapon.VisualScale.x),
            Mathf.Max(0.01f, currentWeapon.VisualScale.y)
        );
        currentBaseRotation = currentWeapon.VisualRotation; // Rotation correction to keep sprite horizontal
        transform.localRotation = Quaternion.Euler(0f, 0f, currentBaseRotation);
        transform.localScale = new Vector3(currentBaseScale.x, currentBaseScale.y, 1f);
    }

    private void HandleAttackPerformed(WeaponDefinition weapon, Vector2 direction, PlayerFormType form)
    {
        if (weapon == null || weapon != currentWeapon)
        {
            return;
        }

        attackDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : GetFacingDirection();
        attackPulseTimer = Mathf.Max(attackPulseDuration, weapon.AttackLungeDuration);
    }

    private static Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(
            (vector.x * cos) - (vector.y * sin),
            (vector.x * sin) + (vector.y * cos)
        );
    }

    private Vector2 GetForwardOffset(Vector2 facing)
    {
        if (facing.sqrMagnitude <= 0.0001f)
        {
            return Vector2.right * weaponForwardOffset;
        }

        return facing.normalized * weaponForwardOffset;
    }

    private Vector2 GetDirectionalBaseOffset(Vector2 facing)
    {
        Vector2 normalizedFacing = facing.sqrMagnitude > 0.0001f
            ? facing.normalized
            : Vector2.right;

        Vector2 forwardBias = normalizedFacing * currentBaseOffset.x;
        Vector2 verticalBias = Vector2.up * currentBaseOffset.y;
        return forwardBias + verticalBias;
    }

    private Vector2 GetFacingDirection()
    {
        if (player == null)
        {
            return Vector2.right;
        }

        Vector2 facing = player.FacingDirection;
        return facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector2.right;
    }
}
