using System.Numerics;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Save.Format;
using Campus.Kernel.World;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Kernel.Save.Tests;

[TestSuite]
public sealed class SaveShutdownTests
{
    private static long _id;

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ClosingDuringTheInitialScanDrainsTheWorkerAndReleasesItsUnclaimedLock()
    {
        await using var f = new Fixture();
        f.Seed(); f.Section.Armed = true;
        var published = 0; f.Save.SlotsChanged += () => published++;
        var initializing = f.Initialize();
        await f.Section.Entered.Task;
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !initializing.IsCompleted).IsTrue();
        f.Section.Release(); await closing;
        AssertThat(initializing.IsCanceled && published == 0 && !f.Save.Enabled).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Directory, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
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
        await using var f = new Fixture();
        using (var prepared = await SaveInitialization.Prepare(new(f.Directory, true, null, null, 1),
            new([]), CancellationToken.None))
        {
            var handoff = f.Save.OnFrame(prepared.TakeRepository);
            f.Save.StopNewWork();
            AssertThat(handoff.IsCanceled).IsTrue();
        }
        using var unlocked = File.Open(Path.Combine(f.Directory, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    [TestCategory(TestReason.Regression)]
    public async Task ClosingAfterSnapshotDetachmentWaitsForRealReadbackAndKeepsItsOutcome(bool failReadback)
    {
        await using var f = new Fixture { ExpectedErrors = failReadback ? 1 : 0 };
        await f.Pump(f.Initialize());
        await f.Pump(f.Save.NewGame(SaveSlots.Auto[0], 1));
        f.Section.Armed = true;
        if (failReadback) f.Section.Failure = new IOException("Injected readback failure.");
        var writing = f.Save.SaveNow(SaveReason.Test);
        await f.Pump(Task.WhenAny(f.Section.Entered.Task, writing));
        AssertThat(f.Section.Entered.Task.IsCompletedSuccessfully)
            .OverrideFailureMessage("Save finished before its generation reached readback.").IsTrue();
        var staging = f.StagingFile;
        AssertThat(staging is not null && !f.HasGeneration)
            .OverrideFailureMessage("The blocked readback must belong to an unpublished staging file.").IsTrue();
        AssertThat(SaveContainer.Decode(File.ReadAllBytes(staging!)).Meta.Slot).IsEqual(SaveSlots.Auto[0]);
        f.World.EndInstance();
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !writing.IsCompleted).IsTrue();
        f.Section.Release(); await closing;
        var result = await writing;
        AssertThat(result.Status == (failReadback ? SaveStatus.Failed : SaveStatus.Saved)).IsTrue();
        AssertThat(result.Error == (failReadback ? SaveErrorKeys.Io : null)).IsTrue();
        AssertThat(f.HasGeneration == !failReadback && !f.Save.Busy).IsTrue();
        AssertThat(f.StagingFile is null).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Directory, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(f.ExpectedErrors);
        if (failReadback)
        {
            AssertThat(f.Diagnostics.Errors.Single()!).IsInstanceOf<IOException>();
            AssertThat(ReferenceEquals(f.Diagnostics.Errors.Single(), f.Section.Failure)).IsTrue();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    [TestCategory(TestReason.Regression)]
    public async Task ClosingDuringAReadDrainsTheWorkerAndQueuedNewGameBeforeReleasingTheLock(bool external)
    {
        await using var f = new Fixture();
        f.Seed(); await f.Pump(f.Initialize());
        f.Section.Armed = true;
        var loading = external ? f.Save.LoadFile(f.GenerationFile!) : f.Save.Load(SaveSlots.Auto[0]);
        await f.Pump(Task.WhenAny(f.Section.Entered.Task, loading));
        AssertThat(f.Section.Entered.Task.IsCompletedSuccessfully && !loading.IsCompleted && f.CameraLocks == 1).IsTrue();
        var queued = f.Save.NewGame(null, 2);
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !loading.IsCompleted).IsTrue();
        var rejected = 0;
        try { await f.Save.NewGame(null); } catch (ObjectDisposedException) { rejected++; }
        try { await f.Save.Load(SaveSlots.Auto[0]); } catch (ObjectDisposedException) { rejected++; }
        try { await f.Save.LoadFile(f.GenerationFile!); } catch (ObjectDisposedException) { rejected++; }
        AssertThat(rejected).IsEqual(3);
        f.Section.Release(); await closing;
        AssertThat(loading.IsCanceled && queued.IsCanceled && f.CameraLocks == 0 && !f.Save.Enabled).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Directory, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ClosingDuringNewGameMapPreparationKeepsItsLeaseAndLockUntilPreparationReallyFinishes()
    {
        await using var f = new Fixture();
        await f.Pump(f.Initialize());
        var preparation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.MapPreparation = preparation;
        var entry = f.Save.NewGame(SaveSlots.Auto[0], 1);
        await f.Pump(Task.WhenAny(f.MapEntered, entry));
        AssertThat(f.MapEntered.IsCompletedSuccessfully && !entry.IsCompleted && f.CameraLocks == 1).IsTrue();
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !entry.IsCompleted && f.CameraLocks == 1).IsTrue();
        preparation.SetResult(); await closing;
        AssertThat(entry.IsCanceled && f.CameraLocks == 0 && !f.Save.Enabled).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Directory, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task DeletionRequiresRealTitleAndPublishesItsActualCompletion()
    {
        await using var f = new Fixture();
        f.Seed(SaveReason.Manual); await f.Pump(f.Initialize());
        f.Flow.SetState(AppState.Playing);
        AssertThat((await f.Save.DeleteEntry(SaveSlots.Auto[0], 1)).Status).IsEqual(DeleteStatus.Rejected);
        f.Flow.SetState(AppState.Title);
        await f.Pump(f.Save.Load(SaveSlots.Auto[0]));
        AssertThat(f.World.Instance != 0 && (await f.Save.DeleteSlot(SaveSlots.Auto[0])).Status == DeleteStatus.Rejected).IsTrue();
        f.World.EndInstance();
        var busy = new List<bool>(); f.Save.SlotsChanged += () => busy.Add(f.Save.Busy);
        var deleting = f.Save.DeleteEntry(SaveSlots.Auto[0], 1);
        await f.Pump(deleting);
        AssertThat((await deleting).Status == DeleteStatus.Deleted && !f.HasGeneration &&
            f.Save.Slots[0].Health == SlotHealth.Empty && busy.SequenceEqual(new[] { true, false })).IsTrue();
        await f.Pump(f.Save.NewGame(SaveSlots.Auto[0], 2));
        await f.Pump(f.Save.SaveNow(SaveReason.NewGame)); f.World.EndInstance();
        var automatic = f.Save.DeleteEntry(SaveSlots.Auto[0], 1); await f.Pump(automatic);
        AssertThat((await automatic).Status == DeleteStatus.Rejected && f.HasGeneration).IsTrue();
        var whole = f.Save.DeleteSlot(SaveSlots.Auto[0]); await f.Pump(whole);
        AssertThat((await whole).Status == DeleteStatus.Deleted && !f.HasGeneration && f.Save.Active is null).IsTrue();
        using var second = new SaveRepository(f.Directory, true, new([]));
        AssertThat(!second.Enabled && f.Save.Enabled && f.Diagnostics.Errors.Count == 0).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ClosingDuringDeletionScanDrainsItAndCancelsTheQueuedRestoreBeforeUnlocking()
    {
        await using var f = new Fixture();
        f.Flow.SetState(AppState.Title);
        f.Seed(SaveReason.Manual); await f.Pump(f.Initialize()); f.Section.Armed = true;
        var deleting = f.Save.DeleteEntry(SaveSlots.Auto[0], 1);
        await f.Pump(Task.WhenAny(f.Section.Entered.Task, deleting));
        AssertThat(f.Section.Entered.Task.IsCompletedSuccessfully && !deleting.IsCompleted && f.HasGeneration).IsTrue();
        var queued = f.Save.NewGame(null, 2);
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !deleting.IsCompleted).IsTrue();
        f.Section.Release(); await closing;
        AssertThat(deleting.IsCanceled && queued.IsCanceled && f.HasGeneration && !f.Save.Busy).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Directory, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(0);
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task ConfirmedNewGameCommitSurvivesFailedFutureEntryMaintenanceAndKeepsTheSlotReadOnly()
    {
        await using var f = new Fixture();
        f.Flow.SetState(AppState.Title);
        f.Seed(SaveReason.Manual);
        var path = f.GenerationFile!;
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

    private sealed class Fixture : IAsyncDisposable
    {
        internal string Directory { get; } = Path.Combine(Path.GetTempPath(),
            "uestc-rpg-save-shutdown-" + Environment.ProcessId + "-" + Interlocked.Increment(ref _id));
        internal SaveService Save { get; } = new();
        internal TestWorldState World { get; }
        internal BlockingSection Section { get; }
        internal TestDiagnosticLog Diagnostics { get; } = new();
        internal TestAppFlow Flow { get; } = new();
        internal int ExpectedErrors { get; init; }
        private readonly TestRuleset _rules = new();
        private readonly Maps _maps;
        private readonly SaveSections _sections;
        private readonly Observer _camera = new();
        internal int CameraLocks => _camera.Locks;
        private TaskCompletionSource? _mapPreparation;
        internal TaskCompletionSource? MapPreparation
        {
            set { _mapPreparation = value; _maps.Preparation = value?.Task; }
        }
        internal Task MapEntered => _maps.Entered.Task;
        internal string? GenerationFile => System.IO.Directory.Exists(Path.Combine(Directory, SaveSlots.Auto[0].Value))
            ? System.IO.Directory.EnumerateFiles(Path.Combine(Directory, SaveSlots.Auto[0].Value), "g*.sav").SingleOrDefault() : null;
        internal bool HasGeneration => System.IO.Directory.Exists(Path.Combine(Directory, SaveSlots.Auto[0].Value)) &&
            System.IO.Directory.EnumerateFiles(Path.Combine(Directory, SaveSlots.Auto[0].Value), "g*.sav").Any();
        internal string? StagingFile => System.IO.Directory.Exists(Path.Combine(Directory, SaveSlots.Auto[0].Value))
            ? System.IO.Directory.EnumerateFiles(Path.Combine(Directory, SaveSlots.Auto[0].Value), SaveRepository.StagingPattern).SingleOrDefault()
            : null;

        internal Fixture()
        {
            World = new(new TestWeatherProcess(), _rules);
            var descriptor = RealMapFixture.Entry().Descriptor;
            _maps = new() { Maps = [descriptor], Default = descriptor.Id, Current = descriptor.Id };
            Section = new(World, () => HasGeneration || StagingFile is not null);
            _sections = new([new(new("world") { Phase = RestorePhase.World }, Section)]);
        }
        internal Task Initialize() => Save.Initialize(new(Directory, true, null, null, 1),
            new(World, _maps, _camera, _camera, _rules, new Atlas(), new TestAppLoadingPresentation(),
                Flow, Diagnostics, _ => { }, World, (_, _, _) => Task.CompletedTask,
                () => Task.FromResult<byte[]?>(null), () => 1792082400), _sections);

        internal void Seed(SaveReason reason = SaveReason.Test)
        {
            using var repository = new SaveRepository(Directory, true, _sections);
            var meta = new SaveMeta(SaveContainer.CurrentFormat, SaveSlots.Auto[0], SlotKind.Auto,
                1, 1, 0, _maps.Default, World.WorldHours, World.Clock, World.SeasonTarget, World.Weather.Kind,
                _camera.Mode, _camera.Pose, null, null, null, 1792082400, reason, _rules.Current.Id.Value);
            var raw = _sections.Serialize([new("world", _sections.Registered[0],
                new WorldSaveV1(World.WorldHours, 0, World.SeasonPos, World.SeasonTarget,
                    World.BaseFlow.Rate, World.LastFlowRate, World.AnimTime))], new Dictionary<string, RawSaveSection>());
            repository.Write(new(meta, raw, null));
        }
        internal async Task Pump(Task task)
        {
            for (var frame = 0; frame < TestSamples.FrameLoop && !task.IsCompleted; frame++)
            { Save.Tick(0); await Task.Delay(1); }
            AssertThat(task.IsCompleted).OverrideFailureMessage("Save shutdown preparation did not finish.").IsTrue();
            await task;
        }
        public async ValueTask DisposeAsync()
        {
            Section.Release(); _mapPreparation?.TrySetResult();
            try { await Save.DisposeAsync(); }
            finally
            {
                Section.Dispose();
                if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, recursive: true);
            }
        }
    }

    private sealed class Maps : TestMapService
    {
        internal Task? Preparation { get; set; }
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override Task Prepare(MapId map)
        {
            Entered.TrySetResult();
            return Preparation ?? Task.CompletedTask;
        }
    }

    private sealed class BlockingSection(TestWorldState world, Func<bool> hasReadbackFile)
        : ISaveSection<WorldSaveV1>, IDisposable
    {
        private readonly ManualResetEventSlim _release = new();
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool Armed { get; set; }
        internal Exception? Failure { get; set; }
        public JsonTypeInfo<WorldSaveV1> TypeInfo
        {
            get
            {
                // Serialization has no file yet; initial scanning and staged readback both do.
                if (Armed && hasReadbackFile())
                {
                    Entered.TrySetResult(); _release.Wait();
                    if (Failure is { } failure) throw failure;
                }
                return LifecycleJsonContext.Strict.WorldSaveV1;
            }
        }
        public WorldSaveV1 Capture(in CaptureContext context) => new(context.Moment.WorldHours,
            context.Moment.ClockOffset, (int)context.Moment.Season, context.Moment.Season,
            context.Moment.FlowRate, world.LastFlowRate, world.AnimTime);
        public WorldSaveV1 CreateDefault(NewGameContext context) => new(context.Start.Clock, 0,
            (int)context.Start.Season, context.Start.Season, world.BaseFlow.Rate, world.LastFlowRate, world.AnimTime);
        public void Restore(WorldSaveV1 dto, RestoreContext context) =>
            world.Restore(new(dto.WorldHours, dto.ClockOffset, dto.SeasonTarget, dto.FlowRate));
        internal void Release() => _release.Set();
        public void Dispose() => _release.Dispose();
    }

    private sealed class Observer : TestCameraRig, IObserverState
    {
        public Vector3 Feet => Pose.Position;
        public Vector3 Eye => Pose.Position;
        public float YawDeg => Pose.YawDeg;
        public NavSpace? Space => null;
    }

    private sealed class Atlas : IAtlasService
    {
        public string? RegionAt(float x, float z) => null;
        public long Rev => 0; public MapId Map => throw new NotSupportedException(); public MapRect PageRect => throw new NotSupportedException();
        public float ZoomMin => 1; public float ZoomMax => 1;
        public IReadOnlyList<AtlasMarker> Markers => []; public IReadOnlyList<AtlasRegion> Regions => [];
        public event Action? Changed { add { } remove { } }
        public AtlasMarker? Find(MarkerId id) => null;
        public UnlockState StateOf(MarkerId id) => throw new NotSupportedException();
        public bool CanTravel(MarkerId id, TravelStyle style, out TravelRefusal why) => throw new NotSupportedException();
        public ITransitHandle? Travel(MarkerId id, TravelStyle style) => throw new NotSupportedException();
        public MarkerId? Tracked => null;
        public void Track(MarkerId? id) => throw new NotSupportedException();
        public IReadOnlyList<CustomPin> Pins => [];
        public CustomPin? AddPin(Vector2 at, PinShape shape) => throw new NotSupportedException();
        public void MovePin(int id, Vector2 at) => throw new NotSupportedException();
        public void SetPinShape(int id, PinShape shape) => throw new NotSupportedException();
        public void RemovePin(int id) => throw new NotSupportedException();
        public string PolicyId => "test";
    }
}
