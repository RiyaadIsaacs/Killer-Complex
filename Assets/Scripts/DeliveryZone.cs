using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// World drop-off: requires a <b>trigger</b> collider. When the object tagged <c>Player</c> stands inside,
/// pressing <b>E</b> completes the active delivery only if this zone's <see cref="dropPointId"/> matches
/// <see cref="DeliveryManager.ActiveDropPointId"/> (a <b>random</b> id chosen for the current leg — not a fixed room order).
/// Use <c>dropPointId</c> values <c>0</c>–<c>6</c> to match apartment rooms <c>201</c>–<c>208</c> (see <see cref="DeliveryManager.TryGetApartmentRoomForDropPoint"/>).
/// When the manager has a reception <see cref="DeliveryItem"/>, the player must <b>Interact</b> with that item first (pickup).
/// </summary>
[RequireComponent(typeof(Collider))]
public class DeliveryZone : MonoBehaviour
{
    [FormerlySerializedAs("targetDeliveryID")]
    [SerializeField]
    [Tooltip("Stable id for this drop-off; must match other zones' ids uniquely. Project map: 0→201 … 6→208 (see DeliveryManager).")]
    private int dropPointId;

    [SerializeField] private DeliveryManager deliveryManager;

    [Header("Package Delivered UI")]
    [Tooltip("Optional root GameObject (e.g. toast panel) toggled on for a few seconds after a successful drop-off.")]
    [SerializeField] private GameObject packageDeliveredNotificationRoot;
    [Tooltip("If no root is assigned, this TextMeshProUGUI is shown with the message. Leave empty when your DeliveryManager sits on the same GameObject as GlobalNotificationHUD — the HUD label is used automatically.")]
    [SerializeField] private TextMeshProUGUI packageDeliveredLabel;
    [SerializeField, Min(0.25f)] private float notificationDisplaySeconds = 2.5f;

    private bool _playerInside;
    private Coroutine _hideNotificationRoutine;

    private void OnEnable()
    {
        if (deliveryManager != null)
            deliveryManager.RegisterDropPoint(dropPointId);
    }

    private void OnDisable()
    {
        if (deliveryManager != null)
            deliveryManager.UnregisterDropPoint(dropPointId);

        _playerInside = false;
        if (_hideNotificationRoutine != null)
        {
            StopCoroutine(_hideNotificationRoutine);
            _hideNotificationRoutine = null;
        }
    }

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null)
            c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        _playerInside = false;
    }

    private void Update()
    {
        if (!_playerInside || deliveryManager == null)
            return;

        if (deliveryManager.CanInteractDropPoint(dropPointId))
        {
            var hud = InteractPromptHud.Instance;
            if (hud != null)
            {
                var c = GetComponent<Collider>();
                var pos = c != null ? c.bounds.center : transform.position + Vector3.up * 0.5f;
                hud.Offer("[E] Deliver package", pos, 5);
            }
        }

        if (Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        if (!deliveryManager.TryCompleteDeliveryAtDropPoint(dropPointId))
            return;

        ShowPackageDeliveredNotification();
    }

    private void ShowPackageDeliveredNotification()
    {
        const string message = "Package Delivered";

        if (packageDeliveredNotificationRoot != null)
        {
            packageDeliveredNotificationRoot.SetActive(true);
            var tmp = packageDeliveredNotificationRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
                tmp.text = message;
            RestartHideNotificationRoutine(packageDeliveredNotificationRoot);
            return;
        }

        var label = ResolvePackageDeliveredLabel();
        if (label != null)
        {
            label.text = message;
            var row = GlobalNotificationHud.GetPackageDeliveredRowRoot(label);
            row.SetActive(true);
            RestartHideNotificationRoutine(row);
        }
    }

    /// <summary>
    /// Explicit Inspector reference, or the default slot on <see cref="GlobalNotificationHud"/> when it shares a root with <see cref="deliveryManager"/>.
    /// </summary>
    private TextMeshProUGUI ResolvePackageDeliveredLabel()
    {
        if (packageDeliveredLabel != null)
            return packageDeliveredLabel;
        if (deliveryManager == null)
            return null;
        var hud = deliveryManager.GetComponent<GlobalNotificationHud>();
        return hud != null ? hud.PackageDeliveredLabel : null;
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
