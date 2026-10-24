using System.Collections.Concurrent;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Kernel.Save.Tests;

[TestSuite]
public sealed class SaveLifecycleTests
{
    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task NewGameDefersPoseDefaultsUntilMapPreparationAndPreservesItsExplicitContext()
    {
        using var fixture = new SaveLifecycleFixture();
        await fixture.Start();
        fixture.Camera.BeforeDefault = () => AssertThat(fixture.Maps.HasPrepared).OverrideFailureMessage("A pose default was requested before its target map was prepared.").IsTrue();
        var commands = new ConcurrentQueue<SaveLoadEvent>();
        fixture.Save.LoadCommand += commands.Enqueue;
        var start = new WorldPreset(6.5, Season.Winter, WeatherKind.Snow, .4f);
        var result = await fixture.Pump(fixture.Save.NewGame(new("s1"), 733, start));
        AssertThat(result.Status == LoadStatus.Loaded && fixture.Camera.DefaultContext == new NewGameContext(733, fixture.Maps.Default, start, ViewMode.Fly)).OverrideFailureMessage("Deferred pose defaults lost the requested seed, map or starting preset.").IsTrue();
        var events = commands.ToArray();
        AssertThat(events.Select(value => value.Phase).SequenceEqual(new[] { SaveLoadPhase.Started, SaveLoadPhase.Completed })).IsTrue();
        AssertThat(events.All(value => value.Operation == events[0].Operation && value.Operation > 0 &&
            value.Kind == SaveLoadKind.NewGame && value.Slot == result.Slot && value.RequestedGeneration is null && value.File is null)).IsTrue();
        AssertThat(events[0].Result is null && ReferenceEquals(events[1].Result, result) && result.Gen == 0).IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task FullLoadRestoresPoseOnceAndWaitsForDestinationBeforeCompleting()
    {
        using var fixture = new SaveLifecycleFixture();
        await fixture.Start();
        await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        var commands = new ConcurrentQueue<SaveLoadEvent>();
        fixture.Save.LoadCommand += commands.Enqueue;
        fixture.World.Hours = 91.25; fixture.Camera.SetPose(new(new Vector3(30, 40, 50), 70, -20));
        fixture.Flow = AppState.Playing;
        AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame))).Status == SaveStatus.Saved).OverrideFailureMessage("Initial save failed.").IsTrue();
        AssertThat(commands.IsEmpty).OverrideFailureMessage("Saving a slot must not publish a load command.").IsTrue();
        fixture.Camera.SetPose(new(new Vector3(100, 200, 300), 0, 0));
        fixture.Flow = AppState.Entering; fixture.Log.Clear(); fixture.Ready = false;
        var instance = fixture.World.Instance;
        var loading = fixture.Save.Load(new("s1"));
        for (var frame = 0; frame < TestSamples.FrameLoop && !fixture.Log.Contains("view"); frame++) await fixture.Frames(1);
        AssertThat(!loading.IsCompleted).OverrideFailureMessage("Full load ignored destination readiness.").IsTrue();
        AssertThat(commands.ToArray().Select(value => value.Phase).SequenceEqual(new[] { SaveLoadPhase.Started })).IsTrue();
        AssertThat(fixture.Log.SequenceEqual(new[] { "world", "instance", "prepare-map", "view" })).OverrideFailureMessage("World/instance/map/pose ordering changed.").IsTrue();
        AssertThat(fixture.Camera.Pose.Position == new Vector3(30, 40, 50)).OverrideFailureMessage("Full load did not restore the saved pose.").IsTrue();
        fixture.Ready = true;
        var loaded = await fixture.Pump(loading);
        AssertThat(loaded.Status == LoadStatus.Loaded && fixture.World.Instance == instance + 1).OverrideFailureMessage("Full load did not commit exactly one new instance.").IsTrue();
        AssertThat(fixture.Log.Count(x => x == "view") == 1 && fixture.Camera.Locks == 0).OverrideFailureMessage("Pose restored more than once or retained its input lease.").IsTrue();
        var events = commands.ToArray();
        AssertThat(events.Select(value => value.Phase).SequenceEqual(new[] { SaveLoadPhase.Started, SaveLoadPhase.Completed }) &&
            events.All(value => value.Operation == events[0].Operation && value.Kind == SaveLoadKind.Slot &&
                value.Slot == loaded.Slot && value.RequestedGeneration is null && value.File is null) &&
            ReferenceEquals(events[1].Result, loaded)).IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task TemporaryNewGameAndEndedWorldNeverSelectOrWriteASlot()
    {
        using var fixture = new SaveLifecycleFixture();
        await fixture.Start();
        AssertThat(fixture.World.Instance == 0 && fixture.Save.Active is null && fixture.Log.Count == 0).OverrideFailureMessage("Reading slot summaries created a world.").IsTrue();
        fixture.Ready = false;
        var entry = fixture.Save.NewGame(null, 17);
        for (var frame = 0; frame < TestSamples.FrameLoop && !fixture.Log.Contains("view"); frame++) await fixture.Frames(1);
        AssertThat(!entry.IsCompleted && fixture.Save.Active is null).OverrideFailureMessage("Temporary new game skipped its destination or selected a slot.").IsTrue();
        fixture.Ready = true;
        AssertThat((await fixture.Pump(entry)).Status == LoadStatus.Loaded && fixture.World.Instance == 1).OverrideFailureMessage("Temporary new game did not fully restore a formal instance.").IsTrue();
        AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test))).Status == SaveStatus.Deferred).OverrideFailureMessage("Temporary new game wrote a save slot.").IsTrue();
        await fixture.Pump(fixture.Save.NewGame(new("s1"), 19));
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.World.EndInstance();
        fixture.Flow = AppState.Title;
        AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test))).Status == SaveStatus.Deferred).OverrideFailureMessage("An ended world was captured.").IsTrue();
        AssertThat((await fixture.Pump(fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(1)))).Status == SaveStatus.Deferred).OverrideFailureMessage("Title exit started another world save.").IsTrue();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == 1 && fixture.Save.Slots.Skip(1).All(slot => slot.Health == SlotHealth.Empty)).OverrideFailureMessage("Temporary entry or Title exit changed a save generation.").IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    [TestCategory(TestReason.Contract)]
    public async Task TitleTimeoutRequiresADetachedSnapshotAndKeepsRealWriteFailures(bool detached)
    {
        var operation = new SaveWriteOperation();
        if (detached) operation.MarkSnapshotDetached();
        var pending = new TaskCompletionSource<SaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var deadline = new CancellationTokenSource();
        deadline.Cancel();
        var canceled = false;
        SaveResult? title = null;
        try { title = await operation.WaitForFlush(pending.Task, SaveReason.ToTitle, new("s1"), deadline.Token); }
        catch (OperationCanceledException) { canceled = true; }
        AssertThat(detached ? title?.Status == SaveStatus.Deferred && !canceled : title is null && canceled)
            .OverrideFailureMessage("Title timeout ended a live snapshot or blocked a detached disk write.").IsTrue();
        AssertThat(!pending.Task.IsCompleted).OverrideFailureMessage("Title timeout canceled the sole disk writer.").IsTrue();
        var failure = new SaveResult(SaveStatus.Failed, new("s1"), 2, SaveErrorKeys.Io);
        pending.SetResult(failure);
        AssertThat(await operation.WaitForFlush(pending.Task, SaveReason.ToTitle, new("s1"), deadline.Token))
            .IsEqual(failure);
        operation.MarkSnapshotDetached();
        var stillWriting = new TaskCompletionSource<SaveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        canceled = false;
        try { await operation.WaitForFlush(stillWriting.Task, SaveReason.Quit, new("s1"), deadline.Token); }
        catch (OperationCanceledException) { canceled = true; }
        AssertThat(canceled && !stillWriting.Task.IsCompleted).OverrideFailureMessage("Title policy changed quit timeout or canceled its writer.").IsTrue();
        stillWriting.SetResult(new(SaveStatus.Saved, new("s1"), 3));
    }

    [TestCase(false)]
    [TestCase(true)]
    [TestCategory(TestReason.Smoke)]
    public async Task QuitTimeoutRetainsQueuedWriteAndReportsItsRealOutcome(bool endedWorld)
    {
        using var fixture = new SaveLifecycleFixture { ExpectedErrors = endedWorld ? 1 : 0 };
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Flow = AppState.Quitting;
        fixture.ThumbnailBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var quit = await fixture.Pump(fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromMilliseconds(30)));
        AssertThat(quit.Status == SaveStatus.Failed && fixture.Save.Busy && fixture.Save.State.ErrorKey == SaveErrorKeys.Timeout).OverrideFailureMessage("Timeout canceled or silently accepted a save.").IsTrue();
        if (endedWorld)
        {
            fixture.World.EndInstance();
            fixture.Flow = AppState.Title;
            AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test))).Status == SaveStatus.Deferred && fixture.Save.Busy).OverrideFailureMessage("Title save replaced an already queued write.").IsTrue();
        }
        var retry = fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(2));
        fixture.ThumbnailBarrier.SetException(new IOException("Thumbnail capture failed."));
        var result = await fixture.Pump(retry);
        AssertThat(result.Status == (endedWorld ? SaveStatus.Failed : SaveStatus.Saved) && fixture.Save.State.Last == result.Status).OverrideFailureMessage("The existing write result was hidden by title exit.").IsTrue();
        AssertThat(result.Error == (endedWorld ? SaveErrorKeys.Snapshot : null)).IsTrue();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == (endedWorld ? 1UL : 2UL)).OverrideFailureMessage("Retry launched a duplicate write or damaged the old generation.").IsTrue();
        var pose = fixture.Camera.Pose;
        fixture.Camera.SetPose(default);
        await fixture.Pump(fixture.Save.Load(new("s1")));
        AssertThat(fixture.Camera.Pose == pose).OverrideFailureMessage("Quit save did not recover the departure pose.").IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(fixture.ExpectedErrors);
        if (endedWorld) AssertThat(fixture.Diagnostics.Errors.Single()!).IsInstanceOf<InvalidOperationException>();
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task OnlyIntervalRequestsSaveAndTitleNeverWrites()
    {
        using var fixture = new SaveLifecycleFixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing; await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.AdvanceFrames((int)(SaveSchedule.AttemptSpacingSeconds / .1) + 1);
        await fixture.PumpUntilIdle();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == 1).OverrideFailureMessage("An autosave was written without a request.").IsTrue();
        fixture.Save.Request(SaveReason.Interval);
        for (var i = 0; i < 500 && fixture.Save.Slots[0].Meta?.Gen != 2; i++) await fixture.Frames(1);
        AssertThat(fixture.Save.Slots[0].Meta is { Gen: 2, Reason: SaveReason.Interval }).OverrideFailureMessage("The interval request did not save.").IsTrue();
        fixture.Flow = AppState.Title;
        fixture.Save.Request(SaveReason.Interval);
        await fixture.Frames(400);
        await fixture.PumpUntilIdle();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == 2).OverrideFailureMessage("Title saved its camera state.").IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task ForcedCaptureUsesOneMomentEvenWhenHudChangedTargetAfterWeatherTick()
    {
        using var fixture = new SaveLifecycleFixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        fixture.World.TargetHours = 91.25;
        fixture.Weather.LastTickHours = 17.5;
        var result = await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test));
        AssertThat(result.Status == SaveStatus.Saved).OverrideFailureMessage("Coherent forced capture failed.").IsTrue();
        AssertThat(fixture.Weather.CapturedMoment?.WorldHours == 91.25 && fixture.Save.Slots[0].Meta?.WorldHours == 91.25).OverrideFailureMessage("Weather used its previous tick instead of the shared save moment.").IsTrue();
        AssertThat(fixture.World.Hours == 17.5 && fixture.Weather.LastTickHours == 17.5).OverrideFailureMessage("Capturing advanced live world or weather.").IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task ActiveMapTransitionCannotWriteANewGeneration()
    {
        using var fixture = new SaveLifecycleFixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Maps.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test));
        AssertThat(result.Status == SaveStatus.Deferred && fixture.Save.Slots[0].Meta?.Gen == 1).OverrideFailureMessage("Captured across map generations.").IsTrue();
        fixture.Maps.Pending.SetResult(MapTravelResult.Arrived);
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task WorldPhaseFailureAbortsInSameFrameAndBlocksWritesUntilFullRecovery()
    {
        using var fixture = new SaveLifecycleFixture { ExpectedErrors = 1 };
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        var failure = new InvalidDataException("Injected core semantic restore failure.");
        fixture.Weather.RestoreFailure = failure;
        var failed = await fixture.Pump(fixture.Save.Load(new("s1")));
        AssertThat(failed.Status == LoadStatus.Failed && fixture.World.RestoreFaulted && fixture.Camera.Locks == 1).OverrideFailureMessage("A partially restored world remained interactive.").IsTrue();
        AssertThat(fixture.Save.State is { Last: SaveStatus.Failed, ErrorKey: SaveErrorKeys.RestoreWorld }).OverrideFailureMessage("Restore failure is not visible through the save state.").IsTrue();
        await fixture.Frames(10); // FakeWorld.Tick throws if a pending restore escaped the failing Save frame.
        AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test))).Status == SaveStatus.Failed).OverrideFailureMessage("Faulted state was saved.").IsTrue();
        AssertThat((await fixture.Pump(fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(1)))).Status == SaveStatus.Deferred).OverrideFailureMessage("Fault exit attempted to serialize the partial world.").IsTrue();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == 1).OverrideFailureMessage("Fault damaged the last good generation.").IsTrue();
        fixture.Weather.RestoreFailure = null;
        var recovered = await fixture.Pump(fixture.Save.NewGame(null, 2));
        AssertThat(recovered.Status == LoadStatus.Loaded && !fixture.World.RestoreFaulted && fixture.Camera.Locks == 0).OverrideFailureMessage("A complete temporary new game did not release the fault lease.").IsTrue();
        AssertThat(fixture.Save.Active is null && fixture.Save.Slots[0].Meta?.Gen == 1).OverrideFailureMessage("Temporary-world recovery selected a writable slot or changed the previous generation.").IsTrue();
        AssertThat(fixture.Save.State.ErrorKey is null).OverrideFailureMessage("Recovery left the stale error visible.").IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(fixture.ExpectedErrors);
        AssertThat(fixture.Diagnostics.Errors.Single()!).IsInstanceOf<InvalidDataException>();
        AssertThat(ReferenceEquals(fixture.Diagnostics.Errors.Single(), failure)).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task FailureAfterWorldCommitAlsoFreezesAndCanRecoverByLoading()
    {
        using var fixture = new SaveLifecycleFixture { ExpectedErrors = 1 };
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        fixture.World.Hours = 91.25;
        var savedPose = new Pose(new Vector3(30, 40, 50), 70, -20);
        fixture.Camera.SetPose(savedPose);
        AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame))).Status == SaveStatus.Saved)
            .OverrideFailureMessage("The recovery slot was not saved.").IsTrue();
        fixture.World.Hours = 120;
        fixture.Camera.SetPose(default);
        var failure = new InvalidDataException("Injected pose restore failure.");
        fixture.Camera.RestoreFailure = failure;
        var instance = fixture.World.Instance;
        var failed = await fixture.Pump(fixture.Save.Load(new("s1")));
        AssertThat(fixture.World.Instance == instance + 1 && fixture.World.Hours == 91.25)
            .OverrideFailureMessage("The injected pose failure did not occur after the World phase committed.").IsTrue();
        AssertThat(failed.Status == LoadStatus.Failed && fixture.World.RestoreFaulted && fixture.Camera.Locks == 1)
            .OverrideFailureMessage("A failure after the World commit released input or left the world usable.").IsTrue();
        AssertThat(fixture.Save.State is { Last: SaveStatus.Failed, ErrorKey: SaveErrorKeys.RestoreWorld })
            .OverrideFailureMessage("The post-commit failure was not reported by the save state.").IsTrue();
        await fixture.Frames(10);
        AssertThat(fixture.World.RestoreFaulted && fixture.Camera.Locks == 1 && fixture.Save.Slots[0].Meta?.Gen == 1)
            .OverrideFailureMessage("The fault or its input lock cleared without a full restore, or damaged the recovery slot.").IsTrue();

        fixture.Camera.RestoreFailure = null;
        fixture.Ready = false; fixture.Log.Clear();
        var recovering = fixture.Save.Load(new("s1"));
        for (var frame = 0; frame < TestSamples.FrameLoop && !fixture.Log.Contains("view"); frame++) await fixture.Frames(1);
        AssertThat(!recovering.IsCompleted && fixture.Camera.Locks == 1 && fixture.Camera.Pose == savedPose)
            .OverrideFailureMessage("Recovery skipped pose restoration or released input before destination readiness.").IsTrue();
        fixture.Ready = true;
        var recovered = await fixture.Pump(recovering);
        AssertThat(recovered.Status == LoadStatus.Loaded && fixture.World.Instance == instance + 2 &&
            fixture.World.Hours == 91.25 && fixture.Camera.Pose == savedPose && !fixture.World.RestoreFaulted && fixture.Camera.Locks == 0)
            .OverrideFailureMessage("Loading the valid slot did not fully recover the world and release input.").IsTrue();
        AssertThat(fixture.Save.Active == new SlotId("s1") && fixture.Save.Slots[0].Meta?.Gen == 1 && fixture.Save.State.ErrorKey is null)
            .OverrideFailureMessage("Recovery changed the saved generation or left the stale restore error visible.").IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(fixture.ExpectedErrors);
        AssertThat(fixture.Diagnostics.Errors.Single()!).IsInstanceOf<InvalidDataException>();
        AssertThat(ReferenceEquals(fixture.Diagnostics.Errors.Single(), failure)).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ChangingActiveSlotClearsItsPreviousFailureAndKeepsQueuedResultsWithTheirSlot()
    {
        using var fixture = new SaveLifecycleFixture { ExpectedErrors = 1 };
        await fixture.Start();
        var first = SaveSlots.Auto[0];
        var second = SaveSlots.Auto[1];
        await fixture.Pump(fixture.Save.NewGame(first, 1));
        fixture.Flow = AppState.Playing;
        fixture.World.CaptureFailure = new IOException("Injected capture failure.");
        var failed = await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test));
        AssertThat(failed.Status == SaveStatus.Failed && failed.Slot == first &&
            fixture.Save.State is { Last: SaveStatus.Failed, ErrorKey: SaveErrorKeys.Io } && fixture.Save.State.Slot == first)
            .OverrideFailureMessage("The first slot did not acquire the real write failure needed for this regression.").IsTrue();

        fixture.World.CaptureFailure = null;
        fixture.Flow = AppState.Entering;
        var switching = fixture.Save.NewGame(second, 2);
        var queuedForFirst = fixture.Save.SaveNow(SaveReason.Test);
        AssertThat((await fixture.Pump(switching)).Status == LoadStatus.Loaded && fixture.Save.Active == second)
            .OverrideFailureMessage("The second slot was not actually activated.").IsTrue();
        var stale = await fixture.Pump(queuedForFirst);
        AssertThat(stale.Status == SaveStatus.Deferred && stale.Slot == first)
            .OverrideFailureMessage("A queued save changed its slot while waiting for the restore.").IsTrue();
        AssertThat(fixture.Save.State is { Last: null, ErrorKey: null } && fixture.Save.State.Slot == second)
            .OverrideFailureMessage("The second slot inherited the first slot's failure or queued result.").IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(fixture.ExpectedErrors);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task FailedAutomaticAttemptsWaitThirtySecondsWhileExplicitSavesRemainImmediate()
    {
        using var fixture = new SaveLifecycleFixture { ExpectedErrors = 2 };
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(SaveSlots.Auto[0], 1));
        fixture.Flow = AppState.Playing;
        fixture.World.CaptureFailure = new IOException("Injected capture failure.");
        fixture.Save.Request(SaveReason.Interval);
        for (var i = 0; i < TestSamples.FrameLoop && fixture.Diagnostics.Errors.Count == 0; i++) await fixture.Frames(1, 0);
        await fixture.PumpUntilIdle();
        AssertThat(fixture.Diagnostics.Errors.Count == 1 && fixture.Save.State.Last == SaveStatus.Failed).IsTrue();
        var beforeRetry = (int)(SaveSchedule.AttemptSpacingSeconds / .1) - 10;
        // Advance the existing frame clock without a real timer per simulated frame;
        // drain at zero delta so an early retry cannot hide behind queued async work.
        fixture.AdvanceFrames(beforeRetry);
        await fixture.PumpUntilIdle();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(1);
        var explicitSave = await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test), 0);
        AssertThat(explicitSave.Status == SaveStatus.Failed && fixture.Diagnostics.Errors.Count == 2).IsTrue();
        fixture.World.CaptureFailure = null;
        fixture.AdvanceFrames(beforeRetry);
        await fixture.PumpUntilIdle();
        AssertThat(fixture.Diagnostics.Errors.Count == 2 && fixture.Save.Slots[0].Health == SlotHealth.Empty).IsTrue();
        fixture.AdvanceFrames(20);
        await fixture.PumpUntilIdle();
        AssertThat(fixture.Save.Slots[0].Meta is { Gen: 1, Reason: SaveReason.Interval }).IsTrue();
        fixture.Save.Request(SaveReason.Interval);
        fixture.AdvanceFrames(beforeRetry - 10);
        await fixture.PumpUntilIdle();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen).IsEqual(1UL);
        fixture.AdvanceFrames(40); await fixture.PumpUntilIdle();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen).IsEqual(2UL);
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(fixture.ExpectedErrors);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task WriteEventsExposeTheRealBusyStateAtStartAndAfterCompletion()
    {
        using var fixture = new SaveLifecycleFixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(SaveSlots.Auto[0], 1));
        fixture.Flow = AppState.Playing;
        var busy = new List<bool>();
        fixture.Save.SlotsChanged += () => busy.Add(fixture.Save.Busy);
        var result = await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test));
        AssertThat(result.Status == SaveStatus.Saved && busy.SequenceEqual(new[] { true, false })).IsTrue();
        AssertThat(!fixture.Save.Busy && fixture.Save.State.Last == SaveStatus.Saved).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ManualSavesCaptureNewThumbnailsImmediatelyAndRotateOnlyTheirOwnPool()
    {
        using var fixture = new SaveLifecycleFixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(SaveSlots.Auto[0], 1));
        fixture.Flow = AppState.Playing;
        fixture.NextThumbnail = "RIFF\u0004\0\0\0WEBP"u8.ToArray();
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame), 0);
        var previous = fixture.Save.Thumbnail(SaveSlots.Auto[0], 1);
        fixture.NextThumbnail = "RIFF\u0005\0\0\0WEBPa"u8.ToArray();
        for (var index = 0; index <= SaveSlots.ManualLimit; index++)
            AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.Manual), 0)).Status).IsEqual(SaveStatus.Saved);
        var slot = fixture.Save.Slots[0];
        AssertThat(fixture.ThumbnailCaptures == SaveSlots.ManualLimit + 2 && previous!.SequenceEqual("RIFF\u0004\0\0\0WEBP"u8.ToArray())).IsTrue();
        AssertThat(slot.Entries.Count(entry => entry.Meta?.Reason == SaveReason.Manual) == SaveSlots.ManualLimit &&
            slot.Entries.Any(entry => entry.Gen == 1) && !slot.Entries.Any(entry => entry.Gen == 2)).IsTrue();
        var cached = fixture.Save.Thumbnail(slot.Slot)!;
        File.Delete(fixture.Storage.Folder.Entry(slot.Meta!.Gen));
        cached[0] = 0;
        AssertThat(fixture.Save.Thumbnail(slot.Slot)!.SequenceEqual(fixture.NextThumbnail) &&
            fixture.Save.Thumbnail(slot.Slot, 1)!.SequenceEqual(previous)).IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ExactLoadKeepsTheLatestSummaryAndNeverRestoresAroundAnUnreadableEntry()
    {
        using var fixture = new SaveLifecycleFixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(SaveSlots.Auto[0], 1));
        fixture.Flow = AppState.Playing; fixture.World.Hours = 23;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.World.Hours = 30; await fixture.Pump(fixture.Save.SaveNow(SaveReason.Manual));
        var commands = new ConcurrentQueue<SaveLoadEvent>();
        fixture.Save.LoadCommand += commands.Enqueue;
        var loaded = await fixture.Pump(fixture.Save.Load(SaveSlots.Auto[0], 1));
        AssertThat(loaded.Status == LoadStatus.Loaded && loaded.Gen == 1 && fixture.World.Hours == 23 &&
            fixture.Save.Slots[0].Meta?.Gen == 2).IsTrue();
        var document = SaveContainer.Decode(File.ReadAllBytes(fixture.Storage.Folder.Entry(2)));
        var broken = document.Sections.ToDictionary(pair => pair.Key, pair => pair.Value);
        var version = document.Sections["world"].Version;
        broken["world"] = new(version, System.Text.Encoding.UTF8.GetBytes("{\"v\":" + version + ",\"data\":{\"worldHours\":\"bad\"}}"));
        File.WriteAllBytes(fixture.Storage.Folder.Entry(2), SaveContainer.Encode(document with { Sections = broken }));
        var instance = fixture.World.Instance;
        var rejected = await fixture.Pump(fixture.Save.Load(SaveSlots.Auto[0], 2));
        AssertThat(rejected.Status == LoadStatus.Corrupt && fixture.World.Instance == instance && fixture.World.Hours == 23).IsTrue();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == 1 && fixture.Camera.Locks == 0 && fixture.Diagnostics.Errors.Count == 0).IsTrue();
        var completed = commands.Where(value => value.Phase == SaveLoadPhase.Completed).ToArray();
        AssertThat(completed.Length == 2 && completed[0].RequestedGeneration == loaded.Gen &&
            completed[1].RequestedGeneration == rejected.Gen && completed[0].Operation != completed[1].Operation &&
            ReferenceEquals(completed[0].Result, loaded) && ReferenceEquals(completed[1].Result, rejected)).IsTrue();
    }
}
