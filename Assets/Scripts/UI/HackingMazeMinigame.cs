using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Procedural maze breach sim: difficulty scales with <see cref="HackingTerminalPanel.GetMazeTier"/>.
/// The base carve is a perfect maze, then extra passages add <b>loops</b> so several viable routes exist; hazards are
/// blended between shortest-path and longer-path cells so they do not paint a single obvious line. Hold keys to move.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HackingMazeMinigame : MonoBehaviour
{
    const int MinDim = 7;
    const int MaxDim = 25;

    const float MazeBoxWidth = 1120f;
    const float MazeBoxHeight = 860f;
    const float GridMinHeight = 320f;
    const float MazeHostMargin = 10f;
    const float MazeHostFill = 0.97f;
    /// <summary>Vertical space reserved in <see cref="MazeBox"/> for title, status line, and abort row (instructions live in <see cref="_controlsDockRoot"/>).</summary>
    const float MazeChromeVerticalReserve = 172f;
    const float MazeStatusBarHeight = 40f;
    const float ControlsDockWidth = 280f;
    const float ControlsDockGap = 10f;

    static readonly Vector2Int[] CardinalDirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
    };

    [SerializeField, Range(MinDim, MaxDim)] private int mazeColumns = 15;
    [SerializeField, Range(MinDim, MaxDim)] private int mazeRows = 13;

    [Header("Layout")]
    [Tooltip("If set, the maze dimmer + dialog are parented here. If empty, uses PanelHackingTerminal (full terminal), not the layout-driven content area.")]
    [SerializeField] private RectTransform mazeHostOverride;

    [Header("Movement (hold keys)")]
    [Tooltip("After first step, seconds before auto-repeat starts.")]
    [SerializeField, Range(0.05f, 0.5f)] private float holdInitialDelay = 0.14f;
    [Tooltip("Seconds between steps while a direction key is held.")]
    [SerializeField, Range(0.03f, 0.3f)] private float holdRepeatInterval = 0.07f;

    [Header("Maze topology (loops & hazards)")]
    [Tooltip("Fraction of interior wall cells opened after the perfect maze to create alternate routes (higher = more branches).")]
    [SerializeField, Range(0.04f, 0.28f)] private float loopCarveDensity = 0.11f;
    [Tooltip("When placing bombs/blocks, chance to pick a shortest-route cell vs a longer-route cell (lower = less obvious 'line' toward the goal).")]
    [SerializeField, Range(0.15f, 0.75f)] private float corridorHazardBias = 0.38f;

    [Header("Vision")]
    [Tooltip("Chebyshev radius around the player for visible cells (1 = 3×3, 2 = 5×5).")]
    [SerializeField, Range(0, 3)]
    private int visionRadius = 2;

    [SerializeField] private Color32 fogColor = new(12, 16, 22, 255);

    [Header("Goal placement")]
    [Tooltip("Minimum BFS steps from start (1,1) when picking a random green uplink cell.")]
    [SerializeField, Min(3)]
    private int minGoalDistanceFromStart = 6;

    [Header("Colours")]
    [SerializeField] private Color32 wallColor = new(25, 35, 50, 255);
    [SerializeField] private Color32 floorColor = new(52, 73, 94, 255);
    [SerializeField] private Color32 playerColor = new(52, 152, 219, 255);
    [SerializeField] private Color32 goalColor = new(39, 174, 96, 255);
    [SerializeField] private Color32 obstacleColor = new(211, 84, 0, 255);
    [SerializeField] private Color32 bombColor = new(192, 57, 43, 255);

    private HackingTerminalPanel _host;
    private GameObject _overlayRoot;
    private GameObject _controlsDockRoot;
    private GameObject _consoleScrollRoot;
    private RectTransform _terminalPanelRt;
    private RectTransform _boxRt;
    private LayoutElement _gridLayoutElement;
    private RectTransform _gridRt;
    private GridLayoutGroup _gridLayout;
    private TMP_Text _hintLabel;
    private readonly List<Image> _cellImages = new();
    private Sprite _pixelSprite;

    private bool[,] _walkable;
    private bool[,] _obstacle;
    private bool[,] _bomb;
    private int _cols;
    private int _rows;
    private int _px;
    private int _py;
    private int _gx;
    private int _gy;
    private bool _won;
    private bool _runEnded;
    private float _nextAutoMoveTime;
    private Coroutine _deferredGridLayoutRefresh;

    static HackingMazeMinigame s_ActiveOverlay;

    public bool IsOpen => _overlayRoot != null && _overlayRoot.activeSelf;

    public static bool TryConsumeEscape()
    {
        if (s_ActiveOverlay == null || !s_ActiveOverlay.IsOpen)
            return false;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return false;
        s_ActiveOverlay.CloseWithoutSuccess();
        return true;
    }

    public void Initialize(HackingTerminalPanel host)
    {
        _host = host;
        EnsureUiBuilt();
    }

    public void OpenAndRegenerate()
    {
        if (_host == null)
            return;

        EnsureUiBuilt();
        EnsureOverlayOnMazeHost();
        ApplyMazeChromeLayout();
        LayoutControlsDock();
        SetConsoleScrollVisible(false);
        _controlsDockRoot?.SetActive(true);
        BringMazeOverlayToFront();
        _overlayRoot.SetActive(true);
        s_ActiveOverlay = this;
        _won = false;
        _runEnded = false;
        _nextAutoMoveTime = 0f;

        var tier = _host.GetMazeTier();
        var colExtra = Mathf.Min(tier * 3, MaxDim - mazeColumns);
        var rowExtra = Mathf.Min(tier * 3, MaxDim - mazeRows);
        _cols = ToOddClamped(mazeColumns + colExtra);
        _rows = ToOddClamped(mazeRows + rowExtra);

        var obstacleCount = ComputeObstacleCount(tier);
        var bombCount = ComputeBombCount(tier);

        GenerateMaze();
        CarveRandomLoopPassages();
        PickRandomGoalCell();
        PlaceHazards(obstacleCount, bombCount);

        ApplyMazeChromeLayout();
        BuildOrResizeCellGrid();
        RefreshAllCells();
        UpdateHint(tier, obstacleCount, bombCount);

        if (_deferredGridLayoutRefresh != null)
        {
            StopCoroutine(_deferredGridLayoutRefresh);
            _deferredGridLayoutRefresh = null;
        }

        _deferredGridLayoutRefresh = StartCoroutine(DeferredMazeGridLayoutRefresh());

        _host.AppendConsoleLine(
            $"> Breach sim tier {tier}: {_cols}×{_rows} branched maze — {obstacleCount} blocks, {bombCount} bombs (hold WASD / arrows).");
    }

    IEnumerator DeferredMazeGridLayoutRefresh()
    {
        yield return null;
        if (_overlayRoot == null || !_overlayRoot.activeSelf)
        {
            _deferredGridLayoutRefresh = null;
            yield break;
        }

        EnsureOverlayOnMazeHost();
        BringMazeOverlayToFront();
        ApplyMazeChromeLayout();
        LayoutControlsDock();
        if (_boxRt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_boxRt);
        if (_gridRt != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(_gridRt);

        BuildOrResizeCellGrid();
        RefreshAllCells();
        _deferredGridLayoutRefresh = null;
    }

    void ApplyMazeChromeLayout()
    {
        if (_boxRt == null || _overlayRoot == null)
            return;

        var host = _overlayRoot.transform.parent as RectTransform;
        if (host == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(host);
        for (var p = host.parent as RectTransform; p != null; p = p.parent as RectTransform)
            LayoutRebuilder.ForceRebuildLayoutImmediate(p);

        var aw = Mathf.Max(48f, host.rect.width - MazeHostMargin * 2f);
        var ah = Mathf.Max(48f, host.rect.height - MazeHostMargin * 2f);
        var boxW = Mathf.Min(MazeBoxWidth, aw * MazeHostFill);
        var boxH = Mathf.Min(MazeBoxHeight, ah * MazeHostFill);

        _boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        _boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        _boxRt.pivot = new Vector2(0.5f, 0.5f);
        _boxRt.anchoredPosition = Vector2.zero;
        _boxRt.sizeDelta = new Vector2(boxW, boxH);

        if (_gridLayoutElement != null)
        {
            var gridCap = Mathf.Max(GridMinHeight, boxH - MazeChromeVerticalReserve);
            _gridLayoutElement.minHeight = Mathf.Min(GridMinHeight, gridCap);
            _gridLayoutElement.preferredHeight = gridCap;
            _gridLayoutElement.flexibleHeight = 1f;
        }

        LayoutControlsDock();
    }

    void LayoutControlsDock()
    {
        if (_controlsDockRoot == null || _terminalPanelRt == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_terminalPanelRt);

        var pr = _terminalPanelRt.rect;
        var dockRt = _controlsDockRoot.GetComponent<RectTransform>();
        dockRt.anchorMin = new Vector2(0.5f, 0.5f);
        dockRt.anchorMax = new Vector2(0.5f, 0.5f);
        dockRt.pivot = new Vector2(1f, 0.5f);
        dockRt.sizeDelta = new Vector2(ControlsDockWidth, Mathf.Max(160f, pr.height - 36f));
        dockRt.anchoredPosition = new Vector2(-pr.width * 0.5f - ControlsDockGap, 0f);
    }

    RectTransform GetMazeHostRect()
    {
        if (mazeHostOverride != null)
            return mazeHostOverride;

        if (_terminalPanelRt == null)
            _terminalPanelRt = transform.parent as RectTransform;
        if (_terminalPanelRt != null)
            return _terminalPanelRt;

        return transform as RectTransform ?? GetComponent<RectTransform>();
    }

    void EnsureOverlayOnMazeHost()
    {
        if (_overlayRoot == null)
            return;

        var hostRt = GetMazeHostRect();
        if (hostRt == null)
            return;

        var overlayRt = _overlayRoot.GetComponent<RectTransform>();
        if (overlayRt.parent != hostRt)
        {
            overlayRt.SetParent(hostRt, false);
            StretchFull(overlayRt);
        }

        var layoutElement = _overlayRoot.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = _overlayRoot.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        BringMazeOverlayToFront();
    }

    void BringMazeOverlayToFront()
    {
        if (_overlayRoot != null)
            _overlayRoot.transform.SetAsLastSibling();
    }

    void SetConsoleScrollVisible(bool visible)
    {
        if (_consoleScrollRoot == null)
        {
            var scroll = transform.Find("ConsoleScrollView");
            if (scroll != null)
                _consoleScrollRoot = scroll.gameObject;
        }

        if (_consoleScrollRoot != null && _consoleScrollRoot.activeSelf != visible)
            _consoleScrollRoot.SetActive(visible);
    }

    static int ToOddClamped(int v)
    {
        if ((v & 1) == 0)
            v++;
        return Mathf.Clamp(v, MinDim, MaxDim);
    }

    static int ComputeObstacleCount(int tier)
    {
        if (tier <= 0)
            return 0;
        return Mathf.Min(16, tier * 2 + Mathf.Max(0, (tier - 1) * 2) + (tier >= 4 ? 1 : 0));
    }

    static int ComputeBombCount(int tier)
    {
        if (tier <= 1)
            return 0;
        return Mathf.Min(9, tier + (tier >= 3 ? 1 : 0) + (tier >= 5 ? 1 : 0) + (tier >= 7 ? 1 : 0));
    }

    public void CloseWithoutSuccess()
    {
        if (_host != null && _walkable != null && !_won && !_runEnded)
            _host.OnMazeRoundAttemptFinished(false);

        HideMazeUi();
        if (s_ActiveOverlay == this)
            s_ActiveOverlay = null;
        _host?.AppendConsoleLine("> Breach sim aborted.");
    }

    /// <summary>Scene restart / load — close overlay without maze outcome side effects.</summary>
    public void ForceCloseForSessionReset()
    {
        _won = true;
        _runEnded = true;
        if (_overlayRoot != null)
            _overlayRoot.SetActive(false);
        if (_controlsDockRoot != null)
            _controlsDockRoot.SetActive(false);
        SetConsoleScrollVisible(true);
        if (s_ActiveOverlay == this)
            s_ActiveOverlay = null;
    }

    void HideMazeUi()
    {
        if (_overlayRoot != null)
            _overlayRoot.SetActive(false);
        if (_controlsDockRoot != null)
            _controlsDockRoot.SetActive(false);
        SetConsoleScrollVisible(true);

        DeliveryUrgencyTimer.TryResumeDeferredCountdownAfterMazeClosed();
    }

    private void Update()
    {
        if (!IsOpen || _won || _runEnded || _walkable == null)
            return;

        if (Keyboard.current == null)
            return;

        if (!TryGetSteering(out var dx, out var dy))
            return;

        var now = Time.unscaledTime;
        var firstThisFrame = DirKeysPressedThisFrame(dx, dy);
        if (firstThisFrame)
        {
            if (!TryStep(dx, dy))
            {
                _nextAutoMoveTime = now + holdRepeatInterval * 0.35f;
                return;
            }

            _nextAutoMoveTime = now + holdInitialDelay;
            return;
        }

        if (now < _nextAutoMoveTime)
            return;
        if (!TryStep(dx, dy))
        {
            _nextAutoMoveTime = now + holdRepeatInterval * 0.35f;
            return;
        }

        _nextAutoMoveTime = now + holdRepeatInterval;
    }

    static bool TryGetSteering(out int dx, out int dy)
    {
        dx = 0;
        dy = 0;
        var kb = Keyboard.current;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
            dy = -1;
        else if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
            dy = 1;
        else if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
            dx = -1;
        else if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
            dx = 1;
        return dx != 0 || dy != 0;
    }

    static bool DirKeysPressedThisFrame(int dx, int dy)
    {
        var kb = Keyboard.current;
        if (dy == -1)
            return kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
        if (dy == 1)
            return kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
        if (dx == -1)
            return kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
        if (dx == 1)
            return kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
        return false;
    }

    bool TryStep(int dx, int dy)
    {
        var nx = _px + dx;
        var ny = _py + dy;
        if (!CanMoveInto(nx, ny))
            return false;

        _px = nx;
        _py = ny;
        RefreshAllCells();
        UpdateHint(_host.GetMazeTier(), CountTrue(_obstacle), CountTrue(_bomb));

        if (_bomb[_px, _py])
        {
            OnHitBomb();
            return true;
        }

        if (_px == _gx && _py == _gy)
            OnReachedGoal();

        return true;
    }

    bool CanMoveInto(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _cols || y >= _rows)
            return false;
        if (!_walkable[x, y])
            return false;
        return !_obstacle[x, y];
    }

    private void OnHitBomb()
    {
        _runEnded = true;
        _host?.AppendConsoleLine("> CORRUPTED SECTOR — packet lost. Breach attempt failed (no progress).");
        _host?.OnMazeRoundAttemptFinished(false);
        HideMazeUi();
        if (s_ActiveOverlay == this)
            s_ActiveOverlay = null;
    }

    private void OnReachedGoal()
    {
        _won = true;
        _host?.AppendConsoleLine("> Uplink node reached — segment cleared.");
        // Apply progress (and 100% hack / good ending) before maze-outcome suspicion or bad-ending dispatch.
        _host?.ApplyMazeRoundWin();
        _host?.OnMazeRoundAttemptFinished(true);
        HideMazeUi();
        if (s_ActiveOverlay == this)
            s_ActiveOverlay = null;
    }

    private void EnsureUiBuilt()
    {
        if (_overlayRoot != null)
            return;

        _pixelSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        var font = TMP_Settings.defaultFontAsset;

        _terminalPanelRt = transform.parent as RectTransform;
        if (_terminalPanelRt == null)
            _terminalPanelRt = GetComponent<RectTransform>();

        if (_terminalPanelRt != null)
        {
            _controlsDockRoot = new GameObject("MazeControlsDock", typeof(RectTransform), typeof(Image));
            var dockRt = _controlsDockRoot.GetComponent<RectTransform>();
            dockRt.SetParent(_terminalPanelRt, false);
            var dk = _controlsDockRoot.GetComponent<Image>();
            dk.sprite = _pixelSprite;
            dk.type = Image.Type.Simple;
            dk.color = new Color32(22, 32, 45, 248);
            dk.raycastTarget = false;
            CreateControlsSection(_controlsDockRoot.transform, font, dockOutsideTerminal: true);
            _controlsDockRoot.SetActive(false);
        }

        var hostRt = GetMazeHostRect();

        _overlayRoot = new GameObject("MazeMinigameOverlay", typeof(RectTransform), typeof(Image));
        var overlayRt = _overlayRoot.GetComponent<RectTransform>();
        overlayRt.SetParent(hostRt, false);
        StretchFull(overlayRt);
        var dim = _overlayRoot.GetComponent<Image>();
        dim.sprite = _pixelSprite;
        dim.type = Image.Type.Simple;
        dim.color = new Color32(0, 0, 0, 200);
        dim.raycastTarget = true;

        var box = new GameObject("MazeBox", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        _boxRt = box.GetComponent<RectTransform>();
        _boxRt.SetParent(overlayRt, false);
        _boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        _boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        _boxRt.pivot = new Vector2(0.5f, 0.5f);
        _boxRt.sizeDelta = new Vector2(MazeBoxWidth, MazeBoxHeight);

        var boxBg = box.GetComponent<Image>();
        boxBg.sprite = _pixelSprite;
        boxBg.color = new Color32(30, 40, 55, 250);

        var v = box.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 14, 20);
        v.spacing = 12f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var titleGo = new GameObject("MazeTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        titleGo.transform.SetParent(box.transform, false);
        titleGo.GetComponent<LayoutElement>().preferredHeight = 32f;
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            titleTmp.font = font;
        titleTmp.fontSize = 28;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color32(220, 230, 240, 255);
        titleTmp.text = "Packet routing maze";

        var gridGo = new GameObject("MazeGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        gridGo.transform.SetParent(box.transform, false);
        _gridLayoutElement = gridGo.GetComponent<LayoutElement>();
        _gridLayoutElement.flexibleHeight = 1f;
        _gridLayoutElement.minHeight = GridMinHeight;
        _gridLayoutElement.preferredHeight = 480f;
        _gridRt = gridGo.GetComponent<RectTransform>();
        _gridLayout = gridGo.GetComponent<GridLayoutGroup>();
        _gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        _gridLayout.childAlignment = TextAnchor.UpperLeft;
        _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

        var btnRow = new GameObject("MazeButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        btnRow.transform.SetParent(box.transform, false);
        var btnRowLe = btnRow.GetComponent<LayoutElement>();
        btnRowLe.preferredHeight = 48f;
        btnRowLe.minHeight = 44f;
        var h = btnRow.GetComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.spacing = 12f;
        h.childForceExpandWidth = true;
        h.childControlWidth = true;
        h.padding = new RectOffset(0, 0, 0, 0);

        CreatePushButton(btnRow.transform, "Abort breach", CloseWithoutSuccess);

        CreateMazeStatusBar(box.transform, font);

        EnsureOverlayOnMazeHost();
        ApplyMazeChromeLayout();
        _overlayRoot.SetActive(false);
    }

    void CreateControlsSection(Transform boxParent, TMP_FontAsset font, bool dockOutsideTerminal = false)
    {
        var wrap = new GameObject("ControlsSection", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        wrap.transform.SetParent(boxParent, false);
        var wrapLe = wrap.GetComponent<LayoutElement>();
        if (dockOutsideTerminal)
        {
            Object.Destroy(wrapLe);
        }
        else
        {
            wrapLe.preferredHeight = 168f;
            wrapLe.minHeight = 148f;
            wrapLe.flexibleHeight = 0f;
        }

        var wrapImg = wrap.GetComponent<Image>();
        wrapImg.sprite = _pixelSprite;
        wrapImg.type = Image.Type.Simple;
        wrapImg.color = new Color32(18, 26, 38, 230);

        var v = wrap.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(16, 16, 12, 14);
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = true;
        v.childControlHeight = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = dockOutsideTerminal;

        var titleGo = new GameObject("ControlsTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        titleGo.transform.SetParent(wrap.transform, false);
        titleGo.GetComponent<LayoutElement>().preferredHeight = 30f;
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            titleTmp.font = font;
        titleTmp.fontSize = dockOutsideTerminal ? 20 : 22;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.TopLeft;
        titleTmp.color = new Color32(210, 220, 235, 255);
        titleTmp.text = "Controls";
        titleTmp.textWrappingMode = TextWrappingModes.NoWrap;

        var bodyGo = new GameObject("ControlsBody", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        bodyGo.transform.SetParent(wrap.transform, false);
        var bodyLe = bodyGo.GetComponent<LayoutElement>();
        if (dockOutsideTerminal)
        {
            bodyLe.minHeight = 80f;
            bodyLe.preferredHeight = 120f;
            bodyLe.flexibleHeight = 1f;
        }
        else
        {
            bodyLe.preferredHeight = 118f;
            bodyLe.flexibleHeight = 1f;
        }

        var bodyTmp = bodyGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            bodyTmp.font = font;
        bodyTmp.fontSize = dockOutsideTerminal ? 17 : 19;
        bodyTmp.alignment = TextAlignmentOptions.TopLeft;
        bodyTmp.color = new Color32(175, 198, 218, 255);
        bodyTmp.textWrappingMode = TextWrappingModes.Normal;
        bodyTmp.lineSpacing = 6f;
        bodyTmp.text =
            "• Move: WASD or arrow keys — hold to slide along corridors\n" +
            "• Vision: only a 3×3 area around you is lit — explore to find the uplink\n" +
            "• Goal: reach the green uplink node (hidden until you are close)\n" +
            "• Orange: blocked relay (cannot pass)\n" +
            "• Red: corrupted sector — stepping fails this run (no decryption %)\n" +
            "• Esc or Abort breach: leave maze without progress";

        if (dockOutsideTerminal)
            StretchFull(wrap.GetComponent<RectTransform>());
    }

    void CreateMazeStatusBar(Transform boxParent, TMP_FontAsset font)
    {
        var statusGo = new GameObject("MazeStatusBar", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        statusGo.transform.SetParent(boxParent, false);
        var statusLe = statusGo.GetComponent<LayoutElement>();
        statusLe.preferredHeight = MazeStatusBarHeight;
        statusLe.minHeight = MazeStatusBarHeight - 4f;
        statusLe.flexibleHeight = 0f;

        var statusBg = statusGo.GetComponent<Image>();
        statusBg.sprite = _pixelSprite;
        statusBg.type = Image.Type.Simple;
        statusBg.color = new Color32(236, 241, 248, 255);
        statusBg.raycastTarget = false;

        var hintGo = new GameObject("MazeStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintGo.transform.SetParent(statusGo.transform, false);
        var hintRt = hintGo.GetComponent<RectTransform>();
        hintRt.anchorMin = Vector2.zero;
        hintRt.anchorMax = Vector2.one;
        hintRt.offsetMin = new Vector2(12f, 4f);
        hintRt.offsetMax = new Vector2(-12f, -4f);

        _hintLabel = hintGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            _hintLabel.font = font;
        _hintLabel.fontSize = 19;
        _hintLabel.fontStyle = FontStyles.Normal;
        _hintLabel.alignment = TextAlignmentOptions.Center;
        _hintLabel.color = new Color32(16, 20, 24, 255);
        _hintLabel.textWrappingMode = TextWrappingModes.NoWrap;
        _hintLabel.overflowMode = TextOverflowModes.Ellipsis;
        _hintLabel.raycastTarget = false;
        _hintLabel.text = string.Empty;
    }

    private static TMP_Text CreateTmpLine(Transform parent, TMP_FontAsset font, float fontSize, string text)
    {
        var go = new GameObject("Line", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (font != null)
            tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color32(195, 215, 235, 255);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.text = text;
        return tmp;
    }

    private void CreatePushButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().flexibleWidth = 1f;
        go.GetComponent<LayoutElement>().preferredHeight = 52f;
        var img = go.GetComponent<Image>();
        img.sprite = _pixelSprite;
        img.color = new Color32(127, 140, 141, 255);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        StretchFull(labelGo.GetComponent<RectTransform>());
        var tmp = labelGo.GetComponent<TextMeshProUGUI>();
        var font = TMP_Settings.defaultFontAsset;
        if (font != null)
            tmp.font = font;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.text = label;
    }

    private void UpdateHint(int tier, int obstacles, int bombs)
    {
        if (_hintLabel == null || _won)
            return;
        var vision = visionRadius * 2 + 1;
        _hintLabel.text =
            $"Tier {tier} · {obstacles} blocks · {bombs} bombs — {vision}×{vision} vision. Pos ({_px},{_py}). Find the uplink.";
    }

    private void GenerateMaze()
    {
        _obstacle = null;
        _bomb = null;
        _walkable = new bool[_cols, _rows];
        for (var x = 0; x < _cols; x++)
        for (var y = 0; y < _rows; y++)
            _walkable[x, y] = false;

        var stack = new Stack<Vector2Int>();
        var start = new Vector2Int(1, 1);
        _walkable[start.x, start.y] = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            var c = stack.Peek();
            var neighbors = new List<Vector2Int>(4);
            TryAddUnvisitedAtDistance2(neighbors, c, 2, 0);
            TryAddUnvisitedAtDistance2(neighbors, c, -2, 0);
            TryAddUnvisitedAtDistance2(neighbors, c, 0, 2);
            TryAddUnvisitedAtDistance2(neighbors, c, 0, -2);

            if (neighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            var n = neighbors[Random.Range(0, neighbors.Count)];
            var wall = new Vector2Int((c.x + n.x) / 2, (c.y + n.y) / 2);
            _walkable[wall.x, wall.y] = true;
            _walkable[n.x, n.y] = true;
            stack.Push(n);
        }

        _px = 1;
        _py = 1;
        _gx = _cols - 2;
        _gy = _rows - 2;
    }

    /// <summary>Picks a random walkable uplink cell far enough from the start so the green goal is not always in the same corner.</summary>
    void PickRandomGoalCell()
    {
        var distFromStart = BfsDistances(1, 1);
        var minDist = Mathf.Clamp(minGoalDistanceFromStart, 3, Mathf.Max(4, (_cols + _rows) / 2));
        var candidates = new List<Vector2Int>();

        for (var x = 1; x < _cols - 1; x++)
        for (var y = 1; y < _rows - 1; y++)
        {
            if (!_walkable[x, y])
                continue;
            if (x == 1 && y == 1)
                continue;
            if (distFromStart[x, y] < minDist)
                continue;
            candidates.Add(new Vector2Int(x, y));
        }

        if (candidates.Count == 0)
        {
            for (var x = 1; x < _cols - 1; x++)
            for (var y = 1; y < _rows - 1; y++)
            {
                if (!_walkable[x, y] || (x == 1 && y == 1))
                    continue;
                if (distFromStart[x, y] < 0)
                    continue;
                candidates.Add(new Vector2Int(x, y));
            }
        }

        if (candidates.Count == 0)
        {
            _gx = Mathf.Clamp(_cols - 2, 1, _cols - 2);
            _gy = Mathf.Clamp(_rows - 2, 1, _rows - 2);
            return;
        }

        var goal = candidates[Random.Range(0, candidates.Count)];
        _gx = goal.x;
        _gy = goal.y;
    }

    bool IsCellVisible(int x, int y)
    {
        return Mathf.Abs(x - _px) <= visionRadius && Mathf.Abs(y - _py) <= visionRadius;
    }

    bool IsHazardAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= _cols || y >= _rows)
            return false;
        if (_obstacle != null && x < _obstacle.GetLength(0) && y < _obstacle.GetLength(1) && _obstacle[x, y])
            return true;
        if (_bomb != null && x < _bomb.GetLength(0) && y < _bomb.GetLength(1) && _bomb[x, y])
            return true;
        return false;
    }

    /// <summary>
    /// Opens extra floor cells on interior walls that already touch ≥2 corridors, turning the spanning tree into a
    /// graph with cycles so multiple routes to the goal exist.
    /// </summary>
    void CarveRandomLoopPassages()
    {
        var target = Mathf.Clamp(Mathf.RoundToInt(_cols * _rows * loopCarveDensity), 4, 85);
        var candidates = new List<Vector2Int>();
        for (var x = 1; x < _cols - 1; x++)
        for (var y = 1; y < _rows - 1; y++)
        {
            if (_walkable[x, y])
                continue;
            if (CountWalkableNeighbors4(x, y) < 2)
                continue;
            candidates.Add(new Vector2Int(x, y));
        }

        Shuffle(candidates);
        var carved = 0;
        foreach (var c in candidates)
        {
            if (carved >= target)
                break;
            if (_walkable[c.x, c.y])
                continue;
            _walkable[c.x, c.y] = true;
            carved++;
        }
    }

    static int CountWalkableNeighbors4(bool[,] walkable, int cols, int rows, int x, int y)
    {
        var n = 0;
        foreach (var d in CardinalDirs)
        {
            var nx = x + d.x;
            var ny = y + d.y;
            if (nx < 0 || ny < 0 || nx >= cols || ny >= rows)
                continue;
            if (walkable[nx, ny])
                n++;
        }

        return n;
    }

    int CountWalkableNeighbors4(int x, int y) => CountWalkableNeighbors4(_walkable, _cols, _rows, x, y);

    private void TryAddUnvisitedAtDistance2(List<Vector2Int> list, Vector2Int c, int dx, int dy)
    {
        var nx = c.x + dx;
        var ny = c.y + dy;
        if (nx <= 0 || ny <= 0 || nx >= _cols - 1 || ny >= _rows - 1)
            return;
        if (_walkable[nx, ny])
            return;
        list.Add(new Vector2Int(nx, ny));
    }

    private void PlaceHazards(int obstacleTarget, int bombTarget)
    {
        _obstacle = new bool[_cols, _rows];
        _bomb = new bool[_cols, _rows];

        for (var attempt = 0; attempt < 64; attempt++)
        {
            ClearHazardFlags();
            if (obstacleTarget > 0 && !TryPlaceObstacles(obstacleTarget))
                continue;
            if (bombTarget > 0 && !TryPlaceBombs(bombTarget))
                continue;
            if (!PathExistsForRouting())
                continue;
            return;
        }

        ClearHazardFlags();
    }

    private void ClearHazardFlags()
    {
        if (_obstacle == null || _bomb == null)
            return;
        for (var x = 0; x < _cols; x++)
        for (var y = 0; y < _rows; y++)
        {
            _obstacle[x, y] = false;
            _bomb[x, y] = false;
        }
    }

    private bool TryPlaceObstacles(int target)
    {
        for (var p = 0; p < target; p++)
        {
            var options = BuildBlendedRouteCandidates();
            var placed = false;
            foreach (var c in options)
            {
                _obstacle[c.x, c.y] = true;
                if (PathExistsForRouting())
                {
                    placed = true;
                    break;
                }

                _obstacle[c.x, c.y] = false;
            }

            if (!placed)
                return false;
        }

        return true;
    }

    private bool TryPlaceBombs(int target)
    {
        for (var p = 0; p < target; p++)
        {
            var options = BuildBlendedRouteCandidates();
            var placed = false;
            foreach (var c in options)
            {
                _bomb[c.x, c.y] = true;
                if (PathExistsForRouting())
                {
                    placed = true;
                    break;
                }

                _bomb[c.x, c.y] = false;
            }

            if (!placed)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Stochastic merge of shortest-route cells vs longer-route cells so hazards sit on competing paths, not one
    /// obvious geodesic ribbon toward the goal.
    /// </summary>
    List<Vector2Int> BuildBlendedRouteCandidates()
    {
        var distS = BfsDistances(1, 1);
        var distG = BfsDistances(_gx, _gy);
        var shortest = distS[_gx, _gy];
        var corridor = new List<Vector2Int>();
        var other = new List<Vector2Int>();

        if (shortest < 0)
            return BuildFloorCandidateListShuffled();

        for (var x = 0; x < _cols; x++)
        for (var y = 0; y < _rows; y++)
        {
            if (!_walkable[x, y])
                continue;
            if (x == 1 && y == 1)
                continue;
            if (x == _gx && y == _gy)
                continue;
            if (_obstacle[x, y] || _bomb[x, y])
                continue;

            var ds = distS[x, y];
            var dg = distG[x, y];
            if (ds < 0 || dg < 0)
                other.Add(new Vector2Int(x, y));
            else if (ds + dg == shortest)
                corridor.Add(new Vector2Int(x, y));
            else
                other.Add(new Vector2Int(x, y));
        }

        Shuffle(corridor);
        Shuffle(other);

        var merged = new List<Vector2Int>(corridor.Count + other.Count);
        var i = 0;
        var j = 0;
        while (i < corridor.Count || j < other.Count)
        {
            var canCorridor = i < corridor.Count;
            var canOther = j < other.Count;
            if (!canCorridor)
                merged.Add(other[j++]);
            else if (!canOther)
                merged.Add(corridor[i++]);
            else if (Random.value < corridorHazardBias)
                merged.Add(corridor[i++]);
            else
                merged.Add(other[j++]);
        }

        return merged;
    }

    List<Vector2Int> BuildFloorCandidateListShuffled()
    {
        var list = new List<Vector2Int>();
        for (var x = 0; x < _cols; x++)
        for (var y = 0; y < _rows; y++)
        {
            if (!_walkable[x, y])
                continue;
            if (x == 1 && y == 1)
                continue;
            if (x == _gx && y == _gy)
                continue;
            if (_obstacle[x, y] || _bomb[x, y])
                continue;
            list.Add(new Vector2Int(x, y));
        }

        Shuffle(list);
        return list;
    }

    int[,] BfsDistances(int sx, int sy)
    {
        var dist = new int[_cols, _rows];
        for (var x = 0; x < _cols; x++)
        for (var y = 0; y < _rows; y++)
            dist[x, y] = -1;

        if (!_walkable[sx, sy] || IsHazardAt(sx, sy))
            return dist;

        var q = new Queue<Vector2Int>();
        dist[sx, sy] = 0;
        q.Enqueue(new Vector2Int(sx, sy));

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            var d = dist[c.x, c.y];
            foreach (var off in CardinalDirs)
            {
                var nx = c.x + off.x;
                var ny = c.y + off.y;
                if (nx < 0 || ny < 0 || nx >= _cols || ny >= _rows)
                    continue;
                if (!_walkable[nx, ny] || IsHazardAt(nx, ny))
                    continue;
                if (dist[nx, ny] >= 0)
                    continue;
                dist[nx, ny] = d + 1;
                q.Enqueue(new Vector2Int(nx, ny));
            }
        }

        return dist;
    }

    private static void Shuffle(List<Vector2Int> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private bool PathExistsForRouting()
    {
        var visited = new bool[_cols, _rows];
        var q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(1, 1));
        visited[1, 1] = true;
        var goal = new Vector2Int(_gx, _gy);

        while (q.Count > 0)
        {
            var c = q.Dequeue();
            if (c.x == goal.x && c.y == goal.y)
                return true;

            foreach (var d in CardinalDirs)
            {
                var nx = c.x + d.x;
                var ny = c.y + d.y;
                if (nx < 0 || ny < 0 || nx >= _cols || ny >= _rows)
                    continue;
                if (!_walkable[nx, ny] || IsHazardAt(nx, ny))
                    continue;
                if (visited[nx, ny])
                    continue;
                visited[nx, ny] = true;
                q.Enqueue(new Vector2Int(nx, ny));
            }
        }

        return false;
    }

    private static int CountTrue(bool[,] m)
    {
        if (m == null)
            return 0;
        var w = m.GetLength(0);
        var h = m.GetLength(1);
        var n = 0;
        for (var x = 0; x < w; x++)
        for (var y = 0; y < h; y++)
        {
            if (m[x, y])
                n++;
        }

        return n;
    }

    private void BuildOrResizeCellGrid()
    {
        var count = _cols * _rows;
        while (_cellImages.Count < count)
        {
            var cell = new GameObject("Cell", typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(_gridRt, false);
            var img = cell.GetComponent<Image>();
            img.sprite = _pixelSprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            _cellImages.Add(img);
        }

        for (var i = count; i < _cellImages.Count; i++)
            _cellImages[i].gameObject.SetActive(false);

        _gridLayout.constraintCount = _cols;

        Canvas.ForceUpdateCanvases();
        var w = _gridRt.rect.width;
        var h = _gridRt.rect.height;
        if (w <= 1f || h <= 1f)
        {
            var host = GetMazeHostRect();
            if (host != null && host.rect.height > 1f)
            {
                w = Mathf.Max(400f, host.rect.width - 48f);
                h = Mathf.Max(320f, host.rect.height - MazeChromeVerticalReserve - 48f);
            }
            else
            {
                w = 820f;
                h = 620f;
            }
        }

        var cellW = w / _cols;
        var cellH = h / _rows;
        var side = Mathf.Max(7f, Mathf.Floor(Mathf.Min(cellW, cellH)));
        _gridLayout.cellSize = new Vector2(side, side);
        _gridLayout.spacing = Vector2.zero;
        _gridLayout.padding = new RectOffset(0, 0, 0, 0);

        for (var i = 0; i < count; i++)
        {
            _cellImages[i].gameObject.SetActive(true);
            var rt = _cellImages[i].GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(side, side);
        }
    }

    private void RefreshAllCells()
    {
        for (var y = 0; y < _rows; y++)
        for (var x = 0; x < _cols; x++)
        {
            var idx = y * _cols + x;
            var img = _cellImages[idx];
            img.color = IsCellVisible(x, y) ? GetRevealedCellColor(x, y) : fogColor;
        }
    }

    Color32 GetRevealedCellColor(int x, int y)
    {
        if (x == _px && y == _py)
            return playerColor;
        if (x == _gx && y == _gy)
            return goalColor;
        if (!_walkable[x, y])
            return wallColor;
        if (_obstacle != null && _obstacle[x, y])
            return obstacleColor;
        if (_bomb != null && _bomb[x, y])
            return bombColor;
        return floorColor;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private void OnDestroy()
    {
        if (_deferredGridLayoutRefresh != null)
        {
            StopCoroutine(_deferredGridLayoutRefresh);
            _deferredGridLayoutRefresh = null;
        }

        if (s_ActiveOverlay == this)
            s_ActiveOverlay = null;
        if (_pixelSprite != null)
            Destroy(_pixelSprite);
        if (_controlsDockRoot != null)
            Destroy(_controlsDockRoot);
    }
}
