using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game settings overlay (opened from <see cref="PauseScreen"/>). Persists mouse sensitivity and SFX volume.
/// Assign <see cref="settingsPanel"/> in the scene (or run <b>Killer Complex → UI → Rebuild Pause Settings Panel</b>) to edit layout in the Editor.
/// </summary>
public class GameSettingsMenu : MonoBehaviour
{
    public static GameSettingsMenu Instance { get; private set; }

    static readonly Color32 PanelBoxColor = new(38, 48, 62, 250);
    static readonly Color32 TitleTextColor = new(255, 255, 255, 255);
    static readonly Color32 LabelTextColor = new(236, 242, 248, 255);
    static readonly Color32 ValueTextColor = new(200, 220, 235, 255);

    const float SettingsBoxHeight = 500f;
    const float HorizontalInset = 32f;
    const float ButtonRowBottom = 24f;
    const float ButtonHeight = 52f;
    const float RowGap = 20f;
    const float SliderHeight = 32f;
    const float LabelHeight = 28f;
    const float ValueHeight = 24f;

    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private TMP_Text mouseSensitivityValueLabel;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxVolumeValueLabel;
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

        TryResolveReferencesFromPanel();
        WirePanelControls();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying)
            TryResolveReferencesFromPanel();
    }
#endif

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

        WirePanelControls();

        if (mouseSensitivitySlider != null)
        {
            ConfigureSensitivitySlider(mouseSensitivitySlider);
            mouseSensitivitySlider.SetValueWithoutNotify(PlayerController.GetSavedSensitivityMultiplier());
            UpdateSensitivityLabel(mouseSensitivitySlider.value);
        }

        if (sfxVolumeSlider != null)
        {
            ConfigureSfxVolumeSlider(sfxVolumeSlider);
            sfxVolumeSlider.SetValueWithoutNotify(SoundManager.GetSavedSfxVolume());
            UpdateSfxVolumeLabel(sfxVolumeSlider.value);
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

        if (sfxVolumeSlider != null)
            SoundManager.SaveSfxVolume(sfxVolumeSlider.value);

        SoundManager.ApplySavedSfxVolume();

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
        player?.ApplySavedMouseSensitivity();

        CloseSettings();
    }

    public void OnBackClicked() => CloseSettings();

#if UNITY_EDITOR
    /// <summary>Destroys any existing panel and rebuilds it as scene children so layout can be edited in the Inspector.</summary>
    public void EditorRebuildPanel()
    {
        DestroyExistingPanel();
        _built = true;
        BuildDefaultPanel();
        WirePanelControls();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    void EnsureBuilt()
    {
        if (_built)
            return;

        if (TryResolveReferencesFromPanel())
        {
            _built = true;
            return;
        }

        if (settingsPanel != null)
        {
            _built = true;
            return;
        }

        _built = true;
        BuildDefaultPanel();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    bool TryResolveReferencesFromPanel()
    {
        if (settingsPanel == null)
        {
            var panelTransform = transform.Find("SettingsPanel");
            if (panelTransform != null)
                settingsPanel = panelTransform.gameObject;
        }

        if (settingsPanel == null)
            return false;

        var box = settingsPanel.transform.Find("SettingsBox");
        if (box == null)
            return false;

        mouseSensitivitySlider ??= box.Find("MouseSlider")?.GetComponent<Slider>();
        mouseSensitivityValueLabel ??= box.Find("MouseValue")?.GetComponent<TMP_Text>();
        sfxVolumeSlider ??= box.Find("SfxSlider")?.GetComponent<Slider>();
        sfxVolumeValueLabel ??= box.Find("SfxValue")?.GetComponent<TMP_Text>();

        return mouseSensitivitySlider != null && sfxVolumeSlider != null;
    }

    void WirePanelControls()
    {
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.onValueChanged.RemoveListener(OnSensitivitySliderChanged);
            mouseSensitivitySlider.onValueChanged.AddListener(OnSensitivitySliderChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeSliderChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeSliderChanged);
        }

        if (settingsPanel == null)
            return;

        var box = settingsPanel.transform.Find("SettingsBox");
        if (box == null)
            return;

        WireButton(box.Find("ApplyButton")?.GetComponent<Button>(), OnApplyClicked);
        WireButton(box.Find("BackButton")?.GetComponent<Button>(), OnBackClicked);
    }

    static void WireButton(Button button, UnityEngine.Events.UnityAction onClick)
    {
        if (button == null || onClick == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }

    void DestroyExistingPanel()
    {
        var existing = transform.Find("SettingsPanel");
        if (existing != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(existing.gameObject);
            else
#endif
                Destroy(existing.gameObject);
        }

        settingsPanel = null;
        mouseSensitivitySlider = null;
        mouseSensitivityValueLabel = null;
        sfxVolumeSlider = null;
        sfxVolumeValueLabel = null;
        _built = false;
    }

    void OnSensitivitySliderChanged(float value) => UpdateSensitivityLabel(value);

    void OnSfxVolumeSliderChanged(float value) => UpdateSfxVolumeLabel(value);

    static void ConfigureSensitivitySlider(Slider slider)
    {
        slider.minValue = PlayerController.MinSensitivityMultiplier;
        slider.maxValue = PlayerController.MaxSensitivityMultiplier;
    }

    void UpdateSensitivityLabel(float multiplier)
    {
        if (mouseSensitivityValueLabel == null)
            return;
        mouseSensitivityValueLabel.text = $"{multiplier:0.0}×";
    }

    static void ConfigureSfxVolumeSlider(Slider slider)
    {
        slider.minValue = SoundManager.MinSfxVolume;
        slider.maxValue = SoundManager.MaxSfxVolume;
    }

    void UpdateSfxVolumeLabel(float volume)
    {
        if (sfxVolumeValueLabel == null)
            return;
        sfxVolumeValueLabel.text = $"{Mathf.RoundToInt(volume * 100f)}%";
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
        boxRt.sizeDelta = new Vector2(520f, SettingsBoxHeight);
        var boxImg = box.GetComponent<Image>();
        boxImg.sprite = panelSprite;
        boxImg.color = PanelBoxColor;
        boxImg.raycastTarget = true;

        var titleGo = CreateLabel(box.transform, "Title", "Settings", 28, TextAlignmentOptions.Center, TitleTextColor);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -24f);
        titleRt.sizeDelta = new Vector2(-48f, 48f);
        titleGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var buttonRowTop = ButtonRowBottom + ButtonHeight;
        var sfxValueBottom = buttonRowTop + RowGap;
        var sfxSliderBottom = sfxValueBottom + ValueHeight + 8f;
        var sfxLabelBottom = sfxSliderBottom + SliderHeight + 8f;
        var mouseValueBottom = sfxLabelBottom + LabelHeight + RowGap;
        var mouseSliderBottom = mouseValueBottom + ValueHeight + 8f;
        var mouseLabelBottom = mouseSliderBottom + SliderHeight + 8f;

        CreateStyledButton(box.transform, "ApplyButton", "Apply", new Vector2(-124f, ButtonRowBottom), OnApplyClicked);
        CreateStyledButton(box.transform, "BackButton", "Back", new Vector2(124f, ButtonRowBottom), OnBackClicked);

        PlaceBottomRow(box.transform, CreateLabel(box.transform, "MouseLabel", "Mouse sensitivity", 22, TextAlignmentOptions.Left, LabelTextColor), mouseLabelBottom, LabelHeight);
        mouseSensitivitySlider = CreateSettingsSlider(box.transform, whiteSprite, "MouseSlider");
        PlaceBottomRow(box.transform, mouseSensitivitySlider.GetComponent<RectTransform>(), mouseSliderBottom, SliderHeight);
        ConfigureSensitivitySlider(mouseSensitivitySlider);
        mouseSensitivitySlider.wholeNumbers = false;
        mouseSensitivitySlider.value = PlayerController.DefaultSensitivityMultiplier;

        var valueGo = CreateLabel(box.transform, "MouseValue", "1.0×", 20, TextAlignmentOptions.Center, ValueTextColor);
        mouseSensitivityValueLabel = valueGo.GetComponent<TextMeshProUGUI>();
        PlaceBottomRow(box.transform, valueGo, mouseValueBottom, ValueHeight);

        PlaceBottomRow(box.transform, CreateLabel(box.transform, "SfxLabel", "Sound effects volume", 22, TextAlignmentOptions.Left, LabelTextColor), sfxLabelBottom, LabelHeight);
        sfxVolumeSlider = CreateSettingsSlider(box.transform, whiteSprite, "SfxSlider");
        PlaceBottomRow(box.transform, sfxVolumeSlider.GetComponent<RectTransform>(), sfxSliderBottom, SliderHeight);
        ConfigureSfxVolumeSlider(sfxVolumeSlider);
        sfxVolumeSlider.wholeNumbers = false;
        sfxVolumeSlider.value = SoundManager.DefaultSfxVolume;

        var sfxValueGo = CreateLabel(box.transform, "SfxValue", "100%", 20, TextAlignmentOptions.Center, ValueTextColor);
        sfxVolumeValueLabel = sfxValueGo.GetComponent<TextMeshProUGUI>();
        PlaceBottomRow(box.transform, sfxValueGo, sfxValueBottom, ValueHeight);

        WirePanelControls();
    }

    static void PlaceBottomRow(Transform parent, RectTransform rt, float bottom, float height)
    {
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottom);
        rt.sizeDelta = new Vector2(-HorizontalInset * 2f, height);
    }

    static void PlaceBottomRow(Transform parent, GameObject go, float bottom, float height) =>
        PlaceBottomRow(parent, go.GetComponent<RectTransform>(), bottom, height);

    GameObject CreateLabel(Transform parent, string name, string text, int fontSize, TextAlignmentOptions alignment, Color32 color)
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
        tmp.color = color;
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
            new Vector2(200f, ButtonHeight),
            _templateButton,
            onClick);

        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
    }

    Slider CreateSettingsSlider(Transform parent, Sprite sprite, string name)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        StretchFull(bg.GetComponent<RectTransform>());
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = sprite;
        bgImg.color = new Color32(24, 32, 42, 255);

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
        handleRt.sizeDelta = new Vector2(24f, 0f);
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
