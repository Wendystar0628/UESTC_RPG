using Campus.Contracts;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

public enum SettingsPage : byte { Keys, General, Graphics, Sound, Rebind, Licenses }

/// <summary>
/// 设置卡（UI-VISUAL、UI-NAV、SYS-GRAPHICS）：贴左的大页，620×1016，切角 14，随主题；不压暗，右侧 2/3 留给实景。
/// 页：键位、设置、画面（按 KnobSpec 生成，只经 IQualityService）、声音（只经 IAudioService）、改键、许可；底部"返回标题""退出"。
/// 打开期间经 ICameraRig.LockInput(Menu) 占用观者输入。内容在打开时才建、关上就释放：引擎只在释放节点时归还
/// 实例参数槽（<see cref="UiKit.Holders"/>），隐藏的大页不该和 3D 流式实例抢同一个池；画面页没应用的改动跨开关保留。
/// </summary>
public partial class SettingsCard : Control
{
    private static readonly SettingsPage[] Order = [SettingsPage.Keys, SettingsPage.General, SettingsPage.Graphics, SettingsPage.Sound, SettingsPage.Rebind, SettingsPage.Licenses];

    private readonly Dictionary<SettingsPage, Control> _pages = [];
    private readonly HashSet<SettingsPage> _filled = [];
    private readonly List<TechButton> _tabs = [];
    private UiKit _kit = null!;
    private IInputRouter _router = null!;   // 改键与键名经 IInputRouter（WP-06）
    private HudSettings _settings = null!;
    private Panel _tabBar = null!;
    private ScrollContainer _scroll = null!;
    private TechButton _toTitle = null!;
    private IDisposable? _lock;
    private SettingsPage _page = SettingsPage.Graphics;
    private string? _rebindAction;
    private IDisposable? _rebindLock;
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
            _kit = kit,
            _settings = settings,
            _router = router,
            Position = new Vector2(32, 32),
            Size = kit.Tokens.V2("metrics", "size", "settingsCard"),
            MouseFilter = MouseFilterEnum.Stop,
            Visible = false,
        };
        parent.AddChild(card);
        return card;
    }

    private void Build()
    {
        _built = true;
        var k = _kit;
        var chamfer = k.Tokens.Chamfer("page");
        var frost = _settings.Frost && !_settings.ReduceTransparency;
        if (frost)
        {
            AddChild(new BackBufferCopy { CopyMode = BackBufferCopy.CopyModeEnum.Rect, Rect = new Rect2(Vector2.Zero, Size) });
            var blur = new ColorRect { Size = Size, Color = Colors.White, MouseFilter = MouseFilterEnum.Ignore, Material = k.Materials.Frost };
            AddChild(blur);
            blur.SetInstanceShaderParameter("chamfer", chamfer);
        }
        var fill = _settings.ReduceTransparency ? "surface.solid" : frost ? "surface.frost" : "surface.panel";
        k.Box(this, new Rect2(Vector2.Zero, Size), new UiFx(Fill: fill, Line: "line.panel", Chamfer: chamfer, Flags: UiFlags.Highlight | UiFlags.Via));

        const float x = 24;
        var width = Size.X - 48;
        k.Text(this, UiKit.T("settings.title"), "h2", "text.1", new Vector2(x, 18));
        k.Text(this, "SETTINGS", "mono-caps", "text.2", new Vector2(x + 62, 30));
        var close = k.Button(this, ButtonStyle.Icon, "", new Rect2(Size.X - 24 - 32, 18, 32, 32), padX: 4, icon: "close", iconMax: 20, padY: 4,
            pressed: () => CloseRequested?.Invoke());
        close.TooltipText = UiKit.T("ui.common.close") + " · Esc";

        float tx = x;
        for (var i = 0; i < Order.Length; i++)
        {
            var page = Order[i];
            var label = UiKit.T("settings.tab." + Key(page)) + " " + (i + 1);
            var probe = k.Text(this, label, "body", "text.2", Vector2.Zero);
            var w = k.Width(probe) + 4;
            probe.QueueFree();
            var tab = k.Button(this, ButtonStyle.Tab, label, new Rect2(tx, 64, w, 38), "body", 0, padY: 2);
            tab.Pressed += () => Show(page);
            _tabs.Add(tab);
            tx += w + 22;
        }
        _tabBar = k.Box(this, new Rect2(x, 100, 40, 2), new UiFx(Fill: "primary.line"));
        k.Box(this, new Rect2(x, 102, width, 1), new UiFx(Fill: "line.hair"));

        _scroll = new ScrollContainer
        {
            Position = new Vector2(x - 8, 114),
            Size = new Vector2(width + 16, Size.Y - 114 - 76),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            MouseFilter = MouseFilterEnum.Pass,
        };
        AddChild(_scroll);
        _scroll.GetVScrollBar().ThemeTypeVariation = "ScrollThin";
        _scroll.GetVScrollBar().Material = k.Materials.Text;
        k.Recolor(_scroll.GetVScrollBar(), "line.scroll");
        var stack = new VBoxContainer { CustomMinimumSize = new Vector2(width + 8, 0) };
        _scroll.AddChild(stack);
        foreach (var page in Order)
        {
            // 各页的内容在第一次切到它时才填（Fill），开卡只付当前这一页的代价
            var content = new Control { CustomMinimumSize = new Vector2(width + 8, 0), MouseFilter = MouseFilterEnum.Pass, Visible = false };
            stack.AddChild(content);
            _pages[page] = content;
        }

        var footY = Size.Y - 64;
        k.Box(this, new Rect2(x, footY - 12, width, 1), new UiFx(Fill: "line.hair"));
        _toTitle = k.Button(this, ButtonStyle.Secondary, UiKit.T("settings.to_title"), new Rect2(x, footY, 132, 40), pressed: () => ToTitleRequested?.Invoke());
        k.Button(this, ButtonStyle.Secondary, UiKit.T("settings.quit"), new Rect2(Size.X - x - 132, footY, 132, 40), pressed: () => QuitRequested?.Invoke());
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
            var t = CreateTween();
            t.TweenProperty(this, "modulate:a", 1f, _kit.Motion.Fade(0.26f));
            if (_kit.Motion.Spatial)
            {
                Position = new Vector2(24, 32);
                t.Parallel().TweenProperty(this, "position:x", 32f, 0.26f).SetTrans(Tween.TransitionType.Quart).SetEase(Tween.EaseType.Out);
            }
        }
        RefreshGraphics();
    }

    public void Close()
    {
        EndCapture();
        Visible = false;
        _lock?.Dispose();
        _lock = null;
        Release();
    }

    /// <summary>释放全部内容（排队到帧末，删在本帧绘制之前）；下次打开重建。</summary>
    private void Release()
    {
        foreach (var child in GetChildren()) child.QueueFree();
        _pages.Clear();
        _filled.Clear();
        _tabs.Clear();
        _rebindRows.Clear();
        _preset = null;
        _applyStatus = null;
        _apply = null;
        _built = false;
    }

    private void Fill(SettingsPage page)
    {
        if (!_filled.Add(page)) return;
        var content = _pages[page];
        var width = Size.X - 48;
        var height = page switch
        {
            SettingsPage.Keys => BuildKeys(content, width),
            SettingsPage.General => BuildGeneral(content, width),
            SettingsPage.Graphics => BuildGraphics(content, width),
            SettingsPage.Sound => BuildSound(content, width),
            SettingsPage.Rebind => BuildRebind(content, width),
            SettingsPage.Licenses => BuildLicenses(content, width),
            _ => throw new InvalidOperationException("Unknown settings page."),
        };
        content.CustomMinimumSize = new Vector2(width + 8, height);
    }

    private void Show(SettingsPage page)
    {
        _page = page;
        Fill(page);
        foreach (var (p, content) in _pages) content.Visible = p == page;
        for (var i = 0; i < _tabs.Count; i++) _tabs[i].Selected = Order[i] == page;
        var tab = _tabs[Array.IndexOf(Order, page)];
        _tabBar.Position = new Vector2(tab.Position.X, 100);
        _tabBar.Size = new Vector2(tab.Size.X, 2);
        _scroll.ScrollVertical = 0;
    }

}
