using System.Collections.ObjectModel;

namespace Campus.Contracts;

/// <summary>Frozen logical data inputs. Create once while loading an asset catalog/map, then share the same instance across all tile contexts.</summary>
/// <remarks>The loader captures Version once; this value performs no IO or input hashing.</remarks>
public sealed class BuildInputSnapshot
{
    public static BuildInputSnapshot Empty { get; } = Freeze(new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal), "empty");
    private BuildInputSnapshot(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> entries, string version)
    { Entries = entries; Version = version; }
    public IReadOnlyDictionary<string, ReadOnlyMemory<byte>> Entries { get; }
    public string Version { get; }

    public static BuildInputSnapshot Freeze(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> source, string version)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        var entries = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var pair in source)
        {
            ValidateKey(pair.Key);
            entries.Add(pair.Key, pair.Value.ToArray());
        }
        return new(new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(entries), version);
    }

    public ReadOnlyMemory<byte> Require(string key)
    {
        ValidateKey(key);
        return Entries.TryGetValue(key, out var bytes) ? bytes : throw new KeyNotFoundException($"Missing preloaded build input: {key}");
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key != key.Trim() || key.StartsWith('/') || key.Contains('\\') || key.Contains(':') ||
            key.Split('/').Any(part => part.Length == 0 || part is "." or ".."))
            throw new ArgumentException("Build input keys are canonical forward-slash relative paths, not resource or filesystem paths.", nameof(key));
    }
}
