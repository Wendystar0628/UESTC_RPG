using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using WorldFlow = Campus.Contracts.Flow;

namespace Campus.Kernel.World;

public interface IWorldFrame : IWorldState, IMapAware
{
    event Action? ReducedTransitionRequested;
    double SeasonFrom { get; }
    double SeasonTo { get; }
    double SeasonProgress { get; }
    void SetStepRate(int stepRate);
    void Tick(double delta, bool freezeAnimation = false);
}

/// <summary>One monotone world timeline. Only Tick consumes real delta; restoring a save commits a new instance.</summary>
[KernelService(typeof(IWorldFrame))]
[KernelService(typeof(IWorldState))]
[KernelService(typeof(IWorldControl))]
[KernelService(typeof(IWorldRestore))]
[SaveSection("world", Version = 1, Phase = RestorePhase.World, Order = 10)]
public sealed class WorldStateService : IWorldFrame, IWorldControl,
    IWorldRestore, ISaveSection<WorldSaveV1>
{
    private readonly IWeatherProcess _weather;
    private readonly IMotionSettings? _motion;
    private readonly WorldFlowArbiter _flow;

    public WorldStateService(IWeatherProcess weather, IMotionSettings? motion = null)
    {
        _weather = weather;
        _motion = motion;
        _flow = new(EnsureUsable, () => Rev++);
    }

    private double _targetHours;
    private double _offset;
    private double _velocity;
    private double _jumpFrom;
    private double _jumpElapsed;
    private double _jumpDuration;
    private double _dragIdle = WorldTimeTuning.DragIdleSeconds;
    private bool _dragging;
    private double _seasonFrom;
    private double _seasonTo;
    private double _seasonElapsed;
    private double _seasonDuration;
    private float _seasonAtmosphereFrom;
    private double _seasonStartOffset;
    private double _seasonArc;
    private double _seasonStartArcClock;
    private double _finalOffset;
    private int _stepRate = WorldTime.DefaultStepRate;
    private long _instanceSerial;
    private bool _initializationApplied;
    private bool _restorePending;
    private bool _mapUnavailable = true;
    private MapWorldRules? _rules;
    private MapId? _map;

    public event Action? ReducedTransitionRequested;
    public long Rev { get; private set; } = 1;
    public long Instance { get; private set; }
    public event Action<long>? InstanceChanged;
    public double WorldHours { get; private set; }
    public double Clock => ArtEphemeris.Wrap(WorldHours + _offset, 24);
    public double SeasonPos { get; private set; }
    public Season SeasonTarget { get; private set; }
    // Valid unit directions and zero intensity keep pre-map shader writes neutral.
    public Body Sun { get; private set; } = new(System.Numerics.Vector3.UnitY, 0, 0, 0);
    public Body Moon { get; private set; } = new(-System.Numerics.Vector3.UnitY, 0, 0, 0);
    public float MoonPhase => 1;
    public float Omega { get; private set; }
    public WorldFlow Flow => _flow.Current;
    public WorldFlow BaseFlow => _flow.Base;
    public int LastFlowRate => _flow.LastRateBeforePause;
    public bool Scrubbing => _dragging && _dragIdle < WorldTimeTuning.DragIdleSeconds;
    public WorldTransition Transition => Instance == 0 ? WorldTransition.None :
        (TimeMoving ? WorldTransition.Time : WorldTransition.None) |
        (SeasonMoving ? WorldTransition.Season : WorldTransition.None) |
        (_weather.Transitioning ? WorldTransition.Weather : WorldTransition.None);
    public WeatherSel Weather => _weather.Selection;
    public WeatherVec WeatherNow => _weather.Now;
    public IReadOnlyList<WeatherSpan> WeatherAhead => _weather.Ahead;
    public uint WeatherSeed => _weather.Seed;
    public double AnimTime { get; private set; }
    public bool RestoreFaulted { get; private set; }
    public double Step12 => Math.Floor(AnimTime * _stepRate) / _stepRate;
    public bool IsSettled => _initializationApplied && !RestoreFaulted && !_restorePending && !TimeMoving && !SeasonMoving && !Scrubbing;
    public double SeasonFrom => _seasonFrom;
    public double SeasonTo => _seasonTo;
    public double SeasonProgress => SeasonMoving ? Math.Clamp(_seasonElapsed / _seasonDuration, 0, 1) : 1;
    public float SeasonAtmosphere
    {
        get
        {
            if (!SeasonMoving || _motion?.ReduceMotion == true) return 0f;
            // Use the same four-second phases for both durations; interruptions keep the current fog weight.
            var phase = SeasonProgress * WorldTimeTuning.SeasonSingleSeconds;
            var rise = ScalarMath.Smoothstep(WorldTimeTuning.SeasonFogStart, WorldTimeTuning.SeasonFogPeak, phase);
            var fall = 1 - ScalarMath.Smoothstep(WorldTimeTuning.SeasonFogRelease, WorldTimeTuning.SeasonSingleSeconds, phase);
            return (float)(ScalarMath.Lerp(_seasonAtmosphereFrom, 1d, rise) * fall);
        }
    }
    public double ClockOffset => _offset;
    public double TargetHours => _targetHours;
    public JsonTypeInfo<WorldSaveV1> TypeInfo => WorldSaveJson.Contract.WorldSaveV1;
    private bool SeasonMoving => _seasonDuration > 0;
    private bool TimeMoving => _targetHours > WorldHours;

    public double PresetClock(ClockPreset preset, Season season) => ArtEphemeris.Preset(preset, season);
    public float SunAltDegAt(double clock, Season season)
    {
        if (!double.IsFinite(clock)) throw new ArgumentOutOfRangeException(nameof(clock));
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        return ArtEphemeris.Evaluate(ArtEphemeris.Wrap(clock, 24), (int)season).Sun.AltDeg;
    }
    public bool IsNative(WeatherKind kind) => _weather.IsNative(kind, SeasonTarget);

    public void Tick(double delta, bool freezeAnimation = false)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (RestoreFaulted) return;
        var dt = Math.Min(delta, WorldTimeTuning.MaxFrameSeconds); // Suspend/resume never advances a night in the first frame.
        if (_restorePending) throw new InvalidOperationException("CompleteRestore must commit the World phase before ticking.");
        if (!_initializationApplied) return;
        var previousSun = Sun;
        var previousMoon = Moon;
        if (!freezeAnimation) AnimTime = ArtEphemeris.Wrap(AnimTime + dt, WorldTime.AnimLoopSeconds);

        if (_dragging)
        {
            _dragIdle += dt;
            if (_dragIdle >= WorldTimeTuning.DragIdleSeconds) _dragging = false;
        }
        if (_targetHours > WorldHours)
        {
            if (_jumpDuration > 0)
            {
                _jumpElapsed = AdvanceElapsed(_jumpElapsed, dt, _jumpDuration);
                WorldHours = Math.Clamp(ScalarMath.Lerp(_jumpFrom, _targetHours,
                    _motion?.ReduceMotion == true ? 1 : ScalarMath.Smoothstep(_jumpElapsed / _jumpDuration)), WorldHours, _targetHours);
                if (_motion?.ReduceMotion == true || _jumpElapsed >= _jumpDuration) LandClock();
            }
            else if (_motion?.ReduceMotion == true) LandClock();
            else
            {
                const double damping = WorldTimeTuning.ClockDamping;
                var j0 = WorldHours - _targetHours;
                var j1 = _velocity + damping * j0;
                var decay = Math.Exp(-damping * dt);
                var next = decay * (j0 + j1 * dt) + _targetHours;
                _velocity = decay * (_velocity - damping * j1 * dt);
                WorldHours = Math.Clamp(next, WorldHours, _targetHours);
                if (_targetHours - WorldHours < WorldTimeTuning.CompletionEpsilon) LandClock();
            }
        }
        else if (!Scrubbing && Flow.Rate > 0)
        {
            WorldHours += dt * Flow.Rate / 3600;
            _targetHours = WorldHours;
        }

        if (SeasonMoving)
        {
            _seasonElapsed = AdvanceElapsed(_seasonElapsed, dt, _seasonDuration);
            var blend = _motion?.ReduceMotion == true ? 1 : ScalarMath.Smoothstep(_seasonElapsed / _seasonDuration);
            SeasonPos = ArtEphemeris.Wrap(ScalarMath.Lerp(_seasonFrom, _seasonTo, blend), 4);
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
        Omega = dt > 0 ? AngularSpeed(Sun.AltDeg >= WorldTimeTuning.AngularSpeedSunAltitude ? previousSun : previousMoon,
            Sun.AltDeg >= WorldTimeTuning.AngularSpeedSunAltitude ? Sun : Moon, dt) : 0;
        if (!_mapUnavailable)
            _weather.Step(new WeatherTick(WorldHours, TargetHours, _offset, SeasonTarget, Sun.AltDeg, Flow.Rate, dt));
        Rev++;
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
                BeginReducedTransition();
                var currentTarget = ArtEphemeris.Wrap(_targetHours + PlannedOffset, 24);
                var delta = ArtEphemeris.Wrap(hours - currentTarget + 12, 24) - 12;
                // The pointer can cross midnight. Counterclockwise samples cannot rewind or add a day.
                if (delta > 0) _targetHours += delta;
                _jumpDuration = 0;
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

    public void SetFlow(int rate)
    {
        EnsureUsable();
        ValidateRate(rate);
        _flow.SetBase(rate);
        Rev++;
    }

    public IFlowLease AcquireFlow(FlowPriority priority, int rate)
    {
        EnsureUsable();
        return _flow.Acquire(priority, rate);
    }

    public void SetSeason(Season season)
    {
        EnsureUsable();
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        if (season == SeasonTarget) return;
        BeginReducedTransition();
        _seasonFrom = SeasonPos;
        var distance = ArtEphemeris.Wrap((int)season - SeasonPos, 4);
        _seasonTo = _seasonFrom + distance;
        _seasonAtmosphereFrom = SeasonAtmosphere;
        _seasonElapsed = 0;
        _seasonDuration = _motion?.ReduceMotion == true ? WorldTimeTuning.ReducedTransitionSeconds : distance <= 1 ? WorldTimeTuning.SeasonSingleSeconds : WorldTimeTuning.SeasonMultipleSeconds;
        _seasonStartOffset = _offset;
        _seasonArc = ArtEphemeris.Phase(Clock, SeasonPos);
        _seasonStartArcClock = ArtEphemeris.ClockAtPhase(_seasonArc, SeasonPos);
        _finalOffset = _offset + ArtEphemeris.ClockAtPhase(_seasonArc, (int)season) - _seasonStartArcClock;
        SeasonTarget = season;
        _weather.Reseason(WorldHours, season, _finalOffset);
        Rev++;
    }

    public void SetWeather(WeatherKind kind, float? intensity = null)
    {
        EnsureUsable();
        ValidateWeather(kind, intensity);
        if (!IsNative(kind)) return;
        BeginReducedTransition();
        _weather.Select(new WeatherSel(WeatherMode.Manual, kind, intensity ?? .5f));
        Rev++;
    }

    public void SetWeatherIntensity(float intensity)
    {
        EnsureUsable();
        ValidateWeather(Weather.Kind, intensity);
        BeginReducedTransition();
        _weather.Select(Weather with { Intensity = intensity });
        Rev++;
    }

    public void SetWeatherAuto()
    {
        EnsureUsable();
        BeginReducedTransition();
        _weather.Select(Weather with { Mode = WeatherMode.Auto });
        Rev++;
    }

    public void Apply(WorldPreset preset)
    {
        EnsureUsable();
        ValidatePreset(preset);
        SetSeason(preset.Season);
        SetClock(preset.Clock);
        var kind = _weather.MapToSeason(preset.Weather ?? WeatherKind.Clear, preset.Season);
        var intensity = preset.Weather == WeatherKind.Thunderstorm &&
            preset.Season is Season.Autumn or Season.Winter ? 1f : preset.Intensity;
        _weather.Pin(WorldHours, kind, intensity, _targetHours - WorldTimeTuning.WeatherPinBeforeHours, _targetHours + WorldTimeTuning.WeatherPinAfterHours);
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
        if (distance < -WorldTimeTuning.HeroBeforeHours || distance > WorldTimeTuning.HeroAfterHours) SetClock(rules.Default.Clock);
        SetFlow(1);
        SetWeatherAuto();
        if (rules.QuietUntilDusk)
        {
            var end = ArtEphemeris.DuskEnd(SeasonTarget);
            _weather.Quiet(WorldHours, _targetHours + ForwardHours(end,
                ArtEphemeris.Wrap(_targetHours + PlannedOffset, 24)));
        }
        else _weather.Pin(WorldHours, _weather.MapToSeason(rules.Default.Weather ?? WeatherKind.Clear, SeasonTarget),
            rules.Default.Intensity, _targetHours - WorldTimeTuning.WeatherPinBeforeHours, _targetHours + WorldTimeTuning.WeatherPinAfterHours);
        Rev++;
    }

    public void Freeze(double clock, Season season, double anim)
    {
        EnsureUsable();
        if (_initializationApplied || _restorePending)
            throw new InvalidOperationException("Freeze only prepares one initial world; it cannot rewind a committed instance.");
        ValidateClock(clock);
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        if (!double.IsFinite(anim) || anim < 0) throw new ArgumentOutOfRangeException(nameof(anim));
        SetImmediate(new WorldSaveV1(clock, 0, (int)season, season, 0, 1, ArtEphemeris.Wrap(anim, WorldTime.AnimLoopSeconds)));
        _restorePending = true;
        Rev++;
    }

    public void OnMapUnloading(MapId map)
    {
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
            _weather.Enter(context.Descriptor.Id, rules.WeatherProfile, WorldHours);
        }
    }

    public void SetStepRate(int stepRate)
    {
        if (!WorldTime.IsStepRate(stepRate)) throw new ArgumentOutOfRangeException(nameof(stepRate));
        _stepRate = stepRate;
    }

    public WorldSaveMoment GetSaveMoment()
    {
        EnsureUsable();
        if (!_initializationApplied) throw new InvalidOperationException("World state has not been initialized.");
        return new(TargetHours, PlannedOffset, SeasonTarget, BaseFlow.Rate);
    }

    public WorldSaveV1 Capture(in CaptureContext context)
    {
        EnsureUsable();
        if (!_initializationApplied) throw new InvalidOperationException("World state has not been initialized.");
        var moment = context.Moment;
        return new(moment.WorldHours, moment.ClockOffset, (int)moment.Season, moment.Season,
            moment.FlowRate, _flow.LastRateBeforePause, AnimTime);
    }

    public WorldSaveV1 CreateDefault(NewGameContext context)
    {
        ValidatePreset(context.Start);
        var rate = context.Boot?.FlowRate ?? 1;
        ValidateRate(rate);
        var animation = context.Boot?.AnimationTime ?? WorldTime.InitialAnimTime;
        if (!double.IsFinite(animation) || animation < 0) throw new ArgumentOutOfRangeException(nameof(context));
        return new(context.Start.Clock, 0, (int)context.Start.Season, context.Start.Season,
            rate, rate > 0 ? rate : 1, ArtEphemeris.Wrap(animation, WorldTime.AnimLoopSeconds));
    }

    public void Restore(WorldSaveV1 dto, RestoreContext context)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (!double.IsFinite(dto.WorldHours) || dto.WorldHours < 0 || !double.IsFinite(dto.ClockOffset) ||
            !double.IsFinite(dto.WorldHours + dto.ClockOffset) || !double.IsFinite(dto.SeasonPos) ||
            dto.SeasonPos is < 0 or >= 4 || !Enum.IsDefined(dto.SeasonTarget) || dto.SeasonPos != (int)dto.SeasonTarget ||
            !double.IsFinite(dto.AnimTime) || dto.AnimTime is < 0 or >= WorldTime.AnimLoopSeconds ||
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
        _initializationApplied = true;
        Instance = checked(++_instanceSerial);
        Rev++;
        InstanceChanged?.Invoke(Instance);
    }

    public void EndInstance()
    {
        if (Instance == 0 && !_initializationApplied && !_restorePending) return;
        _initializationApplied = false;
        _restorePending = false;
        _velocity = _jumpDuration = _jumpElapsed = _seasonDuration = _seasonElapsed = 0;
        _seasonAtmosphereFrom = 0;
        _dragging = false;
        _dragIdle = WorldTimeTuning.DragIdleSeconds;
        _targetHours = WorldHours;
        _seasonFrom = _seasonTo = SeasonPos;
        Omega = 0;
        Instance = 0;
        Rev++;
        InstanceChanged?.Invoke(Instance);
    }

    public void AbortRestore()
    {
        // A failed transaction cannot later commit by accident. A new valid Restore must prepare it again.
        _restorePending = false;
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
        _flow.RestoreBase(dto.FlowRate, dto.FlowRateBeforePause);
        AnimTime = dto.AnimTime;
        _velocity = _jumpDuration = _jumpElapsed = _seasonDuration = _seasonElapsed = 0;
        _seasonAtmosphereFrom = 0;
        _dragging = false;
        _dragIdle = WorldTimeTuning.DragIdleSeconds;
        (Sun, Moon) = ArtEphemeris.Evaluate(Clock, SeasonPos);
        Omega = 0;
    }

    private void BeginReducedTransition()
    {
        if (_motion?.ReduceMotion == true) ReducedTransitionRequested?.Invoke();
    }

    private void StartJump(double target)
    {
        if (target > WorldHours) BeginReducedTransition();
        _dragging = false;
        _dragIdle = WorldTimeTuning.DragIdleSeconds;
        _targetHours = Math.Max(_targetHours, target);
        _jumpFrom = WorldHours;
        _jumpElapsed = 0;
        _jumpDuration = _targetHours > WorldHours
            ? _motion?.ReduceMotion == true ? WorldTimeTuning.ReducedTransitionSeconds : Math.Min(WorldTimeTuning.JumpMaxSeconds, WorldTimeTuning.JumpBaseSeconds + WorldTimeTuning.JumpSecondsPerHour * (_targetHours - WorldHours)) : 0;
        _velocity = 0;
    }

    private void LandClock() { WorldHours = _targetHours; _velocity = _jumpDuration = 0; }
    private static double AdvanceElapsed(double elapsed, double dt, double duration)
    {
        var next = Math.Min(elapsed + dt, duration);
        // Decimal frame intervals must not add a whole frame for a sub-nanosecond rounding residue.
        return duration - next <= WorldTimeTuning.CompletionEpsilon ? duration : next;
    }
    private double PlannedOffset => SeasonMoving ? _finalOffset : _offset;
    private static double ForwardHours(double requested, double current)
    {
        var distance = ArtEphemeris.Wrap(requested - current, 24);
        return distance < WorldTimeTuning.CompletionEpsilon || 24 - distance < WorldTimeTuning.CompletionEpsilon ? 0 : distance;
    }

    private static float AngularSpeed(Body from, Body to, double dt)
    {
        var az = (ArtEphemeris.Wrap(to.AzDeg - from.AzDeg + 180, 360) - 180) *
            Math.Cos((to.AltDeg + from.AltDeg) * Math.PI / 360);
        var alt = to.AltDeg - from.AltDeg;
        return (float)(Math.Sqrt(az * az + alt * alt) / dt);
    }

    private static bool IsRate(int rate) => FlowRates.IsValid(rate);
    private static void ValidateRate(int rate) => FlowRates.Validate(rate);
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
