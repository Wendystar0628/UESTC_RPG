using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Tests;

namespace Campus.Gen.Zones.North.Tests;

internal static class NorthTestSupport
{
    internal static IEnumerable<Vector2> Samples(IReadOnlyList<Vector2> line)
    {
        for (var i = 0; i + 1 < line.Count; i++)
        {
            var segment = new[] { line[i], line[i + 1] };
            if (segment[0] == segment[1]) continue;
            var length = PlanLines.Length(segment);
            if (length == 0) continue;
            var steps = Math.Max(1, (int)Math.Ceiling(length));
            for (var k = 0; k < steps; k++) yield return PlanLines.Sample(segment, length * k / steps).Point;
        }
        yield return line[^1];
    }

    internal static BuildContext Context() => new()
    {
        Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
        World = RealMapFixture.Load().World,
        Map = null!,
        WorldSeed = 1,
    };

    internal static TerrainOverlayFile Terrain() =>
        TerrainOverlayJson.Parse(RealMapFixture.ReadFiles()["world/north.terrain.json"].Span);
}
