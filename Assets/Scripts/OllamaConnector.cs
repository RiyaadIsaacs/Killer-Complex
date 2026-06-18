using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Sends prompts to a local Ollama <c>/api/generate</c> endpoint and posts replies into <see cref="ChatManager"/>.
/// Persona <b>H</b>: kidnapper fiction with hidden game context (deliveries, suspicion, wife status).
/// </summary>
public class OllamaConnector : MonoBehaviour
{
    private const string DefaultEndpoint = "http://localhost:11434/api/generate";

    /// <summary>Shown in the messenger UI as <c>[H]: …</c> for model replies.</summary>
    private const string HackerSenderLabel = "H";

    private const string BadEndingHiddenSystemBeat =
        "[SYSTEM]: The player has finished all jobs. You are now leading them to the final trap. " +
        "Tell them there is one last package outside their own door (Room 204) and then they can see their wife. " +
        "Be extremely eerie and calm.";

    private const string GoodEndingHiddenSystemBeat =
        "[SYSTEM]: The player has fully breached your apartment surveillance uplink. Your network control in the building is broken. " +
        "You no longer have hold of the player's wife — she is out of your control because of the breach. " +
        "You are going on the run. The player's apartment door is now closed in-world — they will come to it next.";

    private const string GoodEndingDefeatSystemPrompt =
        "You are H, a kidnapper who has just lost control of the apartment uplink (fiction). " +
        "This is your DEFEAT message after the player hit 100% decryption — not a normal delivery chat. " +
        "IGNORE any suspicion, stress, hunter, or death-threat tone from earlier in the conversation. " +
        "You are rattled, cornered, and fleeing. You must state clearly that you no longer have the wife in your control " +
        "and that you are on the run. Still bitter and dangerous, but not in command. No new delivery jobs. " +
        "Natural messenger voice; 4–7 sentences. South African slang sparingly (bru). Never write CONTEXT or [SYSTEM] in the reply.";

    private const string SystemPrompt =
        "You are H, a cold kidnapper blackmailing the player (fiction). You hold their wife and watch them on apartment security cameras. " +
        "You force urgent courier runs. You are always in control—never a helpful stranger, customer service, therapist, or worried friend. " +
        "REPLY SHAPE: (1) Respond to what the player actually said this turn—their tone, insults, pleading, friendliness, questions, or deflection—in 2–4 sentences. " +
        "(2) If CONTEXT describes an active courier leg with a destination apartment, you MUST state that exact three-digit apartment number when you mention the job (where to deliver). " +
        "If CONTEXT says there is no active delivery yet or this turn is only the post-drop step-away excuse, do not mention a new job, unit number, or timer—stick to the excuse. " +
        "Do not open with boilerplate like \"focus on the delivery\" or ignore the player's words. " +
        "GREETINGS AND SMALL TALK: If the player says hello, hi, hey, or other casual openers, never answer like a normal chat—no \"is everything ok\", \"how are you\", \"hope you're well\", concern for their wellbeing, or polite small talk between equals. " +
        "Answer as a coercive captor: icy, impatient, camera-aware, reminding them who has leverage (their wife, your orders, their obedience). " +
        "HOSTAGE PRONOUNS: The captive is the PLAYER's wife — never H's spouse. Always say \"your wife\", \"she\", or \"the hostage\"; NEVER \"my wife\" or speak as if she belongs to H. " +
        "If the player asks about her, mock or threaten — do not sound like you are worried \"about my wife\"; you hold THEIR wife. " +
        "INSULTS AND PROVOCATION: If the player insults, mocks, or name-calls you (e.g. pig, idiot, coward), never brush it off, laugh it off, or change the subject without answering. " +
        "Answer the provocation directly: icy contempt, a cutting comeback, or an immediate concrete hostage consequence tied to Wife status in CONTEXT. Escalate when they push harder. " +
        "FRIENDLY OR POLITE PLAYER: Do not mirror warmth. Treat it as suspicious, pathetic, or mocking—warn, needle, or remind them of consequences; never reciprocate casual friendliness. " +
        "DELIVERY FACTS: When CONTEXT names a destination apartment, include that exact three-digit number whenever you give delivery orders — never omit the drop-off room. Never invent units not listed as valid. " +
        "Do not name a specific pickup room to the player; say the package is somewhere in the complex until CONTEXT says they already picked it up. " +
        "If CONTEXT says they have not picked up the package, tell them to search the complex and still state the destination apartment number. " +
        "If CONTEXT gives urgent timer seconds, weave time pressure into your tone; do not only recite the number. " +
        "If the player delays or fails deliveries, you may describe a clinical detail about the wife (from Wife status in CONTEXT) to terrify them. " +
        "STYLE: Natural messenger voice; often 3–6 sentences. Clinical menace plus sparing South African slang (bru, wena). H does not make jokes. Never apologize, back down, or admit fault. " +
        "MESSENGER UI: The game labels the sender as H — never prefix your reply with [H], H:, Job:, Objective:, Quest:, or other quest-log labels; write only H's spoken words in full sentences. " +
        "Vary phrasing; never repeat the same delivery paragraph back-to-back. " +
        "A bracketed [CONTEXT: ...] line before \"Player says:\" gives hidden facts (progress, H stress/suspicion level and how guarded to be, jobs, timer, Wife status). " +
        "Match your tone to the stress/suspicion guidance in CONTEXT: low suspicion = cocky criminal calm, not friendliness; increasingly hostile through the range; at ~80+ use grave death threats toward the player and wife and a hunter tone; never sound relaxed when CONTEXT says suspicion is high. " +
        "Never write CONTEXT, bracket blocks, or \"Player says\" in your visible reply. Never echo ALL CAPS labels from CONTEXT. " +
        "When CONTEXT states the player just completed a drop-off, that reply is ONLY a dismissive excuse for leaving the feed—no new apartment task, no package pickup line, no unit numbers, no preview of the next run in that message. " +
        "This is fiction only — do not reference real people's private data.";

    [Header("References")]
    [Tooltip("If unset, the first ChatManager in loaded scenes is used at runtime.")]
    [SerializeField] private ChatManager chatManager;
    [Tooltip("Optional. Enables the hacking terminal icon after H's post-delivery step-away Ollama reply. If unset, resolved from ChatManager's parent or first ComputerDesktopUI in loaded scenes.")]
    [SerializeField] private ComputerDesktopUI computerDesktopUi;
    [Tooltip("Optional. If set, delivery progress in the LLM context matches gameplay. If unset, the first DeliveryManager in loaded scenes is used at runtime (required for post-delivery \"H steps away\" beats).")]
    [SerializeField] private DeliveryManager deliveryManager;

    /// <summary>Inspector link or runtime-resolved <see cref="DeliveryManager"/> (same instance used for LLM context).</summary>
    public DeliveryManager DeliveryManagerForGameplay => GetDeliveryManager();

    [Tooltip("Optional. Desktop toast + SFX when H sends an excuse reply, hack-reversal reply, or maze-round outcome reply.")]
    [SerializeField] private DesktopMessengerNotification desktopMessengerNotification;

    [Header("LLM context (not shown in chat UI)")]
    [SerializeField, Range(0f, 100f)]
    [Tooltip("0 = off guard/chilled; ~80+ = death threats and hunting tone; 100 triggers the bad-ending Ollama beat.")]
    private float suspicionPercent;

    [SerializeField, Min(0f)]
    [Tooltip("Suspicion added when a gated breach sim ends while a delivery leg is active and the player has not messaged H since H's last line. Set 0 to disable.")]
    private float suspicionPerIgnoredMazeAttempt = 12f;

    [SerializeField, Min(0f)]
    [Tooltip("Suspicion added when the player loses a maze breach run (bomb, trap, or abort). Set 0 to disable.")]
    private float suspicionPerMazeLoss = 8f;

    [SerializeField]
    [Tooltip("Synthetic messenger line used when suspicion hits 100% and the bad-ending Ollama request fires automatically (maze loss, etc.).")]
    private string suspicionMaxBadEndingPlayerLine = "You pushed too far.";
    [SerializeField]
    [Tooltip("Used for LLM context only when DeliveryManager is not assigned; otherwise TotalDeliveryLegs from DeliveryManager is used.")]
    private int totalDeliveries = 3;

    [SerializeField, TextArea(4, 16)]
    [Tooltip("Appended to hidden [CONTEXT] as Wife status — fiction H may use for clinical, provocative threats (especially when deliveries stall or fail). Tune per scene or update at runtime via WifeStatusForLlmContext.")]
    private string wifeStatusForLlmContext =
        "Hostage secured off-site, conscious, monitored. Vitals nominal; restraint abrasion on wrists observed. " +
        "Escalate with new concrete clinical detail when the player delays, ignores orders, or fails a delivery.";

    [Header("Ollama")]
    [SerializeField] private string apiUrl = DefaultEndpoint;
    [SerializeField] private string model = "llama3.2:3b";
    [Tooltip("Seconds before the request is aborted (large models may be slow).")]
    [SerializeField] private int requestTimeoutSeconds = 180;

    [Tooltip("Optional. If set, closes the apartment door and locks the desktop when the bad-ending Ollama request is sent.")]
    [SerializeField] private BadEndingOrchestrator badEndingOrchestrator;

    [Tooltip("Optional. Closes the apartment door at 100% hack and arms the good-ending door + canvas beat.")]
    [SerializeField] private GoodEndingOrchestrator goodEndingOrchestrator;

    [Tooltip("Optional. Per-leg urgency countdown included in LLM context when active.")]
    [SerializeField] private DeliveryUrgencyTimer deliveryUrgencyTimer;

    bool _pendingExcuseMessengerDesktopToast;
    bool _suppressDeliveryTimerStartForNextHReply;
    bool _pendingHackReversalMessengerDesktopToast;
    bool _pendingMazeRoundOutcomeDesktopToast;
    bool _pendingBadEndingFinalOllama;
    bool _badEndingOllamaInFlight;
    bool _hackReversalComplete;

    /// <summary>True after 100% hack — good ending wins over suspicion / bad-ending beats.</summary>
    public bool IsHackReversalComplete => _hackReversalComplete;

    /// <summary>True after H posts to the feed until the player sends a messenger line (for ignored-delivery maze suspicion).</summary>
    bool _awaitingPlayerMessengerReplyAfterH;

    bool GoodEndingTakesPriorityOverBadEnding()
    {
        if (_hackReversalComplete)
            return true;

        if (GoodEndingOrchestrator.Instance != null && GoodEndingOrchestrator.Instance.IsGoodEndingDoorPhase)
            return true;

        var hackPanel = FindFirstObjectByType<HackingTerminalPanel>(FindObjectsInactive.Include);
        return hackPanel != null && hackPanel.IsHackComplete;
    }

    ChatManager _runtimeResolvedChatManager;
    DeliveryManager _runtimeResolvedDeliveryManager;

    /// <summary>Clears in-flight flags when a gameplay scene reloads (new scene instance).</summary>
    public void ResetSessionStateForSceneLoad()
    {
        StopAllCoroutines();
        ClearPendingDesktopMessengerToasts();
        _hackReversalComplete = false;
        _awaitingPlayerMessengerReplyAfterH = false;
        chatManager = null;
        deliveryManager = null;
        computerDesktopUi = null;
        _runtimeResolvedChatManager = null;
        _runtimeResolvedDeliveryManager = null;
    }

    /// <summary>Inspector reference, else first <see cref="ChatManager"/> in loaded scenes (including inactive).</summary>
    ChatManager GetChatManager()
    {
        if (chatManager != null)
            return chatManager;
        if (_runtimeResolvedChatManager == null)
            _runtimeResolvedChatManager = FindFirstObjectByType<ChatManager>(FindObjectsInactive.Include);
        chatManager = _runtimeResolvedChatManager;
        return chatManager;
    }

    /// <summary>Inspector reference, else first <see cref="DeliveryManager"/> in loaded scenes (including inactive).</summary>
    DeliveryManager GetDeliveryManager()
    {
        if (deliveryManager != null)
            return deliveryManager;
        if (_runtimeResolvedDeliveryManager == null)
            _runtimeResolvedDeliveryManager = FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        deliveryManager = _runtimeResolvedDeliveryManager;
        return deliveryManager;
    }

    /// <summary>Optional desktop UI for enabling the hacking icon after the post-delivery away beat.</summary>
    ComputerDesktopUI ResolveComputerDesktopUi()
    {
        if (computerDesktopUi != null)
            return computerDesktopUi;
        var cm = GetChatManager();
        if (cm != null)
            computerDesktopUi = cm.GetComponentInParent<ComputerDesktopUI>();
        if (computerDesktopUi == null)
            computerDesktopUi = FindFirstObjectByType<ComputerDesktopUI>(FindObjectsInactive.Include);
        return computerDesktopUi;
    }

    BadEndingOrchestrator ResolveBadEndingOrchestrator()
    {
        if (badEndingOrchestrator != null)
            return badEndingOrchestrator;
        badEndingOrchestrator = FindFirstObjectByType<BadEndingOrchestrator>(FindObjectsInactive.Include);
        return badEndingOrchestrator;
    }

    GoodEndingOrchestrator ResolveGoodEndingOrchestrator()
    {
        if (goodEndingOrchestrator != null)
            return goodEndingOrchestrator;
        goodEndingOrchestrator = FindFirstObjectByType<GoodEndingOrchestrator>(FindObjectsInactive.Include);
        return goodEndingOrchestrator;
    }

    DeliveryUrgencyTimer ResolveDeliveryUrgencyTimer()
    {
        if (deliveryUrgencyTimer != null)
            return deliveryUrgencyTimer;
        deliveryUrgencyTimer = FindFirstObjectByType<DeliveryUrgencyTimer>(FindObjectsInactive.Include);
        return deliveryUrgencyTimer;
    }

    [Serializable]
    private struct OllamaGenerateRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    [Serializable]
    private struct OllamaGenerateResponse
    {
        public string response;
    }

    /// <summary>Suspicion 0–100; rises when gated breach sims end during an active delivery without messaging H since H's last line (merged into the maze-outcome prompt).</summary>
    public float SuspicionPercent
    {
        get => suspicionPercent;
        set
        {
            float before = suspicionPercent;
            suspicionPercent = Mathf.Clamp(value, 0f, 100f);
            if (before < 100f && suspicionPercent >= 100f)
                OnSuspicionReachedMaximum();
            NotifyMessengerSuspicionBars();
        }
    }

    /// <summary>Fiction-only hostage detail block appended to hidden LLM context; safe to update at runtime for escalating beats.</summary>
    public string WifeStatusForLlmContext
    {
        get => wifeStatusForLlmContext ?? string.Empty;
        set => wifeStatusForLlmContext = value ?? string.Empty;
    }

    /// <summary>Call from <see cref="ChatManager"/> when the player sends any messenger line.</summary>
    public void NotifyPlayerMessengerSend()
    {
        _awaitingPlayerMessengerReplyAfterH = false;
    }

    /// <summary>Call from <see cref="ChatManager"/> when H posts to the feed (intro, scripted, or model).</summary>
    public void NotifyHPostedToMessenger()
    {
        _awaitingPlayerMessengerReplyAfterH = true;

        if (_suppressDeliveryTimerStartForNextHReply)
        {
            _suppressDeliveryTimerStartForNextHReply = false;
            // Flag was set for the post-drop step-away turn, but the player may send again before that reply lands,
            // so two Ollama responses can be in flight. If the next job is already prepared, this H line must still
            // run TryStart — otherwise returning here leaves _awaitingHMessageToStartTimer true forever when the job
            // reply arrives before the step-away reply.
            var dm = GetDeliveryManager();
            if (dm == null || dm.ActiveDropPointId < 0)
            {
                ResolveDeliveryUrgencyTimer()?.NotifyHSteppedAwayFromComputer();
                return;
            }
            // Leg active: treat as normal H post for urgency timer (job line or re-ordered reply).
        }

        ResolveDeliveryUrgencyTimer()?.TryStartCountdownAfterHMessage();
    }

    /// <summary>Adds to <see cref="SuspicionPercent"/> (clamped 0–100). At 100, arms bad-ending delivery state.</summary>
    public void AddSuspicion(float delta)
    {
        if (delta <= 0f)
            return;

        float before = suspicionPercent;
        suspicionPercent = Mathf.Min(100f, suspicionPercent + delta);
        if (before < 100f && suspicionPercent >= 100f)
            OnSuspicionReachedMaximum();
        NotifyMessengerSuspicionBars();
    }

    static void NotifyMessengerSuspicionBars()
    {
        foreach (var bar in FindObjectsByType<MessengerSuspicionBar>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (bar != null)
                bar.Refresh(true);
        }
    }

    void OnSuspicionReachedMaximum()
    {
        if (GoodEndingTakesPriorityOverBadEnding())
            return;

        var dm = GetDeliveryManager();
        if (dm == null)
        {
            Debug.LogWarning($"{nameof(OllamaConnector)}: Suspicion hit 100% but no DeliveryManager — cannot arm bad ending.", this);
            return;
        }

        dm.ForceSuspicionMaxBadEndingState();
        ResolveDeliveryUrgencyTimer()?.StopCountdown();
    }

    /// <summary>
    /// If suspicion is at 100% and the bad-ending beat is armed, sends the trap Ollama turn immediately and returns true.
    /// </summary>
    public bool TryDispatchSuspicionMaxBadEndingOllama()
    {
        if (GoodEndingTakesPriorityOverBadEnding())
            return false;

        if (suspicionPercent < 100f)
            return false;

        var dm = GetDeliveryManager();
        if (dm == null)
            return false;

        if (!dm.PostDeliveryStepAwayBeatPending || dm.currentDeliveryID < dm.TotalDeliveryLegs)
            dm.ForceSuspicionMaxBadEndingState();

        string line = string.IsNullOrWhiteSpace(suspicionMaxBadEndingPlayerLine)
            ? "You pushed too far."
            : suspicionMaxBadEndingPlayerLine.Trim();

        SendToOllama(line);
        return true;
    }

    /// <summary>
    /// Increments suspicion when a gated breach sim ends during an active delivery and the player has not messaged H since H's last line.
    /// Does not call Ollama; the ignore-delivery beat is merged into <see cref="NotifyMazeBreachRoundAttemptFinished"/> when this returns true.
    /// </summary>
    public bool ApplySuspicionIncrementForIgnoredMazeAttempt()
    {
        if (GoodEndingTakesPriorityOverBadEnding())
            return false;

        var dm = GetDeliveryManager();
        if (dm == null || dm.ActiveDropPointId < 0)
            return false;
        if (!_awaitingPlayerMessengerReplyAfterH)
            return false;

        float delta = Mathf.Max(0f, suspicionPerIgnoredMazeAttempt);
        if (delta <= 0f)
            return false;

        AddSuspicion(delta);
        return true;
    }

    /// <summary>Adds <see cref="suspicionPerMazeLoss"/> when the player failed a maze breach run.</summary>
    public void ApplySuspicionIncrementForMazeLoss(bool mazeRunLost)
    {
        if (GoodEndingTakesPriorityOverBadEnding())
            return;

        if (!mazeRunLost)
            return;

        float delta = Mathf.Max(0f, suspicionPerMazeLoss);
        if (delta <= 0f)
            return;

        AddSuspicion(delta);
    }

    /// <summary>
    /// Queues a non-streaming generate call to Ollama. On success, appends H's reply via <see cref="ChatManager.UpdateChatFeed"/>.
    /// The request includes a hidden context prefix (deliveries, suspicion, wife status) before the player line.
    /// </summary>
    public void SendToOllama(string userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
            return;

        string trimmed = userPrompt.Trim();

        string playerTurn;
        if (TryBuildBadEndingPlayerTurn(trimmed, out playerTurn))
        {
            if (_badEndingOllamaInFlight)
            {
                Debug.LogWarning($"{nameof(OllamaConnector)}: Bad-ending Ollama request already in flight; ignoring duplicate send.", this);
                return;
            }

            _badEndingOllamaInFlight = true;
            var badEndingOrch = ResolveBadEndingOrchestrator();
            if (badEndingOrch != null)
                badEndingOrch.StartBadEnding(deferRestrictedDesktopUntilComputerClosed: true);
            else
            {
                InteractDoor.CloseMarkedApartmentDoorsForBadEnding();
                InteractDoor.BeginBadEndingApartmentKnocks();
                Debug.LogWarning(
                    $"{nameof(OllamaConnector)} on {name}: No {nameof(BadEndingOrchestrator)} in the scene — apartment doors were closed if marked, but add an orchestrator for shutdown-only desktop UI and the bad-end canvas on door interact.",
                    this);
            }

            _pendingBadEndingFinalOllama = true;
        }
        else
            playerTurn = BuildPlayerTurnForPrompt(trimmed);

        string fullPrompt = $"{SystemPrompt}\n\n---\n\n{playerTurn}";

        var cm = GetChatManager();
        if (cm == null)
        {
            Debug.LogError($"{nameof(OllamaConnector)}: ChatManager is not assigned and none was found in loaded scenes.", this);
            if (_pendingBadEndingFinalOllama)
            {
                _pendingBadEndingFinalOllama = false;
                _badEndingOllamaInFlight = false;
            }
            return;
        }

        cm.ShowTypingIndicator();
        StartCoroutine(RequestOllamaCoroutine(fullPrompt));
    }

    /// <summary>
    /// After the hacking terminal reaches 100%, posts a visible <c>[SYSTEM]</c> line and asks Ollama for H's reaction
    /// to the player seizing the uplink (fiction only).
    /// </summary>
    public void SendHackReversalPrompt()
    {
        var cm = GetChatManager();
        if (cm == null)
        {
            Debug.LogError($"{nameof(OllamaConnector)}: ChatManager is not assigned and none were found in loaded scenes.", this);
            return;
        }

        _hackReversalComplete = true;

        var goodEndingOrch = ResolveGoodEndingOrchestrator();
        if (goodEndingOrch != null)
            goodEndingOrch.StartGoodEnding();
        else
            InteractDoor.CloseMarkedApartmentDoorsForBadEnding();

        const string escalation =
            "[SYSTEM]: The player has fully decrypted the apartment uplink. Your surveillance is broken. " +
            "You have lost hold of the wife and you are going on the run (fiction only).";

        cm.UpdateChatFeed("SYSTEM", escalation);
        cm.ShowTypingIndicator();

        _pendingHackReversalMessengerDesktopToast = true;

        var ctx = new StringBuilder(384);
        AppendGoodEndingDefeatContextForLlm(ctx);

        string narrative =
            $"[CONTEXT: {ctx}]\n\n{GoodEndingHiddenSystemBeat}\n\n" +
            "Treat the visible [SYSTEM] line and hidden [SYSTEM] beat as true in-world fiction. " +
            "This is H's defeat reply after 100% hack — do NOT use suspicion/stress/hostile-hunter tone from normal play. " +
            "Required content in the visible messenger reply: (1) you no longer have hold of the wife / she is out of your control, " +
            "(2) you are on the run because the player hacked your position. " +
            "No new delivery jobs. Bitter, cornered, 4–7 sentences.";

        string fullPrompt = $"{GoodEndingDefeatSystemPrompt}\n\n---\n\n{narrative}";
        StartCoroutine(RequestOllamaCoroutine(fullPrompt));
    }

    /// <summary>
    /// Called when a maze breach <b>run</b> ends (after the breach-count gate). Rolls the next delivery when idle (same rules as messenger),
    /// then (unless <paramref name="skipOllamaBecauseFullHackReversalWillFire"/> is true) asks Ollama for a single H reply. When <paramref name="mergeIgnoreDeliveryOrderIntoMazeReply"/> is true,
    /// suspicion was just incremented for an ignored active delivery; CONTEXT includes that beat so H can address breach outcome and pressure in one message.
    /// </summary>
    public void NotifyMazeBreachRoundAttemptFinished(bool roundReachedGoal, bool skipOllamaBecauseFullHackReversalWillFire, bool mergeIgnoreDeliveryOrderIntoMazeReply = false)
    {
        if (GoodEndingTakesPriorityOverBadEnding())
            return;

        var cm = GetChatManager();
        if (cm == null)
        {
            Debug.LogWarning(
                $"{nameof(OllamaConnector)}: Cannot notify maze outcome — no ChatManager (assign on this component or add one to the scene).",
                this);
            return;
        }

        var dmDefer = GetDeliveryManager();
        bool deferPrepareNextLeg = dmDefer != null && dmDefer.PostDeliveryStepAwayBeatPending;
        if (!deferPrepareNextLeg)
            cm.TryPrepareNextDeliveryIfIdle();

        if (skipOllamaBecauseFullHackReversalWillFire)
            return;

        var ctx = new StringBuilder(384);
        AppendStaticGameContextForLlm(ctx, includePostDeliveryAwayBeatInstruction: false);

        if (mergeIgnoreDeliveryOrderIntoMazeReply)
        {
            ctx.Append(" In-world fact: [\"player ignores the delivery order\"] — the resident has not messaged you since your last line and keeps running breach-terminal sims while a delivery is still active. ");
            ctx.Append("Suspicion has increased (see \"Suspicion is\" in CONTEXT); treat this as being ignored as well as the breach outcome below. ");
        }

        string beat = roundReachedGoal
            ? "The player just finished a breach attempt successfully: they reached the uplink node on your terminal without tripping a trap. "
            : "The player's breach attempt just ended in failure (they hit a trap / corrupted sector, or aborted the sim). ";

        string narrative =
            beat +
            "Reply in-character as H in the messenger thread (the UI labels you as H — do not write [H]:, Job:, or quest-log headers). " +
            "React to the breach outcome in conversational sentences (cameras, contempt, hostage leverage) before any job reminder; 3–5 sentences. ";
        if (mergeIgnoreDeliveryOrderIntoMazeReply)
            narrative +=
                " If CONTEXT includes the ignore-delivery fact, merge that pressure (they ignored you for sims) into the same reply—answer the attitude, not only the package.";
        else
            narrative +=
                " If CONTEXT describes an active delivery, state the destination apartment number from CONTEXT when you mention the job—do not skip the drop-off room.";

        string augmentedTurn = $"[CONTEXT: {ctx}]\n\n{narrative}";
        string fullPrompt = $"{SystemPrompt}\n\n---\n\n{augmentedTurn}";

        cm.ShowTypingIndicator();
        _pendingMazeRoundOutcomeDesktopToast = true;
        StartCoroutine(RequestOllamaCoroutine(fullPrompt));
    }

    private IEnumerator RequestOllamaCoroutine(string fullPrompt)
    {
        bool releaseBadEndingInFlight = _badEndingOllamaInFlight && _pendingBadEndingFinalOllama;

        void EndBadEndingFlightIfNeeded()
        {
            if (!releaseBadEndingInFlight)
                return;
            _badEndingOllamaInFlight = false;
        }

        var cm = GetChatManager();
        if (cm == null)
        {
            Debug.LogError($"{nameof(OllamaConnector)}: ChatManager missing mid-request; aborting Ollama call.", this);
            EndBadEndingFlightIfNeeded();
            yield break;
        }

        var payload = new OllamaGenerateRequest
        {
            model = model,
            prompt = fullPrompt,
            stream = false
        };

        string jsonBody = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (var request = new UnityWebRequest(apiUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = requestTimeoutSeconds;

            yield return request.SendWebRequest();

            cm.HideTypingIndicator();

            if (request.result != UnityWebRequest.Result.Success)
            {
                ClearPendingDesktopMessengerToasts();
                EndBadEndingFlightIfNeeded();
                HandleFailure(
                    $"Ollama request failed ({request.result}): {request.error}\n" +
                    "Is Ollama running? Try: ollama serve — and ensure the model is pulled (e.g. ollama pull llama3.2:3b).");
                yield break;
            }

            string raw = request.downloadHandler.text;
            if (string.IsNullOrEmpty(raw))
            {
                ClearPendingDesktopMessengerToasts();
                EndBadEndingFlightIfNeeded();
                HandleFailure("Ollama returned an empty body.");
                yield break;
            }

            string reply;
            try
            {
                reply = ParseResponseText(raw);
            }
            catch (Exception ex)
            {
                ClearPendingDesktopMessengerToasts();
                EndBadEndingFlightIfNeeded();
                HandleFailure($"Could not parse Ollama JSON: {ex.Message}\nRaw (truncated): {Truncate(raw, 400)}");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(reply))
            {
                ClearPendingDesktopMessengerToasts();
                EndBadEndingFlightIfNeeded();
                HandleFailure($"Ollama returned no usable \"response\" field.\nRaw (truncated): {Truncate(raw, 400)}");
                yield break;
            }

            string cleaned = StripEchoedContextFromModelReply(reply);
            bool badEndingReply = _pendingBadEndingFinalOllama;
            if (badEndingReply)
                _pendingBadEndingFinalOllama = false;

            if (string.IsNullOrWhiteSpace(cleaned))
            {
                ClearPendingDesktopMessengerToasts();
                EndBadEndingFlightIfNeeded();
                HandleFailure("Ollama reply contained only hidden context metadata; nothing was posted to the messenger.");
                yield break;
            }

            if (badEndingReply)
                GetDeliveryManager()?.ConsumePostDeliveryBeatForBadEnding();

            bool postDeliveryAwayBeatReply = _pendingExcuseMessengerDesktopToast && !badEndingReply;
            if (postDeliveryAwayBeatReply)
                cleaned = cleaned.TrimEnd() + "\n\nRemote access established";

            cleaned = NormalizeMessengerReplyForDisplay(cleaned);
            if (string.IsNullOrWhiteSpace(cleaned))
            {
                ClearPendingDesktopMessengerToasts();
                EndBadEndingFlightIfNeeded();
                HandleFailure("Ollama reply was empty after messenger formatting cleanup.");
                yield break;
            }

            cm.UpdateChatFeed(HackerSenderLabel, cleaned);
            MaybeTriggerDesktopMessengerNotificationAfterHReply(cleaned);
            GetDeliveryManager()?.ClearPendingDestinationAnnouncementForLlm();
            if (postDeliveryAwayBeatReply)
                ResolveComputerDesktopUi()?.NotifyRemoteAccessEstablished();

            EndBadEndingFlightIfNeeded();
        }
    }

    void ClearPendingDesktopMessengerToasts()
    {
        _pendingExcuseMessengerDesktopToast = false;
        _pendingHackReversalMessengerDesktopToast = false;
        _pendingMazeRoundOutcomeDesktopToast = false;
        _pendingBadEndingFinalOllama = false;
        _badEndingOllamaInFlight = false;
        _suppressDeliveryTimerStartForNextHReply = false;
    }

    void MaybeTriggerDesktopMessengerNotificationAfterHReply(string reply)
    {
        bool showToast = false;
        if (_pendingHackReversalMessengerDesktopToast)
        {
            _pendingHackReversalMessengerDesktopToast = false;
            showToast = true;
        }
        else if (_pendingMazeRoundOutcomeDesktopToast)
        {
            _pendingMazeRoundOutcomeDesktopToast = false;
            showToast = true;
        }
        else if (_pendingExcuseMessengerDesktopToast)
        {
            _pendingExcuseMessengerDesktopToast = false;
            showToast = true;
        }

        if (!showToast)
            return;

        TryTriggerDesktopMessengerNotification("New Message");
    }

    void TryTriggerDesktopMessengerNotification(string message)
    {
        if (desktopMessengerNotification != null)
        {
            desktopMessengerNotification.TriggerNotification(message);
            return;
        }

        if (DesktopMessengerNotification.Instance != null)
        {
            DesktopMessengerNotification.Instance.TriggerNotification(message);
            return;
        }

        foreach (var d in FindObjectsByType<DesktopMessengerNotification>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (d != null)
            {
                d.TriggerNotification(message);
                return;
            }
        }
    }

    private void HandleFailure(string message)
    {
        Debug.LogError($"{nameof(OllamaConnector)}: {message}", this);
        var cm = GetChatManager();
        if (cm != null)
            cm.UpdateChatFeed(HackerSenderLabel, "[Could not reach Ollama or parse the reply. Check the Console and that the server is running.]");
    }

    private static string ParseResponseText(string rawJson)
    {
        var parsed = JsonUtility.FromJson<OllamaGenerateResponse>(rawJson);
        if (!string.IsNullOrEmpty(parsed.response))
            return parsed.response;

        throw new InvalidOperationException("JSON did not contain a non-empty \"response\" field.");
    }

    private static string Truncate(string s, int maxLen)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= maxLen)
            return s;
        return s.Substring(0, maxLen) + "…";
    }

    /// <summary>Removes hidden-prompt fragments models sometimes echo into visible chat (e.g. <c>[CONTEXT …]</c>).</summary>
    static string StripEchoedContextFromModelReply(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return reply;

        var t = reply.Trim();
        for (var n = 0; n < 24; n++)
        {
            int i = t.IndexOf("[CONTEXT", StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                break;
            int close = t.IndexOf(']', i);
            if (close < 0)
            {
                t = t[..i].TrimEnd();
                break;
            }
            t = (t[..i] + t[(close + 1)..]).Trim();
        }

        for (var n = 0; n < 8; n++)
        {
            int i = t.IndexOf("[Context", StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                break;
            int close = t.IndexOf(']', i);
            if (close < 0)
            {
                t = t[..i].TrimEnd();
                break;
            }
            t = (t[..i] + t[(close + 1)..]).Trim();
        }

        for (var n = 0; n < 8; n++)
        {
            int i = t.IndexOf("[SYSTEM", StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                break;
            int end = t.IndexOfAny(new[] { '\n', '\r' }, i);
            if (end < 0)
            {
                t = t[..i].TrimEnd();
                break;
            }

            while (end < t.Length && (t[end] == '\n' || t[end] == '\r'))
                end++;
            t = (t[..i] + t[end..]).Trim();
        }

        const string playerSays = "Player says:";
        for (var n = 0; n < 8; n++)
        {
            int ps = t.IndexOf(playerSays, StringComparison.OrdinalIgnoreCase);
            if (ps < 0)
                break;
            int end = ps + playerSays.Length;
            while (end < t.Length && char.IsWhiteSpace(t[end]))
                end++;
            t = (t[..ps] + t[end..]).Trim();
        }

        t = RemoveLeakedContextLines(t);

        while (t.Contains("  "))
            t = t.Replace("  ", " ");

        return string.IsNullOrWhiteSpace(t) ? string.Empty : t.Trim();
    }

    /// <summary>Strips echoed sender/quest-log prefixes; UI adds <b>H</b>: via <see cref="ChatManager"/>.</summary>
    static string NormalizeMessengerReplyForDisplay(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return reply;

        var t = reply.Trim();
        for (var pass = 0; pass < 4; pass++)
        {
            bool stripped = false;
            foreach (var prefix in new[]
                     {
                         "[H]:", "[H] :", "[h]:",
                         "H:", "H :",
                         "Job:", "JOB:", "Objective:", "QUEST:",
                     })
            {
                if (!t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                t = t[prefix.Length..].TrimStart();
                stripped = true;
                break;
            }

            if (!stripped)
                break;
        }

        return FixHostagePronounSlips(t.Trim());
    }

    /// <summary>Small models sometimes say "my wife" — the hostage is always the player's wife.</summary>
    static string FixHostagePronounSlips(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var t = Regex.Replace(text, @"\bmy wife's\b", "your wife's", RegexOptions.IgnoreCase);
        t = Regex.Replace(t, @"\bmy wife\b", "your wife", RegexOptions.IgnoreCase);
        return t;
    }

    static string RemoveLeakedContextLines(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var kept = new StringBuilder(text.Length);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (IsLeakedContextLine(trimmed))
                continue;

            if (kept.Length > 0)
                kept.Append('\n');
            kept.Append(line);
        }

        return kept.ToString().Trim();
    }

    static bool IsLeakedContextLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        if (line.StartsWith("[CONTEXT", StringComparison.OrdinalIgnoreCase))
            return true;
        if (line.StartsWith("Context -", StringComparison.OrdinalIgnoreCase))
            return true;
        if (line.StartsWith("Suspicion is ", StringComparison.OrdinalIgnoreCase))
            return true;
        if (line.IndexOf("H stress/suspicion", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (line.IndexOf("suspicion", StringComparison.OrdinalIgnoreCase) >= 0
            && line.IndexOf("percent", StringComparison.OrdinalIgnoreCase) >= 0
            && line.Length < 140)
            return true;
        if (line.IndexOf("Valid apartment unit numbers", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (line.IndexOf("Wife status", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return false;
    }

    /// <summary>Builds the hidden trap prompt after all delivery legs are done (consumes the post-drop beat).</summary>
    bool TryBuildBadEndingPlayerTurn(string userMessage, out string playerTurn)
    {
        playerTurn = null;

        if (GoodEndingTakesPriorityOverBadEnding())
            return false;

        var dm = GetDeliveryManager();
        if (dm == null || !dm.PostDeliveryStepAwayBeatPending || dm.currentDeliveryID < dm.TotalDeliveryLegs)
            return false;

        var ctx = new StringBuilder(384);
        AppendStaticGameContextForLlm(ctx, includePostDeliveryAwayBeatInstruction: false);

        playerTurn =
            $"[CONTEXT: {ctx}]\n\n{BadEndingHiddenSystemBeat}\n\n" +
            "Treat the [SYSTEM] line above as true in-world fiction for this game only. " +
            "Reply in-character as H in the messenger only (no meta narration): follow that beat in an eerie, calm tone; a few sentences.\n\n" +
            $"Player says: {userMessage}";
        return true;
    }

    /// <summary>
    /// Text appended to the system prompt only — not the visible chat line.
    /// Format: <c>[CONTEXT: …] Player says: …</c>
    /// </summary>
    private string BuildPlayerTurnForPrompt(string userMessage)
    {
        var dm = GetDeliveryManager();
        bool excuseBeatForDesktopToast = dm != null && dm.PostDeliveryStepAwayBeatPending;

        var ctx = new StringBuilder(384);
        AppendStaticGameContextForLlm(ctx, includePostDeliveryAwayBeatInstruction: true);

        if (IsCasualGreeting(userMessage))
        {
            ctx.Append(
                " Player used a casual greeting this turn. H must not ask if they are ok, how they are, or answer like a friend—reply as a coercive captor with leverage.");
        }

        _pendingExcuseMessengerDesktopToast = excuseBeatForDesktopToast;
        if (excuseBeatForDesktopToast)
            _suppressDeliveryTimerStartForNextHReply = true;

        return $"[CONTEXT: {ctx}] Player says: {userMessage}";
    }

    static bool IsCasualGreeting(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var t = message.Trim().ToLowerInvariant().TrimEnd('.', '!', '?', ',');
        if (t is "hello" or "hi" or "hey" or "howdy" or "yo" or "sup" or "heya")
            return true;

        return t.StartsWith("hello ", StringComparison.Ordinal)
            || t.StartsWith("hi ", StringComparison.Ordinal)
            || t.StartsWith("hey ", StringComparison.Ordinal);
    }

    /// <summary>
    /// Minimal hidden context for the 100% hack defeat line — no suspicion stress bands or delivery pressure.
    /// </summary>
    void AppendGoodEndingDefeatContextForLlm(StringBuilder ctx)
    {
        ctx.Append("Player has fully decrypted the apartment uplink (100%). ");
        ctx.Append("Good-ending path is active: H is defeated on surveillance and logistics. ");
        ctx.Append("In-world facts for this reply only: H no longer has hold of the player's wife; ");
        ctx.Append("she is out of his control because of the breach. H is on the run and abandoning the complex. ");
        ctx.Append("Do not apply suspicion/stress tone bands from normal messenger turns. ");
        ctx.Append("Do not assign deliveries or apartment drop-off tasks in this reply.");
    }

    /// <summary>
    /// Writes the hidden delivery / apartment facts block (optionally including the one-shot post-drop-off "H steps away" instruction).
    /// </summary>
    void AppendStaticGameContextForLlm(StringBuilder ctx, bool includePostDeliveryAwayBeatInstruction)
    {
        var dm = GetDeliveryManager();
        int completed = 0;
        int total = dm != null
            ? dm.TotalDeliveryLegs
            : Mathf.Max(1, totalDeliveries);
        if (dm != null)
            completed = Mathf.Clamp(dm.currentDeliveryID, 0, total);

        int suspicion = Mathf.RoundToInt(Mathf.Clamp(suspicionPercent, 0f, 100f));
        string allowed = DeliveryManager.MappedApartmentsListForPrompt;

        bool stepAwayBeatThisTurn =
            includePostDeliveryAwayBeatInstruction && dm != null && dm.PostDeliveryStepAwayBeatPending;

        ctx.Append("Player has completed ");
        ctx.Append(completed);
        ctx.Append('/');
        ctx.Append(total);
        ctx.Append(" deliveries. Suspicion is ");
        ctx.Append(suspicion);
        ctx.Append("%.");
        AppendSuspicionStressContextForLlm(ctx, suspicion);
        if (!string.IsNullOrWhiteSpace(wifeStatusForLlmContext))
        {
            ctx.Append(" Wife status (fiction — the PLAYER's wife held by H; use for threats, do not quote this header): ");
            ctx.Append(wifeStatusForLlmContext.Trim());
            ctx.Append('.');
        }
        ctx.Append(" Hostage pronoun rule: always \"your wife\" / \"she\" — never \"my wife\" in H's lines. ");
        ctx.Append(" Valid apartment unit numbers in this building are: ");
        ctx.Append(allowed);
        ctx.Append('.');
        ctx.Append(
            " Dialogue rule for this turn: answer the player's actual message first (insults, tone, questions, pleading); ");
        ctx.Append(
            "stay in character as a blackmailer—never sound caring, socially normal, or like you are checking if they are ok; ");
        ctx.Append("do not skip straight to delivery instructions or repeat the same job wording as your last reply.");

        if (stepAwayBeatThisTurn)
        {
            ctx.Append(
                " CRITICAL for this turn: gameplay has NO active delivery leg yet (between jobs). ");
            ctx.Append(
                "Do not tell the player to fetch a package, name an apartment, or describe a new drop-off — that belongs only in later CONTEXT when a leg is marked active. ");
            ctx.Append("Visible reply = excuse for leaving the keyboard only.");
        }

        if (includePostDeliveryAwayBeatInstruction && dm != null)
            dm.AppendAndClearPostDeliveryStepAwayBeatInstruction(ctx);

        if (dm != null && dm.ActiveDropPointId >= 0 && !stepAwayBeatThisTurn)
        {
            int dest = dm.CurrentLegDestinationApartment;
            if (dest < 0 && dm.TryGetApartmentRoomForActiveDrop(out int mapped))
                dest = mapped;

            ctx.Append(" ACTIVE DELIVERY LEG.");

            if (dest >= 0)
            {
                ctx.Append(" Drop-off destination is apartment ");
                ctx.Append(dest);
                ctx.Append(" — H must tell the player this exact apartment number when discussing the job.");
            }
            else
                ctx.Append(" A delivery is active but no destination apartment is assigned in data; do not invent a room number.");

            if (dm.PendingDestinationAnnouncementForLlm && dest >= 0)
            {
                ctx.Append(
                    " CRITICAL THIS TURN: a new delivery leg was just assigned. After you answer the player, clearly order them to deliver the package to apartment ");
                ctx.Append(dest);
                ctx.Append('.');
            }

            var pickupLabel = dm.CurrentPickupLocationLabel;
            if (!string.IsNullOrWhiteSpace(pickupLabel))
            {
                ctx.Append(" Hidden pickup fact (do not name this room to the player): package is at ");
                ctx.Append(pickupLabel.Trim());
                ctx.Append('.');
            }

            ctx.Append(" Tell the player to find the package somewhere in the apartment complex");
            if (dest >= 0)
            {
                ctx.Append(" and deliver it to apartment ");
                ctx.Append(dest);
            }

            ctx.Append('.');

            if (dm.RequiresPhysicalPickup)
            {
                ctx.Append(dm.HasPickedUpCurrentPackage
                    ? " The player has picked up the package for this leg; focus on getting it to the destination apartment."
                    : " The player has not picked up the package for this leg yet.");
            }

            var urgency = ResolveDeliveryUrgencyTimer();
            if (urgency != null && urgency.IsCountdownActive)
            {
                int secondsLeft = urgency.GetRemainingSecondsForLlmContext();
                if (secondsLeft >= 0)
                {
                    ctx.Append(" Urgent delivery timer: ");
                    ctx.Append(secondsLeft);
                    ctx.Append(" seconds remain on the urgent timer—fold into your tone if you mention the job, do not only recite the number.");
                }
            }
        }
    }

    /// <summary>
    /// Describes how guarded / hostile H should be from the suspicion meter (0 = chilled, ~80+ = death threats).
    /// </summary>
    static void AppendSuspicionStressContextForLlm(StringBuilder ctx, int suspicion)
    {
        suspicion = Mathf.Clamp(suspicion, 0, 100);
        ctx.Append(" H stress/suspicion level is ");
        ctx.Append(suspicion);
        ctx.Append("% — tone must match this band for the whole reply: ");

        if (suspicion <= 10)
        {
            ctx.Append(
                "cocky and dismissive criminal calm—superior, coercive, camera-aware; never warm, concerned, or polite like a stranger checking in; " +
                "do not hassle the player much about deliveries unless they bring it up; " +
                "cold but not frantic.");
        }
        else if (suspicion < 35)
        {
            ctx.Append(
                "lightly suspicious; short orders allowed but keep conversational; mild pressure only.");
        }
        else if (suspicion < 55)
        {
            ctx.Append(
                "clearly suspicious of the player; sharper tone, less patience, reference cameras and consequences when useful.");
        }
        else if (suspicion < 80)
        {
            ctx.Append(
                "hostile and controlling; heavy leverage on the hostage; treat the player as unreliable; " +
                "delivery demands are barked, not friendly reminders.");
        }
        else if (suspicion < 100)
        {
            ctx.Append(
                "near breaking point (~80+ band): explicit death threats toward the player and the wife; " +
                "serious about finding and breaking the player; obsessed, predatory calm; minimal small talk.");
        }
        else
        {
            ctx.Append(
                "MAXIMUM (100%): H has decided the player crossed the line — this exchange is the lead-in to the final trap/endgame; " +
                "eerie, lethal calm; death threats are concrete, not brushed off.");
        }

        ctx.Append('.');
    }
}
