using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Geometry;

namespace Campus.Gen.World;

/// <summary>Immutable authored map catalogue. Parsing never reads files or retains a world model.</summary>
public sealed class MapCatalog
{
    private readonly IReadOnlyDictionary<MapId, MapCatalogEntry> _entries;
    public MapId Default { get; }
    public IReadOnlyList<MapDescriptor> Maps { get; }

    public MapCatalog(MapIndex index, IReadOnlyList<MapCatalogEntry> entries)
    {
        if (index.Version != 1 || index.Maps.Count == 0) throw new InvalidDataException("Unsupported or empty map index.");
        var byId = new Dictionary<MapId, MapCatalogEntry>();
        foreach (var entry in entries.OrderBy(e => e.Descriptor.Id.Value, StringComparer.Ordinal))
            if (!byId.TryAdd(entry.Descriptor.Id, entry)) throw new InvalidDataException("Duplicate map ID.");
        if (byId.Count != index.Maps.Count || !byId.ContainsKey(index.DefaultMap))
            throw new InvalidDataException("Map index and catalogue disagree.");
        foreach (var entry in index.Maps)
            if (!byId.TryGetValue(entry.Id, out var map) || map.DataRoot != entry.Dir)
                throw new InvalidDataException("Map index entry is missing or has a different directory.");
        Default = index.DefaultMap;
        _entries = new ReadOnlyDictionary<MapId, MapCatalogEntry>(byId);
        Maps = Array.AsReadOnly(entries.OrderBy(e => e.Descriptor.Id.Value, StringComparer.Ordinal)
            .Select(e => e.Descriptor).ToArray());
    }

    public bool TryGet(MapId id, out MapCatalogEntry entry) => _entries.TryGetValue(id, out entry!);

    public static MapIndex ReadIndex(ReadOnlySpan<byte> bytes)
    {
        var index = JsonSerializer.Deserialize<MapIndex>(bytes, ContractJson.Options)
            ?? throw new InvalidDataException("Map index is null.");
        if (index.Version != 1 || index.Maps is null || index.Maps.Count == 0)
            throw new InvalidDataException("Unsupported or empty map index.");
        var ids = new HashSet<MapId>();
        var dirs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in index.Maps)
        {
            ValidateId(entry.Id);
            ValidateRoot(entry.Dir);
            if (!entry.Dir.EndsWith('/' + entry.Id.Value + '/', StringComparison.Ordinal))
                throw new InvalidDataException("Map ID must match its directory name.");
            if (!ids.Add(entry.Id) || !dirs.Add(entry.Dir)) throw new InvalidDataException("Duplicate map index entry.");
        }
        if (!ids.Contains(index.DefaultMap)) throw new InvalidDataException("Default map is not indexed.");
        return index;
    }

    public static MapCatalogEntry ReadEntry(MapIndexEntry entry, ReadOnlySpan<byte> mapBytes,
        ReadOnlyMemory<byte> campusBytes, ReadOnlySpan<byte> waypointBytes)
    {
        var file = JsonSerializer.Deserialize<MapFile>(mapBytes, ContractJson.Options)
            ?? throw new InvalidDataException("Map file is null.");
        ValidateId(file.Id);
        ValidateRoot(entry.Dir);
        if (file.Id != entry.Id || file.Version < 1) throw new InvalidDataException("Map file identity or version is invalid.");
        Validate(file);
        using var campus = JsonDocument.Parse(campusBytes);
        var meta = campus.RootElement.GetProperty("meta");
        var origin = meta.GetProperty("origin");
        var bounds = meta.GetProperty("bounds");
        if (origin.GetArrayLength() != 2 || bounds.GetArrayLength() != 4) throw new InvalidDataException("Invalid campus metadata dimensions.");
        var lon = origin[0].GetDouble();
        var lat = origin[1].GetDouble();
        var crs = meta.GetProperty("crs").GetString() ?? "";
        if (!double.IsFinite(lon) || !double.IsFinite(lat) || lon is < -180 or > 180 || lat is < -90 or > 90 ||
            !crs.StartsWith("EPSG:", StringComparison.Ordinal) ||
            !int.TryParse(crs.AsSpan(5), NumberStyles.None, CultureInfo.InvariantCulture, out var epsg) || epsg <= 0)
            throw new InvalidDataException("Invalid geographic reference.");
        var min = new Vector2(bounds[0].GetSingle(), bounds[1].GetSingle());
        var max = new Vector2(bounds[2].GetSingle(), bounds[3].GetSingle());
        ValidateRect(min, max);
        if (file.DataRect.Min.X > min.X || file.DataRect.Min.Y > min.Y || file.DataRect.Max.X < max.X || file.DataRect.Max.Y < max.Y)
            throw new InvalidDataException("Data rectangle does not cover the campus boundary.");
        var center = BoundaryCenter(campus.RootElement.GetProperty("boundary"));
        var margin = new Vector2(file.Roam.FallbackMargin);
        var box = new Box3(new(min.X - margin.X, file.Roam.FloorY, min.Y - margin.Y),
            new(max.X + margin.X, file.Roam.CeilY, max.Y + margin.Y));
        var tileMin = TileGrid.At(box.Min);
        var tileMax = new TileKey((int)MathF.Ceiling(box.Max.X / TileGrid.BaseEdge) - 1, (int)MathF.Ceiling(box.Max.Z / TileGrid.BaseEdge) - 1);
        if (Math.Abs(tileMin.X) >= 512 || Math.Abs(tileMin.Z) >= 512 || Math.Abs(tileMax.X) >= 512 || Math.Abs(tileMax.Z) >= 512)
            throw new InvalidDataException("Map tile indices exceed the supported range.");
        var waypoints = waypointBytes.IsEmpty ? new WaypointFile(1, []) :
            JsonSerializer.Deserialize<WaypointFile>(waypointBytes, ContractJson.Options)
            ?? throw new InvalidDataException("Waypoint file is null.");
        if (waypoints.Version != 1 || waypoints.Waypoints is null ||
            waypoints.Waypoints.Select(w => w.Id).Distinct(StringComparer.Ordinal).Count() != waypoints.Waypoints.Count)
            throw new InvalidDataException("Waypoint version or IDs are invalid.");
        foreach (var waypoint in waypoints.Waypoints) ValidatePose(waypoint.Fly);
        var descriptor = file.Complete(new(lon, lat, epsg, min, max), center, box, tileMin, tileMax);
        var result = new MapCatalogEntry(descriptor, entry.Dir,
            Array.AsReadOnly(waypoints.Waypoints.ToArray()));
        _ = result.ResolveSpawn(file.DefaultSpawn);
        return result;
    }

    private static void Validate(MapFile file)
    {
        if (file.DataRect is null || file.Roam is null || file.World is null || file.Env is null ||
            file.Spawns is null || file.Cinematics is null || file.Portals is null || string.IsNullOrWhiteSpace(file.DefaultSpawn))
            throw new InvalidDataException("Missing map fields.");
        if (file.TerrainDefaults is { } defaults)
        {
            try { TerrainOverlayJson.ValidateTerrainDefaults(defaults); }
            catch (ArgumentException error) { throw new InvalidDataException("Invalid map terrain defaults.", error); }
        }
        ValidateRect(file.DataRect.Min, file.DataRect.Max);
        var size = file.DataRect.Max - file.DataRect.Min;
        if (size.X % TileGrid.BaseEdge != 0 || size.Y % TileGrid.BaseEdge != 0) throw new InvalidDataException("Data rectangle dimensions must be multiples of 64 metres.");
        var r = file.Roam;
        float[] limits = [r.HeightFactor, r.HeightStart, r.MaxRadius, r.FloorY, r.CeilY, r.Buffer,
            r.SoftFloorClearance, r.FallbackMargin];
        if (limits.Any(v => !float.IsFinite(v)) || r.MaxRadius <= 0 || r.MaxRadius > 8000 || r.FloorY >= r.CeilY ||
            r.HeightFactor < 0 || r.Buffer < 0 || r.FallbackMargin < 0 || r.SoftFloorClearance < 0)
            throw new InvalidDataException("Invalid map roam limits.");
        if (file.Spawns.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != file.Spawns.Count)
            throw new InvalidDataException("Duplicate map spawn ID.");
        foreach (var spawn in file.Spawns) ValidatePose(spawn.Fly);
        if (file.Title is { } title) ValidatePose(title.T0);
        ValidateRelative(file.Env.Sky);
        ValidateRelative(file.Env.Backdrop);
        if (file.Env.Outskirts is { } outskirts) ValidateRelative(outskirts);
    }

    public static void ValidatePose(Pose pose)
    {
        if (!VectorMath.Finite(pose.Position) ||
            !float.IsFinite(pose.YawDeg) || !float.IsFinite(pose.PitchDeg) ||
            pose.FovDeg is { } fov && (!float.IsFinite(fov) || fov <= 0 || fov >= 180))
            throw new InvalidDataException("Map pose contains invalid numbers.");
    }

    public static void ValidateRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !root.StartsWith("res://", StringComparison.Ordinal) || !root.EndsWith('/'))
            throw new InvalidDataException("Map data root must be an absolute res:// directory.");
        ValidateRelative(root[6..].TrimEnd('/'));
    }

    public static void ValidateRelative(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains(':') || path.Contains('\\') || path.StartsWith('/') ||
            path.Split('/').Any(s => s is "" or "." or "..")) throw new InvalidDataException("Invalid relative map data path.");
    }

    private static void ValidateId(MapId id)
    {
        if (string.IsNullOrEmpty(id.Value) || id.Value.Length > 32 ||
            id.Value.Any(c => c is not (>= 'a' and <= 'z') and not (>= '0' and <= '9') and not '-'))
            throw new InvalidDataException("Invalid map ID.");
    }

    private static void ValidateRect(Vector2 min, Vector2 max)
    {
        if (!VectorMath.Finite(min) || !VectorMath.Finite(max) ||
            min.X >= max.X || min.Y >= max.Y) throw new InvalidDataException("Invalid finite map bounds.");
    }

    private static Vector2 BoundaryCenter(JsonElement boundary)
    {
        var rings = boundary.EnumerateArray().Select(ring => (IReadOnlyList<Vector2>)ring.EnumerateArray()
            .Select(point => new Vector2(point[0].GetSingle(), point[1].GetSingle())).ToArray()).ToArray();
        if (rings.Length == 0) throw new InvalidDataException("Campus boundary has no outer ring.");
        try { return PlanGeometry.Centroid(new PolygonRings(rings[0], rings.Skip(1).ToArray())); }
        catch (ArgumentException error) { throw new InvalidDataException("Invalid campus boundary.", error); }
    }
}

public sealed record MapCatalogEntry(MapDescriptor Descriptor, string DataRoot,
    IReadOnlyList<Waypoint> Waypoints)
{
    public MapSpawn ResolveSpawn(string id)
    {
        var spawn = Descriptor.Spawns.FirstOrDefault(s => s.Id == id);
        if (spawn is not null) return spawn;
        var waypoint = Waypoints.FirstOrDefault(w => w.Id == id);
        return waypoint is null ? throw new ArgumentException($"Unknown spawn or waypoint: {id}.", nameof(id)) :
            new(id, waypoint.Fly, waypoint.Stand);
    }
}
