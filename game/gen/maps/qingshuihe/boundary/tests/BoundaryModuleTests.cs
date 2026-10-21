using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using Campus.Gen.Sites;
using Campus.Gen.Sites.Tests;
using Campus.Gen.Maps.Qingshuihe.South;
using Campus.Tests;
using Campus.Gen.Maps.Qingshuihe.Boundary.Framework;
using Campus.Gen.Maps.Qingshuihe.Boundary.Gates;
using Campus.Gen.Maps.Qingshuihe.Boundary.Ring;
using Campus.Gen.Maps.Qingshuihe.Boundary.Waterfront;
using GdUnit4;
using G = Campus.Gen.Maps.Qingshuihe.Boundary.BoundaryTune.Gate;
using P = Campus.Gen.Maps.Qingshuihe.Boundary.BoundaryTune.Plaza;

namespace Campus.Gen.Maps.Qingshuihe.Boundary.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class BoundaryModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WorldRecordsRegisterTheForecourtAndThePlaza()
    {
        var world = RealMapFixture.Load().World;
        AssertThat(world.Features.ContainsKey(new(BoundaryFeatureIds.NorthForecourt))).OverrideFailureMessage("north forecourt registered").IsTrue();
        AssertThat(world.Features.ContainsKey(new(BoundaryFeatureIds.NorthPlaza))).OverrideFailureMessage("A05 plaza registered").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void GatesSitWhereTheirRoadsLeaveTheCampus()
    {
        var world = RealMapFixture.Load().World;
        var ring = new BoundaryRing(world.Boundary.Outer);
        foreach (var spec in BoundaryTune.Gates)
        {
            var g = GateSite.Of(world, spec) ?? throw new InvalidOperationException($"{spec.Name} not resolved");
            AssertThat(!ring.Contains(g.Frame.P(0, 2f)) && ring.Contains(g.Frame.P(0, -2f))).OverrideFailureMessage($"{spec.Name} +Z leaves the campus").IsTrue();
            AssertThat(g.OpeningLength > 2 * g.Carriage && g.OpeningLength < 40f).OverrideFailureMessage($"{spec.Name} opening {g.OpeningLength:F1} m").IsTrue();
            var origin = ring.Arc(g.Frame.Origin);
            AssertThat(ring.Forward(g.OpeningStart, origin) < g.OpeningLength).OverrideFailureMessage($"{spec.Name} opening holds the gate road").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void TheFenceRunsAllTheWayRound()
    {
        var world = RealMapFixture.Load().World;
        var plan = BoundaryPlan.Of(world)!;
        var ring = plan.Ring;
        // Openings tile the ring with runs between them: nothing overlaps, every gap is a run.
        var covered = 0f;
        for (var i = 0; i < plan.Openings.Count; i++)
        {
            var o = plan.Openings[i];
            var next = plan.Openings[(i + 1) % plan.Openings.Count];
            var gap = ring.Forward(ring.Fold(o.Start + o.Length), next.Start);
            AssertThat(gap < ring.Perimeter - 1f && gap >= 0.3f).OverrideFailureMessage($"opening {o.Key} overlaps or touches {next.Key} (gap {gap:F2} m)").IsTrue();
            covered += o.Length;
        }
        AssertThat(covered < ring.Perimeter * 0.05f).OverrideFailureMessage($"openings take {covered:F0} m of {ring.Perimeter:F0} m").IsTrue();
        AssertThat(plan.Openings.Count(o => o.Kind == OpeningKind.Gate) == BoundaryTune.Gates.Count(spec => GateSite.Of(world, spec) is not null) + (world.Features.ContainsKey(new(SouthIds.Gate.Entity)) ? 1 : 0)).OverrideFailureMessage("each authored gate has an opening").IsTrue();
        foreach (var run in plan.Runs)
        {
            foreach (var p in run.Line)
                AssertThat(ring.Contains(p) || Near(ring, p, 0.35f)).OverrideFailureMessage($"fence point {p} stays inside the ring").IsTrue();
            var posts = run.Posts();
            for (var i = 0; i + 1 < posts.Count; i++)
                AssertThat(Vector2.Distance(posts[i].At, posts[i + 1].At) <= BoundaryTune.Fence.PillarPitch + TestTolerance.Position).OverrideFailureMessage($"pillar pitch at {posts[i].At}").IsTrue();
            foreach (var hedge in run.Hedges)
                foreach (var p in hedge) AssertThat(ring.Contains(p)).OverrideFailureMessage($"hedge point {p} inside the campus").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryRoadAndChannelThroughTheRingMeetsAnOpening()
    {
        var world = RealMapFixture.Load().World;
        var plan = BoundaryPlan.Of(world)!;
        var ring = plan.Ring;
        bool Open(float s) => plan.Openings.Any(o => ring.Forward(o.Start, s) <= o.Length);
        var detours = BoundaryTune.Detours.Select(d => d.Road).ToHashSet();
        foreach (var road in world.Network.Roads.Where(r => !detours.Contains(r.Id.Value)))
            for (var k = 0; k + 1 < road.Line.Points.Count; k++)
                foreach (var s in ring.Crossings(road.Line.Points[k], road.Line.Points[k + 1]))
                    AssertThat(Open(s)).OverrideFailureMessage($"road {road.Id.Value} crosses the ring at {ring.At(s)} through the fence").IsTrue();
        foreach (var water in world.Features.Values.Where(f => f.Kind.Value == "water" && (f.Geometry is PolygonGeometry or MultiPolygonGeometry)))
            foreach (var outer in water.Polygons().Select(p => p.Outer))
                for (var i = 0; i < outer.Count; i++)
                    foreach (var s in ring.Crossings(outer[i], outer[(i + 1) % outer.Count]))
                        AssertThat(plan.Openings.Any(o => o.Kind == OpeningKind.Grate && ring.Forward(o.Start, s) <= o.Length)).OverrideFailureMessage($"water {water.Id.Value} crosses the ring at {ring.At(s)} without a grate").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void NothingWalksOutThroughAGate()
    {
        var ctx = Context();
        var colliders = ZoneTestCells.Colliders(new BoundaryModule(), ctx).ToArray();
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
                AssertThat(blocked).OverrideFailureMessage($"{spec.Name}: a walker at local x {x:F2} can leave through the gate").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void NothingWalksOutAnywhereAlongTheRing()
    {
        var ctx = Context();
        var plan = BoundaryPlan.Of(ctx)!;
        var ring = plan.Ring;
        var colliders = ZoneTestCells.Colliders(new BoundaryModule(), ctx).Where(c => c.Tag == SurfaceTag.Boundary).ToArray();
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
            // Gates are walked by NothingWalksOutThroughAGate (the south gate by game/gen/maps/qingshuihe/south/'s own test).
            if (gates.Any(o => ring.Forward(o.Start - 1.5f, s) <= o.Length + 3f)) continue;
            // Detours leave the corner behind the fence; their line is a run like any other.
            if (plan.Detours.Any(d => ring.Forward(d.Start - 1f, s) <= d.Length + 2f)) continue;
            var p = ring.At(s);
            var n = ring.InwardAt(s);
            var blocked = false;
            for (var d = 2.5f; d >= -0.4f && !blocked; d -= 0.05f) blocked = Hit(p + n * d);
            if (!blocked) misses.Add($"{p}");
        }
        AssertThat(misses.Count == 0).OverrideFailureMessage($"{misses.Count} places along the ring can be walked through, first {string.Join(", ", misses.Take(5))}").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void CollidersAreTaggedAndCarryAFootMaterial()
    {
        var ctx = Context();
        var colliders = ZoneTestCells.Colliders(new BoundaryModule(), ctx).ToArray();
        AssertThat(colliders.Length > 0).OverrideFailureMessage("colliders exist").IsTrue();
        AssertThat(colliders.All(c => c.Foot != FootMat.Auto)).OverrideFailureMessage("every collider fills Foot").IsTrue();
        var rail = NorthPlazaSite.FrameOf(ctx.World)!.Value.P((P.RailT0(ctx.World) + P.RailT1) * 0.5f, P.RailN(ctx.World));
        AssertThat(colliders.Any(c => c.Tag == SurfaceTag.Barrier && Covers(c, rail, 0f))).OverrideFailureMessage("the waterside railing is an ordinary barrier").IsTrue();
        AssertThat(colliders.Where(c => c is WallCollider { Tag: SurfaceTag.Barrier }).All(c => ((WallCollider)c).Y1 >= WalkRules.BarrierHeight)).OverrideFailureMessage("barrier tops reach 1.5 m").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void PlazaBenchesStayOnThePavingBehindTheRailing()
    {
        var world = RealMapFixture.Load().World;
        var frame = NorthPlazaSite.FrameOf(world)!.Value;
        var paving = world.Feature(BoundaryFeatureIds.NorthPlaza).Outer();
        foreach (var bench in P.Benches)
        {
            AssertThat(PlanRings.PointInRing(frame.P(bench.X, bench.Y), paving)).OverrideFailureMessage("bench inside the plaza").IsTrue();
            AssertThat(bench.Y > P.RailN(world)).OverrideFailureMessage("bench behind the railing").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryLevelIsValid()
    {
        var ctx = Context();
        var module = new BoundaryModule();
        AssertThat(module.Tiles(ctx).Any()).OverrideFailureMessage("the module occupies cells").IsTrue();
        foreach (var lod in Levels)
        {
            var items = ZoneTestCells.Items(module, ctx, lod);
            AssertThat(items.Count > 0).OverrideFailureMessage($"{lod} has items").IsTrue();
            var cells = items.Select(i => TileGrid.At(i.Anchor, lod)).Distinct().ToArray();
            var parts = 0;
            foreach (var tile in lod == LodLevel.L4 ? [new TileKey(0, 0)] : cells)
            {
                var output = module.Build(ctx, tile, lod);
                foreach (var mesh in output.Meshes)
                {
                    mesh.ValidateVertexData();
                    AssertThat(mesh.Position.Length / 3 <= 65536).OverrideFailureMessage("bundle vertex limit").IsTrue();
                }
                parts += output.Parts.Count;
            }
            AssertThat(items.Count(i => i.Part is not null) == parts).OverrideFailureMessage($"{lod}: every placed part is emitted exactly once").IsTrue();
        }
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory("Slow")]
    [TestCategory(TestReason.Contract)]
    public void SameInputSameBytes()
    {
        var a = Bytes(Context());
        var b = Bytes(Context());
        AssertThat(a.Length > 0 && a.AsSpan().SequenceEqual(b)).OverrideFailureMessage("two builds give the same bytes").IsTrue();
    }

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
        WallCollider wall => MathF.Sqrt(PlanLines.SegmentDistanceSquared(p, wall.A, wall.B)) <= wall.HalfT + slack,
        _ => false,
    };

    private static bool InBox(BoxCollider box, Vector2 p, float slack)
    {
        var ux = new Vector2(box.At.M11, box.At.M13);
        var uz = new Vector2(box.At.M31, box.At.M33);
        var d = p - new Vector2(box.At.M41, box.At.M43);
        return MathF.Abs(Vector2.Dot(d, ux)) <= box.Half.X + slack && MathF.Abs(Vector2.Dot(d, uz)) <= box.Half.Z + slack;
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

    private static BuildContext Context() => new() { CodeHash = "fixture", WindowNumbering = new(new MapId("fixture"), new string('0', 64), []), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = RealMapFixture.Load().World, Map = null!, WorldSeed = 1 };

}
