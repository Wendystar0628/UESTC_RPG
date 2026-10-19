using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Audio;
using Campus.Gen.World;
using Campus.Tests;
using Campus.Gen.Zones.DesignWaterfront.Bridges;
using Campus.Gen.Zones.DesignWaterfront.Banks;
using Campus.Gen.Zones.Common;
using GdUnit4;
using T = Campus.Gen.Zones.DesignWaterfront.WaterfrontTune;

namespace Campus.Gen.Zones.DesignWaterfront.Tests;

using static GdUnit4.Assertions;

[TestSuite]
public sealed class WaterfrontModuleTests
{
    private static readonly LodLevel[] Levels = [LodLevel.L0, LodLevel.L1, LodLevel.L2, LodLevel.L3, LodLevel.L4];
    private static readonly Lazy<IWorldModel> Loaded = new(() => RealMapFixture.Load().World);

    [TestCase, TestCategory(TestReason.Contract)]
    public void OverlayRegistersTheBridgesAndKeepsTheNetwork()
    {
        var world = World();
        var plans = BridgePlan.All(world);
        var registrations = world.Features.Values.Where(f => f.Kind.Value == T.BridgeKind).OrderBy(f => f.Id.Value, StringComparer.Ordinal).ToArray();
        AssertThat(plans.Count > 0 && plans.Select(b => b.Id).SequenceEqual(registrations.Select(f => f.Id))).IsTrue();
        AssertThat(BridgeStone.All(world).Count == world.Features.Values.Count(f => f.Kind.Value == T.StoneKind)).IsTrue();
        foreach (var registration in registrations)
        {
            var expected = new[] { registration.Text("axis") }.Concat(
                registration.RequiredProps().TryGetProperty("walks", out var walks)
                    ? walks.EnumerateArray().Select(w => w.GetString()!) : []).ToArray();
            foreach (var id in expected)
                AssertThat(world.Network.Bridges.Any(line => line.Id.Value == id)).OverrideFailureMessage($"{id} stays in the network").IsTrue();
            var plan = plans.Single(b => b.Id == registration.Id);
            AssertThat(plan.Lines.Select(line => line.Value).SequenceEqual(expected)).IsTrue();
            AssertThat(plan.Bands.Count(b => b.Kind == BandKind.Carriage) == (plan.HasCarriage ? 1 : 0)).IsTrue();
            AssertThat(plan.Bands.Any(b => b.Kind == BandKind.Walk)).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void DeckSpansTheWaterOnTwoAbutments()
    {
        var world = World();
        foreach (var plan in BridgePlan.All(world))
        {
            AssertThat(plan.Abutments.Count == 2).OverrideFailureMessage($"{plan.Id.Value} crosses water and stands on two abutments").IsTrue();
            foreach (var id in plan.Lines)
            {
                var line = world.Network.Bridges.Single(l => l.Id == id);
                var ends = new[] { line.Line.Points[0], line.Line.Points[^1] }.Select(p => Local(plan, p)).OrderBy(p => p.S).ToArray();
                AssertThat(ends.All(e => e.U >= plan.Left - TestTolerance.Position && e.U <= plan.Right + TestTolerance.Position)).OverrideFailureMessage($"{id} lies within the walking width").IsTrue();
                // The deck is fitted to the water, not to the mapped line ends; it stays of the line's own order of size.
                var length = plan.EndS(ends[0].U) - plan.StartS(ends[0].U);
                AssertThat(length > 3f && length < ends[1].S - ends[0].S + 6f).OverrideFailureMessage($"{plan.Id.Value} deck length {length} along {id}").IsTrue();
            }
            // Both abutments stand under the deck, between its ends.
            foreach (var a in plan.Abutments)
                foreach (var c in new[] { a.A, a.B, a.C, a.D })
                {
                    var (s, u) = Local(plan, c);
                    AssertThat(s >= plan.StartS(u) - TestTolerance.Position && s <= plan.EndS(u) + TestTolerance.Position).OverrideFailureMessage($"{plan.Id.Value} abutment corner {c} lies under the deck").IsTrue();
                }
        }
    }

    private static (float S, float U) Local(BridgePlan plan, Vector2 p)
    {
        var d = p - plan.Origin;
        return (Vector2.Dot(d, plan.Axis), Vector2.Dot(d, plan.Normal));
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void BuildsEveryLevelValidAndDeterministic()
    {
        var ctx = Context();
        var module = new WaterfrontModule();
        var tiles = module.Tiles(ctx).ToArray();
        AssertThat(tiles.Length > 0).OverrideFailureMessage("the module occupies cells").IsTrue();
        foreach (var lod in Levels)
        {
            var keys = lod == LodLevel.L4 ? [new TileKey(0, 0)] : tiles.Select(t => new TileKey(t.X >> (int)lod, t.Z >> (int)lod)).Distinct().ToArray();
            foreach (var tile in keys)
            {
                var output = module.Build(ctx, tile, lod);
                foreach (var mesh in output.Meshes) mesh.ValidateVertexData();
                if (lod == LodLevel.L4) AssertThat(output.Meshes.Count == 0).OverrideFailureMessage("nothing at L4").IsTrue();
            }
        }
        var first = module.Build(ctx, tiles[0], LodLevel.L0);
        var again = module.Build(Context(), tiles[0], LodLevel.L0);
        AssertThat(first.Meshes.Count == again.Meshes.Count && first.Meshes.Zip(again.Meshes).All(p => p.First.Position.SequenceEqual(p.Second.Position))).OverrideFailureMessage("deterministic").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void DeckWalksAndParapetsBlock()
    {
        var ctx = Context();
        var module = new WaterfrontModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        var plans = BridgePlan.All(ctx.World);
        AssertThat(colliders.Length == module.CollidersFor(ctx).Count).OverrideFailureMessage("every collider is filed in exactly one cell").IsTrue();
        foreach (var plan in plans)
        {
            var bridge = BridgeColliders.Of(plan).Select(item => item.Collider).ToArray();
            AssertThat(bridge.All(colliders.Contains)).OverrideFailureMessage("all bridge colliders are published").IsTrue();
            var ramps = bridge.OfType<RampCollider>().ToArray();
            AssertThat(ramps.Length > 0 && ramps.All(r => r.Tag == SurfaceTag.Ground && r.Foot != FootMat.Auto)).OverrideFailureMessage($"{plan.Id.Value}: walkable deck ramps with a foot material").IsTrue();
            AssertThat(ramps.All(r => Vector3.Distance(r.P0, r.P3) <= T.RampStep + TestTolerance.Position)).OverrideFailureMessage("ramps are cut every 2 m").IsTrue();
            AssertThat(ramps.All(r => MathF.Abs(r.P0.Y - plan.RoadY) < TestTolerance.Position || MathF.Abs(r.P0.Y - plan.WalkY) < TestTolerance.Position)).OverrideFailureMessage("ramp tops at the carriageway or walking level").IsTrue();
            var walls = bridge.OfType<WallCollider>().ToArray();
            AssertThat(walls.Length == 2 && walls.All(w => w.Tag == SurfaceTag.Barrier && w.Y1 >= plan.WalkY + WalkRules.BarrierHeight - TestTolerance.Position && w.HalfT <= 0.3f)).OverrideFailureMessage($"{plan.Id.Value}: two parapets, top at least 1.5 m above the walk").IsTrue();
        }
        foreach (var stone in BridgeStone.All(ctx.World))
            AssertThat(colliders.Count(collider => collider == stone.Collider) == 1).OverrideFailureMessage("each stone is one box").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void RegisteredBankUsesTheCompiledShoreAndOpensAtItsWaterAccessSteps()
    {
        var world = World();
        var bank = BankPlan.All(world).Single(plan => plan.Id.Value == T.BankSampleId);
        var registration = world.Feature(T.BankSampleId);
        var (a, b) = registration.PointPair();
        AssertThat(new[] { a, b }.All(point => PlanLines.Project(bank.Shore, point).Distance <= T.BankEndpointTolerance)).IsTrue();
        var source = (ITerrainBankSource)world.Terrain;
        AssertThat(source.Banks.Contains(bank.Span) && bank.Span.Kind == TerrainBankKind.Hard).IsTrue();
        AssertThat(bank.WaterY == world.Terrain.WaterY(new(registration.Text("water")))).IsTrue();
        foreach (var exit in bank.Exits)
        {
            var lowTread = exit.BaseY + exit.Steps.Height / exit.Steps.Steps;
            AssertThat(MathF.Abs(lowTread - (bank.WaterY - T.BankSubmersion)) < TestTolerance.Position).IsTrue();
            AssertThat(MathF.Abs(exit.BaseY + exit.Steps.Height - bank.TopAt(exit.Station)) < TestTolerance.Position).IsTrue();
            AssertThat(bank.Pieces().All(piece =>
                PlanLines.SegmentDistanceSquared(exit.Frame.Origin, piece.A, piece.B) >= MathF.Pow(exit.Steps.Width / 2 - TestTolerance.Position, 2))).IsTrue();
        }
    }

    private static BuildContext Context() => new() { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World(), Map = null!, WorldSeed = 1u };

    private static IWorldModel World() => Loaded.Value;
}
