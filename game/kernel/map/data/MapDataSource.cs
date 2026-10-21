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

/// <summary>All resource I/O stays on the engine side; workers receive detached byte arrays.</summary>
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
        { throw new AppStartupException(AppStartupFailureCodes.DataInvalid, "Map catalogue data is invalid.", error); }
    }

    public virtual IReadOnlyDictionary<string, ReadOnlyMemory<byte>> ReadWorldFiles(MapCatalogEntry entry) =>
        WorldInputFiles.ReadWorldFiles(entry.Descriptor, new ResourceInputs(entry.DataRoot, resources));

    /// <summary>Capture authored map resources on the engine thread before worker loading.</summary>
    public virtual MapLoadInput ReadLoadInput(MapCatalogEntry entry)
    {
        try
        {
            var files = WorldInputFiles.ReadLoadInput(entry.Descriptor, new ResourceInputs(entry.DataRoot, resources), ReadWorldFiles(entry));
            var input = new MapLoadInput(entry, BuildInputSnapshot.Freeze(files,
                CaptureInputVersion(files.Keys.Select(key => entry.DataRoot + key))));
            return input with { Entry = MapCatalog.ReadEntry(entry, input.Files) };
        }
        catch (Exception error) when (error is InvalidDataException or JsonException)
        { throw new AppStartupException(AppStartupFailureCodes.DataInvalid, "Map input data is invalid.", error); }
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

    private sealed class ResourceInputs(string root, IRequiredResources resources) : IWorldInputSource
    {
        public ReadOnlyMemory<byte>? Read(string relativePath)
        {
            if (!Godot.FileAccess.FileExists(root + relativePath)) return null;
            return ReadRequired(resources, root + relativePath);
        }
        public IEnumerable<string> Files(string relativeDirectory) =>
            DirAccess.DirExistsAbsolute(root + relativeDirectory) ? DirAccess.GetFilesAt(root + relativeDirectory) : [];
        public IEnumerable<string> Directories(string relativeDirectory) =>
            DirAccess.DirExistsAbsolute(root + relativeDirectory) ? DirAccess.GetDirectoriesAt(root + relativeDirectory) : [];
    }

    private static byte[] ReadOptional(IRequiredResources resources, string path) => Godot.FileAccess.FileExists(path) ? ReadRequired(resources, path) : [];
    private static byte[] ReadRequired(IRequiredResources resources, string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("res://", StringComparison.Ordinal))
            throw new AppStartupException(AppStartupFailureCodes.DataInvalid, "Map resources must use res:// paths.");
        return resources.ReadBytes(path);
    }
}
