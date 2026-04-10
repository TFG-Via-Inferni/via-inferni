using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.UI;

public class CircleUI : MonoBehaviour
{
    private const float UiRefreshInterval = 0.1f;
    private const float PlayerLookupInterval = 0.5f;
    private const float HeartHpPerFullHeart = 2f;

    public static CircleUI instance;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI circleText;

    [Header("Stats Overlay")]
    [SerializeField] private bool showStatsOverlay = true;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Color statsColor = new Color(0.72f, 0.72f, 0.72f, 0.62f);
    [SerializeField] private int statsFontSize = 20;
    [SerializeField] private Vector2 statsAnchoredPosition = new Vector2(-18f, -18f);

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

    private readonly StringBuilder statsBuilder = new StringBuilder(256);
    private Player trackedPlayer;
    private float nextPlayerLookupTime;
    private float nextUiRefreshTime;
    private bool selectionSpritesLoaded;
    private bool vitalsSpritesLoaded;
    private RectTransform vitalsHudRoot;
    private Image soulCruetImage;
    private RectTransform heartsRoot;
    private Image[] heartOverlayImages = new Image[0];
    private float currentHeartSize;
    private float currentHeartSpacing;

    private void Awake()
    {
        instance = this;
        
        // Asegurar que el Canvas esté en Screen Space - Overlay
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Para que esté por encima de todo
        }

        EnsureStatsTextReference();
        EnsureSelectionHudReferences();
        EnsureSelectionSpritesLoaded();
        EnsureVitalsHudReferences();
        EnsureVitalsSpritesLoaded();
    }

    private void Start()
    {
        UpdateCircleDisplay();
        UpdateStatsDisplay();
        UpdateSelectionHudDisplay();
        UpdateVitalsHudDisplay();
    }

    private void Update()
    {
        EnsureSelectionSpritesLoaded();
        EnsureVitalsSpritesLoaded();

        if (!showStatsOverlay)
        {
            if (statsText != null && statsText.gameObject.activeSelf)
            {
                statsText.gameObject.SetActive(false);
            }
        }

        if (Time.unscaledTime >= nextUiRefreshTime)
        {
            nextUiRefreshTime = Time.unscaledTime + UiRefreshInterval;
            UpdateStatsDisplay();
            UpdateSelectionHudDisplay();
            UpdateVitalsHudDisplay();
        }
    }

    public void UpdateCircleDisplay()
    {
        if (circleText != null && CircleManager.instance != null)
        {
            circleText.text = CircleManager.instance.GetCircleName();
        }
    }

    private void UpdateStatsDisplay()
    {
        EnsureStatsTextReference();
        if (statsText == null)
        {
            return;
        }

        statsText.gameObject.SetActive(showStatsOverlay);
        if (!showStatsOverlay)
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
            textObj.transform.SetParent(transform, false);
            statsText = textObj.AddComponent<TextMeshProUGUI>();
        }

        RectTransform rect = statsText.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = statsAnchoredPosition;
        rect.sizeDelta = new Vector2(650f, 320f);

        statsText.color = statsColor;
        statsText.fontSize = statsFontSize;
        statsText.alignment = TextAlignmentOptions.TopLeft;
        statsText.textWrappingMode = TextWrappingModes.NoWrap;
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
        float weaponAspect = weaponSize.y > 0f ? weaponSize.x / weaponSize.y : 1f;

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

        ConfigureTopLeftRect(heartsRoot, new Vector2(cruetSize.x + gap, 0f), Vector2.zero);

        soulCruetImage.raycastTarget = false;
        soulCruetImage.preserveAspect = true;
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
            return;
        }

        if (soulCruetImage != null)
        {
            int soulState = GetSoulCruetState(stats.CollectedSoul);
            Sprite stateSprite = GetSoulCruetSprite(soulState);
            soulCruetImage.sprite = stateSprite;
            soulCruetImage.enabled = stateSprite != null;
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

        if (heartOverlayImages.Length != heartSlots)
        {
            for (int i = heartsRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(heartsRoot.GetChild(i).gameObject);
            }

            heartOverlayImages = new Image[heartSlots];

            for (int i = 0; i < heartSlots; i++)
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
                heartOverlayImages[i] = overlay;
            }
        }

        for (int i = 0; i < heartsRoot.childCount; i++)
        {
            RectTransform heartRect = heartsRoot.GetChild(i).GetComponent<RectTransform>();
            ConfigureTopLeftRect(
                heartRect,
                new Vector2(i * (currentHeartSize + currentHeartSpacing), 0f),
                new Vector2(currentHeartSize, currentHeartSize)
            );

            Image bg = heartRect.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = heartEmptySprite;
                bg.enabled = heartEmptySprite != null;
            }

            RectTransform overlayRect = heartRect.childCount > 0 ? heartRect.GetChild(0).GetComponent<RectTransform>() : null;
            if (overlayRect != null)
            {
                ConfigureTopLeftRect(overlayRect, Vector2.zero, new Vector2(currentHeartSize, currentHeartSize));
            }
        }

        float totalWidth = heartSlots > 0
            ? (heartSlots * currentHeartSize) + ((heartSlots - 1) * currentHeartSpacing)
            : 0f;

        ConfigureTopLeftRect(heartsRoot, heartsRoot.anchoredPosition, new Vector2(totalWidth, currentHeartSize));
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
}
