using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;

namespace Campus.Tests;

// A real catalogue resolver backs this test double. It does not return a fixed pose
// for arbitrary IDs or silently inherit IMapService's NotSupported default method.
internal sealed class CinematicSpawnMapStub : IMapService
{
    public static readonly MapId Map = new("cinematic-spawn-test");
    public static readonly Pose DefaultPose = new(new(120, 35, -70), 20, -35);
    // This path intentionally has no files. Entry must not re-read waypoints.json.
    public const string DataRoot = "res://cinematic-test-no-files/";
    private readonly MapCatalogEntry _entry;
    public int DefaultRequests { get; private set; }
    public int NamedRequests { get; private set; }
    public long Rev => 1;
    public MapId Default => Map;
    public IReadOnlyList<MapDescriptor> Maps { get; }
    public IReadOnlyList<MapVisit> Visits => [];
    public MapId Current => Map;
    public IMapContext Context { get; }
    public IMapTransition? Transition => null;
    public event Action<MapId, MapTravelCause>? Arrived { add { } remove { } }
    public CinematicSpawnMapStub(IReadOnlyList<MapSpawn>? inlineSpawns = null)
    {
        var descriptor = new MapDescriptor(Map, new("测试地图", "Test map"), MapKind.Outdoor, 1, 1,
            new(0, 0, 0, new(-500, -500), new(500, 500)), Vector2.Zero,
            new(new(-500, -50, -500), new(500, 500, 500)), new(new(-512, -512), new(512, 512)),
            new(0.9f, 30, 3000, -28, 2000, 30, 40, 150), new(-8, -8), new(7, 7), "start", inlineSpawns ?? [],
            new(MapTimeRule.Shared, new(17.5, Season.Autumn, WeatherKind.Clear), new("默认", "Default"), true, "default"),
            new("sky", "backdrop", []), new Dictionary<string, string>(), []);
        Waypoint[] waypoints =
        [
            new("start", new("起点", "Start"), WaypointGroup.Overview, DefaultPose, null, null, new(17.5, Season.Autumn)),
            new("named", new("另一个点", "Other point"), WaypointGroup.Core, new(new(-30, 80, 50), 90, -25), null, null, new(17.5, Season.Autumn)),
        ];
        _entry = new(descriptor, DataRoot, waypoints);
        Maps = [descriptor];
        Context = new TestMapContext(descriptor, new TestWorldModel(),
            BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>()), DataRoot);
    }
    public MapSpawn ResolveSpawn(MapId map, string? spawn = null)
    {
        if (map != Map) throw new ArgumentException($"Unknown map: {map.Value}", nameof(map));
        if (spawn is null) DefaultRequests++; else NamedRequests++;
        return _entry.ResolveSpawn(spawn ?? _entry.Descriptor.DefaultSpawn);
    }
    public IMapTransition Travel(MapTravelRequest request) => throw new InvalidOperationException("The save fixture owns entry; no extra travel is expected.");
}
