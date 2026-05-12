using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the deliveries list UI inside <see cref="ComputerDesktopUICreator"/>'s deliveries panel.
/// </summary>
public static class DeliveryPanelUIBuilder
{
    public static void AttachToPanel(GameObject deliveriesPanel, Sprite whiteSprite)
    {
        var font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            Debug.LogError(
                "TMP default font asset is missing. Use Window > TextMeshPro > Import TMP Essential Resources, then re-run Create Computer Desktop Canvas.");
            return;
        }

        var panelRt = deliveriesPanel.GetComponent<RectTransform>();

        var root = new GameObject("DeliveryContentRoot", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Create DeliveryContentRoot");
        root.transform.SetParent(panelRt, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 0f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.offsetMin = new Vector2(16f, 16f);
        rootRt.offsetMax = new Vector2(-16f, -52f);

        var listArea = new GameObject("AssignmentList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        Undo.RegisterCreatedObjectUndo(listArea, "Create AssignmentList");
        listArea.transform.SetParent(root.transform, false);
        var listRt = listArea.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 0f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.offsetMin = new Vector2(0f, 64f);
        listRt.offsetMax = new Vector2(0f, 0f);
        var vlg = listArea.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var line1 = CreateAssignmentLine(listArea.transform, "AssignmentLine1", font, whiteSprite);
        var line2 = CreateAssignmentLine(listArea.transform, "AssignmentLine2", font, whiteSprite);
        var line3 = CreateAssignmentLine(listArea.transform, "AssignmentLine3", font, whiteSprite);

        var btnGo = new GameObject("BtnCompleteCurrentDelivery", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(btnGo, "Create BtnCompleteCurrentDelivery");
        btnGo.transform.SetParent(root.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0f, 0f);
        btnRt.anchorMax = new Vector2(1f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 4f);
        btnRt.sizeDelta = new Vector2(0f, 52f);
        var btnImg = btnGo.GetComponent<Image>();
        btnImg.sprite = whiteSprite;
        btnImg.color = new Color32(46, 204, 113, 255);
        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = btnImg;

        var btnLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnLabel.transform.SetParent(btnGo.transform, false);
        StretchFull(btnLabel.GetComponent<RectTransform>());
        var btnTmp = btnLabel.GetComponent<TextMeshProUGUI>();
        btnTmp.font = font;
        btnTmp.fontSize = 19;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.color = Color.white;
        btnTmp.text = "COMPLETE CURRENT DELIVERY";

        var mgrGo = new GameObject("DeliveryManager", typeof(RectTransform), typeof(DeliveryManager));
        Undo.RegisterCreatedObjectUndo(mgrGo, "Create DeliveryManager");
        mgrGo.transform.SetParent(root.transform, false);
        var mgrRt = mgrGo.GetComponent<RectTransform>();
        mgrRt.anchorMin = Vector2.zero;
        mgrRt.anchorMax = Vector2.one;
        mgrRt.offsetMin = Vector2.zero;
        mgrRt.offsetMax = Vector2.zero;

        var mgr = mgrGo.GetComponent<DeliveryManager>();
        var so = new SerializedObject(mgr);
        so.FindProperty("assignmentLine1").objectReferenceValue = line1;
        so.FindProperty("assignmentLine2").objectReferenceValue = line2;
        so.FindProperty("assignmentLine3").objectReferenceValue = line3;
        so.FindProperty("completeCurrentDeliveryButton").objectReferenceValue = btn;
        so.ApplyModifiedProperties();
    }

    static TextMeshProUGUI CreateAssignmentLine(Transform parent, string name, TMP_FontAsset font, Sprite whiteSprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TextMeshProUGUI), typeof(LayoutElement));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 48f;
        le.preferredHeight = 48f;
        le.flexibleHeight = 0f;
        var bg = go.GetComponent<Image>();
        bg.sprite = whiteSprite;
        bg.color = new Color32(255, 255, 255, 90);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = new Color32(44, 62, 80, 255);
        tmp.margin = new Vector4(12f, 4f, 12f, 4f);
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(0f, 48f);
        return tmp;
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
