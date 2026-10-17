using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>
/// 存档页（UI-VISUAL、SYS-SAVE）：全屏大页，随主题，背后 scrim；三个存档位画成"存储模组"卡（400×212，切角 10，底边 16 根接点）。
/// 只读 ISaveService 的存档位列表和缩略图；写入只经 IAppFlow（开始页里"继续 / 读取 / 新游戏"）。打开期间 LockInput(Menu)。
/// </summary>
public partial class SavePage : Control
{
    private readonly List<(TechButton Hit, Control Card)> _cards = [];
    private UiKit _kit = null!;
    private Control _panel = null!;
    private TechButton _primary = null!;
    private Label _detail = null!;
    private IDisposable? _lock;
    private int _selected;
    private bool _fromTitle;

    public event Action? CloseRequested;

    private static T Svc<T>() where T : class => Campus.Kernel.Registry.Kernel.Get<T>();

    public static SavePage Create(UiKit kit, Control parent)
    {
        var page = new SavePage { _kit = kit, MouseFilter = MouseFilterEnum.Stop, Visible = false };
        parent.AddChild(page);
        page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return page;
    }

    public void Open(bool fromTitle)
    {
        _fromTitle = fromTitle;
        Rebuild();
        Visible = true;
        _lock ??= TryLock();
        Modulate = new Color(1, 1, 1, 0);
        CreateTween().TweenProperty(this, "modulate:a", 1f, _kit.Motion.Fade(0.36f));
        if (_cards.Count > 0) _cards[_selected].Hit.GrabFocus();
    }

    /// <summary>关页即释放内容（隐藏的件照样占实例参数槽，<see cref="UiKit.Holders"/>），下次打开照常重建。</summary>
    public void Close()
    {
        Visible = false;
        _lock?.Dispose();
        _lock = null;
        foreach (var child in GetChildren()) child.QueueFree();
        _cards.Clear();
    }

    private static IDisposable? TryLock()
    {
        try { return Svc<ICameraRig>().LockInput(InputLockReason.Menu); }
        catch (InvalidOperationException) { return null; }
    }

    private void Rebuild()
    {
        foreach (var child in GetChildren()) child.QueueFree();
        _cards.Clear();
        var view = GetViewportRect().Size;
        _kit.Box(this, new Rect2(Vector2.Zero, view), new UiFx(Fill: "scrim", Margin: 0));
        var size = new Vector2(Math.Min(1280, view.X - 128), 680);
        _panel = new Control { Position = ((view - size) / 2).Round(), Size = size, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_panel);
        _kit.Box(_panel, new Rect2(Vector2.Zero, size), new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: _kit.Tokens.Chamfer("page"),
            Flags: UiFlags.Highlight | UiFlags.Via));
        _kit.Text(_panel, UiKit.T("saves.title"), "h2", "text.1", new Vector2(32, 26));
        _kit.Text(_panel, "SAVES", "mono-caps", "text.2", new Vector2(32 + 62, 38));
        _kit.Button(_panel, ButtonStyle.Icon, "", new Rect2(size.X - 32 - 32, 24, 32, 32), padX: 4, icon: "close", iconMax: 20,
            pressed: () => CloseRequested?.Invoke());

        IReadOnlyList<SlotInfo> slots;
        SlotId? latest = null;
        try { var s = Svc<ISaveService>(); slots = s.Slots; latest = s.Continue; }
        catch (InvalidOperationException) { slots = []; }
        var card = _kit.Tokens.V2("metrics", "size", "memoryCard");
        var gap = 24f;
        var x0 = (size.X - (card.X * 3 + gap * 2)) / 2;
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var at = new Vector2(x0 + i * (card.X + gap), 96);
            var holder = new Control { Position = at, Size = card, MouseFilter = MouseFilterEnum.Ignore };
            _panel.AddChild(holder);
            var index = i;
            var hit = _kit.Button(holder, ButtonStyle.Ghost, "", new Rect2(Vector2.Zero, card));
            hit.SelfModulate = new Color(1, 1, 1, 0);
            hit.Pressed += () => Select(index);
            hit.FocusEntered += () => Select(index);
            _cards.Add((hit, holder));
            BuildCard(holder, slot, slot.Slot == latest, false);
        }
        _detail = _kit.Text(_panel, "", "small", "text.2", new Vector2(x0, 96 + card.Y + 32), width: size.X - x0 * 2, wrap: true);
        _primary = _kit.Button(_panel, ButtonStyle.Primary, "", new Rect2(size.X - x0 - 152, size.Y - 32 - 44, 152, 44), "button.primary", 20,
            pressed: () => _ = Act());
        _kit.Button(_panel, ButtonStyle.Secondary, UiKit.T("ui.common.back"), new Rect2(x0, size.Y - 32 - 40, 132, 40), pressed: () => CloseRequested?.Invoke());
        _selected = Math.Clamp(slots.ToList().FindIndex(s => s.Slot == latest), 0, Math.Max(0, slots.Count - 1));
        if (slots.Count > 0) Select(_selected);
    }

    private void BuildCard(Control holder, SlotInfo slot, bool latest, bool selected)
    {
        foreach (var child in holder.GetChildren())
            if (child is not TechButton) child.QueueFree();
        var size = holder.Size;
        var empty = slot.Health == SlotHealth.Empty || slot.Meta is null;
        var corrupt = slot.Health is SlotHealth.Corrupt or SlotHealth.NewerBuild;
        _kit.Box(holder, new Rect2(Vector2.Zero, size), new UiFx(Fill: empty ? null : "surface.solid", Line: selected ? "primary.line" : empty ? "line.control" : "line.panel",
            Chamfer: _kit.Tokens.Chamfer("card"), Flags: (empty ? UiFlags.Dashed : 0) | (selected ? UiFlags.SelectedIris : 0)));
        _kit.Text(holder, $"SLOT {slot.Slot.Value.TrimStart('s').PadLeft(2, '0')}", "mono-caps", "text.2", new Vector2(24, 16));
        if (latest)
        {
            var tag = _kit.Text(holder, UiKit.T("saves.latest"), "small", "text.accent", new Vector2(size.X - 70, 12));
            _kit.Dot(holder, new Vector2(size.X - 80, 22), 6, new UiFx(Fill: "led.on"));
            tag.Position = new Vector2(size.X - 24 - _kit.Width(tag), 12);
        }
        for (var i = 0; i < 16; i++)
            _kit.Box(holder, new Rect2(24 + i * 11, size.Y - 26, 7, 12), new UiFx(Fill: selected ? "primary.fill" : null, Line: selected ? "primary.line" : "line.hair"));
        if (empty)
        {
            _kit.Text(holder, UiKit.T("saves.empty"), "body", "text.2", new Vector2(0, size.Y / 2 - 14), width: size.X, align: HorizontalAlignment.Center);
            return;
        }
        var thumb = new Rect2(24, 44, 176, 99);
        _kit.Box(holder, thumb, new UiFx(Fill: corrupt ? "danger.fill" : "surface.sunk", Line: "line.hair"));
        if (!corrupt) TryThumbnail(holder, slot.Slot, thumb);
        var meta = slot.Meta!;
        _kit.Text(holder, corrupt ? UiKit.T("saves.corrupt") : Place(meta), "h3", corrupt ? "danger.text" : "text.1", new Vector2(216, 44));
        _kit.Text(holder, Summary(meta), "small", "text.2", new Vector2(216, 74));
        _kit.Text(holder, PlayTime(meta.PlaySeconds), "data", "text.2", new Vector2(216, 98));
        _kit.Text(holder, SavedAt(meta.SavedAtUnix), "data", "text.2", new Vector2(216, 120));
    }

    private void TryThumbnail(Control holder, SlotId slot, Rect2 rect)
    {
        byte[]? bytes;
        try { bytes = Svc<ISaveService>().Thumbnail(slot); }
        catch (InvalidOperationException) { return; }
        if (bytes is null || bytes.Length == 0) return;
        var image = new Image();
        if (image.LoadWebpFromBuffer(bytes) != Error.Ok) return;
        holder.AddChild(new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(image),
            Position = rect.Position,
            Size = rect.Size,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore
        });
    }

    private void Select(int index)
    {
        IReadOnlyList<SlotInfo> slots;
        SlotId? latest;
        try { var s = Svc<ISaveService>(); slots = s.Slots; latest = s.Continue; }
        catch (InvalidOperationException) { return; }
        _selected = index;
        for (var i = 0; i < _cards.Count && i < slots.Count; i++) BuildCard(_cards[i].Card, slots[i], slots[i].Slot == latest, i == index);
        var slot = slots[index];
        var empty = slot.Meta is null;
        _primary.Text = UiKit.T(empty ? "saves.new_here" : "saves.load");
        _primary.OffReason = UiKit.T("saves.only_from_title");
        _primary.Off = !_fromTitle || slot.Health is SlotHealth.Corrupt or SlotHealth.NewerBuild;
        _detail.Text = slot.Health switch
        {
            SlotHealth.Empty => UiKit.T("saves.detail.empty"),
            SlotHealth.Ok => UiKit.T("saves.detail.ok", ("gens", $"{slot.Generations}")),
            SlotHealth.RecoveredOlderGen => UiKit.T("saves.detail.recovered"),
            SlotHealth.Corrupt => UiKit.T("saves.detail.corrupt"),
            SlotHealth.NewerBuild => UiKit.T("saves.detail.newer"),
            _ => throw new InvalidOperationException("Unknown slot health."),
        };
    }

    private async Task Act()
    {
        if (!_fromTitle) return;
        var slots = Svc<ISaveService>().Slots;
        var slot = slots[_selected];
        var request = slot.Meta is null ? new EnterRequest(EnterKind.NewGame, slot.Slot) : new EnterRequest(EnterKind.LoadSlot, slot.Slot);
        CloseRequested?.Invoke();
        await Svc<IAppFlow>().Enter(request);
    }

    private static string Place(SaveMeta meta) =>
        meta.PlaceKey is { } key && Campus.Hud.I18n.Tr.Has(key) ? UiKit.T(key) : UiKit.T("saves.place.unknown");

    public static string Summary(SaveMeta meta) =>
        $"{UiKit.T("hud.season." + TimeCard.SeasonCode(meta.Season).ToLowerInvariant())} · {(int)meta.Clock:00}:{(int)(meta.Clock % 1 * 60):00} · " +
        UiKit.T("weather.cell." + WeatherStrip.CellKey(WeatherStrip.CellOf(meta.Weather)));

    private static string PlayTime(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }

    private static string SavedAt(long unix) =>
        unix <= 0 ? "—" : DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}
