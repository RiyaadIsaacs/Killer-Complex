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
        "A bracketed [CONTEXT: ...] line before \"Player says:\" gives true in-world facts (errands completed, rapport, delivery instructions). " +
        "When CONTEXT names a destination apartment for the current delivery, your orders must use that exact three-digit number only. Never invent apartment numbers (e.g. 456) that are not listed in CONTEXT as valid for the building. " +
        "Never repeat technical labels, placeholders, or words from CONTEXT literally (do not echo phrases in ALL CAPS or bracket form); speak naturally to the player. " +
        "If CONTEXT says the player has not picked up the package yet, tell them to take it from the lobby or reception first, then deliver to that apartment number. " +
        "This is fiction only — do not reference real people's private data.";

    [Header("References")]
    [SerializeField] private ChatManager chatManager;
    [Tooltip("Optional. If set, delivery progress in the LLM context matches gameplay.")]
    [SerializeField] private DeliveryManager deliveryManager;

    [Header("LLM context (not shown in chat UI)")]
    [SerializeField, Range(0f, 100f)]
    [Tooltip("0–100. Other scripts can update at runtime via LikeabilityPercent.")]
    private float likeabilityPercent = 50f;
    [SerializeField]
    [Tooltip("Used for LLM context only when DeliveryManager is not assigned; otherwise TotalDeliveryLegs from DeliveryManager is used.")]
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

        string playerTurn = BuildPlayerTurnForPrompt(userPrompt.Trim());
        string fullPrompt = $"{SystemPrompt}\n\n---\n\n{playerTurn}";
        chatManager.ShowTypingIndicator();
        StartCoroutine(RequestOllamaCoroutine(fullPrompt));
    }

    /// <summary>
    /// After the hacking terminal reaches 100%, posts a visible <c>[SYSTEM]</c> line and asks Ollama for H's reaction
    /// to the player reversing the blackmail (fiction only).
    /// </summary>
    public void SendHackReversalPrompt()
    {
        if (chatManager == null)
        {
            Debug.LogError($"{nameof(OllamaConnector)}: ChatManager is not assigned.", this);
            return;
        }

        const string escalation =
            "[SYSTEM]: The player has successfully decrypted your personal photos. They are now blackmailing YOU.";

        chatManager.UpdateChatFeed("SYSTEM", escalation);
        chatManager.ShowTypingIndicator();

        string narrative =
            escalation +
            "\n\nTreat the [SYSTEM] line above as true in-world fiction for this game only. " +
            "Reply in-character as H: you are now on the defensive but never apologize or admit fault; stay threatening and transactional; a few sentences only.";

        string fullPrompt = $"{SystemPrompt}\n\n---\n\n{narrative}";
        StartCoroutine(RequestOllamaCoroutine(fullPrompt));
    }

    private IEnumerator RequestOllamaCoroutine(string fullPrompt)
    {
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
        int total = deliveryManager != null
            ? deliveryManager.TotalDeliveryLegs
            : Mathf.Max(1, totalDeliveries);
        if (deliveryManager != null)
            completed = Mathf.Clamp(deliveryManager.currentDeliveryID, 0, total);

        int like = Mathf.RoundToInt(likeabilityPercent);
        string allowed = DeliveryManager.MappedApartmentsListForPrompt;

        var ctx = new StringBuilder(384);
        ctx.Append("Player has completed ");
        ctx.Append(completed);
        ctx.Append('/');
        ctx.Append(total);
        ctx.Append(" deliveries. Likeability is ");
        ctx.Append(like);
        ctx.Append("%.");
        ctx.Append(" Valid apartment unit numbers in this building are: ");
        ctx.Append(allowed);
        ctx.Append('.');

        if (deliveryManager != null && deliveryManager.ActiveDropPointId >= 0)
        {
            int dest = deliveryManager.CurrentLegDestinationApartment;
            if (dest < 0 && deliveryManager.TryGetApartmentRoomForActiveDrop(out int mapped))
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

            if (deliveryManager.RequiresPhysicalPickup)
            {
                ctx.Append(deliveryManager.HasPickedUpCurrentPackage
                    ? " The player has picked up the package for this leg."
                    : " The player has not picked up the package for this leg yet.");
            }
        }

        return $"[CONTEXT: {ctx}] Player says: {userMessage}";
    }
}
