using Campus.Contracts;

namespace Campus.Kernel.Stubs;

/// <summary>D0 weather readouts with a permanently clear, dry presentation and lossless DTO round trips.</summary>
[KernelService(typeof(IWeatherProcess), Owner = "WP-00", IsStub = true)]
public sealed class WeatherProcessStub : IWeatherProcess
{
    private static readonly WeatherSel ClearSelection = new(WeatherMode.Auto, WeatherKind.Clear, 0.5f);
    private static readonly WeatherVec ClearVector = new(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0);
    private readonly Dictionary<MapId, WeatherProcessState> _states = [];
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

    public void Reset(WeatherBoot b)
    {
        ArgumentNullException.ThrowIfNull(b);
        Seed = b.Seed;
        _states.Clear();
        _activeMap = null;
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
        if (_activeMap is { } map)
            _states[map] = Capture(map) with { QuietUntilHours = untilHours };
    }

    public void Pin(double worldHours, WeatherKind k, float? intensity, double fromHours, double toHours)
    {
        _worldHours = worldHours;
        Select(new WeatherSel(WeatherMode.Auto, k, intensity ?? 0.5f));
        if (_activeMap is { } map)
            _states[map] = Capture(map) with
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
        if (_states.TryGetValue(map, out var state)) RestoreReadouts(state);
        else if (_states.Count > 0) Select(ClearSelection);
        _worldHours = worldHours;
    }

    public void Leave(MapId map, double worldHours)
    {
        if (_activeMap != map) return;
        _worldHours = worldHours;
        _states[map] = Capture(map);
        _activeMap = null;
    }

    public WeatherProcessState Capture(MapId map)
    {
        var known = _states.TryGetValue(map, out var stored);
        var state = known ? stored! : new WeatherProcessState(
            map, 0, 0, ClearSelection, Array.Empty<WeatherQueueItem>(), 0,
            null, null, null, null, 0, 0, 0, _worldHours);
        if (_activeMap == map || (_activeMap is null && !known))
            state = state with
            {
                Selection = Selection,
                LeftAtHours = _worldHours,
                Season = _season,
                ClockOffset = _clockOffset,
                WetOverride = _wetOverride,
            };
        return Copy(state);
    }

    public WeatherProcessState CaptureAt(MapId map, in WorldSaveMoment moment)
    {
        if (!double.IsFinite(moment.WorldHours) || moment.WorldHours < 0 ||
            !double.IsFinite(moment.ClockOffset) || !double.IsFinite(moment.WorldHours + moment.ClockOffset) ||
            !Enum.IsDefined(moment.Season) || moment.FlowRate is not (0 or 1 or 10 or 60 or 360 or 1440))
            throw new ArgumentOutOfRangeException(nameof(moment));
        if (_activeMap != map) return Capture(map);
        // D0 has no evolving weather model. Preserve its settled DTO and change only the copy's endpoint.
        // Capture already copies Queue; neither this operation nor the caller can mutate live state.
        return Capture(map) with
        {
            LeftAtHours = moment.WorldHours,
            ClockOffset = moment.ClockOffset,
            Season = moment.Season,
        };
    }

    public void Restore(MapId map, WeatherProcessState s)
    {
        ArgumentNullException.ThrowIfNull(s);
        if (s.Map != map) throw new ArgumentException("Weather snapshot belongs to a different map.", nameof(s));
        var state = Copy(s);
        _states[map] = state;
        if (_activeMap is null || _activeMap == map)
        {
            _activeMap = map;
            RestoreReadouts(state);
        }
    }

    private void RestoreReadouts(WeatherProcessState state)
    {
        // Restore does not call Select: that user operation would clear WetOverride.
        Selection = state.Selection;
        _worldHours = state.LeftAtHours;
        _season = state.Season;
        _clockOffset = state.ClockOffset;
        _wetOverride = state.WetOverride;
    }

    private static WeatherProcessState Copy(WeatherProcessState state) => state with
    {
        // Snapshot callers cannot mutate the stub through a mutable IReadOnlyList implementation.
        Queue = Array.AsReadOnly(state.Queue.ToArray()),
    };
}
