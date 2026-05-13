using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a screen-space overlay canvas for persistent notifications (top-left) and attaches <see cref="DeliveryManager"/>.
/// </summary>
public static class GlobalNotificationHudCreator
{
    const string PrefabPath = "Assets/Prefabs/GlobalNotificationHUD.prefab";

    [MenuItem("GameObject/UI/Global Notification HUD (persistent)", false, 11)]
    public static void CreateGlobalNotificationHud()
    {
        var tmpFont = TMP_Settings.defaultFontAsset;
        if (tmpFont == null)
        {
            Debug.LogError("TMP default font missing. Import TMP Essential Resources, then re-run this command.");
            return;
        }

        var sprite = CreateWhiteSprite();

        var root = new GameObject(
            "GlobalNotificationHUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(GlobalNotificationHud),
            typeof(DeliveryManager));

        Undo.RegisterCreatedObjectUndo(root, "Create Global Notification HUD");
        root.SetActive(true);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        StretchFull(root.GetComponent<RectTransform>());

        var topLeft = new GameObject("TopLeftNotifications", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(topLeft, "Create TopLeftNotifications");
        topLeft.transform.SetParent(root.transform, false);
        var tlRt = topLeft.GetComponent<RectTransform>();
        tlRt.anchorMin = new Vector2(0f, 1f);
        tlRt.anchorMax = new Vector2(0f, 1f);
        tlRt.pivot = new Vector2(0f, 1f);
        tlRt.anchoredPosition = new Vector2(20f, -20f);
        tlRt.sizeDelta = new Vector2(420f, 400f);

        // Image and TextMeshProUGUI are both Graphics — only one per GameObject. Background on parent, TMP on child.
        var slot = new GameObject("PackageDeliveredLabel", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(slot, "Create PackageDeliveredLabel");
        slot.transform.SetParent(topLeft.transform, false);
        slot.SetActive(false);
        var slotRt = slot.GetComponent<RectTransform>();
        slotRt.anchorMin = new Vector2(0f, 1f);
        slotRt.anchorMax = new Vector2(1f, 1f);
        slotRt.pivot = new Vector2(0f, 1f);
        slotRt.anchoredPosition = Vector2.zero;
        slotRt.sizeDelta = new Vector2(0f, 44f);

        var slotBg = slot.GetComponent<Image>();
        slotBg.sprite = sprite;
        slotBg.type = Image.Type.Simple;
        slotBg.color = new Color32(44, 62, 80, 230);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(textGo, "Create PackageDeliveredLabel text");
        textGo.transform.SetParent(slot.transform, false);
        StretchFull(textGo.GetComponent<RectTransform>());
        var slotTmp = textGo.GetComponent<TextMeshProUGUI>();
        ApplyPackageDeliveredTmpDefaults(slotTmp, tmpFont);

        var hud = root.GetComponent<GlobalNotificationHud>();
        var soHud = new SerializedObject(hud);
        soHud.FindProperty("topLeftContent").objectReferenceValue = tlRt;
        soHud.FindProperty("packageDeliveredLabel").objectReferenceValue = slotTmp;
        soHud.ApplyModifiedProperties();

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Debug.Log($"Saved {PrefabPath}. DeliveryZone can leave package label empty if it references the DeliveryManager on this root (label is resolved from {nameof(GlobalNotificationHud)}). Point OllamaConnector and DeliveryZones at that DeliveryManager.");

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Object.DestroyImmediate(root);
    }

    /// <summary>
    /// Adds a child <see cref="TextMeshProUGUI"/> under <c>PackageDeliveredLabel</c> when the row only has <see cref="Image"/>
    /// (Image and TMP cannot share one GameObject — both are <see cref="Graphic"/>).
    /// </summary>
    [MenuItem("GameObject/UI/Repair Package Delivered Label (add TMP)", false, 12)]
    public static void RepairPackageDeliveredLabel()
    {
        var tmpFont = TMP_Settings.defaultFontAsset;
        if (tmpFont == null)
        {
            EditorUtility.DisplayDialog(
                "Repair Package Delivered Label",
                "Import TMP Essential Resources (Window → TextMeshPro → Import TMP Essential Resources), then run this command again.",
                "OK");
            return;
        }

        var huds = CollectNotificationHudsFromSelectionOrScene();
        if (huds.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Repair Package Delivered Label",
                "No GlobalNotificationHud found. Add one to the scene or select its root in the Hierarchy, then run again.",
                "OK");
            return;
        }

        foreach (var hud in huds)
            RepairHudPackageLabel(hud, tmpFont);

        Debug.Log($"Repair Package Delivered Label: updated {huds.Count} HUD(s).");
    }

    static List<GlobalNotificationHud> CollectNotificationHudsFromSelectionOrScene()
    {
        var list = new List<GlobalNotificationHud>();
        if (Selection.gameObjects != null && Selection.gameObjects.Length > 0)
        {
            foreach (var go in Selection.gameObjects)
            {
                var inParent = go.GetComponentInParent<GlobalNotificationHud>();
                if (inParent != null && !list.Contains(inParent))
                    list.Add(inParent);
                var onGo = go.GetComponent<GlobalNotificationHud>();
                if (onGo != null && !list.Contains(onGo))
                    list.Add(onGo);
            }

            return list;
        }

        var found = Object.FindObjectsByType<GlobalNotificationHud>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var h in found)
        {
            if (h != null && !list.Contains(h))
                list.Add(h);
        }

        return list;
    }

    static void RepairHudPackageLabel(GlobalNotificationHud hud, TMP_FontAsset tmpFont)
    {
        Transform slot = null;
        foreach (var t in hud.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "PackageDeliveredLabel")
                continue;
            slot = t;
            break;
        }

        if (slot == null)
        {
            Debug.LogWarning($"{nameof(GlobalNotificationHud)} on '{hud.name}': no child named PackageDeliveredLabel.", hud);
            return;
        }

        var tmp = slot.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
        {
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(textGo, "Add PackageDeliveredLabel text");
            textGo.transform.SetParent(slot, false);
            StretchFull(textGo.GetComponent<RectTransform>());
            tmp = textGo.GetComponent<TextMeshProUGUI>();
            ApplyPackageDeliveredTmpDefaults(tmp, tmpFont);
        }

        var so = new SerializedObject(hud);
        so.FindProperty("packageDeliveredLabel").objectReferenceValue = tmp;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(hud);
    }

    internal static void ApplyPackageDeliveredTmpDefaults(TextMeshProUGUI slotTmp, TMP_FontAsset tmpFont)
    {
        if (slotTmp == null || tmpFont == null)
            return;
        slotTmp.font = tmpFont;
        slotTmp.fontSize = 18;
        slotTmp.fontStyle = FontStyles.Bold;
        slotTmp.alignment = TextAlignmentOptions.MidlineLeft;
        slotTmp.color = Color.white;
        slotTmp.margin = new Vector4(14f, 4f, 14f, 4f);
        slotTmp.text = "Package delivered";
        slotTmp.enableWordWrapping = true;
    }

    static Sprite CreateWhiteSprite()
    {
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.DontSaveInEditor | HideFlags.HideInHierarchy;
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
            tex.SetPixel(x, y, Color.white);
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
}
