using UnityEngine;
using TMPro;
using System.Text;
using System;
using UnityEngine.UI;

public class CircleUI : MonoBehaviour
{
    private const float UiRefreshInterval = 0.1f;
    private const float PlayerLookupInterval = 0.5f;
    private const float HeartHpPerFullHeart = 2f;
    private const string MinimapImageObjectName = "MinimapImage";
    private const string MinimapContainerObjectName = "MinimapContainer";
    private const string CircleTextObjectName = "CircleText";
    private static readonly Color SoulCountDisplayColor = new Color(1f, 1f, 1f, 0.99f);

    public static CircleUI instance;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI circleText;

    [Header("Circle Label")]
    [SerializeField] private float circleLabelWidthPercentOfScreen = 0.16f;
    [SerializeField] private float circleLabelHeightPercentOfScreen = 0.04f;
    [SerializeField] private float circleLabelVerticalGap = 6f;

    [Header("Circle Key Indicator")]
    [SerializeField] private bool showCircleKeyIndicator = true;
    [SerializeField] private Vector2 circleKeyIconOffset = new Vector2(-10f, 18f);
    [SerializeField] private Vector2 circleKeyTextOffset = new Vector2(-56f, 18f);
    [SerializeField] private float circleKeyIconSizeMultiplier = 0.62f;
    [SerializeField] private float circleKeyTextWidthMultiplier = 1.45f;
    [SerializeField] private float circleKeyTextHeightMultiplier = 0.9f;
    [SerializeField] private float circleKeyResponsiveWidthThreshold = 220f;
    [SerializeField] private float circleKeyTextVerticalDrop = 14f;
    [SerializeField] private float circleKeySmallScreenTextVerticalDrop = 8f;
    [SerializeField] private float circleKeyIconGap = 8f;
    [SerializeField] private float circleKeySmallScreenIconGap = 2f;

    [Header("Stats Overlay")]
    [SerializeField] private bool showStatsOverlay = true;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Color statsColor = new Color(0.72f, 0.72f, 0.72f, 0.62f);
    [SerializeField] private int statsFontSize = 50;
    [SerializeField] private Vector2 statsAnchoredPosition = new Vector2(24f, 24f);
    [Range(0.15f, 0.7f)] [SerializeField] private float statsWidthPercentOfScreen = 0.54f;
    [Range(0.18f, 0.7f)] [SerializeField] private float statsHeightPercentOfScreen = 0.6f;
    [Range(0.015f, 0.08f)] [SerializeField] private float statsMarginPercentOfScreen = 0.025f;
    [Range(0.015f, 0.09f)] [SerializeField] private float statsFontPercentOfScreenHeight = 0.065f;
    [Range(0.01f, 0.07f)] [SerializeField] private float statsMinFontPercentOfScreenHeight = 0.05f;
    [Range(0.02f, 0.14f)] [SerializeField] private float statsMaxFontPercentOfScreenHeight = 0.09f;

    [Header("Selection HUD")]
    [SerializeField] private bool showSelectionHud = true;
    [SerializeField] private Image selectedCharacterImage;
    [SerializeField] private Image selectedWeaponImage;
    [SerializeField] private Vector2 characterSize = new Vector2(128f, 128f);
    [SerializeField] private Vector2 weaponSize = new Vector2(128f, 128f);

    [Header("Selection HUD Responsive")]
    [Range(0.02f, 0.35f)] [SerializeField] private float iconHeightPercentOfScreen = 0.15f;
    [Range(0.005f, 0.08f)] [SerializeField] private float marginPercentOfScreen = 0.05f;

    [Header("Character Sprites")]
    [SerializeField] private Sprite danteSelectedSprite;
    [SerializeField] private Sprite virgilioSelectedSprite;

    [Header("Weapon Sprites")]
    [SerializeField] private Sprite[] danteWeaponSelectedSprites = new Sprite[3];
    [SerializeField] private Sprite[] virgilioWeaponSelectedSprites = new Sprite[3];

    [Header("Vitals HUD")]
    [SerializeField] private bool showVitalsHud = true;
    [SerializeField] private Sprite heartEmptySprite;
    [SerializeField] private Sprite heartHalfOverlaySprite;
    [SerializeField] private Sprite heartFullOverlaySprite;
    [SerializeField] private Sprite[] soulCruetSprites = new Sprite[5];

    [Header("Vitals HUD Responsive")]
    [Range(0.02f, 0.25f)] [SerializeField] private float cruetHeightPercentOfScreen = 0.15f;
    [Range(0.01f, 0.2f)] [SerializeField] private float heartHeightPercentOfScreen = 0.1f;
    [Range(0.05f, 0.6f)] [SerializeField] private float heartsSpacingPercentOfHeartSize = 0.05f;
    [Range(0.02f, 0.5f)] [SerializeField] private float vitalsGapPercentOfCruet = 0.05f;
    [SerializeField] private int heartsPerRow = 12;

    [Header("Inventory HUD")]
    [SerializeField] private bool showInventoryHud = true;
    [SerializeField] private Sprite inventoryBackgroundSprite;
    [SerializeField] private Sprite inventoryLockSprite;
    [SerializeField] private Vector2 inventoryHudSize = new Vector2(500f, 220f);
    [Range(0.05f, 0.35f)] [SerializeField] private float inventoryHeightPercentOfScreen = 0.2f;
    [SerializeField] private Color inventoryLockedColor = new Color(0.22f, 0.22f, 0.22f, 0.82f);
    [SerializeField] private Color inventoryUnlockedColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color inventorySelectedColor = new Color(0.95f, 0.8f, 0.25f, 0.8f);
    [SerializeField] private Color inventoryFeedbackColor = new Color(1f, 0.75f, 0.35f, 0.95f);
    [SerializeField] private float inventoryFeedbackDuration = 1.2f;
    [SerializeField] private Color inventoryKeyReadyColor = new Color(0.95f, 0.82f, 0.26f, 0.98f);
    [SerializeField] private Color inventoryKeyMissingColor = new Color(0.66f, 0.66f, 0.66f, 0.95f);

    private readonly StringBuilder statsBuilder = new StringBuilder(256);
    private Player trackedPlayer;
    private float nextPlayerLookupTime;
    private float nextUiRefreshTime;
    private bool selectionSpritesLoaded;
    private bool vitalsSpritesLoaded;
    private RectTransform minimapContainerRect;
    private Transform statsDefaultParent;
    private RectTransform vitalsHudRoot;
    private Image soulCruetImage;
    private TextMeshProUGUI soulCountText;
    private RectTransform heartsRoot;
    private Image[] heartBackgroundImages = new Image[0];
    private Image[] heartOverlayImages = new Image[0];
    private float currentHeartSize;
    private float currentHeartSpacing;
    private int currentSoulCountFontSize;
    private RectTransform inventoryHudRoot;
    private Image inventoryBackgroundImage;
    private Image inventoryFrameOverlayImage;
    private RectTransform inventorySlotsRoot;
    private TextMeshProUGUI inventoryFeedbackText;
    private Image inventoryKeyImage;
    private TextMeshProUGUI inventoryKeyText;
    private InventorySlotUi[] inventorySlotUis = Array.Empty<InventorySlotUi>();
    private Vector2 currentInventoryHudSize;

    private void Awake()
    {
        instance = this;
        statsDefaultParent = transform;

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        EnsureStatsTextReference();
        EnsureSelectionHudReferences();
        EnsureSelectionSpritesLoaded();
        EnsureVitalsHudReferences();
        EnsureVitalsSpritesLoaded();
        EnsureInventoryHudReferences();
        EnsureCircleTextLayout();
    }

    private void Start()
    {
        UpdateCircleDisplay();
        UpdateStatsDisplay();
        UpdateSelectionHudDisplay();
        UpdateVitalsHudDisplay();
        UpdateInventoryHudDisplay();
        EnsureCircleTextLayout();
    }

    private void Update()
    {
        EnsureSelectionSpritesLoaded();
        EnsureVitalsSpritesLoaded();

        if (!showStatsOverlay && statsText != null && statsText.gameObject.activeSelf)
        {
            statsText.gameObject.SetActive(false);
        }

        if (Time.unscaledTime >= nextUiRefreshTime)
        {
            nextUiRefreshTime = Time.unscaledTime + UiRefreshInterval;
            UpdateCircleDisplay();
            UpdateStatsDisplay();
            UpdateSelectionHudDisplay();
            UpdateVitalsHudDisplay();
            UpdateInventoryHudDisplay();
            EnsureCircleTextLayout();
        }
    }

    public void UpdateCircleDisplay()
    {
        EnsureCircleTextLayout();

        if (circleText != null && CircleManager.instance != null)
        {
            circleText.text = CircleManager.instance.GetCircleName();
        }
    }

    private void EnsureCircleTextLayout()
    {
        RectTransform targetParent = ResolveMinimapContainer();

        if (circleText == null)
        {
            circleText = EnsureTextReference(targetParent, CircleTextObjectName);
        }

        if (circleText == null)
        {
            return;
        }

        if (targetParent != null && circleText.transform.parent != targetParent)
        {
            circleText.transform.SetParent(targetParent, false);
        }

        RectTransform rect = circleText.rectTransform;
        if (rect == null)
        {
            return;
        }

        float safeScreenWidth = Mathf.Max(1f, Screen.width);
        float safeScreenHeight = Mathf.Max(1f, Screen.height);

        float labelWidth = safeScreenWidth * circleLabelWidthPercentOfScreen;
        float labelHeight = safeScreenHeight * circleLabelHeightPercentOfScreen;
        int circleFontSize = Mathf.RoundToInt(labelHeight * 0.35f);

        if (targetParent != null)
        {
            Rect parentRect = targetParent.rect;
            float parentWidth = Mathf.Max(1f, parentRect.width);
            float parentHeight = Mathf.Max(1f, parentRect.height);

            labelWidth = parentWidth;
            labelHeight = Mathf.Max(1f, parentHeight * 0.14f);
            circleFontSize = Mathf.RoundToInt(parentHeight * 0.08f);

            ConfigureTopLeftRect(
                rect,
                new Vector2(0f, -(parentHeight + circleLabelVerticalGap)),
                new Vector2(labelWidth, labelHeight)
            );
        }
        else
        {
            ConfigureTopLeftRect(
                rect,
                new Vector2(15f, -10f),
                new Vector2(labelWidth, labelHeight)
            );
        }

        circleText.alignment = TextAlignmentOptions.Center;
        circleText.textWrappingMode = TextWrappingModes.NoWrap;
        circleText.raycastTarget = false;
        circleText.enableAutoSizing = false;
        circleText.fontSize = Mathf.Max(1f, circleFontSize);

        if (circleText.font == null && statsText != null && statsText.font != null)
        {
            circleText.font = statsText.font;
        }

        EnsureCircleKeyIndicatorLayout(targetParent, labelHeight, circleFontSize);
    }

    private void EnsureCircleKeyIndicatorLayout(RectTransform targetParent, float labelHeight, int circleFontSize)
    {
        if (inventoryKeyImage == null)
        {
            inventoryKeyImage = EnsureImageReference(targetParent != null ? targetParent : (RectTransform)transform, "InventoryKeyImage");
        }

        if (inventoryKeyText == null)
        {
            inventoryKeyText = EnsureTextReference(targetParent != null ? targetParent : (RectTransform)transform, "InventoryKeyText");
        }

        if (inventoryKeyImage == null || inventoryKeyText == null)
        {
            return;
        }

        RectTransform desiredParent = targetParent != null ? targetParent : (RectTransform)transform;
        if (inventoryKeyImage.transform.parent != desiredParent)
        {
            inventoryKeyImage.transform.SetParent(desiredParent, false);
        }

        if (inventoryKeyText.transform.parent != desiredParent)
        {
            inventoryKeyText.transform.SetParent(desiredParent, false);
        }

        float iconSize = Mathf.Max(12f, labelHeight * Mathf.Max(0.25f, circleKeyIconSizeMultiplier));
        float textWidth = Mathf.Max(90f, labelHeight * Mathf.Max(1f, circleKeyTextWidthMultiplier));
        float textHeight = Mathf.Max(14f, labelHeight * Mathf.Max(0.8f, circleKeyTextHeightMultiplier));
        float baseY = -(targetParent != null ? targetParent.rect.height + circleLabelVerticalGap + labelHeight : 10f + labelHeight);
        float availableWidth = targetParent != null ? targetParent.rect.width : Screen.width;
        float smallScreenFactor = Mathf.Clamp01((circleKeyResponsiveWidthThreshold - availableWidth) / Mathf.Max(1f, circleKeyResponsiveWidthThreshold));
        float textResponsiveDrop = circleKeyTextVerticalDrop + (circleKeySmallScreenTextVerticalDrop * smallScreenFactor);
        float iconGap = Mathf.Lerp(circleKeyIconGap, circleKeySmallScreenIconGap, smallScreenFactor);
        float keyTextX = circleKeyTextOffset.x;
        float keyTextY = baseY + circleKeyTextOffset.y - textResponsiveDrop;
        float keyTextCenterY = keyTextY - (textHeight * 0.5f);

        RectTransform keyImageRect = inventoryKeyImage.rectTransform;
        keyImageRect.anchorMin = new Vector2(1f, 1f);
        keyImageRect.anchorMax = new Vector2(1f, 1f);
        keyImageRect.pivot = new Vector2(0f, 0.5f);
        keyImageRect.anchoredPosition = new Vector2(keyTextX + iconGap, keyTextCenterY);
        keyImageRect.sizeDelta = new Vector2(iconSize, iconSize);
        inventoryKeyImage.raycastTarget = false;
        inventoryKeyImage.preserveAspect = true;

        RectTransform keyTextRect = inventoryKeyText.rectTransform;
        keyTextRect.anchorMin = new Vector2(1f, 1f);
        keyTextRect.anchorMax = new Vector2(1f, 1f);
        keyTextRect.pivot = new Vector2(1f, 1f);
        keyTextRect.anchoredPosition = new Vector2(keyTextX, keyTextY);
        keyTextRect.sizeDelta = new Vector2(textWidth, textHeight);
        inventoryKeyText.alignment = TextAlignmentOptions.Right;
        inventoryKeyText.fontSize = Mathf.Max(1, Mathf.RoundToInt(circleFontSize * 0.8f));
        inventoryKeyText.raycastTarget = false;
        inventoryKeyText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private RectTransform ResolveMinimapContainer()
    {
        if (minimapContainerRect != null)
        {
            return minimapContainerRect;
        }

        GameObject minimapContainer = GameObject.Find(MinimapContainerObjectName);
        if (minimapContainer != null)
        {
            minimapContainerRect = minimapContainer.GetComponent<RectTransform>();
            if (minimapContainerRect != null)
            {
                return minimapContainerRect;
            }
        }

        GameObject minimapImage = GameObject.Find(MinimapImageObjectName);
        if (minimapImage == null)
        {
            return null;
        }

        Transform maskTransform = minimapImage.transform.parent;
        if (maskTransform == null)
        {
            return null;
        }

        Transform containerTransform = maskTransform.parent;
        if (containerTransform == null)
        {
            return null;
        }

        minimapContainerRect = containerTransform.GetComponent<RectTransform>();
        return minimapContainerRect;
    }

    private void UpdateStatsDisplay()
    {
        EnsureStatsTextReference();
        if (statsText == null)
        {
            return;
        }

        bool showPausedStats = showStatsOverlay && PauseMenuController.IsPaused;
        statsText.gameObject.SetActive(showPausedStats);
        if (!showPausedStats)
        {
            return;
        }

        if (!TryResolveTrackedPlayer())
        {
            statsText.text = "Stats: waiting for player...";
            return;
        }

        PlayerStats stats = trackedPlayer.Stats;

        statsBuilder.Clear();
        statsBuilder.AppendLine("CURRENT STATS");
        statsBuilder.Append("Health: ")
            .Append(trackedPlayer.CurrentHealth.ToString("0"))
            .Append(" / ")
            .Append(trackedPlayer.MaxHealth.ToString("0"))
            .AppendLine();

        statsBuilder.Append("Form: ")
            .Append(trackedPlayer.CurrentForm)
            .AppendLine();

        if (stats == null)
        {
            statsBuilder.AppendLine("Stats: unavailable");
            statsText.text = statsBuilder.ToString();
            return;
        }

        statsBuilder.Append("Weapon: ")
            .Append(stats.GetSelectedWeaponId(trackedPlayer.CurrentForm))
            .AppendLine();

        statsBuilder.Append("Damage: ")
            .Append(stats.GetFinalDamage(trackedPlayer.CurrentForm).ToString("0.0"))
            .Append(" (x")
            .Append(stats.DamageMultiplier.ToString("0.00"))
            .AppendLine(")");

        statsBuilder.Append("Speed: x")
            .Append(stats.MoveSpeedMultiplier.ToString("0.00"))
            .AppendLine();

        statsBuilder.Append("Soul: ")
            .Append(stats.CollectedSoul)
            .Append("/100")
            .AppendLine();

        statsBuilder.Append("Crit: ")
            .Append((stats.CritChance * 100f).ToString("0"))
            .Append("%   Dodge: ")
            .Append((stats.DodgeChance * 100f).ToString("0"))
            .AppendLine("%");

        statsBuilder.Append("Bag: ")
            .Append(stats.InventoryCapacity)
            .AppendLine();

        statsText.text = statsBuilder.ToString();
    }

    public void SetStatsPauseVisibility(bool visible)
    {
        EnsureStatsTextReference();
        if (statsText == null)
        {
            return;
        }

        Transform targetParent = visible
            ? PauseMenuController.PauseMenuTransform
            : statsDefaultParent;

        if (targetParent != null && statsText.transform.parent != targetParent)
        {
            statsText.transform.SetParent(targetParent, false);
        }

        if (visible)
        {
            EnsureStatsTextReference();
            UpdateStatsDisplay();
            statsText.gameObject.SetActive(showStatsOverlay);
            return;
        }

        statsText.gameObject.SetActive(false);
    }

    private void EnsureStatsTextReference()
    {
        if (statsText == null)
        {
            Transform existing = transform.Find("StatsOverlayText");
            if (existing != null)
            {
                statsText = existing.GetComponent<TextMeshProUGUI>();
            }
        }

        if (statsText == null)
        {
            GameObject textObj = new GameObject("StatsOverlayText", typeof(RectTransform));
            textObj.transform.SetParent(statsDefaultParent != null ? statsDefaultParent : transform, false);
            statsText = textObj.AddComponent<TextMeshProUGUI>();
        }

        RectTransform rect = statsText.rectTransform;
        float safeScreenWidth = Mathf.Max(1f, Screen.width);
        float safeScreenHeight = Mathf.Max(1f, Screen.height);
        float shorterSide = Mathf.Min(safeScreenWidth, safeScreenHeight);

        float margin = shorterSide * statsMarginPercentOfScreen;
        float width = Mathf.Clamp(safeScreenWidth * statsWidthPercentOfScreen, 460f, safeScreenWidth * 0.68f);
        float height = Mathf.Clamp(safeScreenHeight * statsHeightPercentOfScreen, 340f, safeScreenHeight * 0.74f);
        int preferredFontSize = Mathf.Max(
            statsFontSize,
            Mathf.RoundToInt(safeScreenHeight * statsFontPercentOfScreenHeight));
        int minAutoFontSize = Mathf.Clamp(
            Mathf.RoundToInt(safeScreenHeight * statsMinFontPercentOfScreenHeight),
            34,
            58);
        int maxAutoFontSize = Mathf.Clamp(
            Mathf.RoundToInt(safeScreenHeight * statsMaxFontPercentOfScreenHeight),
            Mathf.Max(minAutoFontSize + 2, preferredFontSize),
            124);

        ConfigureBottomLeftRect(
            rect,
            new Vector2(
                statsAnchoredPosition.x + margin,
                statsAnchoredPosition.y + margin),
            new Vector2(width, height)
        );

        statsText.color = statsColor;
        statsText.enableAutoSizing = true;
        statsText.fontSizeMin = minAutoFontSize;
        statsText.fontSizeMax = maxAutoFontSize;
        statsText.fontSize = preferredFontSize;
        statsText.alignment = TextAlignmentOptions.BottomLeft;
        statsText.textWrappingMode = TextWrappingModes.Normal;
        statsText.enableWordWrapping = true;
        statsText.overflowMode = TextOverflowModes.Overflow;
        statsText.lineSpacing = -4f;
        statsText.raycastTarget = false;

        if (statsText.font == null && circleText != null)
        {
            statsText.font = circleText.font;
        }
    }

    private void EnsureSelectionHudReferences()
    {
        if (selectedCharacterImage == null)
        {
            selectedCharacterImage = EnsureImageReference("SelectedCharacterImage");
        }

        if (selectedWeaponImage == null)
        {
            selectedWeaponImage = EnsureImageReference("SelectedWeaponImage");
        }

        float safeScreenHeight = Mathf.Max(1f, Screen.height);
        float safeScreenWidth = Mathf.Max(1f, Screen.width);
        float shorterSide = Mathf.Min(safeScreenWidth, safeScreenHeight);

        float baseHeight = safeScreenHeight * iconHeightPercentOfScreen;
        float characterAspect = characterSize.y > 0f ? characterSize.x / characterSize.y : 1f;
        float weaponAspect = GetCurrentWeaponAspectRatio();

        Vector2 scaledCharacterSize = new Vector2(baseHeight * characterAspect, baseHeight);
        Vector2 scaledWeaponSize = new Vector2(baseHeight * weaponAspect, baseHeight);

        float margin = shorterSide * marginPercentOfScreen;
        Vector2 characterPosition = new Vector2(margin, margin);

        Vector2 weaponPosition = new Vector2(
            characterPosition.x + Mathf.Max(0f, scaledCharacterSize.x),
            characterPosition.y
        );

        ConfigureImageRect(selectedCharacterImage, characterPosition, scaledCharacterSize);
        ConfigureImageRect(selectedWeaponImage, weaponPosition, scaledWeaponSize);
    }

    private Image EnsureImageReference(string objectName)
    {
        Transform existing = transform.Find(objectName);
        GameObject imageObject;

        if (existing != null)
        {
            imageObject = existing.gameObject;
        }
        else
        {
            imageObject = new GameObject(objectName, typeof(RectTransform));
            imageObject.transform.SetParent(transform, false);
        }

        Image image = imageObject.GetComponent<Image>();
        if (image == null)
        {
            image = imageObject.AddComponent<Image>();
        }

        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private static void ConfigureImageRect(Image image, Vector2 anchoredPosition, Vector2 size)
    {
        if (image == null)
        {
            return;
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void UpdateSelectionHudDisplay()
    {
        EnsureSelectionHudReferences();

        if (!showSelectionHud)
        {
            if (selectedCharacterImage != null)
            {
                selectedCharacterImage.enabled = false;
            }

            if (selectedWeaponImage != null)
            {
                selectedWeaponImage.enabled = false;
            }

            return;
        }

        if (!TryResolveTrackedPlayer())
        {
            if (selectedCharacterImage != null)
            {
                selectedCharacterImage.enabled = false;
            }

            if (selectedWeaponImage != null)
            {
                selectedWeaponImage.enabled = false;
            }

            return;
        }

        PlayerFormType form = trackedPlayer.CurrentForm;
        Sprite characterSprite = form == PlayerFormType.Melee
            ? danteSelectedSprite
            : virgilioSelectedSprite;

        if (selectedCharacterImage != null)
        {
            selectedCharacterImage.sprite = characterSprite;
            selectedCharacterImage.enabled = characterSprite != null;
        }

        PlayerStats stats = trackedPlayer.Stats;
        int weaponIndex = stats != null ? stats.GetSelectedWeaponIndex(form) : 0;
        Sprite weaponSprite = GetWeaponSprite(form, weaponIndex);

        if (selectedWeaponImage != null)
        {
            selectedWeaponImage.sprite = weaponSprite;
            selectedWeaponImage.enabled = weaponSprite != null;

            if (weaponSprite != null)
            {
                float safeScreenHeight = Mathf.Max(1f, Screen.height);
                float baseHeight = safeScreenHeight * iconHeightPercentOfScreen;
                float weaponAspect = GetSpriteAspectRatio(weaponSprite);
                Vector2 size = new Vector2(baseHeight * weaponAspect, baseHeight);
                ConfigureImageRect(selectedWeaponImage, selectedWeaponImage.rectTransform.anchoredPosition, size);
            }
        }
    }

    private void EnsureVitalsHudReferences()
    {
        if (vitalsHudRoot == null)
        {
            Transform existing = transform.Find("VitalsHudRoot");
            if (existing != null)
            {
                vitalsHudRoot = existing.GetComponent<RectTransform>();
            }

            if (vitalsHudRoot == null)
            {
                GameObject root = new GameObject("VitalsHudRoot", typeof(RectTransform));
                root.transform.SetParent(transform, false);
                vitalsHudRoot = root.GetComponent<RectTransform>();
            }
        }

        if (soulCruetImage == null)
        {
            soulCruetImage = EnsureImageReference(vitalsHudRoot, "SoulCruetImage");
        }

        if (soulCountText == null && soulCruetImage != null)
        {
            soulCountText = EnsureTextReference(soulCruetImage.rectTransform, "SoulCountText");
        }

        if (heartsRoot == null)
        {
            Transform existing = vitalsHudRoot.Find("HeartsRoot");
            if (existing != null)
            {
                heartsRoot = existing.GetComponent<RectTransform>();
            }

            if (heartsRoot == null)
            {
                GameObject heartsRootObject = new GameObject("HeartsRoot", typeof(RectTransform));
                heartsRootObject.transform.SetParent(vitalsHudRoot, false);
                heartsRoot = heartsRootObject.GetComponent<RectTransform>();
            }
        }

        float safeScreenHeight = Mathf.Max(1f, Screen.height);
        float safeScreenWidth = Mathf.Max(1f, Screen.width);
        float shorterSide = Mathf.Min(safeScreenHeight, safeScreenWidth);

        float margin = shorterSide * marginPercentOfScreen;
        float cruetHeight = safeScreenHeight * cruetHeightPercentOfScreen;
        float heartHeight = safeScreenHeight * heartHeightPercentOfScreen;
        float gap = cruetHeight * vitalsGapPercentOfCruet;

        float cruetAspect = 1f;
        if (soulCruetImage.sprite != null)
        {
            cruetAspect = soulCruetImage.sprite.rect.height > 0f
                ? soulCruetImage.sprite.rect.width / soulCruetImage.sprite.rect.height
                : 1f;
        }

        float heartAspect = 1f;
        if (heartEmptySprite != null)
        {
            heartAspect = heartEmptySprite.rect.height > 0f
                ? heartEmptySprite.rect.width / heartEmptySprite.rect.height
                : 1f;
        }

        Vector2 cruetSize = new Vector2(cruetHeight * cruetAspect, cruetHeight);
        Vector2 heartSize = new Vector2(heartHeight * heartAspect, heartHeight);

        ConfigureTopLeftRect(vitalsHudRoot, new Vector2(margin, -margin), Vector2.zero);
        ConfigureTopLeftRect(soulCruetImage.rectTransform, Vector2.zero, cruetSize);

        currentHeartSize = heartSize.x;
        currentHeartSpacing = currentHeartSize * heartsSpacingPercentOfHeartSize;
        currentSoulCountFontSize = Mathf.RoundToInt(cruetSize.y * 0.2f);

        ConfigureTopLeftRect(heartsRoot, new Vector2(cruetSize.x + gap, 0f), Vector2.zero);

        soulCruetImage.raycastTarget = false;
        soulCruetImage.preserveAspect = true;

        if (soulCountText != null)
        {
            ConfigureCenterRect(soulCountText.rectTransform, new Vector2(0f, -cruetSize.y * 0.2f), cruetSize);
            soulCountText.raycastTarget = false;
            soulCountText.alignment = TextAlignmentOptions.Center;
            soulCountText.color = SoulCountDisplayColor;
            soulCountText.fontSize = currentSoulCountFontSize;
        }
    }

    private void EnsureInventoryHudReferences()
    {
        if (inventoryHudRoot == null)
        {
            Transform existing = transform.Find("InventoryHudRoot");
            if (existing != null)
            {
                inventoryHudRoot = existing.GetComponent<RectTransform>();
            }

            if (inventoryHudRoot == null)
            {
                GameObject root = new GameObject("InventoryHudRoot", typeof(RectTransform));
                root.transform.SetParent(transform, false);
                inventoryHudRoot = root.GetComponent<RectTransform>();
            }
        }

        EnsureSelectionHudReferences();

        float safeScreenHeight = Mathf.Max(1f, Screen.height);
        float safeScreenWidth = Mathf.Max(1f, Screen.width);
        float shorterSide = Mathf.Min(safeScreenWidth, safeScreenHeight);

        float targetHeight = Mathf.Max(64f, safeScreenHeight * inventoryHeightPercentOfScreen);
        float baseAspect = inventoryHudSize.y > 0.001f ? inventoryHudSize.x / inventoryHudSize.y : (500f / 220f);
        float targetWidth = targetHeight * Mathf.Max(0.1f, baseAspect);

        if (inventoryBackgroundSprite != null && inventoryBackgroundSprite.rect.height > 0f)
        {
            float spriteAspect = inventoryBackgroundSprite.rect.width / inventoryBackgroundSprite.rect.height;
            targetWidth = targetHeight * Mathf.Max(0.1f, spriteAspect);
        }

        currentInventoryHudSize = new Vector2(targetWidth, targetHeight);

        Vector2 anchorPosition = new Vector2(shorterSide * marginPercentOfScreen, shorterSide * marginPercentOfScreen);
        if (selectedWeaponImage != null)
        {
            RectTransform weaponRect = selectedWeaponImage.rectTransform;
            anchorPosition = new Vector2(
                weaponRect.anchoredPosition.x + weaponRect.sizeDelta.x,
                weaponRect.anchoredPosition.y
            );
        }

        ConfigureBottomLeftRect(inventoryHudRoot, anchorPosition, currentInventoryHudSize);

        if (inventoryBackgroundImage == null)
        {
            Image image = inventoryHudRoot.GetComponent<Image>();
            if (image == null)
            {
                image = inventoryHudRoot.gameObject.AddComponent<Image>();
            }

            inventoryBackgroundImage = image;
            inventoryBackgroundImage.raycastTarget = false;
            inventoryBackgroundImage.preserveAspect = false;
        }

        inventoryBackgroundImage.sprite = inventoryBackgroundSprite;
        inventoryBackgroundImage.color = inventoryBackgroundSprite != null
            ? new Color(1f, 1f, 1f, 0f)
            : new Color(0f, 0f, 0f, 0.28f);

        if (inventoryFrameOverlayImage == null)
        {
            Transform existingFrame = inventoryHudRoot.Find("InventoryFrameOverlay");
            if (existingFrame != null)
            {
                inventoryFrameOverlayImage = existingFrame.GetComponent<Image>();
            }

            if (inventoryFrameOverlayImage == null)
            {
                GameObject frameObject = new GameObject("InventoryFrameOverlay", typeof(RectTransform), typeof(Image));
                frameObject.transform.SetParent(inventoryHudRoot, false);
                inventoryFrameOverlayImage = frameObject.GetComponent<Image>();
            }
        }

        ConfigureCenterRect(inventoryFrameOverlayImage.rectTransform, Vector2.zero, currentInventoryHudSize);
        inventoryFrameOverlayImage.sprite = inventoryBackgroundSprite;
        inventoryFrameOverlayImage.color = inventoryBackgroundSprite != null
            ? Color.white
            : new Color(1f, 1f, 1f, 0f);
        inventoryFrameOverlayImage.raycastTarget = false;
        inventoryFrameOverlayImage.preserveAspect = false;
        inventoryFrameOverlayImage.transform.SetAsLastSibling();

        if (inventorySlotsRoot == null)
        {
            Transform existing = inventoryHudRoot.Find("InventorySlotsRoot");
            if (existing != null)
            {
                inventorySlotsRoot = existing.GetComponent<RectTransform>();
            }

            if (inventorySlotsRoot == null)
            {
                GameObject slotsRootObject = new GameObject("InventorySlotsRoot", typeof(RectTransform));
                slotsRootObject.transform.SetParent(inventoryHudRoot, false);
                inventorySlotsRoot = slotsRootObject.GetComponent<RectTransform>();
            }
        }

        Vector2 slotsAreaSize = new Vector2(
            Mathf.Max(1f, currentInventoryHudSize.x),
            Mathf.Max(1f, currentInventoryHudSize.y)
        );
        ConfigureCenterRect(inventorySlotsRoot, Vector2.zero, slotsAreaSize);

        EnsureInventorySlotsUi();

        if (inventoryFeedbackText == null)
        {
            Transform existingFeedback = inventoryHudRoot.Find("InventoryFeedbackText");
            if (existingFeedback != null)
            {
                inventoryFeedbackText = existingFeedback.GetComponent<TextMeshProUGUI>();
            }

            if (inventoryFeedbackText == null)
            {
                GameObject feedbackObject = new GameObject("InventoryFeedbackText", typeof(RectTransform));
                feedbackObject.transform.SetParent(inventoryHudRoot, false);
                inventoryFeedbackText = feedbackObject.AddComponent<TextMeshProUGUI>();
            }
        }

        RectTransform feedbackRect = inventoryFeedbackText.rectTransform;
        feedbackRect.anchorMin = new Vector2(0.5f, 1f);
        feedbackRect.anchorMax = new Vector2(0.5f, 1f);
        feedbackRect.pivot = new Vector2(0.5f, 0f);
        feedbackRect.anchoredPosition = new Vector2(0f, 8f);
        feedbackRect.sizeDelta = new Vector2(Mathf.Max(220f, currentInventoryHudSize.x), 38f * Mathf.Max(1f, currentInventoryHudSize.y / 220f));

        inventoryFeedbackText.alignment = TextAlignmentOptions.Center;
        inventoryFeedbackText.fontSize = Mathf.RoundToInt(20f * Mathf.Max(1f, currentInventoryHudSize.y / 220f));
        inventoryFeedbackText.color = inventoryFeedbackColor;
        inventoryFeedbackText.raycastTarget = false;
        inventoryFeedbackText.text = string.Empty;

        if (inventoryFeedbackText.font == null && circleText != null)
        {
            inventoryFeedbackText.font = circleText.font;
        }

    }

    private void EnsureInventorySlotsUi()
    {
        if (inventorySlotsRoot == null)
        {
            return;
        }

        const int totalSlots = PlayerInventory.MaxSlots;
        if (inventorySlotUis.Length == totalSlots)
        {
            LayoutInventorySlots();
            return;
        }

        for (int i = inventorySlotsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(inventorySlotsRoot.GetChild(i).gameObject);
        }

        inventorySlotUis = new InventorySlotUi[totalSlots];
        for (int i = 0; i < totalSlots; i++)
        {
            GameObject slotObject = new GameObject($"Slot_{i + 1}", typeof(RectTransform), typeof(Image));
            slotObject.transform.SetParent(inventorySlotsRoot, false);

            Image slotBg = slotObject.GetComponent<Image>();
            slotBg.raycastTarget = false;
            slotBg.color = inventoryUnlockedColor;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(slotObject.transform, false);
            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            GameObject lockObject = new GameObject("Lock", typeof(RectTransform), typeof(Image));
            lockObject.transform.SetParent(slotObject.transform, false);
            Image lockImage = lockObject.GetComponent<Image>();
            lockImage.raycastTarget = false;
            lockImage.preserveAspect = true;

            inventorySlotUis[i] = new InventorySlotUi
            {
                root = slotObject.GetComponent<RectTransform>(),
                background = slotBg,
                icon = iconImage,
                lockImage = lockImage,
                selectionOutline = null
            };
        }

        LayoutInventorySlots();
    }

    private void LayoutInventorySlots()
    {
        if (inventorySlotsRoot == null || inventorySlotUis.Length == 0)
        {
            return;
        }

        const int columns = 6;
        const int rows = 2;

        Rect rect = inventorySlotsRoot.rect;
        float availableWidth = Mathf.Max(1f, rect.width);
        float availableHeight = Mathf.Max(1f, rect.height);

        float slotWidth = Mathf.Max(16f, availableWidth / columns);
        float slotHeight = Mathf.Max(16f, availableHeight / rows);
        float slotSize = Mathf.Min(slotWidth, slotHeight);

        float gridWidth = columns * slotSize;
        float gridHeight = rows * slotSize;
        float startX = -gridWidth * 0.5f;
        float startY = gridHeight * 0.5f;

        for (int i = 0; i < inventorySlotUis.Length; i++)
        {
            int col = i % columns;
            int row = i / columns;

            float x = startX + (col * slotSize);
            float y = startY - (row * slotSize);

            RectTransform slotRect = inventorySlotUis[i].root;
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.anchoredPosition = new Vector2(x, y);
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);

            ConfigureCenterRect(inventorySlotUis[i].icon.rectTransform, Vector2.zero, new Vector2(slotSize * 0.76f, slotSize * 0.76f));
            ConfigureCenterRect(inventorySlotUis[i].lockImage.rectTransform, Vector2.zero, new Vector2(slotSize * 0.56f, slotSize * 0.56f));
        }
    }

    private void UpdateInventoryHudDisplay()
    {
        EnsureInventoryHudReferences();

        if (inventoryHudRoot == null)
        {
            return;
        }

        inventoryHudRoot.gameObject.SetActive(showInventoryHud);
        if (!showInventoryHud)
        {
            return;
        }

        if (!TryResolveTrackedPlayer())
        {
            SetInventoryFeedback(string.Empty);
            UpdateInventoryKeyDisplay();
            return;
        }

        PlayerInventory inventory = trackedPlayer.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            SetInventoryFeedback(string.Empty);
            UpdateInventoryKeyDisplay();
            return;
        }

        for (int i = 0; i < inventorySlotUis.Length; i++)
        {
            PlayerInventory.InventorySlotState slotState = inventory.GetSlotState(i);
            InventorySlotUi slotUi = inventorySlotUis[i];

            if (slotUi.background != null)
            {
                Color slotColor = slotState.unlocked ? inventoryUnlockedColor : inventoryLockedColor;
                if (slotState.selected)
                {
                    slotColor = inventorySelectedColor;
                }

                slotUi.background.color = slotColor;
            }

            if (slotUi.icon != null)
            {
                slotUi.icon.sprite = slotState.occupied && slotState.item != null ? slotState.item.Icon : null;
                slotUi.icon.enabled = slotUi.icon.sprite != null;
            }

            if (slotUi.lockImage != null)
            {
                slotUi.lockImage.sprite = inventoryLockSprite;
                slotUi.lockImage.enabled = !slotState.unlocked && inventoryLockSprite != null;
            }

            if (slotUi.selectionOutline != null)
            {
                slotUi.selectionOutline.enabled = false;
            }
        }

        bool hasRecentFailure = !string.IsNullOrWhiteSpace(inventory.LastFailureReason)
            && Time.unscaledTime - inventory.LastFailureTimestamp <= inventoryFeedbackDuration;

        if (!hasRecentFailure)
        {
            SetInventoryFeedback(string.Empty);
            UpdateInventoryKeyDisplay();
            return;
        }

        string message = inventory.LastFailureReason == "No free unlocked slots."
            ? "Inventario lleno"
            : string.Empty;

        SetInventoryFeedback(message);
        UpdateInventoryKeyDisplay();
    }

    private void SetInventoryFeedback(string message)
    {
        if (inventoryFeedbackText == null)
        {
            return;
        }

        inventoryFeedbackText.text = message ?? string.Empty;
        inventoryFeedbackText.enabled = !string.IsNullOrWhiteSpace(inventoryFeedbackText.text);
    }

    private void UpdateInventoryKeyDisplay()
    {
        if (inventoryKeyImage == null || inventoryKeyText == null)
        {
            return;
        }

        if (!showCircleKeyIndicator)
        {
            inventoryKeyImage.enabled = false;
            inventoryKeyText.enabled = false;
            return;
        }

        bool hasCircleManager = CircleManager.instance != null;
        bool hasMapGenerator = MapGenerator.instance != null;
        bool hasKey = hasCircleManager && CircleManager.instance.HasCurrentCircleKey;
        int currentCircle = hasCircleManager ? CircleManager.instance.CurrentCircle : 0;

        inventoryKeyImage.sprite = hasMapGenerator ? MapGenerator.instance.CircleKeySprite : null;
        inventoryKeyImage.enabled = inventoryKeyImage.sprite != null;
        inventoryKeyImage.color = hasKey ? inventoryKeyReadyColor : inventoryKeyMissingColor;

        if (!hasCircleManager)
        {
            inventoryKeyText.text = string.Empty;
            inventoryKeyText.enabled = false;
            return;
        }

        inventoryKeyText.enabled = true;
        inventoryKeyText.color = hasKey ? inventoryKeyReadyColor : inventoryKeyMissingColor;
        inventoryKeyText.text = $"{GetShortCircleName()} KEY: {(hasKey ? "YES" : "NO")}";
    }

    private string GetShortCircleName()
    {
        if (CircleManager.instance == null)
        {
            return string.Empty;
        }

        string circleName = CircleManager.instance.GetCircleName();
        if (string.IsNullOrWhiteSpace(circleName))
        {
            return string.Empty;
        }

        int separatorIndex = circleName.LastIndexOf('-');
        string shortName = separatorIndex >= 0 && separatorIndex < circleName.Length - 1
            ? circleName[(separatorIndex + 1)..].Trim()
            : circleName.Trim();

        return shortName.ToUpperInvariant();
    }

    private void UpdateVitalsHudDisplay()
    {
        EnsureVitalsHudReferences();

        if (vitalsHudRoot == null)
        {
            return;
        }

        vitalsHudRoot.gameObject.SetActive(showVitalsHud);
        if (!showVitalsHud)
        {
            return;
        }

        if (!TryResolveTrackedPlayer())
        {
            if (soulCruetImage != null)
            {
                soulCruetImage.enabled = false;
            }
            return;
        }

        PlayerStats stats = trackedPlayer.Stats;
        if (stats == null)
        {
            if (soulCruetImage != null)
            {
                soulCruetImage.enabled = false;
            }

            if (soulCountText != null)
            {
                soulCountText.gameObject.SetActive(false);
            }
            return;
        }

        if (soulCruetImage != null)
        {
            int soulState = GetSoulCruetState(stats.CollectedSoul);
            Sprite stateSprite = GetSoulCruetSprite(soulState);
            soulCruetImage.sprite = stateSprite;
            soulCruetImage.enabled = stateSprite != null;
        }

        if (soulCountText != null)
        {
            soulCountText.gameObject.SetActive(true);
            soulCountText.text = stats.CollectedSoul.ToString();
        }

        int heartSlots = Mathf.Max(1, Mathf.CeilToInt(trackedPlayer.MaxHealth / HeartHpPerFullHeart));
        EnsureHeartsUi(heartSlots);

        int remainingHalfHearts = Mathf.Clamp(Mathf.RoundToInt(trackedPlayer.CurrentHealth), 0, Mathf.CeilToInt(trackedPlayer.MaxHealth));
        for (int i = 0; i < heartOverlayImages.Length; i++)
        {
            Image overlay = heartOverlayImages[i];
            if (overlay == null)
            {
                continue;
            }

            if (remainingHalfHearts >= 2)
            {
                overlay.sprite = heartFullOverlaySprite;
                overlay.enabled = heartFullOverlaySprite != null;
            }
            else if (remainingHalfHearts == 1)
            {
                overlay.sprite = heartHalfOverlaySprite;
                overlay.enabled = heartHalfOverlaySprite != null;
            }
            else
            {
                overlay.sprite = null;
                overlay.enabled = false;
            }

            remainingHalfHearts = Mathf.Max(0, remainingHalfHearts - 2);
        }
    }

    private void EnsureHeartsUi(int heartSlots)
    {
        if (heartsRoot == null)
        {
            return;
        }

        if (heartBackgroundImages.Length > heartSlots)
        {
            for (int i = heartSlots; i < heartBackgroundImages.Length; i++)
            {
                if (i < heartsRoot.childCount)
                {
                    heartsRoot.GetChild(i).gameObject.SetActive(false);
                }
            }
        }

        if (heartBackgroundImages.Length < heartSlots)
        {
            int previousCount = heartBackgroundImages.Length;
            Array.Resize(ref heartBackgroundImages, heartSlots);
            Array.Resize(ref heartOverlayImages, heartSlots);

            for (int i = previousCount; i < heartSlots; i++)
            {
                GameObject heartObject = new GameObject($"Heart_{i}", typeof(RectTransform), typeof(Image));
                heartObject.transform.SetParent(heartsRoot, false);

                Image heartBackground = heartObject.GetComponent<Image>();
                heartBackground.sprite = heartEmptySprite;
                heartBackground.raycastTarget = false;
                heartBackground.preserveAspect = true;

                GameObject overlayObject = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
                overlayObject.transform.SetParent(heartObject.transform, false);

                Image overlay = overlayObject.GetComponent<Image>();
                overlay.raycastTarget = false;
                overlay.preserveAspect = true;
                heartBackgroundImages[i] = heartBackground;
                heartOverlayImages[i] = overlay;
            }
        }

        int cols = Mathf.Max(1, heartsPerRow);
        int rows = Mathf.CeilToInt(heartSlots / (float)cols);

        for (int i = 0; i < heartSlots; i++)
        {
            int row = i / cols;
            int col = i % cols;

            RectTransform heartRect = heartsRoot.GetChild(i).GetComponent<RectTransform>();
            if (!heartRect.gameObject.activeSelf)
            {
                heartRect.gameObject.SetActive(true);
            }

            float x = col * (currentHeartSize + currentHeartSpacing);
            float y = -row * (currentHeartSize + currentHeartSpacing);

            ConfigureTopLeftRect(
                heartRect,
                new Vector2(x, y),
                new Vector2(currentHeartSize, currentHeartSize)
            );

            Image bg = i < heartBackgroundImages.Length ? heartBackgroundImages[i] : heartRect.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = heartEmptySprite;
                bg.enabled = heartEmptySprite != null;
            }

            Image overlayImage = i < heartOverlayImages.Length ? heartOverlayImages[i] : null;
            RectTransform overlayRect = overlayImage != null
                ? overlayImage.rectTransform
                : (heartRect.childCount > 0 ? heartRect.GetChild(0).GetComponent<RectTransform>() : null);
            if (overlayRect != null)
            {
                ConfigureTopLeftRect(overlayRect, Vector2.zero, new Vector2(currentHeartSize, currentHeartSize));
            }
        }

        int maxColsInRow = Mathf.Min(cols, heartSlots);
        float totalWidth = maxColsInRow > 0
            ? (maxColsInRow * currentHeartSize) + ((maxColsInRow - 1) * currentHeartSpacing)
            : 0f;

        float totalHeight = rows > 0
            ? (rows * currentHeartSize) + ((rows - 1) * currentHeartSpacing)
            : 0f;

        ConfigureTopLeftRect(heartsRoot, heartsRoot.anchoredPosition, new Vector2(totalWidth, totalHeight));
    }

    private static int GetSoulCruetState(int soul)
    {
        if (soul <= 0)
        {
            return 0;
        }

        if (soul <= 25)
        {
            return 1;
        }

        if (soul <= 50)
        {
            return 2;
        }

        if (soul <= 75)
        {
            return 3;
        }

        return 4;
    }

    private Sprite GetSoulCruetSprite(int state)
    {
        if (soulCruetSprites == null || soulCruetSprites.Length == 0)
        {
            return null;
        }

        int safeState = Mathf.Clamp(state, 0, soulCruetSprites.Length - 1);
        return soulCruetSprites[safeState];
    }

    private Sprite GetWeaponSprite(PlayerFormType form, int index)
    {
        Sprite[] pool = form == PlayerFormType.Melee
            ? danteWeaponSelectedSprites
            : virgilioWeaponSelectedSprites;

        if (pool == null || pool.Length == 0)
        {
            return null;
        }

        int safeIndex = Mathf.Clamp(index, 0, pool.Length - 1);
        return pool[safeIndex];
    }

    private float GetCurrentWeaponAspectRatio()
    {
        if (selectedWeaponImage != null && selectedWeaponImage.sprite != null)
        {
            return GetSpriteAspectRatio(selectedWeaponImage.sprite);
        }

        return weaponSize.y > 0f ? weaponSize.x / weaponSize.y : 1f;
    }

    private static float GetSpriteAspectRatio(Sprite sprite)
    {
        if (sprite == null || sprite.rect.height <= 0f)
        {
            return 1f;
        }

        return sprite.rect.width / sprite.rect.height;
    }

    private static void ConfigureTopLeftRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void ConfigureBottomLeftRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private static void ConfigureCenterRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private Image EnsureImageReference(RectTransform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        GameObject imageObject;

        if (existing != null)
        {
            imageObject = existing.gameObject;
        }
        else
        {
            imageObject = new GameObject(objectName, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
        }

        Image image = imageObject.GetComponent<Image>();
        if (image == null)
        {
            image = imageObject.AddComponent<Image>();
        }

        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private TextMeshProUGUI EnsureTextReference(RectTransform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find(objectName);
        GameObject textObject;

        if (existing != null)
        {
            textObject = existing.gameObject;
        }
        else
        {
            textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
        }

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

        if (text.font == null)
        {
            if (statsText != null && statsText.font != null)
            {
                text.font = statsText.font;
            }
            else if (circleText != null && circleText.font != null)
            {
                text.font = circleText.font;
            }
        }

        return text;
    }

    private bool TryResolveTrackedPlayer()
    {
        if (trackedPlayer != null)
        {
            return true;
        }

        if (Time.unscaledTime < nextPlayerLookupTime)
        {
            return false;
        }

        trackedPlayer = FindFirstObjectByType<Player>();
        nextPlayerLookupTime = Time.unscaledTime + PlayerLookupInterval;
        return trackedPlayer != null;
    }

    private void EnsureVitalsSpritesLoaded()
    {
        if (vitalsSpritesLoaded)
        {
            return;
        }

        vitalsSpritesLoaded = true;
    }

    private void EnsureSelectionSpritesLoaded()
    {
        if (selectionSpritesLoaded)
        {
            return;
        }

#if UNITY_EDITOR
        danteSelectedSprite ??= UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/hud/characters/dante-selected.png");
        virgilioSelectedSprite ??= UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/hud/characters/virgilio-selected.png");

        EnsureWeaponSprite(ref danteWeaponSelectedSprites, 0, "Assets/Sprites/hud/weapons/dante/sword-selected.png");
        EnsureWeaponSprite(ref danteWeaponSelectedSprites, 1, "Assets/Sprites/hud/weapons/dante/spear-selected.png");
        EnsureWeaponSprite(ref danteWeaponSelectedSprites, 2, "Assets/Sprites/hud/weapons/dante/axe-selected.png");

        EnsureWeaponSprite(ref virgilioWeaponSelectedSprites, 0, "Assets/Sprites/hud/weapons/virgilio/bow-selected.png");
        EnsureWeaponSprite(ref virgilioWeaponSelectedSprites, 1, "Assets/Sprites/hud/weapons/virgilio/magic-selected.png");
        EnsureWeaponSprite(ref virgilioWeaponSelectedSprites, 2, "Assets/Sprites/hud/weapons/virgilio/ballista-selected.png");
#endif

        selectionSpritesLoaded = true;
    }

#if UNITY_EDITOR
    private static void EnsureWeaponSprite(ref Sprite[] pool, int index, string assetPath)
    {
        if (pool == null || pool.Length != 3)
        {
            pool = new Sprite[3];
        }

        if (pool[index] != null)
        {
            return;
        }

        pool[index] = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }
#endif

    private struct InventorySlotUi
    {
        public RectTransform root;
        public Image background;
        public Image icon;
        public Image lockImage;
        public Outline selectionOutline;
    }
}
