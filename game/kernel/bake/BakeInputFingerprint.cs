using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Bake;
using Campus.Contracts.Maps;
using Campus.Kernel.Maps;

namespace Campus.Kernel.Bake;

/// <summary>Frozen generation inputs. Global asset edits conservatively invalidate every consuming map; presentation settings are excluded.</summary>
internal static class BakeInputFingerprint
{
    private static readonly string[] Files =
        ["campus.json", "network.json", "zones.json", "interiors.json", "fidelity.json"];

    public static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadInputs(IMapContext map)
    {
        var snapshot = map.SourceInputs;
        if (snapshot is null || snapshot.Entries.Count == 0 || !snapshot.Entries.TryGetValue("campus.json", out var campus) || campus.IsEmpty)
            throw new InvalidDataException($"Map {map.Descriptor.Id.Value} has no frozen campus.json input. Publish the MapLoadInput.SourceInputs used for world loading when creating IMapContext; the baker cannot reread DataRoot.");
        return snapshot.Entries;
    }

    public static string Compute(IMapContext map, uint seed, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> inputs,
        PaletteCatalog catalog, BuildInputSnapshot globalInputs, bool includeAtlas = false)
    {
        var hash = new XxHash128();
        var selected = inputs.Where(pair => Files.Contains(pair.Key, StringComparer.Ordinal) ||
            pair.Key == map.Descriptor.Env.Outskirts || pair.Key == map.Descriptor.Env.Backdrop ||
            pair.Key.StartsWith("overlay/", StringComparison.Ordinal) || pair.Key == "vegetation/planting.json" ||
            (includeAtlas && pair.Key is ("atlas/labels.json" or "atlas/markers.json" or "waypoints.json")))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        // Borrow raw memories. Scope/key framing prevents global species from aliasing a map-relative key.
        BuildInputAssembly.AppendScopedInputs(bytes => hash.Append(bytes.Span), globalInputs, selected);
        // TerrainDefaults affects generated elevations. The rest of map.json includes presentation state and is excluded.
        Append(hash, JsonSerializer.SerializeToUtf8Bytes(new
        {
            seed,
            map.Descriptor.DataRect,
            map.Descriptor.TerrainDefaults,
            map.Descriptor.Env.Backdrop,
            map.Descriptor.Env.Outskirts
        }, ContractJson.Options));
        Append(hash, System.Text.Encoding.UTF8.GetBytes("Campus.Bake.PaletteCatalog/v2"));
        Append(hash, JsonSerializer.SerializeToUtf8Bytes(new
        {
            encoding = PaletteLayout.EncodingVersion,
            layout = PaletteLayout.LayoutId,
            entries = catalog.Entries.OrderBy(entry => entry.Index.Value).ToArray()
        }, ContractJson.Options));
        return Convert.ToHexStringLower(hash.GetCurrentHash());
    }

    public static string Identity(BakeKey key, string dataHash, string codeHash)
    {
        if (string.IsNullOrWhiteSpace(codeHash)) throw new InvalidDataException($"Missing generated code hash for {key.Module}.");
        if ((key.Detail.Length > 2 && key.Detail[0] == 'L' && key.Detail[1] is >= '0' and <= '4' && key.Detail[2] == '/') ||
            key.Detail.StartsWith("C/", StringComparison.Ordinal))
        {
            var parsed = BakeRequestKeys.Parse(key);
            return $"{BakeOptions.Abi}/{key.Map.Value}/{dataHash}/{key.Module}@{codeHash}/{(parsed.Collision ? "C" : "L" + (int)parsed.Level)}/{BakeRequestKeys.TileId(key)}";
        }
        return $"{BakeOptions.Abi}/{key.Map.Value}/{dataHash}/{key.Module}@{codeHash}/{key.Detail}";
    }

    private static void Append(XxHash128 hash, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(length, data.Length);
        hash.Append(length);
        hash.Append(data);
    }
}
