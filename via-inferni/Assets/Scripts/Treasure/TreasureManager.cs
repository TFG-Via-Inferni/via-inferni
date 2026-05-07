using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class TreasureManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private TreasureStand treasureStandPrefab;

    [Header("Layout")]
    [SerializeField] private Vector2[] standOffsets = new Vector2[1]
    {
        new Vector2(24f, -11.5f)
    };

    [Header("Interaction")]
    [SerializeField] private float interactRadius = 1.5f;

    private readonly List<TreasureStand> spawnedStands = new List<TreasureStand>(3);
    private Player player;
    private PlayerInventory playerInventory;

    private void Awake()
    {
        if (treasureStandPrefab == null)
        {
            Debug.LogWarning("TreasureManager: no treasureStandPrefab assigned.");
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

        // Populate treasure immediately when starting
        PopulateTreasure();
    }

    private void Update()
    {
        HandleDebugInput();

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
        TreasureStand nearest = null;
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

        if (nearest.TryTake(playerInventory, player))
        {
            Debug.Log($"TreasureManager: {player.name} took item from stand.");
        }
        else
        {
            Debug.Log("TreasureManager: take failed (inventory full).");
        }
    }

    private void HandleDebugInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.f7Key.wasPressedThisFrame)
        {
            PopulateTreasure();
            Debug.Log("TreasureManager: debug F7 -> PopulateTreasure ejecutado.");
        }

        if (keyboard.f8Key.wasPressedThisFrame)
        {
            ClearAll();
            Debug.Log("TreasureManager: debug F8 -> ClearAll ejecutado.");
        }
    }

    public void PopulateTreasure()
    {
        InventoryItemDefinition[] items = new InventoryItemDefinition[spawnedStands.Count];

        // Get regular items from current circle (not special items, just standard drops)
        for (int i = 0; i < spawnedStands.Count; i++)
        {
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
        }

        AssignItems(items);
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

        return null;
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

        if (treasureStandPrefab == null)
        {
            return;
        }

        for (int i = 0; i < standOffsets.Length; i++)
        {
            Vector2 pos = standOffsets[i];
            var go = Instantiate(treasureStandPrefab.gameObject, transform);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            var stand = go.GetComponent<TreasureStand>();
            if (stand != null)
            {
                spawnedStands.Add(stand);
            }
        }
    }

    /// <summary>
    /// Assign items to the treasure stands.
    /// </summary>
    public void AssignItems(InventoryItemDefinition[] items)
    {
        if (items == null || items.Length != spawnedStands.Count)
        {
            Debug.LogWarning("TreasureManager.AssignItems: invalid array.");
            return;
        }

        for (int i = 0; i < spawnedStands.Count; i++)
        {
            spawnedStands[i].SetItem(items[i]);
        }
    }

    /// <summary>
    /// Clear all stands.
    /// </summary>
    public void ClearAll()
    {
        foreach (var s in spawnedStands)
        {
            if (s != null) s.ClearItem();
        }
    }
}
