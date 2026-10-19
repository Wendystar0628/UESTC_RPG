using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using static GdUnit4.Assertions;
using Campus.Pipeline.Map;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.Save;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using GdUnit4;

namespace Campus.Tests;

[TestSuite]
public sealed class MapServiceTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ResolveDefaultWaypointWithoutAnActiveMap()
    {
        var f = new Fixture(waypointDefault: true);
        var spawn = f.Service.ResolveSpawn(f.B);
        AssertThat(spawn.Id == "P00" && spawn.Fly == new Pose(new(37, 19, -4), 15, -12, 60) &&
            spawn.Stand == new WaypointStand(new(37, 0, -4), 15)).OverrideFailureMessage("DefaultSpawn backed by a waypoint was not resolved with its fly and stand poses.").IsTrue();
        AssertThat(f.Service.Context is null && f.Service.Current == default && f.Service.Rev == 0 &&
            f.Loader.Loads == 0 && f.Runtime.Arrivals == 0).OverrideFailureMessage("Spawn lookup unexpectedly loaded or travelled to a map.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ResolveExtraSpawnUsesTheRequestedMap()
    {
        var f = new Fixture(); f.Boot();
        var context = f.Service.Context;
        var extra = f.Service.ResolveSpawn(f.C, "extra");
        AssertThat(extra.Fly == new Pose(new(15, 25, 6), 40, -5) && extra.Stand is null).OverrideFailureMessage("Extra spawn did not come from the explicitly requested map.").IsTrue();
        AssertThat(ReferenceEquals(context, f.Service.Context) && f.Service.Current == f.A && f.Service.Rev == 1 && f.Loader.Loads == 1).OverrideFailureMessage("Resolving another map's extra spawn changed the active map.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ResolveSpawnRejectsUnknownMapAndUnknownSpawn()
    {
        var f = new Fixture(waypointDefault: true);
        foreach (var query in new (MapId Map, string? Spawn)[]
        { (new("missing-map"), null), (f.A, "missing-spawn"), (f.A, "extra"), (f.B, "") })
        {
            try { f.Service.ResolveSpawn(query.Map, query.Spawn); }
            catch (ArgumentException) { continue; }
            throw new InvalidOperationException("Unknown map/spawn was silently replaced by a default.");
        }
        AssertThat(f.Loader.Loads == 0 && f.Service.Transition is null).OverrideFailureMessage("Rejected spawn query started work.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ResolveSpawnRequiresCatalogueInitialization()
    {
        var f = new Fixture(initialize: false);
        try { f.Service.ResolveSpawn(f.A); }
        catch (InvalidOperationException)
        {
            var initialized = f.Service.Initialize();
            f.Until(() => initialized.IsCompleted);
            initialized.GetAwaiter().GetResult();
            AssertThat(f.Service.ResolveSpawn(f.A).Id == "start").OverrideFailureMessage("Initialized catalogue did not resolve its default.").IsTrue();
            return;
        }
        throw new InvalidOperationException("Spawn lookup before catalogue initialization was accepted.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BootWaitsForMountAndArrival()
    {
        var f = new Fixture();
        f.Runtime.DelayMount();
        f.Runtime.ArrivalReady = false;
        var travel = f.Service.Travel(new(f.A, Cause: MapTravelCause.Boot));
        f.Until(() => travel.Stage == MapStage.Mount);
        AssertThat(f.Service.Context is null).OverrideFailureMessage("Context became visible before Mount.").IsTrue();
        f.Service.Step();
        AssertThat(f.Service.Context is not null && f.Service.Current == default && f.Camera.InputLocked).IsTrue();
        AssertThat(f.Data.RectWrites == 1 && f.Runtime.Arrivals == 0).IsTrue();
        f.Runtime.ReleaseMount();
        f.Until(() => travel.Stage == MapStage.Warm);
        f.Service.Step();
        AssertThat(!travel.Done.IsCompleted && f.Runtime.Arrivals == 1).IsTrue();
        f.Runtime.ArrivalReady = true;
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(travel.Done.Result == MapTravelResult.Arrived && f.Service.Current == f.A && f.Service.Rev == 1 && !f.Camera.InputLocked).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SameMapRestoreWaitsForArrivalWithoutReloading()
    {
        var f = new Fixture(); f.Boot();
        var context = f.Service.Context;
        f.Runtime.ArrivalReady = false;
        var destination = new Pose(new(12, 40, 18), 25, -10, 60);
        var travel = f.Service.Travel(new(f.A, Pose: destination, Mode: ViewMode.Fly, Cause: MapTravelCause.LoadSave));
        f.Until(() => travel.Stage == MapStage.Warm);
        f.Service.Step();
        AssertThat(f.Camera.Pose == destination && !travel.Done.IsCompleted && f.Runtime.Arrivals == 2).IsTrue();
        f.Runtime.ArrivalReady = true;
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(travel.Done.Result == MapTravelResult.Arrived && ReferenceEquals(context, f.Service.Context) && f.Loader.Loads == 1).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ReplacedArrivalCannotCompleteMapTravel()
    {
        var f = new Fixture(); f.Boot();
        f.Runtime.ArrivalReady = false;
        var travel = f.Service.Travel(new(f.A, Cause: MapTravelCause.NewGame));
        f.Until(() => travel.Stage == MapStage.Warm);
        var old = f.Runtime.Arrival!;
        var replacement = f.Runtime.Arrive(f.Camera.Pose, f.Camera.Mode);
        old.Cancel();
        AssertThat(old.Done.IsCanceled && !replacement.Done.IsCompleted).IsTrue();
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(travel.Done.Result == MapTravelResult.Failed && !replacement.Done.IsCompleted).IsTrue();
        f.Runtime.ArrivalReady = true;
        AssertThat(replacement.Done.IsCompletedSuccessfully).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void DisposingMapServiceCancelsItsPendingArrival()
    {
        var f = new Fixture(); f.Boot();
        f.Runtime.ArrivalReady = false;
        var travel = f.Service.Travel(new(f.A, Cause: MapTravelCause.NewGame));
        f.Until(() => travel.Stage == MapStage.Warm);
        var arrival = f.Runtime.Arrival!;
        f.Service.Dispose();
        AssertThat(arrival.Done.IsCanceled && travel.Done.Result == MapTravelResult.Failed && !f.Camera.InputLocked).IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void BootWaitsForAsyncMountPreparationBeforeArrival()
    {
        var f = new Fixture();
        f.Runtime.DelayMount();
        var originalPose = f.Camera.Pose;
        var travel = f.Service.Travel(new(f.A, Cause: MapTravelCause.Boot));
        f.Until(() => travel.Stage == MapStage.Mount);
        f.Service.Step(); // Mount returns while its asynchronous prepare task is still pending.
        for (var frame = 0; frame < 3; frame++) f.Service.Step();
        AssertThat(f.Runtime.MountPending && travel.Stage == MapStage.Mount && !travel.Done.IsCompleted).OverrideFailureMessage("Async Mount was treated as a completed map binding.").IsTrue();
        AssertThat(f.Runtime.Arrivals == 0 && f.Camera.Pose == originalPose).OverrideFailureMessage("Destination work started before map preparation and callbacks completed.").IsTrue();
        AssertThat(f.Poses.LegalizeCalls == 0).OverrideFailureMessage("Destination was legalized against old or incomplete map data.").IsTrue();
        AssertThat(f.Service.Rev == 0 && f.Camera.InputLocked).OverrideFailureMessage("Unprepared map was granted to the observer.").IsTrue();

        f.Runtime.ReleaseMount();
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(travel.Done.Result == MapTravelResult.Arrived && f.Runtime.Arrivals == 1).OverrideFailureMessage("Released async Mount did not start one arrival.").IsTrue();
        AssertThat(f.Service.Current == f.A && f.Service.Rev == 1 && !f.Camera.InputLocked).OverrideFailureMessage("Async Mount did not finish the normal arrival lifecycle.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FailedAsyncMountDoesNotArrive()
    {
        var f = new Fixture();
        f.Runtime.DelayMount();
        var travel = f.Service.Travel(new(f.A, Cause: MapTravelCause.Boot));
        f.Until(() => travel.Stage == MapStage.Mount);
        f.Service.Step();
        f.Runtime.FailMount("Fixture asynchronous preparation failure.");
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(travel.Done.Result == MapTravelResult.Failed && f.Runtime.Arrivals == 0 &&
            f.Service.Rev == 0).OverrideFailureMessage("Failed asynchronous preparation was mistaken for readiness.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SameMapPreviewKeepsCameraAndSavedVisitWithoutReload()
    {
        var f = new Fixture(); f.Boot();
        var context = f.Service.Context;
        f.Data.SetVersion(f.A, 2);
        var savedPose = new Pose(new Vector3(23, 17, -9), 70, -8, 60);
        var titlePose = new Pose(new Vector3(-10, 90, 55), 4, -10, 38);
        f.Service.Restore(new(f.A, [new(f.A, savedPose, ViewMode.Fly)]),
            new() { Map = f.A, FromVersion = 1, Missing = false });
        f.Camera.SetPose(titlePose);
        var legalizeCalls = f.Poses.LegalizeCalls;
        var travel = f.Service.Travel(new(f.A, Cause: MapTravelCause.LoadSave));
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(ReferenceEquals(context, f.Service.Context) && f.Loader.Loads == 1).OverrideFailureMessage("Same-map LoadSave rebuilt the world.").IsTrue();
        AssertThat(f.Camera.Pose == titlePose && f.Service.Visits.Single().LastPose == savedPose).OverrideFailureMessage("Preview replaced the title camera or saved destination.").IsTrue();
        AssertThat(f.Poses.LegalizeCalls == legalizeCalls).OverrideFailureMessage("Presentation T0 was incorrectly forced into the roam volume.").IsTrue();
        AssertThat(ReferenceEquals(context!.SourceInputs, f.Service.Context!.SourceInputs) &&
            SnapshotVersion(context) == 1 && f.Data.ReadCount(f.A) == 1).OverrideFailureMessage("Same-map preview reread or replaced the mounted source snapshot.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LegalizedDestinationIsSharedByArrivalAndSpawn()
    {
        var f = new Fixture();
        var reachable = new Box3(new(-20, 0, -20), new(20, 100, 20));
        f.Poses.Project = pose => pose with { Position = Vector3.Clamp(pose.Position, reachable.Min, reachable.Max) };
        f.Poses.Allowed = pose => reachable.Contains(pose.Position);
        var requested = new Pose(new(500, 40, 5), 32, -8, 55);
        var expected = requested with { Position = new(20, 40, 5) };
        var travel = f.Service.Travel(new(f.A, Pose: requested, Cause: MapTravelCause.Boot));
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(travel.Done.Result == MapTravelResult.Arrived && f.Poses.LegalizeCalls == 1 &&
            f.Runtime.LastArrival == expected && f.Camera.Pose == expected).OverrideFailureMessage("Arrival and Spawn did not use the same legal finite destination.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void NonFiniteLegalizationResultFailsWithoutMovingCamera()
    {
        var f = new Fixture();
        var original = f.Camera.Pose;
        f.Poses.Project = pose => pose with { Position = new(float.NaN, 20, 10) };
        var travel = f.Service.Travel(new(f.A, Cause: MapTravelCause.Boot));
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(travel.Done.Result == MapTravelResult.Failed && f.Poses.LegalizeCalls == 1 &&
            f.Runtime.Arrivals == 0 && f.Camera.Pose == original).OverrideFailureMessage("Non-finite legalization output was applied or replaced by an invented fallback position.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void SameMapFullRestoreLegalizesSavedDestinationWithoutReload()
    {
        var f = new Fixture(); f.Boot();
        var context = f.Service.Context;
        var saved = new Pose(new(80, 40, 10), 12, -6, 60);
        var legal = saved with { Position = new(20, 40, 10) };
        f.Service.Restore(new(f.A, [new(f.A, saved, ViewMode.Fly)]),
            new() { Map = f.A, FromVersion = 1, Missing = false });
        f.Poses.Project = _ => legal;
        f.Poses.Allowed = pose => pose.Position.X <= 20;
        var travel = f.Service.Travel(new(f.A, Pose: saved, Mode: ViewMode.Fly, Cause: MapTravelCause.LoadSave));
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(travel.Done.Result == MapTravelResult.Arrived && ReferenceEquals(context, f.Service.Context) &&
            f.Loader.Loads == 1 && f.Runtime.LastArrival == legal && f.Camera.Pose == legal &&
            f.Service.Visits.Single().LastPose == (legal with { FovDeg = null })).OverrideFailureMessage("Full restore retained an obsolete saved pose or unnecessarily rebuilt the current world.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FiniteDestinationRejectedByRoamValidatorStillFails()
    {
        var f = new Fixture();
        var original = f.Camera.Pose;
        var excludedRoamVolume = new Box3(new(-1, 19, 9), new(1, 21, 11));
        f.Poses.Allowed = pose => !excludedRoamVolume.Contains(pose.Position);
        // This tests the map-service protocol, not building collision: fly mode may cross buildings.
        // Identity legalization deliberately cannot repair this excluded roam destination. It is finite,
        // but the independent legality query must still prevent prefetch, camera placement and arrival.
        var travel = f.Service.Travel(new(f.A, Cause: MapTravelCause.Boot));
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(travel.Done.Result == MapTravelResult.Failed && f.Poses.LegalizeCalls == 1 &&
            f.Runtime.Arrivals == 0 && f.Camera.Pose == original && f.Service.Rev == 0).OverrideFailureMessage("A finite destination rejected by the roam validator was treated as a legal arrival.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LeavingTheTreeCompletesAcceptedTravelAndPreventsLatePublication()
    {
        var f = new Fixture();
        f.Loader.DelayNext(f.A);
        var first = f.Service.Travel(new(f.A, Cause: MapTravelCause.Boot));
        var arrivals = 0;
        f.Service.Arrived += (_, _) => arrivals++;
        try
        {
            f.Until(() => f.Loader.DelayedLoadStarted);
            f.Service.Dispose(); f.Service.Dispose();
            AssertThat(first.Done.IsCompleted && first.Done.Result == MapTravelResult.Failed && !f.Camera.InputLocked).OverrideFailureMessage("Map shutdown left an accepted request or its input lease pending.").IsTrue();
            f.Loader.ReleaseDelayed();
            f.Until(() => f.Loader.CompletedLoads == 1);
            AssertThat(f.Service.Context is null && f.Runtime.Mounted.Count == 0 && arrivals == 0 &&
                f.Service.Travel(new(f.C, Cause: MapTravelCause.LoadSave)).Done.Result == MapTravelResult.Rejected).OverrideFailureMessage("A stopped map service published or accepted new work.").IsTrue();
        }
        finally { f.Loader.ReleaseDelayed(); }
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FailedBootFinishesWithoutRetry()
    {
        var f = new Fixture(); f.Loader.Failing.Add(f.A);
        var travel = f.Service.Travel(new(f.A, Cause: MapTravelCause.Boot));
        f.Until(() => travel.Done.IsCompleted);
        AssertThat(travel.Done.Result == MapTravelResult.Failed && f.Loader.Loads == 1).OverrideFailureMessage("Boot retried after a failed load.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void MapsSaveUsesStringIdsAndVectorArraysAndRejectsUnknownFields()
    {
        var f = new Fixture(); f.Boot();
        var context = new CaptureContext(SaveReason.Test, f.A, false);
        var dto = f.Service.Capture(in context);
        var json = JsonSerializer.Serialize(dto, f.Service.TypeInfo);
        AssertThat(json.Contains("\"current\":\"map-a\"", StringComparison.Ordinal) && json.Contains("\"position\":[", StringComparison.Ordinal)).OverrideFailureMessage("Map save lost contract ID/vector encoding.").IsTrue();
        var restored = JsonSerializer.Deserialize(json, f.Service.TypeInfo)!;
        AssertThat(restored.Current == dto.Current && restored.Visits.Single() == dto.Visits.Single()).OverrideFailureMessage("Map save round trip changed values.").IsTrue();
        try { JsonSerializer.Deserialize(json[..^1] + ",\"unknown\":1}", f.Service.TypeInfo); }
        catch (JsonException) { return; }
        throw new InvalidOperationException("Unknown map save field was accepted.");
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void RestoreDropsDeletedMapVisitsAndPreservesKnownVisitsInMapOrder()
    {
        var f = new Fixture();
        var current = new MapVisit(f.A, new(new(12, 20, -3), 45, -10), ViewMode.Fly);
        var known = new MapVisit(f.C, new(new(-9, 30, 4), 80, -15), ViewMode.Walk);
        var removed = new MapVisit(new("deleted-map"), new(new(2, 20, 8), 0, 0), ViewMode.Fly);
        var restore = new RestoreContext { Map = f.A, FromVersion = 1, Missing = false };
        f.Service.Restore(new(f.A, [known, removed, current]), restore);
        AssertThat(f.Service.Visits.SequenceEqual(new[] { current, known })).OverrideFailureMessage("Deleted map visits were retained or known visits changed their values or ordering.").IsTrue();
        AssertThat(restore.Repairs.Count == 1 && restore.Repairs[0].Contains(removed.Map.Value, StringComparison.Ordinal)).OverrideFailureMessage("Dropping a deleted map visit did not report its repair.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void CatalogRejectsTraversalAndDerivesCentroidFromBoundary()
    {
        var f = new Fixture();
        try { MapCatalog.ReadIndex(Encoding.UTF8.GetBytes("{\"version\":1,\"defaultMap\":\"map-a\",\"maps\":[{\"id\":\"map-a\",\"dir\":\"res://data/../map-a/\"}]}")); }
        catch (InvalidDataException)
        {
            var map = f.Catalog.Maps[0];
            var file = new MapFile(map.Id, map.Name, map.Kind, map.Version, map.Seed, map.DataRect, map.Roam,
                map.DefaultSpawn, map.Spawns, map.World, map.Env, map.Cinematics, map.Portals);
            var entry = MapCatalog.ReadEntry(new(map.Id, "res://data/" + map.Id.Value + '/'),
                JsonSerializer.SerializeToUtf8Bytes(file, ContractJson.Options),
                Encoding.UTF8.GetBytes("""
                    {"origin":[104,31],"crs":"EPSG:32648","bounds":[0,0,10,10],
                     "boundary":{"outer":[[0,0],[10,0],[0,10]],"holes":[]}}
                    """), []);
            AssertThat(Vector2.Distance(entry.Descriptor.Center, new(10f / 3, 10f / 3)) < TestTolerance.Position).OverrideFailureMessage("Map center used bounding-box center instead of polygon centroid.").IsTrue();
            return;
        }
        throw new InvalidOperationException("Traversal map root was accepted.");
    }

    private static int SnapshotVersion(IMapContext context)
    {
        using var document = JsonDocument.Parse(context.SourceInputs.Require("world/geo.json"));
        return document.RootElement.GetProperty("revision").GetInt32();
    }

    private sealed class Fixture
    {
        public MapId A { get; } = new("map-a");
        public MapId B { get; } = new("map-b");
        public MapId C { get; } = new("map-c");
        public MapCatalog Catalog { get; }
        public MapService Service { get; }
        public TestCameraRig Camera { get; }
        public TestRuntime Runtime { get; } = new();
        public TestLoader Loader { get; }
        public TestData Data { get; }
        public TestPoseValidator Poses { get; }
        public Fixture(bool initialize = true, bool waypointDefault = false)
        {
            var entries = new[] { A, B, C }.Select(MakeMap).ToArray();
            if (waypointDefault)
                entries[1] = entries[1] with
                {
                    Descriptor = entries[1].Descriptor with { DefaultSpawn = "P00", Spawns = [] },
                    Waypoints = [new("P00", new("总览", "Overview"), WaypointGroup.Overview,
                        new(new(37, 19, -4), 15, -12, 60), new(new(37, 0, -4), 15), null,
                        new(17.5, Season.Autumn, WeatherKind.Clear))]
                };
            entries[2] = entries[2] with
            {
                Descriptor = entries[2].Descriptor with
                { Spawns = [.. entries[2].Descriptor.Spawns, new("extra", new(new(15, 25, 6), 40, -5), null)] }
            };
            Catalog = new(new(1, A, entries.Select(e => new MapIndexEntry(e.Descriptor.Id, e.DataRoot)).ToArray()), entries);
            Data = new(Catalog); Loader = new(Catalog);
            var world = new TestWorldState(new TestWeatherProcess(), new TestRuleset());
            Camera = new(world);
            Poses = new(Runtime);
            Service = new(Camera, world, Runtime, Runtime, Loader, Poses, Data);
            if (initialize)
            {
                var initialized = Service.Initialize();
                Until(() => initialized.IsCompleted);
                initialized.GetAwaiter().GetResult();
            }
        }
        public void Boot() { var t = Service.Travel(new(A, Cause: MapTravelCause.Boot)); Until(() => t.Done.IsCompleted); AssertThat(t.Done.Result == MapTravelResult.Arrived).OverrideFailureMessage("Fixture boot failed.").IsTrue(); }
        public void Until(Func<bool> ready)
        {
            if (!SpinWait.SpinUntil(() => { Service.Step(); return ready(); }, 5000))
                throw new InvalidOperationException("Map transition did not finish; stage=" + Service.Transition?.Stage);
        }
        private static MapCatalogEntry MakeMap(MapId id)
        {
            var map = new MapDescriptor(id, new(id.Value, id.Value), MapKind.Outdoor, 1, 1,
                new(104, 31, 32648, new(-32), new(32)), Vector2.Zero, new(new(-32, -10, -32), new(32, 100, 32)),
                new(new(-64), new(64)), new(.9f, 30, 3000, -28, 2000, 30, 40, 150), new(-1, -1), new(0, 0),
                "start", [new("start", new(new(0, 20, 10), 0, -10), null)],
                new(MapTimeRule.Shared, new(17.5, Season.Autumn, WeatherKind.Clear), new("默认", "Default"), false, "test"),
                new("sky.json", "backdrop.json", []), new Dictionary<string, string>(), []);
            return new(map, "res://data/" + id.Value + '/', []);
        }
    }

    private sealed class TestData(MapCatalog catalog) : MapDataSource
    {
        private readonly Dictionary<MapId, int> _versions = [];
        private readonly Dictionary<MapId, int> _reads = [];
        public List<MapLoadInput> Inputs { get; } = [];
        public int RectWrites { get; private set; }
        public void SetVersion(MapId map, int version) => _versions[map] = version;
        public int ReadCount(MapId map) => _reads.GetValueOrDefault(map);
        public override Task<MapCatalog> ReadCatalog() => Task.FromResult(catalog);
        public override IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadWorldFiles(MapCatalogEntry entry) =>
            new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
            {
                ["map.json"] = JsonSerializer.SerializeToUtf8Bytes(new { id = entry.Descriptor.Id.Value }),
                ["world/geo.json"] = JsonSerializer.SerializeToUtf8Bytes(new { revision = _versions.GetValueOrDefault(entry.Descriptor.Id, 1) })
            };
        public override MapLoadInput ReadLoadInput(MapCatalogEntry entry)
        {
            _reads[entry.Descriptor.Id] = ReadCount(entry.Descriptor.Id) + 1;
            var files = ReadWorldFiles(entry);
            var input = new MapLoadInput(entry, files);
            Inputs.Add(input);
            return input;
        }
        public override MapClaimSnapshot? ReadClaims(MapId map) => null;
        public override void WriteMapRect(MapRect rect) => RectWrites++;
        public override void ReportError(string message) { }
    }
    private sealed class TestLoader(MapCatalog catalog) : IWorldModelLoader
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<IWorldModel, int> _worldVersions = new();
        private MapId? _delayMap;
        private TaskCompletionSource? _delayedEntered;
        private TaskCompletionSource? _releaseDelayed;
        private int _loads;
        public HashSet<MapId> Failing { get; } = [];
        public int Loads => Volatile.Read(ref _loads);
        public int CompletedLoads => _worldVersions.Count;
        public bool DelayedLoadStarted => _delayedEntered?.Task.IsCompleted == true;
        public int WorldVersion(IWorldModel world) => _worldVersions[world];
        public void DelayNext(MapId map)
        { _delayMap = map; _delayedEntered = new(); _releaseDelayed = new(); }
        public void ReleaseDelayed() => _releaseDelayed?.TrySetResult();
        public WorldModelLoadResult Load(MapId map, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files,
            WorldModelLoadInputs inputs)
        {
            Interlocked.Increment(ref _loads);
            if (_delayMap == map)
            {
                _delayMap = null;
                var release = _releaseDelayed!;
                _delayedEntered!.TrySetResult();
                release.Task.GetAwaiter().GetResult();
            }
            if (Failing.Contains(map)) throw new InvalidDataException("Fixture load failure.");
            catalog.TryGet(map, out var entry);
            var d = entry.Descriptor;
            var world = new FakeWorld { Boundary = new([]) };
            using var document = JsonDocument.Parse(files["world/geo.json"]);
            _worldVersions[world] = document.RootElement.GetProperty("revision").GetInt32();
            return new(world, d.Geo, d.Center, d.Bounds, d.TileMin, d.TileMax);
        }
    }
    private sealed class TestRuntime : IMapRuntime, IMapLifetime
    {
        private TaskCompletionSource? _mount;
        private bool _delayMount;
        private bool _arrivalReady = true;
        public IMapArrival? Arrival { get; private set; }
        public bool MountReady => _mount?.Task.IsCompletedSuccessfully == true;
        public bool ArrivalReady
        {
            get => _arrivalReady;
            set { _arrivalReady = value; if (value) (Arrival as TestArrival)?.Complete(); }
        }
        public bool MountPending => _mount is { Task.IsCompleted: false };
        public int Arrivals { get; private set; }
        public Pose? LastArrival { get; private set; }
        public List<MapId> Mounted { get; } = [];
        public List<IMapContext> Contexts { get; } = [];
        public void DelayMount() => _delayMount = true;
        public void ReleaseMount() => _mount!.TrySetResult();
        public void FailMount(string message) => _mount!.TrySetException(new InvalidDataException(message));
        public string DescribeReadiness() => $"mountReady={MountReady},arrival={Arrival?.Done.Status}";
        public Task ShutdownAsync(MapId map)
        {
            Arrival?.Cancel();
            return Task.CompletedTask;
        }
        public Task Mount(IMapContext context)
        {
            Mounted.Add(context.Descriptor.Id);
            Contexts.Add(context);
            _mount = new();
            if (!_delayMount) _mount.TrySetResult();
            return _mount.Task;
        }
        public IMapArrival Arrive(Pose pose, ViewMode mode, NavSpace? space = null)
        {
            if (!MountReady) throw new InvalidOperationException("A map must be mounted before arrival.");
            Arrival?.Cancel();
            Arrivals++;
            LastArrival = pose;
            var arrival = new TestArrival(new(pose, mode, space));
            Arrival = arrival;
            if (ArrivalReady) arrival.Complete();
            return arrival;
        }
    }
    private sealed class TestArrival(MapStreamDestination destination) : IMapArrival
    {
        private readonly TaskCompletionSource _done = new();
        public MapStreamDestination Destination => destination;
        public Task Done => _done.Task;
        public void Cancel() => _done.TrySetCanceled();
        public void Complete() => _done.TrySetResult();
    }
    private sealed class TestPoseValidator(TestRuntime runtime) : ICameraPoseValidator
    {
        public Func<Pose, Pose> Project { get; set; } = pose => pose;
        public Func<Pose, bool> Allowed { get; set; } = _ => true;
        public int LegalizeCalls { get; private set; }
        public Task<Pose> LegalizePrepared(Pose pose, CancellationToken cancellation = default)
        {
            if (!runtime.MountReady) throw new InvalidOperationException("Map callbacks must finish before destination legalization.");
            LegalizeCalls++;
            return Task.FromResult(Project(pose));
        }
        public bool IsLegal(Pose pose, bool outsideBuffer) => Allowed(pose);
    }
}
