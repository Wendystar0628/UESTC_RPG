using System.Text.Json;
using Campus.Contracts;
using Godot;

namespace Campus.Style.Palette;

/// <summary>
/// 色名登记表的加载（ART-STYLE §8.5）：读核心表 core.json 和各包的扩展表 ext/WP-*.json，合成一个 <see cref="PaletteRegistry"/>。
/// 色名重复、色号冲突、成对不全时抛异常。登记表由 WP-01 统一注册成服务；调色板纹理由 WP-37 创建并每帧写入。
/// 第二段色号 256–511（contract-28）另放在 ext-v2/WP-*.json（schema 2），由 <see cref="LoadCatalog()"/> 和旧表合成
/// <see cref="PaletteCatalog"/>；旧的 ext/ 只收 schema 1，两种格式不混读。
/// </summary>
public static class PaletteRegistryLoader
{
    public const string CorePath = "res://style/palette/core.json";
    public const string ExtDirectory = "res://style/palette/ext";
    public const string ExtV2Directory = "res://style/palette/ext-v2";

    public static PaletteRegistry Load()
    {
        var entries = new List<PaletteEntry>(Parse(ReadRequired(CorePath), CorePath).Entries);
        foreach (var path in PackageFiles(ExtDirectory))
            entries.AddRange(Parse(ReadRequired(path), path).Entries);
        return new PaletteRegistry(entries);
    }

    /// <summary>
    /// 完整色表：旧表（<see cref="Load"/>）加 ext-v2 的扩展色。全局色名与色号唯一、受光偶数与影成对、256–511 的边界、
    /// 扩展色不带季相，都由 <see cref="PaletteCatalog"/> 的构造校验，不通过就抛异常。
    /// </summary>
    public static PaletteCatalog LoadCatalog() =>
        LoadCatalog(Load(), PackageFiles(ExtV2Directory).Select(path => (path, ReadRequired(path))));

    /// <summary>纯 .NET 的合成入口（测试与工具用）：扩展表按给定顺序读，只认 schema 2，不读成旧表的 byte 色号。</summary>
    public static PaletteCatalog LoadCatalog(PaletteRegistry legacy, IEnumerable<(string Path, byte[] Json)> extensions)
    {
        ArgumentNullException.ThrowIfNull(legacy);
        ArgumentNullException.ThrowIfNull(extensions);
        var entries = new List<PaletteEntryV2>();
        foreach (var (path, json) in extensions) entries.AddRange(ParseExtension(json, path).Entries);
        try
        {
            return new PaletteCatalog(legacy, entries);
        }
        catch (ArgumentException e)
        {
            throw new InvalidDataException($"Invalid palette extension (ext-v2): {e.Message}", e);
        }
    }

    private static PaletteRegistryData Parse(byte[] json, string path)
    {
        var data = JsonSerializer.Deserialize<PaletteRegistryData>(json, ContractJson.Options)
            ?? throw new InvalidDataException($"Empty palette registry: {path}");
        if (data.Schema != 1) throw new InvalidDataException($"Unsupported palette registry schema {data.Schema}: {path}");
        return data;
    }

    private static PaletteExtensionData ParseExtension(byte[] json, string path)
    {
        PaletteExtensionData? data;
        try
        {
            data = JsonSerializer.Deserialize<PaletteExtensionData>(json, ContractJson.Options);
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"Invalid palette extension {path}: {e.Message}", e);
        }
        if (data is null) throw new InvalidDataException($"Empty palette extension: {path}");
        if (data.Schema != PaletteLayout.ExtensionSchema)
            throw new InvalidDataException($"Unsupported palette extension schema {data.Schema}: {path}");
        return data;
    }

    // 目录里只收各包的色名表（WP-xx.json）；ext/ 里的界面令牌 ui.json 是另一种格式，不在这里读。
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
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing palette registry: {path}");
        return bytes;
    }
}
