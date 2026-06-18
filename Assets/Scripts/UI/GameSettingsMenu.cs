using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game settings overlay (opened from <see cref="PauseScreen"/>). Persists mouse sensitivity via <see cref="PlayerController"/>.
/// </summary>
public class GameSettingsMenu : MonoBehaviour
{
    public static GameSettingsMenu Instance { get; private set; }

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TMP_Text mouseSensitivityValueLabel;
    [SerializeField] private PlayerController player;

    bool _isOpen;

    public bool IsOpen => _isOpen;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (settingsPanel == null)
            BuildDefaultPanel();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OpenSettings()
    {
        if (settingsPanel == null)
            return;

        _isOpen = true;
        settingsPanel.SetActive(true);

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.SetValueWithoutNotify(PlayerController.GetSavedSensitivityMultiplier());
            UpdateSensitivityLabel(mouseSensitivitySlider.value);
        }
    }

    public void CloseSettings()
    {
        _isOpen = false;
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnApplyClicked()
    {
        if (mouseSensitivitySlider != null)
            PlayerController.SaveSensitivityMultiplier(mouseSensitivitySlider.value);

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        player?.ApplySavedMouseSensitivity();

        CloseSettings();
    }

    public void OnBackClicked() => CloseSettings();

    void OnSensitivitySliderChanged(float value) => UpdateSensitivityLabel(value);

    void UpdateSensitivityLabel(float multiplier)
    {
        if (mouseSensitivityValueLabel == null)
            return;
        mouseSensitivityValueLabel.text = $"{multiplier:0.0}×";
    }

    void BuildDefaultPanel()
    {
        var sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        var root = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        settingsPanel = root;

        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var dimmer = root.GetComponent<Image>();
        dimmer.sprite = sprite;
        dimmer.color = new Color32(0, 0, 0, 160);

        var box = new GameObject("SettingsBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(root.transform, false);
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(420f, 280f);
        var boxImg = box.GetComponent<Image>();
        boxImg.sprite = sprite;
        boxImg.color = new Color32(40, 52, 68, 250);

        var font = TMP_Settings.defaultFontAsset;

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(box.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -20f);
        titleRt.sizeDelta = new Vector2(-40f, 40f);
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        if (font != null) titleTmp.font = font;
        titleTmp.text = "Settings";
        titleTmp.fontSize = 28;
        titleTmp.alignment = TextAlignmentOptions.Center;

        var labelGo = new GameObject("MouseLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(box.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.55f);
        labelRt.anchorMax = new Vector2(1f, 0.55f);
        labelRt.sizeDelta = new Vector2(-48f, 32f);
        var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
        if (font != null) labelTmp.font = font;
        labelTmp.text = "Mouse sensitivity";
        labelTmp.fontSize = 20;
        labelTmp.alignment = TextAlignmentOptions.Left;

        var sliderGo = new GameObject("MouseSlider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(box.transform, false);
        var sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.1f, 0.42f);
        sliderRt.anchorMax = new Vector2(0.9f, 0.42f);
        sliderRt.sizeDelta = new Vector2(0f, 24f);
        mouseSensitivitySlider = sliderGo.GetComponent<Slider>();
        mouseSensitivitySlider.minValue = 0.5f;
        mouseSensitivitySlider.maxValue = 3f;
        mouseSensitivitySlider.wholeNumbers = false;
        mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);

        var valueGo = new GameObject("MouseValue", typeof(RectTransform), typeof(TextMeshProUGUI));
        valueGo.transform.SetParent(box.transform, false);
        var valueRt = valueGo.GetComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0f, 0.32f);
        valueRt.anchorMax = new Vector2(1f, 0.32f);
        valueRt.sizeDelta = new Vector2(-48f, 28f);
        mouseSensitivityValueLabel = valueGo.GetComponent<TextMeshProUGUI>();
        if (font != null) mouseSensitivityValueLabel.font = font;
        mouseSensitivityValueLabel.fontSize = 18;
        mouseSensitivityValueLabel.alignment = TextAlignmentOptions.Center;

        CreateButton(box.transform, "ApplyButton", "Apply", new Vector2(-110f, 40f), OnApplyClicked);
        CreateButton(box.transform, "BackButton", "Back", new Vector2(110f, 40f), OnBackClicked);
    }

    void CreateButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(160f, 44f);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = new Color32(70, 95, 120, 255);
        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        var font = TMP_Settings.defaultFontAsset;
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
