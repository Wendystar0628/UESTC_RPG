using System.Numerics;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Gen.Tests.Support;

/// <summary>
/// The shared fake <see cref="IWorldModel"/> of both test hosts: an empty flat world whose parts a test replaces by
/// property, or a copy of a real world with only the replaced parts different (<c>new FakeWorld(real) { Terrain = t }</c>).
/// </summary>
internal class FakeWorld : IWorldModel
{
    public FakeWorld() { }

    public FakeWorld(IWorldModel inner)
    {
        Terrain = inner.Terrain;
        Boundary = inner.Boundary;
        Features = inner.Features;
        TerrainOperations = inner.TerrainOperations;
        Network = inner.Network;
        Spatial = inner.Spatial;
        Interiors = inner.Interiors;
        Outskirts = inner.Outskirts;
        Fidelity = inner.Fidelity;
    }

    public ITerrain Terrain { get; set; } = FlatTerrain.Default;
    public PolygonRings Boundary { get; set; } = new([new(-1000f, -1000f), new(1000f, -1000f), new(1000f, 1000f), new(-1000f, 1000f)]);
    private IReadOnlyDictionary<FeatureId, WorldFeature> _features = new Dictionary<FeatureId, WorldFeature>();
    public IReadOnlyDictionary<FeatureId, WorldFeature> Features
    {
        get => _features;
        set { _features = value; Fidelity = FidelityFixture.Create(value.Values); }
    }
    public IReadOnlyDictionary<string, TerrainOp> TerrainOperations { get; set; } = new Dictionary<string, TerrainOp>(StringComparer.Ordinal);
    public WorldNetwork Network { get; set; } = WorldNetwork.Empty;
    public ISpatialIndex Spatial { get; set; } = new FlatSpatial();
    public IInteriorIndex Interiors { get; set; } = new FakeInteriors();
    public IOutskirtsModel? Outskirts { get; set; }
    public IFidelityModel Fidelity { get; set; } = FidelityFixture.Plain;

    /// <summary>The world with every multi-source entity's Geometry replaced by its parts' bounding rectangle.</summary>
    public static FakeWorld Enveloped(IWorldModel inner) => new(inner)
    {
        Features = inner.Features.ToDictionary(p => p.Key, p => p.Value.Parts.Count == 0 ? p.Value : p.Value with
        {
            Geometry = Envelope(p.Value.Parts.SelectMany(part => part.Geometry switch
            {
                PolygonGeometry g => g.Rings.Outer,
                MultiPolygonGeometry m => m.Polygons.SelectMany(r => r.Outer),
                _ => [],
            }).ToArray()),
        }),
    };

    private static WorldGeometry Envelope(Vector2[] points)
    {
        if (points.Length == 0) return new PointGeometry(Vector3.Zero);
        float x0 = points.Min(p => p.X), x1 = points.Max(p => p.X), z0 = points.Min(p => p.Y), z1 = points.Max(p => p.Y);
        return new PolygonGeometry(new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]));
    }
}

/// <summary>
/// Air and walkable ground everywhere, at <paramref name="height"/>(x, z) (level at 0 by default). A query returns every key of
/// <paramref name="candidates"/> in ordinal order (the caller tests containment itself), or nothing without them.
/// With <paramref name="forbidden"/> every member throws it as an <see cref="InvalidOperationException"/> message.
/// </summary>
internal sealed class FlatSpatial(
    Func<float, float, float>? height = null,
    IReadOnlyDictionary<FeatureId, WorldFeature>? candidates = null,
    string? forbidden = null) : ISpatialIndex
{
    public IReadOnlyList<FeatureId> Query(Box3 box)
    {
        Allow();
        return candidates is null ? Array.Empty<FeatureId>() : candidates.Keys.OrderBy(k => k.Value, StringComparer.Ordinal).ToArray();
    }

    public float GroundY(float x, float z)
    {
        Allow();
        return height?.Invoke(x, z) ?? 0f;
    }

    public CameraMedium MediumAt(Vector3 p)
    {
        Allow();
        return new CameraMedium(MediumKind.Air);
    }

    public WalkClass WalkClassAt(float x, float z)
    {
        Allow();
        return WalkClass.Walk;
    }

    private void Allow()
    {
        if (forbidden is not null) throw new InvalidOperationException(forbidden);
    }
}

/// <summary>An interior index of exactly the given entries (none by default); an entrance is "near" within its radius of its Center.</summary>
internal sealed class FakeInteriors(params InteriorIndexEntry[] entries) : IInteriorIndex
{
    public InteriorIndexEntry? Get(FeatureId id) => entries.FirstOrDefault(e => e.Feature == id);

    public bool IsOpen(FeatureId id, int storey, Vector3 position)
    {
        var entry = Get(id);
        if (entry is null || !entry.Open) return false;
        var point = new Vector2(position.X, position.Z);
        for (var index = 0; index < entry.OpenAreas.Count; index++)
        {
            var area = entry.OpenAreas[index];
            _ = InteriorOpenPart.IsIndoor(area.Kind);
            if (area.Index == storey && position.Y >= area.FloorY && position.Y < area.FloorY + area.Height &&
                Campus.Gen.Geometry.PlanGeometry.PointInRing(point, area.Footprint)) return true;
        }
        return false;
    }

    public InteriorOpenArea? AreaAt(FeatureId id, Vector3 position)
    {
        var entry = Get(id);
        if (entry is null || !entry.Open) return null;
        var point = new Vector2(position.X, position.Z);
        InteriorOpenArea? indoor = null;
        for (var index = 0; index < entry.OpenAreas.Count; index++)
        {
            var area = entry.OpenAreas[index];
            if (position.Y < area.FloorY || position.Y >= area.FloorY + area.Height || !Campus.Gen.Geometry.PlanGeometry.PointInRing(point, area.Footprint)) continue;
            switch (area.Kind)
            {
                case InteriorOpenPartKind.IndoorFloor: indoor ??= area; break;
                case InteriorOpenPartKind.OutdoorHighPlace: return area;
                default: throw new InvalidDataException("Unknown frozen access domain.");
            }
        }
        return indoor;
    }
    public IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r) =>
        entries.SelectMany(e => e.Entrances).Where(d => Vector3.Distance(d.Center, p) <= r).ToArray();
}

/// <summary>Explicit frozen test data consumed by the production fidelity reader.</summary>
internal static class FidelityFixture
{
    public static IFidelityModel Plain { get; } = Create([]);

    public static IFidelityModel Create(IEnumerable<WorldFeature> source,
        FidelityZone zone = FidelityZone.K3, FidelityZone ground = FidelityZone.K3,
        FidelityZone roof = FidelityZone.K4, Func<float, float, FidelityZone>? groundAt = null)
    {
        var features = source.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
        var segments = new List<FacadeSegment>();
        foreach (var feature in features)
        {
            var index = 0;
            foreach (var polygon in BuildingPolygons(feature))
                foreach (var ring in new[] { polygon.Outer }.Concat(polygon.Holes))
                    for (var i = 0; i < ring.Count; i++)
                        segments.Add(new(feature.Id, index++, ring[i], ring[(i + 1) % ring.Count], zone, ground));
        }
        const int side = 1000;
        var cells = new byte[side * side];
        if (groundAt is null) Array.Fill(cells, (byte)ground);
        else
            for (var z = 0; z < side; z++)
                for (var x = 0; x < side; x++)
                    cells[z * side + x] = (byte)groundAt(-1000 + (x + .5f) * WorldFidelityCompiler.GroundCell,
                        -1000 + (z + .5f) * WorldFidelityCompiler.GroundCell);
        var roofs = features.Where(feature => BuildingPolygons(feature).Any())
            .Select(feature => new RoofZone(feature.Id, roof)).ToArray();
        return new WorldFidelityModel(new(new(-1000, -1000), side, side, cells, segments, roofs, new Dictionary<FeatureId, FidelityZone>(), "fixture"));
    }

    // Unknown test geometries belong to the consumer's rejection path, not automatic fidelity setup.
    private static IEnumerable<PolygonRings> BuildingPolygons(WorldFeature feature)
    {
        if (feature.Parts.Count == 0)
            return Known(feature.Geometry) ? WorldFeatureGeometry.FacadePolygonsOf(feature, "building") : [];
        return feature.Parts.Where(part => Known(WorldFeatureGeometry.FacadeGeometryOf(part)))
            .SelectMany(part => WorldFeatureGeometry.FacadePolygonsOf(feature with { Parts = [part] }, "building"));

        static bool Known(WorldGeometry geometry) =>
            geometry is PolygonGeometry or MultiPolygonGeometry or PolylineGeometry or PointGeometry;
    }
}
