using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Zones.Core.Annex;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using GdUnit4;

namespace Campus.Gen.Zones.Core.Tests;

[TestSuite]
public sealed class AnnexShellTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void TheFiveNorthBlocksAreBuiltOnTheLibraryFloorLines()
    {
        var world = RealWorld();
        var shells = CoreSite.Pieces(world).OfType<AnnexShell>().ToArray();
        Require(shells.Select(s => s.Kind).Order().SequenceEqual(Enum.GetValues<AnnexKind>().Order()), $"five north blocks, got {shells.Length}");
        var library = CoreSite.Pieces(world).OfType<LibraryShell>().Single();
        foreach (var shell in shells)
        {
            Require(shell.Frame.StoreyHeights.Count == CoreStyle.AnnexStoreysOf(shell.Kind), $"{shell.Id}: storeys");
            for (var k = 0; k <= shell.Rows; k++)
                Require(MathF.Abs(shell.Frame.StoreyY(k) - library.Plan.StoreyY(k)) < 1e-3f, $"{shell.Id}: floor line {k} runs on from the library");
            Require(shell.Openings.Count > 0, $"{shell.Id}: has windows");
            foreach (var lod in Levels)
            {
                var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
                shell.Build(build, lod);
                var output = build.Finish(lod);
                Require(output.Meshes.Count > 0, $"{shell.Id} {lod}: geometry");
            }
            Require(shell.Collide().All(c => c.Foot != Campus.Contracts.Audio.FootMat.Auto), $"{shell.Id}: every collider has a Foot");
        }
        var low = shells.Single(s => s.Kind == AnnexKind.LowBlock);
        Require(MathF.Abs(low.Frame.EavesY - library.Plan.StoreyY(2)) < 1e-3f, "the low block's roof is the library's 3F floor (G terrace)");
        Require(low.Collide().OfType<SlabCollider>().Any(s => MathF.Abs(s.Y - library.Plan.StoreyY(2)) < 1e-3f), "the G terrace is a walkable slab");
        var annex = shells.Single(s => s.Kind == AnnexKind.NorthAnnex);
        var door = annex.Door ?? throw new InvalidOperationException("Expected: the north annex has its closed door");
        var c = door.Face.At((door.Hole.U0 + door.Hole.U1) * 0.5f, 0f, door.Hole.Y0);
        var o = door.Face.N;
        Console.WriteLine($"annex door: centre ({c.X:F4}, {c.Y:F2}, {c.Z:F4}) facing {Shapes.BearingOf(new(o.X, o.Z)):F1}° outside ({c.X + o.X * 2.5f:F4}, {c.Z + o.Z * 2.5f:F4}) inside ({c.X - o.X * 3f:F4}, {c.Z - o.Z * 3f:F4}) width {door.Hole.U1 - door.Hole.U0:F2}");
        Require(annex.Collide().OfType<WallCollider>().Count(w => w.Tag == SurfaceTag.DoorClosed) == 1, "the closed door's bay is a DoorClosed wall");
        var parts = Build(annex, LodLevel.L0).Parts;
        Require(parts.Count(p => p.Part.Value == PartCatalog.DoorGlass) == 1, "one closed glass door");
    }

    [TestCase]
    public void TheQuietGardenFillsTheClosedCourt()
    {
        var world = RealWorld();
        var garden = CoreSite.Pieces(world).OfType<QuietGarden>().Single();
        var area = MathF.Abs(AnnexSite.SignedArea(garden.Ring));
        Console.WriteLine($"court: {garden.Ring.Count} corners, {area:F1} m², pavilion ({garden.Pavilion.X:F2}, {garden.Pavilion.Y:F2}), planters " +
            string.Join(", ", garden.Planters.Select(p => $"({p.X:F2}, {p.Y:F2})")));
        Require(area is > 150f and < 600f, $"the court is {area:F0} m²");
        Require(garden.Colonnade is not null, "the colonnade stands along the library wall");
        foreach (var p in garden.Planters.Append(garden.Pavilion)) Require(Inside(p, garden.Ring), "pavilion and planters stand in the court");
        foreach (var lod in Levels)
        {
            var build = new CoreBuild(Campus.Contracts.Tests.PaletteFixture.Catalog) { PartLods = PartLodFixture.Catalog };
            garden.Build(build, lod);
            Require(build.Finish(lod).Meshes.Count > 0, $"garden {lod}");
        }
        // The palms WP-23 plants (overlay/core.d/) stand in the planters.
        var palms = world.Features.Values.Where(f => f.Id.Value.StartsWith("design:core-quiet-garden-palm-", StringComparison.Ordinal)).ToArray();
        Require(palms.Length == 2, $"two palms registered, got {palms.Length}");
        foreach (var palm in palms)
        {
            var at = palm.Geometry is PointGeometry point ? new Vector2(point.Position.X, point.Position.Z) : throw new InvalidOperationException("palm is a point");
            Require(garden.Planters.Any(p => Vector2.Distance(p, at) < 0.3f), $"{palm.Id.Value} stands in a planter");
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

    private static bool Inside(Vector2 p, IReadOnlyList<Vector2> ring)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
        {
            Vector2 a = ring[i], b = ring[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
        }
        return inside;
    }

    private static void Require(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException("Expected: " + what);
    }

    private static IWorldModel RealWorld()
    {
        var dir = Path.Combine(FindRepository(), "game", "data", "maps", "qingshuihe");
        var files = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var name in new[] { "campus.json", "map.json", "network.json", "interiors.json" })
            files[name] = File.ReadAllBytes(Path.Combine(dir, name));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(dir, "overlay"), "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            files["overlay/" + Path.GetRelativePath(Path.Combine(dir, "overlay"), file).Replace('\\', '/')] = File.ReadAllBytes(file);
        return WorldModelLoader.Load(new MapId("qingshuihe"), files).World;
    }

    private static string FindRepository()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ownership.json"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
