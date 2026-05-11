using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    private static GameOverController instance;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverMenuRoot;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private bool hideGameOverMenuOnStart = true;

    [Header("Lifetime")]
    [SerializeField] private bool keepAcrossScenes;

    private bool isGameOver;
    private CanvasGroup selfCanvasGroup;
    private Player playerReference;

    public static bool IsGameOver => instance != null && instance.isGameOver;

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

        if (gameOverMenuRoot == null)
        {
            gameOverMenuRoot = gameObject;
        }

        if (gameOverMenuRoot == gameObject)
        {
            selfCanvasGroup = gameOverMenuRoot.GetComponent<CanvasGroup>();
            if (selfCanvasGroup == null)
            {
                selfCanvasGroup = gameOverMenuRoot.AddComponent<CanvasGroup>();
            }
        }

        HookButtons();
        SetGameOverVisible(false);

        if (hideGameOverMenuOnStart)
        {
            SetGameOverVisible(false);
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

    private void Start()
    {
        CachePlayerReference();
    }

    private void Update()
    {
        if (playerReference == null)
        {
            CachePlayerReference();
        }

        if (!isGameOver && playerReference != null && playerReference.IsDead())
        {
            ShowGameOver();
        }
    }

    public void RestartGame()
    {
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
            Debug.LogError($"GameOverController: Scene '{startMenuSceneName}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(startMenuSceneName);
    }

    private void ShowGameOver()
    {
        isGameOver = true;
        SetGameOverVisible(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SelectRestartButton();
    }

    private void SetGameOverVisible(bool visible)
    {
        if (gameOverMenuRoot == null)
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

        gameOverMenuRoot.SetActive(visible);
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
            restartButton.onClick.AddListener(RestartGame);
        }

        if (exitButton != null)
        {
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

    private void CachePlayerReference()
    {
        if (playerReference != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerReference = playerObject.GetComponent<Player>();
        }
    }
}
