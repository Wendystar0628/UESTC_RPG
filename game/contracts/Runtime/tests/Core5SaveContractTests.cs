using Campus.Tests;
using System.Text.Json;
using Campus.Contracts.Save;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Contracts.Tests;

[TestSuite]
public sealed class Core5SaveContractTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CaptureContextCarriesOneRequiredImmutableMoment()
    {
        var map = new MapId("core5-test");
        var moment = new WorldSaveMoment(31.5, 1.25, Season.Summer, 0);
        var context = new CaptureContext(SaveReason.Interval, map, true) { Moment = moment };
        var (reason, capturedMap, forced) = context;
        AssertThat(reason == SaveReason.Interval && capturedMap == map && forced && context.Moment == moment)
            .OverrideFailureMessage("All save sections must receive the same explicit settled moment.").IsTrue();
        var json = JsonSerializer.SerializeToUtf8Bytes(moment, ContractJson.Options);
        AssertThat(JsonSerializer.Deserialize<WorldSaveMoment>(json, ContractJson.Options) == moment).OverrideFailureMessage("The pure contract moment lost a field in serialization.").IsTrue();
    }

}
