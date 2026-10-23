using System.Collections;
using Campus.Contracts;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public enum SettingsPage : byte { Keys, General, Graphics, Sound, Rebind }

/// <summary>贴左的设置页：容器安排页头、单行横滚页签、滚动正文和始终可见的操作栏；关闭即释放。</summary>
public partial class SettingsCard : Control
{
    private const int RowsPerFrame = 4;
    private static readonly SettingsPage[] Order = [SettingsPage.Keys, SettingsPage.General, SettingsPage.Graphics, SettingsPage.Sound, SettingsPage.Rebind];
    private readonly Dictionary<SettingsPage, VBoxContainer> _pages = [];
    private readonly HashSet<SettingsPage> _filled = [];
    private readonly Dictionary<SettingsPage, IEnumerator> _builders = [];
    private readonly List<TechButton> _tabs = [];
    private UiKit _kit = null!;
    private HudSettings _settings = null!;
    private UiPageFrame _frame = null!;
    private VBoxContainer _pageActions = null!;
    private TechButton _toTitle = null!;
    private IDisposable? _lock;
    private SettingsPage _page = SettingsPage.Graphics;
    private SettingsCardContent _content = null!;
    private SettingsCardGraphics _graphics = null!;
    private SettingsCardBindings _bindings = null!;
    private Tween? _opening;
    private bool _built;
    private string? _reported;

    public event Action? CloseRequested;
    public event Action? QuitRequested;
    public event Action? ToTitleRequested;
    public SettingsPage Page => _page;
    public bool Capturing => _bindings.Capturing;

    internal static SettingsCard Create(UiKit kit, Control parent, HudSettings settings, IInputRouter router, HudTask qualityApply)
    {
        var card = new SettingsCard
        {
            _kit = kit, _settings = settings,
            MouseFilter = MouseFilterEnum.Ignore, Visible = false,
        };
        card._bindings = new SettingsCardBindings(kit, router);
        card._content = new SettingsCardContent(kit, settings, card._bindings);
        card._graphics = new SettingsCardGraphics(kit, qualityApply);
        card._bindings.BindingsChanged += card.InvalidateKeys;
        parent.AddChild(card);
        card.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return card;
    }

    private void Build()
    {
        _built = true;
        var k = _kit;
        _frame = k.Layout.Page(this, UiKit.T("settings.title"));
        var reduceTransparency = _settings.Get(HudSettingDefinitions.ReduceTransparency);
        var frost = _settings.Get(HudSettingDefinitions.Frost) && !reduceTransparency;
        var fill = reduceTransparency ? "surface.solid" : frost ? "surface.frost" : "surface.panel";
        k.Apply(_frame.Panel, new UiFx(Fill: fill, Line: "line.panel", Chamfer: k.Tokens.Chamfer("page"), Flags: UiFlags.Highlight | UiFlags.Via));
        _frame.Panel.MouseFilter = MouseFilterEnum.Stop;
        if (frost)
        {
            // 磨砂底与面板同一尺寸来源：都是面板的子件，模糊层由 PanelContainer 排成面板的矩形，画在面板底色之下（ShowBehindParent）。
            var copy = new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Rect, ShowBehindParent = true };
            _frame.Panel.AddChild(copy);
            _frame.Panel.MoveChild(copy, 0);
            var blur = new ColorRect { Color = Colors.White, MouseFilter = MouseFilterEnum.Ignore, Material = k.Materials.Frost, ShowBehindParent = true };
            _frame.Panel.AddChild(blur);
            _frame.Panel.MoveChild(blur, 1);
            blur.SetInstanceShaderParameter("chamfer", k.Tokens.Chamfer("page"));
            void ResizeCopy() => copy.Rect = new Rect2(Vector2.Zero, _frame.Panel.Size);
            _frame.Panel.Resized += ResizeCopy;
            ResizeCopy();
        }
        var close = k.Button(_frame.Header, ButtonStyle.Icon, "", icon: "close", pressed: () => CloseRequested?.Invoke());
        close.TooltipText = UiKit.T("ui.common.close") + " · Esc";
        close.AccessibilityName = UiKit.T("ui.common.close");
        var tabScroll = k.Layout.Scroll(_frame.Tabs);
        tabScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Auto;
        tabScroll.VerticalScrollMode = ScrollContainer.ScrollMode.Disabled;
        tabScroll.SizeFlagsVertical = SizeFlags.Fill;
        tabScroll.FollowFocus = true;
        tabScroll.ScrollHorizontalByDefault = true;
        var horizontalBar = tabScroll.GetHScrollBar();
        horizontalBar.ThemeTypeVariation = "ScrollThinHorizontal";
        horizontalBar.Material = k.Materials.Text;
        k.Recolor(horizontalBar, "line.scroll");
        var tabRow = k.Layout.Row(tabScroll);
        tabRow.ThemeTypeVariation = UiThemeBuilder.Segments;
        for (var i = 0; i < Order.Length; i++)
        {
            var page = Order[i];
            var tab = k.Button(tabRow, ButtonStyle.Tab, UiKit.T("settings.tab." + Key(page)) + " " + (i + 1));
            tab.Pressed += () => Show(page);
            _tabs.Add(tab);
            var content = k.Layout.Stack(_frame.Body);
            content.Visible = false;
            _pages[page] = content;
        }
        // 页内操作和全局操作在滚动区外；换页只切换本页动作。
        var footer = k.Layout.Stack(_frame.Footer, groups: false);
        _pageActions = k.Layout.Stack(footer, groups: false);
        var navigation = k.Layout.Flow(footer);
        _toTitle = k.Button(navigation, ButtonStyle.Secondary, UiKit.T("settings.to_title"), pressed: () => ToTitleRequested?.Invoke());
        k.Button(navigation, ButtonStyle.Secondary, UiKit.T("settings.quit"), pressed: () => QuitRequested?.Invoke());
    }

    public void Open(SettingsPage page, bool fromTitle)
    {
        if (!_built) Build();
        Show(page);
        _toTitle.Off = fromTitle;
        _toTitle.OffReason = UiKit.T("settings.to_title.off");
        if (!Visible)
        {
            Visible = true;
            _lock ??= Campus.Kernel.Registry.Kernel.Get<ICameraRig>().LockInput(InputLockReason.Menu);
            Modulate = new Color(1, 1, 1, 0);
            _opening?.Kill();
            _opening = CreateTween();
            _opening.TweenProperty(this, "modulate:a", 1f, _kit.Motion.Fade(0.26f));
        }
        _graphics.Refresh();
        PublishReady();
    }

    public void Close()
    {
        _bindings.EndCapture();
        _opening?.Kill();
        _opening = null;
        Visible = false;
        PublishReady();
        _lock?.Dispose();
        _lock = null;
        Release();
    }

    private void Release()
    {
        SetProcess(false);
        foreach (var builder in _builders.Values) (builder as IDisposable)?.Dispose();
        _builders.Clear();
        // 当帧再次打开也不能让待释放的旧页参与排版、焦点和绘制。
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _pages.Clear();
        _filled.Clear();
        _tabs.Clear();
        _bindings.Release();
        _graphics.Release();
        _built = false;
    }

    /// <summary>选中页还没建完就登记它的分帧构建；构建在本节点的 _Process 里推进，关闭或退树即停，不留跨帧等待。</summary>
    private void Fill(SettingsPage page)
    {
        if (_filled.Contains(page)) return;
        if (!_builders.ContainsKey(page))
        {
            var content = _pages[page];
            IEnumerable steps = page switch
            {
                SettingsPage.Keys => _content.BuildKeys(content),
                SettingsPage.General => _content.BuildGeneral(content),
                SettingsPage.Graphics => Then(_graphics.Build(content, _pageActions), _content.BuildPerformance(content)),
                SettingsPage.Sound => _content.BuildSound(content),
                SettingsPage.Rebind => _bindings.Build(content),
                _ => throw new InvalidOperationException("Unknown settings page."),
            };
            _builders[page] = steps.GetEnumerator();
        }
        SetProcess(true);
    }

    // 画面页先建画质各组，再接性能信息这类 ui 节的行；画面页以后再收别的 ui 节行，也接在这里。
    private static IEnumerable Then(IEnumerable first, IEnumerable second)
    {
        foreach (var step in first) yield return step;
        foreach (var step in second) yield return step;
    }

    // 先呈现卡片外框，每帧只给当前页建少量设置行，行建好即显示；切页保留各页进度，关闭时连同进度一起释放。
    public override void _Process(double delta)
    {
        if (!_builders.TryGetValue(_page, out var builder))
        {
            SetProcess(false);
            return;
        }
        // 建行抛出的异常原样交给引擎；迭代器随之结束，下一帧按建完收尾，不会每帧重复抛同一个故障。
        for (var row = 0; row < RowsPerFrame; row++)
        {
            if (builder.MoveNext()) continue;
            (builder as IDisposable)?.Dispose();
            _builders.Remove(_page);
            _filled.Add(_page);
            _graphics.Refresh();
            SetProcess(false);
            PublishReady();
            return;
        }
    }

    public string KeyLabel(string action) => _bindings.KeyLabel(action);

    /// <summary>HudRoot 在捕获期间把下一个按键交到这里；Esc 取消，冲突返回提示。</summary>
    public string? Capture(InputEventKey key) => _bindings.Capture(key);

    private void InvalidateKeys()
    {
        // 键位页如已打开过，同步反映本次改键；不重建可交互的改键行。
        if (!_filled.Remove(SettingsPage.Keys)) return;
        var content = _pages[SettingsPage.Keys];
        foreach (var child in content.GetChildren())
        {
            content.RemoveChild(child);
            child.QueueFree();
        }
        PublishReady();
    }

    // 上层（界面根、--ui=0）显隐变化也会改变卡片是否真的看得见，随之重报。
    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged) PublishReady();
    }

    /// <summary>选中页的行建完且卡片真实可见才报这一页（IUiSurfaceSink）；切到未建完的页、关闭、上层隐藏或清空当前页即撤下，建好再报。</summary>
    private void PublishReady()
    {
        var ready = IsInsideTree() && IsVisibleInTree() && _filled.Contains(_page) ? Key(_page) : null;
        if (ready == _reported) return;
        _reported = ready;
        Campus.Kernel.Registry.Kernel.Get<IUiSurfaceSink>().ReportSettings(ready);
    }

    private void Show(SettingsPage page)
    {
        if (_page != page) _bindings.EndCapture();
        _page = page;
        Fill(page);
        foreach (var (p, content) in _pages) content.Visible = p == page;
        for (var i = 0; i < _tabs.Count; i++) _tabs[i].Selected = Order[i] == page;
        _graphics.ShowActions(page == SettingsPage.Graphics);
        _pageActions.Visible = page == SettingsPage.Graphics;
        _frame.Scroll.ScrollVertical = 0;
        PublishReady();
    }

    private static string Key(SettingsPage page) => page switch
    {
        SettingsPage.Keys => "keys", SettingsPage.General => "general", SettingsPage.Graphics => "graphics",
        SettingsPage.Sound => "sound", SettingsPage.Rebind => "rebind",
        _ => throw new ArgumentOutOfRangeException(nameof(page)),
    };
}
