using System.Buffers.Binary;
using System.Text;
using Campus.Contracts;
using Campus.Contracts.Parts;
using Campus.Kernel.Registry;

namespace Campus.Kernel.Maps;

/// <summary>Loading-boundary helpers only. Callers own asset lifetime, I/O delegates and the existing bake hash.</summary>
public static class BuildInputAssembly
{
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

    /// <summary>
    /// Capture the current map's registered part capabilities once on the composition/main thread.
    /// This creates only existing pure CPU part definitions, reads no reflected properties and runs no builders.
    /// Publish the returned immutable catalog in every BuildContext for this map; workers never use the registry.
    /// </summary>
    public static PartLodCatalog CapturePartLods(ServiceRegistry registry, MapId map)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (string.IsNullOrWhiteSpace(map.Value)) throw new ArgumentException("Map identity is required.", nameof(map));
        var definitions = new List<KeyValuePair<PartId, IPartDef>>();
        foreach (var entry in registry.ForMap(map.Value).Where(entry => entry.Kind == "part")
            .OrderBy(entry => entry.Id, StringComparer.Ordinal))
        {
            if (registry.Create(entry.Type) is not IPartDef definition)
                throw new InvalidOperationException($"Registered part '{entry.Id}' ({entry.Type.FullName}) must implement IPartDef.");
            definitions.Add(new(new PartId(entry.Id), definition));
        }
        return new PartLodCatalog(definitions);
    }

    /// <summary>Capture existing content-module capabilities after map filtering, on the composition thread.</summary>
    public static IReadOnlyList<IGroundMaskContributor> CaptureGroundMaskContributors(ServiceRegistry registry, MapId map)
    {
        ArgumentNullException.ThrowIfNull(registry);
        if (string.IsNullOrWhiteSpace(map.Value)) throw new ArgumentException("Map identity is required.", nameof(map));
        var selected = registry.ForMap(map.Value)
            .Where(entry => entry.Kind == "content" && typeof(IGroundMaskContributor).IsAssignableFrom(entry.Type))
            .OrderBy(entry => entry.Id, StringComparer.Ordinal).ToArray();
        var contributors = new List<IGroundMaskContributor>();
        foreach (var entry in selected)
        {
            if (!typeof(IContentModule).IsAssignableFrom(entry.Type))
                throw new InvalidOperationException($"Ground-mask contributor '{entry.Id}' must also implement IContentModule.");
            contributors.Add((IGroundMaskContributor)registry.Create(entry.Type));
        }
        return FreezeGroundMaskContributors(contributors);
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
