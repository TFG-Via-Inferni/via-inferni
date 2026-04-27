using System.Text.RegularExpressions;

public static class InventoryItemValidationUtility
{
    private static readonly Regex ItemIdRegex = new Regex("^[a-z0-9_]+$", RegexOptions.Compiled);

    public static string NormalizeId(string rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId))
        {
            return string.Empty;
        }

        return rawId.Trim().ToLowerInvariant().Replace(" ", "_");
    }

    public static bool IsValidItemId(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        return ItemIdRegex.IsMatch(itemId);
    }

    public static bool ValidateDefinition(InventoryItemDefinition definition, out string reason)
    {
        reason = string.Empty;

        if (definition == null)
        {
            reason = "Definition is null.";
            return false;
        }

        if (!IsValidItemId(definition.ItemId))
        {
            reason = "ItemId must use lowercase letters, numbers, and underscores only.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.DisplayName))
        {
            reason = "DisplayName is required.";
            return false;
        }

        if (definition.IsStackable)
        {
            reason = "Inventory items in this system are unique and cannot be stackable.";
            return false;
        }

        if (definition.ItemType == InventoryItemType.StatBoost)
        {
            if (definition.StatBoost.statType == StatBoostType.None)
            {
                reason = "StatBoost item must define a StatBoostType.";
                return false;
            }

            if (definition.StatBoost.magnitude <= 0f)
            {
                reason = "StatBoost item must define magnitude > 0.";
                return false;
            }
        }

        if (definition.ItemType == InventoryItemType.Special && string.IsNullOrWhiteSpace(definition.SpecialEffectId))
        {
            reason = "Special item must define SpecialEffectId.";
            return false;
        }

        return true;
    }
}
