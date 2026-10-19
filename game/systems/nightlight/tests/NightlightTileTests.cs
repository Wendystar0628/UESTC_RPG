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
        selection.Select([], [first, second], channels, ReadOnlySpan<byte>.Empty, windows[1].Center);
        AssertThat(selection.Data[1] == windows[1].Id && selection.Data[ReflectionLampSelection.Capacity * 4] == windows[1].Center.X)
            .OverrideFailureMessage("Local index zero must address its own tile buffer.").IsTrue();
        var expected = selection.Data.ToArray();
        selection.Select([], [second, first], channels, ReadOnlySpan<byte>.Empty, windows[1].Center);
        AssertThat(selection.Data.SequenceEqual(expected)).OverrideFailureMessage("Mount arrival order may not change reflection candidates.").IsTrue();
    }

}
