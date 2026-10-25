using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Maps;
using Campus.Gen.Tests.Support;
using Campus.Gen.World;
using Campus.Tests;

namespace Campus.Gen.Ground.Tests;

/// <summary>Small synthetic worlds: an east-west street with kerbs, a sidewalk, a plaza and a crossing footpath.</summary>
internal static class GroundTestSupport
{

    public static FakeWorld Empty() => Of([]);

    /// <summary>Final ground surface at a point, classified on a 0.25 m lattice of the L0 tile that holds it (one tile field per world and tile, shared by the probes).</summary>
    public static GroundSurface SurfaceAt(BuildContext context, float x, float z)
    {
        var key = TileGrid.At(new Vector2(x, z));
        var tile = new GroundTile(key.X * TileGrid.BaseEdge, key.Z * TileGrid.BaseEdge, TileGrid.BaseEdge, TileGrid.BaseEdge / 256f);
        var mesher = context.Memo((typeof(GroundTestSupport), key), () =>
        {
            var layout = GroundLayout.Of(context);
            var grid = TerrainGrid.For(FlatTerrain.Default, key, LodLevel.L0, tile);
            var field = TileField.Build(layout, tile, GroundSection.Default, grid);
            return new GroundMesher(field, GroundSection.Default, LodLevel.L0, layout, grid, new GroundSurfaceSink());
        });
        return mesher.SurfaceAt((int)MathF.Round((x - tile.X0) / tile.Cell), (int)MathF.Round((z - tile.Z0) / tile.Cell));
    }

    public static FakeWorld Of(IEnumerable<WorldFeature> features, WorldNetwork? network = null) => new()
    {
        Boundary = new([new(-200f, -200f), new(200f, -200f), new(200f, 200f), new(-200f, 200f)]),
        Features = features.ToDictionary(f => f.Id),
        Network = network ?? WorldNetwork.Empty,
    };

    public static FakeWorld Street()
    {
        var features = new List<WorldFeature>
        {
            WorldFeatureFixture.Create("osm:way:1", "road", new PolygonGeometry(Rect(-200f, -3f, 200f, 3f)), """{"highway":"residential"}"""),
            WorldFeatureFixture.Create("osm:way:2", "road", new PolygonGeometry(Rect(-200f, 3f, 200f, 5.5f)), """{"highway":"footway"}"""),
            WorldFeatureFixture.Create("osm:way:3", "road", new PolygonGeometry(Rect(20f, 8f, 40f, 20f)), """{"highway":"pedestrian"}"""),
            WorldFeatureFixture.Create("osm:way:4", "road", new PolygonGeometry(Rect(30f, -12f, 32.5f, 8f)), """{"highway":"footway"}"""),
            WorldFeatureFixture.Create("osm:way:5", "building", new PolygonGeometry(Rect(-40f, 20f, -20f, 40f)), "{}", height: 20f),
        };
        var roads = new List<NetworkLine>
        {
            new(new FeatureId("osm:way:1"), new Polyline([new(-200f, 0f), new(200f, 0f)]), 6f)
                { Props = JsonDocument.Parse("""{"highway":"residential"}""").RootElement.Clone() },
            new(new FeatureId("osm:way:4"), new Polyline([new(31.25f, -12f), new(31.25f, 8f)]), 2.5f)
                { Props = JsonDocument.Parse("""{"highway":"footway"}""").RootElement.Clone() },
        };
        return Of(features, new WorldNetwork(roads, [], [], [], []));
    }

    /// <summary>A T junction: an east-west road and a road running south from it, both 6 m wide.</summary>
    public static FakeWorld Junction()
    {
        var features = new List<WorldFeature>
        {
            WorldFeatureFixture.Create("osm:way:11", "road", new PolygonGeometry(Rect(-60f, -3f, 60f, 3f)), """{"highway":"residential"}"""),
            WorldFeatureFixture.Create("osm:way:12", "road", new PolygonGeometry(Rect(-3f, 0f, 3f, 60f)), """{"highway":"residential"}"""),
        };
        var roads = new List<NetworkLine>
        {
            new(new FeatureId("osm:way:11"), new Polyline([new(-60f, 0f), new(0f, 0f), new(60f, 0f)]), 6f)
                { Props = JsonDocument.Parse("""{"highway":"residential"}""").RootElement.Clone() },
            new(new FeatureId("osm:way:12"), new Polyline([new(0f, 0f), new(0f, 60f)]), 6f)
                { Props = JsonDocument.Parse("""{"highway":"residential"}""").RootElement.Clone() },
        };
        return Of(features, new WorldNetwork(roads, [], [], [], []));
    }

    public static BuildContext Context(IWorldModel world, float half = 1280f)
    {
        var rect = new MapRect(new(-half, -half), new(half, half));
        var map = new MapDescriptor(new MapId("test"), new LocalizedName("测试", "Test"), MapKind.Outdoor, 1, 1u,
            new GeoFrame(0d, 0d, 32648, new(-200f, -200f), new(200f, 200f)), Vector2.Zero,
            new Box3(new(-half, -28f, -half), new(half, 2000f, half)), rect,
            new RoamLimits(0.9f, 30f, 3000f, -28f, 2000f, 30f, 40f, 150f),
            new TileKey(-20, -20), new TileKey(19, 19), "P00", [],
            new MapWorldRules(MapTimeRule.Shared, new WorldPreset(17.5, Season.Autumn), new LocalizedName("落金初灯", "Golden Hour"), true, "chengdu"),
            new MapEnvironment("sky.json", "backdrop.json", []), []);
        return new BuildContext {
            CodeHash = "fixture", WindowNumbering = new((map).Id, new string('0', 64), []), Catalog = Campus.Contracts.Tests.PaletteFixture.Catalog, World = world, Map = map,
            WorldSeed = 1u, SourceInputs = MaterialInputs.Value.Entries, GlobalInputs = GlobalInputs.Value };
    }

    private static readonly Lazy<BuildInputSnapshot> GlobalInputs = new(() => BuildInputSnapshot.Freeze(
        new Dictionary<string, ReadOnlyMemory<byte>>
        {
            [SceneTextInputs.AtlasKey] = System.IO.File.ReadAllBytes(RepositoryFixture.FilePath("game", "data", "scene-text", "atlas.json"))
        }, "fixture"));

    private static readonly Lazy<BuildInputSnapshot> MaterialInputs = new(() => BuildInputSnapshot.Freeze(
        new Dictionary<string, ReadOnlyMemory<byte>>(StringComparer.Ordinal)
        {
            [SurfaceMaterialCatalog.SourcePath] = System.IO.File.ReadAllBytes(
                System.IO.Path.Combine(RealMapFixture.DataDirectory(), SurfaceMaterialCatalog.SourcePath))
        }, "fixture"));

    private static PolygonRings Rect(float x0, float z0, float x1, float z1) =>
        new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);

}
