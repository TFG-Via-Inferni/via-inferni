using UnityEngine;
using UnityEngine.SceneManagement;

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

    public int CurrentCircle => currentCircle;
    public bool HasCurrentCircleKey { get; private set; }
    public CircleDefinition CurrentCircleDefinition { get; private set; }

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
        if (currentCircle < maxCircles)
        {
            currentCircle++;
            HasCurrentCircleKey = false;
            RefreshCurrentCircleDefinition();
            Debug.Log($"=== DESCENDIENDO AL CÍRCULO {currentCircle} ===");
            
            // Actualizar UI primero
            if (CircleUI.instance != null)
            {
                CircleUI.instance.UpdateCircleDisplay();
            }
            
            // Regenerar el mapa
            if (MapGenerator.instance != null)
            {
                MapGenerator.instance.SetupDungeon();
            }
            else
            {
                Debug.LogError("MapGenerator.instance es null!");
            }
            
            // Aumentar la capacidad de inventario del jugador en +1 al avanzar de círculo
            Player player = UnityEngine.Object.FindFirstObjectByType<Player>();
            if (player != null && player.Stats != null)
            {
                bool increased = player.Stats.IncreaseInventoryCapacity(1);
                if (increased)
                {
                    Debug.Log("CircleManager: capacidad de inventario aumentada en +1.");
                }
            }
        }
        else
        {
            Debug.Log("¡Has alcanzado el último círculo del Infierno!");
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
        if (CurrentCircleDefinition != null && !string.IsNullOrWhiteSpace(CurrentCircleDefinition.displayName))
        {
            return CurrentCircleDefinition.displayName;
        }

        return currentCircle switch
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
            if (Random.value <= circleDropChance && CurrentCircleDefinition.TryPickInventoryDrop(out InventoryItemDefinition circleItem))
            {
                WorldInventoryPickup.Spawn(circleItem, position);
                return true;
            }
        }

        if (Random.value > Mathf.Clamp01(globalEnemyDropChance))
        {
            return false;
        }

        InventoryItemDefinition item = PickGlobalEnemyDropItem();
        if (item == null)
        {
            Debug.LogWarning($"CircleManager: no hay objetos válidos en el pool global de drops para {sourceName}.");
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
            InventoryItemDefinition candidate = globalEnemyDropPool[Random.Range(0, globalEnemyDropPool.Length)];
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
}
