using System.Text.Json;
using Campus.Contracts.Quality;
using Campus.Kernel.Quality.Adaptive;
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
        if (catalog.Knobs.Count != 27) throw new InvalidOperationException("Quality catalog is incomplete.");
        foreach (var preset in new[] { QualityPreset.Smooth, QualityPreset.Balanced, QualityPreset.Fine, QualityPreset.Ultra })
            foreach (var knob in QualityCatalog.All)
                if (!catalog.IsValid(knob, catalog.PresetValue(preset, knob))) throw new InvalidOperationException($"Invalid {preset}/{knob}.");
        if (catalog.PresetValue(QualityPreset.Smooth, QualityKnob.ReflectionInterval) != 4 ||
            catalog.PresetValue(QualityPreset.Smooth, QualityKnob.Vignette) != 1)
            throw new InvalidOperationException("Style floor/default was removed from Smooth.");
    }

    [TestCase]
    public void InvalidDiscreteAndNonFiniteValuesRejectTheWholePlan()
    {
        var state = new QualityState(Catalog());
        foreach (var pair in new[] { (QualityKnob.RenderScale, .6f), (QualityKnob.Anisotropy, 3f),
            (QualityKnob.ShadowDistance, 700f), (QualityKnob.ShadowQuality, -1f), (QualityKnob.FsrSharpness, float.NaN) })
        {
            var plan = state.Plan(new(QualityPreset.Fine, new Dictionary<QualityKnob, float> { [pair.Item1] = pair.Item2 }, true));
            if (plan.Rejected.Count != 1 || state.Effective.Version != 0) throw new InvalidOperationException("Invalid plan mutated state.");
        }
    }

    [TestCase]
    public void RestartRequestDoesNotClaimTheEngineAlreadyChanged()
    {
        var state = new QualityState(Catalog());
        var profile = new QualityProfile(QualityPreset.Fine, new Dictionary<QualityKnob, float> { [QualityKnob.GraphicsApi] = 1 }, true);
        var change = state.Commit(profile, ApplyOrigin.User, false);
        if (!state.RestartPending || state.Effective[QualityKnob.GraphicsApi] != 0 || state.Effective.Requested(QualityKnob.GraphicsApi) != 1 ||
            !change.Has(QualityKnob.GraphicsApi))
            throw new InvalidOperationException("Restart-only setting was reported as active.");
        state.Commit(profile, ApplyOrigin.Startup, true);
        if (state.RestartPending || state.Effective[QualityKnob.GraphicsApi] != 1) throw new InvalidOperationException("Restart setting was not restored on boot.");
    }

    [TestCase]
    public void DisplayRevertPreservesLaterNonDisplayEdits()
    {
        var state = new QualityState(Catalog());
        var original = state.Requested;
        state.Commit(new(QualityPreset.Fine, new Dictionary<QualityKnob, float>
        { [QualityKnob.RenderScale] = .67f, [QualityKnob.LensFlare] = 0 }, true), ApplyOrigin.User, true);
        state.Commit(state.RestoreDisplay(original), ApplyOrigin.Revert, true);
        if (state.Effective[QualityKnob.RenderScale] != 1 || state.Effective[QualityKnob.LensFlare] != 0)
            throw new InvalidOperationException("Display revert lost an unrelated edit.");
    }

    [TestCase]
    public void ProfilesAreCopiedAndManualLocksResetOnPresetSelection()
    {
        var state = new QualityState(Catalog());
        var overrides = new Dictionary<QualityKnob, float> { [QualityKnob.ShadowDistance] = 600 };
        state.Commit(new(QualityPreset.Fine, overrides, true), ApplyOrigin.User, false);
        overrides[QualityKnob.ShadowDistance] = 1600;
        if (!state.Effective.Locked(QualityKnob.ShadowDistance) || state.Requested.Overrides[QualityKnob.ShadowDistance] != 600)
            throw new InvalidOperationException("Caller mutation escaped into the service.");
        state.Commit(QualityCatalog.Profile(QualityPreset.Balanced), ApplyOrigin.User, true);
        if (state.Effective.Locked(QualityKnob.ShadowDistance) || state.Effective.Preset != QualityPreset.Balanced)
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
    public void VrrCapsBelowRefreshAndThirtyFpsChangesStepRate()
    {
        if (QualityCatalog.FrameLimit(3, true, 144) != 141 || QualityCatalog.FrameLimit(1, true, 144) != 141 ||
            QualityCatalog.FrameLimit(2, false, 60) != 30) throw new InvalidOperationException("Frame pacing is inconsistent.");
        var state = new QualityState(Catalog());
        var profile = new QualityProfile(QualityPreset.Fine, new Dictionary<QualityKnob, float> { [QualityKnob.FrameCap] = 0 }, true);
        state.Commit(profile, ApplyOrigin.User, false);
        if (state.Effective.StepRate != 15) throw new InvalidOperationException("30 fps requires a 15 Hz pose cadence.");
        state.CaptureMode = true;
        state.Commit(profile, ApplyOrigin.Gate, false);
        if (state.Effective.StepRate != 12) throw new InvalidOperationException("Capture must stay at 12 Hz.");
    }

    [TestCase]
    public void SettingsRejectObjectConstructorsBeforeGodotParsing()
    {
        foreach (var token in new[] { "Object(Node)", "Resource (\"x\")", "SubResource(1)", "ExtResource(1)" })
            if (GraphicsSettingsStore.SafeText("[profile]\nx=" + token)) throw new InvalidOperationException("Unsafe constructor passed the text guard.");
        if (!GraphicsSettingsStore.SafeText("[profile]\npreset=\"fine\"\nadaptive=true")) throw new InvalidOperationException("Basic settings were rejected.");
    }

    [TestCase]
    public void UmaIsNeverPromotedByDedicatedMemoryAndHighNeedsBudget()
    {
        var integrated = new GraphicsDevice("Intel Arc", "Intel", "test", 256UL << 20, 10UL << 30, true, true);
        if (DeviceProbe.Classify(integrated) != DeviceTier.MinSpec) throw new InvalidOperationException("UMA was mistaken for discrete graphics.");
        var discrete = integrated with { Name = "NVIDIA GeForce RTX 4080", Uma = false, DedicatedBytes = 16UL << 30, BudgetBytes = 12UL << 30 };
        if (DeviceProbe.Classify(discrete) != DeviceTier.DgpuHigh ||
            DeviceProbe.Classify(discrete with { BudgetBytes = 6UL << 30 }) != DeviceTier.DgpuMid)
            throw new InvalidOperationException("High tier ignored its memory budget.");
    }

    [TestCase]
    public void ProbeWithoutEnoughSamplesUsesAConservativeResult()
    {
        var unknown = new GraphicsDevice("Unknown", "Unknown", "test", 0, 0, null, false);
        if (DeviceProbe.FromSamples(unknown, [2, 3], [1, 2], 1920 * 1080) != DeviceTier.MinSpec)
            throw new InvalidOperationException("Insufficient evidence promoted an unknown device.");
    }

    [TestCase]
    public void AdaptiveCeilingsRespectLocksInverseQualityAndOneWayReduction()
    {
        var state = new QualityState(Catalog());
        state.Commit(new(QualityPreset.Fine, new Dictionary<QualityKnob, float> { [QualityKnob.ShadowDistance] = 1000 }, true), ApplyOrigin.User, false);
        state.SetAdaptiveCeilings(new Dictionary<QualityKnob, float>
        { [QualityKnob.ShadowDistance] = 600, [QualityKnob.ReflectionInterval] = 4, [QualityKnob.FoliageDensity] = .75f });
        if (state.Effective[QualityKnob.ShadowDistance] != 1000 || state.Effective[QualityKnob.ReflectionInterval] != 4 ||
            state.Effective[QualityKnob.FoliageDensity] != .75f || !state.Effective.Reduced(QualityKnob.FoliageDensity))
            throw new InvalidOperationException("Adaptive ceiling violated lock or inverse quality ordering.");
        state.SetAdaptiveCeilings(new Dictionary<QualityKnob, float> { [QualityKnob.FoliageDensity] = 1 });
        if (state.Effective[QualityKnob.FoliageDensity] != .75f) throw new InvalidOperationException("Ceiling silently raised quality.");
        state.Commit(state.Requested with { Adaptive = false }, ApplyOrigin.User, false);
        if (state.Effective[QualityKnob.FoliageDensity] != 1) throw new InvalidOperationException("Disabling adaptive did not restore requested quality.");
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
