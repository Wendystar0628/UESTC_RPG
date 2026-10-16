using System.Globalization;
using System.Text.Json;

namespace Campus.Kernel.Bake;

/// <summary>Reference hashes are loaded only from the trusted boot pack. Local cache.index is never a release trust root.</summary>
public sealed class BakeManifest
{
    private readonly SortedDictionary<string, string> _hashes = new(StringComparer.Ordinal);
    // Bounded JSON reader budget, separate from the map's total boot-pack budget.
    public const int MaximumBytes = 8 * 1024 * 1024;
    public int Count => _hashes.Count;
    public string? Find(string identity) => _hashes.GetValueOrDefault(identity);
    public void Add(string identity, byte[] payload) => _hashes[identity] = BakeCacheStore.Hash(payload);

    public static BakeManifest Parse(byte[] bytes)
    {
        if (bytes.Length is <= 0 or > MaximumBytes) throw SizeFailure(bytes.Length, bytes.Length == 0 ? "0" : "unread (size guard)");
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (root.GetProperty("schema").GetInt32() != 1 || root.GetProperty("abi").GetString() != BakeOptions.Abi)
            throw new InvalidDataException("Bake manifest ABI/schema mismatch.");
        var result = new BakeManifest();
        foreach (var property in root.GetProperty("hashes").EnumerateObject())
        {
            var hash = property.Value.GetString();
            if (property.Name.Length > 2048 || hash is null || hash.Length != 32 || !hash.All(char.IsAsciiHexDigit) ||
                !result._hashes.TryAdd(property.Name, hash)) throw new InvalidDataException("Invalid or duplicate bake manifest entry.");
        }
        return result;
    }

    public byte[] Encode()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schema", 1);
            writer.WriteString("abi", BakeOptions.Abi);
            writer.WriteStartObject("hashes");
            foreach (var (key, value) in _hashes) writer.WriteString(key, value);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        if (stream.Length > MaximumBytes) throw SizeFailure(stream.Length, Count.ToString(CultureInfo.InvariantCulture));
        return stream.ToArray();
    }

    private static InvalidDataException SizeFailure(long bytes, string count) => new(FormattableString.Invariant(
        $"Bake manifest size is outside its bounded JSON budget: bytes={bytes}; count={count}; maximumBytes={MaximumBytes}."));

    public void Verify(string identity, byte[] bytes, bool allowUnlisted)
    {
        var expected = Find(identity);
        if (expected is null && allowUnlisted) return;
        var actual = BakeCacheStore.Hash(bytes);
        if (expected is null || !string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Reference hash mismatch: {identity}; expected={expected ?? "unlisted"}; actual={actual}");
    }
}
