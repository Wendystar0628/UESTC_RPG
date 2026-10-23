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

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task DeferredApplicationFailureReachesTickOnce()
    {
        var (service, runtime, store) = await CliSession("deferred-failure");
        using var cleanup = store;
        var original = new InvalidOperationException();
        runtime.Dispatching = () => { runtime.Dispatching = null; throw original; };
        runtime.Transitioning = true;
        var result = await service.Apply(Edit(new(service.Requested.Preset,
            new Dictionary<QualityKnob, float> { [QualityKnob.ParticleDensity] = .5f }), QualityKnob.ParticleDensity), ApplyOrigin.User);
        AssertThat(result.Status == ApplyStatus.Deferred).IsTrue();
        runtime.Transitioning = false;
        service.Tick(.25);
        Exception? observed = null;
        try { service.Tick(0); }
        catch (Exception error) { observed = error; }
        AssertThat(ReferenceEquals(observed, original)).OverrideFailureMessage("Deferred failure did not reach the frame owner.").IsTrue();
        service.Tick(0);
        AssertThat(runtime.Failures.Count == 1 && ReferenceEquals(runtime.Failures[0], original)).IsTrue();
        await service.DisposeAsync();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ObserverFailuresRunAllObserversAndRestoreAppliedAndDurableState()
    {
        var (service, runtime, store) = await CliSession("observer-failures");
        using var cleanup = store;
        var before = service.Effective;
        var beforeHash = store.Hash;
        var first = new InvalidOperationException();
        var second = new IOException();
        var calls = new int[3];
        service.Changed += _ => { if (++calls[0] == 1) throw first; };
        service.Changed += _ => { if (++calls[1] == 1) throw second; };
        service.Changed += _ => calls[2]++;
        var pending = service.Apply(Edit(new(service.Requested.Preset,
            new Dictionary<QualityKnob, float> { [QualityKnob.ParticleDensity] = .5f }), QualityKnob.ParticleDensity), ApplyOrigin.User);
        service.Tick(.25);
        Exception? observed = null;
        try { await pending; }
        catch (Exception error) { observed = error; }
        var failures = (observed as AggregateException)?.Flatten().InnerExceptions;
        AssertThat(failures is not null && failures.Count == 2 && failures.Contains(first) && failures.Contains(second))
            .OverrideFailureMessage("Observer errors lost their original objects.").IsTrue();
        AssertThat(calls.All(count => count == 2)).OverrideFailureMessage("Apply or rollback skipped an observer.").IsTrue();
        AssertThat(QualityCatalog.All.All(knob => service.Effective[knob] == before[knob]) && store.Hash == beforeHash)
            .OverrideFailureMessage("Observer failure escaped quality recovery.").IsTrue();
        service.Tick(0);
        await service.DisposeAsync();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ObserverCancellationRetainsEarlierNonCancellationFailure()
    {
        var (service, runtime, store) = await CliSession("observer-cancellation");
        using var cleanup = store;
        var original = new InvalidOperationException();
        var lateCalls = 0;
        service.Changed += _ => throw original;
        service.Changed += _ => { service.Unbind(runtime); throw new OperationCanceledException(); };
        service.Changed += _ => lateCalls++;
        var pending = service.Apply(Edit(new(service.Requested.Preset,
            new Dictionary<QualityKnob, float> { [QualityKnob.ParticleDensity] = .5f }), QualityKnob.ParticleDensity), ApplyOrigin.User);
        service.Tick(.25);
        Exception? observed = null;
        try { await pending; }
        catch (Exception error) { observed = error; }
        var failures = (observed as AggregateException)?.Flatten().InnerExceptions;
        AssertThat(pending.IsFaulted && failures is not null && failures.Count == 1 && ReferenceEquals(failures[0], original))
            .OverrideFailureMessage("Cancellation discarded an earlier observer failure.").IsTrue();
        AssertThat(lateCalls == 0).IsTrue();
        await service.DisposeAsync();
    }

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
        var pending = service.Apply(Edit(new(beforeProfile.Preset, overrides), QualityKnob.AntiAliasing, QualityKnob.ParticleDensity), ApplyOrigin.User);
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
