using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Scriptable Objects/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    public EnemyType enemyType = EnemyType.Normal;
    public string displayName = "Enemy";
    public Sprite overrideSprite;

    [Header("Sprite Animation")]
    public Sprite[] idleFrames;
    public Sprite moveRightSprite;
    public Sprite moveLeftSprite;

    [Header("Stats")]
    [Min(1f)] public float maxHealth = 30f;
    [Min(0f)] public float moveSpeed = 2f;
    [Min(0f)] public float detectionRadius = 10f;
    [Min(0f)] public float attackRange = 1.5f;
    [Min(0f)] public float attackCooldown = 1f;
    [Min(0f)] public float attackDamage = 10f;

    [Header("Physics")]
    public bool ignoreGravity = true;

    [Header("Behavior")]
    public bool startDormant = true;
}
