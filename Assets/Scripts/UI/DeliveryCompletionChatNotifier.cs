using UnityEngine;

/// <summary>
/// Listens to <see cref="DeliveryManager.OnDeliveryCompleted"/> and appends a scripted messenger line from H
/// when at least one delivery step remains — gives narrative space before the next job / hacking beats.
/// Does not call Ollama; the line is immediate UI text only.
/// </summary>
public class DeliveryCompletionChatNotifier : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DeliveryManager deliveryManager;
    [SerializeField] private ChatManager chatManager;

    [Header("Messenger copy")]
    [SerializeField] private string hackerSenderName = "H";
    [SerializeField, TextArea(2, 5)]
    private string messageWhenMoreDeliveriesRemain =
        "Well done. I'll organise another delivery for you to do.";

    [Header("Delivery flow")]
    [Tooltip("After H's scripted line, roll a new random drop point and activate the reception DeliveryItem (if configured on DeliveryManager).")]
    [SerializeField] private bool prepareNextDeliveryWhenScriptedLineFires = true;

    private void OnEnable()
    {
        if (deliveryManager != null)
            deliveryManager.OnDeliveryCompleted += OnDeliveryCompleted;
    }

    private void OnDisable()
    {
        if (deliveryManager != null)
            deliveryManager.OnDeliveryCompleted -= OnDeliveryCompleted;
    }

    private void OnDeliveryCompleted(int completedDeliveryId)
    {
        if (chatManager == null || deliveryManager == null)
            return;

        if (string.IsNullOrWhiteSpace(messageWhenMoreDeliveriesRemain))
            return;

        // After completion, currentDeliveryID is already the next step (or 3 when the quota is finished).
        if (deliveryManager.currentDeliveryID >= 3)
            return;

        chatManager.UpdateChatFeed(hackerSenderName, messageWhenMoreDeliveriesRemain.Trim());

        if (prepareNextDeliveryWhenScriptedLineFires)
            deliveryManager.PrepareNextDeliveryFromAi();
    }
}
