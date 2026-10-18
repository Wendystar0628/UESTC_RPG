using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.World;
using WorldFlow = Campus.Contracts.Flow;

namespace Campus.Tests.World;

/// <summary>Builders and assertions shared by the world-state test classes.</summary>
internal static class WorldTestKit
{
    public static RestoreContext Restore() => new() { Map = new("test-world"), FromVersion = 1, Missing = false };
    public static (double Hours, double Clock, double Target, double Season, WorldFlow Flow, double Anim,
        Body Sun, Body Moon, long Rev, long Instance, int Steps) LiveReadings(WorldStateService world, RecordingWeatherProcess weather) =>
        (world.WorldHours, world.Clock, world.TargetHours, world.SeasonPos, world.Flow, world.AnimTime,
            world.Sun, world.Moon, world.Rev, world.Instance, weather.Steps.Count);
    public static WorldStateService CreateWorld(RecordingWeatherProcess weather, IMotionSettings? motion = null)
    {
        var world = new WorldStateService(weather, motion);
        world.OnMapLoaded(PresetContext(new WorldPreset(17.5, Season.Autumn, WeatherKind.Clear), true));
        return world;
    }
    public static IMapContext PresetContext(WorldPreset preset, bool quiet) => new TestMapContext(new MapDescriptor(
        new("test-world"), new("测试", "Test"), MapKind.Outdoor, 1, 1,
        new(0, 0, 0, Vector2.Zero, Vector2.One), Vector2.Zero, new(Vector3.Zero, Vector3.One),
        new(Vector2.Zero, Vector2.One), new(1, 1, 1, -1, 1, 1, 1, 1), new(0, 0), new(0, 0),
        "entry", [], new(MapTimeRule.Shared, preset, new("默认", "Default"), quiet, "test-profile"),
        new("sky", "backdrop", []), new Dictionary<string, string>(), []));

    public static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    public static void Near(double actual, double expected, double tolerance = 1e-10) =>
        Check(Math.Abs(actual - expected) <= tolerance, $"Expected {expected:R}, got {actual:R}.");
    public static void Equal<T>(T actual, T expected) => Check(EqualityComparer<T>.Default.Equals(actual, expected), $"Expected {expected}, got {actual}.");
    public static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
