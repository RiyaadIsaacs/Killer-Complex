using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds <c>Assets/Prefabs/BadEndingCanvas.prefab</c> — full-screen black overlay with centered <b>Bad Ending</b> text.
/// Assign the prefab root to <see cref="BadEndingOrchestrator"/>'s bad-ending canvas field (starts inactive).
/// </summary>
public static class BadEndingCanvasCreator
{
    const string PrefabPath = "Assets/Prefabs/BadEndingCanvas.prefab";

    [MenuItem("Tools/Killer-Complex/Create Bad Ending Canvas Prefab")]
    public static void CreateBadEndingCanvasPrefab()
    {
        var tmpFont = TMP_Settings.defaultFontAsset;
        if (tmpFont == null)
        {
            Debug.LogError(
                $"{nameof(BadEndingCanvasCreator)}: TMP default font missing. Use Window > TextMeshPro > Import TMP Essential Resources, then run this again.");
            return;
        }

        var sprite = ComputerDesktopUICreator.CreateWhiteSprite();

        var root = new GameObject("BadEndingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        var rootRt = root.GetComponent<RectTransform>();
        StretchFull(rootRt);

        var bg = CreateImage("Background", root.transform, Color.black, sprite);
        StretchFull(bg.GetComponent<RectTransform>());
        bg.raycastTarget = true;

        var titleGo = new GameObject("TitleBadEnding", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = Vector2.zero;
        titleRt.sizeDelta = new Vector2(1200f, 200f);

        var tmp = titleGo.GetComponent<TextMeshProUGUI>();
        tmp.text = "Bad Ending";
        tmp.font = tmpFont;
        tmp.fontSize = 72f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color32(220, 220, 220, 255);
        tmp.raycastTarget = false;

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log(
            $"{nameof(BadEndingCanvasCreator)}: Saved {PrefabPath}. Drag the prefab root into {nameof(BadEndingOrchestrator)} → Bad Ending Canvas Root (leave inactive in the hierarchy).");
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
