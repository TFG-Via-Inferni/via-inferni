using System;

public enum InventoryItemType
{
    StatBoost = 0,
    Special = 1
}

public enum StatBoostType
{
    None = 0,
    MaxHealth = 1,
    DamageMultiplier = 2,
    MoveSpeedMultiplier = 3,
    CritChance = 4,
    DodgeChance = 5
}

public enum ItemTier
{
    Bronze = 0,
    Silver = 1,
    Gold = 2,
    Platinum = 3
}

[Serializable]
public struct InventoryStatBoostData
{
    public StatBoostType statType;
    public ItemTier tier;
    public float magnitude;
}
