using System.Numerics;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Kernel.Bake;

/// <summary>One input stamp for the effective world and resources read by a requested product.</summary>
internal static class BakeInputFingerprint
{
    public static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadInputs(IMapContext map)
    {
        var snapshot = map.SourceInputs;
        if (snapshot is null || snapshot.Entries.Count == 0 || !snapshot.Entries.TryGetValue("world/geo.json", out var geo) || geo.IsEmpty)
            throw new InvalidDataException($"Map {map.Descriptor.Id.Value} has no frozen world/geo.json input. Publish MapLoadInput.SourceInputs when creating IMapContext.");
        return snapshot.Entries;
    }

    public static string Identity(BakeKey key, string stamp) => $"{BakeOptions.Abi}/{key.Map.Value}/{key.Module}/{key.Detail}/{stamp}";

    public static string Compute(BuildContext context, BakeKey key, string runtimeCode, BakeSourceIndex sources, BakeInputDependencies dependencies,
        IReadOnlyDictionary<string, string>? dependencyCodes = null, MapRect? sourceArea = null, string auxiliaryStamp = "")
    {
        if (string.IsNullOrWhiteSpace(context.CodeHash) || string.IsNullOrWhiteSpace(runtimeCode))
            throw new InvalidDataException($"Missing generated code fingerprint for {key.Module}.");
        var input = new BakeInputHash();
        input.Add(new { BakeOptions.Abi, key, context.CodeHash, runtimeCode, context.WorldSeed });
        if (dependencyCodes is not null) input.Add(dependencyCodes.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray());
        var collision = key.Detail.StartsWith("C/", StringComparison.Ordinal);
        if (!collision) input.Add(sources.PaletteStamp);
        input.Add(new { context.Map.DataRect, context.Map.Bounds });
        var kinds = dependencies.Kinds;
        if ((kinds & BakeInputKinds.Backdrop) != 0)
        {
            input.Add(sources.FileStamp(context.Map.Env.Backdrop)); input.Add(context.World.Outskirts?.CutLine);
        }
        input.Add(sourceArea);
        input.Add(auxiliaryStamp);
        if ((kinds & BakeInputKinds.World) != 0)
        {
            input.Add(sources.CommonStamp);
            sources.AddArea(input, sourceArea ?? Area(context, key), (kinds & BakeInputKinds.Windows) != 0,
                (kinds & BakeInputKinds.Vegetation) != 0, (kinds & BakeInputKinds.Furniture) != 0);
        }
        if ((kinds & BakeInputKinds.ReflectLakes) != 0) input.Add(context.Map.Env.ReflectLakes);
        if ((kinds & BakeInputKinds.OutskirtsBlocks) != 0) input.Add(context.World.Outskirts?.Blocks);
        foreach (var file in dependencies.MapFiles.Order(StringComparer.Ordinal)) input.Add(sources.FileStamp(file));
        foreach (var file in dependencies.GlobalFiles.Order(StringComparer.Ordinal)) input.Add(sources.FileStamp(file, global: true));
        return input.Finish();
    }

    internal static MapRect Area(BuildContext context, BakeKey key)
    {
        if (key.Detail.StartsWith("C/", StringComparison.Ordinal) || key.Detail.StartsWith('L'))
        {
            var request = BakeRequestKeys.Parse(key);
            // L4/0/0 is explicitly the current whole-map distant product, unlike the L0-L3 grid.
            if (request.Level == LodLevel.L4) return context.Map.DataRect;
            var edge = TileGrid.Edge(request.Level);
            var min = TileGrid.Origin(request.Tile, request.Level);
            return Expand(new(min, min + new Vector2(edge)), TileGrid.BaseEdge);
        }
        return context.Map.DataRect; // Atlas and other non-cell products.
    }

    private static MapRect Expand(MapRect box, float margin) => new(box.Min - new Vector2(margin), box.Max + new Vector2(margin));
}
