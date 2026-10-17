using Campus.Pipeline.Map;
using System.Collections.ObjectModel;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Registry;
using Godot;

namespace Campus.Kernel.Maps;

/// <summary>All resource I/O stays on the engine side; workers receive detached byte arrays.</summary>
public class MapDataSource
{
    public virtual Task<MapCatalog> ReadCatalog()
    {
        var indexPath = ProjectSettings.GetSetting("campus/maps/index").AsString();
        var index = MapCatalog.ReadIndex(ReadRequired(indexPath));
        var bytes = index.Maps.Select(entry => (Entry: entry,
            Map: ReadRequired(entry.Dir + "map.json"), Campus: ReadRequired(entry.Dir + "campus.json"),
            Waypoints: ReadOptional(entry.Dir + "waypoints.json"))).ToArray();
        return Task.Run(() => new MapCatalog(index, bytes.Select(b =>
            MapCatalog.ReadEntry(b.Entry, b.Map, b.Campus, b.Waypoints)).ToArray()));
    }

    public virtual IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadWorldFiles(MapCatalogEntry entry)
    {
        var files = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            ["campus.json"] = ReadRequired(entry.DataRoot + "campus.json")
        };
        foreach (var name in new[] { "map.json", "network.json", "zones.json", "interiors.json", "fidelity.json" })
            AddOptional(entry.DataRoot, name, files);
        if (entry.Descriptor.Env.Outskirts is { } outskirts)
            files[outskirts] = ReadRequired(entry.DataRoot + outskirts);
        ReadJsonDirectory(entry.DataRoot, "overlay/", files);
        return new ReadOnlyDictionary<string, ReadOnlyMemory<byte>>(files);
    }

    /// <summary>Capture authored map resources on the engine thread before worker loading.</summary>
    public virtual MapLoadInput ReadLoadInput(MapCatalogEntry entry)
    {
        var worldFiles = ReadWorldFiles(entry);
        var files = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var pair in worldFiles) files.Add(pair.Key, pair.Value);
        // Capture the complete map input coverage before world parsing.
        // Global assets are captured separately. The bake hasher selects only its dependencies.
        ReadJsonDirectory(entry.DataRoot, "", files);
        // Env.Backdrop is an authored relative path, not an extension convention. Preserve already read JSON bytes.
        BackdropInputs.AddOptional(entry.Descriptor, entry.DataRoot, files,
            Godot.FileAccess.FileExists, path => ReadRequired(path));
        return new(entry, files);
    }

    public virtual MapClaimSnapshot? ReadClaims(MapId map) => MapClaimCapture.Capture(Campus.Kernel.Registry.Kernel.Services, map);

    public virtual void WriteMapRect(MapRect rect) => RenderingServer.GlobalShaderParameterSet(
        Campus.Kernel.Renderer.ShaderNames.MapRect,
        new Godot.Vector4(rect.Min.X, rect.Min.Y, 1f / (rect.Max.X - rect.Min.X), 1f / (rect.Max.Y - rect.Min.Y)));

    public virtual void ReportError(string message) => GD.PushError(message);

    private static void ReadJsonDirectory(string root, string relative, IDictionary<string, ReadOnlyMemory<byte>> output)
    {
        if (!DirAccess.DirExistsAbsolute(root + relative)) return;
        foreach (var name in DirAccess.GetFilesAt(root + relative).Order(StringComparer.Ordinal))
            if (name.EndsWith(".json", StringComparison.Ordinal) && !output.ContainsKey(relative + name))
                output[relative + name] = ReadRequired(root + relative + name);
        foreach (var name in DirAccess.GetDirectoriesAt(root + relative).Order(StringComparer.Ordinal))
        {
            MapCatalog.ValidateRelative(name);
            ReadJsonDirectory(root, relative + name + '/', output);
        }
    }

    private static void AddOptional(string root, string name, IDictionary<string, ReadOnlyMemory<byte>> output)
    {
        if (Godot.FileAccess.FileExists(root + name)) output[name] = ReadRequired(root + name);
    }

    private static byte[] ReadOptional(string path) => Godot.FileAccess.FileExists(path) ? ReadRequired(path) : [];
    private static byte[] ReadRequired(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("res://", StringComparison.Ordinal))
            throw new InvalidDataException("Map resources must use res:// paths.");
        var bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0) throw new IOException($"Missing or empty map resource: {path}.");
        return bytes;
    }
}
