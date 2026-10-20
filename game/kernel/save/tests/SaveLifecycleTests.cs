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
    public async Task NewGameDefersPoseDefaultsUntilTravelAndPreservesItsExplicitContext()
    {
        using var fixture = new Fixture();
        await fixture.Start();
        fixture.Camera.BeforeDefault = () => AssertThat(fixture.Maps.HasTravelled).OverrideFailureMessage("A pose default was requested before its target map travelled.").IsTrue();
        var start = new WorldPreset(6.5, Season.Winter, WeatherKind.Snow, .4f);
        var result = await fixture.Pump(fixture.Save.NewGame(new("s1"), 733, start));
        AssertThat(result.Status == LoadStatus.Loaded && fixture.Camera.DefaultContext == new NewGameContext(733, fixture.Maps.Default, start, ViewMode.Fly)).OverrideFailureMessage("Deferred pose defaults lost the requested seed, map or starting preset.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task PreviewThenResumeRestoresPoseOnceAndCommitsWorldOnlyOnce()
    {
        using var fixture = new Fixture();
        await fixture.Start();
        await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.World.Hours = 91.25; fixture.Camera.SetPose(new(new Vector3(30, 40, 50), 70, -20));
        fixture.Flow = AppState.Playing;
        AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame))).Status == SaveStatus.Saved).OverrideFailureMessage("Initial save failed.").IsTrue();
        fixture.Camera.SetPose(new(new Vector3(100, 200, 300), 0, 0));
        fixture.Flow = AppState.Title; fixture.Log.Clear();
        var preview = await fixture.Pump(fixture.Save.Load(new("s1"), LoadScope.Preview));
        AssertThat(preview.Status == LoadStatus.Loaded && fixture.Save.HasPendingPose).OverrideFailureMessage("Preview did not hold pose.").IsTrue();
        AssertThat(fixture.Camera.Pose.Position == new Vector3(100, 200, 300)).OverrideFailureMessage("Preview moved the title camera.").IsTrue();
        AssertThat(fixture.Log.SequenceEqual(new[] { "world", "instance", "travel" })).OverrideFailureMessage("World/instance/travel ordering changed.").IsTrue();
        var instance = fixture.World.Instance;
        fixture.Ready = false;
        var resume = fixture.Save.CompleteResume();
        await fixture.Frames(20);
        AssertThat(!resume.IsCompleted && fixture.Save.HasPendingPose).OverrideFailureMessage("Resume ignored destination readiness.").IsTrue();
        fixture.Ready = true;
        await fixture.Pump(resume);
        AssertThat(!fixture.Save.HasPendingPose && fixture.World.Instance == instance).OverrideFailureMessage("Resume changed world instance.").IsTrue();
        AssertThat(fixture.Camera.Pose.Position == new Vector3(30, 40, 50)).OverrideFailureMessage("Resume did not restore saved pose.").IsTrue();
        AssertThat(fixture.Log.Count(x => x == "view") == 1).OverrideFailureMessage("Pose restored more than once.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task QuitTimeoutKeepsWriteAliveAndLateSuccessUpdatesState()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Flow = AppState.Quitting;
        fixture.ThumbnailBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var quit = await fixture.Pump(fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromMilliseconds(30)));
        AssertThat(quit.Status == SaveStatus.Failed && fixture.Save.Busy && fixture.Save.State.ErrorKey == "save.error.timeout").OverrideFailureMessage("Timeout canceled or silently accepted a save.").IsTrue();
        var retry = fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(2));
        fixture.ThumbnailBarrier.SetException(new IOException("Thumbnail capture failed."));
        var result = await fixture.Pump(retry);
        AssertThat(result.Status == SaveStatus.Saved && fixture.Save.State.Last == SaveStatus.Saved).OverrideFailureMessage("Late write failed to finish.").IsTrue();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == 2).OverrideFailureMessage("Retry launched a duplicate write.").IsTrue();
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
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Weather.FailRestore = true;
        var failed = await fixture.Pump(fixture.Save.Load(new("s1")));
        AssertThat(failed.Status == LoadStatus.Failed && fixture.World.RestoreFaulted && fixture.Camera.Locks == 1).OverrideFailureMessage("A partially restored world remained interactive.").IsTrue();
        AssertThat(fixture.Save.State is { Last: SaveStatus.Failed, ErrorKey: "save.error.restore_world" }).OverrideFailureMessage("Restore failure is not visible through the save state.").IsTrue();
        await fixture.Frames(10); // FakeWorld.Tick throws if a pending restore escaped the failing Save frame.
        AssertThat((await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test))).Status == SaveStatus.Failed).OverrideFailureMessage("Faulted state was saved.").IsTrue();
        AssertThat((await fixture.Pump(fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(1)))).Status == SaveStatus.Deferred).OverrideFailureMessage("Fault exit attempted to serialize the partial world.").IsTrue();
        AssertThat(fixture.Save.Slots[0].Meta?.Gen == 1).OverrideFailureMessage("Fault damaged the last good generation.").IsTrue();
        fixture.Weather.FailRestore = false;
        var recovered = await fixture.Pump(fixture.Save.RestoreTitlePreview());
        AssertThat(recovered.Status == LoadStatus.Loaded && !fixture.World.RestoreFaulted && fixture.Camera.Locks == 0).OverrideFailureMessage("A complete default preview did not release the fault lease.").IsTrue();
        AssertThat(fixture.Save.Active is null && fixture.Save.Slots[0].Meta?.Gen == 1).OverrideFailureMessage("Title recovery selected a writable slot or changed the previous generation.").IsTrue();
        AssertThat(fixture.Save.State.ErrorKey is null).OverrideFailureMessage("Recovery left the stale error visible.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Smoke)]
    public async Task FailureAfterWorldCommitAlsoFreezesAndCanRecoverByLoading()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing; await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Maps.FailTravel = true;
        var failed = await fixture.Pump(fixture.Save.Load(new("s1")));
        AssertThat(failed.Status == LoadStatus.Failed && fixture.World.RestoreFaulted && fixture.Camera.Locks == 1).OverrideFailureMessage("A post-commit map failure exposed a half-restored world.").IsTrue();
        fixture.Maps.FailTravel = false;
        var recovered = await fixture.Pump(fixture.Save.Load(new("s1")));
        AssertThat(recovered.Status == LoadStatus.Loaded && !fixture.World.RestoreFaulted && fixture.Camera.Locks == 0).OverrideFailureMessage("Full load did not recover the faulted transaction.").IsTrue();
    }

    private sealed class Fixture : IDisposable
    {
        public SaveService Save { get; } = new();
        public FakeWorld World { get; }
        public FakeCamera Camera { get; }
        public FakeMaps Maps { get; }
        public FakeWeather Weather { get; } = new();
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
            var bindings = new SaveRuntimeBindings(World, Maps, Camera, Camera, rules, new FakeAtlas(Maps), new TestAppLoadingPresentation(), _flow,
                _ => { }, World, WaitDestination,
                () => ThumbnailBarrier?.Task ?? Task.FromResult<byte[]?>(null), () => 1792082400, pose => Task.FromResult(pose));
            var sections = new SaveSections([
                new(new("world") { Phase = RestorePhase.World, Order = 10 }, World),
                new(new("weather") { Phase = RestorePhase.World, Order = 21 }, Weather),
                new(new("view") { Phase = RestorePhase.Pose, Order = 60 }, Camera)]);
            await Pump(Save.Initialize(new(directory, "test", true, null, null, 1), bindings, sections));
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
        }
    }

    private sealed class FakeWorld(Func<bool> inFrame, List<string> log) : IWorldState, IWorldRestore, ISaveSection<WorldSaveV1>
    {
        public double Hours { get; set; } = 17.5;
        public double? TargetHours { get; set; }
        public bool RestoreFaulted { get; private set; }
        private bool _pending;
        public long Instance { get; set; } = 1;
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
            AssertThat(_pending).OverrideFailureMessage("Commit without a fresh restore.").IsTrue(); _pending = false; RestoreFaulted = false; Instance++; log.Add("instance");
            InstanceChanged?.Invoke(Instance);
        }
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
        public NewGameContext? DefaultContext { get; private set; }
        public Vector3 Feet => Pose.Position; public Vector3 Eye => Pose.Position; public float YawDeg => Pose.YawDeg; public NavSpace? Space => null;
        public JsonTypeInfo<ViewSaveData> TypeInfo => LifecycleJsonContext.Strict.ViewSaveData;
        public ViewSaveData Capture(in CaptureContext context) { AssertThat(inFrame()).OverrideFailureMessage("View capture outside Save frame.").IsTrue(); return new(Mode, Pose, Space, 1, 1); }
        public void Restore(ViewSaveData dto, RestoreContext context) { AssertThat(inFrame()).OverrideFailureMessage("Pose restore outside Save frame.").IsTrue(); SetPose(dto.Pose); log.Add("view"); }
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
        public bool FailRestore { get; set; }
        public JsonTypeInfo<LifecycleWeather> TypeInfo => LifecycleJsonContext.Strict.LifecycleWeather;
        public LifecycleWeather Capture(in CaptureContext context)
        {
            CapturedMoment = context.Moment;
            return new(new(WeatherMode.Auto, WeatherKind.Clear, 0), CapturedMoment.Value.WorldHours);
        }
        public void Restore(LifecycleWeather dto, RestoreContext context)
        { if (FailRestore) throw new InvalidDataException("Injected core semantic restore failure."); LastTickHours = dto.AtHours; }
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
        public bool FailTravel { get; set; }
        public bool HasTravelled { get; private set; }
        public override IMapTransition? Transition => Pending is { Task.IsCompleted: false } ? new TestMapTransition(Id, Id, MapTravelCause.Boot, Pending.Task) : null;
        public override IReadOnlyList<MapVisit> Visits => [new(Id, default, ViewMode.Fly)];
        public override IMapTransition Travel(MapTravelRequest request)
        {
            _log.Add("travel"); HasTravelled = true;
            return new TestMapTransition(request.Target, request.Target, request.Cause,
                Task.FromResult(FailTravel ? MapTravelResult.Failed : MapTravelResult.Arrived));
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
