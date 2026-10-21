using System.Collections;
using Campus.Contracts;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public enum SettingsPage : byte { Keys, General, Graphics, Sound, Rebind, Licenses }

/// <summary>贴左的设置页：容器安排页头、单行横滚页签、滚动正文和始终可见的操作栏；关闭即释放。</summary>
public partial class SettingsCard : Control
{
    private const int RowsPerFrame = 4;
    private static readonly SettingsPage[] Order = [SettingsPage.Keys, SettingsPage.General, SettingsPage.Graphics, SettingsPage.Sound, SettingsPage.Rebind, SettingsPage.Licenses];
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
    private int _fillRevision;

    public event Action? CloseRequested;
    public event Action? QuitRequested;
    public event Action? ToTitleRequested;
    public SettingsPage Page => _page;
    public bool Capturing => _bindings.Capturing;

    public static SettingsCard Create(UiKit kit, Control parent, HudSettings settings, IInputRouter router)
    {
        var card = new SettingsCard
        {
            _kit = kit, _settings = settings,
            MouseFilter = MouseFilterEnum.Ignore, Visible = false,
        };
        card._bindings = new SettingsCardBindings(kit, router);
        card._content = new SettingsCardContent(kit, settings, card._bindings);
        card._graphics = new SettingsCardGraphics(kit);
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
            var copy = new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Rect };
            _frame.Shell.AddChild(copy);
            _frame.Shell.MoveChild(copy, 0);
            var blur = new ColorRect { Color = Colors.White, MouseFilter = MouseFilterEnum.Ignore, Material = k.Materials.Frost };
            _frame.Shell.AddChild(blur);
            _frame.Shell.MoveChild(blur, 1);
            blur.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            blur.SetInstanceShaderParameter("chamfer", k.Tokens.Chamfer("page"));
            void ResizeCopy() => copy.Rect = _frame.Panel.GetRect();
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
    }

    public void Close()
    {
        _bindings.EndCapture();
        _opening?.Kill();
        _opening = null;
        Visible = false;
        _lock?.Dispose();
        _lock = null;
        Release();
    }

    private void Release()
    {
        _fillRevision++;
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

    private async Task Fill(SettingsPage page, int revision)
    {
        if (_filled.Contains(page)) return;
        var content = _pages[page];
        if (!_builders.TryGetValue(page, out var builder))
        {
            IEnumerable steps = page switch
            {
                SettingsPage.Keys => _content.BuildKeys(content),
                SettingsPage.General => _content.BuildGeneral(content),
                SettingsPage.Graphics => _graphics.Build(content, _pageActions),
                SettingsPage.Sound => _content.BuildSound(content),
                SettingsPage.Rebind => _bindings.Build(content),
                SettingsPage.Licenses => _content.BuildLicenses(content),
                _ => throw new InvalidOperationException("Unknown settings page."),
            };
            _builders[page] = builder = steps.GetEnumerator();
        }
        // 先呈现卡片外框，每帧再建少量设置行；切页保留进度，关闭沿原生命周期释放。
        while (true)
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (revision != _fillRevision || !IsInsideTree() || !Visible) return;
            for (var row = 0; row < RowsPerFrame; row++)
            {
                if (builder.MoveNext()) continue;
                (builder as IDisposable)?.Dispose();
                _builders.Remove(page);
                _filled.Add(page);
                content.Visible = true;
                _pageActions.Visible = page == SettingsPage.Graphics;
                _graphics.Refresh();
                return;
            }
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
    }

    public override void _ExitTree() => _fillRevision++;

    private void Show(SettingsPage page)
    {
        if (_page != page) _bindings.EndCapture();
        _page = page;
        _ = Fill(page, ++_fillRevision);
        foreach (var (p, content) in _pages) content.Visible = p == page && _filled.Contains(p);
        for (var i = 0; i < _tabs.Count; i++) _tabs[i].Selected = Order[i] == page;
        _graphics.ShowActions(page == SettingsPage.Graphics);
        _pageActions.Visible = page == SettingsPage.Graphics && _filled.Contains(page);
        _frame.Scroll.ScrollVertical = 0;
    }

    private static string Key(SettingsPage page) => page switch
    {
        SettingsPage.Keys => "keys", SettingsPage.General => "general", SettingsPage.Graphics => "graphics",
        SettingsPage.Sound => "sound", SettingsPage.Rebind => "rebind", SettingsPage.Licenses => "licenses",
        _ => throw new ArgumentOutOfRangeException(nameof(page)),
    };
}
