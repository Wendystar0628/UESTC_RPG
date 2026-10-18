using Campus.Audio.Core;
using Campus.Contracts;
using Campus.Contracts.Audio;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class AudioMixTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SliderUsesSquaredAmplitudeAndCompletesInEightyMilliseconds()
    {
        var mix = new AudioMix(new(Master: 1f, Ui: 1f));
        mix.SetAudible(true, 0f);
        mix.SetSettings(new(Master: 1f, Ui: .5f));
        mix.Step(.04f);
        Near(mix.Gain(AudioGroup.Ui), .625f);
        mix.Step(.04f);
        Near(mix.Gain(AudioGroup.Ui), .25f);
        Near(AudioMix.ToDb(.25f), -12.0412f);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DeepestWorldDuckWinsAndNeverDucksUiOrMusic()
    {
        var mix = new AudioMix(new(Master: 1f, Ambience: 1f, Foley: 1f, Ui: 1f, Music: 1f));
        mix.SetAudible(true, 0f);
        mix.UpdatePages(UiSurface.Atlas);
        mix.Step(AudioMix.PageFadeSeconds);
        Near(AudioMix.ToDb(mix.Gain(AudioGroup.Ambience)), AudioMix.PageOpenDuckDb);
        mix.Duck(DuckReason.Veil, -8f, 0f, .8f);
        Near(AudioMix.ToDb(mix.Gain(AudioGroup.Ambience)), -8f);
        Near(AudioMix.ToDb(mix.Gain(AudioGroup.Foley)), -8f);
        Near(mix.Gain(AudioGroup.Ui), 1f);
        Near(mix.Gain(AudioGroup.Music), 1f);
        mix.ReleaseDuck(DuckReason.Veil);
        mix.Step(.8f);
        Near(AudioMix.ToDb(mix.Gain(AudioGroup.Ambience)), AudioMix.PageOpenDuckDb);
        mix.UpdatePages(UiSurface.AtlasIndex);
        mix.Step(AudioMix.PageFadeSeconds);
        Near(AudioMix.ToDb(mix.Gain(AudioGroup.Ambience)), AudioMix.PageOpenDuckDb);
        mix.UpdatePages(UiSurface.Minimap);
        mix.Step(AudioMix.PageFadeSeconds);
        Near(mix.Gain(AudioGroup.Ambience), 1f);
        Near(mix.Gain(AudioGroup.Foley), 1f);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BootAndBackgroundMuteApplyToEveryGroup()
    {
        var mix = new AudioMix(new(Master: 1f, Ambience: 1f, Foley: 1f, Ui: 1f, Music: 1f));
        foreach (var group in Enum.GetValues<AudioGroup>()) Near(mix.Gain(group), 0f);
        mix.SetAudible(true, .2f);
        mix.Step(.2f);
        mix.SetBackground(true, BackgroundBehavior.Lower);
        mix.Step(.2f);
        foreach (var group in Enum.GetValues<AudioGroup>()) Near(AudioMix.ToDb(mix.Gain(group)), -12f);
        mix.SetBackground(true, BackgroundBehavior.Mute);
        mix.Step(.2f);
        foreach (var group in Enum.GetValues<AudioGroup>()) Near(mix.Gain(group), 0f);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NonfiniteSettingsAreRejectedAndFiniteSlidersAreClamped()
    {
        try { AudioMix.Normalize(new(Master: float.NaN)); }
        catch (ArgumentOutOfRangeException)
        {
            var normalized = AudioMix.Normalize(new(Master: -3f, Ui: 3f));
            Near(normalized.Master, 0f); Near(normalized.Ui, 1f);
            return;
        }
        throw new InvalidOperationException("NaN audio setting was accepted.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MapTravelRestoresDuringRevealAndDoesNotRestartOnHeldFrames()
    {
        var mix = new AudioMix(new(Master: 1f, Ambience: 1f));
        mix.SetAudible(true, 0f);
        mix.UpdateVeil(0f, VeilReason.MapTravel);
        mix.UpdateVeil(.5f, VeilReason.MapTravel);
        mix.Step(.5f);
        Near(AudioMix.ToDb(mix.Gain(AudioGroup.Ambience)), -40f);
        mix.UpdateVeil(1f, VeilReason.MapTravel);
        mix.BeginMapUnload();
        mix.Step(.5f);
        mix.UpdateVeil(1f, VeilReason.MapTravel);
        mix.Step(.5f);
        Near(AudioMix.ToDb(mix.Gain(AudioGroup.Ambience)), -40f);

        mix.UpdateVeil(.96f, VeilReason.MapTravel);
        mix.Step(.1f);
        Near(mix.Gain(AudioGroup.Ambience), .01f + .99f * .1f / 2.5f);
        // A reveal can remain at the same opacity while another stage warms.
        for (var i = 0; i < 24; i++)
        {
            mix.UpdateVeil(.96f, VeilReason.MapTravel);
            mix.Step(.1f);
        }
        Near(mix.Gain(AudioGroup.Ambience), 1f);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NewCoverDuringRevealReappliesWorldDuck()
    {
        var mix = new AudioMix(new(Master: 1f, Ambience: 1f));
        mix.SetAudible(true, 0f);
        mix.UpdateVeil(1f, VeilReason.Teleport);
        mix.Step(.5f);
        mix.UpdateVeil(.7f, VeilReason.Teleport);
        mix.Step(.4f);
        if (AudioMix.ToDb(mix.Gain(AudioGroup.Ambience)) <= -8f)
            throw new InvalidOperationException("Reveal did not begin restoring the world.");
        mix.UpdateVeil(.8f, VeilReason.Teleport);
        mix.Step(.5f);
        Near(AudioMix.ToDb(mix.Gain(AudioGroup.Ambience)), -8f);
    }

    private static void Near(float actual, float expected, float tolerance = TestTolerance.FloatRelative)
    {
        if (!float.IsFinite(actual) || Math.Abs(actual - expected) > tolerance * Math.Max(1f, Math.Abs(expected)))
            throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}
