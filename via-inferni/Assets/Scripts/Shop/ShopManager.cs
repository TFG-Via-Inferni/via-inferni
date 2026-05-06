using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class ShopManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private ShopStand shopStandPrefab;
    [Header("Item Pools")]
    [Tooltip("Pool of special items that can appear in shops (assign special InventoryItemDefinition assets)")]
    [SerializeField] private InventoryItemDefinition[] specialPool = new InventoryItemDefinition[0];

    [Header("Layout")]
    [SerializeField] private Vector2[] standOffsets = new Vector2[3]
    {
        new Vector2(-2f, 0f),
        new Vector2(0f, 0f),
        new Vector2(2f, 0f)
    };

    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.5f;

    private readonly List<ShopStand> spawnedStands = new List<ShopStand>(3);
    private Player player;
    private PlayerInventory playerInventory;

    private void Awake()
    {
        if (shopStandPrefab == null)
        {
            Debug.LogWarning("ShopManager: no shopStandPrefab assigned.");
        }

        // Spawn stands immediately as children
        SpawnStands();
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<Player>();
            playerInventory = playerObj.GetComponent<PlayerInventory>();
        }

        // Populate shop immediately when starting
        PopulateShop();
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (!keyboard.eKey.wasPressedThisFrame)
        {
            return;
        }

        // Find nearest stand within interact radius
        ShopStand nearest = null;
        float nearestSq = float.MaxValue;
        Vector2 playerPos = player.transform.position;

        for (int i = 0; i < spawnedStands.Count; i++)
        {
            var stand = spawnedStands[i];
            if (stand == null || stand.ItemDefinition == null) continue;
            float sq = ((Vector2)stand.transform.position - playerPos).sqrMagnitude;
            if (sq < nearestSq && sq <= interactRadius * interactRadius)
            {
                nearestSq = sq;
                nearest = stand;
            }
        }

        if (nearest == null)
        {
            return;
        }

        if (nearest.TryPurchase(playerInventory, player))
        {
            Debug.Log($"ShopManager: {player.name} purchased item from stand.");
        }
        else
        {
            Debug.Log("ShopManager: purchase failed (not enough souls or inventory full).");
        }
    }

    private void PopulateShop()
    {
        InventoryItemDefinition[] items = new InventoryItemDefinition[spawnedStands.Count];
        int[] prices = new int[spawnedStands.Count];

        // Central index is middle (1) for 3 stands
        int centerIndex = spawnedStands.Count == 3 ? 1 : 0;

        // 1) Try central special
        InventoryItemDefinition centerItem = TryPickAvailableSpecial();
        if (centerItem != null)
        {
            items[centerIndex] = centerItem;
            prices[centerIndex] = PriceForItem(centerItem);
        }
        else
        {
            // Fallback: pick a tier 4 (Platinum) item from current circle
            InventoryItemDefinition tier4 = PickTierDropFromCurrentCircle(ItemTier.Platinum);
            if (tier4 != null)
            {
                items[centerIndex] = tier4;
                prices[centerIndex] = PriceForItem(tier4);
            }
            else
            {
                // last resort: pick any circle drop
                if (CircleManager.instance != null && CircleManager.instance.CurrentCircleDefinition != null && CircleManager.instance.CurrentCircleDefinition.TryPickInventoryDrop(out InventoryItemDefinition any))
                {
                    items[centerIndex] = any;
                    prices[centerIndex] = PriceForItem(any);
                }
            }
        }

        // 2) Pick adjacent (left/right)
        for (int i = 0; i < spawnedStands.Count; i++)
        {
            if (i == centerIndex) continue;

            InventoryItemDefinition pick = null;
            if (CircleManager.instance != null && CircleManager.instance.CurrentCircleDefinition != null)
            {
                CircleDefinition def = CircleManager.instance.CurrentCircleDefinition;
                def.TryPickInventoryDrop(out pick);
            }

            if (pick == null)
            {
                // fallback global
                pick = PickAnyGlobalDropFallback();
            }

            items[i] = pick;
            prices[i] = pick != null ? PriceForItem(pick) : 0;
        }

        AssignItems(items, prices);
    }

    private InventoryItemDefinition TryPickAvailableSpecial()
    {
        if (specialPool == null || specialPool.Length == 0) return null;

        for (int i = 0; i < specialPool.Length; i++)
        {
            var candidate = specialPool[i];
            if (candidate == null) continue;
            SpecialItemType st = SpecialItemManager.ParseSpecialEffectId(candidate.SpecialEffectId);
            if (st == SpecialItemType.None) continue;

            if (SpecialItemManager.Instance.IsSpecialTypeActive(st))
            {
                continue; // already active somewhere
            }

            // Also prevent if player already has it in inventory
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                var inv = playerObj.GetComponent<PlayerInventory>();
                if (inv != null && inv.HasSpecialType(st))
                {
                    continue;
                }
            }

            return candidate;
        }

        return null;
    }

    private InventoryItemDefinition PickTierDropFromCurrentCircle(ItemTier tier)
    {
        if (CircleManager.instance == null || CircleManager.instance.CurrentCircleDefinition == null) return null;
        var def = CircleManager.instance.CurrentCircleDefinition;

        var validEntries = def.inventoryDropPool == null ? null : new System.Collections.Generic.List<WeightedInventoryDropEntry>();
        if (validEntries == null) return null;

        foreach (var entry in def.inventoryDropPool)
        {
            if (entry == null || entry.itemDefinition == null) continue;
            if (entry.itemDefinition.ItemType != InventoryItemType.StatBoost) continue;
            if (entry.itemDefinition.StatBoost.tier == tier)
            {
                validEntries.Add(entry);
            }
        }

        if (validEntries.Count == 0) return null;

        float totalWeight = 0f;
        foreach (var e in validEntries) totalWeight += Mathf.Max(0f, e.weight);
        if (totalWeight <= 0f)
        {
            return validEntries[Random.Range(0, validEntries.Count)].itemDefinition;
        }

        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        foreach (var e in validEntries)
        {
            cumulative += Mathf.Max(0f, e.weight);
            if (roll <= cumulative) return e.itemDefinition;
        }

        return validEntries[validEntries.Count - 1].itemDefinition;
    }

    private InventoryItemDefinition PickAnyGlobalDropFallback()
    {
        if (CircleManager.instance != null)
        {
            // try circle first
            if (CircleManager.instance.CurrentCircleDefinition != null && CircleManager.instance.CurrentCircleDefinition.TryPickInventoryDrop(out InventoryItemDefinition pick))
            {
                return pick;
            }
        }

        // fallback to CircleManager global pool
        if (CircleManager.instance != null)
        {
            var cm = CircleManager.instance;
            // use reflection to call private PickGlobalEnemyDropItem? instead we will try globalEnemyDropPool field via public API: TrySpawnGlobalEnemyDrop uses it, but no getter. So return null and let caller handle.
        }

        return null;
    }

    private int PriceForItem(InventoryItemDefinition def)
    {
        if (def == null) return 0;

        if (def.ItemType == InventoryItemType.Special) return 60;

        if (def.ItemType == InventoryItemType.StatBoost)
        {
            switch (def.StatBoost.tier)
            {
                case ItemTier.Bronze: return 20;
                case ItemTier.Silver: return 30;
                case ItemTier.Gold: return 40;
                case ItemTier.Platinum: return 50;
                default: return 40;
            }
        }

        return 40;
    }

    private void SpawnStands()
    {
        // Clear existing
        for (int i = spawnedStands.Count - 1; i >= 0; i--)
        {
            if (spawnedStands[i] != null)
            {
                DestroyImmediate(spawnedStands[i].gameObject);
            }
        }
        spawnedStands.Clear();

        if (shopStandPrefab == null)
        {
            return;
        }

        for (int i = 0; i < standOffsets.Length; i++)
        {
            Vector2 pos = standOffsets[i];
            var go = Instantiate(shopStandPrefab.gameObject, transform);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            var stand = go.GetComponent<ShopStand>();
            if (stand != null)
            {
                spawnedStands.Add(stand);
            }
        }
    }

    /// <summary>
    /// Assign items and prices to the three stands. Arrays must be length 3.
    /// </summary>
    public void AssignItems(InventoryItemDefinition[] items, int[] prices)
    {
        if (items == null || prices == null || items.Length != spawnedStands.Count || prices.Length != spawnedStands.Count)
        {
            Debug.LogWarning("ShopManager.AssignItems: invalid arrays.");
            return;
        }

        for (int i = 0; i < spawnedStands.Count; i++)
        {
            spawnedStands[i].SetItem(items[i], prices[i]);
        }
    }

    /// <summary>
    /// Clear all stands (useful when leaving room or refreshing shop).
    /// </summary>
    public void ClearAll()
    {
        foreach (var s in spawnedStands)
        {
            if (s != null) s.ClearItem();
        }
    }
}
