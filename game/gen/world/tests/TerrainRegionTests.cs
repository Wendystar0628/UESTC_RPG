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
public sealed class TerrainRegionTests
{
    [TestCase, TestCategory(TestReason.Contract)]
    public void IndexedBoundaryRetainsMillimetreContainmentAndExactEdgeDistance()
    {
        var rings = new PolygonRings([new(-20, -20), new(-5, -20), new(-5, -10), new(5, -10), new(5, -20),
            new(20, -20), new(20, 20), new(5, 20), new(5, 10), new(-5, 10), new(-5, 20), new(-20, 20)], [Box(-4, -4, 4, 4).Outer]);
        var indexed = new TerrainRegion([rings]);
        rings = indexed.Polygons[0]; // Both kernels use the same canonical hole winding and source-edge tie order.
        for (var z = -23.25; z <= 23; z += 1.5)
            for (var x = -23.25; x <= 23; x += 1.5)
            {
                var reference = TerrainGeometry.PolygonDistance(x, z, rings); var actual = indexed.Distance(x, z);
                AssertThat(Math.Abs((double)(reference.Height) - (double)(actual.Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(reference.Dx) - (double)(actual.Dx)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(reference.Dz) - (double)(actual.Dz)) <= TestTolerance.Position).IsTrue();
                AssertThat(indexed.Contains(x, z) == TerrainMath.Contains(x, z, rings)).OverrideFailureMessage("Edge BVH changed exact containment.").IsTrue();
            }
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void GeometricDistanceHandlesHolesConcaveVerticesAndExactBoundaryHits()
    {
        var region = new TerrainRegion([new PolygonRings([new(-8, -8), new(8, -8), new(8, 0),
            new(0, 0), new(0, 8), new(-8, 8)], [Box(-6, -6, -2, -2).Outer])]);
        var ring = new TerrainRingIndex(region.Polygons[0].Outer);
        AssertThat(ring.ContainsGeometry(-4, 0) && !ring.ContainsGeometry(9, 0)).OverrideFailureMessage("A ray through polygon vertices changed real containment.").IsTrue();
        AssertThat(Math.Abs((double)(2) - (double)(region.Distance(-4, -4).Height)) <= TestTolerance.Position).IsTrue(); AssertThat(Math.Abs((double)(-Math.Sqrt(2)) - (double)(region.Distance(-1, -1).Height)) <= TestTolerance.Position).IsTrue();
        foreach (var point in new[] { new Vector2(8, -4), Vector2.Zero, new Vector2(-2, -4), new Vector2(-2, -2) })
            AssertThat(BitConverter.DoubleToInt64Bits(region.Distance(point.X, point.Y).Height) == 0).OverrideFailureMessage("Exact edge/vertex distance is not canonical positive zero.").IsTrue();
        foreach (var epsilon in new[] { 0.05, 0.5 })
        {
            AssertThat(Math.Abs(region.Distance(8 + epsilon, -4).Height - epsilon) < TestTolerance.Position
                && Math.Abs(region.Distance(8 - epsilon, -4).Height + epsilon) < TestTolerance.Position).OverrideFailureMessage("Outer edge distance does not tend to zero from its actual two sides.").IsTrue();
            AssertThat(Math.Abs(region.Distance(-2 - epsilon, -4).Height - epsilon) < TestTolerance.Position
                && Math.Abs(region.Distance(-2 + epsilon, -4).Height + epsilon) < TestTolerance.Position).OverrideFailureMessage("Clockwise hole distance has the wrong real sign.").IsTrue();
            AssertThat(Math.Abs(region.Distance(-epsilon, -epsilon).Height + Math.Sqrt(2) * epsilon) < TestTolerance.Position
                && Math.Abs(region.Distance(epsilon, epsilon).Height - epsilon) < TestTolerance.Position).OverrideFailureMessage("The nearest concave vertex has a discontinuous distance sign.").IsTrue();
        }
        var rect = new MapRect(new(-64), new(128));
        var field = new TerrainHeightField(new("fixture", []), rect, (x, z) => new(region.Distance(x, z) * 0.01, TerrainClass.Slope), [rect]);
        var left = new short[TerrainSampling.SampleCount(0)]; var right = new short[left.Length]; var repeated = new short[left.Length];
        field.FillTile(new(-1, 0), 0, left); field.FillTile(new(0, 0), 0, right); field.FillTile(new(0, 0), 0, repeated);
        AssertThat(right.SequenceEqual(repeated)).OverrideFailureMessage("Repeated geometric-distance tile samples differ.").IsTrue();
        for (var z = 0; z < TerrainSampling.TileSamples; z++)
            AssertThat(left[z * 65 + 64] == right[z * 65]).OverrideFailureMessage("Adjacent tiles disagree at the same geometric-distance world point.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void ProjectedSlantedBankHasZeroTangentialDistanceDerivative()
    {
        var region = new TerrainRegion([new PolygonRings([new(-436.73f, 40.12f), new(-432.11f, 58.69f),
            new(-449.31f, 61.93f), new(-453.93f, 43.36f)], [])]);
        for (var i = 1; i < 100; i++)
        {
            var hit = region.NearestBoundary(-431.8, 41 + i * 0.17);
            var atBank = region.Distance(hit.X, hit.Z);
            AssertThat(Math.Abs((double)(0) - (double)(atBank.Height)) <= TestTolerance.Position).IsTrue();
            AssertThat(Math.Abs((double)(0) - (double)(atBank.Dx * hit.Tx + atBank.Dz * hit.Tz)) <= TestTolerance.Position).IsTrue();
            if (hit.Tx != 0 || hit.Tz != 0) AssertThat(Math.Abs((double)(1) - (double)(atBank.Dx * atBank.Dx + atBank.Dz * atBank.Dz)) <= TestTolerance.Position).IsTrue();
        }
    }
}
