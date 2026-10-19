using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Contracts.Fauna;
using Campus.Contracts.Vegetation;

namespace Campus.Kernel.Bake;

public sealed partial class BakeService
{
    private void PrepareResidentTerrain(MapBuild map)
    {
        foreach (var module in map.Modules.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => pair.Value))
        {
            var context = map.CreateAssetContext(module.CodeHash);
            if (module.Value is IVegetationRuntimeSource vegetation) _ = vegetation.PrepareVegetation(context);
            if (module.Value is IFaunaContentSource fauna) _ = fauna.PrepareFauna(context);
            if (module.Value is IGroundCoverSource ground) _ = ground.PrepareGroundCover(context);
        }
    }
    private void PrepareProductTerrain(MapBuild map, BakeKey key)
    {
        if (!(key.Detail.StartsWith("C/", StringComparison.Ordinal) || key.Detail.StartsWith('L'))) return;
        var request = BakeRequestKeys.Parse(key);
        // Cold entities and sites sample AssetWorld's metre nodes. Only a ready walking
        // height field or L0 ground tile publishes a runtime raster footprint here.
        if (!request.Collision && !(key.Module == "ground" && request.Level == LodLevel.L0)) return;
        var origin = TileGrid.Origin(request.Tile, LodLevel.L0);
        _terrain.PrepareRegion(map.Context, new(origin, origin + new Vector2(TileGrid.BaseEdge)), map.Lifetime.Token)
            .GetAwaiter().GetResult();
    }

    internal void ReleaseColdAssets()
    {
        var progress = _queue.Progress;
        if (progress.Total == 0 || progress.Done != progress.Total) return;
        lock (_sync)
            foreach (var map in _contexts.Values.Where(map => map.Metadata is not null)) _terrain.ReleaseAssets(map.Context.World);
    }
}
