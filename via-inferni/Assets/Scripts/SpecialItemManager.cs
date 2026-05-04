using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks active special items in the current run to prevent duplicate copies
/// of the same special item type from coexisting in inventory or on the floor.
/// </summary>
public class SpecialItemManager : MonoBehaviour
{
    private static SpecialItemManager instance;

    private HashSet<SpecialItemType> activeSpecialTypes = new HashSet<SpecialItemType>();

    public static SpecialItemManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindFirstObjectByType<SpecialItemManager>();
                if (instance == null)
                {
                    GameObject managerObject = new GameObject("SpecialItemManager");
                    instance = managerObject.AddComponent<SpecialItemManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Returns true if a special item of the given type is already active (in inventory or on floor).
    /// </summary>
    public bool IsSpecialTypeActive(SpecialItemType specialType)
    {
        if (specialType == SpecialItemType.None)
        {
            return false;
        }

        return activeSpecialTypes.Contains(specialType);
    }

    /// <summary>
    /// Register that a special item type is now active.
    /// </summary>
    public void RegisterSpecial(SpecialItemType specialType)
    {
        if (specialType != SpecialItemType.None)
        {
            activeSpecialTypes.Add(specialType);
        }
    }

    /// <summary>
    /// Unregister a special item type (when dropped or consumed).
    /// </summary>
    public void UnregisterSpecial(SpecialItemType specialType)
    {
        if (specialType != SpecialItemType.None)
        {
            activeSpecialTypes.Remove(specialType);
        }
    }

    /// <summary>
    /// Clear all registered special types (typically called at end of run or start of new circle).
    /// </summary>
    public void ClearAll()
    {
        activeSpecialTypes.Clear();
    }

    /// <summary>
    /// Parses a special effect ID string and returns the corresponding SpecialItemType.
    /// Convention: "special_{type}" (e.g., "special_dash", "special_cloak").
    /// </summary>
    public static SpecialItemType ParseSpecialEffectId(string effectId)
    {
        if (string.IsNullOrWhiteSpace(effectId))
        {
            return SpecialItemType.None;
        }

        string normalized = effectId.Trim().ToLowerInvariant();

        return normalized switch
        {
            "special_dash" => SpecialItemType.Dash,
            "special_cloak" => SpecialItemType.Cloak,
            "special_shield" => SpecialItemType.Shield,
            "special_fullmap" => SpecialItemType.FullMap,
            "special_cursedcoin" => SpecialItemType.CursedCoin,
            _ => SpecialItemType.None
        };
    }
}
