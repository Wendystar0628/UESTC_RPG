using Campus.Contracts.Bake;
using Campus.Gen.Geometry;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.World;











public sealed class WorldModelSnapshot : IWorldModel
{
    public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; }
    public IReadOnlyDictionary<string, TerrainOp> TerrainOperations { get; }
    public PolygonRings Boundary { get; }
    public WorldNetwork Network { get; }
    public ISpatialIndex Spatial { get; }
    public IInteriorIndex Interiors { get; }
    public IOutskirtsModel? Outskirts { get; }
    public IFidelityModel Fidelity { get; }
    public ITerrain Terrain { get; }

    internal WorldModelSnapshot(IReadOnlyDictionary<FeatureId, WorldFeature> features, PolygonRings boundary,
        WorldNetwork network, IInteriorIndex interiors, IOutskirtsModel? outskirts,
        IFidelityModel fidelity,
        IReadOnlyDictionary<string, TerrainOp> terrainOperations, ITerrain terrain)
    {
        Features = features; Boundary = boundary; Network = network; Interiors = interiors;
        Outskirts = outskirts; Fidelity = fidelity;
        Terrain = terrain;
        TerrainOperations = terrainOperations;
        Spatial = new WorldSpatialIndex(features, boundary, network, interiors, Terrain);
    }
    /// <summary>Complete the world before publication using already baked provider layouts; no provider executes here.</summary>
    public static IWorldModel WithInteriors(IWorldModel source, IReadOnlyList<InteriorIndexEntry> completed)
    {
        var entries = source.Features.Keys.Select(source.Interiors.Get).OfType<InteriorIndexEntry>()
            .ToDictionary(entry => entry.Feature);
        foreach (var entry in completed)
        {
            if (!source.Features.ContainsKey(entry.Feature))
                throw new InvalidDataException($"Completed interior has no building: {entry.Feature.Value}.");
            entries[entry.Feature] = entry;
        }
        return new WorldModelSnapshot(source.Features, source.Boundary, source.Network,
            new WorldInteriorIndex(entries.Values), source.Outskirts, source.Fidelity, source.TerrainOperations, source.Terrain);
    }

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
    public WorldInteriorIndex(IEnumerable<WorldFeature> features, string? catalog = null, ITerrain? terrain = null)
    {
        var entries = new Dictionary<FeatureId, InteriorIndexEntry>();
        foreach (var feature in features.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            var storeys = ClosedBuildingStoreys.ClosedBuilding(feature, terrain ?? FlatTerrain.Default);
            if (storeys.Count > 0) entries.Add(feature.Id, new(feature.Id, storeys, [], false, null, [], []));
        }
        if (catalog is not null)
        {
            using var document = WorldModelLoader.Parse("interiors.json", catalog);
            IReadOnlyDictionary<FeatureId, IReadOnlyList<EntranceDecl>> closed;
            try { closed = InteriorEntranceJson.ParseClosedEntrances(document.RootElement, terrain); }
            catch (JsonException error) { throw new WorldDataException("interiors.json", error.Path ?? "entrances", error.Message, error); }
            foreach (var pair in closed.OrderBy(pair => pair.Key.Value, StringComparer.Ordinal))
            {
                if (!entries.TryGetValue(pair.Key, out var building))
                    throw new WorldDataException("interiors.json", pair.Value[0].Id, $"featureId references '{pair.Key.Value}', which has no building footprint.");
                entries[pair.Key] = building with { Entrances = pair.Value };
            }
            foreach (var item in document.RootElement.GetProperty("interiors").EnumerateArray())
            {
                var parts = InteriorEntranceJson.ParseOpenParts(item);
                var provider = parts.Any(part => InteriorOpenPart.IsIndoor(part.Kind)) ? WorldModelLoader.Text(item, "providerId") : null;
                var openParts = InteriorEntranceJson.ParseOpenParts(item);
                foreach (var idValue in item.GetProperty("featureIds").EnumerateArray())
                {
                    var id = new FeatureId(idValue.GetString()!);
                    if (!entries.TryGetValue(id, out var entry)) throw new WorldDataException("interiors.json", id.Value, $"featureIds references '{id.Value}', which has no building footprint.");
                    var entrances = new List<EntranceDecl>();
                    foreach (var door in item.GetProperty("entrances").EnumerateArray())
                    {
                        if (WorldModelLoader.Text(door, "featureId") != id.Value) continue;
                        EntranceDecl entrance;
                        try { entrance = InteriorEntranceJson.Parse(door, id, terrain); }
                        catch (JsonException error)
                        { throw new WorldDataException("interiors.json", WorldModelLoader.Text(door, "id"), error.Message, error); }
                        entrances.Add(entrance);
                    }
                    var indoor = InteriorOpenPart.IndoorFloorsFor(openParts, id).Count > 0;
                    entries[id] = entry with { Storeys = indoor ? [] : entry.Storeys, Open = true, ProviderId = indoor ? provider : null, Entrances = entrances.AsReadOnly(),
                        OpenStoreys = InteriorOpenPart.IndoorFloorsFor(openParts, id),
                        OpenAreas = [] };
                }
            }
        }
        _entries = new ReadOnlyDictionary<FeatureId, InteriorIndexEntry>(entries);
    }
    internal WorldInteriorIndex(IEnumerable<InteriorIndexEntry> entries) =>
        _entries = new ReadOnlyDictionary<FeatureId, InteriorIndexEntry>(entries.ToDictionary(entry => entry.Feature));
    public InteriorIndexEntry? Get(FeatureId id) => _entries.GetValueOrDefault(id);
    public bool IsOpen(FeatureId id, int storey, Vector3 position)
    {
        if (!_entries.TryGetValue(id, out var entry) || !entry.Open) return false;
        var point = new Vector2(position.X, position.Z);
        for (var index = 0; index < entry.OpenAreas.Count; index++)
        {
            var area = entry.OpenAreas[index];
            _ = InteriorOpenPart.IsIndoor(area.Kind);
            if (area.Index == storey && position.Y >= area.FloorY && position.Y < area.FloorY + area.Height &&
                PlanRings.PointInRing(point, area.Footprint)) return true;
        }
        return false;
    }

    public InteriorOpenArea? AreaAt(FeatureId id, Vector3 position)
    {
        if (!_entries.TryGetValue(id, out var entry) || !entry.Open) return null;
        var point = new Vector2(position.X, position.Z);
        InteriorOpenArea? indoor = null;
        for (var index = 0; index < entry.OpenAreas.Count; index++)
        {
            var area = entry.OpenAreas[index];
            if (position.Y < area.FloorY || position.Y >= area.FloorY + area.Height || !PlanRings.PointInRing(point, area.Footprint)) continue;
            switch (area.Kind)
            {
                case InteriorOpenPartKind.IndoorFloor: indoor ??= area; break;
                case InteriorOpenPartKind.OutdoorHighPlace: return area;
                default: throw new InvalidDataException("Unknown frozen access domain.");
            }
        }
        return indoor;
    }
    public IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r) => _entries.OrderBy(e => e.Key.Value, StringComparer.Ordinal)
        .SelectMany(e => e.Value.Entrances).Where(e => Vector3.DistanceSquared(e.Center, p) <= r * r).ToArray();
}

/// <summary>Immutable 64 m broad phase; exact polygons, including holes, drive narrow-phase queries.</summary>
internal sealed class WorldSpatialIndex : ISpatialIndex
{
    private readonly IReadOnlyDictionary<FeatureId, WorldFeature> _features;
    private readonly Dictionary<TileKey, List<FeatureId>> _cells = [];
    private readonly IInteriorIndex _interiors;
    private readonly ITerrain _terrain;
    private readonly WorldWalkRegions _walkRegions;
    private readonly Dictionary<FeatureId, PolygonRings[]> _buildingRings = [];
    private readonly Dictionary<FeatureId, PolygonRings[]> _waterRings = [];
    public WorldSpatialIndex(IReadOnlyDictionary<FeatureId, WorldFeature> features, PolygonRings boundary,
        WorldNetwork network, IInteriorIndex interiors, ITerrain terrain)
    {
        _features = features; _interiors = interiors; _terrain = terrain;
        _walkRegions = new(features.Values, boundary, network);
        foreach (var feature in features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
        {
            _buildingRings.Add(feature.Id, WorldFeatureKinds.Parts(feature, "building").SelectMany(part => WorldFeatureGeometry.PolygonsOf(part.Geometry)).ToArray());
            _waterRings.Add(feature.Id, WorldFeatureKinds.Parts(feature, "water").SelectMany(part => WorldFeatureGeometry.PolygonsOf(part.Geometry)).ToArray());
        }
        foreach (var feature in features.Values.OrderBy(f => f.Id.Value, StringComparer.Ordinal))
            for (var z = Cell(feature.Bounds.Min.Z); z <= Cell(feature.Bounds.Max.Z); z++)
                for (var x = Cell(feature.Bounds.Min.X); x <= Cell(feature.Bounds.Max.X); x++)
                {
                    var key = new TileKey(x, z);
                    if (!_cells.TryGetValue(key, out var ids)) _cells[key] = ids = [];
                    ids.Add(feature.Id);
                }
    }
    private static int Cell(float coordinate) => TileGrid.At(new Vector2(coordinate, 0)).X;
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
    public CameraMedium MediumAt(Vector3 p)
    {
        var ground = GroundY(p.X, p.Z);
        if (_terrain is ITerrainWaterSource terrain && terrain.WaterAt(p.X, p.Z) is { } water && p.Y < water.Surface && p.Y >= ground)
            return new(MediumKind.Underwater, water.Id, Depth: water.Surface - p.Y);
        var point = new Vector2(p.X, p.Z);
        if (_cells.TryGetValue(new(Cell(p.X), Cell(p.Z)), out var ids))
        {
            // Open high places override their real building's vertical envelope, without becoming interior media.
            for (var index = 0; index < ids.Count; index++)
                if (_interiors.AreaAt(ids[index], p) is { } area)
                    switch (area.Kind)
                    {
                        case InteriorOpenPartKind.OutdoorHighPlace: return new(MediumKind.Air);
                        case InteriorOpenPartKind.IndoorFloor: break;
                        default: throw new InvalidDataException("Unknown frozen access domain.");
                    }
            for (var index = 0; index < ids.Count; index++)
            {
                var id = ids[index];
                var rings = _buildingRings[id];
                for (var ringIndex = 0; ringIndex < rings.Length; ringIndex++)
                {
                    var ring = rings[ringIndex];
                    if (!Contains(ring, point)) continue;
                    var storeys = _interiors.Get(id)?.Storeys;
                    if (storeys is null || storeys.Count == 0) throw new InvalidOperationException($"Interior layout is not prepared: {id.Value}.");
                    StoreyInfo? storey = null;
                    for (var level = 0; level < storeys.Count; level++)
                    {
                        var candidate = storeys[level];
                        if (p.Y >= candidate.FloorY && p.Y < candidate.FloorY + candidate.Height &&
                            PlanRings.PointInRing(point, candidate.Footprint) && (storey is null || candidate.Index < storey.Index)) storey = candidate;
                    }
                    if (storey is null) continue;
                    var distance = PlanRings.DistanceToRing(point, ring.Outer);
                    for (var hole = 0; hole < ring.Holes.Count; hole++) distance = MathF.Min(distance, PlanRings.DistanceToRing(point, ring.Holes[hole]));
                    return new(MediumKind.Interior, id, storey.Index, distance);
                }
                if (_terrain is FlatTerrain && p.Y < ground && p.Y >= ground - 6)
                {
                    var waters = _waterRings[id];
                    for (var ringIndex = 0; ringIndex < waters.Length; ringIndex++)
                        if (Contains(waters[ringIndex], point)) return new(MediumKind.Underwater, id, Depth: ground - p.Y);
                }
            }
        }
        return p.Y < ground ? new(MediumKind.Underground, Depth: ground - p.Y) : new(MediumKind.Air);
    }

    private static bool Contains(PolygonRings rings, Vector2 point)
    {
        if (!PlanRings.PointInRing(point, rings.Outer)) return false;
        for (var index = 0; index < rings.Holes.Count; index++)
            if (PlanRings.PointInRing(point, rings.Holes[index])) return false;
        return true;
    }
    public WalkClass WalkClassAt(float x, float z) => _walkRegions.At(x, z);
}
