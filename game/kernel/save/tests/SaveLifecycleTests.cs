using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
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
public sealed class SaveLifecycleTests
{
    private static long _id;
    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task NewGameDefersPoseDefaultsUntilMapPreparationAndPreservesItsExplicitContext()
    {
        using var fixture = new Fixture();
        await fixture.Start();
        fixture.Camera.BeforeDefault = () => AssertThat(fixture.Maps.HasPrepared).OverrideFailureMessage("A pose default was requested before its target map was prepared.").IsTrue();
        var start = new WorldPreset(6.5, Season.Winter, WeatherKind.Snow, .4f);
        var result = await fixture.Pump(fixture.Save.NewGame(new("s1"), 733, start));
        AssertThat(result.Status == LoadStatus.Loaded && fixture.Camera.DefaultContext == new NewGameContext(733, fixture.Maps.Default, start, ViewMode.Fly)).OverrideFailureMessage("Deferred pose defaults lost the requested seed, map or starting preset.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task FullLoadRestoresPoseOnceAndWaitsForDestinationBeforeCompleting()
    {
        using var fixture = new Fixture();
        await fixture.Start();
        await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.World.Hours = 91.25; fixture.Camera.SetPose(new(new Vector3(30, 40, 50), 70, -20));
        fixture.Flow = AppState.Playing;
        AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame))).Status == SaveStatus.Saved).OverrideFailureMessage("Initial save failed.").IsTrue();
        fixture.Camera.SetPose(new(new Vector3(100, 200, 300), 0, 0));
        fixture.Flow = AppState.Entering; fixture.Log.Clear(); fixture.Ready = false;
        var instance = fixture.World.Instance;
        var loading = fixture.Save.Load(new("s1"));
        for (var frame = 0; frame < TestSamples.FrameLoop && !fixture.Log.Contains("view"); frame++) await fixture.Frames(1);
        AssertThat(!loading.IsCompleted).OverrideFailureMessage("Full load ignored destination readiness.").IsTrue();
        AssertThat(fixture.Log.SequenceEqual(new[] { "world", "instance", "prepare-map", "view" })).OverrideFailureMessage("World/instance/map/pose ordering changed.").IsTrue();
        AssertThat(fixture.Camera.Pose.Position == new Vector3(30, 40, 50)).OverrideFailureMessage("Full load did not restore the saved pose.").IsTrue();
        fixture.Ready = true;
        var loaded = await fixture.Pump(loading);
        AssertThat(loaded.Status == LoadStatus.Loaded && fixture.World.Instance == instance + 1).OverrideFailureMessage("Full load did not commit exactly one new instance.").IsTrue();
        AssertThat(fixture.Log.Count(x => x == "view") == 1 && fixture.Camera.Locks == 0).OverrideFailureMessage("Pose restored more than once or retained its input lease.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task TemporaryNewGameAndEndedWorldNeverSelectOrWriteASlot()
    {
        using var fixture = new Fixture();
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
        var failure = new SaveResult(SaveStatus.Failed, new("s1"), 2, "save.error.io");
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
        using var fixture = new Fixture { ExpectedErrors = endedWorld ? 1 : 0 };
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Flow = AppState.Quitting;
        fixture.ThumbnailBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var quit = await fixture.Pump(fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromMilliseconds(30)));
        AssertThat(quit.Status == SaveStatus.Failed && fixture.Save.Busy && fixture.Save.State.ErrorKey == "save.error.timeout").OverrideFailureMessage("Timeout canceled or silently accepted a save.").IsTrue();
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
        AssertThat(result.Error == (endedWorld ? "save.error.snapshot" : null)).IsTrue();
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(fixture.ExpectedErrors);
        if (endedWorld) AssertThat(fixture.Diagnostics.Errors.Single()!).IsInstanceOf<InvalidOperationException>();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == (endedWorld ? 1UL : 2UL)).OverrideFailureMessage("Retry launched a duplicate write or damaged the old generation.").IsTrue();
        var pose = fixture.Camera.Pose;
        fixture.Camera.SetPose(default);
        await fixture.Pump(fixture.Save.Load(new("s1")));
        AssertThat(fixture.Camera.Pose == pose).OverrideFailureMessage("Quit save did not recover the departure pose.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    [TestCategory("Slow")]
    public async Task OnlyIntervalRequestsSaveAndTitleNeverWrites()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing; await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        await fixture.Frames(200);
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == 1).OverrideFailureMessage("An autosave was written without a request.").IsTrue();
        fixture.Save.Request(SaveReason.Interval);
        for (var i = 0; i < 500 && fixture.Save.Slots[0].Meta?.Gen != 2; i++) await fixture.Frames(1);
        AssertThat(fixture.Save.Slots[0].Meta is { Gen: 2, Reason: SaveReason.Interval }).OverrideFailureMessage("The interval request did not save.").IsTrue();
        fixture.Flow = AppState.Title;
        fixture.Save.Request(SaveReason.Interval);
        await fixture.Frames(400);
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == 2).OverrideFailureMessage("Title saved its camera state.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task ForcedCaptureUsesOneMomentEvenWhenHudChangedTargetAfterWeatherTick()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        fixture.World.TargetHours = 91.25;
        fixture.Weather.LastTickHours = 17.5;
        var result = await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test));
        AssertThat(result.Status == SaveStatus.Saved).OverrideFailureMessage("Coherent forced capture failed.").IsTrue();
        AssertThat(fixture.Weather.CapturedMoment?.WorldHours == 91.25 && fixture.Save.Slots[0].Meta?.WorldHours == 91.25).OverrideFailureMessage("Weather used its previous tick instead of the shared save moment.").IsTrue();
        AssertThat(fixture.World.Hours == 17.5 && fixture.Weather.LastTickHours == 17.5).OverrideFailureMessage("Capturing advanced live world or weather.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task ActiveMapTransitionCannotWriteANewGeneration()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Maps.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test));
        AssertThat(result.Status == SaveStatus.Deferred && fixture.Save.Slots[0].Meta?.Gen == 1).OverrideFailureMessage("Captured across map generations.").IsTrue();
        fixture.Maps.Pending.SetResult(MapTravelResult.Arrived);
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task WorldPhaseFailureAbortsInSameFrameAndBlocksWritesUntilFullRecovery()
    {
        using var fixture = new Fixture { ExpectedErrors = 1 };
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Weather.RestoreFailure = new InvalidDataException("Injected core semantic restore failure.");
        var failed = await fixture.Pump(fixture.Save.Load(new("s1")));
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(fixture.ExpectedErrors);
        AssertThat(fixture.Diagnostics.Errors.Single()!).IsInstanceOf<InvalidDataException>();
        AssertThat(ReferenceEquals(fixture.Diagnostics.Errors.Single(), fixture.Weather.RestoreFailure)).IsTrue();
        AssertThat(failed.Status == LoadStatus.Failed && fixture.World.RestoreFaulted && fixture.Camera.Locks == 1).OverrideFailureMessage("A partially restored world remained interactive.").IsTrue();
        AssertThat(fixture.Save.State is { Last: SaveStatus.Failed, ErrorKey: "save.error.restore_world" }).OverrideFailureMessage("Restore failure is not visible through the save state.").IsTrue();
        await fixture.Frames(10); // FakeWorld.Tick throws if a pending restore escaped the failing Save frame.
        AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test))).Status == SaveStatus.Failed).OverrideFailureMessage("Faulted state was saved.").IsTrue();
        AssertThat((await fixture.Pump(fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(1)))).Status == SaveStatus.Deferred).OverrideFailureMessage("Fault exit attempted to serialize the partial world.").IsTrue();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == 1).OverrideFailureMessage("Fault damaged the last good generation.").IsTrue();
        fixture.Weather.RestoreFailure = null;
        var recovered = await fixture.Pump(fixture.Save.NewGame(null, 2));
        AssertThat(recovered.Status == LoadStatus.Loaded && !fixture.World.RestoreFaulted && fixture.Camera.Locks == 0).OverrideFailureMessage("A complete temporary new game did not release the fault lease.").IsTrue();
        AssertThat(fixture.Save.Active is null && fixture.Save.Slots[0].Meta?.Gen == 1).OverrideFailureMessage("Temporary-world recovery selected a writable slot or changed the previous generation.").IsTrue();
        AssertThat(fixture.Save.State.ErrorKey is null).OverrideFailureMessage("Recovery left the stale error visible.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Regression)]
    public async Task FailureAfterWorldCommitAlsoFreezesAndCanRecoverByLoading()
    {
        using var fixture = new Fixture { ExpectedErrors = 1 };
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        fixture.World.Hours = 91.25;
        var savedPose = new Pose(new Vector3(30, 40, 50), 70, -20);
        fixture.Camera.SetPose(savedPose);
        AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame))).Status == SaveStatus.Saved)
            .OverrideFailureMessage("The recovery slot was not saved.").IsTrue();
        fixture.World.Hours = 120;
        fixture.Camera.SetPose(default);
        fixture.Camera.RestoreFailure = new InvalidDataException("Injected pose restore failure.");
        var instance = fixture.World.Instance;
        var failed = await fixture.Pump(fixture.Save.Load(new("s1")));
        AssertThat(fixture.Diagnostics.Errors.Count).IsEqual(fixture.ExpectedErrors);
        AssertThat(fixture.Diagnostics.Errors.Single()!).IsInstanceOf<InvalidDataException>();
        AssertThat(ReferenceEquals(fixture.Diagnostics.Errors.Single(), fixture.Camera.RestoreFailure)).IsTrue();
        AssertThat(fixture.World.Instance == instance + 1 && fixture.World.Hours == 91.25)
            .OverrideFailureMessage("The injected pose failure did not occur after the World phase committed.").IsTrue();
        AssertThat(failed.Status == LoadStatus.Failed && fixture.World.RestoreFaulted && fixture.Camera.Locks == 1)
            .OverrideFailureMessage("A failure after the World commit released input or left the world usable.").IsTrue();
        AssertThat(fixture.Save.State is { Last: SaveStatus.Failed, ErrorKey: "save.error.restore_world" })
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
    }

    private sealed class Fixture : IDisposable
    {
        public SaveService Save { get; } = new();
        public FakeWorld World { get; }
        public FakeCamera Camera { get; }
        public FakeMaps Maps { get; }
        public FakeWeather Weather { get; } = new();
        public TestDiagnosticLog Diagnostics { get; } = new();
        public int ExpectedErrors { get; init; }
        private readonly TestAppFlow _flow = new();
        public AppState Flow { get => _flow.State; set => _flow.SetState(value); }
        public List<string> Log { get; } = [];
        public bool Ready { get; set; } = true;
        public TaskCompletionSource<byte[]?>? ThumbnailBarrier { get; set; }
        private bool _inFrame;
        private string? _directory;
        public Fixture()
        {
            World = new(() => _inFrame, Log); Camera = new(() => _inFrame, Log); Maps = new(Log);
            Flow = AppState.Title;
            _flow.ActivitySource = () =>
                (Maps.Transition is { Done.IsCompleted: false } ? AppActivity.MapTransition : AppActivity.None) |
                (Save.Busy ? AppActivity.SaveWrite : AppActivity.None);
        }
        public async Task Start()
        {
            var directory = Path.Combine(Path.GetTempPath(), "uestc-rpg-save-lifecycle-" + Environment.ProcessId + "-" + Interlocked.Increment(ref _id));
            _directory = directory;
            var rules = new TestRuleset("test-policy", id: "test-rules");
            var bindings = new SaveRuntimeBindings(World, Maps, Camera, Camera, rules, new FakeAtlas(Maps), new TestAppLoadingPresentation(), _flow, Diagnostics,
                _ => { }, World, WaitDestination,
                () => ThumbnailBarrier?.Task ?? Task.FromResult<byte[]?>(null), () => 1792082400, pose => Task.FromResult(pose));
            var sections = new SaveSections([
                new(new("world") { Phase = RestorePhase.World, Order = 10 }, World),
                new(new("weather") { Phase = RestorePhase.World, Order = 21 }, Weather),
                new(new("view") { Phase = RestorePhase.Pose, Order = 60 }, Camera)]);
            await Pump(Save.Initialize(new(directory, true, null, null, 1), bindings, sections));
        }
        private async Task WaitDestination(Pose pose, NavSpace? space, ViewMode mode)
        { while (!await Save.OnFrame(() => Ready)) { } }
        public async Task Frames(int count)
        {
            for (var i = 0; i < count; i++)
            {
                _inFrame = true;
                try { World.Tick(); Save.Tick(.1); } finally { _inFrame = false; }
                await Task.Delay(1);
            }
        }
        public async Task Pump(Task task)
        {
            for (var i = 0; i < TestSamples.FrameLoop && !task.IsCompleted; i++) await Frames(1);
            AssertThat(task.IsCompleted).OverrideFailureMessage("Save lifecycle did not finish.").IsTrue(); await task;
        }
        public async Task<T> Pump<T>(Task<T> task) { await Pump((Task)task); return await task; }
        public void Dispose()
        {
            Save.Dispose();
            if (_directory is { } directory && Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            AssertThat(Diagnostics.Errors.Count).OverrideFailureMessage("Save lifecycle emitted unexpected Error diagnostics.").IsEqual(ExpectedErrors);
        }
    }

    private sealed class FakeWorld(Func<bool> inFrame, List<string> log) : IWorldState, IWorldRestore, ISaveSection<WorldSaveV1>
    {
        public double Hours { get; set; } = 17.5;
        public double? TargetHours { get; set; }
        public bool RestoreFaulted { get; private set; }
        private bool _pending;
        private long _serial;
        public long Instance { get; private set; }
        public event Action<long>? InstanceChanged;
        public long Rev => 1; public double WorldHours => Hours; public double Clock => Hours % 24;
        public double SeasonPos => 2; public Season SeasonTarget => Season.Autumn; public float SeasonAtmosphere => 0; public Body Sun => default; public Body Moon => default;
        public float SunAltDegAt(double clock, Season season) => Sun.AltDeg;
        public float MoonPhase => 1; public float Omega => 0; public Campus.Contracts.Flow Flow => new(1); public bool Scrubbing => false;
        public Campus.Contracts.Flow BaseFlow => Flow;
        public int LastFlowRate => BaseFlow.Rate;
        public WorldTransition Transition => WorldTransition.None;
        public WeatherSel Weather => new(WeatherMode.Auto, WeatherKind.Clear, 0); public WeatherVec WeatherNow => default;
        public IReadOnlyList<WeatherSpan> WeatherAhead => []; public uint WeatherSeed => 1; public double AnimTime => 240; public double Step12 => 240;
        public double PresetClock(ClockPreset preset, Season season) => 17.5; public bool IsNative(WeatherKind kind) => true;
        public JsonTypeInfo<WorldSaveV1> TypeInfo => LifecycleJsonContext.Strict.WorldSaveV1;
        public WorldSaveMoment GetSaveMoment() => new(TargetHours ?? Hours, 0, Season.Autumn, 1);
        public void AbortRestore() { RestoreFaulted = true; _pending = false; log.Add("abort"); }
        public void CompleteRestore()
        {
            AssertThat(_pending).OverrideFailureMessage("Commit without a fresh restore.").IsTrue(); _pending = false; RestoreFaulted = false; Instance = ++_serial; log.Add("instance");
            InstanceChanged?.Invoke(Instance);
        }
        public void EndInstance() { _pending = false; Instance = 0; InstanceChanged?.Invoke(Instance); }
        public void Tick() { if (!RestoreFaulted) AssertThat(!_pending).OverrideFailureMessage("An uncommitted World phase escaped into the next frame.").IsTrue(); }
        public WorldSaveV1 Capture(in CaptureContext context)
        {
            AssertThat(inFrame()).OverrideFailureMessage("Capture ran outside Save frame.").IsTrue(); var moment = context.Moment;
            return new(moment.WorldHours, moment.ClockOffset, (int)moment.Season, moment.Season, moment.FlowRate, BaseFlow.Rate, AnimTime);
        }
        public void Restore(WorldSaveV1 dto, RestoreContext context)
        { AssertThat(inFrame()).OverrideFailureMessage("Restore ran outside Save frame.").IsTrue(); Hours = dto.WorldHours; TargetHours = null; _pending = true; log.Add("world"); }
        public WorldSaveV1 CreateDefault(NewGameContext context) =>
            new(context.Start.Clock, 0, (int)context.Start.Season, context.Start.Season, BaseFlow.Rate, BaseFlow.Rate, AnimTime);
    }
    private sealed class FakeCamera(Func<bool> inFrame, List<string> log)
        : TestCameraRig(null, new Pose(new Vector3(1, 2, 3), 0, 0)), IObserverState, ISaveSection<ViewSaveData>
    {
        public Action? BeforeDefault { get; set; }
        public Exception? RestoreFailure { get; set; }
        public NewGameContext? DefaultContext { get; private set; }
        public Vector3 Feet => Pose.Position; public Vector3 Eye => Pose.Position; public float YawDeg => Pose.YawDeg; public NavSpace? Space => null;
        public JsonTypeInfo<ViewSaveData> TypeInfo => LifecycleJsonContext.Strict.ViewSaveData;
        public ViewSaveData Capture(in CaptureContext context) { AssertThat(inFrame()).OverrideFailureMessage("View capture outside Save frame.").IsTrue(); return new(Mode, Pose, Space, 1, 1); }
        public void Restore(ViewSaveData dto, RestoreContext context)
        {
            AssertThat(inFrame()).OverrideFailureMessage("Pose restore outside Save frame.").IsTrue();
            if (RestoreFailure is { } failure) throw failure;
            SetPose(dto.Pose); log.Add("view");
        }
        public ViewSaveData CreateDefault(NewGameContext context)
        {
            BeforeDefault?.Invoke(); DefaultContext = context;
            return new(context.Mode, new(new Vector3(1, 2, 3), 0, 0), null, 1, 1);
        }
    }
    private sealed class FakeWeather : ISaveSection<LifecycleWeather>
    {
        public double LastTickHours { get; set; } = 17.5;
        public WorldSaveMoment? CapturedMoment { get; private set; }
        public Exception? RestoreFailure { get; set; }
        public JsonTypeInfo<LifecycleWeather> TypeInfo => LifecycleJsonContext.Strict.LifecycleWeather;
        public LifecycleWeather Capture(in CaptureContext context)
        {
            CapturedMoment = context.Moment;
            return new(new(WeatherMode.Auto, WeatherKind.Clear, 0), CapturedMoment.Value.WorldHours);
        }
        public void Restore(LifecycleWeather dto, RestoreContext context)
        { if (RestoreFailure is { } failure) throw failure; LastTickHours = dto.AtHours; }
        public LifecycleWeather CreateDefault(NewGameContext context) => new(new(WeatherMode.Auto, WeatherKind.Clear, 0), context.Start.Clock);
    }
    private sealed class FakeMaps : TestMapService
    {
        private static readonly MapId Id = new("test-map");
        private readonly List<string> _log;
        public FakeMaps(List<string> log)
        {
            _log = log;
            Rev = 1; Default = Id; Current = Id;
            Maps = [new(Id, new("测试", "Test"), MapKind.Outdoor, 1, 1,
                new(0, 0, 32648, new(-100), new(100)), Vector2.Zero, new(new(-100), new(100)), new(new(-100), new(100)),
                new(1, 0, 3000, -100, 1000, 30, 40, 150), new(-1, -1), new(1, 1), "spawn", [],
                new(MapTimeRule.Shared, new(17.5, Season.Autumn, WeatherKind.Clear), new("测试", "Test"), false, "test"),
                new("test", "test", []), [])];
        }
        public TaskCompletionSource<MapTravelResult>? Pending { get; set; }
        public bool HasPrepared { get; private set; }
        public override IMapTransition? Transition => Pending is { Task.IsCompleted: false } ? new TestMapTransition(Id, Id, Pending.Task) : null;
        public override IReadOnlyList<MapVisit> Visits => [new(Id, default, ViewMode.Fly)];
        public override Task Prepare(MapId map)
        {
            _log.Add("prepare-map"); HasPrepared = true;
            return Task.CompletedTask;
        }
    }
    private sealed class FakeAtlas(IMapService maps) : IAtlasService
    {
        public long Rev => 1; public MapId Map => maps.Current; public IReadOnlyList<AtlasMarker> Markers => []; public IReadOnlyList<AtlasRegion> Regions => [];
        public MapRect PageRect => maps.Maps.Single(map => map.Id == maps.Current).DataRect;
        public float ZoomMin => throw new NotSupportedException();
        public float ZoomMax => throw new NotSupportedException();
        public event Action? Changed { add { } remove { } }
        public AtlasMarker? Find(MarkerId id) => null;
        public UnlockState StateOf(MarkerId id) => UnlockState.Hidden;
        public string? RegionAt(float x, float z) => null;
        public bool CanTravel(MarkerId id, TravelStyle style, out TravelRefusal why) { why = TravelRefusal.Unavailable; return false; }
        public ITransitHandle? Travel(MarkerId id, TravelStyle style) => null;
        public MarkerId? Tracked => null;
        public void Track(MarkerId? id) => throw new NotSupportedException();
        public IReadOnlyList<CustomPin> Pins => [];
        public CustomPin? AddPin(Vector2 at, PinShape shape) => null;
        public void MovePin(int id, Vector2 at) => throw new NotSupportedException();
        public void SetPinShape(int id, PinShape shape) => throw new NotSupportedException();
        public void RemovePin(int id) => throw new NotSupportedException();
        public string PolicyId => "test-policy";
    }
}

public sealed record LifecycleWeather(WeatherSel Selection, double AtHours);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(WorldSaveV1))]
[JsonSerializable(typeof(ViewSaveData))]
[JsonSerializable(typeof(LifecycleWeather))]
internal partial class LifecycleJsonContext : JsonSerializerContext
{
    internal static LifecycleJsonContext Strict { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    { RespectRequiredConstructorParameters = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, DefaultIgnoreCondition = JsonIgnoreCondition.Never });
}
