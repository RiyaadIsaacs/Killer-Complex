using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    private string openingMessage =
        "I see you've finally logged on. Don't bother looking for the files you deleted; I have copies of everything. " +
        "Your computer wasn't all that secure. You're going to do exactly as I say if you want to keep your reputation in this market. " +
        "There's a package in the lobby. Move it now, or everyone in your contacts list and the building manager will get a very interesting email. " +
        "Don't keep me waiting, bru.";

    private bool _openingMessageShown;

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
        if (_openingMessageShown || string.IsNullOrWhiteSpace(openingMessage))
            return;

        _openingMessageShown = true;
        UpdateChatFeed(introSenderName, openingMessage);
    }

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

    /// <summary>
    /// Append a line from another sender (e.g. AI). Formatted as [senderName]: response
    /// </summary>
    public void UpdateChatFeed(string senderName, string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return;

        AppendLine(FormatLine(senderName, response.Trim()));
    }

    private void TrySendPlayerMessage()
    {
        if (chatInputField == null)
            return;

        string text = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        AppendLine(FormatLine(PlayerLabel, text));
        chatInputField.text = string.Empty;
        chatInputField.ActivateInputField();

        if (ollamaConnector != null)
            ollamaConnector.SendToOllama(text);
    }

    private static string FormatLineStatic(string senderName, string body)
    {
        return $"[{senderName}]: {body}";
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
