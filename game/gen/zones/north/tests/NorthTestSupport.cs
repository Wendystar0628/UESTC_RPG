using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Gen.World;

namespace Campus.Gen.Zones.North.Tests;

internal static class NorthTestSupport
{
    internal static float RingDistance(Vector2 p, IReadOnlyList<Vector2> ring) =>
        Enumerable.Range(0, ring.Count).Min(i => DistanceToSegment(p, ring[i], ring[(i + 1) % ring.Count]));

    internal static float PolylineDistance(Vector2 p, IReadOnlyList<Vector2> line) =>
        Enumerable.Range(0, line.Count - 1).Min(i => DistanceToSegment(p, line[i], line[i + 1]));

    internal static string? Text(JsonElement? props, string name) =>
        props is { ValueKind: JsonValueKind.Object } p && p.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    internal static IEnumerable<Vector2> Samples(IReadOnlyList<Vector2> line)
    {
        for (var i = 0; i + 1 < line.Count; i++)
        {
            var steps = Math.Max(1, (int)MathF.Ceiling(Vector2.Distance(line[i], line[i + 1])));
            for (var k = 0; k < steps; k++) yield return Vector2.Lerp(line[i], line[i + 1], (float)k / steps);
        }
        yield return line[^1];
    }

    internal static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var t = Math.Clamp(Vector2.Dot(p - a, ab) / ab.LengthSquared(), 0f, 1f);
        return Vector2.Distance(p, a + ab * t);
    }

    internal static BuildContext Context() => new() { Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = World(), Map = null!, WorldSeed = 1 };

    internal static string DataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe", "campus.json"))) directory = directory.Parent;
        if (directory is null) throw new InvalidOperationException("Repository campus fixture not found.");
        return Path.Combine(directory.FullName, "game", "data", "maps", "qingshuihe");
    }

    internal static TerrainOverlayFile Terrain() =>
        TerrainOverlayJson.Parse(File.ReadAllBytes(Path.Combine(DataDirectory(), "overlay", "terrain", "north.terrain.json")));

    internal static IWorldModel World()
    {
        var data = DataDirectory();
        var files = new[] { "campus.json", "network.json", "interiors.json", "map.json" }
            .Concat(Directory.GetFiles(Path.Combine(data, "overlay", "north.d"), "*.json").Order(StringComparer.Ordinal)
                .Select(path => Path.GetRelativePath(data, path).Replace('\\', '/')))
            .ToDictionary(name => name, name => File.ReadAllText(Path.Combine(data, name)), StringComparer.Ordinal);
        return WorldModelLoader.Load(new("qingshuihe"), files).World;
    }

    internal static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
