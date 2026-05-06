using TMPro;
using UnityEngine;

public class CombatFeedbackText : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.5f;
    [SerializeField] private float floatSpeed = 1.4f;
    [SerializeField] private float drift = 0.2f;
    [SerializeField] private float startScale = 0.85f;
    [SerializeField] private float endScale = 1.08f;

    private TextMeshPro textMesh;
    private Vector3 velocity;
    private Color startColor;
    private float timer;

    public static void Spawn(string message, Vector3 worldPosition, Color color, float scale = 1f)
    {
        GameObject textObject = new GameObject($"CombatText_{message}");
        textObject.transform.position = worldPosition;

        CombatFeedbackText feedbackText = textObject.AddComponent<CombatFeedbackText>();
        feedbackText.Initialize(message, color, scale);
    }

    private void Initialize(string message, Color color, float scale)
    {
        textMesh = gameObject.AddComponent<TextMeshPro>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            textMesh.font = TMP_Settings.defaultFontAsset;
        }

        textMesh.text = message;
        textMesh.fontSize = 4.2f;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.color = color;
        textMesh.sortingOrder = 50;

        startColor = color;
        transform.localScale = Vector3.one * Mathf.Max(0.1f, startScale * scale);

        float horizontalDrift = Random.Range(-drift, drift);
        velocity = new Vector3(horizontalDrift, floatSpeed, 0f);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float normalized = Mathf.Clamp01(timer / Mathf.Max(0.01f, lifetime));

        transform.position += velocity * Time.deltaTime;
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, normalized);

        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        if (textMesh != null)
        {
            Color color = startColor;
            color.a = Mathf.Lerp(startColor.a, 0f, normalized);
            textMesh.color = color;
        }

        if (normalized >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
