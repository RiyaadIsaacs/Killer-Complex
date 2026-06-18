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

    [Tooltip("Inactive by default. Shown while an urgent delivery leg is counting down. Auto-resolves a child named UrgentDeliveryTimerLabel.")]
    [SerializeField] private TextMeshProUGUI urgentDeliveryTimerLabel;

    [Header("Lifecycle")]
    [Tooltip("If true, only one HUD survives scene loads (typical for global toasts).")]
    [SerializeField] private bool persistAcrossScenes = true;

    [Header("Center delivery banner")]
    [SerializeField] private bool useCenterScreenDeliveryBanner = true;
    [SerializeField] private float centerBannerFontSize = 36f;
    [SerializeField] private float centerBannerDisplaySeconds = 3f;
    [SerializeField] private float destinationBannerDisplaySeconds = 4.5f;

    private static GlobalNotificationHud _instance;

    Coroutine _deliveryRowHideRoutine;
    Coroutine _centerBannerHideRoutine;
    GameObject _centerBannerRoot;
    TextMeshProUGUI _centerBannerLabel;

    public RectTransform TopLeftContent => topLeftContent;
    public TextMeshProUGUI PackageDeliveredLabel => packageDeliveredLabel;
    public TextMeshProUGUI UrgentDeliveryTimerLabel => urgentDeliveryTimerLabel;

    /// <summary>
    /// Row GameObject to show or hide (background + text). TMP may be on a child under <c>PackageDeliveredLabel</c>.
    /// </summary>
    public static GameObject GetPackageDeliveredRowRoot(TextMeshProUGUI tmp) =>
        GetNotificationRowRoot(tmp, "PackageDeliveredLabel");

    public static GameObject GetUrgentDeliveryTimerRowRoot(TextMeshProUGUI tmp) =>
        GetNotificationRowRoot(tmp, "UrgentDeliveryTimerLabel");

    static GameObject GetNotificationRowRoot(TextMeshProUGUI tmp, string rowName)
    {
        if (tmp == null)
            return null;
        var p = tmp.transform.parent;
        if (p != null && p.name == rowName)
            return p.gameObject;
        return tmp.gameObject;
    }

    private void OnValidate()
    {
        TryAutoAssignLabel(ref packageDeliveredLabel, "PackageDeliveredLabel");
        TryAutoAssignLabel(ref urgentDeliveryTimerLabel, "UrgentDeliveryTimerLabel");
    }

    void TryAutoAssignLabel(ref TextMeshProUGUI field, string rowName)
    {
        if (field != null)
            return;

        foreach (var tr in GetComponentsInChildren<Transform>(true))
        {
            if (tr.name != rowName)
                continue;
            var tmp = tr.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                field = tmp;
                break;
            }
        }
    }

    /// <summary>Runtime fallback when the urgent timer row exists but was not wired in the Inspector.</summary>
    public void TryAutoAssignUrgentTimerLabel() => TryAutoAssignLabel(ref urgentDeliveryTimerLabel, "UrgentDeliveryTimerLabel");

    private void Awake()
    {
        if (GetComponent<GameSceneIntroPanel>() == null)
            gameObject.AddComponent<GameSceneIntroPanel>();

        if (GetComponent<DeliveryObjectiveHud>() == null)
            gameObject.AddComponent<DeliveryObjectiveHud>();

        var hudRect = transform as RectTransform;
        if (hudRect != null && hudRect.localScale == Vector3.zero)
            hudRect.localScale = Vector3.one;

        if (!persistAcrossScenes)
            return;

        if (_instance != null && _instance != this)
        {
            _instance.MergeSceneInstanceReferences(this);
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureRootActiveForSession();
    }

    public void EnsureRootActiveForSession()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        var hudRect = transform as RectTransform;
        if (hudRect != null && hudRect.localScale == Vector3.zero)
            hudRect.localScale = Vector3.one;
    }

    /// <summary>Clears timed delivery toasts/banners so a prior run cannot flash after reload.</summary>
    public void ResetTransientNotificationsForSession()
    {
        if (_deliveryRowHideRoutine != null)
        {
            StopCoroutine(_deliveryRowHideRoutine);
            _deliveryRowHideRoutine = null;
        }

        if (_centerBannerHideRoutine != null)
        {
            StopCoroutine(_centerBannerHideRoutine);
            _centerBannerHideRoutine = null;
        }

        SetPackageDeliveredLabelVisible(false);
        HideUrgentDeliveryTimer();

        if (_centerBannerRoot != null)
            _centerBannerRoot.SetActive(false);

        if (packageDeliveredLabel != null)
            packageDeliveredLabel.text = string.Empty;
    }

    public static void ResetTransientNotificationsForSessionOnHud() =>
        FindHud()?.ResetTransientNotificationsForSession();

    /// <summary>
    /// Copies scene-only references from a duplicate HUD instance before it is destroyed on reload.
    /// </summary>
    internal void MergeSceneInstanceReferences(GlobalNotificationHud sceneHud)
    {
        if (sceneHud == null || sceneHud == this)
            return;

        GetComponent<DeliveryManager>()?.CopySceneBindingsFrom(sceneHud.GetComponent<DeliveryManager>());
        EnsureRootActiveForSession();
    }

    private void OnDestroy()
    {
        if (_deliveryRowHideRoutine != null)
        {
            StopCoroutine(_deliveryRowHideRoutine);
            _deliveryRowHideRoutine = null;
        }

        if (_centerBannerHideRoutine != null)
        {
            StopCoroutine(_centerBannerHideRoutine);
            _centerBannerHideRoutine = null;
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
    public static TextMeshProUGUI FindPackageDeliveredLabel() =>
        FindHudWithLabel(h => h.packageDeliveredLabel);

    public static TextMeshProUGUI FindUrgentDeliveryTimerLabel() =>
        FindHudWithLabel(h => h.urgentDeliveryTimerLabel);

    static TextMeshProUGUI FindHudWithLabel(System.Func<GlobalNotificationHud, TextMeshProUGUI> selector)
    {
        if (_instance != null)
        {
            var label = selector(_instance);
            if (label != null)
                return label;
        }

        foreach (var hud in Object.FindObjectsByType<GlobalNotificationHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hud == null)
                continue;
            var label = selector(hud);
            if (label != null)
                return label;
        }

        return null;
    }

    /// <summary>Resolves the persistent HUD instance, or any loaded <see cref="GlobalNotificationHud"/>.</summary>
    public static GlobalNotificationHud FindHud()
    {
        if (_instance != null)
            return _instance;

        foreach (var hud in Object.FindObjectsByType<GlobalNotificationHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hud != null)
                return hud;
        }

        return null;
    }

    /// <summary>Persistent <see cref="DeliveryManager"/> on the DontDestroyOnLoad HUD (if present).</summary>
    public static DeliveryManager FindDeliveryManager()
    {
        var hud = FindHud();
        return hud != null ? hud.GetComponent<DeliveryManager>() : null;
    }

    public void SetUrgentDeliveryTimerVisible(bool visible)
    {
        if (urgentDeliveryTimerLabel == null)
            return;

        var row = GetUrgentDeliveryTimerRowRoot(urgentDeliveryTimerLabel);
        if (row == null)
            return;

        if (visible)
            EnsureActiveHierarchy(gameObject);

        row.SetActive(visible);
    }

    /// <summary>Updates the urgent timer row and shows it. No-op if the label is not assigned.</summary>
    public void ShowUrgentDeliveryTimer(string message)
    {
        if (urgentDeliveryTimerLabel == null || string.IsNullOrEmpty(message))
            return;

        EnsureActiveHierarchy(gameObject);

        urgentDeliveryTimerLabel.text = message;
        var row = GetUrgentDeliveryTimerRowRoot(urgentDeliveryTimerLabel);
        if (row != null)
        {
            EnsureActiveHierarchy(row);
            row.SetActive(true);
        }
    }

    public void HideUrgentDeliveryTimer() => SetUrgentDeliveryTimerVisible(false);

    public static void ShowUrgentDeliveryTimerOnHud(string message)
    {
        var hud = FindHud();
        if (hud == null)
            return;
        hud.ShowUrgentDeliveryTimer(message);
    }

    public static void HideUrgentDeliveryTimerOnHud()
    {
        var hud = FindHud();
        hud?.HideUrgentDeliveryTimer();
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
        if (hud.useCenterScreenDeliveryBanner)
            hud.ShowCenterDeliveryBanner(message, hud.centerBannerDisplaySeconds);
    }

    /// <summary>
    /// Large center-screen toast when the player leaves the computer with an active delivery destination.
    /// </summary>
    public static void ShowDeliveryDestinationAnnouncement(int destinationRoom)
    {
        if (destinationRoom < 0)
            return;

        var hud = FindHud();
        if (hud == null)
        {
            Debug.Log($"[Delivery] Deliver to Room {destinationRoom}");
            return;
        }

        hud.ShowCenterDeliveryBanner($"Deliver to Room {destinationRoom}", hud.destinationBannerDisplaySeconds);
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

        EnsureRootActiveForSession();
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

    void ShowCenterDeliveryBanner(string message, float displaySeconds)
    {
        EnsureCenterBannerBuilt();
        if (_centerBannerRoot == null || _centerBannerLabel == null)
            return;

        if (_centerBannerHideRoutine != null)
        {
            StopCoroutine(_centerBannerHideRoutine);
            _centerBannerHideRoutine = null;
        }

        EnsureActiveHierarchy(gameObject);
        _centerBannerLabel.text = message;
        _centerBannerRoot.SetActive(true);
        _centerBannerHideRoutine = StartCoroutine(HideCenterBannerAfter(Mathf.Max(0.5f, displaySeconds)));
    }

    void EnsureCenterBannerBuilt()
    {
        if (_centerBannerRoot != null)
            return;

        var sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        _centerBannerRoot = new GameObject("CenterDeliveryBanner", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        _centerBannerRoot.transform.SetParent(transform, false);

        var rt = _centerBannerRoot.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(760f, 140f);

        var img = _centerBannerRoot.GetComponent<UnityEngine.UI.Image>();
        img.sprite = sprite;
        img.color = new Color32(25, 35, 48, 235);

        var font = TMP_Settings.defaultFontAsset;
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(_centerBannerRoot.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(20f, 12f);
        textRt.offsetMax = new Vector2(-20f, -12f);

        _centerBannerLabel = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            _centerBannerLabel.font = font;
        _centerBannerLabel.fontSize = centerBannerFontSize;
        _centerBannerLabel.fontStyle = FontStyles.Bold;
        _centerBannerLabel.alignment = TextAlignmentOptions.Center;
        _centerBannerLabel.color = new Color32(230, 245, 255, 255);
        _centerBannerLabel.enableWordWrapping = true;
        _centerBannerRoot.SetActive(false);
    }

    IEnumerator HideCenterBannerAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (_centerBannerRoot != null)
            _centerBannerRoot.SetActive(false);
        _centerBannerHideRoutine = null;
    }

    static void EnsureActiveHierarchy(GameObject leaf) => EnsureActiveHierarchyStatic(leaf);

    internal static void EnsureActiveHierarchyStatic(GameObject leaf)
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
