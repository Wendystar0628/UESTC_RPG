using Campus.Contracts.Quality;
using Campus.Kernel.Quality.Settings;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class QualityTests
{
    [TestCase]
    public void EveryPresetKeepsStyleFloorsAndHasAllKnobs()
    {
        var catalog = Catalog();
        if (catalog.Knobs.Count != 11) throw new InvalidOperationException("Quality catalog is incomplete.");
        foreach (var preset in new[] { QualityPreset.Balanced, QualityPreset.Fine, QualityPreset.Ultra })
            foreach (var knob in QualityCatalog.All)
                if (!catalog.IsValid(knob, catalog.PresetValue(preset, knob))) throw new InvalidOperationException($"Invalid {preset}/{knob}.");
    }

    [TestCase]
    public void InvalidDiscreteAndNonFiniteValuesRejectTheWholePlan()
    {
        var state = new QualityState(Catalog());
        foreach (var pair in new[] { (QualityKnob.RenderScale, .6f), (QualityKnob.ShadowDistance, 700f),
            (QualityKnob.ShadowQuality, -1f), (QualityKnob.FoliageDensity, float.NaN) })
        {
            var plan = state.Plan(new(QualityPreset.Fine, new Dictionary<QualityKnob, float> { [pair.Item1] = pair.Item2 }));
            if (plan.Rejected.Count != 1 || state.Effective.Version != 0) throw new InvalidOperationException("Invalid plan mutated state.");
        }
    }

    [TestCase]
    public void ProfilesAreCopiedAndPresetSelectionClearsOverrides()
    {
        var state = new QualityState(Catalog());
        var overrides = new Dictionary<QualityKnob, float> { [QualityKnob.ShadowDistance] = 600 };
        state.Commit(new(QualityPreset.Fine, overrides), ApplyOrigin.User, false);
        overrides[QualityKnob.ShadowDistance] = 1600;
        if (state.Effective.Preset != QualityPreset.Custom || state.Requested.Overrides[QualityKnob.ShadowDistance] != 600)
            throw new InvalidOperationException("Caller mutation escaped into the service.");
        state.Commit(QualityCatalog.Profile(QualityPreset.Balanced), ApplyOrigin.User, true);
        if (state.Requested.Overrides.Count != 0 || state.Effective.Preset != QualityPreset.Balanced)
            throw new InvalidOperationException("Preset did not clear manual overrides.");
    }

    [TestCase]
    public void ResolutionDependentPresetsUseOutputPixelBudget()
    {
        var catalog = Catalog();
        catalog.OutputWidth = 3840; catalog.OutputHeight = 2160;
        if (catalog.PresetValue(QualityPreset.Ultra, QualityKnob.RenderScale) != .77f ||
            catalog.PresetValue(QualityPreset.Fine, QualityKnob.RenderScale) != .5f)
            throw new InvalidOperationException("4K preset used the 1080p scaling column.");
    }

    [TestCase]
    public void ThirtyFpsCapChangesStepRate()
    {
        if (QualityCatalog.FrameLimit(2, 60) != 30) throw new InvalidOperationException("Frame pacing is inconsistent.");
        var state = new QualityState(Catalog());
        var profile = new QualityProfile(QualityPreset.Fine, new Dictionary<QualityKnob, float> { [QualityKnob.FrameCap] = 0 });
        state.Commit(profile, ApplyOrigin.User, false);
        if (state.Effective.StepRate != 15) throw new InvalidOperationException("30 fps requires a 15 Hz pose cadence.");
        state.CaptureMode = true;
        state.Commit(profile, ApplyOrigin.Gate, false);
        if (state.Effective.StepRate != 12) throw new InvalidOperationException("Capture must stay at 12 Hz.");
    }

    private static QualityCatalog Catalog()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "game", "data", "quality-presets.json");
            if (File.Exists(path)) return new(File.ReadAllText(path));
        }
        throw new FileNotFoundException("Cannot locate the production quality preset table.");
    }
}
