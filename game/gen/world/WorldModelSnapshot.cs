using System.Collections.ObjectModel;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.World;

public sealed class WorldModelSnapshot : IWorldModel, IWalkDataSource, IWalkSupportSource
{
    public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; }
    public PolygonRings Boundary { get; }
    public WorldNetwork Network { get; }
    public ISpatialIndex Spatial { get; }
    public IInteriorIndex Interiors { get; }
    public IOutskirtsModel? Outskirts { get; }
    public IFidelityModel? Fidelity { get; }
    public IReadOnlyList<InteriorOverlayFile> InteriorOverlays { get; }
    public WalkBakeData? Walk { get; }
    public ITerrain Terrain { get; }
    public IWalkSupportSource? WalkSupport { get; }

    internal WorldModelSnapshot(IReadOnlyDictionary<FeatureId, WorldFeature> features, PolygonRings boundary,
        WorldNetwork network, IInteriorIndex interiors, IOutskirtsModel? outskirts,
        IReadOnlyList<InteriorOverlayFile> interiorOverlays, WorldLoadOptions options)
    {
        Features = features; Boundary = boundary; Network = network; Interiors = interiors;
        Outskirts = outskirts; Fidelity = options.Fidelity; InteriorOverlays = interiorOverlays; Walk = options.Walk;
        Terrain = options.Terrain; WalkSupport = options.WalkSupport;
        Spatial = new WorldSpatialIndex(features, boundary, interiors, Walk, Terrain, WalkSupport);
    }
    public float? SupportY(float x, float z, float yHint) => WalkSupport?.SupportY(x, z, yHint);

    /// <summary>Finalize the parsed world with its exact, immutable prepared walking support.</summary>
    public WorldModelSnapshot WithWalkSupport(IWalkSupportSource support) => new(Features, Boundary, Network,
        Interiors, Outskirts, InteriorOverlays, new()
        { Fidelity = Fidelity, Walk = Walk, Terrain = Terrain, WalkSupport = support ?? throw new ArgumentNullException(nameof(support)) });
}

internal static class WorldFeatureKinds
{
    public static IEnumerable<WorldFeaturePart> Parts(WorldFeature feature, string kind)
    {
        if (feature.Parts.Count == 0)
        {
            if (feature.Kind.Value == kind && feature.Geometry is not (PointGeometry or PolylineGeometry))
                yield return new("", feature.Geometry, feature.Height, feature.Storeys);
            yield break;
        }
        foreach (var part in feature.Parts)
        {
            var partKind = part.Props is { ValueKind: JsonValueKind.Object } props && props.TryGetProperty("kind", out var declared)
                ? declared.GetString() : feature.Kind.Value;
            if (partKind == kind) yield return part;
        }
    }
}

internal sealed class WorldInteriorIndex : IInteriorIndex
{
    private readonly IReadOnlyDictionary<FeatureId, InteriorIndexEntry> _entries;
    public WorldInteriorIndex(IEnumerable<WorldFeature> features, string? catalog = null)
    {
        var entries = new Dictionary<FeatureId, InteriorIndexEntry>();
        foreach (var feature in features.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            var storeys = new List<StoreyInfo>();
            foreach (var part in WorldFeatureKinds.Parts(feature, "building"))
            {
                var count = Math.Max(1, part.Storeys ?? (int)MathF.Ceiling((part.Height ?? 3.6f) / 3.6f));
                var height = (part.Height ?? count * 3.6f) / count;
                foreach (var polygon in WorldModelLoader.Polygons(part.Geometry))
                    for (var floor = 1; floor <= count; floor++)
                        storeys.Add(new(floor, (floor - 1) * height, height, polygon.Outer));
            }
            if (storeys.Count > 0) entries.Add(feature.Id, new(feature.Id, storeys.AsReadOnly(), [], false, null));
        }
        if (catalog is not null)
        {
            using var document = WorldModelLoader.Parse("interiors.json", catalog);
            IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> closed;
            try { closed = InteriorEntranceJson.ParseClosedEntrances(document.RootElement); }
            catch (JsonException error) { throw new WorldDataException("interiors.json", error.Path ?? "entrances", error.Message, error); }
            foreach (var pair in closed.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal))
            {
                if (!entries.TryGetValue(pair.Key, out var building))
                    throw new WorldDataException("interiors.json", pair.Value[0].Id, $"Closed entrance feature '{pair.Key.Value}' has no building footprint.");
                entries[pair.Key] = building with { Entrances = pair.Value };
            }
            foreach (var item in document.RootElement.GetProperty("interiors").EnumerateArray())
            {
                var provider = WorldModelLoader.Text(item, "providerId");
                foreach (var idValue in item.GetProperty("featureIds").EnumerateArray())
                {
                    var id = new FeatureId(idValue.GetString()!);
                    if (!entries.TryGetValue(id, out var entry)) throw new WorldDataException("interiors.json", id.Value, "Open interior has no building footprint.");
                    var entrances = new List<EntranceDecl>();
                    foreach (var door in item.GetProperty("entrances").EnumerateArray())
                    {
                        if (WorldModelLoader.Text(door, "featureId") != id.Value) continue;
                        var entrance = new EntranceDecl(WorldModelLoader.Text(door, "id"),
                            Vector(door.GetProperty("outsideLanding")), Vector(door.GetProperty("insideLanding")),
                            WorldModelLoader.Finite(door.GetProperty("clearWidth")), door.GetProperty("open").GetBoolean())
                        { Center = Vector(door.GetProperty("doorCenter")), YawDeg = WorldModelLoader.Finite(door.GetProperty("facingDeg")) };
                        if (entrance.ClearWidth <= 0) throw new WorldDataException("interiors.json", entrance.Id, "Invalid entrance width.");
                        entrances.Add(entrance);
                    }
                    entries[id] = entry with { Open = true, ProviderId = provider, Entrances = entrances.AsReadOnly() };
                }
            }
        }
        _entries = new ReadOnlyDictionary<FeatureId, InteriorIndexEntry>(entries);
    }
    private static Vector3 Vector(JsonElement e) => new(WorldModelLoader.Finite(e[0]), WorldModelLoader.Finite(e[1]), WorldModelLoader.Finite(e[2]));
    public InteriorIndexEntry? Get(FeatureId id) => _entries.GetValueOrDefault(id);
    public IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r) => _entries.OrderBy(e => e.Key.Value, StringComparer.Ordinal)
        .SelectMany(e => e.Value.Entrances).Where(e => Vector3.DistanceSquared(e.Center, p) <= r * r).ToArray();
}

/// <summary>Immutable 64 m broad phase; exact polygons, including holes, drive narrow-phase queries.</summary>
internal sealed class WorldSpatialIndex : ISpatialIndex
{
    private readonly IReadOnlyDictionary<FeatureId, WorldFeature> _features;
    private readonly Dictionary<TileKey, List<FeatureId>> _cells = [];
    private readonly PolygonRings _boundary;
    private readonly IInteriorIndex _interiors;
    private readonly WalkBakeData? _walk;
    private readonly ITerrain _terrain;
    private readonly IWalkSupportSource? _support;
    public WorldSpatialIndex(IReadOnlyDictionary<FeatureId, WorldFeature> features, PolygonRings boundary, IInteriorIndex interiors, WalkBakeData? walk, ITerrain terrain, IWalkSupportSource? support)
    {
        _features = features; _boundary = boundary; _interiors = interiors; _walk = walk; _terrain = terrain; _support = support;
        foreach (var feature in features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            for (var z = Cell(feature.Bounds.Min.Z); z <= Cell(feature.Bounds.Max.Z); z++)
                for (var x = Cell(feature.Bounds.Min.X); x <= Cell(feature.Bounds.Max.X); x++)
                {
                    var key = new TileKey(x, z);
                    if (!_cells.TryGetValue(key, out var ids)) _cells[key] = ids = [];
                    ids.Add(feature.Id);
                }
    }
    private static int Cell(float coordinate) => (int)MathF.Floor(coordinate / 64);
    public IReadOnlyList<FeatureId> Query(Box3 box)
    {
        var ids = new HashSet<FeatureId>();
        for (var z = Cell(box.Min.Z); z <= Cell(box.Max.Z); z++)
            for (var x = Cell(box.Min.X); x <= Cell(box.Max.X); x++)
                if (_cells.TryGetValue(new(x, z), out var cell))
                    foreach (var id in cell) if (_features[id].Bounds.Intersects(box)) ids.Add(id);
        return ids.OrderBy(id => id.Value, StringComparer.Ordinal).ToArray();
    }
    public float GroundY(float x, float z) => _terrain.HeightAt(x, z);
    private IEnumerable<WorldFeature> At(float x, float z)
    {
        if (!_cells.TryGetValue(new(Cell(x), Cell(z)), out var ids)) yield break;
        var point = new Vector2(x, z);
        foreach (var id in ids)
        {
            var feature = _features[id];
            if (WorldModelLoader.Polygons(feature.Geometry).Any(p => PlanGeometry.Contains(p, point))) yield return feature;
        }
    }
    public CameraMedium MediumAt(Vector3 p)
    {
        var ground = GroundY(p.X, p.Z);
        foreach (var feature in At(p.X, p.Z))
        {
            var point = new Vector2(p.X, p.Z);
            var part = WorldFeatureKinds.Parts(feature, "building").FirstOrDefault(part => p.Y >= ground && p.Y < ground + (part.Height ?? 3.6f)
                && WorldModelLoader.Polygons(part.Geometry).Any(r => PlanGeometry.Contains(r, point)));
            if (part is not null)
            {
                var storeys = _interiors.Get(feature.Id)?.Storeys;
                var storey = storeys?.Where(s => p.Y >= s.FloorY && p.Y < s.FloorY + s.Height && PlanGeometry.PointInRing(point, s.Footprint)).OrderBy(s => s.Index).FirstOrDefault();
                var distance = WorldModelLoader.Polygons(part.Geometry).Where(r => PlanGeometry.Contains(r, point))
                    .SelectMany(r => new[] { r.Outer }.Concat(r.Holes)).Min(r => PlanGeometry.DistanceToRing(new(p.X, p.Z), r));
                var localFloor = _interiors is WorldInteriorIndex
                    ? 1 + (int)MathF.Floor((p.Y - ground) / ((part.Height ?? 3.6f) / Math.Max(1, part.Storeys ?? 1)))
                    : storey?.Index ?? 1;
                return new(MediumKind.Interior, feature.Id, localFloor, distance);
            }
            if (p.Y < ground && p.Y >= ground - 6 && WorldFeatureKinds.Parts(feature, "water")
                .Any(water => WorldModelLoader.Polygons(water.Geometry).Any(r => PlanGeometry.Contains(r, point))))
                return new(MediumKind.Underwater, feature.Id, Depth: ground - p.Y);
        }
        return p.Y < ground ? new(MediumKind.Underground, Depth: ground - p.Y) : new(MediumKind.Air);
    }
    public WalkClass WalkClassAt(float x, float z)
    {
        if (_walk is { } data)
        {
            var index = WalkIndex(data.Walk, x, z);
            return index < 0 ? WalkClass.Outside : (WalkClass)data.Walk.Classes[index];
        }
        if (!PlanGeometry.Contains(_boundary, new(x, z))) return WalkClass.Outside;
        var water = false;
        foreach (var feature in At(x, z))
        {
            var point = new Vector2(x, z);
            if (WorldFeatureKinds.Parts(feature, "building").Any(part => WorldModelLoader.Polygons(part.Geometry).Any(r => PlanGeometry.Contains(r, point)))) return WalkClass.Building;
            if (WorldFeatureKinds.Parts(feature, "water").Any(part => WorldModelLoader.Polygons(part.Geometry).Any(r => PlanGeometry.Contains(r, point)))) water = true;
        }
        return water ? WalkClass.Water : WalkClass.Walk;
    }
    public float? SupportY(float x, float z, float yHint) => WalkClassAt(x, z) switch
    {
        WalkClass.Walk or WalkClass.Wade or WalkClass.Interior => _support?.SupportY(x, z, yHint),
        WalkClass.Building or WalkClass.Water or WalkClass.Forbidden or WalkClass.Outside or WalkClass.Steep => null,
        _ => throw new ArgumentOutOfRangeException(nameof(x), "Unknown walk class.")
    };
    public Vector3? NearestSafe(Vector3 p, float maxDist)
    {
        if (!float.IsFinite(maxDist) || maxDist < 0) throw new ArgumentOutOfRangeException(nameof(maxDist));
        Vector3? closest = null;
        var best = maxDist * maxDist;
        if (_walk is { } data)
        {
            var grid = data.SafePoints;
            var x0 = Math.Max(0, (int)MathF.Floor((p.X - maxDist - SafePointGrid.CellSize - grid.Origin.X) / SafePointGrid.CellSize));
            var z0 = Math.Max(0, (int)MathF.Floor((p.Z - maxDist - SafePointGrid.CellSize - grid.Origin.Y) / SafePointGrid.CellSize));
            var x1 = Math.Min(grid.Width - 1, (int)MathF.Floor((p.X + maxDist + SafePointGrid.CellSize - grid.Origin.X) / SafePointGrid.CellSize));
            var z1 = Math.Min(grid.Height - 1, (int)MathF.Floor((p.Z + maxDist + SafePointGrid.CellSize - grid.Origin.Y) / SafePointGrid.CellSize));
            for (var z = z0; z <= z1; z++)
                for (var x = x0; x <= x1; x++)
                {
                    if (!grid.TryDecode(z * grid.Width + x, out var candidate)) continue;
                    var support = SupportY(candidate.X, candidate.Z, candidate.Y);
                    if (support is null) continue;
                    candidate.Y = support.Value;
                    var distance = Vector3.DistanceSquared(candidate, p);
                    if (distance < best || (distance == best && closest is null)) { best = distance; closest = candidate; }
                }
            return closest;
        }
        // Deterministic conservative fallback until WP-44 supplies its resident safe-point grid.
        for (var z = (int)MathF.Ceiling((p.Z - maxDist) / 8); z <= (int)MathF.Floor((p.Z + maxDist) / 8); z++)
            for (var x = (int)MathF.Ceiling((p.X - maxDist) / 8); x <= (int)MathF.Floor((p.X + maxDist) / 8); x++)
            {
                var wx = x * 8f; var wz = z * 8f;
                if (WalkClassAt(wx, wz) != WalkClass.Walk) continue;
                var support = SupportY(wx, wz, p.Y);
                if (support is null || _terrain.FactsAt(wx, wz).Grade >= .36397023f) continue;
                var candidate = new Vector3(wx, support.Value, wz);
                var distance = Vector3.DistanceSquared(candidate, p);
                if (distance < best || (distance == best && closest is null)) { best = distance; closest = candidate; }
            }
        return closest;
    }
    private static int WalkIndex(WalkGrid grid, float x, float z)
    {
        var column = (int)MathF.Floor((x - grid.Origin.X) / WalkGrid.CellSize);
        var row = (int)MathF.Floor((z - grid.Origin.Y) / WalkGrid.CellSize);
        return column < 0 || row < 0 || column >= grid.Width || row >= grid.Height ? -1 : row * grid.Width + column;
    }
}
