using Campus.Contracts;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 天气签条（UI-VISUAL §7.6、UI-NAV §4.10）：自动 + 8 格，36 px 图标钮放在一条签条里；不合季节的雷雨格呈禁用态、可聚焦；
/// 强度三枚小方块只在雾、降水、雷雨下出现；自动时当前实际天气所在的格下方一枚 4 px 过孔点。常驻件，默认固定深屏。
/// </summary>
public partial class WeatherStrip : Control
{
    public const int Auto = 0, Clear = 1, Few = 2, Cloudy = 3, Overcast = 4, Fog = 5, Precip = 6, Thunder = 7, After = 8;

    private readonly List<TechButton> _cells = [];
    private readonly List<Panel> _intensity = [];
    private UiKit _kit = null!;
    private HudModel _model = null!;
    private IWorldControl _control = null!;
    private Panel _actual = null!;
    private Season _iconSeason = (Season)255;

    public event Action<string>? Toast;

    public static WeatherStrip Create(UiKit kit, Control parent, HudModel model, IWorldControl control)
    {
        var strip = new WeatherStrip
        {
            _kit = kit,
            _model = model,
            _control = control,
            Size = kit.Tokens.V2("metrics", "size", "weatherStrip"),
            MouseFilter = MouseFilterEnum.Stop
        };
        parent.AddChild(strip);
        strip.Build();
        model.Changed += strip.Sync;
        strip.Sync();
        return strip;
    }

    private void Build()
    {
        var cell = _kit.Tokens.Size("weatherCell");
        _kit.Box(this, new Rect2(Vector2.Zero, Size), new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: _kit.Tokens.Chamfer("card"),
            Flags: UiFlags.Highlight, Pin: true));
        for (var i = 0; i <= After; i++)
        {
            var x = 12 + i * 40 + (i > 0 ? 6 : 0);
            var b = _kit.Button(this, ButtonStyle.Cell, "", new Rect2(x, 8, cell, cell), "body", 8, Icon(i, Season.Autumn), 20, true, 8);
            b.Value = i;
            b.TooltipText = UiKit.T("weather.cell." + CellKey(i));
            var index = i;
            b.Pressed += () => Pick(index);
            _cells.Add(b);
        }
        _kit.Box(this, new Rect2(51, 12, 1, 28), new UiFx(Fill: "line.hair", Pin: true));
        _actual = _kit.Dot(this, Vector2.Zero, 4, new UiFx(Fill: "graphic", Pin: true));
        for (var i = 0; i < 3; i++)
        {
            var square = _kit.Box(this, new Rect2(398 + i * 13, 21, 10, 10), new UiFx(Line: "line.control", Pin: true));
            var level = i;
            _intensity.Add(square);
            var hit = _kit.Button(this, ButtonStyle.Ghost, "", new Rect2(396 + i * 13, 17, 14, 18), pin: true);
            hit.SelfModulate = new Color(1, 1, 1, 0);
            hit.Pressed += () => { _control.SetWeatherIntensity((level + 1) / 3f); _model.Poke(); };
        }
    }

    public void Sync()
    {
        var sel = _model.Weather;
        var season = _model.Season;
        if (season != _iconSeason)
        {
            _iconSeason = season;
            for (var i = 0; i < _cells.Count; i++) _cells[i].Icon = _kit.Icon(Icon(i, season));
        }
        var current = CellOf(sel.Kind);
        for (var i = 0; i < _cells.Count; i++)
        {
            var native = i == Auto || _model.World.IsNative(Kind(i, season));
            _cells[i].OffReason = UiKit.T("weather.thunder.off_season");
            _cells[i].Off = !native;
            _cells[i].Selected = sel.Mode == WeatherMode.Auto ? i == Auto : i == current;
        }
        _actual.Visible = sel.Mode == WeatherMode.Auto;
        _actual.Position = new Vector2(_cells[current].Position.X + 18 - 2, 45);
        var showIntensity = current is Fog or Precip or Thunder;
        var level = Math.Clamp((int)MathF.Ceiling(sel.Intensity * 3) - 1, 0, 2);
        for (var i = 0; i < 3; i++)
        {
            _intensity[i].Visible = showIntensity;
            var on = i == level;
            var role = sel.Mode == WeatherMode.Auto ? "text.2" : "graphic";
            _kit.Apply(_intensity[i], new UiFx(Fill: on ? role : null, Line: on ? null : "line.control", Pin: true), remember: false);
        }
    }

    private void Pick(int cell)
    {
        if (cell == Auto) { _control.SetWeatherAuto(); _model.Poke(); return; }
        var kind = Kind(cell, _model.Season);
        if (!_model.World.IsNative(kind)) { Toast?.Invoke(UiKit.T("weather.thunder.off_season_toast")); return; }
        _control.SetWeather(kind);
        _model.Poke();
        Toast?.Invoke(UiKit.T("weather.manual_toast", ("name", UiKit.T("weather.cell." + CellKey(cell)))));
    }

    /// <summary>Y 键单击：本季可选的格里循环到下一格，进入手动（UI-NAV §4.10）。</summary>
    public void Next()
    {
        var sel = _model.Weather;
        var start = sel.Mode == WeatherMode.Auto ? CellOf(sel.Kind) : CellOf(sel.Kind);
        for (var step = 1; step <= After; step++)
        {
            var cell = (start - 1 + step) % After + 1;
            if (!_model.World.IsNative(Kind(cell, _model.Season))) continue;
            Pick(cell);
            return;
        }
    }

    public static WeatherKind Kind(int cell, Season season) => cell switch
    {
        Clear => WeatherKind.Clear,
        Few => WeatherKind.FewClouds,
        Cloudy => WeatherKind.Cloudy,
        Overcast => WeatherKind.Overcast,
        Fog => WeatherKind.Fog,
        Precip => season == Season.Winter ? WeatherKind.Snow : WeatherKind.Rain,
        Thunder => WeatherKind.Thunderstorm,
        After => season == Season.Winter ? WeatherKind.AfterSnow : WeatherKind.AfterRain,
        Auto => WeatherKind.Clear,
        _ => throw new ArgumentOutOfRangeException(nameof(cell)),
    };

    public static int CellOf(WeatherKind k) => k switch
    {
        WeatherKind.Clear => Clear,
        WeatherKind.FewClouds => Few,
        WeatherKind.Cloudy => Cloudy,
        WeatherKind.Overcast => Overcast,
        WeatherKind.Fog => Fog,
        WeatherKind.LightRain or WeatherKind.Rain or WeatherKind.Snow => Precip,
        WeatherKind.Thunderstorm => Thunder,
        WeatherKind.AfterRain or WeatherKind.AfterSnow => After,
        _ => throw new ArgumentOutOfRangeException(nameof(k)),
    };

    public static string Icon(int cell, Season season) => cell switch
    {
        Auto => "weather_auto",
        Clear => "weather_clear",
        Few => "weather_few",
        Cloudy => "weather_scattered",
        Overcast => "weather_overcast",
        Fog => "weather_fog",
        Precip => season == Season.Winter ? "weather_snow" : "weather_rain",
        Thunder => "weather_thunder",
        After => season == Season.Winter ? "weather_after_snow" : "weather_after_rain",
        _ => throw new ArgumentOutOfRangeException(nameof(cell)),
    };

    public static string CellKey(int cell) => cell switch
    {
        Auto => "auto",
        Clear => "clear",
        Few => "few",
        Cloudy => "cloudy",
        Overcast => "overcast",
        Fog => "fog",
        Precip => "precip",
        Thunder => "thunder",
        After => "after",
        _ => throw new ArgumentOutOfRangeException(nameof(cell)),
    };
}

/// <summary>
/// 观察模式与速度 LED 条（UI-VISUAL §7.7、UI-NAV §4.3、§4.4）：[旁观 | 行走] 两段；旁观 16 格 7×14，已到达 graphic 实心，
/// 当前档 primary.fill + primary.line 加高到 18；格子下方 2 px 光条是贴地限速后的实际速度；行走换成 5 个宽格。常驻件。
/// </summary>
public partial class SpeedBar : Control
{
    private static readonly int[] Anchors = [4, 7, 9, 12, 16];
    private static readonly string[] AnchorKeys = ["stroll", "cycle", "cruise", "bird", "cloud"];
    private static readonly float[] WalkSteps = [0.5f, 0.7f, 1f, 1.4f, 2f];

    private readonly List<Panel> _leds = [];
    private UiKit _kit = null!;
    private HudModel _model = null!;
    private Segmented _mode = null!;
    private Panel _actual = null!;
    private Label _readout = null!;

    public event Action<ViewMode>? ModePicked;

    public static SpeedBar Create(UiKit kit, Control parent, HudModel model)
    {
        var bar = new SpeedBar { _kit = kit, _model = model, Size = kit.Tokens.V2("metrics", "size", "speedBar"), MouseFilter = MouseFilterEnum.Stop };
        parent.AddChild(bar);
        kit.Box(bar, new Rect2(Vector2.Zero, bar.Size), new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: kit.Tokens.Chamfer("panel"),
            Flags: UiFlags.Highlight | UiFlags.Via, Pin: true));
        bar._mode = Segmented.Create(kit, bar, new Vector2(16, 14), [42, 42], ["", ""], true, 36, icons: ["view_free", "view_eye"], iconMax: 24);
        bar._mode.Items[0].TooltipText = UiKit.T("hud.mode.fly");
        bar._mode.Items[1].TooltipText = UiKit.T("hud.mode.walk");
        bar._mode.Changed += i => bar.ModePicked?.Invoke(i == 0 ? ViewMode.Fly : ViewMode.Walk);
        for (var i = 0; i < 16; i++) bar._leds.Add(kit.Box(bar, new Rect2(120 + i * 9, 15, 7, 14), new UiFx(Pin: true)));
        bar._actual = kit.Box(bar, new Rect2(120, 34, 0, 2), new UiFx(Fill: "graphic", Pin: true));
        bar._readout = kit.Text(bar, "", "data", "text.1", new Vector2(120, 40), true);
        model.Changed += bar.Sync;
        bar.Sync();
        return bar;
    }

    public static int Gear(float speed) => Math.Clamp((int)Math.Round(1 + 15 * Math.Log(Math.Max(speed, 0.01) / 0.5) / Math.Log(400)), 1, 16);

    public void Sync()
    {
        var walk = _model.Mode == ViewMode.Walk;
        _mode.Select(walk ? 1 : 0, notify: false);
        if (!walk)
        {
            var gear = Gear(_model.Speed);
            for (var i = 0; i < 16; i++)
            {
                var led = _leds[i];
                led.Visible = true;
                var current = i == gear - 1;
                led.Position = new Vector2(120 + i * 9, current ? 13 : 15);
                led.Size = new Vector2(7, current ? 18 : 14);
                _kit.Apply(led, current ? new UiFx(Fill: "primary.fill", Line: "primary.line", Pin: true)
                    : i < gear - 1 ? new UiFx(Fill: "graphic", Pin: true) : new UiFx(Fill: "surface.sunk", Line: "line.hair", Pin: true), remember: false);
            }
            _actual.Size = new Vector2(gear * 9 - 2, 2);
            var anchor = Array.FindLastIndex(Anchors, a => a <= gear);
            var name = anchor >= 0 ? " · " + UiKit.T("hud.speed." + AnchorKeys[anchor]) : "";
            _readout.Text = $"{gear:00}/16 · {_model.Speed:0.0} m/s{name}";
        }
        else
        {
            var ratio = _model.Speed / 1.4f;
            var step = 0;
            for (var i = 0; i < WalkSteps.Length; i++) if (MathF.Abs(WalkSteps[i] - ratio) < MathF.Abs(WalkSteps[step] - ratio)) step = i;
            for (var i = 0; i < 16; i++)
            {
                var led = _leds[i];
                led.Visible = i < WalkSteps.Length;
                if (!led.Visible) continue;
                led.Position = new Vector2(120 + i * 18, i == step ? 13 : 15);
                led.Size = new Vector2(16, i == step ? 18 : 14);
                _kit.Apply(led, i == step ? new UiFx(Fill: "primary.fill", Line: "primary.line", Pin: true)
                    : i < step ? new UiFx(Fill: "graphic", Pin: true) : new UiFx(Fill: "surface.sunk", Line: "line.hair", Pin: true), remember: false);
            }
            _actual.Size = new Vector2((step + 1) * 18 - 2, 2);
            _readout.Text = $"{WalkSteps[step]:0.0#}× · {_model.Speed:0.0} m/s";
        }
    }
}

/// <summary>收起态的时间读数条（UI-VISUAL §7.5）：钟点（Inter Light 26，tnum）+ 时辰 + 季 · 天气 + 过孔（自动空心、手动实心）。界面态显示，漫游态不显示。</summary>
public partial class TimeStrip : Control
{
    private UiKit _kit = null!;
    private HudModel _model = null!;
    private Label _clock = null!, _text = null!;
    private Panel _via = null!;

    public static TimeStrip Create(UiKit kit, Control parent, HudModel model)
    {
        var strip = new TimeStrip { _kit = kit, _model = model, Size = kit.Tokens.V2("metrics", "size", "timeStrip"), MouseFilter = MouseFilterEnum.Stop };
        parent.AddChild(strip);
        kit.Box(strip, new Rect2(Vector2.Zero, strip.Size), new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: kit.Tokens.Chamfer("panel"),
            Flags: UiFlags.Highlight, Pin: true));
        strip._clock = kit.Text(strip, "", "readout.strip", "text.1", new Vector2(20, 13), true);
        strip._text = kit.Text(strip, "", "small", "text.2", new Vector2(104, 18), true);
        strip._via = kit.Dot(strip, Vector2.Zero, 6, new UiFx(Line: "graphic", Pin: true));
        model.Changed += strip.Sync;
        strip.Sync();
        return strip;
    }

    public void Sync()
    {
        var clock = _model.Clock;
        _clock.Text = $"{(int)clock:00}:{(int)((clock % 1) * 60):00}";
        _text.Text = $"{UiKit.T("time.shichen." + TimeCard.Shichen(clock))} · {UiKit.T("hud.season." + TimeCard.SeasonCode(_model.Season).ToLowerInvariant())} · " +
                     UiKit.T("weather.cell." + WeatherStrip.CellKey(WeatherStrip.CellOf(_model.Weather.Kind)));
        _via.Position = new Vector2(104 + _kit.Width(_text) + 10, 25);
        var manual = _model.Weather.Mode == WeatherMode.Manual;
        _kit.Apply(_via, new UiFx(Fill: manual ? "graphic" : null, Line: manual ? null : "graphic", Flags: UiFlags.Circle, Pin: true), remember: false);
    }
}

/// <summary>
/// 临时提示（UI-VISUAL §7.4、UI-NAV §4.9）：高 40、surface.panel、左侧 3 px 类型条、20 px 图标、Medium 16；
/// 同时只显示 1 条，新的顶替旧的，2 s 内淡出；底部居中、离底边 150 px。常驻件，固定深屏；不配提示音。
/// </summary>
public partial class Toast : Control
{
    private UiKit _kit = null!;
    private Panel _panel = null!, _bar = null!;
    private Label _label = null!;
    private TextureRect? _icon;
    private Tween? _fade;

    public static Toast Create(UiKit kit, Control parent)
    {
        var toast = new Toast { _kit = kit, MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        parent.AddChild(toast);
        toast._panel = kit.Box(toast, new Rect2(0, 0, 200, 40), new UiFx(Fill: "surface.panel", Line: "line.panel", Pin: true));
        toast._bar = kit.Box(toast, new Rect2(0, 0, 3, 40), new UiFx(Fill: "primary.line", Pin: true));
        toast._label = kit.Text(toast, "", "body.strong", "text.1", new Vector2(44, 7), true);
        return toast;
    }

    public void Show(string text, string? icon = null, string bar = "primary.line", float hold = 1.4f)
    {
        _label.Text = text;
        _icon?.QueueFree();
        _icon = icon is null ? null : _kit.IconRect(this, icon, new Rect2(14, 10, 20, 20), "text.1", true);
        _label.Position = new Vector2(icon is null ? 18 : 44, 7);
        var width = _label.Position.X + _kit.Width(_label) + 20;
        _panel.Size = new Vector2(width, 40);
        _kit.Apply(_bar, new UiFx(Fill: bar, Pin: true), remember: false);
        var vp = GetViewportRect().Size;
        Position = new Vector2(Mathf.Round((vp.X - width) / 2), vp.Y - _kit.Tokens.Size("toastBottom") - 40);
        Visible = true;
        Modulate = Colors.White;
        _fade?.Kill();
        _fade = CreateTween();
        _fade.TweenInterval(hold);
        _fade.TweenProperty(this, "modulate:a", 0f, _kit.Motion.Fade(0.6f));
        _fade.TweenCallback(Callable.From(() => Visible = false));
    }
}
