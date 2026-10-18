using Campus.Contracts;

namespace Campus.Tests;

/// <summary>Test fixture weather readouts with a permanently clear, dry presentation and lossless DTO round trips.</summary>
internal sealed class TestWeatherProcess : IWeatherProcess
{
    private static readonly WeatherSel ClearSelection = new(WeatherMode.Auto, WeatherKind.Clear, 0.5f);
    private static readonly WeatherVec ClearVector = new(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0);
    private WeatherProcessState? _state;
    private MapId? _activeMap;
    private double _worldHours = 17.5;
    private Season _season = Season.Autumn;
    private double _clockOffset;
    private float? _wetOverride;

    public WeatherSel Selection { get; private set; } = ClearSelection;
    public WeatherVec Now => ClearVector;
    public IReadOnlyList<WeatherSpan> Ahead => Array.Empty<WeatherSpan>();
    public uint Seed { get; private set; }
    public bool Transitioning => false;

    public WeatherBoot ParseBootOptions(IReadOnlyDictionary<string, string> arguments, uint seed, Season season) => throw new NotSupportedException();
    public WeatherKind MapToSeason(WeatherKind kind, Season season) => throw new NotSupportedException();

    public void Reset(WeatherBoot b)
    {
        ArgumentNullException.ThrowIfNull(b);
        Seed = b.Seed;
        _state = null;
        _worldHours = 17.5;
        _season = Season.Autumn;
        _clockOffset = 0;
        Select(new WeatherSel(b.Kind.HasValue ? WeatherMode.Manual : WeatherMode.Auto,
            b.Kind ?? WeatherKind.Clear, b.Intensity ?? 0.5f));
        _wetOverride = b.Wet;
    }

    public void Step(in WeatherTick t)
    {
        _worldHours = t.WorldHours;
        _season = t.Season;
        _clockOffset = t.ClockOffset;
    }

    public bool IsNative(WeatherKind k, Season s)
    {
        if (!Enum.IsDefined(s)) throw new ArgumentOutOfRangeException(nameof(s));
        return k switch
        {
            WeatherKind.Clear or WeatherKind.FewClouds or WeatherKind.Cloudy or WeatherKind.Overcast or WeatherKind.Fog => true,
            WeatherKind.LightRain or WeatherKind.Rain or WeatherKind.AfterRain => s is not Season.Winter,
            WeatherKind.Thunderstorm => s is Season.Spring or Season.Summer,
            WeatherKind.Snow or WeatherKind.AfterSnow => s is Season.Winter,
            _ => throw new ArgumentOutOfRangeException(nameof(k)),
        };
    }

    public void Select(WeatherSel sel)
    {
        if (!Enum.IsDefined(sel.Mode) || !Enum.IsDefined(sel.Kind))
            throw new ArgumentOutOfRangeException(nameof(sel));
        if (!float.IsFinite(sel.Intensity) || sel.Intensity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(sel));
        Selection = sel;
        _wetOverride = null;
    }

    public void Reseason(double worldHours, Season to, double clockOffset)
    {
        var native = IsNative(Selection.Kind, to);
        _worldHours = worldHours;
        _season = to;
        _clockOffset = clockOffset;
        _wetOverride = null;
        if (native) return;
        var kind = Selection.Kind switch
        {
            WeatherKind.LightRain or WeatherKind.Rain => WeatherKind.Snow,
            WeatherKind.AfterRain => WeatherKind.AfterSnow,
            WeatherKind.Snow => WeatherKind.Rain,
            WeatherKind.AfterSnow => WeatherKind.AfterRain,
            WeatherKind.Thunderstorm => to switch
            {
                Season.Autumn => WeatherKind.Rain,
                Season.Winter => WeatherKind.Snow,
                _ => throw new ArgumentOutOfRangeException(nameof(to)),
            },
            _ => throw new InvalidOperationException("Unexpected non-native weather."),
        };
        Select(Selection with { Kind = kind });
    }

    public void Quiet(double worldHours, double untilHours)
    {
        _worldHours = worldHours;
        Select(ClearSelection);
        _state = Capture() with { QuietUntilHours = untilHours };
    }

    public void Pin(double worldHours, WeatherKind k, float? intensity, double fromHours, double toHours)
    {
        _worldHours = worldHours;
        Select(new WeatherSel(WeatherMode.Auto, k, intensity ?? 0.5f));
        _state = Capture() with
        {
            PinFromHours = fromHours,
            PinToHours = toHours,
            PinKind = k,
            PinIntensity = Selection.Intensity,
        };
    }

    public void Enter(MapId map, string profile, double worldHours)
    {
        _activeMap = map;
        _worldHours = worldHours;
    }

    public WeatherProcessState Capture() =>
        (_state ?? new WeatherProcessState(_activeMap ?? default, ClearSelection,
            null, null, null, null, 0, 0, 0, _worldHours)) with
        {
            Map = _activeMap ?? _state?.Map ?? default,
            Selection = Selection,
            LeftAtHours = _worldHours,
            Season = _season,
            ClockOffset = _clockOffset,
            WetOverride = _wetOverride,
        };

    public WeatherProcessState CaptureAt(in WorldSaveMoment moment)
    {
        if (!double.IsFinite(moment.WorldHours) || moment.WorldHours < 0 ||
            !double.IsFinite(moment.ClockOffset) || !double.IsFinite(moment.WorldHours + moment.ClockOffset) ||
            !Enum.IsDefined(moment.Season) || moment.FlowRate is not (0 or 1 or 10 or 60 or 360 or 1440))
            throw new ArgumentOutOfRangeException(nameof(moment));
        return Capture() with
        {
            LeftAtHours = moment.WorldHours,
            ClockOffset = moment.ClockOffset,
            Season = moment.Season,
        };
    }

    public void Restore(WeatherProcessState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
        // Restore does not call Select: that user operation would clear WetOverride.
        Selection = state.Selection;
        _worldHours = state.LeftAtHours;
        _season = state.Season;
        _clockOffset = state.ClockOffset;
        _wetOverride = state.WetOverride;
    }
}
