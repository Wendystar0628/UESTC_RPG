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
    public void CaptureContextKeepsLegacyShapeAndCarriesOneImmutableMoment()
    {
        var map = new MapId("core5-test");
        var legacy = new CaptureContext(SaveReason.Interval, map, true);
        var (reason, capturedMap, forced) = legacy;
        AssertThat(reason == SaveReason.Interval && capturedMap == map && forced && legacy.Moment is null).OverrideFailureMessage("The existing three-argument capture context changed.").IsTrue();
        var moment = new WorldSaveMoment(31.5, 1.25, Season.Summer, 0);
        var current = legacy with { Moment = moment };
        AssertThat(current.Moment == moment && legacy.Moment is null).OverrideFailureMessage("The moment must belong only to the new context value.").IsTrue();
        var json = JsonSerializer.SerializeToUtf8Bytes(moment, ContractJson.Options);
        AssertThat(JsonSerializer.Deserialize<WorldSaveMoment>(json, ContractJson.Options) == moment).OverrideFailureMessage("The pure contract moment lost a field in serialization.").IsTrue();
    }

}
