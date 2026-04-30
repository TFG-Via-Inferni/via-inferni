using UnityEngine;
using System;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class PlayerInventory : MonoBehaviour
{
    public const int MaxSlots = 12;

    private readonly InventoryItemDefinition[] slots = new InventoryItemDefinition[MaxSlots];
    private PlayerStats playerStats;
    private int selectedSlotIndex;
    private string lastFailureReason = string.Empty;
    private float lastFailureTimestamp = -999f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugItemInjection;
    [SerializeField] private InventoryItemDefinition[] debugItemPool = Array.Empty<InventoryItemDefinition>();
    [SerializeField] private Key debugAddItemKey = Key.F6;
    private int debugNextItemIndex;

    public event Action OnInventoryChanged;
    public event Action<int> OnSelectedSlotChanged;

    public int SelectedSlotIndex => selectedSlotIndex;
    public string LastFailureReason => lastFailureReason;
    public float LastFailureTimestamp => lastFailureTimestamp;

    private void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (!enableDebugItemInjection)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (!keyboard[debugAddItemKey].wasPressedThisFrame)
        {
            return;
        }

        TryAddNextDebugItem();
    }

    public int GetUnlockedSlotCount()
    {
        int capacity = playerStats != null ? playerStats.InventoryCapacity : 3;
        return Mathf.Clamp(capacity, 0, MaxSlots);
    }

    public bool SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSlots)
        {
            return false;
        }

        if (selectedSlotIndex == slotIndex)
        {
            return false;
        }

        selectedSlotIndex = slotIndex;
        OnSelectedSlotChanged?.Invoke(selectedSlotIndex);
        return true;
    }

    public int SelectNextSlotCyclic()
    {
        selectedSlotIndex = (selectedSlotIndex + 1) % MaxSlots;
        OnSelectedSlotChanged?.Invoke(selectedSlotIndex);
        return selectedSlotIndex;
    }

    public bool TryAddItem(InventoryItemDefinition item, out string reason)
    {
        reason = string.Empty;

        if (item == null)
        {
            reason = "Item is null.";
            RegisterFailure(reason);
            return false;
        }

        if (!item.IsValid(out reason))
        {
            RegisterFailure(reason);
            return false;
        }

        int freeSlotIndex = FindFirstFreeUnlockedSlotIndex();
        if (freeSlotIndex < 0)
        {
            reason = "No free unlocked slots.";
            RegisterFailure(reason);
            return false;
        }

        slots[freeSlotIndex] = item;
    ApplyItemEffect(item);
        ClearFailure();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public bool TryDropSelected(out InventoryItemDefinition droppedItem, out string reason)
    {
        droppedItem = null;
        reason = string.Empty;

        if (!IsValidSlotIndex(selectedSlotIndex))
        {
            reason = "Selected slot index is invalid.";
            RegisterFailure(reason);
            return false;
        }

        if (!IsUnlocked(selectedSlotIndex))
        {
            reason = "Selected slot is locked.";
            RegisterFailure(reason);
            return false;
        }

        InventoryItemDefinition current = slots[selectedSlotIndex];
        if (current == null)
        {
            reason = "Selected slot is empty.";
            RegisterFailure(reason);
            return false;
        }

        slots[selectedSlotIndex] = null;
    RemoveItemEffect(current);
        ClearFailure();
        droppedItem = current;
        OnInventoryChanged?.Invoke();
        return true;
    }

    public InventorySlotState GetSlotState(int slotIndex)
    {
        InventorySlotState state = new InventorySlotState();

        if (!IsValidSlotIndex(slotIndex))
        {
            return state;
        }

        bool unlocked = IsUnlocked(slotIndex);
        InventoryItemDefinition item = slots[slotIndex];

        state.slotIndex = slotIndex;
        state.unlocked = unlocked;
        state.occupied = unlocked && item != null;
        state.selected = slotIndex == selectedSlotIndex;
        state.item = item;

        return state;
    }

    public InventoryItemDefinition GetItemAtSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex) || !IsUnlocked(slotIndex))
        {
            return null;
        }

        return slots[slotIndex];
    }

    private int FindFirstFreeUnlockedSlotIndex()
    {
        int unlockedCount = GetUnlockedSlotCount();
        for (int i = 0; i < unlockedCount; i++)
        {
            if (slots[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < MaxSlots;
    }

    private bool IsUnlocked(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < GetUnlockedSlotCount();
    }

    private void RegisterFailure(string reason)
    {
        lastFailureReason = reason ?? string.Empty;
        lastFailureTimestamp = Time.unscaledTime;
    }

    private void ClearFailure()
    {
        lastFailureReason = string.Empty;
        lastFailureTimestamp = -999f;
    }

    private void ApplyItemEffect(InventoryItemDefinition item)
    {
        if (playerStats == null || item == null)
        {
            return;
        }

        if (item.ItemType != InventoryItemType.StatBoost)
        {
            return;
        }

        InventoryStatBoostData boost = item.StatBoost;
        switch (boost.statType)
        {
            case StatBoostType.MaxHealth:
                playerStats.AdjustMaxHealthHearts(Mathf.RoundToInt(boost.magnitude));
                break;
            case StatBoostType.DamageMultiplier:
                playerStats.AdjustDamageMultiplier(boost.magnitude);
                break;
            case StatBoostType.MoveSpeedMultiplier:
                playerStats.AdjustMoveSpeedMultiplier(boost.magnitude);
                break;
            case StatBoostType.CritChance:
                playerStats.AdjustCritChance(boost.magnitude);
                break;
            case StatBoostType.DodgeChance:
                playerStats.AdjustDodgeChance(boost.magnitude);
                break;
        }
    }

    private void RemoveItemEffect(InventoryItemDefinition item)
    {
        if (playerStats == null || item == null)
        {
            return;
        }

        if (item.ItemType != InventoryItemType.StatBoost)
        {
            return;
        }

        InventoryStatBoostData boost = item.StatBoost;
        switch (boost.statType)
        {
            case StatBoostType.MaxHealth:
                playerStats.AdjustMaxHealthHearts(-Mathf.RoundToInt(boost.magnitude));
                break;
            case StatBoostType.DamageMultiplier:
                playerStats.AdjustDamageMultiplier(-boost.magnitude);
                break;
            case StatBoostType.MoveSpeedMultiplier:
                playerStats.AdjustMoveSpeedMultiplier(-boost.magnitude);
                break;
            case StatBoostType.CritChance:
                playerStats.AdjustCritChance(-boost.magnitude);
                break;
            case StatBoostType.DodgeChance:
                playerStats.AdjustDodgeChance(-boost.magnitude);
                break;
        }
    }

    private bool TryAddNextDebugItem()
    {
        if (debugItemPool == null || debugItemPool.Length == 0)
        {
            Debug.LogWarning("PlayerInventory debug: no debug items configured.");
            return false;
        }

        int attempts = debugItemPool.Length;
        while (attempts > 0)
        {
            InventoryItemDefinition candidate = debugItemPool[debugNextItemIndex];
            debugNextItemIndex = (debugNextItemIndex + 1) % debugItemPool.Length;
            attempts--;

            if (candidate == null)
            {
                continue;
            }

            if (TryAddItem(candidate, out string reason))
            {
                Debug.Log($"PlayerInventory debug: added '{candidate.DisplayName}' ({candidate.ItemId}).");
                return true;
            }

            Debug.LogWarning($"PlayerInventory debug: could not add '{candidate.name}'. Reason: {reason}");
        }

        return false;
    }

    [Serializable]
    public struct InventorySlotState
    {
        public int slotIndex;
        public bool unlocked;
        public bool occupied;
        public bool selected;
        public InventoryItemDefinition item;
    }
}

public interface IInventoryPickup
{
    Transform PickupTransform { get; }
    bool TryPickup(PlayerInventory inventory, out string reason);
}

public static class InventoryPickupColliderExtensions
{
    public static bool TryGetInventoryPickup(this Collider2D collider, out IInventoryPickup pickup)
    {
        pickup = null;
        if (collider == null)
        {
            return false;
        }

        MonoBehaviour[] behaviours = collider.GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IInventoryPickup inventoryPickup)
            {
                pickup = inventoryPickup;
                return true;
            }
        }

        return false;
    }
}
