using Campus.Gen.World;
using Campus.Pipeline.Map;
using System.Collections.ObjectModel;
using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Maps;

/// <summary>One frozen version of the authored map inputs.</summary>
public sealed record MapLoadInput(MapCatalogEntry Entry,
    IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files)
{
    private BuildInputSnapshot _sourceInputs = BuildInputSnapshot.Freeze(Files);
    /// <summary>Map bytes freeze at construction and whenever a replacement is supplied.</summary>
    public IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files
    {
        get => _sourceInputs.Entries;
        init => _sourceInputs = BuildInputSnapshot.Freeze(value);
    }
    public BuildInputSnapshot SourceInputs => _sourceInputs;
}

public static class MapWorldLoading
{
    /// <summary>
    /// Worker-safe loading. Claims are detached on the main thread. Area ownership is resolved
    /// from the real base world zones; this method never accesses a registry or engine object.
    /// </summary>
    public static WorldModelLoadResult LoadPrepared(IWorldModelLoader loader, MapLoadInput input,
        MapClaimSnapshot? claims = null)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(input);
        var map = input.Entry.Descriptor;
        IReadOnlyDictionary<FeatureId, WpId>? claimedBy = null;
        if (claims is not null)
        {
            // Resolve AreaClaim only against parsed WorldFeature.Zone. Overlay owner/zone metadata
            // does not constitute a module claim and cannot grant a conflict exception.
            if (claims.RequiresAreaResolution)
            {
                var baselineFiles = new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(input.Files
                    .Where(pair => !pair.Key.StartsWith("overlay/", StringComparison.Ordinal))
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
                var baseline = loader.Load(map.Id, baselineFiles, new(map));
                claimedBy = claims.OverlayOwners(baseline.World);
            }
            else claimedBy = claims.OverlayOwners();
        }
        var loaded = loader.Load(map.Id, input.Files, new(map, claimedBy));
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
                TileMin = new((int)MathF.Floor(min.X / 64f), (int)MathF.Floor(min.Y / 64f)),
                TileMax = new((int)MathF.Ceiling(max.X / 64f) - 1, (int)MathF.Ceiling(max.Y / 64f) - 1)
            };
        }
        return new MapContext(descriptor, loaded.World, entry.DataRoot, sourceInputs);
    }
}
