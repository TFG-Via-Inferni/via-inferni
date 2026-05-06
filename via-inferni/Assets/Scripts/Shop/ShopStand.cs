using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class ShopStand : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private SpriteRenderer standSpriteRenderer;
    [SerializeField] private SpriteRenderer itemSpriteRenderer;

    [Header("Price UI")]
    [SerializeField] private TextMeshProUGUI priceTextTMP;
    [SerializeField] private Text priceTextUI;

    [Header("Runtime")]
    [SerializeField] private InventoryItemDefinition itemDefinition;
    [SerializeField] private int price;

    public InventoryItemDefinition ItemDefinition => itemDefinition;
    public int Price => price;

    public void SetItem(InventoryItemDefinition def, int price)
    {
        itemDefinition = def;
        this.price = price;
        UpdateVisuals();
    }

    public void ClearItem()
    {
        itemDefinition = null;
        price = 0;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (itemSpriteRenderer != null)
        {
            itemSpriteRenderer.sprite = itemDefinition != null ? itemDefinition.Icon : null;
            itemSpriteRenderer.enabled = itemDefinition != null && itemSpriteRenderer.sprite != null;
        }

        string text = price > 0 ? price.ToString() : string.Empty;
        if (priceTextTMP != null)
        {
            priceTextTMP.text = text;
            priceTextTMP.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        if (priceTextUI != null)
        {
            priceTextUI.text = text;
            priceTextUI.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }
    }

    /// <summary>
    /// Attempt to purchase the item: spend player's souls and add to inventory.
    /// Returns true if purchase succeeded; on failure any spent souls are refunded.
    /// </summary>
    public bool TryPurchase(PlayerInventory inventory, Player player)
    {
        if (itemDefinition == null || inventory == null || player == null || player.Stats == null)
        {
            return false;
        }

        if (!player.Stats.SpendSoul(price))
        {
            return false; // not enough souls
        }

        // Try to add item to inventory; if it fails, refund souls
        if (!inventory.TryAddItem(itemDefinition, out string reason))
        {
            player.Stats.AddSoul(price);
            Debug.LogWarning($"ShopStand: compra fallida: {reason}");
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
