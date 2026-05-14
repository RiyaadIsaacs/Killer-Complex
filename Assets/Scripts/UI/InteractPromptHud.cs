using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space prompt that follows world positions (e.g. "[E] Use computer"). Multiple sources per frame call
/// <see cref="Offer"/>; the highest priority wins in <see cref="LateUpdate"/>.
/// </summary>
[DisallowMultipleComponent]
public class InteractPromptHud : MonoBehaviour
{
    struct OfferData
    {
        public string Text;
        public Vector3 World;
        public int Priority;
    }

    public static InteractPromptHud Instance { get; private set; }

    [SerializeField] private Vector2 screenOffsetPixels = new(0f, 48f);
    [SerializeField] private Color panelColor = new(0f, 0f, 0f, 0.65f);

    readonly List<OfferData> _offers = new();
    Canvas _canvas;
    RectTransform _panelRt;
    TextMeshProUGUI _label;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureUiBuilt();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void LateUpdate()
    {
        if (_offers.Count == 0)
        {
            if (_panelRt != null)
                _panelRt.gameObject.SetActive(false);
            return;
        }

        var best = _offers[0];
        for (var i = 1; i < _offers.Count; i++)
        {
            if (_offers[i].Priority > best.Priority)
                best = _offers[i];
        }

        _offers.Clear();
        Show(best);
    }

    /// <summary>Queue a prompt for this frame. Higher <paramref name="priority"/> wins over other offers.</summary>
    public void Offer(string text, Vector3 worldPosition, int priority)
    {
        if (string.IsNullOrEmpty(text))
            return;
        _offers.Add(new OfferData { Text = text, World = worldPosition, Priority = priority });
    }

    void EnsureUiBuilt()
    {
        if (_label != null)
            return;

        var canvasGo = new GameObject("InteractPromptCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        canvasGo.layer = gameObject.layer;

        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 800;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var panelGo = new GameObject("PromptPanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        _panelRt = panelGo.GetComponent<RectTransform>();
        _panelRt.sizeDelta = new Vector2(360f, 56f);
        var img = panelGo.GetComponent<Image>();
        var white = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        img.sprite = white;
        img.color = panelColor;

        var tmpGo = new GameObject("PromptLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        tmpGo.transform.SetParent(panelGo.transform, false);
        var labelRt = tmpGo.GetComponent<RectTransform>();
        StretchFull(labelRt);
        _label = tmpGo.GetComponent<TextMeshProUGUI>();
        var font = TMP_Settings.defaultFontAsset;
        if (font != null)
            _label.font = font;
        _label.fontSize = 26;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = Color.white;
        _label.text = string.Empty;

        _panelRt.gameObject.SetActive(false);
    }

    void Show(OfferData offer)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            _panelRt.gameObject.SetActive(false);
            return;
        }

        var screen = cam.WorldToScreenPoint(offer.World);
        if (screen.z <= 0f)
        {
            _panelRt.gameObject.SetActive(false);
            return;
        }

        screen.x += screenOffsetPixels.x;
        screen.y += screenOffsetPixels.y;

        var canvasRt = _canvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt,
            screen,
            null,
            out var localPoint);

        _panelRt.localPosition = localPoint;
        _label.text = offer.Text;
        _panelRt.gameObject.SetActive(true);
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(12f, 6f);
        rt.offsetMax = new Vector2(-12f, -6f);
    }
}
