using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Contracts.Parts;
using Campus.Gen.World;
using Campus.Gen.Zones.Core.Framework;
using Campus.Gen.Zones.Core.Library;
using Campus.Gen.Zones.Core.Museum;
using GdUnit4;

namespace Campus.Gen.Zones.Core.Tests;

[TestSuite]
public sealed class MuseumShellTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void RingsKeepTheHallBodyShapeAndTheColonnadeWidth()
    {
        var museum = Museum(RealWorld());
        Require(museum.Body.Count == 8 && museum.Gallery.Count == 16 && museum.Hall.Count == 16 && museum.Eave.Count == 16 && museum.Platform.Count == 16,
            "an octagon body and four chamfered rings");
        Require(new[] { museum.South, museum.West, museum.North, museum.East }.Distinct().Count() == 4, "four distinct long faces");
        var south = MuseumGeometry.Normal(museum.Body, museum.South);
        var west = MuseumGeometry.Normal(museum.Body, museum.West);
        Require(south.Y > 0.95f && west.X < -0.95f, $"south {south} faces +z, west {west} faces −x");
        foreach (var p in museum.Hall) Require(Inside(p, museum.Gallery), "the hall stands inside the gallery");
        foreach (var p in museum.Gallery) Require(Inside(p, museum.Eave), "the eave is outside the gallery walls");
        foreach (var p in museum.Eave) Require(Inside(p, museum.Platform), "the eave is inside the platform edge");
        for (var i = 0; i < 8; i++)
        {
            var gallery = museum.GalleryFace(i);
            var platform = MuseumGeometry.FaceOf(museum.Platform, 2 * i + 1);
            var width = Vector2.Dot(platform.A - gallery.A, gallery.Out);
            Require(width is > 4.5f and < 7.5f, $"colonnade ring {i} is {width:F2} m wide (约 5.5 m, from 8402)");
        }
        // The chamfered folds stay out of the outline's 40°–50° band.
        foreach (var ring in new[] { museum.Gallery, museum.Hall, museum.Eave })
            for (var k = 0; k < ring.Count; k++)
            {
                var a = MuseumGeometry.Normal(ring, k);
                var b = MuseumGeometry.Normal(ring, (k + 1) % ring.Count);
                // cos 35° = 0.819: every fold is gentler than 35°.
                Require(Vector2.Dot(a, b) > 0.819f, $"fold between faces {k} and {k + 1} (cos {Vector2.Dot(a, b):F3})");
            }
    }

    [TestCase]
    public void DoorsPorticoAndColumnsSitWhereTheEntrancesAre()
    {
        var museum = Museum(RealWorld());
        var main = museum.MainDoor;
        var lake = museum.LakeDoor;
        Require(main.Face == museum.GalleryFace(museum.South) && lake.Face == museum.GalleryFace(museum.West), "main door south, side door on the lake front");
        var entrances = museum.Entrances().ToArray();
        Require(entrances.Length == 2 && entrances.All(e => e.ClearWidth >= 1.1f), "two open entrances at least 1.1 m clear");
        foreach (var (id, centre, outward, clear) in entrances)
        {
            var bearing = Shapes.BearingOf(new Vector2(outward.X, outward.Z));
            Vector3 outside = centre + outward * 2.5f, inside = centre - outward * 3.0f;
            Console.WriteLine($"{id}: door ({centre.X:F4}, {centre.Y:F2}, {centre.Z:F4}) facing {bearing:F1}° outside ({outside.X:F4}, {outside.Y:F2}, {outside.Z:F4}) inside ({inside.X:F4}, {inside.Y:F2}, {inside.Z:F4}) clear {clear:F2}");
        }
        var columns = museum.ColumnPositions().ToArray();
        Require(columns.Count(c => c.Size == CoreStyle.PorticoColumn) == 4, "four portico columns");
        Require(columns.Length is > 30 and < 70, $"{columns.Length} columns round the ring");
        // No column stands in front of either door's clear way.
        foreach (var (_, centre, outward, clear) in entrances)
            foreach (var c in columns)
            {
                var d = c.C - new Vector2(centre.X, centre.Z);
                var o = new Vector2(outward.X, outward.Z);
                var across = MathF.Abs(d.X * o.Y - d.Y * o.X);
                Require(Vector2.Dot(d, o) < 0 || across > clear * 0.5f + c.Size * 0.5f, "the way out of each door is clear of columns");
            }
        var parts = Build(museum, LodLevel.L0).Parts.Where(p => p.Part.Value == PartCatalog.DoorGlass).ToArray();
        Require(parts.Length == 2 && parts.All(p => p.Params is DoorParams { OpenAngleDeg: 90f }), "two WP-10 glass doors fixed open");
        Require(Build(museum, LodLevel.L2).Parts.Count == 0, "no door parts beyond L1");
    }

    [TestCase]
    public void EveryLevelIsValidWithNumberedGlass()
    {
        var world = RealWorld();
        var palette = Palette();
        var museum = Museum(world);
        var numbering = Numbering(museum.Columns, 1);
        var allocation = numbering.Get(CoreFeatures.Museum, MuseumShell.WindowKey);
        Require(museum.Columns > 20 && museum.Clerestory.Count >= 6, $"{museum.Columns} grid columns, {museum.Clerestory.Count} clerestory runs");
        foreach (var lod in Levels)
        {
            var build = new CoreBuild(palette) { WindowNumbering = numbering, PartLods = PartLodFixture.Catalog };
            museum.Build(build, lod);
            var output = build.Finish(lod);
            var glass = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
            Require(glass.Length == 1, $"{lod}: one window-glass bundle ({glass.Length})");
            for (var i = 0; i < glass[0].Uv2!.Length; i += 2)
                Require(glass[0].Uv2![i] == allocation.BaseId && (int)glass[0].Uv2![i + 1] / 32 == museum.Columns, "UV2 carries the museum's base and columns");
            Require(output.Meshes.Count <= 8, $"{lod}: {output.Meshes.Count} bundles");
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
            Console.WriteLine($"museum {lod}: {output.Meshes.Sum(m => m.Index.Length / 3)} triangles, {output.Meshes.Count} bundles");
        }
        var cells = museum.Cells().ToArray();
        Require(cells.Select(c => (c.Column, c.Row)).Distinct().Count() == cells.Length, "every window cell has its own ID");
        Require(museum.Windows(numbering).All(w => w.Channel == CoreStyle.MuseumWindowChannel), "every window on interior.public");
    }

    [TestCase]
    public void LibraryAndMuseumShareOneWindowGlassBatchInTheirL3Cell()
    {
        var world = RealWorld();
        var palette = Palette();
        var pieces = CoreSite.Pieces(world);
        var library = pieces.OfType<LibraryShell>().Single();
        var museum = pieces.OfType<MuseumShell>().Single();
        var tile = CoreTiles.Of(museum.Anchor, LodLevel.L3);
        Require(tile == CoreTiles.Of(library.Anchor, LodLevel.L3), "library and museum share an L3 cell");
        var numbering = Numbering(museum.Columns, library.Columns);
        var ctx = new BuildContext { World = world, Map = null!, WorldSeed = 1u, Palette = palette, WindowNumbering = numbering, PartLods = PartLodFixture.Catalog };
        var module = new CoreModule();
        foreach (var lod in new[] { LodLevel.L3, LodLevel.L4 })
        {
            var output = module.Build(ctx, CoreTiles.Of(museum.Anchor, lod), lod);
            var glass = output.Meshes.Where(m => m.Material.Spec.Kind == ToonKind.Glass).ToArray();
            Require(glass.Length == 1, $"{lod}: one shared window-glass bundle ({glass.Length})");
            var bases = Enumerable.Range(0, glass[0].Uv2!.Length / 2).Select(i => glass[0].Uv2![2 * i]).Distinct().Order().ToArray();
            var here = pieces.Where(p => p is IWindowedPiece && CoreTiles.Of(p.Anchor, lod) == CoreTiles.Of(museum.Anchor, lod)).Select(p => ((IWindowedPiece)p).WindowLayout.Feature).ToHashSet();
            Require(bases.SequenceEqual(numbering.Allocations.Where(a => here.Contains(a.Feature)).Select(a => (float)a.BaseId).Order()),
                $"{lod}: the shared bundle keeps each building's window base ({string.Join(", ", bases)})");
            Require(output.Meshes.Count <= module.Budget.DrawCalls, $"{lod}: {output.Meshes.Count} bundles within {module.Budget.DrawCalls}");
        }
    }

    [TestCase]
    public void CollisionLeavesBothDoorsOpenAndFillsEveryFoot()
    {
        var museum = Museum(RealWorld());
        var colliders = museum.Collide();
        Require(colliders.All(c => c.Foot != FootMat.Auto), "every collider has a foot material");
        Require(colliders.OfType<SlabCollider>().Count(s => s.Tag == SurfaceTag.Ground) >= museum.Gallery.Count, "the colonnade floor collides round the ring");
        foreach (var (id, centre, outward, clear) in museum.Entrances())
        {
            // Walk the door's centre line from 1 m outside to 1 m inside at knee height: no wall may cross it.
            Vector2 a = new(centre.X + outward.X, centre.Z + outward.Z), b = new(centre.X - outward.X, centre.Z - outward.Z);
            var across = Vector2.Normalize(new Vector2(-outward.Z, outward.X));
            foreach (var offset in new[] { -clear * 0.5f + 0.05f, 0f, clear * 0.5f - 0.05f })
                foreach (var w in colliders.OfType<WallCollider>().Where(w => w.Y0 < centre.Y + 0.5f && w.Y1 > centre.Y + 0.5f))
                    Require(SegmentDistance(a + across * offset, b + across * offset, w.A, w.B) > 0.01f, $"{id}: the way through the door at {offset:F2} m is clear of walls");
            foreach (var w in colliders.OfType<WallCollider>().Where(w => w.Y0 < centre.Y + 0.5f && w.Y1 > centre.Y + 0.5f))
                Require(SegmentDistance(a, b, w.A, w.B) > 0.3f, $"{id}: a walker's radius fits through the door's middle");
        }
    }

    // ---------------------------------------------------------------- fixtures

    private static MuseumShell Museum(IWorldModel world) =>
        CoreSite.Pieces(world).OfType<MuseumShell>().SingleOrDefault() ?? throw new InvalidOperationException("Expected: museum piece");

    private static ContentOutput Build(MuseumShell museum, LodLevel lod)
    {
        var build = new CoreBuild(null) { PartLods = PartLodFixture.Catalog };
        museum.Build(build, lod);
        return build.Finish(lod);
    }

    /// <summary>A stand-in for WP-14's allocation: a generic block, the library and the museum in ordinal feature order.</summary>
    private static WindowNumberingSnapshot Numbering(int museumColumns, int libraryColumns)
    {
        var generic = new WindowAllocation(new FeatureId("qsh:way:0"), "generic", 0, 101, 7);
        var library = new WindowAllocation(CoreFeatures.Library, LibraryShell.WindowKey, generic.Count, libraryColumns, LibraryShell.Rows);
        var museum = new WindowAllocation(CoreFeatures.Museum, MuseumShell.WindowKey, generic.Count + library.Count, museumColumns, MuseumShell.Rows);
        return new(new MapId("qingshuihe"), new string('a', 64), [generic, library, museum, .. AnnexShellTests.Allocations(RealWorld(), museum.BaseId + museum.Count)]);
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
