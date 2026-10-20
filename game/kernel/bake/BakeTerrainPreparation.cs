using Campus.Gen.Ground;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

using static Campus.Kernel.Bake.BakeService;

namespace Campus.Kernel.Bake;

internal sealed class BakeTerrainPreparation(TerrainCacheService terrain)
{
    internal void PrepareResident(MapBuild map)
    {
        foreach (var module in map.Content.Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value))
        {
            var context = map.Content.CreateAssetContext(module.CodeHash);
            if (module.Value is IGroundCoverSource ground) _ = ground.PrepareGroundCover(context);
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
