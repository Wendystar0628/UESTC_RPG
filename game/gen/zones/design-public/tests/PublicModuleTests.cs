using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Zones.DesignPublic.Tests;

using static GdUnit4.Assertions;

/// <summary>WP-29 29a: the two corner pocket greens of overlay/public.d/, loaded with every other overlay of the map.</summary>
[TestSuite]
public sealed class PublicModuleTests
{
    private static readonly string[] Carriageway = ["service", "residential", "tertiary", "secondary", "primary", "unclassified", "living_street", "road", "trunk"];
    private static readonly Lazy<IWorldModel> Loaded = new(Load);

    [TestCase]
    public void OverlayLoadsBesideEveryOtherOverlay()
    {
        var world = Loaded.Value;
        var benches = PocketGreens.Benches(world);
        var pits = PocketGreens.Pits(world);
        AssertThat(benches.Count == 2 && pits.Count == 2).OverrideFailureMessage($"two benches and two site-built pits ({benches.Count}, {pits.Count})").IsTrue();
        foreach (var bench in benches)
        {
            var pad = Pad(world, bench);
            AssertThat(pad.Zone?.Value == "public").OverrideFailureMessage($"{pad.Id.Value} is filed under the public zone").IsTrue();
            AssertThat(Text(pad.Props, "roadType") == "pedestrian").OverrideFailureMessage($"{pad.Id.Value} is a pedestrian area WP-15 paves").IsTrue();
        }
    }

    /// <summary>Each pad keeps off carriageways, footways, buildings, water, parking, courts and every other package's additions.</summary>
    [TestCase]
    public void PadsKeepClearOfRoadsBuildingsAndOtherAdditions()
    {
        var world = Loaded.Value;
        foreach (var bench in PocketGreens.Benches(world))
        {
            var (c, r) = Disc(Pad(world, bench));
            AssertThat(Vector2.Distance(c, bench.Centre) < 0.01f).OverrideFailureMessage($"{bench.Id.Value} is centred on its pad").IsTrue();
            foreach (var road in world.Network.Roads)
            {
                var type = Text(road.Props, "highway") ?? Text(road.Props, "roadType") ?? "";
                var edge = road.Width * 0.5f + (Carriageway.Contains(type) ? 0.15f : 0f);
                var d = Distance(c, road.Line.Points) - edge - r;
                AssertThat(d > 0.2f).OverrideFailureMessage($"{bench.Id.Value} pad comes within {d:F2} m of road {road.Id.Value} ({type})").IsTrue();
            }
            foreach (var f in world.Features.Values)
            {
                if (f.Id.Value.StartsWith(PublicTune.Prefix, StringComparison.Ordinal)) continue;
                var kind = f.Kind.Value;
                if (kind is "building" or "water" or "parking" or "sport" || (kind == "road" && f.Geometry is PolygonGeometry or MultiPolygonGeometry))
                    foreach (var ring in Rings(f.Geometry))
                        AssertThat(Distance(c, ring, closed: true) - r > 0.2f && !Inside(c, ring)).OverrideFailureMessage($"{bench.Id.Value} pad overlaps {f.Id.Value} ({kind})").IsTrue();
                if (f.Source == FeatureSource.Design && f.Geometry is PointGeometry p)
                    AssertThat(Vector2.Distance(c, new(p.Position.X, p.Position.Z)) > r + 1f).OverrideFailureMessage($"{bench.Id.Value} pad sits on {f.Id.Value}").IsTrue();
            }
            foreach (var tree in world.Network.Trees)
                AssertThat(Vector2.Distance(c, new(tree.Position.X, tree.Position.Z)) > r + 1f).OverrideFailureMessage($"{bench.Id.Value} pad sits on OSM tree {tree.Id.Value}").IsTrue();
        }
    }

    /// <summary>MAP-QSH-ADDITIONS: a 4 m bench whose back faces the evening sun, a tree north-east of it, both on the pad.</summary>
    [TestCase]
    public void BenchAndTreeSitOnThePadFacingTheSun()
    {
        var world = Loaded.Value;
        var pits = PocketGreens.Pits(world);
        foreach (var bench in PocketGreens.Benches(world))
        {
            var (c, r) = Disc(Pad(world, bench));
            var length = bench.Radius * (bench.To - bench.From) * MathF.PI / 180f;
            AssertThat(MathF.Abs(length - 4f) < 0.05f).OverrideFailureMessage($"{bench.Id.Value} is 4 m long ({length:F2})").IsTrue();
            AssertThat(MathF.Abs((bench.From + bench.To) * 0.5f - 241f) < 15f).OverrideFailureMessage($"{bench.Id.Value} turns its back to 241° ± 15°").IsTrue();
            AssertThat(bench.Radius + PublicTune.Bench.PostOffset + PublicTune.Bench.PostSize < r - 0.2f).OverrideFailureMessage($"{bench.Id.Value} back stays on the paving").IsTrue();
            var pit = pits.Single(p => Vector2.Distance(p.Centre, c) < r);
            AssertThat(Vector2.Distance(pit.Centre, c) + pit.Radius < r - 0.3f).OverrideFailureMessage($"{pit.Id.Value} lies inside the pad with a paved margin").IsTrue();
            AssertThat(bench.Radius - PublicTune.Bench.HalfDepth - (Vector2.Distance(pit.Centre, c) + pit.Radius) > 0.6f).OverrideFailureMessage($"{pit.Id.Value} leaves knee room in front of the bench").IsTrue();
            var toPit = Vector2.Normalize(pit.Centre - c);
            AssertThat(Vector2.Dot(toPit, PocketGreens.Dir(61f)) > 0.95f).OverrideFailureMessage($"{pit.Id.Value} stands north-east of the pad centre, so its evening shadow misses the bench").IsTrue();
        }
    }

    [TestCase]
    public void SeatsAndCollidersShareTheSeatBoxes()
    {
        var ctx = Context();
        var module = new PublicModule();
        var world = ctx.World;
        float Ground(Vector2 p) => world.Terrain.HeightAt(p.X, p.Y);
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        var benches = PocketGreens.Benches(world);
        AssertThat(colliders.OfType<BoxCollider>().Count() == benches.Count * PublicTune.Bench.ColliderBoxes).OverrideFailureMessage("four seat boxes per bench").IsTrue();
        AssertThat(colliders.OfType<PostCollider>().Count() == PocketGreens.Pits(world).Count).OverrideFailureMessage("one kerb post per pit").IsTrue();
        foreach (var bench in benches)
        {
            var deck = PocketGreens.Paving(bench, Ground);
            foreach (var box in PocketGreens.SeatBoxes(bench, Ground))
            {
                var top = box.At.M42 + box.Half.Y - deck;
                AssertThat(top is >= 0.35f and <= 1.25f).OverrideFailureMessage($"{bench.Id.Value} seat box top {top:F2} m is jumpable (SYS-WALK)").IsTrue();
            }
            var seats = PocketGreens.Seats(bench, Ground);
            AssertThat(seats.Count == PublicTune.Bench.ColliderBoxes && seats.All(s => s.Back)).OverrideFailureMessage($"{bench.Id.Value} declares a backed seat edge on every box").IsTrue();
            foreach (var seat in seats)
            {
                var mid = (seat.From + seat.To) * 0.5f;
                AssertThat(Vector2.Dot(new(seat.Facing.X, seat.Facing.Z), bench.Centre - new Vector2(mid.X, mid.Z)) > 0f).OverrideFailureMessage($"{bench.Id.Value} sitters face the pad centre").IsTrue();
                AssertThat(MathF.Abs(mid.Y - deck - PublicTune.Bench.SeatTop) < 0.01f).OverrideFailureMessage($"{bench.Id.Value} seat edge at the seat top").IsTrue();
            }
        }
    }

    [TestCase]
    public void BuildIsDeterministicWithinBudgetAndRegisteredColours()
    {
        var ctx = Context();
        var module = new PublicModule();
        var tiles = module.Tiles(ctx).ToArray();
        AssertThat(tiles.Length == 2).OverrideFailureMessage($"two occupied cells ({tiles.Length})").IsTrue();
        var tokens = PaletteTokens();
        foreach (var lod in new[] { LodLevel.L0, LodLevel.L1, LodLevel.L2 })
        {
            var budget = lod == LodLevel.L0 ? module.Budget.TrianglesL0 : module.Budget.TrianglesL1;
            var cells = PocketGreens.Benches(ctx.World).Select(b => PublicModule.Cell(b.Centre, lod)).Distinct().ToArray();
            var total = 0;
            foreach (var cell in cells)
            {
                var a = module.Build(ctx, cell, lod);
                var b = module.Build(ctx, cell, lod);
                AssertThat(a.Meshes.Count > 0 && a.Meshes.Count <= module.Budget.DrawCalls).OverrideFailureMessage($"{lod} {cell}: {a.Meshes.Count} draws").IsTrue();
                AssertThat(a.Meshes.Count == b.Meshes.Count && a.Meshes.Zip(b.Meshes).All(p => p.First.Position.SequenceEqual(p.Second.Position) && p.First.Index.SequenceEqual(p.Second.Index))).OverrideFailureMessage($"{lod} {cell} builds the same bytes twice").IsTrue();
                var triangles = a.Meshes.Sum(m => m.Index.Length / 3);
                AssertThat(triangles <= budget).OverrideFailureMessage($"{lod} {cell}: {triangles} triangles over {budget}").IsTrue();
                total += triangles;
                foreach (var mesh in a.Meshes)
                    AssertThat(mesh.Position.All(float.IsFinite) && mesh.Normal.All(float.IsFinite)).OverrideFailureMessage($"{lod} {cell}: finite vertices").IsTrue();
            }
            AssertThat(total > 0).OverrideFailureMessage($"{lod} builds something").IsTrue();
        }
        foreach (var family in new[] { "door.wood", "stone.beige", "equip.fence", "base.warmBrown" })
            AssertThat(tokens.Contains(family + ".lit") && tokens.Contains(family + ".shade")).OverrideFailureMessage($"{family} is a registered pair").IsTrue();
        AssertThat(module.Build(ctx, new(0, 0), LodLevel.L4).Meshes.Count == 0).OverrideFailureMessage("no whole-map product").IsTrue();
        foreach (var t in tiles) AssertThat(module.Build(ctx, PublicModule.Cell(new(t.X * 64f + 1f, t.Z * 64f + 1f), LodLevel.L3), LodLevel.L3).Meshes.Count == 0).OverrideFailureMessage("nothing at L3").IsTrue();
    }

    // ---- Helpers ----

    private static WorldFeature Pad(IWorldModel world, ArcBench bench) =>
        world.Features[new(bench.Id.Value[..^"-bench".Length])];

    private static (Vector2 Centre, float Radius) Disc(WorldFeature pad)
    {
        var ring = ((PolygonGeometry)pad.Geometry).Rings.Outer;
        var c = ring.Aggregate(Vector2.Zero, (s, p) => s + p) / ring.Count;
        return (c, ring.Min(p => Vector2.Distance(p, c)));
    }

    private static IEnumerable<IReadOnlyList<Vector2>> Rings(OverlayGeometry g) => g switch
    {
        PolygonGeometry p => [p.Rings.Outer],
        MultiPolygonGeometry m => m.Polygons.Select(p => p.Outer),
        _ => [],
    };

    private static float Distance(Vector2 p, IReadOnlyList<Vector2> line, bool closed = false)
    {
        var best = float.MaxValue;
        var count = closed ? line.Count : line.Count - 1;
        for (var i = 0; i < count; i++)
        {
            var a = line[i]; var ab = line[(i + 1) % line.Count] - a;
            var len2 = ab.LengthSquared();
            var q = len2 < 1e-8f ? a : a + ab * Math.Clamp(Vector2.Dot(p - a, ab) / len2, 0f, 1f);
            best = MathF.Min(best, Vector2.Distance(p, q));
        }
        return best;
    }

    private static bool Inside(Vector2 p, IReadOnlyList<Vector2> ring)
    {
        var inside = false;
        for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            if ((ring[i].Y > p.Y) != (ring[j].Y > p.Y) && p.X < (ring[j].X - ring[i].X) * (p.Y - ring[i].Y) / (ring[j].Y - ring[i].Y) + ring[i].X) inside = !inside;
        return inside;
    }

    private static string? Text(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static HashSet<string> PaletteTokens()
    {
        var game = Path.GetFullPath(Path.Combine(DataDirectory(), "..", "..", ".."));
        var dir = Path.Combine(game, "style", "palette");
        var files = new[] { Path.Combine(dir, "core.json") }.Concat(Directory.GetFiles(Path.Combine(dir, "ext"), "*.json").Order(StringComparer.Ordinal));
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            using var palette = JsonDocument.Parse(File.ReadAllBytes(file));
            if (!palette.RootElement.TryGetProperty("entries", out var entries)) continue;
            foreach (var e in entries.EnumerateArray()) if (e.TryGetProperty("token", out var t)) tokens.Add(t.GetString()!);
        }
        return tokens;
    }

    private static BuildContext Context() => new() { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = Loaded.Value, Map = null!, WorldSeed = 1 };

    private static string DataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        return Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
    }

    private static IWorldModel Load()
    {
        var data = DataDirectory();
        var names = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetDirectories(Path.Combine(data, "overlay"), "*.d")
                    .Where(dir => Path.GetFileName(dir) is not "interior.d" and not "terrain.d" and not "outskirts.d" and not "walkable.d" and not "fidelity.d")
                    .SelectMany(dir => Directory.GetFiles(dir, "*.json")).Order(StringComparer.Ordinal).Select(p => Path.GetRelativePath(data, p).Replace('\\', '/')));
        var files = names.ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

}
