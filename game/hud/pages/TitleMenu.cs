using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>
/// 开始页的界面部分（UI-VISUAL、UI-NAV、SYS-TITLE）：按"压在画面上"处理，不随主题，不读 ui_night——
/// 只用两套主题里相同的 over.title.* 令牌；左侧一条暗渐变，片名细体宽字距，下面一行真数据；
/// 菜单：继续 / 新游戏 / 存档 / 设置 / 退出。进入请求交界面根（HudPages），只读 ISaveService。
/// </summary>
public partial class TitleMenu : Control
{
    private readonly List<TechButton> _items = [];
    private UiKit _kit = null!;
    private Label _data = null!, _continueInfo = null!;
    private TechButton _continue = null!;
    private Control _backgroundParent = null!;
    private Control? _shade;
    private ISaveService _saves = null!;
    private IMapService _maps = null!;

    /// <summary>继续与新游戏只发请求，进入与失败提示归界面根（HudPages）。</summary>
    public event Action<EnterRequest>? EnterRequested;
    public event Action? SavesRequested;
    /// <summary>三个存档位都占着时的"新游戏"：由界面根以新游戏方式打开存档页，选位并确认覆盖（SYS-TITLE）。</summary>
    public event Action? OverwriteRequested;
    public event Action? SettingsRequested;
    public event Action? QuitRequested;
    public event Action<string>? Notice;

    internal static TitleMenu Create(UiKit kit, Control parent, Control backgroundParent, ISaveService saves, IMapService maps)
    {
        var menu = new TitleMenu { _kit = kit, _backgroundParent = backgroundParent, _saves = saves, _maps = maps, MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        parent.AddChild(menu);
        menu.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return menu;
    }

    private void Build()
    {
        var band = _kit.Colors.Theme("light", "over.title.band");
        var gradient = new Gradient { Offsets = [0f, 0.62f, 1f], Colors = [band, band with { A = band.A * 0.6f }, band with { A = 0 }] };
        _shade = new TextureRect
        {
            Texture = new GradientTexture2D { Gradient = gradient, FillFrom = Vector2.Zero, FillTo = new Vector2(1, 0) },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _backgroundParent.AddChild(_shade);
        _shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // 片名和菜单在安全框左侧带内；高度不够时整列滚动，避免菜单推出窗口。
        var column = _kit.Layout.Host(this, "TitleColumn");
        var bounds = _kit.Tokens.Get("metrics", "base", "centralFree");
        column.AnchorRight = bounds[0].GetSingle() / _kit.Tokens.F("metrics", "base", "width");
        var scroll = _kit.Layout.Scroll(column);
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var content = _kit.Layout.Stack(scroll);
        content.SizeFlagsVertical = SizeFlags.ExpandFill;
        content.Alignment = BoxContainer.AlignmentMode.End;
        var heading = _kit.Layout.Stack(content, groups: false);
        TitleName(_kit, heading);
        var rule = _kit.Box(heading, new UiFx(Fill: "over.title.text"));
        rule.CustomMinimumSize = new Vector2(0, _kit.Tokens.F("metrics", "line", "hairPhys"));
        rule.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _data = _kit.Text(heading, "", "data", "over.title.text");
        _kit.Text(heading, UiKit.T("title.english"), "mono-caps", "over.title.text");

        var menu = _kit.Layout.Stack(content, groups: false);
        string[] keys = ["title.continue", "title.new", "title.saves", "title.settings", "title.quit"];
        for (var i = 0; i < keys.Length; i++)
        {
            var index = i;
            var item = _kit.Button(menu, ButtonStyle.Item, UiKit.T(keys[i]), "h3", pressed: () => Pick(index), align: HorizontalAlignment.Left);
            item.RoleOverride = "over.title.text";
            item.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            item.ClipText = true;
            item.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            item.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            _items.Add(item);
            if (i == 0) _continueInfo = _kit.Text(menu, "", "data", "over.title.text");
        }
        _continue = _items[0];
        _continue.OffReason = UiKit.T("title.continue.none");
        // 没有存档时继续断电，点它只出提示（SYS-TITLE）。
        _continue.OffPressed += () => Notice?.Invoke(UiKit.T("title.continue.none"));
    }

    /// <summary>开始页与加载页共用同一片名字号选择。</summary>
    public static void TitleName(UiKit kit, Control heading)
    {
        var title = kit.Text(heading, UiKit.T("title.name"), "display-1", "over.title.text");
        void FitTitle()
        {
            // 从既有字号阶梯选可放下的片名字号，不额外维护像素断点。
            var fitted = UiThemeBuilder.TextType("h2");
            foreach (var role in new[] { "display-1", "display-2", "h1", "h2" })
            {
                fitted = UiThemeBuilder.TextType(role);
                var font = kit.Theme.Theme.GetFont("font", fitted);
                var size = kit.Theme.Theme.GetFontSize("font_size", fitted);
                if (font.GetStringSize(title.Text, fontSize: size).X <= title.Size.X) break;
            }
            if (title.ThemeTypeVariation != fitted) title.ThemeTypeVariation = fitted;
        }
        title.Resized += FitTitle;
        title.Ready += FitTitle;
    }

    /// <summary>打开时才建，<see cref="Close"/> 时释放：离开开始页以后它不再占实例参数槽（<see cref="UiKit.Holders"/>）。</summary>
    public void Open()
    {
        if (_items.Count == 0) Build();
        Visible = true;
        Sync();
        var focus = _continue.Off ? _items[1] : _continue;
        if (focus.IsVisibleInTree() && focus.GetFocusModeWithOverride() != FocusModeEnum.None) focus.GrabFocus();
    }

    public void Close()
    {
        Visible = false;
        if (_shade is not null)
        {
            _shade.Visible = false;
            _shade.GetParent()?.RemoveChild(_shade);
            _shade.QueueFree();
            _shade = null;
        }
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        _items.Clear();
    }

    private void Sync()
    {
        // 开始页没有世界实例：片名块写最近存档的世界状态，没有存档写默认状态。
        var world = TitleWorld.Resolve(_saves, _maps);
        _data.Text = SaveMetaText.Moment(world.Season, world.Clock, world.Weather);
        _continue.Off = world.Meta is null;
        _continueInfo.Visible = world.Meta is not null;
        if (world.Meta is { } meta) _continueInfo.Text = SaveMetaText.Summary(meta, _maps);
    }

    private void Pick(int index)
    {
        switch (index)
        {
            case 0: EnterRequested?.Invoke(new EnterRequest(EnterKind.Continue)); break;
            case 1: NewGame(); break;
            case 2: SavesRequested?.Invoke(); break;
            case 3: SettingsRequested?.Invoke(); break;
            case 4: QuitRequested?.Invoke(); break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private void NewGame()
    {
        var slots = _saves.Slots;
        var empty = slots.FirstOrDefault(s => s.Health == SlotHealth.Empty);
        if (empty is null && slots.Count > 0) OverwriteRequested?.Invoke();
        else EnterRequested?.Invoke(new EnterRequest(EnterKind.NewGame, empty?.Slot));
    }
}
