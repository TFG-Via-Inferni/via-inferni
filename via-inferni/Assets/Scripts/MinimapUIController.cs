using UnityEngine;
using UnityEngine.UI;

public class MinimapUIController : MonoBehaviour
{
    private const string MinimapImageObjectName = "MinimapImage";
    private const string MinimapContainerObjectName = "MinimapContainer";
    private const string MinimapMaskObjectName = "MinimapMask";
    private const string MinimapBackdropObjectName = "MinimapBackdrop";
    private const string MinimapFrameObjectName = "MinimapFrame";

    [Header("Minimap Visuals")]
    [SerializeField] private Sprite minimapFrameSprite;
    [SerializeField] private Color minimapBackdropColor = new Color(0.06f, 0.06f, 0.06f, 0.72f);
    [SerializeField] private Color minimapFrameTint = Color.white;

    [Header("Minimap Responsive")]
    [Range(0.08f, 0.35f)]
    [SerializeField] private float minimapSizePercentOfScreen = 0.18f;
    [Range(0.005f, 0.08f)]
    [SerializeField] private float minimapMarginPercentOfScreen = 0.03f;
    [SerializeField] private Vector2 minimapPadding = new Vector2(3f, 3f);
    [SerializeField] private float minimapMinSize = 120f;
    [SerializeField] private float minimapMaxSize = 320f;

    private RawImage minimapImage;
    private RectTransform minimapImageRect;
    private RectTransform minimapContainerRect;
    private RectTransform minimapMaskRect;
    private Image minimapBackdropImage;
    private Image minimapFrameImage;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        ResolveReferences();
        ApplyLayout(true);
    }

    private void Start()
    {
        ApplyLayout(true);
    }

    private void Update()
    {
        if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
        {
            return;
        }

        ApplyLayout(true);
    }

    private void ResolveReferences()
    {
        if (minimapImage == null)
        {
            GameObject minimapObject = GameObject.Find(MinimapImageObjectName);
            if (minimapObject != null)
            {
                minimapImage = minimapObject.GetComponent<RawImage>();
            }
        }

        if (minimapImage == null)
        {
            return;
        }

        minimapImageRect = minimapImage.rectTransform;
        minimapImage.raycastTarget = false;

        EnsureContainer();
        EnsureBackdrop();
        EnsureFrame();
    }

    private void EnsureContainer()
    {
        if (minimapImageRect == null)
        {
            return;
        }

        Transform currentParent = minimapImageRect.parent;
        if (currentParent == null)
        {
            return;
        }

        Transform existingContainer = currentParent.Find(MinimapContainerObjectName);
        if (existingContainer != null)
        {
            minimapContainerRect = existingContainer.GetComponent<RectTransform>();
        }

        if (minimapContainerRect == null)
        {
            GameObject containerObject = new GameObject(MinimapContainerObjectName, typeof(RectTransform));
            containerObject.transform.SetParent(currentParent, false);
            minimapContainerRect = containerObject.GetComponent<RectTransform>();
        }

        // Ensure there is an inner mask object so we can clip the minimap content without clipping the frame
        Transform existingMask = minimapContainerRect.Find(MinimapMaskObjectName);
        if (existingMask != null)
        {
            minimapMaskRect = existingMask.GetComponent<RectTransform>();
        }

        if (minimapMaskRect == null)
        {
            GameObject maskObject = new GameObject(MinimapMaskObjectName, typeof(RectTransform));
            maskObject.transform.SetParent(minimapContainerRect, false);
            minimapMaskRect = maskObject.GetComponent<RectTransform>();
            // add RectMask2D to clip child graphics
            var rectMask = maskObject.AddComponent<UnityEngine.UI.RectMask2D>();
        }

        if (minimapImageRect.parent != minimapMaskRect)
        {
            minimapImageRect.SetParent(minimapMaskRect, false);
        }

        minimapImageRect.anchorMin = Vector2.zero;
        minimapImageRect.anchorMax = Vector2.one;
        minimapImageRect.pivot = new Vector2(0.5f, 0.5f);
        minimapImageRect.offsetMin = Vector2.zero;
        minimapImageRect.offsetMax = Vector2.zero;
        minimapImageRect.anchoredPosition = Vector2.zero;
        minimapImageRect.sizeDelta = Vector2.zero;
        minimapImageRect.SetAsLastSibling();
    }

    private void EnsureBackdrop()
    {
        if (minimapContainerRect == null)
        {
            return;
        }

        Transform existingBackdrop = minimapContainerRect.Find(MinimapBackdropObjectName);
        if (existingBackdrop != null)
        {
            minimapBackdropImage = existingBackdrop.GetComponent<Image>();
        }

        if (minimapBackdropImage == null)
        {
            GameObject backdropObject = new GameObject(MinimapBackdropObjectName, typeof(RectTransform), typeof(Image));
            // backdrop should be inside the mask so it is clipped with the minimap content
            backdropObject.transform.SetParent(minimapMaskRect != null ? minimapMaskRect : minimapContainerRect, false);
            minimapBackdropImage = backdropObject.GetComponent<Image>();
        }

        ConfigureFillImage(minimapBackdropImage, null, minimapBackdropColor);
        if (minimapMaskRect != null)
            minimapBackdropImage.transform.SetSiblingIndex(0);
    }

    private void EnsureFrame()
    {
        if (minimapContainerRect == null)
        {
            return;
        }

        Transform existingFrame = minimapContainerRect.Find(MinimapFrameObjectName);
        if (existingFrame != null)
        {
            minimapFrameImage = existingFrame.GetComponent<Image>();
        }

        if (minimapFrameImage == null)
        {
            GameObject frameObject = new GameObject(MinimapFrameObjectName, typeof(RectTransform), typeof(Image));
            // frame must be sibling of mask (not a child) so it is not clipped
            frameObject.transform.SetParent(minimapContainerRect, false);
            minimapFrameImage = frameObject.GetComponent<Image>();
        }

        ConfigureFillImage(minimapFrameImage, minimapFrameSprite, Color.white);
        minimapFrameImage.type = Image.Type.Simple;
        minimapFrameImage.transform.SetAsLastSibling();
    }

    private void ApplyLayout(bool force)
    {
        ResolveReferences();

        if (minimapContainerRect == null || minimapImageRect == null)
        {
            return;
        }

        if (!force && Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float baseSize = Mathf.Min(Screen.width, Screen.height) * minimapSizePercentOfScreen;
        float minimapSize = Mathf.Clamp(baseSize, minimapMinSize, minimapMaxSize);
        
        // Frame size includes padding so the frame border is visible
        Vector2 frameSize = new Vector2(
            minimapSize + minimapPadding.x * 2f,
            minimapSize + minimapPadding.y * 2f
        );

        float marginX = Screen.width * minimapMarginPercentOfScreen;
        float marginY = Screen.height * minimapMarginPercentOfScreen;
        Vector2 containerPosition = new Vector2(
            -(marginX + frameSize.x * 0.5f),
            -(marginY + frameSize.y * 0.5f)
        );

        ConfigureTopRightRect(minimapContainerRect, containerPosition, frameSize);
        ConfigureInsetRect(minimapImageRect, minimapPadding, new Vector2(minimapSize, minimapSize));

        if (minimapMaskRect != null)
        {
            ConfigureInsetRect(minimapMaskRect, minimapPadding, new Vector2(minimapSize, minimapSize));
        }

        if (minimapBackdropImage != null)
        {
            ConfigureStretchRect(minimapBackdropImage.rectTransform);
        }

        if (minimapFrameImage != null)
        {
            ConfigureStretchRect(minimapFrameImage.rectTransform);
        }
    }

    private static void ConfigureFillImage(Image image, Sprite sprite, Color color)
    {
        if (image == null)
        {
            return;
        }

        image.raycastTarget = false;
        image.sprite = sprite;
        image.color = color;
        image.type = sprite != null ? Image.Type.Simple : Image.Type.Simple;
        image.preserveAspect = false;
    }

    private static void ConfigureInsetRect(RectTransform rectTransform, Vector2 padding, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        // Force square aspect ratio (1:1) since RenderTexture is 256x256
        float squareSize = Mathf.Min(size.x, size.y);
        
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = new Vector2(squareSize, squareSize);
    }

    private static void ConfigureTopRightRect(RectTransform rectTransform, Vector2 anchoredPosition, Vector2 size)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
    }

    private static void ConfigureStretchRect(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }
}
