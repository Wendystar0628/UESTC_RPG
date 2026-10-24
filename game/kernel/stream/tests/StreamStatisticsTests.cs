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
        Check(during.MountedBytes == 0 && during.MountedTilesDelta == 0 &&
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
            queue.Enqueue(new StreamMountQueueTests.MountWork(StreamMountKind.Visual, queue.Revision,
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
            fixture.Director.Statistics.MountedTilesDelta == 0);
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
        sample.MountedBytes == 0 && sample.MountedTilesDelta == 0);
}
