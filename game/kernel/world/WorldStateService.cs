using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using WorldFlow = Campus.Contracts.Flow;

namespace Campus.Kernel.World;

/// <summary>One monotone world timeline. Only Tick consumes real delta; restoring a save commits a new instance.</summary>
[KernelService(typeof(IWorldState), Owner = "WP-34")]
[KernelService(typeof(IWorldControl), Owner = "WP-34")]
[KernelService(typeof(IWorldRestore), Owner = "WP-34")]
[SaveSection("world", Owner = "WP-34", Version = 1, Phase = RestorePhase.World, Order = 10)]
public sealed class WorldStateService(IWeatherProcess weather, IMotionSettings? motion = null) : IWorldState, IWorldControl,
    IWorldRestore, ISaveSection<WorldSaveV1>
{
    private double _targetHours = 17.5;
    private double _offset;
    private double _velocity;
    private double _jumpFrom;
    private double _jumpElapsed;
    private double _jumpDuration;
    private double? _flowStop;
    private double? _flowClock;
    private double _dragIdle = 1;
    private bool _dragging;
    private double _seasonFrom = 2;
    private double _seasonTo = 2;
    private double _seasonElapsed;
    private double _seasonDuration;
    private double _seasonStartOffset;
    private double _seasonArc;
    private double _seasonStartArcClock;
    private double _finalOffset;
    private int _flowBeforePause = 1;
    private int _stepRate = 12;
    private bool _initializationApplied;
    private bool _restorePending;
    private bool _requestSettled;
    private bool _mapUnavailable = true;
    private MapWorldRules? _rules;
    private MapId? _map;

    public event Action? Settled;
    public long Rev { get; private set; } = 1;
    public long Instance { get; private set; } = 1;
    public double WorldHours { get; private set; } = 17.5;
    public double Clock => ArtEphemeris.Wrap(WorldHours + _offset, 24);
    public double SeasonPos { get; private set; } = 2;
    public Season SeasonTarget { get; private set; } = Season.Autumn;
    public Body Sun { get; private set; } = ArtEphemeris.Evaluate(17.5, 2).Sun;
    public Body Moon { get; private set; } = ArtEphemeris.Evaluate(17.5, 2).Moon;
    public float MoonPhase => 1;
    public float Omega { get; private set; }
    public WorldFlow Flow { get; private set; } = new(1);
    public bool Scrubbing => _dragging && _dragIdle < 1;
    public WorldTransition Transition =>
        (TimeMoving ? WorldTransition.Time : WorldTransition.None) |
        (SeasonMoving ? WorldTransition.Season : WorldTransition.None) |
        (weather.Transitioning ? WorldTransition.Weather : WorldTransition.None);
    public ViewMode ViewMode { get; private set; } = ViewMode.Fly;
    public WeatherSel Weather => weather.Selection;
    public WeatherVec WeatherNow => weather.Now;
    public IReadOnlyList<WeatherSpan> WeatherAhead => weather.Ahead;
    public uint WeatherSeed => weather.Seed;
    public double AnimTime { get; private set; } = 240;
    public bool RestoreFaulted { get; private set; }
    public double Step12 => Math.Floor(AnimTime * _stepRate) / _stepRate;
    public bool IsSettled => !RestoreFaulted && !_restorePending && !TimeMoving && !SeasonMoving && !Scrubbing;
    public double SeasonFrom => _seasonFrom;
    public double SeasonTo => _seasonTo;
    public double SeasonProgress => SeasonMoving ? Math.Clamp(_seasonElapsed / _seasonDuration, 0, 1) : 1;
    public double ClockOffset => _offset;
    public double TargetHours => _flowStop ?? _targetHours;
    public JsonTypeInfo<WorldSaveV1> TypeInfo => WorldSaveJson.Default.WorldSaveV1;
    private bool SeasonMoving => _seasonDuration > 0;
    private bool TimeMoving => _targetHours > WorldHours || _flowStop.HasValue;

    public double PresetClock(ClockPreset preset, Season season) => ArtEphemeris.Preset(preset, season);
    public float SunAltDegAt(double clock, Season season)
    {
        if (!double.IsFinite(clock)) throw new ArgumentOutOfRangeException(nameof(clock));
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        return ArtEphemeris.Evaluate(ArtEphemeris.Wrap(clock, 24), (int)season).Sun.AltDeg;
    }
    public bool IsNative(WeatherKind kind) => weather.IsNative(kind, SeasonTarget);

    public void Tick(double delta, bool freezeAnimation = false)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (RestoreFaulted) return;
        var dt = Math.Min(delta, .1); // Suspend/resume never advances a night in the first frame.
        if (_restorePending) throw new InvalidOperationException("CompleteRestore must commit the World phase before ticking.");
        var previousSun = Sun;
        var previousMoon = Moon;
        if (!freezeAnimation) AnimTime = ArtEphemeris.Wrap(AnimTime + dt, 480);

        if (_dragging)
        {
            _dragIdle += dt;
            if (_dragIdle >= 1) _dragging = false;
        }
        if (_targetHours > WorldHours)
        {
            if (_jumpDuration > 0)
            {
                _jumpElapsed = AdvanceElapsed(_jumpElapsed, dt, _jumpDuration);
                WorldHours = Math.Clamp(ArtEphemeris.Lerp(_jumpFrom, _targetHours,
                    ArtEphemeris.Smooth(_jumpElapsed / _jumpDuration)), WorldHours, _targetHours);
                if (_jumpElapsed >= _jumpDuration) LandClock();
            }
            else
            {
                const double damping = 2 * .6931471805599453 / .15;
                var j0 = WorldHours - _targetHours;
                var j1 = _velocity + damping * j0;
                var decay = Math.Exp(-damping * dt);
                var next = decay * (j0 + j1 * dt) + _targetHours;
                _velocity = decay * (_velocity - damping * j1 * dt);
                WorldHours = Math.Clamp(next, WorldHours, _targetHours);
                if (_targetHours - WorldHours < 1e-10) LandClock();
            }
        }
        else if (!Scrubbing && Flow.Rate > 0)
        {
            var next = WorldHours + dt * Flow.Rate / 3600;
            if (_flowStop is { } stop && next >= stop)
            {
                WorldHours = stop;
                _flowStop = null;
                SetFlow(0);
            }
            else WorldHours = next;
            _targetHours = WorldHours;
        }

        if (SeasonMoving)
        {
            _seasonElapsed = AdvanceElapsed(_seasonElapsed, dt, _seasonDuration);
            var blend = ArtEphemeris.Smooth(_seasonElapsed / _seasonDuration);
            SeasonPos = ArtEphemeris.Wrap(ArtEphemeris.Lerp(_seasonFrom, _seasonTo, blend), 4);
            _offset = _seasonStartOffset + ArtEphemeris.ClockAtPhase(_seasonArc, SeasonPos) - _seasonStartArcClock;
            if (_seasonElapsed >= _seasonDuration)
            {
                SeasonPos = (int)SeasonTarget;
                _offset = _finalOffset;
                _seasonDuration = 0;
            }
        }
        (Sun, Moon) = ArtEphemeris.Evaluate(Clock, SeasonPos);
        // Altitude/azimuth differences avoid float dot-product noise at ordinary 1x speed.
        Omega = dt > 0 ? AngularSpeed(Sun.AltDeg >= -7 ? previousSun : previousMoon,
            Sun.AltDeg >= -7 ? Sun : Moon, dt) : 0;
        if (!_mapUnavailable)
            weather.Step(new WeatherTick(WorldHours, TargetHours, _offset, SeasonTarget, Sun.AltDeg, Flow.Rate, dt));
        Rev++;
        if (_requestSettled && IsSettled && !weather.Transitioning)
        {
            _requestSettled = false;
            Settled?.Invoke();
        }
    }

    public void SetClock(double hours, ClockSetMode mode = ClockSetMode.Jump)
    {
        EnsureUsable();
        ValidateClock(hours);
        switch (mode)
        {
            case ClockSetMode.Jump:
                StartJump(_targetHours + ForwardHours(hours, ArtEphemeris.Wrap(_targetHours + PlannedOffset, 24)));
                break;
            case ClockSetMode.Drag:
                var currentTarget = ArtEphemeris.Wrap(_targetHours + PlannedOffset, 24);
                var delta = ArtEphemeris.Wrap(hours - currentTarget + 12, 24) - 12;
                // The pointer can cross midnight. Counterclockwise samples cannot rewind or add a day.
                if (delta > 0) _targetHours += delta;
                _jumpDuration = 0;
                _flowStop = null;
                _flowClock = null;
                _dragging = true;
                _dragIdle = 0;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(mode));
        }
        Rev++;
    }

    public void Advance(double hours)
    {
        EnsureUsable();
        if (!double.IsFinite(hours) || hours <= 0 || !double.IsFinite(_targetHours + hours))
            throw new ArgumentOutOfRangeException(nameof(hours));
        StartJump(_targetHours + hours);
        Rev++;
    }

    public void FlowTo(double clock, int rate)
    {
        EnsureUsable();
        ValidateClock(clock);
        ValidateRate(rate);
        var stop = _targetHours + ForwardHours(clock, ArtEphemeris.Wrap(_targetHours + PlannedOffset, 24));
        SetFlow(rate);
        if (stop == WorldHours && !TimeMoving) SetFlow(0);
        else
        {
            _flowClock = clock;
            _flowStop = stop;
        }
        Rev++;
    }

    public void SetFlow(int rate)
    {
        EnsureUsable();
        ValidateRate(rate);
        if (rate > 0) _flowBeforePause = rate;
        else if (Flow.Rate > 0) _flowBeforePause = Flow.Rate;
        Flow = new(rate);
        _flowStop = null;
        _flowClock = null;
        Rev++;
    }

    public void SetSeason(Season season)
    {
        EnsureUsable();
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        if (season == SeasonTarget) return;
        _seasonFrom = SeasonPos;
        var distance = ArtEphemeris.Wrap((int)season - SeasonPos, 4);
        _seasonTo = _seasonFrom + distance;
        _seasonElapsed = 0;
        _seasonDuration = motion?.ReduceMotion == true ? .5 : distance <= 1 ? 4 : 5.5;
        _seasonStartOffset = _offset;
        _seasonArc = ArtEphemeris.Phase(Clock, SeasonPos);
        _seasonStartArcClock = ArtEphemeris.ClockAtPhase(_seasonArc, SeasonPos);
        _finalOffset = _offset + ArtEphemeris.ClockAtPhase(_seasonArc, (int)season) - _seasonStartArcClock;
        SeasonTarget = season;
        if (_flowClock is { } clock)
        {
            // FlowTo owns a clock destination, not an absolute hour from the previous season's offset.
            _flowStop = _targetHours + ForwardHours(clock, ArtEphemeris.Wrap(_targetHours + _finalOffset, 24));
            if (_flowStop == WorldHours && _targetHours == WorldHours) SetFlow(0);
        }
        weather.Reseason(WorldHours, season, _finalOffset);
        _requestSettled = true;
        Rev++;
    }

    public void SetWeather(WeatherKind kind, float? intensity = null)
    {
        EnsureUsable();
        ValidateWeather(kind, intensity);
        if (!IsNative(kind)) return;
        weather.Select(new WeatherSel(WeatherMode.Manual, kind, intensity ?? .5f));
        Rev++;
    }

    public void SetWeatherIntensity(float intensity)
    {
        EnsureUsable();
        ValidateWeather(Weather.Kind, intensity);
        weather.Select(Weather with { Intensity = intensity });
        Rev++;
    }

    public void SetWeatherAuto()
    {
        EnsureUsable();
        weather.Select(Weather with { Mode = WeatherMode.Auto });
        Rev++;
    }

    public void Apply(WorldPreset preset)
    {
        EnsureUsable();
        ValidatePreset(preset);
        SetSeason(preset.Season);
        SetClock(preset.Clock);
        var kind = MapWeather(preset.Weather ?? WeatherKind.Clear, preset.Season);
        var intensity = preset.Weather == WeatherKind.Thunderstorm &&
            preset.Season is Season.Autumn or Season.Winter ? 1f : preset.Intensity;
        weather.Pin(WorldHours, kind, intensity, _targetHours - .5, _targetHours + 2);
        _requestSettled = true;
        Rev++;
    }

    public void ResetDefault()
    {
        EnsureUsable();
        var rules = _rules ?? throw new InvalidOperationException("A map must be loaded before ResetDefault.");
        ValidatePreset(rules.Default);
        SetSeason(rules.Default.Season);
        var projectedClock = ArtEphemeris.Wrap(_targetHours + PlannedOffset, 24);
        // The default hero band is relative to the map's default, never to a hard-coded map ID.
        var distance = ArtEphemeris.Wrap(projectedClock - rules.Default.Clock + 12, 24) - 12;
        if (distance < -8 / 60d || distance > 10 / 60d) SetClock(rules.Default.Clock);
        SetFlow(1);
        SetWeatherAuto();
        if (rules.QuietUntilDusk)
        {
            var end = ArtEphemeris.DuskEnd(SeasonTarget);
            weather.Quiet(WorldHours, _targetHours + ForwardHours(end,
                ArtEphemeris.Wrap(_targetHours + PlannedOffset, 24)));
        }
        else weather.Pin(WorldHours, MapWeather(rules.Default.Weather ?? WeatherKind.Clear, SeasonTarget),
            rules.Default.Intensity, _targetHours - .5, _targetHours + 2);
        _requestSettled = true;
        Rev++;
    }

    public void Freeze(double clock, Season season, double anim)
    {
        EnsureUsable();
        if (_initializationApplied || _restorePending)
            throw new InvalidOperationException("Freeze is a one-time boot initialization, not a runtime rewind.");
        ValidateClock(clock);
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        if (!double.IsFinite(anim) || anim < 0) throw new ArgumentOutOfRangeException(nameof(anim));
        SetImmediate(new WorldSaveV1(clock, 0, (int)season, season, 0, 1, ArtEphemeris.Wrap(anim, 480)));
        _initializationApplied = true;
        Rev++;
    }

    public void OnMapUnloading(MapId map)
    {
        weather.Leave(map, WorldHours);
        _map = null;
        _rules = null;
        _mapUnavailable = true;
    }

    public void OnMapLoaded(IMapContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rules = context.Descriptor.World;
        switch (rules.Time)
        {
            case MapTimeRule.Shared: break;
            case MapTimeRule.Fixed: throw new NotSupportedException("Fixed map time is reserved for a future ruleset.");
            default: throw new ArgumentOutOfRangeException(nameof(context));
        }
        if (rules.SunProfile != "default") throw new NotSupportedException($"Unknown sun profile: {rules.SunProfile}");
        _rules = rules;
        _mapUnavailable = false;
        if (_map != context.Descriptor.Id)
        {
            _map = context.Descriptor.Id;
            weather.Enter(context.Descriptor.Id, rules.WeatherProfile, WorldHours);
        }
    }

    public void SetFrameReadouts(ViewMode mode, int stepRate)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        if (stepRate is not (12 or 15)) throw new ArgumentOutOfRangeException(nameof(stepRate));
        ViewMode = mode;
        _stepRate = stepRate;
    }

    public WorldSaveMoment GetSaveMoment()
    {
        EnsureUsable();
        return new(TargetHours, PlannedOffset, SeasonTarget, _flowStop.HasValue ? 0 : Flow.Rate);
    }

    public WorldSaveV1 Capture(in CaptureContext context)
    {
        EnsureUsable();
        var moment = context.Moment ?? GetSaveMoment();
        return new(moment.WorldHours, moment.ClockOffset, (int)moment.Season, moment.Season,
            moment.FlowRate, _flowBeforePause, AnimTime);
    }

    public WorldSaveV1 CreateDefault(NewGameContext context)
    {
        ValidatePreset(context.Start);
        return new(context.Start.Clock, 0, (int)context.Start.Season, context.Start.Season, 1, 1, 240);
    }

    public void Restore(WorldSaveV1 dto, RestoreContext context)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (!double.IsFinite(dto.WorldHours) || dto.WorldHours < 0 || !double.IsFinite(dto.ClockOffset) ||
            !double.IsFinite(dto.WorldHours + dto.ClockOffset) || !double.IsFinite(dto.SeasonPos) ||
            dto.SeasonPos is < 0 or >= 4 || !Enum.IsDefined(dto.SeasonTarget) || dto.SeasonPos != (int)dto.SeasonTarget ||
            !double.IsFinite(dto.AnimTime) || dto.AnimTime is < 0 or >= 480 ||
            !IsRate(dto.FlowRate) || !IsRate(dto.FlowRateBeforePause) || dto.FlowRateBeforePause == 0)
            throw new JsonException("Invalid core world segment.");
        SetImmediate(dto);
        _restorePending = true;
    }

    public void CompleteRestore()
    {
        if (!_restorePending) throw new InvalidOperationException("No restored world is pending commit.");
        _restorePending = false;
        RestoreFaulted = false;
        Instance++;
        Rev++;
    }

    public void AbortRestore()
    {
        // A failed transaction cannot later commit by accident. A new valid Restore must prepare it again.
        _restorePending = false;
        _requestSettled = false;
        RestoreFaulted = true;
    }

    private void EnsureUsable()
    {
        if (RestoreFaulted)
            throw new InvalidOperationException("World restore failed; complete a fresh load or new game before controls or capture.");
        if (_restorePending)
            throw new InvalidOperationException("CompleteRestore must commit the World phase before controls or capture.");
    }

    private void SetImmediate(WorldSaveV1 dto)
    {
        WorldHours = _targetHours = dto.WorldHours;
        _offset = _finalOffset = dto.ClockOffset;
        SeasonPos = _seasonFrom = _seasonTo = dto.SeasonPos;
        SeasonTarget = dto.SeasonTarget;
        Flow = new(dto.FlowRate);
        _flowBeforePause = dto.FlowRateBeforePause;
        AnimTime = dto.AnimTime;
        _velocity = _jumpDuration = _jumpElapsed = _seasonDuration = _seasonElapsed = 0;
        _flowStop = null;
        _flowClock = null;
        _dragging = _requestSettled = false;
        _dragIdle = 1;
        (Sun, Moon) = ArtEphemeris.Evaluate(Clock, SeasonPos);
        Omega = 0;
    }

    private void StartJump(double target)
    {
        _flowStop = null;
        _flowClock = null;
        _dragging = false;
        _dragIdle = 1;
        _targetHours = Math.Max(_targetHours, target);
        _jumpFrom = WorldHours;
        _jumpElapsed = 0;
        _jumpDuration = _targetHours > WorldHours
            ? motion?.ReduceMotion == true ? .5 : Math.Min(3, .6 + .18 * (_targetHours - WorldHours)) : 0;
        _velocity = 0;
    }

    private void LandClock() { WorldHours = _targetHours; _velocity = _jumpDuration = 0; }
    private static double AdvanceElapsed(double elapsed, double dt, double duration)
    {
        var next = Math.Min(elapsed + dt, duration);
        // Decimal frame intervals must not add a whole frame for a sub-nanosecond rounding residue.
        return duration - next <= 1e-10 ? duration : next;
    }
    private double PlannedOffset => SeasonMoving ? _finalOffset : _offset;
    private static double ForwardHours(double requested, double current)
    {
        var distance = ArtEphemeris.Wrap(requested - current, 24);
        return distance < 1e-10 || 24 - distance < 1e-10 ? 0 : distance;
    }

    private static float AngularSpeed(Body from, Body to, double dt)
    {
        var az = (ArtEphemeris.Wrap(to.AzDeg - from.AzDeg + 180, 360) - 180) *
            Math.Cos((to.AltDeg + from.AltDeg) * Math.PI / 360);
        var alt = to.AltDeg - from.AltDeg;
        return (float)(Math.Sqrt(az * az + alt * alt) / dt);
    }

    private static WeatherKind MapWeather(WeatherKind kind, Season season) => kind switch
    {
        WeatherKind.Clear or WeatherKind.FewClouds or WeatherKind.Cloudy or WeatherKind.Overcast or WeatherKind.Fog => kind,
        WeatherKind.LightRain or WeatherKind.Rain => season == Season.Winter ? WeatherKind.Snow : kind,
        WeatherKind.Thunderstorm => season switch
        {
            Season.Spring or Season.Summer => kind,
            Season.Autumn => WeatherKind.Rain,
            Season.Winter => WeatherKind.Snow,
            _ => throw new ArgumentOutOfRangeException(nameof(season)),
        },
        WeatherKind.AfterRain => season == Season.Winter ? WeatherKind.AfterSnow : kind,
        WeatherKind.Snow => season == Season.Winter ? kind : WeatherKind.Rain,
        WeatherKind.AfterSnow => season == Season.Winter ? kind : WeatherKind.AfterRain,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static bool IsRate(int rate) => rate is 0 or 1 or 10 or 60 or 360 or 1440;
    private static void ValidateRate(int rate) { if (!IsRate(rate)) throw new ArgumentOutOfRangeException(nameof(rate)); }
    private static void ValidateClock(double clock)
    {
        if (!double.IsFinite(clock) || clock is < 0 or >= 24) throw new ArgumentOutOfRangeException(nameof(clock));
    }
    private static void ValidateWeather(WeatherKind kind, float? intensity)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (intensity is { } value && (!float.IsFinite(value) || value is < 0 or > 1))
            throw new ArgumentOutOfRangeException(nameof(intensity));
    }
    private static void ValidatePreset(WorldPreset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        ValidateClock(preset.Clock);
        if (!Enum.IsDefined(preset.Season)) throw new ArgumentOutOfRangeException(nameof(preset));
        ValidateWeather(preset.Weather ?? WeatherKind.Clear, preset.Intensity);
    }
}
