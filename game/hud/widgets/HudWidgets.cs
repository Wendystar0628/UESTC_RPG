using Campus.Contracts;
using Campus.Hud.Style;
using Godot;

namespace Campus.Hud.Widgets;

/// <summary>
/// 天气签条（UI-VISUAL、UI-NAV）：自动 + 8 格，36 px 图标钮放在一条签条里；不合季节的雷雨格呈禁用态、可聚焦；
/// 强度三枚小方块只在雾、降水、雷雨下出现；自动时当前实际天气所在的格下方一枚 4 px 过孔点。常驻件，默认固定深屏。
/// </summary>
public partial class WeatherStrip : PanelContainer
{
    public const int Auto = 0, Clear = 1, Few = 2, Cloudy = 3, Overcast = 4, Fog = 5, Precip = 6, Thunder = 7, After = 8;

    private readonly List<TechButton> _cells = [];
    private readonly List<Panel> _intensity = [];
    private readonly List<TechButton> _intensityHits = [];
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
            CustomMinimumSize = new Vector2(kit.Tokens.V2("metrics", "size", "weatherStrip").X, 0),
            ThemeTypeVariation = UiThemeBuilder.PanelContainerType,
            Material = kit.Materials.Panel,
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            MouseFilter = MouseFilterEnum.Ignore
        };
        parent.AddChild(strip);
        strip.Build();
        strip.Sync();
        return strip;
    }

    private void Build()
    {
        var cell = _kit.Tokens.Size("weatherCell");
        _kit.Apply(this, new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: _kit.Tokens.Chamfer("card"),
            Flags: UiFlags.Highlight, Pin: true));
        var row = _kit.Layout.Flow(_kit.Layout.Margin(this));
        for (var i = 0; i <= After; i++)
        {
            var b = _kit.Button(row, ButtonStyle.Cell, "", icon: Icon(i, Season.Autumn), pin: true);
            b.CustomMinimumSize = new Vector2(cell, cell);
            b.Value = i;
            b.TooltipText = UiKit.T("weather.cell." + CellKey(i));
            var index = i;
            b.Pressed += () => Pick(index);
            _cells.Add(b);
        }
        _actual = _kit.Box(_cells[Clear], new UiFx(Fill: "graphic", Flags: UiFlags.Circle, Pin: true));
        PlaceActual();
        for (var i = 0; i < 3; i++)
        {
            var level = i;
            var hit = _kit.Button(row, ButtonStyle.Cell, "", pin: true);
            hit.CustomMinimumSize = new Vector2(cell / 2, cell);
            var square = _kit.Box(hit, new UiFx(Line: "line.control", Pin: true));
            square.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
            var edge = _kit.Tokens.F("metrics", "via", "small") * 2;
            square.OffsetLeft = square.OffsetTop = -edge / 2;
            square.OffsetRight = square.OffsetBottom = edge / 2;
            _intensity.Add(square);
            _intensityHits.Add(hit);
            hit.Pressed += () => { _control.SetWeatherIntensity((level + 1) / 3f); Sync(); };
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
        if (_actual.GetParent() != _cells[current]) { _actual.Reparent(_cells[current], false); PlaceActual(); }
        var showIntensity = current is Fog or Precip or Thunder;
        var level = Math.Clamp((int)MathF.Ceiling(sel.Intensity * 3) - 1, 0, 2);
        for (var i = 0; i < 3; i++)
        {
            _intensity[i].Visible = showIntensity;
            _intensityHits[i].Visible = showIntensity;
            var on = i == level;
            var role = sel.Mode == WeatherMode.Auto ? "text.2" : "graphic";
            _kit.Apply(_intensity[i], new UiFx(Fill: on ? role : null, Line: on ? null : "line.control", Pin: true), remember: false);
        }
    }

    private void PlaceActual()
    {
        var edge = _kit.Tokens.F("metrics", "via", "small");
        _actual.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        _actual.OffsetLeft = -edge / 2;
        _actual.OffsetRight = edge / 2;
        _actual.OffsetTop = -edge;
        _actual.OffsetBottom = 0;
    }

    private void Pick(int cell)
    {
        if (cell == Auto) { _control.SetWeatherAuto(); Sync(); return; }
        var kind = Kind(cell, _model.Season);
        if (!_model.World.IsNative(kind)) { Toast?.Invoke(UiKit.T("weather.thunder.off_season_toast")); return; }
        _control.SetWeather(kind);
        Sync();
        Toast?.Invoke(UiKit.T("weather.manual_toast", ("name", UiKit.T("weather.cell." + CellKey(cell)))));
    }

    /// <summary>Y 键单击：本季可选的格里循环到下一格，进入手动（UI-NAV）。</summary>
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
/// 观察模式与速度 LED 条（UI-VISUAL、UI-NAV）：[旁观 | 行走] 两段；旁观 16 格 7×14，已到达 graphic 实心，
/// 当前档 primary.fill + primary.line 加高到 18；格子下方 2 px 光条是贴地限速后的实际速度；行走换成 5 个宽格。常驻件。
/// </summary>
public partial class SpeedBar : PanelContainer
{
    private static readonly int[] Anchors = [4, 7, 9, 12, 16];
    private static readonly string[] AnchorKeys = ["stroll", "cycle", "cruise", "bird", "cloud"];

    private readonly List<Panel> _leds = [];
    private UiKit _kit = null!;
    private HudModel _model = null!;
    private Segmented _mode = null!;
    private Panel _actual = null!;
    private Label _readout = null!;
    private int _ledCount;
    private float _ledGap, _ledWalkWidth, _ledOffsetY, _ledStripHeight, _ledCurrentHeight;
    private Vector2 _ledSize;

    public event Action<ViewMode>? ModePicked;

    public static SpeedBar Create(UiKit kit, Control parent, HudModel model)
    {
        var bar = new SpeedBar
        {
            _kit = kit, _model = model,
            CustomMinimumSize = new Vector2(kit.Tokens.V2("metrics", "size", "speedBar").X, 0),
            ThemeTypeVariation = UiThemeBuilder.PanelContainerType, Material = kit.Materials.Panel,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin, MouseFilter = MouseFilterEnum.Ignore,
            _ledCount = kit.Tokens.I("metrics", "speedLed", "count"),
            _ledSize = kit.Tokens.V2("metrics", "speedLed", "size"),
            _ledCurrentHeight = kit.Tokens.F("metrics", "speedLed", "currentHeight"),
            _ledGap = kit.Tokens.F("metrics", "speedLed", "gap"),
            _ledWalkWidth = kit.Tokens.F("metrics", "speedLed", "walkWidth"),
            _ledOffsetY = kit.Tokens.F("metrics", "speedLed", "offsetY"),
            _ledStripHeight = kit.Tokens.F("metrics", "speedLed", "stripHeight"),
        };
        parent.AddChild(bar);
        kit.Apply(bar, new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: kit.Tokens.Chamfer("panel"),
            Flags: UiFlags.Highlight | UiFlags.Via, Pin: true));
        var row = kit.Layout.Row(kit.Layout.Margin(bar));
        bar._mode = Segmented.Create(kit, row, ["", ""], true, icons: ["view_free", "view_eye"]);
        bar._mode.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        bar._mode.Items[0].TooltipText = UiKit.T("hud.mode.fly");
        bar._mode.Items[1].TooltipText = UiKit.T("hud.mode.walk");
        bar._mode.Changed += i => bar.ModePicked?.Invoke(i == 0 ? ViewMode.Fly : ViewMode.Walk);
        var readout = kit.Layout.Stack(row, groups: false);
        var leds = kit.Layout.Add(readout, new Control
        {
            CustomMinimumSize = new Vector2(bar._ledCount * (bar._ledSize.X + bar._ledGap), kit.Tokens.F("metrics", "speedLed", "height")),
        });
        for (var i = 0; i < bar._ledCount; i++) bar._leds.Add(kit.Box(leds, new UiFx(Pin: true)));
        bar._actual = kit.Box(leds, new UiFx(Fill: "graphic", Pin: true));
        bar._actual.Position = new Vector2(0, kit.Tokens.F("metrics", "speedLed", "stripY"));
        bar._readout = kit.Text(readout, "", "data", "text.1", true);
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
            var pitch = _ledSize.X + _ledGap;
            for (var i = 0; i < _ledCount; i++)
            {
                var led = _leds[i];
                led.Visible = true;
                var current = i == gear - 1;
                led.Position = new Vector2(i * pitch, current ? 0 : _ledOffsetY);
                led.Size = new Vector2(_ledSize.X, current ? _ledCurrentHeight : _ledSize.Y);
                _kit.Apply(led, current ? new UiFx(Fill: "primary.fill", Line: "primary.line", Pin: true)
                    : i < gear - 1 ? new UiFx(Fill: "graphic", Pin: true) : new UiFx(Fill: "surface.sunk", Line: "line.hair", Pin: true), remember: false);
            }
            _actual.Size = new Vector2(gear * pitch - _ledGap, _ledStripHeight);
            var anchor = Array.FindLastIndex(Anchors, a => a <= gear);
            var name = anchor >= 0 ? " · " + UiKit.T("hud.speed." + AnchorKeys[anchor]) : "";
            _readout.Text = $"{gear:00}/16 · {_model.Speed:0.0} m/s{name}";
        }
        else
        {
            var ratio = _model.Speed / WalkRules.WalkSpeed;
            var step = 0;
            for (var i = 0; i < WalkRules.SpeedStepCount; i++) if (MathF.Abs(WalkRules.SpeedMultiplier(i) - ratio) < MathF.Abs(WalkRules.SpeedMultiplier(step) - ratio)) step = i;
            var pitch = _ledWalkWidth + _ledGap;
            for (var i = 0; i < _ledCount; i++)
            {
                var led = _leds[i];
                led.Visible = i < WalkRules.SpeedStepCount;
                if (!led.Visible) continue;
                led.Position = new Vector2(i * pitch, i == step ? 0 : _ledOffsetY);
                led.Size = new Vector2(_ledWalkWidth, i == step ? _ledCurrentHeight : _ledSize.Y);
                _kit.Apply(led, i == step ? new UiFx(Fill: "primary.fill", Line: "primary.line", Pin: true)
                    : i < step ? new UiFx(Fill: "graphic", Pin: true) : new UiFx(Fill: "surface.sunk", Line: "line.hair", Pin: true), remember: false);
            }
            _actual.Size = new Vector2((step + 1) * pitch - _ledGap, _ledStripHeight);
            _readout.Text = $"{WalkRules.SpeedMultiplier(step):0.0#}× · {_model.Speed:0.0} m/s";
        }
    }
}

/// <summary>收起态的时间读数条（UI-VISUAL）：钟点（Inter Light 26，tnum）+ 时辰 + 季 · 天气 + 过孔（自动空心、手动实心）。时间卡收起时常驻显示。</summary>
public partial class TimeStrip : PanelContainer
{
    private UiKit _kit = null!;
    private HudModel _model = null!;
    private Label _clock = null!, _text = null!;
    private Panel _via = null!;

    public static TimeStrip Create(UiKit kit, Control parent, HudModel model)
    {
        var strip = new TimeStrip
        {
            _kit = kit, _model = model,
            CustomMinimumSize = new Vector2(kit.Tokens.V2("metrics", "size", "timeStrip").X, 0),
            ThemeTypeVariation = UiThemeBuilder.PanelContainerType, Material = kit.Materials.Panel,
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd, MouseFilter = MouseFilterEnum.Stop
        };
        parent.AddChild(strip);
        kit.Apply(strip, new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: kit.Tokens.Chamfer("panel"),
            Flags: UiFlags.Highlight, Pin: true));
        var row = kit.Layout.Row(kit.Layout.Margin(strip));
        strip._clock = kit.Text(row, "", "readout.strip", "text.1", true, wrap: false);
        strip._clock.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
        strip._text = kit.Text(row, "", "small", "text.2", true);
        strip._text.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        strip._via = kit.Box(row, new UiFx(Line: "graphic", Flags: UiFlags.Circle, Pin: true));
        var via = kit.Tokens.F("metrics", "via", "core");
        strip._via.CustomMinimumSize = new Vector2(via, via);
        strip._via.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        strip.Sync();
        return strip;
    }

    public void Sync()
    {
        var clock = _model.Clock;
        _clock.Text = WorldTime.FormatClock(clock);
        _text.Text = $"{UiKit.T("time.shichen." + TimeCard.Shichen(clock))} · {UiKit.T("hud.season." + TimeCard.SeasonCode(_model.Season).ToLowerInvariant())} · " +
                     UiKit.T("weather.cell." + WeatherStrip.CellKey(WeatherStrip.CellOf(_model.Weather.Kind)));
        var manual = _model.Weather.Mode == WeatherMode.Manual;
        _kit.Apply(_via, new UiFx(Fill: manual ? "graphic" : null, Line: manual ? null : "graphic", Flags: UiFlags.Circle, Pin: true), remember: false);
    }
}

/// <summary>
/// 临时提示（UI-VISUAL、UI-NAV）：高 40、surface.panel、左侧 3 px 类型条、20 px 图标、Medium 16；
/// 同时只显示 1 条，新的顶替旧的，2 s 内淡出；底部居中、离底边 150 px。常驻件，固定深屏；不配提示音。
/// </summary>
public partial class Toast : PanelContainer
{
    private UiKit _kit = null!;
    private Panel _bar = null!;
    private Label _label = null!;
    private TextureRect? _icon;
    private Tween? _fade;

    public static Toast Create(UiKit kit, Control parent)
    {
        var toast = new Toast
        {
            _kit = kit, MouseFilter = MouseFilterEnum.Ignore, Visible = false,
            ThemeTypeVariation = UiThemeBuilder.PanelContainerType, Material = kit.Materials.Panel,
            GrowHorizontal = GrowDirection.Both, GrowVertical = GrowDirection.Begin
        };
        parent.AddChild(toast);
        kit.Apply(toast, new UiFx(Fill: "surface.panel", Line: "line.panel", Pin: true));
        toast.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        toast.OffsetTop = toast.OffsetBottom = -kit.Tokens.Size("toastBottom");
        var row = kit.Layout.Row(kit.Layout.Margin(toast));
        toast._icon = kit.IconRect(row, "help", pin: true);
        toast._icon.Visible = false;
        toast._label = kit.Text(row, "", "body.strong", "text.1", true);
        // 类型条是面板自己的装饰，不参加内容排版。
        var decoration = kit.Layout.Host(toast, "ToastDecoration");
        toast._bar = kit.Box(decoration, new UiFx(Fill: "primary.line", Pin: true));
        toast._bar.SetAnchorsAndOffsetsPreset(LayoutPreset.LeftWide);
        toast._bar.OffsetRight = kit.Tokens.F("metrics", "line", "toastBar");
        UiLayout.BindResize(parent, toast, toast.Fit);
        return toast;
    }

    public void Show(string text, string? icon = null, string bar = "primary.line", float hold = 1.4f)
    {
        _label.Text = text;
        _icon!.Visible = icon is not null;
        if (icon is not null) _icon.Texture = _kit.Icon(icon);
        Fit();
        _kit.Apply(_bar, new UiFx(Fill: bar, Pin: true), remember: false);
        Visible = true;
        Modulate = Colors.White;
        _fade?.Kill();
        _fade = CreateTween();
        _fade.TweenInterval(hold);
        _fade.TweenProperty(this, "modulate:a", 0f, _kit.Motion.Fade(0.6f));
        _fade.TweenCallback(Callable.From(() => Visible = false));
    }

    private void Fit()
    {
        var font = _kit.Theme.Font("body.strong", out var size, out _);
        var icon = _icon!.Visible ? _kit.Layout.Metric("icon") + _kit.Layout.Metric("rowGap") : 0;
        var width = font.GetStringSize(_label.Text, HorizontalAlignment.Left, -1, size).X + icon + _kit.Tokens.Space("panelPad") * 2;
        CustomMinimumSize = new Vector2(Math.Min(width, GetParentControl().Size.X), 0);
        CustomMaximumSize = GetParentControl().Size;
        ResetSize();
    }
}
