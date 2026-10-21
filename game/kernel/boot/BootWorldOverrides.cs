using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Registry;
using Campus.Kernel.World;

namespace Campus.Kernel.Boot;

/// <summary>Initial world values precede the first weather Enter; loaded worlds only use the save restore path.</summary>
public static class BootWorldOverrides
{
    public static void InitializeFreshWorld(BootRunOptions args, ServiceRegistry services, MapDescriptor map)
    {
        if (args.HasLoad) throw new InvalidOperationException("A loaded world cannot use boot Freeze or weather Reset.");
        var world = services.Get<IWorldState>();
        var control = services.Get<IWorldControl>();
        var weather = services.Get<IWeatherProcess>();
        var options = args.World;
        var season = options.Season ?? map.World.Default.Season;
        var clock = options.Clock ?? map.World.Default.Clock;
        var anim = options.AnimationTime ?? WorldTime.InitialAnimTime;
        var seed = options.Weather.Seed ?? options.Seed ?? map.Seed;
        var boot = new WeatherBoot(seed, options.Weather.Kind, options.Weather.Intensity,
            options.Weather.Wet, options.Weather.RemainingMinutes);
        weather.ValidateBoot(boot, season);
        // Effective capture defaults count as weather choices too. Seed/clock/season alone do not override policy.
        var useMapWeather = !options.Weather.Requested;
        if (useMapWeather && !string.Equals(map.World.SunProfile, "default", StringComparison.Ordinal))
            throw new NotSupportedException($"Boot weather policy does not support sun profile {map.World.SunProfile}.");

        // No await or weather call may split this initial state ordering. Freeze itself must be world-only.
        control.Freeze(clock, season, anim);
        control.SetFlow(options.FlowRate ?? 1);
        weather.Reset(boot);
        weather.Enter(map.Id, map.World.WeatherProfile, world.WorldHours);
        if (useMapWeather)
        {
            if (map.World.QuietUntilDusk)
            {
                var dusk = ArtEphemeris.DuskEnd(season);
                weather.Quiet(world.WorldHours, world.WorldHours + ArtEphemeris.Wrap(dusk - world.Clock, 24));
            }
            else
            {
                var defaultKind = weather.MapToSeason(map.World.Default.Weather ?? WeatherKind.Clear, season);
                weather.Pin(world.WorldHours, defaultKind, map.World.Default.Intensity,
                    world.WorldHours - .5, world.WorldHours + 2);
            }
        }
    }

    /// <summary>Only view/flow overrides remain after PrepareWorld. Never Freeze, Reset or reseason a live process here.</summary>
    public static async Task<string?> ApplyView(BootRunOptions args, ServiceRegistry services,
        bool restoringWorld, bool showTitle)
    {
        if (restoringWorld || args.HasLoad || showTitle && !args.CaptureMode) return null;
        var options = args.World;
        var context = services.Get<IMapService>().Context ?? throw new InvalidOperationException("View overrides require a loaded map.");
        var control = services.Get<IWorldControl>();
        if (!showTitle || options.ExplicitFlow) control.SetFlow(options.FlowRate ?? 1);
        var poseId = options.Pose;
        if (poseId is null)
        {
            var spawn = services.Get<IMapService>().ResolveSpawn(context.Descriptor.Id, args.Flow.Spawn);
            var pose = await services.Get<ICameraPoseValidator>().LegalizePrepared(args.Flow.Pose ?? spawn.Fly);
            services.Get<ICameraRig>().SetPose(pose);
        }
        if (poseId is not null)
        {
            var titlePose = showTitle && poseId == "T0";
            var pose = titlePose ? context.Descriptor.Title?.T0 ?? throw new InvalidDataException("This map has no title pose.")
                : ReviewPoseResolver.Resolve(poseId, context);
            if (!titlePose)
            {
                var validator = services.Get<ICameraPoseValidator>();
                _ = await validator.LegalizePrepared(pose);
                if (!validator.IsLegal(pose, outsideBuffer: true))
                    throw new ArgumentException($"Pose is outside the reachable space or inside its buffer: {poseId}.");
            }
            var terrainPoint = new System.Numerics.Vector2(pose.Position.X, pose.Position.Z);
            await services.Get<ITerrainPreparation>().PreparePoints(context, [terrainPoint]);
            services.Get<ICameraRig>().SetPose(pose);
        }
        if (!showTitle || options.ExplicitViewMode)
        {
            var mode = options.ViewMode ?? args.Flow.Mode ?? ViewMode.Fly;
            var switched = await services.Get<IViewControl>().SetMode(mode, ModeSwitchVia.Key);
            if (switched == ModeSwitchResult.Refused) throw new InvalidOperationException("Boot view mode was refused.");
        }
        return poseId;
    }

    public static async Task ApplyQuality(BootRunOptions args, IQualityService quality)
    {
        var options = args.Quality;
        if (!options.Requested) return;
        var requested = quality.Requested;
        var preset = options.Preset ?? requested.Preset;
        var overrides = options.Preset is not null ? new Dictionary<QualityKnob, float>() : new Dictionary<QualityKnob, float>(requested.Overrides);
        foreach (var (knob, value) in options.Overrides) overrides[knob] = value;
        var profile = new QualityProfile(preset, overrides);
        var result = await quality.Apply(profile, ApplyOrigin.CommandLine);
        if (result.Status != ApplyStatus.Applied)
            throw new InvalidOperationException($"Command-line quality was not applied: {result.Status}; rejected={string.Join(',', result.Rejected)}.");
    }
}
