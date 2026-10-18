using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Tests;
using Campus.Contracts.Maps;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Gen.World.Tests;

public sealed partial class TerrainCompilerTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void FreeboardRaisesBankTopWithoutRaisingSoftWaterlineOrLoweringWater()
    {
        var water = WorldFeatureFixture.Create("fixture:raised-water", "water", new PolygonGeometry(Box(-10, -10, 10, 10)));
        var lake = new TerrainLakeOp("design:terrain:raised-water", "freeboard fixture", water.Id, 1,
            new(TerrainBankKind.Soft, "1:5"), new(1.5), []) { Zone = Zone };
        var terrain = Compile([lake], [water]);
        AssertThat(Math.Abs((double)(1) - (double)(terrain.WaterY(water.Id)!.Value)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(1) - (double)(terrain.HeightAt(10, 0))) <= TestTolerance.Position).IsTrue();
        AssertThat(terrain.HeightAt(12.5f, 0) >= 1.3f - TestTolerance.Position).OverrideFailureMessage("Landward bank top lacks the required freeboard.").IsTrue();
        AssertThat(Math.Abs(terrain.HeightAt(10.0001f, 0) - terrain.HeightAt(9.9999f, 0)) < TestTolerance.Position).OverrideFailureMessage("Freeboard introduced a jump at the soft waterline.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SlopeBudgetedQuadraticJoinPreservesEndpointsAndParameterContinuity()
    {
        foreach (var peak in new[] { 1.1, 1.2, 1.5 })
        {
            AssertThat(Math.Abs((double)(0) - (double)(TerrainCompiler.LimitedEase(0, peak).Value)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(TerrainCompiler.LimitedEase(0, peak).Derivative)) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs((double)(1) - (double)(TerrainCompiler.LimitedEase(1, peak).Value)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(TerrainCompiler.LimitedEase(1, peak).Derivative)) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs((double)(0.5) - (double)(TerrainCompiler.LimitedEase(0.5, peak).Value)) <= TestTolerance.Position).IsTrue();
            for (var i = 0; i <= 100; i++) AssertThat(TerrainCompiler.LimitedEase(i / 100d, peak).Derivative <= peak + TestTolerance.Position).OverrideFailureMessage("Join exceeded its analytically allocated derivative budget.").IsTrue();
        }
        AssertThat(Math.Abs(TerrainCompiler.LimitedEase(0.25, 1.5).Value - TerrainCompiler.LimitedEase(0.25, 1.5 - 0.00000001).Value) < TestTolerance.Position).OverrideFailureMessage("Switching interpolation families at the peak cap created a height jump.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SameSpanRoadContributorsStayContinuousAcrossTheirEqualDistanceLine()
    {
        var first = BankContributor("fixture:bank-a", (_, z) => (new(2 + z * 0.05, 0, 0.05), TerrainDifferential.Constant(1)));
        var second = BankContributor("fixture:bank-b", (_, z) => (new(2 - z * 0.05, 0, -0.05), TerrainDifferential.Constant(2)));
        var banks = BanksForContributors(first, second);
        const double epsilon = 0.0001;
        var left = banks.ProfileAt(-10, -epsilon).Top!.Value;
        var right = banks.ProfileAt(-10, epsilon).Top!.Value;
        var center = banks.ProfileAt(-10, 0).Top!.Value;
        AssertThat(Math.Abs(right.Height - left.Height) < TestTolerance.Position).OverrideFailureMessage("The same-span contributor bisector still has a height jump.").IsTrue();
        var finiteDifference = (right.Height - left.Height) / (2 * epsilon);
        AssertThat(Math.Abs(center.Dz - finiteDifference) < TestTolerance.Position).OverrideFailureMessage("Reported bank derivative omits the changing contributor weights.").IsTrue();
        AssertThat(Math.Abs(center.Dz) > 0.02).OverrideFailureMessage("Constant source tops lost the nonzero distance-weight derivative.").IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(center.Dx)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Determinism)]
    public void SameSpanRoadContributorInputOrderDoesNotChangeItsSurface()
    {
        var first = BankContributor("fixture:bank-a", (_, z) => (new(2 + z * 0.05, 0, 0.05), new(1 + z * 0.02, 0, 0.02)));
        var second = BankContributor("fixture:bank-b", (_, z) => (new(2 - z * 0.05, 0, -0.05), new(2 - z * 0.03, 0, -0.03)));
        var forward = BanksForContributors(first, second); var reverse = BanksForContributors(second, first);
        foreach (var z in new[] { -0.2, 0, 0.2 })
        {
            var a = forward.ProfileAt(-10, z); var b = reverse.ProfileAt(-10, z);
            AssertThat(a == b).OverrideFailureMessage("Reversing source roads changed the composed bank value or derivatives.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AZeroDistanceRoadCoreKeepsItsExactTopAndDerivative()
    {
        var expected = new TerrainDifferential(0.75, 0, 0.125);
        foreach (var distance in new[] { 0d, -0.25 })
        {
            var core = BankContributor("fixture:bank-a", (_, _) => (TerrainDifferential.Constant(distance), expected));
            var other = BankContributor("fixture:bank-b", (_, _) => (TerrainDifferential.Constant(2), TerrainDifferential.Constant(2)));
            var actual = BanksForContributors(other, core).ProfileAt(-10, 0).Top!.Value;
            AssertThat(actual == expected).OverrideFailureMessage("A zero/negative-distance fixed core was averaged into another road.").IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CoincidentRoadCoresWithDifferentHeightsAreRejected()
    {
        var first = BankContributor("fixture:bank-a", (_, _) => (TerrainDifferential.Constant(0), TerrainDifferential.Constant(0.75)));
        var second = BankContributor("fixture:bank-b", (_, _) => (TerrainDifferential.Constant(0), TerrainDifferential.Constant(0.7502)));
        var banks = BanksForContributors(second, first);
        {
            WorldDataException? rejection = null;
            try { banks.ProfileAt(-10, 0); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void NonFiniteRoadBankContactsAreRejectedBeforeComposition()
    {
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            var invalidDistance = BankContributor("fixture:bad-distance", (_, _) => (new(value, 0, 0), TerrainDifferential.Constant(0)));
            var invalidDerivative = BankContributor("fixture:bad-distance-derivative", (_, _) => (new(1, value, 0), TerrainDifferential.Constant(0)));
            var invalidTop = BankContributor("fixture:bad-top", (_, _) => (TerrainDifferential.Constant(1), new(0, 0, value)));
            {
            WorldDataException? rejection = null;
            try { BanksForContributors(invalidDistance); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
            {
            WorldDataException? rejection = null;
            try { BanksForContributors(invalidDerivative); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
            {
            WorldDataException? rejection = null;
            try { BanksForContributors(invalidTop); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        }
    }

    private static TerrainBridgeBanks.RoadContact BankContributor(string id,
        Func<double, double, (TerrainDifferential Distance, TerrainDifferential Top)> evaluate) =>
        new(new(id), new(new(-12), new(12)), [], (x, z) => evaluate(x, z));

    private static TerrainBridgeBanks BanksForContributors(params TerrainBridgeBanks.RoadContact[] contributors)
    {
        var banks = new TerrainBridgeBanks(new([Box(-10, -10, 10, 10)]), []);
        banks.ResolveFootways(contributors, (_, _) => -100);
        AssertThat(banks.RoadSpans.Count == 1 && banks.RoadSpans[0].Roads.Count == contributors.Length).OverrideFailureMessage("Synthetic crossing contributors did not share one Hard span.").IsTrue();
        return banks;
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FreeboardNetworkBoundsKeepFrozenSoftTargetsSharedNodesAndInputOrder()
    {
        var a = new TerrainRoad(new("fixture:floor-a"), new("fixture:floor-a"), new(new([new(-20, 0), new(0, 0)])), 2.5, "footway", null);
        var c = new TerrainRoad(new("fixture:floor-c"), new("fixture:floor-c"), new(new([new(0, 0), new(20, 0)])), 2.5, "footway", null);
        TerrainRoadProfile.Request[] requests = [new(a, [], 1d / 12), new(c, [], 1d / 12)];
        var evaluations = 0;
        var baseline = new TerrainProgram([new("base", "fixture", Zone, TerrainGroup.Regional, 0, new(new(-30), new(30)),
            (previous, _, _) => { evaluations++; return previous with { Surface = TerrainDifferential.Constant(-0.5) }; })]);
        var fit = new TerrainRoadProfile.NetworkFit(requests, baseline, [], (_, _) => false);
        fit.Solve(); var sampledOnce = evaluations;
        foreach (var sample in fit.Samples) fit.Require(sample.Source, sample.Index, new(-0.18, "fixture:water"));
        var forward = fit.Solve(); fit.Solve();
        AssertThat(evaluations == sampledOnce).OverrideFailureMessage("A freeboard re-solve sampled a changed soft field instead of retaining its original objective.").IsTrue();
        var reverseFit = new TerrainRoadProfile.NetworkFit(requests.Reverse().ToArray(), baseline, [], (_, _) => false);
        foreach (var sample in reverseFit.Samples.Reverse()) reverseFit.Require(sample.Source, sample.Index, new(-0.18, "fixture:water"));
        var reverse = reverseFit.Solve();
        AssertThat(Math.Abs((double)(-0.18) - (double)(forward[a.Source].At(a.Path.Length).Y)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-0.18) - (double)(forward[c.Source].At(0).Y)) <= TestTolerance.Position).IsTrue();
        AssertThat(forward[a.Source].At(20).Y == forward[c.Source].At(0).Y).OverrideFailureMessage("Shared freeboard node acquired two values.").IsTrue();
        foreach (var road in new[] { a, c })
            for (var station = 0d; station <= road.Path.Length; station += 0.025)
            {
                AssertThat(Math.Abs(forward[road.Source].At(station).Y - reverse[road.Source].At(station).Y) < TestTolerance.FloatRelative).OverrideFailureMessage("Input order changed the constrained profile.").IsTrue();
                AssertThat(Math.Abs(forward[road.Source].At(station).Grade) <= 0.031).OverrideFailureMessage("Freeboard changed the junction grade ceiling.").IsTrue();
            }
        AssertThat(!fit.Require(a.Source, 0, new(-0.2, "fixture:lower-water"))).OverrideFailureMessage("A lower candidate weakened a cumulative node bound.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ConservativeFreeboardNeighbourIsUnsupportedWithoutMovingTheAuthor()
    {
        var automatic = new TerrainRoad(new("fixture:conservative-auto"), new("fixture:conservative-auto"),
            new(new([new(0, 0), new(20, 0)])), 2.5, "footway", null);
        var authored = new TerrainRoad(new("fixture:conservative-author"), new("fixture:conservative-author"),
            new(new([new(0, 0), new(0, -20)])), 2.5, "footway", null);
        var authorProfile = new TerrainRoadProfile([new(0, -0.25), new(20, -0.25)], 20, [], 1d / 12, "owner.terrain.json", "design:terrain:good-author");
        var fit = new TerrainRoadProfile.NetworkFit([new(automatic, [], 1d / 12)], new TerrainProgram([]), [(authored, authorProfile)], (_, _) => false);
        fit.Solve();
        try { fit.Require(automatic.Source, 0, new(-0.18, "fixture:distant-water")); throw new InvalidOperationException("Conservative neighbour was silently moved."); }
        catch (WorldDataException error)
        {
            AssertThat((string?)error.Data["TerrainConstraintKind"] == "unsupported").OverrideFailureMessage("A conservative neighbour was incorrectly blamed on author data.").IsTrue();
        }
        AssertThat(Math.Abs((double)(-0.25) - (double)(authorProfile.At(0).Y)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-0.25) - (double)(fit.Profiles[automatic.Source].At(0).Y)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void AutomaticLowSoftRoadClosesRealHardBankFreeboardWithoutMovingWaterOrXZ()
    {
        var water = WorldFeatureFixture.Create("fixture:low-soft-water", "water", new PolygonGeometry(Box(0, -10, 20, 10)));
        var points = new Vector2[] { new(-0.5f, -20), new(-0.5f, 20) };
        var line = new NetworkLine(new("fixture:low-soft-road"), new(points), 0.5f)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var context = Context([water]) with { Network = new([line], [], [], [], []) };
        var terrain = TerrainCompiler.Compile(context, []);
        AssertThat(Math.Abs((double)(-0.5) - (double)(terrain.WaterY(water.Id)!.Value)) <= TestTolerance.Position).IsTrue();
        AssertThat(line.Line.Points.SequenceEqual(points)).OverrideFailureMessage("Freeboard solving changed the real road XZ.").IsTrue();
        AssertThat(terrain.Banks.Count > 0).OverrideFailureMessage("The actual road/soft-bank conflict was not resolved as Hard geometry.").IsTrue();
        var waterY = terrain.WaterY(water.Id)!.Value; var requiredTop = waterY + 0.3;
        foreach (var span in terrain.Banks)
        {
            // The exported shore includes Soft joins; freeboard applies to the Hard core.
            AssertThat(terrain.BankTopY(span.Id, span.CoreFrom) >= requiredTop - TestTolerance.Position
                && terrain.BankTopY(span.Id, span.CoreTo) >= requiredTop - TestTolerance.Position).OverrideFailureMessage("A final Hard core endpoint retained the low soft target.").IsTrue();
            for (var station = span.CoreFrom; station <= span.CoreTo; station += 0.025)
                AssertThat(terrain.BankTopY(span.Id, station) >= requiredTop - TestTolerance.Position).OverrideFailureMessage("Final exported continuous Hard bank retained the low soft target.").IsTrue();
            if (span.TransitionLength == 0) continue;
            // This fixture has no overlapping banks: both outer join ends are the Soft
            // shoreline water level, while the inner ends meet the Hard top continuously.
            AssertThat(Math.Abs((double)(waterY) - (double)(terrain.BankTopY(span.Id, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(waterY) - (double)(terrain.BankTopY(span.Id, span.Length))) <= TestTolerance.Position).IsTrue();
            const double epsilon = 0.0001;
            AssertThat(Math.Abs(terrain.BankTopY(span.Id, epsilon) - terrain.BankTopY(span.Id, 0)) < TestTolerance.Position
                && Math.Abs(terrain.BankTopY(span.Id, span.Length - epsilon) - terrain.BankTopY(span.Id, span.Length)) < TestTolerance.Position).OverrideFailureMessage("A Soft transition outer endpoint does not meet its fixed waterline continuously.").IsTrue();
            foreach (var join in new[] { span.CoreFrom, span.CoreTo })
                AssertThat(Math.Abs(terrain.BankTopY(span.Id, join - epsilon) - terrain.BankTopY(span.Id, join + epsilon)) < TestTolerance.Position).OverrideFailureMessage("A Soft transition inner endpoint does not meet its Hard core continuously.").IsTrue();
        }
        AssertThat(terrain.Diagnostics.Any(value => value.StartsWith("Road freeboard initial own deficit:", StringComparison.Ordinal))
            && terrain.Diagnostics.Any(value => value.StartsWith("Road freeboard sample final:", StringComparison.Ordinal))
            && terrain.Diagnostics.Any(value => value.Contains("influenceBBox=", StringComparison.Ordinal))
            && terrain.Diagnostics.Any(value => value.Contains("closure complete", StringComparison.Ordinal))).OverrideFailureMessage("Reviewable closure diagnostics are incomplete.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FootwayPlanningDoesNotPolluteTheSoftBaseAndCommitsOnlyTheLastRealPlan()
    {
        var banks = new TerrainBridgeBanks(new([Box(-10, -10, 10, 10)]), []);
        var road = BankContributor("fixture:plan-road", (_, _) => (TerrainDifferential.Constant(1), TerrainDifferential.Constant(0)));
        var initial = banks.PlanFootways([road], (_, _) => -0.5);
        AssertThat(initial.Count == 1 && banks.RoadSpans.Count == 0 && banks.ProfileAt(-10, 0).Top is null).OverrideFailureMessage("Pure planning mutated the WaterNode-owned bank state.").IsTrue();
        var final = banks.PlanFootways([road], (_, _) => 0);
        AssertThat(final.Count == 0 && banks.RoadSpans.Count == 0).OverrideFailureMessage("A disappearing real conflict survived as cumulative geometry.").IsTrue();
        banks.CommitFootways(final);
        AssertThat(banks.RoadSpans.Count == 0).OverrideFailureMessage("The initial stale Hard geometry was committed.").IsTrue();
        try { banks.CommitFootways(initial); throw new InvalidOperationException("Banks were mutable after snapshot finalization."); }
        catch (InvalidOperationException) { }
    }

}
