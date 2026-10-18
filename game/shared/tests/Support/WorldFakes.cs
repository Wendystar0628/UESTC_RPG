using System.Numerics;
using Campus.Contracts;

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
        Network = inner.Network;
        Spatial = inner.Spatial;
        Interiors = inner.Interiors;
        Outskirts = inner.Outskirts;
        Fidelity = inner.Fidelity;
    }

    public ITerrain Terrain { get; set; } = FlatTerrain.Default;
    public PolygonRings Boundary { get; set; } = new([new(-1000f, -1000f), new(1000f, -1000f), new(1000f, 1000f), new(-1000f, 1000f)]);
    public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; set; } = new Dictionary<FeatureId, WorldFeature>();
    public WorldNetwork Network { get; set; } = WorldNetwork.Empty;
    public ISpatialIndex Spatial { get; set; } = new FlatSpatial();
    public IInteriorIndex Interiors { get; set; } = new FakeInteriors();
    public IOutskirtsModel? Outskirts { get; set; }
    public IFidelityModel? Fidelity { get; set; }

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

    private static OverlayGeometry Envelope(Vector2[] points)
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

    public IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 p, float r) =>
        entries.SelectMany(e => e.Entrances).Where(d => Vector3.Distance(d.Center, p) <= r).ToArray();
}

/// <summary>
/// A fidelity model with fixed answers. <paramref name="facades"/> gives each building's segments (none by default),
/// <paramref name="groundAt"/> the ground zone at (x, z) (K3 by default).
/// </summary>
internal sealed class FakeFidelity(
    Func<FeatureId, IReadOnlyList<FacadeSegment>>? facades = null,
    FidelityZone roof = FidelityZone.K3,
    Func<float, float, FidelityZone>? groundAt = null,
    float walkDistance = 0f) : IFidelityModel
{
    public string InputHash => "test";
    public IReadOnlyList<FacadeSegment> Facades(FeatureId building) => facades?.Invoke(building) ?? Array.Empty<FacadeSegment>();
    public RoofZone Roof(FeatureId building) => new(building, roof);
    public FidelityZone GroundAt(float x, float z) => groundAt?.Invoke(x, z) ?? FidelityZone.K3;
    public float WalkDistanceAt(float x, float z) => walkDistance;
}
