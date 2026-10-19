using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using Campus.Gen.Geometry;
using Campus.Gen.World;
using Campus.Gen.Zones.Common;
using Campus.Gen.Zones.Common.Tests;
using Campus.Gen.Zones.North.Clusters;
using GdUnit4;

namespace Campus.Gen.Zones.North.Tests;

using static GdUnit4.Assertions;
using static Campus.Gen.Zones.North.Tests.NorthTestSupport;

[TestSuite]
public sealed class NorthModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryClusterHasItsPolygonAndGateAddition()
    {
        var world = RealMapFixture.Load().World;
        foreach (var cluster in NorthTune.Clusters)
        {
            AssertThat(world.Features.ContainsKey(new(cluster.Id))).OverrideFailureMessage($"{cluster.Key} polygon exists").IsTrue();
            AssertThat(world.Features.TryGetValue(new(NorthFeatureIds.GatePrefix + cluster.Key), out var gate) && gate.Geometry is PointGeometry).OverrideFailureMessage($"{cluster.Key} gate addition is registered").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryClusterGetsAMainGateAndAClosedFence()
    {
        var world = RealMapFixture.Load().World;
        foreach (var cluster in NorthTune.Clusters)
        {
            var layout = ClusterLayout.Of(world, cluster) ?? throw new InvalidOperationException($"{cluster.Key} has no layout");
            var open = world.Features[new(NorthFeatureIds.GatePrefix + cluster.Key)].Props is { ValueKind: JsonValueKind.Object } gateProps
                && gateProps.TryGetProperty("open", out var openFlag) && openFlag.ValueKind == JsonValueKind.True;
            AssertThat(layout.Gates.Count(g => g.Main) == 1).OverrideFailureMessage($"{cluster.Key} has one main gate").IsTrue();
            AssertThat(layout.Gates.Count(g => g.Open) == (open ? 1 : 0)).OverrideFailureMessage($"{cluster.Key} open gates").IsTrue();
            AssertThat(layout.Runs.Count > 0).OverrideFailureMessage($"{cluster.Key} has fence runs").IsTrue();
            foreach (var run in layout.Runs)
                for (var i = 0; i + 1 < run.Posts.Count; i++)
                    AssertThat(Vector2.Distance(run.Posts[i], run.Posts[i + 1]) <= NorthTune.Fence.PostPitch + TestTolerance.Position).OverrideFailureMessage($"{cluster.Key} post gap at {run.Posts[i]}").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void OpenGateLeavesPassagesAndClosedGatesBarTheLine()
    {
        var ctx = Context();
        var module = new NorthModule();
        var colliders = ZoneTestCells.Colliders(module, ctx).ToArray();
        foreach (var cluster in NorthTune.Clusters)
        {
            var layout = ClusterLayout.Of(ctx.World, cluster)!;
            foreach (var gate in layout.Gates.Where(g => g.Kind == GateKind.Vehicle))
            {
                var f = gate.Frame;
                var cabinets = gate.Cabinets().ToArray();
                for (var k = 0; k + 1 < cabinets.Length; k++)
                {
                    var middle = f.P((cabinets[k] + cabinets[k + 1]) * 0.5f, 0);
                    var blocked = colliders.Any(c => Covers(c, middle, 0f));
                    AssertThat(blocked != gate.Open).OverrideFailureMessage($"{cluster.Key} gate passage {k} is {(gate.Open ? "open" : "closed")}").IsTrue();
                }
                // The lane under the barrier arm is closed either way, and nothing crosses the gate line above 0.5 m wide.
                AssertThat(colliders.Any(c => Covers(c, f.P(0, 0), 0f))).OverrideFailureMessage($"{cluster.Key} vehicle lane is barred").IsTrue();
                var tops = colliders.OfType<BoxCollider>().Where(c => Covers(c, f.P(cabinets[0], 0), 0f));
                AssertThat(tops.All(b => b.At.M42 + b.Half.Y >= WalkRules.BarrierHeight - TestTolerance.Position)).OverrideFailureMessage($"{cluster.Key} cabinets reach 1.5 m").IsTrue();
            }
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void EveryLevelIsValidAndEveryItemLandsInOneCell()
    {
        var ctx = Context();
        var module = new NorthModule();
        AssertThat(module.Tiles(ctx).Any()).OverrideFailureMessage("the module occupies cells").IsTrue();
        foreach (var lod in Levels)
        {
            var items = ZoneTestCells.Items(module, ctx, lod);
            AssertThat(items.Count > 0).OverrideFailureMessage($"{lod} has items").IsTrue();
            var cells = items.Select(i => TileGrid.At(i.Anchor, lod)).Distinct().ToArray();
            var meshes = 0;
            var parts = 0;
            foreach (var (x, z) in cells)
            {
                var output = module.Build(ctx, new(x, z), lod);
                foreach (var mesh in output.Meshes)
                {
                    mesh.ValidateVertexData();
                    AssertThat(mesh.Position.Length / 3 <= 65536).OverrideFailureMessage("bundle vertex limit").IsTrue();
                    meshes++;
                }
                parts += output.Parts.Count;
            }
            AssertThat(meshes > 0).OverrideFailureMessage($"{lod} emits meshes").IsTrue();
            AssertThat(items.Count(i => i.Part is not null) == parts).OverrideFailureMessage($"{lod}: every placed part is emitted exactly once").IsTrue();
            string[] entranceSet = [Campus.Contracts.Parts.PartCatalog.EntranceMat, Campus.Contracts.Parts.PartCatalog.OpenSign, Campus.Contracts.Parts.PartCatalog.EntranceLamp];
            AssertThat(!items.Any(i => i.Part is { } p && entranceSet.Contains(p.Part.Value))).OverrideFailureMessage($"{lod}: the open-entrance set is WP-13's, not placed by the zone").IsTrue();
        }
    }

    [TestCase, TestCategory(TestTag.Determinism)]
    [TestCategory("Slow")]
    [TestCategory(TestReason.Contract)]
    public void BuildIsDeterministic()
    {
        var ctx = Context();
        var cells = ZoneTestCells.Heaviest(new NorthModule(NorthSites.All.ToArray()), ctx);
        var a = Bytes(ctx, cells);
        var b = Bytes(Context(), cells);
        AssertThat(a.SequenceEqual(b)).OverrideFailureMessage("two worlds give the same bytes").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void SitesStayInsideTheNorthZone()
    {
        var ctx = Context();
        var module = new NorthModule();
        var sourcePoints = NorthTune.Clusters.SelectMany(cluster => ctx.World.Feature(cluster.Id).Contour(facade: false))
            .Concat(ctx.World.ContourSegment(Campus.Gen.Zones.DesignBoundary.BoundaryFeatureIds.NorthBankContour)).ToArray();
        var min = sourcePoints.Aggregate(Vector2.Min) - new Vector2(TileGrid.BaseEdge);
        var max = sourcePoints.Aggregate(Vector2.Max) + new Vector2(TileGrid.BaseEdge);
        foreach (var item in ZoneTestCells.Items(module, ctx, LodLevel.L0))
            AssertThat(item.Anchor.X >= min.X && item.Anchor.X <= max.X && item.Anchor.Y >= min.Y && item.Anchor.Y <= max.Y)
                .OverrideFailureMessage($"item at {item.Anchor} is within the north sources' cells").IsTrue();
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

    private static byte[] Bytes(BuildContext ctx, IEnumerable<TileKey> cells)
    {
        var module = new NorthModule(NorthSites.All.ToArray());
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        foreach (var tile in cells)
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
}
