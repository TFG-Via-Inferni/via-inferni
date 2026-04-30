using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class WorldInventoryPickup : MonoBehaviour, IInventoryPickup
{
    private static readonly Vector3 DefaultPickupScale = new Vector3(0.75f, 0.75f, 1f);

    [Header("Pickup")]
    [SerializeField] private InventoryItemDefinition itemDefinition;
    [SerializeField] private bool destroyOnPickup = true;

    private SpriteRenderer spriteRenderer;
    private Collider2D pickupCollider;

    public Transform PickupTransform => transform;
    public InventoryItemDefinition ItemDefinition => itemDefinition;

    private void Awake()
    {
        EnsureComponents();
        RefreshVisual();
    }

    private void OnValidate()
    {
        EnsureComponents();
        RefreshVisual();
    }

    public void Configure(InventoryItemDefinition item)
    {
        itemDefinition = item;
        RefreshVisual();
    }

    public bool TryPickup(PlayerInventory inventory, out string reason)
    {
        reason = string.Empty;

        if (inventory == null)
        {
            reason = "Inventory is null.";
            return false;
        }

        if (itemDefinition == null)
        {
            reason = "Pickup has no item definition.";
            return false;
        }

        if (!inventory.TryAddItem(itemDefinition, out reason))
        {
            return false;
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }

        return true;
    }

    public static WorldInventoryPickup Spawn(InventoryItemDefinition item, Vector3 position, GameObject pickupPrefab = null)
    {
        if (item == null)
        {
            return null;
        }

        if (pickupPrefab != null)
        {
            GameObject instance = Object.Instantiate(pickupPrefab, position, Quaternion.identity);
            WorldInventoryPickup pickupFromPrefab = instance.GetComponent<WorldInventoryPickup>();
            if (pickupFromPrefab == null)
            {
                pickupFromPrefab = instance.AddComponent<WorldInventoryPickup>();
            }

            pickupFromPrefab.Configure(item);
            return pickupFromPrefab;
        }

        GameObject pickupObject = new GameObject($"Pickup_{item.ItemId}");
        pickupObject.transform.position = position;
        pickupObject.transform.localScale = DefaultPickupScale;

        SpriteRenderer renderer = pickupObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 25;

        CircleCollider2D collider = pickupObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.35f;
        collider.isTrigger = true;

        WorldInventoryPickup pickup = pickupObject.AddComponent<WorldInventoryPickup>();
        pickup.Configure(item);
        return pickup;
    }

    private void EnsureComponents()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (pickupCollider == null)
        {
            pickupCollider = GetComponent<Collider2D>();
        }

        if (pickupCollider != null)
        {
            pickupCollider.isTrigger = true;
        }
    }

    private void RefreshVisual()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = itemDefinition != null ? itemDefinition.Icon : null;
    }
}
