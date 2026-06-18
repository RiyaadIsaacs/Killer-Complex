using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Messenger feed: append lines to TMP inside a ScrollRect; send via TMP_InputField + SEND or Enter.
/// </summary>
public class ChatManager : MonoBehaviour
{
    private const string PlayerLabel = "Player";

    [Header("UI")]
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private TMP_Text chatFeedText;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private Button sendButton;

    [Header("LLM")]
    [Tooltip("Optional. When set, player messages are also sent to Ollama (with hidden game context on the connector).")]
    [SerializeField] private OllamaConnector ollamaConnector;

    [Header("Typing indicator")]
    [Tooltip("If set, typing text is shown here with a soft alpha pulse (not mixed into the main feed).")]
    [SerializeField] private TMP_Text typingIndicatorText;
    [SerializeField] private string typingIndicatorMessage = "H is typing...";
    [SerializeField, Min(0.05f)] private float typingPulseHalfPeriodSeconds = 0.55f;
    [SerializeField, Range(0f, 1f)] private float typingPulseAlphaMin = 0.35f;
    [SerializeField, Range(0f, 1f)] private float typingPulseAlphaMax = 1f;

    [Header("Intro")]
    [SerializeField] private string introSenderName = "H";
    [SerializeField, TextArea(5, 20)]
    private string openingMessage = DefaultOpeningMessage;

    /// <summary>Default messenger intro (also baked into <c>ComputerDesktopCanvas</c> prefab).</summary>
    public const string DefaultOpeningMessage =
        "I see you're finally at the computer. Stop looking for your wife\u2014she's not at home anymore. " +
        "If you want to see her again, you're running urgent package deliveries for my customers across this complex tonight. " +
        "When I give you a job, find the package somewhere in the building and get it to the unit I name\u2014fast. " +
        "If you don't get back to me, you will never hear from her again. Don't test me, bru. Move it.";

    [Header("Messenger → next delivery job")]
    [Tooltip("When true, each non-empty SEND prepares the next delivery leg if none is active yet and runs remain—before Ollama runs so CONTEXT includes the destination. Also used after each maze breach run ends (win, fail, or abort) and when full decryption completes (see OllamaConnector). Turn off only if another script prepares jobs.")]
    [SerializeField, FormerlySerializedAs("prepareFirstDeliveryOnFirstPlayerMessage")]
    private bool prepareDeliveryOnMessengerSendWhenIdle = true;

    [Tooltip("Optional. If unset, the first DeliveryManager found in loaded scenes is used.")]
    [SerializeField] private DeliveryManager deliveryManager;

    private bool _openingMessageShown;
    private DeliveryManager _cachedDeliveryManager;

    private bool _typingLineInFeed;
    private string _typingFeedLineSnapshot;
    private Color _typingIndicatorBaseColor = Color.white;
    private Coroutine _typingPulseCoroutine;
    private int _typingIndicatorRefCount;

    private void Awake()
    {
        if (typingIndicatorText != null)
            typingIndicatorText.gameObject.SetActive(false);

        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendClicked);

        if (chatInputField != null)
            chatInputField.onSubmit.AddListener(OnInputSubmit);
    }

    private void OnEnable()
    {
        ResolveOllamaConnector();

        if (_openingMessageShown || string.IsNullOrWhiteSpace(openingMessage))
            return;

        _openingMessageShown = true;
        UpdateChatFeed(introSenderName, openingMessage);
    }

#if UNITY_EDITOR
    [ContextMenu("Reset Opening Message To Default")]
    void ResetOpeningMessageToDefault()
    {
        openingMessage = DefaultOpeningMessage;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void OnDestroy()
    {
        if (sendButton != null)
            sendButton.onClick.RemoveListener(OnSendClicked);

        if (chatInputField != null)
            chatInputField.onSubmit.RemoveListener(OnInputSubmit);

        StopTypingPulse();
    }

    /// <summary>
    /// Shows that H is composing a reply. Uses <see cref="typingIndicatorText"/> when assigned; otherwise appends a single line to the feed.
    /// </summary>
    public void ShowTypingIndicator()
    {
        _typingIndicatorRefCount++;
        if (_typingIndicatorRefCount > 1)
            return;

        if (typingIndicatorText != null)
        {
            typingIndicatorText.text = typingIndicatorMessage;
            typingIndicatorText.gameObject.SetActive(true);
            _typingIndicatorBaseColor = typingIndicatorText.color;
            StopTypingPulse();
            _typingPulseCoroutine = StartCoroutine(PulseTypingIndicatorRoutine());
            RefreshScrollLayout();
            ScrollToBottom();
            return;
        }

        if (_typingLineInFeed || chatFeedText == null)
            return;

        _typingFeedLineSnapshot = BuildTypingFeedLine();
        _typingLineInFeed = true;
        AppendLine(_typingFeedLineSnapshot);
    }

    /// <summary>
    /// Hides the typing indicator (dedicated label or temporary feed line).
    /// </summary>
    public void HideTypingIndicator()
    {
        if (_typingIndicatorRefCount > 0)
            _typingIndicatorRefCount--;

        if (_typingIndicatorRefCount > 0)
            return;

        if (typingIndicatorText != null)
        {
            StopTypingPulse();
            typingIndicatorText.gameObject.SetActive(false);
            typingIndicatorText.color = _typingIndicatorBaseColor;
            RefreshScrollLayout();
            ScrollToBottom();
            return;
        }

        if (!_typingLineInFeed || chatFeedText == null)
            return;

        RemoveTypingLineFromFeed();
        _typingLineInFeed = false;
        _typingFeedLineSnapshot = null;
        RefreshScrollLayout();
        ScrollToBottom();
    }

    private void OnSendClicked()
    {
        TrySendPlayerMessage();
    }

    private void OnInputSubmit(string _)
    {
        TrySendPlayerMessage();
    }

    // Appends a new line to the feed with the format "[senderName]: response". 
    public void UpdateChatFeed(string senderName, string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return;

        var line = FormatLine(senderName, response.Trim());
        if (IsHSender(senderName))
            line += "\n";
        AppendLine(line);
        if (IsHSender(senderName))
        {
            var connector = ResolveOllamaConnector();
            if (connector != null)
                connector.NotifyHPostedToMessenger();
            else
                TryStartDeliveryUrgencyTimerAfterHMessage();
        }
    }

    /// <summary>True for messenger lines from <b>H</b> so we can add a blank line after each reply for readability.</summary>
    static bool IsHSender(string senderName)
    {
        if (string.IsNullOrWhiteSpace(senderName))
            return false;
        return string.Equals(senderName.Trim(), "H", StringComparison.OrdinalIgnoreCase);
    }

    private void TrySendPlayerMessage()
    {
        if (chatInputField == null)
            return;

        string text = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        var connector = ResolveOllamaConnector();
        if (connector != null)
            connector.NotifyPlayerMessengerSend();

        AppendLine(FormatLine(PlayerLabel, text));
        chatInputField.text = string.Empty;
        chatInputField.ActivateInputField();

        var dm = ResolveDeliveryManager();
        bool deferPrepareNextLeg = dm != null && dm.PostDeliveryStepAwayBeatPending;

        if (!deferPrepareNextLeg)
            MaybePrepareDeliveryWhenMessengerSendIfIdle();

        if (connector != null)
            connector.SendToOllama(text);
        else if (deferPrepareNextLeg && dm != null)
            dm.AbandonPostDeliveryStepAwayBeat();

        // Do not prepare the next leg on the same send that triggers H's post-drop "step away" reply.
        // The next job is rolled on the player's following messenger send (see refinements-changes.md).
    }

    void MaybePrepareDeliveryWhenMessengerSendIfIdle()
    {
        if (!prepareDeliveryOnMessengerSendWhenIdle)
            return;

        var dm = ResolveDeliveryManager();
        if (dm == null || dm.currentDeliveryID >= dm.TotalDeliveryLegs || dm.ActiveDropPointId >= 0)
            return;

        dm.PrepareNextDeliveryFromAi();
    }

    /// <summary>
    /// Rolls the next delivery when no leg is active and runs remain — same rules as messenger SEND
    /// (<see cref="prepareDeliveryOnMessengerSendWhenIdle"/>). Called from <see cref="OllamaConnector.NotifyMazeBreachRoundAttemptFinished"/>
    /// after each maze run (win, fail, or abort) and from <see cref="OllamaConnector.SendHackReversalPrompt"/> at full decryption,
    /// so <b>H</b> can mention the new job in the same reply as the hack-reversal beat when applicable.
    /// </summary>
    public void TryPrepareNextDeliveryIfIdle()
    {
        MaybePrepareDeliveryWhenMessengerSendIfIdle();
    }

    DeliveryManager ResolveDeliveryManager()
    {
        var persistent = GlobalNotificationHud.FindDeliveryManager();
        if (persistent != null)
        {
            deliveryManager = persistent;
            return persistent;
        }

        if (deliveryManager != null)
            return deliveryManager;

        _cachedDeliveryManager = UnityEngine.Object.FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        deliveryManager = _cachedDeliveryManager;
        return deliveryManager;
    }

    OllamaConnector ResolveOllamaConnector()
    {
        if (ollamaConnector != null)
            return ollamaConnector;

        ollamaConnector = UnityEngine.Object.FindFirstObjectByType<OllamaConnector>(FindObjectsInactive.Include);
        return ollamaConnector;
    }

    static void TryStartDeliveryUrgencyTimerAfterHMessage()
    {
        foreach (var timer in UnityEngine.Object.FindObjectsByType<DeliveryUrgencyTimer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (timer != null)
                timer.TryStartCountdownAfterHMessage();
        }
    }

    private static string FormatLineStatic(string senderName, string body)
    {
        string sender = string.IsNullOrWhiteSpace(senderName) ? "H" : senderName.Trim();
        string text = string.IsNullOrWhiteSpace(body) ? string.Empty : body.Trim();
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Bold sender + noparse body: avoids TMP eating [H] sprite tags and prevents model <tags> breaking layout.
        string safeBody = text.Replace("</noparse>", string.Empty, StringComparison.OrdinalIgnoreCase);
        return $"<b>{sender}</b>: <noparse>{safeBody}</noparse>";
    }

    private static string FormatLine(string senderName, string body)
    {
        return FormatLineStatic(senderName, body);
    }

    private string BuildTypingFeedLine()
    {
        string sender = string.IsNullOrWhiteSpace(introSenderName) ? "H" : introSenderName.Trim();
        string msg = string.IsNullOrWhiteSpace(typingIndicatorMessage) ? $"{sender} is typing..." : typingIndicatorMessage.Trim();
        return FormatLine(sender, msg);
    }

    private void RemoveTypingLineFromFeed()
    {
        if (string.IsNullOrEmpty(_typingFeedLineSnapshot))
            return;

        string t = chatFeedText.text;
        if (string.IsNullOrEmpty(t))
            return;

        var lines = new List<string>(t.Split('\n'));
        lines.Remove(_typingFeedLineSnapshot);
        chatFeedText.text = lines.Count > 0 ? string.Join("\n", lines) : string.Empty;
    }

    private void StopTypingPulse()
    {
        if (_typingPulseCoroutine != null)
        {
            StopCoroutine(_typingPulseCoroutine);
            _typingPulseCoroutine = null;
        }
    }

    private IEnumerator PulseTypingIndicatorRoutine()
    {
        float minA = typingPulseAlphaMin;
        float maxA = typingPulseAlphaMax;
        float spd = Mathf.Max(0.05f, typingPulseHalfPeriodSeconds);

        while (typingIndicatorText != null && typingIndicatorText.gameObject.activeInHierarchy)
        {
            float t = (Mathf.Sin(Time.unscaledTime * Mathf.PI / spd) + 1f) * 0.5f;
            float a = Mathf.Lerp(minA, maxA, t);
            Color c = _typingIndicatorBaseColor;
            c.a = a;
            typingIndicatorText.color = c;
            yield return null;
        }
    }

    private void AppendLine(string line)
    {
        if (chatFeedText == null)
            return;

        if (string.IsNullOrEmpty(chatFeedText.text))
            chatFeedText.text = line;
        else
            chatFeedText.text += "\n" + line;

        RefreshScrollLayout();
        ScrollToBottom();
    }

    private void RefreshScrollLayout()
    {
        if (chatFeedText == null)
            return;

        chatFeedText.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatFeedText.rectTransform);
    }

    private void ScrollToBottom()
    {
        if (chatScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }
}
