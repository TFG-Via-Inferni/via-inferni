using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class ShopManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private ShopStand shopStandPrefab;

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
