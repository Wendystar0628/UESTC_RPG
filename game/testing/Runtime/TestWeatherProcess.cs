using Campus.Contracts;
using Campus.Systems.Weather;

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
    public List<WeatherTick> Steps { get; } = [];
    public List<string> Calls { get; } = [];
    public int Resets { get; private set; }
    public int Reseasons { get; private set; }
    public int Quiets { get; private set; }
    public int Enters { get; private set; }
    public double PinFrom { get; private set; }
    public double PinTo { get; private set; }
    public double QuietTo { get; private set; }
    public double LastEnterHours { get; private set; }
    public WeatherKind PinnedKind { get; private set; }
    public float? PinnedIntensity { get; private set; }

    public void ValidateBoot(WeatherBoot boot, Season season)
    {
        ArgumentNullException.ThrowIfNull(boot);
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        if (boot.Kind is { } kind && !IsNative(kind, season)) throw new ArgumentException("Weather is not native to the requested season.", nameof(boot));
    }
    public WeatherKind MapToSeason(WeatherKind kind, Season season) => WeatherRules.MapToSeason(kind, season);

    public void Reset(WeatherBoot b)
    {
        ArgumentNullException.ThrowIfNull(b);
        Resets++;
        Seed = b.Seed;
        _state = null;
        _worldHours = 17.5;
        _season = Season.Autumn;
        _clockOffset = 0;
        ApplySelection(new WeatherSel(b.Kind.HasValue ? WeatherMode.Manual : WeatherMode.Auto,
            b.Kind ?? WeatherKind.Clear, b.Intensity ?? 0.5f));
        _wetOverride = b.Wet;
    }

    public void Step(in WeatherTick t)
    {
        Steps.Add(t);
        _worldHours = t.WorldHours;
        _season = t.Season;
        _clockOffset = t.ClockOffset;
    }

    public bool IsNative(WeatherKind k, Season s) => WeatherRules.IsNative(k, s);

    public void Select(WeatherSel sel)
    {
        ApplySelection(sel);
        Calls.Add("select");
    }

    private void ApplySelection(WeatherSel sel)
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
        Reseasons++;
        Calls.Add("reseason");
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
        ApplySelection(Selection with { Kind = kind });
    }

    public void Quiet(double worldHours, double untilHours)
    {
        Quiets++;
        QuietTo = untilHours;
        Calls.Add("quiet");
        _worldHours = worldHours;
        ApplySelection(ClearSelection);
        _state = Capture() with { QuietUntilHours = untilHours };
    }

    public void Pin(double worldHours, WeatherKind k, float? intensity, double fromHours, double toHours)
    {
        PinnedKind = k;
        PinnedIntensity = intensity;
        PinFrom = fromHours;
        PinTo = toHours;
        Calls.Add("pin");
        _worldHours = worldHours;
        ApplySelection(new WeatherSel(WeatherMode.Auto, k, intensity ?? 0.5f));
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
        Enters++;
        LastEnterHours = worldHours;
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
