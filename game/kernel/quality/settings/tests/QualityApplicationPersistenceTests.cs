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
    [TestCategory(TestReason.Contract)]
    public async Task CliProfileStaysSessionOnlyWhileUserDeltaIsSaved()
    {
        var (service, _, store) = await CliSession("cli-user");
        using var cleanup = store;
        var beforeHash = store.Hash;
        if (store.SaveCount != 0) throw new InvalidOperationException("CLI startup wrote settings.");
        var next = service.Requested.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        next[QualityKnob.ParticleDensity] = .5f;
        await ApplyUser(service, new(service.Requested.Preset, next));
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
        await ApplyUser(service, QualityCatalog.Profile(QualityPreset.Ultra));
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
        await ApplyUser(service, new(QualityPreset.Custom, new Dictionary<QualityKnob, float> { [sharpness] = desired }));
        var saved = store.Read() ?? throw new InvalidOperationException("The independent sharpening request was not saved.");
        if (store.SaveCount != 1 || saved.Overrides[sharpness] != desired || service.Effective[sharpness] != 0 || service.Effective.Requested(sharpness) != desired)
            throw new InvalidOperationException("Inactive sharpening was confused with its requested value.");
    }

}
