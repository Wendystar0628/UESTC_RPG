using System.Numerics;
using Campus.Contracts;

namespace Campus.Tests;

/// <summary>Test fixture world readouts. Explicit controls settle immediately; no clock runs in the background.</summary>
internal sealed class TestWorldState(IWeatherProcess weather, IRuleset ruleset) : IWorldState, IWorldControl, IViewControl, IWorldRestore
{
    private double _clockOffset;
    private bool _restorePending;
    private readonly List<TestFlowLease> _flows = [];
    private TestFlowLease? _activeFlow;
    public bool RestoreFaulted { get; private set; }
    public long Rev { get; private set; } = 1;
    public long Instance { get; private set; } = 1;
    public double WorldHours { get; private set; } = 17.5;
    public double Clock => ((WorldHours + _clockOffset) % 24 + 24) % 24;
    public double SeasonPos => (int)SeasonTarget;
    public Season SeasonTarget { get; private set; } = Season.Autumn;
    public Body Sun { get; } = new(new Vector3(-0.870593f, 0.095846f, 0.482578f), 5.5f, 241, 1);
    public Body Moon { get; } = new(new Vector3(0.870593f, -0.095846f, -0.482578f), -5.5f, 61, 0);
    public float MoonPhase => 1;
    public float Omega => 0;
    public float SunAltDegAt(double clock, Season season)
    {
        ValidateClock(clock);
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        return Sun.AltDeg; // Test fixture deliberately exposes a settled, fixed celestial state.
    }
    public Campus.Contracts.Flow Flow => new(_activeFlow?.Rate ?? BaseFlow.Rate);
    public Campus.Contracts.Flow BaseFlow { get; private set; } = new(1);
    public int LastFlowRate { get; private set; } = 1;
    public bool Scrubbing => false;
    public WorldTransition Transition => WorldTransition.None;
    public ViewMode Mode { get; private set; } = ViewMode.Fly;
    public WeatherSel Weather => weather.Selection;
    public WeatherVec WeatherNow => weather.Now;
    public IReadOnlyList<WeatherSpan> WeatherAhead => weather.Ahead;
    public uint WeatherSeed => weather.Seed;
    public double AnimTime { get; private set; } = 240;
    public double Step12 => Math.Floor(AnimTime * 12) / 12;
    public bool IsNative(WeatherKind k) => weather.IsNative(k, SeasonTarget);

    public double PresetClock(ClockPreset preset, Season season)
    {
        // SYS-TIME section 2: art-directed sunrise/sunset, independent of the real calendar.
        (int sunriseMinutes, int sunsetMinutes) = season switch
        {
            Season.Spring => (6 * 60 + 50, 19 * 60 + 20),
            Season.Summer => (6 * 60 + 10, 19 * 60 + 55),
            Season.Autumn => (7 * 60 + 35, 18 * 60 + 5),
            Season.Winter => (7 * 60 + 55, 17 * 60 + 55),
            _ => throw new ArgumentOutOfRangeException(nameof(season)),
        };
        return preset switch
        {
            ClockPreset.Morning => (sunriseMinutes + 40) / 60d,
            ClockPreset.Noon => (sunriseMinutes + sunsetMinutes) / 120d,
            ClockPreset.Dusk => (sunsetMinutes - 35) / 60d,
            ClockPreset.Night => 21d,
            _ => throw new ArgumentOutOfRangeException(nameof(preset)),
        };
    }

    public void SetClock(double hours, ClockSetMode mode = ClockSetMode.Jump)
    {
        EnsureControlsAvailable();
        ValidateClock(hours);
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        WorldHours += (hours - Clock + 24) % 24;
        foreach (var lease in _flows) lease.Clock = null;
        Rev++;
    }

    public void Advance(double hours)
    {
        EnsureControlsAvailable();
        if (!double.IsFinite(hours) || hours <= 0 || !double.IsFinite(WorldHours + hours))
            throw new ArgumentOutOfRangeException(nameof(hours));
        WorldHours += hours;
        foreach (var lease in _flows) lease.Clock = null;
        Rev++;
    }

    public void SetSeason(Season s)
    {
        EnsureControlsAvailable();
        if (!Enum.IsDefined(s)) throw new ArgumentOutOfRangeException(nameof(s));
        SeasonTarget = s;
        weather.Reseason(WorldHours, s, _clockOffset);
        Rev++;
    }

    public void SetFlow(int rate)
    {
        EnsureControlsAvailable();
        ValidateRate(rate);
        BaseFlow = new Campus.Contracts.Flow(rate);
        if (rate > 0) LastFlowRate = rate;
        Rev++;
    }

    public IFlowLease AcquireFlow(FlowPriority priority, int rate)
    {
        EnsureControlsAvailable();
        if (!Enum.IsDefined(priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        ValidateRate(rate);
        var lease = new TestFlowLease(this, priority, rate);
        _flows.Add(lease);
        SelectFlow();
        Rev++;
        return lease;
    }

    private void SelectFlow()
    {
        _activeFlow = null;
        foreach (var lease in _flows)
            if (_activeFlow is null || (byte)lease.Priority >= (byte)_activeFlow.Priority) _activeFlow = lease;
        if (RestoreFaulted || _restorePending || _activeFlow?.Clock is not { } clock) return;
        // Explicit fixture commands settle immediately, including a previously suspended owner.
        WorldHours += (clock - Clock + 24) % 24;
        _activeFlow.Clock = null;
        _activeFlow.Rate = 0;
    }

    private sealed class TestFlowLease(TestWorldState world, FlowPriority priority, int rate) : IFlowLease
    {
        private TestWorldState? _world = world;
        public FlowPriority Priority { get; } = priority;
        public int Rate { get; set; } = rate;
        public double? Clock { get; set; }
        public void SetRate(int rate)
        {
            var owner = Owner;
            owner.EnsureControlsAvailable();
            ValidateRate(rate);
            Rate = rate;
            Clock = null;
            owner.Rev++;
        }
        public void FlowTo(double clock, int rate)
        {
            var owner = Owner;
            owner.EnsureControlsAvailable();
            ValidateClock(clock);
            ValidateRate(rate);
            Rate = rate;
            Clock = clock;
            owner.SelectFlow();
            owner.Rev++;
        }
        public void Dispose()
        {
            var owner = _world;
            if (owner is null) return;
            _world = null;
            owner._flows.Remove(this);
            owner.SelectFlow();
            owner.Rev++;
        }
        private TestWorldState Owner => _world ?? throw new ObjectDisposedException(nameof(IFlowLease));
    }

    public void SetWeather(WeatherKind kind, float? intensity = null)
    {
        EnsureControlsAvailable();
        if (!IsNative(kind)) return;
        weather.Select(new WeatherSel(WeatherMode.Manual, kind, intensity ?? 0.5f));
        Rev++;
    }

    public void SetWeatherIntensity(float intensity)
    {
        EnsureControlsAvailable();
        weather.Select(Weather with { Intensity = intensity });
        Rev++;
    }

    public void SetWeatherAuto()
    {
        EnsureControlsAvailable();
        weather.Select(Weather with { Mode = WeatherMode.Auto });
        Rev++;
    }

    public void Apply(WorldPreset p)
    {
        EnsureControlsAvailable();
        ArgumentNullException.ThrowIfNull(p);
        ValidateClock(p.Clock);
        if (!Enum.IsDefined(p.Season)) throw new ArgumentOutOfRangeException(nameof(p));
        var kind = p.Weather ?? WeatherKind.Clear;
        if (!weather.IsNative(kind, p.Season)) throw new ArgumentException("Weather is not native to the preset season.", nameof(p));
        if (p.Intensity is { } intensity && (!float.IsFinite(intensity) || intensity is < 0 or > 1))
            throw new ArgumentOutOfRangeException(nameof(p));
        SetSeason(p.Season);
        SetClock(p.Clock);
        weather.Pin(WorldHours, kind, p.Intensity, WorldHours - 0.5, WorldHours + 2);
        Rev++;
    }

    public void ResetDefault()
    {
        EnsureControlsAvailable();
        SetSeason(Season.Autumn);
        SetClock(17.5);
        SetFlow(1);
        weather.Quiet(WorldHours, WorldHours + 2.1);
        Rev++;
    }

    public void Freeze(double clock, Season season, double anim)
    {
        EnsureControlsAvailable();
        ValidateClock(clock);
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        if (!double.IsFinite(anim) || anim < 0) throw new ArgumentOutOfRangeException(nameof(anim));
        WorldHours = clock;
        _clockOffset = 0;
        SeasonTarget = season;
        AnimTime = anim % 480;
        BaseFlow = new Campus.Contracts.Flow(0);
        LastFlowRate = 1;
        foreach (var lease in _flows) lease.Clock = null;
        // Boot initialization only: the startup owner resets/enters weather after these values exist.
        Rev++;
    }

    /// <summary>Called by the Test fixture save stub when starting a fresh world instance.</summary>
    public void BeginNewInstance()
    {
        Restore(new WorldSaveMoment(17.5, 0, Season.Autumn, 1));
        AnimTime = 240;
        Mode = ViewMode.Fly;
        CompleteRestore();
    }

    public WorldSaveMoment GetSaveMoment()
    {
        EnsureControlsAvailable();
        return new(WorldHours, _clockOffset, SeasonTarget, BaseFlow.Rate);
    }

    public void AbortRestore()
    {
        // Do not pretend a partially restored world was rolled back or successfully committed.
        var changed = !RestoreFaulted || _restorePending;
        _restorePending = false;
        RestoreFaulted = true;
        if (changed) Rev++;
    }

    /// <summary>Test fixture staging hook for a fresh load/new-game restore; no weather mutation or instance change.</summary>
    public void Restore(in WorldSaveMoment moment)
    {
        if (!double.IsFinite(moment.WorldHours) || moment.WorldHours < 0 ||
            !double.IsFinite(moment.ClockOffset) || !double.IsFinite(moment.WorldHours + moment.ClockOffset) ||
            !Enum.IsDefined(moment.Season)) throw new ArgumentOutOfRangeException(nameof(moment));
        ValidateRate(moment.FlowRate);
        WorldHours = moment.WorldHours;
        _clockOffset = moment.ClockOffset;
        SeasonTarget = moment.Season;
        BaseFlow = new Campus.Contracts.Flow(moment.FlowRate);
        if (moment.FlowRate > 0) LastFlowRate = moment.FlowRate;
        foreach (var lease in _flows) lease.Clock = null;
        _restorePending = true;
        // Keep the fault latch until the coordinator successfully commits all World sections.
    }

    public void CompleteRestore()
    {
        if (!_restorePending) throw new InvalidOperationException("A fresh world restore is required before commit.");
        _restorePending = false;
        RestoreFaulted = false;
        // Test fixture celestial readouts are already settled; leave restored weather untouched.
        Instance++;
        Rev++;
    }

    public Task<ModeSwitchResult> SetMode(ViewMode m, ModeSwitchVia via)
    {
        EnsureControlsAvailable();
        if (!Enum.IsDefined(m)) throw new ArgumentOutOfRangeException(nameof(m));
        if (!Enum.IsDefined(via)) throw new ArgumentOutOfRangeException(nameof(via));
        if (!ruleset.Current.ViewModes.Contains(m)) return Task.FromResult(ModeSwitchResult.Refused);
        Mode = m;
        Rev++;
        return Task.FromResult(ModeSwitchResult.Ok);
    }

    private void EnsureControlsAvailable()
    {
        if (RestoreFaulted || _restorePending)
            throw new InvalidOperationException("World restore is incomplete or faulted; load or start a new game.");
    }

    private static void ValidateClock(double hours)
    {
        if (!double.IsFinite(hours) || hours is < 0 or >= 24)
            throw new ArgumentOutOfRangeException(nameof(hours));
    }

    private static void ValidateRate(int rate) => FlowRates.Validate(rate);
}
