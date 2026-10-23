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
using static GdUnit4.Assertions;

namespace Campus.Kernel.Save.Tests;

internal sealed class SaveLifecycleFixture : IDisposable, IAsyncDisposable
{
    public SaveTestStorage Storage { get; }
    private readonly bool _ownsStorage;
    public SaveService Save { get; } = new();
    public FakeWorld World { get; }
    public FakeCamera Camera { get; }
    public FakeMaps Maps { get; }
    public FakeWeather Weather { get; } = new();
    public FakeWalk? Walk { get; init; }
    public TestDiagnosticLog Diagnostics { get; } = new();
    public int ExpectedErrors { get; init; }
    private readonly TestAppFlow _flow = new();
    public AppState Flow { get => _flow.State; set => _flow.SetState(value); }
    public List<string> Log { get; } = [];
    public bool Ready { get; set; } = true;
    public TaskCompletionSource<byte[]?>? ThumbnailBarrier { get; set; }
    internal byte[]? NextThumbnail { get; set; }
    internal int ThumbnailCaptures { get; private set; }
    internal string EntryFile(ulong generation) => Storage.EntryFile(generation);
    private bool _inFrame;
    public SaveLifecycleFixture() : this(new SaveTestStorage(), true) { }
    private SaveLifecycleFixture(SaveTestStorage storage, bool ownsStorage)
    {
        Storage = storage; _ownsStorage = ownsStorage;
        World = new(() => _inFrame, Log, Storage); Camera = new(() => _inFrame, Log); Maps = new(Log);
        Flow = AppState.Title;
        _flow.ActivitySource = () =>
            (Maps.Transition is { Done.IsCompleted: false } ? AppActivity.MapTransition : AppActivity.None) |
            (Save.Busy ? AppActivity.SaveWrite : AppActivity.None);
    }
    public Task Start() => Pump(Initialize());
    public Task Initialize()
    {
        var rules = new TestRuleset("test-policy", id: "test-rules");
        var bindings = new SaveRuntimeBindings(World, Maps, Camera, Camera, rules, new FakeAtlas(Maps), new TestAppLoadingPresentation(), _flow, Diagnostics,
            _ => { }, World, WaitDestination,
            () => { ThumbnailCaptures++; return ThumbnailBarrier?.Task ?? Task.FromResult(NextThumbnail); }, () => 1792082400, pose => Task.FromResult(pose))
        {
            PrepareWalk = Walk is { } walk ? walk.Prepare : null
        };
        var registrations = new List<SaveSectionRegistration>
        {
            new(new("world") { Phase = RestorePhase.World, Order = 10 }, World),
            new(new("weather") { Phase = RestorePhase.World, Order = 21 }, Weather),
            new(new("view") { Phase = RestorePhase.Pose, Order = 60 }, Camera)
        };
        if (Walk is not null) registrations.Add(new(new("walk") { Phase = RestorePhase.Pose, Order = 61 }, Walk));
        var sections = new SaveSections(registrations);
        return Save.Initialize(new(Storage.Directory, true, null, null, 1), bindings, sections);
    }
    private async Task WaitDestination(Pose pose, NavSpace? space, ViewMode mode)
    { while (!await Save.OnFrame(() => Ready)) { } }
    public async Task Frames(int count, double delta = .1)
    {
        AdvanceFrames(count, delta);
        // Yield only to the actual file worker; simulated time is advanced in one synchronous batch.
        await Task.Delay(1);
    }
    internal void AdvanceFrames(int count, double delta = .1)
    {
        for (var i = 0; i < count; i++)
        {
            _inFrame = true;
            try { World.Tick(); Save.Tick(delta); } finally { _inFrame = false; }
        }
    }
    internal async Task PumpUntilIdle()
    {
        for (var i = 0; i < TestSamples.FrameLoop && Save.Busy; i++) await Frames(1, 0);
        await Frames(2, 0);
        AssertThat(!Save.Busy).OverrideFailureMessage("Save did not drain its attempt.").IsTrue();
    }
    public async Task Pump(Task task, double delta = 0)
    {
        for (var i = 0; i < TestSamples.FrameLoop && !task.IsCompleted; i++) await Frames(1, delta);
        AssertThat(task.IsCompleted).OverrideFailureMessage("Save lifecycle did not finish.").IsTrue(); await task;
    }
    public async Task<T> Pump<T>(Task<T> task, double delta = 0) { await Pump((Task)task, delta); return await task; }
    internal async Task Seed(SaveReason reason = SaveReason.Test)
    {
        await using var seed = new SaveLifecycleFixture(Storage, false);
        await seed.Start();
        AssertThat((await seed.Pump(seed.Save.NewGame(SaveSlots.Auto[0], 1))).Status).IsEqual(LoadStatus.Loaded);
        seed.Flow = AppState.Playing;
        AssertThat((await seed.Pump(seed.Save.SaveNow(reason))).Status).IsEqual(SaveStatus.Saved);
    }
    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();
    public async ValueTask DisposeAsync()
    {
        Storage.ReleaseReads(); Maps.Preparation?.TrySetResult();
        try { await Save.DisposeAsync(); }
        finally { if (_ownsStorage) Storage.Dispose(); }
    }

    internal sealed class FakeWorld(Func<bool> inFrame, List<string> log, SaveTestStorage storage) : IWorldState, IWorldRestore, ISaveSection<WorldSaveV1>
    {
        public double Hours { get; set; } = 17.5;
        public Exception? CaptureFailure { get; set; }
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
        public JsonTypeInfo<WorldSaveV1> TypeInfo
        {
            get { storage.BeforeTypeInfo(); return LifecycleJsonContext.Strict.WorldSaveV1; }
        }
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
            AssertThat(inFrame()).OverrideFailureMessage("Capture ran outside Save frame.").IsTrue();
            if (CaptureFailure is { } failure) throw failure;
            var moment = context.Moment;
            return new(moment.WorldHours, moment.ClockOffset, (int)moment.Season, moment.Season, moment.FlowRate, BaseFlow.Rate, AnimTime);
        }
        public void Restore(WorldSaveV1 dto, RestoreContext context)
        { AssertThat(inFrame()).OverrideFailureMessage("Restore ran outside Save frame.").IsTrue(); Hours = dto.WorldHours; TargetHours = null; _pending = true; log.Add("world"); }
        public WorldSaveV1 CreateDefault(NewGameContext context) =>
            new(context.Start.Clock, 0, (int)context.Start.Season, context.Start.Season, BaseFlow.Rate, BaseFlow.Rate, AnimTime);
    }
    internal sealed class FakeCamera(Func<bool> inFrame, List<string> log)
        : TestCameraRig(null, new Pose(new Vector3(1, 2, 3), 0, 0)), IObserverState, ISaveSection<ViewSaveData>
    {
        public ObserverSaveSnapshot? Held { get; set; }
        public override ObserverSaveSnapshot CaptureSettledObserver(MapId map) => Held ?? base.CaptureSettledObserver(map);
        public Action? BeforeDefault { get; set; }
        public Exception? RestoreFailure { get; set; }
        public NewGameContext? DefaultContext { get; private set; }
        public Vector3 Feet => Pose.Position; public Vector3 Eye => Pose.Position; public float YawDeg => Pose.YawDeg; public NavSpace? Space => null;
        public JsonTypeInfo<ViewSaveData> TypeInfo => LifecycleJsonContext.Strict.ViewSaveData;
        public ViewSaveData Capture(in CaptureContext context) { AssertThat(inFrame()).OverrideFailureMessage("View capture outside Save frame.").IsTrue(); return context.Observer.View; }
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
    internal sealed class FakeWeather : ISaveSection<LifecycleWeather>
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
    internal sealed class FakeMaps : TestMapService
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
        internal TaskCompletionSource? Preparation { get; set; }
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override IMapTransition? Transition => Pending is { Task.IsCompleted: false } ? new TestMapTransition(Id, Id, Pending.Task) : null;
        public override IReadOnlyList<MapVisit> Visits => [new(Id, default, ViewMode.Fly)];
        public override Task Prepare(MapId map)
        {
            _log.Add("prepare-map"); HasPrepared = true; Entered.TrySetResult();
            return Preparation?.Task ?? Task.CompletedTask;
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

    internal sealed class FakeWalk : ISaveSection<WalkSaveData>
    {
        public WalkSaveData Current { get; private set; } = new(new(new(3, 0, 4), 0, 0), false, WalkRules.DefaultSpeedStep,
            [new(new(3, 0, 4), 0, 0), new(new(7, 0, 8), 0, 0)]);
        public TaskCompletionSource? Barrier { get; set; }
        public TaskCompletionSource Entered { get; set; } = new();
        public int Restores { get; private set; }
        public JsonTypeInfo<WalkSaveData> TypeInfo => LifecycleJsonContext.Strict.WalkSaveData;
        public WalkSaveData Capture(in CaptureContext context) => context.Observer.Walk!;
        public void Restore(WalkSaveData data, RestoreContext context) { Current = data; Restores++; }
        public WalkSaveData CreateDefault(NewGameContext context) => Current;
        public async Task<WalkSaveData> Prepare(WalkSaveData data, RestoreContext context, CancellationToken cancellation)
        {
            Entered.TrySetResult();
            if (Barrier is not { } barrier) return data;
            await barrier.Task.WaitAsync(cancellation);
            context.Repair("walk: unsafe history point removed");
            return data with { SafePoints = data.SafePoints.Take(1).ToArray() };
        }
    }
}

public sealed record LifecycleWeather(WeatherSel Selection, double AtHours);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(WorldSaveV1))]
[JsonSerializable(typeof(ViewSaveData))]
[JsonSerializable(typeof(LifecycleWeather))]
[JsonSerializable(typeof(WalkSaveData))]
internal partial class LifecycleJsonContext : JsonSerializerContext
{
    internal static LifecycleJsonContext Strict { get; } = new(new JsonSerializerOptions(ContractJson.Options)
    { RespectRequiredConstructorParameters = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, DefaultIgnoreCondition = JsonIgnoreCondition.Never });
}
