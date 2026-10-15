using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;

namespace Campus.Kernel.Stubs.Foundation;

[KernelService(typeof(IWorldModel), Owner = "WP-00", IsStub = true)]
public sealed class StubWorldModel(ISpatialIndex spatial, IInteriorIndex interiors, IFidelityModel fidelity) : IWorldModel
{
    public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; private set; } =
        new ReadOnlyDictionary<FeatureId, WorldFeature>(new Dictionary<FeatureId, WorldFeature>());
    public WorldNetwork Network { get; private set; } = WorldNetwork.Empty;
    public ISpatialIndex Spatial { get; } = spatial;
    public IInteriorIndex Interiors { get; } = interiors;
    public IOutskirtsModel? Outskirts => null;
    public IFidelityModel? Fidelity { get; } = fidelity;

    /// <summary>Allows D0 fixture data to be supplied without embedding a map or feature ID.</summary>
    public void Configure(IEnumerable<WorldFeature> features, WorldNetwork? network = null)
    {
        ArgumentNullException.ThrowIfNull(features);
        var snapshot = new ReadOnlyDictionary<FeatureId, WorldFeature>(
            features.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal)
                .ToDictionary(feature => feature.Id));
        Features = snapshot;
        Network = network ?? WorldNetwork.Empty;
        if (Spatial is StubSpatialIndex stubSpatial) stubSpatial.Configure(snapshot);
        if (Fidelity is StubFidelityModel stubFidelity) stubFidelity.Configure(snapshot);
    }
}

[KernelService(typeof(ISpatialIndex), Owner = "WP-00", IsStub = true)]
public sealed class StubSpatialIndex : ISpatialIndex
{
    private IReadOnlyList<WorldFeature> _features = [];
    public void Configure(IReadOnlyDictionary<FeatureId, WorldFeature> features) =>
        _features = features.Values.OrderBy(feature => feature.Id.Value, StringComparer.Ordinal).ToArray();
    public IReadOnlyList<FeatureId> Query(Box3 box) =>
        _features.Where(feature => feature.Bounds.Intersects(box)).Select(feature => feature.Id).ToArray();
    public float GroundY(float x, float z) => 0f;
    public CameraMedium MediumAt(Vector3 p) => new(MediumKind.Air);
    public float? SupportY(float x, float z, float yHint) => 0f;
    public WalkClass WalkClassAt(float x, float z) => WalkClass.Walk;
    public Vector3? NearestSafe(Vector3 p, float maxDist)
    {
        if (!float.IsFinite(maxDist) || maxDist < 0f) throw new ArgumentOutOfRangeException(nameof(maxDist));
        if (!float.IsFinite(p.X) || !float.IsFinite(p.Y) || !float.IsFinite(p.Z)) return null;
        return MathF.Abs(p.Y) <= maxDist ? new Vector3(p.X, 0f, p.Z) : null;
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
        if (!_features.TryGetValue(building, out var feature) || feature.Geometry is not PolygonGeometry polygon)
            return [];
        var segments = new List<FacadeSegment>();
        Append(polygon.Rings.Outer);
        foreach (var hole in polygon.Rings.Holes) Append(hole);
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
