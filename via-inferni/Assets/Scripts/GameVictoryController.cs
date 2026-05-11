using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameVictoryController : MonoBehaviour
{
    private static GameVictoryController instance;

    [Header("Victory UI")]
    [SerializeField] private GameObject victoryMenuRoot;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private bool hideVictoryMenuOnStart = true;

    [Header("Lifetime")]
    [SerializeField] private bool keepAcrossScenes;

    private bool isVictory;
    private CanvasGroup selfCanvasGroup;

    public static bool IsVictory => instance != null && instance.isVictory;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (keepAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        if (victoryMenuRoot == null)
        {
            victoryMenuRoot = gameObject;
        }

        if (victoryMenuRoot == gameObject)
        {
            selfCanvasGroup = victoryMenuRoot.GetComponent<CanvasGroup>();
            if (selfCanvasGroup == null)
            {
                selfCanvasGroup = victoryMenuRoot.AddComponent<CanvasGroup>();
            }
        }

        HookButtons();
        SetVictoryVisible(false);

        if (hideVictoryMenuOnStart)
        {
            SetVictoryVisible(false);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        UnhookButtons();
    }

    public static void TriggerVictory()
    {
        if (instance == null)
        {
            instance = Object.FindFirstObjectByType<GameVictoryController>();
        }

        if (instance != null)
        {
            instance.ShowVictory();
        }
    }

    public void RestartGame()
    {
        if (CircleManager.instance != null)
        {
            CircleManager.instance.ResetRunState();
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (!SceneExistsInBuildSettings(startMenuSceneName))
        {
            Debug.LogError($"GameVictoryController: Scene '{startMenuSceneName}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(startMenuSceneName);
    }

    private void ShowVictory()
    {
        if (isVictory)
        {
            return;
        }

        isVictory = true;
        SetVictoryVisible(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SetGameplayHudVisible(false);

        SelectRestartButton();
    }

    private void SetVictoryVisible(bool visible)
    {
        if (victoryMenuRoot == null)
        {
            return;
        }

        if (selfCanvasGroup != null)
        {
            selfCanvasGroup.alpha = visible ? 1f : 0f;
            selfCanvasGroup.interactable = visible;
            selfCanvasGroup.blocksRaycasts = visible;
            return;
        }

        victoryMenuRoot.SetActive(visible);
    }

    private void SelectRestartButton()
    {
        if (EventSystem.current != null && restartButton != null)
        {
            EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
        }
    }

    private void HookButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
            exitButton.onClick.AddListener(ExitGame);
        }
    }

    private void UnhookButtons()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
        }
    }

    private bool SceneExistsInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (scenePath.Contains(sceneName))
            {
                return true;
            }
        }

        return false;
    }

    private void SetGameplayHudVisible(bool visible)
    {
        if (CircleUI.instance != null)
        {
            CircleUI.instance.SetStatsPauseVisibility(!visible);
            CircleUI.instance.gameObject.SetActive(visible);
        }
    }
}