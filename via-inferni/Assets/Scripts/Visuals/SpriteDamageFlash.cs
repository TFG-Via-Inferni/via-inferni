using UnityEngine;

[DisallowMultipleComponent]
public class SpriteDamageFlash : MonoBehaviour
{
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int FlipId = Shader.PropertyToID("_Flip");
    private static readonly int AlphaTexId = Shader.PropertyToID("_AlphaTex");
    private static readonly int EnableExternalAlphaId = Shader.PropertyToID("_EnableExternalAlpha");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    [Header("Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [Min(0.01f)] [SerializeField] private float flashDuration = 0.12f;
    [Min(1)] [SerializeField] private int flashCycles = 1;
    [SerializeField] private bool includeInactiveChildren = true;

    private SpriteRenderer[] spriteRenderers;
    private Material[] originalMaterials;
    private Material[] flashMaterials;
    private float flashTimer;
    private bool flashing;
    private Shader flashShader;

    private void Awake()
    {
        flashShader = Shader.Find("ViaInferni/SpriteFlash");
        CacheSpriteRenderers();
        EnsureFlashMaterials();
        ApplyFlashAmount(0f);
    }

    private void OnEnable()
    {
        CacheSpriteRenderers();
        EnsureFlashMaterials();
        ApplyFlashAmount(0f);
    }

    private void OnDisable()
    {
        flashing = false;
        flashTimer = 0f;
        RestoreOriginalMaterials();
    }

    private void OnDestroy()
    {
        RestoreOriginalMaterials();

        if (flashMaterials == null)
        {
            return;
        }

        for (int i = 0; i < flashMaterials.Length; i++)
        {
            if (flashMaterials[i] != null)
            {
                Destroy(flashMaterials[i]);
            }
        }
    }

    private void LateUpdate()
    {
        if (!flashing)
        {
            return;
        }

        flashTimer += Time.deltaTime;
        float duration = Mathf.Max(0.01f, flashDuration);
        float normalized = Mathf.Clamp01(flashTimer / duration);
        float pulse = Mathf.Abs(Mathf.Cos(normalized * flashCycles * Mathf.PI));
        ApplyFlashAmount(pulse);

        if (flashTimer < duration)
        {
            return;
        }

        flashing = false;
        flashTimer = 0f;
        ApplyFlashAmount(0f);
    }

    public void PlayFlash()
    {
        CacheSpriteRenderers();
        EnsureFlashMaterials();
        flashing = flashMaterials != null && flashMaterials.Length > 0;
        flashTimer = 0f;

        if (!flashing)
        {
            return;
        }

        ApplyFlashAmount(1f);
    }

    private void CacheSpriteRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(includeInactiveChildren);

        if (spriteRenderers == null)
        {
            spriteRenderers = new SpriteRenderer[0];
        }
    }

    private void EnsureFlashMaterials()
    {
        if (flashShader == null || spriteRenderers == null)
        {
            return;
        }

        int count = spriteRenderers.Length;
        bool needsRebuild = originalMaterials == null
            || flashMaterials == null
            || originalMaterials.Length != count
            || flashMaterials.Length != count;

        if (!needsRebuild)
        {
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                if (spriteRenderer == null)
                {
                    continue;
                }

                if (originalMaterials[i] != spriteRenderer.sharedMaterial)
                {
                    needsRebuild = true;
                    break;
                }
            }
        }

        if (!needsRebuild)
        {
            return;
        }

        RestoreOriginalMaterials();

        originalMaterials = new Material[count];
        flashMaterials = new Material[count];

        for (int i = 0; i < count; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            originalMaterials[i] = spriteRenderer.sharedMaterial;
            Material flashMaterial = new Material(flashShader);
            CopySpriteProperties(spriteRenderer, flashMaterial);
            flashMaterial.SetColor(FlashColorId, flashColor);
            flashMaterial.SetFloat(FlashAmountId, 0f);
            flashMaterials[i] = flashMaterial;
            spriteRenderer.material = flashMaterial;
        }
    }

    private void RestoreOriginalMaterials()
    {
        if (spriteRenderers == null || originalMaterials == null)
        {
            return;
        }

        int count = Mathf.Min(spriteRenderers.Length, originalMaterials.Length);
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            if (spriteRenderer == null)
            {
                continue;
            }

            spriteRenderer.sharedMaterial = originalMaterials[i];
        }
    }

    private void ApplyFlashAmount(float amount)
    {
        if (spriteRenderers == null || flashMaterials == null)
        {
            return;
        }

        int count = Mathf.Min(spriteRenderers.Length, flashMaterials.Length);
        for (int i = 0; i < count; i++)
        {
            SpriteRenderer spriteRenderer = spriteRenderers[i];
            Material flashMaterial = flashMaterials[i];
            if (spriteRenderer == null || flashMaterial == null)
            {
                continue;
            }

            CopySpriteProperties(spriteRenderer, flashMaterial);
            flashMaterial.SetColor(FlashColorId, flashColor);
            flashMaterial.SetFloat(FlashAmountId, amount);
        }
    }

    private static void CopySpriteProperties(SpriteRenderer spriteRenderer, Material material)
    {
        if (spriteRenderer == null || material == null)
        {
            return;
        }

        Sprite sprite = spriteRenderer.sprite;
        if (sprite != null)
        {
            material.SetTexture(MainTexId, sprite.texture);
        }

        material.SetColor(ColorId, spriteRenderer.color);
        material.SetVector(FlipId, spriteRenderer.flipX || spriteRenderer.flipY
            ? new Vector4(spriteRenderer.flipX ? -1f : 1f, spriteRenderer.flipY ? -1f : 1f, 0f, 0f)
            : Vector4.one);

        if (spriteRenderer.spriteSortPoint == SpriteSortPoint.Pivot)
        {
            material.enableInstancing = true;
        }

        Texture alphaTexture = sprite != null ? sprite.associatedAlphaSplitTexture : null;
        material.SetTexture(AlphaTexId, alphaTexture);
        material.SetFloat(EnableExternalAlphaId, alphaTexture != null ? 1f : 0f);
    }
}
