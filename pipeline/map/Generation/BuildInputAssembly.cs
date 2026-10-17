using System.Buffers.Binary;
using System.Text;
using Campus.Contracts;

namespace Campus.Pipeline.Map;

/// <summary>Loading-boundary helpers only. Callers own asset lifetime, I/O delegates and the existing bake hash.</summary>
public static class BuildInputAssembly
{
    public static IReadOnlyList<string> RequiredGlobalKeys { get; } = Array.AsReadOnly(new[] { "vegetation/species.json" });

    /// <summary>Read an explicit global asset manifest and freeze its bytes once. Keys are relative to game/data.</summary>
    public static BuildInputSnapshot ReadRequiredGlobalInputs(IEnumerable<string> requiredGlobalKeys,
        Func<string, ReadOnlyMemory<byte>> read)
    {
        ArgumentNullException.ThrowIfNull(requiredGlobalKeys);
        ArgumentNullException.ThrowIfNull(read);
        var keys = requiredGlobalKeys.Order(StringComparer.Ordinal).ToArray();
        foreach (var key in keys) ValidateKey("global", key);
        if (keys.Distinct(StringComparer.Ordinal).Count() != keys.Length)
            throw new ArgumentException("The global build-input manifest contains duplicate keys.", nameof(requiredGlobalKeys));
        if (keys.Length == 0) return BuildInputSnapshot.Empty;
        var bytes = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            ReadOnlyMemory<byte> input;
            try { input = read(key); }
            catch (Exception error) when (error is IOException or KeyNotFoundException)
            { throw new InvalidDataException($"Missing required build input: scope=global key='{key}'.", error); }
            if (input.IsEmpty) throw new InvalidDataException($"Missing or empty required build input: scope=global key='{key}'.");
            bytes.Add(key, input);
        }
        return BuildInputSnapshot.Freeze(bytes);
    }

    /// <summary>Publish a stable read-only capability list, rejecting duplicate IDs/channel owners before any Paint call.</summary>
    public static IReadOnlyList<IGroundMaskContributor> FreezeGroundMaskContributors(IEnumerable<IGroundMaskContributor> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var items = source.ToArray();
        foreach (var item in items) ArgumentNullException.ThrowIfNull(item);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var owned = GroundMaskChannels.None;
        const GroundMaskChannels known = GroundMaskChannels.GinkgoLeaves | GroundMaskChannels.OtherLeaves | GroundMaskChannels.CanopyShelter;
        foreach (var item in items.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id))
                throw new InvalidOperationException($"Empty or duplicate ground-mask contributor ID: '{item.Id}'.");
            if (item.Channels == GroundMaskChannels.None || (item.Channels & ~known) != GroundMaskChannels.None)
                throw new InvalidOperationException($"Ground-mask contributor '{item.Id}' declares empty or unknown channels.");
            if ((owned & item.Channels) != GroundMaskChannels.None)
                throw new InvalidOperationException($"Ground-mask contributor '{item.Id}' conflicts on channels {owned & item.Channels}.");
            owned |= item.Channels;
        }
        return Array.AsReadOnly(items.OrderBy(item => item.Id, StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// Append to the caller's existing DataHash at load time, never per tile. The caller selects map files that affect generation;
    /// preserve its existing exclusions for pose/time/quality settings and its seed/DataRect/palette contributions.
    /// Each record is uint32LE scope length + UTF8 scope, uint32LE key length + UTF8 key, uint64LE byte length + raw bytes.
    /// Global and map entries are separate namespaces, sorted ordinally by scope then key. This method performs no IO or deep copy.
    /// </summary>
    public static void AppendScopedInputs(Action<ReadOnlyMemory<byte>> append, BuildInputSnapshot globalInputs,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> mapInputs)
    {
        ArgumentNullException.ThrowIfNull(append);
        ArgumentNullException.ThrowIfNull(globalInputs);
        ArgumentNullException.ThrowIfNull(mapInputs);
        append(Encoding.UTF8.GetBytes("campus-scoped-build-inputs-v1\0"));
        AppendScope("global", globalInputs.Entries);
        AppendScope("map", mapInputs);

        void AppendScope(string scope, IReadOnlyDictionary<string, ReadOnlyMemory<byte>> entries)
        {
            foreach (var pair in entries.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                ValidateKey(scope, pair.Key);
                AppendText(scope);
                AppendText(pair.Key);
                var length = new byte[sizeof(ulong)];
                BinaryPrimitives.WriteUInt64LittleEndian(length, (ulong)pair.Value.Length);
                append(length);
                append(pair.Value);
            }
        }
        void AppendText(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var length = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(length, (uint)bytes.Length);
            append(length);
            append(bytes);
        }
    }

    private static void ValidateKey(string scope, string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key != key.Trim() || key.StartsWith('/') || key.Contains('\\') || key.Contains(':') ||
            key.Split('/').Any(part => part.Length == 0 || part is "." or ".."))
            throw new ArgumentException($"Invalid build-input path: scope={scope} key='{key}'. Expected a canonical relative key.", nameof(key));
    }
}
