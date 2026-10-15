using Campus.Contracts;

namespace Campus.Systems.Weather;

[KernelService(typeof(IWeatherProcess), Owner = "WP-45a")]
[KernelService(typeof(WeatherProcess), Owner = "WP-45a")]
public sealed class WeatherProcess(IWeatherProfileSource profiles) : IWeatherProcess
{
    private readonly Dictionary<MapId, WeatherTimeline> _maps = [];
    private readonly Dictionary<MapId, string> _profileNames = [];
    private WeatherTimeline? _current;
    private WeatherBoot? _boot;
    private bool _restoredAwaitingEnter;
    private WeatherVec _blendFrom;
    private double _blendElapsed = 3;
    private double? _jumpFrom, _jumpTo;
    private WeatherVec _jumpVector;
    private IReadOnlyList<WeatherSpan> _ahead = Array.Empty<WeatherSpan>();
    public WeatherSel Selection { get; private set; } = new(WeatherMode.Auto, WeatherKind.Clear, 1);
    public WeatherVec Now { get; private set; } = WeatherVectors.For(WeatherKind.Clear, 1);
    public IReadOnlyList<WeatherSpan> Ahead => _ahead;
    public uint Seed { get; private set; }
    public bool Transitioning => _blendElapsed < 3 || _jumpTo.HasValue;

    public bool IsNative(WeatherKind k, Season s) => WeatherRules.IsNative(k, s);

    public void Reset(WeatherBoot b)
    {
        ArgumentNullException.ThrowIfNull(b);
        WeatherBootOptions.Validate(b);
        var previous = _current;
        string? profile = previous is not null && _profileNames.TryGetValue(previous.Map, out var known) ? known : null;
        RestoreSeed(b.Seed);
        _boot = b;
        // Reset starts a new world instance; keep only map identity, never the old timeline's clock.
        if (previous is not null && profile is not null) Enter(previous.Map, profile, 0);
    }

    internal void RestoreSeed(uint seed)
    {
        Seed = seed;
        _maps.Clear();
        _profileNames.Clear();
        _current = null;
        _boot = null;
        _restoredAwaitingEnter = false;
        _blendElapsed = 3;
        _jumpFrom = _jumpTo = null;
        Selection = new(WeatherMode.Auto, WeatherKind.Clear, 1);
        Now = WeatherVectors.For(WeatherKind.Clear, 1);
        _ahead = Array.Empty<WeatherSpan>();
    }

    public void Step(in WeatherTick t)
    {
        ValidateHours(t.WorldHours);
        ValidateHours(t.TargetHours);
        if (!double.IsFinite(t.ClockOffset) || !double.IsFinite(t.RealDt) || t.RealDt < 0)
            throw new ArgumentOutOfRangeException(nameof(t));
        if (t.FlowRate is not (0 or 1 or 10 or 60 or 360 or 1440)) throw new ArgumentOutOfRangeException(nameof(t));
        WeatherRules.ValidateSeason(t.Season);
        var state = Current(t.WorldHours);
        if (t.WorldHours < state.Hours) throw new ArgumentOutOfRangeException(nameof(t), "Weather time must move forward; use Restore for another world instance.");
        if (state.Queue.Count == 0)
        {
            state.Season = t.Season;
            state.ClockOffset = t.ClockOffset;
        }
        state.Hours = t.WorldHours;
        if (_boot is { } boot)
        {
            ApplyBoot(state, boot);
            _boot = null;
        }
        state.Ensure(Seed, Math.Max(t.WorldHours, t.TargetHours) + 24);
        if (t.TargetHours > t.WorldHours + 1e-9)
        {
            if (!_jumpTo.HasValue || _jumpTo.Value != t.TargetHours)
            {
                _jumpFrom = t.WorldHours;
                _jumpTo = t.TargetHours;
                _jumpVector = Now;
                _blendElapsed = 3;
            }
            double total = _jumpTo.Value - _jumpFrom!.Value;
            float progress = (float)((t.WorldHours - _jumpFrom.Value) / total);
            var end = Vector(state, t.TargetHours);
            Now = WeatherVectors.Lerp(_jumpVector, end, WeatherVectors.Smooth((progress - .3f) / .5f));
            Selection = state.SelectionAt(t.WorldHours);
        }
        else
        {
            if (_jumpTo.HasValue) { _jumpFrom = _jumpTo = null; _blendElapsed = 3; }
            var target = Vector(state, t.WorldHours);
            if (_blendElapsed < 3)
            {
                _blendElapsed = Math.Min(3, _blendElapsed + t.RealDt);
                Now = WeatherVectors.Lerp(_blendFrom, target, WeatherVectors.Smooth((float)(_blendElapsed / 3)));
            }
            else Now = target;
            Selection = state.SelectionAt(t.WorldHours);
        }
        state.Trim(t.WorldHours);
        RefreshAhead(state, t.WorldHours);
    }

    public void Select(WeatherSel sel)
    {
        if (sel.Mode is not (WeatherMode.Auto or WeatherMode.Manual)) throw new ArgumentOutOfRangeException(nameof(sel));
        WeatherRules.ValidateIntensity(sel.Intensity);
        var state = Current();
        if (!IsNative(sel.Kind, state.Season)) throw new ArgumentException($"{sel.Kind} is unavailable in {state.Season}; use {WeatherRules.MapToSeason(sel.Kind, state.Season)}.", nameof(sel));
        BeginBlend();
        state.WetOverride = null;
        state.PinFrom = state.PinTo = null;
        state.PinKind = null;
        state.PinIntensity = null;
        state.QuietUntil = 0;
        state.Selection = sel;
        state.Ensure(Seed, state.Hours + 24);
        Selection = state.SelectionAt(state.Hours);
        RefreshAhead(state, state.Hours);
    }

    public void Reseason(double worldHours, Season to, double clockOffset)
    {
        ValidateHours(worldHours);
        if (!double.IsFinite(clockOffset)) throw new ArgumentOutOfRangeException(nameof(clockOffset));
        WeatherRules.ValidateSeason(to);
        var state = Current(worldHours);
        if (worldHours < state.Hours) throw new ArgumentOutOfRangeException(nameof(worldHours));
        state.Ensure(Seed, worldHours + 24);
        var current = state.SelectionAt(worldHours);
        var kind = WeatherRules.MapToSeason(current.Kind, to);
        float intensity = current.Kind == WeatherKind.Thunderstorm && kind != current.Kind ? 1 : current.Intensity;
        var oldSpan = state.At(worldHours);
        BeginBlend();
        state.Hours = worldHours;
        state.Season = to;
        state.ClockOffset = clockOffset;
        state.WetOverride = null;
        state.Selection = current with { Kind = kind, Intensity = intensity };
        if (state.PinKind is { } pin) state.PinKind = WeatherRules.MapToSeason(pin, to);
        Restart(state, worldHours, kind, intensity, Math.Max(worldHours + WeatherRules.MinimumHours(kind), oldSpan.ToHours));
        state.Ensure(Seed, worldHours + 24);
        Selection = state.SelectionAt(worldHours);
        RefreshAhead(state, worldHours);
    }

    public void Quiet(double worldHours, double untilHours)
    {
        ValidateHours(worldHours);
        ValidateHours(untilHours);
        if (untilHours < worldHours) throw new ArgumentOutOfRangeException(nameof(untilHours));
        var state = Current(worldHours);
        BeginBlend();
        state.Hours = worldHours;
        state.Selection = new(WeatherMode.Auto, WeatherKind.Clear, 1);
        state.PinFrom = state.PinTo = null;
        state.PinKind = null;
        state.PinIntensity = null;
        state.WetOverride = null;
        state.QuietUntil = untilHours;
        Restart(state, worldHours, WeatherKind.Clear, 1, Math.Max(untilHours, worldHours + state.Profile.ClearGapHours));
        state.Ensure(Seed, worldHours + 24);
        Selection = state.Selection;
        RefreshAhead(state, worldHours);
    }

    public void Pin(double worldHours, WeatherKind k, float? intensity, double fromHours, double toHours)
    {
        ValidateHours(worldHours);
        if (!double.IsFinite(fromHours) || !double.IsFinite(toHours) || toHours <= fromHours || toHours <= worldHours)
            throw new ArgumentOutOfRangeException(nameof(toHours));
        var state = Current(worldHours);
        if (!IsNative(k, state.Season)) throw new ArgumentException("Pinned weather is not native to the target season.", nameof(k));
        float strength = intensity ?? WeatherRules.DefaultIntensity(k);
        WeatherRules.ValidateIntensity(strength);
        BeginBlend();
        state.Hours = worldHours;
        state.Selection = new(WeatherMode.Auto, k, strength);
        state.QuietUntil = 0;
        state.PinFrom = fromHours;
        state.PinTo = toHours;
        state.PinKind = k;
        state.PinIntensity = strength;
        state.WetOverride = null;
        Restart(state, worldHours, WeatherKind.Clear, 1, Math.Max(worldHours, fromHours));
        if (state.Queue.Count == 1 && state.Queue[0].ToHours <= worldHours) state.ClearQueue();
        double start = state.Queue.Count == 0 ? worldHours : state.Queue[^1].ToHours;
        double end = k == WeatherKind.Fog ? WeatherRules.FogEnd(toHours, state.ClockOffset, state.Season)
            : WeatherRules.AfterProtected(toHours, state.ClockOffset, state.Season);
        state.Add(new(start, end, k, strength));
        ReturnToClear(state, end, k);
        state.Ensure(Seed, worldHours + 24);
        Selection = state.SelectionAt(worldHours);
        RefreshAhead(state, worldHours);
    }

    public void Enter(MapId map, string profile, double worldHours)
    {
        ValidateHours(worldHours);
        if (string.IsNullOrWhiteSpace(map.Value)) throw new ArgumentException("Map is required.", nameof(map));
        if (!_maps.TryGetValue(map, out var state))
        {
            state = new(map, profiles.Load(profile), _current?.Season ?? Season.Autumn, worldHours);
            _maps.Add(map, state);
        }
        _profileNames[map] = profile;
        // Cross-map catch-up is deliberately deferred; entering an existing map never rewinds it.
        if (worldHours < state.Hours) throw new ArgumentOutOfRangeException(nameof(worldHours));
        _current = state;
        _restoredAwaitingEnter = false;
        state.Hours = worldHours;
        _jumpFrom = _jumpTo = null;
        _blendElapsed = 3;
        if (state.Queue.Count > 0)
        {
            state.Ensure(Seed, worldHours + 24);
            Now = Vector(state, worldHours);
            Selection = state.SelectionAt(worldHours);
            RefreshAhead(state, worldHours);
        }
    }

    public void Leave(MapId map, double worldHours)
    {
        ValidateHours(worldHours);
        if (_restoredAwaitingEnter || _current is null || _current.Map != map) return;
        if (worldHours < _current.Hours) throw new ArgumentOutOfRangeException(nameof(worldHours));
        _current.Hours = worldHours;
        _current = null;
    }

    public WeatherProcessState Capture(MapId map)
    {
        if (!_maps.TryGetValue(map, out var state)) throw new ArgumentException("Weather map has not been entered.", nameof(map));
        double hours = state == _current && _jumpTo.HasValue ? _jumpTo.Value : state.Hours;
        state.Ensure(Seed, hours + 24);
        var settled = Vector(state, hours);
        int first = 0;
        while (first + 1 < state.Queue.Count && state.Queue[first + 1].ToHours < hours - 24) first++;
        var items = state.Queue.Skip(first).Select((span, i) => new WeatherQueueItem(span.Kind, state.Minutes[first + i]) { Intensity = span.Intensity }).ToArray();
        if (items.Length > 256) throw new InvalidDataException("Weather snapshot exceeds the 256-item queue limit.");
        return new(map, state.Epoch, state.Segment, state.SelectionAt(hours), Array.AsReadOnly(items), state.Queue[first].FromHours,
            state.PinFrom, state.PinTo, state.PinKind, state.PinIntensity, state.QuietUntil, settled.Wet, settled.Puddle, hours)
        {
            Attempt = state.Attempt,
            Season = state.Season,
            ClockOffset = state.ClockOffset,
            SnowAdd = settled.SnowAdd,
            WetOverride = state.WetOverride
        };
    }

    public void Restore(MapId map, WeatherProcessState saved)
    {
        ValidateSaved(map, saved);
        string profile = _profileNames.TryGetValue(map, out var previous) ? previous
            : throw new InvalidOperationException("Bind the map's weather profile before Restore.");
        RestoreWithProfile(saved, profiles.Load(profile));
    }

    internal void BindProfile(MapId map, string profile) => _profileNames[map] = profile;

    internal void RestoreWithProfile(WeatherProcessState saved, WeatherProfile profile)
    {
        ValidateSaved(saved.Map, saved);
        var state = new WeatherTimeline(saved.Map, profile, saved.Season, saved.LeftAtHours)
        {
            Epoch = saved.Epoch,
            Segment = saved.Segment,
            Attempt = saved.Attempt,
            ClockOffset = saved.ClockOffset,
            Selection = saved.Selection,
            PinFrom = saved.PinFromHours,
            PinTo = saved.PinToHours,
            PinKind = saved.PinKind,
            PinIntensity = saved.PinIntensity,
            QuietUntil = saved.QuietUntilHours,
            WetOverride = saved.WetOverride
        };
        double from = saved.QueueFromHours;
        foreach (var item in saved.Queue)
        {
            double to = from + item.Minutes / 60;
            state.Queue.Add(new(from, to, item.Kind, item.Intensity ?? WeatherRules.DefaultIntensity(item.Kind)));
            state.Minutes.Add(item.Minutes);
            from = to;
        }
        _maps[saved.Map] = state;
        _current = state;
        _restoredAwaitingEnter = true;
        _boot = null;
        _jumpFrom = _jumpTo = null;
        _blendElapsed = 3;
        Selection = state.SelectionAt(state.Hours);
        Now = Vector(state, state.Hours);
        RefreshAhead(state, state.Hours);
    }

    private WeatherTimeline Current(double hours = 0)
    {
        if (_current is not null) return _current;
        throw new InvalidOperationException("Enter a map before using weather.");
    }

    private static WeatherVec Vector(WeatherTimeline state, double hours)
    {
        var vector = state.Evaluate(hours);
        return state.WetOverride is { } wet ? vector with { Wet = wet } : vector;
    }

    private void BeginBlend()
    {
        _blendFrom = Now;
        _blendElapsed = 0;
        _jumpFrom = _jumpTo = null;
    }

    private static void Restart(WeatherTimeline state, double hours, WeatherKind kind, float intensity, double until)
    {
        state.Epoch = checked(state.Epoch + 1);
        state.Segment = state.Attempt = 0;
        state.ClearQueue();
        if (kind != WeatherKind.Clear)
            until = kind == WeatherKind.Fog ? WeatherRules.FogEnd(until, state.ClockOffset, state.Season)
                : WeatherRules.AfterProtected(until, state.ClockOffset, state.Season);
        if (until > hours) state.Add(new(hours, until, kind, intensity));
        if (kind != WeatherKind.Clear) ReturnToClear(state, until, kind);
    }

    private static void ReturnToClear(WeatherTimeline state, double hours, WeatherKind from)
    {
        WeatherKind? tail = from switch
        {
            WeatherKind.LightRain or WeatherKind.Rain or WeatherKind.Thunderstorm => WeatherKind.AfterRain,
            WeatherKind.Snow => WeatherKind.AfterSnow,
            WeatherKind.Overcast => WeatherKind.Cloudy,
            WeatherKind.Clear or WeatherKind.FewClouds or WeatherKind.Cloudy or WeatherKind.Fog or WeatherKind.AfterRain or WeatherKind.AfterSnow => null,
            _ => throw new ArgumentOutOfRangeException(nameof(from))
        };
        if (tail is { } kind)
        {
            if (!state.Profile.Allows(from, kind)) throw new InvalidDataException("Missing weather return edge.");
            double end = WeatherRules.AfterProtected(hours + 1, state.ClockOffset, state.Season);
            state.Add(new(hours, end, kind, 1));
            hours = state.Queue[^1].ToHours;
            from = kind;
        }
        if (!state.Profile.Allows(from, WeatherKind.Clear)) throw new InvalidDataException("Missing clear return edge.");
        state.Add(new(hours, WeatherRules.AfterProtected(hours + state.Profile.ClearGapHours, state.ClockOffset, state.Season), WeatherKind.Clear, 1));
    }

    private void ApplyBoot(WeatherTimeline state, WeatherBoot boot)
    {
        WeatherBootOptions.Validate(boot, state.Season, state.Profile);
        state.WetOverride = boot.Wet;
        if (boot.Queue is { Count: > 0 } queue)
        {
            state.ClearQueue();
            double from = state.Hours;
            foreach (var item in queue)
            {
                double to = from + item.Minutes / 60;
                state.Queue.Add(new(from, to, item.Kind, item.Intensity ?? boot.Intensity ?? WeatherRules.DefaultIntensity(item.Kind)));
                state.Minutes.Add(item.Minutes);
                from = to;
            }
            ReturnToClear(state, from, queue[^1].Kind);
            state.Selection = new(WeatherMode.Auto, queue[0].Kind, queue[0].Intensity ?? boot.Intensity ?? WeatherRules.DefaultIntensity(queue[0].Kind));
        }
        else if (boot.Kind is { } kind)
        {
            float intensity = boot.Intensity ?? WeatherRules.DefaultIntensity(kind);
            if (boot.RemainingMinutes is { } remaining)
            {
                state.Selection = new(WeatherMode.Auto, kind, intensity);
                state.Add(new(state.Hours, state.Hours + remaining / 60, kind, intensity));
                ReturnToClear(state, state.Hours + remaining / 60, kind);
            }
            else state.Selection = new(WeatherMode.Manual, kind, intensity);
        }
    }

    private void RefreshAhead(WeatherTimeline state, double hours)
    {
        if (state.Selection.Mode == WeatherMode.Manual)
        {
            _ahead = Array.AsReadOnly(new[] { new WeatherSpan(hours, hours + 24, state.Selection.Kind, state.Selection.Intensity) });
            return;
        }
        _ahead = Array.AsReadOnly(state.Queue.Where(s => s.ToHours > hours && s.FromHours < hours + 24)
            .Select(s => s with { FromHours = Math.Max(s.FromHours, hours), ToHours = Math.Min(s.ToHours, hours + 24) }).ToArray());
    }

    internal static void ValidateSaved(MapId map, WeatherProcessState saved)
    {
        ArgumentNullException.ThrowIfNull(saved);
        if (saved.Map != map || string.IsNullOrWhiteSpace(map.Value) || saved.Segment < 0 || saved.Attempt < 0 ||
            !double.IsFinite(saved.ClockOffset) || !double.IsFinite(saved.QueueFromHours) ||
            saved.Queue is not { Count: > 0 } || saved.Queue.Count > 256)
            throw new InvalidDataException("Invalid weather process state.");
        ValidateHours(saved.LeftAtHours);
        ValidateHours(saved.QuietUntilHours);
        WeatherRules.ValidateSeason(saved.Season);
        WeatherRules.ValidateIntensity(saved.Selection.Intensity);
        WeatherRules.ValidateIntensity(saved.Wet);
        WeatherRules.ValidateIntensity(saved.Puddle);
        WeatherRules.ValidateIntensity(saved.SnowAdd);
        if (saved.WetOverride is { } wet) WeatherRules.ValidateIntensity(wet);
        if (saved.Selection.Mode is not (WeatherMode.Auto or WeatherMode.Manual)) throw new InvalidDataException("Unknown weather mode.");
        if (!WeatherRules.IsNative(saved.Selection.Kind, saved.Season)) throw new InvalidDataException("Saved weather is not native to its season.");
        if (saved.PinKind.HasValue != saved.PinFromHours.HasValue || saved.PinKind.HasValue != saved.PinToHours.HasValue ||
            !saved.PinKind.HasValue && saved.PinIntensity.HasValue ||
            saved.PinKind.HasValue && (!double.IsFinite(saved.PinFromHours!.Value) || !double.IsFinite(saved.PinToHours!.Value) ||
            saved.PinToHours <= saved.PinFromHours || !WeatherRules.IsNative(saved.PinKind.Value, saved.Season)))
            throw new InvalidDataException("Invalid weather pin window.");
        if (saved.PinIntensity is { } intensity) WeatherRules.ValidateIntensity(intensity);
        double end = saved.QueueFromHours;
        foreach (var item in saved.Queue)
        {
            if (!double.IsFinite(item.Minutes) || item.Minutes <= 0 || item.Minutes > 1e6 ||
                !WeatherRules.IsNative(item.Kind, saved.Season)) throw new InvalidDataException("Invalid weather queue item.");
            if (item.Intensity is { } strength) WeatherRules.ValidateIntensity(strength);
            end += item.Minutes / 60;
        }
        if (saved.QueueFromHours > saved.LeftAtHours || end < saved.LeftAtHours + 24 - 1e-8)
            throw new InvalidDataException("Weather queue must cover the saved time and its 24 hour forecast.");
    }

    private static void ValidateHours(double hours)
    {
        if (!double.IsFinite(hours) || hours < 0) throw new ArgumentOutOfRangeException(nameof(hours));
    }
}
