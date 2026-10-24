using System.Security.Cryptography;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Quality.Settings;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;
using static Campus.Tests.QualityApplicationTestFixture;

[TestSuite]
public sealed class QualityApplicationPersistenceTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task OrdinaryCliUltraAndOtherCliOverridesNeverJoinAVignetteEdit()
    {
        var catalog = Catalog();
        var baseline = new QualityProfile(QualityPreset.Balanced, new Dictionary<QualityKnob, float>
            { [QualityKnob.RenderScale] = .77f });
        var overlays = new[]
        {
            new Dictionary<QualityKnob, float>(),
            new Dictionary<QualityKnob, float>
                { [QualityKnob.ShadowDistance] = catalog[QualityKnob.ShadowDistance].Min, [QualityKnob.RenderScale] = .67f },
            new Dictionary<QualityKnob, float>
                { [QualityKnob.Vignette] = catalog[QualityKnob.Vignette].Min },
        };
        for (var index = 0; index < overlays.Length; index++)
        {
            var overlay = overlays[index];
            var (service, _, store) = await Session($"cli-vignette-{index}", baseline,
                new QualityBootOptions { Preset = QualityPreset.Ultra, Overrides = overlay });
            await using var serviceCleanup = service;
            using var cleanup = store;
            var cliProfile = new QualityProfile(QualityPreset.Ultra, baseline.Overrides.Concat(overlay)
                .GroupBy(pair => pair.Key).ToDictionary(group => group.Key, group => group.Last().Value));
            var cliValues = catalog.ResolveRequested(cliProfile, QualityPreset.Ultra);
            if (service.Requested.Preset != QualityPreset.Ultra ||
                QualityCatalog.All.Any(knob => service.Effective.Requested(knob) != cliValues[(int)knob]))
                throw new InvalidOperationException("The ordinary CLI Ultra overlay did not reach the live session.");
            var vignette = QualityKnob.Vignette;
            var requested = service.Requested.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
            requested[vignette] = catalog[vignette].Min;
            await ApplyUser(service, Edit(new(service.Requested.Preset, requested), vignette));
            var expected = catalog.ResolveRequested(baseline, baseline.Preset);
            expected[(int)vignette] = requested[vignette];
            AssertSaved(store, baseline.Preset, expected);
        }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ExplicitPresetAndLaterEditsPreserveOnlyDurableIndependentPreferences()
    {
        var catalog = Catalog();
        var baseline = new QualityProfile(QualityPreset.Balanced, new Dictionary<QualityKnob, float>
            { [QualityKnob.RenderScale] = .77f, [QualityKnob.Vignette] = catalog[QualityKnob.Vignette].Min });
        var (service, _, store) = await Session("preset-intent", baseline, new QualityBootOptions
        {
            Preset = QualityPreset.Ultra,
            Overrides = new Dictionary<QualityKnob, float>
                { [QualityKnob.RenderScale] = .67f, [QualityKnob.ShadowDistance] = catalog[QualityKnob.ShadowDistance].Min },
        });
        await using var serviceCleanup = service;
        using var cleanup = store;
        if (service.Requested.Preset != QualityPreset.Ultra || service.Effective[QualityKnob.RenderScale] != .67f)
            throw new InvalidOperationException("The CLI preset and independent override were not applied.");
        var edited = QualityKnob.ParticleDensity;
        var profile = new QualityProfile(QualityPreset.Fine, new Dictionary<QualityKnob, float>
            { [edited] = catalog[edited].Min });
        await ApplyUser(service, new(profile, QualityEditIntent.SelectPreset, [edited]));
        var expected = catalog.ResolveRequested(new(QualityPreset.Fine, baseline.Overrides), QualityPreset.Fine);
        expected[(int)edited] = catalog[edited].Min;
        AssertSaved(store, QualityPreset.Fine, expected);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task RestoreDefaultsResetsEveryScopeBeforeAnyLaterExplicitEdit()
    {
        var catalog = Catalog();
        foreach (var editAfterReset in new[] { false, true })
        {
            var baseline = new QualityProfile(QualityPreset.Balanced, new Dictionary<QualityKnob, float>
                { [QualityKnob.RenderScale] = .77f, [QualityKnob.Vignette] = catalog[QualityKnob.Vignette].Min });
            var (service, _, store) = await Session($"reset-intent-{editAfterReset}", baseline,
                new QualityBootOptions { Preset = QualityPreset.Ultra });
            await using var serviceCleanup = service;
            using var cleanup = store;
            if (service.Requested.Preset != QualityPreset.Ultra || service.Effective[QualityKnob.RenderScale] != .77f)
                throw new InvalidOperationException("Reset regression did not start with CLI Ultra and an independent preference.");
            var edited = QualityKnob.VSync;
            var overrides = service.Default.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (editAfterReset) overrides[edited] = catalog[edited].Min;
            var request = new QualityRequest(service.Default with { Overrides = overrides },
                QualityEditIntent.RestoreDefaults, editAfterReset ? [edited] : []);
            await ApplyUser(service, request);
            var expected = catalog.ResolveRequested(catalog.DefaultProfile, catalog.DefaultPreset);
            if (editAfterReset) expected[(int)edited] = catalog[edited].Min;
            AssertSaved(store, catalog.DefaultPreset, expected);
        }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task QueuedRequestFreezesTheExplicitEditAndItsValue()
    {
        var catalog = Catalog();
        var baseline = QualityCatalog.Profile(QualityPreset.Balanced);
        var (service, runtime, store) = await Session("frozen-intent", baseline,
            new QualityBootOptions { Preset = QualityPreset.Ultra });
        await using var serviceCleanup = service;
        using var cleanup = store;
        if (service.Requested.Preset != QualityPreset.Ultra)
            throw new InvalidOperationException("Request freeze regression requires the live CLI preset.");
        var knob = QualityKnob.Vignette;
        var values = new Dictionary<QualityKnob, float> { [knob] = catalog[knob].Min };
        var edits = new List<QualityKnob> { knob };
        var commands = new List<QualityApplyEvent>();
        service.ApplyCommand += commands.Add;
        runtime.Transitioning = true;
        await service.Apply(new(new(QualityPreset.Ultra, values), QualityEditIntent.EditKnobs, edits), ApplyOrigin.User);
        values[knob] = catalog[knob].Max;
        edits.Clear();
        edits.Add(QualityKnob.ParticleDensity);
        runtime.Transitioning = false;
        service.Tick(.25);
        service.Tick(0);
        AssertThat(commands.Select(value => value.Phase).SequenceEqual(new[]
            { QualityApplyPhase.Queued, QualityApplyPhase.Started, QualityApplyPhase.Completed }) &&
            commands.All(value => value.Target.EditedKnobs.SequenceEqual(new[] { knob }) &&
                value.Target.Profile.Overrides[knob] == catalog[knob].Min)).IsTrue();
        var expected = catalog.ResolveRequested(baseline, baseline.Preset);
        expected[(int)knob] = catalog[knob].Min;
        AssertSaved(store, baseline.Preset, expected);
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public async Task CliProfileStaysSessionOnlyWhileUserDeltaIsSaved()
    {
        var (service, _, store) = await CliSession("cli-user");
        using var cleanup = store;
        var beforeHash = store.Hash;
        if (store.SaveCount != 0) throw new InvalidOperationException("CLI startup wrote settings.");
        var next = service.Requested.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        next[QualityKnob.ParticleDensity] = .5f;
        await ApplyUser(service, Edit(new(service.Requested.Preset, next), QualityKnob.ParticleDensity));
        var saved = store.Read()!;
        var values = Catalog().Resolve(saved, saved.Preset);
        var balanced = Catalog().Resolve(QualityCatalog.Profile(QualityPreset.Balanced), QualityPreset.Balanced);
        if (store.SaveCount != 1 || store.Hash == beforeHash || saved.Preset != QualityPreset.Balanced ||
            values[(int)QualityKnob.ShadowDistance] != balanced[(int)QualityKnob.ShadowDistance] || values[(int)QualityKnob.ParticleDensity] != .5f)
            throw new InvalidOperationException("User delta was dropped or a CLI override leaked into the saved profile.");
        if (service.Requested.Preset != QualityPreset.Ultra || service.Effective[QualityKnob.ShadowDistance] != CliShadowDistance)
            throw new InvalidOperationException("Saving a user edit removed the live CLI overlay.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public async Task CleanPresetSelectionPersistsTheChoiceWithoutCliOverrides()
    {
        var (service, _, store) = await CliSession("cli-preset");
        using var cleanup = store;
        await ApplyUser(service, SelectPreset(QualityPreset.Ultra));
        await service.Initialize(BootPhase.BeforeReveal, Context());
        var saved = store.Read()!;
        if (store.SaveCount == 0 || saved.Preset != QualityPreset.Ultra || saved.Overrides.Count != 0)
            throw new InvalidOperationException("Explicit preset selection was dropped or carried CLI overrides.");
        if (service.Effective[QualityKnob.RenderScale] != .67f || service.Effective.Preset != QualityPreset.Ultra)
            throw new InvalidOperationException("Effect preset selection changed the session's independent precision request.");
    }
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task NativeSharpnessRequestIsSavedEvenThoughItHasNoCurrentGpuEffect()
    {
        var runtime = new FakeRuntime();
        using var store = new FileStoreSpy("native-sharpness", null);
        await using var service = new QualityService(Catalog(), store);
        service.Bind(runtime);
        await service.Initialize(BootPhase.AfterMapWarm, Context());
        runtime.Booting = false;
        var sharpness = QualityKnob.FsrSharpness;
        if (service.Effective[QualityKnob.RenderScale] != 1 || service.Effective[sharpness] != 0)
            throw new InvalidOperationException("This regression requires native rendering with inactive sharpening.");
        var desired = service.Knobs.Single(spec => spec.Knob == sharpness).Max;
        await ApplyUser(service, Edit(new(QualityPreset.Custom, new Dictionary<QualityKnob, float> { [sharpness] = desired }), sharpness));
        var saved = store.Read() ?? throw new InvalidOperationException("The independent sharpening request was not saved.");
        if (store.SaveCount != 1 || saved.Overrides[sharpness] != desired || service.Effective[sharpness] != 0 || service.Effective.Requested(sharpness) != desired)
            throw new InvalidOperationException("Inactive sharpening was confused with its requested value.");
    }

    private static void AssertSaved(FileStoreSpy store, QualityPreset preset, IReadOnlyList<float> expected)
    {
        var saved = store.Read() ?? throw new InvalidOperationException("The explicit user intent was not persisted.");
        var values = Catalog().ResolveRequested(saved, saved.Preset);
        if (saved.Preset != preset || QualityCatalog.All.Any(knob => values[(int)knob] != expected[(int)knob]))
            throw new InvalidOperationException("The saved graphics profile differs from the explicit user intent.");
    }
}
