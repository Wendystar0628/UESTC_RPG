using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using GdUnit4;

namespace Campus.Gen.Walkable.Tests;

[TestSuite]
public sealed class WalkableBakerTests
{
    private static readonly MapRect Rect = new(new Vector2(-8f), new Vector2(40f));
    private static readonly PolygonRings Boundary = Rectangle(0, 0, 32, 32);
    private static readonly string InputHash = new('a', 64);

    [TestCase]
    public void RealGeometryOverridesRemainDeterministicAndDoNotOpenClosedBuildings()
    {
        WorldFeature[] features =
        [
            Feature("b", "building", Rectangle(4, 4, 12, 12)),
            Feature("water", "water", new PolygonRings(Rectangle(18, 2, 30, 22).Outer, [Rectangle(22, 8, 26, 12).Outer])),
            Feature("main", "main", Rectangle(2, 2, 14, 14)),
            Feature("door", "door", Rectangle(4, 6, 8, 10)),
            Feature("wade", "wade", Rectangle(18, 14, 22, 18)),
            Feature("forbid", "viewOnly", Rectangle(2, 22, 12, 30)),
            new WorldFeature(new FeatureId("multi"), FeatureSource.Design, new DesignKind("building"),
                new MultiPolygonGeometry([Rectangle(14, 26, 18, 30), Rectangle(24, 26, 28, 30)]))
        ];
        var result = Bake(features);
        Require(At(result, -1, 1) == WalkClass.Outside, "Exterior must remain outside.");
        Require(At(result, 5, 5) == WalkClass.Building, "A main corridor must not open a closed building.");
        Require(At(result, 5, 7) == WalkClass.Interior, "A registered opening must connect the interior.");
        Require(At(result, 19, 15) == WalkClass.Wade, "Registered shallow water must remain wadeable.");
        Require(At(result, 23, 9) == WalkClass.Forbidden, "An unregistered lake island is not a safe landing.");
        Require(At(result, 25, 27) == WalkClass.Building, "All multipolygon parts must block walking.");
        Require(At(result, 5, 25) == WalkClass.Forbidden, "View-only areas must block walking.");
        var reversed = Bake(features.Reverse().ToArray());
        Require(JsonSerializer.Serialize(result, ContractJson.Options) == JsonSerializer.Serialize(reversed, ContractJson.Options),
            "Feature dictionary insertion order must not change the bake.");
    }

    [TestCase]
    public void SafePointsAreDryHaveBoundaryClearanceAndNeverInventAnOriginFallback()
    {
        var water = Feature("lake", "water", Rectangle(10, 0, 22, 32));
        var product = Bake([water]);
        for (var i = 0; i < product.SafePoints.Coordinates.Length; i += 3)
        {
            var coordinates = product.SafePoints.Coordinates;
            if (coordinates[i] == short.MinValue) continue;
            var point = product.SafePoints.QuantizationOrigin + new Vector3(coordinates[i], coordinates[i + 1], coordinates[i + 2]) * .25f;
            Require(point.X >= 1f && point.X <= 31f && point.Z >= 1f && point.Z <= 31f, "Safe point is too close to the campus boundary.");
            Require(point.X < 9.4f || point.X > 22.6f, "Safe point is too close to water.");
            Require(At(product, point.X, point.Z) == WalkClass.Walk, "Safe point must be dry walkable ground.");
        }
        var none = Bake([Feature("water", "water", Boundary)]);
        Require(none.SafePoints.Coordinates.All(value => value == short.MinValue), "No-safe maps must retain invalid sentinels.");
        var slope = Bake([], new PlaneSpatial((x, _) => x * .5f));
        Require(slope.SafePoints.Coordinates.All(value => value == short.MinValue), "Ground steeper than 20 degrees must not seed safety.");
    }

    [TestCase]
    public void CampusPerimeterIsNotAnInvisibleFenceAndWadeOuterEdgeStaysOpen()
    {
        Require(Bake([]).Walls.Count == 0, "Campus boundary collision belongs to visible fences.");
        var gate = new WorldFeature(new FeatureId("walk:gate"), FeatureSource.Design, new DesignKind("boundary"),
            new PolylineGeometry(new Polyline([new Vector2(8, 0), new Vector2(16, 0)])));
        var result = Bake([gate, Feature("lake", "water", Rectangle(16, 4, 28, 28)), Feature("wade", "wade", Rectangle(12, 12, 20, 20))]);
        Require(result.Walls.Count(wall => wall.Tag == SurfaceTag.Boundary) == 1, "Only a registered gate produces Boundary walls.");
        Require(result.Walls.All(wall => wall.Foot != Campus.Contracts.Audio.FootMat.Auto), "Walls must carry a resolved contact material.");
        Require(!result.Walls.Any(wall => wall.Tag == SurfaceTag.Invisible && wall.A.X == 20f && wall.B.X == 20f &&
            Math.Min(wall.A.Y, wall.B.Y) < 20f && Math.Max(wall.A.Y, wall.B.Y) > 12f), "Wade-to-water edge must remain open.");
        var i = 4 * result.Walk.Width + 4;
        Require((result.Walk.Flags[i] & 1) != 0, "The 3 m campus buffer must be marked.");
    }

    [TestCase]
    public void CollisionModuleClipsWallsAcrossTileBoundariesWithoutGaps()
    {
        var wall = new WallCollider(new Vector2(-10f, 1f), new Vector2(130f, 1f), .05f, -.25f, 2.2f, SurfaceTag.Invisible)
        { Foot = Campus.Contracts.Audio.FootMat.Concrete };
        var product = Bake([]) with { Walls = [wall] };
        var world = new TestWorld([], new PlaneSpatial((_, _) => 0f)) { Walk = product };
        var context = new BuildContext { World = world, Map = null!, WorldSeed = 1 };
        var module = new WalkableModule();
        var tiles = module.Tiles(context).ToArray();
        Require(tiles.Length == 4, "A crossing wall must be available in every touched collision tile.");
        var pieces = tiles.SelectMany(tile => module.Collide(context, tile)).Cast<WallCollider>().ToArray();
        Require(pieces.Length == 4 && pieces.Sum(piece => piece.B.X - piece.A.X) == 140f, "Tile clipping must preserve the complete wall exactly once.");
        Require(pieces.All(piece => piece.Tag == wall.Tag && piece.Foot == wall.Foot), "Clipping must preserve collision semantics.");
    }

    [TestCase]
    public void MixedPoiEntitiesClassifyTheirBuildingPartsWithoutBlockingTheParkEnvelope()
    {
        var mixed = Feature("mixed-park", "poi", Boundary) with
        {
            Parts =
            [
                new WorldFeaturePart("building-b", new PolygonGeometry(Rectangle(22, 4, 28, 10)), 9f, 3)
                    { Props = JsonSerializer.SerializeToElement(new { kind = "building" }) },
                new WorldFeaturePart("park", new PolygonGeometry(Rectangle(12, 12, 22, 22)), null, null)
                    { Props = JsonSerializer.SerializeToElement(new { kind = "green" }) },
                new WorldFeaturePart("building-a", new PolygonGeometry(Rectangle(4, 4, 10, 10)), 6f, 2)
                    { Props = JsonSerializer.SerializeToElement(new { kind = "building" }) }
            ]
        };
        var product = Bake([mixed]);
        Require(At(product, 5, 5) == WalkClass.Building && At(product, 23, 5) == WalkClass.Building,
            "Building parts under a non-building entity must block walking.");
        Require(At(product, 15, 15) == WalkClass.Walk && At(product, 29, 29) == WalkClass.Walk,
            "A mixed entity's park and aggregate envelope must not become a building.");
        var reversed = Bake([mixed with { Parts = mixed.Parts.Reverse().ToArray() }]);
        Require(JsonSerializer.Serialize(product, ContractJson.Options) == JsonSerializer.Serialize(reversed, ContractJson.Options),
            "Source part order must not change the bake.");
    }

    [TestCase]
    public void BridgeOccupancyOpensItsFullDeckKeepsHolesAndUsesCollisionTops()
    {
        var bridge = Feature("deck", "bridge-deck", Rectangle(0, 6, 32, 26)) with
        { Props = JsonSerializer.SerializeToElement(new { axis = "legacy-bridge" }) };
        var world = new TestWorld(
        [
            Feature("lake", "water", Boundary), bridge,
            Feature("closed", "building", Rectangle(4, 6, 8, 10)),
            Feature("view", "viewOnly", Rectangle(20, 6, 24, 10)),
            Feature("seal", "seal", Rectangle(24, 10, 28, 14))
        ], new PlaneSpatial((_, _) => -5f))
        {
            Network = WorldNetwork.Empty with
            { Bridges = [new(new FeatureId("legacy-bridge"), new Polyline([new(0, 12), new(32, 12)]), 6f)] }
        };
        var footprint = new Footprint2D(bridge.Id,
            new PolygonRings(Rectangle(-4, 6, 36, 26).Outer, [Rectangle(12, 10, 16, 14).Outer]), -10f, 99f);
        var ramp = new RampCollider(new(-4, 2.5f, 6), new(36, 2.5f, 6), new(36, 2.5f, 26), new(-4, 2.5f, 26), .2f, SurfaceTag.Ground);
        var unrelated = new Footprint2D(new("unregistered"), Rectangle(26, 28, 32, 32), 0f, 100f);
        var product = WalkableBaker.Build(world, new("example"), Rect, Boundary, InputHash, [footprint, unrelated], [ramp]);
        Require(At(product, 3, 19) == WalkClass.Walk, "The deck outside the old centreline must open.");
        Require(At(product, 13, 11) == WalkClass.Water, "An old bridge line must not fill a deck hole.");
        Require(At(product, 33, 19) == WalkClass.Outside, "A bridge must not open the campus exterior.");
        Require(At(product, 5, 7) == WalkClass.Building, "Bridge occupancy must not open a closed building.");
        Require(At(product, 21, 7) == WalkClass.Forbidden && At(product, 25, 11) == WalkClass.Forbidden,
            "View-only areas and seals take precedence over bridge occupancy.");
        Require(At(product, 29, 29) == WalkClass.Water, "Unmatched occupancy must not grant walking.");
        var index = (19 + 8) / 2 * product.Walk.Width + (3 + 8) / 2;
        Require(product.Walk.SupportY[index] == 2.5f, "Deck support must use the collision top, not ground or occupancy bounds.");
        var repeated = WalkableBaker.Build(world, new("example"), Rect, Boundary, InputHash,
            [unrelated, footprint with { MinY = -20f, MaxY = 200f }, footprint], [ramp, ramp]);
        Require(JsonSerializer.Serialize(product, ContractJson.Options) == JsonSerializer.Serialize(repeated, ContractJson.Options),
            "Repeated LOD footprints and collider order must not change the product.");
    }

    [TestCase]
    public void BridgeOccupancyWithoutAnActualCollisionSupportIsRejected()
    {
        var bridge = Feature("deck", "bridge-deck", Rectangle(4, 4, 28, 28));
        var world = new TestWorld([bridge], new PlaneSpatial((_, _) => 0f));
        try
        {
            WalkableBaker.Build(world, new("example"), Rect, Boundary, InputHash,
                [new Footprint2D(bridge.Id, Rectangle(4, 4, 28, 28), 0f, 10f)], []);
        }
        catch (InvalidOperationException error) when (error.Message.Contains("collision-top support", StringComparison.Ordinal))
        {
            return;
        }
        throw new InvalidOperationException("Missing bridge collision tops must not become invented ground support.");
    }

    private static WalkBakeData Bake(IReadOnlyList<WorldFeature> features, ISpatialIndex? spatial = null) =>
        WalkableBaker.Build(new TestWorld(features, spatial ?? new PlaneSpatial((_, _) => 0f)), new MapId("example"), Rect, Boundary, InputHash);
    private static WorldFeature Feature(string id, string kind, PolygonRings polygon) =>
        new(new FeatureId(id), FeatureSource.Design, new DesignKind(kind), new PolygonGeometry(polygon));
    private static PolygonRings Rectangle(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);
    private static WalkClass At(WalkBakeData data, float x, float z) => (WalkClass)data.Walk.Classes[
        (int)MathF.Floor((z - data.Walk.Origin.Y) / 2f) * data.Walk.Width + (int)MathF.Floor((x - data.Walk.Origin.X) / 2f)];
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TestWorld(IReadOnlyList<WorldFeature> features, ISpatialIndex spatial) : IWorldModel
    {
        public PolygonRings Boundary => WalkableBakerTests.Boundary;
        public IReadOnlyDictionary<FeatureId, WorldFeature> Features { get; } = features.ToDictionary(feature => feature.Id);
        public WorldNetwork Network { get; init; } = WorldNetwork.Empty;
        public ISpatialIndex Spatial => spatial;
        public IInteriorIndex Interiors => throw new NotSupportedException();
        public IOutskirtsModel? Outskirts => null;
        public IFidelityModel? Fidelity => null;
        public WalkBakeData? Walk { get; init; }
    }

    private sealed class PlaneSpatial(Func<float, float, float> ground) : ISpatialIndex
    {
        public IReadOnlyList<FeatureId> Query(Box3 box) => [];
        public float GroundY(float x, float z) => ground(x, z);
        public CameraMedium MediumAt(Vector3 p) => throw new NotSupportedException();
        public float? SupportY(float x, float z, float yHint) => GroundY(x, z);
        public WalkClass WalkClassAt(float x, float z) => throw new NotSupportedException();
        public Vector3? NearestSafe(Vector3 p, float maxDist) => throw new NotSupportedException();
    }
}
