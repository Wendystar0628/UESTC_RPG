using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;
using Campus.Kernel.World;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Inspect;

/// <summary>A fixed-shot row is a fresh world instance with the already mounted map.</summary>
internal static class CaptureWorldState
{
    internal static void Start(BootRunOptions args, MapDescriptor map)
    {
        var options = args.World;
        var clock = options.Clock ?? map.World.Default.Clock;
        var season = options.Season ?? map.World.Default.Season;
        var anim = options.AnimationTime ?? WorldTime.InitialAnimTime;
        var weather = Services.Get<IWeatherProcess>();
        var world = Services.Get<IWorldState>();
        var restore = Services.Get<IWorldRestore>();
        var boot = new WeatherBoot(options.Weather.Seed ?? options.Seed ?? map.Seed,
            options.Weather.Kind, options.Weather.Intensity, options.Weather.Wet, options.Weather.RemainingMinutes);
        weather.ValidateBoot(boot, season);
        try
        {
            // Use the world section's new-instance path; Freeze is only legal at process startup.
            ((ISaveSection<WorldSaveV1>)world).Restore(new(clock, 0, (int)season, season, 0, 1, anim),
                new RestoreContext { Map = map.Id, FromVersion = 1, Missing = false });
            weather.Reset(boot);
            weather.Enter(map.Id, map.World.WeatherProfile, clock);
            // The next normal WorldState phase evaluates this boot weather before readiness can pass.
            restore.CompleteRestore();
        }
        catch
        {
            restore.AbortRestore();
            throw;
        }
    }
}
