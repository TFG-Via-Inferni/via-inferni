using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Reflection;
using System.Collections;
using TMPro;
using UnityEngine.UI;

public class CircleManager : MonoBehaviour
{
    public static CircleManager instance;

    [Header("Circle Configuration")]
    [SerializeField] private int currentCircle = 1;
    [SerializeField] private int maxCircles = 9;
    [SerializeField] private CircleDatabase circleDatabase;

    [Header("Enemy Inventory Drops")]
    [Range(0f, 1f)] [SerializeField] private float globalEnemyDropChance = 1f;
    [SerializeField] private InventoryItemDefinition[] globalEnemyDropPool = new InventoryItemDefinition[0];

    [Header("Circle Transition")]
    [SerializeField] private Color transitionOverlayColor = Color.black;
    [SerializeField] private Color transitionTextColor = new Color(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private float transitionFadeInDuration = 0.4f;
    [SerializeField] private float transitionHoldDuration = 0.85f;
    [SerializeField] private float transitionFadeOutDuration = 0.65f;
    [SerializeField] private float transitionMapRefreshDelay = 0.12f;
    [Range(0.2f, 0.9f)] [SerializeField] private float transitionTextWidthPercent = 0.72f;
    [Range(0.05f, 0.25f)] [SerializeField] private float transitionTextHeightPercent = 0.14f;

    public int CurrentCircle => currentCircle;
    public bool HasCurrentCircleKey { get; private set; }
    public bool HasGameCompleted { get; private set; }
    public CircleDefinition CurrentCircleDefinition { get; private set; }
    public bool IsTransitioningCircle { get; private set; }

    private Canvas transitionCanvas;
    private Image transitionOverlayImage;
    private TextMeshProUGUI transitionText;
    private Coroutine transitionRoutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            RefreshCurrentCircleDefinition();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void DescendToNextCircle()
    {
        if (IsTransitioningCircle)
        {
            return;
        }

        if (currentCircle < maxCircles)
        {
            int nextCircle = currentCircle + 1;
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(DescendToNextCircleRoutine(nextCircle));
        }
        else
        {
            Debug.Log("Has alcanzado el ultimo circulo del Infierno!");
            HasGameCompleted = true;
            TriggerVictoryScreen();
        }
    }

    public void ResetRunState()
    {
        currentCircle = 1;
        HasCurrentCircleKey = false;
        HasGameCompleted = false;
        RefreshCurrentCircleDefinition();

        if (CircleUI.instance != null)
        {
            CircleUI.instance.UpdateCircleDisplay();
        }
    }

    public bool TryCollectCurrentCircleKey(int keyCircleNumber, out string reason)
    {
        reason = string.Empty;

        if (keyCircleNumber != currentCircle)
        {
            reason = $"La llave es del circulo {keyCircleNumber}, pero el actual es {currentCircle}.";
            return false;
        }

        if (HasCurrentCircleKey)
        {
            reason = "La llave de este circulo ya esta recogida.";
            return false;
        }

        HasCurrentCircleKey = true;
        Debug.Log($"CircleManager: llave del circulo {currentCircle} recogida.");
        RefreshStairsActivation();
        return true;
    }

    public void RefreshStairsActivation()
    {
        Stairs[] stairs = UnityEngine.Object.FindObjectsByType<Stairs>(FindObjectsSortMode.None);
        for (int i = 0; i < stairs.Length; i++)
        {
            if (stairs[i] != null)
            {
                stairs[i].RefreshActivationFromProgress();
            }
        }
    }

    public string GetCircleName()
    {
        return GetCircleName(currentCircle);
    }

    public string GetCircleName(int circleNumber)
    {
        if (circleNumber == currentCircle && CurrentCircleDefinition != null && !string.IsNullOrWhiteSpace(CurrentCircleDefinition.displayName))
        {
            return CurrentCircleDefinition.displayName;
        }

        if (circleDatabase != null)
        {
            CircleDefinition definition = circleDatabase.GetByNumber(circleNumber);
            if (definition != null && !string.IsNullOrWhiteSpace(definition.displayName))
            {
                return definition.displayName;
            }
        }

        return circleNumber switch
        {
            1 => "First Circle - Limbo",
            2 => "Second Circle - Lust",
            3 => "Third Circle - Gluttony",
            4 => "Fourth Circle - Greed",
            5 => "Fifth Circle - Wrath",
            6 => "Sixth Circle - Heresy",
            7 => "Seventh Circle - Violence",
            8 => "Eighth Circle - Fraud",
            9 => "Ninth Circle - Treachery",
            _ => "Unknown"
        };
    }

    public bool TrySpawnGlobalEnemyDrop(Vector3 position, string sourceName = "Enemy")
    {
        if (CurrentCircleDefinition != null)
        {
            float circleDropChance = Mathf.Clamp01(CurrentCircleDefinition.enemyDropChance);
            if (UnityEngine.Random.value <= circleDropChance && CurrentCircleDefinition.TryPickInventoryDrop(out InventoryItemDefinition circleItem))
            {
                WorldInventoryPickup.Spawn(circleItem, position);
                return true;
            }
        }

        if (UnityEngine.Random.value > Mathf.Clamp01(globalEnemyDropChance))
        {
            return false;
        }

        InventoryItemDefinition item = PickGlobalEnemyDropItem();
        if (item == null)
        {
            Debug.LogWarning($"CircleManager: no hay objetos validos en el pool global de drops para {sourceName}.");
            return false;
        }

        WorldInventoryPickup.Spawn(item, position);
        return true;
    }

    private InventoryItemDefinition PickGlobalEnemyDropItem()
    {
        if (globalEnemyDropPool == null || globalEnemyDropPool.Length == 0)
        {
            return null;
        }

        int attempts = globalEnemyDropPool.Length;
        while (attempts > 0)
        {
            InventoryItemDefinition candidate = globalEnemyDropPool[UnityEngine.Random.Range(0, globalEnemyDropPool.Length)];
            attempts--;

            if (candidate == null)
            {
                continue;
            }

            if (!candidate.IsValid(out _))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private void RefreshCurrentCircleDefinition()
    {
        CurrentCircleDefinition = circleDatabase != null
            ? circleDatabase.GetByNumber(currentCircle)
            : null;
    }

    private IEnumerator DescendToNextCircleRoutine(int nextCircle)
    {
        IsTransitioningCircle = true;
        EnsureTransitionOverlay();
        UpdateTransitionLayout();

        if (transitionText != null)
        {
            transitionText.text = GetCircleName(nextCircle);
        }

        yield return FadeTransition(0f, 1f, 0f, 1f, transitionFadeInDuration);
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, transitionMapRefreshDelay));

        currentCircle = nextCircle;
        HasCurrentCircleKey = false;
        RefreshCurrentCircleDefinition();
        Debug.Log($"=== DESCENDIENDO AL CIRCULO {currentCircle} ===");

        if (CircleUI.instance != null)
        {
            CircleUI.instance.UpdateCircleDisplay();
        }

        if (MapGenerator.instance != null)
        {
            MapGenerator.instance.SetupDungeon();
        }
        else
        {
            Debug.LogError("MapGenerator.instance es null!");
        }

        Player player = UnityEngine.Object.FindFirstObjectByType<Player>();
        if (player != null && player.Stats != null)
        {
            bool increased = player.Stats.IncreaseInventoryCapacity(1);
            if (increased)
            {
                Debug.Log("CircleManager: capacidad de inventario aumentada en +1.");
            }
        }

        if (CircleUI.instance != null)
        {
            CircleUI.instance.UpdateCircleDisplay();
        }

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, transitionHoldDuration));
        yield return FadeTransition(1f, 0f, 1f, 0f, transitionFadeOutDuration);

        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(false);
        }

        IsTransitioningCircle = false;
        transitionRoutine = null;
    }

    private void EnsureTransitionOverlay()
    {
        if (transitionCanvas == null)
        {
            GameObject canvasObject = new GameObject(
                "CircleTransitionCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            DontDestroyOnLoad(canvasObject);

            transitionCanvas = canvasObject.GetComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject overlayObject = new GameObject("TransitionOverlay", typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(canvasObject.transform, false);
            transitionOverlayImage = overlayObject.GetComponent<Image>();

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject textObject = new GameObject("TransitionText", typeof(RectTransform));
            textObject.transform.SetParent(overlayObject.transform, false);
            transitionText = textObject.AddComponent<TextMeshProUGUI>();
        }

        if (transitionCanvas != null)
        {
            transitionCanvas.gameObject.SetActive(true);
        }

        if (transitionOverlayImage != null)
        {
            transitionOverlayImage.raycastTarget = false;
        }

        if (transitionText != null)
        {
            transitionText.raycastTarget = false;
            transitionText.alignment = TextAlignmentOptions.Center;
            transitionText.textWrappingMode = TextWrappingModes.Normal;
            transitionText.enableWordWrapping = true;
            transitionText.enableAutoSizing = true;

            if (transitionText.font == null)
            {
                TextMeshProUGUI[] texts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i] != null && texts[i].font != null)
                    {
                        transitionText.font = texts[i].font;
                        break;
                    }
                }
            }
        }
    }

    private void UpdateTransitionLayout()
    {
        if (transitionText == null)
        {
            return;
        }

        float safeScreenWidth = Mathf.Max(1f, Screen.width);
        float safeScreenHeight = Mathf.Max(1f, Screen.height);
        float preferredFontSize = Mathf.Clamp(safeScreenHeight * 0.08f, 42f, 120f);

        RectTransform textRect = transitionText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(
            safeScreenWidth * transitionTextWidthPercent,
            safeScreenHeight * transitionTextHeightPercent);
        textRect.anchoredPosition = new Vector2(0f, safeScreenHeight * 0.03f);

        transitionText.fontSize = preferredFontSize;
        transitionText.fontSizeMin = Mathf.Clamp(preferredFontSize * 0.55f, 24f, 64f);
        transitionText.fontSizeMax = preferredFontSize;
    }

    private IEnumerator FadeTransition(float fromOverlayAlpha, float toOverlayAlpha, float fromTextAlpha, float toTextAlpha, float duration)
    {
        if (transitionOverlayImage == null || transitionText == null)
        {
            yield break;
        }

        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            ApplyTransitionVisuals(
                Mathf.Lerp(fromOverlayAlpha, toOverlayAlpha, t),
                Mathf.Lerp(fromTextAlpha, toTextAlpha, t));
            yield return null;
        }

        ApplyTransitionVisuals(toOverlayAlpha, toTextAlpha);
    }

    private void ApplyTransitionVisuals(float overlayAlpha, float textAlpha)
    {
        if (transitionOverlayImage != null)
        {
            Color overlayColor = transitionOverlayColor;
            overlayColor.a = overlayAlpha;
            transitionOverlayImage.color = overlayColor;
        }

        if (transitionText != null)
        {
            Color textColor = transitionTextColor;
            textColor.a = textAlpha;
            transitionText.color = textColor;
        }
    }

    private void TriggerVictoryScreen()
    {
        Type victoryType = null;
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        for (int i = 0; i < assemblies.Length; i++)
        {
            victoryType = assemblies[i].GetType("GameVictoryController");
            if (victoryType != null)
            {
                break;
            }
        }

        if (victoryType == null)
        {
            Debug.LogWarning("CircleManager: no se encontro GameVictoryController en el proyecto.");
            return;
        }

        MethodInfo triggerMethod = victoryType.GetMethod("TriggerVictory", BindingFlags.Public | BindingFlags.Static);
        if (triggerMethod != null)
        {
            triggerMethod.Invoke(null, null);
        }
    }
}
