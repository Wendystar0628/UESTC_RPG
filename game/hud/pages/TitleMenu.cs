using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Hud.Style;
using Campus.Hud.Widgets;
using Godot;

namespace Campus.Hud.Pages;

/// <summary>
/// 开始页的界面部分（UI-VISUAL、UI-NAV、SYS-TITLE）：按"压在画面上"处理，不随主题，不读 ui_night——
/// 只用两套主题里相同的 over.title.* 令牌；左侧一条暗渐变，片名细体宽字距，下面一行真数据；
/// 菜单：继续 / 新游戏 / 存档 / 设置 / 制作人员与许可 / 退出。只经 IAppFlow 写，只读 ISaveService。
/// </summary>
public partial class TitleMenu : Control
{
    private readonly List<TechButton> _items = [];
    private UiKit _kit = null!;
    private Label _data = null!, _continueInfo = null!;
    private TechButton _continue = null!;

    public event Action? SavesRequested;
    /// <summary>三个存档位都占着时的"新游戏"：由界面根以新游戏方式打开存档页，选位并确认覆盖（SYS-TITLE §2.2）。</summary>
    public event Action? OverwriteRequested;
    public event Action? SettingsRequested;
    public event Action? CreditsRequested;
    public event Action? QuitRequested;
    public event Action<string>? Notice;

    private static T Svc<T>() where T : class => Campus.Kernel.Registry.Kernel.Get<T>();

    public static TitleMenu Create(UiKit kit, Control parent)
    {
        var menu = new TitleMenu { _kit = kit, MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        parent.AddChild(menu);
        menu.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return menu;
    }

    private void Build()
    {
        var band = _kit.Colors.Theme("light", "over.title.band");
        var gradient = new Gradient { Offsets = [0f, 0.62f, 1f], Colors = [band, band with { A = band.A * 0.6f }, band with { A = 0 }] };
        var shade = new TextureRect
        {
            Texture = new GradientTexture2D { Gradient = gradient, Width = 256, Height = 4, FillFrom = Vector2.Zero, FillTo = new Vector2(1, 0) },
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
            Size = new Vector2(760, 1080),
        };
        AddChild(shade);
        _kit.Text(this, UiKit.T("title.name"), "display-1", "over.title.text", new Vector2(112, 300));
        _kit.Box(this, new Rect2(112, 432, 360, 1), new UiFx(Fill: "over.title.text"));
        _kit.Dot(this, new Vector2(112, 432.5f), 4, new UiFx(Fill: "over.title.text"));
        _data = _kit.Text(this, "", "data", "over.title.text", new Vector2(112, 444));
        _kit.Text(this, UiKit.T("title.english"), "mono-caps", "over.title.text", new Vector2(112, 470));

        string[] keys = ["title.continue", "title.new", "title.saves", "title.settings", "title.credits", "title.quit"];
        float y = 540;
        for (var i = 0; i < keys.Length; i++)
        {
            var item = _kit.Button(this, ButtonStyle.Item, UiKit.T(keys[i]), new Rect2(104, y, 300, 44), "h3", 16, align: HorizontalAlignment.Left);
            item.RoleOverride = "over.title.text";
            var index = i;
            item.Pressed += () => Pick(index);
            _items.Add(item);
            y += i == 0 ? 64 : 48;
        }
        _continue = _items[0];
        _continue.OffReason = UiKit.T("title.continue.none");
        _continueInfo = _kit.Text(this, "", "data", "over.title.text", new Vector2(120, 584));
        for (var i = 0; i < _items.Count; i++)
        {
            _items[i].FocusNeighborTop = _items[(i + _items.Count - 1) % _items.Count].GetPath();
            _items[i].FocusNeighborBottom = _items[(i + 1) % _items.Count].GetPath();
        }
    }

    /// <summary>打开时才建，<see cref="Close"/> 时释放：离开开始页以后它不再占实例参数槽（<see cref="UiKit.Holders"/>）。</summary>
    public void Open()
    {
        if (_items.Count == 0) Build();
        Visible = true;
        Sync();
        var focus = _continue.Off ? _items[1] : _continue;
        focus.GrabFocus();
    }

    public void Close()
    {
        Visible = false;
        foreach (var child in GetChildren()) child.QueueFree();
        _items.Clear();
    }

    public void Sync()
    {
        var world = Svc<IWorldState>();
        _data.Text = $"{UiKit.T("hud.season." + TimeCard.SeasonCode(world.SeasonTarget).ToLowerInvariant())} · " +
                     $"{WorldTime.FormatClock(world.Clock)} · " +
                     $"{UiKit.T("weather.cell." + WeatherStrip.CellKey(WeatherStrip.CellOf(world.Weather.Kind)))}";
        SlotInfo? latest = null;
        var saves = Svc<ISaveService>();
        if (saves.Continue is { } id) latest = saves.Slots.FirstOrDefault(s => s.Slot == id);
        _continue.Off = latest?.Meta is null;
        _continueInfo.Visible = latest?.Meta is not null;
        if (latest?.Meta is { } meta) _continueInfo.Text = SavePage.Summary(meta);
    }

    private void Pick(int index)
    {
        switch (index)
        {
            case 0:
                if (_continue.Off) { Notice?.Invoke(UiKit.T("title.continue.none")); return; }
                _ = Enter(new EnterRequest(EnterKind.Continue));
                break;
            case 1: NewGame(); break;
            case 2: SavesRequested?.Invoke(); break;
            case 3: SettingsRequested?.Invoke(); break;
            case 4: CreditsRequested?.Invoke(); break;
            case 5: QuitRequested?.Invoke(); break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private void NewGame()
    {
        var slots = Svc<ISaveService>().Slots;
        var empty = slots.FirstOrDefault(s => s.Health == SlotHealth.Empty);
        if (empty is null && slots.Count > 0) OverwriteRequested?.Invoke();
        else _ = Enter(new EnterRequest(EnterKind.NewGame, empty?.Slot));
    }

    private async Task Enter(EnterRequest request)
    {
        var result = await Svc<IAppFlow>().Enter(request);
        switch (result)
        {
            case EnterResult.Entered: break;
            case EnterResult.Rejected: break;
            case EnterResult.Failed: Notice?.Invoke(UiKit.T("title.enter_failed")); break;
            default: throw new InvalidOperationException("Unknown enter result.");
        }
    }
}
