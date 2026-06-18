using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the active delivery objective while a leg is in progress (stacked under the urgent timer).
/// </summary>
public class DeliveryObjectiveHud : MonoBehaviour
{
    const float RowHeight = 44f;
    const float RowGap = 8f;

    [SerializeField] private DeliveryManager deliveryManager;
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private TextMeshProUGUI objectiveLabel;

    DeliveryManager _subscribedManager;
    DeliveryUrgencyTimer _cachedTimer;

    void Awake()
    {
        CleanupLegacyObjectiveCanvas();
        TryAutoAssignPanel();

        if (objectivePanel == null)
            BuildDefaultPanel();
        else
            PositionUnderTimer();
    }

    void TryAutoAssignPanel()
    {
        if (objectivePanel != null && objectiveLabel != null)
            return;

        foreach (var tr in GetComponentsInChildren<Transform>(true))
        {
            if (tr.name != "DeliveryObjectiveLabel")
                continue;

            objectivePanel = tr.gameObject;
            objectiveLabel = tr.GetComponentInChildren<TextMeshProUGUI>(true);
            break;
        }
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
        var persistent = GlobalNotificationHud.FindDeliveryManager();
        if (persistent != null)
        {
            deliveryManager = persistent;
            return persistent;
        }

        if (deliveryManager != null)
            return deliveryManager;

        return FindFirstObjectByType<DeliveryManager>(FindObjectsInactive.Include);
    }

    GlobalNotificationHud ResolveHud() =>
        GetComponent<GlobalNotificationHud>() ??
        FindFirstObjectByType<GlobalNotificationHud>(FindObjectsInactive.Include);

    DeliveryUrgencyTimer ResolveTimer()
    {
        if (_cachedTimer != null)
            return _cachedTimer;

        _cachedTimer = GetComponent<DeliveryUrgencyTimer>();
        if (_cachedTimer != null)
            return _cachedTimer;

        _cachedTimer = FindFirstObjectByType<DeliveryUrgencyTimer>(FindObjectsInactive.Include);
        return _cachedTimer;
    }

    void Refresh()
    {
        if (objectivePanel == null || objectiveLabel == null)
            return;

        var timer = ResolveTimer();
        if (timer == null || !timer.IsCountdownActive)
        {
            objectivePanel.SetActive(false);
            return;
        }

        var dm = ResolveManager();
        if (dm == null || dm.ActiveDropPointId < 0 || dm.FinishedAllConfiguredDeliveryLegs)
        {
            objectivePanel.SetActive(false);
            return;
        }

        string text;
        if (dm.RequiresPhysicalPickup && !dm.HasPickedUpCurrentPackage)
            text = "Find the package in the complex.";
        else if (dm.CurrentLegDestinationApartment >= 0)
            text = $"Deliver to: Room {dm.CurrentLegDestinationApartment}";
        else
            text = "Complete the active delivery";

        objectiveLabel.text = text;
        objectivePanel.SetActive(true);
    }

    void CleanupLegacyObjectiveCanvas()
    {
        var legacy = transform.Find("DeliveryObjectiveCanvas");
        if (legacy != null)
            Destroy(legacy.gameObject);
    }

    void BuildDefaultPanel()
    {
        var hud = ResolveHud();
        var parent = hud != null && hud.TopLeftContent != null
            ? hud.TopLeftContent
            : transform;

        var sprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        var panelGo = new GameObject("DeliveryObjectiveLabel", typeof(RectTransform), typeof(Image));
        panelGo.transform.SetParent(parent, false);
        objectivePanel = panelGo;

        var panelRt = panelGo.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0f, 1f);
        panelRt.anchorMax = new Vector2(1f, 1f);
        panelRt.pivot = new Vector2(0f, 1f);
        panelRt.sizeDelta = new Vector2(0f, RowHeight);

        var panelImg = panelGo.GetComponent<Image>();
        panelImg.sprite = sprite;
        panelImg.type = Image.Type.Simple;
        panelImg.color = new Color32(45, 58, 72, 230);

        var font = TMP_Settings.defaultFontAsset;
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panelGo.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        objectiveLabel = textGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            objectiveLabel.font = font;
        objectiveLabel.fontSize = 18;
        objectiveLabel.fontStyle = FontStyles.Bold;
        objectiveLabel.alignment = TextAlignmentOptions.MidlineLeft;
        objectiveLabel.margin = new Vector4(14f, 4f, 14f, 4f);
        objectiveLabel.color = new Color32(220, 235, 250, 255);
        objectiveLabel.textWrappingMode = TextWrappingModes.Normal;

        PositionUnderTimer();
        objectivePanel.SetActive(false);
    }

    void PositionUnderTimer()
    {
        if (objectivePanel == null)
            return;

        var objectiveRt = objectivePanel.GetComponent<RectTransform>();
        if (objectiveRt == null)
            return;

        var hud = ResolveHud();
        var timerRoot = hud != null
            ? GlobalNotificationHud.GetUrgentDeliveryTimerRowRoot(hud.UrgentDeliveryTimerLabel)
            : null;

        if (timerRoot != null)
        {
            var timerRt = timerRoot.GetComponent<RectTransform>();
            objectiveRt.anchorMin = timerRt.anchorMin;
            objectiveRt.anchorMax = timerRt.anchorMax;
            objectiveRt.pivot = timerRt.pivot;
            objectiveRt.sizeDelta = timerRt.sizeDelta;
            objectiveRt.anchoredPosition = timerRt.anchoredPosition + new Vector2(0f, -(timerRt.sizeDelta.y + RowGap));
            return;
        }

        objectiveRt.anchorMin = new Vector2(0f, 1f);
        objectiveRt.anchorMax = new Vector2(1f, 1f);
        objectiveRt.pivot = new Vector2(0f, 1f);
        objectiveRt.sizeDelta = new Vector2(0f, RowHeight);
        objectiveRt.anchoredPosition = new Vector2(0f, -(52f + RowHeight + RowGap));
    }
}
