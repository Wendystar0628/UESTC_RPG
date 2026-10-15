using Campus.Contracts;

namespace Campus.Kernel.Stubs;

/// <summary>D0 weather readouts with a permanently clear, dry presentation.</summary>
[KernelService(typeof(IWeatherProcess), Owner = "WP-00", IsStub = true)]
public sealed class WeatherProcessStub : IWeatherProcess
{
    private static readonly WeatherSel ClearSelection = new(WeatherMode.Auto, WeatherKind.Clear, 0.5f);
    private static readonly WeatherVec ClearVector = new(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0);

    public WeatherSel Selection { get; private set; } = ClearSelection;
    public WeatherVec Now => ClearVector;
    public IReadOnlyList<WeatherSpan> Ahead => Array.Empty<WeatherSpan>();
    public uint Seed { get; private set; }
    public bool Transitioning => false;

    public void Reset(WeatherBoot b)
    {
        ArgumentNullException.ThrowIfNull(b);
        Seed = b.Seed;
        Select(new WeatherSel(b.Kind.HasValue ? WeatherMode.Manual : WeatherMode.Auto,
            b.Kind ?? WeatherKind.Clear, b.Intensity ?? 0.5f));
    }

    public void Step(in WeatherTick t) { }

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
    }

    public void Reseason(double worldHours, Season to, double clockOffset)
    {
        if (IsNative(Selection.Kind, to)) return;
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

    public void Quiet(double worldHours, double untilHours) => Selection = ClearSelection;

    public void Pin(double worldHours, WeatherKind k, float? intensity, double fromHours, double toHours) =>
        Select(new WeatherSel(WeatherMode.Auto, k, intensity ?? 0.5f));

    public void Enter(MapId map, string profile, double worldHours) { }
    public void Leave(MapId map, double worldHours) { }
    public WeatherProcessState Capture(MapId map) => new(map, 0, 0, ClearSelection,
        Array.Empty<WeatherQueueItem>(), 0, null, null, null, null, 0, 0, 0, 0);
    public void Restore(MapId map, WeatherProcessState s) { }
}
