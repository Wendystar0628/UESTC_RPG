using System.Numerics;
using Campus.Contracts;

namespace Campus.Tests;

// Unregistered mutable world fixture; production worlds come from IWorldModelLoader.
internal sealed class MutableWorldFixture(TestInteriorIndex? interiors = null) : IWorldModel
{
    public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; private set; } = new Dictionary<FeatureId, WorldFeature>();
    public PolygonRings Boundary { get; private set; } = new([]);
    public WorldNetwork Network => WorldNetwork.Empty;
    public ITerrain Terrain => FlatTerrain.Default;
    public ISpatialIndex Spatial { get; } = new TestSpatialIndex();
    public IInteriorIndex Interiors { get; } = interiors ?? new TestInteriorIndex();
    public IOutskirtsModel? Outskirts => null;
    public IFidelityModel? Fidelity => null;
    public void Configure(IEnumerable<WorldFeature> features, PolygonRings? boundary = null)
    {
        Features = features.ToDictionary(feature => feature.Id);
        Boundary = boundary ?? new([]);
    }
}

internal sealed class TestInteriorIndex : IInteriorIndex
{
    private InteriorIndexEntry[] _entries = [];
    public void Configure(IEnumerable<InteriorIndexEntry> entries) => _entries = entries.ToArray();
    public InteriorIndexEntry? Get(FeatureId id) => _entries.SingleOrDefault(entry => entry.Feature == id);
    public IReadOnlyList<EntranceDecl> EntrancesNear(Vector3 point, float radius) => _entries
        .SelectMany(entry => entry.Entrances).Where(entry => Vector3.DistanceSquared(entry.Center, point) <= radius * radius).ToArray();
}
