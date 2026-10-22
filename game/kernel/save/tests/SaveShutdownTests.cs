using System.Numerics;
using System.Text.Json.Serialization.Metadata;
using Campus.Contracts;
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
        f.World.EndInstance();
        var closing = f.Save.DisposeAsync().AsTask();
        AssertThat(!closing.IsCompleted && !writing.IsCompleted).IsTrue();
        f.Section.Release(); await closing;
        var result = await writing;
        AssertThat(result.Status == (failReadback ? SaveStatus.Failed : SaveStatus.Saved)).IsTrue();
        AssertThat(result.Error == (failReadback ? "save.error.io" : null)).IsTrue();
        AssertThat(f.Diagnostics.Errors.Count).IsEqual(f.ExpectedErrors);
        if (failReadback)
        {
            AssertThat(f.Diagnostics.Errors.Single()!).IsInstanceOf<IOException>();
            AssertThat(ReferenceEquals(f.Diagnostics.Errors.Single(), f.Section.Failure)).IsTrue();
        }
        AssertThat(f.HasGeneration == !failReadback && !f.Save.Busy).IsTrue();
        using var unlocked = File.Open(Path.Combine(f.Directory, ".lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal string Directory { get; } = Path.Combine(Path.GetTempPath(),
            "uestc-rpg-save-shutdown-" + Environment.ProcessId + "-" + Interlocked.Increment(ref _id));
        internal SaveService Save { get; } = new();
        internal TestWorldState World { get; }
        internal BlockingSection Section { get; }
        internal TestDiagnosticLog Diagnostics { get; } = new();
        internal int ExpectedErrors { get; init; }
        private readonly TestRuleset _rules = new();
        private readonly TestMapService _maps;
        private readonly SaveSections _sections;
        private readonly Observer _camera = new();
        internal bool HasGeneration => System.IO.Directory.Exists(Path.Combine(Directory, SaveSlots.Auto[0].Value)) &&
            System.IO.Directory.EnumerateFiles(Path.Combine(Directory, SaveSlots.Auto[0].Value), "g*.sav").Any();

        internal Fixture()
        {
            World = new(new TestWeatherProcess(), _rules);
            var descriptor = RealMapFixture.Entry().Descriptor;
            _maps = new() { Maps = [descriptor], Default = descriptor.Id, Current = descriptor.Id };
            Section = new(World, () => HasGeneration);
            _sections = new([new(new("world") { Phase = RestorePhase.World }, Section)]);
        }
        internal Task Initialize() => Save.Initialize(new(Directory, true, null, null, 1),
            new(World, _maps, _camera, _camera, _rules, new Atlas(), new TestAppLoadingPresentation(),
                new TestAppFlow(), Diagnostics, _ => { }, World, (_, _, _) => Task.CompletedTask,
                () => Task.FromResult<byte[]?>(null), () => 1792082400), _sections);

        internal void Seed()
        {
            using var repository = new SaveRepository(Directory, true, _sections);
            var meta = new SaveMeta(SaveContainer.CurrentFormat, SaveSlots.Auto[0], SlotKind.Auto,
                1, 1, 0, _maps.Default, World.WorldHours, World.Clock, World.SeasonTarget, World.Weather.Kind,
                _camera.Mode, _camera.Pose, null, null, null, 1792082400, SaveReason.Test, _rules.Current.Id.Value);
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
            Section.Release();
            try { await Save.DisposeAsync(); }
            finally
            {
                Section.Dispose();
                if (System.IO.Directory.Exists(Directory)) System.IO.Directory.Delete(Directory, recursive: true);
            }
            AssertThat(Diagnostics.Errors.Count).OverrideFailureMessage("Save shutdown emitted unexpected Error diagnostics.").IsEqual(ExpectedErrors);
        }
    }

    private sealed class BlockingSection(TestWorldState world, Func<bool> hasGeneration)
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
                if (Armed && hasGeneration())
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
