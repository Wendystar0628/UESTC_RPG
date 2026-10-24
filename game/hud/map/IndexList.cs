using Campus.Contracts.Atlas;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Map;

/// <summary>按区分组的地点目录。行由布局容器排版，Tab 按阅读顺序经过每个地点。</summary>
public partial class IndexList : Control
{
    private readonly MapHudContext _ctx;
    private readonly ScrollContainer _scroll;
    private readonly VBoxContainer _stack;
    private readonly List<(MapEntry Entry, TechButton Button)> _rows = [];
    private int _selected = -1;

    public IndexList(MapHudContext ctx)
    {
        _ctx = ctx;
        FocusMode = FocusModeEnum.None;
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AccessibilityName = ctx.T("map.index.title");
        _scroll = ctx.Ui.Layout.Scroll(this);
        _scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _stack = ctx.Ui.Layout.Stack(_scroll, groups: false);
    }

    public event Action<MapEntry>? SelectionChanged;
    public event Action<MapEntry>? Activated;
    /// <summary>指针进入某行时给出该行，离开时给 null。</summary>
    public event Action<MapEntry?>? Hovered;
    public ISet<MarkerId> Here { get; } = new HashSet<MarkerId>();
    public MapEntry? SelectedEntry => _selected >= 0 && _selected < _rows.Count ? _rows[_selected].Entry : null;
    public bool OwnsFocus => _rows.Any(row => row.Button.HasFocus());

    public void FocusFirst()
    {
        if (_rows.Count == 0) return;
        var i = _selected >= 0 ? _selected : 0;
        _rows[i].Button.GrabFocus();
    }

    public void SetEntries(IReadOnlyList<MapEntry> entries)
    {
        var keep = SelectedEntry?.Id;
        foreach (var child in _stack.GetChildren()) child.QueueFree();
        _rows.Clear();
        var groups = entries.Where(e => e.Travelable)
            .GroupBy(e => e.Group).OrderBy(g => g.Key is { } key ? (int)key : int.MaxValue);
        foreach (var group in groups)
        {
            var title = group.Key is { } key ? MapHudContext.GroupName(key) : _ctx.T("map.index.other");
            _ctx.Ui.Text(_stack, title, "mono-caps", "text.2");
            foreach (var entry in group.OrderBy(KindOrder).ThenBy(e => e.Id.Value, StringComparer.Ordinal))
                AddEntry(entry);
        }
        _selected = keep is { } id ? _rows.FindIndex(row => row.Entry.Id == id) : -1;
        if (_rows.Count > 0)
        {
            // 方向键用原生焦点移动；首末行的上、下邻居指向自身，到头不外溢。
            _rows[0].Button.FocusNeighborTop = new NodePath(".");
            _rows[^1].Button.FocusNeighborBottom = new NodePath(".");
        }
        Refresh();
    }

    private void AddEntry(MapEntry entry)
    {
        var index = _rows.Count;
        var button = _ctx.Ui.Layout.ListRow(_stack, RowText(entry));
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.ClipText = true;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        button.TooltipText = $"{_ctx.Name(entry.Name)} · {entry.Id.Value}";
        button.AccessibilityName = button.TooltipText;
        button.MouseEntered += () => Hovered?.Invoke(entry);
        button.MouseExited += () => Hovered?.Invoke(null);
        button.FocusEntered += () => SetSelected(index, notify: true);
        button.Pressed += () =>
        {
            SetSelected(index, notify: true);
            Activated?.Invoke(entry);
        };
        _rows.Add((entry, button));
    }

    private string RowText(MapEntry entry)
    {
        var state = Here.Contains(entry.Id) ? _ctx.T("map.index.here")
            : entry.FlyOnly ? _ctx.T("map.index.flyOnly") : "";
        return string.IsNullOrEmpty(state)
            ? $"{_ctx.Name(entry.Name)}  ·  {entry.Id.Value}"
            : $"{_ctx.Name(entry.Name)}  ·  {entry.Id.Value}  ·  {state}";
    }

    private static int KindOrder(MapEntry e) => e.Marker.Kind switch
    {
        MarkerKind.Anchor => 0,
        MarkerKind.Viewpoint => 1,
        MarkerKind.Interior => 2,
        MarkerKind.Route => 3,
        MarkerKind.Landmark or MarkerKind.Facility or MarkerKind.Custom or MarkerKind.Quest => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(e)),
    };

    public void Select(MarkerId id)
    {
        var i = _rows.FindIndex(row => row.Entry.Id == id);
        if (i >= 0) SetSelected(i, notify: false);
    }

    public void SelectFirst()
    {
        if (_selected < 0 && _rows.Count > 0) SetSelected(0, notify: true);
    }

    private void SetSelected(int index, bool notify)
    {
        if (index == _selected) return;
        _selected = index;
        Refresh();
        _scroll.EnsureControlVisible(_rows[index].Button);
        if (notify) SelectionChanged?.Invoke(_rows[index].Entry);
    }

    /// <summary>行文字与选中态按当前集合（含"此处"）重排；集合变了由调用方显式调用。</summary>
    public void Refresh()
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var (entry, button) = _rows[i];
            var text = RowText(entry);
            if (button.Text != text) button.Text = text;
            button.Selected = i == _selected;
        }
    }
}
