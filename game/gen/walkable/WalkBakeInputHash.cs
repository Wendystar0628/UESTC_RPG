using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using Campus.Contracts;

namespace Campus.Gen.Walkable;

/// <summary>Pure hash of map source bytes and walk bake rules; never reads files.</summary>
public static class WalkBakeInputHash
{
    /// <summary>The one source selection used by offline producers and runtime freshness validation.</summary>
    public static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> SelectSourceFiles(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        var selected = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var name in SelectSourceNames(files.Keys)) selected.Add(name, files[name]);
        return new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(selected);
    }

    /// <summary>Allows a file reader to select inputs before loading their bytes; shares the same selection rules.</summary>
    public static IReadOnlyList<string> SelectSourceNames(IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var selected = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            ValidateName(name);
            if (name is "map.json" or "campus.json" or "network.json" or "zones.json" or "interiors.json" ||
                name.StartsWith("overlay/", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
                selected.Add(name);
        }
        if (!selected.Contains("map.json") || !selected.Contains("campus.json"))
            throw new ArgumentException("Walk source inputs require map.json and campus.json.", nameof(names));
        return selected.ToArray();
    }

    public static string ComputeForMap(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files) =>
        Compute(SelectSourceFiles(files), "campus-walk-rules-2\0"u8);

    /// <summary>Validate a non-empty startup product before publishing its world. Offline empty baselines skip this call.</summary>
    /// <remarks>Never rebuilds, changes support heights, or returns a fallback. Map identity and extent checks remain with the map loader.</remarks>
    public static void RequireCurrent(WalkBakeData? data, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files)
    {
        if (data is null)
            throw new WalkBakeFreshnessException(WalkBakeFreshnessFailure.InvalidData, "walk.invalid: startup walk data is null.");
        if (data.Version != WalkBakeData.CurrentVersion)
            throw new WalkBakeFreshnessException(WalkBakeFreshnessFailure.UnknownVersion,
                $"walk.version: startup walk version {data.Version} is unsupported; expected {WalkBakeData.CurrentVersion}. Rebuild the startup set offline.");
        try
        {
            data.Validate();
        }
        catch (Exception error) when (error is ArgumentException or NullReferenceException or OverflowException or IndexOutOfRangeException)
        {
            throw new WalkBakeFreshnessException(WalkBakeFreshnessFailure.InvalidData,
                "walk.invalid: startup walk data failed structural validation: " + error.Message, error);
        }
        var expected = ComputeForMap(files);
        if (!string.Equals(data.InputHash, expected, StringComparison.Ordinal))
            throw new WalkBakeFreshnessException(WalkBakeFreshnessFailure.StaleInputs,
                $"walk.stale: startup walk inputs differ (stored {data.InputHash}, current {expected}). Rebuild the startup set offline before loading this map.");
    }

    private static string Compute(IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files, ReadOnlySpan<byte> domain)
    {
        ArgumentNullException.ThrowIfNull(files);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(domain);
        Span<byte> size = stackalloc byte[8];
        foreach (var pair in files.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            ValidateName(pair.Key);
            var name = Encoding.UTF8.GetBytes(pair.Key);
            BinaryPrimitives.WriteUInt32LittleEndian(size, checked((uint)name.Length));
            hash.AppendData(size[..4]);
            hash.AppendData(name);
            BinaryPrimitives.WriteUInt64LittleEndian(size, checked((ulong)pair.Value.Length));
            hash.AppendData(size);
            hash.AppendData(pair.Value.Span);
        }
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void ValidateName(string name)
    {
        if (name.Contains('\\') || name.StartsWith('/') || name.Split('/').Any(part => part is "" or "." or ".."))
            throw new ArgumentException("Walk input names must be canonical slash-separated relative paths.", nameof(name));
    }
}
