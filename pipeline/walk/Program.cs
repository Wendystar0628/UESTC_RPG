using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Walkable;

namespace Campus.Pipeline.Walk;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            var arguments = ParseArguments(args);
            var dataRoot = arguments["--data-root"];
            var paletteRoot = arguments["--palette-root"];
            var output = arguments["--out"];
            if (!Path.IsPathFullyQualified(dataRoot) || !Path.IsPathFullyQualified(paletteRoot) || !Path.IsPathFullyQualified(output))
                throw new ArgumentException("--data-root, --palette-root and --out must be absolute paths.");
            var files = ReadInputs(dataRoot);
            using var descriptor = JsonDocument.Parse(files["map.json"]);
            var map = new MapId(descriptor.RootElement.GetProperty("id").GetString() ?? throw new InvalidDataException("Map id is missing."));
            if (arguments.TryGetValue("--map", out var expectedMap) && map.Value != expectedMap)
                throw new InvalidDataException("--map does not match the supplied data root.");
            var product = WalkBakeGenerator.Build(files, ReadCatalog(paletteRoot));
            Directory.CreateDirectory(output);
            var destination = Path.Combine(output, "walk.bake.json");
            File.WriteAllBytes(destination, JsonSerializer.SerializeToUtf8Bytes(product, ContractJson.Options));
            var classCounts = Enum.GetValues<WalkClass>().Select(value => new
            {
                name = value.ToString(),
                count = product.Walk.Classes.Count(item => item == (byte)value)
            }).ToArray();
            var report = new
            {
                map = map.Value,
                inputHash = product.InputHash,
                inputs = files.Keys.Order(StringComparer.Ordinal).ToArray(),
                walkCells = product.Walk.Classes.Length,
                classes = classCounts,
                nearBoundaryCells = product.Walk.Flags.Count(value => (value & 1) != 0),
                safeCells = product.SafePoints.Width * product.SafePoints.Height,
                mappedSafeCells = product.SafePoints.Coordinates.Where((_, index) => index % 3 == 0).Count(value => value != short.MinValue),
                walls = product.Walls.Count,
                outputBytes = new FileInfo(destination).Length
            };
            File.WriteAllBytes(Path.Combine(output, "walk.report.json"), JsonSerializer.SerializeToUtf8Bytes(report, ContractJson.Options));
            Console.WriteLine(JsonSerializer.Serialize(report, ContractJson.Options));
            return 0;
        }
        catch (Exception error) when (error is ArgumentException or IOException or JsonException or InvalidOperationException or OverflowException)
        {
            Console.Error.WriteLine(error.Message);
            return 1;
        }
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || args[index] is not ("--data-root" or "--palette-root" or "--out" or "--map") || !result.TryAdd(args[index], args[index + 1]))
                throw new ArgumentException("Usage: --data-root <absolute map data directory> --palette-root <absolute palette directory> --out <absolute boot staging directory> [--map <id>]");
        }
        if (!result.ContainsKey("--data-root") || !result.ContainsKey("--palette-root") || !result.ContainsKey("--out"))
            throw new ArgumentException("--data-root, --palette-root and --out are required.");
        return result;
    }

    private static Dictionary<string, ReadOnlyMemory<byte>> ReadInputs(string root)
    {
        var candidates = Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
            .ToDictionary(path => Path.GetRelativePath(root, path).Replace('\\', '/'), StringComparer.Ordinal);
        var files = new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal);
        foreach (var name in WalkBakeInputHash.SelectSourceNames(candidates.Keys))
            files.Add(name, File.ReadAllBytes(candidates[name]));
        return files;
    }

    private static PaletteCatalog ReadCatalog(string root)
    {
        var bank0 = new[] { Path.Combine(root, "core.json") }
            .Concat(Directory.GetFiles(Path.Combine(root, "ext"), "WP-*.json").Order(StringComparer.Ordinal));
        var bank1 = Directory.GetFiles(Path.Combine(root, "ext-v2"), "WP-*.json").Order(StringComparer.Ordinal);
        return PaletteCatalog.FromSources(bank0.Select(path => (path, File.ReadAllBytes(path))),
            bank1.Select(path => (path, File.ReadAllBytes(path))));
    }
}
