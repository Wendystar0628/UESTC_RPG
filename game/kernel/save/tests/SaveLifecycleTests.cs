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
using Campus.Tests;
using GdUnit4;

namespace Campus.Kernel.Save.Tests;

[TestSuite]
public sealed partial class SaveLifecycleTests
{
    private static long _id;
    [TestCase]
    public async Task NewGameDefersPoseDefaultsUntilTravelAndPreservesItsExplicitContext()
    {
        using var fixture = new Fixture();
        await fixture.Start();
        fixture.Camera.BeforeDefault = () => Check(fixture.Maps.HasTravelled,
            "A pose default was requested before its target map travelled.");
        var start = new WorldPreset(6.5, Season.Winter, WeatherKind.Snow, .4f);
        var result = await fixture.Pump(fixture.Save.NewGame(new("s1"), 733, start));
        Check(result.Status == LoadStatus.Loaded && fixture.Camera.DefaultContext == new NewGameContext(733, fixture.Maps.Default, start),
            "Deferred pose defaults lost the requested seed, map or starting preset.");
    }

    [TestCase]
    public async Task PreviewThenResumeRestoresPoseOnceAndCommitsWorldOnlyOnce()
    {
        using var fixture = new Fixture();
        await fixture.Start();
        await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.World.Hours = 91.25; fixture.Camera.SetPose(new(new Vector3(30, 40, 50), 70, -20));
        fixture.Flow = AppState.Playing;
        Check((await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame))).Status == SaveStatus.Saved, "Initial save failed.");
        fixture.Camera.SetPose(new(new Vector3(100, 200, 300), 0, 0));
        fixture.Flow = AppState.Title; fixture.Log.Clear();
        var preview = await fixture.Pump(fixture.Save.Load(new("s1"), LoadScope.Preview));
        Check(preview.Status == LoadStatus.Loaded && fixture.Save.HasPendingPose, "Preview did not hold pose.");
        Check(fixture.Camera.Pose.Position == new Vector3(100, 200, 300), "Preview moved the title camera.");
        Check(fixture.Log.SequenceEqual(new[] { "world", "instance", "travel" }), "World/instance/travel ordering changed.");
        var instance = fixture.World.Instance;
        fixture.Ready = false;
        var resume = fixture.Save.CompleteResume();
        await fixture.Frames(20);
        Check(!resume.IsCompleted && fixture.Save.HasPendingPose, "Resume ignored destination readiness.");
        fixture.Ready = true;
        await fixture.Pump(resume);
        Check(!fixture.Save.HasPendingPose && fixture.World.Instance == instance, "Resume changed world instance.");
        Check(fixture.Camera.Pose.Position == new Vector3(30, 40, 50), "Resume did not restore saved pose.");
        Check(fixture.Log.Count(x => x == "view") == 1, "Pose restored more than once.");
    }

    [TestCase]
    public async Task QuitTimeoutKeepsWriteAliveAndLateSuccessUpdatesState()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Flow = AppState.Quitting;
        fixture.ThumbnailBarrier = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var quit = await fixture.Pump(fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromMilliseconds(30)));
        Check(quit.Status == SaveStatus.Failed && fixture.Save.Busy && fixture.Save.State.ErrorKey == "save.error.timeout", "Timeout canceled or silently accepted a save.");
        var retry = fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(2));
        fixture.ThumbnailBarrier.SetResult(null);
        var result = await fixture.Pump(retry);
        Check(result.Status == SaveStatus.Saved && fixture.Save.State.Last == SaveStatus.Saved, "Late write failed to finish.");
        Check(fixture.Save.Slots[0].Meta?.Gen == 2, "Retry launched a duplicate write.");
        var pose = fixture.Camera.Pose;
        fixture.Camera.SetPose(default);
        await fixture.Pump(fixture.Save.Load(new("s1")));
        Check(fixture.Camera.Pose == pose, "Quit save did not recover the departure pose.");
    }

    [TestCase]
    public async Task OnlyIntervalRequestsSaveAndTitleNeverWrites()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing; await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        await fixture.Frames(200);
        Check(fixture.Save.Slots[0].Meta?.Gen == 1, "An autosave was written without a request.");
        fixture.Save.Request(SaveReason.Interval);
        for (var i = 0; i < 500 && fixture.Save.Slots[0].Meta?.Gen != 2; i++) await fixture.Frames(1);
        Check(fixture.Save.Slots[0].Meta is { Gen: 2, Reason: SaveReason.Interval }, "The interval request did not save.");
        fixture.Flow = AppState.Title;
        fixture.Save.Request(SaveReason.Interval);
        await fixture.Frames(400);
        Check(fixture.Save.Slots[0].Meta?.Gen == 2, "Title saved its camera state.");
    }

    [TestCase]
    public async Task ForcedCaptureUsesOneMomentEvenWhenHudChangedTargetAfterWeatherTick()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        fixture.World.TargetHours = 91.25;
        fixture.Weather.LastTickHours = 17.5;
        var result = await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test));
        Check(result.Status == SaveStatus.Saved, "Coherent forced capture failed.");
        Check(fixture.Weather.CapturedMoment?.WorldHours == 91.25 && fixture.Save.Slots[0].Meta?.WorldHours == 91.25,
            "Weather used its previous tick instead of the shared save moment.");
        Check(fixture.World.Hours == 17.5 && fixture.Weather.LastTickHours == 17.5, "Capturing advanced live world or weather.");
    }

    [TestCase]
    public async Task ActiveMapTransitionCannotWriteANewGeneration()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Maps.Pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var result = await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test));
        Check(result.Status == SaveStatus.Deferred && fixture.Save.Slots[0].Meta?.Gen == 1, "Captured across map generations.");
        fixture.Maps.Pending.SetResult(MapTravelResult.Arrived);
    }

    [TestCase]
    public async Task WorldPhaseFailureAbortsInSameFrameAndBlocksWritesUntilFullRecovery()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing;
        await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Weather.FailRestore = true;
        var failed = await fixture.Pump(fixture.Save.Load(new("s1")));
        Check(failed.Status == LoadStatus.Failed && fixture.World.RestoreFaulted && fixture.Camera.Locks == 1,
            "A partially restored world remained interactive.");
        Check(fixture.Save.State is { Last: SaveStatus.Failed, ErrorKey: "save.error.restore_world" }, "Restore failure is not visible through the save state.");
        await fixture.Frames(10); // FakeWorld.Tick throws if a pending restore escaped the failing Save frame.
        Check((await fixture.Pump(fixture.Save.SaveNow(SaveReason.Test))).Status == SaveStatus.Failed, "Faulted state was saved.");
        Check((await fixture.Pump(fixture.Save.FlushForQuit(SaveReason.Quit, TimeSpan.FromSeconds(1)))).Status == SaveStatus.Deferred,
            "Fault exit attempted to serialize the partial world.");
        Check(fixture.Save.Slots[0].Meta?.Gen == 1, "Fault damaged the last good generation.");
        fixture.Weather.FailRestore = false;
        var recovered = await fixture.Pump(fixture.Save.NewGame(new("s1"), 2));
        Check(recovered.Status == LoadStatus.Loaded && !fixture.World.RestoreFaulted && fixture.Camera.Locks == 0,
            "A complete new game did not release the fault lease.");
        Check(fixture.Save.State.ErrorKey is null, "Recovery left the stale error visible.");
    }

    [TestCase]
    public async Task FailureAfterWorldCommitAlsoFreezesAndCanRecoverByLoading()
    {
        using var fixture = new Fixture();
        await fixture.Start(); await fixture.Pump(fixture.Save.NewGame(new("s1"), 1));
        fixture.Flow = AppState.Playing; await fixture.Pump(fixture.Save.SaveNow(SaveReason.NewGame));
        fixture.Maps.FailTravel = true;
        var failed = await fixture.Pump(fixture.Save.Load(new("s1")));
        Check(failed.Status == LoadStatus.Failed && fixture.World.RestoreFaulted && fixture.Camera.Locks == 1,
            "A post-commit map failure exposed a half-restored world.");
        fixture.Maps.FailTravel = false;
        var recovered = await fixture.Pump(fixture.Save.Load(new("s1")));
        Check(recovered.Status == LoadStatus.Loaded && !fixture.World.RestoreFaulted && fixture.Camera.Locks == 0,
            "Full load did not recover the faulted transaction.");
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private sealed class Fixture : IDisposable
    {
        public SaveService Save { get; } = new();
        public FakeWorld World { get; }
        public FakeCamera Camera { get; }
        public FakeMaps Maps { get; }
        public FakeWeather Weather { get; } = new();
        public AppState Flow { get; set; } = AppState.Title;
        public List<string> Log { get; } = [];
        public bool Ready { get; set; } = true;
        public TaskCompletionSource<byte[]?>? ThumbnailBarrier { get; set; }
        private bool _inFrame;
        public Fixture()
        {
            World = new(() => _inFrame, Log); Camera = new(() => _inFrame, Log); Maps = new(Log);
        }
        public async Task Start()
        {
            var root = Environment.GetEnvironmentVariable("UESTC_REPOSITORY") ?? throw new InvalidOperationException("UESTC_REPOSITORY required.");
            var directory = Path.Combine(root, "output", "save-tests", "lifecycle-" + Environment.ProcessId + "-" + Interlocked.Increment(ref _id));
            var rules = new TestRuleset("test-policy", id: "test-rules");
            var bindings = new SaveRuntimeBindings(World, Maps, Camera, Camera, rules, new FakeAtlas(Maps), new FakeVeil(), () => Flow,
                _ => { }, World, WaitDestination,
                () => ThumbnailBarrier?.Task ?? Task.FromResult<byte[]?>(null), () => 1792082400);
            var sections = new SaveSections([
                new(new("world") { Owner = "WP-50", Phase = RestorePhase.World, Order = 10 }, World),
                new(new("weather") { Owner = "WP-50", Phase = RestorePhase.World, Order = 21 }, Weather),
                new(new("view") { Owner = "WP-50", Phase = RestorePhase.Pose, Order = 60 }, Camera)]);
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
            for (var i = 0; i < 5000 && !task.IsCompleted; i++) await Frames(1);
            Check(task.IsCompleted, "Save lifecycle did not finish."); await task;
        }
        public async Task<T> Pump<T>(Task<T> task) { await Pump((Task)task); return await task; }
        public void Dispose() => Save.Dispose();
    }

    private sealed class FakeVeil : IVeilState { public float Opacity => 0; public VeilReason Reason => VeilReason.Boot; }
    private sealed class FakeWorld(Func<bool> inFrame, List<string> log) : IWorldState, IWorldRestore, ISaveSection<LifecycleWorld>
    {
        public double Hours { get; set; } = 17.5;
        public double? TargetHours { get; set; }
        public bool RestoreFaulted { get; private set; }
        private bool _pending;
        public long Instance { get; set; } = 1;
        public long Rev => 1; public double WorldHours => Hours; public double Clock => Hours % 24;
        public double SeasonPos => 2; public Season SeasonTarget => Season.Autumn; public Body Sun => default; public Body Moon => default;
        public float MoonPhase => 1; public float Omega => 0; public Campus.Contracts.Flow Flow => new(1); public bool Scrubbing => false;
        public WorldTransition Transition => WorldTransition.None; public ViewMode ViewMode => ViewMode.Fly;
        public WeatherSel Weather => new(WeatherMode.Auto, WeatherKind.Clear, 0); public WeatherVec WeatherNow => default;
        public IReadOnlyList<WeatherSpan> WeatherAhead => []; public uint WeatherSeed => 1; public double AnimTime => 240; public double Step12 => 240;
        public double PresetClock(ClockPreset preset, Season season) => 17.5; public bool IsNative(WeatherKind kind) => true;
        public JsonTypeInfo<LifecycleWorld> TypeInfo => LifecycleJsonContext.Strict.LifecycleWorld;
        public WorldSaveMoment GetSaveMoment() => new(TargetHours ?? Hours, 0, Season.Autumn, 1);
        public void AbortRestore() { RestoreFaulted = true; _pending = false; log.Add("abort"); }
        public void CompleteRestore()
        {
            Check(_pending, "Commit without a fresh restore."); _pending = false; RestoreFaulted = false; Instance++; log.Add("instance");
        }
        public void Tick() { if (!RestoreFaulted) Check(!_pending, "An uncommitted World phase escaped into the next frame."); }
        public LifecycleWorld Capture(in CaptureContext context)
        {
            Check(inFrame(), "Capture ran outside Save frame."); var moment = context.Moment ?? GetSaveMoment();
            return new(moment.WorldHours, moment.ClockOffset, moment.Season, moment.FlowRate);
        }
        public void Restore(LifecycleWorld dto, RestoreContext context)
        { Check(inFrame(), "Restore ran outside Save frame."); Hours = dto.WorldHours; TargetHours = null; _pending = true; log.Add("world"); }
        public LifecycleWorld CreateDefault(NewGameContext context) => new(context.Start.Clock, 0, context.Start.Season);
    }
    private sealed class FakeCamera(Func<bool> inFrame, List<string> log)
        : TestCameraRig(null, new Pose(new Vector3(1, 2, 3), 0, 0)), IObserverState, ISaveSection<LifecycleView>
    {
        public Action? BeforeDefault { get; set; }
        public NewGameContext? DefaultContext { get; private set; }
        public Vector3 Feet => Pose.Position; public Vector3 Eye => Pose.Position; public float YawDeg => Pose.YawDeg; public NavSpace? Space => null;
        public JsonTypeInfo<LifecycleView> TypeInfo => LifecycleJsonContext.Strict.LifecycleView;
        public LifecycleView Capture(in CaptureContext context) { Check(inFrame(), "View capture outside Save frame."); return new(Pose, Mode, Space); }
        public void Restore(LifecycleView dto, RestoreContext context) { Check(inFrame(), "Pose restore outside Save frame."); SetPose(dto.Pose); log.Add("view"); }
        public LifecycleView CreateDefault(NewGameContext context)
        {
            BeforeDefault?.Invoke(); DefaultContext = context;
            return new(new(new Vector3(1, 2, 3), 0, 0), ViewMode.Fly, null);
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
            CapturedMoment = context.Moment ?? throw new InvalidOperationException("Missing shared capture moment.");
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
                new("test", "test", []), new Dictionary<string, string>(), [])];
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
        public event Action? Changed { add { } remove { } }
        public AtlasMarker? Find(MarkerId id) => null;
        public UnlockState StateOf(MarkerId id) => UnlockState.Hidden;
        public string? RegionAt(float x, float z) => null;
        public bool CanTravel(MarkerId id, TravelStyle style, out TravelRefusal why) { why = TravelRefusal.Unavailable; return false; }
        public void Prefetch(MarkerId id) { }
        public ITransitHandle? Travel(MarkerId id, TravelStyle style) => null;
        public MarkerId? Tracked => null;
        public void Track(MarkerId? id) { }
        public IReadOnlyList<CustomPin> Pins => [];
        public CustomPin? AddPin(Vector2 at, PinShape shape) => null;
        public void MovePin(int id, Vector2 at) { }
        public void SetPinShape(int id, PinShape shape) { }
        public void RemovePin(int id) { }
        public string PolicyId => "test-policy";
    }
}

public sealed record LifecycleWorld(double WorldHours, double ClockOffset, Season SeasonTarget, int FlowRate = 1);
public sealed record LifecycleView(Pose Pose, ViewMode Mode, NavSpace? Space);
public sealed record LifecycleWeather(WeatherSel Selection, double AtHours);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(LifecycleWorld))]
[JsonSerializable(typeof(LifecycleView))]
[JsonSerializable(typeof(LifecycleWeather))]
internal partial class LifecycleJsonContext : JsonSerializerContext
{
    internal static LifecycleJsonContext Strict { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    { RespectRequiredConstructorParameters = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, DefaultIgnoreCondition = JsonIgnoreCondition.Never });
}
