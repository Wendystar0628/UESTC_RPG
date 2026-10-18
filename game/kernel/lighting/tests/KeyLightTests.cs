using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Kernel.Lighting;
using GdUnit4;

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
        float precip = 0f, float kind = 0f, long instance = 1) =>
        new(Dir(sunAlt, 241f), sunAlt, Dir(moonAlt, 120f), moonAlt, overcast, precip, kind, instance);

    private static float AltitudeOf(Vector3 d) => MathF.Asin(d.Y) * 180f / MathF.PI;

    private static void Near(float expected, float actual, float tolerance, string what)
    {
        if (MathF.Abs(expected - actual) > tolerance)
            throw new InvalidOperationException($"{what}: expected {expected}, got {actual}.");
    }

    private static void True(bool condition, string what)
    {
        if (!condition) throw new InvalidOperationException(what);
    }

    [TestCase]
    public void DefaultDuskIsFullSunWithShadows()
    {
        var frame = new KeyLightModel().Step(Input(5.48f));
        True(frame.Body == KeyBody.Sun && frame.ShadowPass, "17:30 must be lit by the sun with a shadow pass.");
        Near(1f, frame.KeyStrength, 1e-4f, "key strength at 5.5°");
        Near(1f, frame.ShadowStrength, 0f, "shadow strength at 5.5°");
        Near(-10f, frame.RetreatY, 0f, "retreat line at 5.5°");
        Near(5.48f, AltitudeOf(frame.LightDir), 0.01f, "unclamped light altitude");
        Near(55f, AltitudeOf(frame.OvercastDir), 0.01f, "overcast elevation");
    }

    [TestCase]
    public void KeyStrengthFollowsReviewedCurve()
    {
        Near(0.2593f, KeyLightModel.KeyStrength(KeyBody.Sun, 0f, 0f), 1e-3f, "smoothstep(−2°, 4°, 0)");
        Near(0f, KeyLightModel.KeyStrength(KeyBody.Sun, -2f, 0f), 0f, "below −2°");
        Near(1f, KeyLightModel.KeyStrength(KeyBody.Sun, 4f, 0f), 0f, "above 4°");
        Near(0f, KeyLightModel.KeyStrength(KeyBody.Moon, -7f, 40f), 0f, "moon before −7°");
        Near(1f, KeyLightModel.KeyStrength(KeyBody.Moon, -12f, 40f), 0f, "moon after −12°");
    }

    [TestCase]
    public void ShadowDirectionClampsAtFourDegrees()
    {
        var frame = new KeyLightModel().Step(Input(1f));
        Near(4f, AltitudeOf(frame.LightDir), 0.01f, "clamped altitude");
        var expected = Dir(4f, 241f);
        Near(expected.X, frame.LightDir.X, 1e-4f, "azimuth x");
        Near(expected.Z, frame.LightDir.Z, 1e-4f, "azimuth z");
    }

    [TestCase]
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
                True(frame.Switched && frame.ShadowStrength == 0f && !frame.ShadowPass, "body changed with a visible shadow");
                True(previous.ShadowStrength == 0f, "shadow before the hand-off was not faded out");
            }
            if (h is < -2f and > -7f) True(frame.ShadowStrength == 0f, $"blue hour at {h} has shadows");
            previous = frame;
        }
        True(switches == 1, $"expected one hand-off, saw {switches}");
        Near(0.45f, previous.ShadowStrength, 1e-4f, "full moon shadow");
        True(previous.Body == KeyBody.Moon && previous.ShadowPass, "night must be moon-lit with shadows");
    }

    [TestCase]
    public void JumpAcrossBlueHourSwitchesThenRestoresBeforeFading()
    {
        var model = new KeyLightModel();
        model.Step(Input(5f));
        var jump = model.Step(Input(-15f));
        True(jump.Switched && jump.Body == KeyBody.Moon && jump.ShadowStrength == 0f && !jump.ShadowPass, "jump frame");
        var restore = model.Step(Input(-15f));
        True(restore.ShadowPass && restore.ShadowStrength == 0f, "restore frame keeps strength 0 with the pass on");
        var fade = model.Step(Input(-15f));
        Near(0.45f, fade.ShadowStrength, 1e-4f, "moon shadow after restore");
    }

    [TestCase]
    public void NewInstanceSnapsWithoutRestoreFrame()
    {
        var model = new KeyLightModel();
        model.Step(Input(-4f));
        var frame = model.Step(Input(30f, instance: 2));
        True(!frame.Switched && frame.ShadowPass && frame.ShadowStrength == 1f, "new instance must settle immediately");
    }

    [TestCase]
    public void OvercastKeepsTwoStepsAndRainNightIdles()
    {
        var noon = new KeyLightModel().Step(Input(41f, overcast: 1f));
        Near(0.2f, noon.ShadowStrength, 1e-5f, "daytime overcast floor");
        var snowNight = new KeyLightModel().Step(Input(-20f, moonAlt: 40f, overcast: 0.5f, precip: 0.6f, kind: 1f));
        Near(0.45f * 0.6f, snowNight.ShadowStrength, 1e-4f, "snowy night keeps a lighter moon shadow");
        var rainNight = new KeyLightModel().Step(Input(-20f, moonAlt: 40f, overcast: 1f, precip: 0.6f));
        True(!rainNight.ShadowPass && rainNight.ShadowStrength == 0f, "rainy night must idle the shadow pass");
        var lowMoon = new KeyLightModel().Step(Input(-20f, moonAlt: 12f));
        True(!lowMoon.ShadowPass, "moon below 15° casts no shadow");
    }

    [TestCase]
    public void RetreatLineRisesThroughSunset()
    {
        Near(-10f, KeyLightModel.RetreatLine(2.5f), 0f, "start");
        Near(80f, KeyLightModel.RetreatLine(0.3f), 1e-4f, "end");
        Near(35f, KeyLightModel.RetreatLine(1.4f), 1e-3f, "middle");
        Near(-10f, new KeyLightModel().Step(Input(-20f)).RetreatY, 0f, "moonlight is never cut by the retreat line");
    }

    [TestCase]
    public void ShadowSettingsMatchPresets()
    {
        var fine = ShadowSettings.From(1, 1600f);
        True(fine is { AtlasSize: 4096, Splits: 4, MaxDistance: 1600f }, "fine preset");
        Near(0.03f, fine.Split1, 1e-6f, "split 1");
        Near(0.12f, fine.Split2, 1e-6f, "split 2");
        Near(0.40f, fine.Split3, 1e-6f, "split 3");
        True((fine.CasterMask & ShadowSettings.LayerBit(2)) == 0 && (fine.CasterMask & ShadowSettings.LayerBit(1)) != 0, "layers");
        var twoSplits = ShadowSettings.From(0, 600f);
        True(twoSplits is { AtlasSize: 2048, Splits: 2, MaxDistance: 600f, Split1: 0.15f }, "two-split preset");
        var custom = ShadowSettings.From(2, 600f);
        True(custom.Split1 < custom.Split2 && custom.Split2 < custom.Split3 && custom.Split3 <= 0.6f, "custom splits ordered");
        var view = new QualityView(1, QualityPreset.Fine,
            Enumerable.Range(0, Enum.GetValues<QualityKnob>().Length).Select(i => (QualityKnob)i switch
            {
                QualityKnob.ShadowQuality => 2f,
                QualityKnob.ShadowDistance => 1600f,
                _ => 0f,
            }).ToArray());
        True(ShadowSettings.From(view).AtlasSize == 8192, "ultra atlas from the quality view");
    }

    [TestCase]
    public void ChannelCatalogRoundTrips()
    {
        for (var slot = 0; slot < LightChannelCatalog.Count; slot++)
            True(LightChannelCatalog.Slot(LightChannelCatalog.Id(slot)) == slot, $"slot {slot}");
        True(LightChannelCatalog.Slot(new ChannelId("missing")) == -1, "unknown id");
        True(LightChannelCatalog.Slot(new ChannelId("window.dorm")) == 1, "dorm slot");
        True(LightChannelCatalog.Slot(new ChannelId("sys.poolGain")) == 31, "pool gain slot");
    }
}
