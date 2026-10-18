using Campus.Tests;
using Campus.Contracts;
using Campus.Contracts.Fauna;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Systems.Fauna.Tests;

[TestSuite]
public sealed class FaunaScheduleTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ScheduleFollowsTheSpeciesTable()
    {
        var clear = new WeatherVec(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0);
        var rain = clear with { Precip = 0.6f, Wet = 0.8f };
        var after = clear with { Wet = 0.6f };
        AssertThat(FaunaSchedule.Classify(rain) == WeatherDim.Rain && FaunaSchedule.Classify(after) == WeatherDim.AfterRain && FaunaSchedule.Classify(clear) == WeatherDim.Clear).OverrideFailureMessage("Weather dimension.").IsTrue();
        AssertThat(FaunaSchedule.Target(FaunaKind.Cat, 20, 9.0, clear) == FaunaSet.Day && FaunaSchedule.Target(FaunaKind.Cat, -10, 9.0, clear) == FaunaSet.Night).OverrideFailureMessage("Cats by day and night.").IsTrue();
        AssertThat(FaunaSchedule.Target(FaunaKind.Cat, 20, 9.0, rain) == FaunaSet.Rain && FaunaSchedule.Target(FaunaKind.Cat, -10, 9.0, rain) == FaunaSet.Rain).OverrideFailureMessage("Cats under the eaves in rain.").IsTrue();
        AssertThat(FaunaSchedule.Target(FaunaKind.Pigeon, 20, 9.0, clear) == FaunaSet.Day && FaunaSchedule.Target(FaunaKind.Pigeon, 20, 9.0, rain) == FaunaSet.Night).OverrideFailureMessage("Pigeons roost in rain.").IsTrue();
        AssertThat(FaunaSchedule.Target(FaunaKind.Sparrow, 20, 9.0, rain) is null && FaunaSchedule.Target(FaunaKind.Sparrow, -10, 9.0, clear) is null).OverrideFailureMessage("No sparrows at night or in rain.").IsTrue();
        AssertThat(FaunaSchedule.Target(FaunaKind.Sparrow, 20, 9.0, after) == FaunaSet.Day).OverrideFailureMessage("Birds come back after rain.").IsTrue();
        AssertThat(FaunaSchedule.Target(FaunaKind.Cat, -5.9f, 9.0, clear) == FaunaSet.Day).OverrideFailureMessage("Night starts below -6 degrees.").IsTrue();
        AssertThat(FaunaSchedule.Target(FaunaKind.Swan, 20, 9.0, clear with { Precip = 0.3f }) == FaunaSet.Day
            && FaunaSchedule.Target(FaunaKind.Swan, 20, 9.0, rain with { Precip = 0.8f }) == FaunaSet.Night).OverrideFailureMessage("Swans stay out in light rain, tuck in in heavy rain.").IsTrue();
        AssertThat(FaunaSchedule.Target(FaunaKind.Crow, 4f, 17.5, clear) == FaunaSet.Day && FaunaSchedule.Target(FaunaKind.Crow, 20f, 15.0, clear) is null
            && FaunaSchedule.Target(FaunaKind.Crow, 4f, 6.5, clear) is null && FaunaSchedule.Target(FaunaKind.Crow, -8f, 18.5, clear) is null
            && FaunaSchedule.Target(FaunaKind.Crow, 4f, 17.5, rain) is null).OverrideFailureMessage("Crows only at dusk, never at night or in rain.").IsTrue();
    }
}
