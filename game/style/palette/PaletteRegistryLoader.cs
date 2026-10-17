using Campus.Contracts;
using Godot;

namespace Campus.Style.Palette;

/// <summary>只读取基础 bank 0 与扩展 bank 1 的源文件，由 PaletteCatalog 统一解析和校验，不创建 GPU 资源。</summary>
public static class PaletteRegistryLoader
{
    public const string CorePath = "res://style/palette/core.json";
    public const string ExtDirectory = "res://style/palette/ext";
    public const string ExtV2Directory = "res://style/palette/ext-v2";

    public static PaletteCatalog LoadCatalog() => PaletteCatalog.FromSources(
        new[] { CorePath }.Concat(PackageFiles(ExtDirectory)).Select(path => (path, ReadRequired(path))),
        PackageFiles(ExtV2Directory).Select(path => (path, ReadRequired(path))));

    // ext/ui.json 是界面令牌，不属于场景色表。
    private static IEnumerable<string> PackageFiles(string directory)
    {
        if (!DirAccess.DirExistsAbsolute(directory)) return [];
        return ResourceLoader.ListDirectory(directory)
            .Where(name => name.StartsWith("WP-", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(name => directory + "/" + name)
            .ToArray();
    }

    private static byte[] ReadRequired(string path)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing palette data: {path}");
        return bytes;
    }
}
