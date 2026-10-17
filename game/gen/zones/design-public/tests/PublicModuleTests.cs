using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.World;
using GdUnit4;

namespace Campus.Gen.Zones.DesignPublic.Tests;

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
        Require(benches.Count == 2 && pits.Count == 2, $"two benches and two site-built pits ({benches.Count}, {pits.Count})");
        foreach (var bench in benches)
        {
            var pad = Pad(world, bench);
            Require(pad.Zone?.Value == "public", $"{pad.Id.Value} is filed under the public zone");
            Require(Text(pad.Props, "roadType") == "pedestrian", $"{pad.Id.Value} is a pedestrian area WP-15 paves");
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
            Require(Vector2.Distance(c, bench.Centre) < 0.01f, $"{bench.Id.Value} is centred on its pad");
            foreach (var road in world.Network.Roads)
            {
                var type = Text(road.Props, "highway") ?? Text(road.Props, "roadType") ?? "";
                var edge = road.Width * 0.5f + (Carriageway.Contains(type) ? 0.15f : 0f);
                var d = Distance(c, road.Line.Points) - edge - r;
                Require(d > 0.2f, $"{bench.Id.Value} pad comes within {d:F2} m of road {road.Id.Value} ({type})");
            }
            foreach (var f in world.Features.Values)
            {
                if (f.Id.Value.StartsWith(PublicTune.Prefix, StringComparison.Ordinal)) continue;
                var kind = f.Kind.Value;
                if (kind is "building" or "water" or "parking" or "sport" || (kind == "road" && f.Geometry is PolygonGeometry or MultiPolygonGeometry))
                    foreach (var ring in Rings(f.Geometry))
                        Require(Distance(c, ring, closed: true) - r > 0.2f && !Inside(c, ring), $"{bench.Id.Value} pad overlaps {f.Id.Value} ({kind})");
                if (f.Source == FeatureSource.Design && f.Geometry is PointGeometry p)
                    Require(Vector2.Distance(c, new(p.Position.X, p.Position.Z)) > r + 1f, $"{bench.Id.Value} pad sits on {f.Id.Value}");
            }
            foreach (var tree in world.Network.Trees)
                Require(Vector2.Distance(c, new(tree.Position.X, tree.Position.Z)) > r + 1f, $"{bench.Id.Value} pad sits on OSM tree {tree.Id.Value}");
        }
    }

    /// <summary>MAP-QSH-ADDITIONS §2.3: a 4 m bench whose back faces the evening sun, a tree north-east of it, both on the pad.</summary>
    [TestCase]
    public void BenchAndTreeSitOnThePadFacingTheSun()
    {
        var world = Loaded.Value;
        var pits = PocketGreens.Pits(world);
        foreach (var bench in PocketGreens.Benches(world))
        {
            var (c, r) = Disc(Pad(world, bench));
            var length = bench.Radius * (bench.To - bench.From) * MathF.PI / 180f;
            Require(MathF.Abs(length - 4f) < 0.05f, $"{bench.Id.Value} is 4 m long ({length:F2})");
            Require(MathF.Abs((bench.From + bench.To) * 0.5f - 241f) < 15f, $"{bench.Id.Value} turns its back to 241° ± 15°");
            Require(bench.Radius + PublicTune.Bench.PostOffset + PublicTune.Bench.PostSize < r - 0.2f, $"{bench.Id.Value} back stays on the paving");
            var pit = pits.Single(p => Vector2.Distance(p.Centre, c) < r);
            Require(Vector2.Distance(pit.Centre, c) + pit.Radius < r - 0.3f, $"{pit.Id.Value} lies inside the pad with a paved margin");
            Require(bench.Radius - PublicTune.Bench.HalfDepth - (Vector2.Distance(pit.Centre, c) + pit.Radius) > 0.6f, $"{pit.Id.Value} leaves knee room in front of the bench");
            var toPit = Vector2.Normalize(pit.Centre - c);
            Require(Vector2.Dot(toPit, PocketGreens.Dir(61f)) > 0.95f, $"{pit.Id.Value} stands north-east of the pad centre, so its evening shadow misses the bench");
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
        Require(colliders.OfType<BoxCollider>().Count() == benches.Count * PublicTune.Bench.ColliderBoxes, "four seat boxes per bench");
        Require(colliders.OfType<PostCollider>().Count() == PocketGreens.Pits(world).Count, "one kerb post per pit");
        foreach (var bench in benches)
        {
            var deck = PocketGreens.Paving(bench, Ground);
            foreach (var box in PocketGreens.SeatBoxes(bench, Ground))
            {
                var top = box.At.M42 + box.Half.Y - deck;
                Require(top is >= 0.35f and <= 1.25f, $"{bench.Id.Value} seat box top {top:F2} m is jumpable (SYS-WALK §7)");
            }
            var seats = PocketGreens.Seats(bench, Ground);
            Require(seats.Count == PublicTune.Bench.ColliderBoxes && seats.All(s => s.Back), $"{bench.Id.Value} declares a backed seat edge on every box");
            foreach (var seat in seats)
            {
                var mid = (seat.From + seat.To) * 0.5f;
                Require(Vector2.Dot(new(seat.Facing.X, seat.Facing.Z), bench.Centre - new Vector2(mid.X, mid.Z)) > 0f, $"{bench.Id.Value} sitters face the pad centre");
                Require(MathF.Abs(mid.Y - deck - PublicTune.Bench.SeatTop) < 0.01f, $"{bench.Id.Value} seat edge at the seat top");
            }
        }
    }

    [TestCase]
    public void BuildIsDeterministicWithinBudgetAndRegisteredColours()
    {
        var ctx = Context();
        var module = new PublicModule();
        var tiles = module.Tiles(ctx).ToArray();
        Require(tiles.Length == 2, $"two occupied cells ({tiles.Length})");
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
                Require(a.Meshes.Count > 0 && a.Meshes.Count <= module.Budget.DrawCalls, $"{lod} {cell}: {a.Meshes.Count} draws");
                Require(a.Meshes.Count == b.Meshes.Count && a.Meshes.Zip(b.Meshes).All(p => p.First.Position.SequenceEqual(p.Second.Position) && p.First.Index.SequenceEqual(p.Second.Index)),
                    $"{lod} {cell} builds the same bytes twice");
                var triangles = a.Meshes.Sum(m => m.Index.Length / 3);
                Require(triangles <= budget, $"{lod} {cell}: {triangles} triangles over {budget}");
                total += triangles;
                foreach (var mesh in a.Meshes)
                    Require(mesh.Position.All(float.IsFinite) && mesh.Normal.All(float.IsFinite), $"{lod} {cell}: finite vertices");
            }
            Require(total > 0, $"{lod} builds something");
        }
        foreach (var family in new[] { "door.wood", "stone.beige", "equip.fence", "base.warmBrown" })
            Require(tokens.Contains(family + ".lit") && tokens.Contains(family + ".shade"), $"{family} is a registered pair");
        Require(module.Build(ctx, new(0, 0), LodLevel.L4).Meshes.Count == 0, "no whole-map product");
        foreach (var t in tiles) Require(module.Build(ctx, PublicModule.Cell(new(t.X * 64f + 1f, t.Z * 64f + 1f), LodLevel.L3), LodLevel.L3).Meshes.Count == 0, "nothing at L3");
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

    private static BuildContext Context() => new() { World = Loaded.Value, Map = null!, WorldSeed = 1 };

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

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
