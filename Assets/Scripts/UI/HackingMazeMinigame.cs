using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Procedural maze breach sim: difficulty scales with <see cref="HackingTerminalPanel.GetMazeTier"/>.
/// Hazards prefer cells on shortest routes so you must detour. Hold movement keys to slide along corridors.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class HackingMazeMinigame : MonoBehaviour
{
    const int MinDim = 7;
    const int MaxDim = 25;

    const float MazeBoxWidth = 680f;
    const float MazeBoxHeight = 560f;
    const float GridMinHeight = 320f;
    const float GridPreferredHeight = 420f;

    static readonly Vector2Int[] CardinalDirs =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
    };

    [SerializeField, Range(MinDim, MaxDim)] private int mazeColumns = 15;
    [SerializeField, Range(MinDim, MaxDim)] private int mazeRows = 13;

    [Header("Movement (hold keys)")]
    [Tooltip("After first step, seconds before auto-repeat starts.")]
    [SerializeField, Range(0.05f, 0.5f)] private float holdInitialDelay = 0.14f;
    [Tooltip("Seconds between steps while a direction key is held.")]
    [SerializeField, Range(0.03f, 0.3f)] private float holdRepeatInterval = 0.07f;

    [Header("Colours")]
    [SerializeField] private Color32 wallColor = new(25, 35, 50, 255);
    [SerializeField] private Color32 floorColor = new(52, 73, 94, 255);
    [SerializeField] private Color32 playerColor = new(52, 152, 219, 255);
    [SerializeField] private Color32 goalColor = new(39, 174, 96, 255);
    [SerializeField] private Color32 obstacleColor = new(211, 84, 0, 255);
    [SerializeField] private Color32 bombColor = new(192, 57, 43, 255);

    private HackingTerminalPanel _host;
    private GameObject _overlayRoot;
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
        ApplyMazeChromeLayout();
        _overlayRoot.transform.SetAsLastSibling();
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
        PlaceHazards(obstacleCount, bombCount);

        BuildOrResizeCellGrid();
        RefreshAllCells();
        UpdateHint(tier, obstacleCount, bombCount);

        _host.AppendConsoleLine(
            $"> Breach sim tier {tier}: {_cols}×{_rows} — {obstacleCount} static blocks, {bombCount} corrupted cells (step = wipe). Hold WASD / arrows.");
    }

    void ApplyMazeChromeLayout()
    {
        if (_boxRt != null)
            _boxRt.sizeDelta = new Vector2(MazeBoxWidth, MazeBoxHeight);
        if (_gridLayoutElement != null)
        {
            _gridLayoutElement.minHeight = GridMinHeight;
            _gridLayoutElement.preferredHeight = GridPreferredHeight;
        }
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
        if (_overlayRoot != null)
            _overlayRoot.SetActive(false);
        if (s_ActiveOverlay == this)
            s_ActiveOverlay = null;
        _host?.AppendConsoleLine("> Breach sim aborted.");
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
        _host.AppendConsoleLine("> CORRUPTED SECTOR — packet lost. Breach attempt failed (no progress).");
        if (_overlayRoot != null)
            _overlayRoot.SetActive(false);
        if (s_ActiveOverlay == this)
            s_ActiveOverlay = null;
    }

    private void OnReachedGoal()
    {
        _won = true;
        _host.AppendConsoleLine("> Uplink node reached — segment cleared.");
        if (_overlayRoot != null)
            _overlayRoot.SetActive(false);
        if (s_ActiveOverlay == this)
            s_ActiveOverlay = null;
        _host.ApplyMazeRoundWin();
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

        var panelRt = GetComponent<RectTransform>();

        _overlayRoot = new GameObject("MazeMinigameOverlay", typeof(RectTransform), typeof(Image));
        var overlayRt = _overlayRoot.GetComponent<RectTransform>();
        overlayRt.SetParent(panelRt, false);
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
        v.padding = new RectOffset(12, 12, 12, 12);
        v.spacing = 8f;
        v.childAlignment = TextAnchor.UpperCenter;
        v.childControlHeight = true;
        v.childControlWidth = true;
        v.childForceExpandWidth = true;
        v.childForceExpandHeight = false;

        var font = TMP_Settings.defaultFontAsset;
        var titleGo = new GameObject("MazeTitle", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        titleGo.transform.SetParent(box.transform, false);
        titleGo.GetComponent<LayoutElement>().preferredHeight = 28f;
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        if (font != null)
            titleTmp.font = font;
        titleTmp.fontSize = 18;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = new Color32(220, 230, 240, 255);
        titleTmp.text = "Packet routing maze";

        _hintLabel = CreateTmpLine(box.transform, font, 22f, "Hold WASD / arrows. Red = bomb (fail). Orange = block — hazards favor shortest routes.");
        _hintLabel.gameObject.GetComponent<LayoutElement>().preferredHeight = 40f;

        var gridGo = new GameObject("MazeGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        gridGo.transform.SetParent(box.transform, false);
        _gridLayoutElement = gridGo.GetComponent<LayoutElement>();
        _gridLayoutElement.flexibleHeight = 1f;
        _gridLayoutElement.minHeight = GridMinHeight;
        _gridLayoutElement.preferredHeight = GridPreferredHeight;
        _gridRt = gridGo.GetComponent<RectTransform>();
        _gridLayout = gridGo.GetComponent<GridLayoutGroup>();
        _gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        _gridLayout.childAlignment = TextAnchor.UpperLeft;
        _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

        var btnRow = new GameObject("MazeButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        btnRow.transform.SetParent(box.transform, false);
        btnRow.GetComponent<LayoutElement>().preferredHeight = 40f;
        var h = btnRow.GetComponent<HorizontalLayoutGroup>();
        h.childAlignment = TextAnchor.MiddleCenter;
        h.spacing = 12f;
        h.childForceExpandWidth = true;
        h.childControlWidth = true;
        h.padding = new RectOffset(0, 0, 0, 0);

        CreatePushButton(btnRow.transform, "Abort breach", CloseWithoutSuccess);

        _overlayRoot.SetActive(false);
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
        tmp.color = new Color32(180, 200, 220, 255);
        tmp.enableWordWrapping = true;
        tmp.text = text;
        return tmp;
    }

    private void CreatePushButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().flexibleWidth = 1f;
        go.GetComponent<LayoutElement>().preferredHeight = 40f;
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
        tmp.fontSize = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = label;
    }

    private void UpdateHint(int tier, int obstacles, int bombs)
    {
        if (_hintLabel == null || _won)
            return;
        _hintLabel.text =
            $"Tier {tier} · {obstacles} blocks · {bombs} bombs — reach green. Pos ({_px},{_py}). Escape aborts.";
    }

    private void GenerateMaze()
    {
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

        _gx = _cols - 2;
        _gy = _rows - 2;
        _px = 1;
        _py = 1;
    }

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
            var options = BuildCorridorFirstCandidates();
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
            var options = BuildCorridorFirstCandidates();
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
    /// Shortest-path corridor cells first (where you are forced to consider detours), then other floor tiles.
    /// </summary>
    List<Vector2Int> BuildCorridorFirstCandidates()
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
        corridor.AddRange(other);
        return corridor;
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

        if (!_walkable[sx, sy] || _obstacle[sx, sy] || _bomb[sx, sy])
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
                if (!_walkable[nx, ny] || _obstacle[nx, ny] || _bomb[nx, ny])
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
                if (!_walkable[nx, ny] || _obstacle[nx, ny] || _bomb[nx, ny])
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
            w = 520f;
            h = 380f;
        }

        var cellW = w / _cols;
        var cellH = h / _rows;
        var side = Mathf.Max(4f, Mathf.Floor(Mathf.Min(cellW, cellH)));
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
            Color32 c;
            if (x == _px && y == _py)
                c = playerColor;
            else if (x == _gx && y == _gy)
                c = goalColor;
            else if (!_walkable[x, y])
                c = wallColor;
            else if (_obstacle[x, y])
                c = obstacleColor;
            else if (_bomb[x, y])
                c = bombColor;
            else
                c = floorColor;
            img.color = c;
        }
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
        if (s_ActiveOverlay == this)
            s_ActiveOverlay = null;
        if (_pixelSprite != null)
            Destroy(_pixelSprite);
    }
}
