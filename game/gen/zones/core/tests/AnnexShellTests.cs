using Campus.Gen.Geometry;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Zones.Core.Annex;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using GdUnit4;
using Campus.Tests;

namespace Campus.Gen.Zones.Core.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class AnnexShellTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase, TestCategory(TestReason.Contract)]
    public void TheFiveNorthBlocksAreBuiltOnTheLibraryFloorLines()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var shells = CoreSite.Pieces(world).OfType<AnnexShell>().ToArray();
        AssertThat(shells.Select(s => s.Kind).Order().SequenceEqual(Enum.GetValues<AnnexKind>().Order())).OverrideFailureMessage($"five north blocks, got {shells.Length}").IsTrue();
        var library = CoreSite.Pieces(world).OfType<LibraryShell>().Single();
        foreach (var shell in shells)
        {
            AssertThat(shell.Frame.StoreyHeights.Count == world.Features[shell.Feature].Storeys).OverrideFailureMessage($"{shell.Id}: storeys follow the data").IsTrue();
            for (var k = 0; k <= shell.Rows; k++)
                AssertThat(MathF.Abs(shell.Frame.StoreyY(k) - library.Plan.StoreyY(k)) < TestTolerance.Position).OverrideFailureMessage($"{shell.Id}: floor line {k} runs on from the library").IsTrue();
            AssertThat(shell.Openings.Count > 0).OverrideFailureMessage($"{shell.Id}: has windows").IsTrue();
            foreach (var lod in Levels)
            {
                var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
                shell.Build(build, lod);
                var output = build.Finish(lod);
                AssertThat(output.Meshes.Count > 0).OverrideFailureMessage($"{shell.Id} {lod}: geometry").IsTrue();
            }
            AssertThat(shell.Collide().All(c => c.Foot != Campus.Contracts.Audio.FootMat.Auto)).OverrideFailureMessage($"{shell.Id}: every collider has a Foot").IsTrue();
        }
        var low = shells.Single(s => s.Kind == AnnexKind.LowBlock);
        AssertThat(MathF.Abs(low.Frame.EavesY - library.Plan.StoreyY(2)) < TestTolerance.Position).OverrideFailureMessage("the low block's roof is the library's 3F floor (G terrace)").IsTrue();
        AssertThat(low.Collide().OfType<SlabCollider>().Any(s => MathF.Abs(s.Y - library.Plan.StoreyY(2)) < TestTolerance.Position)).OverrideFailureMessage("the G terrace is a walkable slab").IsTrue();
        var annex = shells.Single(s => s.Kind == AnnexKind.NorthAnnex);
        var door = annex.Door ?? throw new InvalidOperationException("Expected: the north annex has its closed door");
        var c = door.Face.At((door.Hole.U0 + door.Hole.U1) * 0.5f, 0f, door.Hole.Y0);
        var o = door.Face.N;
        Console.WriteLine($"annex door: centre ({c.X:F4}, {c.Y:F2}, {c.Z:F4}) facing {Angles.Bearing(new(o.X, o.Z)):F1}° outside ({c.X + o.X * 2.5f:F4}, {c.Z + o.Z * 2.5f:F4}) inside ({c.X - o.X * 3f:F4}, {c.Z - o.Z * 3f:F4}) width {door.Hole.U1 - door.Hole.U0:F2}");
        AssertThat(annex.Collide().OfType<WallCollider>().Count(w => w.Tag == SurfaceTag.DoorClosed) == 1).OverrideFailureMessage("the closed door's bay is a DoorClosed wall").IsTrue();
        var parts = Build(annex, LodLevel.L0).Parts;
        AssertThat(parts.Count(p => p.Part.Value == PartCatalog.DoorGlass) == 1).OverrideFailureMessage("one closed glass door").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void TheQuietGardenFillsTheClosedCourt()
    {
        var world = Campus.Tests.RealMapFixture.Load().World;
        var garden = CoreSite.Pieces(world).OfType<QuietGarden>().Single();
        var area = MathF.Abs((float)PlanGeometry.SignedArea(garden.Ring));
        Console.WriteLine($"court: {garden.Ring.Count} corners, {area:F1} m², pavilion ({garden.Pavilion.X:F2}, {garden.Pavilion.Y:F2}), planters " +
            string.Join(", ", garden.Planters.Select(p => $"({p.X:F2}, {p.Y:F2})")));
        AssertThat(garden.Colonnade is not null).OverrideFailureMessage("the colonnade stands along the library wall").IsTrue();
        foreach (var p in garden.Planters.Append(garden.Pavilion)) AssertThat(PlanGeometry.InsideByCrossings(garden.Ring, p)).OverrideFailureMessage("pavilion and planters stand in the court").IsTrue();
        foreach (var lod in Levels)
        {
            var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
            garden.Build(build, lod);
            AssertThat(build.Finish(lod).Meshes.Count > 0).OverrideFailureMessage($"garden {lod}").IsTrue();
        }
        // The palms WP-23 plants (world/core.json) stand in the planters.
        var palms = world.Features.Values.Where(f => f.Id.Value.StartsWith(CoreIds.GardenPalmPrefix, StringComparison.Ordinal)).ToArray();
        AssertThat(palms.Length == garden.Planters.Count).OverrideFailureMessage($"two palms registered, got {palms.Length}").IsTrue();
        foreach (var palm in palms)
        {
            var at = palm.Geometry is PointGeometry point ? new Vector2(point.Position.X, point.Position.Z) : throw new InvalidOperationException("palm is a point");
            AssertThat(garden.Planters.Any(p => Vector2.Distance(p, at) < 0.3f)).OverrideFailureMessage($"{palm.Id.Value} stands in a planter").IsTrue();
        }
    }

    /// <summary>Stand-in allocations of the north blocks' grids, in feature order from <paramref name="start"/> (test numbering helpers).</summary>
    internal static IEnumerable<WindowAllocation> Allocations(IWorldModel world, int start)
    {
        foreach (var shell in CoreSite.Pieces(world).OfType<AnnexShell>().OrderBy(s => s.Feature.Value, StringComparer.Ordinal))
        {
            var allocation = new WindowAllocation(shell.Feature, AnnexShell.WindowKey, start, shell.Columns, shell.Rows);
            start += allocation.Count;
            yield return allocation;
        }
    }

    private static ContentOutput Build(ICorePiece piece, LodLevel lod)
    {
        var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
        piece.Build(build, lod);
        return build.Finish(lod);
    }



}
