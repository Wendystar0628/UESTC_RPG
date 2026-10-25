using System.Numerics;
using Campus.Gen.Tests.Support;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Tests;

/// <summary>Shared bounded outdoor map for flight, transit and walking regressions.</summary>
internal static class FlightTestSupport
{
    internal static IMapContext CreateContext() => new TestMapContext(Descriptor(), new FakeWorld
    {
        Boundary = new(Square(100)),
        Spatial = new FlatSpatial(),
        Outskirts = new Outskirts(),
    }, BuildInputSnapshot.Empty);
    internal static Vector2[] Square(float radius) => [new(-radius, -radius), new(radius, -radius), new(radius, radius), new(-radius, radius)];
    internal static MapDescriptor Descriptor() => new(new("test"), new("测试", "Test"), MapKind.Outdoor, 1, 1,
        new(0, 0, 0, new(-100, -100), new(100, 100)), Vector2.Zero,
        new(new(-200, -28, -200), new(200, 2000, 200)), new(new(-200, -200), new(200, 200)),
        new(.9f, 30, 3000, -28, 2000, 30, 40, 150), default, default, "start", [], null!,
        new("test", "test", []), []);
    internal sealed class Outskirts : IOutskirtsModel
    {
        public IReadOnlyList<Vector2> FrontLine { get; } = Square(120);
        public IReadOnlyList<Vector2> CutLine { get; } = Square(200);
        public IReadOnlyList<OutskirtsBlock> Blocks => [];
        public float FrontDistance(float x, float z) => new Vector2(Math.Max(0, Math.Abs(x) - 120), Math.Max(0, Math.Abs(z) - 120)).Length();
        public float SoftFloor(float x, float z) => 80;
    }
}
