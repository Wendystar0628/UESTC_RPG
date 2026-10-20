using Campus.Contracts;
using Campus.Kernel.Maps;
using Campus.Pipeline.Map;
using Godot;

namespace Campus.Kernel.Registry;

/// <summary>One frozen global asset version shared by all maps and bake workers.</summary>
[KernelService(typeof(GlobalBuildInputs), Owner = "WP-00")]
public sealed class GlobalBuildInputs
{
    public GlobalBuildInputs()
    {
        var keys = BuildInputAssembly.RequiredGlobalKeys;
        var paths = keys.Select(key => "res://data/" + key)
            .Append("res://style/palette/core.json")
            .Concat(DirAccess.GetFilesAt("res://style/palette/ext")
                .Where(name => name.EndsWith(".json", StringComparison.Ordinal))
                .Select(name => "res://style/palette/ext/" + name));
        var version = MapDataSource.CaptureInputVersion(paths);
        Snapshot = BuildInputAssembly.ReadRequiredGlobalInputs(keys,
            key => Godot.FileAccess.GetFileAsBytes("res://data/" + key), version);
    }

    public BuildInputSnapshot Snapshot { get; }
}
