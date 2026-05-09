using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class CircleKeyPickup : MonoBehaviour, IInventoryPickup
{
    private static readonly Vector3 DefaultPickupScale = new Vector3(0.75f, 0.75f, 1f);

    [Header("Key Data")]
    [SerializeField] private int circleNumber = 1;
    [SerializeField] private Sprite icon;
    [SerializeField] private bool destroyOnPickup = true;

    private SpriteRenderer spriteRenderer;
    private Collider2D pickupCollider;

    public Transform PickupTransform => transform;

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

    public void Configure(int targetCircleNumber, Sprite targetIcon)
    {
        circleNumber = Mathf.Max(1, targetCircleNumber);
        icon = targetIcon;
        RefreshVisual();
    }

    public bool TryPickup(PlayerInventory inventory, out string reason)
    {
        reason = string.Empty;

        if (CircleManager.instance == null)
        {
            reason = "CircleManager no disponible.";
            return false;
        }

        if (!CircleManager.instance.TryCollectCurrentCircleKey(circleNumber, out reason))
        {
            return false;
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }

        return true;
    }

    public static CircleKeyPickup Spawn(int targetCircleNumber, Vector3 position, Sprite targetIcon, Transform parent = null)
    {
        GameObject keyObject = new GameObject($"CircleKey_{targetCircleNumber}");
        keyObject.transform.position = position;
        keyObject.transform.localScale = DefaultPickupScale;

        if (parent != null)
        {
            keyObject.transform.SetParent(parent);
        }

        SpriteRenderer renderer = keyObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 25;

        CircleCollider2D collider = keyObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.35f;
        collider.isTrigger = true;

        CircleKeyPickup pickup = keyObject.AddComponent<CircleKeyPickup>();
        pickup.Configure(targetCircleNumber, targetIcon);
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
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = icon;
        }
    }
}
