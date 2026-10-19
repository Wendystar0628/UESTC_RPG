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

    public static string Compute(BuildContext context, BakeKey key, string runtimeCode, BakeSourceIndex sources,
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
        if (key.Module == "diorama")
        {
            input.Add(sources.FileStamp(context.Map.Env.Backdrop)); input.Add(context.World.Outskirts?.CutLine); return input.Finish();
        }
        var vegetation = key.Module is BakeModuleIds.VegetationTrees or BakeModuleIds.VegetationGrass || key.Module == "vegetation" || key.Module == "collision.world" && dependencyCodes?.ContainsKey("vegetation") == true;
        var furniture = key.Module == "furniture" || key.Module == "collision.world" && dependencyCodes?.ContainsKey("furniture") == true;
        input.Add(sources.CommonStamp);
        input.Add(sourceArea);
        input.Add(auxiliaryStamp);
        sources.AddArea(input, sourceArea ?? Area(context, key), UsesWindows(key.Module) || key.Module == BakeModuleIds.NightlightWindows, vegetation, furniture);
        if (key.Module is BakeModuleIds.NightlightLamps or BakeModuleIds.NightlightWindows)
        {
            input.Add(sources.FileStamp("lighting-schedule.json"));
        }
        if (key.Module == BakeModuleIds.FaunaRuntime) input.Add(context.Map.Env.ReflectLakes);
        if (vegetation) input.Add(sources.FileStamp("vegetation/species.json", global: true));
        if (!collision && (key.Module is "furniture" or "zone.north" or "zone.center-east" or "zone.west")) input.Add(sources.FileStamp(SceneTextInputs.AtlasKey, global: true));
        if (!collision && (key.Module is "ground" or "buildings")) input.Add(sources.FileStamp(SurfaceMaterialCatalog.SourcePath));
        if (key.Module == "water") input.Add(context.Map.Env.ReflectLakes);
        if (key.Module == "map")
        {
            foreach (var file in new[] { "atlas/labels.json", "atlas/markers.json", "waypoints.json" }) input.Add(sources.FileStamp(file));
            input.Add(context.Map.Env.ReflectLakes); input.Add(context.World.Outskirts?.Blocks);
        }
        return input.Finish();
    }

    internal static MapRect Area(BuildContext context, BakeKey key)
    {
        if (key.Module.StartsWith("interior.", StringComparison.Ordinal))
        {
            var feature = new FeatureId(key.Detail.Split('/')[0]);
            if (!context.World.Features.TryGetValue(feature, out var source)) throw new InvalidDataException("Interior source feature is absent.");
            return Expand(new(new(source.Bounds.Min.X, source.Bounds.Min.Z), new(source.Bounds.Max.X, source.Bounds.Max.Z)), TileGrid.BaseEdge);
        }
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

    private static bool UsesWindows(string module) => module == "buildings" || module == "core" || module.StartsWith("zone.", StringComparison.Ordinal);

    private static MapRect Expand(MapRect box, float margin) => new(box.Min - new Vector2(margin), box.Max + new Vector2(margin));
}
