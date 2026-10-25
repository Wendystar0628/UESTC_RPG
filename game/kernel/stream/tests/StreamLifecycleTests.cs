using Campus.Kernel.Boot;
using Campus.Gen.Tests.Support;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Bake;
using Campus.Style.Materials;
using Campus.Kernel.Lod;
using Campus.Kernel.Registry;
using Campus.Kernel.Stream;
using GdUnit4;
using Godot;
using N = System.Numerics;
using static Campus.Tests.StreamLifecycleTestSupport;
namespace Campus.Tests;
[TestSuite]
public sealed class StreamLifecycleTests
{
    // 本组建的着色器与材质要等包装终结才归还原生引用；整组跑完在这里收齐，单跑本组时才不漏到引擎退出（DroppedNatives）。
    [After]
    public void ReleaseDroppedNatives() => DroppedNatives.Release();

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task PendingDestinationCannotQueryTerrainOrInheritAppliedSelection()
    {
        using var fixture = new StreamFixture();
        await fixture.Until(() => fixture.Director.ViewReady);
        var terrain = fixture.Terrain;
        terrain.Prepared = false;
        ((FakeWorld)fixture.Context.World).Spatial = new FlatSpatial(forbidden: "Terrain has not been prepared.");
        var target = fixture.Camera.Pose with { Position = new(1, 2, 3) };
        fixture.Director.Prefetch(target, ViewMode.Fly);
        fixture.Tick();
        Check(!fixture.Director.DestinationReady && !fixture.Director.ViewReady && terrain.Work.Count > 0);
        var destination = fixture.Director.Destination;
        using var walking = fixture.Director.PinDestination(new(target, ViewMode.Walk));
        using var flying = fixture.Director.PinDestination(new(target, ViewMode.Fly));
        Check(fixture.Director.PinnedDestinations[0].Mode == ViewMode.Walk && ReferenceEquals(destination, fixture.Director.Destination));
        walking.Dispose();
        Check(fixture.Director.PinnedDestinations.Single().Mode == ViewMode.Fly && ReferenceEquals(destination, fixture.Director.Destination));
        var drain = fixture.Unload();
        foreach (var work in terrain.Work) work.SetResult();
        await drain;
        Check(fixture.Director.PinnedDestinations.Count == 0);
        terrain.Prepared = true; ((FakeWorld)fixture.Context.World).Spatial = new FlatSpatial();
        fixture.Director.OnMapLoaded(fixture.Context);
        using var nextGeneration = fixture.Director.PinDestination(new(target, ViewMode.Fly));
        flying.Dispose();
        Check(fixture.Director.PinnedDestinations.Single() == new MapStreamDestination(target, ViewMode.Fly));
        await fixture.Until(() => fixture.Director.PinnedDestinationReady(new(target, ViewMode.Fly)));
        nextGeneration.Dispose();
        Check(fixture.Director.PinnedDestinations.Count == 0);
        await fixture.Unload();
    }
    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task CurrentViewAndArrivalRequireFineGeometryToReplaceItsCoarseAncestor()
    {
        using var fixture = new StreamFixture(geometry: true);
        fixture.Bake.Hold = key => BakeRequestKeys.Parse(key).Level is LodLevel.L0 or LodLevel.L4;
        fixture.Director.Prefetch(fixture.Camera.Pose, ViewMode.Fly);
        await fixture.Until(() => fixture.Bake.Waiting.Any(handle => handle.TileKey.Level == LodLevel.L0));
        Check(!fixture.Director.ResidentReady && !fixture.Director.ViewReady && !fixture.Director.DestinationReady);
        fixture.Bake.CompleteLevel(LodLevel.L4);
        await fixture.Until(() => fixture.Director.ResidentReady);
        Check(!fixture.Director.ViewReady && !fixture.Director.DestinationReady);
        fixture.Bake.Release();
        await fixture.Until(() => fixture.Mount.Pending > 0, mount: false);
        Check(!fixture.Director.ViewReady && !fixture.Director.DestinationReady);
        await fixture.Until(() => fixture.Director.ViewReady && fixture.Director.DestinationReady);
        Check(fixture.Geometry.Prepared.Any(tile => tile.VisibleInstanceCount > 0));
        await fixture.Unload();
    }
    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task ElevatedMixedFrontierNeedsOnlyItsDrawnGeometryAndStillRejectsAHole()
    {
        using var fixture = new StreamFixture(geometry: true, elevated: true);
        var tree = new LodTree(fixture.Context.Descriptor.Bounds, new());
        var required = new HashSet<StreamTile>(); var refined = new HashSet<StreamTile>();
        tree.Select(fixture.Camera.Pose.Position, 0, fixture.Camera.Pose.FovDeg!.Value,
            LodLevel.L0, refined, required, refined);
        var frontier = required.Where(tile => !refined.Contains(tile)).ToArray();
        Check(frontier.Select(tile => tile.Level).Distinct().Count() > 1);
        var missing = frontier.OrderBy(tile => tile.Level).First();
        fixture.Bake.Hold = key => BakeRequestKeys.Parse(key) is var parsed && parsed.Tile == missing.Tile && parsed.Level == missing.Level;
        fixture.Director.Prefetch(fixture.Camera.Pose, ViewMode.Fly);
        await fixture.Until(() => fixture.Director.ResidentReady && fixture.Bake.Waiting.Count > 0);
        Check(!fixture.Director.ViewReady && !fixture.Director.DestinationReady);
        fixture.Bake.Release();
        await fixture.Until(() => fixture.Director.ViewReady && fixture.Director.DestinationReady);
        var drawn = fixture.Geometry.Prepared.Where(tile => tile.VisibleInstanceCount > 0).Select(tile => BakeRequestKeys.Parse(tile.Key).Level).Distinct();
        Check(drawn.Count() > 1);
        foreach (var currentFiner in new[] { true, false })
        {
            var pose = fixture.Camera.Pose;
            fixture.Camera.SetPose(pose with { FovDeg = currentFiner ? 15 : 120 });
            fixture.Director.Prefetch(pose with { Position = pose.Position + new N.Vector3(20, 0, 0), FovDeg = currentFiner ? 120 : 15 }, ViewMode.Fly);
            await fixture.Until(() => fixture.Director.ViewReady && fixture.Director.DestinationReady);
        }
        await fixture.Unload();
    }
    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task StaticTitlePinsMountHiddenResourcesAndArrivalReusesThoseOwners()
    {
        using var fixture = new StreamFixture(geometry: true, elevated: true, staticTitle: true);
        var target = new MapStreamDestination(fixture.Camera.Pose with { Position = new(100, 2, 100) }, ViewMode.Fly);
        using var first = fixture.Director.PinDestination(target);
        Check(!fixture.Director.PinnedDestinationReady(target) && fixture.Director.Destination is null);
        await fixture.Until(() => fixture.Director.PinnedDestinationReady(target));
        var eye = fixture.Camera.Pose;
        var owners = fixture.Geometry.Prepared.ToArray();
        Check(owners.Length > 1 && fixture.Director.Destination is null && fixture.Camera.Pose == eye);
        Check(owners.Where(owner => BakeRequestKeys.Parse(owner.Key).Level != LodLevel.L4).All(owner => owner.VisibleInstanceCount == 0));
        using var shared = fixture.Director.PinDestination(target);
        first.Dispose(); first.Dispose();
        Check(fixture.Director.PinnedDestinationReady(target));
        fixture.Camera.SetPose(target.Pose);
        fixture.Director.Prefetch(target.Pose, target.Mode, target.Space, target.CollisionFeet);
        await fixture.Until(() => fixture.Director.DestinationReady);
        Check(fixture.Geometry.Prepared.Count == owners.Length && owners.All(owner => !owner.IsDisposed));
        fixture.Director.ClearDestination(); fixture.Tick();
        Check(fixture.Director.Destination is null && fixture.Director.PinnedDestinationReady(target) && !fixture.Director.ViewReady);
        Check(owners.Where(owner => BakeRequestKeys.Parse(owner.Key).Level != LodLevel.L4).All(owner => owner.VisibleInstanceCount == 0));
        shared.Dispose();
        Check(!fixture.Director.PinnedDestinationReady(target));
        await fixture.Unload();
        Check(!fixture.Director.PinnedDestinationReady(target));
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task PinReadinessRequiresParticipantUploadAndReleasedTargetsCannotKeepReady()
    {
        using var fixture = new StreamFixture(staticTitle: true);
        fixture.CollisionParticipant.Need = StreamTileNeed.Required;
        fixture.CollisionParticipant.ResourcesUploaded = false;
        var feet = new N.Vector3(20, 0, 20);
        var target = new MapStreamDestination(fixture.Camera.Pose, ViewMode.Walk, CollisionFeet: feet);
        using var pin = fixture.Director.PinDestination(target);
        await fixture.Until(() => fixture.CollisionParticipant.Mounted.Count > 0);
        Check(!fixture.Director.PinnedDestinationReady(target));
        fixture.CollisionParticipant.ResourcesUploaded = true;
        await fixture.Until(() => fixture.Director.PinnedDestinationReady(target));
        Check(!fixture.Director.PinnedDestinationReady(target with { CollisionFeet = feet + N.Vector3.UnitY }));
        fixture.Director.Prefetch(target.Pose, target.Mode, target.Space, target.CollisionFeet);
        await fixture.Until(() => fixture.Director.ViewReady && fixture.Director.DestinationReady && fixture.Director.PinnedDestinationReady(target));
        fixture.Collision.CollisionReady = false;
        fixture.Tick();
        Check(fixture.Director.ViewReady && !fixture.Director.DestinationReady && !fixture.Director.PinnedDestinationReady(target));
        fixture.Collision.CollisionReady = true;
        await fixture.Until(() => fixture.Director.DestinationReady && fixture.Director.PinnedDestinationReady(target));
        Check(fixture.Director.ViewReady && fixture.Director.DestinationReady && fixture.Director.PinnedDestinationReady(target));
        fixture.Director.ClearDestination();
        pin.Dispose();
        Check(!fixture.Director.PinnedDestinationReady(target));
        fixture.Tick();
        Check(fixture.CollisionParticipant.Mounted.Count == 0);
        await fixture.Unload();
    }

    [TestCase, TestCategory(TestReason.Contract), RequireGodotRuntime]
    public async Task ShutdownDrainsTheCatalogAndDeliversLateFailuresWithoutPublication()
    {
        foreach (var fail in new[] { false, true })
        {
            var work = new TaskCompletionSource<PreparedBakeTiles>();
            using var fixture = new StreamFixture(catalog: work.Task);
            var original = new InvalidOperationException("Retired catalog failed.");
            var draining = fixture.Unload();
            Check(!draining.IsCompleted && !fixture.Director.IsUnloaded);
            if (fail) work.SetException(original); else work.SetResult(new(new Dictionary<string, IReadOnlyList<TileKey>>(), []));
            try { await draining; Check(!fail); } catch (Exception error) when (ReferenceEquals(error, original)) { }
            Check(fixture.Director.IsUnloaded && !fixture.Director.ResidentReady && ReferenceEquals(fixture.Director.Failure, fail ? original : null));
        }
    }
    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task CancelledSelectionAndPinsRetainTheirTasksAndLateFailuresUntilDrain()
    {
        using var fixture = new StreamFixture();
        await fixture.Until(() => fixture.Director.ViewReady);
        fixture.Terrain.Prepared = false;
        ((FakeWorld)fixture.Context.World).Spatial = new FlatSpatial(forbidden: "Terrain has not been prepared.");
        fixture.Director.Prefetch(fixture.Camera.Pose, ViewMode.Fly);
        using var pin = fixture.Director.PinDestination(new(fixture.Camera.Pose, ViewMode.Fly));
        fixture.Tick();
        var draining = fixture.Unload();
        Check(fixture.Terrain.Work.Count == 2 && !draining.IsCompleted && !fixture.Director.IsUnloaded && fixture.Terrain.Tokens.All(token => token.IsCancellationRequested));
        var planning = new InvalidDataException("late planning failure"); var pinned = new InvalidDataException("late pin failure");
        fixture.Terrain.Work[0].SetException(planning); fixture.Terrain.Work[1].SetException(pinned);
        try { await draining; throw new InvalidOperationException("Late terrain faults were lost."); }
        catch (AggregateException error) { Check(error.Flatten().InnerExceptions.Contains(planning) && error.Flatten().InnerExceptions.Contains(pinned)); }
        Check(fixture.Director.IsUnloaded && !fixture.Director.ResidentReady);
    }
}
