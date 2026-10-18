using System.Text.Json;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class WeatherStateWireTests
{
    [TestCase]
    public void SettingsAndResidueRoundTripWithoutGeneratedWeatherState()
    {
        var state = new WeatherProcessState(new("weather-test"), new(WeatherMode.Manual, WeatherKind.Rain, .75f),
            null, null, null, null, 0, .4f, .2f, 42) { WetOverride = .8f, ClockOffset = 1.5 };
        var json = JsonSerializer.Serialize(state, ContractJson.Options);
        if (JsonSerializer.Deserialize<WeatherProcessState>(json, ContractJson.Options) != state)
            throw new InvalidOperationException("Weather settings or residue were lost.");
        using var document = JsonDocument.Parse(json);
        foreach (var removed in new[] { "epoch", "segment", "attempt", "queue", "queueFromHours" })
            if (document.RootElement.TryGetProperty(removed, out _))
                throw new InvalidOperationException("Generated weather state was serialized: " + removed);
    }
}
