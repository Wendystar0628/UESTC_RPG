using System.Numerics;
using Campus.Camera;
using Campus.Gen.Tests.Support;
using Campus.Contracts;
using Campus.Contracts.Maps;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class FlightBoundsTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Contract)]
    public void UndergroundBoundaryPushesHorizontallyWithoutSurfacing()
    {
        var projected = Create().Project(new(110, -10, 0));
        if (Math.Abs(projected.Y + 10) > TestTolerance.Position || projected.X > 100)
            throw new InvalidOperationException("Underground boundary lifted the camera out through the street.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
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

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task PreparedProjectionCapsRawCoordinatesBeforePreparingOrReadingTerrain()
    {
        var prepared = new HashSet<Vector2>();
        var descriptor = Descriptor();
        var terrain = new ProjectionTerrain(prepared, descriptor);
        var world = new FakeWorld
        {
            Boundary = new(Square(100)), Outskirts = new Outskirts(),
            Spatial = new FlatSpatial((x, z) => prepared.Contains(new(x, z)) ? 0
                : throw new InvalidOperationException("Projection read terrain before preparing the bounded point."))
        };
        var context = new TestMapContext(descriptor, world, BuildInputSnapshot.Empty);
        var bounds = new FlightBounds(context, world.Boundary);
        foreach (var point in new[] { new Vector3(float.MaxValue, 3000, float.MaxValue), new Vector3(3000.1f, 1200, 0), new Vector3(110, -10, 0) })
        {
            var actual = await bounds.ProjectPrepared(point, terrain);
            var expected = Create().Project(point);
            if (!bounds.IsLegal(actual) || Vector3.Distance(actual, expected) > TestTolerance.Position)
                throw new InvalidOperationException("Prepared and synchronous flight projection disagree.");
        }
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task CancelledPreparedProjectionDoesNotReadTheOldWorldAfterTerrainCompletes()
    {
        using var cancellation = new CancellationTokenSource();
        var preparation = new TaskCompletionSource();
        var reads = 0;
        var descriptor = Descriptor();
        var world = new FakeWorld
        {
            Boundary = new(Square(100)), Outskirts = new Outskirts(),
            Spatial = new FlatSpatial((x, z) => { reads++; return 0; })
        };
        var context = new TestMapContext(descriptor, world, BuildInputSnapshot.Empty);
        var terrain = new ProjectionTerrain([], descriptor, preparation.Task);
        var projection = new FlightBounds(context, world.Boundary).ProjectPrepared(Vector3.Zero, terrain, cancellation.Token);
        cancellation.Cancel();
        if (projection.IsCompleted)
            throw new InvalidOperationException("Projection discarded its actual terrain consumer before completion.");
        preparation.SetResult();
        try { await projection; throw new InvalidOperationException("Cancelled projection published a late result."); }
        catch (OperationCanceledException) when (projection.IsCanceled) { }
        if (reads != 0) throw new InvalidOperationException("Cancelled projection read the unloaded world after preparation.");
    }

    private sealed class ProjectionTerrain(HashSet<Vector2> prepared, MapDescriptor descriptor, Task? preparation = null) : ITerrainPreparation
    {
        public bool IsPrepared(IWorldModel world, Vector2 point) => prepared.Contains(point);
        public Task PrepareRegion(IMapContext context, MapRect region, CancellationToken cancellation = default) =>
            PreparePoints(context, [region.Min, region.Max], cancellation);
        public async Task PreparePoints(IMapContext context, IReadOnlyList<Vector2> points, CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            if (preparation is not null) await preparation;
            foreach (var point in points)
            {
                if (!VectorMath.Finite(point) || Vector2.Distance(point, descriptor.Center) > descriptor.Roam.MaxRadius + TestTolerance.Position)
                    throw new InvalidOperationException("Projection submitted a raw out-of-radius terrain coordinate.");
                prepared.Add(point);
            }
        }
    }

    private static FlightBounds Create()
    {
        var context = CreateContext();
        return new(context, context.World.Boundary);
    }
    internal static IMapContext CreateContext() => new TestMapContext(Descriptor(), new FakeWorld
    {
        Boundary = new(Square(100)),
        Spatial = new FlatSpatial(),
        Outskirts = new Outskirts(),
    }, BuildInputSnapshot.Empty);
    private static Vector2[] Square(float radius) => [new(-radius, -radius), new(radius, -radius), new(radius, radius), new(-radius, radius)];
    private static MapDescriptor Descriptor() => new(new("test"), new("测试", "Test"), MapKind.Outdoor, 1, 1,
        new(0, 0, 0, new(-100, -100), new(100, 100)), Vector2.Zero,
        new(new(-200, -28, -200), new(200, 2000, 200)), new(new(-200, -200), new(200, 200)),
        new(.9f, 30, 3000, -28, 2000, 30, 40, 150), default, default, "start", [], null!,
        new("test", "test", []), []);
    private sealed class Outskirts : IOutskirtsModel
    {
        public IReadOnlyList<Vector2> FrontLine { get; } = Square(120);
        public IReadOnlyList<Vector2> CutLine { get; } = Square(200);
        public IReadOnlyList<OutskirtsBlock> Blocks => [];
        public float FrontDistance(float x, float z) => new Vector2(Math.Max(0, Math.Abs(x) - 120), Math.Max(0, Math.Abs(z) - 120)).Length();
        public float SoftFloor(float x, float z) => 80;
    }
}
