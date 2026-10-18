using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Kernel.Lighting;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class KeyLightTests
{
    private static Vector3 Dir(float altDeg, float azDeg)
    {
        var h = altDeg * MathF.PI / 180f;
        var a = azDeg * MathF.PI / 180f;
        return new(MathF.Cos(h) * MathF.Sin(a), MathF.Sin(h), -MathF.Cos(h) * MathF.Cos(a));
    }

    private static KeyLightInput Input(float sunAlt, float moonAlt = 30f, float overcast = 0f,
        float precip = 0f, float kind = 0f) =>
        new(Dir(sunAlt, 241f), sunAlt, Dir(moonAlt, 120f), moonAlt, overcast, precip, kind);

    private static float AltitudeOf(Vector3 d) => MathF.Asin(d.Y) * 180f / MathF.PI;

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DefaultDuskIsFullSunWithShadows()
    {
        var frame = new KeyLightModel().Step(Input(5.48f));
        AssertThat(frame.Body == KeyBody.Sun && frame.ShadowPass).OverrideFailureMessage("17:30 must be lit by the sun with a shadow pass.").IsTrue();
        AssertThat(frame.KeyStrength).AppendFailureMessage("key strength at 5.5°").IsEqualApprox(1f, TestTolerance.FloatRelative);
        AssertThat(frame.ShadowStrength).AppendFailureMessage("shadow strength at 5.5°").IsEqual(1f);
        AssertThat(frame.RetreatY).AppendFailureMessage("retreat line at 5.5°").IsEqual(-10f);
        AssertThat(AltitudeOf(frame.LightDir)).AppendFailureMessage("unclamped light altitude").IsEqualApprox(5.48f, TestTolerance.AngleDegrees);
        AssertThat(AltitudeOf(frame.OvercastDir)).AppendFailureMessage("overcast elevation").IsEqualApprox(55f, TestTolerance.AngleDegrees);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void KeyStrengthFollowsReviewedCurve()
    {
        AssertThat(KeyLightModel.KeyStrength(KeyBody.Sun, 0f, 0f)).AppendFailureMessage("smoothstep(−2°, 4°, 0)").IsEqualApprox(0.2593f, TestTolerance.FloatRelative);
        AssertThat(KeyLightModel.KeyStrength(KeyBody.Sun, -2f, 0f)).AppendFailureMessage("below −2°").IsEqual(0f);
        AssertThat(KeyLightModel.KeyStrength(KeyBody.Sun, 4f, 0f)).AppendFailureMessage("above 4°").IsEqual(1f);
        AssertThat(KeyLightModel.KeyStrength(KeyBody.Moon, -7f, 40f)).AppendFailureMessage("moon before −7°").IsEqual(0f);
        AssertThat(KeyLightModel.KeyStrength(KeyBody.Moon, -12f, 40f)).AppendFailureMessage("moon after −12°").IsEqual(1f);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ShadowDirectionClampsAtFourDegrees()
    {
        var frame = new KeyLightModel().Step(Input(1f));
        AssertThat(AltitudeOf(frame.LightDir)).AppendFailureMessage("clamped altitude").IsEqualApprox(4f, TestTolerance.AngleDegrees);
        var expected = Dir(4f, 241f);
        AssertThat(frame.LightDir.X).AppendFailureMessage("azimuth x").IsEqualApprox(expected.X, TestTolerance.FloatRelative);
        AssertThat(frame.LightDir.Z).AppendFailureMessage("azimuth z").IsEqualApprox(expected.Z, TestTolerance.FloatRelative);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void HandoffOnlyHappensWithZeroShadowStrength()
    {
        var model = new KeyLightModel();
        var previous = model.Step(Input(2f));
        var switches = 0;
        for (var h = 2f; h >= -24f; h -= 0.05f)
        {
            var frame = model.Step(Input(h, moonAlt: 25f));
            if (frame.Body != previous.Body)
            {
                switches++;
                AssertThat(frame.Switched && frame.ShadowStrength == 0f && !frame.ShadowPass).OverrideFailureMessage("body changed with a visible shadow").IsTrue();
                AssertThat(previous.ShadowStrength == 0f).OverrideFailureMessage("shadow before the hand-off was not faded out").IsTrue();
            }
            if (h is < -2f and > -7f) AssertThat(frame.ShadowStrength == 0f).OverrideFailureMessage($"blue hour at {h} has shadows").IsTrue();
            previous = frame;
        }
        AssertThat(switches == 1).OverrideFailureMessage($"expected one hand-off, saw {switches}").IsTrue();
        AssertThat(previous.ShadowStrength).AppendFailureMessage("full moon shadow").IsEqualApprox(0.45f, TestTolerance.FloatRelative);
        AssertThat(previous.Body == KeyBody.Moon && previous.ShadowPass).OverrideFailureMessage("night must be moon-lit with shadows").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void JumpAcrossBlueHourSwitchesThenRestoresBeforeFading()
    {
        var model = new KeyLightModel();
        model.Step(Input(5f));
        var jump = model.Step(Input(-15f));
        AssertThat(jump.Switched && jump.Body == KeyBody.Moon && jump.ShadowStrength == 0f && !jump.ShadowPass).OverrideFailureMessage("jump frame").IsTrue();
        var restore = model.Step(Input(-15f));
        AssertThat(restore.ShadowPass && restore.ShadowStrength == 0f).OverrideFailureMessage("restore frame keeps strength 0 with the pass on").IsTrue();
        var fade = model.Step(Input(-15f));
        AssertThat(fade.ShadowStrength).AppendFailureMessage("moon shadow after restore").IsEqualApprox(0.45f, TestTolerance.FloatRelative);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NewInstanceSnapsWithoutRestoreFrame()
    {
        var model = new KeyLightModel();
        model.Step(Input(-4f));
        model.Reset();
        var frame = model.Step(Input(30f));
        AssertThat(!frame.Switched && frame.ShadowPass && frame.ShadowStrength == 1f).OverrideFailureMessage("new instance must settle immediately").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void OvercastKeepsTwoStepsAndRainNightIdles()
    {
        var noon = new KeyLightModel().Step(Input(41f, overcast: 1f));
        AssertThat(noon.ShadowStrength).AppendFailureMessage("daytime overcast floor").IsEqualApprox(0.2f, TestTolerance.FloatRelative);
        var snowNight = new KeyLightModel().Step(Input(-20f, moonAlt: 40f, overcast: 0.5f, precip: 0.6f, kind: 1f));
        AssertThat(snowNight.ShadowStrength).AppendFailureMessage("snowy night keeps a lighter moon shadow").IsEqualApprox(0.45f * 0.6f, TestTolerance.FloatRelative);
        var rainNight = new KeyLightModel().Step(Input(-20f, moonAlt: 40f, overcast: 1f, precip: 0.6f));
        AssertThat(!rainNight.ShadowPass && rainNight.ShadowStrength == 0f).OverrideFailureMessage("rainy night must idle the shadow pass").IsTrue();
        var lowMoon = new KeyLightModel().Step(Input(-20f, moonAlt: 12f));
        AssertThat(!lowMoon.ShadowPass).OverrideFailureMessage("moon below 15° casts no shadow").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RetreatLineRisesThroughSunset()
    {
        AssertThat(KeyLightModel.RetreatLine(2.5f)).AppendFailureMessage("start").IsEqual(-10f);
        AssertThat(KeyLightModel.RetreatLine(0.3f)).AppendFailureMessage("end").IsEqualApprox(80f, TestTolerance.Position);
        AssertThat(KeyLightModel.RetreatLine(1.4f)).AppendFailureMessage("middle").IsEqualApprox(35f, TestTolerance.Position);
        AssertThat(new KeyLightModel().Step(Input(-20f)).RetreatY).AppendFailureMessage("moonlight is never cut by the retreat line").IsEqual(-10f);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ShadowSettingsMatchPresets()
    {
        var fine = ShadowSettings.From(1, 1600f);
        AssertThat(fine is { AtlasSize: 4096, Splits: 4, MaxDistance: 1600f }).OverrideFailureMessage("fine preset").IsTrue();
        AssertThat(fine.Split1).AppendFailureMessage("split 1").IsEqualApprox(0.03f, TestTolerance.FloatRelative);
        AssertThat(fine.Split2).AppendFailureMessage("split 2").IsEqualApprox(0.12f, TestTolerance.FloatRelative);
        AssertThat(fine.Split3).AppendFailureMessage("split 3").IsEqualApprox(0.40f, TestTolerance.FloatRelative);
        AssertThat((fine.CasterMask & ShadowSettings.LayerBit(2)) == 0 && (fine.CasterMask & ShadowSettings.LayerBit(1)) != 0).OverrideFailureMessage("layers").IsTrue();
        var twoSplits = ShadowSettings.From(0, 600f);
        AssertThat(twoSplits is { AtlasSize: 2048, Splits: 2, MaxDistance: 600f, Split1: 0.15f }).OverrideFailureMessage("two-split preset").IsTrue();
        var custom = ShadowSettings.From(2, 600f);
        AssertThat(custom.Split1 < custom.Split2 && custom.Split2 < custom.Split3 && custom.Split3 <= 0.6f).OverrideFailureMessage("custom splits ordered").IsTrue();
        var view = new QualityView(1, QualityPreset.Fine,
            Enumerable.Range(0, Enum.GetValues<QualityKnob>().Length).Select(i => (QualityKnob)i switch
            {
                QualityKnob.ShadowQuality => 2f,
                QualityKnob.ShadowDistance => 1600f,
                _ => 0f,
            }).ToArray());
        AssertThat(ShadowSettings.From(view).AtlasSize == 8192).OverrideFailureMessage("ultra atlas from the quality view").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ChannelCatalogRoundTrips()
    {
        for (var slot = 0; slot < LightChannelCatalog.Count; slot++)
            AssertThat(LightChannelCatalog.Slot(LightChannelCatalog.Id(slot)) == slot).OverrideFailureMessage($"slot {slot}").IsTrue();
        AssertThat(LightChannelCatalog.Slot(new ChannelId("missing")) == -1).OverrideFailureMessage("unknown id").IsTrue();
        AssertThat(LightChannelCatalog.Slot(new ChannelId("window.dorm")) == 1).OverrideFailureMessage("dorm slot").IsTrue();
        AssertThat(LightChannelCatalog.Slot(new ChannelId("sys.poolGain")) == 31).OverrideFailureMessage("pool gain slot").IsTrue();
    }
}
