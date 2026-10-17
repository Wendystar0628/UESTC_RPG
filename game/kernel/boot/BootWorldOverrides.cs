using System.Globalization;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Quality;
using Campus.Kernel.Registry;
using Campus.Kernel.World;
using Campus.Systems.Weather;

namespace Campus.Kernel.Boot;

/// <summary>Initial world values precede the first weather Enter; loaded worlds only use the save restore path.</summary>
public static class BootWorldOverrides
{
    /// <returns>The requested flow rate to restore after initial map warming, before the flow captures its preview state.</returns>
    public static int InitializeFreshWorld(BootArguments args, ServiceRegistry services, MapDescriptor map)
    {
        if (args.HasLoad) throw new InvalidOperationException("A loaded world cannot use boot Freeze or weather Reset.");
        var world = services.Get<IWorldState>();
        var control = services.Get<IWorldControl>();
        var weather = services.Get<IWeatherProcess>();
        var season = args.Values.ContainsKey("season") ? Enum.Parse<Season>(args.Value("season"), true) : map.World.Default.Season;
        var clock = Number(args, "clock", map.World.Default.Clock);
        var anim = Number(args, "anim", world.AnimTime);
        var seed = args.Values.TryGetValue("seed", out var seedText) ? uint.Parse(seedText, CultureInfo.InvariantCulture) : map.Seed;
        var boot = WeatherBootOptions.Parse(args.Values, seed, season);
        // Effective capture defaults count as weather choices too. Seed/clock/season alone do not override policy.
        var useMapWeather = !new[] { "weather", "weather-queue", "weather-intensity", "weather-remaining", "wet" }
            .Any(args.Values.ContainsKey);
        if (useMapWeather && !string.Equals(map.World.SunProfile, "default", StringComparison.Ordinal))
            throw new NotSupportedException($"Boot weather policy does not support sun profile {map.World.SunProfile}.");

        // No await or weather call may split this initial state ordering. Freeze itself must be world-only.
        control.Freeze(clock, season, anim);
        control.SetFlow(0);
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
                var defaultKind = WeatherRules.MapToSeason(map.World.Default.Weather ?? WeatherKind.Clear, season);
                weather.Pin(world.WorldHours, defaultKind, map.World.Default.Intensity,
                    world.WorldHours - .5, world.WorldHours + 2);
            }
        }
        return (int)Number(args, "flow", 1);
    }

    /// <summary>Only view/flow overrides remain after PrepareWorld. Never Freeze, Reset or reseason a live process here.</summary>
    public static async Task ApplyView(BootArguments args, ServiceRegistry services,
        bool restoringWorld, bool showTitle)
    {
        if (restoringWorld || args.HasLoad || showTitle && !args.CaptureMode) return;
        bool Applies(string key) => showTitle ? args.Has(key) : args.Values.ContainsKey(key);
        var context = services.Get<IMapService>().Context ?? throw new InvalidOperationException("View overrides require a loaded map.");
        var control = services.Get<IWorldControl>();
        if (!showTitle || args.Has("flow")) control.SetFlow((int)Number(args, "flow", 1));
        if (args.Has("focus") || args.Has("pose-file"))
            throw new NotSupportedException("Content preview location provider is not yet connected.");
        var poseId = args.Value("pose");
        if (poseId.Length > 0)
        {
            var titlePose = showTitle && poseId == "T0";
            var pose = titlePose ? context.Descriptor.Title?.T0 ?? throw new InvalidDataException("This map has no title pose.")
                : ReviewPoseResolver.Resolve(poseId, context);
            if (!titlePose && !services.Get<ICameraPoseValidator>().IsLegal(pose, outsideBuffer: true))
                throw new ArgumentException($"Pose is outside the reachable space or inside its buffer: {poseId}.");
            services.Get<ICameraRig>().SetPose(pose);
        }
        if (Applies("mode"))
        {
            var mode = args.Value("mode") switch { "fly" => ViewMode.Fly, "walk" => ViewMode.Walk, _ => throw new ArgumentException("Unknown mode.") };
            await services.Get<IViewControl>().SetMode(mode, ModeSwitchVia.Key);
        }
    }

    public static async Task ApplyQuality(BootArguments args, IQualityService quality)
    {
        if (!new[] { "quality", "quality-set", "adaptive" }.Any(args.Values.ContainsKey)) return;
        var requested = quality.Requested;
        var preset = args.Values.ContainsKey("quality") ? Enum.Parse<QualityPreset>(args.Value("quality"), true) : requested.Preset;
        var overrides = args.Values.ContainsKey("quality") ? new Dictionary<QualityKnob, float>() : new Dictionary<QualityKnob, float>(requested.Overrides);
        foreach (var (knob, value) in args.QualityOverrides()) overrides[knob] = value;
        var profile = new QualityProfile(preset, overrides, args.Values.ContainsKey("adaptive") ? args.Value("adaptive") == "1" : requested.Adaptive);
        var result = await quality.Apply(profile, ApplyOrigin.CommandLine);
        if (result.Status != ApplyStatus.Applied)
            throw new InvalidOperationException($"Command-line quality was not applied: {result.Status}; rejected={string.Join(',', result.Rejected)}.");
    }

    private static double Number(BootArguments args, string key, double fallback) => args.Values.TryGetValue(key, out var value)
        ? double.Parse(value, CultureInfo.InvariantCulture) : fallback;
}
