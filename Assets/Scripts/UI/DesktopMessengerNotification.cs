using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// In-world desktop toast for new messenger activity (same row look as <see cref="GlobalNotificationHud"/> delivery slot).
/// Call <see cref="TriggerNotification"/> from <see cref="OllamaConnector"/> when H sends an excuse or blackmail-class reply.
/// </summary>
public class DesktopMessengerNotification : MonoBehaviour
{
    public static DesktopMessengerNotification Instance { get; private set; }

    [Header("Toast (match HUD PackageDeliveredLabel style)")]
    [SerializeField] private TextMeshProUGUI toastMessageText;
    [Tooltip("Background row (Image + TMP child). Hidden when idle.")]
    [SerializeField] private GameObject toastRowRoot;
    [SerializeField] private CanvasGroup toastCanvasGroup;
    [SerializeField] private RectTransform toastRowRect;

    [Header("Animation")]
    [SerializeField] private Animator toastAnimator;
    [SerializeField] private string animatorShowTrigger = "Show";
    [Tooltip("If no Animator is assigned, plays a short scale/alpha pop on the row.")]
    [SerializeField] private bool playDefaultPopWhenNoAnimator = true;
    [SerializeField] private float defaultPopInSeconds = 0.12f;

    [Header("Timing")]
    [SerializeField] private float displayDurationSeconds = 2.5f;

    [Header("Audio")]
    [SerializeField] private SoundManager soundManager;

    Coroutine _hideRoutine;
    Coroutine _popRoutine;

    void Awake()
    {
        if (toastRowRect == null && toastRowRoot != null)
            toastRowRect = toastRowRoot.GetComponent<RectTransform>();
        if (toastCanvasGroup == null && toastRowRoot != null)
            toastCanvasGroup = toastRowRoot.GetComponent<CanvasGroup>();

        if (toastRowRoot != null)
            toastRowRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (Instance != null && Instance != this)
            return;
        Instance = this;
    }

    void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Updates the toast copy, shows the row, plays optional Animator trigger or default pop, and plays the notification SFX.
    /// </summary>
    public void TriggerNotification(string message)
    {
        if (toastMessageText != null)
            toastMessageText.text = string.IsNullOrWhiteSpace(message) ? "New Message" : message.Trim();

        if (soundManager != null)
            soundManager.PlayNotificationSound();

        if (!gameObject.activeInHierarchy)
            return;

        if (toastRowRoot != null)
        {
            EnsureActiveHierarchy(toastRowRoot);
            toastRowRoot.SetActive(true);
        }

        if (_hideRoutine != null)
        {
            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        if (_popRoutine != null)
        {
            StopCoroutine(_popRoutine);
            _popRoutine = null;
        }

        if (toastAnimator != null && !string.IsNullOrEmpty(animatorShowTrigger))
        {
            toastAnimator.ResetTrigger(animatorShowTrigger);
            toastAnimator.SetTrigger(animatorShowTrigger);
        }
        else if (playDefaultPopWhenNoAnimator && (toastCanvasGroup != null || toastRowRect != null))
            _popRoutine = StartCoroutine(DefaultPopIn());

        _hideRoutine = StartCoroutine(HideAfter(Mathf.Max(0.25f, displayDurationSeconds)));
    }

    IEnumerator DefaultPopIn()
    {
        if (toastCanvasGroup != null)
        {
            toastCanvasGroup.alpha = 0f;
            toastCanvasGroup.interactable = false;
            toastCanvasGroup.blocksRaycasts = false;
        }

        if (toastRowRect != null)
            toastRowRect.localScale = new Vector3(0.94f, 0.94f, 1f);

        float t = 0f;
        float dur = Mathf.Max(0.02f, defaultPopInSeconds);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / dur);
            float e = 1f - (1f - u) * (1f - u);
            if (toastCanvasGroup != null)
                toastCanvasGroup.alpha = e;
            if (toastRowRect != null)
                toastRowRect.localScale = Vector3.Lerp(new Vector3(0.94f, 0.94f, 1f), Vector3.one, e);
            yield return null;
        }

        if (toastCanvasGroup != null)
            toastCanvasGroup.alpha = 1f;
        if (toastRowRect != null)
            toastRowRect.localScale = Vector3.one;
        _popRoutine = null;
    }

    IEnumerator HideAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (toastRowRoot != null)
            toastRowRoot.SetActive(false);
        _hideRoutine = null;
    }

    static void EnsureActiveHierarchy(GameObject leaf)
    {
        if (leaf == null)
            return;
        for (var tr = leaf.transform; tr != null; tr = tr.parent)
        {
            if (!tr.gameObject.activeSelf)
                tr.gameObject.SetActive(true);
        }
    }
}
