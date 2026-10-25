using System.Text.Json;
using System.Text.Json.Nodes;
using Campus.Kernel.Inspect;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Kernel.Frame;
using Campus.Kernel.Stream;
using GdUnit4;
using static Campus.Tests.StreamLifecycleTestSupport;

namespace Campus.Tests;

[TestSuite]
public sealed class StreamStatisticsTests
{
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task OneRealMapRetirementBurstKeepsEveryEventThroughBeginAndStop()
    {
        var probe = new PerfValueChannel("probe", "samples");
        var row = new PerfCapturedRow("samples", null, 0, new Dictionary<string, object?>());
        var slots = 0;
        while (probe.TryRow(row)) { slots++; probe.Flush(); }
        var modules = Enumerable.Range(0, slots + 1).ToDictionary(index => "fixture.geometry-" + index,
            _ => (IReadOnlyList<TileKey>)[new(0, 0)]);
        using var fixture = new StreamLifecycleTestSupport.StreamFixture(geometry: true,
            catalog: Task.FromResult(new Campus.Contracts.Bake.PreparedBakeTiles(modules, [])));
        await fixture.Until(() => fixture.Director.ViewReady && fixture.Director.Statistics.PendingBakes == 0 &&
            fixture.Director.Statistics.PendingPreparations == 0 && fixture.Director.Statistics.PendingMounts == 0);
        string root = PerformanceTestSupport.TemporaryDirectory();
        var session = new PerfRecordingSession(root, Campus.Contracts.Perf.PerfOrigin.Ai,
            DateTimeOffset.UnixEpoch, new PerfFieldDictionary(File.ReadAllText(RepositoryFixture.FilePath("game", "data", "perf-fields.json"))), PerformanceTestSupport.SessionManifest());
        var captured = new List<PerfCapturedEvent>();
        try
        {
            if (!await session.Ready) throw new InvalidOperationException("The retirement writer did not open.");
            fixture.Director.TileChanged += change =>
            {
                var value = new PerfCapturedEvent("stream.tileChanged", Godot.Engine.GetProcessFrames(), change.MonotonicMilliseconds, change);
                captured.Add(value);
                if (!session.TryEvent(value)) throw new InvalidOperationException("A same-frame retirement was rejected.");
            };
            await fixture.Unload();
            if (captured.Count <= slots || captured.Select(value => value.Frame).Distinct().Count() != 1)
                throw new InvalidOperationException("The fixture did not retire more events than the old single-row queue in one frame.");
            session.Begin(new(0, 0, 0));
            if (!await session.StartedWriting) throw new InvalidOperationException("The retirement writer did not start.");
            var tail = new PerfCapturedEvent("retirement.tail", captured[^1].Frame, captured[^1].Time + 1, new { });
            captured.Add(tail);
            if (!session.TryEvent(tail)) throw new InvalidOperationException("The final unsealed publication was rejected.");
            session.Stop(Campus.Contracts.Perf.PerfEndReason.Exit, tail.Time);
            var ended = await session.Completion;
            var directory = Path.Combine(root, ended.SessionId);
            var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(directory, "manifest.json")))!;
            var stream = manifest["streams"]!.AsArray().Single(value => value!["file"]!.GetValue<string>() == "events.jsonl")!;
            var written = File.ReadLines(Path.Combine(directory, "events.jsonl")).Select(line => JsonNode.Parse(line)!).ToArray();
            var actual = written.Where(value => value["source"]!.GetValue<string>() == "main").ToArray();
            if (!ended.Closed || !ended.Complete || !manifest["complete"]!.GetValue<bool>() ||
                stream["droppedRows"]!.GetValue<long>() != 0 || actual.Length != captured.Count ||
                written.Any(value => value["event"]!.GetValue<string>() == "record.gap"))
                throw new InvalidOperationException("A real retirement burst lost rows or advertised an incomplete drain.");
            for (var index = 0; index < captured.Count; index++)
                if (actual[index]["sequence"]!.GetValue<long>() != index + 3 ||
                    actual[index]["collectionFrame"]!.GetValue<ulong>() != captured[index].Frame ||
                    actual[index]["monotonicMs"]!.GetValue<double>() != captured[index].Time ||
                    !JsonNode.DeepEquals(actual[index]["payload"], JsonSerializer.SerializeToNode(captured[index].Payload, PerfFieldDictionary.JsonOptions)))
                    throw new InvalidOperationException("Event publication changed source order, frame, time or payload.");
        }
        finally { session.Stop(Campus.Contracts.Perf.PerfEndReason.Exit, 0); await session.Completion; Directory.Delete(root, true); }
    }


    [TestCase, TestCategory(TestReason.Contract), RequireGodotRuntime]
    public async Task UnloadRetainsCatalogBacklogUntilItsActualTaskDrains()
    {
        var catalog = new TaskCompletionSource<PreparedBakeTiles>();
        using var fixture = new StreamFixture(catalog: catalog.Task);
        var before = fixture.Director.Statistics;
        Check(before.PendingBakes > 0);
        var draining = fixture.Unload();
        var during = fixture.Director.Statistics;
        Check(!draining.IsCompleted && during.Revision > before.Revision &&
            during.PendingBakes > 0 && during.PendingRetirements > 0);
        Check(during.MountedBytes == 0 && during.MountedTiles == 0 && during.MountedTilesDelta == 0 &&
            during.MountMilliseconds == 0 && during.WatchdogTrips == 0);
        catalog.SetResult(new(new Dictionary<string, IReadOnlyList<TileKey>>(), []));
        await draining;
        RequireEmpty(fixture.Director.Statistics);
    }

    [TestCase, TestCategory(TestReason.Contract), RequireGodotRuntime]
    public async Task RevisionClearsMapCostAndTripBaselineWithoutResettingTheExecutorWatchdog()
    {
        var clock = 0d;
        var queue = new StreamMountQueue(() => clock);
        using var fixture = new StreamFixture(mount: queue);
        for (var frame = 0; frame < 3; frame++)
        {
            queue.Enqueue(new StreamLifecycleTestSupport.MountWork(StreamMountKind.Visual, queue.Revision,
                _ => clock += KernelBudgets.StreamMountMilliseconds * 3, map: fixture.Context.Descriptor.Id));
            queue.Drain(VisibleWorld);
        }
        var before = fixture.Director.Statistics;
        Check(before.MountMilliseconds > 0 && before.WatchdogTrips > 0);
        var trips = queue.WatchdogTrips;
        queue.RemoveMap(fixture.Context.Descriptor.Id);
        var after = fixture.Director.Statistics;
        Check(after.Revision > before.Revision && after.MountMilliseconds == 0 &&
            after.WatchdogTrips == 0 && queue.WatchdogTrips == trips);
        await fixture.Unload();
        RequireEmpty(fixture.Director.Statistics);
    }

    [TestCase, TestCategory(TestReason.Contract), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task CompletedPayloadEventsAndFrameTotalsFollowTheRealOwners()
    {
        using var fixture = new StreamFixture(geometry: true);
        fixture.CollisionParticipant.Need = StreamTileNeed.Required;
        var changes = new List<StreamTileChange>();
        fixture.Director.TileChanged += changes.Add;
        var cursor = 0;
        await fixture.Until(() =>
        {
            var sample = fixture.Director.Statistics;
            var frame = changes.Skip(cursor).ToArray(); cursor = changes.Count;
            Check(sample.MountedBytes == frame.Where(change => change.Change == StreamTileChangeKind.Mounted)
                .Sum(change => change.Bytes ?? 0));
            Check(sample.MountedTiles == frame.Count(change => change.Change == StreamTileChangeKind.Mounted));
            Check(sample.MountedTilesDelta == frame.Sum(change => change.Change switch
            {
                StreamTileChangeKind.Mounted => 1,
                StreamTileChangeKind.Unmounted => -1,
                _ => throw new InvalidOperationException("Unknown tile event.")
            }));
            return fixture.Director.ViewReady && sample.PendingBakes == 0 && sample.PendingPreparations == 0 && sample.PendingMounts == 0;
        });
        var mounted = changes.Where(change => change.Change == StreamTileChangeKind.Mounted).ToArray();
        Check(mounted.Length > 0 && mounted.All(change => change.Revision == fixture.Mount.Revision));
        var visual = mounted.Where(change => change.Kind == StreamMountKind.Visual).ToArray();
        Check(visual.Select(change => change.Key).ToHashSet().SetEquals(fixture.Geometry.Prepared.Select(owner => owner.Key)));
        Check(visual.Sum(change => change.Bytes ?? 0) == fixture.Geometry.Prepared.Sum(owner => owner.Bytes));
        Check(mounted.Any(change => change.Kind == StreamMountKind.Collision) &&
            mounted.Where(change => change.Kind == StreamMountKind.Collision).All(change => change.Bytes is null));
        fixture.Tick();
        Check(changes.Count == cursor && fixture.Director.Statistics.MountedBytes == 0 &&
            fixture.Director.Statistics.MountedTiles == 0 && fixture.Director.Statistics.MountedTilesDelta == 0);
        var revision = fixture.Mount.Revision;
        await fixture.Unload();
        var unmounted = changes.Where(change => change.Change == StreamTileChangeKind.Unmounted).ToArray();
        Check(unmounted.Length == mounted.Length && unmounted.All(change => change.Revision == revision));
        Check(unmounted.Select(change => (change.Key, change.Kind)).ToHashSet()
            .SetEquals(mounted.Select(change => (change.Key, change.Kind))));
        Check(changes.Zip(changes.Skip(1)).All(pair => pair.First.MonotonicMilliseconds <= pair.Second.MonotonicMilliseconds));
        RequireEmpty(fixture.Director.Statistics);
    }

    [TestCase, TestCategory(TestReason.Contract), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task UnfinishedOldPreparationNeverPublishesAMountAfterRevisionChanges()
    {
        using var fixture = new StreamFixture(geometry: true);
        fixture.Geometry.Hold = true;
        var changes = new List<StreamTileChange>();
        fixture.Director.TileChanged += changes.Add;
        await fixture.Until(() => fixture.Geometry.Waiting.Count > 0);
        var revision = fixture.Mount.Revision;
        var draining = fixture.Unload();
        Check(!draining.IsCompleted && fixture.Mount.Revision > revision &&
            fixture.Director.Statistics.PendingPreparations > 0);
        fixture.Geometry.Release();
        await draining;
        Check(changes.Count == 0);
        RequireEmpty(fixture.Director.Statistics);
    }

    [TestCase, TestCategory(TestReason.Contract), RequireGodotRuntime]
    public async Task OldCacheWorkerRemainsApplicationCumulativeWhileUnmountKeepsItsOldRevision()
    {
        var root = Path.Combine(Path.GetTempPath(), "uestc-rpg-stream-statistics-" + Guid.NewGuid().ToString("N"));
        var cache = new BakeCacheStore(root);
        try
        {
            using var fixture = new StreamFixture();
            fixture.CollisionParticipant.Need = StreamTileNeed.Required;
            var changes = new List<StreamTileChange>();
            fixture.Director.TileChanged += changes.Add;
            await fixture.Until(() => fixture.CollisionParticipant.Mounted.Count > 0);
            var revision = fixture.Mount.Revision;
            var key = new BakeKey(fixture.Context.Descriptor.Id, "fixture.cache", "old-worker");
            var release = new TaskCompletionSource();
            var worker = Task.Run(async () =>
            {
                await release.Task.ConfigureAwait(false);
                cache.Write(key, BakePayloadClass.Product, "fixture", [1, 2, 3]);
                Check(cache.TryRead(key, BakePayloadClass.Product, "fixture", static bytes => bytes) is not null);
            });
            // Advance the existing revision without loading another map or bypassing drain-before-load.
            try
            {
                fixture.Mount.RemoveMap(fixture.Context.Descriptor.Id);
                Check(fixture.Director.Statistics.Revision > revision && fixture.Director.Statistics.MountedTilesDelta == 0);
            }
            finally
            {
                release.TrySetResult();
                await worker;
            }
            Check(cache.CacheStatistics == new BakeCacheStatistics(1, 0, 1));
            await fixture.Unload();
            var retired = changes.Where(change => change.Change == StreamTileChangeKind.Unmounted).ToArray();
            Check(retired.Length > 0 && retired.All(change => change.Revision == revision));
            RequireEmpty(fixture.Director.Statistics);
            cache.Clear();
            Check(cache.CacheStatistics == new BakeCacheStatistics(1, 0, 1));
        }
        finally
        {
            cache.Stop();
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    private static void RequireEmpty(StreamStatistics sample) => Check(sample.PendingRequests == 0 &&
        sample.PendingBakes == 0 && sample.PendingPreparations == 0 && sample.PendingMounts == 0 &&
        sample.PendingRetirements == 0 && sample.MountMilliseconds == 0 && sample.WatchdogTrips == 0 &&
        sample.MountedBytes == 0 && sample.MountedTiles == 0 && sample.MountedTilesDelta == 0);
}
