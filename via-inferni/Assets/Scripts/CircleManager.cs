using UnityEngine;
using UnityEngine.SceneManagement;

public class CircleManager : MonoBehaviour
{
    public static CircleManager instance;

    [Header("Circle Configuration")]
    [SerializeField] private int currentCircle = 1;
    [SerializeField] private int maxCircles = 9;
    [SerializeField] private CircleDatabase circleDatabase;

    public int CurrentCircle => currentCircle;
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
        }
        else
        {
            Debug.Log("¡Has alcanzado el último círculo del Infierno!");
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

    private void RefreshCurrentCircleDefinition()
    {
        CurrentCircleDefinition = circleDatabase != null
            ? circleDatabase.GetByNumber(currentCircle)
            : null;
    }
}
