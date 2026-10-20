using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Tests.Support;
using Campus.Tests;
using GdUnit4;

namespace Campus.Gen.Collision.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class CollisionGeometryTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void PlatformsBecomeBoxesWithoutFillingTheCourtyard()
    {
        var slab = new SlabCollider(new Vector2[] { new(0, 0), new(4, 0), new(4, 1), new(1, 1), new(1, 4), new(0, 4) },
            3, .2f, SurfaceTag.Ground) { Foot = FootMat.Concrete };
        var boxes = CollisionGeometry.Localize([slab], new(0, 0)).Cast<BoxCollider>().ToArray();
        AssertThat(boxes.Length <= 3 && boxes.All(b => b.Tag == slab.Tag && b.Foot == slab.Foot)).OverrideFailureMessage("Platform boxes lost source semantics.").IsTrue();
        AssertThat(!boxes.Any(b => Contains(b, new(2, 3, 2)))).OverrideFailureMessage("The L-shaped platform's courtyard was filled.").IsTrue();
        AssertThat(boxes.Any(b => Contains(b, new(.5f, 2.9f, 3)))).OverrideFailureMessage("The elevated thin platform was removed.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void NegativeCoordinatesUseFloorAndLocalTransforms()
    {
        var tile = TileGrid.At(new Vector3(-0.01f, 0, -TileGrid.BaseEdge - 0.01f));
        AssertThat(tile == new TileKey(-1, -2)).OverrideFailureMessage("Negative coordinates must floor, not truncate.").IsTrue();
        var origin = TileGrid.Origin(tile);
        var wall = new WallCollider(origin, origin + new Vector2(4, 0), 0.1f, 0, 2, SurfaceTag.Barrier);
        var local = (BoxCollider)CollisionGeometry.Localize([wall], tile).Single();
        AssertThat(Contains(local, new(2, 1, 0)) && !Contains(local, new(5, 1, 0))).OverrideFailureMessage("Tile localization changed the world alignment.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void BodyBatchesRespectShapeCapAndSemantics()
    {
        var wall = new WallCollider(Vector2.Zero, Vector2.UnitX, 0.1f, 0, 2, SurfaceTag.Wall) { Foot = FootMat.Concrete };
        var batches = CollisionGeometry.BodyBatches(Enumerable.Repeat<Collider>(wall, CollisionGeometry.MaxShapesPerBody * 2 + 1).Append(wall with { Tag = SurfaceTag.Boundary }));
        AssertThat(batches.Count == 4).OverrideFailureMessage("Two full wall batches, a partial batch and a separate boundary batch are required.").IsTrue();
        AssertThat(batches.All(b => b.Count <= CollisionGeometry.MaxShapesPerBody && b.Select(c => (c.Tag, c.Foot)).Distinct().Count() == 1)).OverrideFailureMessage("Invalid collision body grouping.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void OpenEntranceCutsOnlyTheDoorwayAndClosedEntranceStaysSolid()
    {
        var ring = new PolygonRings(new Vector2[] { new(0, 0), new(10, 0), new(10, 10), new(0, 10) });
        var door = new EntranceDecl("entrance", new(5, 0, -2.5f), new(5, 0, 3), 2, true) { Center = new(5, 0, 0) };
        var entry = new InteriorIndexEntry(new("building"), [], [door], true, "provider", [1], []);
        var opened = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry);
        AssertThat(!opened.Any(w => Blocks(w, new(5, 0.5f, 0)))).OverrideFailureMessage("Open doorway is blocked.").IsTrue();
        AssertThat(opened.Any(w => Blocks(w, new(5, 4, 0)))).OverrideFailureMessage("Lintel must remain above the open doorway.").IsTrue();
        var closed = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry with { Entrances = [door with { Open = false }] });
        AssertThat(closed.Any(w => w.Tag == SurfaceTag.DoorClosed && Blocks(w, new(5, 0.5f, 0)))).OverrideFailureMessage("Closed doorway must stay solid and tagged.").IsTrue();
        var closedBuilding = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry with { Open = false });
        AssertThat(closedBuilding.Any(w => w.Tag == SurfaceTag.DoorClosed && Blocks(w, new(5, 0.5f, 0)))).OverrideFailureMessage("An open door declaration cannot open a closed building.").IsTrue();

        var raisedDoor = door with { Center = new(5, 5, 0), HeadY = 7.4f };
        var raised = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry with { Entrances = [raisedDoor] });
        AssertThat(!raised.Any(w => Blocks(w, new(5, 6.5f, 0)))).OverrideFailureMessage("An absolute raised doorway is blocked by a ground-relative lintel.").IsTrue();
        AssertThat(raised.Any(w => Blocks(w, new(5, 4.9f, 0))) && raised.Any(w => Blocks(w, new(5, 7.5f, 0)))).OverrideFailureMessage("Raised doors must retain the walls below and above their declared opening.").IsTrue();
        var unknownHead = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry with { Entrances = [raisedDoor with { HeadY = 0 }] });
        AssertThat(!unknownHead.Any(w => Blocks(w, new(5, 6.5f, 0))) && unknownHead.Any(w => Blocks(w, new(5, 9, 0)))).OverrideFailureMessage("An unknown head uses door height above its absolute threshold.").IsTrue();
        var stacked = CollisionTileBuilder.BuildingWalls(ring, new FlatSpatial(), entry with { Entrances = [door with { Open = false, HeadY = 2.4f }, raisedDoor] });
        AssertThat(stacked.Any(w => w.Tag == SurfaceTag.DoorClosed && Blocks(w, new(5, 0.5f, 0))) &&
            !stacked.Any(w => Blocks(w, new(5, 6.5f, 0)))).OverrideFailureMessage("A closed lower door must not block an open door on another floor at the same horizontal span.").IsTrue();
        AssertThat(stacked.Any(w => w.Tag == SurfaceTag.Wall && Blocks(w, new(5, 4, 0))) && stacked.Any(w => Blocks(w, new(5, 7.5f, 0)))).OverrideFailureMessage("The wall between stacked doorways and above the upper doorway must remain.").IsTrue();
        AssertThat(opened.All(w => w.HalfT <= 0.15f)).OverrideFailureMessage("Wall horizontal expansion exceeds 0.3 m.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void HeightFieldSamplesSurviveLocalizationExactly()
    {
        var tile = new TileKey(-5, 8);
        var heights = Enumerable.Range(0, 65 * 65).Select(i => (short)(i % 101)).ToArray();
        var field = new HeightFieldCollider(tile, heights, SurfaceTag.Ground);
        var local = (HeightFieldCollider)CollisionGeometry.Localize([field], tile).Single();
        AssertThat(local.Tile == tile && local.HeightsCm.SequenceEqual(heights)).OverrideFailureMessage("Height-map lattice changed.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void MixedParkParentBuildsItsBuildingPartsWithoutEnclosingTheOpenGround()
    {
        var buildingA = Rectangle(10, 10, 20, 20);
        var buildingB = Rectangle(40, 10, 50, 20);
        var parent = WorldFeatureFixture.Create("fixture:park", "poi", new PolygonGeometry(Rectangle(0, 0, 60, 40))) with
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
        AssertThat(footprints.Count == 2).OverrideFailureMessage("Mixed park parent lost building parts or generated its aggregate envelope.").IsTrue();
        AssertThat(footprints[0].SourceId == "source:a-building" && footprints[1].SourceId == "source:z-building").OverrideFailureMessage("Part order must follow SourceId.").IsTrue();
        AssertThat(footprints[0].Height == 6 && footprints[1].Height == 9).OverrideFailureMessage("Each part must retain its own height.").IsTrue();
        var walls = footprints.SelectMany(p => CollisionTileBuilder.BuildingWalls(p.Rings, new FlatSpatial(), null, p.Height)).ToArray();
        AssertThat(walls.Length == 8).OverrideFailureMessage("Two rectangular buildings must create exactly their eight perimeter walls.").IsTrue();
        AssertThat(walls.Any(w => Blocks(w, new(10, 1, 15))) && walls.Any(w => Blocks(w, new(40, 1, 15)))).OverrideFailureMessage("A building perimeter is missing.").IsTrue();
        AssertThat(!walls.Any(w => Blocks(w, new(0, 1, 20))) && !walls.Any(w => Blocks(w, new(30, 1, 15)))).OverrideFailureMessage("Open park ground or the aggregate perimeter was walled off.").IsTrue();
        AssertThat(walls.All(w => MathF.Abs(w.Y1 - WalkRules.BuildingCollisionHeight) < TestTolerance.Position)).OverrideFailureMessage("Aggregate parent height leaked into its building parts.").IsTrue();
        var joined = parent with { Parts = [Part("source:a", buildingA, "building", 6), Part("source:b", Rectangle(15, 10, 25, 20), "building", 9)] };
        var exteriorWalls = CollisionTileBuilder.BuildingFootprints(joined, exteriorOnly: true)
            .SelectMany(p => CollisionTileBuilder.BuildingWalls(p.Rings, new FlatSpatial(), null, p.Height)).ToArray();
        AssertThat(!exteriorWalls.Any(w => Blocks(w, new(15, 1, 15)) || Blocks(w, new(20, 1, 15))) &&
            exteriorWalls.Any(w => Blocks(w, new(10, 1, 15))) && exteriorWalls.Any(w => Blocks(w, new(25, 1, 15))))
            .OverrideFailureMessage("An open interior must connect overlapping building blocks while retaining its exterior walls.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void GroupedBuildingParentKeepsSeparateMultiPolygonsAndNonBuildingPartsOpen()
    {
        var parent = WorldFeatureFixture.Create("fixture:group", "building", new PolygonGeometry(Rectangle(0, 0, 80, 40))) with
        {
            Parts =
            [
                new WorldFeaturePart("source:building",new MultiPolygonGeometry([Rectangle(10,10,20,20),Rectangle(40,10,50,20)]),12,3)
                    {Props=JsonSerializer.SerializeToElement(new {kind="building"})},
                Part("source:courtyard",Rectangle(25,10,35,20),"landuse",0),
            ],
        };
        var footprints = CollisionTileBuilder.BuildingFootprints(parent);
        AssertThat(footprints.Count == 2).OverrideFailureMessage("A multi-polygon part must retain separate footprints without its courtyard or parent envelope.").IsTrue();
        var walls = footprints.SelectMany(p => CollisionTileBuilder.BuildingWalls(p.Rings, new FlatSpatial(), null, p.Height)).ToArray();
        AssertThat(!walls.Any(w => Blocks(w, new(0, 1, 20))) && !walls.Any(w => Blocks(w, new(25, 1, 15)))).OverrideFailureMessage("The aggregate boundary or non-building courtyard was walled off.").IsTrue();
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
    private static bool Contains(BoxCollider box, Vector3 point)
    {
        if (!Matrix4x4.Invert(box.At, out var inverse)) return false;
        var local = Vector3.Abs(Vector3.Transform(point, inverse));
        return local.X <= box.Half.X && local.Y <= box.Half.Y && local.Z <= box.Half.Z;
    }
    [TestCase, TestCategory(TestReason.Contract)]
    public void WallBottomCoversTheExteriorThreeMetreTerrainBand()
    {
        var walls = CollisionTileBuilder.BuildingWalls(Rectangle(0, 0, 10, 10),
            new FlatSpatial((x, z) => z <= -2 ? -3 : 0), null);
        var south = walls.Single(wall => wall.A.Y == wall.B.Y && wall.A.Y < 1);
        AssertThat(south.Y0 == -5 && south.Y1 == WalkRules.BuildingCollisionHeight).OverrideFailureMessage("Exterior low ground was missed or the visible-side wall top moved.").IsTrue();
        var corner = CollisionTileBuilder.BuildingWalls(Rectangle(0, 0, 10, 10),
            new FlatSpatial((x, z) => x <= -2 && z <= -2 ? -4 : 0), null);
        AssertThat(corner.Single(wall => wall.A.Y == wall.B.Y && wall.A.Y < 1).Y0 == -6).OverrideFailureMessage("The exterior three-metre band omitted the footprint corner.").IsTrue();
    }
}
