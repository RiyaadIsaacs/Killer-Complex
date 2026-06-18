using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Drop-off interactable: <see cref="PlayerController"/> raycast calls <see cref="Interact"/>.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DeliveryZone : MonoBehaviour
{
    [FormerlySerializedAs("targetDeliveryID")]
    [SerializeField]
    [Tooltip("Stable id for this drop-off; must match other zones' ids uniquely. Room numbers are defined on DeliveryManager (201–208, 301–308, etc.).")]
    private int dropPointId;

    [SerializeField] private DeliveryManager deliveryManager;

    DeliveryManager _registeredManager;

    public int DropPointId => dropPointId;

    [Header("Package Delivered UI")]
    [Tooltip("Optional root GameObject (e.g. toast panel) toggled on for a few seconds after a successful drop-off.")]
    [SerializeField] private GameObject packageDeliveredNotificationRoot;
    [Tooltip("If no root is assigned, the global notification HUD row is used (see GlobalNotificationHud in scene).")]
    [SerializeField] private TextMeshProUGUI packageDeliveredLabel;
    [SerializeField, Min(0.25f)] private float notificationDisplaySeconds = 2.5f;

    [Header("Delivery knock SFX")]
    [Tooltip("Played at this door on successful drop-off. If unset, uses SoundManager Door Knock clip.")]
    [SerializeField] private AudioClip deliveryKnockClip;
    [SerializeField, Range(0f, 2f)] private float deliveryKnockVolumeScale = 1f;

    private Coroutine _hideNotificationRoutine;

    private void OnEnable()
    {
        var dm = ResolveDeliveryManager();
        if (dm == null)
            return;

        dm.RegisterDropPoint(dropPointId);
        _registeredManager = dm;
    }

    private void OnDisable()
    {
        if (_registeredManager != null)
        {
            _registeredManager.UnregisterDropPoint(dropPointId);
            _registeredManager = null;
        }

        if (_hideNotificationRoutine != null)
        {
            StopCoroutine(_hideNotificationRoutine);
            _hideNotificationRoutine = null;
        }
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

        deliveryManager = FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
        return deliveryManager;
    }

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null)
            c.isTrigger = false;
    }

    /// <summary>For <see cref="InteractPromptResolver"/> — world prompt when the player looks at this collider.</summary>
    public bool TryGetWorldInteractPrompt(RaycastHit hit, out string text, out Vector3 worldPos)
    {
        text = null;
        worldPos = hit.point + hit.normal * 0.12f;
        var col = GetComponent<Collider>();
        if (col != null)
            worldPos = col.bounds.center + Vector3.up * 0.15f;

        var dm = ResolveDeliveryManager();
        if (dm == null)
        {
            text = "[E] Door";
            return true;
        }

        if (dm.GetDeliveryDropFailureReason(dropPointId) == null)
        {
            text = "[E] Deliver package";
            return true;
        }

        if (dm.ActiveDropPointId >= 0 && dropPointId != dm.ActiveDropPointId)
            text = "[E] Wrong apartment";
        else if (dm.RequiresPhysicalPickup && !dm.HasPickedUpCurrentPackage)
            text = "[E] Get package first";
        else
            text = "[E] Door";

        return true;
    }

    public void Interact()
    {
        var dm = ResolveDeliveryManager();
        if (dm == null)
        {
            GlobalNotificationHud.ShowDeliveryFeedback("Delivery: assign Delivery Manager on this zone.", notificationDisplaySeconds);
            return;
        }

        string failReason = dm.GetDeliveryDropFailureReason(dropPointId);
        if (failReason != null)
        {
            GlobalNotificationHud.ShowDeliveryFeedback(failReason, notificationDisplaySeconds);
            return;
        }

        if (!dm.TryCompleteDeliveryAtDropPoint(dropPointId))
        {
            GlobalNotificationHud.ShowDeliveryFeedback("Could not complete delivery.", notificationDisplaySeconds);
            return;
        }

        ShowPackageDeliveredNotification();
    }

    void PlayDeliveryKnockSound()
    {
        var col = GetComponent<Collider>();
        var pos = col != null ? col.bounds.center : transform.position;
        SoundManager.TryPlayDoorKnockAt(pos, deliveryKnockClip, deliveryKnockVolumeScale);
    }

    private void ShowPackageDeliveredNotification()
    {
        const string message = "Package Delivered";

        PlayDeliveryKnockSound();

        var hudLabel = GlobalNotificationHud.FindPackageDeliveredLabel();
        GlobalNotificationHud.ShowDeliveryFeedback(message, notificationDisplaySeconds);

        if (packageDeliveredNotificationRoot != null)
        {
            packageDeliveredNotificationRoot.SetActive(true);
            var tmp = packageDeliveredNotificationRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
                tmp.text = message;
            RestartHideNotificationRoutine(packageDeliveredNotificationRoot);
        }

        if (packageDeliveredLabel != null && !ReferenceEquals(packageDeliveredLabel, hudLabel))
        {
            packageDeliveredLabel.text = message;
            var row = GlobalNotificationHud.GetPackageDeliveredRowRoot(packageDeliveredLabel);
            if (row != null)
            {
                row.SetActive(true);
                RestartHideNotificationRoutine(row);
            }
        }
    }

    private void RestartHideNotificationRoutine(GameObject toHide)
    {
        if (toHide == null)
            return;

        if (_hideNotificationRoutine != null)
            StopCoroutine(_hideNotificationRoutine);

        _hideNotificationRoutine = StartCoroutine(HideNotificationAfterDelay(toHide));
    }

    private IEnumerator HideNotificationAfterDelay(GameObject root)
    {
        yield return new WaitForSecondsRealtime(notificationDisplaySeconds);
        if (root != null)
            root.SetActive(false);
        _hideNotificationRoutine = null;
    }
}
