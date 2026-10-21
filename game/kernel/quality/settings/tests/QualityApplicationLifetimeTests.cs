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
public sealed class QualityApplicationLifetimeTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ConcurrentUnbindDoesNotTurnConsumerFailureIntoCancellation()
    {
        foreach (var deferred in new[] { false, true })
        {
            var (service, runtime, store) = await CliSession($"close-failure-{deferred}");
            using var cleanup = store;
            var original = new InvalidOperationException();
            runtime.Dispatching = () => { service.Unbind(runtime); throw original; };
            runtime.Transitioning = deferred;
            var pending = service.Apply(new(service.Requested.Preset,
                new Dictionary<QualityKnob, float> { [QualityKnob.ParticleDensity] = .5f }), ApplyOrigin.User);
            if (deferred) AssertThat((await pending).Status == ApplyStatus.Deferred).IsTrue();
            runtime.Transitioning = false;
            service.Tick(.25);
            if (!deferred)
            {
                Exception? observed = null;
                try { await pending; }
                catch (Exception error) { observed = error; }
                AssertThat(ReferenceEquals(observed, original)).OverrideFailureMessage("The active request lost its non-cancellation failure.").IsTrue();
            }
            AssertThat(runtime.Failures.Count == 1 && ReferenceEquals(runtime.Failures[0], original))
                .OverrideFailureMessage("The unmounted scheduler discarded its consumer failure.").IsTrue();
            await service.DisposeAsync();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task LoadingReleaseFailureRetainsTheApplicationFailureAfterUnbind()
    {
        foreach (var canceled in new[] { false, true })
        {
            var (service, runtime, store) = await CliSession($"close-release-failure-{canceled}");
            using var cleanup = store;
            var releaseFailure = new InvalidOperationException();
            runtime.HeldRelease = new();
            runtime.LoadingReleaseFailure = releaseFailure;
            runtime.FailReadiness = !canceled;
            if (canceled) runtime.Dispatching = () => service.Unbind(runtime);
            var pending = service.Apply(new(service.Requested.Preset,
                new Dictionary<QualityKnob, float> { [QualityKnob.RenderScale] = 1 }), ApplyOrigin.User);
            service.Tick(.25);
            service.Unbind(runtime);
            var shutdown = service.DisposeAsync().AsTask();
            AssertThat(!pending.IsCompleted).OverrideFailureMessage("The active request completed before releasing its loading lease.").IsTrue();
            AssertThat(!shutdown.IsCompleted).OverrideFailureMessage("Quality shutdown did not drain its unmounted application.").IsTrue();
            runtime.HeldRelease.SetResult();
            Exception? observed = null;
            try { await pending; }
            catch (Exception error) { observed = error; }
            var failures = (observed as AggregateException)?.Flatten().InnerExceptions;
            AssertThat(failures is not null && failures.Count == 2 && failures.Contains(releaseFailure) &&
                failures.Any(error => canceled ? error is OperationCanceledException : error is TimeoutException))
                .OverrideFailureMessage("Loading release replaced the original application failure.").IsTrue();
            AssertThat(runtime.Failures.Count == 1 && ReferenceEquals(runtime.Failures[0], observed)).IsTrue();
            Exception? shutdownFailure = null;
            try { await shutdown; }
            catch (Exception error) { shutdownFailure = error; }
            var shutdownFailures = (shutdownFailure as AggregateException)?.Flatten().InnerExceptions;
            AssertThat(shutdownFailures is not null && failures is not null &&
                shutdownFailures.Count == failures.Count && failures.All(shutdownFailures.Contains))
                .OverrideFailureMessage("Registry disposal did not receive the original application and cleanup failures.").IsTrue();
            AssertThat(runtime.Failures.Count == 1).OverrideFailureMessage("Disposal reported the same application failure twice.").IsTrue();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public async Task UnbindingCancelsApplicationWaitsAndDiscardsLateResults()
    {
        foreach (var boundary in new[] { "loading", "frame", "destination", "hide", "save" })
        {
            var (service, runtime, store) = await CliSession($"close-{boundary}");
            using var cleanup = store;
            var changes = new List<QualityChange>();
            service.Changed += changes.Add;
            runtime.HoldAt = boundary;
            if (boundary == "save") store.HeldWrite = new();
            var next = service.Requested.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
            // The CLI's FSR1 profile already uses FXAA; changing render scale still requires the screen.
            next[QualityKnob.RenderScale] = 1;
            next[QualityKnob.ParticleDensity] = .5f;
            var pending = service.Apply(new(service.Requested.Preset, next), ApplyOrigin.User);
            service.Tick(.25);
            if (pending.IsCompleted || (boundary == "save" ? store.SaveCount != 1 : runtime.HeldCalls != 1))
                throw new InvalidOperationException($"Application did not wait at {boundary}.");
            // An active application also owns the latest deferred request.
            var queued = await service.Apply(QualityCatalog.Profile(QualityPreset.Balanced), ApplyOrigin.User);
            if (queued.Status != ApplyStatus.Deferred) throw new InvalidOperationException("Active application did not defer the next request.");
            var dispatches = runtime.DispatchCount;
            var hides = runtime.HideCount;
            var saves = store.SaveCount;
            service.Unbind(runtime);
            await AssertThrown(pending.WaitAsync(TimeSpan.FromSeconds(5))).ContinueWith(result => { AssertThat(result.Result).OverrideFailureMessage("Runtime shutdown did not cancel its request.").IsNotNull(); result.Result!.IsInstanceOf<OperationCanceledException>(); });
            service.Tick(20);
            if (runtime.DispatchCount != dispatches || runtime.HideCount != hides || store.SaveCount != saves || changes.Count != 0)
                throw new InvalidOperationException($"Closing at {boundary} allowed a late publish, rollback, hide or save.");
            // Rebinding must not revive either request from the old runtime.
            var replacement = new FakeRuntime();
            service.Bind(replacement);
            await service.Initialize(BootPhase.AfterMapWarm, Context());
            await service.Initialize(BootPhase.BeforeReveal, Context()).WaitAsync(TimeSpan.FromSeconds(5));
            replacement.Booting = false;
            var afterRebind = replacement.DispatchCount;
            var notificationsAfterRebind = changes.Count;
            runtime.Held.TrySetResult(.1);
            store.HeldWrite?.TrySetResult();
            await Task.Yield();
            service.Unbind(runtime); // A stale node's exit cannot detach the replacement.
            service.Tick(0);
            if (replacement.DispatchCount != afterRebind || runtime.DispatchCount != dispatches ||
                runtime.HideCount != hides || store.SaveCount != saves || changes.Count != notificationsAfterRebind)
                throw new InvalidOperationException("Old completion or queued request survived runtime replacement.");
            await ApplyUser(service, new(service.Requested.Preset, new Dictionary<QualityKnob, float> { [QualityKnob.ParticleDensity] = .5f }));
            service.Unbind(replacement);
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public async Task UnbindingCancelsDebouncedAndTransitionQueuedRequests()
    {
        foreach (var transitioning in new[] { false, true })
        {
            var (service, runtime, store) = await CliSession($"close-queued-{transitioning}");
            using var cleanup = store;
            runtime.Transitioning = transitioning;
            var dispatches = runtime.DispatchCount;
            var pending = service.Apply(new(service.Requested.Preset,
                new Dictionary<QualityKnob, float> { [QualityKnob.ParticleDensity] = .5f }), ApplyOrigin.User);
            service.Unbind(runtime);
            if (transitioning)
            {
                if ((await pending).Status != ApplyStatus.Deferred) throw new InvalidOperationException("Transition request did not defer.");
            }
            else await AssertThrown(pending.WaitAsync(TimeSpan.FromSeconds(5))).ContinueWith(result => { AssertThat(result.Result).OverrideFailureMessage("Runtime shutdown did not cancel its request.").IsNotNull(); result.Result!.IsInstanceOf<OperationCanceledException>(); });
            service.Tick(20);
            if (runtime.DispatchCount != dispatches || store.SaveCount != 0)
                throw new InvalidOperationException("Unbound service ran its queued application.");
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public async Task UnbindingCancelsInitializationAndBootApplyWithoutPublishing()
    {
        foreach (var initializing in new[] { true, false })
        {
            var runtime = new FakeRuntime();
            using var store = new FileStoreSpy($"close-boot-{initializing}", null);
            var service = new QualityService(Catalog(), store);
            var token = service.Bind(runtime);
            if (!initializing) await service.Initialize(BootPhase.AfterMapWarm, Context());
            runtime.HoldAt = "frame";
            var dispatches = runtime.DispatchCount;
            var changes = 0;
            service.Changed += _ => changes++;
            var pending = initializing ? service.Initialize(BootPhase.AfterMapWarm, Context()) :
                service.Apply(QualityCatalog.Profile(QualityPreset.Ultra), ApplyOrigin.CommandLine);
            service.Unbind(runtime);
            await AssertThrown(pending.WaitAsync(TimeSpan.FromSeconds(5))).ContinueWith(result => { AssertThat(result.Result).OverrideFailureMessage("Runtime shutdown did not cancel its request.").IsNotNull(); result.Result!.IsInstanceOf<OperationCanceledException>(); });
            if (!token.IsCancellationRequested) throw new InvalidOperationException("Unbinding left the runtime lifetime open.");
            runtime.Held.TrySetResult(.1);
            await Task.Yield();
            if (runtime.DispatchCount != dispatches || changes != 0 || store.SaveCount != 0)
                throw new InvalidOperationException("Closed boot application published or saved its late frame.");
        }
    }
}
