using UnityEngine;
using TMPro;
using System.Text;

public class CircleUI : MonoBehaviour
{
    public static CircleUI instance;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI circleText;

    [Header("Stats Overlay")]
    [SerializeField] private bool showStatsOverlay = true;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Color statsColor = new Color(0.72f, 0.72f, 0.72f, 0.62f);
    [SerializeField] private int statsFontSize = 20;
    [SerializeField] private Vector2 statsAnchoredPosition = new Vector2(18f, -18f);

    private readonly StringBuilder statsBuilder = new StringBuilder(256);
    private Player trackedPlayer;
    private float nextPlayerLookupTime;
    private float nextStatsRefreshTime;

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
    }

    private void Start()
    {
        UpdateCircleDisplay();
        UpdateStatsDisplay();
    }

    private void Update()
    {
        if (!showStatsOverlay)
        {
            if (statsText != null && statsText.gameObject.activeSelf)
            {
                statsText.gameObject.SetActive(false);
            }
            return;
        }

        if (Time.unscaledTime >= nextStatsRefreshTime)
        {
            nextStatsRefreshTime = Time.unscaledTime + 0.1f;
            UpdateStatsDisplay();
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

        if (trackedPlayer == null && Time.unscaledTime >= nextPlayerLookupTime)
        {
            trackedPlayer = FindFirstObjectByType<Player>();
            nextPlayerLookupTime = Time.unscaledTime + 0.5f;
        }

        if (trackedPlayer == null)
        {
            statsText.text = "Stats: esperando jugador...";
            return;
        }

        PlayerStats stats = trackedPlayer.Stats;

        statsBuilder.Clear();
        statsBuilder.AppendLine("STATS ACTUALES");
        statsBuilder.Append("Vida: ")
            .Append(trackedPlayer.CurrentHealth.ToString("0"))
            .Append(" / ")
            .Append(trackedPlayer.MaxHealth.ToString("0"))
            .AppendLine();

        statsBuilder.Append("Forma: ")
            .Append(trackedPlayer.CurrentForm)
            .AppendLine();

        if (stats == null)
        {
            statsBuilder.AppendLine("Stats: no disponible");
            statsText.text = statsBuilder.ToString();
            return;
        }

        statsBuilder.Append("Arma: ")
            .Append(stats.GetSelectedWeaponId(trackedPlayer.CurrentForm))
            .AppendLine();

        statsBuilder.Append("Daño: ")
            .Append(stats.GetFinalDamage(trackedPlayer.CurrentForm).ToString("0.0"))
            .Append(" (x")
            .Append(stats.DamageMultiplier.ToString("0.00"))
            .AppendLine(")");

        statsBuilder.Append("Velocidad: x")
            .Append(stats.MoveSpeedMultiplier.ToString("0.00"))
            .AppendLine();

        statsBuilder.Append("Suerte: x")
            .Append(stats.LuckMultiplier.ToString("0.00"))
            .AppendLine();

        statsBuilder.Append("Crit: ")
            .Append((stats.CritChance * 100f).ToString("0"))
            .Append("%   Esquiva: ")
            .Append((stats.DodgeChance * 100f).ToString("0"))
            .AppendLine("%");

        statsBuilder.Append("Monedas: ")
            .Append(stats.Coins)
            .Append("   Mochila: ")
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
        statsText.enableWordWrapping = false;
        statsText.raycastTarget = false;

        if (statsText.font == null && circleText != null)
        {
            statsText.font = circleText.font;
        }
    }
}
