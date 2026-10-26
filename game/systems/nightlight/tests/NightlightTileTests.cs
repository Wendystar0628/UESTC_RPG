using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Systems.Nightlight;
using Campus.Systems.Nightlight.Glow;
using Campus.Systems.Nightlight.Lamps;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Tests;

[TestSuite]
public sealed class NightlightTileTests
{
    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void TileLampGlowReferencesTheResidentCatalogIds()
    {
        LampRecord Lamp(float x) => new(new Vector3(x, 2f, 4f), 5f, 3f, 51, 0.75f, 12, 6f, 0.2f, 0, 0.6f);
        var lamps = new[] { Lamp(-70f), Lamp(4f), Lamp(80f) };
        var set = GlowSets.Lamps(lamps, [2, 0]);
        AssertThat(set.Count == 2 && set.Buffer[12] == 2f && set.Buffer[GlowSet.Stride + 12] == 0f)
            .OverrideFailureMessage("Tile compaction must preserve global lamp IDs in custom.x.").IsTrue();
        AssertThat(set.Buffer[3] == lamps[2].Head.X && set.Buffer[7] == lamps[2].Head.Y)
            .OverrideFailureMessage("Glow must use the resident lamp's real ground height.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void FacadeCrossingTileBoundaryKeepsWindowNumberingAndOneOwner()
    {
        var facade = new WindowFacadeDecl(1000, 12, LightChannelCatalog.Id(1), 4, 4, 1,
            new Vector3(59f, 0f, 5f), Vector3.UnitX, Vector3.UnitY, 3f, 2f, 2f, [1f]);
        var west = GlowSets.Windows([facade], [], new TileKey(0, 0), out _);
        var east = GlowSets.Windows([facade], [], new TileKey(1, 0), out _);
        AssertThat(west.Count + east.Count == facade.Columns).OverrideFailureMessage("Each center owns exactly one tile.").IsTrue();
        foreach (var pair in new[] { (west, new TileKey(0, 0)), (east, new TileKey(1, 0)) })
            for (var i = 0; i < pair.Item1.Count; i++)
            {
                var o = i * GlowSet.Stride;
                var center = new Vector3(pair.Item1.Buffer[o + 3], pair.Item1.Buffer[o + 7], pair.Item1.Buffer[o + 11]);
                AssertThat(TileGrid.At(center) == pair.Item2).OverrideFailureMessage("Window belongs to its center's owner.").IsTrue();
            }
        var ids = Enumerable.Range(0, west.Count).Select(i => west.Buffer[i * GlowSet.Stride + 12])
            .Concat(Enumerable.Range(0, east.Count).Select(i => east.Buffer[i * GlowSet.Stride + 12])).ToArray();
        AssertThat(ids.SequenceEqual(Enumerable.Range(0, facade.Columns).Select(c => (float)(facade.BaseId + facade.ColumnStart + c))))
            .OverrideFailureMessage("Splitting a facade may not renumber or duplicate windows.").IsTrue();
        AssertThat(east.Buffer[13] == facade.BaseId && east.Buffer[14] == LightChannelCatalog.Slot(facade.Channel) + 32f * facade.TotalColumns)
            .OverrideFailureMessage("Base ID, total columns and channel slot remain global.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void LampDemandIncludesGroundReachAndHasNoWindowDistanceCutoff()
    {
        var map = new MapId("fixture.nightlight");
        var groundEntry = new BakeTileEntry(new(map, BakeModuleIds.NightlightLamps, "L0/0/0"),
            new Box3(new Vector3(-4f, -2f, -20f), new Vector3(4f, 8f, 4f)));
        var view = new StreamDemandView(new Pose(Vector3.Zero, 0f, 0f), new Plane[] { new(Vector3.UnitZ, 0f) }, ReadOnlyMemory<Box3>.Empty);
        AssertThat(GlowDemand.Required(groundEntry, view)).OverrideFailureMessage("A head behind the eye can still light visible ground.").IsTrue();
        var farLamp = groundEntry with { Bounds = new(new Vector3(-1f, 2f, -1001f), new Vector3(1f, 8f, -999f)) };
        AssertThat(GlowDemand.Required(farLamp, view)).OverrideFailureMessage("Lamp halos must not inherit the 250 m window fade.").IsTrue();
        var farWindow = farLamp with { Key = new(map, BakeModuleIds.NightlightWindows, "L0/0/0") };
        AssertThat(!GlowDemand.Required(farWindow, view)).OverrideFailureMessage("Windows retain their existing shader fade extent.").IsTrue();
    }

    [TestCase]
    [TestCategory(TestReason.Contract)]
    public void ReflectionWindowIndicesStayLocalToTheirMountedTile()
    {
        var map = new MapId("fixture.nightlight");
        var windows = new[]
        {
            new WindowDecl(GlowSets.OutskirtsFirstId, GlowSets.OutskirtsFirstId, 2, LightChannelCatalog.Id(6),
                new Vector3(10f, 4f, 2f), Vector3.UnitX, Vector3.UnitY, 2f, 2f),
            new WindowDecl(GlowSets.OutskirtsFirstId + 1, GlowSets.OutskirtsFirstId, 2, LightChannelCatalog.Id(6),
                new Vector3(70f, 4f, 2f), Vector3.UnitX, Vector3.UnitY, 2f, 2f)
        };
        NightlightTileContent Tile(int x) => new(new(map, BakeModuleIds.NightlightWindows, $"L0/{x}/0"),
            new(x, 0), [], GlowSets.Windows([], windows, new(x, 0), out _), [0]);
        var first = Tile(0);
        var second = Tile(1);
        var channels = Enumerable.Repeat(Vector2.One, LightChannelCatalog.Count).ToArray();
        var selection = new ReflectionLampSelection();
        selection.Select([], [first, second], channels, ReadOnlySpan<byte>.Empty, ReadOnlySpan<float>.Empty, windows[1].Center);
        AssertThat(selection.Data[1] == windows[1].Id && selection.Data[ReflectionLampSelection.Capacity * 4] == windows[1].Center.X)
            .OverrideFailureMessage("Local index zero must address its own tile buffer.").IsTrue();
        var expected = selection.Data.ToArray();
        selection.Select([], [second, first], channels, ReadOnlySpan<byte>.Empty, ReadOnlySpan<float>.Empty, windows[1].Center);
        AssertThat(selection.Data.SequenceEqual(expected)).OverrideFailureMessage("Mount arrival order may not change reflection candidates.").IsTrue();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public void StaticGlowUploadDoesNotRequireDisplayActivation()
    {
        var pool = new ReadyPool();
        var host = new ReadyPoolHost(pool);
        using var material = new Godot.ShaderMaterial { Shader = new Godot.Shader() };
        var lamp = new LampRecord(Vector3.Zero, 4f, 3f, 51, 0.75f, 12, 6f, 0.2f, 0, 0.6f);
        var mount = new GlowMount(GlowKind.Lamp, "fixture.preload");
        mount.Upload(host, () => material, GlowSets.Lamps([lamp], [0]));
        AssertThat(mount.IsReady && !mount.VisibleReady && pool.VisibleCount == 0)
            .OverrideFailureMessage("Static glow resources must be ready while the title keeps their draws hidden.").IsTrue();
        mount.SetVisible(true);
        AssertThat(mount.IsReady && mount.VisibleReady && pool.VisibleCount == 1).IsTrue();
        mount.SetVisible(false);
        AssertThat(mount.IsReady && !mount.VisibleReady && pool.VisibleCount == 0)
            .OverrideFailureMessage("Ending the world must hide the pool without releasing uploaded resources.").IsTrue();
        mount.Free();
    }

    [TestCase, TestCategory(TestReason.Contract)]
    [RequireGodotRuntime]
    public void ViewReadinessIgnoresUnselectedHiddenGlowResources()
    {
        var map = new MapId("fixture.preload");
        var host = new ReadyPoolHost(new ReadyPool());
        var materials = new ReadyMaterialSource();
        var queue = new Campus.Kernel.Stream.StreamMountQueue(() => 0);
        var mounts = new GlowTileMounts(host, materials, () => true);
        mounts.Bind(map);
        var owners = new List<IStreamMountWork>();
        NightlightTileContent Tile(int x)
        {
            var center = new Vector3(x * TileGrid.BaseEdge, 0f, 0f);
            var glow = new GlowSet(GlowKind.Window, 1, new float[GlowSet.Stride], new(center, center + Vector3.One));
            return new(new(map, BakeModuleIds.NightlightWindows, $"L0/{x}/0"), new(x, 0), [], glow, []);
        }
        var current = Tile(0);
        var background = Tile(20);
        try
        {
            foreach (var tile in new[] { current, background })
            {
                var work = mounts.CreateMount(tile.Key, tile, BakePriority.Background, 0);
                owners.Add(work);
                AssertThat(work.Step(1d)).IsTrue();
            }
            var currentPool = host.Mounted[0];
            var backgroundPool = host.Mounted[1];
            var view = new StreamDemandView(new Pose(Vector3.Zero, 0f, 0f),
                new Plane[] { new(Vector3.UnitX, -20f), new(-Vector3.UnitX, -20f) }, ReadOnlyMemory<Box3>.Empty);
            StreamDemandView reflected = default;
            AssertThat(!mounts.IsReady(in view))
                .OverrideFailureMessage("Uploaded current-view resources still need real display application.").IsTrue();
            mounts.ApplyView(in view, false, in reflected);
            backgroundPool.Available = false;
            AssertThat(mounts.IsReady(in view) && !mounts.IsReady(background.Key) &&
                currentPool.VisibleCount == 1 && backgroundPool.VisibleCount == 0 &&
                mounts.ReflectionTiles.Count == 1 && mounts.ReflectionTiles[0].Key == current.Key)
                .OverrideFailureMessage("An unselected background tile must not block or change the applied view/reflection content.").IsTrue();
            currentPool.Available = false;
            AssertThat(!mounts.IsReady(in view)).OverrideFailureMessage("Selected resource readiness remains mandatory.").IsTrue();
        }
        finally { mounts.Bind(null); foreach (var owner in owners) queue.Retire(owner); queue.Drain(Campus.Tests.StreamLifecycleTestSupport.CoveredScreen); mounts.Dispose(); }
    }

    private sealed class ReadyMaterialSource : IShaderMaterialSource
    {
        public Godot.ShaderMaterial Create(ShaderId id) => new() { Shader = new Godot.Shader() };
    }

    private sealed class ReadyPool : IInstancePool
    {
        public int Capacity => 256;
        public bool Available;
        public bool IsReady => Available;
        public int VisibleCount { get; set; }
        public void WriteBuffer(ReadOnlySpan<float> data) => Available = true;
        public void SetAabb(Godot.Aabb aabb) { }
        public void SetParameter(Godot.StringName name, Godot.Vector4 value) { }
        public void SetParameter(Godot.StringName name, float value) { }
        public void Free() => Available = false;
    }

    private sealed class ReadyPoolHost(ReadyPool pool) : IInstancePoolHost
    {
        public readonly List<ReadyPool> Mounted = [];
        public IInstancePool Mount(InstancePoolSpec spec)
        {
            var next = Mounted.Count == 0 ? pool : new ReadyPool();
            Mounted.Add(next);
            return next;
        }
        public IInstancePool Mount(GeometryInstancePoolSpec spec) => throw new NotSupportedException();
    }

}
