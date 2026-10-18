using Campus.Contracts;
using Campus.Contracts.Fauna;
using GdUnit4;

namespace Campus.Systems.Fauna.Tests;

[TestSuite]
public sealed class FaunaScheduleTests
{
    [TestCase]
    public void ScheduleFollowsTheSpeciesTable()
    {
        var clear = new WeatherVec(0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0);
        var rain = clear with { Precip = 0.6f, Wet = 0.8f };
        var after = clear with { Wet = 0.6f };
        Check(FaunaSchedule.Classify(rain) == WeatherDim.Rain && FaunaSchedule.Classify(after) == WeatherDim.AfterRain && FaunaSchedule.Classify(clear) == WeatherDim.Clear, "Weather dimension.");
        Check(FaunaSchedule.Target(FaunaKind.Cat, 20, 9.0, clear) == FaunaSet.Day && FaunaSchedule.Target(FaunaKind.Cat, -10, 9.0, clear) == FaunaSet.Night, "Cats by day and night.");
        Check(FaunaSchedule.Target(FaunaKind.Cat, 20, 9.0, rain) == FaunaSet.Rain && FaunaSchedule.Target(FaunaKind.Cat, -10, 9.0, rain) == FaunaSet.Rain, "Cats under the eaves in rain.");
        Check(FaunaSchedule.Target(FaunaKind.Pigeon, 20, 9.0, clear) == FaunaSet.Day && FaunaSchedule.Target(FaunaKind.Pigeon, 20, 9.0, rain) == FaunaSet.Night, "Pigeons roost in rain.");
        Check(FaunaSchedule.Target(FaunaKind.Sparrow, 20, 9.0, rain) is null && FaunaSchedule.Target(FaunaKind.Sparrow, -10, 9.0, clear) is null, "No sparrows at night or in rain.");
        Check(FaunaSchedule.Target(FaunaKind.Sparrow, 20, 9.0, after) == FaunaSet.Day, "Birds come back after rain.");
        Check(FaunaSchedule.Target(FaunaKind.Cat, -5.9f, 9.0, clear) == FaunaSet.Day, "Night starts below -6 degrees.");
        Check(FaunaSchedule.Target(FaunaKind.Swan, 20, 9.0, clear with { Precip = 0.3f }) == FaunaSet.Day
            && FaunaSchedule.Target(FaunaKind.Swan, 20, 9.0, rain with { Precip = 0.8f }) == FaunaSet.Night, "Swans stay out in light rain, tuck in in heavy rain.");
        Check(FaunaSchedule.Target(FaunaKind.Crow, 4f, 17.5, clear) == FaunaSet.Day && FaunaSchedule.Target(FaunaKind.Crow, 20f, 15.0, clear) is null
            && FaunaSchedule.Target(FaunaKind.Crow, 4f, 6.5, clear) is null && FaunaSchedule.Target(FaunaKind.Crow, -8f, 18.5, clear) is null
            && FaunaSchedule.Target(FaunaKind.Crow, 4f, 17.5, rain) is null, "Crows only at dusk, never at night or in rain.");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
