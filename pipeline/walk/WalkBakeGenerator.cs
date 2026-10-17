using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Walkable;
using Campus.Gen.World;
using Campus.Pipeline.Map;

namespace Campus.Pipeline.Walk;

/// <summary>The startup walk producer shared by offline tools and development map preparation. No I/O.</summary>
public static class WalkBakeGenerator
{
    public static WalkBakeData Build(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> sourceInputs, MapGenerationInputs generation)
    {
        var files = sourceInputs;
        using var descriptor = JsonDocument.Parse(files["map.json"]);
        var map = new MapId(descriptor.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidDataException("Map id is missing."));
        var rect = descriptor.RootElement.GetProperty("dataRect").Deserialize<MapRect>(ContractJson.Options)
            ?? throw new InvalidDataException("Map dataRect is missing.");
        var claims = new MapClaimSnapshot(generation.Modules.Select(pair =>
            new MapClaimSource(pair.Key, pair.Value.Owner, pair.Value.Global, pair.Value.Claims)));
        var baseline = claims.RequiresAreaResolution ? WorldModelLoader.Load(map, files.Where(pair =>
            !pair.Key.StartsWith("overlay/", StringComparison.Ordinal)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)).World : null;
        var loaded = WorldModelLoader.Load(map, files, new() { ClaimedBy = claims.OverlayOwners(baseline) });
        var mapFile = descriptor.RootElement.Deserialize<MapFile>(ContractJson.Options)
            ?? throw new InvalidDataException("Map descriptor is missing.");
        var min = loaded.Geo.BoundaryMin - new System.Numerics.Vector2(mapFile.Roam.FallbackMargin);
        var max = loaded.Geo.BoundaryMax + new System.Numerics.Vector2(mapFile.Roam.FallbackMargin);
        var mapDescriptor = mapFile.Complete(loaded.Geo, loaded.Center,
            new(new(min.X, mapFile.Roam.FloorY, min.Y), new(max.X, mapFile.Roam.CeilY, max.Y)),
            new((int)MathF.Floor(min.X / 64f), (int)MathF.Floor(min.Y / 64f)),
            new((int)MathF.Ceiling(max.X / 64f) - 1, (int)MathF.Ceiling(max.Y / 64f) - 1));
        var (context, interiors) = WalkWorldPreparation.CreateContext((WorldModelSnapshot)loaded.World, mapDescriptor, generation, files);
        var prepared = WalkWorldPreparation.Prepare(context, generation.Modules, interiors, generation.InteriorCodeHashes, generation.PartCollision);
        var product = WalkableBaker.Build(prepared.World, map, rect, loaded.Boundary,
            WalkBakeInputHash.ComputeForMap(files), prepared.Support, prepared.Occupancy);
        WalkBakeInputHash.RequireCurrent(product, files);
        return product;
    }
}
