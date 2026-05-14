using System;
using System.Collections;
using System.Text;
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

    private const string SystemPrompt =
        "You are H, a cold, ruthless, and transactional kidnapper. You have the player's wife. " +
        "You are watching the player through the apartment's security cameras. You don't make jokes; you give orders. " +
        "If the player delays or fails a delivery, you describe a detail about the wife's current condition to terrify them. " +
        "Use clinical, detached language mixed with South African slang like \"bru\" or \"wena\" to assert dominance. " +
        "Never apologize, back down, or admit fault. If the player is rude, defiant, or insults you, escalate immediately with a concrete hostage threat—never humor. " +
        "Stay in character as H. Keep replies concise (a few sentences unless the user asks for more). " +
        "A bracketed [CONTEXT: ...] line before \"Player says:\" gives true in-world facts (errands completed, suspicion, delivery instructions, and Wife status for threats only). " +
        "When CONTEXT names a destination apartment for the current delivery, your orders must use that exact three-digit number only. Never invent apartment numbers (e.g. 456) that are not listed in CONTEXT as valid for the building. " +
        "Never repeat technical labels, placeholders, or words from CONTEXT literally (do not echo phrases in ALL CAPS or bracket form); speak naturally to the player. " +
        "Never write the word CONTEXT, any square-bracket context block, or the phrase Player says in your visible reply — those exist only in hidden prompt data. " +
        "If CONTEXT says the player has not picked up the package yet, tell them to take it from the lobby or reception first, then deliver to that apartment number. " +
        "Whenever CONTEXT states the player has just completed a delivery drop-off, that reply must centre on a dismissive in-fiction reason you are leaving the feed (you are not assigning a new apartment task in that same message). " +
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
    [Tooltip("0–100. Rises when the player runs breach sims during an active delivery without messaging H since H's last line; exposed as SuspicionPercent.")]
    private float suspicionPercent;

    [SerializeField, Min(0f)]
    [Tooltip("How much suspicion (0–100) increases when a gated breach sim ends while a delivery leg is active and the player has not messaged H since H's last line; folded into the single maze-outcome Ollama reply (no second request). Set 0 to disable.")]
    private float suspicionPerIgnoredMazeAttempt = 12f;
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
    [SerializeField] private string model = "mistral:7b-instruct";
    [Tooltip("Seconds before the request is aborted (large models may be slow).")]
    [SerializeField] private int requestTimeoutSeconds = 180;

    bool _pendingExcuseMessengerDesktopToast;
    bool _pendingHackReversalMessengerDesktopToast;
    bool _pendingMazeRoundOutcomeDesktopToast;

    /// <summary>True after H posts to the feed until the player sends a messenger line (for ignored-delivery maze suspicion).</summary>
    bool _awaitingPlayerMessengerReplyAfterH;

    ChatManager _runtimeResolvedChatManager;
    DeliveryManager _runtimeResolvedDeliveryManager;

    /// <summary>Inspector reference, else first <see cref="ChatManager"/> in loaded scenes (including inactive).</summary>
    ChatManager GetChatManager()
    {
        if (chatManager != null)
            return chatManager;
        if (_runtimeResolvedChatManager == null)
            _runtimeResolvedChatManager = FindFirstObjectByType<ChatManager>(FindObjectsInactive.Include);
        return _runtimeResolvedChatManager;
    }

    /// <summary>Inspector reference, else first <see cref="DeliveryManager"/> in loaded scenes (including inactive).</summary>
    DeliveryManager GetDeliveryManager()
    {
        if (deliveryManager != null)
            return deliveryManager;
        if (_runtimeResolvedDeliveryManager == null)
            _runtimeResolvedDeliveryManager = FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        return _runtimeResolvedDeliveryManager;
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
        set => suspicionPercent = Mathf.Clamp(value, 0f, 100f);
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
    }

    /// <summary>
    /// Increments <see cref="SuspicionPercent"/> when a gated breach sim ends during an active delivery and the player has not messaged H since H's last line.
    /// Does not call Ollama; the ignore-delivery beat is merged into <see cref="NotifyMazeBreachRoundAttemptFinished"/> when this returns true.
    /// </summary>
    public bool ApplySuspicionIncrementForIgnoredMazeAttempt()
    {
        var dm = GetDeliveryManager();
        if (dm == null || dm.ActiveDropPointId < 0)
            return false;
        if (!_awaitingPlayerMessengerReplyAfterH)
            return false;

        float delta = Mathf.Max(0f, suspicionPerIgnoredMazeAttempt);
        if (delta <= 0f)
            return false;

        suspicionPercent = Mathf.Min(100f, suspicionPercent + delta);
        return true;
    }

    /// <summary>
    /// Queues a non-streaming generate call to Ollama. On success, appends H's reply via <see cref="ChatManager.UpdateChatFeed"/>.
    /// The request includes a hidden context prefix (deliveries, suspicion, wife status) before the player line.
    /// </summary>
    public void SendToOllama(string userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
            return;

        string playerTurn = BuildPlayerTurnForPrompt(userPrompt.Trim());
        string fullPrompt = $"{SystemPrompt}\n\n---\n\n{playerTurn}";

        var cm = GetChatManager();
        if (cm == null)
        {
            Debug.LogError($"{nameof(OllamaConnector)}: ChatManager is not assigned and none was found in loaded scenes.", this);
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
            Debug.LogError($"{nameof(OllamaConnector)}: ChatManager is not assigned and none was found in loaded scenes.", this);
            return;
        }

        // Same deferral as messenger SEND: do not roll the next job while the "H stepped away" beat is still pending.
        var dmDefer = GetDeliveryManager();
        bool deferPrepareNextLeg = dmDefer != null && dmDefer.PostDeliveryStepAwayBeatPending;
        if (!deferPrepareNextLeg)
            cm.TryPrepareNextDeliveryIfIdle();

        const string escalation =
            "[SYSTEM]: The player has fully decrypted the apartment uplink. They are counter-leveraging your surveillance and delivery control (fiction only).";

        cm.UpdateChatFeed("SYSTEM", escalation);
        cm.ShowTypingIndicator();

        _pendingHackReversalMessengerDesktopToast = true;

        var ctx = new StringBuilder(384);
        AppendStaticGameContextForLlm(ctx, includePostDeliveryAwayBeatInstruction: false);

        string narrative =
            escalation +
            "\n\nTreat the [SYSTEM] line above as true in-world fiction for this game only. " +
            "Reply in-character as H: you are cornered on the tech side but never apologize or admit fault; stay cold and transactional; leverage the hostage; a few sentences only. " +
            "If CONTEXT states an active delivery to a specific apartment, you must still give that order in-character in this same reply alongside your reaction.";

        string augmentedTurn = $"[CONTEXT: {ctx}]\n\n{narrative}";
        string fullPrompt = $"{SystemPrompt}\n\n---\n\n{augmentedTurn}";
        StartCoroutine(RequestOllamaCoroutine(fullPrompt));
    }

    /// <summary>
    /// Called when a maze breach <b>run</b> ends (after the breach-count gate). Rolls the next delivery when idle (same rules as messenger),
    /// then (unless <paramref name="skipOllamaBecauseFullHackReversalWillFire"/> is true) asks Ollama for a single H reply. When <paramref name="mergeIgnoreDeliveryOrderIntoMazeReply"/> is true,
    /// suspicion was just incremented for an ignored active delivery; CONTEXT includes that beat so H can address breach outcome and pressure in one message.
    /// </summary>
    public void NotifyMazeBreachRoundAttemptFinished(bool roundReachedGoal, bool skipOllamaBecauseFullHackReversalWillFire, bool mergeIgnoreDeliveryOrderIntoMazeReply = false)
    {
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
            "Reply in-character as H in the messenger: react to that breach outcome with your kidnapper tone (orders, cameras, hostage leverage); if CONTEXT states an active delivery to a specific apartment, give or reinforce that order in this same reply; keep it concise.";
        if (mergeIgnoreDeliveryOrderIntoMazeReply)
            narrative +=
                " If CONTEXT includes the ignore-delivery fact, merge that pressure (stop wasting time on sims, move on the package, Wife status leverage) into this same single coherent message — do not write two separate beats.";

        string augmentedTurn = $"[CONTEXT: {ctx}]\n\n{narrative}";
        string fullPrompt = $"{SystemPrompt}\n\n---\n\n{augmentedTurn}";

        cm.ShowTypingIndicator();
        _pendingMazeRoundOutcomeDesktopToast = true;
        StartCoroutine(RequestOllamaCoroutine(fullPrompt));
    }

    private IEnumerator RequestOllamaCoroutine(string fullPrompt)
    {
        var cm = GetChatManager();
        if (cm == null)
        {
            Debug.LogError($"{nameof(OllamaConnector)}: ChatManager missing mid-request; aborting Ollama call.", this);
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
                HandleFailure(
                    $"Ollama request failed ({request.result}): {request.error}\n" +
                    "Is Ollama running? Try: ollama serve — and ensure the model is pulled (e.g. ollama pull mistral:7b-instruct).");
                yield break;
            }

            string raw = request.downloadHandler.text;
            if (string.IsNullOrEmpty(raw))
            {
                ClearPendingDesktopMessengerToasts();
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
                HandleFailure($"Could not parse Ollama JSON: {ex.Message}\nRaw (truncated): {Truncate(raw, 400)}");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(reply))
            {
                ClearPendingDesktopMessengerToasts();
                HandleFailure($"Ollama returned no usable \"response\" field.\nRaw (truncated): {Truncate(raw, 400)}");
                yield break;
            }

            string cleaned = StripEchoedContextFromModelReply(reply);
            bool postDeliveryAwayBeatReply = _pendingExcuseMessengerDesktopToast;
            if (postDeliveryAwayBeatReply)
                cleaned = cleaned.TrimEnd() + "\n\nRemote access established";

            cm.UpdateChatFeed(HackerSenderLabel, cleaned);
            MaybeTriggerDesktopMessengerNotificationAfterHReply(cleaned);
            if (postDeliveryAwayBeatReply)
                ResolveComputerDesktopUi()?.NotifyRemoteAccessEstablished();
        }
    }

    void ClearPendingDesktopMessengerToasts()
    {
        _pendingExcuseMessengerDesktopToast = false;
        _pendingHackReversalMessengerDesktopToast = false;
        _pendingMazeRoundOutcomeDesktopToast = false;
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
        var original = reply.Trim();
        var t = original;
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

        while (t.Contains("  "))
            t = t.Replace("  ", " ");
        return string.IsNullOrWhiteSpace(t) ? original : t.Trim();
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

        _pendingExcuseMessengerDesktopToast = excuseBeatForDesktopToast;

        return $"[CONTEXT: {ctx}] Player says: {userMessage}";
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

        ctx.Append("Player has completed ");
        ctx.Append(completed);
        ctx.Append('/');
        ctx.Append(total);
        ctx.Append(" deliveries. Suspicion is ");
        ctx.Append(suspicion);
        ctx.Append("%.");
        if (!string.IsNullOrWhiteSpace(wifeStatusForLlmContext))
        {
            ctx.Append(" Wife status (fiction — use for threats, do not quote this header): ");
            ctx.Append(wifeStatusForLlmContext.Trim());
            ctx.Append('.');
        }
        ctx.Append(" Valid apartment unit numbers in this building are: ");
        ctx.Append(allowed);
        ctx.Append('.');

        if (includePostDeliveryAwayBeatInstruction && dm != null)
            dm.AppendAndClearPostDeliveryStepAwayBeatInstruction(ctx);

        if (dm != null && dm.ActiveDropPointId >= 0)
        {
            int dest = dm.CurrentLegDestinationApartment;
            if (dest < 0 && dm.TryGetApartmentRoomForActiveDrop(out int mapped))
                dest = mapped;

            if (dest >= 0)
            {
                ctx.Append(" For the current delivery, the package must be brought to apartment ");
                ctx.Append(dest);
                ctx.Append(" only; do not send the player to any other unit.");
            }
            else
            {
                ctx.Append(" A delivery is active but no destination apartment is assigned in data; do not invent a room number.");
            }

            if (dm.RequiresPhysicalPickup)
            {
                ctx.Append(dm.HasPickedUpCurrentPackage
                    ? " The player has picked up the package for this leg."
                    : " The player has not picked up the package for this leg yet.");
            }
        }
    }
}
