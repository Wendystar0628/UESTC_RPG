using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.Core.Framework;
using Campus.Gen.Maps.Qingshuihe.Core.Library;
using Campus.Gen.Maps.Qingshuihe.Core.Museum;
using GdUnit4;
using Campus.Tests;

namespace Campus.Gen.Maps.Qingshuihe.Core.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class MuseumShellTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void RingsKeepTheHallBodyShapeAndTheColonnadeWidth()
    {
        var museum = Museum(Campus.Tests.RealMapFixture.Load().World);
        AssertThat(museum.Gallery.Count == 2 * museum.Body.Count && museum.Hall.Count == museum.Gallery.Count && museum.Eave.Count == museum.Gallery.Count && museum.Platform.Count == museum.Gallery.Count).OverrideFailureMessage("a body and four chamfered rings, two vertices per body corner").IsTrue();
        AssertThat(new[] { museum.South, museum.West, museum.North, museum.East }.Distinct().Count() == 4).OverrideFailureMessage("four distinct long faces").IsTrue();
        var south = MuseumGeometry.Normal(museum.Body, museum.South);
        var west = MuseumGeometry.Normal(museum.Body, museum.West);
        AssertThat(south.Y > 0.95f && west.X < -0.95f).OverrideFailureMessage($"south {south} faces +z, west {west} faces −x").IsTrue();
        foreach (var p in museum.Hall) AssertThat(PlanRings.InsideByCrossings(museum.Gallery, p)).OverrideFailureMessage("the hall stands inside the gallery").IsTrue();
        foreach (var p in museum.Gallery) AssertThat(PlanRings.InsideByCrossings(museum.Eave, p)).OverrideFailureMessage("the eave is outside the gallery walls").IsTrue();
        foreach (var p in museum.Eave) AssertThat(PlanRings.InsideByCrossings(museum.Platform, p)).OverrideFailureMessage("the eave is inside the platform edge").IsTrue();
        for (var i = 0; i < museum.Body.Count; i++)
        {
            var gallery = museum.GalleryFace(i);
            var platform = MuseumGeometry.FaceOf(museum.Platform, 2 * i + 1);
            var width = Vector2.Dot(platform.A - gallery.A, gallery.Out);
            AssertThat(width > 0f).OverrideFailureMessage($"colonnade ring {i} is {width:F2} m wide").IsTrue();
        }
        // The chamfered folds stay out of the outline's 40°–50° band.
        foreach (var ring in new[] { museum.Gallery, museum.Hall, museum.Eave })
            for (var k = 0; k < ring.Count; k++)
            {
                var a = MuseumGeometry.Normal(ring, k);
                var b = MuseumGeometry.Normal(ring, (k + 1) % ring.Count);
                // cos 35° = 0.819: every fold is gentler than 35°.
                AssertThat(Vector2.Dot(a, b) > 0.819f).OverrideFailureMessage($"fold between faces {k} and {k + 1} (cos {Vector2.Dot(a, b):F3})").IsTrue();
            }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void DoorsPorticoAndColumnsSitWhereTheEntrancesAre()
    {
        var museum = Museum(Campus.Tests.RealMapFixture.Load().World);
        var main = museum.MainDoor;
        var lake = museum.LakeDoor;
        AssertThat(main.Face == museum.GalleryFace(museum.South) && lake.Face == museum.GalleryFace(museum.West)).OverrideFailureMessage("main door south, side door on the lake front").IsTrue();
        var entrances = museum.Entrances().ToArray();
        AssertThat(entrances.Length == 2 && entrances.All(e => e.ClearWidth >= 1.1f)).OverrideFailureMessage("two open entrances at least 1.1 m clear").IsTrue();
        foreach (var (id, centre, outward, clear) in entrances)
        {
            var bearing = Angles.Bearing(new Vector2(outward.X, outward.Z));
            Vector3 outside = centre + outward * 2.5f, inside = centre - outward * 3.0f;
            Console.WriteLine($"{id}: door ({centre.X:F4}, {centre.Y:F2}, {centre.Z:F4}) facing {bearing:F1}° outside ({outside.X:F4}, {outside.Y:F2}, {outside.Z:F4}) inside ({inside.X:F4}, {inside.Y:F2}, {inside.Z:F4}) clear {clear:F2}");
        }
        var columns = museum.ColumnPositions().ToArray();
        AssertThat(columns.Count(c => c.Size == MuseumStyle.PorticoColumn) == 4).OverrideFailureMessage("four portico columns").IsTrue();
        AssertThat(columns.Length > 0).OverrideFailureMessage($"{columns.Length} columns round the ring").IsTrue();
        // No column stands in front of either door's clear way.
        foreach (var (_, centre, outward, clear) in entrances)
            foreach (var c in columns)
            {
                var d = c.C - new Vector2(centre.X, centre.Z);
                var o = new Vector2(outward.X, outward.Z);
                var across = MathF.Abs(d.X * o.Y - d.Y * o.X);
                AssertThat(Vector2.Dot(d, o) < 0 || across > clear * 0.5f + c.Size * 0.5f).OverrideFailureMessage("the way out of each door is clear of columns").IsTrue();
            }
        var parts = Build(museum, LodLevel.L0).Parts.Where(p => p.Part.Value == PartCatalog.DoorGlass).ToArray();
        AssertThat(parts.Length == 2 && parts.All(p => p.Params is DoorParams { OpenAngleDeg: 90f })).OverrideFailureMessage("two WP-10 glass doors fixed open").IsTrue();
        AssertThat(Build(museum, LodLevel.L2).Parts.Count == 0).OverrideFailureMessage("no door parts beyond L1").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryLevelIsValidWithNumberedGlass()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var palette = Campus.Contracts.Tests.PaletteFixture.Catalog;
        var museum = Museum(world);
        var numbering = Numbering(museum.Columns, 1);
        var allocation = numbering.Get(CoreIds.Museum, MuseumShell.WindowKey);
        AssertThat(museum.Columns > 0 && museum.Clerestory.Count > 0).OverrideFailureMessage($"{museum.Columns} grid columns, {museum.Clerestory.Count} clerestory runs").IsTrue();
        foreach (var lod in Levels)
        {
            var build = new CoreBuild(palette) { WindowNumbering = numbering, PartLods = PartLodFixture.Catalog };
            museum.Build(build, lod);
            var output = build.Finish(lod);
            var glass = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
            AssertThat(glass.Length == 1).OverrideFailureMessage($"{lod}: one window-glass bundle ({glass.Length})").IsTrue();
            for (var i = 0; i < glass[0].Uv2!.Length; i += 2)
                AssertThat(glass[0].Uv2![i] == allocation.BaseId && (int)glass[0].Uv2![i + 1] / 32 == museum.Columns).OverrideFailureMessage("UV2 carries the museum's base and columns").IsTrue();
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
        var cells = museum.Cells().ToArray();
        AssertThat(cells.Select(c => (c.Column, c.Row)).Distinct().Count() == cells.Length).OverrideFailureMessage("every window cell has its own ID").IsTrue();
        AssertThat(museum.Windows(numbering).All(w => w.Channel == MuseumStyle.MuseumWindowChannel)).OverrideFailureMessage("every window on interior.public").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void LibraryAndMuseumShareOneWindowGlassBatchInTheirL3Cell()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var palette = Campus.Contracts.Tests.PaletteFixture.Catalog;
        var pieces = CoreSite.Pieces(world);
        var library = pieces.OfType<LibraryShell>().Single();
        var museum = pieces.OfType<MuseumShell>().Single();
        var tile = TileGrid.At(museum.Anchor, LodLevel.L3);
        AssertThat(tile == TileGrid.At(library.Anchor, LodLevel.L3)).OverrideFailureMessage("library and museum share an L3 cell").IsTrue();
        var numbering = Numbering(museum.Columns, library.Columns);
        var ctx = new BuildContext { CodeHash = "fixture", World = world, Map = null!, WorldSeed = 1u, Catalog = palette ?? Campus.Contracts.Tests.PaletteFixture.Catalog, WindowNumbering = numbering, PartLods = PartLodFixture.Catalog };
        var module = new CoreModule();
        foreach (var lod in new[] { LodLevel.L3, LodLevel.L4 })
        {
            var output = module.Build(ctx, TileGrid.At(museum.Anchor, lod), lod);
            var glass = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
            AssertThat(glass.Length == 1).OverrideFailureMessage($"{lod}: one shared window-glass bundle ({glass.Length})").IsTrue();
            var bases = Enumerable.Range(0, glass[0].Uv2!.Length / 2).Select(i => glass[0].Uv2![2 * i]).Distinct().Order().ToArray();
            var here = pieces.Where(p => p is IWindowedPiece && TileGrid.At(CoreModuleTests.Anchor(p), lod) == TileGrid.At(museum.Anchor, lod)).Select(p => ((IWindowedPiece)p).WindowLayout.Feature).ToHashSet();
            AssertThat(bases.SequenceEqual(numbering.Allocations.Where(a => here.Contains(a.Feature)).Select(a => (float)a.BaseId).Order())).OverrideFailureMessage($"{lod}: the shared bundle keeps each building's window base ({string.Join(", ", bases)})").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void CollisionLeavesBothDoorsOpenAndFillsEveryFoot()
    {
        var museum = Museum(Campus.Tests.RealMapFixture.Load().World);
        var colliders = museum.Collide();
        AssertThat(colliders.All(c => c.Foot != FootMat.Auto)).OverrideFailureMessage("every collider has a foot material").IsTrue();
        AssertThat(colliders.OfType<SlabCollider>().Count(s => s.Tag == SurfaceTag.Ground) >= museum.Gallery.Count).OverrideFailureMessage("the colonnade floor collides round the ring").IsTrue();
        foreach (var (id, centre, outward, clear) in museum.Entrances())
        {
            // Walk the door's centre line from 1 m outside to 1 m inside at knee height: no wall may cross it.
            Vector2 a = new(centre.X + outward.X, centre.Z + outward.Z), b = new(centre.X - outward.X, centre.Z - outward.Z);
            var across = Vector2.Normalize(new Vector2(-outward.Z, outward.X));
            foreach (var offset in new[] { -clear * 0.5f + 0.05f, 0f, clear * 0.5f - 0.05f })
                foreach (var w in colliders.OfType<WallCollider>().Where(w => w.Y0 < centre.Y + 0.5f && w.Y1 > centre.Y + 0.5f))
                    AssertThat(SegmentDistance(a + across * offset, b + across * offset, w.A, w.B) > 0.01f).OverrideFailureMessage($"{id}: the way through the door at {offset:F2} m is clear of walls").IsTrue();
            foreach (var w in colliders.OfType<WallCollider>().Where(w => w.Y0 < centre.Y + 0.5f && w.Y1 > centre.Y + 0.5f))
                AssertThat(SegmentDistance(a, b, w.A, w.B) > 0.3f).OverrideFailureMessage($"{id}: a walker's radius fits through the door's middle").IsTrue();
        }
    }

    // ---------------------------------------------------------------- fixtures

    private static MuseumShell Museum(IWorldModel world) =>
        CoreSite.Pieces(world).OfType<MuseumShell>().SingleOrDefault() ?? throw new InvalidOperationException("Expected: museum piece");

    private static ContentOutput Build(MuseumShell museum, LodLevel lod)
    {
        var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
        museum.Build(build, lod);
        return build.Finish(lod);
    }

    /// <summary>A stand-in for WP-14's allocation: a generic block, the library and the museum in ordinal feature order.</summary>
    private static WindowNumberingSnapshot Numbering(int museumColumns, int libraryColumns)
    {
        var generic = new WindowAllocation(new FeatureId("fixture:generic-before"), "generic", 0, 101, 7);
        var library = new WindowAllocation(CoreIds.Library, LibraryShell.WindowKey, generic.Count, libraryColumns, LibraryShell.Rows);
        var museum = new WindowAllocation(CoreIds.Museum, MuseumShell.WindowKey, generic.Count + library.Count, museumColumns, MuseumShell.Rows);
        return new(Campus.Tests.RealMapFixture.Entry().Descriptor.Id, new string('a', 64), [generic, library, museum, .. AnnexShellTests.Allocations(Campus.Tests.RealMapFixture.Load().World, museum.BaseId + museum.Count)]);
    }

    private static float SegmentDistance(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        static float Point(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Math.Clamp(Vector2.Dot(p - a, ab) / MathF.Max(ab.LengthSquared(), 1e-12f), 0f, 1f);
            return Vector2.Distance(p, a + ab * t);
        }
        static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
        var r = b - a; var s = d - c;
        var den = Cross(r, s);
        if (MathF.Abs(den) > 1e-9f)
        {
            float t = Cross(c - a, s) / den, u = Cross(c - a, r) / den;
            if (t is >= 0 and <= 1 && u is >= 0 and <= 1) return 0f;
        }
        return MathF.Min(MathF.Min(Point(a, c, d), Point(b, c, d)), MathF.Min(Point(c, a, b), Point(d, a, b)));
    }

    private static Vector3 P(GeometryBundle b, int i) => new(b.Position[3 * i], b.Position[3 * i + 1], b.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle b, int i) => new(b.Normal[3 * i], b.Normal[3 * i + 1], b.Normal[3 * i + 2]);

}
