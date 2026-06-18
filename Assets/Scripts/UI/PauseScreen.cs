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

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (settingsMenu == null)
            settingsMenu = GetComponentInChildren<GameSettingsMenu>(true);
        if (settingsMenu == null)
        {
            var settingsGo = new GameObject("GameSettingsMenu", typeof(RectTransform), typeof(GameSettingsMenu));
            settingsGo.transform.SetParent(transform, false);
            settingsMenu = settingsGo.GetComponent<GameSettingsMenu>();
        }

        EnsurePauseSettingsButton();
    }

    void EnsurePauseSettingsButton()
    {
        if (pausePanel == null)
            return;

        if (pausePanel.transform.Find("BtnSettings") != null)
            return;

        var sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        var btnGo = new GameObject("BtnSettings", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        btnGo.transform.SetParent(pausePanel.transform, false);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.35f);
        rt.sizeDelta = new Vector2(220f, 44f);

        var img = btnGo.GetComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.color = new Color32(70, 95, 120, 255);

        var btn = btnGo.GetComponent<UnityEngine.UI.Button>();
        btn.onClick.AddListener(OpenSettings);

        var font = TMPro.TMP_Settings.defaultFontAsset;
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
        textGo.transform.SetParent(btnGo.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp = textGo.GetComponent<TMPro.TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = "Settings";
        tmp.fontSize = 22;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
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

        if (pausePanel != null)
            pausePanel.SetActive(false);

        settingsMenu?.CloseSettings();

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
