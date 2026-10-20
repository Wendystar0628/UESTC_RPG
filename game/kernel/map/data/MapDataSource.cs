using Campus.Kernel.Diagnostics;
using Campus.Gen.BakeInput;
using Campus.Gen.World;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Kernel.Registry;
using Godot;

namespace Campus.Kernel.Maps;

/// <summary>All resource I/O stays on the engine side; workers receive detached byte arrays.</summary>
public class MapDataSource
{
    private static readonly StringName _shaderGlobalMapRect = ShaderGlobals.MapRect;

    public virtual Task<MapCatalog> ReadCatalog()
    {
        var indexPath = ProjectSettings.GetSetting("campus/maps/index").AsString();
        var index = MapCatalog.ReadIndex(ReadRequired(indexPath));
        var bytes = index.Maps.Select(entry => (Entry: entry,
            Map: ReadRequired(entry.Dir + "map.json"), Geo: ReadRequired(entry.Dir + "world/geo.json"),
            Waypoints: ReadOptional(entry.Dir + "waypoints.json"))).ToArray();
        return Task.Run(() => new MapCatalog(index, bytes.Select(b =>
            MapCatalog.ReadEntry(b.Entry, b.Map, b.Geo, b.Waypoints)).ToArray()));
    }

    public virtual IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadWorldFiles(MapCatalogEntry entry) =>
        WorldInputFiles.ReadWorldFiles(entry.Descriptor, new ResourceInputs(entry.DataRoot));

    /// <summary>Capture authored map resources on the engine thread before worker loading.</summary>
    public virtual MapLoadInput ReadLoadInput(MapCatalogEntry entry)
    {
        var files = WorldInputFiles.ReadLoadInput(entry.Descriptor, new ResourceInputs(entry.DataRoot), ReadWorldFiles(entry));
        var input = new MapLoadInput(entry, BuildInputSnapshot.Freeze(files,
            CaptureInputVersion(files.Keys.Select(key => entry.DataRoot + key))));
        return input with { Entry = MapCatalog.ReadEntry(entry, input.Files) };
    }

    // Capture file revisions once at the IO boundary, never on a tile worker.
    internal static string CaptureInputVersion(IEnumerable<string> paths)
    {
        if (!string.Equals(BuildInfo.Kind, "dev", StringComparison.Ordinal)) return "release/" + BuildInfo.Version;
        return "dev\n" + string.Join("\n", paths.Order(StringComparer.Ordinal).Select(path =>
        {
            var file = new FileInfo(ProjectSettings.GlobalizePath(path));
            return path + "|" + file.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" +
                file.LastWriteTimeUtc.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }));
    }

    public virtual MapClaimSnapshot? ReadClaims(MapId map) => MapClaimCapture.Capture(Campus.Kernel.Registry.Kernel.Services, map);

    public virtual void WriteMapRect(MapRect rect) => RenderingServer.GlobalShaderParameterSet(
        _shaderGlobalMapRect,
        new Godot.Vector4(rect.Min.X, rect.Min.Y, 1f / (rect.Max.X - rect.Min.X), 1f / (rect.Max.Y - rect.Min.Y)));

    public virtual void ReportError(string context, Exception error) => DiagnosticLog.Error(context, error);

    private sealed class ResourceInputs(string root) : IWorldInputSource
    {
        public ReadOnlyMemory<byte>? Read(string relativePath)
        {
            if (!Godot.FileAccess.FileExists(root + relativePath)) return null;
            return ReadRequired(root + relativePath);
        }
        public IEnumerable<string> Files(string relativeDirectory) =>
            DirAccess.DirExistsAbsolute(root + relativeDirectory) ? DirAccess.GetFilesAt(root + relativeDirectory) : [];
        public IEnumerable<string> Directories(string relativeDirectory) =>
            DirAccess.DirExistsAbsolute(root + relativeDirectory) ? DirAccess.GetDirectoriesAt(root + relativeDirectory) : [];
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
