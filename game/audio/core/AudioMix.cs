using Campus.Contracts;
using Campus.Contracts.Audio;

namespace Campus.Audio.Core;

/// <summary>All gain automation is applied to players; bus levels stay at unity.</summary>
public sealed class AudioMix
{
    public const float SilenceDb = -80f;
    public const float PageOpenDuckDb = -4f;
    public const float PageFadeSeconds = .5f;
    private readonly Ramp[] _groups = [new(), new(), new(), new(), new()];
    private readonly Ramp[] _ducks = [new(1), new(1), new(1), new(1)];
    private readonly float[] _release = new float[4];
    private readonly Ramp _audible = new();
    private readonly Ramp _background = new(1);
    private readonly Ramp _route = new(1);
    private bool _loading, _mapUnloading;

    public AudioMix(AudioSettings settings) => SetSettings(settings, true);

    public static AudioSettings Normalize(AudioSettings next)
    {
        ArgumentNullException.ThrowIfNull(next);
        if (!Enum.IsDefined(next.Sudden) || !Enum.IsDefined(next.Background))
            throw new ArgumentException("Unsupported audio settings value.", nameof(next));
        return next with
        {
            Master = Slider(next.Master),
            Ambience = Slider(next.Ambience),
            Foley = Slider(next.Foley),
            Ui = Slider(next.Ui),
            Music = Slider(next.Music),
            OutputDevice = string.IsNullOrWhiteSpace(next.OutputDevice) ? AudioSettings.Default.OutputDevice : next.OutputDevice
        };
    }

    private static float Slider(float value) => float.IsFinite(value)
        ? Math.Clamp(value, (float)AudioSettingDefinitions.VolumeRange.Min, (float)AudioSettingDefinitions.VolumeRange.Max) : throw new ArgumentOutOfRangeException(nameof(value));

    public static int GroupIndex(AudioGroup group) => group switch
    {
        AudioGroup.Master => 0,
        AudioGroup.Ambience => 1,
        AudioGroup.Foley => 2,
        AudioGroup.Ui => 3,
        AudioGroup.Music => 4,
        _ => throw new ArgumentOutOfRangeException(nameof(group))
    };

    private static int DuckIndex(DuckReason reason) => reason switch
    {
        DuckReason.Boot => 0,
        DuckReason.Loading => 1,
        DuckReason.PageOpen => 2,
        DuckReason.MapTravel => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    };

    public void SetSettings(AudioSettings settings, bool snap = false)
    {
        var values = new[] { settings.Master, settings.Ambience, settings.Foley, settings.Ui, settings.Music };
        for (var i = 0; i < values.Length; i++) _groups[i].To(values[i] * values[i], snap ? 0f : .08f);
    }

    public void SetAudible(bool audible, float seconds) => _audible.To(audible ? 1f : 0f, seconds);
    public void SetRouteAvailable(bool available) => _route.To(available ? 1f : 0f, .1f);

    public void SetBackground(bool background, BackgroundBehavior behavior)
    {
        var target = behavior switch
        {
            BackgroundBehavior.Keep => 1f,
            BackgroundBehavior.Lower => FromDb(-12f),
            BackgroundBehavior.Mute => 0f,
            _ => throw new ArgumentOutOfRangeException(nameof(behavior))
        };
        _background.To(background ? target : 1f, .2f);
    }

    public void Duck(DuckReason reason, float db, float attack, float release)
    {
        if (!float.IsFinite(db) || !float.IsFinite(attack) || !float.IsFinite(release))
            throw new ArgumentOutOfRangeException(nameof(db));
        var index = DuckIndex(reason);
        _release[index] = Math.Max(0f, release);
        _ducks[index].To(FromDb(Math.Clamp(db, SilenceDb, 0f)), Math.Max(0f, attack));
    }

    public void ReleaseDuck(DuckReason reason)
    {
        var index = DuckIndex(reason);
        _ducks[index].To(1f, _release[index]);
    }

    public void UpdatePages(UiSurface open)
    {
        const UiSurface pages = UiSurface.Atlas | UiSurface.AtlasIndex | UiSurface.SettingsCard | UiSurface.SavePage;
        if ((open & pages) != 0)
            Duck(DuckReason.PageOpen, PageOpenDuckDb, PageFadeSeconds, PageFadeSeconds);
        else ReleaseDuck(DuckReason.PageOpen);
    }

    public void BeginMapUnload()
    {
        _mapUnloading = true;
        Duck(DuckReason.MapTravel, -40f, 0f, 2.5f);
    }

    public void UpdateLoading(bool active)
    {
        if (active == _loading) return;
        _loading = active;
        if (active) Duck(DuckReason.Loading, -8f, .5f, .8f);
        else
        {
            ReleaseDuck(DuckReason.Loading);
            if (_mapUnloading) ReleaseDuck(DuckReason.MapTravel);
            _mapUnloading = false;
        }
    }

    public void ResetWorldAutomation()
    {
        for (var i = 0; i < _ducks.Length; i++) _ducks[i].SnapTarget();
    }

    public void Step(float seconds)
    {
        for (var i = 0; i < _groups.Length; i++) _groups[i].Step(seconds);
        for (var i = 0; i < _ducks.Length; i++) _ducks[i].Step(seconds);
        _audible.Step(seconds);
        _background.Step(seconds);
        _route.Step(seconds);
    }

    public float Gain(AudioGroup group)
    {
        var index = GroupIndex(group);
        var gain = _groups[0].Value * _audible.Value * _background.Value * _route.Value;
        if (index != 0) gain *= _groups[index].Value;
        if (group is AudioGroup.Ambience or AudioGroup.Foley)
        {
            var duck = 1f;
            for (var i = 0; i < _ducks.Length; i++) duck = Math.Min(duck, _ducks[i].Value);
            gain *= duck;
        }
        return gain;
    }

    public static float FromDb(float db) => db <= SilenceDb ? 0f : MathF.Pow(10f, db / 20f);
    public static float ToDb(float gain) => gain <= .0001f ? SilenceDb : Math.Max(SilenceDb, 20f * MathF.Log10(gain));


    private sealed class Ramp(float initial = 0f)
    {
        public float Value { get; private set; } = initial;
        private float _target = initial;
        private float _remaining;
        public void To(float target, float seconds)
        {
            if (target == _target) return;
            _target = target;
            _remaining = Math.Max(0f, seconds);
            if (_remaining == 0f) Value = target;
        }
        public void SnapTarget() { Value = _target; _remaining = 0f; }
        public void Step(float seconds)
        {
            if (_remaining <= 0f) return;
            var elapsed = Math.Min(seconds, _remaining);
            Value += (_target - Value) * elapsed / _remaining;
            _remaining -= elapsed;
        }
    }
}
