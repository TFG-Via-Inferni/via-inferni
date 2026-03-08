using UnityEngine;
using TMPro;

public class CircleUI : MonoBehaviour
{
    public static CircleUI instance;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI circleText;

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
    }

    private void Start()
    {
        UpdateCircleDisplay();
    }

    public void UpdateCircleDisplay()
    {
        if (circleText != null && CircleManager.instance != null)
        {
            circleText.text = CircleManager.instance.GetCircleName();
        }
    }
}
