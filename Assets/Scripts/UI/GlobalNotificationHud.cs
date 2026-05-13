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
}
