using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Contracts.Save;
using Campus.Contracts.Settings;
using Campus.Contracts.Startup;
using Campus.Kernel.Registry;

namespace Campus.Kernel.Boot;

/// <summary>Resolves explicit fresh-entry inputs without creating or rewinding an active world.</summary>
public static class BootWorldOverrides
{
    public static async Task<(WorldPreset Start, NewGameBoot Boot)> PrepareFreshEntry(BootRunOptions args, ServiceRegistry services)
    {
        if (args.HasLoad) throw new InvalidOperationException("Loaded worlds use their complete saved endpoint.");
        var maps = services.Get<IMapService>();
        var context = maps.Context ?? throw new InvalidOperationException("Fresh entry requires its prepared map.");
        var map = context.Descriptor;
        var options = args.World;
        var season = options.Season ?? map.World.Default.Season;
        var start = new WorldPreset(options.Clock ?? map.World.Default.Clock, season,
            options.Weather.Kind ?? map.World.Default.Weather, options.Weather.Intensity ?? map.World.Default.Intensity);
        var mode = options.ViewMode ?? args.Flow.Mode ?? ViewMode.Fly;
        var spawn = maps.ResolveSpawn(map.Id, args.Flow.Spawn);
        var pose = options.Pose is { } poseId ? ReviewPoseResolver.Resolve(poseId, context)
            : args.Flow.Pose ?? spawn.ViewPose(mode);
        var validator = services.Get<ICameraPoseValidator>();
        if (options.Pose is { } requested)
        {
            _ = await validator.LegalizePrepared(pose);
            if (!validator.IsLegal(pose, outsideBuffer: true))
                throw new ArgumentException($"Pose is outside the reachable space or inside its buffer: {requested}.");
        }
        else pose = await validator.LegalizePrepared(pose);
        var fov = mode switch
        {
            ViewMode.Fly => UiSettingDefinitions.FlyFov,
            ViewMode.Walk => UiSettingDefinitions.WalkFov,
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };
        pose = pose with { FovDeg = pose.FovDeg ?? (args.CaptureMode ? fov.Default : services.Get<ISettingsStore>().Section("ui").Get(fov)) };
        var destination = new MapStreamDestination(pose, mode, CollisionFeet: mode == ViewMode.Walk
            ? pose.Position - System.Numerics.Vector3.UnitY * WalkRules.StandingEye : null);
        var weather = new WeatherBoot(options.Weather.Seed ?? options.Seed ?? map.Seed, options.Weather.Kind,
            options.Weather.Intensity, options.Weather.Wet, options.Weather.RemainingMinutes);
        var boot = new NewGameBoot(options.AnimationTime ?? WorldTime.InitialAnimTime, options.FlowRate ?? 1,
            weather, !options.Weather.Requested) { Map = map.Id, Destination = destination };
        return (start, boot);
    }

    public static async Task ApplyQuality(BootRunOptions args, IQualityService quality)
    {
        var options = args.Quality;
        if (!options.Requested) return;
        var profile = new QualityProfile(options.Preset ?? quality.Requested.Preset, options.Overrides);
        var request = new QualityRequest(profile,
            options.Preset is null ? QualityEditIntent.EditKnobs : QualityEditIntent.SelectPreset,
            options.Overrides.Keys.Order().ToArray());
        var result = await quality.Apply(request, ApplyOrigin.CommandLine);
        if (result.Status != ApplyStatus.Applied)
            throw new InvalidOperationException($"Command-line quality was not applied: {result.Status}; rejected={string.Join(',', result.Rejected)}.");
    }
}
