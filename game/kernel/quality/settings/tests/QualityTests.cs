using Campus.Contracts.Quality;
using Campus.Kernel.Quality.Settings;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class QualityTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EveryPresetKeepsStyleFloorsAndHasAllKnobs()
    {
        var catalog = Catalog();
        if (catalog.Knobs.Count != 11) throw new InvalidOperationException("Quality catalog is incomplete.");
        foreach (var preset in new[] { QualityPreset.Balanced, QualityPreset.Fine, QualityPreset.Ultra })
            foreach (var knob in QualityCatalog.All)
                if (!catalog.IsValid(knob, catalog.PresetValue(preset, knob))) throw new InvalidOperationException($"Invalid {preset}/{knob}.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Contract)]
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
    [TestCategory(TestReason.Contract)]
    public void ResolutionDependentPresetsUseOutputPixelBudget()
    {
        var catalog = Catalog();
        catalog.OutputWidth = 3840; catalog.OutputHeight = 2160;
        if (catalog.PresetValue(QualityPreset.Ultra, QualityKnob.RenderScale) != .77f ||
            catalog.PresetValue(QualityPreset.Fine, QualityKnob.RenderScale) != .5f)
            throw new InvalidOperationException("4K preset used the 1080p scaling column.");
        var state = new QualityState(catalog);
        if (state.Effective[QualityKnob.AntiAliasing] != 1 || catalog.PresetValue(QualityPreset.Fine, QualityKnob.AntiAliasing) != 1 ||
            state.Requested.Overrides.ContainsKey(QualityKnob.AntiAliasing) ||
            state.Plan(QualityCatalog.Profile(QualityPreset.Ultra)).Next.Overrides.ContainsKey(QualityKnob.AntiAliasing))
            throw new InvalidOperationException("Output-derived FSR1 presets did not normalize their AA.");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void FsrProfilesNormalizeRequestsAndPersistenceWhileNativeSmaaRemainsAvailable()
    {
        foreach (var origin in new[] { ApplyOrigin.Startup, ApplyOrigin.CommandLine, ApplyOrigin.User, ApplyOrigin.Gate })
        {
            var state = new QualityState(Catalog());
            var input = new Dictionary<QualityKnob, float> { [QualityKnob.RenderScale] = .77f, [QualityKnob.AntiAliasing] = 0 };
            var profile = new QualityProfile(QualityPreset.Fine, input);
            if (state.Plan(profile).Next.Overrides[QualityKnob.AntiAliasing] != 1 || input[QualityKnob.AntiAliasing] != 0)
                throw new InvalidOperationException("FSR1 planning did not normalize a copy of the request.");
            state.Commit(profile, origin, true);
            if (state.Effective[QualityKnob.AntiAliasing] != 1 || state.Requested.Overrides[QualityKnob.AntiAliasing] != 1)
                throw new InvalidOperationException("Requested and applied FSR1 AA disagree.");
            var restored = new QualityState(Catalog());
            restored.Commit(state.Requested, ApplyOrigin.Startup, true);
            if (restored.Effective[QualityKnob.AntiAliasing] != 1 || restored.Effective[QualityKnob.RenderScale] != .77f)
                throw new InvalidOperationException("The canonical saved profile lost the supported FSR1 combination.");
            state.Commit(new(QualityPreset.Fine, new Dictionary<QualityKnob, float>
                { [QualityKnob.RenderScale] = 1, [QualityKnob.AntiAliasing] = 0 }), origin, true);
            if (state.Effective[QualityKnob.AntiAliasing] != 0 || state.Requested.Overrides.ContainsKey(QualityKnob.AntiAliasing))
                throw new InvalidOperationException("Native rendering lost SMAA or retained a stale AA override.");
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FrameRateLimitOffersSixtyHundredTwentyAndRefresh()
    {
        if (QualityCatalog.FrameLimit(0, 144) != 60 || QualityCatalog.FrameLimit(1, 144) != 120 || QualityCatalog.FrameLimit(2, 144) != 144)
            throw new InvalidOperationException("Frame rate limit must be 60, 120 or the display refresh rate (DECISION-309).");
        if (QualityCatalog.FrameLimit(2, float.NaN) != 60) throw new InvalidOperationException("An unreadable refresh rate falls back to 60.");
    }

    private static QualityCatalog Catalog() => new(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "quality-presets.json")));
}
