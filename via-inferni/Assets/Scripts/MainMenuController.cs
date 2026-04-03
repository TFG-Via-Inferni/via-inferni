using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Loading")]
    [SerializeField] private string gameplaySceneName = "Map Generator";

    [Header("Canvas Buttons")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitGameButton;
    [SerializeField] private Button devModeButton;

    private void Awake()
    {
        HookButtons();
    }

    private void OnDestroy()
    {
        UnhookButtons();
    }

    public void StartGame()
    {
        if (!SceneExistsInBuildSettings(gameplaySceneName))
        {
            Debug.LogError($"MainMenuController: Scene '{gameplaySceneName}' is not in Build Settings.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OpenOptions()
    {
    }

    public void CloseOptions()
    {
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ToggleDevMode()
    {
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

    private void HookButtons()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
            startGameButton.onClick.AddListener(StartGame);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveListener(OpenOptions);
            optionsButton.onClick.AddListener(OpenOptions);
        }

        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveListener(ExitGame);
            exitGameButton.onClick.AddListener(ExitGame);
        }

        if (devModeButton != null)
        {
            devModeButton.onClick.RemoveListener(ToggleDevMode);
            devModeButton.onClick.AddListener(ToggleDevMode);
        }
    }

    private void UnhookButtons()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveListener(StartGame);
        }

        if (optionsButton != null)
        {
            optionsButton.onClick.RemoveListener(OpenOptions);
        }

        if (exitGameButton != null)
        {
            exitGameButton.onClick.RemoveListener(ExitGame);
        }

        if (devModeButton != null)
        {
            devModeButton.onClick.RemoveListener(ToggleDevMode);
        }
    }
}