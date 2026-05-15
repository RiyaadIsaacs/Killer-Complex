using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MessengerChatUIBuilder
{
    public const float SuspicionBarHeight = 36f;
    public const string SuspicionBarRowName = "SuspicionBarRow";

    public static void AttachToPanel(GameObject messengerPanel, Sprite whiteSprite)
    {
        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            Debug.LogError("TMP default font asset is missing. Use Window > TextMeshPro > Import TMP Essential Resources, then re-run Create Computer Desktop Canvas.");
            return;
        }
        var panelRt = messengerPanel.GetComponent<RectTransform>();
        var chatRoot = new GameObject("ChatRoot", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(chatRoot, "Create ChatRoot");
        chatRoot.transform.SetParent(panelRt, false);
        var chatRootRt = chatRoot.GetComponent<RectTransform>();
        chatRootRt.anchorMin = new Vector2(0f, 0f);
        chatRootRt.anchorMax = new Vector2(1f, 1f);
        chatRootRt.offsetMin = new Vector2(16f, 16f);
        chatRootRt.offsetMax = new Vector2(-16f, -52f);

        CreateSuspicionBarRow(chatRoot.transform, whiteSprite, font);

        var scrollGo = new GameObject("ChatScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        Undo.RegisterCreatedObjectUndo(scrollGo, "Create ChatScrollView");
        scrollGo.transform.SetParent(chatRoot.transform, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(0f, 56f + SuspicionBarHeight + 6f);
        scrollRt.offsetMax = new Vector2(0f, 0f);
        var scrollBg = scrollGo.GetComponent<Image>();
        scrollBg.sprite = whiteSprite;
        scrollBg.type = Image.Type.Simple;
        scrollBg.color = new Color32(255, 255, 255, 40);
        var scrollRect = scrollGo.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        Undo.RegisterCreatedObjectUndo(viewport, "Create Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = new Vector2(4f, 4f);
        vpRt.offsetMax = new Vector2(-4f, -4f);
        var vpImg = viewport.GetComponent<Image>();
        vpImg.sprite = whiteSprite;
        vpImg.type = Image.Type.Simple;
        vpImg.color = new Color32(255, 255, 255, 20);
        var content = new GameObject("Content", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(ContentSizeFitter));
        Undo.RegisterCreatedObjectUndo(content, "Create Content");
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
        feedTmp.fontSize = 22;
        feedTmp.alignment = TextAlignmentOptions.TopLeft;
        feedTmp.color = new Color32(44, 62, 80, 255);
        feedTmp.text = string.Empty;
        feedTmp.enableWordWrapping = true;
        feedTmp.overflowMode = TextOverflowModes.Overflow;
        scrollRect.viewport = vpRt;
        scrollRect.content = contentRt;
        var inputRow = new GameObject("InputRow", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(inputRow, "Create InputRow");
        inputRow.transform.SetParent(chatRoot.transform, false);
        var rowRt = inputRow.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 0f);
        rowRt.anchorMax = new Vector2(1f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.anchoredPosition = new Vector2(0f, 4f);
        rowRt.sizeDelta = new Vector2(0f, 52f);
        var inputGo = new GameObject("ChatInput", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        Undo.RegisterCreatedObjectUndo(inputGo, "Create ChatInput");
        inputGo.transform.SetParent(inputRow.transform, false);
        var inputRt = inputGo.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0f, 0f);
        inputRt.anchorMax = new Vector2(1f, 0f);
        inputRt.pivot = new Vector2(0f, 0f);
        inputRt.anchoredPosition = Vector2.zero;
        inputRt.sizeDelta = new Vector2(-112f, 46f);
        var inputBg = inputGo.GetComponent<Image>();
        inputBg.sprite = whiteSprite;
        inputBg.color = new Color32(255, 255, 255, 220);
        var tmpInput = inputGo.GetComponent<TMP_InputField>();
        var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputGo.transform, false);
        var taRt = textArea.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.offsetMin = new Vector2(8f, 6f);
        taRt.offsetMax = new Vector2(-8f, -6f);
        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderGo.transform.SetParent(textArea.transform, false);
        StretchFull(placeholderGo.GetComponent<RectTransform>());
        var phTmp = placeholderGo.GetComponent<TextMeshProUGUI>();
        phTmp.font = font;
        phTmp.fontSize = 20;
        phTmp.color = new Color(0.4f, 0.4f, 0.4f, 0.65f);
        phTmp.text = "Type a message...";
        phTmp.alignment = TextAlignmentOptions.Left;
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(textArea.transform, false);
        StretchFull(textGo.GetComponent<RectTransform>());
        var inputTextTmp = textGo.GetComponent<TextMeshProUGUI>();
        inputTextTmp.font = font;
        inputTextTmp.fontSize = 20;
        inputTextTmp.color = new Color32(44, 62, 80, 255);
        inputTextTmp.alignment = TextAlignmentOptions.Left;
        inputTextTmp.enableWordWrapping = false;
        inputTextTmp.overflowMode = TextOverflowModes.Overflow;
        tmpInput.textViewport = taRt;
        tmpInput.textComponent = inputTextTmp;
        tmpInput.placeholder = phTmp;
        tmpInput.lineType = TMP_InputField.LineType.SingleLine;
        tmpInput.characterLimit = 512;
        var sendGo = new GameObject("BtnSend", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(sendGo, "Create BtnSend");
        sendGo.transform.SetParent(inputRow.transform, false);
        var sendRt = sendGo.GetComponent<RectTransform>();
        sendRt.anchorMin = new Vector2(1f, 0f);
        sendRt.anchorMax = new Vector2(1f, 0f);
        sendRt.pivot = new Vector2(1f, 0f);
        sendRt.anchoredPosition = new Vector2(-2f, 0f);
        sendRt.sizeDelta = new Vector2(100f, 46f);
        var sendImg = sendGo.GetComponent<Image>();
        sendImg.sprite = whiteSprite;
        sendImg.color = new Color32(52, 152, 219, 255);
        var sendBtn = sendGo.GetComponent<Button>();
        sendBtn.targetGraphic = sendImg;
        var sendLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        sendLabel.transform.SetParent(sendGo.transform, false);
        StretchFull(sendLabel.GetComponent<RectTransform>());
        var sendTmp = sendLabel.GetComponent<TextMeshProUGUI>();
        sendTmp.font = font;
        sendTmp.fontSize = 19;
        sendTmp.fontStyle = FontStyles.Bold;
        sendTmp.alignment = TextAlignmentOptions.Center;
        sendTmp.color = Color.white;
        sendTmp.text = "SEND";
        var managerGo = new GameObject("ChatManager", typeof(RectTransform), typeof(ChatManager));
        Undo.RegisterCreatedObjectUndo(managerGo, "Create ChatManager");
        managerGo.transform.SetParent(chatRoot.transform, false);
        var mgrRt = managerGo.GetComponent<RectTransform>();
        mgrRt.anchorMin = Vector2.zero;
        mgrRt.anchorMax = Vector2.one;
        mgrRt.offsetMin = Vector2.zero;
        mgrRt.offsetMax = Vector2.zero;
        var chatManager = managerGo.GetComponent<ChatManager>();
        var so = new SerializedObject(chatManager);
        so.FindProperty("chatScrollRect").objectReferenceValue = scrollRect;
        so.FindProperty("chatFeedText").objectReferenceValue = feedTmp;
        so.FindProperty("chatInputField").objectReferenceValue = tmpInput;
        so.FindProperty("sendButton").objectReferenceValue = sendBtn;
        so.ApplyModifiedProperties();
    }

    /// <summary>Adds <see cref="SuspicionBarRowName"/> under an existing <c>ChatRoot</c> and nudges <c>ChatScrollView</c> down.</summary>
    public static bool TryAttachSuspicionBarToExistingChatRoot(Transform chatRoot, Sprite whiteSprite, TMP_FontAsset font)
    {
        if (chatRoot == null || font == null)
            return false;

        if (chatRoot.Find(SuspicionBarRowName) != null)
            return false;

        CreateSuspicionBarRow(chatRoot, whiteSprite, font);

        var scroll = chatRoot.Find("ChatScrollView")?.GetComponent<RectTransform>();
        if (scroll != null)
        {
            var min = scroll.offsetMin;
            min.y = Mathf.Max(min.y, 56f + SuspicionBarHeight + 6f);
            scroll.offsetMin = min;
        }

        return true;
    }

    static void CreateSuspicionBarRow(Transform chatRoot, Sprite whiteSprite, TMP_FontAsset font)
    {
        var row = new GameObject(SuspicionBarRowName, typeof(RectTransform), typeof(Image), typeof(MessengerSuspicionBar));
        Undo.RegisterCreatedObjectUndo(row, "Create SuspicionBarRow");
        row.transform.SetParent(chatRoot, false);
        row.transform.SetAsFirstSibling();

        var rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, -2f);
        rowRt.sizeDelta = new Vector2(0f, SuspicionBarHeight);

        var rowBg = row.GetComponent<Image>();
        rowBg.sprite = whiteSprite;
        rowBg.type = Image.Type.Simple;
        rowBg.color = new Color32(44, 62, 80, 90);

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(row.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 0.5f);
        titleRt.anchoredPosition = new Vector2(8f, 0f);
        titleRt.sizeDelta = new Vector2(118f, 0f);
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.font = font;
        titleTmp.fontSize = 14;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        titleTmp.color = new Color32(236, 240, 241, 255);
        titleTmp.text = "H suspicion";

        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(row.transform, false);
        var sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0f, 0f);
        sliderRt.anchorMax = new Vector2(1f, 1f);
        sliderRt.offsetMin = new Vector2(128f, 8f);
        sliderRt.offsetMax = new Vector2(-108f, -8f);

        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.wholeNumbers = true;
        slider.interactable = false;
        slider.direction = Slider.Direction.LeftToRight;

        var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(sliderGo.transform, false);
        StretchFull(bgGo.GetComponent<RectTransform>());
        var bgImg = bgGo.GetComponent<Image>();
        bgImg.sprite = whiteSprite;
        bgImg.color = new Color32(30, 40, 50, 200);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        StretchFull(fillAreaRt);
        fillAreaRt.offsetMin = new Vector2(4f, 4f);
        fillAreaRt.offsetMax = new Vector2(-4f, -4f);

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.sprite = whiteSprite;
        fillImg.color = new Color32(52, 152, 219, 255);

        slider.targetGraphic = fillImg;
        slider.fillRect = fillRt;

        var percentGo = new GameObject("Percent", typeof(RectTransform), typeof(TextMeshProUGUI));
        percentGo.transform.SetParent(row.transform, false);
        var percentRt = percentGo.GetComponent<RectTransform>();
        percentRt.anchorMin = new Vector2(1f, 0f);
        percentRt.anchorMax = new Vector2(1f, 1f);
        percentRt.pivot = new Vector2(1f, 0.5f);
        percentRt.anchoredPosition = new Vector2(-8f, 0f);
        percentRt.sizeDelta = new Vector2(44f, 0f);
        var percentTmp = percentGo.GetComponent<TextMeshProUGUI>();
        percentTmp.font = font;
        percentTmp.fontSize = 14;
        percentTmp.fontStyle = FontStyles.Bold;
        percentTmp.alignment = TextAlignmentOptions.MidlineRight;
        percentTmp.color = Color.white;
        percentTmp.text = "0%";

        var statusGo = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGo.transform.SetParent(row.transform, false);
        var statusRt = statusGo.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(1f, 0f);
        statusRt.anchorMax = new Vector2(1f, 1f);
        statusRt.pivot = new Vector2(1f, 0.5f);
        statusRt.anchoredPosition = new Vector2(-54f, 0f);
        statusRt.sizeDelta = new Vector2(72f, 0f);
        var statusTmp = statusGo.GetComponent<TextMeshProUGUI>();
        statusTmp.font = font;
        statusTmp.fontSize = 12;
        statusTmp.alignment = TextAlignmentOptions.MidlineRight;
        statusTmp.color = new Color32(200, 210, 220, 255);
        statusTmp.text = "Chilled";

        var bar = row.GetComponent<MessengerSuspicionBar>();
        var barSo = new SerializedObject(bar);
        barSo.FindProperty("slider").objectReferenceValue = slider;
        barSo.FindProperty("fillImage").objectReferenceValue = fillImg;
        barSo.FindProperty("percentText").objectReferenceValue = percentTmp;
        barSo.FindProperty("statusText").objectReferenceValue = statusTmp;
        barSo.ApplyModifiedProperties();
    }

    [MenuItem("Tools/Killer-Complex/Patch ComputerDesktopCanvas prefab (Messenger Suspicion Bar)")]
    public static void PatchComputerDesktopCanvasPrefabSuspicionBar()
    {
        const string prefabPath = "Assets/Prefabs/ComputerDesktopCanvas.prefab";
        if (!System.IO.File.Exists(prefabPath))
        {
            EditorUtility.DisplayDialog("Patch Messenger Suspicion Bar", $"Prefab not found:\n{prefabPath}", "OK");
            return;
        }

        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            EditorUtility.DisplayDialog("Patch Messenger Suspicion Bar", "Import TMP Essential Resources first.", "OK");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform chatRoot = null;
            foreach (var tr in root.GetComponentsInChildren<Transform>(true))
            {
                if (tr.name == "ChatRoot")
                {
                    chatRoot = tr;
                    break;
                }
            }

            if (chatRoot == null)
            {
                EditorUtility.DisplayDialog("Patch Messenger Suspicion Bar", "No ChatRoot under prefab.", "OK");
                return;
            }

            var sprite = ComputerDesktopUICreator.CreateWhiteSprite();
            if (!TryAttachSuspicionBarToExistingChatRoot(chatRoot, sprite, font))
            {
                EditorUtility.DisplayDialog("Patch Messenger Suspicion Bar", "SuspicionBarRow already present.", "OK");
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log($"Patched {prefabPath} with {SuspicionBarRowName}.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static Transform FindChatRootFromSelection()
    {
        var sel = Selection.activeTransform;
        if (sel == null)
            return null;
        if (sel.name == "ChatRoot")
            return sel;
        var direct = sel.Find("ChatRoot");
        if (direct != null)
            return direct;
        foreach (var t in sel.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "ChatRoot")
                return t;
        }

        return null;
    }

    [MenuItem("GameObject/UI/Add Messenger Suspicion Bar (PanelMessenger)", false, 14)]
    public static void AddSuspicionBarToSelectedMessenger()
    {
        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            EditorUtility.DisplayDialog("Messenger Suspicion Bar", "Import TMP Essential Resources first.", "OK");
            return;
        }

        Transform chatRoot = FindChatRootFromSelection();

        if (chatRoot == null)
        {
            foreach (var panel in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (panel.name != "PanelMessenger")
                    continue;
                chatRoot = panel.Find("ChatRoot");
                if (chatRoot != null)
                    break;
            }
        }

        if (chatRoot == null)
        {
            EditorUtility.DisplayDialog("Messenger Suspicion Bar", "Select PanelMessenger or ChatRoot in the hierarchy.", "OK");
            return;
        }

        var sprite = ComputerDesktopUICreator.CreateWhiteSprite();
        if (!TryAttachSuspicionBarToExistingChatRoot(chatRoot, sprite, font))
        {
            EditorUtility.DisplayDialog("Messenger Suspicion Bar", "SuspicionBarRow already exists under ChatRoot.", "OK");
            return;
        }

        EditorUtility.SetDirty(chatRoot.gameObject);
        Debug.Log($"Added {SuspicionBarRowName} under {chatRoot.name}. Save the scene or prefab.");
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
