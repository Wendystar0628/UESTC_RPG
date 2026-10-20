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
public sealed class QualityApplicationRecoveryTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public async Task QualityTimeoutRestoresStateReleasesOwnLoadingAndDoesNotSave() =>
        await VerifyFailedApplication("timeout", failDispatch: false, takeover: false, failRollback: false);

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public async Task QualityDispatchFailureRestoresStateAndReleasesOwnLoading() =>
        await VerifyFailedApplication("dispatch", failDispatch: true, takeover: false, failRollback: false);

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public async Task QualityFailureNeverRevealsAnotherFlowsLoading() =>
        await VerifyFailedApplication("takeover", failDispatch: false, takeover: true, failRollback: false);

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public async Task RollbackConsumerFailureStillReleasesOwnLoadingAndReportsBothErrors() =>
        await VerifyFailedApplication("rollback", failDispatch: true, takeover: false, failRollback: true);

    private static async Task VerifyFailedApplication(string name, bool failDispatch, bool takeover, bool failRollback)
    {
        var runtime = new FakeRuntime();
        var baseline = new QualityProfile(QualityPreset.Fine,
            new Dictionary<QualityKnob, float> { [QualityKnob.ShadowDistance] = 1000 });
        using var store = new FileStoreSpy(name, baseline);
        var service = new QualityService(Catalog(), store);
        service.Bind(runtime);
        await service.Initialize(BootPhase.AfterMapWarm, Context());
        runtime.Booting = false;
        var before = service.Effective;
        var beforeProfile = service.Requested;
        var beforeHash = store.Hash;
        runtime.FailDispatch = failDispatch;
        runtime.FailRollback = failRollback;
        runtime.FailReadiness = !failDispatch;
        runtime.Takeover = takeover;
        var changes = new List<QualityChange>();
        service.Changed += changes.Add;
        var overrides = beforeProfile.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        overrides[QualityKnob.AntiAliasing] = 1; // UnderLoading
        overrides[QualityKnob.ParticleDensity] = .5f;
        var pending = service.Apply(new(beforeProfile.Preset, overrides), ApplyOrigin.User);
        service.Tick(.25);
        var failed = false;
        try { await pending; }
        catch (Exception error) when (error is TimeoutException or InvalidOperationException or AggregateException) { failed = true; }
        service.Tick(0); // Observe the fault held by the scheduler as well as the caller's task.
        if (!failed || runtime.Errors.Count == 0) throw new InvalidOperationException("Failed application was not observable.");
        foreach (var knob in QualityCatalog.All)
        {
            if (service.Effective[knob] != before[knob] || service.Effective.Requested(knob) != before.Requested(knob) ||
                runtime.EngineView![knob] != before[knob])
                throw new InvalidOperationException($"Failed quality application was not rolled back: {knob}");
        }
        if (service.Requested.Preset != beforeProfile.Preset ||
            service.Requested.Overrides.Count != beforeProfile.Overrides.Count || changes.Count == 0)
            throw new InvalidOperationException("Requested profile or rollback notification was lost.");
        if (store.SaveCount != 0 || store.Hash != beforeHash) throw new InvalidOperationException("Failed application reached persistent storage.");
        if (takeover)
        {
            if (runtime.HideCount != 1 || !runtime.OtherLoadingActive || runtime.QualityLoadingActive)
                throw new InvalidOperationException("Recovery hid another flow's screen.");
        }
        else if (runtime.HideCount != 1 || runtime.QualityLoadingActive)
            throw new InvalidOperationException("Recovery left its quality screen covering the player.");
    }
}
