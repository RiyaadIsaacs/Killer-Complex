using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds <c>Assets/Prefabs/GoodEndingCanvas.prefab</c> — full-screen overlay for the hack-reversal victory beat.
/// </summary>
public static class GoodEndingCanvasCreator
{
    const string PrefabPath = "Assets/Prefabs/GoodEndingCanvas.prefab";

    [MenuItem("Tools/Killer-Complex/Create Good Ending Canvas Prefab")]
    public static void CreateGoodEndingCanvasPrefab()
    {
        var tmpFont = TMP_Settings.defaultFontAsset;
        if (tmpFont == null)
        {
            Debug.LogError(
                $"{nameof(GoodEndingCanvasCreator)}: TMP default font missing. Import TMP Essential Resources, then run again.");
            return;
        }

        var sprite = ComputerDesktopUICreator.CreateWhiteSprite();

        var root = new GameObject("GoodEndingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.SetActive(false);

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        canvas.pixelPerfect = true;
        canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                           | AdditionalCanvasShaderChannels.Normal
                                           | AdditionalCanvasShaderChannels.Tangent;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;

        StretchFull(root.GetComponent<RectTransform>());

        var bg = CreateImage("Background", root.transform, new Color32(8, 18, 14, 255), sprite);
        StretchFull(bg.GetComponent<RectTransform>());
        bg.raycastTarget = true;

        var titleGo = new GameObject("TitleGoodEnding", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.55f);
        titleRt.anchorMax = new Vector2(0.5f, 0.55f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = Vector2.zero;
        titleRt.sizeDelta = new Vector2(1200f, 120f);

        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Good Ending";
        titleTmp.font = tmpFont;
        titleTmp.fontSize = 72f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color32(168, 220, 178, 255);
        titleTmp.raycastTarget = false;

        var subtitleGo = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subtitleGo.transform.SetParent(root.transform, false);
        var subRt = subtitleGo.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.42f);
        subRt.anchorMax = new Vector2(0.5f, 0.42f);
        subRt.pivot = new Vector2(0.5f, 0.5f);
        subRt.anchoredPosition = Vector2.zero;
        subRt.sizeDelta = new Vector2(1100f, 160f);

        var subTmp = subtitleGo.GetComponent<TextMeshProUGUI>();
        subTmp.text =
            "You breached H's uplink. He's on the run—and the apartment door is yours again.\n" +
            "Finding your wife is still ahead, but tonight you took the complex back.";
        subTmp.font = tmpFont;
        subTmp.fontSize = 28f;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.color = new Color32(200, 215, 205, 255);
        subTmp.enableWordWrapping = true;
        subTmp.raycastTarget = false;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log(
            $"{nameof(GoodEndingCanvasCreator)}: Saved {PrefabPath}. Assign the prefab root to {nameof(GoodEndingOrchestrator)} → Good Ending Canvas Root.");
    }

    [MenuItem("Tools/Killer-Complex/Add Good Ending Canvas to open scene")]
    public static void AddGoodEndingCanvasToOpenScene()
    {
        if (!System.IO.File.Exists(PrefabPath))
            CreateGoodEndingCanvasPrefab();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            return;

        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tr != null && tr.name == "GoodEndingCanvas")
            {
                Debug.Log("GoodEndingCanvas already exists in the scene.");
                Selection.activeGameObject = tr.gameObject;
                return;
            }
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
            return;

        instance.SetActive(false);
        Undo.RegisterCreatedObjectUndo(instance, "Add Good Ending Canvas");
        Selection.activeGameObject = instance;

        var orchestrator = Object.FindFirstObjectByType<GoodEndingOrchestrator>(FindObjectsInactive.Include);
        if (orchestrator != null)
        {
            var so = new SerializedObject(orchestrator);
            so.FindProperty("goodEndingCanvasRoot").objectReferenceValue = instance;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        Debug.Log($"Added {instance.name} to the scene (inactive). Assign on {nameof(GoodEndingOrchestrator)} if needed.");
    }

    [MenuItem("Tools/Killer-Complex/Wire Good Ending Canvas in open scene")]
    public static void WireGoodEndingInOpenScene()
    {
        var orchestrator = Object.FindFirstObjectByType<GoodEndingOrchestrator>(FindObjectsInactive.Include);
        if (orchestrator == null)
        {
            Debug.LogError($"No {nameof(GoodEndingOrchestrator)} in the open scene. Add one (e.g. on the BadEndingOrchestrator object).");
            return;
        }

        var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        GameObject root = null;
        foreach (var tr in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tr != null && tr.name == "GoodEndingCanvas")
            {
                root = tr.gameObject;
                break;
            }
        }

        if (root == null)
        {
            Debug.LogError("No GoodEndingCanvas in scene. Run Create Good Ending Canvas Prefab and drag it into the scene.");
            return;
        }

        var so = new SerializedObject(orchestrator);
        so.FindProperty("goodEndingCanvasRoot").objectReferenceValue = root;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(orchestrator);
        Debug.Log($"Wired {root.name} to {orchestrator.name}.");
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
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.color = color;
        return img;
    }
}
