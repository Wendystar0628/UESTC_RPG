using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Kernel.Bake;
using Campus.Kernel.Lod;
using Campus.Kernel.Stream;
using GdUnit4;
using Godot;
using N = System.Numerics;
using static Campus.Tests.StreamLifecycleTestSupport;

namespace Campus.Tests;
[TestSuite]
public sealed class StreamPreparationLifecycleTests
{
    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task InitializationFaultKeepsItsActualTaskAndReleasesPartialNativeStorage()
    {
        var bounds = new Box3(N.Vector3.Zero, N.Vector3.One);
        var key = new BakeKey(new("fixture"), "fixture.native", "L4/0/0");
        var valid = Bundle(key, bounds).Surfaces[0];
        var invalid = new BakedSurface(valid.Material, 0, valid.Primitive, valid.VertexCount, valid.IndexCount,
            valid.VertexData, valid.AttributeData, valid.IndexData, bounds);
        var bundle = new TileBundle(key, StreamTile.Root.Tile, LodLevel.L4, bounds, [valid, invalid]);
        using var owner = new PreparedTile(bundle);
        var task = new StreamGeometryPreparation().PrepareAsync(owner, bundle);
        Exception? original = null;
        try { await task; throw new InvalidOperationException("Invalid native input initialized successfully."); }
        catch (InvalidDataException error) { original = error; }
        Check(task.IsFaulted && owner.IsDisposed && owner.Draws.Count == 0);
        try { await task; throw new InvalidOperationException("Faulted initialization became successful."); }
        catch (Exception error) when (ReferenceEquals(error, original)) { }
        owner.Dispose();
        Check(owner.IsDisposed);
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task ShutdownAwaitsLateNativePreparationAndReleasesItsActualOwner()
    {
        using var fixture = new StreamFixture(geometry: true);
        fixture.Geometry.Hold = true;
        await fixture.Until(() => fixture.Geometry.Waiting.Count > 0);
        var draining = fixture.Unload();
        Check(!draining.IsCompleted && !fixture.Director.IsUnloaded);
        Check(fixture.Geometry.Prepared.Count > 0 && fixture.Geometry.Prepared.All(tile => !tile.IsDisposed));
        fixture.Geometry.Release();
        await draining;
        Check(fixture.Geometry.Prepared.Count > 0 && fixture.Geometry.Prepared.All(tile => tile.IsDisposed) && fixture.Director.IsUnloaded);
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public async Task FailedNativePreparationRetainsItsOwnerUntilExplicitDrain()
    {
        using var fixture = new StreamFixture(geometry: true);
        fixture.Geometry.Hold = true;
        await fixture.Until(() => fixture.Geometry.Waiting.Count > 0);
        var original = new InvalidDataException("native preparation failed");
        fixture.Geometry.Fail(original);
        Check(fixture.Director.Failure is null);
        fixture.Director.FinishMountFrame(100);
        Check(ReferenceEquals(fixture.Director.Failure, original));
        Check(fixture.Geometry.Prepared.Count > 0 && fixture.Geometry.Prepared.All(tile => !tile.IsDisposed));
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, original) && fixture.Geometry.Prepared.All(tile => !tile.IsDisposed));
        try { await fixture.Unload(); throw new InvalidOperationException("Drain lost failed initialization."); }
        catch (Exception error) when (ReferenceEquals(error, original)) { }
        Check(fixture.Director.IsUnloaded && fixture.Geometry.Prepared.All(tile => tile.IsDisposed));
        try { await fixture.Director.DrainAsync(); throw new InvalidOperationException("Repeated drain lost its original failure."); }
        catch (Exception error) when (ReferenceEquals(error, original)) { }
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task EmptyAggregateFailuresRemainPublishedAndReachDrainWithoutLosingNestedCauses()
    {
        using var fixture = new StreamFixture();
        await fixture.Until(() => fixture.Director.ViewReady);
        var original = new AggregateException();
        fixture.Mount.Drain(false, _ => throw original);
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, original));
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, original));
        var later = new InvalidDataException("later mount failure");
        var nested = new AggregateException();
        fixture.Mount.Drain(false, _ => throw new AggregateException(original, later, nested));
        fixture.Director._Process(0);
        var published = fixture.Director.Failure as AggregateException
            ?? throw new InvalidOperationException("Stream failure lost its collected causes.");
        Check(published.InnerExceptions.Count == 3 && ReferenceEquals(published.InnerExceptions[0], original) &&
            ReferenceEquals(published.InnerExceptions[1], later) && ReferenceEquals(published.InnerExceptions[2], nested));
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, published));
        try { await fixture.Unload(); throw new InvalidOperationException("Drain lost empty aggregate failures."); }
        catch (Exception error) when (ReferenceEquals(error, published)) { }
        Check(fixture.Director.IsUnloaded);
    }

    [TestCase, TestCategory(TestReason.Regression), RequireGodotRuntime]
    public async Task MountFailureIsCollectedByPollWithoutAdvancingReaders()
    {
        using var fixture = new StreamFixture();
        await fixture.Until(() => fixture.Director.ViewReady);
        var original = new InvalidDataException("mount queue failed");
        fixture.Mount.Drain(false, _ => throw original);
        Check(fixture.Director.Failure is null);
        _ = fixture.Director.ResidentReady;
        _ = fixture.Director.DestinationReady;
        _ = fixture.Director.ViewReady;
        Check(fixture.Director.Failure is null);
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, original));
        fixture.Director._Process(0);
        Check(ReferenceEquals(fixture.Director.Failure, original));
        try { await fixture.Unload(); throw new InvalidOperationException("Drain lost the original mount failure."); }
        catch (Exception error) when (ReferenceEquals(error, original)) { }
        Check(fixture.Director.IsUnloaded);
    }

    [TestCase, TestCategory(TestReason.Regression), TestCategory("NativeRender"), RequireGodotRuntime]
    public void ResidentBackdropAppearsEvenWhenReadyChildrenSkipTheCoarseRoot()
    {
        var bounds = new Box3(System.Numerics.Vector3.Zero, System.Numerics.Vector3.One);
        var tree = new LodTree(bounds, new StreamSettings());
        var visible = new HashSet<StreamTile>();
        tree.ResolveVisible(new HashSet<StreamTile> { StreamTile.Root }, _ => true, visible);
        if (visible.Contains(StreamTile.Root)) throw new InvalidOperationException("The fixture must skip the ready coarse root.");
        var source = new GeometryBundle(new(new ToonSpec(new("wall.lit"), new("wall.shade"))),
            [0, 0, 0, 1, 0, 0, 0, 0, 1], [0, 1, 0, 0, 1, 0, 0, 1, 0], [0, 1, 2]);
        var palette = Campus.Contracts.Tests.PaletteFixture.Catalog;
        var key = new BakeKey(new("fixture"), "fixture.resident", "L4/0/0");
        var bundle = new TileBundle(key, StreamTile.Root.Tile, LodLevel.L4, bounds,
            [BakeSurfacePacker.Pack(source with { Shader = ShaderId.DioramaFar }, bounds, palette),
             BakeSurfacePacker.Pack(source, bounds, palette)]);
        var scenario = RenderingServer.ScenarioCreate();
        var material = RenderingServer.MaterialCreate();
        try
        {
            using var prepared = new PreparedTile(bundle);
            prepared.Initialize(bundle);
            for (var i = 0; i < prepared.Draws.Count; i++) prepared.MountOne(i, scenario, material);
            var resident = prepared.Draws.Count(draw => draw.Resident);
            if (prepared.VisibleInstanceCount != resident || resident == 0 || resident == prepared.Draws.Count)
                throw new InvalidOperationException("Resident background stayed hidden until a root selection, or ordinary coarse geometry appeared early.");
            prepared.SetVisible(false, 0);
            if (prepared.VisibleInstanceCount != resident)
                throw new InvalidOperationException("Refining the root hid its resident background.");
            prepared.BeginRetirement();
            if (prepared.ReleaseOne() || prepared.IsDisposed)
                throw new InvalidOperationException("One bounded release dropped the tile's remaining native ownership.");
            prepared.Dispose();
            if (!prepared.IsDisposed || prepared.Instances.Count != 0 || prepared.Draws.Count != 0 || !prepared.ReleaseOne())
                throw new InvalidOperationException("Full release could not resume after a bounded retirement step.");
            prepared.Dispose();
        }
        finally { RenderingServer.FreeRid(material); RenderingServer.FreeRid(scenario); }
    }
}
