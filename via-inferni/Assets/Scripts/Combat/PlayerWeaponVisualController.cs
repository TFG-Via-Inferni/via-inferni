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
    [Header("Melee Back Carry")]
    [SerializeField] private Vector2 meleeBackCarrySideOffset = new Vector2(0.1f, 0f);
    [SerializeField] private Vector2 meleeBackCarryDownOffset = new Vector2(0.1f, -0.1f);
    [SerializeField] private Vector2 meleeBackCarryUpOffset = new Vector2(0f, -0.1f);
    [Range(-180f, 180f)] [SerializeField] private float meleeBackCarrySideAngle = 35f;
    [Range(-180f, 180f)] [SerializeField] private float meleeBackCarryDownAngle = 25f;
    [Range(-180f, 180f)] [SerializeField] private float meleeBackCarryUpAngle = 25f;
    [SerializeField] private int meleeBackCarrySortingOrder = 1;
    [SerializeField] private int meleeBackCarryUpSortingOrder = 6;

    private SpriteRenderer spriteRenderer;
    private WeaponDefinition currentWeapon;
    private Vector2 currentBaseOffset;
    private Vector2 currentBaseScale = Vector2.one;
    private float currentBaseRotation;
    private int baseSortingOrder;
    private Vector2 attackDirection = Vector2.right;
    private float attackPulseTimer;
    private Vector2 recoilDirection = Vector2.zero;
    private float recoilTimer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseSortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder : 0;

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

        if (recoilTimer > 0f)
        {
            recoilTimer = Mathf.Max(0f, recoilTimer - Time.deltaTime);
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
        Vector2 visualDirection = facing;
        
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
            visualDirection = attackFacing;

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

        if (recoilTimer > 0f)
        {
            float recoilDuration = Mathf.Max(0.01f, currentWeapon.AttackRecoilDuration);
            float recoilNormalized = recoilTimer / recoilDuration;
            float recoilDistance = currentWeapon.AttackRecoilDistance * recoilNormalized;
            attackOffset += recoilDirection * recoilDistance;
        }

        bool isIdleMeleePose = attackPulseTimer <= 0f
            && currentWeapon.AttackType == WeaponAttackType.Melee;

        if (isIdleMeleePose)
        {
            ApplyIdleMeleeBackCarryPose(facing);
            return;
        }

        spriteRenderer.sortingOrder = baseSortingOrder;
        bool mirrorVisual = ShouldMirrorVisual(visualDirection);
        Vector2 rotationDirection = mirrorVisual ? Vector2.right : visualDirection;
        float visualAngle = Mathf.Atan2(rotationDirection.y, rotationDirection.x) * Mathf.Rad2Deg;
        Vector2 directionalBaseOffset = GetDirectionalBaseOffset(visualDirection);
        Vector2 forwardOffset = GetForwardOffset(visualDirection);
        float finalAttackAngle = mirrorVisual ? -attackAngle : attackAngle;

        // During the attack, the whole weapon pose follows the attack direction.
        transform.localPosition = directionalBaseOffset + forwardOffset + attackOffset;
        transform.localRotation = Quaternion.Euler(0f, 0f, currentBaseRotation + visualAngle + finalAttackAngle);

        Vector2 scale = currentBaseScale * attackScale;
        transform.localScale = new Vector3(scale.x, mirrorVisual ? -scale.y : scale.y, 1f);
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
        recoilDirection = -attackDirection;
        recoilTimer = Mathf.Max(0f, weapon.AttackRecoilDuration);
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

    private bool ShouldMirrorVisual(Vector2 direction)
    {
        if (currentWeapon == null || !currentWeapon.FlipVisualWithFacing)
        {
            return false;
        }

        return direction.x < -0.0001f && Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
    }

    private void ApplyIdleMeleeBackCarryPose(Vector2 facing)
    {
        Vector2 cardinalDirection = GetDominantCardinalDirection(facing);
        bool mirrorVisual = cardinalDirection.x < -0.5f;
        float carryAngle = GetIdleMeleeBackCarryAngle(cardinalDirection, mirrorVisual);
        Vector2 carryOffset = GetIdleMeleeBackCarryOffset(cardinalDirection, mirrorVisual);
        float sortingOrder = cardinalDirection.y > 0.5f ? meleeBackCarryUpSortingOrder : meleeBackCarrySortingOrder;

        spriteRenderer.sortingOrder = Mathf.RoundToInt(sortingOrder);
        transform.localPosition = carryOffset;
        transform.localRotation = Quaternion.Euler(0f, 0f, currentBaseRotation + carryAngle);
        transform.localScale = new Vector3(currentBaseScale.x, mirrorVisual ? -currentBaseScale.y : currentBaseScale.y, 1f);
    }

    private float GetIdleMeleeBackCarryAngle(Vector2 cardinalDirection, bool mirrorVisual)
    {
        if (Mathf.Abs(cardinalDirection.x) > 0.5f)
        {
            return mirrorVisual ? -meleeBackCarrySideAngle : meleeBackCarrySideAngle;
        }

        return cardinalDirection.y > 0.5f ? meleeBackCarryUpAngle : meleeBackCarryDownAngle;
    }

    private Vector2 GetIdleMeleeBackCarryOffset(Vector2 cardinalDirection, bool mirrorVisual)
    {
        if (Mathf.Abs(cardinalDirection.x) > 0.5f)
        {
            return new Vector2(
                mirrorVisual ? -meleeBackCarrySideOffset.x : meleeBackCarrySideOffset.x,
                meleeBackCarrySideOffset.y
            );
        }

        return cardinalDirection.y > 0.5f
            ? meleeBackCarryUpOffset
            : meleeBackCarryDownOffset;
    }

    private static Vector2 GetDominantCardinalDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector2.right;
        }

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            return direction.x >= 0f ? Vector2.right : Vector2.left;
        }

        return direction.y >= 0f ? Vector2.up : Vector2.down;
    }
}
