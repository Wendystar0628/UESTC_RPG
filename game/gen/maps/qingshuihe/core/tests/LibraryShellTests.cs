using Campus.Gen.Geometry;
using Campus.Gen.Sites;
using Campus.Gen.Sites.Tests;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;
using Campus.Gen.Maps.Qingshuihe.Core.Library;
using GdUnit4;
using Campus.Tests;

namespace Campus.Gen.Maps.Qingshuihe.Core.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class LibraryShellTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void PlanHasEveryBlockAndASetBackOnEachWing()
    {
        var plan = Shell(Campus.Tests.RealMapFixture.Load().World).Plan;
        int Count(BlockKind kind) => plan.Blocks.Count(b => b.Kind == kind);
        AssertThat(Count(BlockKind.Core) > 0 && Count(BlockKind.Wing) > 0 && Count(BlockKind.Setback) > 0).OverrideFailureMessage("core, wings and set-back strips").IsTrue();
        AssertThat(Count(BlockKind.Tower) > 0 && Count(BlockKind.FrontTower) == 1 && Count(BlockKind.Podium) > 0).OverrideFailureMessage($"towers {Count(BlockKind.Tower)}, front tower {Count(BlockKind.FrontTower)}, podium {Count(BlockKind.Podium)}").IsTrue();
        AssertThat(plan.PadY < plan.SecondFloorY && plan.SecondFloorY < plan.TerraceY && plan.TerraceY < plan.RoofY).OverrideFailureMessage("the pad, 2F, terrace and roof rise in that order").IsTrue();
        var front = plan.Blocks.Single(b => b.Kind == BlockKind.FrontTower);
        AssertThat(front.Y1 > plan.RoofY).OverrideFailureMessage("the front tower rises above the roof").IsTrue();
        AssertThat(plan.Landing.Count > 0).OverrideFailureMessage("the stair base 8398 stands against the podium").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void ExteriorPiecesFaceOutwardAndTheFrontDoorFaceMatchesTheEntrance()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var plan = Shell(world).Plan;
        AssertThat(plan.Pieces.Count > 0).OverrideFailureMessage($"exterior pieces ({plan.Pieces.Count})").IsTrue();
        foreach (var piece in plan.Pieces)
        {
            var mid = (piece.A + piece.B) * 0.5f;
            AssertThat(!PlanRings.InsideByCrossings(piece.Block.Ring, mid + piece.Out * 0.2f) && PlanRings.InsideByCrossings(piece.Block.Ring, mid - piece.Out * 0.2f)).OverrideFailureMessage($"{piece.Block.Kind} piece faces out").IsTrue();
            AssertThat(piece.Y1 > piece.Y0).OverrideFailureMessage("piece has height").IsTrue();
        }
        // No exterior wall is hidden inside another block of the shell.
        foreach (var piece in plan.Pieces)
        {
            var probe = (piece.A + piece.B) * 0.5f + piece.Out * 0.2f;
            var y = (piece.Y0 + piece.Y1) * 0.5f;
            var buried = plan.Blocks.FirstOrDefault(b => !ReferenceEquals(b, piece.Block) && y > b.Y0 && y < b.Y1 && PlanRings.InsideByCrossings(b.Ring, probe));
            AssertThat(buried is null).OverrideFailureMessage($"{piece.Block.Kind} piece {piece.A}-{piece.B} y {piece.Y0}-{piece.Y1} is not buried in {buried?.Kind} {buried?.Source}").IsTrue();
        }
        // The front tower's outer face carries lib.main's door: its centre is on the slot glass 0.75 m in.
        var door = plan.Pieces.Single(p => p.Block.Kind == BlockKind.FrontTower && p.Length > 8f && p.Y1 - p.Y0 > 6f);
        var centre = (door.A + door.B) * 0.5f;
        var entrance = world.Interiors.Get(CoreIds.Library)!.Entrances.Single(e => e.Open && e.Id == "lib.main");
        AssertThat(Vector2.Distance(centre, new(entrance.Center.X, entrance.Center.Z)) < door.Length * 0.5f).OverrideFailureMessage($"front face centre {centre} at the lib.main door").IsTrue();
        AssertThat(MathF.Abs(Angles.Bearing(door.Out) - entrance.YawDeg) < 3 * TestTolerance.AngleDegrees).OverrideFailureMessage($"front face bearing {Angles.Bearing(door.Out)}").IsTrue();
        AssertThat(MathF.Abs(door.Y0 - plan.SecondFloorY) < TestTolerance.Position).OverrideFailureMessage("the front face starts on the 2F terrace").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WindowGridFitsItsNumberRange()
    {
        var shell = Shell(Campus.Tests.RealMapFixture.Load().World);
        AssertThat(shell.WindowCount > 0 && shell.WindowCount <= WindowNumberingSnapshot.Capacity).OverrideFailureMessage($"the library grid ({shell.WindowCount} IDs) fits the map table").IsTrue();
        var ids = new HashSet<int>();
        foreach (var o in shell.Openings)
            foreach (var (_, _, row) in o.Rows)
                for (var c = 0; c < o.Cells; c++)
                    AssertThat(ids.Add(row * shell.Columns + o.Column + c)).OverrideFailureMessage("window IDs are unique").IsTrue();
        int Count(OpeningKind kind) => shell.Openings.Count(o => o.Kind == kind);
        AssertThat(Count(OpeningKind.Slit) == 3 * LibraryStyle.TowerSlits).OverrideFailureMessage($"seven slits on each of three towers ({Count(OpeningKind.Slit)})").IsTrue();
        AssertThat(Count(OpeningKind.Slot) == 1).OverrideFailureMessage("one glass slot on the front tower").IsTrue();
        AssertThat(Count(OpeningKind.Ribbon) > 0 && Count(OpeningKind.Square) > 0 && Count(OpeningKind.Podium) > 0).OverrideFailureMessage($"ribbons {Count(OpeningKind.Ribbon)}, squares {Count(OpeningKind.Square)}, podium {Count(OpeningKind.Podium)}").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryLevelBuildsValidGlassAndLandmarkInstances()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var palette = Campus.Contracts.Tests.PaletteFixture.Catalog;
        var shell = Shell(world);
        var numbering = CoreTestSupport.Context(world, palette).WindowNumbering;
        var allocation = numbering.Get(CoreIds.Library, LibraryShell.WindowKey);
        foreach (var lod in Levels)
        {
            var build = new CoreBuild(palette) { WindowNumbering = numbering, PartLods = PartLodFixture.Catalog };
            shell.Build(build, lod);
            var output = build.Finish(lod);
            var glass = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
            var clear = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.GlassNear).ToArray();
            AssertThat(glass.Length >= 1 && clear.Length >= 1).OverrideFailureMessage($"{lod}: window glass and clear glass bundles").IsTrue();
            foreach (var g in glass.Concat(clear))
            {
                AssertThat(g.Uv is not null && g.Uv2 is not null && g.Reserved is not null).OverrideFailureMessage("VF-B glass").IsTrue();
                var ids = ZoneTestCells.WindowIds([g]).Ids;
                AssertThat(ids.Count > 0 && ids.All(id => id >= allocation.BaseId && id < allocation.BaseId + allocation.Count))
                    .OverrideFailureMessage("window glass carries only IDs from the allocated block").IsTrue();
            }
            foreach (var m in output.Meshes)
            {
                m.ValidateVertexData();
                palette.Validate(m.Material.Spec);
                for (var t = 0; t < m.Index.Length; t += 3)
                {
                    var a = P(m, m.Index[t]);
                    var g = Vector3.Cross(P(m, m.Index[t + 1]) - a, P(m, m.Index[t + 2]) - a);
                    var n = N(m, m.Index[t]) + N(m, m.Index[t + 1]) + N(m, m.Index[t + 2]);
                    AssertThat(Vector3.Dot(g, n) <= 1e-6f).OverrideFailureMessage("front-face winding").IsTrue();
                }
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WindowDeclarationIsOneLibraryGridOfTheSharedLayout()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var shell = Shell(world);
        var layouts = new CoreModule().DescribeWindowLayouts(world, 1u);
        var windowed = layouts.Where(l => l.Grids.Count > 0).ToArray();
        AssertThat(windowed.Length > 0 && windowed.All(l => CoreSite.Claims.OfType<FeatureClaim>().Any(c => c.Feature == l.Feature && (c.Aspects & Aspects.Facade) != 0))).OverrideFailureMessage("every windowed feature is a claimed facade").IsTrue();
        AssertThat(layouts.Single(l => l.Feature == CoreIds.LibraryStair).Grids.Count == 0).OverrideFailureMessage("the claimed stair base declares an empty set").IsTrue();
        var layout = windowed.Single(l => l.Feature == CoreIds.Library);
        AssertThat(layout.Feature == CoreIds.Library && world.Features.ContainsKey(layout.Feature)).OverrideFailureMessage("the declaration targets the library entity in this world").IsTrue();
        AssertThat(CoreSite.Claims.OfType<FeatureClaim>().Any(c => c.Feature == layout.Feature && (c.Aspects & Aspects.Facade) != 0)).OverrideFailureMessage("the zone claims the library facade").IsTrue();
        AssertThat(layout.Grids.Count == 1).OverrideFailureMessage("a single grid covers every source part").IsTrue();
        var grid = layout.Grids[0];
        AssertThat(grid.Key == LibraryShell.WindowKey && grid.Columns == shell.Columns && grid.Rows == LibraryShell.Rows).OverrideFailureMessage($"library-shell {grid.Columns} × {grid.Rows}").IsTrue();
        AssertThat(shell.Cells().Count() == shell.Cells().Select(c => (c.Column, c.Row)).Distinct().Count()).OverrideFailureMessage("each logical cell is declared once").IsTrue();
        AssertThat(shell.Cells().All(c => c.Column >= 0 && c.Column < grid.Columns && c.Row >= 0 && c.Row < grid.Rows)).OverrideFailureMessage("every cell lies inside the declared grid").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void GlassAndMetadataReadTheSameAllocation()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var shell = Shell(world);
        var ctx = CoreTestSupport.Context(world);
        var numbering = ctx.WindowNumbering;
        var allocation = numbering.Get(CoreIds.Library, LibraryShell.WindowKey);
        AssertThat(numbering.Allocations.Any(other => other.BaseId < allocation.BaseId))
            .OverrideFailureMessage("the production prepass places other map windows before the library").IsTrue();
        var module = new CoreModule();
        var output = module.Build(ctx, TileGrid.At(shell.Anchor, LodLevel.L0), LodLevel.L0);
        var glassIds = ZoneTestCells.WindowIds(output.Meshes).Ids
            .Where(id => id >= allocation.BaseId && id < allocation.BaseId + allocation.Count).ToHashSet();
        var windows = module.DescribeWindows(ctx).Windows.Where(w => w.Channel == LibraryStyle.LibraryWindowChannel && w.BaseId == allocation.BaseId).ToArray();
        var metaIds = new SortedSet<int>(windows.Select(w => w.Id));
        AssertThat(metaIds.Count == windows.Length).OverrideFailureMessage("one declaration per window").IsTrue();
        AssertThat(windows.All(w => w.BaseId == allocation.BaseId && w.TotalColumns == shell.Columns && w.Channel == LibraryStyle.LibraryWindowChannel)).OverrideFailureMessage("declarations carry the allocation").IsTrue();
        AssertThat(metaIds.Min >= allocation.BaseId && metaIds.Max < allocation.BaseId + allocation.Count).OverrideFailureMessage("IDs stay inside the library block").IsTrue();
        AssertThat(glassIds.SetEquals(metaIds)).OverrideFailureMessage($"decoded glass IDs ({glassIds.Count}) equal the declared windows ({metaIds.Count})").IsTrue();
        foreach (var w in windows)
            AssertThat(Vector3.Distance(Vector3.Normalize(Vector3.Cross(w.Right, w.Up)), Vector3.UnitY) > 1e-3f || w.Width > 30f).OverrideFailureMessage("facade windows face sideways").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WithoutNumberingTheGlassCarriesNoWindowIds()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var shell = Shell(world);
        var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
        shell.Build(build, LodLevel.L0);
        var glass = build.Finish(LodLevel.L0).Meshes.Where(m => m.Material.Spec.Kind is ToonKind.Glass or ToonKind.GlassNear).ToArray();
        AssertThat(glass.Length > 0 && glass.All(g => g.Uv is null && g.Uv2 is null)).OverrideFailureMessage("no base is invented without a snapshot").IsTrue();
        var numbering = CoreTestSupport.Context(world).WindowNumbering;
        var allocation = numbering.Get(CoreIds.Library, LibraryShell.WindowKey);
        var wrong = new WindowNumberingSnapshot(numbering.Map, numbering.Fingerprint,
            [allocation with { Columns = allocation.Columns + 1 }]);
        Exception? wrongAllocation = null;
        try { shell.Build(new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { WindowNumbering = wrong }, LodLevel.L0); }
        catch (InvalidOperationException ex) { wrongAllocation = ex; }
        catch (KeyNotFoundException ex) { wrongAllocation = ex; }
        AssertThat(wrongAllocation is InvalidOperationException or KeyNotFoundException).OverrideFailureMessage("an allocation of another size is refused").IsTrue();
        var ctx = new BuildContext { CodeHash = "fixture", WindowNumbering = new(new MapId("fixture"), new string('0', 64), []), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = null!, WorldSeed = 1u };
        Exception? missingSnapshot = null;
        try { new CoreModule().DescribeWindows(ctx); }
        catch (InvalidOperationException ex) { missingSnapshot = ex; }
        catch (KeyNotFoundException ex) { missingSnapshot = ex; }
        AssertThat(missingSnapshot is InvalidOperationException or KeyNotFoundException).OverrideFailureMessage("metadata needs the snapshot").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void CollidersCoverTheShellAndLeaveTheStairLandingLow()
    {
        var shell = Shell(Campus.Tests.RealMapFixture.Load().World);
        var colliders = shell.Collide();
        AssertThat(colliders.All(c => c.Foot != FootMat.Auto)).OverrideFailureMessage("every collider has a foot material").IsTrue();
        var plan = shell.Plan;
        AssertThat(colliders.OfType<SlabCollider>().Count(s => s.Ring.Count > 4) == plan.Blocks.Count(b => b.Kind == BlockKind.Podium)).OverrideFailureMessage("the podium terrace is a slab").IsTrue();
        AssertThat(colliders.OfType<WallCollider>().Any(w => MathF.Abs(w.Y1 - plan.SecondFloorY) < TestTolerance.Position)).OverrideFailureMessage("low wall under the stair landing").IsTrue();
        AssertThat(colliders.OfType<WallCollider>().Any(w => w.Y1 > plan.PadY + 200f)).OverrideFailureMessage("walls above the terrace run to 300 m").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void BothDoorsLeaveAClearWayThroughTheirBays()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var shell = Shell(world);
        var plan = shell.Plan;
        var cafe = shell.CafeDoor ?? throw new InvalidOperationException("Expected: the 1F cafe door");
        // lib.cafe: on the 1F floor of the podium face that starts at the 品·悦吧 store front, two open leaves clear 1.1 m.
        AssertThat(MathF.Abs(cafe.Origin.Y - plan.FloorY) < TestTolerance.Position && plan.CafeFace is { Piece.Block.Kind: BlockKind.Podium }).OverrideFailureMessage("the cafe door is in the 1F podium").IsTrue();
        var store = FeatureShapes.Contour(world.Features[CoreIds.CafeStore]);
        AssertThat(store.Min(p => Vector2.Distance(p, new(cafe.Origin.X, cafe.Origin.Z))) < 6f).OverrideFailureMessage("the cafe door is beside the store front").IsTrue();
        AssertThat(cafe.Width / EntranceStyle.DoorLeaves * 2 >= 1.1f && cafe.Height >= 2.4f).OverrideFailureMessage($"cafe door {cafe.Width} × {cafe.Height}").IsTrue();
        var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
        shell.Build(build, LodLevel.L0);
        var output = build.Finish(LodLevel.L0);
        AssertThat(output.Parts.Count(p => p.Part.Value == PartCatalog.DoorGlass) == 1).OverrideFailureMessage("the shell places the cafe's glass door").IsTrue();
        var colliders = shell.Collide();
        foreach (var bay in new[] { shell.EntranceDoor!.Value, cafe })
        {
            var o = new Vector2(bay.Origin.X, bay.Origin.Z);
            var n = new Vector2(bay.Outward.X, bay.Outward.Z);
            var across = new Vector2(-n.Y, n.X);
            // No wall between 1.5 m outside and 1.5 m inside on the axis and ±0.45 m off it, between knee and head height.
            foreach (var k in new[] { -0.45f, 0f, 0.45f })
            {
                Vector2 a = o + across * k + n * 1.5f, b = o + across * k - n * 1.5f;
                foreach (var w in colliders.OfType<WallCollider>().Where(w => w.Y0 < bay.Origin.Y + 1.8f && w.Y1 > bay.Origin.Y + 0.3f))
                    AssertThat(SegmentDistance(a, b, w.A, w.B) > w.HalfT).OverrideFailureMessage($"the way through the door at {bay.Origin} crosses a wall {w.A}–{w.B}").IsTrue();
            }
            AssertThat(colliders.OfType<SlabCollider>().Any(s => MathF.Abs(s.Y - bay.Origin.Y) < TestTolerance.Position && PlanRings.InsideByCrossings(s.Ring, o + n * 0.01f))).OverrideFailureMessage("the recess has a floor at the door's level").IsTrue();
            // The face line is closed beside the bay: a wall 1 m to either side of the door's outer leaves.
            foreach (var side in new[] { -1f, 1f })
            {
                var p = o + across * side * (bay.Width * 0.5f + 0.2f);
                AssertThat(colliders.OfType<WallCollider>().Any(w => SegmentDistance(p + n * 0.3f, p - n * 0.3f, w.A, w.B) <= w.HalfT)).OverrideFailureMessage($"the fixed glass beside the door at {bay.Origin} is closed").IsTrue();
            }
        }
    }

    private static float SegmentDistance(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        static float Cross(Vector2 u, Vector2 v) => u.X * v.Y - u.Y * v.X;
        static float PointSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Math.Clamp(Vector2.Dot(p - a, ab) / MathF.Max(ab.LengthSquared(), 1e-12f), 0f, 1f);
            return Vector2.Distance(p, a + ab * t);
        }
        float d1 = Cross(b - a, c - a), d2 = Cross(b - a, d - a), d3 = Cross(d - c, a - c), d4 = Cross(d - c, b - c);
        if (((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0))) return 0f;
        return MathF.Min(MathF.Min(PointSegment(a, c, d), PointSegment(b, c, d)), MathF.Min(PointSegment(c, a, b), PointSegment(d, a, b)));
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void SkylightSpansTheAtrium()
    {
        var plan = Shell(Campus.Tests.RealMapFixture.Load().World).Plan;
        var across = 2 * plan.SkylightRadius * MathF.Cos(MathF.Tau / 16f);
        AssertThat(MathF.Abs(across - LibraryStyle.SkylightAcross) < TestTolerance.Position).OverrideFailureMessage($"skylight across flats {across}").IsTrue();
        AssertThat(LibraryRoof.SkylightApexY(plan) > plan.RoofY + 3f).OverrideFailureMessage("the cone rises above the curb").IsTrue();
        var north = LibraryRoof.FaceDirection(plan, LibraryRoof.NorthFace(plan));
        AssertThat(north.Y < -0.9f).OverrideFailureMessage($"mechanical box on the north axis {north}").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void CafeApronJoinsTheNearestFootwayWithAPavedLink()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var shell = Shell(world);
        var walk = shell.CafeWalk ?? throw new InvalidOperationException("Expected: a paved link from the cafe apron");
        var cafe = shell.CafeDoor ?? throw new InvalidOperationException("Expected: the cafe door");
        var face = cafe.Face;
        float S(Vector2 p) => Vector2.Dot(p - face.A, face.Out);
        AssertThat(S(walk.A) > 0f && S(walk.A) < LibraryStyle.CafeApronDepth).OverrideFailureMessage($"the link starts inside the apron ({S(walk.A):F2} m out)").IsTrue();
        AssertThat(S(walk.B) > S(walk.A) && Vector2.Distance(walk.A, walk.B) is > 0.5f and < LibraryStyle.CafeWalkReach).OverrideFailureMessage($"the link runs out {Vector2.Distance(walk.A, walk.B):F2} m").IsTrue();
        var walks = world.Network.Roads.Where(r => r.Props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty("highway", out var h)
            && h.GetString() is "footway" or "path" or "pedestrian").ToArray();
        var reach = walks.Min(r => Enumerable.Range(1, r.Line.Points.Count - 1).Min(k => SegmentDistance(walk.B, walk.B, r.Line.Points[k - 1], r.Line.Points[k])) - (r.Width > 0f ? r.Width : 2f) * 0.5f);
        AssertThat(reach < 0f).OverrideFailureMessage($"the link ends on a footway ({reach:F2})").IsTrue();
        var slab = shell.Collide().OfType<SlabCollider>().Where(c => c.Ring.Count == 4 && PlanRings.InsideByCrossings(c.Ring, (walk.A + walk.B) * 0.5f)).ToArray();
        AssertThat(slab.Length >= 1 && slab.All(c => c.Foot == FootMat.StonePaving)).OverrideFailureMessage("the link is walkable stone paving").IsTrue();
        var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
        shell.Build(build, LodLevel.L0);
        var output = build.Finish(LodLevel.L0);
        AssertThat(output.Meshes.Any(m => m.ColorIndices?.Contains(Campus.Contracts.Tests.PaletteFixture.Catalog.Resolve(new(LibraryStyle.CafeWalkPaving + ".lit")).Value) == true)).OverrideFailureMessage("pavers on the link").IsTrue();
    }

    // ---------------------------------------------------------------- fixtures

    private static LibraryShell Shell(IWorldModel world) =>
        CoreSite.Pieces(world).OfType<LibraryShell>().SingleOrDefault() ?? throw new InvalidOperationException("Expected: library shell piece");

    private static Vector3 P(GeometryBundle b, int i) => new(b.Position[3 * i], b.Position[3 * i + 1], b.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle b, int i) => new(b.Normal[3 * i], b.Normal[3 * i + 1], b.Normal[3 * i + 2]);

}
