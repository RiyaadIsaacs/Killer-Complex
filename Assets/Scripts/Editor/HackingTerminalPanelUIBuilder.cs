using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the hacking terminal (decryption bar, hack button, console) inside
/// <see cref="ComputerDesktopUICreator"/>'s hacking panel.
/// </summary>
public static class HackingTerminalPanelUIBuilder
{
    public static void AttachToPanel(GameObject hackingPanel, Sprite whiteSprite)
    {
        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            Debug.LogError(
                "TMP default font asset is missing. Use Window > TextMeshPro > Import TMP Essential Resources, then re-run Create Computer Desktop Canvas.");
            return;
        }

        var panelRt = hackingPanel.GetComponent<RectTransform>();

        var root = new GameObject("HackingTerminalContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(HackingTerminalPanel));
        Undo.RegisterCreatedObjectUndo(root, "Create HackingTerminalContent");
        root.transform.SetParent(panelRt, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 0f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.offsetMin = new Vector2(16f, 16f);
        rootRt.offsetMax = new Vector2(-16f, -52f);

        var vlg = root.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var statusGo = new GameObject("DecryptionStatusLabel", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        Undo.RegisterCreatedObjectUndo(statusGo, "Create DecryptionStatusLabel");
        statusGo.transform.SetParent(root.transform, false);
        statusGo.GetComponent<LayoutElement>().preferredHeight = 28f;
        var statusTmp = statusGo.GetComponent<TextMeshProUGUI>();
        statusTmp.font = font;
        statusTmp.fontSize = 20;
        statusTmp.fontStyle = FontStyles.Bold;
        statusTmp.alignment = TextAlignmentOptions.MidlineLeft;
        statusTmp.color = new Color32(44, 62, 80, 255);
        statusTmp.text = "Decryption Status: 0%";

        var slider = CreateDecryptionSlider(root.transform, whiteSprite);
        var sliderLe = slider.gameObject.AddComponent<LayoutElement>();
        sliderLe.preferredHeight = 32f;
        sliderLe.minHeight = 32f;

        var hackBtn = CreateHackButton(root.transform, whiteSprite, font);
        var hackLe = hackBtn.gameObject.AddComponent<LayoutElement>();
        hackLe.preferredHeight = 48f;
        hackLe.minHeight = 48f;

        var consoleTmp = CreateConsoleScroll(root.transform, whiteSprite, font);

        var panel = root.GetComponent<HackingTerminalPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("decryptionSlider").objectReferenceValue = slider;
        so.FindProperty("decryptionStatusLabel").objectReferenceValue = statusTmp;
        so.FindProperty("hackButton").objectReferenceValue = hackBtn;
        so.FindProperty("consoleOutput").objectReferenceValue = consoleTmp;

        var canvasRoot = hackingPanel.transform.root.gameObject;
        var ollama = canvasRoot.GetComponentInChildren<OllamaConnector>(true);
        so.FindProperty("ollamaConnector").objectReferenceValue = ollama;
        so.ApplyModifiedProperties();
    }

    static Slider CreateDecryptionSlider(Transform parent, Sprite sprite)
    {
        var root = new GameObject("DecryptionSlider", typeof(RectTransform), typeof(Slider));
        Undo.RegisterCreatedObjectUndo(root, "Create DecryptionSlider");
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 32f);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(bg, "Create Slider Background");
        bg.transform.SetParent(root.transform, false);
        StretchFull(bg.GetComponent<RectTransform>());
        var bgImg = bg.GetComponent<Image>();
        bgImg.sprite = sprite;
        bgImg.type = Image.Type.Simple;
        bgImg.color = new Color32(40, 55, 71, 220);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(fillArea, "Create Fill Area");
        fillArea.transform.SetParent(root.transform, false);
        StretchFull(fillArea.GetComponent<RectTransform>());

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(fill, "Create Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fRt = fill.GetComponent<RectTransform>();
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = new Vector2(0f, 1f);
        fRt.pivot = new Vector2(0f, 0.5f);
        fRt.offsetMin = Vector2.zero;
        fRt.offsetMax = Vector2.zero;
        var fImg = fill.GetComponent<Image>();
        fImg.sprite = sprite;
        fImg.type = Image.Type.Simple;
        fImg.color = new Color32(39, 174, 96, 255);

        var sl = root.GetComponent<Slider>();
        sl.fillRect = fRt;
        sl.targetGraphic = fImg;
        sl.navigation = new Navigation { mode = Navigation.Mode.None };
        sl.direction = Slider.Direction.LeftToRight;
        sl.minValue = 0f;
        sl.maxValue = 100f;
        sl.wholeNumbers = true;
        sl.value = 0f;
        return sl;
    }

    static Button CreateHackButton(Transform parent, Sprite sprite, TMP_FontAsset tmpFont)
    {
        var go = new GameObject("BtnHack", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Create BtnHack");
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 48f);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = new Color32(41, 128, 185, 255);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(labelGo, "Create Hack label");
        labelGo.transform.SetParent(go.transform, false);
        StretchFull(labelGo.GetComponent<RectTransform>());
        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.font = tmpFont;
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = "Hack";
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;

        return btn;
    }

    static TextMeshProUGUI CreateConsoleScroll(Transform parent, Sprite sprite, TMP_FontAsset font)
    {
        var scrollGo = new GameObject("ConsoleScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
        Undo.RegisterCreatedObjectUndo(scrollGo, "Create ConsoleScrollView");
        scrollGo.transform.SetParent(parent, false);
        var scrollLe = scrollGo.GetComponent<LayoutElement>();
        scrollLe.minHeight = 120f;
        scrollLe.preferredHeight = 160f;
        scrollLe.flexibleHeight = 1f;

        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.sizeDelta = Vector2.zero;

        var scrollBg = scrollGo.GetComponent<Image>();
        scrollBg.sprite = sprite;
        scrollBg.type = Image.Type.Simple;
        scrollBg.color = new Color32(20, 30, 40, 200);

        var scrollRect = scrollGo.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        Undo.RegisterCreatedObjectUndo(viewport, "Create Console Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(4f, 4f);
        vpRt.offsetMax = new Vector2(-4f, -4f);
        var vpImg = viewport.GetComponent<Image>();
        vpImg.sprite = sprite;
        vpImg.type = Image.Type.Simple;
        vpImg.color = new Color32(0, 0, 0, 40);

        var content = new GameObject("ConsoleContent", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(content, "Create ConsoleContent");
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);
        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var feedTmp = content.GetComponent<TextMeshProUGUI>();
        feedTmp.font = font;
        feedTmp.fontSize = 16;
        feedTmp.alignment = TextAlignmentOptions.TopLeft;
        feedTmp.color = new Color32(180, 220, 180, 255);
        feedTmp.text = string.Empty;
        feedTmp.enableWordWrapping = true;
        feedTmp.overflowMode = TextOverflowModes.Overflow;

        scrollRect.viewport = vpRt;
        scrollRect.content = contentRt;

        return feedTmp;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
