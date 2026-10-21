using Campus.Gen.World;
using Campus.Gen.Tests.Support;
using static GdUnit4.Assertions;
using Campus.Gen.BakeInput;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Contracts.AppFlow;
using Campus.Contracts.Save;
using Campus.Kernel.Maps;
using Campus.Kernel.Registry;
using GdUnit4;
using static Campus.Tests.MapServiceTestSupport;

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
    public void MapsSaveUsesStringIdsAndVectorArraysAndRejectsUnknownFields()
    {
        var f = new Fixture(); f.Boot();
        var context = new CaptureContext(SaveReason.Test, f.A, false) { Moment = new WorldSaveMoment(0, 0, Season.Autumn, 0) };
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
                map.DefaultSpawn, map.Spawns, map.World, map.Env, map.Portals);
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

    [TestCase, TestCategory(TestReason.Contract)]
    public void FoundationWaitsForMountWithoutSelectingAnEndpoint()
    {
        var f = new Fixture();
        f.Runtime.DelayMount();
        var pose = f.Camera.Pose;
        var preparing = f.Service.Prepare(f.A);
        f.Until(() => f.Runtime.MountPending);
        AssertThat(!preparing.IsCompleted && f.Runtime.Arrivals == 0 && f.Camera.Pose == pose && f.Service.Current == default).IsTrue();
        f.Runtime.ReleaseMount();
        f.Until(() => preparing.IsCompleted);
        preparing.GetAwaiter().GetResult();
        AssertThat(f.Service.Current == f.A && f.Runtime.Arrivals == 0 && f.Service.Rev == 1 && !f.Camera.InputLocked).IsTrue();
        var context = f.Service.Context;
        f.Service.Prepare(f.A).GetAwaiter().GetResult();
        AssertThat(ReferenceEquals(context, f.Service.Context) && f.Loader.Loads == 1 && f.Camera.Pose == pose).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    public void FailedFoundationRetainsItsOriginalFailureWithoutArrival()
    {
        var f = new Fixture(); f.Loader.Failing.Add(f.A);
        var preparing = f.Service.Prepare(f.A);
        f.Until(() => preparing.IsCompleted);
        AssertThat(preparing.IsFaulted && preparing.Exception!.InnerException is InvalidDataException &&
            f.Runtime.Arrivals == 0 && f.Service.Rev == 0 && !f.Camera.InputLocked).IsTrue();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task StopDuringWorldLoadWaitsForItsWorkerAndNeverMounts()
    {
        var f = new Fixture(); f.Loader.DelayNext(f.A);
        var preparing = f.Service.Prepare(f.A);
        f.Until(() => f.Loader.DelayedLoadStarted);
        f.Service.StopNewWork(); f.Runtime.StopNewWork();
        var drain = f.Service.DrainAsync();
        try
        {
            f.Service.Step();
            AssertThat(!drain.IsCompleted && !preparing.IsCompleted && f.Camera.InputLocked && f.Runtime.Mounted.Count == 0).IsTrue();
        }
        finally { f.Loader.ReleaseDelayed(); }
        await drain;
        try { await preparing; throw new InvalidOperationException("Stopped map preparation completed successfully."); }
        catch (OperationCanceledException) when (preparing.IsCanceled) { }
        AssertThat(f.Loader.CompletedLoads == 1 && f.Runtime.Mounted.Count == 0 && f.Service.Context is null &&
            f.Service.Rev == 0 && !f.Camera.InputLocked && ReferenceEquals(f.Service.DrainAsync(), drain)).IsTrue();
        f.Service.Dispose();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task StopDuringInputReadDrainsTheReadWithoutStartingWorldLoad()
    {
        var f = new Fixture(); f.Data.DelayRead();
        var preparing = f.Service.Prepare(f.A);
        f.Service.Step();
        f.Service.StopNewWork(); f.Runtime.StopNewWork();
        var drain = f.Service.DrainAsync();
        AssertThat(!drain.IsCompleted && !preparing.IsCompleted && f.Loader.Loads == 0).IsTrue();
        f.Data.ReleaseRead(); await drain;
        try { await preparing; throw new InvalidOperationException("Stopped input read completed preparation."); }
        catch (OperationCanceledException) when (preparing.IsCanceled) { }
        AssertThat(f.Loader.Loads == 0 && f.Runtime.Mounted.Count == 0 && !f.Camera.InputLocked).IsTrue();
        f.Service.Dispose();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task StopRetainsAnInputFailureAndCannotDisposeItsPendingRead()
    {
        var f = new Fixture(); f.Data.DelayRead();
        var preparing = f.Service.Prepare(f.A);
        f.Service.Step(); f.Service.StopNewWork(); f.Runtime.StopNewWork();
        var drain = f.Service.DrainAsync();
        var rejected = false;
        try { f.Service.Dispose(); }
        catch (InvalidOperationException) { rejected = true; }
        AssertThat(rejected && !drain.IsCompleted).IsTrue();
        var failure = new IOException("Input read failed during exit.");
        f.Data.ReleaseRead(failure);
        try { await drain; throw new InvalidOperationException("Drain hid the input error."); }
        catch (IOException error) when (ReferenceEquals(error, failure)) { }
        try { await preparing; throw new InvalidOperationException("Preparation hid the input error."); }
        catch (IOException error) when (ReferenceEquals(error, failure)) { }
        AssertThat(drain.IsFaulted && preparing.IsFaulted && f.Loader.Loads == 0 &&
            f.Runtime.Mounted.Count == 0 && !f.Camera.InputLocked).IsTrue();
        f.Service.Dispose();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task StopDuringMountObservesItsCanceledCompletion()
    {
        var f = new Fixture(); f.Runtime.DelayMount();
        var preparing = f.Service.Prepare(f.A);
        f.Until(() => f.Runtime.MountPending);
        f.Service.StopNewWork(); f.Runtime.StopNewWork();
        await f.Service.DrainAsync();
        try { await preparing; throw new InvalidOperationException("Stopped mount completed preparation."); }
        catch (OperationCanceledException) when (preparing.IsCanceled) { }
        AssertThat(f.Service.Current == default && f.Service.Rev == 0 && !f.Camera.InputLocked).IsTrue();
        f.Service.Dispose();
    }

    [TestCase, TestCategory(TestReason.Regression)]
    public async Task StopDuringCatalogReadObservesTheWorkerBeforeCancelingInitialization()
    {
        var f = new Fixture(initialize: false); f.Data.DelayCatalog();
        var initializing = f.Service.Initialize();
        f.Service.StopNewWork();
        var drain = f.Service.DrainAsync();
        AssertThat(!drain.IsCompleted && !initializing.IsCompleted).IsTrue();
        f.Data.ReleaseCatalog(); await drain;
        try { await initializing; throw new InvalidOperationException("Stopped catalogue initialization completed successfully."); }
        catch (OperationCanceledException) when (initializing.IsCanceled) { }
        AssertThat(f.Loader.Loads == 0 && f.Runtime.Mounted.Count == 0).IsTrue();
        f.Service.Dispose();
    }

}
