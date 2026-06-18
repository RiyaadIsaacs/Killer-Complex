using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Full-screen narrative intro on the <see cref="GlobalNotificationHud"/> canvas when the main game scene loads
/// (e.g. after Play from the main menu). Blocks movement until the player continues.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class GameSceneIntroPanel : MonoBehaviour
{
    public const string DefaultTitle = "Tonight";

    public const string DefaultBody =
        "Your wife is missing. Whoever took her left your apartment computer running—a messenger contact who calls himself H has her held off-site.\n\n" +
        "He is forcing you to run urgent package deliveries for his customers across this complex tonight. " +
        "Pick up each package somewhere in the building, deliver it to the unit he names, and check in on the computer before his patience runs out.\n\n" +
        "When you are ready, head to the computer. He will reach out with your first job.";

    [Header("Scenes")]
    [SerializeField] private string[] gameSceneNames = { "Main Game" };

    [SerializeField] private string[] menuSceneNames = { "Main Menu" };

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;

    [SerializeField] private TextMeshProUGUI titleText;

    [SerializeField] private TextMeshProUGUI bodyText;

    [SerializeField] private Button continueButton;

    [Header("Copy")]
    [SerializeField] private string title = DefaultTitle;

    [SerializeField, TextArea(6, 16)]
    private string body = DefaultBody;

    [SerializeField] private string continueHint = "Continue  (click, Space, or Enter)";

    [Header("Gameplay")]
    [SerializeField] private PlayerController playerController;

    [Tooltip("When true, time is frozen while the intro is visible (same feel as pause).")]
    [SerializeField] private bool freezeTimeWhileVisible = true;

    static GameSceneIntroPanel _instance;

    bool _introVisible;
    float _savedTimeScale = 1f;
    Coroutine _showIntroRoutine;

    /// <summary>True while the intro panel is shown and gameplay should stay blocked.</summary>
    public static bool BlocksGameplay => _instance != null && _instance._introVisible;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;

        EnsureUiBuilt();
        ResolvePanelReferences();
        WireContinueButton();
        HideIntroImmediate();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (_showIntroRoutine != null)
            StopCoroutine(_showIntroRoutine);

        if (_introVisible)
            RestoreTimeScale();

        if (_instance == this)
            _instance = null;
    }

    void Start()
    {
        TryScheduleShowIntroForActiveScene();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMenuScene(scene.name))
        {
            CancelScheduledShow();
            HideIntroImmediate();
            return;
        }

        if (!IsGameScene(scene.name))
            return;

        TryScheduleShowIntroForActiveScene();
    }

    void TryScheduleShowIntroForActiveScene()
    {
        if (!IsGameScene(SceneManager.GetActiveScene().name))
            return;

        CancelScheduledShow();

        GlobalNotificationHud.FindHud()?.EnsureRootActiveForSession();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!isActiveAndEnabled)
            return;

        _showIntroRoutine = StartCoroutine(ShowIntroNextFrame());
    }

    void CancelScheduledShow()
    {
        if (_showIntroRoutine == null)
            return;

        StopCoroutine(_showIntroRoutine);
        _showIntroRoutine = null;
    }

    IEnumerator ShowIntroNextFrame()
    {
        yield return null;
        _showIntroRoutine = null;
        ShowIntro();
    }

    void Update()
    {
        if (!_introVisible)
            return;

        if (WasContinuePressedThisFrame())
            DismissIntro();
    }

    static bool WasContinuePressedThisFrame()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.spaceKey.wasPressedThisFrame
               || Keyboard.current.enterKey.wasPressedThisFrame
               || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
    }

    public void ShowIntro()
    {
        if (_introVisible)
            return;

        if (!gameObject.activeInHierarchy)
            gameObject.SetActive(true);

        EnsureUiBuilt();
        ResolvePanelReferences();
        WireContinueButton();
        ApplyCopy();

        if (panelRoot == null)
        {
            Debug.LogWarning(
                $"{nameof(GameSceneIntroPanel)} on {name}: No intro panel child — run Tools → Killer-Complex → Build Game Intro Panel UI on GlobalNotificationHUD.",
                this);
            return;
        }

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        _introVisible = true;

        if (freezeTimeWhileVisible)
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (playerController != null)
            playerController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GlobalNotificationHud.EnsureActiveHierarchyStatic(gameObject);
        GlobalNotificationHud.EnsureActiveHierarchyStatic(panelRoot);
        panelRoot.SetActive(true);
    }

    public void DismissIntro()
    {
        if (!_introVisible)
            return;

        _introVisible = false;
        RestoreTimeScale();

        if (panelRoot != null && panelRoot != gameObject)
            panelRoot.SetActive(false);

        if (playerController != null)
            playerController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void HideIntroImmediate()
    {
        _introVisible = false;
        RestoreTimeScale();

        if (panelRoot != null && panelRoot != gameObject)
            panelRoot.SetActive(false);
    }

    void RestoreTimeScale()
    {
        if (!freezeTimeWhileVisible)
            return;

        Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
    }

    void ApplyCopy()
    {
        if (titleText != null)
            titleText.text = title;
        if (bodyText != null)
            bodyText.text = body;
    }

    void WireContinueButton()
    {
        if (continueButton == null)
            return;

        continueButton.onClick.RemoveListener(DismissIntro);
        continueButton.onClick.AddListener(DismissIntro);
    }

    /// <summary>
    /// Fixes Inspector mistakes where <see cref="panelRoot"/> was assigned to the HUD canvas root instead of <c>GameIntroPanel</c>.
    /// </summary>
    void ResolvePanelReferences()
    {
        if (panelRoot == gameObject)
            panelRoot = null;

        if (GameIntroPanelUiLayout.TryFindExisting(transform, out var found))
        {
            panelRoot = found.PanelRoot;
            if (titleText == null)
                titleText = found.TitleText;
            if (bodyText == null)
                bodyText = found.BodyText;
            if (continueButton == null)
                continueButton = found.ContinueButton;
        }
    }

    bool IsGameScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || gameSceneNames == null)
            return false;

        foreach (var name in gameSceneNames)
        {
            if (!string.IsNullOrEmpty(name) && sceneName == name)
                return true;
        }

        return false;
    }

    bool IsMenuScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || menuSceneNames == null)
            return false;

        foreach (var name in menuSceneNames)
        {
            if (!string.IsNullOrEmpty(name) && sceneName == name)
                return true;
        }

        return false;
    }

    void EnsureUiBuilt()
    {
        if (panelRoot != null && panelRoot != gameObject && titleText != null && bodyText != null && continueButton != null)
            return;

        ResolvePanelReferences();

        if (panelRoot != null && panelRoot != gameObject && titleText != null && bodyText != null && continueButton != null)
            return;

        if (GameIntroPanelUiLayout.TryBuild(transform, continueHint, out var built))
        {
            panelRoot = built.PanelRoot;
            titleText = built.TitleText;
            bodyText = built.BodyText;
            continueButton = built.ContinueButton;
            ApplyCopy();
        }
        else
        {
            Debug.LogWarning(
                $"{nameof(GameSceneIntroPanel)} on {name}: Intro UI is not assigned and could not be built. " +
                $"Run Tools → Killer-Complex → Build Game Intro Panel UI on GlobalNotificationHUD.",
                this);
        }
    }
}
