using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Root for always-on screen notifications. Use <see cref="TopLeftContent"/> as the parent for new toast UIs.
/// The package-delivered line uses a <see cref="TextMeshProUGUI"/> (often on a child named <c>Text</c>) because
/// <see cref="UnityEngine.UI.Image"/> and TMP cannot sit on the same GameObject (only one <see cref="UnityEngine.UI.Graphic"/> per object).
/// </summary>
public class GlobalNotificationHud : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Parent for top-left notification widgets (vertical stack, etc.).")]
    [SerializeField] private RectTransform topLeftContent;

    [Header("Built-in slots")]
    [Tooltip("Inactive by default. Filled automatically for a child named PackageDeliveredLabel with TextMeshProUGUI, or assign manually.")]
    [SerializeField] private TextMeshProUGUI packageDeliveredLabel;

    [Header("Lifecycle")]
    [Tooltip("If true, only one HUD survives scene loads (typical for global toasts).")]
    [SerializeField] private bool persistAcrossScenes = true;

    private static GlobalNotificationHud _instance;

    Coroutine _deliveryRowHideRoutine;

    public RectTransform TopLeftContent => topLeftContent;
    public TextMeshProUGUI PackageDeliveredLabel => packageDeliveredLabel;

    /// <summary>
    /// Row GameObject to show or hide (background + text). TMP may be on a child under <c>PackageDeliveredLabel</c>.
    /// </summary>
    public static GameObject GetPackageDeliveredRowRoot(TextMeshProUGUI tmp)
    {
        if (tmp == null)
            return null;
        var p = tmp.transform.parent;
        if (p != null && p.name == "PackageDeliveredLabel")
            return p.gameObject;
        return tmp.gameObject;
    }

    private void OnValidate()
    {
        if (packageDeliveredLabel != null)
            return;
        foreach (var tr in GetComponentsInChildren<Transform>(true))
        {
            if (tr.name != "PackageDeliveredLabel")
                continue;
            var tmp = tr.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                packageDeliveredLabel = tmp;
                break;
            }
        }
    }

    private void Awake()
    {
        if (!persistAcrossScenes)
            return;

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_deliveryRowHideRoutine != null)
        {
            StopCoroutine(_deliveryRowHideRoutine);
            _deliveryRowHideRoutine = null;
        }

        if (_instance == this)
            _instance = null;
    }

    public void SetPackageDeliveredLabelVisible(bool visible)
    {
        if (packageDeliveredLabel == null)
            return;
        var root = GetPackageDeliveredRowRoot(packageDeliveredLabel);
        if (root != null)
            root.SetActive(visible);
    }

    /// <summary>
    /// Resolves the package-delivered TMP for <see cref="DeliveryZone"/> when it is not wired in the Inspector:
    /// persistent HUD instance first, then any <see cref="GlobalNotificationHud"/> in loaded scenes.
    /// </summary>
    public static TextMeshProUGUI FindPackageDeliveredLabel()
    {
        if (_instance != null && _instance.packageDeliveredLabel != null)
            return _instance.packageDeliveredLabel;

        foreach (var hud in Object.FindObjectsByType<GlobalNotificationHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hud != null && hud.packageDeliveredLabel != null)
                return hud.packageDeliveredLabel;
        }

        return null;
    }

    /// <summary>
    /// Shows a short message on the package-delivered row (top-left HUD). Falls back to <see cref="Debug.Log"/> if no label exists.
    /// Hides automatically after <paramref name="displaySeconds"/>; runs the coroutine on the resolved HUD instance.
    /// </summary>
    public static void ShowDeliveryFeedback(string message, float displaySeconds = 2.5f)
    {
        if (string.IsNullOrEmpty(message))
            return;

        GlobalNotificationHud hud = null;
        if (_instance != null && _instance.packageDeliveredLabel != null)
            hud = _instance;
        else
        {
            foreach (var h in Object.FindObjectsByType<GlobalNotificationHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (h != null && h.packageDeliveredLabel != null)
                {
                    hud = h;
                    break;
                }
            }
        }

        if (hud == null)
        {
            Debug.Log($"[Delivery] {message}");
            return;
        }

        hud.ShowTimedDeliveryRowMessage(message, displaySeconds);
    }

    void ShowTimedDeliveryRowMessage(string message, float displaySeconds)
    {
        if (packageDeliveredLabel == null)
        {
            Debug.Log($"[Delivery] {message}");
            return;
        }

        if (_deliveryRowHideRoutine != null)
        {
            StopCoroutine(_deliveryRowHideRoutine);
            _deliveryRowHideRoutine = null;
        }

        EnsureActiveHierarchy(gameObject);

        packageDeliveredLabel.text = message;
        var row = GetPackageDeliveredRowRoot(packageDeliveredLabel);
        if (row != null)
        {
            EnsureActiveHierarchy(row);
            row.SetActive(true);
        }

        _deliveryRowHideRoutine = StartCoroutine(HideDeliveryRowAfter(row, Mathf.Max(0.25f, displaySeconds)));
    }

    static void EnsureActiveHierarchy(GameObject leaf)
    {
        if (leaf == null)
            return;
        var ancestors = new List<Transform>();
        for (var t = leaf.transform; t != null; t = t.parent)
            ancestors.Add(t);
        for (var i = ancestors.Count - 1; i >= 0; i--)
        {
            if (!ancestors[i].gameObject.activeSelf)
                ancestors[i].gameObject.SetActive(true);
        }
    }

    IEnumerator HideDeliveryRowAfter(GameObject row, float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (row != null)
            row.SetActive(false);
        _deliveryRowHideRoutine = null;
    }
}
