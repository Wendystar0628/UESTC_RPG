using Campus.Contracts;

namespace Campus.Tests;

/// <summary>
/// IWeatherProcess spy: counts and orders the calls a world makes into weather and fakes nothing else.
/// Use TestWeatherProcess when a test needs weather readouts that behave.
/// </summary>
internal sealed class RecordingWeatherProcess : IWeatherProcess
{
    public readonly List<WeatherTick> Steps = [];
    public readonly List<string> Calls = [];
    public int Reseasons, Resets, Quiets, Enters;
    public double PinFrom, PinTo, QuietTo, LastEnterHours;
    public WeatherKind PinnedKind;
    public float? PinnedIntensity;
    public WeatherSel Selection { get; private set; } = new(WeatherMode.Auto, WeatherKind.Clear, 0);
    public WeatherVec Now => default;
    public IReadOnlyList<WeatherSpan> Ahead => [];
    public uint Seed => 17;
    public bool Transitioning => false;
    public bool IsNative(WeatherKind kind, Season season) => true;
    public WeatherBoot ParseBootOptions(IReadOnlyDictionary<string, string> arguments, uint seed, Season season) => throw new NotSupportedException();
    public WeatherKind MapToSeason(WeatherKind kind, Season season) => throw new NotSupportedException();
    public void Reset(WeatherBoot boot) => Resets++;
    public void Step(in WeatherTick tick) => Steps.Add(tick);
    public void Select(WeatherSel selection) { Selection = selection; Calls.Add("select"); }
    public void Reseason(double hours, Season season, double offset) { Reseasons++; Calls.Add("reseason"); }
    public void Quiet(double hours, double until) { Quiets++; QuietTo = until; Calls.Add("quiet"); }
    public void Pin(double hours, WeatherKind kind, float? intensity, double from, double to)
    { PinnedKind = kind; PinnedIntensity = intensity; PinFrom = from; PinTo = to; Calls.Add("pin"); }
    public void Enter(MapId map, string profile, double hours) { Enters++; LastEnterHours = hours; }
    public WeatherProcessState Capture() => throw new NotSupportedException();
    public void Restore(WeatherProcessState state) => throw new NotSupportedException();
}
