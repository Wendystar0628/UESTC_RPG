using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Tests;

internal sealed class TestWorldModel : IWorldModel
{
    public PolygonRings Boundary { get; } = new([]);
    public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; } = new Dictionary<FeatureId, WorldFeature>();
    public WorldNetwork Network => WorldNetwork.Empty;
    public ISpatialIndex Spatial { get; } = new TestSpatialIndex();
    public IInteriorIndex Interiors => throw new NotSupportedException("This domain fixture does not query interiors.");
    public IOutskirtsModel? Outskirts => null;
    public IFidelityModel? Fidelity => null;
}

internal sealed class TestSpatialIndex : ISpatialIndex
{
    public IReadOnlyList<FeatureId> Query(Box3 box) => [];
    public float GroundY(float x, float z) => 0;
    public CameraMedium MediumAt(Vector3 point) => new(MediumKind.Air);
    public WalkClass WalkClassAt(float x, float z) => WalkClass.Walk;
}

internal sealed class TestCollisionWorld : ICollisionWorld
{
    public string? Error { get; set; }
    public bool Ready(Vector3 position, float radius) => Error is null;
    public SweepHit? Sweep(Vector3 from, Vector3 to, float radius, float height) => throw new NotSupportedException();
    public SupportHit? SupportY(float x, float z, float yHint) => throw new NotSupportedException();
}
