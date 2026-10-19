using System.Collections.ObjectModel;
using Campus.Contracts.Maps;

namespace Campus.Gen.World;

/// <summary>Relative map resource access supplied by the engine or an offline caller.</summary>
public interface IWorldInputSource
{
    /// <summary>Null means absent; an existing unreadable file must throw.</summary>
    ReadOnlyMemory<byte>? Read(string relativePath);
    /// <summary>Immediate names only; a missing directory has no entries.</summary>
    IEnumerable<string> Files(string relativeDirectory);
    IEnumerable<string> Directories(string relativeDirectory);
}

/// <summary>One authored input selection for production loads and offline callers; performs no filesystem IO.</summary>
public static class WorldInputFiles
{
    public static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadWorldFiles(MapDescriptor map, IWorldInputSource source)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(source);
        var files = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            ["world/geo.json"] = Required(source, "world/geo.json")
        };
        ReadJsonDirectory(source, "world/", files);
        foreach (var name in new[] { "map.json", "interiors.json", "fidelity.json" })
            AddOptional(source, name, files);
        if (map.Env.Outskirts is { } outskirts)
        {
            ValidateRelative(outskirts);
            files[outskirts] = Required(source, outskirts);
        }
        ReadJsonDirectory(source, "overlay/", files);
        return new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(files);
    }

    public static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadLoadInput(MapDescriptor map, IWorldInputSource source) =>
        ReadLoadInput(map, source, ReadWorldFiles(map, source));

    public static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadLoadInput(MapDescriptor map, IWorldInputSource source,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> worldFiles)
    {
        var files = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var pair in worldFiles) files.Add(pair.Key, pair.Value);
        ReadJsonDirectory(source, "", files);
        ValidateRelative(map.Env.Backdrop);
        if (!files.ContainsKey(map.Env.Backdrop)) AddOptional(source, map.Env.Backdrop, files);
        return new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(files);
    }

    private static void ValidateRelative(string path) => MapCatalog.ValidateRelative(path);

    private static void ReadJsonDirectory(IWorldInputSource source, string relative,
        IDictionary<string, ReadOnlyMemory<byte>> output)
    {
        foreach (var name in source.Files(relative).Order(StringComparer.Ordinal))
        {
            ValidateRelative(name);
            if (name.EndsWith(".json", StringComparison.Ordinal) && !output.ContainsKey(relative + name))
                output[relative + name] = Required(source, relative + name);
        }
        foreach (var name in source.Directories(relative).Order(StringComparer.Ordinal))
        {
            ValidateRelative(name);
            ReadJsonDirectory(source, relative + name + '/', output);
        }
    }

    private static void AddOptional(IWorldInputSource source, string name,
        IDictionary<string, ReadOnlyMemory<byte>> output)
    {
        if (source.Read(name) is { } bytes) output[name] = Nonempty(name, bytes);
    }

    private static ReadOnlyMemory<byte> Required(IWorldInputSource source, string name) =>
        source.Read(name) is { } bytes ? Nonempty(name, bytes) : throw new IOException($"Missing map resource: {name}.");

    private static ReadOnlyMemory<byte> Nonempty(string name, ReadOnlyMemory<byte> bytes) =>
        !bytes.IsEmpty ? bytes : throw new InvalidDataException($"Empty map resource: {name}.");
}
