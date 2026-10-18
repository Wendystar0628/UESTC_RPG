using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;
using GdUnit4;
using static GdUnit4.Assertions;

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
        AssertThat(Math.Abs((double)(3) - (double)(peak.Height)) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(peak.Dx)) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(1.5) - (double)(flank.Height)) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(-0.375) - (double)(flank.Dx)) <= 0.001).IsTrue();
        AssertThat(outside == TerrainDifferential.Constant(0)).OverrideFailureMessage("Compact support is not exact zero.").IsTrue();
        var basin = TerrainMath.EllipticalMound(6, 0, Vector2.Zero, new(12, 24), 0, -3);
        AssertThat(basin == -flank).OverrideFailureMessage("Basin does not use the same deterministic profile.").IsTrue();
    }

    [TestCase]
    public void SmoothMinimumPreservesOutsideIdentityAndDerivative()
    {
        AssertThat(TerrainMath.SmoothMinimum(new(1, 2, 3), new(5, 4, 6), 1) == new TerrainDifferential(1, 2, 3)).OverrideFailureMessage("Smooth minimum changed the field outside its blend.").IsTrue();
        var result = TerrainMath.SmoothMinimum(new(0, 1, 0), new(0, 0, 1), 2);
        AssertThat(Math.Abs((double)(-0.5) - (double)(result.Height)) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(0.5) - (double)(result.Dx)) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(0.5) - (double)(result.Dz)) <= 0.001).IsTrue();
        var distance = TerrainMath.FilletDistance(new(1, 1, 0), 2);
        AssertThat(Math.Abs((double)(0.25) - (double)(distance.Height)) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(0.5) - (double)(distance.Dx)) <= 0.001).IsTrue();
        var constrained = TerrainMath.ClampToEnvelope(TerrainDifferential.Constant(0), TerrainDifferential.Constant(3), new(2, 1, 0), 0.25, 0.25, 2);
        AssertThat(Math.Abs((double)(2.75) - (double)(constrained.Height)) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(-0.25) - (double)(constrained.Dx)) <= 0.001).IsTrue();
    }

    [TestCase]
    public void QuantizationUsesEvenTiesAndNeverHidesOutOfRangeHeights()
    {
        AssertThat(TerrainMath.QuantizeCentimetres(0.125) == 12 && TerrainMath.QuantizeCentimetres(0.375) == 38).OverrideFailureMessage("Positive tie rounding differs.").IsTrue();
        AssertThat(TerrainMath.QuantizeCentimetres(-0.125) == -12).OverrideFailureMessage("Negative tie rounding differs.").IsTrue();
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
        AssertThat(right.SequenceEqual(repeated)).OverrideFailureMessage("Repeated terrain samples differ.").IsTrue();
        for (var z = 0; z < TerrainSampling.TileSamples; z++)
            AssertThat(left[z * 65 + 64] == right[z * 65]).OverrideFailureMessage("Neighbouring cells disagree on their shared edge.").IsTrue();
        var coarse = new short[TerrainSampling.SampleCount(1)]; terrain.FillTile(new(0, 0), 1, coarse);
        for (var z = 0; z <= 32; z++) AssertThat(coarse[z * 65] == right[z * 2 * 65]).OverrideFailureMessage("LOD changed a shared world-coordinate sample.").IsTrue();
        AssertThat(!terrain.IsFlat(new(0, 0)) && terrain.IsFlat(new(10, 10))).OverrideFailureMessage("Flat path ignored complete support bounds.").IsTrue();
        AssertThat(Math.Abs((double)(3) - (double)(terrain.HeightAt(0, 0))) <= 0.001).IsTrue();
    }

    [TestCase]
    public void WholeMapSamplingUsesItsRectAndIgnoresTile()
    {
        var rect = new MapRect(new(-80, -40), new(160, 80));
        var terrain = new TerrainHeightField(rect, (x, z) => new(TerrainMath.EllipticalMound(x, z, new(40, 20), new(120, 60), 0, 3), TerrainClass.Slope), [rect]);
        var a = new short[TerrainSampling.SampleCount(4)]; var b = new short[a.Length];
        terrain.FillTile(new(-100, 20), 4, a); terrain.FillTile(default, 4, b);
        AssertThat(a.SequenceEqual(b) && a[40 * 81 + 40] == 300 && a[0] == 0 && a[^1] == 0).OverrideFailureMessage("L4 used a fixed map extent or tile origin.").IsTrue();
    }

    [TestCase]
    public void PadAndWaterTablesAreExplicitAndCopied()
    {
        var pads = new Dictionary<FeatureId, float> { [new("fixture:pad")] = 1.5f };
        var waters = new Dictionary<FeatureId, float> { [new("fixture:water")] = -0.5f };
        var terrain = new TerrainHeightField(new(new(-64), new(64)), (_, _) => new(TerrainDifferential.Constant(0), TerrainClass.Flat), [], pads, waters);
        pads[new("fixture:pad")] = 7; waters.Clear();
        AssertThat(Math.Abs((double)(1.5) - (double)(terrain.PadY(new("fixture:pad")))) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(-0.5) - (double)(terrain.WaterY(new("fixture:water"))!.Value)) <= 0.001).IsTrue();
        AssertThat(terrain.WaterY(new("fixture:missing")) is null).OverrideFailureMessage("Unknown entity became a water body.").IsTrue();
        var heights = new short[TerrainSampling.SampleCount(0)]; terrain.FillTile(default, 0, heights);
        AssertThat(heights.All(value => value == 0) && terrain.IsFlat(default)).OverrideFailureMessage("Zero compiled terrain differs from the flat path.").IsTrue();
    }

    [TestCase]
    public void IntegerContainmentAndNearestSegmentHaveStableBoundaries()
    {
        var ring = new Vector2[] { new(0, 0), new(2, 0), new(2, 2), new(0, 2) };
        AssertThat(TerrainMath.PointInRing(2.00049, 1, ring) && !TerrainMath.PointInRing(2.00051, 1, ring)).OverrideFailureMessage("Containment is not based on millimetre-rounded coordinates.").IsTrue();
        var nearest = TerrainGeometry.ProjectToLine(0, 0, new([new(-1, 0), new(0, 1), new(1, 0)]));
        AssertThat(nearest.Segment == 0).OverrideFailureMessage("Equal distances did not select the lower segment index.").IsTrue();
        AssertThat(Math.Abs((double)(-0.5) - (double)(nearest.X)) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(0.5) - (double)(nearest.Z)) <= 0.001).IsTrue();
        var distance = TerrainGeometry.PolygonDistance(3, 1, new(ring));
        AssertThat(Math.Abs((double)(1) - (double)(distance.Height)) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(1) - (double)(distance.Dx)) <= 0.001).IsTrue(); AssertThat(Math.Abs((double)(0) - (double)(distance.Dz)) <= 0.001).IsTrue();
    }

    [TestCase]
    public void DrapeInterpolationRequiresAnExplicitDiagonalAndRejectsHoles()
    {
        AssertThat(Math.Abs((double)(0.5) - (double)(TerrainMath.TriangleSurface(0, 0, 0, 100, 0.5, 0.5, true))) <= 0.001).IsTrue();
        AssertThat(Math.Abs((double)(0) - (double)(TerrainMath.TriangleSurface(0, 0, 0, 100, 0.5, 0.5, false))) <= 0.001).IsTrue();
        try { TerrainMath.TriangleSurface(TerrainSampling.HoleCm, 0, 0, 0, 0.5, 0.5, false); throw new InvalidOperationException("Draped geometry across a hole."); }
        catch (ArgumentException) { }
    }

    private static TerrainHeightField Mound()
    {
        var support = new MapRect(new(-40, -60), new(40, 60));
        return new(new(new(-128), new(128)), (x, z) => new(TerrainMath.EllipticalMound(x, z, Vector2.Zero, new(40, 60), 0, 3), TerrainClass.Slope), [support]);
    }
}
