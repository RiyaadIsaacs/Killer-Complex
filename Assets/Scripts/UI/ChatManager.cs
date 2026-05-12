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

    private void Awake()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendClicked);

        if (chatInputField != null)
            chatInputField.onSubmit.AddListener(OnInputSubmit);
    }

    private void OnDestroy()
    {
        if (sendButton != null)
            sendButton.onClick.RemoveListener(OnSendClicked);

        if (chatInputField != null)
            chatInputField.onSubmit.RemoveListener(OnInputSubmit);
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
    }

    private static string FormatLine(string senderName, string body)
    {
        return $"[{senderName}]: {body}";
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
