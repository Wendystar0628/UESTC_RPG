using Campus.Contracts;

namespace Campus.Systems.Weather;

/// <summary>
/// Automatic weather is a pure function of (seed, map, world time); see <see cref="WeatherTimeline"/>. This service keeps the
/// world-level settings on top of it (manual choice, pin, quiet window, wetness residue), the visual blend, and the save mapping.
/// </summary>
[KernelService(typeof(IWeatherProcess), Owner = "WP-45a")]
[KernelService(typeof(WeatherProcess), Owner = "WP-45a")]
public sealed class WeatherProcess(IWeatherProfileSource profiles) : IWeatherProcess
{
    private WeatherSettings _set = new();
    private WeatherTimeline? _line;
    private WeatherProfile? _profile;
    private MapId? _map;
    private WeatherBoot? _boot;
    private double _hours;
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
        RestoreSeed(b.Seed);
        _boot = b;
    }

    /// <summary>A new world line: the seed replaces everything, the entered map stays.</summary>
    internal void RestoreSeed(uint seed)
    {
        Seed = seed;
        _set = new();
        _line = null;
        _boot = null;
        _hours = 0;
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
        var line = Line();
        if (!_set.FrameBound) _set.Reseason(t.Season, t.ClockOffset);
        if (_boot is { } boot)
        {
            ApplyBoot(boot, t.WorldHours);
            _boot = null;
        }
        _hours = t.WorldHours;
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
            Now = WeatherVectors.Lerp(_jumpVector, Vector(t.TargetHours), WeatherVectors.Smooth((progress - .3f) / .5f));
        }
        else
        {
            if (_jumpTo.HasValue) { _jumpFrom = _jumpTo = null; _blendElapsed = 3; }
            var target = Vector(t.WorldHours);
            if (_blendElapsed < 3)
            {
                _blendElapsed = Math.Min(3, _blendElapsed + t.RealDt);
                Now = WeatherVectors.Lerp(_blendFrom, target, WeatherVectors.Smooth((float)(_blendElapsed / 3)));
            }
            else Now = target;
        }
        Selection = line.SelectionAt(t.WorldHours);
        _ahead = Array.AsReadOnly(line.Ahead(t.WorldHours));
    }

    public void Select(WeatherSel sel)
    {
        if (sel.Mode is not (WeatherMode.Auto or WeatherMode.Manual)) throw new ArgumentOutOfRangeException(nameof(sel));
        WeatherRules.ValidateIntensity(sel.Intensity);
        if (!IsNative(sel.Kind, _set.Season)) throw new ArgumentException($"{sel.Kind} is unavailable in {_set.Season}; use {WeatherRules.MapToSeason(sel.Kind, _set.Season)}.", nameof(sel));
        var line = Line();
        BeginBlend();
        _set.SetResidue(Now, _hours);
        _set.ClearChoices();
        _set.Manual = sel.Mode == WeatherMode.Manual ? sel : null;
        Refresh(line);
    }

    public void Reseason(double worldHours, Season to, double clockOffset)
    {
        ValidateHours(worldHours);
        if (!double.IsFinite(clockOffset)) throw new ArgumentOutOfRangeException(nameof(clockOffset));
        WeatherRules.ValidateSeason(to);
        var line = Line();
        BeginBlend();
        _hours = worldHours;
        _set.Reseason(to, clockOffset);
        Refresh(line);
    }

    public void Quiet(double worldHours, double untilHours)
    {
        ValidateHours(worldHours);
        ValidateHours(untilHours);
        if (untilHours < worldHours) throw new ArgumentOutOfRangeException(nameof(untilHours));
        var line = Line();
        BeginBlend();
        _hours = worldHours;
        _set.ClearChoices();
        _set.ClearResidue();
        _set.QuietUntil = untilHours;
        Refresh(line);
    }

    public void Pin(double worldHours, WeatherKind k, float? intensity, double fromHours, double toHours)
    {
        ValidateHours(worldHours);
        if (!double.IsFinite(fromHours) || !double.IsFinite(toHours) || toHours <= fromHours || toHours <= worldHours)
            throw new ArgumentOutOfRangeException(nameof(toHours));
        float strength = intensity ?? WeatherRules.DefaultIntensity(k);
        WeatherRules.ValidateIntensity(strength);
        // Before the first Step the season is not known yet; the first Step maps the kind to it.
        if (_set.FrameBound && !IsNative(k, _set.Season)) throw new ArgumentException("Pinned weather is not native to the target season.", nameof(k));
        var line = Line();
        BeginBlend();
        _hours = worldHours;
        _set.ClearChoices();
        _set.ClearResidue();
        _set.PinKind = k;
        _set.PinIntensity = strength;
        _set.PinFrom = fromHours;
        _set.PinTo = toHours;
        Refresh(line);
    }

    /// <summary>Enter a map: its profile and name pick the stream; weather needs no catch-up, so nothing is replayed.</summary>
    public void Enter(MapId map, string profile, double worldHours)
    {
        ValidateHours(worldHours);
        if (string.IsNullOrWhiteSpace(map.Value)) throw new ArgumentException("Map is required.", nameof(map));
        _map = map;
        _profile = profiles.Load(profile);
        _line = null;
        _hours = worldHours;
        _jumpFrom = _jumpTo = null;
        _blendElapsed = 3;
        if (_set.FrameBound)
        {
            var line = Line();
            Now = Vector(worldHours);
            Refresh(line);
        }
    }

    public void Leave(MapId map, double worldHours)
    {
        ValidateHours(worldHours);
        if (_map != map) return;
        _map = null;
        _profile = null;
        _line = null;
    }

    public WeatherProcessState Capture(MapId map) => Snapshot(map, _set, _jumpTo ?? _hours);

    /// <summary>The settings at a save moment, without touching the live process.</summary>
    public WeatherProcessState CaptureAt(MapId map, in WorldSaveMoment moment)
    {
        ValidateHours(moment.WorldHours);
        if (!double.IsFinite(moment.ClockOffset) || moment.FlowRate is not (0 or 1 or 10 or 60 or 360 or 1440))
            throw new ArgumentOutOfRangeException(nameof(moment));
        WeatherRules.ValidateSeason(moment.Season);
        var copy = _set.Copy();
        if (!copy.FrameBound || copy.Season != moment.Season || copy.ClockOffset != moment.ClockOffset)
            copy.Reseason(moment.Season, moment.ClockOffset);
        return Snapshot(map, copy, moment.WorldHours);
    }

    public void Restore(MapId map, WeatherProcessState saved)
    {
        ValidateSaved(saved);
        _set = new()
        {
            Season = saved.Season,
            ClockOffset = saved.ClockOffset,
            FrameBound = true,
            Manual = saved.Selection.Mode == WeatherMode.Manual ? saved.Selection : null,
            PinFrom = saved.PinFromHours,
            PinTo = saved.PinToHours,
            PinKind = saved.PinKind,
            PinIntensity = saved.PinIntensity,
            QuietUntil = saved.QuietUntilHours,
            WetOverride = saved.WetOverride,
            ResidueWet = saved.Wet,
            ResiduePuddle = saved.Puddle,
            ResidueSnow = saved.SnowAdd,
            ResidueAt = saved.LeftAtHours
        };
        _line = null;
        _boot = null;
        _hours = saved.LeftAtHours;
        _jumpFrom = _jumpTo = null;
        _blendElapsed = 3;
        if (_map is null)
        {
            Selection = _set.Manual ?? new WeatherSel(WeatherMode.Auto, WeatherKind.Clear, 1);
            return;
        }
        var line = Line();
        Now = Vector(_hours);
        Refresh(line);
    }

    /// <summary>Contract shape of the saved settings. The queue, epoch and segment fields belong to the removed per-map queue and stay empty.</summary>
    private static WeatherProcessState Snapshot(MapId map, WeatherSettings s, double hours)
    {
        var residue = s.ResidueNow(hours);
        return new(map, 0, 0, s.Manual ?? new WeatherSel(WeatherMode.Auto, WeatherKind.Clear, 1), Array.Empty<WeatherQueueItem>(), hours,
            s.PinFrom, s.PinTo, s.PinKind, s.PinIntensity, s.QuietUntil, residue.Wet, residue.Puddle, hours)
        {
            Season = s.Season,
            ClockOffset = s.ClockOffset,
            SnowAdd = residue.Snow,
            WetOverride = s.WetOverride
        };
    }

    private WeatherTimeline Line()
    {
        if (_map is not { } map || _profile is null) throw new InvalidOperationException("Enter a map before using weather.");
        return _line ??= new(_profile, Seed, map, _set);
    }

    private WeatherVec Vector(double hours)
    {
        var vector = Line().Evaluate(hours);
        return _set.WetOverride is { } wet ? vector with { Wet = wet } : vector;
    }

    private void BeginBlend()
    {
        _blendFrom = Now;
        _blendElapsed = 0;
        _jumpFrom = _jumpTo = null;
    }

    private void Refresh(WeatherTimeline line)
    {
        Selection = line.SelectionAt(_hours);
        _ahead = Array.AsReadOnly(line.Ahead(_hours));
    }

    /// <summary>Command-line weather lands once the first Step has bound the season; it overrides map defaults.</summary>
    private void ApplyBoot(WeatherBoot boot, double hours)
    {
        WeatherBootOptions.Validate(boot, _set.Season);
        _set.WetOverride = boot.Wet;
        if (boot.Kind is not { } kind) return;
        float intensity = boot.Intensity ?? WeatherRules.DefaultIntensity(kind);
        _set.Manual = null;
        _set.QuietUntil = 0;
        _set.PinKind = null;
        if (boot.RemainingMinutes is { } remaining)
        {
            _set.PinKind = kind;
            _set.PinIntensity = intensity;
            _set.PinFrom = hours;
            _set.PinTo = hours + remaining / 60;
        }
        else _set.Manual = new(WeatherMode.Manual, kind, intensity);
    }

    private static void ValidateSaved(WeatherProcessState saved)
    {
        ArgumentNullException.ThrowIfNull(saved);
        if (!double.IsFinite(saved.ClockOffset)) throw new InvalidDataException("Invalid weather state.");
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
    }

    private static void ValidateHours(double hours)
    {
        if (!double.IsFinite(hours) || hours < 0) throw new ArgumentOutOfRangeException(nameof(hours));
    }
}
