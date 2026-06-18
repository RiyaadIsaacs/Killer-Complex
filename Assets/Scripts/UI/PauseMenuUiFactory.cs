using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Builds pause-menu-styled UI controls. Scene pause buttons use image sprites with baked labels,
/// so dynamic buttons use a plain generated sprite plus TMP text.
/// </summary>
public static class PauseMenuUiFactory
{
    static Sprite _whiteSprite;

    public static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        _whiteSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return _whiteSprite;
    }

    public static Button FindMenuButtonStyleSource(Transform pauseMenuRoot)
    {
        if (pauseMenuRoot == null)
            return null;

        var restart = pauseMenuRoot.Find("Restart");
        if (restart != null)
        {
            var restartButton = restart.GetComponent<Button>();
            if (restartButton != null)
                return restartButton;
        }

        foreach (Transform child in pauseMenuRoot)
        {
            if (child.name is "GameSettingsMenu" or "Settings" or "Panel")
                continue;

            var btn = child.GetComponent<Button>();
            if (btn != null)
                return btn;
        }

        return pauseMenuRoot.GetComponentInChildren<Button>(true);
    }

    public static Button CreateTextButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        Button styleSource,
        UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.sprite = GetWhiteSprite();
        img.type = Image.Type.Simple;
        img.color = Color.white;

        var button = go.GetComponent<Button>();
        if (styleSource != null)
        {
            button.colors = styleSource.colors;
            button.spriteState = styleSource.spriteState;
            button.transition = styleSource.transition;

            var styleImage = styleSource.GetComponent<Image>();
            if (styleImage != null)
                img.color = styleImage.color;
        }

        button.targetGraphic = img;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(onClick);

        ApplyButtonLabel(go.transform, label, styleSource);
        return button;
    }

    public static void ApplyButtonLabel(Transform buttonRoot, string label, Button styleSource)
    {
        var tmp = buttonRoot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
        {
            var textGo = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(buttonRoot, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            tmp = textGo.GetComponent<TextMeshProUGUI>();
        }

        if (styleSource != null)
        {
            var styleLabel = styleSource.GetComponentInChildren<TextMeshProUGUI>(true);
            if (styleLabel != null)
            {
                tmp.font = styleLabel.font;
                tmp.fontSize = Mathf.Max(styleLabel.fontSize, 22);
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = styleLabel.color;
            }
        }

        if (tmp.font == null)
            tmp.font = TMP_Settings.defaultFontAsset;

        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }

    public static void RewireButton(Button button, UnityAction onClick)
    {
        if (button == null)
            return;

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(onClick);
    }
}
