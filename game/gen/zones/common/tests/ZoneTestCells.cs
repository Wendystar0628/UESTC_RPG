using Campus.Contracts;
using Campus.Tests;

namespace Campus.Gen.Zones.Common.Tests;

/// <summary>Picks the cells a whole-zone determinism test compares: the few with the most placed items.</summary>
internal static class ZoneTestCells
{
    public static TileKey[] Heaviest(ZoneModuleBase module, BuildContext ctx, LodLevel lod = LodLevel.L0) =>
        [.. module.ItemsFor(ctx, lod).GroupBy(item => TileGrid.At(item.Anchor, lod))
            .OrderByDescending(group => group.Count()).ThenBy(group => group.Key.X).ThenBy(group => group.Key.Z)
            .Take(TestSamples.HeaviestCells).Select(group => group.Key)];
}
