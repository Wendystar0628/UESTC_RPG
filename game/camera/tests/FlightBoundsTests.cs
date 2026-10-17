using System.Numerics;
using Campus.Camera;
using Campus.Contracts;
using Campus.Contracts.Maps;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class FlightBoundsTests
{
    [TestCase]
    public void ExteriorLowAltitudeAndHighRadiusAreProjectedInside()
    {
        var bounds = Create();
        foreach (var point in new[] { new Vector3(150, 20, 0), new Vector3(4000, 3000, 0), new Vector3(0, -60, 0) })
        {
            if (bounds.IsLegal(point)) throw new InvalidOperationException("Invalid flight point was accepted.");
            var projected = bounds.Project(point);
            if (!bounds.IsLegal(projected)) throw new InvalidOperationException("Projection did not reach the permitted volume.");
        }
    }

    [TestCase]
    public void UndergroundBoundaryPushesHorizontallyWithoutSurfacing()
    {
        var projected = Create().Project(new(110, -10, 0));
        if (Math.Abs(projected.Y + 10) > .001f || projected.X > 100)
            throw new InvalidOperationException("Underground boundary lifted the camera out through the street.");
    }

    [TestCase]
    public void BufferSlowsOnlyOutwardMovement()
    {
        var bounds = Create();
        var p = new Vector3(110, 20, 0);
        var outward = bounds.FilterVelocity(p, new(10, 0, 0), out float fog);
        var inward = bounds.FilterVelocity(p, new(-10, 0, 0), out _);
        var tangent = bounds.FilterVelocity(p, new(0, 0, 10), out _);
        if (outward.X >= 5 || inward.X != -10 || tangent.Z != 10 || fog <= 0)
            throw new InvalidOperationException("Flight buffer changed the tangential/inward movement or failed to slow outward movement.");
    }

    private static FlightBounds Create()
    {
        var world = new World();
        return new(new Context(world, BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>())), world.Boundary);
    }
    internal static IMapContext CreateContext() => new Context(new World(),
        BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>()));
    private static Vector2[] Square(float radius) => [new(-radius, -radius), new(radius, -radius), new(radius, radius), new(-radius, radius)];
    private sealed class Context(World world, BuildInputSnapshot sourceInputs) : IMapContext
    {
        public BuildInputSnapshot SourceInputs => sourceInputs;
        public IWorldModel World => world;
        public string DataRoot => "res://test/";
        public MapDescriptor Descriptor { get; } = new(new("test"), new("测试", "Test"), MapKind.Outdoor, 1, 1,
            new(0, 0, 0, new(-100, -100), new(100, 100)), Vector2.Zero,
            new(new(-200, -28, -200), new(200, 2000, 200)), new(new(-200, -200), new(200, 200)),
            new(.9f, 30, 3000, -28, 2000, 30, 40, 150), default, default, "start", [], null!,
            new("test", "test", []), new Dictionary<string, string>(), []);
    }
    private sealed class World : IWorldModel
    {
        public PolygonRings Boundary { get; } = new(Square(100));
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; } = new Dictionary<FeatureId, WorldFeature>();
        public WorldNetwork Network => WorldNetwork.Empty;
        public ISpatialIndex Spatial { get; } = new Spatial();
        public IInteriorIndex Interiors => null!;
        public IOutskirtsModel? Outskirts { get; } = new Outskirts();
        public IFidelityModel? Fidelity => null;
    }
    private sealed class Outskirts : IOutskirtsModel
    {
        public IReadOnlyList<Vector2> FrontLine { get; } = Square(120);
        public IReadOnlyList<Vector2> CutLine { get; } = Square(200);
        public IReadOnlyList<OutskirtsBlock> Blocks => [];
        public float FrontDistance(float x, float z) => new Vector2(Math.Max(0, Math.Abs(x) - 120), Math.Max(0, Math.Abs(z) - 120)).Length();
        public float SoftFloor(float x, float z) => 80;
    }
    private sealed class Spatial : ISpatialIndex
    {
        public IReadOnlyList<FeatureId> Query(Box3 box) => [];
        public float GroundY(float x, float z) => 0;
        public CameraMedium MediumAt(Vector3 p) => new(MediumKind.Air);
        public WalkClass WalkClassAt(float x, float z) => WalkClass.Walk;
    }
}
