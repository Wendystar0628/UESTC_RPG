using System.Numerics;
using System.Text.Json;
using Campus.Contracts;
using Campus.Contracts.Atlas;
using Campus.Contracts.Maps;
using Campus.Gen.Tests.Support;
using Campus.Tests;
using GdUnit4;
using static GdUnit4.Assertions;

namespace Campus.Pipeline.Map.Tests;

[TestSuite]
public sealed class MapBakeTests
{
    [TestCase]
    public void VectorDistancePreservesHolesAndBoundaryEncoding()
    {
        var polygon = new PolygonRings(Square(-8, -8, 8, 8).Outer, [Square(-2, -2, 2, 2).Outer]);
        AssertThat(MapDistance.Polygon(polygon, 4, 0)).IsEqual(-2d);
        AssertThat(MapDistance.Polygon(polygon, 0, 0)).IsEqual(2d);
        AssertThat(MapDistance.Polygon(polygon, 11, 0)).IsEqual(3d);
        AssertThat(MapDistance.Encode(-8)).IsEqual((byte)0); AssertThat(MapDistance.Encode(0)).IsEqual((byte)128); AssertThat(MapDistance.Encode(8)).IsEqual((byte)255);
        AssertThat(MapDistance.Encode(1)).IsEqual((byte)144);
    }

    [TestCase]
    public void MultiplePolygonsAndNarrowRoadSurviveNorthUpRasterization()
    {
        var multi = WorldFeatureFixture.Create("fixture:building", "building",
            new MultiPolygonGeometry([Square(-12, -12, -8, -8), Square(8, 8, 12, 12)]));
        var road = new NetworkLine(new("fixture:road"), new([new(-16, 0), new(16, 0)]), 0.1f);
        var world = World([multi], new([road], [], [], [], []));
        var product = MapBaker.Build(Input(world));
        var a = product.Textures[0];
        AssertThat(Pixel(a, 5, 5, 3) < 128 && Pixel(a, 26, 26, 3) < 128).OverrideFailureMessage("A multipolygon island was lost.").IsTrue();
        AssertThat(Pixel(a, 26, 5, 3)).IsEqual((byte)255);
        AssertThat(Pixel(a, 16, 16, 2) is > 128 and < 144).OverrideFailureMessage("Subtexel road distance was eroded.").IsTrue();
        AssertThat(product.Manifest.RowOrder).IsEqual("z-increasing");
    }

    [TestCase]
    public void MixedPoiUsesOnlyBuildingPartsAndLeavesCampusGroundUnfilled()
    {
        var grounds = new PolygonGeometry(Square(-14, -14, 14, 14));
        var poi = WorldFeatureFixture.Create("fixture:campus", "poi", grounds, source: FeatureSource.Osm, parts:
            [
                new("source:grounds", grounds, null, null) { Props = JsonSerializer.SerializeToElement(new { kind = "poi" }) },
                new("source:building-a", new MultiPolygonGeometry([Square(-12, -12, -8, -8), Square(8, 8, 12, 12)]), 15.6f, 4)
                    { Props = JsonSerializer.SerializeToElement(new { kind = "building" }) },
                new("source:building-b", new PolygonGeometry(Square(8, -12, 12, -8)), 58.5f, 15)
                    { Props = JsonSerializer.SerializeToElement(new { kind = "building" }) }
            ]);
        var product = MapBaker.Build(Input(World([poi])));
        var a = product.Textures[0];
        AssertThat(Pixel(a, 5, 5, 3) < 128 && Pixel(a, 26, 26, 3) < 128 && Pixel(a, 26, 5, 3) < 128).OverrideFailureMessage("A building part of the POI was omitted.").IsTrue();
        AssertThat(Pixel(a, 16, 16, 3)).IsEqual((byte)255);
        AssertThat(Pixel(product.Textures[1], 8, 8, 1)).IsEqual((byte)MapFeatureClass.None);
        AssertThat(Pixel(product.Textures[1], 2, 2, 1)).IsEqual((byte)MapFeatureClass.Building);
        var reordered = poi with { Parts = poi.Parts.Reverse().ToArray() };
        AssertThat(MapBakeCodec.Serialize(product).SequenceEqual(MapBakeCodec.Serialize(MapBaker.Build(Input(World([reordered])))))).OverrideFailureMessage("Source part insertion order changed map bytes.").IsTrue();
    }

    [TestCase]
    public void TextureRectsKeepCampusDetailSeparateFromDataAndPageCoverage()
    {
        var input = Input(World([]));
        var product = MapBaker.Build(input with
        {
            AtlasRect = new(new(-128, -128), new(128, 128)),
            Options = input.Options with { CampusMarginMetres = 64 }
        });
        AssertThat(product.Textures[0].Rect).IsEqual(new MapRect(new(-80, -80), new(80, 80)));
        AssertThat(product.Textures[1].Rect).IsEqual(product.Textures[0].Rect);
        AssertThat(product.Textures[2].Rect).IsEqual(input.Map.DataRect);
        AssertThat(product.Textures[3].Rect).IsEqual(new MapRect(new(-128, -128), new(128, 128)));
        AssertThat(product.Regions.Rect).IsEqual(input.Map.DataRect);
        MapBakeCodec.Validate(product);
    }

    [TestCase]
    public void MissingRegionalPolygonsReturnUnknownAndKnownRegionsClipAtTheCampus()
    {
        var input = Input(World([]));
        var unknown = MapBaker.Build(input);
        AssertThat(unknown.Regions.Cells.All(v => v == 0)).OverrideFailureMessage("Missing regional boundaries invented a region.").IsTrue();
        AssertThat(unknown.Regions.RegionAt(0, 0)).IsNull();
        var known = MapBaker.Build(input with { RegionAreas = [new("district", [Square(-16, -16, 16, 16)])] });
        AssertThat(known.Regions.RegionAt(0, 0)).IsEqual("district");
        AssertThat(known.Regions.RegionAt(16, 0)).IsNull();
        AssertThat(known.Regions.RegionAt(float.NaN, 0)).IsNull();
    }

    [TestCase]
    public void OutputIsIndependentOfFeatureDictionaryInsertionOrder()
    {
        var one = WorldFeatureFixture.Create("fixture:one", "water", new PolygonGeometry(Square(-8, -8, 0, 0)));
        var two = WorldFeatureFixture.Create("fixture:two", "green", new PolygonGeometry(Square(0, 0, 8, 8)));
        var a = MapBakeCodec.Serialize(MapBaker.Build(Input(World([one, two]))));
        var b = MapBakeCodec.Serialize(MapBaker.Build(Input(World([two, one]))));
        AssertThat(a.SequenceEqual(b)).OverrideFailureMessage("Dictionary insertion order changed bytes.").IsTrue();
        AssertThat(a.SequenceEqual(MapBakeCodec.Serialize(MapBakeCodec.Deserialize(a)))).OverrideFailureMessage("Map codec changed a valid product.").IsTrue();
    }

    [TestCase]
    public void CodecRejectsTruncationOversizedCountsAndCorruptTextureBytes()
    {
        var product = MapBaker.Build(Input(World([])));
        var bytes = MapBakeCodec.Serialize(product);
        AssertThrown(() => MapBakeCodec.Deserialize(bytes.AsSpan(0, bytes.Length - 1))).IsInstanceOf<InvalidDataException>();
        var badCount = (byte[])bytes.Clone(); badCount[8] = 200;
        AssertThrown(() => MapBakeCodec.Deserialize(badCount)).IsInstanceOf<InvalidDataException>();
        var files = MapBakeCodec.Files(product).ToDictionary(p => p.Key, p => (byte[])p.Value.Clone(), StringComparer.Ordinal);
        files["map_sdf_a.rgba8"][0] ^= 1;
        AssertThrown(() => MapBakeCodec.FromFiles(files)).IsInstanceOf<InvalidDataException>();
    }

    [TestCase]
    public void LabelPlacementNeedsRealMeasurementsAndProducesBothLanguages()
    {
        var source = Labels();
        var pending = LabelLayouter.Build(source, "label-input", null);
        AssertThat(!pending.Ready && pending.Bands.Count == 0).OverrideFailureMessage("Unmeasured font sizes were invented.").IsTrue();
        List<LabelMeasurement> measurements = [];
        foreach (string language in new[] { "zh", "en" })
            for (byte band = 0; band < 5; band++) measurements.Add(new("district-label", language, band, "District", new(60, 20)));
        var layout = new LabelLayoutInput(measurements, [], [0.4f, 0.6f, 0.9f, 1.4f, 2.1f], "font-metrics");
        var baked = LabelLayouter.Build(source, "label-input", layout);
        AssertThat(baked.Ready && baked.Bands.Count == 10 && baked.Bands.All(b => b.Labels.Count == 1)).OverrideFailureMessage("Language/zoom layout incomplete.").IsTrue();
        AssertThrown(() => LabelLayouter.Build(source, "label-input", layout with { Measurements = [] })).IsInstanceOf<ArgumentException>();
    }

    private static MapBakeInput Input(IWorldModel world)
    {
        var rect = new MapRect(new(-16, -16), new(16, 16));
        var map = new MapDescriptor(new("fixture"), new("Fixture", "Fixture"), MapKind.Outdoor, 1, 1,
            null!, Vector2.Zero, default, rect, null!, default, default, "", [], null!, null!, new Dictionary<string, string>(), []);
        return new(world, map, rect, Labels(), "world-hash", "code-hash", "label-hash")
        {
            Options = new() { MainSize = 32, ClassSize = 16, OutskirtsSize = 16, RingSize = 16, CampusMarginMetres = 0 }
        };
    }

    private static FakeWorld World(IReadOnlyList<WorldFeature> features, WorldNetwork? network = null) => new()
    {
        Features = features.ToDictionary(f => f.Id),
        Network = network ?? WorldNetwork.Empty,
        Boundary = Square(-16, -16, 16, 16),
    };

    private static AtlasLabels Labels() => new(1, [new AtlasRegion("district", new("District", "District"), null)], [],
        [new("district-label", "region", 100, Ref: "district", At: Vector2.Zero)]);
    private static PolygonRings Square(float x0, float z0, float x1, float z1) => new([new(x0, z0), new(x1, z0), new(x1, z1), new(x0, z1)]);
    private static byte Pixel(MapTexture texture, int x, int z, int c) => texture.Rgba8[(z * texture.Width + x) * 4 + c];
}
