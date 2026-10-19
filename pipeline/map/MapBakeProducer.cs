using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;

namespace Campus.Pipeline.Map;

/// <summary>Discovered by the kernel. SourceInputs are an immutable snapshot loaded by the caller.</summary>
[BakeProducer("map", Owner = "WP-48a")]
public sealed class MapBakeProducer : IBakeProducer
{
    public IReadOnlyList<BakeTileEntry> Tiles(BuildContext context) => [];

    public bool CanBuild(BakeKey key) => key.Module == MapBaker.Module && key.Detail == MapBaker.Detail;

    public object Build(BakeKey key, BuildContext context)
    {
        if (!CanBuild(key) || key.Map != context.Map.Id) throw new ArgumentException("Map bake key does not match its context.");
        var labels = AtlasLabelData.Parse(Required(context, "atlas/labels.json").Span);
        using var markers = JsonDocument.Parse(Required(context, "atlas/markers.json"));
        if (!markers.RootElement.TryGetProperty("atlasRect", out var rectJson)) throw new InvalidDataException("Atlas page rectangle is missing.");
        var atlasRect = rectJson.Deserialize<MapRect>(AtlasLabelData.JsonOptions)
            ?? throw new InvalidDataException("Invalid atlas page rectangle.");
        var input = new MapBakeInput(context.World, context.Map, atlasRect, labels, context.DataHash, context.CodeHash, LabelHash(context));
        // No authoritative region polygons exist in the current world contract/source schema.
        // Leave RegionAreas empty: unknown is preferable to invented regional boundaries.
        return MapBaker.Build(input);
    }

    private static ReadOnlyMemory<byte> Required(BuildContext context, string path) =>
        context.SourceInputs.TryGetValue(path, out var bytes) && !bytes.IsEmpty
            ? bytes : throw new InvalidDataException($"Map bake input {path} is missing.");

    private static string LabelHash(BuildContext context)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string path in new[] { "atlas/labels.json", "atlas/markers.json", "waypoints.json" })
        {
            hash.AppendData(Encoding.UTF8.GetBytes(path));
            hash.AppendData([0]);
            if (context.SourceInputs.TryGetValue(path, out var bytes)) hash.AppendData(bytes.Span);
            hash.AppendData([0]);
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
