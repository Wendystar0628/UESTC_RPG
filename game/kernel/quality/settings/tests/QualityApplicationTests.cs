using System.Security.Cryptography;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Quality;
using Campus.Contracts.Startup;
using Campus.Kernel.Quality.Settings;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed partial class QualityApplicationTests
{
    [TestCase]
    public async Task QualityTimeoutRestoresStateReleasesOwnVeilAndDoesNotSave() =>
        await VerifyFailedApplication("timeout", failDispatch: false, takeover: false, failRollback: false);

    [TestCase]
    public async Task QualityDispatchFailureRestoresStateAndReleasesOwnVeil() =>
        await VerifyFailedApplication("dispatch", failDispatch: true, takeover: false, failRollback: false);

    [TestCase]
    public async Task QualityFailureNeverRevealsAnotherFlowsVeil() =>
        await VerifyFailedApplication("takeover", failDispatch: false, takeover: true, failRollback: false);

    [TestCase]
    public async Task RollbackConsumerFailureStillReleasesOwnVeilAndReportsBothErrors() =>
        await VerifyFailedApplication("rollback", failDispatch: true, takeover: false, failRollback: true);

    private static async Task VerifyFailedApplication(string name, bool failDispatch, bool takeover, bool failRollback)
    {
        var runtime = new FakeRuntime();
        var baseline = new QualityProfile(QualityPreset.Fine,
            new Dictionary<QualityKnob, float> { [QualityKnob.ShadowDistance] = 1000 });
        var store = new FileStoreSpy(name, baseline);
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
        overrides[QualityKnob.AntiAliasing] = 1; // UnderVeil
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
            if (runtime.RevealCount != 1 || !runtime.OtherVeilActive || runtime.QualityVeilActive)
                throw new InvalidOperationException("Recovery revealed another flow's veil.");
        }
        else if (runtime.RevealCount != 1 || runtime.QualityVeilActive)
            throw new InvalidOperationException("Recovery left its quality veil covering the player.");
    }

    [TestCase]
    public async Task CliProfileStaysTemporaryWhileUserDeltaIsSaved()
    {
        var (service, _, store) = await CliSession("cli-user");
        var beforeHash = store.Hash;
        if (store.SaveCount != 0) throw new InvalidOperationException("CLI startup wrote settings.");
        var next = service.Requested.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
        next[QualityKnob.ParticleDensity] = .5f;
        await ApplyUser(service, new(service.Requested.Preset, next));
        var saved = store.Read()!;
        var values = Catalog().Resolve(saved, saved.Preset);
        if (store.SaveCount != 1 || store.Hash == beforeHash || saved.Preset != QualityPreset.Balanced ||
            values[(int)QualityKnob.ShadowDistance] != 1000 || values[(int)QualityKnob.ParticleDensity] != .5f)
            throw new InvalidOperationException("User delta was dropped or a CLI override leaked into the saved profile.");
        if (service.Requested.Preset != QualityPreset.Ultra || service.Effective[QualityKnob.ShadowDistance] != 600)
            throw new InvalidOperationException("Saving a user edit removed the live CLI overlay.");
    }

    [TestCase]
    public async Task CleanPresetSelectionPersistsTheChoiceWithoutCliOverrides()
    {
        var (service, _, store) = await CliSession("cli-preset");
        await ApplyUser(service, QualityCatalog.Profile(QualityPreset.Ultra));
        await service.Initialize(BootPhase.BeforeReveal, Context());
        var saved = store.Read()!;
        if (store.SaveCount == 0 || saved.Preset != QualityPreset.Ultra || saved.Overrides.Count != 0)
            throw new InvalidOperationException("Explicit preset selection was dropped or carried CLI overrides.");
    }

    [TestCase]
    public async Task UnbindingCancelsApplicationWaitsAndDiscardsLateResults()
    {
        foreach (var boundary in new[] { "cover", "frame", "destination", "reveal", "save" })
        {
            var (service, runtime, store) = await CliSession($"close-{boundary}");
            var changes = new List<QualityChange>();
            service.Changed += changes.Add;
            runtime.HoldAt = boundary;
            if (boundary == "save") store.HeldWrite = new();
            var next = service.Requested.Overrides.ToDictionary(pair => pair.Key, pair => pair.Value);
            next[QualityKnob.AntiAliasing] = 1;
            next[QualityKnob.ParticleDensity] = .5f;
            var pending = service.Apply(new(service.Requested.Preset, next), ApplyOrigin.User);
            service.Tick(.25);
            if (pending.IsCompleted || (boundary == "save" ? store.SaveCount != 1 : runtime.HeldCalls != 1))
                throw new InvalidOperationException($"Application did not wait at {boundary}.");
            // An active application also owns the latest deferred request.
            var queued = await service.Apply(QualityCatalog.Profile(QualityPreset.Balanced), ApplyOrigin.User);
            if (queued.Status != ApplyStatus.Deferred) throw new InvalidOperationException("Active application did not defer the next request.");
            var dispatches = runtime.DispatchCount;
            var reveals = runtime.RevealCount;
            var saves = store.SaveCount;
            service.Unbind(runtime);
            await AssertThrown(pending.WaitAsync(TimeSpan.FromSeconds(5))).ContinueWith(result => { AssertThat(result.Result).OverrideFailureMessage("Runtime shutdown did not cancel its request.").IsNotNull(); result.Result!.IsInstanceOf<OperationCanceledException>(); });
            service.Tick(20);
            if (runtime.DispatchCount != dispatches || runtime.RevealCount != reveals || store.SaveCount != saves || changes.Count != 0)
                throw new InvalidOperationException($"Closing at {boundary} allowed a late publish, rollback, reveal or save.");
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
                runtime.RevealCount != reveals || store.SaveCount != saves || changes.Count != notificationsAfterRebind)
                throw new InvalidOperationException("Old completion or queued request survived runtime replacement.");
            await ApplyUser(service, new(service.Requested.Preset, new Dictionary<QualityKnob, float> { [QualityKnob.ParticleDensity] = .5f }));
            service.Unbind(replacement);
        }
    }

    [TestCase]
    public async Task UnbindingCancelsDebouncedAndTransitionQueuedRequests()
    {
        foreach (var transitioning in new[] { false, true })
        {
            var (service, runtime, store) = await CliSession($"close-queued-{transitioning}");
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
    public async Task UnbindingCancelsInitializationAndBootApplyWithoutPublishing()
    {
        foreach (var initializing in new[] { true, false })
        {
            var runtime = new FakeRuntime();
            var store = new FileStoreSpy($"close-boot-{initializing}", null);
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

    [TestCase]
    [RequireGodotRuntime]
    public async Task RuntimeExitCancelsFrameQueueAndRejectsNewFrames()
    {
        var runtime = new QualityRuntime();
        try
        {
            var pending = runtime.NextFrame(CancellationToken.None);
            runtime._ExitTree();
            await AssertThrown(pending.WaitAsync(TimeSpan.FromSeconds(5))).ContinueWith(result => { AssertThat(result.Result).OverrideFailureMessage("Runtime shutdown did not cancel its request.").IsNotNull(); result.Result!.IsInstanceOf<OperationCanceledException>(); });
            await AssertThrown(runtime.NextFrame(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5))).ContinueWith(result => { AssertThat(result.Result).OverrideFailureMessage("Runtime shutdown did not cancel its request.").IsNotNull(); result.Result!.IsInstanceOf<OperationCanceledException>(); });
        }
        finally { runtime.Free(); }
    }

    private static async Task<(QualityService, FakeRuntime, FileStoreSpy)> CliSession(string name)
    {
        var runtime = new FakeRuntime();
        var store = new FileStoreSpy(name, QualityCatalog.Profile(QualityPreset.Balanced));
        var service = new QualityService(Catalog(), store);
        service.Bind(runtime);
        var originalHash = store.Hash;
        await service.Initialize(BootPhase.AfterMapWarm, Context());
        await service.Apply(new(QualityPreset.Ultra, new Dictionary<QualityKnob, float>
        { [QualityKnob.ShadowDistance] = 600, [QualityKnob.RenderScale] = .67f }), ApplyOrigin.CommandLine);
        if (store.Hash != originalHash || store.SaveCount != 0) throw new InvalidOperationException("CLI application touched the file.");
        runtime.Booting = false;
        return (service, runtime, store);
    }

    private static async Task<ApplyResult> ApplyUser(QualityService service, QualityProfile profile)
    {
        service.Tick(0);
        var pending = service.Apply(profile, ApplyOrigin.User);
        service.Tick(.25);
        var result = await pending;
        service.Tick(0);
        return result;
    }

    private static BootContext Context() => new(
        new BootRunOptions(),
        Path.Combine(RepositoryFixture.Root, "output", "tests", "quality"), "quality-tests");

    private sealed class FileStoreSpy : IGraphicsSettingsStore
    {
        private static int _nextId;
        private readonly string _path;
        public int SaveCount { get; private set; }
        public TaskCompletionSource? HeldWrite { get; set; }
        public string Hash => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(_path)));
        public FileStoreSpy(string scenario, QualityProfile? baseline)
        {
            var directory = Path.Combine(RepositoryFixture.Root, "output", "tests", "quality", $"{scenario}-{Environment.ProcessId}-{Interlocked.Increment(ref _nextId)}");
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, "graphics-spy.json");
            File.WriteAllBytes(_path, baseline is null ? [] : JsonSerializer.SerializeToUtf8Bytes(baseline));
        }
        public QualityProfile? Read()
        {
            var bytes = File.ReadAllBytes(_path);
            return bytes.Length == 0 ? null : JsonSerializer.Deserialize<QualityProfile>(bytes);
        }
        public Task Write(QualityProfile profile)
        {
            File.WriteAllBytes(_path, JsonSerializer.SerializeToUtf8Bytes(profile));
            SaveCount++;
            return HeldWrite?.Task ?? Task.CompletedTask;
        }
    }

    private static QualityCatalog Catalog() => new(File.ReadAllText(Path.Combine(RepositoryFixture.Root, "game", "data", "quality-presets.json")));

}
