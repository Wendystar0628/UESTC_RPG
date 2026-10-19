using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Tests;

namespace Campus.Gen.Zones.Common.Tests;

/// <summary>Picks the cells a whole-zone determinism test compares: the few with the most placed items.</summary>
internal static class ZoneTestCells
{
    public static TileKey[] Heaviest(ZoneModuleBase module, BuildContext ctx, LodLevel lod = LodLevel.L0) =>
        [.. Items(module, ctx, lod).GroupBy(item => TileGrid.At(item.Anchor, lod))
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.X).ThenBy(group => group.Key.Z)
            .Take(TestSamples.HeaviestCells).Select(group => group.Key)];

    public static IReadOnlyList<SiteItem> Items(ZoneModuleBase module, BuildContext ctx, LodLevel lod = LodLevel.L0)
    {
        var tiles = lod == LodLevel.L4 ? [new TileKey(0, 0)] : module.Tiles(ctx)
            .Select(tile => TileGrid.At(TileGrid.Origin(tile), lod)).Distinct()
            .OrderBy(tile => tile.X).ThenBy(tile => tile.Z).ToArray();
        return tiles.SelectMany(tile => module.BuildItems(ctx, tile, lod)).ToArray();
    }

    public static IReadOnlyList<Collider> Colliders(ZoneModuleBase module, BuildContext ctx) =>
        module.Tiles(ctx).SelectMany(tile => module.Collide(ctx, tile)).ToArray();
}
