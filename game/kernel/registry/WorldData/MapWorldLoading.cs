using System.Collections.ObjectModel;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Registry;

namespace Campus.Kernel.Maps;

/// <summary>One frozen map input version. Walk bytes come from BakeRoot and never masquerade as DataRoot input.</summary>
public sealed record MapLoadInput(MapCatalogEntry Entry,
    IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files, ReadOnlyMemory<byte> WalkBytes)
{
    private BuildInputSnapshot _sourceInputs = BuildInputSnapshot.Freeze(Files);
    /// <summary>The original positional constructor and deconstruction remain available; map bytes freeze at construction.</summary>
    public IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Files
    {
        get => _sourceInputs.Entries;
        init => _sourceInputs = BuildInputSnapshot.Freeze(value);
    }
    /// <summary>with { WalkBytes = ... } shares this exact snapshot; with { Files = ... } freezes only the new record's inputs.</summary>
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
        var walk = ReadWalk(input);
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
                var baseline = loader.Load(map.Id, baselineFiles, new(map, walk, null));
                claimedBy = claims.OverlayOwners(baseline.World);
            }
            else claimedBy = claims.OverlayOwners();
        }
        var loaded = loader.Load(map.Id, input.Files, new(map, walk, claimedBy));
        _ = claims?.Build(loaded.World); // Validate final per-aspect ownership before exposing the context.
        return loaded;
    }

    /// <summary>Compatibility path for old fixtures; formal loading must use the snapshot overload.</summary>
    public static IMapContext CreateContext(MapCatalogEntry entry, WorldModelLoadResult loaded) =>
        CreateContext(entry, loaded, BuildInputSnapshot.Empty);

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
        return new MapContext(descriptor, loaded.World, entry.DataRoot, entry.BakeRoot) { SourceInputs = sourceInputs };
    }

    public static WalkBakeData? ReadWalk(MapLoadInput input)
    {
        if (input.WalkBytes.IsEmpty) return null;
        var walk = JsonSerializer.Deserialize<WalkBakeData>(input.WalkBytes.Span, ContractJson.Options)
            ?? throw new InvalidDataException("Resident walk data is null.");
        walk.Validate();
        var rect = input.Entry.Descriptor.DataRect;
        var max = walk.Walk.Origin + new Vector2(walk.Walk.Width * WalkGrid.CellSize, walk.Walk.Height * WalkGrid.CellSize);
        if (walk.Map != input.Entry.Descriptor.Id || walk.Walk.Origin != rect.Min || max != rect.Max)
            throw new InvalidDataException("Resident walk data does not match the requested map and data rectangle.");
        return walk;
    }
}
