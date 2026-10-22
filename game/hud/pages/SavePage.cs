using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>浏览存档，或由开始页选择新游戏覆盖的位置。</summary>
public enum SaveIntent : byte { Browse, NewGame }

/// <summary>居中的可滚动存档页；卡片随安全框宽度换列，覆盖确认进入独立对话框宿主。</summary>
public partial class SavePage : Control
{
    private sealed record SaveCard(TechButton Hit, PanelContainer Panel, List<Panel> Contacts, bool Empty);
    private readonly List<SaveCard> _cards = [];
    private UiKit _kit = null!;
    private Control _dialogParent = null!;
    private TechButton _primary = null!;
    private TechButton? _overwrite;
    private Control? _confirm;
    private Label _detail = null!;
    private IDisposable? _lock;
    private Tween? _fade;
    private int _selected;
    private bool _fromTitle;
    private bool _reported;
    private SaveIntent _intent;

    public event Action? CloseRequested;
    private static T Svc<T>() where T : class => Campus.Kernel.Registry.Kernel.Get<T>();

    public static SavePage Create(UiKit kit, Control parent, Control dialogParent)
    {
        var page = new SavePage { _kit = kit, _dialogParent = dialogParent, MouseFilter = MouseFilterEnum.Stop, Visible = false };
        parent.AddChild(page);
        page.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return page;
    }

    public void Open(bool fromTitle, SaveIntent intent = SaveIntent.Browse)
    {
        _fromTitle = fromTitle;
        _intent = intent;
        ReleaseContent();
        Build();
        Visible = true;
        _lock ??= Svc<ICameraRig>().LockInput(InputLockReason.Menu);
        Modulate = new Color(1, 1, 1, 0);
        _fade = CreateTween();
        _fade.TweenProperty(this, "modulate:a", 1f, _kit.Motion.Fade(0.36f));
        if (_cards.Count > 0) FocusWhenInteractive(_cards[_selected].Hit);
        PublishOpen();
    }

    /// <summary>大页关上立即移出树并排队释放，不留下隐藏的 UiKit.Holders。</summary>
    public void Close()
    {
        Visible = false;
        _lock?.Dispose();
        _lock = null;
        ReleaseContent();
        PublishOpen();
    }

    // 上层（界面根、--ui=0）显隐变化也会改变存档页是否真的看得见，随之重报。
    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged) PublishOpen();
    }

    /// <summary>存档页真实可见才报开着（IUiSurfaceSink），不可见或已离树即撤下；值不变不报。</summary>
    private void PublishOpen()
    {
        var open = IsInsideTree() && IsVisibleInTree();
        if (open == _reported) return;
        _reported = open;
        Svc<IUiSurfaceSink>().Report(UiSurface.SavePage, open);
    }

    private void ReleaseContent()
    {
        _fade?.Kill();
        _fade = null;
        ReleaseConfirm();
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _cards.Clear();
        _overwrite = null;
    }

    private void Build()
    {
        var scrim = _kit.Box(this, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var frame = _kit.Layout.Page(this, UiKit.T(_intent == SaveIntent.NewGame ? "saves.title.new_game" : "saves.title"), "savesWidth", centered: true);
        frame.Tabs.Visible = false;
        _kit.Text(frame.Header, "SAVES", "mono-caps", "text.2", wrap: false).SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        _kit.Button(frame.Header, ButtonStyle.Icon, "", icon: "close", pressed: () => CloseRequested?.Invoke());

        var saves = Svc<ISaveService>();
        var slots = saves.Slots;
        var grid = _kit.Layout.Cards(frame.Body);
        for (var i = 0; i < slots.Count; i++)
        {
            var index = i;
            var card = BuildCard(grid, slots[i], slots[i].Slot == saves.Continue);
            card.Hit.Pressed += () => Select(index);
            card.Hit.FocusEntered += () => Select(index);
            _cards.Add(card);
        }
        _detail = _kit.Text(frame.Body, "", "small", "text.2");
        _kit.Button(frame.Footer, ButtonStyle.Secondary, UiKit.T("ui.common.back"), pressed: () => CloseRequested?.Invoke());
        _primary = _kit.Button(frame.Footer, ButtonStyle.Primary, "", "button.primary", pressed: () => _ = Act());
        if (_intent == SaveIntent.NewGame)
            _overwrite = _kit.Button(frame.Footer, ButtonStyle.Danger, UiKit.T("saves.overwrite"), "button.primary", pressed: ShowConfirm);
        _selected = Math.Clamp(slots.ToList().FindIndex(s => s.Slot == saves.Continue), 0, Math.Max(0, slots.Count - 1));
        if (slots.Count > 0) Select(_selected);
        else _primary.Off = true;
    }

    private SaveCard BuildCard(Control parent, SlotInfo slot, bool latest)
    {
        var empty = slot.Health == SlotHealth.Empty || slot.Meta is null;
        var corrupt = slot.Health == SlotHealth.Corrupt;
        var panel = _kit.Panel(parent, CardFx(empty, selected: false));
        var margin = _kit.Layout.Margin(panel);
        var content = _kit.Layout.Stack(margin, groups: false);
        var header = _kit.Layout.Row(content);
        _kit.Text(header, $"SLOT {slot.Slot.Value.TrimStart('s').PadLeft(2, '0')}", "mono-caps", "text.2");
        if (latest)
        {
            var lamp = _kit.Box(header, new UiFx(Fill: "led.on", Flags: UiFlags.Circle));
            var diameter = _kit.Tokens.F("metrics", "via", "core");
            lamp.CustomMinimumSize = new Vector2(diameter, diameter);
            lamp.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            _kit.Text(header, UiKit.T("saves.latest"), "small", "text.accent").SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        }
        // 所有槽共用同一内容区；空槽也保留缩略图占位和槽状态，不缩成只有接点的细条。
        var body = _kit.Layout.Row(content);
        var aspect = _kit.Layout.Add(body, new AspectRatioContainer
        {
            Ratio = 16f / 9f,
            StretchMode = AspectRatioContainer.StretchModeEnum.WidthControlsHeight,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        });
        // AspectRatioContainer 只调整孩子的比例，不把宽度推导的高度交给外层 HBox。
        // 首次排版按卡宽令牌估出内容最小高度，随后只从实际分配宽度更新，不写位置。
        var padding = margin.GetThemeConstant("margin_left") + margin.GetThemeConstant("margin_right");
        var previewWidth = Math.Max(0, (_kit.Layout.Metric("cardMinWidth") - padding - body.GetThemeConstant("separation")) / 2f);
        aspect.CustomMinimumSize = new Vector2(0, previewWidth / aspect.Ratio);
        void FitPreviewHeight()
        {
            if (aspect.Size.X > 0) aspect.CustomMinimumSize = new Vector2(0, aspect.Size.X / aspect.Ratio);
        }
        aspect.Resized += FitPreviewHeight;
        var thumbnail = _kit.Panel(aspect, new UiFx(Fill: corrupt ? "danger.fill" : "surface.sunk", Line: "line.hair"));
        if (!empty && !corrupt) TryThumbnail(thumbnail, slot.Slot);
        var data = _kit.Layout.Stack(body, groups: false);
        if (empty)
        {
            data.Alignment = BoxContainer.AlignmentMode.Center;
            _kit.Text(data, UiKit.T("saves.empty"), "body", "text.2", align: HorizontalAlignment.Center);
        }
        else
        {
            var meta = slot.Meta!;
            _kit.Text(data, corrupt ? UiKit.T("saves.corrupt") : Place(meta), "h3", corrupt ? "danger.text" : "text.1");
            _kit.Text(data, Summary(meta), "small", "text.2");
            _kit.Text(data, PlayTime(meta.PlaySeconds), "data", "text.2");
            _kit.Text(data, SavedAt(meta.SavedAtUnix), "data", "text.2");
        }
        var pins = _kit.Layout.Row(content);
        var contacts = new List<Panel>();
        for (var i = 0; i < 16; i++)
        {
            var pin = _kit.Box(pins, new UiFx(Line: "line.hair"));
            pin.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            pin.CustomMinimumSize = new Vector2(0, _kit.Layout.Metric("keycap") / 2f);
            contacts.Add(pin);
        }
        // PanelContainer 把内容和命中层叠放；空文字按钮保留原生焦点、悬停和鼠标命中。
        var hit = _kit.Button(panel, ButtonStyle.Ghost, "");
        hit.SizeFlagsHorizontal = hit.SizeFlagsVertical = SizeFlags.ExpandFill;
        hit.TooltipText = $"SLOT {slot.Slot.Value.TrimStart('s').PadLeft(2, '0')}";
        return new SaveCard(hit, panel, contacts, empty);
    }

    private UiFx CardFx(bool empty, bool selected) => new(Fill: empty ? null : "surface.solid",
        Line: selected ? "primary.line" : empty ? "line.control" : "line.panel",
        Chamfer: _kit.Tokens.Chamfer("card"), Flags: (empty ? UiFlags.Dashed : 0) | (selected ? UiFlags.SelectedIris : 0));

    private void TryThumbnail(Control parent, SlotId slot)
    {
        var bytes = Svc<ISaveService>().Thumbnail(slot);
        if (bytes is null || bytes.Length == 0) return;
        var image = new Image();
        if (image.LoadWebpFromBuffer(bytes) != Error.Ok) return;
        parent.AddChild(new TextureRect
        {
            Texture = ImageTexture.CreateFromImage(image),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore,
        });
    }

    private void Select(int index)
    {
        var slots = Svc<ISaveService>().Slots;
        _selected = index;
        for (var i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];
            var selected = i == index;
            _kit.Apply(card.Panel, CardFx(card.Empty, selected));
            card.Hit.Selected = selected;
            foreach (var pin in card.Contacts)
                _kit.Apply(pin, new UiFx(Fill: selected ? "primary.fill" : null, Line: selected ? "primary.line" : "line.hair"));
        }
        var slot = slots[index];
        var overwrite = _overwrite is not null && slot.Health != SlotHealth.Empty;
        _primary.Visible = !overwrite;
        if (_overwrite is not null) _overwrite.Visible = overwrite;
        _primary.Text = UiKit.T(slot.Meta is null ? "saves.new_here" : "saves.load");
        _primary.OffReason = UiKit.T("saves.only_from_title");
        _primary.Off = !_fromTitle || slot.Health == SlotHealth.Corrupt;
        _detail.Text = slot.Health switch
        {
            SlotHealth.Empty => UiKit.T("saves.detail.empty"),
            SlotHealth.Ok => UiKit.T(overwrite ? "saves.detail.overwrite" : "saves.detail.ok", ("gens", $"{slot.Generations}")),
            SlotHealth.Corrupt => UiKit.T(overwrite ? "saves.detail.corrupt_overwrite" : "saves.detail.corrupt"),
            _ => throw new InvalidOperationException("Unknown slot health."),
        };
    }

    private void ShowConfirm()
    {
        if (_confirm is not null || !_fromTitle) return;
        var slot = Svc<ISaveService>().Slots[_selected];
        _confirm = _kit.Layout.Host(_dialogParent, "OverwriteConfirm");
        _confirm.MouseFilter = MouseFilterEnum.Stop;
        var scrim = _kit.Box(_confirm, new UiFx(Fill: "scrim", Margin: 0));
        scrim.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var dialog = _kit.Layout.Dialog(_confirm, UiKit.T("saves.overwrite.title"));
        dialog.Tabs.Visible = false;
        _kit.Text(dialog.Body, UiKit.T("saves.overwrite.body", ("slot", slot.Slot.Value.TrimStart('s').PadLeft(2, '0'))), "small", "text.2");
        var cancel = _kit.Button(dialog.Footer, ButtonStyle.Secondary, UiKit.T("saves.overwrite.cancel"), pressed: HideConfirm);
        _kit.Button(dialog.Footer, ButtonStyle.Danger, UiKit.T("saves.overwrite"), "button.primary", pressed: () => _ = Act());
        FocusWhenInteractive(cancel);
    }

    private void ReleaseConfirm()
    {
        if (_confirm is null) return;
        _confirm.Visible = false;
        _confirm.GetParent()?.RemoveChild(_confirm);
        _confirm.QueueFree();
        _confirm = null;
    }

    private void HideConfirm()
    {
        ReleaseConfirm();
        if (_cards.Count > 0) FocusWhenInteractive(_overwrite is { Visible: true } ? _overwrite : _cards[_selected].Hit);
    }

    private static void FocusWhenInteractive(Control target)
    {
        // 根宿主先仲裁上层输入；隐藏 HUD 和被上层遮挡的页面不争抢焦点。
        if (target.IsVisibleInTree() && target.GetFocusModeWithOverride() != FocusModeEnum.None) target.GrabFocus();
    }

    /// <summary>Esc 优先取消覆盖确认。</summary>
    public bool DismissConfirm()
    {
        if (_confirm is null) return false;
        HideConfirm();
        return true;
    }

    private async Task Act()
    {
        if (!_fromTitle) return;
        var slot = Svc<ISaveService>().Slots[_selected];
        var request = _intent == SaveIntent.NewGame || slot.Meta is null
            ? new EnterRequest(EnterKind.NewGame, slot.Slot)
            : new EnterRequest(EnterKind.LoadSlot, slot.Slot);
        CloseRequested?.Invoke();
        await Svc<IAppFlow>().Enter(request);
    }

    private static string Place(SaveMeta meta) =>
        meta.PlaceKey is { } key && Campus.Hud.I18n.Tr.Has(key) ? UiKit.T(key) : UiKit.T("saves.place.unknown");

    public static string Summary(SaveMeta meta) =>
        $"{UiKit.T("hud.season." + TimeCard.SeasonCode(meta.Season).ToLowerInvariant())} · {WorldTime.FormatClock(meta.Clock)} · " +
        UiKit.T("weather.cell." + WeatherStrip.CellKey(WeatherStrip.CellOf(meta.Weather)));

    private static string PlayTime(double seconds)
    {
        var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
    }

    private static string SavedAt(long unix) =>
        unix <= 0 ? "—" : DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}
