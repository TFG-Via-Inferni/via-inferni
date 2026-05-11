using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseMenuController : MonoBehaviour
{
    private static PauseMenuController instance;

    [Header("Input")]
    [SerializeField] private Key pauseKey = Key.Escape;

    [Header("Manual Pause UI")]
    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private string startMenuSceneName = "StartMenu";
    [SerializeField] private bool hidePauseMenuOnStart = true;

    [Header("Lifetime")]
    [SerializeField] private bool keepAcrossScenes;

    private float cachedTimeScale = 1f;
    private bool isPaused;
    private CanvasGroup selfCanvasGroup;

    public static bool IsPaused => instance != null && instance.isPaused;

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

        if (pauseMenuRoot == null)
        {
            pauseMenuRoot = gameObject;
        }

        if (pauseMenuRoot == gameObject)
        {
            selfCanvasGroup = pauseMenuRoot.GetComponent<CanvasGroup>();
            if (selfCanvasGroup == null)
            {
                selfCanvasGroup = pauseMenuRoot.AddComponent<CanvasGroup>();
            }
        }

        HookButtons();
        SetPaused(false, force: true);

        if (hidePauseMenuOnStart)
        {
            SetPauseMenuVisible(false);
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

    private void Update()
    {
        // Don't allow pausing if game is over
        if (GameOverController.IsGameOver)
        {
            return;
        }

        if (Keyboard.current != null && Keyboard.current[pauseKey].wasPressedThisFrame)
        {
            TogglePause();
        }

        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        SetPaused(!isPaused);
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    public void RestartGame()
    {
        ResumeGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitGame()
    {
        ResumeGame();

        if (!SceneExistsInBuildSettings(startMenuSceneName))
        {
            Debug.LogError($"PauseMenuController: Scene '{startMenuSceneName}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(startMenuSceneName);
    }

    private void SetPaused(bool paused, bool force = false)
    {
        if (!force && isPaused == paused)
        {
            return;
        }

        isPaused = paused;
        SetPauseMenuVisible(paused);

        if (paused)
        {
            cachedTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            SetGameplayHudVisible(false);
            SelectResumeButton();
            return;
        }

        Time.timeScale = cachedTimeScale;
        AudioListener.pause = false;
        SetGameplayHudVisible(true);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void SetPauseMenuVisible(bool visible)
    {
        if (pauseMenuRoot == null)
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

        pauseMenuRoot.SetActive(visible);
    }

    private void SetGameplayHudVisible(bool visible)
    {
        if (CircleUI.instance != null)
        {
            CircleUI.instance.gameObject.SetActive(visible);
        }
    }

    private void HookButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
            resumeButton.onClick.AddListener(ResumeGame);
        }

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
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(ExitGame);
        }
    }

    private void SelectResumeButton()
    {
        if (resumeButton == null || EventSystem.current == null)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    private bool SceneExistsInBuildSettings(string sceneName)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;

        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName)
            {
                return true;
            }
        }

        return false;
    }
}
