using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Contracts.Startup;
using Campus.Kernel.Collision.Stubs;
using Campus.Kernel.Maps;
using Campus.Kernel.Stubs.Foundation;
using Godot;
using Services = Campus.Kernel.Registry.Kernel;

namespace Campus.Kernel.Stubs.Boot;

/// <summary>Configures only D0 fallback data before the formal kernel startup selects and travels to a map.</summary>
[KernelNode("d0.bootstrap", Owner = "WP-00")]
public sealed partial class D0Bootstrap : Node, IBootParticipant
{
    private Task? _initialization;
    public string Id => "d0.bootstrap";
    public int Order => -1000;

    public Task Initialize(BootPhase phase, BootContext context)
    {
        return phase switch
        {
            BootPhase.BeforeMap => _initialization ??= Configure(context),
            BootPhase.AfterMapWarm or BootPhase.BeforeReveal => Task.CompletedTask,
            _ => throw new ArgumentOutOfRangeException(nameof(phase))
        };
    }

    private async Task Configure(BootContext boot)
    {
        var registry = Services.Services;
        if (registry.Get<IMapService>() is not MapServiceStub maps) return;
        maps.ConfigureRuntime(NextFrame, registry.Get<IViewControl>());

        var indexBytes = Read(ProjectSettings.GetSetting("campus/maps/index").AsString());
        var index = JsonSerializer.Deserialize<MapIndex>(indexBytes, ContractJson.Options)
            ?? throw new JsonException("Map index is null.");
        if (index.Maps.Count != 1) throw new InvalidDataException("D0 bootstrap supports exactly one map.");
        var entry = index.Maps[0];
        if (boot.Arguments.TryGetValue("map", out var requested) && requested.Length > 0 && requested != entry.Id.Value)
            throw new InvalidDataException($"Unknown D0 map: {requested}");
        var mapBytes = Read(entry.Dir + "map.json");
        var campusBytes = Read(entry.Dir + "campus.json");
        var interiorBytes = Read(entry.Dir + "interiors.json");
        var waypointBytes = Read(entry.Dir + "waypoints.json");
        using var interiorDocument = JsonDocument.Parse(interiorBytes);
        var open = interiorDocument.RootElement.GetProperty("interiors").EnumerateArray()
            .SelectMany(item => item.GetProperty("featureIds").EnumerateArray())
            .Select(value => new FeatureId(value.GetString() ?? throw new JsonException("Null interior feature ID."))).ToHashSet();
        var campus = await Task.Run(() => D0CampusLoader.FromBytes(campusBytes, open));
        var world = registry.Get<IWorldModel>();
        if (world is StubWorldModel stubWorld) stubWorld.Configure(campus);
        var waypoints = JsonSerializer.Deserialize<WaypointFile>(waypointBytes, ContractJson.Options)
            ?? throw new JsonException("Waypoint file is null.");
        var mapFile = JsonSerializer.Deserialize<MapFile>(mapBytes, ContractJson.Options)
            ?? throw new JsonException("Map file is null.");
        var descriptor = mapFile.Complete(campus.Geo, campus.Center, campus.Bounds, campus.TileMin, campus.TileMax);
        var inputBytes = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            ["map.json"] = mapBytes,
            ["campus.json"] = campusBytes,
            ["interiors.json"] = interiorBytes,
            ["waypoints.json"] = waypointBytes
        };
        // Canonical map-relative path only. No implicit lookup of the retired global planting path.
        const string plantingKey = "vegetation/planting.json";
        if (Godot.FileAccess.FileExists(entry.Dir + plantingKey)) inputBytes[plantingKey] = Read(entry.Dir + plantingKey);
        var mapInputs = BackdropInputs.Freeze(descriptor, entry.Dir, inputBytes,
            Godot.FileAccess.FileExists, path => Read(path));
        maps.LoadFromBytes(indexBytes, mapInputs, world, campus.Geo, campus.Center, campus.Bounds,
            campus.TileMin, campus.TileMax, "res://bake/" + entry.Id.Value + "/boot/", waypoints.Waypoints);
        var context = maps.Context ?? throw new InvalidDataException("D0 map context was not configured.");

        // This participant only prepares fallback map data. The normal kernel startup is the sole
        // owner of initial Freeze -> weather Reset/Enter, after map selection and before Travel.
        // Existing saves use Restore; do not create a second timeline or freeze again in this adapter.

        var providers = registry.ForMap(entry.Id.Value).Where(registration => registration.Kind == "interior")
            .ToDictionary(registration => registration.Id,
                registration => registry.CreateInteriorProvider(registration.Type, entry.Id), StringComparer.Ordinal);
        var bindings = D0InteriorSetup.Configure(interiorBytes, providers, world.Terrain);
        var groundContributors = BuildInputAssembly.CaptureGroundMaskContributors(registry, entry.Id);
        var build = new BuildContext
        {
            Catalog = Services.Get<IPaletteCatalogSource>().Catalog,
            Map = context.Descriptor,
            World = world,
            WorldSeed = ReadSeed(boot, "seed", context.Descriptor.Seed),
            PartLods = BuildInputAssembly.CapturePartLods(registry, entry.Id),
            InteriorProviders = bindings.ToDictionary(binding => binding.ProviderId, binding => providers[binding.ProviderId], StringComparer.Ordinal),
            SourceInputs = mapInputs.Entries,
            GlobalInputs = Services.GlobalBuildInputs,
            GroundMaskContributors = groundContributors
        };
        var interiors = await Task.Run(() => D0InteriorSetup.BuildConfigured(build, bindings, mapInputs.Entries["interiors.json"]));
        if (registry.Get<IInteriorIndex>() is InteriorIndexStub interiorIndex) interiorIndex.Configure(interiors.Entries);
        if (registry.Get<INavGraph>() is NavGraphStub navigation) navigation.Configure(interiors.NavNodes, interiors.NavEdges, interiors.NavPortals);
        if (registry.Get<ICollisionWorld>() is CollisionWorldStub collision) collision.Configure(campus.Colliders.Concat(interiors.Colliders));
        if (registry.Get<ICameraRig>() is CameraRigStub camera) camera.ConfigureBounds(campus.Bounds);
        if (registry.Get<IAtlasService>() is AtlasServiceStub atlas)
        {
            atlas.ReadData = Read;
            atlas.LoadFromBytes(entry.Id, waypointBytes);
        }
        if (registry.Get<ISaveService>() is SaveServiceStub save)
        {
            var state = registry.Get<IWorldState>();
            var restore = registry.Get<IWorldRestore>();
            save.BeginNewInstance = _ =>
            {
                if (state is WorldStateStub stubState) stubState.BeginNewInstance();
                else restore.CompleteRestore();
            };
            var registrations = registry.Entries.Where(registration => registration.Kind == "save").ToArray();
            foreach (var type in registrations.Select(registration => registration.Type).Distinct().OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                var section = (ISaveSection)registry.Create(type);
                foreach (var metadata in type.GetCustomAttributes<SaveSectionAttribute>())
                    if (registrations.Any(registration => registration.Type == type && registration.Id == metadata.Id && registration.Scope == metadata.Scope))
                        save.RegisterSection(section, metadata);
            }
        }
        registry.ValidateMap(entry.Id.Value);
    }

    private async Task NextFrame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    private static uint ReadSeed(BootContext context, string name, uint fallback) =>
        context.ExplicitArguments.Contains(name) && context.Arguments.TryGetValue(name, out var value)
            ? uint.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture) : fallback;
    private static byte[] Read(string path)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing required D0 map data: {path}");
        return bytes;
    }
}
