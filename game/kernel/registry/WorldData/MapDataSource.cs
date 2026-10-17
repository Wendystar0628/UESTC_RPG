using System.Collections.ObjectModel;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Walkable;
using Campus.Kernel.Registry;
using Campus.Pipeline.Walk;
using Godot;

namespace Campus.Kernel.Maps;

/// <summary>All resource I/O stays on the engine side; workers receive detached byte arrays.</summary>
public class MapDataSource
{
    private readonly HashSet<MapId> _mountedPacks = [];

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
        var pack = "res://boot_" + entry.Descriptor.Id.Value + ".pck";
        if (!_mountedPacks.Contains(entry.Descriptor.Id) && Godot.FileAccess.FileExists(pack))
        {
            if (!ProjectSettings.LoadResourcePack(pack, false)) throw new IOException($"Cannot mount startup pack: {pack}.");
            _mountedPacks.Add(entry.Descriptor.Id);
        }
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

    /// <summary>Capture map resources and development bake metadata on the engine thread before worker preparation.</summary>
    public virtual MapLoadInput ReadLoadInput(MapCatalogEntry entry)
    {
        var worldFiles = ReadWorldFiles(entry); // Mount the startup pack before reading its resident walk data.
        var files = new SortedDictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var pair in worldFiles) files.Add(pair.Key, pair.Value);
        // Capture the complete map input coverage before world parsing.
        // Global assets and BakeRoot products are separate scopes. The bake hasher still selects only its dependencies.
        ReadJsonDirectory(entry.DataRoot, "", files);
        // Env.Backdrop is an authored relative path, not an extension convention. Preserve already read JSON bytes.
        BackdropInputs.AddOptional(entry.Descriptor, entry.DataRoot, files,
            Godot.FileAccess.FileExists, path => ReadRequired(path));
        var input = new MapLoadInput(entry, files, ReadOptional(entry.BakeRoot + "walk.bake.json"));
        return OS.IsDebugBuild() ? input with
        {
            DevelopmentWalkPath = ProjectSettings.GlobalizePath(entry.BakeRoot + "walk.bake.json"),
            DevelopmentWalkCatalog = Campus.Kernel.Registry.Kernel.Services.Get<PaletteCatalog>()
        } : input;
    }

    public virtual MapClaimSnapshot? ReadClaims(MapId map) => MapClaimLedger.Capture(Campus.Kernel.Registry.Kernel.Services, map);

    /// <summary>Prepare the frozen startup input on workers; exported games require their shipped startup product.</summary>
    public virtual async Task<MapLoadInput> PrepareLoadInput(MapLoadInput input)
    {
        var destination = input.DevelopmentWalkPath;
        var catalog = input.DevelopmentWalkCatalog;
        var development = destination is not null;
        var rebuild = await Task.Run(() =>
        {
            if (input.WalkBytes.IsEmpty)
            {
                if (!development)
                    throw new InvalidDataException("Required resident walk data is missing: " + input.Entry.BakeRoot + "walk.bake.json");
                return true;
            }
            try { WalkBakeInputHash.RequireCurrent(MapWorldLoading.ReadWalk(input), input.Files); }
            catch (WalkBakeFreshnessException error) when (development && error.Failure == WalkBakeFreshnessFailure.StaleInputs)
            { return true; }
            return false;
        });
        if (!rebuild) return input;
        if (catalog is null) throw new InvalidOperationException("Development walk preparation requires the map palette catalog.");
        var prepared = await Task.Run(() =>
        {
            var product = WalkBakeGenerator.Build(input.Files, catalog);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(product, ContractJson.Options);
            var result = input with { WalkBytes = bytes };
            WalkBakeInputHash.RequireCurrent(MapWorldLoading.ReadWalk(result), result.Files);
            Directory.CreateDirectory(Path.GetDirectoryName(destination!)!);
            File.WriteAllBytes(destination!, bytes);
            return result;
        });
        Console.WriteLine($"[walk][map={input.Entry.Descriptor.Id.Value}] prepared development startup product");
        return prepared;
    }

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
