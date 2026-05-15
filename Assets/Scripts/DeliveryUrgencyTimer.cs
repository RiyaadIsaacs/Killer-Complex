using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Per-leg countdown for urgent package deliveries. Each new leg gets
/// <see cref="initialLegTimeSeconds"/> minus <see cref="secondsReducedPerCompletedLeg"/> times legs already completed.
/// Shows the bad-ending (death) canvas when time runs out.
/// </summary>
public class DeliveryUrgencyTimer : MonoBehaviour
{
    [Header("Timer")]
    [Tooltip("Countdown budget for the first delivery leg (seconds).")]
    [SerializeField, Min(1f)]
    private float initialLegTimeSeconds = 90f;

    [Tooltip("Subtract this many seconds from the budget for each delivery leg already completed when the next leg starts.")]
    [SerializeField, Min(0f)]
    private float secondsReducedPerCompletedLeg = 5f;

    [Tooltip("Optional floor for the per-leg budget after reductions. Leave at 0 so the timer can keep shrinking (e.g. 50 → 40 → 30 … → 5 → 1). A value of 10 stops the budget at 10s even when the formula would go lower.")]
    [SerializeField, Min(0f)]
    private float minimumLegTimeSeconds = 0f;

    [Header("References")]
    [Tooltip("If unset, the first DeliveryManager in loaded scenes is used.")]
    [SerializeField] private DeliveryManager deliveryManager;

    [Tooltip("If unset, BadEndingOrchestrator.Instance or the first instance in the scene is used.")]
    [SerializeField] private BadEndingOrchestrator badEndingOrchestrator;

    [Tooltip("Optional. Disabled when the death canvas is shown.")]
    [SerializeField] private PlayerController playerController;

    [Header("HUD")]
    [Tooltip("If unset, uses GlobalNotificationHud.UrgentDeliveryTimerLabel.")]
    [SerializeField] private GlobalNotificationHud notificationHud;

    [SerializeField]
    private string countdownFormat = "Urgent: {0:0}s";

    [Header("Scenes")]
    [Tooltip("Timer UI is hidden and countdown stopped when any of these scenes load (e.g. main menu).")]
    [SerializeField] private string[] menuSceneNames = { "Main Menu" };

    float _remainingSeconds;
    float _currentLegBudgetSeconds;
    bool _countdownActive;
    bool _timedOut;
    bool _awaitingHMessageToStartTimer;
    int _pendingTimerCompletedLegCount;

    DeliveryManager _resolvedDeliveryManager;
    GlobalNotificationHud _resolvedNotificationHud;

    /// <summary>Inspector value for the first leg budget.</summary>
    public float InitialLegTimeSeconds => initialLegTimeSeconds;

    /// <summary>Seconds left on the active leg; 0 when idle or after timeout.</summary>
    public float RemainingSeconds => _countdownActive ? Mathf.Max(0f, _remainingSeconds) : 0f;

    public bool IsCountdownActive => _countdownActive && !_timedOut;

    public bool TimedOut => _timedOut;

    void Awake()
    {
        ResolveDeliveryManager();
        ResolveNotificationHud()?.HideUrgentDeliveryTimer();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        var dm = ResolveDeliveryManager();
        if (dm != null)
        {
            dm.OnDeliveryLegPrepared += HandleDeliveryLegPrepared;
            dm.OnDeliveryCompleted += HandleDeliveryCompleted;
        }

        if (IsMenuScene(SceneManager.GetActiveScene().name))
            EnterMenuScene();
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        var dm = ResolveDeliveryManager();
        if (dm != null)
        {
            dm.OnDeliveryLegPrepared -= HandleDeliveryLegPrepared;
            dm.OnDeliveryCompleted -= HandleDeliveryCompleted;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMenuScene(scene.name))
            EnterMenuScene();
        else
            _timedOut = false;
    }

    void EnterMenuScene()
    {
        CancelPendingTimerStart();
        _timedOut = false;
        StopCountdown();
    }

    void CancelPendingTimerStart() => _awaitingHMessageToStartTimer = false;

    bool IsMenuScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || menuSceneNames == null)
            return false;

        foreach (var menu in menuSceneNames)
        {
            if (!string.IsNullOrEmpty(menu) && sceneName == menu)
                return true;
        }

        return false;
    }

    void HandleDeliveryCompleted(int _)
    {
        CancelPendingTimerStart();
        StopCountdown();
    }

    void Update()
    {
        if (!_countdownActive || _timedOut)
            return;

        _remainingSeconds -= Time.deltaTime;
        RefreshCountdownLabel();

        if (_remainingSeconds > 0f)
            return;

        _remainingSeconds = 0f;
        _countdownActive = false;
        TriggerTimeoutDeath();
    }

    void HandleDeliveryLegPrepared()
    {
        var dm = ResolveDeliveryManager();
        if (dm == null || _timedOut)
            return;

        if (dm.PostDeliveryStepAwayBeatPending)
        {
            CancelPendingTimerStart();
            StopCountdown();
            return;
        }

        if (IsMenuScene(SceneManager.GetActiveScene().name))
        {
            StopCountdown();
            return;
        }

        if (dm.FinishedAllConfiguredDeliveryLegs || dm.ActiveDropPointId < 0)
        {
            CancelPendingTimerStart();
            StopCountdown();
            return;
        }

        _pendingTimerCompletedLegCount = dm.currentDeliveryID;
        _awaitingHMessageToStartTimer = true;
        StopCountdown();
    }

    /// <summary>
    /// Starts the leg countdown after <b>H</b> posts to the messenger (call from <see cref="OllamaConnector.NotifyHPostedToMessenger"/>).
    /// </summary>
    public void TryStartCountdownAfterHMessage()
    {
        if (!_awaitingHMessageToStartTimer || _timedOut)
            return;

        if (IsMenuScene(SceneManager.GetActiveScene().name))
        {
            CancelPendingTimerStart();
            return;
        }

        var dm = ResolveDeliveryManager();
        if (dm == null || dm.ActiveDropPointId < 0 || dm.PostDeliveryStepAwayBeatPending)
        {
            CancelPendingTimerStart();
            StopCountdown();
            return;
        }

        CancelPendingTimerStart();
        BeginLegCountdown(_pendingTimerCompletedLegCount);
    }

    /// <summary>Called when H's post-drop "step away" line posts — keeps the lull free of an active countdown.</summary>
    public void NotifyHSteppedAwayFromComputer()
    {
        CancelPendingTimerStart();
        StopCountdown();
    }

    /// <summary>
    /// Budget = initial − (completed legs × reduction), optionally clamped to <see cref="minimumLegTimeSeconds"/> when that value is &gt; 0.
    /// </summary>
    public float GetBudgetSecondsForCompletedLegCount(int completedLegCount)
    {
        float budget = initialLegTimeSeconds - completedLegCount * secondsReducedPerCompletedLeg;
        if (minimumLegTimeSeconds > 0f)
            budget = Mathf.Max(minimumLegTimeSeconds, budget);
        return Mathf.Max(0f, budget);
    }

    public void BeginLegCountdown(int completedLegCount)
    {
        if (_timedOut)
            return;

        var dm = ResolveDeliveryManager();
        if (dm != null)
            completedLegCount = dm.currentDeliveryID;

        _currentLegBudgetSeconds = GetBudgetSecondsForCompletedLegCount(completedLegCount);
        _remainingSeconds = _currentLegBudgetSeconds;

        if (_remainingSeconds <= 0f)
        {
            TriggerTimeoutDeath();
            return;
        }

        _countdownActive = true;
        RefreshCountdownLabel();
    }

    /// <summary>Stops an active countdown and hides the HUD row. Does not clear a pending post-<b>H</b> start.</summary>
    public void StopCountdown()
    {
        _countdownActive = false;
        _remainingSeconds = 0f;
        ResolveNotificationHud()?.HideUrgentDeliveryTimer();
    }

    /// <summary>Whole seconds left for LLM context; -1 when no active countdown.</summary>
    public int GetRemainingSecondsForLlmContext()
    {
        if (!_countdownActive || _timedOut)
            return -1;
        return Mathf.CeilToInt(Mathf.Max(0f, _remainingSeconds));
    }

    void TriggerTimeoutDeath()
    {
        if (_timedOut)
            return;

        _timedOut = true;
        StopCountdown();

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        if (playerController != null)
            playerController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var orchestrator = ResolveBadEndingOrchestrator();
        if (orchestrator != null)
            orchestrator.RevealBadEndingCanvas();
        else
            Debug.LogError(
                $"{nameof(DeliveryUrgencyTimer)} on {name}: No {nameof(BadEndingOrchestrator)} — assign one with the death/bad-ending canvas.",
                this);
    }

    void RefreshCountdownLabel()
    {
        var hud = ResolveNotificationHud();
        if (hud == null)
        {
            if (_countdownActive)
                Debug.LogWarning(
                    $"{nameof(DeliveryUrgencyTimer)} on {name}: No {nameof(GlobalNotificationHud)} with {nameof(GlobalNotificationHud.UrgentDeliveryTimerLabel)} — timer runs but is not visible.",
                    this);
            return;
        }

        if (!_countdownActive)
        {
            hud.HideUrgentDeliveryTimer();
            return;
        }

        int display = Mathf.CeilToInt(Mathf.Max(0f, _remainingSeconds));
        hud.ShowUrgentDeliveryTimer(string.Format(countdownFormat, display));

        if (hud.UrgentDeliveryTimerLabel == null)
            Debug.LogWarning(
                $"{nameof(DeliveryUrgencyTimer)}: HUD found but UrgentDeliveryTimerLabel is not assigned. Run Tools → Killer-Complex → Patch ComputerDesktopCanvas prefab (Messenger Suspicion Bar) or add UrgentDeliveryTimerLabel to GlobalNotificationHUD.",
                this);
    }

    GlobalNotificationHud ResolveNotificationHud()
    {
        if (notificationHud != null)
            return notificationHud;

        if (_resolvedNotificationHud != null)
            return _resolvedNotificationHud;

        _resolvedNotificationHud = GlobalNotificationHud.FindHud();

        if (_resolvedNotificationHud != null && _resolvedNotificationHud.UrgentDeliveryTimerLabel == null)
            _resolvedNotificationHud.TryAutoAssignUrgentTimerLabel();

        return _resolvedNotificationHud;
    }

    DeliveryManager ResolveDeliveryManager()
    {
        if (deliveryManager != null)
            return deliveryManager;
        if (_resolvedDeliveryManager == null)
            _resolvedDeliveryManager = FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        return _resolvedDeliveryManager;
    }

    BadEndingOrchestrator ResolveBadEndingOrchestrator()
    {
        if (badEndingOrchestrator != null)
            return badEndingOrchestrator;
        if (BadEndingOrchestrator.Instance != null)
            return BadEndingOrchestrator.Instance;
        return FindFirstObjectByType<BadEndingOrchestrator>(FindObjectsInactive.Include);
    }
}
