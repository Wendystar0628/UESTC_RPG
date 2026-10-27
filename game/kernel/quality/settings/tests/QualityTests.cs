using Campus.Contracts.Quality;
using Campus.Kernel.Renderer;
using Campus.Kernel.Quality.Settings;
using Campus.Kernel.Settings;
using GdUnit4;

namespace Campus.Tests;
using static Campus.Tests.QualityApplicationTestFixture;

[TestSuite]
public sealed class QualityTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ApplyAdmissionReportsRejectedAndFaultedWithoutInventingAppliedResults()
    {
        using var store = new FileStoreSpy("command-admission", null);
        await using var service = new QualityService(Catalog(), store);
        var commands = new List<QualityApplyEvent>();
        service.ApplyCommand += commands.Add;
        var invalid = Edit(new(service.Requested.Preset,
            new Dictionary<QualityKnob, float> { [QualityKnob.FoliageDensity] = float.NaN }), QualityKnob.FoliageDensity);
        var rejected = await service.Apply(invalid, ApplyOrigin.User);
        if (rejected.Status != ApplyStatus.Rejected || rejected.Rejected.Count == 0)
            throw new InvalidOperationException("Admission regression did not reach the plan rejection.");
        if (commands.Count != 1 || commands[0].Phase != QualityApplyPhase.Rejected ||
            !ReferenceEquals(commands[0].Result, rejected) || !ReferenceEquals(commands[0].Target, invalid))
            throw new InvalidOperationException("Rejected admission lost its actual request or result.");
        foreach (var (request, exceptionType) in new[]
            { (Edit(service.Requested) with { Intent = (QualityEditIntent)byte.MaxValue }, typeof(ArgumentOutOfRangeException)),
                (Edit(service.Requested), typeof(InvalidOperationException)) })
        {
            Exception? observed = null;
            try { await service.Apply(request, ApplyOrigin.User); }
            catch (Exception error) { observed = error; }
            if (observed?.GetType() != exceptionType)
                throw new InvalidOperationException("Admission regression did not preserve the original exception type.");
        }
        if (commands.Count != 3 || commands.Skip(1).Any(value => value.Phase != QualityApplyPhase.Faulted || value.Result is not null) ||
            commands.Select(value => value.Operation).Distinct().Count() != commands.Count ||
            commands.Any(value => value.Origin != ApplyOrigin.User))
            throw new InvalidOperationException("Faulted admission started work, reused an operation or invented a result.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EveryPresetKeepsStyleFloorsAndHasAllKnobs()
    {
        var catalog = Catalog();
        if (catalog.Knobs.Count != Enum.GetValues<QualityKnob>().Length) throw new InvalidOperationException("Quality catalog is incomplete.");
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
            var plan = state.Plan(Edit(new(QualityPreset.Fine, new Dictionary<QualityKnob, float> { [pair.Item1] = pair.Item2 }), pair.Item1));
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
    public void OnlyEffectOverridesMarkThePresetCustom()
    {
        var catalog = Catalog();
        var state = new QualityState(catalog);
        var preset = state.Requested.Preset;
        var overrides = new Dictionary<QualityKnob, float>();
        foreach (var spec in catalog.Knobs.Where(spec => spec.Scope != KnobScope.Effect))
        {
            overrides[spec.Knob] = spec.Values?.First(value => value != state.Effective[spec.Knob])
                ?? (state.Effective[spec.Knob] != spec.Min ? spec.Min : spec.Max);
            state.Commit(new(preset, overrides), ApplyOrigin.User, true);
            if (state.Effective.Preset != preset)
                throw new InvalidOperationException($"Non-effect override marked the preset custom: {spec.Knob}.");
        }
        var effect = catalog.Knobs.First(spec => spec.Scope == KnobScope.Effect);
        overrides[effect.Knob] = state.Effective[effect.Knob] != effect.Min ? effect.Min : effect.Max;
        state.Commit(new(preset, overrides), ApplyOrigin.User, true);
        if (state.Effective.Preset != QualityPreset.Custom)
            throw new InvalidOperationException("An effect override did not mark the preset custom.");
        overrides[effect.Knob] = catalog.PresetValue(preset, effect.Knob);
        state.Commit(new(preset, overrides), ApplyOrigin.User, true);
        if (state.Effective.Preset != preset || state.Requested.Overrides.Count == 0)
            throw new InvalidOperationException("Restoring the effect did not clear custom status while keeping non-effect overrides.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void EffectPresetSelectionPreservesIndependentRequestsAndDefaultResetsThem()
    {
        var catalog = Catalog();
        var state = new QualityState(catalog);
        var defaults = catalog.ResolveRequested(catalog.DefaultProfile, catalog.DefaultPreset);
        if (state.Effective[QualityKnob.RenderScale] != 1 || state.Effective[QualityKnob.FrameRateLimit] != 2)
            throw new InvalidOperationException("Fresh settings must render at 100% and follow display refresh.");
        state.Commit(new(QualityPreset.Fine, new Dictionary<QualityKnob, float>
        { [QualityKnob.RenderScale] = .77f, [QualityKnob.FrameRateLimit] = 1, [QualityKnob.WindowSize] = 2 }), ApplyOrigin.User, true);
        var before = state.Effective;
        foreach (var preset in new[] { QualityPreset.Balanced, QualityPreset.Ultra })
        {
            state.Commit(state.Plan(SelectPreset(preset)).Next, ApplyOrigin.User, true);
            foreach (var spec in catalog.Knobs.Where(spec => spec.Scope != KnobScope.Effect))
                if (state.Effective.Requested(spec.Knob) != before.Requested(spec.Knob))
                    throw new InvalidOperationException($"Preset changed an independent request: {spec.Knob}.");
            if (state.Effective.Preset != preset) throw new InvalidOperationException("Preset selection kept effect overrides.");
        }
        state.Commit(state.Plan(new(catalog.DefaultProfile, QualityEditIntent.RestoreDefaults, [])).Next, ApplyOrigin.User, true);
        foreach (var knob in QualityCatalog.All)
            if (state.Effective.Requested(knob) != defaults[(int)knob]) throw new InvalidOperationException($"Default did not restore {knob}.");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void FsrProfilesKeepTheRequestedSmaaAcrossPersistenceAndScaleChanges()
    {
        foreach (var origin in new[] { ApplyOrigin.Startup, ApplyOrigin.CommandLine, ApplyOrigin.User, ApplyOrigin.Gate })
        {
            var state = new QualityState(Catalog());
            var input = new Dictionary<QualityKnob, float> { [QualityKnob.RenderScale] = .77f, [QualityKnob.AntiAliasing] = 0 };
            var profile = new QualityProfile(QualityPreset.Fine, input);
            if (state.Plan(Edit(profile, QualityKnob.RenderScale, QualityKnob.AntiAliasing)).Next.Overrides[QualityKnob.AntiAliasing] != 0 || input[QualityKnob.AntiAliasing] != 0)
                throw new InvalidOperationException("FSR1 planning rewrote the requested AA.");
            state.Commit(profile, origin, true);
            if (state.Effective[QualityKnob.AntiAliasing] != 0)
                throw new InvalidOperationException("FSR1 did not retain SMAA.");
            var restored = new QualityState(Catalog());
            restored.Commit(state.Requested, ApplyOrigin.Startup, true);
            if (restored.Effective[QualityKnob.AntiAliasing] != 0 || restored.Effective[QualityKnob.RenderScale] != .77f)
                throw new InvalidOperationException("The saved profile lost the requested FSR1 and SMAA combination.");
            state.Commit(new(QualityPreset.Fine, new Dictionary<QualityKnob, float> { [QualityKnob.RenderScale] = 1 }), origin, true);
            if (state.Effective[QualityKnob.AntiAliasing] != 0)
                throw new InvalidOperationException("Returning to native rendering lost SMAA.");
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FrameRateLimitOffersSixtyHundredTwentyAndRefresh()
    {
        foreach (var vSync in new[] { false, true })
            foreach (var refreshRate in new[] { -1f, 144f })
            {
                if (FrameLimitPolicy.Resolve(0, vSync, refreshRate) != (FrameLimitPolicy.DefaultFramesPerSecond, "requested") ||
                    FrameLimitPolicy.Resolve(1, vSync, refreshRate) != (FrameLimitPolicy.HighFramesPerSecond, "requested"))
                    throw new InvalidOperationException("Selected frame limits must be independent of VSync and display refresh.");
                if (vSync && FrameLimitPolicy.Resolve(2, vSync, refreshRate) != (0, "vsync"))
                    throw new InvalidOperationException("Synchronized refresh following must leave the software cap unset.");
            }
        foreach (var refreshRate in new[] { 144, 165 })
            if (FrameLimitPolicy.Resolve(2, false, refreshRate) != (refreshRate, "refresh-rate"))
                throw new InvalidOperationException("Unsynchronized refresh following must cap at the current display rate.");
        if (FrameLimitPolicy.Resolve(2, false, 59.94f).MaxFps != FrameLimitPolicy.DefaultFramesPerSecond)
            throw new InvalidOperationException("Fractional display rates must resolve to whole frames per second.");
        foreach (var unknownRate in new[] { -1f, 0f, float.NaN, float.PositiveInfinity })
            if (FrameLimitPolicy.Resolve(2, false, unknownRate) != (FrameLimitPolicy.DefaultFramesPerSecond, "refresh-fallback"))
                throw new InvalidOperationException("Unknown display refresh must use the default frame limit.");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task GraphicsSectionDropsTheOldSchemaAndPersistsTheCurrentOne()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"quality-schema-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        try
        {
            File.WriteAllText(path, """{"graphics":{"preset":"balanced","knobs":{"render_scale":0.5}},"ui":{"scale":1.25}}""");
            await using (var settings = new SettingsStore(path))
            {
                var graphics = new GraphicsSettingsStore(settings, Catalog());
                if (graphics.Read() is not null) throw new InvalidOperationException("A legacy graphics section was accepted.");
                await graphics.Write(Catalog().DefaultProfile);
            }
            await using (var settings = new SettingsStore(path))
            {
                var catalog = Catalog();
                var restored = new GraphicsSettingsStore(settings, catalog).Read();
                if (restored is null || catalog.Resolve(restored, catalog.DefaultPreset)[(int)QualityKnob.RenderScale] != 1)
                    throw new InvalidOperationException("The current graphics schema did not round-trip.");
                if (settings.Section("ui").Float("scale", 1, .5f, 2) != 1.25f)
                    throw new InvalidOperationException("Resetting graphics changed another settings section.");
            }
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NativeSharpnessKeepsTheRequestAndReportsWhyItIsInactive()
    {
        var catalog = Catalog();
        var state = new QualityState(catalog);
        var sharpness = QualityKnob.FsrSharpness;
        var native = state.Plan(Edit(state.Requested));
        if (state.Effective[sharpness] != 0 || state.Effective.Requested(sharpness) != catalog.PresetValue(catalog.DefaultPreset, sharpness) ||
            native.Notes.Count != 1 || native.Notes[0].Knob != sharpness || native.Notes[0].Effective != 0)
            throw new InvalidOperationException("Native precision must preserve sharpening intent and expose the inactive dependency.");
        var desired = catalog[sharpness].Max;
        var change = state.Commit(new(QualityPreset.Custom, new Dictionary<QualityKnob, float> { [sharpness] = desired }), ApplyOrigin.User, false);
        if (!change.Has(sharpness) || state.Effective[sharpness] != 0 || state.Effective.Requested(sharpness) != desired)
            throw new InvalidOperationException("A request-only change was lost.");
        var restored = new QualityState(catalog);
        restored.Commit(state.Requested, ApplyOrigin.Startup, true);
        restored.Commit(restored.Plan(Edit(new(QualityPreset.Custom, new Dictionary<QualityKnob, float>
            { [QualityKnob.RenderScale] = QualityCatalog.RenderScales[0] }), QualityKnob.RenderScale)).Next, ApplyOrigin.User, true);
        if (restored.Effective[sharpness] != desired || restored.Plan(Edit(restored.Requested)).Notes.Count != 0)
            throw new InvalidOperationException("Re-enabling spatial upscaling did not restore the saved sharpening request.");
        restored.Commit(restored.Plan(Edit(new(QualityPreset.Custom, new Dictionary<QualityKnob, float>
            { [QualityKnob.RenderScale] = 1 }), QualityKnob.RenderScale)).Next, ApplyOrigin.User, true);
        if (restored.Effective[sharpness] != 0 || restored.Effective.Requested(sharpness) != desired || restored.Effective.Preset == QualityPreset.Custom)
            throw new InvalidOperationException("Clarity changes affected the effect preset or discarded the independent request.");
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public void HigherSharpnessRequestsReachTheSharperEngineEndpoint()
    {
        var spec = Catalog()[QualityKnob.FsrSharpness];
        var scale = QualityCatalog.RenderScales[0];
        if (scale >= 1 || spec.Min >= spec.Max)
            throw new InvalidOperationException("This regression requires upscaling and a selectable sharpness range.");
        var soft = ViewportClarity.MapClarity(scale, 0, spec.Min);
        var sharp = ViewportClarity.MapClarity(scale, 0, spec.Max);
        if (sharp.Sharpness >= soft.Sharpness || sharp.Sharpness != QualityRules.FsrSharpnessMin ||
            soft.Sharpness != QualityRules.FsrSharpnessMax)
            throw new InvalidOperationException("Increasing player sharpness did not reach Godot's sharper endpoint.");
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public void MeshLodUsesTheFixedVerticalReferenceAcrossOutputResizes()
    {
        var reference = ViewportClarity.MeshLodReferenceHeight;
        foreach (var (height, expected) in new[] { (reference * 2, 2f), (reference, 1f),
            (reference + reference / 3, 4f / 3), (reference, 1f) })
        {
            if (ViewportClarity.MapMeshLodThreshold(height) != expected)
                throw new InvalidOperationException("Mesh LOD did not preserve the fixed vertical reference after an output resize.");
        }
    }

    private static QualityCatalog Catalog() => new(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "quality-presets.json")));
}
