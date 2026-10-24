using System.Collections.Concurrent;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Kernel.Save.Tests;

[TestSuite]
public sealed class SaveShutdownTests
{
    [TestCase, TestCategory(TestReason.Regression)]
    public async Task ThrowingLoadObserversPreserveRestorationAndReachDisposal()
    {
        var f = new SaveLifecycleFixture();
        var original = new IOException();
        Exception? retained = null;
        try
        {
            await f.Start();
            var observed = new ConcurrentQueue<SaveLoadEvent>();
            f.Save.LoadCommand += _ => throw original;
            f.Save.LoadCommand += observed.Enqueue;
            var result = await f.Pump(f.Save.NewGame(SaveSlots.Auto[0], 1));
            AssertThat(result.Status == LoadStatus.Loaded && !f.World.RestoreFaulted && f.Diagnostics.Errors.Count == 0 &&
                observed.Count == 2 && observed.Last().Phase == SaveLoadPhase.Completed && observed.Last().Result == result).IsTrue();
        }
        finally
        {
            try { await f.DisposeAsync(); }
            catch (Exception error) { retained = error; }
        }
        AssertThat(ReferenceEquals(retained, original)).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task ShutdownTracksLoadBeforeItsSynchronousObserverReturns()
    {
        await using var f = new SaveLifecycleFixture();
        await f.Start();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Save.LoadCommand += value =>
        {
            if (value.Phase != SaveLoadPhase.Started) return;
            entered.TrySetResult();
            release.Wait();
        };
        var loading = Task.Run(() => f.Save.Load(SaveSlots.Auto[0]));
        Task? closing = null;
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            closing = f.Save.DisposeAsync().AsTask();
            AssertThat(!closing.IsCompleted && !loading.IsCompleted).IsTrue();
        }
        finally { release.Set(); }
        try { await loading; }
        catch (OperationCanceledException) when (loading.IsCanceled) { }
        if (closing is not null) await closing.WaitAsync(TimeSpan.FromSeconds(5));
        AssertThat(loading.IsCanceled && f.Diagnostics.Errors.Count == 0).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ClosingDuringTheInitialScanDrainsTheWorkerAndReleasesItsUnclaimedLock()
    {
        await using var f = new SaveLifecycleFixture();
        await f.Seed(); f.Storage.BlockReads();
        var published = 0; f.Save.SlotsChanged += () => published++;
        var initializing = f.Initialize();
        await f.Pump(Task.WhenAny(f.Storage.ReadEntered.Task, initializing));
        AssertThat(f.Storage.ReadEntered.Task.IsCompletedSuccessfully && !initializing.IsCompleted).IsTrue();
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !initializing.IsCompleted).IsTrue();
        f.Storage.ReleaseReads(); await closing;
        AssertThat(initializing.IsCanceled && published == 0 && !f.Save.Enabled).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Storage.Folder.Path, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task StoppingTheFrameQueueCancelsPendingAndLateCallbacksWithoutRunningTheirBodies()
    {
        await using var save = new SaveService();
        var ran = false;
        var pending = save.OnFrame(() => ran = true);
        save.StopNewWork();
        var late = save.OnFrame(() => ran = true);
        AssertThat(pending.IsCanceled && late.IsCanceled && !ran).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task CancelingAQueuedRepositoryHandoffLeavesTheProcessLockWithItsProducer()
    {
        await using var f = new SaveLifecycleFixture();
        using (var prepared = await SaveInitialization.Prepare(new(f.Storage.Folder.Path, true, null, null, 1),
            new([]), CancellationToken.None))
        {
            var handoff = f.Save.OnFrame(prepared.TakeRepository);
            f.Save.StopNewWork();
            AssertThat(handoff.IsCanceled).IsTrue();
        }
        using var unlocked = File.Open(Path.Combine(f.Storage.Folder.Path, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    [TestCategory(TestReason.Regression)]
    public async Task ClosingAfterSnapshotDetachmentWaitsForRealReadbackAndKeepsItsOutcome(bool failReadback)
    {
        await using var f = new SaveLifecycleFixture { ExpectedErrors = failReadback ? 1 : 0 };
        await f.Pump(f.Initialize());
        await f.Pump(f.Save.NewGame(SaveSlots.Auto[0], 1));
        AssertThat((await f.Pump(f.Save.SaveNow(SaveReason.Test))).Status).IsEqual(SaveStatus.Saved);
        var previous = File.ReadAllBytes(f.Storage.Folder.Entry(1));
        f.Storage.BlockReadback();
        if (failReadback) f.Storage.ReadFailure = new IOException("Injected readback failure.");
        var writing = f.Save.SaveNow(SaveReason.Test);
        await f.Pump(Task.WhenAny(f.Storage.ReadEntered.Task, writing));
        AssertThat(f.Storage.ReadEntered.Task.IsCompletedSuccessfully)
            .OverrideFailureMessage("Save finished before its generation reached readback.").IsTrue();
        var staging = f.Storage.StagingFile;
        AssertThat(staging is not null && !File.Exists(f.Storage.Folder.Entry(2)) && File.ReadAllBytes(f.Storage.Folder.Entry(1)).SequenceEqual(previous))
            .OverrideFailureMessage("The blocked readback must belong to an unpublished staging file and preserve the previous generation.").IsTrue();
        var staged = SaveContainer.Decode(File.ReadAllBytes(staging!));
        AssertThat(staged.Meta.Slot == SaveSlots.Auto[0] && staged.Meta.Gen == 2).IsTrue();
        f.World.EndInstance();
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !writing.IsCompleted).IsTrue();
        f.Storage.ReleaseReads(); await closing;
        var result = await writing;
        AssertThat(result.Status == (failReadback ? SaveStatus.Failed : SaveStatus.Saved)).IsTrue();
        AssertThat(result.Error == (failReadback ? SaveErrorKeys.Io : null)).IsTrue();
        AssertThat(File.Exists(f.Storage.Folder.Entry(2)) == !failReadback && !f.Save.Busy &&
            File.ReadAllBytes(f.Storage.Folder.Entry(1)).SequenceEqual(previous)).IsTrue();
        AssertThat(f.Storage.StagingFile is null).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Storage.Folder.Path, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(f.ExpectedErrors);
        if (failReadback)
        {
            AssertThat(f.Diagnostics.Errors.Single()!).IsInstanceOf<IOException>();
            AssertThat(ReferenceEquals(f.Diagnostics.Errors.Single(), f.Storage.ReadFailure)).IsTrue();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    [TestCategory(TestReason.Regression)]
    public async Task ClosingDuringAReadDrainsTheWorkerAndQueuedNewGameBeforeReleasingTheLock(bool external)
    {
        await using var f = new SaveLifecycleFixture();
        await f.Seed(); await f.Pump(f.Initialize());
        f.Storage.BlockReads();
        var commands = new ConcurrentQueue<SaveLoadEvent>();
        f.Save.LoadCommand += commands.Enqueue;
        var loading = external ? f.Save.LoadFile(f.Storage.GenerationFile!) : f.Save.Load(SaveSlots.Auto[0]);
        await f.Pump(Task.WhenAny(f.Storage.ReadEntered.Task, loading));
        AssertThat(f.Storage.ReadEntered.Task.IsCompletedSuccessfully && !loading.IsCompleted && f.Camera.Locks == 1).IsTrue();
        var queued = f.Save.NewGame(null, 2);
        var admitted = commands.ToArray();
        AssertThat(admitted.Select(value => value.Phase).SequenceEqual(new[] { SaveLoadPhase.Started, SaveLoadPhase.Queued })).IsTrue();
        AssertThat(admitted[0].Kind == (external ? SaveLoadKind.File : SaveLoadKind.Slot) &&
            (external ? admitted[0].Slot is null : admitted[0].Slot == SaveSlots.Auto[0]) &&
            admitted[0].File == (external ? f.Storage.GenerationFile : null) && admitted[0].RequestedGeneration is null &&
            admitted[1].Kind == SaveLoadKind.NewGame && admitted[1].Slot is null && admitted[1].Result is null).IsTrue();
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !loading.IsCompleted).IsTrue();
        var rejected = 0;
        try { await f.Save.NewGame(null); } catch (ObjectDisposedException) { rejected++; }
        try { await f.Save.Load(SaveSlots.Auto[0]); } catch (ObjectDisposedException) { rejected++; }
        try { await f.Save.LoadFile(f.Storage.GenerationFile!); } catch (ObjectDisposedException) { rejected++; }
        AssertThat(rejected).IsEqual(3);
        f.Storage.ReleaseReads(); await closing;
        AssertThat(loading.IsCanceled && queued.IsCanceled && f.Camera.Locks == 0 && !f.Save.Enabled).IsTrue();
        var events = commands.ToArray();
        AssertThat(events.Where(value => value.Operation == admitted[0].Operation).Select(value => value.Phase)
            .SequenceEqual(new[] { SaveLoadPhase.Started, SaveLoadPhase.Canceled })).IsTrue();
        AssertThat(events.Where(value => value.Operation == admitted[1].Operation).Select(value => value.Phase)
            .SequenceEqual(new[] { SaveLoadPhase.Queued, SaveLoadPhase.Canceled })).IsTrue();
        AssertThat(events.Count(value => value.Phase == SaveLoadPhase.Rejected) == rejected &&
            events.All(value => value.Result is null) &&
            events.Select(value => value.Operation).Distinct().Count() == rejected + admitted.Length).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Storage.Folder.Path, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ClosingDuringNewGameMapPreparationKeepsItsLeaseAndLockUntilPreparationReallyFinishes()
    {
        await using var f = new SaveLifecycleFixture();
        await f.Pump(f.Initialize());
        var preparation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Maps.Preparation = preparation;
        var entry = f.Save.NewGame(SaveSlots.Auto[0], 1);
        await f.Pump(Task.WhenAny(f.Maps.Entered.Task, entry));
        AssertThat(f.Maps.Entered.Task.IsCompletedSuccessfully && !entry.IsCompleted && f.Camera.Locks == 1).IsTrue();
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !entry.IsCompleted && f.Camera.Locks == 1).IsTrue();
        preparation.SetResult(); await closing;
        AssertThat(entry.IsCanceled && f.Camera.Locks == 0 && !f.Save.Enabled).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Storage.Folder.Path, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task DeletionRequiresRealTitleAndPublishesItsActualCompletion()
    {
        await using var f = new SaveLifecycleFixture();
        await f.Seed(SaveReason.Manual); await f.Pump(f.Initialize());
        f.Flow = AppState.Playing;
        AssertThat((await f.Save.DeleteEntry(SaveSlots.Auto[0], 1)).Status).IsEqual(DeleteStatus.Rejected);
        f.Flow = AppState.Title;
        await f.Pump(f.Save.Load(SaveSlots.Auto[0]));
        AssertThat(f.World.Instance != 0 && (await f.Save.DeleteSlot(SaveSlots.Auto[0])).Status == DeleteStatus.Rejected).IsTrue();
        f.World.EndInstance();
        var busy = new List<bool>(); f.Save.SlotsChanged += () => busy.Add(f.Save.Busy);
        var deleting = f.Save.DeleteEntry(SaveSlots.Auto[0], 1);
        await f.Pump(deleting);
        AssertThat((await deleting).Status == DeleteStatus.Deleted && !f.Storage.HasGeneration &&
            f.Save.Slots[0].Health == SlotHealth.Empty && busy.SequenceEqual(new[] { true, false })).IsTrue();
        await f.Pump(f.Save.NewGame(SaveSlots.Auto[0], 2));
        await f.Pump(f.Save.SaveNow(SaveReason.NewGame)); f.World.EndInstance();
        var automatic = f.Save.DeleteEntry(SaveSlots.Auto[0], 1); await f.Pump(automatic);
        AssertThat((await automatic).Status == DeleteStatus.Rejected && f.Storage.HasGeneration).IsTrue();
        var whole = f.Save.DeleteSlot(SaveSlots.Auto[0]); await f.Pump(whole);
        AssertThat((await whole).Status == DeleteStatus.Deleted && !f.Storage.HasGeneration && f.Save.Active is null).IsTrue();
        using var second = new SaveRepository(f.Storage.Folder.Path, true, new([]));
        AssertThat(!second.Enabled && f.Save.Enabled && f.Diagnostics.Errors.Count == 0).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ClosingDuringDeletionScanDrainsItAndCancelsTheQueuedRestoreBeforeUnlocking()
    {
        await using var f = new SaveLifecycleFixture();
        f.Flow = AppState.Title;
        await f.Seed(SaveReason.Manual); await f.Pump(f.Initialize()); f.Storage.BlockReads();
        var deleting = f.Save.DeleteEntry(SaveSlots.Auto[0], 1);
        await f.Pump(Task.WhenAny(f.Storage.ReadEntered.Task, deleting));
        AssertThat(f.Storage.ReadEntered.Task.IsCompletedSuccessfully && !deleting.IsCompleted && f.Storage.HasGeneration).IsTrue();
        var queued = f.Save.NewGame(null, 2);
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !deleting.IsCompleted).IsTrue();
        f.Storage.ReleaseReads(); await closing;
        AssertThat(deleting.IsCanceled && queued.IsCanceled && f.Storage.HasGeneration && !f.Save.Busy).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Storage.Folder.Path, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ConfirmedNewGameCommitSurvivesFailedFutureEntryMaintenanceAndKeepsTheSlotReadOnly()
    {
        await using var f = new SaveLifecycleFixture();
        f.Flow = AppState.Title;
        await f.Seed(SaveReason.Manual);
        var path = f.Storage.GenerationFile!;
        var document = SaveContainer.Decode(File.ReadAllBytes(path));
        var sections = document.Sections.ToDictionary(pair => pair.Key, pair => pair.Value);
        var newerVersion = sections["world"].Version + 1;
        sections["world"] = new(newerVersion, System.Text.Encoding.UTF8.GetBytes("{\"v\":" + newerVersion + ",\"data\":{}}"));
        var future = SaveContainer.Encode(document with { Sections = sections });
        File.WriteAllBytes(path, future);
        await f.Pump(f.Initialize());
        AssertThat(f.Save.Slots[0].Health == SlotHealth.Newer && f.Save.Continue is null).IsTrue();
        var blocked = f.Save.Load(SaveSlots.Auto[0], 1); await f.Pump(blocked);
        AssertThat((await blocked).Status == LoadStatus.Corrupt && f.World.Instance == 0).IsTrue();
        var single = f.Save.DeleteEntry(SaveSlots.Auto[0], 1); await f.Pump(single);
        AssertThat((await single).Status).IsEqual(DeleteStatus.Rejected);
        await f.Pump(f.Save.NewGame(SaveSlots.Auto[0], 3));
        var ordinary = f.Save.SaveNow(SaveReason.Manual); await f.Pump(ordinary);
        AssertThat((await ordinary).Status).IsEqual(SaveStatus.Disabled);
        using (var locked = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var writing = f.Save.SaveNow(SaveReason.NewGame); await f.Pump(writing);
            AssertThat((await writing).Status == SaveStatus.Saved && (await writing).Gen == 2 &&
                f.Save.Slots[0].Health == SlotHealth.Newer && f.Save.Repository!.PendingMaintenanceErrors.Count == 1).IsTrue();
            AssertThat(f.Save.Repository!.PendingMaintenanceErrors.Single()).IsInstanceOf<IOException>();
            var retry = f.Save.SaveNow(SaveReason.Manual); await f.Pump(retry);
            AssertThat((await retry).Status == SaveStatus.Disabled && File.ReadAllBytes(path).SequenceEqual(future)).IsTrue();
        }
        f.World.EndInstance();
        var deleting = f.Save.DeleteSlot(SaveSlots.Auto[0]); await f.Pump(deleting);
        AssertThat((await deleting).Status == DeleteStatus.Deleted && f.Save.Slots[0].Health == SlotHealth.Empty &&
            f.Save.Repository!.PendingMaintenanceErrors.Count == 0 && f.Diagnostics.Errors.Count == 0).IsTrue();
    }
}
