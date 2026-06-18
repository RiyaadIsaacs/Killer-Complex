using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a small on-screen control reference in the gameplay scene (WASD, jump, sprint, pause).
/// Add to any active object (e.g. player or a "GameUI" empty). Builds its own overlay canvas if none is assigned.
/// </summary>
public class MovementControlsHud : MonoBehaviour
{
    const string DefaultControlsText =
        "WASD — move\n" +
        "SPACE — jump\n" +
        "SHIFT — toggle sprint\n" +
        "ESC — pause";

    [Header("UI")]
    [Tooltip("Optional. If set, this panel is shown/hidden; otherwise UI is created at runtime.")]
    [SerializeField] private GameObject controlsPanel;

    [TextArea(2, 6)]
    [SerializeField] private string controlsText = DefaultControlsText;

    [Header("Layout")]
    [SerializeField] private Vector2 screenPadding = new(24f, 24f);
    [SerializeField] private Vector2 panelSize = new(280f, 142f);

    [Header("Behaviour")]
    [SerializeField] private bool hideWhilePaused = true;

    [SerializeField] private bool hideDuringIntro = true;

    Canvas _ownedCanvas;

    void Awake()
    {
        if (controlsPanel == null)
            BuildDefaultPanel();
    }

    void Update()
    {
        if (controlsPanel == null)
            return;

        var visible = true;
        if (hideWhilePaused && PauseScreen.IsGameplayPaused)
            visible = false;
        if (hideDuringIntro && GameSceneIntroPanel.BlocksGameplay)
            visible = false;

        if (controlsPanel.activeSelf != visible)
            controlsPanel.SetActive(visible);
    }

    void BuildDefaultPanel()
    {
        var sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        var canvasGo = new GameObject("MovementControlsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        _ownedCanvas = canvasGo.GetComponent<Canvas>();
        _ownedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _ownedCanvas.sortingOrder = 50;
        _ownedCanvas.pixelPerfect = true;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panelGo = new GameObject("MovementControlsPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        controlsPanel = panelGo;

        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(0f, 0f);
        panelRt.pivot = new Vector2(0f, 0f);
        panelRt.anchoredPosition = screenPadding;
        panelRt.sizeDelta = panelSize;

        var panelImg = panelGo.GetComponent<Image>();
        panelImg.sprite = sprite;
        panelImg.type = Image.Type.Simple;
        panelImg.color = new Color32(30, 40, 55, 220);

        var font = TMP_Settings.defaultFontAsset;
        var textGo = new GameObject("ControlsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panelGo.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(14f, 10f);
        textRt.offsetMax = new Vector2(-14f, -10f);

        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color32(195, 215, 235, 255);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.lineSpacing = 4f;
        tmp.text = string.IsNullOrWhiteSpace(controlsText) ? DefaultControlsText : controlsText;
    }
}
