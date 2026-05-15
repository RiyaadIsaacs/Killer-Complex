using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// H suspicion meter on <c>PanelMessenger</c> — reads <see cref="OllamaConnector.SuspicionPercent"/>.
/// </summary>
public class MessengerSuspicionBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text percentText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private OllamaConnector ollamaConnector;

    static readonly Color32 ColorLow = new(52, 152, 219, 255);
    static readonly Color32 ColorMid = new(241, 196, 15, 255);
    static readonly Color32 ColorHigh = new(192, 57, 43, 255);

    OllamaConnector _resolvedOllama;
    float _lastDisplayed = -1f;

    void Awake()
    {
        if (slider == null)
            return;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
        slider.interactable = false;
    }

    void OnEnable() => Refresh(true);

    void Update()
    {
        var oc = ResolveOllama();
        float current = oc != null ? oc.SuspicionPercent : 0f;
        if (Mathf.Approximately(current, _lastDisplayed))
            return;
        Refresh(true);
    }

    public void Refresh(bool force = false)
    {
        var oc = ResolveOllama();
        float percent = oc != null ? Mathf.Clamp(oc.SuspicionPercent, 0f, 100f) : 0f;
        if (!force && Mathf.Approximately(percent, _lastDisplayed))
            return;

        _lastDisplayed = percent;
        int display = Mathf.RoundToInt(percent);

        if (slider != null)
            slider.SetValueWithoutNotify(percent);

        if (percentText != null)
            percentText.text = $"{display}%";

        if (statusText != null)
            statusText.text = GetStatusLabel(display);

        if (fillImage != null)
            fillImage.color = GetColorForSuspicion(percent);
    }

    static string GetStatusLabel(int suspicion)
    {
        if (suspicion >= 100)
            return "Breaking";
        if (suspicion >= 80)
            return "Lethal";
        if (suspicion >= 55)
            return "Hostile";
        if (suspicion >= 35)
            return "Wary";
        if (suspicion <= 10)
            return "Chilled";
        return "Uneasy";
    }

    static Color GetColorForSuspicion(float suspicion)
    {
        if (suspicion >= 80f)
            return ColorHigh;
        if (suspicion >= 40f)
            return Color.Lerp(ColorMid, ColorHigh, (suspicion - 40f) / 40f);
        return Color.Lerp(ColorLow, ColorMid, suspicion / 40f);
    }

    OllamaConnector ResolveOllama()
    {
        if (ollamaConnector != null)
            return ollamaConnector;
        if (_resolvedOllama == null)
            _resolvedOllama = FindFirstObjectByType<OllamaConnector>(FindObjectsInactive.Include);
        return _resolvedOllama;
    }
}
