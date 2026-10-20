using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;

namespace Campus.Gen.World.Tests;

internal static class TerrainCompilerTestFixture
{
    internal static readonly ZoneId Zone = new("fixture");

    internal static readonly TerrainAutoSettings Defaults = new(0.2, 1.5, 0.25, 1.2, 0.75);

    internal static CompiledTerrain Compile(IReadOnlyList<TerrainOp> operations, IReadOnlyList<WorldFeature>? features = null) =>
        TerrainCompiler.Compile(Context(features ?? []), [Source(operations)]);

    internal static float RenderedHeight(ITerrain terrain, float x, float z)
    {
        var tile = TileGrid.At(new Vector2(x, z));
        var origin = TileGrid.Origin(tile, LodLevel.L0);
        var cm = new short[TerrainSampling.SampleCount(0)]; terrain.FillTile(tile, 0, cm);
        var u = x - origin.X; var v = z - origin.Y;
        var ix = Math.Clamp((int)MathF.Floor(u), 0, TerrainSampling.TileQuads - 1);
        var iz = Math.Clamp((int)MathF.Floor(v), 0, TerrainSampling.TileQuads - 1);
        float H(int dx, int dz) => TerrainSampling.DecodeHeight(cm[(iz + dz) * TerrainSampling.TileSamples + ix + dx]);
        return Campus.Gen.Ground.CellSplit.Lerp(H(0, 0), H(1, 0), H(0, 1), H(1, 1), u - ix, v - iz);
    }

    internal static TerrainSource Source(IReadOnlyList<TerrainOp> operations) => new("world/fixture.terrain.json", new(Zone, operations));

    internal static TerrainCompileContext Context(IReadOnlyList<WorldFeature> features) => new(new("fixture"), new(new(-128), new(128)),
        Box(-512, -512, 512, 512), features.ToDictionary(f => f.Id), WorldNetwork.Empty, "fixture", Defaults,
        new Dictionary<ZoneId, IReadOnlyList<PolygonRings>> { [Zone] = [Box(-512, -512, 512, 512)] });

    internal static PolygonRings Box(float x0, float z0, float x1, float z1) => new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

    internal static JsonElement Kind(string kind) => JsonSerializer.SerializeToElement(new { kind });
}
