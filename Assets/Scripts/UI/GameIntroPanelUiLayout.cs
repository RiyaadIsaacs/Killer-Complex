using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the game-intro overlay hierarchy under a screen-space canvas root.
/// </summary>
public static class GameIntroPanelUiLayout
{
    public struct BuiltUi
    {
        public GameObject PanelRoot;
        public TextMeshProUGUI TitleText;
        public TextMeshProUGUI BodyText;
        public Button ContinueButton;
    }

    public const string PanelObjectName = "GameIntroPanel";
    public const string CardObjectName = "Card";
    public const string TitleObjectName = "Title";
    public const string BodyObjectName = "Body";
    public const string ContinueButtonObjectName = "BtnContinue";

    public static bool TryBuild(Transform hudCanvasRoot, string continueButtonLabel, out BuiltUi built)
    {
        built = default;
        if (hudCanvasRoot == null)
            return false;

        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
            return false;

        var sprite = CreateWhiteSprite();

        var panelGo = new GameObject(PanelObjectName, typeof(RectTransform), typeof(CanvasRenderer));
        panelGo.transform.SetParent(hudCanvasRoot, false);
        var panelRt = panelGo.GetComponent<RectTransform>();
        StretchFull(panelRt);
        panelRt.SetAsLastSibling();

        var backdrop = panelGo.AddComponent<Image>();
        backdrop.sprite = sprite;
        backdrop.color = new Color32(0, 0, 0, 210);
        backdrop.raycastTarget = true;

        var card = new GameObject(CardObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(panelGo.transform, false);
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(760f, 460f);
        cardRt.anchoredPosition = Vector2.zero;
        var cardBg = card.GetComponent<Image>();
        cardBg.sprite = sprite;
        cardBg.color = new Color32(28, 36, 48, 245);

        var titleGo = new GameObject(TitleObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(card.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -28f);
        titleRt.sizeDelta = new Vector2(-56f, 48f);
        var titleText = titleGo.GetComponent<TextMeshProUGUI>();
        titleText.font = font;
        titleText.fontSize = 32;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color32(236, 240, 244, 255);
        titleText.text = GameSceneIntroPanel.DefaultTitle;

        var bodyGo = new GameObject(BodyObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        bodyGo.transform.SetParent(card.transform, false);
        var bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(32f, 88f);
        bodyRt.offsetMax = new Vector2(-32f, -80f);
        var bodyText = bodyGo.GetComponent<TextMeshProUGUI>();
        bodyText.font = font;
        bodyText.fontSize = 20;
        bodyText.alignment = TextAlignmentOptions.TopLeft;
        bodyText.color = new Color32(210, 218, 226, 255);
        bodyText.enableWordWrapping = true;
        bodyText.lineSpacing = 4f;
        bodyText.text = GameSceneIntroPanel.DefaultBody;

        var btnGo = new GameObject(ContinueButtonObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(card.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 24f);
        btnRt.sizeDelta = new Vector2(240f, 44f);
        var btnImg = btnGo.GetComponent<Image>();
        btnImg.sprite = sprite;
        btnImg.color = new Color32(52, 73, 94, 255);
        var continueButton = btnGo.GetComponent<Button>();
        continueButton.targetGraphic = btnImg;

        var btnLabelGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        btnLabelGo.transform.SetParent(btnGo.transform, false);
        StretchFull(btnLabelGo.GetComponent<RectTransform>());
        var btnLabel = btnLabelGo.GetComponent<TextMeshProUGUI>();
        btnLabel.font = font;
        btnLabel.fontSize = 18;
        btnLabel.fontStyle = FontStyles.Bold;
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.color = Color.white;
        btnLabel.text = continueButtonLabel;

        panelGo.SetActive(false);

        built = new BuiltUi
        {
            PanelRoot = panelGo,
            TitleText = titleText,
            BodyText = bodyText,
            ContinueButton = continueButton
        };
        return true;
    }

    public static bool TryFindExisting(Transform hudCanvasRoot, out BuiltUi built)
    {
        built = default;
        if (hudCanvasRoot == null)
            return false;

        var panel = hudCanvasRoot.Find(PanelObjectName);
        if (panel == null)
            return false;

        var card = panel.Find(CardObjectName);
        if (card == null)
            return false;

        built.PanelRoot = panel.gameObject;
        built.TitleText = card.Find(TitleObjectName)?.GetComponent<TextMeshProUGUI>();
        built.BodyText = card.Find(BodyObjectName)?.GetComponent<TextMeshProUGUI>();
        built.ContinueButton = card.Find(ContinueButtonObjectName)?.GetComponent<Button>();
        return built.PanelRoot != null && built.TitleText != null && built.BodyText != null && built.ContinueButton != null;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        for (var y = 0; y < 4; y++)
        for (var x = 0; x < 4; x++)
            tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
    }
}
