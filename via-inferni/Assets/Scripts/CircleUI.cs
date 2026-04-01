using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.UI;

public class CircleUI : MonoBehaviour
{
    private const float UiRefreshInterval = 0.1f;
    private const float PlayerLookupInterval = 0.5f;

    public static CircleUI instance;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI circleText;

    [Header("Stats Overlay")]
    [SerializeField] private bool showStatsOverlay = true;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Color statsColor = new Color(0.72f, 0.72f, 0.72f, 0.62f);
    [SerializeField] private int statsFontSize = 20;
    [SerializeField] private Vector2 statsAnchoredPosition = new Vector2(18f, -18f);

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

    private readonly StringBuilder statsBuilder = new StringBuilder(256);
    private Player trackedPlayer;
    private float nextPlayerLookupTime;
    private float nextUiRefreshTime;
    private bool selectionSpritesLoaded;

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
    }

    private void Start()
    {
        UpdateCircleDisplay();
        UpdateStatsDisplay();
        UpdateSelectionHudDisplay();
    }

    private void Update()
    {
        EnsureSelectionSpritesLoaded();

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

        statsBuilder.Append("Luck: x")
            .Append(stats.LuckMultiplier.ToString("0.00"))
            .AppendLine();

        statsBuilder.Append("Crit: ")
            .Append((stats.CritChance * 100f).ToString("0"))
            .Append("%   Dodge: ")
            .Append((stats.DodgeChance * 100f).ToString("0"))
            .AppendLine("%");

        statsBuilder.Append("Coins: ")
            .Append(stats.Coins)
            .Append("   Bag: ")
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
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
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

        float baseHeight = Mathf.Max(24f, safeScreenHeight * iconHeightPercentOfScreen);
        float characterAspect = characterSize.y > 0f ? characterSize.x / characterSize.y : 1f;
        float weaponAspect = weaponSize.y > 0f ? weaponSize.x / weaponSize.y : 1f;

        Vector2 scaledCharacterSize = new Vector2(baseHeight * characterAspect, baseHeight);
        Vector2 scaledWeaponSize = new Vector2(baseHeight * weaponAspect, baseHeight);

        float margin = Mathf.Max(8f, shorterSide * marginPercentOfScreen);
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
