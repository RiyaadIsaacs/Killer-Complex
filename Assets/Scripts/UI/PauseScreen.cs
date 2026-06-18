using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [SerializeField] private GameSettingsMenu settingsMenu;

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

        EnsurePauseCanvasScale();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        EnsureSettingsMenu();
        EnsurePauseSettingsButton();
    }

    void EnsureSettingsMenu()
    {
        if (pausePanel == null)
            return;

        if (settingsMenu == null)
            settingsMenu = pausePanel.GetComponentInChildren<GameSettingsMenu>(true);

        if (settingsMenu == null)
        {
            var settingsGo = new GameObject("GameSettingsMenu", typeof(RectTransform), typeof(GameSettingsMenu));
            settingsGo.transform.SetParent(pausePanel.transform, false);
            var settingsRt = settingsGo.GetComponent<RectTransform>();
            settingsRt.anchorMin = Vector2.zero;
            settingsRt.anchorMax = Vector2.one;
            settingsRt.offsetMin = Vector2.zero;
            settingsRt.offsetMax = Vector2.zero;
            settingsMenu = settingsGo.GetComponent<GameSettingsMenu>();
        }

        var styleSource = PauseMenuUiFactory.FindMenuButtonStyleSource(pausePanel.transform);
        settingsMenu.Initialize(pausePanel, styleSource, this);
    }

    /// <summary>Hides pause dimmer and buttons while the settings overlay is open.</summary>
    public void SetPauseMenuChromeVisible(bool visible)
    {
        if (pausePanel == null)
            return;

        foreach (Transform child in pausePanel.transform)
        {
            if (child.name == "GameSettingsMenu")
                continue;

            child.gameObject.SetActive(visible);
        }
    }

    void EnsurePauseSettingsButton()
    {
        if (pausePanel == null)
            return;

        CleanupLegacySettingsButtons();

        var styleSource = PauseMenuUiFactory.FindMenuButtonStyleSource(pausePanel.transform);
        if (styleSource == null)
            return;

        RepositionMainMenuButton();

        var existing = pausePanel.transform.Find("Settings");
        if (existing != null)
        {
            PauseMenuUiFactory.RewireButton(existing.GetComponent<Button>(), OpenSettings);
            PauseMenuUiFactory.ApplyButtonLabel(existing, "Settings", styleSource);
            return;
        }

        PauseMenuUiFactory.CreateTextButton(
            pausePanel.transform,
            "Settings",
            "Settings",
            new Vector2(0f, -111f),
            new Vector2(250f, 80f),
            styleSource,
            OpenSettings);
    }

    void CleanupLegacySettingsButtons()
    {
        foreach (Transform child in pausePanel.transform)
        {
            if (child.name != "BtnSettings")
                continue;

            Destroy(child.gameObject);
        }

        var settings = pausePanel.transform.Find("Settings");
        if (settings == null)
            return;

        var image = settings.GetComponent<Image>();
        if (image == null || image.sprite == null)
            return;

        var resume = pausePanel.transform.Find("Resume")?.GetComponent<Image>();
        if (resume != null && image.sprite == resume.sprite)
            Destroy(settings.gameObject);
    }

    void RepositionMainMenuButton()
    {
        var mainMenu = pausePanel.transform.Find("Main Menu");
        if (mainMenu == null)
            return;

        var mainMenuRt = mainMenu.GetComponent<RectTransform>();
        if (mainMenuRt != null && mainMenuRt.anchoredPosition.y > -150f)
            mainMenuRt.anchoredPosition = new Vector2(mainMenuRt.anchoredPosition.x, -222f);
    }

    void EnsurePauseCanvasScale()
    {
        if (pausePanel == null)
            return;

        var rt = pausePanel.transform as RectTransform;
        if (rt != null && rt.localScale == Vector3.zero)
            rt.localScale = Vector3.one;
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
            if (settingsMenu != null && settingsMenu.IsOpen)
            {
                settingsMenu.CloseSettings();
                return;
            }

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

        settingsMenu?.CloseSettings();

        if (pausePanel != null)
        {
            SetPauseMenuChromeVisible(true);
            pausePanel.SetActive(false);
        }

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

        EnsurePauseCanvasScale();

        IsPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (player != null)
            player.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Hook to Settings button on the pause panel.</summary>
    public void OpenSettings()
    {
        if (!IsPaused || settingsMenu == null)
            return;

        settingsMenu.OpenSettings();
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
