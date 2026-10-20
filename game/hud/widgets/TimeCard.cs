using Campus.Contracts;
using Campus.Hud.Style;
using Godot;
using N = System.Numerics;

namespace Campus.Hud.Widgets;

/// <summary>
/// 时间卡（UI-VISUAL、UI-NAV）：24 h 天色光谱环，12 在顶、00 在底、06 在左、18 在右，钟点顺时针增加；
/// 只能顺时针拨：拖动按 ClockSetMode.Drag 累计顺时针角度，逆时针时把手不动；点刻度按 Jump 跳到下一次出现。
/// 拆三层：静态环与刻度只画一次；把手与细针只改变换；读数 10 Hz。常驻件，默认固定深屏。
/// </summary>
public partial class TimeCard : PanelContainer
{
    public static ReadOnlySpan<int> Rates => FlowRates.All;
    private static readonly string[] ShichenNames = ["zi", "chou", "yin", "mao", "chen", "si", "wu", "wei", "shen", "you", "xu", "hai"];
    private const float Ring = 104, Band = 12, HandleR = 98;

    private UiKit _kit = null!;
    private HudModel _model = null!;
    private IWorldControl _control = null!;
    private IPaletteProbe _probe = null!;
    private Func<string> _resetName = () => "";
    private SpectrumRing _spectrum = null!;
    private Panel _sun = null!, _moon = null!;
    private Line2D _needle = null!;
    private Label _readout = null!, _period = null!, _tag = null!, _forecast = null!, _flowText = null!;
    private Segmented _flow = null!, _seasons = null!, _presets = null!;
    private ColorRect _wave = null!;
    private TechButton _reset = null!, _plus = null!;
    private Vector2 _center;
    private (Season, WeatherMode, WeatherKind)? _spectrumKey;
    private bool _dragging;
    private float _dragAngle;
    private double _dragTarget;

    public event Action<string>? Toast;

    public static TimeCard Create(UiKit kit, Control parent, HudModel model, IWorldControl control, IPaletteProbe probe, Func<string> resetName)
    {
        var card = new TimeCard
        {
            _kit = kit,
            _model = model,
            _control = control,
            _probe = probe,
            _resetName = resetName,
            CustomMinimumSize = new Vector2(kit.Tokens.V2("metrics", "size", "timeCard").X, 0),
            ThemeTypeVariation = UiThemeBuilder.PanelContainerType,
            Material = kit.Materials.Panel,
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        parent.AddChild(card);
        card.Build();
        card.Sync();
        return card;
    }

    private void Build()
    {
        var k = _kit;
        var pin = true;
        k.Apply(this, new UiFx(Fill: "surface.panel", Line: "line.panel", Chamfer: k.Tokens.Chamfer("panel"),
            Flags: UiFlags.Highlight | UiFlags.Via, Pin: pin));
        var scroll = k.Layout.Scroll(k.Layout.Margin(this));
        scroll.VerticalScrollMode = ScrollContainer.ScrollMode.MaximizeFirst;
        scroll.CustomMaximumSize = new Vector2(-1,
            k.Tokens.V2("metrics", "size", "timeCard").Y - 2 * k.Tokens.Space("panelPad"));
        var stack = k.Layout.Stack(scroll, groups: false);
        var header = k.Layout.Row(stack);
        k.Text(header, UiKit.T("hud.time.title"), "mono-caps", "text.2", pin);
        var lamp = k.Box(header, new UiFx(Fill: "led.on", Flags: UiFlags.Circle, Pin: pin));
        lamp.CustomMinimumSize = new Vector2(k.Tokens.F("metrics", "via", "core"), k.Tokens.F("metrics", "via", "core"));
        lamp.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        _flowText = k.Text(header, "1×", "data", "text.1", pin, wrap: false);
        _flowText.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;

        BuildDial(stack, pin);
        BuildControls(stack, pin);
    }

    private void BuildDial(Control stack, bool pin)
    {
        var k = _kit;
        var center = k.Layout.Add(stack, new CenterContainer());
        var dial = k.Layout.Add(center, new Control { CustomMinimumSize = new Vector2((Ring + 26) * 2, (Ring + 26) * 2) });
        dial.MouseFilter = MouseFilterEnum.Stop;
        dial.GuiInput += HandleRingInput;
        _center = dial.CustomMinimumSize / 2;
        _spectrum = new SpectrumRing { Position = _center - new Vector2(110, 110), Size = new Vector2(220, 220), MouseFilter = MouseFilterEnum.Ignore };
        dial.AddChild(_spectrum);
        k.Dot(dial, _center, (Ring + 1) * 2, new UiFx(Line: "line.hair", Pin: pin));
        k.Dot(dial, _center, (Ring - Band - 1) * 2, new UiFx(Line: "line.hair", Pin: pin));
        var ticks = new TickRing { Position = _center - new Vector2(110, 110), Size = new Vector2(220, 220), MouseFilter = MouseFilterEnum.Ignore };
        ticks.Material = k.Materials.Text;
        dial.AddChild(ticks);
        k.Recolor(ticks, "line.control", pin);
        foreach (var (h, label) in new[] { (12, "12"), (18, "18"), (0, "00"), (6, "06") })
        {
            var p = Dir(h) * (Ring + 16) + _center;
            var tick = k.Text(dial, label, "tick", "text.2", pin, wrap: false, align: HorizontalAlignment.Center);
            tick.Position = p - new Vector2(12, 7);
            tick.Size = new Vector2(24, tick.GetCombinedMinimumSize().Y);
        }
        _needle = k.Line(dial, [Vector2.Zero, Vector2.One], "graphic", pin);
        _sun = k.Dot(dial, Vector2.Zero, 16, new UiFx(Fill: "now", Line: "now.outline", Pin: pin));
        _moon = k.Dot(dial, Vector2.Zero, 16, new UiFx(Fill: "moon.fill", Flags: UiFlags.Glow, Pin: pin));

        var readouts = k.Layout.Stack(dial, groups: false);
        readouts.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        readouts.GrowHorizontal = GrowDirection.Both;
        readouts.GrowVertical = GrowDirection.Both;
        _readout = k.Text(readouts, "", "readout", "text.1", pin, wrap: false, align: HorizontalAlignment.Center);
        _period = k.Text(readouts, "", "small", "text.2", pin, wrap: false, align: HorizontalAlignment.Center);
        _tag = k.Text(readouts, "", "mono-caps", "text.2", pin, wrap: false, align: HorizontalAlignment.Center);

    }

    private void BuildControls(Control stack, bool pin)
    {
        var k = _kit;
        var presets = k.Layout.Flow(stack);
        _presets = Segmented.Create(k, presets,
            [UiKit.T("hud.time.preset.morning"), UiKit.T("hud.time.preset.noon"), UiKit.T("hud.time.preset.dusk"), UiKit.T("hud.time.preset.night")], pin);
        _presets.Changed += i =>
        {
            var preset = (ClockPreset)i;
            _control.SetClock(_model.World.PresetClock(preset, _model.Season), ClockSetMode.Jump);
            _presets.Select(-1, notify: false);
            Sync();
        };
        _plus = k.Button(presets, ButtonStyle.Ghost, "+1h", "data", pin: pin,
            pressed: () => { _control.Advance(1); Sync(); });
        var flow = k.Layout.Flow(stack);
        _flow = Segmented.Create(k, flow, FlowRates.All.ToArray().Select(rate => rate == 0 ? "" : $"{rate}×").ToArray(), pin, "data",
            icons: ["pause", null, null, null, null, null]);
        _flow.Changed += i => { _control.SetFlow(Rates[i]); Sync(); };
        _wave = k.Layout.Add(flow, new ColorRect
        {
            CustomMinimumSize = new Vector2(44, 24), Color = Colors.White,
            Material = k.Materials.Line, SizeFlagsVertical = SizeFlags.ShrinkCenter
        });
        k.Apply(_wave, new UiFx(Role: "graphic", Pin: pin, Flags: UiFlags.Wave));
        _seasons = Segmented.Create(k, stack,
            [UiKit.T("hud.season.spring"), UiKit.T("hud.season.summer"), UiKit.T("hud.season.autumn"), UiKit.T("hud.season.winter")], pin,
            icons: ["season_spring", "season_summer", "season_autumn", "season_winter"]);
        _seasons.Changed += i => { _control.SetSeason((Season)i); Sync(); };
        _forecast = k.Text(stack, "", "small", "text.2", pin);
        _reset = k.Button(stack, ButtonStyle.Text, "", "small", pin: pin,
            align: HorizontalAlignment.Right, pressed: () => { _control.ResetDefault(); Sync(); });
    }

    public void Sync()
    {
        var clock = _model.Clock;
        _readout.Text = WorldTime.FormatClock(clock);
        _period.Text = $"{UiKit.T("time.shichen." + Shichen(clock))} · {UiKit.T("time.period." + Period(clock))}";
        _tag.Text = $"{SeasonCode(_model.Season)} · {WeatherCode(_model.Weather.Kind)}".ToUpperInvariant();
        var angle = (float)((clock - 12.0) / 24.0 * Math.Tau);
        var dir = new Vector2(MathF.Sin(angle), -MathF.Cos(angle));
        var handle = _center + dir * HandleR;
        var night = _model.World.Sun.AltDeg < 0;
        _sun.Visible = !night;
        _moon.Visible = night;
        (night ? _moon : _sun).Position = handle - new Vector2(8, 8);
        _needle.SetPointPosition(0, _center + dir * 60);
        _needle.SetPointPosition(1, _center + dir * (HandleR - 9));
        var rateIndex = Rates.IndexOf(_model.FlowRate);
        if (rateIndex >= 0) _flow.Select(rateIndex, notify: false);
        _flowText.Text = _model.FlowRate == 0 ? "‖" : $"{_model.FlowRate}×";
        _kit.Apply(_wave, new UiFx(Role: "graphic", Pin: true, Flags: UiFlags.Wave,
            Extra: _model.FlowRate == 0 ? new Vector4(0, 1, 0, 1.2f)
                : new Vector4(0.25f, 1 + MathF.Log10(_model.FlowRate) * 1.5f, _kit.Motion.Spatial ? 0.25f : 0, 1.2f)), remember: false);
        _seasons.Select((int)_model.Season, notify: false);
        _reset.Text = UiKit.T("hud.time.reset", ("name", _resetName())) + " →";
        _forecast.Text = Forecast();
        _forecast.Visible = _model.Weather.Mode == WeatherMode.Auto;
        var allowClock = _model.Allows(WorldControlMask.Clock);
        _presets.Visible = _plus.Visible = allowClock;
        _flow.Visible = _wave.Visible = _model.Allows(WorldControlMask.Flow);
        _seasons.Visible = _model.Allows(WorldControlMask.Season);
        UpdateSpectrum();
    }

    private void UpdateSpectrum()
    {
        var key = (_model.Season, _model.Weather.Mode, _model.Weather.Kind);
        if (_spectrumKey == key) return;
        _spectrumKey = key;
        Span<N.Vector3> samples = stackalloc N.Vector3[24];
        _probe.SampleHorizon(_model.World.SeasonTarget, _model.World.WeatherNow, samples);
        var colors = new Color[24];
        for (var i = 0; i < 24; i++)
        {
            var lab = Oklab.FromSrgb(samples[i].X, samples[i].Y, samples[i].Z);
            var (r, g, b) = Oklab.ToLinear(new Lab(lab.L, lab.A * 0.7f, lab.B * 0.7f));
            // The spectrum clips the result, preserving its existing saturation rule.
            colors[i] = new Color((float)Oklab.LinearToSrgb(Math.Clamp(r, 0, 1)),
                (float)Oklab.LinearToSrgb(Math.Clamp(g, 0, 1)), (float)Oklab.LinearToSrgb(Math.Clamp(b, 0, 1)));
        }
        _spectrum.Samples = colors;
        _spectrum.QueueRedraw();
    }

    private string Forecast()
    {
        var ahead = _model.World.WeatherAhead;
        if (ahead.Count == 0) return UiKit.T("hud.forecast.steady");
        var now = _model.World.WorldHours;
        var parts = new List<string>();
        foreach (var span in ahead)
        {
            if (span.ToHours <= now) continue;
            var name = UiKit.T("weather.kind." + WeatherCode(span.Kind).ToLowerInvariant());
            if (parts.Count == 0 || parts[^1] != name) parts.Add(name);
            if (parts.Count == 3) break;
        }
        if (parts.Count <= 1) return UiKit.T("hud.forecast.steady");
        var lead = ahead[0].FromHours - now < 3 ? "hud.forecast.later" : "hud.forecast.tonight";
        return $"{UiKit.T(lead)} · {string.Join(" → ", parts)}";
    }

    // ---- 拖动：只能顺时针 ----
    private void HandleRingInput(InputEvent e)
    {
        if (!_model.Allows(WorldControlMask.Clock)) return;
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } mb when mb.Pressed && OnRing(mb.Position):
                _dragging = true;
                _dragAngle = AngleOf(mb.Position);
                _dragTarget = _model.Clock;
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } when _dragging:
                _dragging = false;
                AcceptEvent();
                break;
            case InputEventMouseMotion mm when _dragging:
                var a = AngleOf(mm.Position);
                var delta = Mathf.Wrap(a - _dragAngle, -MathF.PI, MathF.PI);
                if (delta > 0)
                {
                    _dragTarget = (_dragTarget + delta / Math.Tau * 24 + 24) % 24;
                    _control.SetClock(_dragTarget, ClockSetMode.Drag);
                    Sync();
                }
                else if (delta < -0.02f) Toast?.Invoke(UiKit.T("hud.time.forward_only"));
                if (delta > 0) _dragAngle = a;
                AcceptEvent();
                break;
        }
    }

    private bool OnRing(Vector2 p)
    {
        var d = (p - _center).Length();
        return d > Ring - Band - 14 && d < Ring + 14;
    }

    private float AngleOf(Vector2 p)
    {
        var v = p - _center;
        return MathF.Atan2(v.X, -v.Y);   // 0 在顶（12 点），顺时针为正
    }

    private static Vector2 Dir(double h)
    {
        var th = (h - 12.0) / 24.0 * Math.Tau;
        return new Vector2((float)Math.Sin(th), -(float)Math.Cos(th));
    }

    public static string Shichen(double clock)
    {
        return ShichenNames[(int)Math.Floor(((clock + 1) % 24) / 2)];
    }

    public static string Period(double clock) => clock switch
    {
        < 4 => "late_night",
        < 6 => "dawn",
        < 9 => "early_morning",
        < 11 => "morning",
        < 13 => "noon",
        < 16.5 => "afternoon",
        < 18.5 => "dusk",
        < 20 => "evening",
        _ => "night",
    };

    public static string SeasonCode(Season s) => s switch
    {
        Season.Spring => "Spring",
        Season.Summer => "Summer",
        Season.Autumn => "Autumn",
        Season.Winter => "Winter",
        _ => throw new ArgumentOutOfRangeException(nameof(s)),
    };

    public static string WeatherCode(WeatherKind k) => k switch
    {
        WeatherKind.Clear => "Clear",
        WeatherKind.FewClouds => "Few",
        WeatherKind.Cloudy => "Cloudy",
        WeatherKind.Overcast => "Overcast",
        WeatherKind.Fog => "Fog",
        WeatherKind.LightRain => "Drizzle",
        WeatherKind.Rain => "Rain",
        WeatherKind.Thunderstorm => "Storm",
        WeatherKind.AfterRain => "Clearing",
        WeatherKind.Snow => "Snow",
        WeatherKind.AfterSnow => "Clearing",
        _ => throw new ArgumentOutOfRangeException(nameof(k)),
    };
}

/// <summary>光谱环：24 个整点的地平线天色沿环插值（"再现场景"，不经界面色表；彩度已乘 0.7）。逐段画抗锯齿的粗弧。</summary>
public partial class SpectrumRing : Control
{
    public Color[] Samples { get; set; } = [];

    public override void _Draw()
    {
        if (Samples.Length != 24) return;
        var c = Size / 2;
        const float outer = 104, inner = 92;
        const int segments = 192;
        for (var i = 0; i < segments; i++)
        {
            double h0 = i * 24.0 / segments, h1 = (i + 1.6) * 24.0 / segments;
            var a0 = (float)((h0 - 12.0) / 24.0 * Math.Tau - Math.PI / 2);
            var a1 = (float)((h1 - 12.0) / 24.0 * Math.Tau - Math.PI / 2);
            DrawArc(c, (outer + inner) / 2, a0, a1, 4, Sample((h0 + h1) / 2), outer - inner, true);
        }
    }

    private Color Sample(double h)
    {
        var i = (int)Math.Floor(h) % 24;
        var t = (float)(h - Math.Floor(h));
        return Samples[i].Lerp(Samples[(i + 1) % 24], t);
    }
}

/// <summary>环内 24 根刻度：1×5 px，每 6 h 一根长刻（UI-VISUAL）；颜色由 ui_panel kind 3 按 role 换。</summary>
public partial class TickRing : Control
{
    public override void _Draw()
    {
        var c = Size / 2;
        for (var h = 0; h < 24; h++)
        {
            var th = (h - 12.0) / 24.0 * Math.Tau;
            var d = new Vector2((float)Math.Sin(th), -(float)Math.Cos(th));
            var len = h % 6 == 0 ? 9 : 5;
            if (h % 6 == 0) DrawLine(c + d * 90, c + d * (90 - len), Colors.White, -1);
            else DrawLine(c + d * 90, c + d * (90 - len), Colors.White, 1, true);
        }
    }
}
