using Campus.Pipeline.Map;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;
using Campus.Contracts.Save;
using Campus.Gen.Interior.Shell.Box;
using Campus.Gen.Tests.Support;
using Campus.Hud.Map;
using Campus.Systems.Atlas;
using GdUnit4;
using static GdUnit4.Assertions;

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
        AssertThat(!ReferenceEquals(test.Atlas.TypeInfo.Options, ContractJson.Options)).OverrideFailureMessage("Atlas context bound the shared options instance.").IsTrue();
        var pin = test.Atlas.AddPin(new(10, 20), PinShape.Flag)!;
        test.Atlas.Track(new("pin:" + pin.Id));
        var capture = new CaptureContext(SaveReason.Test, test.Context.Descriptor.Id, false);
        var saved = test.Atlas.Capture(in capture);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(saved, test.Atlas.TypeInfo);
        var decoded = JsonSerializer.Deserialize(bytes, test.Atlas.TypeInfo)!;
        test.Atlas.RemovePin(pin.Id);
        test.Atlas.Restore(decoded, test.RestoreContext());
        AssertThat(test.Atlas.Pins.Single().Id == pin.Id && test.Atlas.Tracked?.Value == "pin:1").OverrideFailureMessage("Round trip lost pin or tracking.").IsTrue();
        var repairs = test.RestoreContext();
        test.Atlas.Restore(decoded with
        {
            Pins = [pin, pin, new(99, new(2000, 0), PinShape.Circle)],
            NextPinId = 1,
            Tracked = "missing",
            Unlock = new Dictionary<string, UnlockState> { ["missing"] = UnlockState.Unlocked },
            VisitedAnchors = ["missing"],
        }, repairs);
        AssertThat(test.Atlas.Pins.Count == 1 && test.Atlas.Tracked is null && repairs.Repairs.Count == 6).OverrideFailureMessage("Malformed save was not repaired.").IsTrue();
        AssertThat(test.Atlas.AddPin(Vector2.Zero, PinShape.Circle)?.Id == 2).OverrideFailureMessage("Pin ID was reused.").IsTrue();
        AssertThat(saved.Pins.Count == 1 && saved.Pins[0] == pin).OverrideFailureMessage("Capture retained mutable live state.").IsTrue();
    }

    [TestCase]
    public void ProximityReadsObserverAndRevealsOnlyTheVisitedDistrict()
    {
        var test = new Fixture("proximity");
        test.Camera.SetPose(new(new(0, 0, 0), 0, 0));
        test.Observer.Feet = new(100, 0, 100);
        test.Atlas.Tick();
        AssertThat(test.Atlas.StateOf(new("test-kiosk")) == UnlockState.Known).OverrideFailureMessage("Camera position unlocked a marker.").IsTrue();
        test.Observer.Mode = ViewMode.Fly;
        test.Observer.Feet = new(0, 26, 0);
        test.Atlas.Tick();
        AssertThat(test.Atlas.StateOf(new("test-kiosk")) == UnlockState.Known).OverrideFailureMessage("High flight unlocked an anchor.").IsTrue();
        test.Observer.Feet = Vector3.Zero;
        test.Atlas.Tick();
        AssertThat(test.Atlas.StateOf(new("test-kiosk")) == UnlockState.Unlocked).OverrideFailureMessage("Observer visit did not unlock kiosk.").IsTrue();
        AssertThat(test.Atlas.StateOf(new("test-post")) == UnlockState.Known).OverrideFailureMessage("Kiosk visit did not reveal district post.").IsTrue();
        var revision = test.Atlas.Rev;
        test.Atlas.Tick();
        AssertThat(test.Atlas.Rev == revision).OverrideFailureMessage("Stationary visit emitted repeated changes.").IsTrue();
    }

    [TestCase]
    [TestCategory("Contract")]
    public void TravelPassesPreferenceAndRetainsInput()
    {
        var test = new Fixture();
        test.Observer.Feet = new(100, 0, 100);
        var activitySource = test.Flow.ActivitySource;
        test.Flow.ActivitySource = null;
        test.Flow.Activity = AppActivity.MapTransition;
        AssertThat(!test.Atlas.CanTravel(new("test-kiosk"), TravelStyle.Teleport, out var refusal)
            && refusal == TravelRefusal.Unavailable).OverrideFailureMessage("Map transition changed its travel refusal.").IsTrue();
        test.Flow.Activity = AppActivity.CameraTransition;
        AssertThat(!test.Atlas.CanTravel(new("test-kiosk"), TravelStyle.Teleport, out refusal)
            && refusal == TravelRefusal.Busy).OverrideFailureMessage("Camera transition changed its travel refusal.").IsTrue();
        test.Flow.Activity = AppActivity.SaveWrite | AppActivity.SeasonTransition;
        AssertThat(test.Atlas.CanTravel(new("test-kiosk"), TravelStyle.Teleport, out _)).OverrideFailureMessage("Unrelated activity blocked Atlas travel.").IsTrue();
        test.Flow.Activity = AppActivity.None;
        test.Flow.ActivitySource = activitySource;
        test.Atlas.Travel(new("test-kiosk"), TravelStyle.Teleport);
        AssertThat(test.Camera.Options?.Prefer == TransitPreference.Teleport && test.Camera.Options.Entry is null).OverrideFailureMessage("Teleport used flight path options.").IsTrue();
        AssertThat(test.Camera.Locks == 1 && !test.Atlas.IsSettled).OverrideFailureMessage("Travel lock or unsettled state missing.").IsTrue();
        AssertThat(!test.Atlas.CanTravel(new("test-post"), TravelStyle.Fly, out var why) && why == TravelRefusal.Busy).OverrideFailureMessage("Concurrent travel was allowed.").IsTrue();
        test.Camera.Handle!.Completion.SetResult(TransitResult.Arrived);
        test.Atlas.Tick();
        AssertThat(test.Camera.Locks == 0 && test.Atlas.IsSettled).OverrideFailureMessage("Arrival did not release input.").IsTrue();
        test.Atlas.Travel(new("test-post"), TravelStyle.Fly);
        AssertThat(test.Camera.Options?.Prefer == TransitPreference.Auto).OverrideFailureMessage("Fly did not use normal transit preference.").IsTrue();
        AssertThat(test.Camera.Locks == 0).OverrideFailureMessage("Fly retained an Atlas input lock and cannot be interrupted.").IsTrue();
        test.Camera.Handle!.Completion.SetResult(TransitResult.Cancelled);
        test.Atlas.Tick();
        AssertThat(test.Camera.Locks == 0).OverrideFailureMessage("Cancelled transit retained the input lock.").IsTrue();
    }

    [TestCase]
    public void WalkingTravelToFlyOnlyMarkerSwitchesToFlyFirst()
    {
        var test = new Fixture();
        test.Observer.Feet = new(100, 0, 100);
        AssertThat(test.Atlas.Travel(new("test-roof"), TravelStyle.Teleport) is not null && test.View.Switches == 1
            && test.Observer.Mode == ViewMode.Fly && test.Camera.Options?.LandOn is null).OverrideFailureMessage("Fly-only marker was not entered from the fly camera.").IsTrue();
        test.Camera.Handle!.Completion.SetResult(TransitResult.Cancelled);
        test.Atlas.Tick();
        test.Observer.Mode = ViewMode.Walk; test.View.Refuse = true;
        AssertThat(test.Atlas.Travel(new("test-roof"), TravelStyle.Teleport) is null && test.Camera.Locks == 0).OverrideFailureMessage("Refused mode switch still started travel.").IsTrue();
    }

    [TestCase]
    public void UnreadyTransitDriverCannotPretendToArrive()
    {
        var test = new Fixture();
        test.Driver.IsReady = false;
        foreach (var style in new[] { TravelStyle.Teleport, TravelStyle.Fly })
        {
            AssertThat(!test.Atlas.CanTravel(new("test-kiosk"), style, out var why)
                && why == TravelRefusal.Unavailable).OverrideFailureMessage("Unready transit driver advertised travel.").IsTrue();
            AssertThat(test.Atlas.Travel(new("test-kiosk"), style) is null).OverrideFailureMessage("Unready transit driver was started.").IsTrue();
        }
        AssertThat(test.Camera.Handle is null && test.Camera.Locks == 0).OverrideFailureMessage("Unready transit changed camera or input state.").IsTrue();
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
            AssertThat(entry.Name == title && entry.Group == authored.Group && entry.Recommend == authored.Recommend).OverrideFailureMessage("Display metadata changed the marker title or lost linked waypoint values.").IsTrue();
        }
        var automatic = new MapEntry(test.Atlas.Find(new(generated.Id))!);
        AssertThat(automatic.Name == generated.Name && automatic.Group == generated.Group
            && automatic.Recommend == generated.Recommend && automatic.FlyOnly).OverrideFailureMessage("Generated waypoint marker lost its display metadata or travel mode.").IsTrue();
        foreach (var id in new[] { "test-missing", "test-unlinked" })
        {
            var entry = new MapEntry(test.Atlas.Find(new(id))!);
            AssertThat(entry.Group is null && entry.Recommend is null).OverrideFailureMessage("Missing waypoint acquired display defaults.").IsTrue();
        }
        AssertThat(!JsonSerializer.Serialize(test.Atlas.Find(new("test-view")), ContractJson.Options).Contains("recommend")).OverrideFailureMessage("Runtime display metadata became an authored Atlas field.").IsTrue();
        var published = automatic.Marker;
        test.Atlas.OnMapUnloading(test.Context.Descriptor.Id);
        test.Reload();
        AssertThat(test.Atlas.Find(new(generated.Id)) is null && published.Recommend == generated.Recommend).OverrideFailureMessage("Reload retained old display data or mutated a published marker.").IsTrue();
    }

    [TestCase]
    public void MapUnloadDropsMarkersRegionsAndSaveState()
    {
        var test = new Fixture();
        test.Atlas.SetRegionLookup((_, _) => "test-district");
        test.Atlas.AddPin(Vector2.Zero, PinShape.Diamond);
        test.Atlas.OnMapUnloading(test.Context.Descriptor.Id);
        AssertThat(test.Atlas.Markers.Count == 0 && test.Atlas.Regions.Count == 0 && test.Atlas.Pins.Count == 0
            && test.Atlas.RegionAt(0, 0) is null).OverrideFailureMessage("Unload retained old map data.").IsTrue();
        test.Reload();
        AssertThat(test.Atlas.Pins.Count == 0 && test.Atlas.StateOf(new("test-kiosk")) == UnlockState.Unlocked).OverrideFailureMessage("Reload carried state from old map.").IsTrue();
    }

    [TestCase]
    public void ForcedCaptureUsesTransitDestinationWithoutCommittingCancelledVisit()
    {
        var test = new Fixture();
        test.Observer.Feet = new(100, 0, 100);
        test.Atlas.Travel(new("test-kiosk"), TravelStyle.Teleport);
        var forced = new CaptureContext(SaveReason.Quit, test.Context.Descriptor.Id, true);
        var settled = new CaptureContext(SaveReason.Test, test.Context.Descriptor.Id, false);
        AssertThat(test.Atlas.Capture(in forced).VisitedAnchors.Contains("test-kiosk")).OverrideFailureMessage("Forced snapshot omitted terminal anchor.").IsTrue();
        AssertThat(test.Atlas.Capture(in settled).VisitedAnchors.Count == 0).OverrideFailureMessage("Forced capture changed live visit state.").IsTrue();
        test.Camera.Handle!.Cancel();
        test.Atlas.Tick();
        AssertThat(test.Atlas.Capture(in settled).VisitedAnchors.Count == 0).OverrideFailureMessage("Cancelled transit committed a visit.").IsTrue();
    }

    [TestCase]
    public void OpenInteriorIndexGeneratesDoorAtCenterWithOutsideLanding()
    {
        var test = new Fixture();
        var featureId = new FeatureId("test-building");
        var feature = WorldFeatureFixture.Create(featureId.Value, "building",
            new PolygonGeometry(new PolygonRings([new(1, 1), new(2, 1), new(2, 2)]))) with { Name = "测试楼" };
        test.World.Features = new Dictionary<FeatureId, WorldFeature> { [featureId] = feature };
        var entrance = new EntranceDecl("test-door", new(10, 3, 12.5f), new(10, 3, 7), 2, true)
        { Center = new(10, 3, 10), YawDeg = 180 };
        test.World.Interiors = new FakeInteriors(new InteriorIndexEntry(featureId, [], [entrance], true, "test-provider"));
        test.Reload();
        var door = test.Atlas.Find(new("door:test-door"))!;
        AssertThat(door.At == new Vector2(10, 10) && door.Teleport?.Stand?.Feet == entrance.Outside).OverrideFailureMessage("Door marker copied an incorrect location.").IsTrue();
        AssertThat(door.Teleport!.Fly.Position.Y == 9 && door.Teleport.Stand!.YawDeg == 0).OverrideFailureMessage("Door viewing direction is wrong.").IsTrue();
    }

    [TestCase]
    public void RealCampusAndInteriorAssemblyProduceUniqueAtlasDoorMarkers()
    {
        var files = RealMapFixture.ReadFiles();
        var interiorBytes = files["interiors.json"].ToArray();
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
        var campus = RealMapFixture.Load();
        var world = new FakeWorld
        {
            Features = campus.World.Features,
            Boundary = campus.World.Boundary,
            Spatial = new FlatSpatial(),
        };
        var descriptor = RealMapFixture.Entry().Descriptor;
        var providers = typeof(LibraryBoxInterior).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IInteriorProvider).IsAssignableFrom(type))
            .Select(type => (Type: type, Registration: type.GetCustomAttribute<InteriorProviderAttribute>()))
            .Where(item => item.Registration is not null)
            .ToDictionary(item => item.Registration!.Id, item => (IInteriorProvider)Activator.CreateInstance(item.Type)!, StringComparer.Ordinal);
        var built = InteriorAssembly.Build(interiorBytes,
            new BuildContext { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, Map = descriptor, World = world, WorldSeed = descriptor.Seed }, providers);
        world.Interiors = new FakeInteriors(built.Entries.ToArray());
        var doors = built.Entries.SelectMany(entry => entry.Entrances.Where(door => door.Open)
            .Select(door => (Feature: entry.Feature, Door: door))).ToArray();
        AssertThat(campus.World.Features.Count > 0 && built.Entries.Select(entry => entry.Feature)
            .OrderBy(id => id.Value, StringComparer.Ordinal).SequenceEqual(expectedIndexFeatures)).OverrideFailureMessage("Regression did not assemble the complete campus and registered interior index.").IsTrue();
        AssertThat(built.Entries.Where(entry => entry.Open).Select(entry => entry.Feature)
            .OrderBy(id => id.Value, StringComparer.Ordinal).SequenceEqual(open.OrderBy(id => id.Value, StringComparer.Ordinal))).OverrideFailureMessage("Closed registration changed the open interior feature set.").IsTrue();
        AssertThat(doors.Count(item => !authoredDoorIds.Contains(item.Door.Id)) > 1).OverrideFailureMessage("Regression no longer exercises multiple generated fallback entrances.").IsTrue();
        AssertThat(doors.Select(item => item.Door.Id).Distinct(StringComparer.Ordinal).Count() == doors.Length).OverrideFailureMessage("Interior provider reused an entrance ID across different building features.").IsTrue();
        var atlas = new AtlasService(NewCamera(), new Observer(), new TestRuleset());
        atlas.LoadMap(new TestMapContext(descriptor, world),
            JsonSerializer.Deserialize<AtlasMarkerFile>(files["atlas/markers.json"].Span, ContractJson.Options)!,
            JsonSerializer.Deserialize<AtlasLabelFile>(files["atlas/labels.json"].Span, ContractJson.Options)!,
            JsonSerializer.Deserialize<WaypointFile>(files["waypoints.json"].Span, ContractJson.Options)!);
        AssertThat(atlas.Markers.Count(marker => marker.Kind == MarkerKind.Interior) == doors.Length).OverrideFailureMessage("Atlas silently discarded an entrance from the real interior index.").IsTrue();
        foreach (var item in doors)
        {
            var marker = atlas.Find(new("door:" + item.Door.Id));
            AssertThat(marker is not null && marker.Feature == item.Feature
                && marker.At == new Vector2(item.Door.Center.X, item.Door.Center.Z)
                && marker.Teleport?.Stand?.Feet == item.Door.Outside).OverrideFailureMessage("A real interior marker lost its owner, center, or outside landing.").IsTrue();
        }
        foreach (var id in authoredDoorIds)
            AssertThat(atlas.Find(new("door:" + id)) is not null).OverrideFailureMessage("An authored entrance ID changed during fallback repair.").IsTrue();
        foreach (var door in closed)
        {
            var feature = new FeatureId(door.GetProperty("featureId").GetString()!);
            var id = door.GetProperty("id").GetString()!;
            var entry = built.Entries.Single(item => item.Feature == feature);
            AssertThat(!entry.Open && entry.ProviderId is null && entry.Entrances.Single(item => item.Id == id).Open == false &&
                atlas.Find(new("door:" + id)) is null).OverrideFailureMessage("Closed registration gained an open interior marker or provider.").IsTrue();
        }
    }
    private static TestCameraRig NewCamera() => new(pose: new Pose(new(500, 100, 500), 0, 0), transit: true) { Mode = ViewMode.Walk };
    private sealed class Fixture
    {
        internal TestCameraRig Camera { get; } = NewCamera();
        internal Driver Driver { get; } = new();
        internal TestView View { get; }
        internal TestAppFlow Flow { get; } = new();
        internal Observer Observer { get; } = new();
        internal FakeWorld World { get; }
        internal TestMapContext Context { get; }
        internal AtlasService Atlas { get; }
        internal Fixture(string policy = "all-unlocked")
        {
            World = new() { Boundary = new([]) };
            var map = new MapDescriptor(new("test-map"), new("测试", "Test"), MapKind.Outdoor, 1, 1,
                new(0, 0, 1, new(-1000), new(1000)), Vector2.Zero, new(new(-1000), new(1000)),
                new(new(-1000), new(1000)), new(1, 30, 3000, -28, 2000, 30, 40, 150),
                default, default, "test-spawn", [],
                new(MapTimeRule.Shared, new(17.5, Season.Autumn), new("测试", "Test"), false, "test"),
                new("test", "test", []), new Dictionary<string, string>(), []);
            Context = new(map, World);
            var maps = new SingleMapService(Context, Camera);
            View = new(Observer);
            Atlas = new(Camera, Observer, new TestRuleset(policy), Driver);
            Flow.ActivitySource = () =>
                (maps.Transition is { Done.IsCompleted: false } ? AppActivity.MapTransition : AppActivity.None) |
                (Camera.ActiveTransit is { Done.IsCompleted: false } ? AppActivity.CameraTransition : AppActivity.None);
            Atlas.Connect(maps, Flow, new TestAudio(), View);
            Reload();
        }
        internal void Reload() => Atlas.LoadMap(Context, new(1, Context.Descriptor.DataRect,
            [Anchor("test-kiosk", Vector2.Zero, AnchorForm.Kiosk), Anchor("test-post", new(50, 0), AnchorForm.Post),
                Anchor("test-roof", new(600, 600), AnchorForm.Post) with { Teleport = new(new(new(600, 30, 600), 0, -12), null) }]),
            new(1, [new("test-district", new("测试区", "Test District"), null)], []), new(1, []));
        internal RestoreContext RestoreContext() => new() { Map = Context.Descriptor.Id, FromVersion = 1, Missing = false };
        private static AtlasMarker Anchor(string id, Vector2 at, AnchorForm form) => new(new(id), MarkerKind.Anchor,
            new(id, id), at, "anchor", 1, 0, "test-district", new(new(new(at.X, 6, at.Y), 0, -12), new(new(at.X, 0, at.Y), 0)), new(id), Form: form);
    }
    private sealed class Observer : IObserverState
    {
        public Vector3 Feet { get; set; } = new(200, 0, 200);
        public Vector3 Eye => Feet + Vector3.UnitY;
        public float YawDeg => 0; public NavSpace? Space => null; public ViewMode Mode { get; set; } = ViewMode.Walk;
    }
    private sealed class SingleMapService : TestMapService
    {
        private readonly TestMapContext _context;
        private readonly TestCameraRig _camera;
        public SingleMapService(TestMapContext context, TestCameraRig camera)
        {
            _context = context; _camera = camera;
            Rev = 1; Default = Current = context.Descriptor.Id; Maps = [context.Descriptor]; Context = context;
        }
        // This fixture starts on its sole map at world hour zero, before any weather epoch change.
        public override IReadOnlyList<MapVisit> Visits => [new(Current, _camera.Pose, _camera.Mode)];

        public override IMapTransition Travel(MapTravelRequest request)
        {
            if (request.Target != Current) return Finish(Current, request.Target, request.Cause, MapTravelResult.Rejected);
            var spawn = request.Spawn is null ? null : _context.Descriptor.Spawns.SingleOrDefault(s => s.Id == request.Spawn);
            if (request.Pose is null && request.Spawn is not null && spawn is null)
                return Finish(Current, request.Target, request.Cause, MapTravelResult.Rejected);
            var mode = request.Mode ?? _camera.Mode;
            _ = mode switch
            {
                ViewMode.Fly or ViewMode.Walk => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(request)),
            };
            _camera.SetPose(request.Pose ?? spawn?.Fly ?? _camera.Pose);
            _camera.Mode = mode;
            Rev++;
            var arrived = Finish(Current, Current, request.Cause, MapTravelResult.Arrived);
            RaiseArrived(Current, request.Cause);
            return arrived;
        }

        private IMapTransition Finish(MapId from, MapId to, MapTravelCause cause, MapTravelResult result) =>
            LastTransition = TestMapTransition.Completed(from, to, cause, result);
    }
    private sealed class TestView(Observer observer) : IViewControl
    {
        public int Switches { get; private set; }
        public bool Refuse { get; set; }
        public ViewMode Mode => observer.Mode;
        public Task<ModeSwitchResult> SetMode(ViewMode m, ModeSwitchVia via)
        {
            if (Refuse) return Task.FromResult(ModeSwitchResult.Refused);
            Switches++; observer.Mode = m; return Task.FromResult(ModeSwitchResult.Ok);
        }
    }
    private sealed class Driver : ITransitDriver
    {
        public bool IsReady { get; set; } = true;
        public Pose? Destination => null;
        public ITransitHandle Start(Pose from, Pose to, TransitOptions? options = null) => throw new NotSupportedException();
    }
    private sealed class TestAudio : Campus.Contracts.Audio.IAudioService
    {
        public Campus.Contracts.Audio.AudioSettings Settings { get; } = new();
        public Campus.Contracts.Audio.AudioSnapshot Snapshot => default!;
        public event Action<Campus.Contracts.Audio.AudioSettings>? SettingsChanged { add { } remove { } }
        public void ApplySettings(Campus.Contracts.Audio.AudioSettings settings) => throw new NotSupportedException();
        public void PlayUi(Campus.Contracts.Audio.UiCue cue) { }
        public void SetMusic(Campus.Contracts.Audio.MusicCue cue, float fadeSeconds = 1.5f) => throw new NotSupportedException();
        public void Cine(string cue, float seconds, float db = 0f) => throw new NotSupportedException();
        public void Duck(Campus.Contracts.Audio.DuckReason reason, float db, float attackSeconds, float releaseSeconds) { }
        public void ReleaseDuck(Campus.Contracts.Audio.DuckReason reason) { }
    }

}
