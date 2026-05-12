using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tracks delivery progress for the DELIVERIES panel. Fires <see cref="OnDeliveryCompleted"/> when the player completes the current step.
/// </summary>
public class DeliveryManager : MonoBehaviour
{
    [Header("Assignment lines (TMP)")]
    [SerializeField] private TMP_Text assignmentLine1;
    [SerializeField] private TMP_Text assignmentLine2;
    [SerializeField] private TMP_Text assignmentLine3;

    [Header("Copy (edit in Inspector)")]
    [SerializeField] private string assignment1 = "1. Drop off secure drive to Apt 401";
    [SerializeField] private string assignment2 = "2. Collect signed waiver from lobby";
    [SerializeField] private string assignment3 = "3. Return courier bag to the mail room";

    [Header("Actions")]
    [SerializeField] private Button completeCurrentDeliveryButton;

    /// <summary>
    /// Increments each time the player completes the current delivery (starts at 0).
    /// </summary>
    public int currentDeliveryID;

    /// <summary>
    /// Raised after a completion with the delivery id that was just finished (before <see cref="currentDeliveryID"/> was incremented).
    /// </summary>
    public event Action<int> OnDeliveryCompleted;

    private void Awake()
    {
        if (completeCurrentDeliveryButton != null)
            completeCurrentDeliveryButton.onClick.AddListener(OnCompleteCurrentDeliveryClicked);

        RefreshAssignmentTexts();
        UpdateButtonInteractable();
    }

    private void OnDestroy()
    {
        if (completeCurrentDeliveryButton != null)
            completeCurrentDeliveryButton.onClick.RemoveListener(OnCompleteCurrentDeliveryClicked);
    }

    private void OnCompleteCurrentDeliveryClicked()
    {
        int completedId = currentDeliveryID;
        currentDeliveryID++;
        OnDeliveryCompleted?.Invoke(completedId);
        RefreshAssignmentTexts();
        UpdateButtonInteractable();
    }

    private void RefreshAssignmentTexts()
    {
        SetLine(assignmentLine1, 0, assignment1);
        SetLine(assignmentLine2, 1, assignment2);
        SetLine(assignmentLine3, 2, assignment3);
    }

    private void SetLine(TMP_Text line, int slotIndex, string plainText)
    {
        if (line == null)
            return;

        line.richText = true;
        if (currentDeliveryID > slotIndex)
            line.text = $"<s>{plainText}</s>";
        else
            line.text = plainText;
    }

    private void UpdateButtonInteractable()
    {
        if (completeCurrentDeliveryButton == null)
            return;

        completeCurrentDeliveryButton.interactable = currentDeliveryID < 3;
    }
}
