using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Walkable;
using Campus.Gen.World;
using Campus.Gen.Zones.DesignWaterfront;

namespace Campus.Pipeline.Walk;

/// <summary>The startup walk producer shared by offline tools and development map preparation. No I/O.</summary>
public static class WalkBakeGenerator
{
    public static WalkBakeData Build(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> sourceInputs, PaletteCatalog catalog)
    {
        var files = WalkBakeInputHash.SelectSourceFiles(sourceInputs);
        using var descriptor = JsonDocument.Parse(files["map.json"]);
        var map = new MapId(descriptor.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidDataException("Map id is missing."));
        var rect = descriptor.RootElement.GetProperty("dataRect").Deserialize<MapRect>(ContractJson.Options)
            ?? throw new InvalidDataException("Map dataRect is missing.");
        var loaded = WorldModelLoader.Load(map, files);
        var mapFile = descriptor.RootElement.Deserialize<MapFile>(ContractJson.Options)
            ?? throw new InvalidDataException("Map descriptor is missing.");
        var context = new BuildContext
        {
            Catalog = catalog,
            World = loaded.World,
            Map = mapFile.Complete(loaded.Geo, loaded.Center,
                new(new(rect.Min.X, mapFile.Roam.FloorY, rect.Min.Y), new(rect.Max.X, mapFile.Roam.CeilY, rect.Max.Y)),
                new((int)MathF.Floor(rect.Min.X / 64f), (int)MathF.Floor(rect.Min.Y / 64f)),
                new((int)MathF.Ceiling(rect.Max.X / 64f) - 1, (int)MathF.Ceiling(rect.Max.Y / 64f) - 1)),
            WorldSeed = mapFile.Seed,
            SourceInputs = files
        };
        var waterfront = new WaterfrontModule();
        var tiles = waterfront.Tiles(context).Distinct().OrderBy(tile => tile.Z).ThenBy(tile => tile.X).ToArray();
        var occupancy = tiles.SelectMany(tile => waterfront.Build(context, tile, LodLevel.L0).Occupancy ?? []).ToArray();
        var supports = tiles.SelectMany(tile => waterfront.Collide(context, tile)).OfType<RampCollider>()
            .Where(ramp => ramp.Tag == SurfaceTag.Ground).ToArray();
        var product = WalkableBaker.Build(loaded.World, map, rect, loaded.Boundary,
            WalkBakeInputHash.ComputeForMap(files), occupancy, supports);
        WalkBakeInputHash.RequireCurrent(product, files);
        return product;
    }
}
