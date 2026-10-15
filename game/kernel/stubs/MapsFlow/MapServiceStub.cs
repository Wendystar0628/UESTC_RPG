using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Stubs;

[KernelService(typeof(IMapService), Owner = "WP-00", IsStub = true)]
public sealed class MapServiceStub(ICameraRig camera) : IMapService
{
    private IReadOnlyList<Waypoint> _waypoints = [];
    public long Rev { get; private set; }
    public MapId Default { get; private set; }
    public IReadOnlyList<MapDescriptor> Maps { get; private set; } = [];
    public IReadOnlyList<MapVisit> Visits { get; private set; } = [];
    public MapId Current { get; private set; }
    public IMapContext? Context { get; private set; }
    public IMapTransition? Transition => null;
    public event Action<MapId, MapTravelCause>? Arrived;

    public void LoadFromBytes(ReadOnlySpan<byte> indexBytes, ReadOnlySpan<byte> mapBytes,
        IWorldModel world, GeoFrame geo, Vector2 center, Box3 bounds,
        TileKey tileMin, TileKey tileMax, string bakeRoot, IReadOnlyList<Waypoint> waypoints)
    {
        var index = JsonSerializer.Deserialize<MapIndex>(indexBytes, ContractJson.Options)
            ?? throw new JsonException("Map index is null.");
        var map = JsonSerializer.Deserialize<MapFile>(mapBytes, ContractJson.Options)
            ?? throw new JsonException("Map file is null.");
        if (index.Version != 1 || map.Version < 1 || index.Maps.Count != 1)
            throw new InvalidDataException("D0 requires one versioned map in the index.");
        var entry = index.Maps[0];
        if (entry.Id != map.Id || index.DefaultMap != map.Id)
            throw new InvalidDataException("Map file and default index entry do not match.");
        if (!entry.Dir.StartsWith("res://", StringComparison.Ordinal) || !entry.Dir.EndsWith('/'))
            throw new InvalidDataException("Map data root must be an absolute res:// directory.");
        if (!bakeRoot.StartsWith("res://", StringComparison.Ordinal) || !bakeRoot.EndsWith('/'))
            throw new InvalidDataException("Map bake root must be an absolute res:// directory.");
        ArgumentNullException.ThrowIfNull(world);
        var descriptor = map.Complete(geo, center, bounds, tileMin, tileMax);
        _waypoints = Array.AsReadOnly(waypoints.ToArray());
        var pose = ResolvePose(descriptor, descriptor.DefaultSpawn);
        Default = map.Id;
        Current = map.Id;
        Maps = Array.AsReadOnly(new[] { descriptor });
        Context = new MapContextStub(descriptor, world, entry.Dir, bakeRoot);
        camera.SetPose(pose);
        Visits = Array.AsReadOnly(new[] { new MapVisit(Current, pose, camera.Mode, 0, 0) });
        Rev++;
        Arrived?.Invoke(Current, MapTravelCause.Boot);
    }

    public IMapTransition Travel(MapTravelRequest r)
    {
        var from = Current;
        if (Context is not { } context || r.Target != Current)
            return new MapTransitionStub(from, r.Target, r.Cause, MapTravelResult.Rejected);
        Pose pose;
        try
        {
            pose = r.Pose ?? ResolvePose(context.Descriptor, r.Spawn ?? context.Descriptor.DefaultSpawn);
        }
        catch (ArgumentException)
        {
            return new MapTransitionStub(from, r.Target, r.Cause, MapTravelResult.Rejected);
        }
        camera.SetPose(pose);
        Visits = Array.AsReadOnly(new[] { new MapVisit(Current, pose, camera.Mode, 0, 0) });
        Rev++;
        Arrived?.Invoke(Current, r.Cause);
        return new MapTransitionStub(from, r.Target, r.Cause, MapTravelResult.Arrived);
    }

    public Pose ResolvePose(MapDescriptor map, string id) =>
        map.Spawns.FirstOrDefault(s => s.Id == id)?.Fly ??
        _waypoints.FirstOrDefault(w => w.Id == id)?.Fly ??
        throw new ArgumentException($"Unknown spawn or waypoint: {id}", nameof(id));
}

internal sealed record MapContextStub(MapDescriptor Descriptor, IWorldModel World,
    string DataRoot, string BakeRoot) : IMapContext;

internal sealed class MapTransitionStub(MapId from, MapId to, MapTravelCause cause, MapTravelResult result) : IMapTransition
{
    public MapId From { get; } = from;
    public MapId To { get; } = to;
    public MapTravelCause Cause { get; } = cause;
    public MapStage Stage => MapStage.Done;
    public float Progress => 1;
    public Task<MapTravelResult> Done { get; } = Task.FromResult(result);
}
