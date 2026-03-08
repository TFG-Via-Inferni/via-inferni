using UnityEngine;
using UnityEngine.SceneManagement;

public class CircleManager : MonoBehaviour
{
    public static CircleManager instance;

    [Header("Circle Configuration")]
    [SerializeField] private int currentCircle = 1;
    [SerializeField] private int maxCircles = 9;

    public int CurrentCircle => currentCircle;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
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
        return currentCircle switch
        {
            1 => "Primer Círculo - Limbo",
            2 => "Segundo Círculo - Lujuria",
            3 => "Tercer Círculo - Gula",
            4 => "Cuarto Círculo - Avaricia",
            5 => "Quinto Círculo - Ira",
            6 => "Sexto Círculo - Herejía",
            7 => "Séptimo Círculo - Violencia",
            8 => "Octavo Círculo - Fraude",
            9 => "Noveno Círculo - Traición",
            _ => "Desconocido"
        };
    }
}
