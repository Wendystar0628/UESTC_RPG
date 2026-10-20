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
                var provider = WorldModelLoader.Text(item, "providerId");
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
                    entries[id] = entry with { Storeys = [], Open = true, ProviderId = provider, Entrances = entrances.AsReadOnly(),
                        OpenStoreys = Array.AsReadOnly(openParts.Where(part => part.FeatureId == id).SelectMany(part => part.Floors).Distinct().Order().ToArray()),
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
        if (!_entries.TryGetValue(id, out var entry) || !entry.Open || string.IsNullOrWhiteSpace(entry.ProviderId)) return false;
        var point = new Vector2(position.X, position.Z);
        for (var index = 0; index < entry.OpenAreas.Count; index++)
        {
            var area = entry.OpenAreas[index];
            if (area.Index == storey && position.Y >= area.FloorY && position.Y < area.FloorY + area.Height &&
                PlanGeometry.PointInRing(point, area.Footprint)) return true;
        }
        return false;
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
    public WorldSpatialIndex(IReadOnlyDictionary<FeatureId, WorldFeature> features, PolygonRings boundary,
        WorldNetwork network, IInteriorIndex interiors, ITerrain terrain)
    {
        _features = features; _interiors = interiors; _terrain = terrain;
        _walkRegions = new(features.Values, boundary, network);
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
    private IEnumerable<WorldFeature> At(float x, float z)
    {
        if (!_cells.TryGetValue(new(Cell(x), Cell(z)), out var ids)) yield break;
        var point = new Vector2(x, z);
        foreach (var id in ids)
        {
            var feature = _features[id];
            if (WorldFeatureGeometry.PolygonsOf(feature.Geometry).Any(p => PlanGeometry.Contains(p, point))) yield return feature;
        }
    }
    public CameraMedium MediumAt(Vector3 p)
    {
        var ground = GroundY(p.X, p.Z);
        if (_terrain is ITerrainWaterSource terrain && terrain.WaterAt(p.X, p.Z) is { } water
            && p.Y < water.Surface && p.Y >= ground)
            return new(MediumKind.Underwater, water.Id, Depth: water.Surface - p.Y);
        foreach (var feature in At(p.X, p.Z))
        {
            var point = new Vector2(p.X, p.Z);
            var part = WorldFeatureKinds.Parts(feature, "building").FirstOrDefault(part =>
                WorldFeatureGeometry.PolygonsOf(part.Geometry).Any(r => PlanGeometry.Contains(r, point)));
            if (part is not null)
            {
                var storeys = _interiors.Get(feature.Id)?.Storeys;
                var storey = storeys?.Where(s => p.Y >= s.FloorY && p.Y < s.FloorY + s.Height && PlanGeometry.PointInRing(point, s.Footprint)).OrderBy(s => s.Index).FirstOrDefault();
                var distance = WorldFeatureGeometry.PolygonsOf(part.Geometry).Where(r => PlanGeometry.Contains(r, point))
                    .SelectMany(r => new[] { r.Outer }.Concat(r.Holes)).Min(r => PlanGeometry.DistanceToRing(new(p.X, p.Z), r));
                if (storeys is null || storeys.Count == 0)
                    throw new InvalidOperationException($"Interior layout is not prepared: {feature.Id.Value}.");
                if (storey is null) continue;
                return new(MediumKind.Interior, feature.Id, storey.Index, distance);
            }
            if (_terrain is FlatTerrain && p.Y < ground && p.Y >= ground - 6 && WorldFeatureKinds.Parts(feature, "water")
                .Any(water => WorldFeatureGeometry.PolygonsOf(water.Geometry).Any(r => PlanGeometry.Contains(r, point))))
                return new(MediumKind.Underwater, feature.Id, Depth: ground - p.Y);
        }
        return p.Y < ground ? new(MediumKind.Underground, Depth: ground - p.Y) : new(MediumKind.Air);
    }
    public WalkClass WalkClassAt(float x, float z) => _walkRegions.At(x, z);
}
