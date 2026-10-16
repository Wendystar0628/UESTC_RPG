using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Stubs;
using Campus.Kernel.Stubs.AudioUi;
using Campus.Kernel.Stubs.Foundation;
using Campus.Systems.Atlas;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class AtlasServiceTests
{
    [TestCase]
    public void SaveRoundTripRepairsBadReferencesAndPreservesMonotoneIds()
    {
        // Reproduce the integration order: another consumer has already frozen the shared options.
        _ = JsonSerializer.Serialize(0, ContractJson.Options);
        var test = new Fixture();
        Check(!ReferenceEquals(test.Atlas.TypeInfo.Options, ContractJson.Options), "Atlas context bound the shared options instance.");
        var pin = test.Atlas.AddPin(new(10, 20), PinShape.Flag)!;
        test.Atlas.Track(new("pin:" + pin.Id));
        var capture = new CaptureContext(SaveReason.Test, test.Context.Descriptor.Id, false);
        var saved = test.Atlas.Capture(in capture);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(saved, test.Atlas.TypeInfo);
        var decoded = JsonSerializer.Deserialize(bytes, test.Atlas.TypeInfo)!;
        test.Atlas.RemovePin(pin.Id);
        test.Atlas.Restore(decoded, test.RestoreContext());
        Check(test.Atlas.Pins.Single().Id == pin.Id && test.Atlas.Tracked?.Value == "pin:1", "Round trip lost pin or tracking.");
        var repairs = test.RestoreContext();
        test.Atlas.Restore(decoded with
        {
            Pins = [pin, pin, new(99, new(2000, 0), PinShape.Circle)],
            NextPinId = 1,
            Tracked = "missing",
            Unlock = new Dictionary<string, UnlockState> { ["missing"] = UnlockState.Unlocked },
            VisitedAnchors = ["missing"],
        }, repairs);
        Check(test.Atlas.Pins.Count == 1 && test.Atlas.Tracked is null && repairs.Repairs.Count == 6, "Malformed save was not repaired.");
        Check(test.Atlas.AddPin(Vector2.Zero, PinShape.Circle)?.Id == 2, "Pin ID was reused.");
        Check(saved.Pins.Count == 1 && saved.Pins[0] == pin, "Capture retained mutable live state.");
    }

    [TestCase]
    public void ProximityReadsObserverAndRevealsOnlyTheVisitedDistrict()
    {
        var test = new Fixture("proximity");
        test.Camera.SetPose(new(new(0, 0, 0), 0, 0));
        test.Observer.Feet = new(100, 0, 100);
        test.Atlas.Tick();
        Check(test.Atlas.StateOf(new("test-kiosk")) == UnlockState.Known, "Camera position unlocked a marker.");
        test.Observer.Mode = ViewMode.Fly;
        test.Observer.Feet = new(0, 26, 0);
        test.Atlas.Tick();
        Check(test.Atlas.StateOf(new("test-kiosk")) == UnlockState.Known, "High flight unlocked an anchor.");
        test.Observer.Feet = Vector3.Zero;
        test.Atlas.Tick();
        Check(test.Atlas.StateOf(new("test-kiosk")) == UnlockState.Unlocked, "Observer visit did not unlock kiosk.");
        Check(test.Atlas.StateOf(new("test-post")) == UnlockState.Known, "Kiosk visit did not reveal district post.");
        var revision = test.Atlas.Rev;
        test.Atlas.Tick();
        Check(test.Atlas.Rev == revision, "Stationary visit emitted repeated changes.");
    }

    [TestCase]
    public void TravelPassesPreferenceRetainsInputAndSavesOnlyAfterArrival()
    {
        var test = new Fixture();
        test.Observer.Feet = new(100, 0, 100);
        test.Atlas.Travel(new("test-kiosk"), TravelStyle.Teleport);
        Check(test.Camera.Options?.Prefer == TransitPreference.Teleport && test.Camera.Options.Entry is null, "Teleport used flight path options.");
        Check(test.Camera.Locks == 1 && !test.Atlas.IsSettled && test.Saves.Requests == 0, "Travel lock or unsettled state missing.");
        Check(!test.Atlas.CanTravel(new("test-post"), TravelStyle.Fly, out var why) && why == TravelRefusal.Busy, "Concurrent travel was allowed.");
        test.Camera.Handle!.Completion.SetResult(TransitResult.Arrived);
        test.Atlas.Tick();
        Check(test.Camera.Locks == 0 && test.Atlas.IsSettled && test.Saves.Requests == 1, "Arrival did not release input and request save exactly once.");
        test.Atlas.Tick();
        Check(test.Saves.Requests == 1, "Arrival requested duplicate save.");
        test.Atlas.Travel(new("test-post"), TravelStyle.Fly);
        Check(test.Camera.Options?.Prefer == TransitPreference.Auto, "Fly did not use normal transit preference.");
        Check(test.Camera.Locks == 0, "Fly retained an Atlas input lock and cannot be interrupted.");
        test.Camera.Handle!.Completion.SetResult(TransitResult.Cancelled);
        test.Atlas.Tick();
        Check(test.Camera.Locks == 0 && test.Saves.Requests == 1, "Cancelled transit requested a settled save.");
    }

    [TestCase]
    public void UnreadyTransitDriverCannotPretendToArrive()
    {
        var test = new Fixture();
        test.Driver.IsReady = false;
        foreach (var style in new[] { TravelStyle.Teleport, TravelStyle.Fly })
        {
            Check(!test.Atlas.CanTravel(new("test-kiosk"), style, out var why)
                && why == TravelRefusal.Unavailable, "Unready transit driver advertised travel.");
            Check(test.Atlas.Travel(new("test-kiosk"), style) is null, "Unready transit driver was started.");
        }
        Check(test.Camera.Handle is null && test.Camera.Locks == 0 && test.Saves.Requests == 0,
            "Unready transit changed camera, input, or save state.");
    }

    [TestCase]
    public void MapUnloadDropsMarkersRegionsAndSaveState()
    {
        var test = new Fixture();
        test.Atlas.SetRegionLookup((_, _) => "test-district");
        test.Atlas.AddPin(Vector2.Zero, PinShape.Diamond);
        test.Atlas.OnMapUnloading(test.Context.Descriptor.Id);
        Check(test.Atlas.Markers.Count == 0 && test.Atlas.Regions.Count == 0 && test.Atlas.Pins.Count == 0
            && test.Atlas.RegionAt(0, 0) is null, "Unload retained old map data.");
        test.Reload();
        Check(test.Atlas.Pins.Count == 0 && test.Atlas.StateOf(new("test-kiosk")) == UnlockState.Unlocked, "Reload carried state from old map.");
    }

    [TestCase]
    public void ForcedCaptureUsesTransitDestinationWithoutCommittingCancelledVisit()
    {
        var test = new Fixture();
        test.Observer.Feet = new(100, 0, 100);
        test.Atlas.Travel(new("test-kiosk"), TravelStyle.Teleport);
        var forced = new CaptureContext(SaveReason.Quit, test.Context.Descriptor.Id, true);
        var settled = new CaptureContext(SaveReason.Test, test.Context.Descriptor.Id, false);
        Check(test.Atlas.Capture(in forced).VisitedAnchors.Contains("test-kiosk"), "Forced snapshot omitted terminal anchor.");
        Check(test.Atlas.Capture(in settled).VisitedAnchors.Count == 0, "Forced capture changed live visit state.");
        test.Camera.Handle!.Cancel();
        test.Atlas.Tick();
        Check(test.Atlas.Capture(in settled).VisitedAnchors.Count == 0, "Cancelled transit committed a visit.");
    }

    [TestCase]
    public void OpenInteriorIndexGeneratesDoorAtCenterWithOutsideLanding()
    {
        var test = new Fixture();
        var featureId = new FeatureId("test-building");
        test.World.Configure([new(featureId, FeatureSource.Design, new("building"),
            new PolygonGeometry(new PolygonRings([new(1, 1), new(2, 1), new(2, 2)]))) { Name = "测试楼" }]);
        var entrance = new EntranceDecl("test-door", new(10, 3, 12.5f), new(10, 3, 7), 2, true)
        { Center = new(10, 3, 10), YawDeg = 180 };
        test.Interiors.Configure([new(featureId, [], [entrance], true, "test-provider")]);
        test.Reload();
        var door = test.Atlas.Find(new("door:test-door"))!;
        Check(door.At == new Vector2(10, 10) && door.Teleport?.Stand?.Feet == entrance.Outside, "Door marker copied an incorrect location.");
        Check(door.Teleport!.Fly.Position.Y == 9 && door.Teleport.Stand!.YawDeg == 0, "Door viewing direction is wrong.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Fixture
    {
        internal Camera Camera { get; } = new();
        internal Driver Driver { get; } = new();
        internal Observer Observer { get; } = new();
        internal InteriorIndexStub Interiors { get; } = new();
        internal StubWorldModel World { get; }
        internal Context Context { get; }
        internal AtlasService Atlas { get; }
        internal SaveRecorder Saves { get; }
        internal Fixture(string policy = "all-unlocked")
        {
            World = new(new StubSpatialIndex(), Interiors, new StubFidelityModel());
            var map = new MapDescriptor(new("test-map"), new("测试", "Test"), MapKind.Outdoor, 1, 1,
                new(0, 0, 1, new(-1000), new(1000)), Vector2.Zero, new(new(-1000), new(1000)),
                new(new(-1000), new(1000)), new(1, 30, 3000, -28, 2000, 30, 40, 150),
                default, default, "test-spawn", [],
                new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("测试", "Test"), false, "test"),
                new("test", "test", []), new Dictionary<string, string>(), []);
            Context = new(map, World);
            var maps = new TestMapService(Context, Camera);
            Saves = new(maps);
            Atlas = new(Camera, Observer, new Rules(policy), Driver);
            Atlas.Connect(maps, new Flow(), new AudioServiceStub(), Saves);
            Reload();
        }
        internal void Reload() => Atlas.LoadMap(Context, new(1, Context.Descriptor.DataRect,
            [Anchor("test-kiosk", Vector2.Zero, AnchorForm.Kiosk), Anchor("test-post", new(50, 0), AnchorForm.Post)]),
            new(1, [new("test-district", new("测试区", "Test District"), null)], []), new(1, []));
        internal RestoreContext RestoreContext() => new() { Map = Context.Descriptor.Id, FromVersion = 1, Missing = false };
        private static AtlasMarker Anchor(string id, Vector2 at, AnchorForm form) => new(new(id), MarkerKind.Anchor,
            new(id, id), at, "anchor", 1, 0, "test-district", new(new(new(at.X, 6, at.Y), 0, -12), new(new(at.X, 0, at.Y), 0)), new(id), Form: form);
    }
    private sealed record Context(MapDescriptor Descriptor, IWorldModel World) : IMapContext
    { public string DataRoot => "res://test/"; public string BakeRoot => "res://test-bake/"; }
    private sealed class Rules(string policy) : IRuleset
    {
        public RulesetDescriptor Current { get; } = new(new("test"), [ViewMode.Fly, ViewMode.Walk], WorldControlMask.All, policy, [], []);
        public long Rev => 1;
    }
    private sealed class Observer : IObserverState
    {
        public Vector3 Feet { get; set; } = new(200, 0, 200);
        public Vector3 Eye => Feet + Vector3.UnitY;
        public float YawDeg => 0; public NavSpace? Space => null; public ViewMode Mode { get; set; } = ViewMode.Walk;
    }
    private sealed class TestMapService(Context context, Camera camera) : IMapService
    {
        public long Rev { get; private set; } = 1;
        public MapId Default => Current; public MapId Current => context.Descriptor.Id;
        public IReadOnlyList<MapDescriptor> Maps => [context.Descriptor]; public IMapContext? Context => context;
        // This fixture starts on its sole map at world hour zero, before any weather epoch change.
        public IReadOnlyList<MapVisit> Visits => [new(Current, camera.Pose, camera.Mode, 0, 0)];
        public IMapTransition? Transition { get; private set; }
        public event Action<MapId, MapTravelCause>? Arrived;

        public IMapTransition Travel(MapTravelRequest request)
        {
            if (request.Target != Current)
                return Transition = new TestMapTransition(Current, request.Target, request.Cause, MapTravelResult.Rejected);
            var spawn = request.Spawn is null ? null : context.Descriptor.Spawns.SingleOrDefault(s => s.Id == request.Spawn);
            if (request.Pose is null && request.Spawn is not null && spawn is null)
                return Transition = new TestMapTransition(Current, request.Target, request.Cause, MapTravelResult.Rejected);
            var mode = request.Mode ?? camera.Mode;
            _ = mode switch
            {
                ViewMode.Fly or ViewMode.Walk => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(request)),
            };
            camera.SetPose(request.Pose ?? spawn?.Fly ?? camera.Pose);
            camera.Mode = mode;
            Rev++;
            Transition = new TestMapTransition(Current, Current, request.Cause, MapTravelResult.Arrived);
            Arrived?.Invoke(Current, request.Cause);
            return Transition;
        }
    }
    private sealed record TestMapTransition(MapId From, MapId To, MapTravelCause Cause, MapTravelResult Result) : IMapTransition
    {
        public MapStage Stage => MapStage.Done;
        public float Progress => 1;
        public Task<MapTravelResult> Done { get; } = Task.FromResult(Result);
    }
    private sealed class Flow : IAppFlow
    {
        public AppState State => AppState.Playing; public long Rev => 1; public EnterPath? Path => null; public bool Skippable => false;
        public event Action<AppState, AppState>? Changed { add { } remove { } }
        public Task<EnterResult> Enter(EnterRequest r) => throw new NotSupportedException();
        public Task ReturnToTitle() => Task.CompletedTask; public void Skip() { }
        public Task Quit() => Task.CompletedTask;
    }
    private sealed class Camera : ICameraRig
    {
        public Pose Pose { get; private set; } = new(new(500, 100, 500), 0, 0);
        public float Speed => 0; public ViewMode Mode { get; set; } = ViewMode.Walk;
        public float EdgeFog => 0;
        public int Locks { get; private set; }
        public TransitOptions? Options { get; private set; }
        public Handle? Handle { get; private set; }
        public ITransitHandle? ActiveTransit => Handle;
        public void SetPose(Pose pose) => Pose = pose;
        public ITransitHandle Transit(Pose to, TransitOptions? options = null)
        { Options = options; return Handle = new(options?.Prefer == TransitPreference.Teleport ? TransitKind.Teleport : TransitKind.Slide); }
        public IDisposable LockInput(InputLockReason reason) { Locks++; return new Lease(() => Locks--); }
    }
    private sealed class Driver : ITransitDriver
    {
        public bool IsReady { get; set; } = true;
        public Pose? Destination => null;
        public ITransitHandle Start(Pose from, Pose to, TransitOptions? options = null) => throw new NotSupportedException();
    }
    private sealed class Handle(TransitKind kind) : ITransitHandle
    {
        public TaskCompletionSource<TransitResult> Completion { get; } = new();
        public TransitKind Kind => kind; public float Progress => 0; public Task<TransitResult> Done => Completion.Task;
        public void Cancel() => Completion.TrySetResult(TransitResult.Cancelled);
    }
    private sealed class Lease(Action release) : IDisposable
    { private Action? _release = release; public void Dispose() { _release?.Invoke(); _release = null; } }
    private sealed class SaveRecorder(IMapService maps) : ISaveService
    {
        private readonly SaveServiceStub _stub = new(maps);
        public int Requests { get; private set; }
        public void Request(SaveReason reason) { Check(reason == SaveReason.Settled, "Unexpected save reason."); Requests++; }
        public bool Enabled => false; public IReadOnlyList<SlotInfo> Slots => _stub.Slots; public SlotId? Continue => null;
        public SlotId? Active => null; public bool Busy => false; public SaveState State => _stub.State; public bool HasPendingPose => false;
        public event Action? SlotsChanged { add { } remove { } }
        public Task<SaveResult> SaveNow(SaveReason r) => _stub.SaveNow(r);
        public Task<LoadResult> Load(SlotId s, LoadScope scope = LoadScope.Full) => _stub.Load(s, scope);
        public Task CompleteResume() => _stub.CompleteResume(); public Task<LoadResult> LoadFile(string path) => _stub.LoadFile(path);
        public Task<LoadResult> NewGame(SlotId s, uint? seed = null, WorldPreset? start = null) => _stub.NewGame(s, seed, start);
        public Task<SaveResult> FlushForQuit(SaveReason r, TimeSpan timeout) => _stub.FlushForQuit(r, timeout);
        public byte[]? Thumbnail(SlotId s) => null;
    }
}
