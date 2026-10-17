using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.World;
using Campus.Gen.Zones.DesignBoundary.Framework;
using Campus.Gen.Zones.DesignBoundary.Gates;
using Campus.Gen.Zones.DesignBoundary.Ring;
using Campus.Gen.Zones.DesignBoundary.Waterfront;
using GdUnit4;
using G = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Gate;
using P = Campus.Gen.Zones.DesignBoundary.BoundaryTune.Plaza;

namespace Campus.Gen.Zones.DesignBoundary.Tests;

[TestSuite]
public sealed class BoundaryModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase]
    public void OverlayRegistersTheForecourtAndThePlaza()
    {
        var world = World();
        Require(world.Features.ContainsKey(new("design:boundary-north-forecourt")), "north forecourt registered");
        Require(world.Features.ContainsKey(new(P.Feature)), "A05 plaza registered");
    }

    [TestCase]
    public void GatesSitWhereTheirRoadsLeaveTheCampus()
    {
        var world = World();
        var ring = new BoundaryRing(world.Boundary.Outer);
        var expected = new Dictionary<string, Vector2>
        {
            ["north"] = new(-106.66f, -827.23f),
            ["west"] = new(-370.78f, -507.43f),
            ["northwest"] = new(-195.71f, -733.83f),
            ["west2"] = new(-619.65f, -192.53f),
            ["southeast"] = new(602.32f, 581.34f),
        };
        foreach (var spec in BoundaryTune.Gates)
        {
            var g = GateSite.Of(world, spec) ?? throw new InvalidOperationException($"{spec.Name} not resolved");
            Require(Vector2.Distance(g.Frame.Origin, expected[spec.Key]) < 0.2f, $"{spec.Name} origin {g.Frame.Origin}");
            Require(!ring.Contains(g.Frame.P(0, 2f)) && ring.Contains(g.Frame.P(0, -2f)), $"{spec.Name} +Z leaves the campus");
            Require(g.OpeningLength > 2 * g.Carriage && g.OpeningLength < 40f, $"{spec.Name} opening {g.OpeningLength:F1} m");
            var origin = ring.Arc(g.Frame.Origin);
            Require(ring.Forward(g.OpeningStart, origin) < g.OpeningLength, $"{spec.Name} opening holds the gate road");
        }
    }

    [TestCase]
    public void TheFenceRunsAllTheWayRound()
    {
        var world = World();
        var plan = BoundaryPlan.Of(world)!;
        var ring = plan.Ring;
        // Openings tile the ring with runs between them: nothing overlaps, every gap is a run.
        var covered = 0f;
        for (var i = 0; i < plan.Openings.Count; i++)
        {
            var o = plan.Openings[i];
            var next = plan.Openings[(i + 1) % plan.Openings.Count];
            var gap = ring.Forward(ring.Fold(o.Start + o.Length), next.Start);
            Require(gap < ring.Perimeter - 1f && gap >= 0.3f, $"opening {o.Key} overlaps or touches {next.Key} (gap {gap:F2} m)");
            covered += o.Length;
        }
        Require(covered < ring.Perimeter * 0.05f, $"openings take {covered:F0} m of {ring.Perimeter:F0} m");
        Require(plan.Openings.Count(o => o.Kind == OpeningKind.Gate) == BoundaryTune.Gates.Length + 1, "five gates of this package plus the south gate");
        Require(plan.Openings.Count(o => o.Kind == OpeningKind.Grate) >= 5, "a grate at every river crossing");
        foreach (var run in plan.Runs)
        {
            foreach (var p in run.Line)
                Require(ring.Contains(p) || Near(ring, p, 0.35f), $"fence point {p} stays inside the ring");
            var posts = run.Posts();
            for (var i = 0; i + 1 < posts.Count; i++)
                Require(Vector2.Distance(posts[i].At, posts[i + 1].At) <= BoundaryTune.Fence.PillarPitch + 0.01f, $"pillar pitch at {posts[i].At}");
            foreach (var hedge in run.Hedges)
                foreach (var p in hedge) Require(ring.Contains(p), $"hedge point {p} inside the campus");
        }
    }

    [TestCase]
    public void EveryRoadAndChannelThroughTheRingMeetsAnOpening()
    {
        var world = World();
        var plan = BoundaryPlan.Of(world)!;
        var ring = plan.Ring;
        bool Open(float s) => plan.Openings.Any(o => ring.Forward(o.Start, s) <= o.Length);
        var detours = BoundaryTune.Detours.Select(d => d.Road).ToHashSet();
        foreach (var road in world.Network.Roads.Where(r => !detours.Contains(r.Id.Value)))
            for (var k = 0; k + 1 < road.Line.Points.Count; k++)
                foreach (var s in ring.Crossings(road.Line.Points[k], road.Line.Points[k + 1]))
                    Require(Open(s), $"road {road.Id.Value} crosses the ring at {ring.At(s)} through the fence");
        foreach (var water in world.Features.Values.Where(f => f.Kind.Value == "water"))
            foreach (var outer in Outers(water.Geometry))
                for (var i = 0; i < outer.Count; i++)
                    foreach (var s in ring.Crossings(outer[i], outer[(i + 1) % outer.Count]))
                        Require(plan.Openings.Any(o => o.Kind == OpeningKind.Grate && ring.Forward(o.Start, s) <= o.Length), $"water {water.Id.Value} crosses the ring at {ring.At(s)} without a grate");
    }

    [TestCase]
    public void NothingWalksOutThroughAGate()
    {
        var ctx = Context();
        var colliders = new BoundaryModule().CollidersFor(ctx).Select(i => i.Collider).ToArray();
        foreach (var spec in BoundaryTune.Gates)
        {
            var g = GateSite.Of(ctx.World, spec)!;
            // Every line walked straight out across the gate, from 1 m beyond the left end to 1 m beyond the guard house.
            var ring = new BoundaryRing(ctx.World.Boundary.Outer);
            for (var x = g.LeftEnd - 1f; x <= g.HouseEnd + 1f; x += 0.1f)
            {
                // Only lines that start well inside the campus (north: the ring turns inward beside the left pier).
                var start = g.P(x, -6f);
                if (!ring.Contains(start) || Near(ring, start, 0.6f)) continue;
                // Walk out until 0.4 m past the ring (the north gate's ring jogs out round its forecourt).
                var blocked = false;
                var outside = 0f;
                for (var z = -6f; z <= 30f && !blocked && outside < 0.4f; z += 0.05f)
                {
                    blocked = colliders.Any(c => c.Tag == SurfaceTag.Boundary && Covers(c, g.P(x, z), 0.2f));
                    outside = ring.Contains(g.P(x, z)) ? 0f : outside + 0.05f;
                }
                Require(blocked, $"{spec.Name}: a walker at local x {x:F2} can leave through the gate");
            }
        }
    }

    [TestCase]
    public void NothingWalksOutAnywhereAlongTheRing()
    {
        var ctx = Context();
        var plan = BoundaryPlan.Of(ctx.World)!;
        var ring = plan.Ring;
        var colliders = new BoundaryModule().CollidersFor(ctx).Select(i => i.Collider).Where(c => c.Tag == SurfaceTag.Boundary).ToArray();
        // Bucket the colliders by 8 m cells of their plan extent.
        var buckets = new Dictionary<(int, int), List<Collider>>();
        foreach (var c in colliders)
        {
            var (min, max) = Extent(c);
            for (var x = (int)MathF.Floor(min.X / 8f); x <= (int)MathF.Floor(max.X / 8f); x++)
                for (var z = (int)MathF.Floor(min.Y / 8f); z <= (int)MathF.Floor(max.Y / 8f); z++)
                {
                    if (!buckets.TryGetValue((x, z), out var list)) buckets[(x, z)] = list = [];
                    list.Add(c);
                }
        }
        bool Hit(Vector2 p) => buckets.TryGetValue(((int)MathF.Floor(p.X / 8f), (int)MathF.Floor(p.Y / 8f)), out var list) && list.Any(c => Covers(c, p, 0.2f));
        var gates = plan.Openings.Where(o => o.Kind == OpeningKind.Gate).ToArray();
        var misses = new List<string>();
        for (var s = 0f; s < ring.Perimeter; s += 0.25f)
        {
            // Gates are walked by NothingWalksOutThroughAGate (the south gate by WP-18's own test).
            if (gates.Any(o => ring.Forward(o.Start - 1.5f, s) <= o.Length + 3f)) continue;
            // Detours leave the corner behind the fence; their line is a run like any other.
            if (plan.Detours.Any(d => ring.Forward(d.Start - 1f, s) <= d.Length + 2f)) continue;
            var p = ring.At(s);
            var n = ring.InwardAt(s);
            var blocked = false;
            for (var d = 2.5f; d >= -0.4f && !blocked; d -= 0.05f) blocked = Hit(p + n * d);
            if (!blocked) misses.Add($"{p}");
        }
        Require(misses.Count == 0, $"{misses.Count} places along the ring can be walked through, first {string.Join(", ", misses.Take(5))}");
    }

    [TestCase]
    public void CollidersAreTaggedAndCarryAFootMaterial()
    {
        var ctx = Context();
        var colliders = new BoundaryModule().CollidersFor(ctx).Select(i => i.Collider).ToArray();
        Require(colliders.Length > 0, "colliders exist");
        Require(colliders.All(c => c.Foot != FootMat.Auto), "every collider fills Foot");
        var rail = NorthPlazaSite.Frame.P((P.RailT0 + P.RailT1) * 0.5f, P.RailN);
        Require(colliders.Any(c => c.Tag == SurfaceTag.Barrier && Covers(c, rail, 0f)), "the waterside railing is an ordinary barrier");
        Require(colliders.Where(c => c is WallCollider { Tag: SurfaceTag.Barrier }).All(c => ((WallCollider)c).Y1 >= 1.5f), "barrier tops reach 1.5 m");
    }

    [TestCase]
    public void PlazaKeepsTheKioskSpotClearOfTheLaneAndTheRailing()
    {
        var world = World();
        var kiosk = NorthPlazaSite.Frame.P(P.Kiosk.X, P.Kiosk.Y);
        var lane = world.Network.Roads.First(r => r.Id.Value == "qsh:source:way:987943170");
        var laneEdge = PolylineDistance(kiosk, lane.Line.Points) - lane.Width * 0.5f;
        Require(laneEdge >= 4f, $"kiosk is {laneEdge:F2} m from the lane edge (≥ 3 m plus its half length)");
        Require(P.Kiosk.Y - P.RailN >= 2f, "kiosk stays clear of the railing");
        Require(P.Kiosk.X > P.T0 + 2f && P.Kiosk.X < P.T1 - 1f && P.Kiosk.Y < P.N1 - 1f, "kiosk inside the plaza");
        foreach (var bench in P.Benches) Require(Vector2.Distance(bench, P.Kiosk) > 5f, "benches leave the kiosk front free");
        var water = world.Features.Values.Where(f => f.Kind.Value == "water").ToArray();
        Require(water.Length > 0, "water features present");
    }

    [TestCase]
    public void EveryLevelIsValidAndInsideTheBudget()
    {
        var ctx = Context();
        var module = new BoundaryModule();
        Require(module.Tiles(ctx).Any(), "the module occupies cells");
        foreach (var lod in Levels)
        {
            var items = module.ItemsFor(ctx, lod);
            Require(items.Count > 0, $"{lod} has items");
            var size = 64 << (int)lod;
            var cells = items.Select(i => ((int)MathF.Floor(i.Anchor.X / size), (int)MathF.Floor(i.Anchor.Y / size))).Distinct().ToArray();
            var parts = 0;
            foreach (var (x, z) in lod == LodLevel.L4 ? [(0, 0)] : cells)
            {
                var output = module.Build(ctx, new(x, z), lod);
                foreach (var mesh in output.Meshes)
                {
                    mesh.ValidateVertexData();
                    Require(mesh.Position.Length / 3 <= 65536, "bundle vertex limit");
                }
                parts += output.Parts.Count;
                if (lod is LodLevel.L0 or LodLevel.L1)
                {
                    var triangles = output.Meshes.Sum(m => m.Index.Length / 3);
                    var limit = lod == LodLevel.L0 ? module.Budget.TrianglesL0 : module.Budget.TrianglesL1;
                    Require(triangles <= limit, $"{lod} cell ({x}, {z}) has {triangles} triangles, budget {limit}");
                    Require(output.Meshes.Select(m => m.Material).Distinct().Count() <= module.Budget.DrawCalls, $"{lod} cell ({x}, {z}) material count");
                }
            }
            Require(items.Count(i => i.Part is not null) == parts, $"{lod}: every placed part is emitted exactly once");
        }
    }

    [TestCase]
    public void SameInputSameBytes()
    {
        var a = Bytes(Context());
        var b = Bytes(Context());
        Require(a.Length > 0 && a.AsSpan().SequenceEqual(b), "two builds give the same bytes");
    }

    private static IEnumerable<IReadOnlyList<Vector2>> Outers(OverlayGeometry geometry) => geometry switch
    {
        PolygonGeometry p => [p.Rings.Outer],
        MultiPolygonGeometry m => m.Polygons.Select(r => r.Outer),
        _ => [],
    };

    private static (Vector2 Min, Vector2 Max) Extent(Collider c)
    {
        switch (c)
        {
            case WallCollider w:
                return (Vector2.Min(w.A, w.B) - new Vector2(w.HalfT), Vector2.Max(w.A, w.B) + new Vector2(w.HalfT));
            case BoxCollider b:
                {
                    var r = MathF.Max(b.Half.X, b.Half.Z) * 1.5f;
                    var o = new Vector2(b.At.M41, b.At.M43);
                    return (o - new Vector2(r), o + new Vector2(r));
                }
            default:
                return (Vector2.Zero, Vector2.Zero);
        }
    }

    private static bool Near(BoundaryRing ring, Vector2 p, float d)
    {
        var (edge, t) = ring.Project(p);
        return Vector2.Distance(ring.At(edge, t), p) <= d;
    }

    private static bool Covers(Collider c, Vector2 p, float slack) => c switch
    {
        BoxCollider box => InBox(box, p, slack),
        WallCollider wall => DistanceToSegment(p, wall.A, wall.B) <= wall.HalfT + slack,
        _ => false,
    };

    private static bool InBox(BoxCollider box, Vector2 p, float slack)
    {
        var ux = new Vector2(box.At.M11, box.At.M13);
        var uz = new Vector2(box.At.M31, box.At.M33);
        var d = p - new Vector2(box.At.M41, box.At.M43);
        return MathF.Abs(Vector2.Dot(d, ux)) <= box.Half.X + slack && MathF.Abs(Vector2.Dot(d, uz)) <= box.Half.Z + slack;
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }

    private static float PolylineDistance(Vector2 p, IReadOnlyList<Vector2> line)
    {
        var best = float.MaxValue;
        for (var i = 0; i + 1 < line.Count; i++) best = MathF.Min(best, DistanceToSegment(p, line[i], line[i + 1]));
        return best;
    }

    private static byte[] Bytes(BuildContext ctx)
    {
        var module = new BoundaryModule(BoundarySites.All.ToArray());
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (var tile in module.Tiles(ctx))
        {
            var output = module.Build(ctx, tile, LodLevel.L0);
            foreach (var mesh in output.Meshes)
            {
                foreach (var v in mesh.Position) writer.Write(v);
                foreach (var v in mesh.Normal) writer.Write(v);
                foreach (var v in mesh.Index) writer.Write(v);
            }
            foreach (var part in output.Parts) writer.Write(part.Part.Value + part.At.ToString());
            foreach (var collider in module.Collide(ctx, tile)) writer.Write(collider.ToString());
        }
        return stream.ToArray();
    }

    private static BuildContext Context() => new() { World = World(), Map = null!, WorldSeed = 1 };

    private static string DataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        return Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
    }

    private static IWorldModel World()
    {
        var data = DataDirectory();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetFiles(Path.Combine(data, "overlay", "boundary.d"), "*.json").Order(StringComparer.Ordinal)
                .Select(path => Path.GetRelativePath(data, path).Replace('\\', '/')))
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
