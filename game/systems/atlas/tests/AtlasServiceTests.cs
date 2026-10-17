using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Gen.Parts.Stubs;
using Campus.Hud.Map;
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
    public void DisplayMetadataUsesLoadedWaypointsAndPreservesMarkerNamesAndMissingValues()
    {
        var test = new Fixture();
        var authored = new Waypoint("test-authored", new("点位标题", "Waypoint title"), WaypointGroup.West,
            new(new(25, 10, 30), 40, -10), null, null, new(17.5, Season.Autumn));
        var generated = authored with
        {
            Id = "test-generated",
            Group = WaypointGroup.North,
            Recommend = new(8, Season.Winter, WeatherKind.Snow)
        };
        var title = new LocalizedName("标记标题", "Marker title");
        test.Atlas.LoadMap(test.Context, new(1, test.Context.Descriptor.DataRect,
            [new(new("test-view"), MarkerKind.Viewpoint, title, default, "viewpoint", 1, 1, null,
                WaypointId: authored.Id),
             new(new("test-facility"), MarkerKind.Facility, title, default, "facility", 1, 1, null,
                WaypointId: authored.Id),
             new(new("test-missing"), MarkerKind.Facility, title, default, "facility", 1, 1, null,
                WaypointId: "missing") { Group = WaypointGroup.Core },
             new(new("test-unlinked"), MarkerKind.Facility, title, default, "facility", 1, 1, null)]),
            new(1, [], []), new(1, [authored, generated]));
        foreach (var id in new[] { "test-view", "test-facility" })
        {
            var entry = new MapEntry(test.Atlas.Find(new(id))!);
            Check(entry.Name == title && entry.Group == authored.Group && entry.Recommend == authored.Recommend,
                "Display metadata changed the marker title or lost linked waypoint values.");
        }
        var automatic = new MapEntry(test.Atlas.Find(new(generated.Id))!);
        Check(automatic.Name == generated.Name && automatic.Group == generated.Group
            && automatic.Recommend == generated.Recommend && automatic.FlyOnly,
            "Generated waypoint marker lost its display metadata or travel mode.");
        foreach (var id in new[] { "test-missing", "test-unlinked" })
        {
            var entry = new MapEntry(test.Atlas.Find(new(id))!);
            Check(entry.Group is null && entry.Recommend is null, "Missing waypoint acquired display defaults.");
        }
        Check(!JsonSerializer.Serialize(test.Atlas.Find(new("test-view")), ContractJson.Options).Contains("recommend"),
            "Runtime display metadata became an authored Atlas field.");
        var published = automatic.Marker;
        test.Atlas.OnMapUnloading(test.Context.Descriptor.Id);
        test.Reload();
        Check(test.Atlas.Find(new(generated.Id)) is null && published.Recommend == generated.Recommend,
            "Reload retained old display data or mutated a published marker.");
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

    [TestCase]
    public void RealCampusAndD0InteriorIndexProduceUniqueAtlasDoorMarkers()
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "game", "data", "maps", "qingshuihe", "campus.json")))
            repository = repository.Parent;
        if (repository is null) throw new InvalidOperationException("Real campus data not found.");
        var root = Path.Combine(repository.FullName, "game", "data", "maps", "qingshuihe");
        var interiorBytes = File.ReadAllBytes(Path.Combine(root, "interiors.json"));
        using var catalog = JsonDocument.Parse(interiorBytes);
        var items = catalog.RootElement.GetProperty("interiors").EnumerateArray().ToArray();
        var open = items.SelectMany(item => item.GetProperty("featureIds").EnumerateArray())
            .Select(id => new FeatureId(id.GetString()!)).ToHashSet();
        var closed = catalog.RootElement.TryGetProperty("closedEntrances", out var closedRegister)
            ? closedRegister.EnumerateArray().ToArray() : [];
        var expectedIndexFeatures = open.Concat(closed.Select(door => new FeatureId(door.GetProperty("featureId").GetString()!)))
            .Distinct().OrderBy(id => id.Value, StringComparer.Ordinal).ToArray();
        var authoredDoorIds = items.SelectMany(item => item.GetProperty("entrances").EnumerateArray())
            .Where(door => door.GetProperty("open").GetBoolean())
            .Select(door => door.GetProperty("id").GetString()!).ToHashSet(StringComparer.Ordinal);
        var campus = D0CampusLoader.FromBytes(File.ReadAllBytes(Path.Combine(root, "campus.json")), open);
        var interiorIndex = new InteriorIndexStub();
        var world = new StubWorldModel(new StubSpatialIndex(), interiorIndex, new StubFidelityModel());
        world.Configure(campus);
        var mapFile = JsonSerializer.Deserialize<MapFile>(File.ReadAllBytes(Path.Combine(root, "map.json")), ContractJson.Options)!;
        var descriptor = mapFile.Complete(campus.Geo, campus.Center, campus.Bounds, campus.TileMin, campus.TileMax);
        var providers = typeof(BoxInteriorProviderStub).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IInteriorProvider).IsAssignableFrom(type))
            .Select(type => (Type: type, Registration: type.GetCustomAttribute<InteriorProviderAttribute>()))
            .Where(item => item.Registration is not null)
            .ToDictionary(item => item.Registration!.Id, item => (IInteriorProvider)Activator.CreateInstance(item.Type)!, StringComparer.Ordinal);
        var built = D0InteriorSetup.Build(interiorBytes,
            new BuildContext { Map = descriptor, World = world, WorldSeed = descriptor.Seed }, providers);
        interiorIndex.Configure(built.Entries);
        var doors = built.Entries.SelectMany(entry => entry.Entrances.Where(door => door.Open)
            .Select(door => (Feature: entry.Feature, Door: door))).ToArray();
        Check(campus.SourceEntityCount > 400 && built.Entries.Select(entry => entry.Feature)
            .OrderBy(id => id.Value, StringComparer.Ordinal).SequenceEqual(expectedIndexFeatures),
            "Regression did not assemble the complete campus and registered interior index.");
        Check(built.Entries.Where(entry => entry.Open).Select(entry => entry.Feature)
            .OrderBy(id => id.Value, StringComparer.Ordinal).SequenceEqual(open.OrderBy(id => id.Value, StringComparer.Ordinal)),
            "Closed registration changed the open interior feature set.");
        Check(doors.Count(item => !authoredDoorIds.Contains(item.Door.Id)) > 1,
            "Regression no longer exercises multiple generated fallback entrances.");
        Check(doors.Select(item => item.Door.Id).Distinct(StringComparer.Ordinal).Count() == doors.Length,
            "D0 interior provider reused an entrance ID across different building features.");
        var atlas = new AtlasService(new Camera(), new Observer(), new Rules("all-unlocked"));
        atlas.LoadMap(new Context(descriptor, world),
            JsonSerializer.Deserialize<AtlasMarkerFile>(File.ReadAllBytes(Path.Combine(root, "atlas", "markers.json")), ContractJson.Options)!,
            JsonSerializer.Deserialize<AtlasLabelFile>(File.ReadAllBytes(Path.Combine(root, "atlas", "labels.json")), ContractJson.Options)!,
            JsonSerializer.Deserialize<WaypointFile>(File.ReadAllBytes(Path.Combine(root, "waypoints.json")), ContractJson.Options)!);
        Check(atlas.Markers.Count(marker => marker.Kind == MarkerKind.Interior) == doors.Length,
            "Atlas silently discarded an entrance from the real interior index.");
        foreach (var item in doors)
        {
            var marker = atlas.Find(new("door:" + item.Door.Id));
            Check(marker is not null && marker.Feature == item.Feature
                && marker.At == new Vector2(item.Door.Center.X, item.Door.Center.Z)
                && marker.Teleport?.Stand?.Feet == item.Door.Outside,
                "A real interior marker lost its owner, center, or outside landing.");
        }
        foreach (var id in authoredDoorIds)
            Check(atlas.Find(new("door:" + id)) is not null, "An authored entrance ID changed during fallback repair.");
        foreach (var door in closed)
        {
            var feature = new FeatureId(door.GetProperty("featureId").GetString()!);
            var id = door.GetProperty("id").GetString()!;
            var entry = built.Entries.Single(item => item.Feature == feature);
            Check(!entry.Open && entry.ProviderId is null && entry.Entrances.Single(item => item.Id == id).Open == false &&
                atlas.Find(new("door:" + id)) is null, "Closed registration gained an open interior marker or provider.");
        }
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
    { public BuildInputSnapshot SourceInputs => BuildInputSnapshot.Empty; public string DataRoot => "res://test/"; public string BakeRoot => "res://test-bake/"; }
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
