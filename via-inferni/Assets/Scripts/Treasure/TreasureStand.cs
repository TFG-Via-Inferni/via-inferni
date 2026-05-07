using UnityEngine;

[DisallowMultipleComponent]
public class TreasureStand : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer standSpriteRenderer;
    [SerializeField] private SpriteRenderer itemSpriteRenderer;

    [Header("Runtime")]
    [SerializeField] private InventoryItemDefinition itemDefinition;

    public InventoryItemDefinition ItemDefinition => itemDefinition;

    public void SetItem(InventoryItemDefinition def)
    {
        itemDefinition = def;
        UpdateVisuals();
    }

    public void ClearItem()
    {
        itemDefinition = null;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (itemSpriteRenderer != null)
        {
            itemSpriteRenderer.sprite = itemDefinition != null ? itemDefinition.Icon : null;
            itemSpriteRenderer.enabled = itemDefinition != null && itemSpriteRenderer.sprite != null;
        }
    }

    /// <summary>
    /// Attempt to take the item: add it to inventory without cost.
    /// Returns true if take succeeded; false if inventory is full.
    /// </summary>
    public bool TryTake(PlayerInventory inventory, Player player)
    {
        if (itemDefinition == null || inventory == null || player == null)
        {
            return false;
        }

        // Try to add item to inventory
        if (!inventory.TryAddItem(itemDefinition, out string reason))
        {
            Debug.LogWarning($"TreasureStand: take failed: {reason}");
            return false;
        }

        // On success, clear the stand
        ClearItem();
        return true;
    }

    private void OnValidate()
    {
        UpdateVisuals();
    }
}
