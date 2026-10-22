using Campus.Kernel.Diagnostics;
using Campus.Gen.BakeInput;
using Campus.Gen.World;
using Campus.Contracts;
using Campus.Contracts.AppFlow;
using System.Text.Json;
using Campus.Contracts.Maps;
using Campus.Kernel.Registry;
using Godot;

namespace Campus.Kernel.Maps;

/// <summary>Captures engine paths on the main thread; loose map inputs are read and frozen by a worker.</summary>
[KernelService(typeof(MapDataSource))]
public class MapDataSource(IRequiredResources resources)
{
    private static readonly StringName _shaderGlobalMapRect = ShaderGlobals.MapRect;

    public virtual async Task<MapCatalog> ReadCatalog()
    {
        try
        {
            var indexPath = ProjectSettings.GetSetting("campus/maps/index").AsString();
            var index = MapCatalog.ReadIndex(ReadRequired(resources, indexPath));
            var bytes = index.Maps.Select(entry => (Entry: entry,
                Map: ReadRequired(resources, entry.Dir + "map.json"), Geo: ReadRequired(resources, entry.Dir + "world/geo.json"),
                Waypoints: ReadOptional(resources, entry.Dir + "waypoints.json"))).ToArray();
            return await Task.Run(() => new MapCatalog(index, bytes.Select(b =>
                MapCatalog.ReadEntry(b.Entry, b.Map, b.Geo, b.Waypoints)).ToArray()));
        }
        catch (Exception error) when (error is InvalidDataException or JsonException)
        { throw new AppStartupException("Map catalogue data is invalid.", error); }
    }

    /// <summary>
    /// Capture the project root on the main thread; read, parse and freeze inputs using only .NET on a worker.
    /// </summary>
    public virtual Task<MapLoadInput> ReadLoadInputAsync(MapCatalogEntry entry)
    {
        var root = ProjectSettings.GlobalizePath(entry.DataRoot);
        return Task.Run(() => ReadLooseLoadInput(entry, root));
    }

    private static MapLoadInput ReadLooseLoadInput(MapCatalogEntry entry, string root)
    {
        try
        {
            var source = new LooseInputs(entry.DataRoot, root);
            var files = WorldInputFiles.ReadLoadInput(entry.Descriptor, source);
            var version = CaptureInputVersion(files.Keys.Select(key => entry.DataRoot + key), source.Globalize);
            return AssembleLoadInput(entry, files, version);
        }
        catch (Exception error) when (error is InvalidDataException or JsonException)
        { throw new AppStartupException("Map input data is invalid.", error); }
    }

    private static MapLoadInput AssembleLoadInput(MapCatalogEntry entry,
        IReadOnlyDictionary<string, ReadOnlyMemory<byte>> files, string version)
    {
        try
        {
            var input = new MapLoadInput(entry, BuildInputSnapshot.Freeze(files, version));
            return input with { Entry = MapCatalog.ReadEntry(entry, input.Files) };
        }
        catch (Exception error) when (error is InvalidDataException or JsonException)
        { throw new AppStartupException("Map input data is invalid.", error); }
    }

    // Capture revisions once at the IO boundary. Only global asset capture resolves Godot paths here.
    internal static string CaptureInputVersion(IEnumerable<string> paths) => CaptureInputVersion(paths, ProjectSettings.GlobalizePath);

    private static string CaptureInputVersion(IEnumerable<string> paths, Func<string, string> globalize)
    {
        return string.Join("\n", paths.Order(StringComparer.Ordinal).Select(path =>
        {
            var file = new FileInfo(globalize(path));
            return path + "|" + file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" +
                file.LastWriteTimeUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }));
    }

    public virtual MapClaimSnapshot? ReadClaims(MapId map) => MapClaimCapture.Capture(Campus.Kernel.Registry.Kernel.Services, map);

    public virtual void WriteMapRect(MapRect rect) => RenderingServer.GlobalShaderParameterSet(
        _shaderGlobalMapRect,
        new Godot.Vector4(rect.Min.X, rect.Min.Y, 1f / (rect.Max.X - rect.Min.X), 1f / (rect.Max.Y - rect.Min.Y)));

    public virtual void ReportError(string context, Exception error) => DiagnosticLog.Error(context, error);

    private sealed class LooseInputs(string resourceRoot, string root) : IWorldInputSource
    {
        // Match DirAccess's default directory selection; hidden authoring files are not map inputs.
        private static readonly EnumerationOptions VisibleEntries = new()
        { AttributesToSkip = FileAttributes.Hidden, IgnoreInaccessible = false };

        public string Globalize(string resourcePath) => Path.Combine(root,
            resourcePath[resourceRoot.Length..].Replace('/', Path.DirectorySeparatorChar));

        public ReadOnlyMemory<byte>? Read(string relativePath)
        {
            var resourcePath = resourceRoot + relativePath;
            var path = Globalize(resourcePath);
            if (!File.Exists(path)) return null;
            try
            {
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length == 0) throw new InvalidDataException($"Required resource is empty: {resourcePath}.");
                return bytes;
            }
            catch (Exception error)
            {
                throw new AppStartupException($"Required installation resource could not be read: {resourcePath}.", error);
            }
        }

        public IEnumerable<string> Files(string relativeDirectory)
        {
            var path = Globalize(resourceRoot + relativeDirectory);
            return Directory.Exists(path) ? Directory.EnumerateFiles(path, "*", VisibleEntries)
                .Select(file => Path.GetFileName(file)!) : [];
        }

        public IEnumerable<string> Directories(string relativeDirectory)
        {
            var path = Globalize(resourceRoot + relativeDirectory);
            return Directory.Exists(path) ? Directory.EnumerateDirectories(path, "*", VisibleEntries)
                .Select(directory => Path.GetFileName(directory)!) : [];
        }
    }

    private static byte[] ReadOptional(IRequiredResources resources, string path) => Godot.FileAccess.FileExists(path) ? ReadRequired(resources, path) : [];
    private static byte[] ReadRequired(IRequiredResources resources, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("res://", StringComparison.Ordinal))
            throw new AppStartupException("Map resources must use res:// paths.");
        return resources.ReadBytes(path);
    }
}
