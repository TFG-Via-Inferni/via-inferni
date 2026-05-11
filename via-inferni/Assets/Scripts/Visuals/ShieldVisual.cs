using UnityEngine;

[DisallowMultipleComponent]
public class ShieldVisual : MonoBehaviour
{
    [Header("Shield Visual")]
    [SerializeField] private Color shieldColor = new Color(0.3f, 0.7f, 1f, 0.2f);
    [SerializeField] private float shieldRadius = 0.25f;
    [SerializeField] private int sortingOrderOffset = 7;

    private GameObject shieldVisual;
    private SpriteRenderer shieldRenderer;
    private Sprite circleSprite;

    private void Awake()
    {
        // Generar sprite al iniciar
        circleSprite = CreateCircleSprite();
        CreateShieldVisual();
    }

    private void OnDestroy()
    {
        if (shieldVisual != null)
        {
            Destroy(shieldVisual);
        }
        if (circleSprite != null && circleSprite.texture != null)
        {
            Destroy(circleSprite.texture);
        }
    }

    private void CreateShieldVisual()
    {
        // Crear un GameObject hijo para el visual del escudo
        shieldVisual = new GameObject("ShieldVisual");
        shieldVisual.transform.SetParent(transform);
        shieldVisual.transform.localPosition = Vector3.zero;
        shieldVisual.transform.localScale = Vector3.one;

        // Agregar un SpriteRenderer
        shieldRenderer = shieldVisual.AddComponent<SpriteRenderer>();
        shieldRenderer.sprite = circleSprite;
        shieldRenderer.color = shieldColor;
        shieldRenderer.sortingOrder = sortingOrderOffset;

        // Configurar la escala del visual
        float scale = shieldRadius * 2f;
        shieldVisual.transform.localScale = new Vector3(scale, scale, 1f);

        // Inicialmente desactivado
        shieldVisual.SetActive(false);
    }

    private Sprite CreateCircleSprite()
    {
        // Crear una textura más grande y con mejor calidad
        int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "ShieldCircle";
        
        Color[] pixels = new Color[size * size];
        float centerX = size / 2f;
        float centerY = size / 2f;
        float radius = size / 2f - 2f;

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // Dibujar el círculo
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float distSquared = dx * dx + dy * dy;
                float radiusSquared = radius * radius;

                if (distSquared <= radiusSquared)
                {
                    // Interior del círculo
                    pixels[y * size + x] = Color.white;
                }
                else if (distSquared <= (radius + 3f) * (radius + 3f))
                {
                    // Borde suave
                    float dist = Mathf.Sqrt(distSquared);
                    float alpha = Mathf.Clamp01((radius + 3f - dist) / 3f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f
        );
        
        return sprite;
    }

    public void ShowShield()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(true);
            // Reasignar el color para mantener consistencia en cada activación
            if (shieldRenderer != null)
            {
                shieldRenderer.color = shieldColor;
            }
        }
    }

    public void HideShield()
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }
    }
}
