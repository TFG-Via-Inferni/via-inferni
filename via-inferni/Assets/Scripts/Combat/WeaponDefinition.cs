using UnityEngine;

public enum WeaponAttackType
{
    Melee,
    Projectile
}

public enum WeaponAttackMotionStyle
{
    Auto,
    Sweep,
    Thrust,
    Chop
}

public enum WeaponIdentityEffect
{
    None,
    ApplyMark,
    ConsumeMarkBonus
}

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Via Inferni/Combat/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponId;
    [SerializeField] private string displayName;
    [SerializeField] private PlayerFormType form;

    [Header("Visual")]
    [SerializeField] private Sprite visualSprite;
    [SerializeField] private Vector2 visualOffset = new Vector2(0.28f, -0.04f);
    [SerializeField] private Vector2 visualScale = Vector2.one;
    [Range(-180f, 180f)] [SerializeField] private float visualRotation;
    [SerializeField] private WeaponAttackMotionStyle attackMotionStyle = WeaponAttackMotionStyle.Auto;
    [Min(0f)] [SerializeField] private float attackArcDegrees = 90f;
    [Min(0f)] [SerializeField] private float attackLungeMultiplier = 1f;
    [Min(0f)] [SerializeField] private float idleBobAmplitude = 0.012f;
    [Min(0f)] [SerializeField] private float idleBobSpeed = 7f;
    [Min(0f)] [SerializeField] private float attackLungeDistance = 0.08f;
    [Min(0.01f)] [SerializeField] private float attackLungeDuration = 0.35f;
    [SerializeField] private bool flipVisualWithFacing = true;

    [Header("Attack")]
    [SerializeField] private WeaponAttackType attackType = WeaponAttackType.Melee;
    [Min(0.01f)] [SerializeField] private float baseDamage = 10f;
    [Min(0.05f)] [SerializeField] private float cooldown = 0.4f;
    [Min(0.01f)] [SerializeField] private float range = 1.2f;

    [Header("Melee")]
    [SerializeField] private Vector2 hitboxSize = new Vector2(1.2f, 1.2f);
    [SerializeField] private Vector2 hitboxOffset = new Vector2(0.9f, 0f);
    [Min(0f)] [SerializeField] private float meleeSlashVisualReach = 0f;
    [SerializeField] private bool meleeAppliesKnockback;
    [Min(0f)] [SerializeField] private float meleeKnockbackForce = 0f;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [Min(0.1f)] [SerializeField] private float projectileSpeed = 12f;
    [Min(0.1f)] [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private Vector2 projectileSpawnOffset = new Vector2(0.9f, 0f);
    [SerializeField] private bool projectileHoming;
    [Min(0f)] [SerializeField] private float projectileHomingTurnSpeed = 0f;
    [Min(0f)] [SerializeField] private float projectileHomingSearchRadius = 0f;

    [Header("Charge Shot")]
    [SerializeField] private bool requiresChargeAttack;
    [Min(0.1f)] [SerializeField] private float chargeTimeToMax = 1.25f;
    [Min(0.1f)] [SerializeField] private float chargeMinDamageMultiplier = 0.6f;
    [Min(0.1f)] [SerializeField] private float chargeMaxDamageMultiplier = 2.2f;

    [Header("Identity")]
    [SerializeField] private WeaponIdentityEffect identityEffect;
    [Min(0f)] [SerializeField] private float markDuration = 3f;
    [Min(1f)] [SerializeField] private float markedDamageMultiplier = 1.5f;

    [Header("Impact Feedback")]
    [Min(0f)] [SerializeField] private float hitstopDuration = 0.03f;
    [Min(0f)] [SerializeField] private float cameraShakeDistance = 0.08f;
    [Min(0f)] [SerializeField] private float cameraShakeDuration = 0.08f;
    [Min(0f)] [SerializeField] private float attackRecoilDistance = 0.08f;
    [Min(0f)] [SerializeField] private float attackRecoilDuration = 0.09f;

    public string WeaponId => weaponId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? weaponId : displayName;
    public PlayerFormType Form => form;
    public Sprite VisualSprite => visualSprite;
    public Vector2 VisualOffset => visualOffset;
    public Vector2 VisualScale => visualScale;
    public float VisualRotation => visualRotation;
    public WeaponAttackMotionStyle AttackMotionStyle => attackMotionStyle;
    public float AttackArcDegrees => attackArcDegrees;
    public float AttackLungeMultiplier => attackLungeMultiplier;
    public float IdleBobAmplitude => idleBobAmplitude;
    public float IdleBobSpeed => idleBobSpeed;
    public float AttackLungeDistance => attackLungeDistance;
    public float AttackLungeDuration => attackLungeDuration;
    public bool FlipVisualWithFacing => flipVisualWithFacing;
    public WeaponAttackType AttackType => attackType;
    public float BaseDamage => baseDamage;
    public float Cooldown => cooldown;
    public float Range => range;
    public Vector2 HitboxSize => hitboxSize;
    public Vector2 HitboxOffset => hitboxOffset;
    public float MeleeSlashVisualReach => meleeSlashVisualReach;
    public bool MeleeAppliesKnockback => meleeAppliesKnockback;
    public float MeleeKnockbackForce => meleeKnockbackForce;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public Vector2 ProjectileSpawnOffset => projectileSpawnOffset;
    public bool ProjectileHoming => projectileHoming;
    public float ProjectileHomingTurnSpeed => projectileHomingTurnSpeed;
    public float ProjectileHomingSearchRadius => projectileHomingSearchRadius;
    public bool RequiresChargeAttack => requiresChargeAttack;
    public float ChargeTimeToMax => chargeTimeToMax;
    public float ChargeMinDamageMultiplier => chargeMinDamageMultiplier;
    public float ChargeMaxDamageMultiplier => chargeMaxDamageMultiplier;
    public WeaponIdentityEffect IdentityEffect => identityEffect;
    public float MarkDuration => markDuration;
    public float MarkedDamageMultiplier => markedDamageMultiplier;
    public float HitstopDuration => hitstopDuration;
    public float CameraShakeDistance => cameraShakeDistance;
    public float CameraShakeDuration => cameraShakeDuration;
    public float AttackRecoilDistance => attackRecoilDistance;
    public float AttackRecoilDuration => attackRecoilDuration;

    public WeaponAttackMotionStyle GetResolvedAttackMotionStyle()
    {
        if (attackMotionStyle != WeaponAttackMotionStyle.Auto)
        {
            return attackMotionStyle;
        }

        string key = string.IsNullOrWhiteSpace(weaponId) ? displayName : weaponId;
        key = string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();

        if (key.Contains("spear") || key.Contains("lanza"))
        {
            return WeaponAttackMotionStyle.Thrust;
        }

        if (key.Contains("axe") || key.Contains("hacha"))
        {
            return WeaponAttackMotionStyle.Chop;
        }

        return WeaponAttackMotionStyle.Sweep;
    }
}
