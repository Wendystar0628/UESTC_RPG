using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Geometry;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;
using static Campus.Gen.World.Tests.TerrainCompilerTestFixture;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class TerrainWaterCompilationTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void DrySwaleStaysDryAndOnlyARealNamedCrossingIsAccepted()
    {
        var road = new NetworkLine(new("fixture:road"), new([new(0, -30), new(0, 30)]), 4)
            { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
        var context = Context([]) with { Network = new([road], [], [], [], []) };
        var swale = new TerrainChannelOp("design:terrain:swale", "synthetic crossing", null, 0.4,
            new(TerrainBankKind.Swale) { Inner = "1:4" }, null, [], new([new(-20, 0), new(20, 0)]), 1,
            Crossings: [new TerrainUnderpassCrossing(road.Id, Vector2.Zero)]) { Zone = Zone, Dry = true };
        var terrain = TerrainCompiler.Compile(context, [Source([swale])]);
        AssertThat(Math.Abs((double)(-0.4) - (double)(terrain.HeightAt(-10, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
        AssertThat(terrain.WaterY(new(swale.Id)) is null && terrain.WaterAt(-10, 0) is null).OverrideFailureMessage("Dry swale fabricated a water entity.").IsTrue();
        var invalid = swale with { Crossings = [new TerrainUnderpassCrossing(road.Id, new(0.9f, 0.9f))] };
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.Compile(context, [Source([invalid])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        var ending = swale with { Crossings = [new TerrainEndBeforeCrossing(road.Id)] };
        var terminal = TerrainCompiler.Compile(context, [Source([ending])]);
        var control = TerrainCompiler.Compile(context, []);
        AssertThat(Math.Abs((double)(control.HeightAt(-3, 0)) - (double)(terminal.HeightAt(-3, 0))) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(control.HeightAt(-2.99f, 0)) - (double)(terminal.HeightAt(-2.99f, 0))) <= TestTolerance.Position).IsTrue();

        var touching = swale with { Line = new([new(QuantizedPlan.Tolerance / 2, 0), new(20, 0)]) };
        var path = new TerrainPath(touching.Line!); var roadPath = new TerrainPath(road.Line);
        AssertThat(path.Intersections(roadPath).Count == 0).OverrideFailureMessage("Strict callers admitted a gap.").IsTrue();
        var contactTerrain = TerrainCompiler.Compile(context, [Source([touching])]);
        AssertThat(Math.Abs(contactTerrain.HeightAt(0, 0)) <= TestTolerance.Position && contactTerrain.HeightAt(10, 0) < 0).IsTrue();
        var separated = touching with { Line = new([new(QuantizedPlan.Tolerance * 2, 0), new(20, 0)]) };
        WorldDataException? separatedRejection = null;
        try { TerrainCompiler.Compile(context, [Source([separated])]); } catch (WorldDataException error) { separatedRejection = error; }
        AssertThat(separatedRejection is not null).OverrideFailureMessage("The at matching radius became a segment-contact tolerance.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void SourceRiverAreaUsesHardShoreWithoutInventingItsMissingCentreline()
    {
        var polygon = Box(35, -10, 45, 10);
        var river = WorldFeatureFixture.Create("fixture:river-area", "water", new PolygonGeometry(polygon)) with
        {
            Parts = [new("osm:way:1", new PolygonGeometry(polygon), null, null)
                { Props = JsonSerializer.SerializeToElement(new { kind = "water", water = "river" }) }]
        };
        var context = Context([river]) with { Boundary = Box(-50, -50, 50, 50) };
        var terrain = TerrainCompiler.Compile(context, []);
        AssertThat(Math.Abs((double)(-1.2) - (double)(terrain.WaterY(river.Id)!.Value)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(46, 0))) <= TestTolerance.Position).IsTrue();
        AssertThat(terrain.HeightAt(40, 0) < -1.2).OverrideFailureMessage("Actual water=river was treated as a soft lake.").IsTrue();
        var explicitChannel = new TerrainChannelOp("design:terrain:missing-line", "negative fixture", river.Id, 1.2,
            new(TerrainBankKind.Hard, "1:4"), -1.2, []) { Zone = Zone };
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.Compile(context, [Source([explicitChannel])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AutomaticWaterUsesActualShoreTop()
    {
        var water = WorldFeatureFixture.Create("fixture:raised-lake", "water", new PolygonGeometry(Box(-20, -20, 20, 20)));
        var plateau = new TerrainPlatformOp("design:terrain:raised-lake-base", "fixture", Box(-40, -40, 40, 40), 3,
            new("1:4", "1:4"), 2) { Zone = Zone };
        var terrain = Compile([plateau], [water]); AssertThat(Math.Abs((double)(2.5) - (double)(terrain.WaterY(water.Id)!.Value)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void BridgeBanksFollowActualShoreArclengthAndHaveTwoMetreContinuousEnds()
    {
        var region = new TerrainRegion([Box(-10, -10, 10, 10)]);
        var bridge = new NetworkLine(new("fixture:bridge"), new([new(-20, 0), new(20, 0)]), 2);
        var banks = new TerrainBridgeBanks(region, [bridge]);
        AssertThat(banks.Contacts.Count == 2).OverrideFailureMessage("Bridge/shore crossings were lost or invented.").IsTrue();
        AssertThat(Math.Abs((double)(4) - (double)(banks.NearestContact(-10, 4).AlongDistance)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(1) - (double)(banks.HardWeight(-10, 5).Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(banks.HardWeight(-10, 5).Dz)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0.5) - (double)(banks.HardWeight(-10, 6).Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-0.75) - (double)(banks.HardWeight(-10, 6).Dz)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(banks.HardWeight(-10, 7).Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(banks.HardWeight(-10, 7).Dz)) <= TestTolerance.Position).IsTrue();
        var unrelated = new NetworkLine(new("fixture:nearby"), new([new(-11, -20), new(-11, 20)]), 2);
        AssertThat(new TerrainBridgeBanks(region, [unrelated]).Contacts.Count == 0).OverrideFailureMessage("Nearby nonintersecting bridge was guessed into a bank segment.").IsTrue();
        var water = WorldFeatureFixture.Create("fixture:lake", "water", new PolygonGeometry(region.Polygons[0]));
        var context = Context([water]) with { Network = new([], [], [bridge], [], []) };
        var terrain = TerrainCompiler.Compile(context, []);
        AssertThat(Math.Abs((double)(-0.5) - (double)(terrain.WaterY(water.Id)!.Value)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(terrain.HeightAt(-10, 4))) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(-0.25) - (double)(terrain.HeightAt(-10, 6))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-0.5) - (double)(terrain.HeightAt(-10, 7))) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void CompiledBankQueryExportsFrozenGeometryAndRejectsInvalidStations()
    {
        var water = WorldFeatureFixture.Create("fixture:bank-water", "water", new PolygonGeometry(Box(-10, -10, 10, 10)));
        var bridge = new NetworkLine(new("fixture:bank-bridge"), new([new(-20, 0), new(20, 0)]), 2);
        var context = Context([water]) with { Network = new([], [], [bridge], [], []) };
        ITerrainBankSource source = TerrainCompiler.Compile(context, []);
        AssertThat(source.Banks.Count == 2).OverrideFailureMessage("Resolved bridge banks were not exported once per actual crossing.").IsTrue();
        AssertThat(source.Banks.Select(span => span.Id).SequenceEqual(source.Banks.Select(span => span.Id).OrderBy(id => id, StringComparer.Ordinal))).OverrideFailureMessage("Bank IDs are not in stable ordinal order.").IsTrue();
        var first = source.Banks[0];
        AssertThat(first.Waters.SequenceEqual(new[] { water.Id }) && first.Bridges.SequenceEqual(new[] { bridge.Id }) && first.Roads.Count == 0).OverrideFailureMessage("Exported provenance does not identify the actual water/bridge.").IsTrue();
        AssertThat(Math.Abs((double)(2) - (double)(first.TransitionLength)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(source.BankTopY(first.Id, first.CoreFrom))) <= TestTolerance.Position).IsTrue();
        var retained = source.BankTopY(first.Id, (first.CoreFrom + first.CoreTo) / 2);
        var restored = new PreparedTerrain(((CompiledTerrain)source).Freeze());
        AssertThat(Math.Abs(restored.BankTopY(first.Id, (first.CoreFrom + first.CoreTo) / 2) - retained) <= TestTolerance.Position
            && restored.Banks.Count == source.Banks.Count).OverrideFailureMessage("Frozen original-shore profiles changed the bank top or its exported spans.").IsTrue();
        var plateau = new TerrainPlatformOp("design:terrain:bank-plateau", "new snapshot", Box(-40, -40, 40, 40), 3, new("1:4", "1:4"), 2) { Zone = Zone };
        ITerrainBankSource changed = TerrainCompiler.Compile(context, [Source([plateau])]);
        AssertThat(Math.Abs((double)(3) - (double)(changed.BankTopY(changed.Banks[0].Id, changed.Banks[0].CoreFrom))) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(retained) - (double)(source.BankTopY(first.Id, (first.CoreFrom + first.CoreTo) / 2))) <= TestTolerance.Position).IsTrue();
        foreach (var station in new[] { double.NaN, double.PositiveInfinity, -0.01, first.Length + 0.01 })
        {
            try { source.BankTopY(first.Id, station); throw new InvalidOperationException("Invalid station was silently clamped."); }
            catch (ArgumentOutOfRangeException) { }
        }
        try { source.BankTopY(" missing ", 0); throw new InvalidOperationException("Malformed ID accepted."); } catch (ArgumentException) { }
        try { source.BankTopY("missing", 0); throw new InvalidOperationException("Unknown ID substituted terrain height."); } catch (KeyNotFoundException) { }
        var hard = new TerrainLakeOp("design:terrain:whole-bank", "whole ring", water.Id, -0.5, new(TerrainBankKind.Hard, "1:4"), new(1.5), []) { Zone = Zone };
        ITerrainBankSource closed = Compile([hard], [water]); var ring = closed.Banks.Single();
        AssertThat(ring.Shore.Points[0] == ring.Shore.Points[^1] && ring.TransitionLength == 0 && ring.CoreTo == ring.Length).OverrideFailureMessage("Whole Hard ring lost its final station contract.").IsTrue();
        AssertThat(Math.Abs((double)(closed.BankTopY(ring.Id, 0)) - (double)(closed.BankTopY(ring.Id, ring.Length))) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FootwayConflictIsOneContinuousHardSpanWithThreeMetreExtensions()
    {
        var water = new TerrainRegion([Box(-10, -10, 10, 10)]);
        var road = new TerrainRegion([Box(-12, -3, -11, 3)]);
        var banks = new TerrainBridgeBanks(water, []);
        banks.ResolveFootways([new(new("fixture:shore-path"), TerrainRegion.Expand(road.Bounds, 5),
            road.Polygons[0].Outer, (x, z) => (road.Distance(x, z), TerrainDifferential.Constant(0)))],
            (_, _) => -0.5);
        AssertThat(banks.RoadSpans.Count == 1).OverrideFailureMessage("One geometric conflict was split into sample-sized walls.").IsTrue();
        var span = banks.RoadSpans[0];
        var expectedEnd = 3 + Math.Sqrt(1.5 * 1.5 - 1) + 3;
        AssertThat(Math.Abs(span.To - span.From - 2 * expectedEnd) < TestTolerance.Position).OverrideFailureMessage("The continuous conflict root or 3 m extension changed.").IsTrue();
        AssertThat(Math.Abs((double)(1) - (double)(banks.HardWeight(-10, 0).Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs(banks.HardWeight(-10, expectedEnd + 1).Height - 0.5) < TestTolerance.Position).OverrideFailureMessage("Hard/Soft transition is not the fixed 2 m Hermite join.").IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(banks.HardWeight(-10, expectedEnd + 2.01).Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(banks.ProfileAt(-10, 0).Top!.Value.Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(span.Roads.Count == 1 && span.Roads[0].Road == new FeatureId("fixture:shore-path")).OverrideFailureMessage("Resolved bank lost its actual road identity.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void AutomaticRoadFollowsSoftBankAndPreservesItsWaterBoundary()
    {
        var water = WorldFeatureFixture.Create("fixture:lake", "water", new PolygonGeometry(Box(0, -20, 20, 20)));
        CompiledTerrain Build(float center, float width)
        {
            var roadGeometry = new PolygonGeometry(Box(center - width / 2, -15, center + width / 2, 15));
            var road = new WorldFeature(new("fixture:road"), new("road"), roadGeometry)
                { Parts = [new("osm:way:8", roadGeometry, null, null) { Props = Kind("road") }] };
            var line = new NetworkLine(new("fixture:source:way:8"), new([new(center, -15), new(center, 15)]), width)
                { Props = JsonSerializer.SerializeToElement(new { highway = "footway" }) };
            return TerrainCompiler.Compile(Context([water, road]) with { Network = new([line], [], [], [], []) }, []);
        }
        var feasible = Build(-4, 2); AssertThat(Math.Abs((double)(-0.5) - (double)(feasible.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(feasible.HeightAt(-4, 0))) <= TestTolerance.Position).IsTrue();
        AssertThat(feasible.FactsAt(-0.5f, 0).Grade <= 1f / 3 + TestTolerance.Position).OverrideFailureMessage("Narrowed side exceeded its true transverse grade.").IsTrue();
        // Default base samples are now soft terrain-following targets, not two hidden Y=0
        // anchors. This narrow path can follow the dry soft bank without moving its waterline.
        var narrow = Build(-0.2f, 0.1f);
        AssertThat(Math.Abs((double)(-0.5) - (double)(narrow.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
        // A sub-metre path drapes on the published grid; it cannot retain a separate analytical floor.
        var narrowFloor = narrow.HeightAt(-0.2f, 0);
        AssertThat(narrowFloor < 0 && narrow.WaterAt(-0.2f, 0) is null).OverrideFailureMessage("The path was raised to hidden zero anchors or classified as water.").IsTrue();
        AssertThat(Math.Abs(narrowFloor - RenderedHeight(narrow, -0.2f, 0)) <= TestTolerance.Position)
            .OverrideFailureMessage("The narrow path floats above its rendered terrain.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void WaterUnionRemovesInternalEdgesAndPreservesRemainingHoles()
    {
        var union = TerrainUnion.Build([new([Box(-2, -10, 1, 10)]), new([Box(-1, -10, 2, 10)])]);
        AssertThat(Math.Abs((double)(-2) - (double)(union.Distance(0, 0).Height)) <= TestTolerance.Position).IsTrue();
        var touching = TerrainUnion.Build([new([Box(-2, -2, 0, 2)]), new([Box(0, -2, 2, 2)])]);
        AssertThat(Math.Abs((double)(-2) - (double)(touching.Distance(0, 0).Height)) <= TestTolerance.Position).IsTrue();
        var island = new TerrainRegion([new PolygonRings(Box(-10, -10, 10, 10).Outer, [Box(-3, -3, 3, 3).Outer])]);
        var filled = TerrainUnion.Build([island, new([Box(-1, -5, 1, 5)])]);
        AssertThat(filled.Contains(0, 0) && !filled.Contains(2, 0) && filled.Polygons.Sum(p => p.Holes.Count) == 2).OverrideFailureMessage("Water union filled an unrelated island hole or retained an internal boundary.").IsTrue();
        var a = new TerrainRegion([Box(-2.00037f, -10, 1, 10)]);
        var c = new TerrainRegion([Box(-1, -10, 2.00037f, 10)]);
        var fractional = TerrainUnion.Build([a, c]);
        AssertThat(Math.Abs((double)(-2) - (double)(fractional.Distance(0, 0).Height)) <= TestTolerance.Position).IsTrue();
        var expected = Nodes(fractional);
        AssertThat(expected.SequenceEqual(Nodes(TerrainUnion.Build([a, c]))) && expected.SequenceEqual(Nodes(TerrainUnion.Build([c, a])))).OverrideFailureMessage("Centimetre-grid union geometry changes between repeated builds or input order.").IsTrue();
        static IEnumerable<Vector2> Nodes(TerrainRegion region) => region.Polygons
            .SelectMany(p => new[] { p.Outer }.Concat(p.Holes)).SelectMany(ring => ring);
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ShoreEnvelopeKeepsFixedBoundaryValuesAndCapsTheFullLandGradient()
    {
        var envelope = new TerrainShoreEnvelope(new([Box(-1, -1, 1, 1)]), (x, _) => 0.2 * x, "fixture", "shore-envelope");
        AssertThat(Math.Abs((double)(-0.2) - (double)(envelope.Constrain(TerrainDifferential.Constant(0), -1, 0).Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0.2) - (double)(envelope.Constrain(TerrainDifferential.Constant(0), 1, 0).Height)) <= TestTolerance.Position).IsTrue();
        var sample = envelope.Constrain(TerrainDifferential.Constant(0), -1.2, 0.2);
        AssertThat(Math.Abs((double)(-0.1) - (double)(sample.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(0.5) - (double)(Math.Sqrt(sample.Dx * sample.Dx + sample.Dz * sample.Dz))) <= TestTolerance.Position).IsTrue();
        const double e = 0.01;
        var finite = (envelope.Constrain(TerrainDifferential.Constant(0), -1.2 + e, 0.2).Height
            - envelope.Constrain(TerrainDifferential.Constant(0), -1.2 - e, 0.2).Height) / (2 * e);
        AssertThat(Math.Abs((double)(finite) - (double)(sample.Dx)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void WaterJoinKeepsTwoMetreShoreArclengthWhileInteriorWidthChanges()
    {
        var lake = new TerrainRegion([Box(-10, -10, 10, 10)]);
        var channel = new TerrainRegion([Box(-4, -25, 4, 0)]);
        var join = new TerrainWaterJoin(TerrainUnion.Build([lake, channel]), lake);
        foreach (var width in new[] { 2d, 6d, 12d })
        {
            AssertThat(Math.Abs((double)(1) - (double)(join.Weight(-4, -10, width).Height)) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs((double)(0.5) - (double)(join.Weight(-4, -11, width).Height)) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs((double)(0) - (double)(join.Weight(-4, -12, width).Height)) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs((double)(0) - (double)(join.Weight(-4, -13, width).Height)) <= TestTolerance.Position).IsTrue();
            var a = join.Weight(-4.01, -11, width); var c = join.Weight(-3.99, -11, width);
            AssertThat(Math.Abs(a.Height - c.Height) < TestTolerance.Position).OverrideFailureMessage("The fixed exposed shore and interior join are discontinuous.").IsTrue();
        }
        AssertThat(Math.Abs((double)(1) - (double)(join.Weight(0, 0, 4).Height)) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(join.Weight(0, -22, 4).Height)) <= TestTolerance.Position).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void WaterUnionPreservesAuthoredLocalProfileAndZoneExtentBehindAnAutomaticMember()
    {
        var automatic = WorldFeatureFixture.Create("fixture:a-auto-lake", "water", new PolygonGeometry(Box(-15, -8, 1, 8)));
        var authored = WorldFeatureFixture.Create("fixture:z-authored-lake", "water", new PolygonGeometry(Box(-1, -8, 15, 8)));
        var op = new TerrainLakeOp("design:terrain:authored-water", "zone regression", authored.Id, -0.5,
            new(TerrainBankKind.Soft, "1:4"), new(1.5), []) { Zone = Zone };
        var context = Context([automatic, authored]) with
        {
            Zones = new Dictionary<ZoneId, IReadOnlyList<PolygonRings>> { [Zone] = [Box(-7, -20, 7, 20)] }
        };
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.CompileAndValidate(context, [Source([op])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
        var accepted = TerrainCompiler.CompileAndValidate(Context([automatic, authored]), [Source([op])]);
        AssertThat(accepted.ZonesValidated).OverrideFailureMessage("A valid author extent lost its zone validation after union.").IsTrue();
        var authoredOnly = Compile([op], [authored]);
        var automaticOnly = Compile([], [automatic]);
        AssertThat(Math.Abs(accepted.HeightAt(10, 0) - authoredOnly.HeightAt(10, 0)) <= TestTolerance.Position
            && Math.Abs(accepted.HeightAt(-10, 0) - automaticOnly.HeightAt(-10, 0)) <= TestTolerance.Position)
            .OverrideFailureMessage("A shared water level replaced a member's local depth/bank profile.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ConnectedNaturalStreamUsesLakeLevelWithoutRenamingOrChangingItsDepth()
    {
        var lake = WorldFeatureFixture.Create("fixture:lake", "water", new PolygonGeometry(Box(-10, -10, 10, 10)));
        var shape = new PolygonGeometry(Box(-4, -25, 4, 0));
        var stream = new WorldFeature(new("fixture:stream"), new("water"), shape)
            { Parts = [new("osm:way:9", shape, null, null) { Props = Kind("water") }] };
        var line = new NetworkLine(new("fixture:source:way:9"), new([new(0, -25), new(0, 0)]), 8)
            { Props = JsonSerializer.SerializeToElement(new { waterway = "stream" }) };
        var terrain = TerrainCompiler.Compile(Context([lake, stream]) with { Network = new([], [line], [], [], []) }, []);
        AssertThat(Math.Abs((double)(-0.5) - (double)(terrain.WaterY(lake.Id)!.Value)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-0.5) - (double)(terrain.WaterY(stream.Id)!.Value)) <= TestTolerance.Position).IsTrue();
        AssertThat(terrain.HeightAt(0, -10) < -0.5).OverrideFailureMessage("Internal lake/stream boundary became a fake shore.").IsTrue();
        AssertThat(Math.Abs((double)(-1.7) - (double)(terrain.HeightAt(0, -18))) <= TestTolerance.Position).IsTrue();
        AssertThat(Math.Abs((double)(Compile([], [lake]).HeightAt(0, 0)) - (double)(terrain.HeightAt(0, 0))) <= TestTolerance.Position).IsTrue();
        var canal = line with { Props = JsonSerializer.SerializeToElement(new { waterway = "canal" }) };
        var fixedCanal = new TerrainChannelOp("design:terrain:fixed-canal", "explicit incompatible level", stream.Id,
            1.2, new(TerrainBankKind.Hard, "1:4"), -1.2, []) { Zone = Zone };
        {
            WorldDataException? rejection = null;
            try { TerrainCompiler.Compile(Context([lake, stream]) with { Network = new([], [canal], [], [], []) }, [Source([fixedCanal])]); } catch (WorldDataException error) { rejection = error; }
            AssertThat(rejection is not null).IsTrue();
        }
    }
}
