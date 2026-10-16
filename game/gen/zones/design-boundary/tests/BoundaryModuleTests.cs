using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.World;
using Campus.Gen.Zones.DesignBoundary.Framework;
using Campus.Gen.Zones.DesignBoundary.Gates;
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
        var expected = new Dictionary<string, Vector2> { ["north"] = new(-106.66f, -827.23f), ["west"] = new(-370.78f, -507.43f) };
        foreach (var spec in BoundaryTune.Gates)
        {
            var g = GateSite.Of(world, spec) ?? throw new InvalidOperationException($"{spec.Name} not resolved");
            Require(Vector2.Distance(g.Frame.Origin, expected[spec.Key]) < 0.2f, $"{spec.Name} origin {g.Frame.Origin}");
            Require(!ring.Contains(g.Frame.P(0, 2f)) && ring.Contains(g.Frame.P(0, -2f)), $"{spec.Name} +Z leaves the campus");
            Require(g.Wings.Count == spec.Wings.Length, $"{spec.Name} wings");
            foreach (var wing in g.Wings)
            {
                foreach (var p in wing.Line)
                    Require(ring.Contains(p) || Near(ring, p, 0.05f), $"{spec.Name} fence point {p} stays inside the ring");
                var posts = wing.Posts();
                for (var i = 0; i + 1 < posts.Count; i++)
                    Require(Vector2.Distance(posts[i].At, posts[i + 1].At) <= BoundaryTune.Fence.PillarPitch + 0.01f, $"{spec.Name} pillar pitch at {posts[i].At}");
                Require(posts[^1].End, $"{spec.Name} wing ends on an end pillar");
            }
        }
    }

    [TestCase]
    public void NothingWalksOutThroughAGate()
    {
        var ctx = Context();
        var colliders = new BoundaryModule().CollidersFor(ctx).Select(i => i.Collider).ToArray();
        foreach (var spec in BoundaryTune.Gates)
        {
            var g = GateSite.Of(ctx.World, spec)!;
            var f = g.Frame;
            // Every line walked straight out across the gate, from 1 m beyond the left pier to 1 m beyond the guard house.
            var ring = new BoundaryRing(ctx.World.Boundary.Outer);
            for (var x = g.LeftPierX - G.PierSize * 0.5f - 1f; x <= g.HouseEnd + 1f; x += 0.1f)
            {
                // Only lines that start well inside the campus (north: the ring turns inward beside the left pier).
                var start = f.P(x, -6f);
                if (!ring.Contains(start) || Near(ring, start, 0.6f)) continue;
                var blocked = false;
                for (var z = -6f; z <= 0.4f && !blocked; z += 0.05f)
                    blocked = colliders.Any(c => c.Tag == SurfaceTag.Boundary && Covers(c, f.P(x, z), 0.2f));
                Require(blocked, $"{spec.Name}: a walker at local x {x:F2} can leave through the gate");
            }
            // The wings are walled too, a few metres along.
            foreach (var wing in g.Wings)
            {
                var mid = (wing.Line[0] + wing.Line[1]) * 0.5f;
                Require(colliders.Any(c => c.Tag == SurfaceTag.Boundary && Covers(c, mid, 0f)), $"{spec.Name} wing at {mid} has a collider");
            }
        }
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
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json", "overlay/boundary.overlay.json" }
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
