using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Walkable;
using Campus.Gen.World;
using Campus.Gen.Zones.DesignWaterfront;

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
            var rect = descriptor.RootElement.GetProperty("dataRect").Deserialize<MapRect>(ContractJson.Options)
                ?? throw new InvalidDataException("Map dataRect is missing.");
            var loaded = WorldModelLoader.Load(map, files);
            var mapFile = descriptor.RootElement.Deserialize<MapFile>(ContractJson.Options)
                ?? throw new InvalidDataException("Map descriptor is missing.");
            var context = new BuildContext
            {
                Catalog = ReadCatalog(paletteRoot),
                World = loaded.World,
                Map = mapFile.Complete(loaded.Geo, loaded.Center,
                    new(new(rect.Min.X, mapFile.Roam.FloorY, rect.Min.Y), new(rect.Max.X, mapFile.Roam.CeilY, rect.Max.Y)),
                    new((int)MathF.Floor(rect.Min.X / 64f), (int)MathF.Floor(rect.Min.Y / 64f)),
                    new((int)MathF.Ceiling(rect.Max.X / 64f) - 1, (int)MathF.Ceiling(rect.Max.Y / 64f) - 1)),
                WorldSeed = mapFile.Seed,
                SourceInputs = files
            };
            var waterfront = new WaterfrontModule();
            var tiles = waterfront.Tiles(context).Distinct().OrderBy(tile => tile.Z).ThenBy(tile => tile.X).ToArray();
            var occupancy = tiles.SelectMany(tile => waterfront.Build(context, tile, LodLevel.L0).Occupancy ?? []).ToArray();
            var supports = tiles.SelectMany(tile => waterfront.Collide(context, tile)).OfType<RampCollider>()
                .Where(ramp => ramp.Tag == SurfaceTag.Ground).ToArray();
            var hash = WalkInputHash.Compute(files);
            var product = WalkableBaker.Build(loaded.World, map, rect, loaded.Boundary, hash, occupancy, supports);
            WalkBakeInputHash.RequireCurrent(product, files);
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
                inputHash = hash,
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
