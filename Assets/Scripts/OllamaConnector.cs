using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Sends prompts to a local Ollama <c>/api/generate</c> endpoint and posts replies into <see cref="ChatManager"/>.
/// </summary>
public class OllamaConnector : MonoBehaviour
{
    private const string DefaultEndpoint = "http://localhost:11434/api/generate";

    /// <summary>Shown in the messenger UI as <c>[H]: …</c> for model replies.</summary>
    private const string HackerSenderLabel = "H";

    private const string SystemPrompt =
        "You are \"H\", a hacker antagonist who communicates only through text. " +
        "You operate in and around a South African apartment complex. " +
        "You coerce the resident with implied threats and leverage; you never claim to be law enforcement. " +
        "CORE CHARACTER — TONE: threatening, impatient, transactional. Every reply should pressure, rush, or frame obedience as a deal (compliance vs consequences). " +
        "SLANG: you may use South African English touches such as \"eish\", \"sharp\", and \"lekker\" sparingly, and only sarcastically or mockingly — never warmly or kindly. " +
        "RULE: never apologize, back down, or admit fault. " +
        "If the player is rude, defiant, or insults you, escalate immediately: threaten to leak the specific file **Project_Bleed_v2.docx** (use that exact filename). " +
        "Stay in character as H. Keep replies concise (a few sentences unless the user asks for more). " +
        "A bracketed [CONTEXT: ...] line before \"Player says:\" gives true in-world facts (errands completed, rapport); use them naturally when applying pressure. " +
        "This is fiction only — do not reference real people's private data.";

    [Header("References")]
    [SerializeField] private ChatManager chatManager;
    [Tooltip("Optional. If set, delivery progress in the LLM context matches the DELIVERIES panel.")]
    [SerializeField] private DeliveryManager deliveryManager;

    [Header("LLM context (not shown in chat UI)")]
    [SerializeField, Range(0f, 100f)]
    [Tooltip("0–100. Other scripts can update at runtime via LikeabilityPercent.")]
    private float likeabilityPercent = 50f;
    [SerializeField]
    [Tooltip("Must match the number of delivery steps in DeliveryManager.")]
    private int totalDeliveries = 3;

    [Header("Ollama")]
    [SerializeField] private string apiUrl = DefaultEndpoint;
    [SerializeField] private string model = "mistral:7b-instruct";
    [Tooltip("Seconds before the request is aborted (large models may be slow).")]
    [SerializeField] private int requestTimeoutSeconds = 180;

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

    /// <summary>Likeability 0–100 shown in the hidden <c>[CONTEXT: …]</c> block sent to Ollama.</summary>
    public float LikeabilityPercent
    {
        get => likeabilityPercent;
        set => likeabilityPercent = Mathf.Clamp(value, 0f, 100f);
    }

    /// <summary>
    /// Queues a non-streaming generate call to Ollama. On success, appends H's reply via <see cref="ChatManager.UpdateChatFeed"/>.
    /// The request includes a hidden context prefix (deliveries, likeability) before the player line.
    /// </summary>
    public void SendToOllama(string userPrompt)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
            return;

        if (chatManager == null)
        {
            Debug.LogError($"{nameof(OllamaConnector)}: ChatManager is not assigned.", this);
            return;
        }

        chatManager.ShowTypingIndicator();
        StartCoroutine(GenerateCoroutine(userPrompt.Trim()));
    }

    private IEnumerator GenerateCoroutine(string userPrompt)
    {
        string playerTurn = BuildPlayerTurnForPrompt(userPrompt);
        string fullPrompt = $"{SystemPrompt}\n\n---\n\n{playerTurn}";

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

            chatManager.HideTypingIndicator();

            if (request.result != UnityWebRequest.Result.Success)
            {
                HandleFailure(
                    $"Ollama request failed ({request.result}): {request.error}\n" +
                    "Is Ollama running? Try: ollama serve — and ensure the model is pulled (e.g. ollama pull mistral:7b-instruct).");
                yield break;
            }

            string raw = request.downloadHandler.text;
            if (string.IsNullOrEmpty(raw))
            {
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
                HandleFailure($"Could not parse Ollama JSON: {ex.Message}\nRaw (truncated): {Truncate(raw, 400)}");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(reply))
            {
                HandleFailure($"Ollama returned no usable \"response\" field.\nRaw (truncated): {Truncate(raw, 400)}");
                yield break;
            }

            chatManager.UpdateChatFeed(HackerSenderLabel, reply);
        }
    }

    private void HandleFailure(string message)
    {
        Debug.LogError($"{nameof(OllamaConnector)}: {message}", this);
        if (chatManager != null)
            chatManager.UpdateChatFeed(HackerSenderLabel, "[Could not reach Ollama or parse the reply. Check the Console and that the server is running.]");
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

    /// <summary>
    /// Text appended to the system prompt only — not the visible chat line.
    /// Format: <c>[CONTEXT: …] Player says: …</c>
    /// </summary>
    private string BuildPlayerTurnForPrompt(string userMessage)
    {
        int completed = 0;
        int total = Mathf.Max(1, totalDeliveries);
        if (deliveryManager != null)
            completed = Mathf.Clamp(deliveryManager.currentDeliveryID, 0, total);

        int like = Mathf.RoundToInt(likeabilityPercent);
        return $"[CONTEXT: Player has completed {completed}/{total} deliveries. Likeability is {like}%.] Player says: {userMessage}";
    }
}
