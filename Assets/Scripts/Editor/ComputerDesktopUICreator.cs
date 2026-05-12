using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class ComputerDesktopUICreator
{
    const string PrefabPath = "Assets/Prefabs/ComputerDesktopCanvas.prefab";

    [MenuItem("GameObject/Computer Desktop Canvas (Screen Space Overlay)", false, 10)]
    public static void CreateComputerDesktopCanvas()
    {
        EnsureEventSystemForInputSystem();

        var sprite = CreateWhiteSprite();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var tmpFont = TMP_Settings.defaultFontAsset;
        if (tmpFont == null)
        {
            Debug.LogError("TMP default font missing. Use Window > TextMeshPro > Import TMP Essential Resources, then re-run Create Computer Desktop Canvas.");
            return;
        }

        var root = new GameObject("ComputerDesktopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ComputerDesktopUI));
        Undo.RegisterCreatedObjectUndo(root, "Create Computer Desktop Canvas");
        root.SetActive(false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        var rootRt = root.GetComponent<RectTransform>();
        StretchFull(rootRt);

        var bg = CreateImage("DesktopBackground", root.transform, new Color32(52, 73, 94, 255), sprite);
        StretchFull(bg.GetComponent<RectTransform>());

        var dock = new GameObject("IconDock", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(dock, "Create IconDock");
        dock.transform.SetParent(root.transform, false);
        var dockRt = dock.GetComponent<RectTransform>();
        dockRt.anchorMin = new Vector2(0f, 0f);
        dockRt.anchorMax = new Vector2(0f, 0f);
        dockRt.pivot = new Vector2(0f, 0f);
        dockRt.anchoredPosition = new Vector2(48f, 48f);
        dockRt.sizeDelta = new Vector2(400f, 176f);

        var messengerBtn = CreateIconButton("BtnMessenger", dock.transform, new Color32(28, 40, 51, 255), sprite, tmpFont, "MESSENGER", new Vector2(0f, 0f), true);
        var deliveriesBtn = CreateIconButton("BtnDeliveries", dock.transform, new Color32(160, 130, 109, 255), sprite, tmpFont, "DELIVERIES", new Vector2(196f, 0f), false);

        var messengerPanel = CreateWindowPanel("PanelMessenger", root.transform, sprite, font, tmpFont, "MESSENGER", "Chat window (placeholder).", new Color32(44, 62, 80, 255), false, out var messengerClose);
        MessengerChatUIBuilder.AttachToPanel(messengerPanel, sprite);
        var deliveriesPanel = CreateWindowPanel("PanelDeliveries", root.transform, sprite, font, tmpFont, "DELIVERIES", "Deliveries (placeholder).", new Color32(93, 109, 126, 255), false, out var deliveriesClose);
        DeliveryPanelUIBuilder.AttachToPanel(deliveriesPanel, sprite);

        messengerPanel.SetActive(false);
        deliveriesPanel.SetActive(false);

        var desktop = root.GetComponent<ComputerDesktopUI>();
        var so = new SerializedObject(desktop);
        so.FindProperty("messengerIconButton").objectReferenceValue = messengerBtn;
        so.FindProperty("deliveriesIconButton").objectReferenceValue = deliveriesBtn;
        so.FindProperty("messengerPanel").objectReferenceValue = messengerPanel;
        so.FindProperty("deliveriesPanel").objectReferenceValue = deliveriesPanel;
        so.FindProperty("messengerCloseButton").objectReferenceValue = messengerClose;
        so.FindProperty("deliveriesCloseButton").objectReferenceValue = deliveriesClose;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = root;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Debug.Log("Computer desktop canvas saved to " + PrefabPath + ". Root is inactive; assign it to ComputerTerminal.computerScreenRoot.");
    }

    static void EnsureEventSystemForInputSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }

    static Sprite CreateWhiteSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        var white = Color.white;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
            tex.SetPixel(x, y, white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = color;
        return img;
    }

    static Button CreateIconButton(string name, Transform parent, Color bubbleColor, Sprite sprite, TMP_FontAsset tmpFont, string label, Vector2 anchoredPos, bool messengerStyle)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(168f, 132f);

        var bubble = CreateImage("Bubble", go.transform, bubbleColor, sprite);
        var bubbleRt = bubble.GetComponent<RectTransform>();
        StretchFull(bubbleRt);
        bubbleRt.offsetMin = new Vector2(6f, 22f);
        bubbleRt.offsetMax = new Vector2(-6f, -6f);

        if (messengerStyle)
        {
            var tail = CreateImage("Tail", go.transform, new Color(bubbleColor.r * 0.85f, bubbleColor.g * 0.85f, bubbleColor.b * 0.85f, bubbleColor.a), sprite);
            var tailRt = tail.GetComponent<RectTransform>();
            tailRt.anchorMin = new Vector2(0.15f, 0f);
            tailRt.anchorMax = new Vector2(0.35f, 0f);
            tailRt.pivot = new Vector2(0.5f, 1f);
            tailRt.anchoredPosition = new Vector2(0f, 18f);
            tailRt.sizeDelta = new Vector2(28f, 18f);
        }
        else
        {
            var tape = CreateImage("Tape", go.transform, new Color32(210, 200, 180, 255), sprite);
            var tapeRt = tape.GetComponent<RectTransform>();
            tapeRt.anchorMin = new Vector2(0.2f, 0.55f);
            tapeRt.anchorMax = new Vector2(0.8f, 0.75f);
            tapeRt.offsetMin = Vector2.zero;
            tapeRt.offsetMax = Vector2.zero;
        }

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textGo, "Create Label");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 0.38f);
        trt.offsetMin = new Vector2(4f, 2f);
        trt.offsetMax = new Vector2(-4f, -2f);
        var tmpLabel = textGo.GetComponent<TextMeshProUGUI>();
        tmpLabel.font = tmpFont;
        tmpLabel.fontSize = 20;
        tmpLabel.fontStyle = FontStyles.Bold;
        tmpLabel.alignment = TextAlignmentOptions.Center;
        tmpLabel.color = Color.white;
        tmpLabel.text = label;
        tmpLabel.enableWordWrapping = false;
        tmpLabel.overflowMode = TextOverflowModes.Overflow;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = new Color(1f, 1f, 1f, 0.08f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = bubble;
        return btn;
    }

    static GameObject CreateWindowPanel(string name, Transform parent, Sprite sprite, Font font, TMP_FontAsset tmpFont, string title, string body, Color32 headerColor, bool includeBodyPlaceholder, out Button closeButton)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create " + name);
        panel.transform.SetParent(parent, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(720f, 480f);

        var panelBg = panel.GetComponent<Image>();
        panelBg.sprite = sprite;
        panelBg.type = Image.Type.Simple;
        panelBg.color = new Color32(236, 240, 241, 255);

        var header = CreateImage("Header", panel.transform, headerColor, sprite);
        var hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0f, 1f);
        hrt.anchorMax = new Vector2(1f, 1f);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.anchoredPosition = Vector2.zero;
        hrt.sizeDelta = new Vector2(0f, 48f);

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(titleGo, "Create Title");
        titleGo.transform.SetParent(panel.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(12f, -6f);
        titleRt.sizeDelta = new Vector2(-120f, 40f);
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.font = tmpFont;
        titleTmp.fontSize = 28;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.color = Color.white;
        titleTmp.text = title;
        titleTmp.enableWordWrapping = false;
        titleTmp.overflowMode = TextOverflowModes.Overflow;

        var closeGo = new GameObject("BtnClose", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(closeGo, "Create BtnClose");
        closeGo.transform.SetParent(panel.transform, false);
        var crt = closeGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(1f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot = new Vector2(1f, 1f);
        crt.anchoredPosition = new Vector2(-8f, -8f);
        crt.sizeDelta = new Vector2(36f, 36f);
        var closeImg = closeGo.GetComponent<Image>();
        closeImg.sprite = sprite;
        closeImg.type = Image.Type.Simple;
        closeImg.color = new Color32(192, 57, 43, 255);
        closeButton = closeGo.GetComponent<Button>();
        closeButton.targetGraphic = closeImg;

        var xGo = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(xGo, "Create X");
        xGo.transform.SetParent(closeGo.transform, false);
        var xrt = xGo.GetComponent<RectTransform>();
        StretchFull(xrt);
        var xTmp = xGo.GetComponent<TextMeshProUGUI>();
        xTmp.font = tmpFont;
        xTmp.fontSize = 24;
        xTmp.fontStyle = FontStyles.Bold;
        xTmp.alignment = TextAlignmentOptions.Center;
        xTmp.color = Color.white;
        xTmp.text = "X";
        xTmp.enableWordWrapping = false;
        xTmp.overflowMode = TextOverflowModes.Overflow;

        if (includeBodyPlaceholder)
        {
        var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Text));
        Undo.RegisterCreatedObjectUndo(bodyGo, "Create Body");
        bodyGo.transform.SetParent(panel.transform, false);
        var brt = bodyGo.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0f, 0f);
        brt.anchorMax = new Vector2(1f, 1f);
        brt.offsetMin = new Vector2(16f, 16f);
        brt.offsetMax = new Vector2(-16f, -52f);
        var bodyTxt = bodyGo.GetComponent<Text>();
        bodyTxt.font = font;
        bodyTxt.fontSize = 20;
        bodyTxt.alignment = TextAnchor.UpperLeft;
        bodyTxt.color = new Color32(44, 62, 80, 255);
        bodyTxt.text = body;
        }

        return panel;
    }
}


