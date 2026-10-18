using System.Text;
using Campus.Contracts;
using Campus.Gen.World;

namespace Campus.Tests;

/// <summary>Real authored maps through the production catalogue parser and input capture.</summary>
internal static class RealMapFixture
{
    public static MapCatalogEntry Entry(MapId? map = null)
    {
        var index = MapCatalog.ReadIndex(File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "maps", "index.json")));
        var id = map ?? index.DefaultMap;
        var entry = index.Maps.SingleOrDefault(entry => entry.Id == id)
            ?? throw new ArgumentException("The requested map is not in the authored catalogue.", nameof(map));
        var source = new DiskWorldInputSource(ResourceDirectory(entry.Dir));
        return MapCatalog.ReadEntry(entry, Required(source, "map.json").Span,
            Required(source, "campus.json"), (source.Read("waypoints.json") ?? ReadOnlyMemory<byte>.Empty).Span);
    }

    public static string DataDirectory(MapId? map = null) => ResourceDirectory(Entry(map).DataRoot);

    public static IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadFiles(MapId? map = null)
    {
        var entry = Entry(map);
        return WorldInputFiles.ReadLoadInput(entry.Descriptor, new DiskWorldInputSource(ResourceDirectory(entry.DataRoot)));
    }

    /// <summary>Mutable text inputs for tests that intentionally edit the captured production data.</summary>
    public static Dictionary<string, string> ReadTextFiles(MapId? map = null) =>
        ReadFiles(map).ToDictionary(pair => pair.Key, pair => Encoding.UTF8.GetString(pair.Value.Span), StringComparer.Ordinal);

    public static WorldModelLoadResult Load(MapId? map = null, WorldLoadOptions? options = null)
    {
        var entry = Entry(map);
        var files = WorldInputFiles.ReadLoadInput(entry.Descriptor, new DiskWorldInputSource(ResourceDirectory(entry.DataRoot)));
        return WorldModelLoader.Load(entry.Descriptor.Id, files, options);
    }

    private static string ResourceDirectory(string resource)
    {
        MapCatalog.ValidateRoot(resource);
        return RepositoryFixture.FilePath("game", resource["res://".Length..].Replace('/', Path.DirectorySeparatorChar));
    }

    private static ReadOnlyMemory<byte> Required(DiskWorldInputSource source, string path) =>
        source.Read(path) is { IsEmpty: false } bytes ? bytes : throw new IOException($"Missing or empty map resource: {path}.");

    private sealed class DiskWorldInputSource(string directory) : IWorldInputSource
    {
        public ReadOnlyMemory<byte>? Read(string relativePath)
        {
            try { return File.ReadAllBytes(Path.Combine(directory, relativePath)); }
            catch (FileNotFoundException) { return null; }
            catch (DirectoryNotFoundException) { return null; }
        }

        public IEnumerable<string> Files(string relativeDirectory)
        {
            var path = Path.Combine(directory, relativeDirectory);
            return Directory.Exists(path) ? Directory.EnumerateFiles(path).Select(file => Path.GetFileName(file)!) : [];
        }

        public IEnumerable<string> Directories(string relativeDirectory)
        {
            var path = Path.Combine(directory, relativeDirectory);
            return Directory.Exists(path) ? Directory.EnumerateDirectories(path).Select(child => Path.GetFileName(child)!) : [];
        }
    }
}
