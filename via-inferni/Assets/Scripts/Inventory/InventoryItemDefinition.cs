using UnityEngine;

[CreateAssetMenu(fileName = "InventoryItem", menuName = "Scriptable Objects/Inventory Item Definition")]
public class InventoryItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string itemId = "item_new";
    [SerializeField] private string displayName = "New Inventory Item";
    [SerializeField] private Sprite icon;

    [Header("Type")]
    [SerializeField] private InventoryItemType itemType = InventoryItemType.StatBoost;
    [SerializeField] private bool isStackable = false;

    [Header("Stat Boost")]
    [SerializeField] private InventoryStatBoostData statBoost = new InventoryStatBoostData
    {
        statType = StatBoostType.MaxHealth,
        tier = ItemTier.Bronze,
        magnitude = 1f
    };

    [Header("Special")]
    [SerializeField] private string specialEffectId = "special_new";

    public string ItemId => itemId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public InventoryItemType ItemType => itemType;
    public bool IsStackable => isStackable;
    public InventoryStatBoostData StatBoost => statBoost;
    public string SpecialEffectId => specialEffectId;

    private void OnValidate()
    {
        itemId = InventoryItemValidationUtility.NormalizeId(itemId);

        // En este sistema los objetos no se apilan en una sola ranura; cada copia ocupa su propio hueco.
        if (isStackable)
        {
            isStackable = false;
        }

        if (itemType == InventoryItemType.StatBoost)
        {
            if (statBoost.statType == StatBoostType.None)
            {
                statBoost.statType = StatBoostType.MaxHealth;
            }

            statBoost.magnitude = GetMagnitudeForTier(statBoost.statType, statBoost.tier);
        }

        if (itemType == InventoryItemType.Special)
        {
            specialEffectId = InventoryItemValidationUtility.NormalizeId(specialEffectId);
        }
    }

    public bool IsValid(out string reason)
    {
        return InventoryItemValidationUtility.ValidateDefinition(this, out reason);
    }

    private static float GetMagnitudeForTier(StatBoostType statType, ItemTier tier)
    {
        int tierIndex = Mathf.Clamp((int)tier + 1, 1, 4);

        if (statType == StatBoostType.MaxHealth)
        {
            return tierIndex;
        }

        return tierIndex * 0.1f;
    }
}
