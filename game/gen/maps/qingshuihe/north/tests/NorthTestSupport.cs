using System.Numerics;
using Campus.Contracts;
using Campus.Gen.Geometry;
using Campus.Tests;

namespace Campus.Gen.Maps.Qingshuihe.North.Tests;

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

    internal static BuildContext Context()
    {
        var world = RealMapFixture.Load().World;
        var module = new NorthModule();
        var claims = module.Claims.OfType<FeatureClaim>().Select(c => c.Feature).ToHashSet();
        var inputs = BuildInputSnapshot.Freeze(new Dictionary<string, ReadOnlyMemory<byte>>
        {
            [SceneTextInputs.AtlasKey] = System.IO.File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "scene-text", "atlas.json"))
        }, "fixture");
        var context = new BuildContext
        {
            CodeHash = "fixture", WindowNumbering = new((RealMapFixture.Entry().Descriptor).Id, new string('0', 64), []),
            Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog,
            World = world,
            Map = RealMapFixture.Entry().Descriptor,
            SourceInputs = RealMapFixture.WindowInputs(RealMapFixture.Entry().Descriptor.Id),
            WorldSeed = 1,
            GlobalInputs = inputs,
            ClaimedBy = (feature, _) => claims.Contains(feature) ? "zone.north" : null,
        };
        var numbering = new Campus.Gen.Buildings.BuildingsModule()
            .PrepareWindowNumbering(context, module.DescribeWindowLayouts(world, context.WorldSeed));
        return new BuildContext {
            CodeHash = "fixture", Catalog = context.Catalog, World = world, Map = context.Map,
            WorldSeed = context.WorldSeed, GlobalInputs = inputs, ClaimedBy = context.ClaimedBy, WindowNumbering = numbering };
    }

    internal static TerrainOverlayFile Terrain() =>
        TerrainOverlayJson.Parse(RealMapFixture.ReadFiles()["world/north.terrain.json"].Span);
}
