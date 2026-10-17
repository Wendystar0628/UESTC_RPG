using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using GdUnit4;

namespace Campus.Gen.Collision.Tests;

[TestSuite]
public sealed class CollisionGeometryTests
{
    [TestCase]
    public void ConcaveSlabKeepsItsRecessEmpty()
    {
        var slab = new SlabCollider(new Vector2[] { new(0, 0), new(4, 0), new(4, 1), new(1, 1), new(1, 4), new(0, 4) }, 3, 0.2f, SurfaceTag.Ground);
        var triangles = CollisionGeometry.Triangulate(slab);
        Require(triangles.Count == 4, "An L-shaped six-point ring needs four triangles.");
        var area = triangles.Sum(t => MathF.Abs(Cross(t.Ring[0], t.Ring[1], t.Ring[2])) / 2);
        Require(MathF.Abs(area - 7) < 0.00001f, "Triangulation changed floor area.");
        foreach (var t in triangles) Require(!Contains(t.Ring, new(2, 2)), "A recess must not become a floor.");
    }

    [TestCase]
    public void ClockwiseAndRepeatedClosingVertexAreAccepted()
    {
        var triangles = CollisionGeometry.Triangulate(new(new Vector2[] { new(0, 0), new(0, 2), new(2, 2), new(2, 0), new(0, 0) }, 0, 0.2f, SurfaceTag.Ground));
        Require(triangles.Count == 2, "A rectangle needs two triangles.");
    }

    [TestCase]
    public void ConvexFloorStaysWholeWhileConcaveRecessIsPreserved()
    {
        var rectangle = new SlabCollider(new Vector2[] { new(20, 40), new(24, 40), new(24, 44), new(20, 44) }, 2, 0.2f, SurfaceTag.Ground);
        var whole = CollisionGeometry.PrepareSlab(rectangle);
        Require(whole.Count == 1 && whole[0].Ring.Count == 4, "A convex rectangular floor must not gain an internal diagonal.");
        var retraced = rectangle with
        {
            Ring = new Vector2[] { new(20, 40), new(22, 40), new(22, 42), new(22, 40), new(24, 40), new(24, 40), new(24, 44), new(20, 44) }
        };
        Require(CollisionGeometry.PrepareSlab(retraced).Single().Ring.SequenceEqual(rectangle.Ring),
            "Zero-area retracing and adjacent duplicates must collapse fully without changing the occupied floor.");
        var small = rectangle with { Ring = new Vector2[] { new(63.9f, 63.9f), new(63.902f, 63.9f), new(63.902f, 63.902f), new(63.9f, 63.902f) } };
        Require(CollisionGeometry.PrepareSlab(small).Single().Ring.Count == 4,
            "Translating a small valid floor near the tile edge must not cancel its area.");
        var tiny = rectangle with { Ring = new Vector2[] { new(38.999573f, 63.00049f), new(38.999023f, 63), new(39, 63) } };
        var tinyPrepared = CollisionGeometry.PrepareSlab(tiny).Single();
        Require(tinyPrepared.Ring.SequenceEqual(tiny.Ring) && CollisionGeometry.IsConvexRing(tinyPrepared.Ring),
            "A nonzero sub-square-millimetre support triangle must remain a valid convex floor.");
        var concave = rectangle with { Ring = new Vector2[] { new(0, 0), new(4, 0), new(4, 1), new(1, 1), new(1, 4), new(0, 4) } };
        var pieces = CollisionGeometry.PrepareSlab(concave);
        Require(pieces.All(p => CollisionGeometry.IsConvexRing(p.Ring)), "Floor decomposition emitted a concave shape.");
        Require(pieces.All(p => !Contains(p.Ring, new(2, 2))), "Convex preparation filled the L-shaped recess.");
    }

    [TestCase]
    public void NegativeCoordinatesUseFloorAndLocalTransforms()
    {
        var tile = CollisionGeometry.TileAt(new(-0.01f, 0, -64.01f));
        Require(tile == new TileKey(-1, -2), "Negative coordinates must floor, not truncate.");
        var wall = new WallCollider(new(-64, -128), new(-60, -128), 0.1f, 0, 2, SurfaceTag.Barrier);
        var local = (WallCollider)CollisionGeometry.Localize([wall], tile).Single();
        Require(local.A == Vector2.Zero && local.B == new Vector2(4, 0), "Tile localization changed the world alignment.");
    }

    [TestCase]
    public void BodyBatchesRespectShapeCapAndSemantics()
    {
        var wall = new WallCollider(Vector2.Zero, Vector2.UnitX, 0.1f, 0, 2, SurfaceTag.Wall) { Foot = FootMat.Concrete };
        var batches = CollisionGeometry.BodyBatches(Enumerable.Repeat<Collider>(wall, 501).Append(wall with { Tag = SurfaceTag.Boundary }));
        Require(batches.Count == 4, "501 walls plus one boundary need four bodies.");
        Require(batches.All(b => b.Count <= 250 && b.Select(c => (c.Tag, c.Foot)).Distinct().Count() == 1), "Invalid collision body grouping.");
    }

    [TestCase]
    public void OpenEntranceCutsOnlyTheDoorwayAndClosedEntranceStaysSolid()
    {
        var ring = new PolygonRings(new Vector2[] { new(0, 0), new(10, 0), new(10, 10), new(0, 10) });
        var door = new EntranceDecl("entrance", new(5, 0, -2.5f), new(5, 0, 3), 2, true) { Center = new(5, 0, 0) };
        var entry = new InteriorIndexEntry(new("building"), [], [door], true, "provider");
        var opened = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry);
        Require(!opened.Any(w => Blocks(w, new(5, 0.5f, 0))), "Open doorway is blocked.");
        Require(opened.Any(w => Blocks(w, new(5, 4, 0))), "Lintel must remain above the open doorway.");
        var closed = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry with { Entrances = [door with { Open = false }] });
        Require(closed.Any(w => w.Tag == SurfaceTag.DoorClosed && Blocks(w, new(5, 0.5f, 0))), "Closed doorway must stay solid and tagged.");
        var closedBuilding = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry with { Open = false });
        Require(closedBuilding.Any(w => w.Tag == SurfaceTag.DoorClosed && Blocks(w, new(5, 0.5f, 0))), "An open door declaration cannot open a closed building.");

        var raisedDoor = door with { Center = new(5, 5, 0), HeadY = 7.4f };
        var raised = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry with { Entrances = [raisedDoor] });
        Require(!raised.Any(w => Blocks(w, new(5, 6.5f, 0))), "An absolute raised doorway is blocked by a ground-relative lintel.");
        Require(raised.Any(w => Blocks(w, new(5, 4.9f, 0))) && raised.Any(w => Blocks(w, new(5, 7.5f, 0))), "Raised doors must retain the walls below and above their declared opening.");
        var unknownHead = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry with { Entrances = [raisedDoor with { HeadY = 0 }] });
        Require(!unknownHead.Any(w => Blocks(w, new(5, 6.5f, 0))) && unknownHead.Any(w => Blocks(w, new(5, 9, 0))), "An unknown head uses door height above its absolute threshold.");
        var stacked = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry with { Entrances = [door with { Open = false, HeadY = 2.4f }, raisedDoor] });
        Require(stacked.Any(w => w.Tag == SurfaceTag.DoorClosed && Blocks(w, new(5, 0.5f, 0))) &&
            !stacked.Any(w => Blocks(w, new(5, 6.5f, 0))), "A closed lower door must not block an open door on another floor at the same horizontal span.");
        Require(stacked.Any(w => w.Tag == SurfaceTag.Wall && Blocks(w, new(5, 4, 0))) && stacked.Any(w => Blocks(w, new(5, 7.5f, 0))),
            "The wall between stacked doorways and above the upper doorway must remain.");
        Require(opened.All(w => w.HalfT <= 0.15f), "Wall horizontal expansion exceeds 0.3 m.");
    }

    [TestCase]
    public void HeightFieldSamplesSurviveLocalizationExactly()
    {
        var tile = new TileKey(-5, 8);
        var heights = Enumerable.Range(0, 65 * 65).Select(i => (short)(i % 101)).ToArray();
        var field = new HeightFieldCollider(tile, heights, SurfaceTag.Ground);
        var local = (HeightFieldCollider)CollisionGeometry.Localize([field], tile).Single();
        Require(local.Tile == tile && local.HeightsCm.SequenceEqual(heights), "Height-map lattice changed.");
    }

    [TestCase]
    public void MixedParkParentBuildsItsBuildingPartsWithoutEnclosingTheOpenGround()
    {
        var buildingA = Rectangle(10, 10, 20, 20);
        var buildingB = Rectangle(40, 10, 50, 20);
        var parent = new WorldFeature(new("fixture:park"), FeatureSource.Osm, new("poi"), new PolygonGeometry(Rectangle(0, 0, 60, 40)))
        {
            Height = 900,
            Parts =
            [
                Part("source:z-building",buildingB,"building",9),
                Part("source:ground",Rectangle(0,0,60,40),"park",0),
                Part("source:a-building",buildingA,"building",6),
            ],
        };
        var footprints = CollisionTileBuilder.BuildingFootprints(parent);
        Require(footprints.Count == 2, "Mixed park parent lost building parts or generated its aggregate envelope.");
        Require(footprints[0].SourceId == "source:a-building" && footprints[1].SourceId == "source:z-building", "Part order must follow SourceId.");
        Require(footprints[0].Height == 6 && footprints[1].Height == 9, "Each part must retain its own height.");
        var walls = footprints.SelectMany(p => CollisionTileBuilder.BuildingWalls(p.Rings, new FlatSpatial(), null, p.Height)).ToArray();
        Require(walls.Length == 8, "Two rectangular buildings must create exactly their eight perimeter walls.");
        Require(walls.Any(w => Blocks(w, new(10, 1, 15))) && walls.Any(w => Blocks(w, new(40, 1, 15))), "A building perimeter is missing.");
        Require(!walls.Any(w => Blocks(w, new(0, 1, 20))) && !walls.Any(w => Blocks(w, new(30, 1, 15))), "Open park ground or the aggregate perimeter was walled off.");
        Require(walls.All(w => MathF.Abs(w.Y1 - 300) < 0.0001f), "Aggregate parent height leaked into its building parts.");
    }

    [TestCase]
    public void GroupedBuildingParentKeepsSeparateMultiPolygonsAndNonBuildingPartsOpen()
    {
        var parent = new WorldFeature(new("fixture:group"), FeatureSource.Osm, new("building"), new PolygonGeometry(Rectangle(0, 0, 80, 40)))
        {
            Parts =
            [
                new WorldFeaturePart("source:building",new MultiPolygonGeometry([Rectangle(10,10,20,20),Rectangle(40,10,50,20)]),12,3)
                    {Props=JsonSerializer.SerializeToElement(new {kind="building"})},
                Part("source:courtyard",Rectangle(25,10,35,20),"landuse",0),
            ],
        };
        var footprints = CollisionTileBuilder.BuildingFootprints(parent);
        Require(footprints.Count == 2, "A multi-polygon part must retain separate footprints without its courtyard or parent envelope.");
        var walls = footprints.SelectMany(p => CollisionTileBuilder.BuildingWalls(p.Rings, new FlatSpatial(), null, p.Height)).ToArray();
        Require(!walls.Any(w => Blocks(w, new(0, 1, 20))) && !walls.Any(w => Blocks(w, new(25, 1, 15))), "The aggregate boundary or non-building courtyard was walled off.");
    }

    private static PolygonRings Rectangle(float x0, float z0, float x1, float z1) => new(new Vector2[] { new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1) });
    private static WorldFeaturePart Part(string id, PolygonRings ring, string kind, float height) =>
        new(id, new PolygonGeometry(ring), height, 1) { Props = JsonSerializer.SerializeToElement(new { kind }) };

    private static bool Blocks(WallCollider w, Vector3 p)
    {
        var a = w.A; var b = w.B; var q = new Vector2(p.X, p.Z); var d = b - a;
        var t = Math.Clamp(Vector2.Dot(q - a, d) / d.LengthSquared(), 0, 1);
        return p.Y >= w.Y0 && p.Y <= w.Y1 && Vector2.Distance(a + d * t, q) <= w.HalfT + 0.00001f;
    }
    private static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
    private static bool Contains(IReadOnlyList<Vector2> t, Vector2 p) => Cross(t[0], t[1], p) >= 0 && Cross(t[1], t[2], p) >= 0 && Cross(t[2], t[0], p) >= 0;
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    [TestCase]
    public void WallBottomCoversTheExteriorThreeMetreTerrainBand()
    {
        var walls = CollisionTileBuilder.BuildingWalls(Rectangle(0, 0, 10, 10),
            new FlatSpatial((x, z) => z <= -2 ? -3 : 0), null);
        var south = walls.Single(wall => wall.A.Y == wall.B.Y && wall.A.Y < 1);
        Require(south.Y0 == -5 && south.Y1 == 300, "Exterior low ground was missed or the visible-side wall top moved.");
        var corner = CollisionTileBuilder.BuildingWalls(Rectangle(0, 0, 10, 10),
            new FlatSpatial((x, z) => x <= -2 && z <= -2 ? -4 : 0), null);
        Require(corner.Single(wall => wall.A.Y == wall.B.Y && wall.A.Y < 1).Y0 == -6,
            "The exterior three-metre band omitted the footprint corner.");
    }

    private sealed class FlatSpatial(Func<float, float, float>? height = null) : ISpatialIndex
    {
        public IReadOnlyList<FeatureId> Query(Box3 box) => [];
        public float GroundY(float x, float z) => height?.Invoke(x, z) ?? 0;
        public CameraMedium MediumAt(Vector3 p) => new(MediumKind.Air);
        public float? SupportY(float x, float z, float yHint) => 0;
        public WalkClass WalkClassAt(float x, float z) => WalkClass.Walk;
        public Vector3? NearestSafe(Vector3 p, float maxDist) => new(p.X, 0, p.Z);
    }
}
