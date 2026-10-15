using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs.Foundation;

[KernelService(typeof(IWorldModel), Owner = "WP-00", IsStub = true)]
public sealed class StubWorldModel(ISpatialIndex spatial, IInteriorIndex interiors, IFidelityModel fidelity) : IWorldModel
{
    public PolygonRings Boundary { get; private set; } = new([]);
    public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; private set; } =
        new ReadOnlyDictionary<FeatureId, WorldFeature>(new Dictionary<FeatureId, WorldFeature>());
    public WorldNetwork Network { get; private set; } = WorldNetwork.Empty;
    public ITerrain Terrain { get; private set; } = FlatTerrain.Default;
    public ISpatialIndex Spatial { get; } = spatial;
    public IInteriorIndex Interiors { get; } = interiors;
    public IOutskirtsModel? Outskirts => null;
    public IFidelityModel? Fidelity { get; } = fidelity;

    /// <summary>Allows D0 fixture data to be supplied without embedding a map or feature ID.</summary>
    public void Configure(IEnumerable<WorldFeature> features, WorldNetwork? network = null, PolygonRings? boundary = null,
        ITerrain? terrain = null)
    {
        ArgumentNullException.ThrowIfNull(features);
        var snapshot = new ReadOnlyDictionary<FeatureId, WorldFeature>(
            features.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal)
                .ToDictionary(feature => feature.Id));
        Features = snapshot;
        Network = network ?? WorldNetwork.Empty;
        Terrain = terrain ?? FlatTerrain.Default;
        Boundary = boundary is null ? new([]) : new(
            Array.AsReadOnly(boundary.Outer.ToArray()),
            Array.AsReadOnly(boundary.Holes.Select(hole => (IReadOnlyList<Vector2>)Array.AsReadOnly(hole.ToArray())).ToArray()));
        if (Spatial is StubSpatialIndex stubSpatial) stubSpatial.Configure(snapshot, Terrain);
        if (Fidelity is StubFidelityModel stubFidelity) stubFidelity.Configure(snapshot);
    }

    public void Configure(D0CampusData data, ITerrain? terrain = null)
    {
        ArgumentNullException.ThrowIfNull(data);
        Configure(data.Features.Values, boundary: data.Boundary, terrain: terrain);
    }
}

[KernelService(typeof(ISpatialIndex), Owner = "WP-00", IsStub = true)]
public sealed class StubSpatialIndex : ISpatialIndex
{
    private IReadOnlyList<WorldFeature> _features = [];
    private ITerrain _terrain = FlatTerrain.Default;
    public void Configure(IReadOnlyDictionary<FeatureId, WorldFeature> features, ITerrain? terrain = null)
    {
        _features = features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
        _terrain = terrain ?? FlatTerrain.Default;
    }
    public IReadOnlyList<FeatureId> Query(Box3 box) =>
        _features.Where(feature => feature.Bounds.Intersects(box)).Select(feature => feature.Id).ToArray();
    public float GroundY(float x, float z) => _terrain.HeightAt(x, z);
    public CameraMedium MediumAt(Vector3 p) => new(MediumKind.Air);
    public float? SupportY(float x, float z, float yHint) => _terrain.HeightAt(x, z);
    public WalkClass WalkClassAt(float x, float z) => WalkClass.Walk;
    public Vector3? NearestSafe(Vector3 p, float maxDist)
    {
        if (!float.IsFinite(maxDist) || maxDist < 0f) throw new ArgumentOutOfRangeException(nameof(maxDist));
        if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z)) return null;
        float y = _terrain.HeightAt(p.X, p.Z);
        return float.IsFinite(y) && MathF.Abs(p.Y - y) <= maxDist ? new Vector3(p.X, y, p.Z) : null;
    }
}

[KernelService(typeof(IFidelityModel), Owner = "WP-00", IsStub = true)]
public sealed class StubFidelityModel : IFidelityModel
{
    private IReadOnlyDictionary<FeatureId, WorldFeature> _features =
        new ReadOnlyDictionary<FeatureId, WorldFeature>(new Dictionary<FeatureId, WorldFeature>());
    public string InputHash => "d0-stub-k3";
    public void Configure(IReadOnlyDictionary<FeatureId, WorldFeature> features) => _features = features;
    public IReadOnlyList<FacadeSegment> Facades(FeatureId building)
    {
        if (!_features.TryGetValue(building, out var feature)) return [];
        IReadOnlyList<PolygonRings> polygons = feature.Geometry switch
        {
            PolygonGeometry polygon => [polygon.Rings],
            MultiPolygonGeometry multi => multi.Polygons,
            _ => []
        };
        var segments = new List<FacadeSegment>();
        foreach (var polygon in polygons) Append(polygon.Outer);
        foreach (var polygon in polygons)
            foreach (var hole in polygon.Holes) Append(hole);
        return segments.AsReadOnly();

        void Append(IReadOnlyList<Vector2> ring)
        {
            var count = ring.Count;
            if (count > 1 && ring[0] == ring[count - 1]) count--;
            if (count < 3) return;
            for (var i = 0; i < count; i++)
                segments.Add(new(building, segments.Count, ring[i], ring[(i + 1) % count], FidelityZone.K3, FidelityZone.K3));
        }
    }
    public RoofZone Roof(FeatureId building) => new(building, FidelityZone.K3);
    public FidelityZone GroundAt(float x, float z) => FidelityZone.K3;
    public float WalkDistanceAt(float x, float z) => 15f;
}
