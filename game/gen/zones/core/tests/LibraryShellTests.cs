using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.World;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using GdUnit4;

namespace Campus.Gen.Zones.Core.Tests;

[TestSuite]
public sealed class LibraryShellTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void PlanHasEveryBlockAndASetBackOnEachWing()
    {
        var plan = Shell(RealWorld()).Plan;
        int Count(BlockKind kind) => plan.Blocks.Count(b => b.Kind == kind);
        Require(Count(BlockKind.Core) == 1 && Count(BlockKind.Wing) == 4 && Count(BlockKind.Setback) == 4, "core, four wings, four set-back strips");
        Require(Count(BlockKind.Tower) == 3 && Count(BlockKind.FrontTower) == 1 && Count(BlockKind.Podium) == 2,
            $"towers {Count(BlockKind.Tower)}, front tower {Count(BlockKind.FrontTower)}, podium {Count(BlockKind.Podium)}");
        Require(MathF.Abs(plan.SecondFloorY - plan.PadY - 5.1f) < 1e-3f && MathF.Abs(plan.TerraceY - plan.PadY - 18.6f) < 1e-3f && MathF.Abs(plan.RoofY - plan.PadY - 23.1f) < 1e-3f,
            "2F 5.1, 5F 18.6, roof 23.1 above the pad (MAP-QSH-LIBRARY §0.4)");
        var front = plan.Blocks.Single(b => b.Kind == BlockKind.FrontTower);
        Require(MathF.Abs(front.Y1 - plan.PadY - 27.6f) < 1e-3f, "front tower six storeys");
        Require(plan.Landing.Count > 0, "the stair base 8398 stands against the podium");
    }

    [TestCase]
    public void ExteriorPiecesFaceOutwardAndTheFrontDoorFaceMatchesTheEntrance()
    {
        var plan = Shell(RealWorld()).Plan;
        Require(plan.Pieces.Count > 40, $"exterior pieces ({plan.Pieces.Count})");
        foreach (var piece in plan.Pieces)
        {
            var mid = (piece.A + piece.B) * 0.5f;
            Require(!Inside(mid + piece.Out * 0.2f, piece.Block.Ring) && Inside(mid - piece.Out * 0.2f, piece.Block.Ring), $"{piece.Block.Kind} piece faces out");
            Require(piece.Y1 > piece.Y0, "piece has height");
        }
        // No exterior wall is hidden inside another block of the shell.
        foreach (var piece in plan.Pieces)
        {
            var probe = (piece.A + piece.B) * 0.5f + piece.Out * 0.2f;
            var y = (piece.Y0 + piece.Y1) * 0.5f;
            var buried = plan.Blocks.FirstOrDefault(b => !ReferenceEquals(b, piece.Block) && y > b.Y0 && y < b.Y1 && Inside(probe, b.Ring));
            Require(buried is null, $"{piece.Block.Kind} piece {piece.A}-{piece.B} y {piece.Y0}-{piece.Y1} is not buried in {buried?.Kind} {buried?.Source}");
        }
        // interiors.json lib.main: door centre (−154.6, 80), facing 127°.
        var door = plan.Pieces.Single(p => p.Block.Kind == BlockKind.FrontTower && p.Length > 8f && p.Y1 - p.Y0 > 6f);
        var centre = (door.A + door.B) * 0.5f;
        Require(Vector2.Distance(centre, new(-154.6f, 80f)) < 0.6f, $"front face centre {centre} at the lib.main door");
        Require(MathF.Abs(Shapes.BearingOf(door.Out) - 127f) < 3f, $"front face bearing {Shapes.BearingOf(door.Out)}");
        Require(MathF.Abs(door.Y0 - plan.SecondFloorY) < 1e-3f, "the front face starts on the 2F terrace");
    }

    [TestCase]
    public void WindowGridFitsItsNumberRange()
    {
        var shell = Shell(RealWorld());
        Require(shell.WindowCount > 0 && shell.WindowCount <= 4096, $"the library grid ({shell.WindowCount} IDs) is a small part of the 65536-ID map table");
        var ids = new HashSet<int>();
        foreach (var o in shell.Openings)
            foreach (var (_, _, row) in o.Rows)
                for (var c = 0; c < o.Cells; c++)
                    Require(ids.Add(row * shell.Columns + o.Column + c), "window IDs are unique");
        int Count(OpeningKind kind) => shell.Openings.Count(o => o.Kind == kind);
        Require(Count(OpeningKind.Slit) == 3 * CoreStyle.TowerSlits, $"seven slits on each of three towers ({Count(OpeningKind.Slit)})");
        Require(Count(OpeningKind.Slot) == 1, "one glass slot on the front tower");
        Require(Count(OpeningKind.Ribbon) >= 24 && Count(OpeningKind.Square) >= 40 && Count(OpeningKind.Podium) >= 6,
            $"ribbons {Count(OpeningKind.Ribbon)}, squares {Count(OpeningKind.Square)}, podium {Count(OpeningKind.Podium)}");
    }

    [TestCase]
    public void EveryLevelBuildsValidGlassAndLandmarkInstances()
    {
        var world = RealWorld();
        var palette = Palette();
        var shell = Shell(world);
        var numbering = Numbering(shell, shell.Columns);
        var allocation = numbering.Get(CoreFeatures.Library, LibraryShell.WindowKey);
        foreach (var lod in Levels)
        {
            var build = new CoreBuild(palette) { WindowNumbering = numbering, PartLods = PartLodFixture.Catalog };
            shell.Build(build, lod);
            var output = build.Finish(lod);
            var glass = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
            var clear = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.GlassNear).ToArray();
            Require(glass.Length >= 1 && clear.Length >= 1, $"{lod}: window glass and clear glass bundles");
            foreach (var g in glass.Concat(clear))
            {
                Require(g.Uv is not null && g.Uv2 is not null && g.Reserved is not null, "VF-B glass");
                for (var i = 0; i < g.Uv2!.Length; i += 2)
                    Require(g.Uv2[i] == allocation.BaseId && (int)g.Uv2[i + 1] / 32 == shell.Columns, "UV2 carries the allocated base and the columns");
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
                    Require(Vector3.Dot(g, n) <= 1e-6f, "front-face winding");
                }
            }
            var triangles = output.Meshes.Sum(m => m.Index.Length / 3);
            Console.WriteLine($"library {lod}: {triangles} triangles, {output.Meshes.Count} bundles");
        }
    }

    [TestCase]
    public void WindowDeclarationIsOneLibraryGridOfTheSharedLayout()
    {
        var world = RealWorld();
        var shell = Shell(world);
        var layouts = new CoreModule().DescribeWindowLayouts(world, 1u);
        var windowed = layouts.Where(l => l.Grids.Count > 0).ToArray();
        Require(windowed.Length == 1, $"one claimed facade declares windows ({windowed.Length})");
        Require(layouts.Single(l => l.Feature == CoreFeatures.LibraryStair).Grids.Count == 0, "the claimed stair base declares an empty set");
        var layout = windowed[0];
        Require(layout.Feature == CoreFeatures.Library && world.Features.ContainsKey(layout.Feature), "the declaration targets the library entity in this world");
        Require(CoreSite.Claims.OfType<FeatureClaim>().Any(c => c.Feature == layout.Feature && (c.Aspects & Aspects.Facade) != 0), "the zone claims the library facade");
        Require(layout.Grids.Count == 1, "a single grid covers every source part");
        var grid = layout.Grids[0];
        Require(grid.Key == "library-shell" && grid.Columns == shell.Columns && grid.Rows == 6, $"library-shell {grid.Columns} × {grid.Rows}");
        Require(shell.Cells().Count() == shell.Cells().Select(c => (c.Column, c.Row)).Distinct().Count(), "each logical cell is declared once");
        Require(shell.Cells().All(c => c.Column >= 0 && c.Column < grid.Columns && c.Row >= 0 && c.Row < grid.Rows), "every cell lies inside the declared grid");
    }

    [TestCase]
    public void GlassAndMetadataReadTheSameAllocation()
    {
        var world = RealWorld();
        var shell = Shell(world);
        var numbering = Numbering(shell, shell.Columns);
        var allocation = numbering.Get(CoreFeatures.Library, LibraryShell.WindowKey);
        Require(allocation.BaseId == 707, "a generic block in front moves the library base");
        var module = new CoreModule();
        var ctx = new BuildContext { World = world, Map = null!, WorldSeed = 1u, WindowNumbering = numbering, PartLods = PartLodFixture.Catalog };
        var output = module.Build(ctx, CoreTiles.Of(shell.Anchor, LodLevel.L0), LodLevel.L0);
        var glassIds = new SortedSet<int>();
        foreach (var g in output.Meshes.Where(m => m.Material.Spec.Kind is ToonKind.Glass or ToonKind.GlassNear))
            for (var v = 0; v < g.Position.Length / 3; v++)
            {
                var columns = (int)(g.Uv2![2 * v + 1] / 32f);
                glassIds.Add((int)g.Uv2[2 * v] + (int)MathF.Floor(g.Uv![2 * v + 1]) * columns + (int)MathF.Floor(g.Uv[2 * v]));
            }
        var windows = module.DescribeWindows(ctx).Windows;
        var metaIds = new SortedSet<int>(windows.Select(w => w.Id));
        Require(metaIds.Count == windows.Count, "one declaration per window");
        Require(windows.All(w => w.BaseId == allocation.BaseId && w.TotalColumns == shell.Columns && w.Channel == CoreStyle.LibraryWindowChannel), "declarations carry the allocation");
        Require(metaIds.Min >= allocation.BaseId && metaIds.Max < allocation.BaseId + allocation.Count, "IDs stay inside the library block");
        Require(glassIds.SetEquals(metaIds), $"decoded glass IDs ({glassIds.Count}) equal the declared windows ({metaIds.Count})");
        foreach (var w in windows)
            Require(Vector3.Distance(Vector3.Normalize(Vector3.Cross(w.Right, w.Up)), Vector3.UnitY) > 1e-3f || w.Width > 30f, "facade windows face sideways");
    }

    [TestCase]
    public void WithoutNumberingTheGlassCarriesNoWindowIds()
    {
        var world = RealWorld();
        var shell = Shell(world);
        var build = new CoreBuild(null) { PartLods = PartLodFixture.Catalog };
        shell.Build(build, LodLevel.L0);
        var glass = build.Finish(LodLevel.L0).Meshes.Where(m => m.Material.Spec.Kind is ToonKind.Glass or ToonKind.GlassNear).ToArray();
        Require(glass.Length > 0 && glass.All(g => g.Uv is null && g.Uv2 is null), "no base is invented without a snapshot");
        var wrong = Numbering(shell, shell.Columns + 1);
        Require(Throws(() => shell.Build(new CoreBuild(null) { WindowNumbering = wrong }, LodLevel.L0)), "an allocation of another size is refused");
        var ctx = new BuildContext { World = world, Map = null!, WorldSeed = 1u };
        Require(Throws(() => new CoreModule().DescribeWindows(ctx)), "metadata needs the snapshot");
    }

    [TestCase]
    public void CollidersCoverTheShellAndLeaveTheStairLandingLow()
    {
        var shell = Shell(RealWorld());
        var colliders = shell.Collide();
        Require(colliders.All(c => c.Foot != FootMat.Auto), "every collider has a foot material");
        var plan = shell.Plan;
        Require(colliders.OfType<SlabCollider>().Count() == 2, "the podium terrace is a slab");
        Require(colliders.OfType<WallCollider>().Any(w => MathF.Abs(w.Y1 - plan.SecondFloorY) < 1e-3f), "low wall under the stair landing");
        Require(colliders.OfType<WallCollider>().Count(w => w.Y1 > plan.PadY + 200f) >= 12, "walls above the terrace run to 300 m");
    }

    [TestCase]
    public void SkylightSpansTheAtrium()
    {
        var plan = Shell(RealWorld()).Plan;
        var across = 2 * plan.SkylightRadius * DetMath.Cos(Shapes.Tau / 16f);
        Require(MathF.Abs(across - 34.5f) < 0.01f, $"skylight across flats {across}");
        Require(LibraryRoof.SkylightApexY(plan) > plan.RoofY + 3f, "the cone rises above the curb");
        var north = LibraryRoof.FaceDirection(plan, LibraryRoof.NorthFace(plan));
        Require(north.Y < -0.9f, $"mechanical box on the north axis {north}");
    }

    // ---------------------------------------------------------------- fixtures

    private static LibraryShell Shell(IWorldModel world) =>
        CoreSite.Pieces(world).OfType<LibraryShell>().SingleOrDefault() ?? throw new InvalidOperationException("Expected: library shell piece");

    /// <summary>A stand-in for WP-14's allocation: a 101 × 7 generic block sorted before the library, one after it.</summary>
    private static WindowNumberingSnapshot Numbering(LibraryShell shell, int libraryColumns)
    {
        var library = new WindowAllocation(CoreFeatures.Library, LibraryShell.WindowKey, 707, libraryColumns, LibraryShell.Rows);
        return new(new MapId("qingshuihe"), new string('a', 64),
        [
            new WindowAllocation(new FeatureId("qsh:way:0"), "generic", 0, 101, 7),
            library,
            new WindowAllocation(new FeatureId("qsh:way:9"), "generic", 707 + library.Count, 10, 1),
        ]);
    }

    private static bool Throws(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return true; }
        catch (KeyNotFoundException) { return true; }
        return false;
    }

    private static bool Inside(Vector2 p, IReadOnlyList<Vector2> ring)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            if ((ring[i].Y > p.Y) != (ring[j].Y > p.Y) && p.X < (ring[j].X - ring[i].X) * (p.Y - ring[i].Y) / (ring[j].Y - ring[i].Y) + ring[i].X)
                inside = !inside;
        return inside;
    }

    private static Vector3 P(GeometryBundle b, int i) => new(b.Position[3 * i], b.Position[3 * i + 1], b.Position[3 * i + 2]);
    private static Vector3 N(GeometryBundle b, int i) => new(b.Normal[3 * i], b.Normal[3 * i + 1], b.Normal[3 * i + 2]);

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

    private static PaletteRegistry Palette()
    {
        var dir = Path.Combine(FindRepository(), "game", "style", "palette");
        var entries = new List<PaletteEntry>();
        foreach (var file in new[] { Path.Combine(dir, "core.json") }.Concat(Directory.EnumerateFiles(Path.Combine(dir, "ext"), "WP-*.json").Order(StringComparer.Ordinal)))
            entries.AddRange(JsonSerializer.Deserialize<PaletteRegistryData>(File.ReadAllBytes(file), ContractJson.Options)!.Entries);
        return new PaletteRegistry(entries);
    }

    private static string FindRepository()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ownership.json"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }
}
