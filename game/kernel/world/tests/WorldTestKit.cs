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
        Body Sun, Body Moon, long Rev, long Instance, int Steps) LiveReadings(WorldStateService world, TestWeatherProcess weather) =>
        (world.WorldHours, world.Clock, world.TargetHours, world.SeasonPos, world.Flow, world.AnimTime,
            world.Sun, world.Moon, world.Rev, world.Instance, weather.Steps.Count);
    public static WorldStateService CreateWorld(TestWeatherProcess weather, IMotionSettings? motion = null, bool initialize = true)
    {
        var world = new WorldStateService(weather, motion);
        var context = PresetContext(new WorldPreset(17.5, Season.Autumn, WeatherKind.Clear), true);
        if (initialize)
        {
            var preset = context.Descriptor.World.Default;
            world.Freeze(preset.Clock, preset.Season, WorldTime.InitialAnimTime);
            world.SetFlow(1);
        }
        world.OnMapLoaded(context);
        return world;
    }
    public static IMapContext PresetContext(WorldPreset preset, bool quiet) => new TestMapContext(new MapDescriptor(
        new("test-world"), new("测试", "Test"), MapKind.Outdoor, 1, 1,
        new(0, 0, 0, Vector2.Zero, Vector2.One), Vector2.Zero, new(Vector3.Zero, Vector3.One),
        new(Vector2.Zero, Vector2.One), new(1, 1, 1, -1, 1, 1, 1, 1), new(0, 0), new(0, 0),
        "entry", [], new(MapTimeRule.Shared, preset, new("默认", "Default"), quiet, "test-profile"),
        new("sky", "backdrop", []), []));

}
