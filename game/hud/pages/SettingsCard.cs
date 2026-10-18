using Campus.Contracts;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public enum SettingsPage : byte { Keys, General, Graphics, Sound, Rebind, Licenses }

/// <summary>贴左的设置页：容器安排页头、可换行页签、滚动正文和始终可见的操作栏；关闭即释放。</summary>
public partial class SettingsCard : Control
{
    private static readonly SettingsPage[] Order = [SettingsPage.Keys, SettingsPage.General, SettingsPage.Graphics, SettingsPage.Sound, SettingsPage.Rebind, SettingsPage.Licenses];
    private readonly Dictionary<SettingsPage, VBoxContainer> _pages = [];
    private readonly HashSet<SettingsPage> _filled = [];
    private readonly List<TechButton> _tabs = [];
    private UiKit _kit = null!;
    private IInputRouter _router = null!;
    private HudSettings _settings = null!;
    private UiPageFrame _frame = null!;
    private VBoxContainer _pageActions = null!;
    private TechButton _toTitle = null!;
    private IDisposable? _lock;
    private SettingsPage _page = SettingsPage.Graphics;
    private string? _rebindAction;
    private IDisposable? _rebindLock;
    private Tween? _opening;
    private bool _built;

    public event Action? CloseRequested;
    public event Action? QuitRequested;
    public event Action? ToTitleRequested;
    public SettingsPage Page => _page;
    public bool Capturing => _rebindAction is not null;

    public static SettingsCard Create(UiKit kit, Control parent, HudSettings settings, IInputRouter router)
    {
        var card = new SettingsCard
        {
            _kit = kit, _settings = settings, _router = router,
            MouseFilter = MouseFilterEnum.Ignore, Visible = false,
        };
        parent.AddChild(card);
        card.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return card;
    }

    private void Build()
    {
        _built = true;
        var k = _kit;
        _frame = k.Layout.Page(this, UiKit.T("settings.title"));
        var frost = _settings.Frost && !_settings.ReduceTransparency;
        var fill = _settings.ReduceTransparency ? "surface.solid" : frost ? "surface.frost" : "surface.panel";
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
        for (var i = 0; i < Order.Length; i++)
        {
            var page = Order[i];
            var tab = k.Button(_frame.Tabs, ButtonStyle.Tab, UiKit.T("settings.tab." + Key(page)) + " " + (i + 1));
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
            _lock ??= Lock(InputLockReason.Menu);
            Modulate = new Color(1, 1, 1, 0);
            _opening?.Kill();
            _opening = CreateTween();
            _opening.TweenProperty(this, "modulate:a", 1f, _kit.Motion.Fade(0.26f));
        }
        RefreshGraphics();
    }

    public void Close()
    {
        EndCapture();
        _opening?.Kill();
        _opening = null;
        Visible = false;
        _lock?.Dispose();
        _lock = null;
        Release();
    }

    private void Release()
    {
        // 当帧再次打开也不能让待释放的旧页参与排版、焦点和绘制。
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _pages.Clear();
        _filled.Clear();
        _tabs.Clear();
        _rebindRows.Clear();
        _knobSync.Clear();
        _preset = null;
        _applyStatus = null;
        _apply = null;
        _graphicsActions = null;
        _built = false;
    }

    private void Fill(SettingsPage page)
    {
        if (!_filled.Add(page)) return;
        var content = _pages[page];
        switch (page)
        {
            case SettingsPage.Keys: BuildKeys(content); break;
            case SettingsPage.General: BuildGeneral(content); break;
            case SettingsPage.Graphics: BuildGraphics(content); break;
            case SettingsPage.Sound: BuildSound(content); break;
            case SettingsPage.Rebind: BuildRebind(content); break;
            case SettingsPage.Licenses: BuildLicenses(content); break;
            default: throw new InvalidOperationException("Unknown settings page.");
        }
    }

    private void Show(SettingsPage page)
    {
        if (_page != page) EndCapture();
        _page = page;
        Fill(page);
        foreach (var (p, content) in _pages) content.Visible = p == page;
        for (var i = 0; i < _tabs.Count; i++) _tabs[i].Selected = Order[i] == page;
        if (_graphicsActions is not null) _graphicsActions.Visible = page == SettingsPage.Graphics;
        _pageActions.Visible = page == SettingsPage.Graphics;
        _frame.Scroll.ScrollVertical = 0;
    }
}
