using Campus.Gen.Geometry;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Sites;
using Campus.Tests;
using Campus.Gen.World;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Framework;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Pinxue;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Research;
using Campus.Gen.Maps.Qingshuihe.CenterEast.Wenke;
using Campus.Gen.Tests.Support;
using GdUnit4;
using M = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Monuments;
using P = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Pinxue;
using R = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Research;
using F = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Friendship;
using K = Campus.Gen.Maps.Qingshuihe.CenterEast.CenterEastTune.Wenke;

namespace Campus.Gen.Maps.Qingshuihe.CenterEast.Tests;
using static GdUnit4.Assertions;

public sealed partial class CenterEastModuleTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void PassageAndBridgesKeepTheirClearHeights()
    {
        var ctx = Context();
        var module = new CenterEastModule();
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        var gate = PinxueFrameSite.GateOf(ctx.World);
        var baseY = PinxueFrameSite.BaseY(ctx);
        AssertThat(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto)).OverrideFailureMessage("every collider fills its foot material").IsTrue();
        var boxes = colliders.OfType<BoxCollider>().Where(b => b.Tag == SurfaceTag.Wall && b.At.M42 - b.Half.Y > baseY + 1f).ToArray();
        AssertThat(boxes.Length == PinxueFrameSite.Bridges(ctx.World).Count).OverrideFailureMessage($"the beam and the two bridges are lifted boxes, got {boxes.Length}").IsTrue();
        AssertThat(boxes.All(b => b.At.M42 - b.Half.Y >= baseY + gate.BridgeBottom - TestTolerance.Position)).OverrideFailureMessage("nothing walls off the passage or the paths under the bridges").IsTrue();
        var frame = gate.F.P(gate.Axis, -gate.Depth * 0.5f);
        var columns = colliders.OfType<BoxCollider>().Count(b => b.Tag == SurfaceTag.Wall && b.At.M42 - b.Half.Y < baseY + TestTolerance.Position && b.Half.Y * 2 >= gate.BeamBottom - TestTolerance.Position
            && Vector2.Distance(new(b.At.M41, b.At.M43), frame) < 25f);
        AssertThat(columns == P.ColumnX.Length).OverrideFailureMessage("the four columns reach the beam soffit").IsTrue();
        // The beam leaves the two-storey passage open between the columns.
        var beam = boxes.OrderByDescending(b => b.At.M42).First();
        AssertThat(MathF.Abs(beam.At.M42 - beam.Half.Y - baseY - gate.BeamBottom) < TestTolerance.Position).OverrideFailureMessage("the beam soffit sits at two storeys above its terrain pad").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void RampWallsStepWithTheGround()
    {
        var world = World();
        var frame = PinxueFrameSite.GateOf(world).F;
        var walls = PinxueForecourtSite.RampWalls(world);
        // Flat stub: each wall is one run at seat height.
        foreach (var (x, z0, z1) in walls)
        {
            var runs = PinxueForecourtSite.RampRuns(_ => 0f, frame, x, z0, z1);
            AssertThat(runs.Count == 1 && MathF.Abs(runs[0].Top - P.SeatHeight) < TestTolerance.Position).OverrideFailureMessage("flat ground gives one continuous wall").IsTrue();
        }
        // Controlled monotone ground: verify coverage and seat height against the registered wall span.
        const float slope = 0.05f;
        var wall = walls[0];
        float Ramp(Vector2 p) => (wall.Z1 - frame.Local(p).Y) * slope;
        var stepped = PinxueForecourtSite.RampRuns(Ramp, frame, wall.X, wall.Z0, wall.Z1);
        AssertThat(stepped.Count > 1 && stepped.All(run => run.Z1 > run.Z0 && run.Z1 - run.Z0 <= P.RampWallSegment + TestTolerance.Position))
            .OverrideFailureMessage("sloping ground has positive runs within the segment length").IsTrue();
        AssertThat(MathF.Abs(stepped[0].Z0 - wall.Z0) < TestTolerance.Position && MathF.Abs(stepped[^1].Z1 - wall.Z1) < TestTolerance.Position)
            .OverrideFailureMessage("runs cover the registered wall span within a centimetre").IsTrue();
        foreach (var run in stepped)
        {
            var uphillGround = Ramp(frame.P(wall.X, run.Z0));
            AssertThat(MathF.Abs(run.Top - uphillGround - P.SeatHeight) < TestTolerance.Position)
                .OverrideFailureMessage("every seat top follows the uphill ground plus seat height").IsTrue();
        }
        for (var i = 1; i < stepped.Count; i++)
        {
            AssertThat(MathF.Abs(stepped[i - 1].Z1 - stepped[i].Z0) < 5 * QuantizedPlan.Tolerance)
                .OverrideFailureMessage("neighbouring runs meet without a gap").IsTrue();
            var expectedDrop = (stepped[i].Z0 - stepped[i - 1].Z0) * slope;
            AssertThat(MathF.Abs(stepped[i - 1].Top - stepped[i].Top - expectedDrop) < TestTolerance.Position)
                .OverrideFailureMessage("each step follows the ground fall across its actual span").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void MonumentsStandAtTheirPointsAndLeaveTheDoorsClear()
    {
        var world = World();
        Vector2 Point(string id) => world.Feature(id).Point();
        // The authored record is the monument placement authority.
        AssertThat(Vector2.Distance(Point(CenterEastIds.Layout.LinPlacement), StatueLawnSite.LinFrame(world).Origin) <= TestTolerance.Position).OverrideFailureMessage($"{CenterEastIds.Monuments.Lin} follows its authored point").IsTrue();
        AssertThat(StatueLawnSite.Island(world) is { } island && PlanRings.InsideByCrossings(island, StatueLawnSite.LinFrame(world).Origin)).OverrideFailureMessage("林为干像 stands on the plaza's tree island").IsTrue();
        var maxwellAt = StatueLawnSite.MaxwellFrame(world).Origin;
        var stoneAt = StatueLawnSite.StoneFrame(world).Origin;
        foreach (var at in new[] { maxwellAt, stoneAt })
            AssertThat(PlanRings.InsideByCrossings(StatueLawnSite.ApronRing(world), at)).OverrideFailureMessage($"{at} stands on the apron").IsTrue();
        // Every inscription line lies on the dressed face with stone round it.
        foreach (var (x, y, length) in StatueLawnSite.StoneLines())
            foreach (var (fx, fy) in new[] { (x - length * 0.5f, y), (x + length * 0.5f, y), (x - length * 0.5f, y + M.StoneLineHeight), (x + length * 0.5f, y + M.StoneLineHeight) })
                AssertThat(M.Stone.OnDressedFace(fx, fy, 0.04f)).OverrideFailureMessage($"inscription corner ({fx}, {fy}) is on the dressed face").IsTrue();
        // Colliders: filled foot, a top at every pedestal, and nothing in front of the three research doors.
        var ctx = Context();
        var module = new CenterEastModule([new StatueLawnSite(), new ResearchSteleSite()]);
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        AssertThat(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto)).OverrideFailureMessage("every monument collider fills its foot material").IsTrue();
        var boxes = colliders.OfType<BoxCollider>().ToArray();
        foreach (var (at, top) in new[] { (StatueLawnSite.LinFrame(world).Origin, StatueLawnSite.LinPedestalTop), (maxwellAt, M.MaxwellPedestalTop), (stoneAt, M.Stone.Height - 0.08f) })
            AssertThat(boxes.Any(b => Vector2.Distance(new(b.At.M41, b.At.M43), at) < 0.6f && MathF.Abs(b.At.M42 + b.Half.Y - top) < TestTolerance.Layout)).OverrideFailureMessage($"the box at {at} reaches its top {top}").IsTrue();
        var doors = new[] { world.Feature(CenterEastIds.Layout.KeADoor1).Point(), world.Feature(CenterEastIds.Layout.KeBDoor1).Point(), new KeCSite().HallApex(new KeCSite().Footprints(world)).Apex };
        var clear = 2.5f + R.PortalDepth + 3 * R.TreadDepth;
        foreach (var b in boxes)
            AssertThat(doors.All(d => Vector2.Distance(new(b.At.M41, b.At.M43), d) > clear + b.Half.X + b.Half.Z)).OverrideFailureMessage("monuments keep 2.5 m clear outside the doors and their steps").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [TestCategory("Slow")]
    public void WenkeStandsTerracedWithOneShutDoorAndAClearForecourt()
    {
        var ctx = Context();
        var plan = WenkeSite.Footprints(ctx);
        static float Area(IReadOnlyList<Vector2> ring) => MathF.Abs((float)PlanRings.SignedArea(ring));
        WenkeSite.Block Block(string part) => plan.Blocks.Single(b => b.Part == part);
        AssertThat(Block(CenterEastIds.Wenke.Tower).Storeys == ctx.World.Feature(CenterEastIds.Wenke.Entity).Part(CenterEastIds.Wenke.Tower).Levels().Storeys).OverrideFailureMessage("the tower stands fifteen storeys, about 60 m").IsTrue();
        // The wings step back toward their tips: each top storey is smaller than its ground storey, with planted terraces between.
        foreach (var part in new[] { CenterEastIds.Wenke.South, CenterEastIds.Wenke.West, CenterEastIds.Wenke.North })
        {
            var block = Block(part);
            AssertThat(Area(block.Outlines[^1]) < 0.8f * Area(block.Outlines[0])).OverrideFailureMessage($"{part} steps back toward its tip").IsTrue();
            AssertThat(Enumerable.Range(1, block.Storeys - 1).Any(k => WenkeSite.Terraces(plan, block, k).Any())).OverrideFailureMessage($"{part} has terraces").IsTrue();
        }
        AssertThat(Block(CenterEastIds.Wenke.Tower).Rows.Skip(K.PanelStripFrom).All(r => r.Any(c => c.Kind == WenkeSite.CellKind.Panel))).OverrideFailureMessage("the yellow-green strip runs up the tower").IsTrue();
        AssertThat(plan.Blocks.All(b => b.Rows.All(r => r.Count(c => c.Kind == WenkeSite.CellKind.Window) > 0))).OverrideFailureMessage("every storey of every block shows glass").IsTrue();
        // One shut entrance at the atrium porch; nothing but paving within 2.5 m outside it.
        var module = new CenterEastModule([new WenkeSite(), new WenkeForecourtSite()]);
        var colliders = module.Tiles(ctx).SelectMany(t => module.Collide(ctx, t)).ToArray();
        AssertThat(colliders.All(c => c.Foot != Contracts.Audio.FootMat.Auto)).OverrideFailureMessage("every 文科楼 collider fills its foot material").IsTrue();
        var shut = colliders.OfType<WallCollider>().Where(w => w.Tag == SurfaceTag.DoorClosed).ToArray();
        AssertThat(shut.Length == 1 && Vector2.Distance((shut[0].A + shut[0].B) * 0.5f, WenkeSite.Door(ctx.World).Origin) < K.PorchDepth + 0.5f).OverrideFailureMessage("one shut glass entrance at the atrium porch").IsTrue();
        var outside = WenkeSite.Door(ctx.World).P(0, K.PorchDepth);
        foreach (var box in colliders.OfType<BoxCollider>().Where(b => b.Tag == SurfaceTag.Prop))
            AssertThat(Vector2.Distance(new(box.At.M41, box.At.M43), outside) > 2.5f + box.Half.X + box.Half.Z).OverrideFailureMessage("nothing stands within 2.5 m outside the door").IsTrue();
        foreach (var post in colliders.OfType<PostCollider>())
            AssertThat(Vector2.Distance(post.C, outside) > 2.5f + post.R).OverrideFailureMessage("no stake within 2.5 m outside the door").IsTrue();
        AssertThat(PlanRings.InsideByCrossings(Block(CenterEastIds.Wenke.Hub).Outlines[^1], WenkeSite.AtriumOf(ctx.World).Frame.Origin)).OverrideFailureMessage("the vault stands on the hub").IsTrue();
        // The emblem sits on the stone's dressed face with stone round it.
        foreach (var (x, y) in new[] { (-F.EmblemRadius, F.EmblemY), (F.EmblemRadius, F.EmblemY), (0f, F.EmblemY - F.EmblemRadius), (0f, F.EmblemY + F.EmblemRadius) })
            AssertThat(F.Stone.OnDressedFace(x, y, 0.03f)).OverrideFailureMessage($"emblem edge ({x}, {y}) is on the dressed face").IsTrue();
    }

}
