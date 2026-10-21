using Campus.Gen.Ground;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

using static Campus.Kernel.Bake.BakeService;

namespace Campus.Kernel.Bake;

internal sealed class BakeTerrainPreparation(TerrainCacheService terrain)
{
    internal async Task PrepareOffline(MapBuild map)
    {
        foreach (var block in TerrainBlockCoverage.Region(TerrainBlockCoverage.OfflineDomain(map.Context)))
        {
            map.Lifetime.Token.ThrowIfCancellationRequested();
            var origin = TileGrid.Origin(block, LodLevel.L2);
            await terrain.PrepareRegion(map.Context, new(origin, origin), map.Lifetime.Token).ConfigureAwait(false);
        }
    }

    internal void PrepareProduct(MapBuild map, BakeKey key)
    {
        if (!(key.Detail.StartsWith("C/", StringComparison.Ordinal) || key.Detail.StartsWith('L'))) return;
        var request = BakeRequestKeys.Parse(key);
        // Cold entities and sites sample AssetWorld's metre nodes. Only a ready walking
        // height field or L0 ground tile publishes a runtime raster footprint here.
        if (!request.Collision && !(key.Module == GroundModule.Module && request.Level == LodLevel.L0)) return;
        var origin = TileGrid.Origin(request.Tile, LodLevel.L0);
        terrain.PrepareRegion(map.Context, new(origin, origin + new Vector2(TileGrid.BaseEdge)), map.Lifetime.Token)
            .GetAwaiter().GetResult();
    }
}
