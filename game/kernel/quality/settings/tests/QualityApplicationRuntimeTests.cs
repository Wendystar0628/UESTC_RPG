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
public sealed class QualityApplicationRuntimeTests
{
    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task CommandQueueReplacementAndNoChangeSuccessHaveTheirOwnBoundaries()
    {
        var (service, runtime, store) = await CliSession("command-queue");
        using var cleanup = store;
        await using var serviceCleanup = service;
        var commands = new List<QualityApplyEvent>();
        var changes = new List<QualityChange>();
        service.ApplyCommand += commands.Add;
        service.Changed += changes.Add;
        var request = Edit(service.Requested);
        var pending = service.Apply(request, ApplyOrigin.User);
        if (pending.IsCompleted || commands.Count != 1 || commands[0].Phase != QualityApplyPhase.Queued || commands[0].Result is not null)
            throw new InvalidOperationException("The immediate request did not enter its debounce queue.");
        service.Tick(0);
        AssertThat(commands.Count == 1).IsTrue();
        runtime.Transitioning = true;
        var deferred = await service.Apply(request, ApplyOrigin.User);
        var replaced = await pending;
        AssertThat(commands.Select(value => value.Phase).SequenceEqual(new[]
            { QualityApplyPhase.Queued, QualityApplyPhase.Superseded, QualityApplyPhase.Queued })).IsTrue();
        AssertThat(commands[0].Operation == commands[1].Operation && commands[2].Operation > commands[0].Operation).IsTrue();
        AssertThat(replaced.Status == ApplyStatus.Deferred && ReferenceEquals(commands[1].Result, replaced) &&
            deferred.Status == ApplyStatus.Deferred && ReferenceEquals(commands[2].Result, deferred)).IsTrue();
        service.Tick(.25);
        AssertThat(commands.Count == 3).IsTrue();
        runtime.Transitioning = false;
        service.Tick(.25);
        service.Tick(0);
        AssertThat(commands.Select(value => value.Phase).SequenceEqual(new[]
            { QualityApplyPhase.Queued, QualityApplyPhase.Superseded, QualityApplyPhase.Queued,
                QualityApplyPhase.Started, QualityApplyPhase.Completed })).IsTrue();
        AssertThat(commands.Skip(2).All(value => value.Operation == commands[2].Operation &&
            ReferenceEquals(value.Target, commands[2].Target) && value.Origin == ApplyOrigin.User)).IsTrue();
        AssertThat(commands[^1].Result?.Status == ApplyStatus.Applied && commands[3].Result is null &&
            changes.Count == 0 && store.SaveCount == 0).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task BootCommandSuccessAndFailureCloseWithoutTreatingInitializationAsApply()
    {
        var runtime = new FakeRuntime();
        using var store = new FileStoreSpy("command-boot", null);
        await using var service = new QualityService(Catalog(), store);
        service.Bind(runtime);
        var commands = new List<QualityApplyEvent>();
        service.ApplyCommand += commands.Add;
        await service.Initialize(BootPhase.AfterMapWarm, Context());
        AssertThat(commands.Count == 0).IsTrue();
        runtime.HoldAt = "frame";
        var pending = service.Apply(SelectPreset(QualityPreset.Ultra), ApplyOrigin.CommandLine);
        if (pending.IsCompleted || runtime.HeldCalls != 1 || commands.Count != 1 || commands[0].Phase != QualityApplyPhase.Started)
            throw new InvalidOperationException("Boot application did not wait for its real frame.");
        runtime.Held.SetResult(.1);
        var result = await pending;
        AssertThat(commands.Count == 2 && commands[1].Phase == QualityApplyPhase.Completed &&
            commands[0].Operation == commands[1].Operation && commands[0].Result is null &&
            ReferenceEquals(result, commands[1].Result) && result.Status == ApplyStatus.Applied).IsTrue();
        runtime.Dispatching = () => throw new InvalidOperationException();
        var failed = service.Apply(SelectPreset(QualityPreset.Balanced), ApplyOrigin.Startup);
        await AssertThrown(failed).ContinueWith(result =>
        {
            AssertThat(result.Result).IsNotNull();
            result.Result!.IsInstanceOf<InvalidOperationException>();
        });
        AssertThat(commands.Count == 4 && commands[2].Phase == QualityApplyPhase.Started &&
            commands[3].Phase == QualityApplyPhase.Faulted && commands[3].Result is null &&
            commands[2].Operation == commands[3].Operation && commands[2].Origin == ApplyOrigin.Startup &&
            store.SaveCount == 0).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ApplyCompletionWaitsForPersistenceNotificationAndLoadingLeaseDisposal()
    {
        var (service, runtime, store) = await CliSession("command-completion");
        using var cleanup = store;
        await using var serviceCleanup = service;
        var commands = new List<QualityApplyEvent>();
        var changed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        service.ApplyCommand += commands.Add;
        service.Changed += _ => changed.TrySetResult();
        store.HeldWrite = new();
        runtime.HeldRelease = new();
        try
        {
            // The durable baseline already uses 1; choose a real edit as well as replacing the CLI overlay.
            float scale = QualityCatalog.RenderScales[0];
            var catalog = Catalog();
            AssertThat(catalog.Resolve(store.Read()!, catalog.DefaultPreset)[(int)QualityKnob.RenderScale] != scale &&
                service.Effective[QualityKnob.RenderScale] != scale).IsTrue();
            var pending = service.Apply(Edit(new(service.Requested.Preset,
                new Dictionary<QualityKnob, float> { [QualityKnob.RenderScale] = scale }), QualityKnob.RenderScale), ApplyOrigin.User);
            service.Tick(.25);
            if (store.SaveCount != 1 || pending.IsCompleted || changed.Task.IsCompleted || commands.Count != 2 ||
                commands[^1].Phase != QualityApplyPhase.Started)
                throw new InvalidOperationException("Application did not wait for persistence before notification and completion.");
            store.HeldWrite.SetResult();
            await changed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            AssertThat(!pending.IsCompleted && commands.Count == 2).IsTrue();
            runtime.HeldRelease.SetResult();
            var result = await pending;
            AssertThat(commands.Count == 3 && commands[^1].Phase == QualityApplyPhase.Completed &&
                commands.All(value => value.Operation == commands[0].Operation) &&
                ReferenceEquals(commands[^1].Result, result) && !runtime.QualityLoadingActive).IsTrue();
            service.Tick(0);
        }
        finally
        {
            store.HeldWrite.TrySetResult();
            runtime.HeldRelease.TrySetResult();
        }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
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
}
