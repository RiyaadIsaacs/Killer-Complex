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

    GameObject _pauseMenuRoot;
    Button _templateButton;
    Image _panelBackground;
    PauseScreen _pauseScreen;
    bool _built;

    public bool IsOpen { get; private set; }

    public void Initialize(GameObject pauseMenuRoot, Button menuButtonTemplate, PauseScreen pauseScreen)
    {
        _pauseMenuRoot = pauseMenuRoot;
        _pauseScreen = pauseScreen;
        _templateButton = menuButtonTemplate != null
            ? menuButtonTemplate
            : PauseMenuUiFactory.FindMenuButtonStyleSource(pauseMenuRoot != null ? pauseMenuRoot.transform : null);
        if (_pauseMenuRoot != null)
        {
            var panelTransform = _pauseMenuRoot.transform.Find("Panel");
            if (panelTransform != null)
                _panelBackground = panelTransform.GetComponent<Image>();
        }

        EnsureBuilt();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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
        EnsureBuilt();
        if (settingsPanel == null)
            return;

        IsOpen = true;
        _pauseScreen?.SetPauseMenuChromeVisible(false);
        settingsPanel.SetActive(true);
        settingsPanel.transform.SetAsLastSibling();

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.SetValueWithoutNotify(PlayerController.GetSavedSensitivityMultiplier());
            UpdateSensitivityLabel(mouseSensitivitySlider.value);
        }
    }

    public void CloseSettings()
    {
        IsOpen = false;
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (_pauseScreen != null && _pauseScreen.IsPaused)
            _pauseScreen.SetPauseMenuChromeVisible(true);
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

    void EnsureBuilt()
    {
        if (_built || settingsPanel != null)
            return;

        _built = true;
        BuildDefaultPanel();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void OnSensitivitySliderChanged(float value) => UpdateSensitivityLabel(value);

    void UpdateSensitivityLabel(float multiplier)
    {
        if (mouseSensitivityValueLabel == null)
            return;
        mouseSensitivityValueLabel.text = $"{multiplier:0.0}×";
    }

    void BuildDefaultPanel()
    {
        var whiteSprite = PauseMenuUiFactory.GetWhiteSprite();
        var panelSprite = _panelBackground != null && _panelBackground.sprite != null
            ? _panelBackground.sprite
            : whiteSprite;

        var root = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(transform, false);
        settingsPanel = root;

        var rootRt = root.GetComponent<RectTransform>();
        StretchFull(rootRt);

        var dimmer = root.GetComponent<Image>();
        dimmer.sprite = panelSprite;
        dimmer.color = _panelBackground != null
            ? _panelBackground.color
            : new Color32(0, 0, 0, 160);
        dimmer.raycastTarget = true;

        var box = new GameObject("SettingsBox", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(root.transform, false);
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(520f, 340f);
        var boxImg = box.GetComponent<Image>();
        boxImg.sprite = panelSprite;
        boxImg.color = Color.white;
        boxImg.raycastTarget = true;

        var font = GetPauseMenuFont();

        var titleGo = CreateLabel(box.transform, "Title", "Settings", 28, TextAlignmentOptions.Center);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -24f);
        titleRt.sizeDelta = new Vector2(-48f, 48f);
        titleGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var labelGo = CreateLabel(box.transform, "MouseLabel", "Mouse sensitivity", 22, TextAlignmentOptions.Left);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.58f);
        labelRt.anchorMax = new Vector2(1f, 0.58f);
        labelRt.sizeDelta = new Vector2(-64f, 32f);

        mouseSensitivitySlider = CreateSensitivitySlider(box.transform, whiteSprite);
        var sliderRt = mouseSensitivitySlider.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.08f, 0.44f);
        sliderRt.anchorMax = new Vector2(0.92f, 0.44f);
        sliderRt.sizeDelta = new Vector2(0f, 28f);
        mouseSensitivitySlider.minValue = 0.5f;
        mouseSensitivitySlider.maxValue = 3f;
        mouseSensitivitySlider.wholeNumbers = false;
        mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);

        var valueGo = CreateLabel(box.transform, "MouseValue", "1.0×", 20, TextAlignmentOptions.Center);
        mouseSensitivityValueLabel = valueGo.GetComponent<TextMeshProUGUI>();
        var valueRt = valueGo.GetComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0f, 0.34f);
        valueRt.anchorMax = new Vector2(1f, 0.34f);
        valueRt.sizeDelta = new Vector2(-64f, 28f);

        CreateStyledButton(box.transform, "ApplyButton", "Apply", new Vector2(-140f, 56f), OnApplyClicked);
        CreateStyledButton(box.transform, "BackButton", "Back", new Vector2(140f, 56f), OnBackClicked);
    }

    GameObject CreateLabel(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        var font = GetPauseMenuFont();
        if (font != null)
            tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = new Color32(50, 50, 50, 255);
        tmp.raycastTarget = false;
        return go;
    }

    void CreateStyledButton(Transform parent, string name, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        var btn = PauseMenuUiFactory.CreateTextButton(
            parent,
            name,
            label,
            pos,
            new Vector2(220f, 64f),
            _templateButton,
            onClick);

        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
    }

    Slider CreateSensitivitySlider(Transform parent, Sprite sprite)
    {
        var root = new GameObject("MouseSlider", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        StretchFull(bg.GetComponent<RectTransform>());
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = sprite;
        bgImg.color = new Color32(40, 55, 71, 220);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        StretchFull(fillArea.GetComponent<RectTransform>());

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.sprite = sprite;
        fillImg.color = new Color32(39, 174, 96, 255);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        StretchFull(handleArea.GetComponent<RectTransform>());

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20f, 0f);
        var handleImg = handle.GetComponent<Image>();
        handleImg.sprite = sprite;
        handleImg.color = Color.white;

        var slider = root.GetComponent<Slider>();
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    TMP_FontAsset GetPauseMenuFont()
    {
        if (_templateButton != null)
        {
            var tmp = _templateButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null && tmp.font != null)
                return tmp.font;
        }

        return TMP_Settings.defaultFontAsset;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
