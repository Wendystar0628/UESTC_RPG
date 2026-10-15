using System.Text.Json;
using Campus.Contracts;
using Godot;

namespace Campus.Style.Palette;

/// <summary>
/// 色名登记表的加载（ART-STYLE §8.5）：读核心表 core.json 和各包的扩展表 ext/WP-*.json，合成一个 <see cref="PaletteRegistry"/>。
/// 色名重复、色号冲突、成对不全时抛异常。登记表由 WP-01 统一注册成服务；调色板纹理由 WP-37 创建并每帧写入。
/// </summary>
public static class PaletteRegistryLoader
{
    public const string CorePath = "res://style/palette/core.json";
    public const string ExtDirectory = "res://style/palette/ext";

    public static PaletteRegistry Load()
    {
        var entries = new List<PaletteEntry>(Parse(ReadRequired(CorePath), CorePath).Entries);
        if (DirAccess.DirExistsAbsolute(ExtDirectory))
        {
            // ext 里只收各包的色名扩展（WP-xx.json）；界面令牌 ui.json 是另一种格式，不在这里读。
            var files = ResourceLoader.ListDirectory(ExtDirectory)
                .Where(name => name.StartsWith("WP-", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.Ordinal))
                .Order(StringComparer.Ordinal);
            foreach (var name in files)
            {
                var path = ExtDirectory + "/" + name;
                entries.AddRange(Parse(ReadRequired(path), path).Entries);
            }
        }
        return new PaletteRegistry(entries);
    }

    private static PaletteRegistryData Parse(byte[] json, string path)
    {
        var data = JsonSerializer.Deserialize<PaletteRegistryData>(json, ContractJson.Options)
            ?? throw new InvalidDataException($"Empty palette registry: {path}");
        if (data.Schema != 1) throw new InvalidDataException($"Unsupported palette registry schema {data.Schema}: {path}");
        return data;
    }

    private static byte[] ReadRequired(string path)
    {
        var bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0) throw new FileNotFoundException($"Missing palette registry: {path}");
        return bytes;
    }
}
