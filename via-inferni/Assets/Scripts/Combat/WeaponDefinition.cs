using UnityEngine;

public enum WeaponAttackType
{
    Melee,
    Projectile
}

[CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Via Inferni/Combat/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponId;
    [SerializeField] private string displayName;
    [SerializeField] private PlayerFormType form;

    [Header("Attack")]
    [SerializeField] private WeaponAttackType attackType = WeaponAttackType.Melee;
    [Min(0.01f)] [SerializeField] private float baseDamage = 10f;
    [Min(0.05f)] [SerializeField] private float cooldown = 0.4f;
    [Min(0.01f)] [SerializeField] private float range = 1.2f;

    [Header("Melee")]
    [SerializeField] private Vector2 hitboxSize = new Vector2(1.2f, 1.2f);
    [SerializeField] private Vector2 hitboxOffset = new Vector2(0.9f, 0f);

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [Min(0.1f)] [SerializeField] private float projectileSpeed = 12f;
    [Min(0.1f)] [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private Vector2 projectileSpawnOffset = new Vector2(0.9f, 0f);

    public string WeaponId => weaponId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? weaponId : displayName;
    public PlayerFormType Form => form;
    public WeaponAttackType AttackType => attackType;
    public float BaseDamage => baseDamage;
    public float Cooldown => cooldown;
    public float Range => range;
    public Vector2 HitboxSize => hitboxSize;
    public Vector2 HitboxOffset => hitboxOffset;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public Vector2 ProjectileSpawnOffset => projectileSpawnOffset;
}
