using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using GdUnit4;

namespace Campus.Gen.World.Tests;

[TestSuite]
public sealed class TerrainKernelTests
{
    [TestCase]
    public void HermiteMoundHasAnalyticalGradeAndExactCompactSupport()
    {
        var peak = TerrainMath.EllipticalMound(0, 0, Vector2.Zero, new(12, 24), 0, 3);
        var flank = TerrainMath.EllipticalMound(6, 0, Vector2.Zero, new(12, 24), 0, 3);
        var outside = TerrainMath.EllipticalMound(12, 0, Vector2.Zero, new(12, 24), 0, 3);
        Equal(3, peak.Height); Equal(0, peak.Dx); Equal(1.5, flank.Height); Equal(-0.375, flank.Dx);
        Require(outside == TerrainDifferential.Constant(0), "Compact support is not exact zero.");
        var basin = TerrainMath.EllipticalMound(6, 0, Vector2.Zero, new(12, 24), 0, -3);
        Require(basin == -flank, "Basin does not use the same deterministic profile.");
    }

    [TestCase]
    public void SmoothMinimumPreservesOutsideIdentityAndDerivative()
    {
        Require(TerrainMath.SmoothMinimum(new(1, 2, 3), new(5, 4, 6), 1) == new TerrainDifferential(1, 2, 3), "Smooth minimum changed the field outside its blend.");
        var result = TerrainMath.SmoothMinimum(new(0, 1, 0), new(0, 0, 1), 2);
        Equal(-0.5, result.Height); Equal(0.5, result.Dx); Equal(0.5, result.Dz);
        var distance = TerrainMath.FilletDistance(new(1, 1, 0), 2);
        Equal(0.25, distance.Height); Equal(0.5, distance.Dx);
        var constrained = TerrainMath.ClampToEnvelope(TerrainDifferential.Constant(0), TerrainDifferential.Constant(3), new(2, 1, 0), 0.25, 0.25, 2);
        Equal(2.75, constrained.Height); Equal(-0.25, constrained.Dx);
    }

    [TestCase]
    public void QuantizationUsesEvenTiesAndNeverHidesOutOfRangeHeights()
    {
        Require(TerrainMath.QuantizeCentimetres(0.125) == 12 && TerrainMath.QuantizeCentimetres(0.375) == 38, "Positive tie rounding differs.");
        Require(TerrainMath.QuantizeCentimetres(-0.125) == -12, "Negative tie rounding differs.");
        try { TerrainMath.QuantizeCentimetres(10.01); throw new InvalidOperationException("Out-of-range height silently clamped."); }
        catch (ArgumentOutOfRangeException) { }
    }

    [TestCase]
    public void SharedEdgesAndRepeatedQueriesHaveIdenticalCentimetres()
    {
        var terrain = Mound();
        var left = new short[TerrainSampling.SampleCount(0)];
        var right = new short[left.Length]; var repeated = new short[left.Length];
        terrain.FillTile(new(-1, 0), 0, left); terrain.FillTile(new(0, 0), 0, right); terrain.FillTile(new(0, 0), 0, repeated);
        Require(right.SequenceEqual(repeated), "Repeated terrain samples differ.");
        for (var z = 0; z < TerrainSampling.TileSamples; z++)
            Require(left[z * 65 + 64] == right[z * 65], "Neighbouring cells disagree on their shared edge.");
        var coarse = new short[TerrainSampling.SampleCount(1)]; terrain.FillTile(new(0, 0), 1, coarse);
        for (var z = 0; z <= 32; z++) Require(coarse[z * 65] == right[z * 2 * 65], "LOD changed a shared world-coordinate sample.");
        Require(!terrain.IsFlat(new(0, 0)) && terrain.IsFlat(new(10, 10)), "Flat path ignored complete support bounds.");
        Equal(3, terrain.HeightAt(0, 0));
    }

    [TestCase]
    public void WholeMapSamplingUsesItsRectAndIgnoresTile()
    {
        var rect = new MapRect(new(-80, -40), new(160, 80));
        var terrain = new TerrainHeightField(rect, (x, z) => new(TerrainMath.EllipticalMound(x, z, new(40, 20), new(120, 60), 0, 3), TerrainClass.Slope), [rect]);
        var a = new short[TerrainSampling.SampleCount(4)]; var b = new short[a.Length];
        terrain.FillTile(new(-100, 20), 4, a); terrain.FillTile(default, 4, b);
        Require(a.SequenceEqual(b) && a[40 * 81 + 40] == 300 && a[0] == 0 && a[^1] == 0, "L4 used a fixed map extent or tile origin.");
    }

    [TestCase]
    public void PadAndWaterTablesAreExplicitAndCopied()
    {
        var pads = new Dictionary<FeatureId, float> { [new("fixture:pad")] = 1.5f };
        var waters = new Dictionary<FeatureId, float> { [new("fixture:water")] = -0.5f };
        var terrain = new TerrainHeightField(new(new(-64), new(64)), (_, _) => new(TerrainDifferential.Constant(0), TerrainClass.Flat), [], pads, waters);
        pads[new("fixture:pad")] = 7; waters.Clear();
        Equal(1.5, terrain.PadY(new("fixture:pad"))); Equal(-0.5, terrain.WaterY(new("fixture:water"))!.Value);
        Require(terrain.WaterY(new("fixture:missing")) is null, "Unknown entity became a water body.");
        var heights = new short[TerrainSampling.SampleCount(0)]; terrain.FillTile(default, 0, heights);
        Require(heights.All(value => value == 0) && terrain.IsFlat(default), "Zero compiled terrain differs from the flat path.");
    }

    [TestCase]
    public void IntegerContainmentAndNearestSegmentHaveStableBoundaries()
    {
        var ring = new Vector2[] { new(0, 0), new(2, 0), new(2, 2), new(0, 2) };
        Require(TerrainMath.PointInRing(2.00049, 1, ring) && !TerrainMath.PointInRing(2.00051, 1, ring), "Containment is not based on millimetre-rounded coordinates.");
        var nearest = TerrainGeometry.ProjectToLine(0, 0, new([new(-1, 0), new(0, 1), new(1, 0)]));
        Require(nearest.Segment == 0, "Equal distances did not select the lower segment index.");
        Equal(-0.5, nearest.X); Equal(0.5, nearest.Z);
        var distance = TerrainGeometry.PolygonDistance(3, 1, new(ring));
        Equal(1, distance.Height); Equal(1, distance.Dx); Equal(0, distance.Dz);
    }

    [TestCase]
    public void DrapeInterpolationRequiresAnExplicitDiagonalAndRejectsHoles()
    {
        Equal(0.5, TerrainMath.TriangleSurface(0, 0, 0, 100, 0.5, 0.5, true));
        Equal(0, TerrainMath.TriangleSurface(0, 0, 0, 100, 0.5, 0.5, false));
        try { TerrainMath.TriangleSurface(TerrainSampling.HoleCm, 0, 0, 0, 0.5, 0.5, false); throw new InvalidOperationException("Draped geometry across a hole."); }
        catch (ArgumentException) { }
    }

    private static TerrainHeightField Mound()
    {
        var support = new MapRect(new(-40, -60), new(40, 60));
        return new(new(new(-128), new(128)), (x, z) => new(TerrainMath.EllipticalMound(x, z, Vector2.Zero, new(40, 60), 0, 3), TerrainClass.Slope), [support]);
    }
    private static void Equal(double expected, double actual) => Require(Math.Abs(expected - actual) <= 0.000001, $"Expected {expected}, got {actual}.");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
