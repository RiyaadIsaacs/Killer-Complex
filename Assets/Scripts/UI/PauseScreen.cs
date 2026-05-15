using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Attach to a persistent object in the gameplay scene (e.g. empty "GameUI" or the pause Canvas root).
/// Assign a child panel that holds the pause UI; wire buttons to <see cref="Resume"/>, <see cref="RestartGame"/>,
/// <see cref="GoToMainMenu"/>, and <see cref="QuitGame"/>.
/// Press Escape to pause / resume (skipped while a computer terminal or maze overlay handles Escape).
/// </summary>
public class PauseScreen : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Panel (dimmer + buttons) shown while paused. Keep this object on an always-active parent so this script still receives Escape.")]
    [SerializeField] private GameObject pausePanel;

    [Header("Gameplay")]
    [SerializeField] private PlayerController player;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    public bool IsPaused { get; private set; }

    /// <summary>True when any <see cref="PauseScreen"/> in the scene has the menu open.</summary>
    public static bool IsGameplayPaused
    {
        get
        {
            var screens = FindObjectsByType<PauseScreen>(FindObjectsSortMode.None);
            foreach (var screen in screens)
            {
                if (screen != null && screen.IsPaused)
                    return true;
            }

            return false;
        }
    }

    void Awake()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (IsPaused)
            Time.timeScale = 1f;
    }

    void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (IsPaused)
        {
            Resume();
            return;
        }

        if (GameSceneIntroPanel.BlocksGameplay)
            return;

        if (IsAnyComputerOpen())
            return;

        if (HackingMazeMinigame.TryConsumeEscape())
            return;

        Pause();
    }

    /// <summary>Hook to Resume button, or call from code.</summary>
    public void Resume()
    {
        if (!IsPaused)
            return;

        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (player != null)
            player.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>Hook to Restart button.</summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Hook to Main Menu button.</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>Hook to Quit Game button. Stops Play mode in the Editor; closes the build on a standalone player.</summary>
    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>Can also be wired to a Pause button if you add one.</summary>
    public void Pause()
    {
        if (IsPaused)
            return;

        IsPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (player != null)
            player.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    static bool IsAnyComputerOpen()
    {
        var terminals = FindObjectsByType<ComputerTerminal>(FindObjectsSortMode.None);
        foreach (var terminal in terminals)
        {
            if (terminal != null && terminal.IsOpen)
                return true;
        }

        return false;
    }
}
