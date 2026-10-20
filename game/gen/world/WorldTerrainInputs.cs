using System.Collections.ObjectModel;
using System.Text.Json;
using Campus.Contracts;

namespace Campus.Gen.World;

internal static class WorldTerrainInputs
{
    internal static IReadOnlyList<TerrainSource> ReadSources(MapId map, SortedDictionary<string, string> input)
    {
        var sources = new List<TerrainSource>();
        foreach (var (file, text) in input.Where(pair => pair.Key.StartsWith("world/", StringComparison.Ordinal)
            && pair.Key.EndsWith(".terrain.json", StringComparison.Ordinal) && pair.Key.Split('/').Length == 2))
        {
            TerrainOverlayFile overlay;
            try { overlay = TerrainOverlayJson.Parse(WorldModelLoader.Utf8.GetBytes(text)); }
            catch (JsonException error)
            { throw new WorldDataException(file, TerrainErrorEntry(text, error), error.Message, error); }
            WorldModelLoader.CheckMap(map, overlay.Map, file);
            sources.Add(new(file, overlay));
        }
        return sources.AsReadOnly();
    }

    private static string TerrainErrorEntry(string text, JsonException error)
    {
        // Decode an identity only on a failed strict parse; the evaluator never consumes this projection.
        try
        {
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("ops", out var ops) && ops.ValueKind == JsonValueKind.Array)
            {
                var path = error.Path ?? "";
                var start = path.IndexOf(".ops[", StringComparison.Ordinal);
                var end = start < 0 ? -1 : path.IndexOf(']', start);
                if (end > start && int.TryParse(path.AsSpan(start + 5, end - start - 5), out var index)
                    && index >= 0 && index < ops.GetArrayLength() && ops[index].ValueKind == JsonValueKind.Object
                    && ops[index].TryGetProperty("id", out var identity) && identity.ValueKind == JsonValueKind.String)
                    return identity.GetString()!;
                foreach (var op in ops.EnumerateArray())
                    if (op.ValueKind == JsonValueKind.Object && op.TryGetProperty("id", out var id)
                        && id.ValueKind == JsonValueKind.String && id.GetString() is { Length: > 0 } value
                        && error.Message.Contains(value, StringComparison.Ordinal)) return value;
            }
        }
        catch (JsonException) { }
        return error.Path ?? "root";
    }

    internal static TerrainCompileContext? Prepare(WorldModelLoadInputs? inputs, IReadOnlyList<TerrainSource> sources,
        PolygonRings boundary, IReadOnlyDictionary<FeatureId, WorldFeature> features, WorldNetwork network, string? prefix)
    {
        if (inputs is null)
        {
            if (sources.Count != 0)
                throw new WorldDataException(sources[0].File, sources[0].Data.Ops.FirstOrDefault()?.Id ?? "root",
                    "Terrain evaluation requires explicit WorldModelLoadInputs with this map's descriptor.");
            return null;
        }
        TerrainAutoSettings? automatic = null;
        if (inputs.Descriptor.TerrainDefaults is { } defaults)
        {
            try
            {
                TerrainOverlayJson.ValidateTerrainDefaults(defaults);
                automatic = new(TerrainReferences.Grade(defaults.LakeBankSlope), defaults.LakeMaxDepth,
                    TerrainReferences.Grade(defaults.ChannelBankSlope), defaults.ChannelDepth, defaults.CorridorShoulder);
            }
            catch (ArgumentException error)
            { throw new WorldDataException("map.json", "terrainDefaults", error.Message, error); }
        }
        return new TerrainCompileContext(inputs.Descriptor.Id, inputs.Descriptor.DataRect, boundary,
            features, network, prefix ?? "", automatic, inputs.ZoneBoundaries);
    }

    internal static IReadOnlyDictionary<string, TerrainOp> Operations(IReadOnlyList<TerrainSource> sources)
    {
        var operations = new SortedDictionary<string, TerrainOp>(StringComparer.Ordinal);
        foreach (var source in sources)
            foreach (var op in source.Data.Ops)
                if (!operations.TryAdd(op.Id, op)) throw new WorldDataException(source.File, op.Id, "Duplicate terrain operation ID.");
        return new ReadOnlyDictionary<string, TerrainOp>(operations);
    }

    internal static void ValidateReferences(IReadOnlyList<TerrainSource> sources,
        IReadOnlySet<FeatureId> features, IReadOnlySet<FeatureId> roads)
    {
        foreach (var source in sources)
            foreach (var op in source.Data.Ops)
            {
                switch (op)
                {
                    case TerrainPadOp pad:
                        Reference("feature", pad.Feature);
                        if (pad.Edge is { } edge) Reference("edge.structureId", new(edge.StructureId));
                        break;
                    case TerrainLakeOp lake: Reference("water", lake.Water); break;
                    case TerrainCorridorOp corridor:
                        Reference("road", corridor.Road, roads);
                        for (var index = 0; index < corridor.Steps.Count; index++)
                            Reference($"steps[{index}].structure", corridor.Steps[index].Structure);
                        break;
                    case TerrainChannelOp channel:
                        if (channel.Water is { } water) Reference("water", water);
                        for (var index = 0; index < channel.Weirs.Count; index++)
                        {
                            var weir = channel.Weirs[index];
                            Reference($"weirs[{index}].structure", weir.Structure);
                            Reference($"weirs[{index}].upstream", weir.Upstream);
                            Reference($"weirs[{index}].downstream", weir.Downstream);
                        }
                        for (var index = 0; index < channel.Crossings.Count; index++)
                            Reference($"crossings[{index}].road", channel.Crossings[index].Road, roads);
                        break;
                }
                void Reference(string field, FeatureId target, IReadOnlySet<FeatureId>? targets = null) =>
                    WorldModelLoader.RequireReference(source.File, op.Id, field, target, targets ?? features);
            }
    }
}
