using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Systems.Vegetation.Tests;

[TestSuite]
public sealed class VegetationNearReadyTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ReadinessTexelsAddressWholeOwnerCellsAcrossNonAlignedMapBounds()
    {
        var edge = TileGrid.BaseEdge;
        var rect = new MapRect(new(-edge * 1.25f, -edge * .5f), new(edge * 1.25f, edge * .5f));
        AssertThat(VegetationNearReadyEncoding.Origin(rect) == new TileKey(-2, -1) &&
            VegetationNearReadyEncoding.Width(rect) == 4 && VegetationNearReadyEncoding.Height(rect) == 2).IsTrue();
        AssertThat(VegetationNearReadyEncoding.TryTexel(rect, new(-2, -1), out var x, out var y) && x == 0 && y == 0).IsTrue();
        AssertThat(VegetationNearReadyEncoding.TryTexel(rect, new(1, 0), out x, out y) && x == 3 && y == 1).IsTrue();
        AssertThat(!VegetationNearReadyEncoding.TryTexel(rect, new(-3, -1), out _, out _) &&
            !VegetationNearReadyEncoding.TryTexel(rect, new(2, 0), out _, out _)).OverrideFailureMessage("outside owner cells must not alias the edge readiness texels").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void CellHandoffsFinishBeforeReversingAcrossTheClockLoop()
    {
        var fade = new VegetationCellFade();
        var start = WorldTime.AnimLoopSeconds - RenderFade.Seconds * .5f;
        fade.Update(0f, start);
        AssertThat(fade.Active && fade.Transitioning && fade.Code > 0f).IsTrue();
        var entering = fade.Code;
        var outside = VegetationBuckets.NearReach * VegetationBuckets.Hysteresis * 2f;
        fade.Update(outside, 0f);
        AssertThat(fade.Code == entering && fade.Active).OverrideFailureMessage("an unfinished enter segment cannot reverse at the loop boundary").IsTrue();
        var reverse = RenderFade.Seconds * .75f;
        fade.Update(outside, reverse);
        AssertThat(fade.Active && fade.Transitioning && fade.Code < 0f).OverrideFailureMessage("the child remains genuinely ready throughout its exit segment").IsTrue();
        fade.Update(outside, reverse + RenderFade.Seconds * 1.5f);
        AssertThat(!fade.Active && !fade.Transitioning && fade.Code == 0f).OverrideFailureMessage("only a finished exit releases near coverage").IsTrue();
        fade.Update(0f, 0f);
        AssertThat(fade.Code == VegetationNearReadyEncoding.StartBiasSeconds).OverrideFailureMessage("a fade starting at clock zero must have an unambiguous signed code").IsTrue();
        fade.Update(0f, RenderFade.Seconds * 1.5f);
        AssertThat(fade.Active && !fade.Transitioning && fade.Code == 0f).IsTrue();
        fade.Update(0f, WorldTime.AnimLoopSeconds - RenderFade.Seconds);
        AssertThat(fade.Code == 0f && !fade.Transitioning).OverrideFailureMessage("settled coverage does not replay on a later clock loop").IsTrue();
    }
}
