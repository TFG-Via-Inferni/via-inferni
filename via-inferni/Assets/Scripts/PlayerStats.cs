using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    [Header("Combat")]
    [Min(0.01f)] [SerializeField] private float damageMultiplier = 1f;
    [Range(0f, 1f)] [SerializeField] private float critChance = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float dodgeChance = 0.05f;

    [Header("Weapons")]
    [SerializeField] private int selectedMeleeWeaponIndex;
    [SerializeField] private int selectedRangedWeaponIndex;
    [SerializeField] private float[] meleeWeaponBaseDamage = new float[3] { 10f, 12f, 15f };
    [SerializeField] private float[] rangedWeaponBaseDamage = new float[3] { 9f, 11f, 14f };

    [Header("Mobility")]
    [Min(0.01f)] [SerializeField] private float moveSpeedMultiplier = 1f;

    [Header("Progression")]
    [Min(0.01f)] [SerializeField] private float luckMultiplier = 1f;
    [Range(3, 12)] [SerializeField] private int inventoryCapacity = 3;
    [Min(0)] [SerializeField] private int coins;

    public float DamageMultiplier => damageMultiplier;
    public float CritChance => critChance;
    public float DodgeChance => dodgeChance;
    public float MoveSpeedMultiplier => moveSpeedMultiplier;
    public float LuckMultiplier => luckMultiplier;
    public int InventoryCapacity => inventoryCapacity;
    public int Coins => coins;

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
        damageMultiplier = Mathf.Max(0.01f, damageMultiplier);
        critChance = Mathf.Clamp01(critChance);
        dodgeChance = Mathf.Clamp01(dodgeChance);
        moveSpeedMultiplier = Mathf.Max(0.01f, moveSpeedMultiplier);
        luckMultiplier = Mathf.Max(0.01f, luckMultiplier);
        inventoryCapacity = Mathf.Clamp(inventoryCapacity, 3, 12);
        coins = Mathf.Max(0, coins);

        EnsureWeaponArray(ref meleeWeaponBaseDamage, 10f, 12f, 15f);
        EnsureWeaponArray(ref rangedWeaponBaseDamage, 9f, 11f, 14f);

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
                2 => "Spear",
                _ => "Axe"
            }
            : slot switch
            {
                1 => "Bow",
                2 => "Magic",
                _ => "Ballista"
            };
    }

    public bool IncreaseInventoryCapacity(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        int previous = inventoryCapacity;
        inventoryCapacity = Mathf.Clamp(inventoryCapacity + amount, 3, 12);
        return inventoryCapacity != previous;
    }

    public bool AddCoins(int amount)
    {
        if (amount <= 0)
        {
            return false;
        }

        coins += amount;
        return true;
    }

    public bool SpendCoins(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (coins < amount)
        {
            return false;
        }

        coins -= amount;
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
