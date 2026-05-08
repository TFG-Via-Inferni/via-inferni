using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    [Header("Vitals")]
    [Min(1f)] [SerializeField] private float maxHealth = 12f;
    private float currentHealth;

    [Header("Combat")]
    [Min(0.01f)] [SerializeField] private float damageMultiplier = 1f;
    [Range(0f, 1f)] [SerializeField] private float critChance = 0.05f;
    [Range(0f, 1f)] [SerializeField] private float dodgeChance = 0.05f;

    [Header("Weapons")]
    [SerializeField] private int selectedMeleeWeaponIndex;
    [SerializeField] private int selectedRangedWeaponIndex;
    [SerializeField] private float[] meleeWeaponBaseDamage = new float[3] { 10f, 15f, 12f };
    [SerializeField] private float[] rangedWeaponBaseDamage = new float[3] { 9f, 14f, 11f };

    [Header("Mobility")]
    [Min(0.01f)] [SerializeField] private float moveSpeedMultiplier = 1f;

    [Header("Progression")]
    [Range(4, 12)] [SerializeField] private int inventoryCapacity = 4;
    [Range(0, 100)] [SerializeField] private int collectedSoul = 0;

    // Health Properties
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0f;

    // Combat Properties
    public float DamageMultiplier => damageMultiplier;
    public float CritChance => critChance;
    public float DodgeChance => dodgeChance;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public int InventoryCapacity => inventoryCapacity;
    public int CollectedSoul => collectedSoul;

    private void Awake()
    {
        InitializeRuntime();
    }

    private void OnValidate()
    {
        InitializeRuntime();
    }

    public void InitializeRuntime()
    {
        // Health initialization
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }

        damageMultiplier = Mathf.Max(0.01f, damageMultiplier);
        critChance = Mathf.Clamp01(critChance);
        dodgeChance = Mathf.Clamp01(dodgeChance);
        moveSpeedMultiplier = Mathf.Max(0.01f, moveSpeedMultiplier);
        inventoryCapacity = Mathf.Clamp(inventoryCapacity, 4, 12);
        collectedSoul = Mathf.Clamp(collectedSoul, 0, 100);

        EnsureWeaponArray(ref meleeWeaponBaseDamage, 10f, 15f, 12f);
        EnsureWeaponArray(ref rangedWeaponBaseDamage, 9f, 14f, 11f);

        selectedMeleeWeaponIndex = Mathf.Clamp(selectedMeleeWeaponIndex, 0, meleeWeaponBaseDamage.Length - 1);
        selectedRangedWeaponIndex = Mathf.Clamp(selectedRangedWeaponIndex, 0, rangedWeaponBaseDamage.Length - 1);
    }

    public int GetSelectedWeaponIndex(PlayerFormType form)
    {
        return form == PlayerFormType.Melee
            ? selectedMeleeWeaponIndex
            : selectedRangedWeaponIndex;
    }

    public void SetSelectedWeaponIndex(PlayerFormType form, int index)
    {
        float[] pool = GetWeaponPool(form);
        int safeIndex = Mathf.Clamp(index, 0, pool.Length - 1);

        if (form == PlayerFormType.Melee)
        {
            selectedMeleeWeaponIndex = safeIndex;
            return;
        }

        selectedRangedWeaponIndex = safeIndex;
    }

    public bool SelectWeaponSlot(PlayerFormType form, int slotOneBased)
    {
        int desiredIndex = Mathf.Clamp(slotOneBased - 1, 0, 2);
        int currentIndex = GetSelectedWeaponIndex(form);
        if (desiredIndex == currentIndex)
        {
            return false;
        }

        SetSelectedWeaponIndex(form, desiredIndex);
        return true;
    }

    public float GetSelectedWeaponBaseDamage(PlayerFormType form)
    {
        float[] pool = GetWeaponPool(form);
        int index = GetSelectedWeaponIndex(form);
        return pool[index];
    }

    public float GetFinalDamage(PlayerFormType form)
    {
        return GetSelectedWeaponBaseDamage(form) * damageMultiplier;
    }

    public string GetSelectedWeaponId(PlayerFormType form)
    {
        int slot = GetSelectedWeaponIndex(form) + 1;
        return form == PlayerFormType.Melee
            ? slot switch
            {
                1 => "Sword",
                2 => "Axe",
                _ => "Spear"
            }
            : slot switch
            {
                1 => "Bow",
                2 => "Ballista",
                _ => "Magic"
            };
    }

    public bool IncreaseInventoryCapacity(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        int previous = inventoryCapacity;
        inventoryCapacity = Mathf.Clamp(inventoryCapacity + amount, 4, 12);
        return inventoryCapacity != previous;
    }

    public void AdjustMaxHealthHearts(int heartsDelta)
    {
        if (heartsDelta == 0)
        {
            return;
        }

        maxHealth = Mathf.Max(1f, maxHealth + (heartsDelta * 2f));
        currentHealth = Mathf.Min(currentHealth, maxHealth);
    }

    public void AdjustDamageMultiplier(float delta)
    {
        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        damageMultiplier = Mathf.Max(0.01f, damageMultiplier + delta);
    }

    public void AdjustMoveSpeedMultiplier(float delta)
    {
        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        moveSpeedMultiplier = Mathf.Max(0.01f, moveSpeedMultiplier + delta);
    }

    public void AdjustCritChance(float delta)
    {
        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        critChance = Mathf.Clamp01(critChance + delta);
    }

    public void AdjustDodgeChance(float delta)
    {
        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        dodgeChance = Mathf.Clamp01(dodgeChance + delta);
    }

    /// <summary>
    /// Restore health by the given amount. Returns the actual amount healed.
    /// </summary>
    public float RestoreHealth(float amount)
    {
        if (amount <= 0f)
        {
            return 0f;
        }

        float previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        return currentHealth - previousHealth;
    }

    /// <summary>
    /// Reduce health by the given amount. Returns the actual amount of damage taken.
    /// </summary>
    public float TakeDamage(float amount)
    {
        if (amount <= 0f)
        {
            return 0f;
        }

        float previousHealth = currentHealth;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        return previousHealth - currentHealth;
    }

    /// <summary>
    /// Reset health to full.
    /// </summary>
    public void HealToFull()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Add soul points. Returns actual amount added (capped at 100).
    /// </summary>
    public int AddSoul(int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int previousSoul = collectedSoul;
        collectedSoul = Mathf.Clamp(collectedSoul + amount, 0, 100);
        return collectedSoul - previousSoul;
    }

    /// <summary>
    /// Spend soul points (used for purchases/healing). Returns true if successful.
    /// </summary>
    public bool SpendSoul(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (collectedSoul < amount)
        {
            return false;
        }

        collectedSoul -= amount;
        return true;
    }

    public bool RollCritical()
    {
        return Random.value <= critChance;
    }

    public bool RollDodge()
    {
        return Random.value <= dodgeChance;
    }

    private float[] GetWeaponPool(PlayerFormType form)
    {
        return form == PlayerFormType.Melee
            ? meleeWeaponBaseDamage
            : rangedWeaponBaseDamage;
    }

    private static void EnsureWeaponArray(ref float[] values, float a, float b, float c)
    {
        if (values != null && values.Length == 3)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = Mathf.Max(0.01f, values[i]);
            }
            return;
        }

        values = new float[3] { a, b, c };
    }
}
