using System.Text.Json;
using GdUnit4;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class WeatherQueueWireTests
{
    [TestCase]
    public void LegacyQueueAndNullOmittingWriterRemainReadable()
    {
        const string legacy = """{"kind":"clear","minutes":30}""";
        var item = JsonSerializer.Deserialize<WeatherQueueItem>(legacy, ContractJson.Options);
        if (item.Kind != WeatherKind.Clear || item.Minutes != 30 || item.Intensity is not null)
            throw new InvalidOperationException("Legacy weather queue data changed.");
        var bytes = JsonSerializer.SerializeToUtf8Bytes(item, ContractJson.Options);
        if (JsonSerializer.Deserialize<WeatherQueueItem>(bytes, ContractJson.Options) != item)
            throw new InvalidOperationException("A null-omitting queue write cannot be read back.");
        var wet = item with { Kind = WeatherKind.Rain, Intensity = 0.75f };
        if (JsonSerializer.Deserialize<WeatherQueueItem>(JsonSerializer.SerializeToUtf8Bytes(wet, ContractJson.Options), ContractJson.Options) != wet)
            throw new InvalidOperationException("Explicit weather intensity was lost.");
    }
}
