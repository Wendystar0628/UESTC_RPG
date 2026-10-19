using Campus.Gen.World;
using Campus.Pipeline.Map;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Maps;

/// <summary>One frozen version of the authored map inputs.</summary>
public sealed record MapLoadInput(MapCatalogEntry Entry, BuildInputSnapshot SourceInputs)
{
    public IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files => SourceInputs.Entries;
}

public static class MapWorldLoading
{
    /// <summary>
    /// Worker-safe loading. Claims are detached on the main thread and resolved against the
    /// final world zones; this method never accesses a registry or engine object.
    /// </summary>
    public static WorldModelLoadResult LoadPrepared(IWorldModelLoader loader, MapLoadInput input,
        MapClaimSnapshot? claims = null)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(input);
        var map = input.Entry.Descriptor;
        var loaded = loader.Load(map.Id, input.Files, new(map));
        _ = claims?.Build(loaded.World); // Validate final per-aspect ownership before exposing the context.
        return loaded;
    }

    public static IMapContext CreateContext(MapCatalogEntry entry, WorldModelLoadResult loaded, BuildInputSnapshot sourceInputs)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(loaded);
        ArgumentNullException.ThrowIfNull(sourceInputs);
        var descriptor = entry.Descriptor with { Geo = loaded.Geo, Center = loaded.Center };
        if (descriptor.Env.Outskirts is not null)
        {
            var outskirts = loaded.World.Outskirts ?? throw new InvalidDataException("Declared outskirts data was not loaded.");
            if (outskirts.CutLine.Count < 3) throw new InvalidDataException("Outskirts cut line is empty.");
            var min = outskirts.CutLine.Aggregate(Vector2.Min);
            var max = outskirts.CutLine.Aggregate(Vector2.Max);
            descriptor = descriptor with
            {
                Bounds = new(new(min.X, descriptor.Roam.FloorY, min.Y), new(max.X, descriptor.Roam.CeilY, max.Y)),
                TileMin = TileGrid.At(min),
                TileMax = new((int)MathF.Ceiling(max.X / TileGrid.BaseEdge) - 1, (int)MathF.Ceiling(max.Y / TileGrid.BaseEdge) - 1)
            };
        }
        return new MapContext(descriptor, loaded.World, entry.DataRoot, sourceInputs);
    }
}
