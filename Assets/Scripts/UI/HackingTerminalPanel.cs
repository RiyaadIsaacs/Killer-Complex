using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// In-world hacking UI: <b>Hack</b> opens a procedural maze breach sim. Each maze exit adds
/// <see cref="mazeWinProgressPercent"/> to the decryption bar; at 100% <see cref="OnHackSuccessful"/> runs.
/// </summary>
public class HackingTerminalPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider decryptionSlider;
    [SerializeField] private TMP_Text decryptionStatusLabel;
    [SerializeField] private Button hackButton;
    [SerializeField] private TMP_Text consoleOutput;

    [Header("Behaviour")]
    [SerializeField, Range(1f, 40f)] private float hackProgressPercentPerClick = 8f;
    [Tooltip("Decryption slider increase for each maze completed without hitting a bomb.")]
    [SerializeField, Range(1f, 50f)] private float mazeWinProgressPercent = 10f;
    [Tooltip("If unset, the first OllamaConnector in loaded scenes is used at runtime (so maze rounds can still message H).")]
    [SerializeField] private OllamaConnector ollamaConnector;

    [Header("Maze → H / next job")]
    [Tooltip("Finished maze runs (win, fail, or abort) required before Ollama is called for breach banter, suspicion nudge, and the next idle delivery is prepared. Full decryption to 100% on a win bypasses this count. Minimum 2 so the first breach run never contacts the LLM alone.")]
    [SerializeField, Min(2)]
    private int mazeBreachesBeforeMessengerJob = 2;

    [Header("Events")]
    [SerializeField] private UnityEvent onHackSuccessful;

    private bool _hackComplete;

    /// <summary>True after decryption reaches 100% (good-ending path takes priority over bad ending).</summary>
    public bool IsHackComplete => _hackComplete;

    private HackingMazeMinigame _mazeMinigame;
    OllamaConnector _runtimeResolvedOllamaConnector;
    int _completedMazeBreachesSinceLastMessenger;

    OllamaConnector GetOllamaConnector()
    {
        if (ollamaConnector != null)
            return ollamaConnector;
        if (_runtimeResolvedOllamaConnector == null)
            _runtimeResolvedOllamaConnector = FindFirstObjectByType<OllamaConnector>(FindObjectsInactive.Include);
        return _runtimeResolvedOllamaConnector;
    }

    DeliveryManager ResolveDeliveryManager()
    {
        var oc = GetOllamaConnector();
        if (oc != null && oc.DeliveryManagerForGameplay != null)
            return oc.DeliveryManagerForGameplay;
        return FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
    }

    private void Awake()
    {
        _mazeMinigame = GetComponent<HackingMazeMinigame>();
        if (_mazeMinigame == null)
            _mazeMinigame = gameObject.AddComponent<HackingMazeMinigame>();
        _mazeMinigame.Initialize(this);

        if (decryptionSlider != null)
        {
            decryptionSlider.minValue = 0f;
            decryptionSlider.maxValue = 100f;
            decryptionSlider.wholeNumbers = true;
            decryptionSlider.value = 0f;
        }

        if (hackButton != null)
            hackButton.onClick.AddListener(OnHackClicked);

        RefreshStatusLabel();
        AppendConsoleLine("> Session opened.");
        AppendConsoleLine("> Awaiting breach sim (Hack).");
    }

    private void OnDestroy()
    {
        if (hackButton != null)
            hackButton.onClick.RemoveListener(OnHackClicked);
    }

    void OnValidate()
    {
        mazeBreachesBeforeMessengerJob = Mathf.Max(2, mazeBreachesBeforeMessengerJob);
    }

    private void OnHackClicked()
    {
        if (_hackComplete)
            return;

        if (_mazeMinigame != null)
        {
            if (_mazeMinigame.IsOpen)
                return;
            _mazeMinigame.OpenAndRegenerate();
            return;
        }

        if (decryptionSlider == null)
            return;

        float next = Mathf.Min(100f, decryptionSlider.value + hackProgressPercentPerClick);
        decryptionSlider.value = next;
        AppendConsoleLine($"> Inject… {next:F0}% decrypted.");

        RefreshStatusLabel();

        if (next >= 100f)
            OnHackSuccessful();
    }

    private void RefreshStatusLabel()
    {
        if (decryptionStatusLabel == null || decryptionSlider == null)
            return;

        decryptionStatusLabel.text = $"Decryption Status: {decryptionSlider.value:F0}%";
    }

    /// <summary>Current decryption percent (0–100). Used by the maze to scale difficulty per round.</summary>
    public float GetHackProgressPercent()
    {
        return decryptionSlider != null ? decryptionSlider.value : 0f;
    }

    /// <summary>Called when the player clears a maze round. Adds progress; at 100% triggers full hack completion.</summary>
    public void ApplyMazeRoundWin()
    {
        if (_hackComplete || decryptionSlider == null)
            return;

        var next = Mathf.Min(100f, decryptionSlider.value + mazeWinProgressPercent);
        decryptionSlider.value = next;
        RefreshStatusLabel();
        AppendConsoleLine($"> Segment decrypted +{mazeWinProgressPercent:F0}% (now {next:F0}%).");

        if (next >= 100f)
            OnHackSuccessful();
    }

    /// <summary>Maze difficulty step based on completed progress (0 before first win, 1 after first +10%, …).</summary>
    public int GetMazeTier()
    {
        if (decryptionSlider == null)
            return 0;
        var step = Mathf.Max(1f, mazeWinProgressPercent);
        return Mathf.Clamp(Mathf.FloorToInt(decryptionSlider.value / step), 0, 99);
    }

    public float GetMazeWinStep() => mazeWinProgressPercent;

    public void AppendConsoleLine(string line)
    {
        if (consoleOutput == null || string.IsNullOrEmpty(line))
            return;

        if (string.IsNullOrEmpty(consoleOutput.text))
            consoleOutput.text = line;
        else
            consoleOutput.text += "\n" + line;

        consoleOutput.ForceMeshUpdate();
    }

    /// <summary>Called when decryption reaches 100%. Invokes <see cref="onHackSuccessful"/> and sends the reversal beat to Ollama.</summary>
    public void OnHackSuccessful()
    {
        if (_hackComplete)
            return;

        _hackComplete = true;

        if (decryptionSlider != null)
            decryptionSlider.value = 100f;

        if (hackButton != null)
            hackButton.interactable = false;

        RefreshStatusLabel();
        AppendConsoleLine("> DECRYPTION COMPLETE — uplink sealed.");

        onHackSuccessful?.Invoke();

        var oc = GetOllamaConnector();
        if (oc != null)
            oc.SendHackReversalPrompt();
        else
            Debug.LogWarning(
                $"{nameof(HackingTerminalPanel)}: Assign OllamaConnector (or add one to the scene) to push the reversal prompt.",
                this);
    }

    /// <summary>
    /// Invoked when a maze <b>run</b> ends (win, bomb, or abort). Requires <see cref="mazeBreachesBeforeMessengerJob"/> finished runs
    /// before calling Ollama / preparing the next idle delivery, except when this win immediately completes decryption (100% — reversal only).
    /// </summary>
    public void OnMazeRoundAttemptFinished(bool roundReachedGoal)
    {
        if (_hackComplete)
            return;

        var ocEarly = GetOllamaConnector();
        if (ocEarly != null)
        {
            ocEarly.ApplySuspicionIncrementForMazeLoss(!roundReachedGoal);
            if (ocEarly.TryDispatchSuspicionMaxBadEndingOllama())
                return;
        }

        bool skipMazeOllamaBecauseReversalNext = false;
        if (roundReachedGoal && decryptionSlider != null)
        {
            float next = Mathf.Min(100f, decryptionSlider.value + mazeWinProgressPercent);
            skipMazeOllamaBecauseReversalNext = next >= 100f;
        }

        if (skipMazeOllamaBecauseReversalNext)
        {
            _completedMazeBreachesSinceLastMessenger = 0;
            RunMazeRoundOllamaHooks(roundReachedGoal, true);
            return;
        }

        int need = Mathf.Max(2, mazeBreachesBeforeMessengerJob);
        _completedMazeBreachesSinceLastMessenger++;
        if (_completedMazeBreachesSinceLastMessenger < need)
        {
            int remaining = need - _completedMazeBreachesSinceLastMessenger;
            AppendConsoleLine(
                remaining == 1
                    ? "> Uplink: one more breach sim run required before H is messaged."
                    : $"> Uplink: {remaining} more breach sim runs required before H is messaged.");
            return;
        }

        _completedMazeBreachesSinceLastMessenger = 0;
        RunMazeRoundOllamaHooks(roundReachedGoal, false);
    }

    /// <summary>
    /// Applies suspicion increment when applicable, then a single maze-outcome Ollama call (ignore-delivery beat merged into that prompt when the increment ran).
    /// </summary>
    void RunMazeRoundOllamaHooks(bool roundReachedGoal, bool skipMazeOllamaBecauseReversalNext)
    {
        var oc = GetOllamaConnector();
        if (oc != null)
        {
            bool mergeIgnoreBeat = oc.ApplySuspicionIncrementForIgnoredMazeAttempt();
            if (oc.TryDispatchSuspicionMaxBadEndingOllama())
                return;
            InvokeMazeNotify(roundReachedGoal, skipMazeOllamaBecauseReversalNext, mergeIgnoreBeat);
            return;
        }

        InvokeMazeNotify(roundReachedGoal, skipMazeOllamaBecauseReversalNext, false);
    }

    void InvokeMazeNotify(bool roundReachedGoal, bool skipMazeOllamaBecauseReversalNext, bool mergeIgnoreDeliveryOrderIntoMazeReply)
    {
        var oc = GetOllamaConnector();
        if (oc != null)
            oc.NotifyMazeBreachRoundAttemptFinished(roundReachedGoal, skipMazeOllamaBecauseReversalNext, mergeIgnoreDeliveryOrderIntoMazeReply);
        else
        {
            Debug.LogWarning(
                $"{nameof(HackingTerminalPanel)}: No OllamaConnector in scene — maze rounds cannot message H. Add OllamaConnector or assign the reference on this panel.",
                this);
            var dm = ResolveDeliveryManager();
            bool deferPrepareNextLeg = dm != null && dm.PostDeliveryStepAwayBeatPending;
            if (!deferPrepareNextLeg)
                UnityEngine.Object.FindFirstObjectByType<ChatManager>()?.TryPrepareNextDeliveryIfIdle();
        }
    }
}
