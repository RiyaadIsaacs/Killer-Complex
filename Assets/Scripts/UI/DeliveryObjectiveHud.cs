using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the active delivery objective (pickup location, then deliver room) while a leg is in progress.
/// </summary>
public class DeliveryObjectiveHud : MonoBehaviour
{
    [SerializeField] private DeliveryManager deliveryManager;
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private TextMeshProUGUI objectiveLabel;

    [SerializeField] private Vector2 screenPadding = new(24f, 24f);
    [SerializeField] private Vector2 panelSize = new(360f, 56f);

    DeliveryManager _subscribedManager;

    void Awake()
    {
        if (objectivePanel == null)
            BuildDefaultPanel();
    }

    void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    void OnDisable() => Unsubscribe();

    void Update()
    {
        if (PauseScreen.IsGameplayPaused || GameSceneIntroPanel.BlocksGameplay)
        {
            if (objectivePanel != null && objectivePanel.activeSelf)
                objectivePanel.SetActive(false);
            return;
        }

        Refresh();
    }

    void Subscribe()
    {
        Unsubscribe();
        var dm = ResolveManager();
        if (dm == null)
            return;
        dm.OnDeliveryLegPrepared += OnLegPrepared;
        dm.OnDeliveryCompleted += OnLegCompleted;
        _subscribedManager = dm;
    }

    void Unsubscribe()
    {
        if (_subscribedManager == null)
            return;
        _subscribedManager.OnDeliveryLegPrepared -= OnLegPrepared;
        _subscribedManager.OnDeliveryCompleted -= OnLegCompleted;
        _subscribedManager = null;
    }

    void OnLegPrepared() => Refresh();
    void OnLegCompleted(int _) => Refresh();

    DeliveryManager ResolveManager()
    {
        if (deliveryManager != null)
            return deliveryManager;
        return FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
    }

    void Refresh()
    {
        if (objectivePanel == null || objectiveLabel == null)
            return;

        var dm = ResolveManager();
        if (dm == null || dm.ActiveDropPointId < 0 || dm.FinishedAllConfiguredDeliveryLegs)
        {
            objectivePanel.SetActive(false);
            return;
        }

        string text;
        if (dm.RequiresPhysicalPickup && !dm.HasPickedUpCurrentPackage)
        {
            var loc = dm.CurrentPickupLocationLabel;
            text = string.IsNullOrWhiteSpace(loc)
                ? "Find the package in the complex"
                : $"Find package: {FormatLocation(loc)}";
        }
        else if (dm.CurrentLegDestinationApartment >= 0)
            text = $"Deliver to: Room {dm.CurrentLegDestinationApartment}";
        else
            text = "Complete the active delivery";

        objectiveLabel.text = text;
        objectivePanel.SetActive(true);
    }

    static string FormatLocation(string label)
    {
        var t = label.Trim();
        if (t.Length == 0)
            return t;
        if (char.IsUpper(t[0]))
            return t;
        return char.ToUpper(t[0]) + t.Substring(1);
    }

    void BuildDefaultPanel()
    {
        var sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        var canvasGo = new GameObject("DeliveryObjectiveCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 55;
        canvas.pixelPerfect = true;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panelGo = new GameObject("DeliveryObjectivePanel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(canvasGo.transform, false);
        objectivePanel = panelGo;

        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 1f);
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot = new Vector2(0.5f, 1f);
        panelRt.anchoredPosition = new Vector2(0f, -screenPadding.y);
        panelRt.sizeDelta = panelSize;

        var panelImg = panelGo.GetComponent<Image>();
        panelImg.sprite = sprite;
        panelImg.color = new Color32(45, 58, 72, 230);

        var font = TMP_Settings.defaultFontAsset;
        var textGo = new GameObject("ObjectiveText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panelGo.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(16f, 8f);
        textRt.offsetMax = new Vector2(-16f, -8f);

        objectiveLabel = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            objectiveLabel.font = font;
        objectiveLabel.fontSize = 22;
        objectiveLabel.fontStyle = FontStyles.Bold;
        objectiveLabel.alignment = TextAlignmentOptions.Center;
        objectiveLabel.color = new Color32(220, 235, 250, 255);
        objectivePanel.SetActive(false);
    }
}
